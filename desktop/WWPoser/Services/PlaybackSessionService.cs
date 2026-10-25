namespace WWPoser.Services;

public enum PlaybackSource { CurrentCharacter, CurrentSquad, CreatedCharacter, MixedActors }
// Stage coordinates are relative to the controlled character when playback starts:
// X to its right, Y ahead, Z above its floor (cm), Yaw relative to its facing.
public sealed record StagePosition(double X, double Y, double Z = 0, double Yaw = 0);
public sealed record PlaybackActorRequest(string BodySource, string? FaceSource = null, double? Scale = null, SquadPlacement? Placement = null, long RoleId = 0, bool Enabled = true, string? CharacterId = null, uint Slot = 0, StagePosition? Stage = null);
public sealed record PlaybackSessionRequest(PlaybackSource Source, IReadOnlyList<PlaybackActorRequest> Actors, string? CameraSource = null, int CameraMode = 0, double Speed = 1, bool Loop = false, double DurationSeconds = 0, bool NativePhysics = true, bool Physics = false, string? StageSession = null, double SpacingCm = 180, bool FormationNative = false);

// New requests use one dynamic actor host. The old source formats remain
// readable so saved requests can be converted to the mixed actor preparation.
public sealed class PlaybackSessionService
{
    public const string RequestFilename = "unified_session_request.json";
    private readonly string root;
    private readonly SquadService squad;
    private readonly OwnedMmdPlaybackService owned;
    public string? ActiveSession { get; private set; }
    public event Action<string>? Progress;

    public PlaybackSessionService(string repoRoot)
    {
        root = Path.GetFullPath(repoRoot);
        squad = new(root); owned = new(root);
        owned.Progress += message => Progress?.Invoke(message);
    }

    public JsonObject ReadRoster(string directory) { squad.Configure(directory); return squad.ReadRoster(); }
    public Task<JsonObject> RefreshRosterAsync(string directory, CancellationToken cancellation = default) { squad.Configure(directory); return squad.RefreshRosterAsync(cancellation); }
    public Task StopSamplerAsync(CancellationToken cancellation = default) => squad.StopSamplerAsync(cancellation);
    public async Task StopPendingAsync(CancellationToken cancellation = default)
    {
        await squad.StopSamplerAsync(cancellation);
        if (owned.ActiveSession is not null) await owned.FinishCurrentAsync(cancellation);
        ActiveSession = owned.ActiveSession;
    }

    public static PlaybackSessionRequest AssignSlots(PlaybackSessionRequest request)
    {
        if (!request.Actors.Any(actor => actor.Enabled)) throw new InvalidDataException("请至少添加一名演员。");
        var slots = new HashSet<uint>(request.Actors.Where(actor => actor.Slot != 0).Select(actor => actor.Slot));
        if (slots.Count != request.Actors.Count(actor => actor.Slot != 0)) throw new InvalidDataException("演员槽位重复。");
        uint next = 1;
        var actors = request.Actors.Select(actor =>
        {
            if (actor.Slot != 0) return actor;
            while (slots.Contains(next)) next = checked(next + 1);
            var slot = next; slots.Add(slot); return actor with { Slot = slot };
        }).ToArray();
        if (!double.IsFinite(request.Speed) || request.Speed < .1 || request.Speed > 4) throw new InvalidDataException("速度须在 0.1–4 倍之间。");
        if (!double.IsFinite(request.DurationSeconds) || request.DurationSeconds < 0 || request.DurationSeconds * 1000 > int.MaxValue) throw new InvalidDataException("播放时限无效。");
        if (!double.IsFinite(request.SpacingCm) || request.SpacingCm < 0) throw new InvalidDataException("演员间距无效。");
        if (request.CameraMode < 0 || request.CameraMode > 2 || (request.CameraMode == 2 && string.IsNullOrWhiteSpace(request.CameraSource))) throw new InvalidDataException("VMD 镜头模式需要镜头文件。");
        return request with { Source = PlaybackSource.MixedActors, Actors = actors, StageSession = null };
    }

