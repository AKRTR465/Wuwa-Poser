namespace WWPoser.Services;
public sealed record SinglePrepareRequest(string ScanDirectory, string ReportPath, string OutputDirectory, bool IdentityContext = false, JsonArray? WeaponTargets = null, string? ExpressionProfile = null, string? ExpressionClip = null, bool FreezeNativeAnimationTime = false, bool NativePhysics = true, string? NativeGraphDirectory = null, string? CameraProfile = null, string? CameraClip = null, string? CameraManifest = null, int DurationMs = 0, bool Autoplay = false);
public sealed record SingleLaunchRequest(string BodySource, string? FaceSource = null, string? CameraSource = null, bool NativePhysics = true, bool FreezeNativeAnimationTime = false, double Speed = 1, bool Loop = false, bool Physics = false, int CameraMode = 0, double OffsetX = 0, double OffsetY = 0, double Height = 0, double Yaw = 0, bool InPlace = false, double? Scale = null, int DurationMs = 0);
public sealed partial class SinglePlaybackService(string repoRoot)
{
    private readonly string root = Path.GetFullPath(repoRoot);
    private readonly CharacterRegistry registry = new(repoRoot);
    private readonly MotionImportService importer = new(repoRoot);
    private readonly NativeGraphService native = new(repoRoot);
    private static readonly string[] IdentityFields = ["expected_game_pid", "source_sha256", "hook_mode", "publish_rva", "function_bytes_hex", "object", "index", "serial", "item_address", "class_address", "outer_address", "controller_address", "gobjects_rva", "name_pool_rva", "expected_bone_count", "expected_bone_names", "expected_bone_parents", "expected_mesh_asset_name", "expected_mesh_asset_names", "expected_skeleton_asset_name", "expected_skeleton_sha256", "expected_pawn_class", "character_profile_id", "physics_model_id", "expected_expression_count", "character_profile_sha256", "profile_resolution", "vmd_source_sha256"];
    public (JsonObject Profile, JsonObject Identity) Identify(string scan)
    {
        var recon = JsonData.ReadObject(Path.Combine(scan, "recon.json"));
        var relations = JsonData.ReadObject(Path.Combine(scan, "player_relations.json"));
        var found = new List<(JsonObject Profile, JsonObject Identity)>();
        foreach (var row in Contract.A(relations, "relations"))
        {
            var cls = Contract.S(Contract.O(row!, "controller"), "class");
            if (!Contract.True(row!, "reciprocal_pawn") || !Contract.True(row!, "reciprocal_acknowledged_pawn") || !(cls.Contains("CharacterController") || cls.Contains("PlayerController")) || cls.Contains("AI") || cls.Contains("Summon"))
                continue;
            var profile = registry.Resolve(Contract.S(row!, "mesh_asset_name"), Contract.S(row!, "skeleton_asset_name"));
            found.Add((profile, new JsonObject { ["game_pid"] = recon["pid"]!.DeepClone(), ["pawn_class"] = row!["class"]!.DeepClone(), ["pawn_address"] = row["address"]!.DeepClone(), ["mesh_address"] = row["main_mesh_address"]!.DeepClone(), ["mesh_asset_name"] = row["mesh_asset_name"]!.DeepClone(), ["skeleton_asset_name"] = row["skeleton_asset_name"]!.DeepClone() }));
        }

        Contract.Require(found.Count == 1, "无法唯一识别当前受控角色");
        return found[0];
    }

