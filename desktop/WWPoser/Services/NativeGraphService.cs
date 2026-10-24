namespace WWPoser.Services;
public sealed partial class NativeGraphService(string repoRoot)
{
    private readonly string root = Path.GetFullPath(repoRoot);
    private readonly CharacterRegistry registry = new(repoRoot);
    private static readonly string[] ObserverFunctions = ["0x4e0ba10", "0x49a34d0", "0x49dd440", "0xa3db40"];

    private JsonArray FunctionWitnesses(params string[] rvas)
    {
        var rows = new[] { "native_kawaii.json", "native_activation.json" }.SelectMany(file => Contract.A(JsonData.ReadObject(Path.Combine(root, "data", "runtime_contracts", file)), "functions")).ToArray();
        return new JsonArray(rvas.Select(rva => (JsonNode)new JsonObject { ["rva"] = rva, ["function_bytes_hex"] = Contract.S(rows.First(n => Contract.S(n!, "rva") == rva)!, "function_bytes_hex").ToLowerInvariant() }).ToArray());
    }
    public JsonArray RootWitnesses() => FunctionWitnesses("0xa40710", "0xa3fc60");
    public JsonArray InputWitnesses() => FunctionWitnesses("0x4e0ba10", "0xa3f270", "0x4dedaa0", "0x4e0d850");
    public JsonArray ActivationWitnesses() => FunctionWitnesses("0x49dd440", "0x4e1abc0", "0xa40a50", "0xa44ae0");

    public void PrepareIdentity(string configPath, string output, int pid)
    {
        var cfg = JsonData.ReadObject(configPath);
        var registration = Path.Combine(root, "data", "native_identity_registration_contract.json");
        cfg.Remove("native_graph_discovery");
        cfg["native_identity_bootstrap"] = new JsonObject { ["schema"] = 1, ["phase"] = "identity_registration", ["expected_pid"] = pid,
            ["duration_ms"] = 2000, ["owner_limit"] = 32, ["function_witnesses"] = FunctionWitnesses(ObserverFunctions),
            ["registration_contract_sha256"] = JsonData.Digest(registration) };
        StageObserver(cfg, output, "native_identity_bootstrap.dll", registration);
    }

    public void PrepareDiscovery(string configPath, string output, int pid)
    {
        var cfg = JsonData.ReadObject(configPath);
        cfg.Remove("native_identity_bootstrap");
        cfg["native_graph_discovery"] = new JsonObject { ["schema"] = 1, ["phase"] = "discovery", ["expected_pid"] = pid,
            ["duration_ms"] = 6000, ["frame_limit"] = 8, ["node_limit"] = 128,
            ["function_witnesses"] = FunctionWitnesses(ObserverFunctions), ["root_layout_witnesses"] = RootWitnesses() };
        StageObserver(cfg, output, "native_graph_discovery.dll");
    }

    private void StageObserver(JsonObject cfg, string output, string dll, string? registration = null)
    {
        Directory.CreateDirectory(Path.Combine(output, "logs"));
        JsonData.Write(Path.Combine(output, "observe_target.json"), cfg);
        File.Copy(Path.Combine(root, "probes", "v20_recon", dll), Path.Combine(output, "recon_carrier_v20.dll"));
        File.Copy(Path.Combine(root, "probes", "v20_recon", "hook_install_v20.exe"), Path.Combine(output, "hook_install_v20.exe"));
        if (registration != null)
            File.Copy(registration, Path.Combine(output, "native_identity_registration_contract.json"));
    }

    public static JsonObject PromoteTargetIdentity(string configPath, string identityDirectory)
    {
        var cfg = JsonData.ReadObject(configPath);
        var report = JsonData.ReadObject(Path.Combine(identityDirectory, "native_identity_bootstrap.json"));
        Contract.Require(Contract.S(report, "status") == "complete", "引擎动画实例身份登记失败：" + report["reasons"]?.ToJsonString());
        var target = Contract.O(cfg, "animation_target");
        foreach (var row in Contract.A(report, "support_registrations")) target[Contract.S(row!, "role")] = row!["after"]!.DeepClone();
        target["main_anim"] = Contract.A(report, "owners").First(x => Contract.S(x!, "role") is "main" or "main_anim")!["after"]!.DeepClone();
        return cfg;
    }

    public void AttachPlaybackBinding(JsonObject cfg, JsonObject binding, int pid)
    {
        cfg["native_physics"] = true;
        cfg["physics_backend"] = "game_native";
        cfg["freeze_native_animation_time"] = false;
        cfg["native_physics_activate_curve_groups"] = true;
        cfg["native_graph_binding"] = binding.DeepClone();
        cfg["native_graph_game_pid"] = pid;
        cfg["native_graph_source_config_file"] = "native_graph_source_config.json";
        cfg["native_graph_source_config_sha256"] = binding["proof"]!["source_config_sha256"]!.DeepClone();
        cfg["native_physics_witnesses"] = InputWitnesses();
        cfg["native_physics_activation_witnesses"] = ActivationWitnesses();
        if (Contract.A(binding, "curve_groups").Any(x => x!["root_binding"] != null)) cfg["native_graph_root_layout_witnesses"] = RootWitnesses();
    }
}