    public PlaybackSessionRequest ReadPreparation(string directory)
    {
        var path = Path.Combine(directory, RequestFilename);
        if (File.Exists(path))
        {
            var saved = AssignSlots(JsonSerializer.Deserialize<PlaybackSessionRequest>(File.ReadAllText(path))!);
            var configPath = Path.Combine(directory, "owned_mmd_config.json");
            if (!File.Exists(configPath)) return saved;
            var frozen = OwnedMmdPlaybackService.Config(directory);
            var bySlot = OwnedMmdPlaybackService.ActorConfigs(frozen).ToDictionary(row => row["slot"]?.GetValue<uint>() ?? 1);
            var actors = saved.Actors.Select(actor =>
            {
                if (!actor.Enabled || !bySlot.TryGetValue(actor.Slot, out var row)) return actor;
                var actorDirectory = Path.Combine(directory, row["actor_directory"]?.GetValue<string>() ?? "");
                var body = Path.Combine(actorDirectory, "source.vmd"); var face = Path.Combine(actorDirectory, "face_source.vmd");
                return actor with { BodySource = File.Exists(body) ? body : actor.BodySource, FaceSource = File.Exists(face) ? face : actor.FaceSource,
                    CharacterId = actor.CharacterId ?? row["character_id"]?.GetValue<string>() };
            }).ToArray();
            var camera = Path.Combine(directory, "camera_source.vmd");
            return saved with { Actors = actors, CameraSource = File.Exists(camera) ? camera : saved.CameraSource };
        }
        var config = OwnedMmdPlaybackService.Config(directory);
        if (config["actors"] is JsonArray)
        {
            var actors = OwnedMmdPlaybackService.ActorConfigs(config).Select(row =>
            {
                var actorDirectory = Path.Combine(directory, Contract.S(row, "actor_directory"));
                var p = Contract.O(row, "placement");
                return new PlaybackActorRequest(Path.Combine(actorDirectory, "source.vmd"),
                    File.Exists(Path.Combine(actorDirectory, "face_source.vmd")) ? Path.Combine(actorDirectory, "face_source.vmd") : null,
                    Placement: new(Contract.D(p, "offset_x_cm"), Contract.D(p, "offset_y_cm"), Contract.D(p, "height_cm"), Contract.D(p, "yaw_degrees"), Contract.True(p, "in_place")),
                    RoleId: Contract.I(row, "role_id"), CharacterId: Contract.S(row, "character_id"), Slot: row["slot"]!.GetValue<uint>(), Stage: OwnedMmdPlaybackService.ReadStage(row));
            }).ToArray();
            return new(PlaybackSource.MixedActors, actors, File.Exists(Path.Combine(directory, "camera_source.vmd")) ? Path.Combine(directory, "camera_source.vmd") : null,
                config["camera_mode"]?.GetValue<int>() ?? 0, Contract.D(config, "speed"), Contract.True(config, "loop"), Contract.D(config, "duration_ms") / 1000d,
                config["native_physics"]?.GetValue<bool>() ?? true, Contract.True(config, "physics_enabled"), SpacingCm: config["spacing_cm"]?.GetValue<double>() ?? 180,
                FormationNative: config["formation_native"]?.GetValue<bool>() ?? false);
        }
        var actor = new PlaybackActorRequest(Path.Combine(directory, "source.vmd"), CharacterId: config["character_id"]?.GetValue<string>());
        return new(PlaybackSource.CreatedCharacter, [actor], CameraMode: config["camera_mode"]?.GetValue<int>() ?? 0, Speed: Contract.D(config, "speed"), Loop: Contract.True(config, "loop"), DurationSeconds: Contract.D(config, "duration_ms") / 1000d, NativePhysics: config["native_physics"]?.GetValue<bool>() ?? true, Physics: Contract.True(config, "physics_enabled"));
    }

    private static int Milliseconds(double seconds) => checked((int)Math.Round(seconds * 1000));
    private static void WriteRequest(string directory, PlaybackSessionRequest request) => JsonData.Write(Path.Combine(directory, RequestFilename), JsonSerializer.SerializeToNode(request)!.AsObject());
    private string CompilationFingerprint(PlaybackSessionRequest request)
    {
        request = AssignSlots(request);
        string? Digest(string? file) => string.IsNullOrWhiteSpace(file) ? null : JsonData.Digest(file);
        var actors = new JsonArray(request.Actors.Select(actor => (JsonNode)new JsonObject
        {
            ["slot"] = actor.Slot, ["role"] = actor.RoleId, ["character"] = actor.CharacterId, ["enabled"] = actor.Enabled,
            ["body"] = actor.Enabled ? Digest(actor.BodySource) : null,
            ["face"] = actor.Enabled ? Digest(actor.FaceSource) : null, ["scale"] = actor.Scale
        }).ToArray());
        var forms = Path.Combine(root, "data", "characters", "character_forms.json");
        return JsonData.CanonicalDigest(new JsonObject { ["character_loading_version"] = 2,
            ["catalog"] = Digest(Path.Combine(root, OwnedCharacterCatalog.RelativePath)), ["character_index"] = Digest(Path.Combine(root, "data", "characters", "index.json")),
            ["forms"] = File.Exists(forms) ? Digest(forms) : null,
            ["source"] = (int)request.Source, ["actors"] = actors, ["camera"] = Digest(request.CameraSource), ["stage"] = request.Source == PlaybackSource.CurrentSquad ? Digest(Path.Combine(request.StageSession!, "stage_config.json")) : null });
    }
    public bool RequiresPreparation(string directory, PlaybackSessionRequest request)
    {
        var path = Path.Combine(directory, "preparation_inputs.json");
        var recorded = File.Exists(path) ? JsonData.ReadObject(path)["compilation_sha256"]?.GetValue<string>() : null;
        return recorded != CompilationFingerprint(request);
    }