    public JsonObject Prepare(SinglePrepareRequest request)
    {
        var(profile, identity) = Identify(request.ScanDirectory);
        var recon = JsonData.ReadObject(Path.Combine(request.ScanDirectory, "recon.json"));
        var relations = JsonData.ReadObject(Path.Combine(request.ScanDirectory, "player_relations.json"));
        var mesh = Contract.A(recon, "meshes").First(x => Contract.Equal(x!["address"], identity["mesh_address"]))!.AsObject();
        var relation = Contract.A(relations, "relations").First(x => Contract.Equal(x!["address"], identity["pawn_address"]))!;
        var cfg = JsonData.ReadObject(Path.Combine(root, "data", "runtime_contracts", "publication.json"));
        var report = JsonData.ReadObject(request.ReportPath);
        var clip = Contract.O(report, "compiled_target_clip");
        var pid = (int)Contract.I(identity, "game_pid");
        var meshName = Contract.S(identity, "mesh_asset_name");
        var targets = registry.MorphNames(profile, meshName);
        cfg["expected_game_pid"] = pid;
        cfg["object"] = mesh["address"]!.DeepClone();
        foreach (var k in new[] { "index", "serial", "item_address", "class_address", "outer_address" })
            cfg[k] = mesh[k]!.DeepClone();
        cfg["controller_address"] = Contract.O(relation, "controller")["address"]!.DeepClone();
        cfg["gobjects_rva"] = Contract.A(recon, "gobjects_candidates")[0]!["rva"]!.DeepClone();
        cfg["name_pool_rva"] = Contract.A(recon, "name_pool_candidates")[0]!["rva"]!.DeepClone();
        cfg["expected_bone_count"] = profile["bone_count"]!.DeepClone();
        cfg["expected_bone_names"] = profile["bone_names"]!.DeepClone();
        cfg["expected_bone_parents"] = profile["bone_parents"]!.DeepClone();
        cfg["expected_mesh_asset_name"] = meshName;
        cfg["expected_mesh_asset_names"] = profile["mesh_asset_names"]!.DeepClone();
        cfg["expected_skeleton_asset_name"] = profile["skeleton_asset_name"]!.DeepClone();
        cfg["expected_skeleton_sha256"] = profile["skeleton_sha256"]!.DeepClone();
        cfg["expected_pawn_class"] = relation["class"]!.DeepClone();
        cfg["character_profile_id"] = profile["profile_id"]!.DeepClone();
        cfg["physics_model_id"] = profile["physics_model_id"]?.DeepClone();
        cfg["expected_expression_count"] = targets.Count;
        cfg["character_profile_sha256"] = JsonData.Digest(registry.ProfilePath(profile));
        cfg["profile_resolution"] = "fresh_reference_identity";
        cfg["vmd_source_sha256"] = report["source_sha256"]!.DeepClone();
        Directory.CreateDirectory(request.OutputDirectory);
        if (request.IdentityContext)
        {
            var context = Contract.Select(cfg, IdentityFields);
            context["context_kind"] = "current_character_identity";
            JsonData.Write(Path.Combine(request.OutputDirectory, "observe_target.json"), context);
            return context;
        }

        cfg["mode"] = "mmd_playback";
        cfg["probe_mode"] = "full_body_mmd_player";
        cfg["duration_ms"] = request.DurationMs;
        cfg["autoplay"] = request.Autoplay;
        cfg["clip_epoch"] = checked((ulong)(DateTime.UtcNow.Ticks - DateTime.UnixEpoch.Ticks) * 100UL);
        cfg["local_clip_file"] = "motion.wwlocal";
        cfg["local_clip_sha256"] = clip["sha256"]!.DeepClone();
        foreach (var k in new[] { "canonical_position_mask", "canonical_local_positions", "canonical_rotation_mask", "canonical_local_rotations" })
            cfg[k] = clip[k]!.DeepClone();
        cfg["freeze_native_animation_time"] = request.FreezeNativeAnimationTime;
        cfg["native_physics"] = request.NativePhysics;
        cfg["native_physics_activate_curve_groups"] = request.NativePhysics;
        if (request.NativePhysics)
            native.AttachPlaybackBinding(cfg, native.Bind(request.NativeGraphDirectory!), pid);
        if (request.WeaponTargets is { } weapons)
        {
            cfg["weapon_targets"] = weapons.DeepClone();
            var visibility = Contract.O(JsonData.ReadObject(Path.Combine(root, "data", "runtime_contracts", "weapon_visibility.json")), "set_hidden_native");
            cfg["visibility_function_rva"] = visibility["rva"]!.DeepClone();
            cfg["visibility_function_bytes_hex"] = visibility["function_bytes_hex"]!.DeepClone();
        }

        if (request.ExpressionProfile != null)
        {
            var expression = JsonData.ReadObject(request.ExpressionProfile);
            var morph = Contract.O(JsonData.ReadObject(Path.Combine(root, "data", "runtime_contracts", "morph.json")), "set_morph_native");
            cfg["expression_clip_sha256"] = expression["expression_binary_sha256"]!.DeepClone();
            cfg["expression_duration_seconds"] = expression["duration_seconds"]!.DeepClone();
            cfg["expression_target_names"] = expression["target_morph_names"]!.DeepClone();
            cfg["expression_mapped_names"] = new JsonArray(Contract.A(expression, "bindings").SelectMany(x => Contract.A(x!, "targets")).Select(x => Contract.S(x!, "morph")).Distinct().Order().Select(x => (JsonNode)JsonValue.Create(x)!).ToArray());
            cfg["morph_function_rva"] = morph["rva"]!.DeepClone();
            cfg["morph_function_bytes_hex"] = morph["function_bytes_hex"]!.DeepClone();
            cfg["expression_source_sha256"] = expression["expression_source_sha256"]!.DeepClone();
        }

        cfg["camera_enabled"] = request.CameraManifest != null;
        cfg["camera_clip_enabled"] = request.CameraProfile != null;
        cfg["camera_mode"] = request.CameraProfile != null ? 1 : 0;
        cfg["timeline_duration_seconds"] = Math.Max(Contract.D(clip, "duration_seconds"), cfg["expression_duration_seconds"]?.GetValue<double>() ?? 0);
        if (request.CameraManifest != null)
        {
            cfg["camera_observation"] = JsonData.ReadObject(request.CameraManifest);
            cfg["camera_runtime_contract_file"] = "camera_runtime_contract.json";
            cfg["camera_runtime_contract_sha256"] = JsonData.Digest(Path.Combine(root, "data", "camera_runtime_contract.json"));
        }

        if (request.CameraProfile != null)
        {
            var camera = JsonData.ReadObject(request.CameraProfile);
            var duration = Contract.D(camera, "timeline_duration_seconds");
            cfg["camera_clip_file"] = "camera.wwcam";
            cfg["camera_clip_sha256"] = camera["binary_sha256"]!.DeepClone();
            cfg["camera_source_sha256"] = camera["source_sha256"]!.DeepClone();
            cfg["camera_fps"] = 60;
            cfg["camera_frame_count"] = camera["frame_count"]!.DeepClone();
            cfg["camera_clip_duration_seconds"] = duration;
            cfg["timeline_duration_seconds"] = Math.Max(Contract.D(cfg, "timeline_duration_seconds"), duration);
            cfg["camera_source_to_component_rotation_xyzw"] = clip["camera_source_to_component_rotation_xyzw"]!.DeepClone();
            cfg["camera_units_per_mmd_unit"] = clip["source_to_target_scale"]!.DeepClone();
        }

        Directory.CreateDirectory(Path.Combine(request.OutputDirectory, "logs"));
        void Copy(string from, string leaf) => File.Copy(from, Path.Combine(request.OutputDirectory, leaf));
        cfg["physics_profile_sha256"] = JsonData.Digest(Path.Combine(root, "data", "bone_chains.json"));
        JsonData.Write(Path.Combine(request.OutputDirectory, "observe_target.json"), cfg);
        Copy(Contract.S(clip, "path"), "motion.wwlocal");
        Copy(Path.Combine(root, "data", "bone_chains.json"), "bone_chains.json");
        if (request.NativeGraphDirectory != null)
        {
            Copy(Path.Combine(request.NativeGraphDirectory, "native_graph_discovery.json"), "native_graph_discovery.json");
            Copy(Path.Combine(request.NativeGraphDirectory, "observe_target.json"), "native_graph_source_config.json");
        }

        if (request.ExpressionClip != null)
            Copy(request.ExpressionClip, "expression.wwexpr");
        if (request.CameraClip != null)
            Copy(request.CameraClip, "camera.wwcam");
        if (request.CameraManifest != null)
            Copy(Path.Combine(root, "data", "camera_runtime_contract.json"), "camera_runtime_contract.json");
        Copy(Path.Combine(root, "probes", "v20_recon", "hook_install_v20.exe"), "hook_install_v20.exe");
        Copy(Path.Combine(root, "probes", "v20_recon", "publish_probe_v21.dll"), "recon_carrier_v20.dll");
        return cfg;
    }
}
