namespace WWPoser.Services;
public sealed partial class NativeGraphService(string repoRoot)
{
    private readonly string root = Path.GetFullPath(repoRoot);
    private readonly CharacterRegistry registry = new(repoRoot);
    // Function byte evidence captured from the supported game build.
    private JsonArray VersionedWitnesses()
    {
        var witness = JsonData.ReadObject(Path.Combine(root, "data", "runtime_contracts", "native_functions_3_8.json"));
        Contract.Require(Contract.S(witness, "source_sha256") == GameCompat.SourceBuildSha256, "函数字节证据与当前游戏兼容配置的版本不符。");
        return Contract.A(witness, "functions");
    }
    private static readonly (string Label, ulong Rva)[] ObserverFunctions =
    [
        ("ConvertLocalToComponentEvaluate", GameCompat.FunctionLocalToComponentRva),
        ("SharedSkeletalControlEvaluateCS", GameCompat.FunctionSharedSkeletalControlEvaluateCSRva),
        ("SharedSkeletalControlUpdate", GameCompat.FunctionSharedSkeletalControlUpdateRva),
        ("GroupEvaluate", GameCompat.FunctionGroupEvaluateRva)
    ];

    private JsonArray FunctionWitnesses(params (string Label, ulong Rva)[] functions) => ResolveFunctionWitnesses(VersionedWitnesses(), functions);

    private static JsonArray ResolveFunctionWitnesses(IEnumerable<JsonNode?> rows, params (string Label, ulong Rva)[] functions)
    {
        // The saved contracts retain the byte evidence for each semantic
        // function. Execution addresses come from the selected compatibility
        // definition, so a relocated function need not rewrite its archive.
        return new JsonArray(functions.Select(function =>
        {
            var witnesses = rows.Where(row => Contract.S(row!, "label") == function.Label)
                .Select(row => Contract.S(row!, "function_bytes_hex").ToLowerInvariant()).Distinct(StringComparer.Ordinal).ToArray();
            Contract.Require(witnesses.Length == 1, "函数字节证据缺失或存在冲突：" + function.Label);
            return (JsonNode)new JsonObject { ["label"] = function.Label, ["rva"] = GameCompat.AddressHex(function.Rva), ["function_bytes_hex"] = witnesses[0] };
        }).ToArray());
    }
    public JsonArray RootWitnesses() => FunctionWitnesses(
        ("GroupInitializeBoneReferences", GameCompat.FunctionGroupInitializeBoneReferencesRva),
        ("GroupInitializeModifyBones", GameCompat.FunctionGroupInitializeModifyBonesRva));
    public JsonArray InputWitnesses() => FunctionWitnesses(
        ("ConvertLocalToComponentEvaluate", GameCompat.FunctionLocalToComponentRva),
        ("ComponentSpaceTransformGetter", GameCompat.FunctionComponentSpaceTransformGetterRva),
        ("AnimInstanceRootEvaluationCounterWitness", GameCompat.FunctionAnimInstanceRootEvaluationCounterRva),
        ("CachedPoseEvaluationCounterWitness", GameCompat.FunctionCachedPoseEvaluationCounterRva));
    public JsonArray ActivationWitnesses() => FunctionWitnesses(
        ("SharedSkeletalControlUpdate", GameCompat.FunctionSharedSkeletalControlUpdateRva),
        ("SkeletalControlLODAllowed", GameCompat.FunctionSkeletalControlLODAllowedRva),
        ("GroupIsValidToEvaluate", GameCompat.FunctionGroupIsValidToEvaluateRva),
        ("SharedUpdateInternal", GameCompat.FunctionSharedUpdateInternalRva));

    public void PrepareIdentity(string configPath, string output, int pid)
    {
        var cfg = JsonData.ReadObject(configPath);
        var registration = Path.Combine(root, "data", "native_identity_registration_contract.json");
        cfg.Remove("native_graph_discovery");
        cfg["native_identity_bootstrap"] = new JsonObject { ["schema"] = 1, ["phase"] = "identity_registration", ["expected_pid"] = pid,
            ["duration_ms"] = 2000, ["owner_limit"] = 32, ["function_witnesses"] = FunctionWitnesses(ObserverFunctions) };
        StageObserver(cfg, output, "native_identity_bootstrap.dll", registration);
    }

