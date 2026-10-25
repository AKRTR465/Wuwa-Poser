using System.Globalization;
using System.Text;

namespace WWPoser.Services;

public sealed partial class OwnedMmdPlaybackService
{
    public static IReadOnlyList<JsonObject> ActorConfigs(JsonObject config) => config["actors"] is JsonArray actors
        ? actors.Select(actor => actor!.AsObject()).ToArray() : [config];

    public static void ConfigureStage(JsonObject config, double spacingCm, bool formationNative = false)
    {
        var actors = ActorConfigs(config);
        config["spacing_cm"] = spacingCm;
        config["formation_native"] = formationNative;
        config["formation_floor_fallback_cm"] = 2.5;
        for (var index = 0; index < actors.Count; index++)
        {
            // Stage offsets are separate from each animation's local placement.
            // A chosen stage position replaces the actor's automatic grid cell.
            // Native formation stands every actor on the shared stage origin so
            // the clips' own choreography offsets rebuild the original lineup.
            var actor = actors[index];
            var position = formationNative ? new StagePosition(0, 0) : ReadStage(actor) ?? GridPosition(index, actors.Count, spacingCm);
            actor["stage_x_cm"] = position.X; actor["stage_y_cm"] = position.Y;
            actor["stage_z_cm"] = position.Z; actor["stage_yaw_degrees"] = position.Yaw;
        }
    }

