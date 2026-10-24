namespace WWPoser.Services;

internal sealed class PlaybackAssets(string root)
{
    internal static JsonObject Placement(SquadPlacement? value)
    {
        value ??= new();
        return new() { ["offset_x_cm"] = value.OffsetX, ["offset_y_cm"] = value.OffsetY, ["height_cm"] = value.Height, ["yaw_degrees"] = value.Yaw, ["in_place"] = value.InPlace };
    }

    internal void Expression(JsonObject config, JsonObject imported, string prefix = "")
    {
        var enabled = Contract.True(imported, "expression_enabled");
        config["expression_enabled"] = enabled;
        if (!enabled) return;
        var expression = JsonData.ReadObject(Contract.S(imported, "expression_profile"));
        var morph = Contract.O(JsonData.ReadObject(Path.Combine(root, "data", "runtime_contracts", "morph.json")), "set_morph_native");
        config["expression_clip_file"] = prefix + "expression.wwexpr";
        config["expression_profile_file"] = prefix + "expression_profile.json";
        config["expression_clip_sha256"] = expression["expression_binary_sha256"]!.DeepClone();
        config["expression_duration_seconds"] = expression["duration_seconds"]!.DeepClone();
        config["expression_source_sha256"] = expression["expression_source_sha256"]!.DeepClone();
        config["expression_target_names"] = expression["target_morph_names"]!.DeepClone();
        config["expected_expression_count"] = Contract.A(expression, "target_morph_names").Count;
        config["expression_mapped_names"] = new JsonArray(Contract.A(expression, "bindings").SelectMany(x => Contract.A(x!, "targets")).Select(x => Contract.S(x!, "morph")).Distinct().Order().Select(x => (JsonNode)JsonValue.Create(x)!).ToArray());
        config["morph_function_rva"] = morph["rva"]!.DeepClone();
        config["morph_function_bytes_hex"] = morph["function_bytes_hex"]!.DeepClone();
    }

    internal async Task CameraAsync(JsonObject config, string directory, string? source, int mode, JsonObject anchorClip, CancellationToken cancellation)
    {
        config["camera_enabled"] = source is not null || mode != 0;
        config["camera_clip_enabled"] = source is not null;
        config["camera_mode"] = mode;
        config["camera_runtime_contract_file"] = "camera_runtime_contract.json";
        File.Copy(Path.Combine(root, "data", "camera_runtime_contract.json"), Path.Combine(directory, "camera_runtime_contract.json"), true);
        if (source is null) return;
        var profilePath = Path.Combine(directory, "camera_profile.json");
        var clipPath = Path.Combine(directory, "camera.wwcam");
        (await ProcessRunner.RunAsync(Path.Combine(root, "tools", "mmd_outputs", "mmd_camera_compile.exe"), [source, profilePath, clipPath], root, cancellation)).EnsureSuccess("镜头编译失败");
        var profile = JsonData.ReadObject(profilePath);
        config["camera_clip_file"] = "camera.wwcam";
        config["camera_clip_sha256"] = profile["binary_sha256"]!.DeepClone();
        config["camera_source_sha256"] = profile["source_sha256"]!.DeepClone();
        config["camera_fps"] = 60;
        config["camera_frame_count"] = profile["frame_count"]!.DeepClone();
        config["camera_clip_duration_seconds"] = profile["timeline_duration_seconds"]!.DeepClone();
        config["camera_source_to_component_rotation_xyzw"] = anchorClip["camera_source_to_component_rotation_xyzw"]!.DeepClone();
        config["camera_units_per_mmd_unit"] = anchorClip["source_to_target_scale"]!.DeepClone();
    }

    internal void Physics(JsonObject config, string directory, bool native, bool secondary)
    {
        config["native_physics"] = native;
        config["physics_enabled"] = !native && secondary;
        config["physics_mode"] = native ? "native" : secondary ? "secondary_inertia" : "direct";
        config["physics_profile_file"] = "bone_chains.json";
        File.Copy(Path.Combine(root, "data", "bone_chains.json"), Path.Combine(directory, "bone_chains.json"), true);
    }
}