    public void PrepareDiscovery(string configPath, string output, int pid)
    {
        var cfg = JsonData.ReadObject(configPath);
        cfg.Remove("native_identity_bootstrap");
        cfg["native_graph_discovery"] = new JsonObject { ["schema"] = 1, ["phase"] = "discovery", ["expected_pid"] = pid,
            ["duration_ms"] = 6000, ["frame_limit"] = 8, ["node_limit"] = 128,
            ["function_witnesses"] = FunctionWitnesses(ObserverFunctions), ["root_layout_witnesses"] = RootWitnesses() };
        if (Environment.GetEnvironmentVariable("WWPOSER_OBSERVE_KAWAII_SETTINGS") == "1")
            cfg["native_graph_discovery"]!["kawaii_settings_witnesses"] = FunctionWitnesses(
                ("GroupUpdatePhysicsSettings", GameCompat.FunctionGroupUpdatePhysicsSettingsRva), ("GroupSimulate", GameCompat.FunctionGroupSimulateRva));
        StageObserver(cfg, output, "native_graph_discovery.dll");
    }

    private void StageObserver(JsonObject cfg, string output, string dll, string? registration = null)
    {
        Directory.CreateDirectory(Path.Combine(output, "logs"));
        cfg["compatibility_profile"] = GameCompat.ProfileId;
        cfg["compatibility_source_sha256"] = GameCompat.SourceSha256;
        if (registration != null)
        {
            var contract = JsonData.ReadObject(registration);
            contract["functions"] = ResolveFunctionWitnesses(VersionedWitnesses(),
                ("FUObjectArray_AllocateSerialNumber", GameCompat.FunctionAllocateSerialNumberRva),
                ("FWeakObjectPtr_AssignObject", GameCompat.FunctionAssignWeakObjectRva));
            contract["source_archive_sha256"] = JsonData.Digest(registration);
            contract["compatibility_profile"] = GameCompat.ProfileId;
            contract["compatibility_source_sha256"] = GameCompat.SourceSha256;
            var stagedRegistration = Path.Combine(output, "native_identity_registration_contract.json");
            JsonData.Write(stagedRegistration, contract);
            Contract.O(cfg, "native_identity_bootstrap")["registration_contract_sha256"] = JsonData.Digest(stagedRegistration);
        }
        JsonData.Write(Path.Combine(output, "observe_target.json"), cfg);
        File.Copy(Path.Combine(root, "probes", "v20_recon", dll), Path.Combine(output, "recon_carrier_v20.dll"));
        File.Copy(Path.Combine(root, "probes", "v20_recon", "hook_install_v20.exe"), Path.Combine(output, "hook_install_v20.exe"));
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

    public void AttachPlaybackBinding(JsonObject cfg, JsonObject binding)
    {
        cfg["native_graph_binding"] = binding.DeepClone();
        cfg["native_physics_witnesses"] = InputWitnesses();
        cfg["native_physics_activation_witnesses"] = ActivationWitnesses();
        cfg["native_physics_settings_witnesses"] = FunctionWitnesses(
            ("GroupEvaluate", GameCompat.FunctionGroupEvaluateRva), ("GroupUpdatePhysicsSettings", GameCompat.FunctionGroupUpdatePhysicsSettingsRva),
            ("GroupSimulate", GameCompat.FunctionGroupSimulateRva), ("GroupApplyResult", GameCompat.FunctionGroupApplyResultRva));
        cfg["native_physics_capsule_witnesses"] = FunctionWitnesses(
            ("GroupUpdateCapsuleLimits", GameCompat.FunctionGroupUpdateCapsuleLimitsRva), ("GroupWorldCollision", GameCompat.FunctionGroupWorldCollisionRva),
            ("GroupCollisionCandidate", GameCompat.FunctionGroupCollisionCandidateRva), ("CapsuleContactsA", GameCompat.FunctionCapsuleContactsARva),
            ("CapsuleContactsB", GameCompat.FunctionCapsuleContactsBRva), ("CapsuleContactsC", GameCompat.FunctionCapsuleContactsCRva));
    }
}