    public static StagePosition GridPosition(int index, int count, double spacingCm)
    {
        // One actor retains the established 150 cm separation from the player.
        var columns = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(count)));
        var rows = (count + columns - 1) / columns;
        return new(150 + (index % columns - (columns - 1) * .5) * spacingCm, (index / columns - (rows - 1) * .5) * spacingCm);
    }

    public static void SetStage(JsonObject actor, StagePosition? position)
    {
        if (position is null) { actor.Remove("stage_position"); return; }
        actor["stage_position"] = new JsonObject { ["x_cm"] = position.X, ["y_cm"] = position.Y, ["z_cm"] = position.Z, ["yaw_degrees"] = position.Yaw };
    }

    public static StagePosition? ReadStage(JsonObject actor) => actor["stage_position"] is JsonObject p
        ? new(Contract.D(p, "x_cm"), Contract.D(p, "y_cm"), Contract.D(p, "z_cm"), Contract.D(p, "yaw_degrees")) : null;

    public static void WriteActorsFile(string directory, JsonObject config)
    {
        static string Number(JsonObject actor, string key) => Contract.D(actor, key).ToString("R", CultureInfo.InvariantCulture);
        var lines = ActorConfigs(config).Select(actor =>
        {
            var model = Contract.S(actor, "mesh_asset_name");
            var form = actor["form"] as JsonObject;
            string Text(string key, string fallback = "") => form?[key]?.GetValue<string>() ?? fallback;
            var children = form?["child_mesh_paths"] as JsonArray;
            var fields = new[] { model, Text("id", "default"), Text("strategy", "role_skin"), Text("main_mesh_path"), Text("anim_class_path"),
                string.Join('|', children?.Select(child => child!.GetValue<string>()) ?? []) };
            if (fields.Any(field => field.IndexOfAny(['\t', '\r', '\n']) >= 0) || children?.Any(child => child!.GetValue<string>().Contains('|')) == true)
                throw new InvalidDataException("角色形态含无效文本字符。");
            return string.Join('\t', (actor["slot"]?.GetValue<uint>() ?? 1).ToString(CultureInfo.InvariantCulture),
                Contract.I(actor, "role_id").ToString(CultureInfo.InvariantCulture), Contract.I(actor, "skin_id").ToString(CultureInfo.InvariantCulture),
                Number(actor, "stage_x_cm"), Number(actor, "stage_y_cm"), Number(actor, "stage_z_cm"), model, Number(actor, "stage_yaw_degrees"),
                fields[1], fields[2], (form?["morph_type"]?.GetValue<int>() ?? 0).ToString(CultureInfo.InvariantCulture),
                Text("base_model_id", actor["model_id"]?.GetValue<string>() ?? "0"), fields[3], fields[4], fields[5], Contract.True(actor, "independent_instance") ? "1" : "0");
        });
        File.WriteAllLines(Path.Combine(directory, "actors.tsv"), lines, new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(directory, "stage_floor.tsv"),
            (Contract.True(config, "formation_native") ? "walkable_floor" : "capsule_bottom") + "\t" +
            (config["formation_floor_fallback_cm"]?.GetValue<double>() ?? 2.5).ToString("R", CultureInfo.InvariantCulture) + "\n", new UTF8Encoding(false));
    }

    public async Task<JsonObject> PrepareActorsAsync(PlaybackSessionRequest request, string outputDirectory, CancellationToken cancellation = default)
    {
        request = PlaybackSessionService.AssignSlots(request);
        var selected = request.Actors.Where(actor => actor.Enabled).ToArray();
        var catalog = CharacterCatalog();
        foreach (var actor in selected)
        {
            if (string.IsNullOrWhiteSpace(actor.CharacterId)) throw new InvalidDataException($"请为演员 {actor.Slot} 选择角色。");
            catalog.Select(actor.CharacterId);
        }
        var output = Path.GetFullPath(outputDirectory); Directory.CreateDirectory(output);
        var epoch = NewEpoch(); var actors = new JsonArray(); JsonObject? anchorClip = null; double bodyDuration = 0, duration = 0;
        for (var index = 0; index < selected.Length; index++)
        {
            cancellation.ThrowIfCancellationRequested();
            var actor = selected[index];
            Progress?.Invoke($"准备演员 {index + 1} / {selected.Length} 的动作与表情");
            var relative = "actors/" + actor.Slot.ToString(CultureInfo.InvariantCulture);
            var actorDirectory = Path.Combine(output, relative);
            await PrepareAsync(new(actor.BodySource, actorDirectory, actor.Scale, checked((int)Math.Round(request.DurationSeconds * 1000)), request.Speed,
                request.Loop, actor.CharacterId, actor.FaceSource, CameraMode: 0, Placement: actor.Placement, NativePhysics: request.NativePhysics, Physics: request.Physics), cancellation);
            var config = Config(actorDirectory);
            config["slot"] = actor.Slot; config["target_slot"] = actor.Slot; config["actor_directory"] = relative; SetStage(config, actor.Stage);
            config["session_epoch"] = epoch; config["enabled"] = true; config["ownership_preference"] = "scene_team_then_create";
            var report = JsonData.ReadObject(Path.Combine(actorDirectory, "compiled_report.json"));
            anchorClip ??= Contract.O(report, "compiled_target_clip");
            bodyDuration = Math.Max(bodyDuration, Contract.D(config, "clip_duration_seconds"));
            duration = Math.Max(duration, Contract.D(config, "duration_seconds"));
            JsonData.Write(Path.Combine(actorDirectory, "owned_mmd_config.json"), config);
            actors.Add(config);
        }
        var scene = new JsonObject
        {
            ["schema"] = 3, ["scope"] = "mixed_actor_mmd", ["session_epoch"] = epoch,
            ["actors"] = actors, ["actor_count"] = actors.Count, ["camera_anchor_slot"] = selected[0].Slot,
            ["ownership_preference"] = "scene_team_then_create", ["duration_ms"] = checked((int)Math.Round(request.DurationSeconds * 1000)),
            ["speed"] = request.Speed, ["loop"] = request.Loop, ["clip_duration_seconds"] = bodyDuration
        };
        var assets = new PlaybackAssets(root);
        assets.Physics(scene, output, request.NativePhysics, request.Physics);
        var cameraSource = request.CameraSource is null ? null : Path.Combine(output, "camera_source.vmd");
        if (cameraSource is not null) File.Copy(request.CameraSource!, cameraSource);
        await assets.CameraAsync(scene, output, cameraSource, request.CameraMode, anchorClip!, cancellation);
        scene["camera_enabled"] = true;
        scene["duration_seconds"] = Math.Max(duration, scene["camera_clip_duration_seconds"]?.GetValue<double>() ?? 0);
        ConfigureStage(scene, request.SpacingCm, request.FormationNative); WriteActorsFile(output, scene);
        JsonData.Write(Path.Combine(output, "owned_mmd_config.json"), scene);
        return new JsonObject { ["state"] = "prepared_not_run", ["session_dir"] = output, ["config"] = Path.Combine(output, "owned_mmd_config.json"),
            ["session_epoch"] = epoch, ["actor_count"] = actors.Count, ["duration_seconds"] = scene["duration_seconds"]!.DeepClone(),
            ["actors"] = actors.DeepClone(), ["message"] = $"{actors.Count} 名演员已准备；开始时优先使用编队中的原角色，其余另行创建。" };
    }
}
