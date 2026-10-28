namespace WWPoser.Services;

public sealed partial class PlaybackSessionService
{
    public static PlaybackSessionRequest AssignSlots(PlaybackSessionRequest request)
    {
        var slots = new HashSet<uint>(request.Actors.Where(actor => actor.Slot != 0).Select(actor => actor.Slot));
        if (slots.Count != request.Actors.Count(actor => actor.Slot != 0)) throw new InvalidDataException("演员槽位重复。");
        uint next = 1;
        return request with { Actors = request.Actors.Select(actor =>
        {
            if (actor.Slot != 0) return actor;
            while (slots.Contains(next)) next = checked(next + 1);
            var slot = next; slots.Add(slot); return actor with { Slot = slot };
        }).ToArray() };
    }

    internal static void ValidateRequest(PlaybackSessionRequest request, bool preview = false)
    {
        request.Validate();
        var selected = request.Actors.Where(actor => actor.Enabled).ToArray();
        if (selected.Length == 0) throw new InvalidDataException("请至少添加一名演员。");
        if (selected.Any(actor => string.IsNullOrWhiteSpace(actor.CharacterId))) throw new InvalidDataException("请为每名参与演员选择角色。");
        if (preview) return;
        if (selected.Any(actor => string.IsNullOrWhiteSpace(actor.BodySource))) throw new InvalidDataException("请为参与演员选择身体动作。");
        if (request.CameraMode == 2 && string.IsNullOrWhiteSpace(request.CameraSource)) throw new InvalidDataException("VMD 镜头模式需要镜头文件。");
    }

    public PlaybackSessionRequest ReadPreparation(string directory)
    {
        var savedPath = Path.Combine(directory, RequestFilename);
        var saved = File.Exists(savedPath) ? JsonSerializer.Deserialize<PlaybackSessionRequest>(File.ReadAllText(savedPath)) : null;
        var configPath = Path.Combine(directory, "owned_mmd_config.json");
        var config = File.Exists(configPath) ? OwnedMmdPlaybackService.Config(directory) : null;
        if (saved is null && config is null) throw new InvalidDataException("目录中没有可读取的演员准备配置。");
        var rows = config is null ? [] : OwnedMmdPlaybackService.ActorConfigs(config);
        PlaybackActorRequest FromConfig(JsonObject row)
        {
            var actorDirectory = Path.Combine(directory, row["actor_directory"]?.GetValue<string>() ?? "");
            var placement = row["placement"] as JsonObject;
            var scale = row["requested_scale"]?.GetValue<double>();
            var reportPath = Path.Combine(actorDirectory, "compiled_report.json");
            if (!row.ContainsKey("requested_scale") && File.Exists(reportPath))
            {
                var clip = JsonData.ReadObject(reportPath)["compiled_target_clip"];
                if (clip?["translation_scale_source"]?.GetValue<string>() == "explicit CLI --scale override")
                    scale = clip["source_to_target_scale"]?.GetValue<double>();
            }
            return new(Path.Combine(actorDirectory, PlaybackAssets.BodySourceFile),
                PlaybackAssets.ReadSource(actorDirectory, PlaybackAssets.FaceSourceFile), scale,
                placement is null ? null : new(Contract.D(placement, "offset_x_cm"), Contract.D(placement, "offset_y_cm"), Contract.D(placement, "height_cm"), Contract.D(placement, "yaw_degrees"), Contract.True(placement, "in_place")),
                RoleId: row["role_id"]?.GetValue<long>() ?? 0, Enabled: row["enabled"]?.GetValue<bool>() ?? true, CharacterId: row["character_id"]?.GetValue<string>(),
                Slot: row["slot"]?.GetValue<uint>() ?? 1, Stage: OwnedMmdPlaybackService.ReadStage(row),
                GazeSource: PlaybackAssets.ReadSource(actorDirectory, PlaybackAssets.GazeSourceFile), GazeStrength: row["gaze_strength"]?.GetValue<double>() ?? 1);
        }
        var frozenActors = rows.Select(FromConfig).ToArray();
        var bySlot = frozenActors.ToDictionary(actor => actor.Slot);
        var request = AssignSlots(saved ?? new PlaybackSessionRequest(frozenActors,
            CameraMode: config!["camera_mode"]?.GetValue<int>() ?? 0, Speed: Contract.D(config, "speed"), Loop: Contract.True(config, "loop"),
            DurationSeconds: Contract.D(config, "duration_ms") / 1000d, NativePhysics: config["native_physics"]?.GetValue<bool>() ?? true,
            Physics: Contract.True(config, "physics_enabled"), SpacingCm: config["spacing_cm"]?.GetValue<double>() ?? 180,
            FormationNative: Contract.True(config, "formation_native"), HideSceneCharacters: Contract.True(config, "hide_scene_characters"),
            GroundHeightCm: config["ground_height_cm"]?.GetValue<double>() ?? -2, CameraTrackHeightCm: config["camera_track_height_cm"]?.GetValue<double>() ?? 0));
        var actors = request.Actors.Select(actor =>
        {
            if (!actor.Enabled || !bySlot.TryGetValue(actor.Slot, out var frozen)) return actor;
            return actor with { BodySource = File.Exists(frozen.BodySource) ? frozen.BodySource : actor.BodySource,
                FaceSource = frozen.FaceSource ?? actor.FaceSource, GazeSource = frozen.GazeSource ?? actor.GazeSource,
                CharacterId = actor.CharacterId ?? frozen.CharacterId, Placement = actor.Placement ?? frozen.Placement,
                Stage = actor.Stage ?? frozen.Stage, Scale = actor.Scale ?? frozen.Scale };
        }).ToArray();
        if (actors.Any(actor => actor.Enabled && string.IsNullOrWhiteSpace(actor.CharacterId)))
            throw new InvalidDataException("旧准备目录未保存可还原的角色身份；请重新选择角色并准备动作。");
        var camera = request.CameraSource;
        var cameraProfile = Path.Combine(directory, "camera_profile.json");
        if (camera is null && File.Exists(cameraProfile)) camera = JsonData.ReadObject(cameraProfile)["source_camera_vmd"]?.GetValue<string>();
        return request with { Actors = actors, CameraSource = PlaybackAssets.ReadSource(directory, PlaybackAssets.CameraSourceFile, camera) };
    }