    public async Task<JsonObject> PrepareAsync(PlaybackSessionRequest request, CancellationToken cancellation = default)
    {
        request = AssignSlots(request);
        string directory;
        JsonObject result;
        directory = Path.Combine(root, "playback", "actor_preparations", "actors_" + Guid.NewGuid().ToString("N"));
        result = await owned.PrepareActorsAsync(request, directory, cancellation);
        WriteRequest(directory, request);
        JsonData.Write(Path.Combine(directory, "preparation_inputs.json"), new JsonObject { ["compilation_sha256"] = CompilationFingerprint(request) });
        return result;
    }

    public void UpdatePreparation(string directory, PlaybackSessionRequest request)
    {
        request = AssignSlots(request);
        WriteRequest(directory, request);
        if (request.Source == PlaybackSource.MixedActors)
        {
            var mixed = OwnedMmdPlaybackService.Config(directory);
            mixed["speed"] = request.Speed; mixed["loop"] = request.Loop; mixed["duration_ms"] = Milliseconds(request.DurationSeconds); mixed["camera_mode"] = request.CameraMode;
            mixed["camera_enabled"] = true; mixed["native_physics"] = request.NativePhysics; mixed["physics_enabled"] = request.Physics && !request.NativePhysics;
            mixed["physics_mode"] = request.NativePhysics ? "native" : request.Physics ? "secondary_inertia" : "direct";
            var bySlot = request.Actors.Where(actor => actor.Enabled).ToDictionary(actor => actor.Slot);
            foreach (var actor in OwnedMmdPlaybackService.ActorConfigs(mixed))
            {
                if (!bySlot.TryGetValue(actor["slot"]?.GetValue<uint>() ?? 1, out var selected)) throw new InvalidDataException("演员集合已更改，需要重新准备。");
                actor["placement"] = PlaybackAssets.Placement(selected.Placement); OwnedMmdPlaybackService.SetStage(actor, selected.Stage);
                foreach (var key in new[] { "native_physics", "physics_enabled", "physics_mode" }) actor[key] = mixed[key]!.DeepClone();
            }
            OwnedMmdPlaybackService.ConfigureStage(mixed, request.SpacingCm, request.FormationNative);
            OwnedMmdPlaybackService.WriteActorsFile(directory, mixed);
            JsonData.Write(Path.Combine(directory, "owned_mmd_config.json"), mixed);
            return;
        }
    }

    public Task<JsonObject> LaunchAsync(string preparedDirectory, CancellationToken cancellation = default) => LaunchAsync(preparedDirectory, null, cancellation);
    public async Task<JsonObject> LaunchAsync(string preparedDirectory, PlaybackSessionRequest? settings, CancellationToken cancellation = default, bool startPaused = false)
    {
        if (ActiveSession is not null) throw new InvalidOperationException("请先停止当前演员会话并等待恢复。");
        var request = AssignSlots(settings ?? ReadPreparation(preparedDirectory));
        if (RequiresPreparation(preparedDirectory, request))
        {
            Progress?.Invoke("素材或角色已更改，正在重新准备");
            var replacement = await PrepareAsync(request, cancellation);
            preparedDirectory = Contract.S(replacement, "session_dir");
        }
        UpdatePreparation(preparedDirectory, request);
        try
        {
            var result = await owned.LaunchAsync(preparedDirectory, cancellation, startPaused);
            var directory = Contract.S(result, "session_dir");
            WriteRequest(directory, request);
            ActiveSession = Contract.True(result, "cleanup_restored") ? null : directory;
            return result;
        }
        catch { ActiveSession = owned.ActiveSession; throw; }
    }

    public async Task<JsonObject> StartAsync(PlaybackSessionRequest request, CancellationToken cancellation = default, bool startPaused = false)
    {
        var prepared = await PrepareAsync(request, cancellation);
        return await LaunchAsync(Contract.S(prepared, "session_dir"), null, cancellation, startPaused);
    }

    // Preview needs characters and stage positions only, never motion files.
    public async Task<JsonObject> PreviewAsync(PlaybackSessionRequest request, CancellationToken cancellation = default)
    {
        if (ActiveSession is not null) throw new InvalidOperationException("请先停止当前演员会话并等待恢复。");
        try
        {
            var result = await owned.PreviewAsync(request, cancellation);
            ActiveSession = Contract.True(result, "loaded") ? Contract.S(result, "session_dir") : owned.ActiveSession;
            return result;
        }
        catch { ActiveSession = owned.ActiveSession; throw; }
    }

    public JsonObject ReadStatus(string directory) => PlaybackSessionStatus.Read(directory).State;
    public Task<JsonObject> SendAsync(string directory, string kind, double value = 0, uint targetSlot = 0, CancellationToken cancellation = default) => PlaybackControl.SendAsync(directory, kind, value, cancellation, targetSlot);
    public async Task<JsonObject> StopAsync(string directory, CancellationToken cancellation = default)
    {
        await owned.FinishSessionAsync(directory, cancellation);
        ActiveSession = owned.ActiveSession;
        return new JsonObject { ["session_dir"] = directory, ["state"] = "restored", ["restoration_complete"] = true };
    }
}
