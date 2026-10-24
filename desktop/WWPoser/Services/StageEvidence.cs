namespace WWPoser.Services;
public sealed class StageEvidence(string repoRoot)
{
    public const string Route = "csharp_managed_readonly";
    public const string SourceContract = "scene-team-csharp-managed-readonly-20261021-v1";
    private readonly string repo = Path.GetFullPath(repoRoot);
    private static readonly (string Assembly, string Name, uint Token)[] ClassPins = [("Aki.Game.dll", "PlayerInfoModel", 0x020025ee), ("Aki.Game.dll", "SceneTeamModel", 0x0200295c), ("Aki.Game.dll", "SceneTeamPlayer", 0x0200294b), ("Aki.Game.dll", "SceneTeamGroup", 0x0200294c), ("Aki.Game.dll", "SceneTeamRole", 0x0200294a), ("Aki.Game.dll", "SceneTeamItem", 0x02002953), ("Aki.Game.dll", "EntityHandle", 0x020031bb), ("Aki.Game.dll", "CharacterModel", 0x02002e32), ("Aki.Game.dll", "WorldEntity", 0x02003266), ("Aki.Core.dll", "Entity", 0x02000075), ("Aki.Core.dll", "ObjectBase", 0x020000b4), ("Aki.Core.dll", "ObjectSystem", 0x020000b6)];
    private static readonly string[] IdentityContracts = ["property_diagnostic_contract", "entity_diagnostic_contract"];

    private static JsonObject OriginIdentity(JsonObject origin)
    {
        var classes = ClassPins.ToDictionary(p => p.Name, p => Contract.A(Contract.O(Contract.O(origin, "class_layouts"), p.Assembly), "selected_types").First(t => Contract.S(t!, "name") == p.Name && Contract.S(t!, "namespace") == "" && Contract.Number(Contract.N(t!, "token")) == p.Token)!.AsObject());
        var storage = new JsonObject();
        foreach (var name in new[] { "SceneTeamModel", "PlayerInfoModel", "CharacterModel", "ObjectSystem" })
            storage[name] = new JsonObject
            {
                ["storage"] = classes[name]["static_storage"]!.DeepClone(),
                ["singleton_fields"] = new JsonArray(Contract.A(classes[name], "fields").Where(f => Contract.S(f!, "name") is "_Instance" or "_instance").Select(f => f!.DeepClone()).ToArray())
            };
        var mono = Contract.O(origin, "mono_inventory");
        var ue = Contract.O(origin, "ue_inventory");
        return new JsonObject
        {
            ["mono"] = origin["mono_module"]?.DeepClone(),
            ["extent"] = origin["module_extent"]?.DeepClone(),
            ["corelib"] = origin["corelib_image"]?.DeepClone(),
            ["game"] = origin["game_image"]?.DeepClone(),
            ["core"] = origin["core_image"]?.DeepClone(),
            ["root_domain"] = mono["root_domain"]?.DeepClone(),
            ["ue_domain"] = ue["domain_candidate"]?.DeepClone(),
            ["ue_environment"] = ue["mono_environment"]?.DeepClone(),
            ["static_lineage"] = storage,
            ["raw_anchors"] = origin["raw_anchors"]?.DeepClone(),
            ["classes"] = new JsonArray(ClassPins.Select(p => (JsonNode?)JsonValue.Create(Contract.Number(Contract.N(classes[p.Name], "klass")))).ToArray())
        };
    }

    public string PrepareStage(string sourcePath, string sampleDirectory, string output, GameIdentity identity)
    {
        var source = JsonData.ReadObject(sourcePath);
        var sampler = JsonData.ReadObject(Path.Combine(sampleDirectory, "puerts_sampler_config.json"));
        var result = JsonData.ReadObject(Path.Combine(sampleDirectory, "puerts_sampler_result.json"));
        Contract.Require(Contract.S(result, "status") == "sampled", "队伍采样失败：" + result["error"]?.ToJsonString());
        var samples = Contract.A(result, "samples");
        var last = samples[^1]!;
        var value = Contract.O(last, "value");
        var capture = new JsonObject { ["roster"] = value["roster"]!.DeepClone(), ["csharp"] = value["csharp_witness"]!["stable_identity"]!.DeepClone() };
        var native = Contract.O(last, "native_join");
        var frames = new JsonArray(samples.Select(s => (JsonNode?)JsonValue.Create(s!["engine_frame"]!.GetValue<ulong>())).ToArray());
        var evidenceId = JsonData.CanonicalDigest(new JsonObject { ["pid"] = identity.Pid, ["creation_filetime"] = identity.CreationFileTime, ["engine_frames"] = frames.DeepClone(), ["source_route"] = Route });
        var cfg = new JsonObject
        {
            ["schema"] = 3,
            ["mode"] = "residency",
            ["binding_mode"] = "scene_team_native",
            ["host_sha256"] = Contract.Build,
            ["source_route"] = Route,
            ["source_contract"] = SourceContract,
            ["expected_game_pid"] = identity.Pid,
            ["expected_process_creation_filetime"] = identity.CreationFileTime,
            ["game_window_handle"] = identity.WindowHandle,
            ["residency_duration_ms"] = 15000,
            ["command_protocol"] = 1,
            ["spacing_cm"] = 180.0,
            ["session_evidence_id"] = evidenceId,
            ["binding_evidence_id"] = evidenceId,
            ["profiles"] = source["profiles"]?.DeepClone(),
            ["scan_contract"] = source["scan_contract"]?.DeepClone(),
            ["csharp_origin"] = result["csharp_origin"]?.DeepClone(),
            ["csharp_expected_origin"] = OriginIdentity(Contract.O(result, "csharp_origin")),
            ["scene_team_csharp_identity"] = capture,
            ["scene_team_roster"] = capture["roster"]!.DeepClone(),
            ["scene_team_preflight"] = native.DeepClone(),
            ["native_witnesses"] = Contract.A(JsonData.ReadObject(Path.Combine(repo, "data/stage_native_runtime_contract.json")), "functions").DeepClone(),
            ["sampler_engine_frames"] = frames,
            ["result_limit"] = sampler["result_limit"]?.DeepClone(),
            ["gobjects_rva"] = source["gobjects_rva"]?.DeepClone(),
            ["name_pool_rva"] = source["name_pool_rva"]?.DeepClone(),
            ["csharp_managed_contract"] = JsonData.ReadObject(Path.Combine(repo, "data/mono_managed_readonly_contract.json")),
            ["csharp_business_contract"] = JsonData.ReadObject(Path.Combine(repo, "data/csharp_scene_team_readonly_contract.json"))
        };
        foreach (var key in IdentityContracts)
            cfg[key] = sampler[key]?.DeepClone();
        Directory.CreateDirectory(Path.Combine(output, "evidence"));
        JsonData.Write(Path.Combine(output, "stage_config.json"), cfg);
        File.Copy(Path.Combine(repo, "probes/v20_recon/stage_residency.dll"), Path.Combine(output, "recon_carrier_v20.dll"));
        File.Copy(Path.Combine(repo, "probes/v20_recon/hook_install_v20.exe"), Path.Combine(output, "hook_install_v20.exe"));
        File.Copy(sourcePath, Path.Combine(output, "evidence", "source_config.json"));
        return output;
    }
}