    private static int Milliseconds(double seconds) => checked((int)Math.Round(seconds * 1000));
    private static void WriteRequest(string directory, PlaybackSessionRequest request)
    {
        var saved = JsonSerializer.SerializeToNode(request)!.AsObject(); saved["schema"] = 1;
        JsonData.Write(Path.Combine(directory, RequestFilename), saved);
    }
    private string CompilationFingerprint(PlaybackSessionRequest request)
    {
        request = AssignSlots(request);
        string? Digest(string? file) => string.IsNullOrWhiteSpace(file) ? null : JsonData.Digest(file);
        var actors = new JsonArray(request.Actors.Select(actor => (JsonNode)new JsonObject
        {
            ["slot"] = actor.Slot, ["character"] = actor.CharacterId, ["enabled"] = actor.Enabled,
            ["body"] = actor.Enabled ? Digest(actor.BodySource) : null,
            ["face"] = actor.Enabled ? Digest(actor.FaceSource) : null, ["scale"] = actor.Scale,
            ["gaze"] = actor.Enabled ? Digest(actor.GazeSource) : null, ["gaze_strength"] = actor.GazeStrength
        }).ToArray());
        var forms = Path.Combine(root, "data", "characters", "character_forms.json");
        return JsonData.CanonicalDigest(new JsonObject { ["character_loading_version"] = 2,
            ["body_compiler_sha256"] = Digest(Path.Combine(root, "tools", "mmd_outputs", "mmd_vmd_cli.exe")),
            ["face_compiler_sha256"] = Digest(Path.Combine(root, "tools", "mmd_outputs", "mmd_face_compile.exe")),
            ["camera_compiler_sha256"] = Digest(Path.Combine(root, "tools", "mmd_outputs", "mmd_camera_compile.exe")),
            ["catalog"] = Digest(Path.Combine(root, OwnedCharacterCatalog.RelativePath)), ["character_index"] = Digest(Path.Combine(root, "data", "characters", "index.json")),
            ["forms"] = File.Exists(forms) ? Digest(forms) : null, ["actors"] = actors, ["camera"] = Digest(request.CameraSource) });
    }
    public bool RequiresPreparation(string directory, PlaybackSessionRequest request)
    {
        var path = Path.Combine(directory, "preparation_inputs.json");
        var recorded = File.Exists(path) ? JsonData.ReadObject(path)["compilation_sha256"]?.GetValue<string>() : null;
        return recorded != CompilationFingerprint(request);
    }

    private async Task<JsonObject> PrepareCoreAsync(PlaybackSessionRequest request, CancellationToken cancellation = default)
    {
        request = AssignSlots(request);
        var directory = Path.Combine(root, "playback", "actor_preparations", "actors_" + Guid.NewGuid().ToString("N"));
        var result = await owned.PrepareActorsAsync(request, directory, cancellation);
        WriteRequest(directory, request);
        JsonData.Write(Path.Combine(directory, "preparation_inputs.json"), new JsonObject { ["compilation_sha256"] = CompilationFingerprint(request) });
        return result;
    }

    public void UpdatePreparation(string directory, PlaybackSessionRequest request)
    {
        ValidateRequest(request); request = AssignSlots(request);
        var mixed = OwnedMmdPlaybackService.Config(directory);
        mixed["speed"] = request.Speed; mixed["loop"] = request.Loop; mixed["duration_ms"] = Milliseconds(request.DurationSeconds); mixed["camera_mode"] = request.CameraMode;
        mixed["hide_scene_characters"] = request.HideSceneCharacters; mixed["camera_track_height_cm"] = request.CameraTrackHeightCm;
        mixed["camera_enabled"] = true; mixed["native_physics"] = request.NativePhysics; mixed["physics_enabled"] = request.Physics;
        mixed["physics_mode"] = PlaybackAssets.PhysicsMode(request.NativePhysics, request.Physics);
        var bySlot = request.Actors.Where(actor => actor.Enabled).ToDictionary(actor => actor.Slot);
        foreach (var actor in OwnedMmdPlaybackService.ActorConfigs(mixed))
        {
            if (!bySlot.TryGetValue(actor["slot"]?.GetValue<uint>() ?? 1, out var selected)) throw new InvalidDataException("演员集合已更改，需要重新准备。");
            actor["placement"] = PlaybackAssets.Placement(selected.Placement); OwnedMmdPlaybackService.SetStage(actor, selected.Stage);
            foreach (var key in new[] { "native_physics", "physics_enabled", "physics_mode" }) actor[key] = mixed[key]!.DeepClone();
        }
        OwnedMmdPlaybackService.ConfigureStage(mixed, request.SpacingCm, request.FormationNative, request.GroundHeightCm);
        OwnedMmdPlaybackService.WriteActorsFile(directory, mixed);
        JsonData.Write(Path.Combine(directory, "owned_mmd_config.json"), mixed);
        WriteRequest(directory, request);
    }
}
