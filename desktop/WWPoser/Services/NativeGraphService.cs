namespace WWPoser.Services;
public sealed partial class NativeGraphService(string repoRoot)
{
    private readonly string root = Path.GetFullPath(repoRoot);
    private readonly CharacterRegistry registry = new(repoRoot);
    // Function byte evidence captured from the supported game build.
    internal FunctionEvidence LoadEvidence()
    {
        var witness = JsonData.ReadObject(Path.Combine(root, "data", "runtime_contracts", "native_functions_3_8.json"));
        Contract.Require(Contract.S(witness, "source_sha256") == GameCompat.SourceBuildSha256, "函数字节证据与当前游戏兼容配置的版本不符。");
        return new FunctionEvidence(Contract.A(witness, "functions"));
    }

    internal sealed class FunctionEvidence(JsonArray rows)
    {
        private readonly ILookup<string, string> byLabel = rows.ToLookup(row => Contract.S(row!, "label"),
            row => Contract.S(row!, "function_bytes_hex").ToLowerInvariant(), StringComparer.Ordinal);

        // Execution addresses come from the compiled compatibility definition.
        public JsonArray Resolve(params (string Label, ulong Rva)[] functions) => new(functions.Select(function =>
        {
            var candidates = byLabel[function.Label];
            var bytes = candidates.FirstOrDefault();
            Contract.Require(bytes is not null && candidates.All(value => value == bytes), "函数字节证据缺失或存在冲突：" + function.Label);
            return (JsonNode)new JsonObject { ["label"] = function.Label, ["rva"] = GameCompat.AddressHex(function.Rva), ["function_bytes_hex"] = bytes };
        }).ToArray());

        public JsonArray RootWitnesses() => Resolve(
            ("GroupInitializeBoneReferences", GameCompat.FunctionGroupInitializeBoneReferencesRva),
            ("GroupInitializeModifyBones", GameCompat.FunctionGroupInitializeModifyBonesRva));
        public JsonArray InputWitnesses() => Resolve(
            ("ConvertLocalToComponentEvaluate", GameCompat.FunctionLocalToComponentRva),
            ("ComponentSpaceTransformGetter", GameCompat.FunctionComponentSpaceTransformGetterRva),
            ("AnimInstanceRootEvaluationCounterWitness", GameCompat.FunctionAnimInstanceRootEvaluationCounterRva),
            ("CachedPoseEvaluationCounterWitness", GameCompat.FunctionCachedPoseEvaluationCounterRva));
        public JsonArray ActivationWitnesses() => Resolve(
            ("SharedSkeletalControlUpdate", GameCompat.FunctionSharedSkeletalControlUpdateRva),
            ("SkeletalControlLODAllowed", GameCompat.FunctionSkeletalControlLODAllowedRva),
            ("GroupIsValidToEvaluate", GameCompat.FunctionGroupIsValidToEvaluateRva),
            ("SharedUpdateInternal", GameCompat.FunctionSharedUpdateInternalRva));
    }
    private static readonly (string Label, ulong Rva)[] ObserverFunctions =
    [
        ("ConvertLocalToComponentEvaluate", GameCompat.FunctionLocalToComponentRva),
        ("SharedSkeletalControlEvaluateCS", GameCompat.FunctionSharedSkeletalControlEvaluateCSRva),
        ("SharedSkeletalControlUpdate", GameCompat.FunctionSharedSkeletalControlUpdateRva),
        ("GroupEvaluate", GameCompat.FunctionGroupEvaluateRva)
    ];

    internal void PrepareIdentity(string configPath, string output, FunctionEvidence evidence)
    {
        var cfg = JsonData.ReadObject(configPath);
        cfg["native_identity_bootstrap"] = new JsonObject
        {
            ["duration_ms"] = 2000, ["owner_limit"] = 32, ["function_witnesses"] = evidence.Resolve(ObserverFunctions),
            ["allocator_witnesses"] = evidence.Resolve(
                ("FUObjectArray_AllocateSerialNumber", GameCompat.FunctionAllocateSerialNumberRva),
                ("FWeakObjectPtr_AssignObject", GameCompat.FunctionAssignWeakObjectRva))
        };
        StageObserver(cfg, output, "native_identity_bootstrap.dll");
    }

    internal void PrepareDiscovery(string configPath, string output, FunctionEvidence evidence)
    {
        var cfg = JsonData.ReadObject(configPath);
        var discovery = new JsonObject { ["duration_ms"] = 6000, ["frame_limit"] = 8, ["node_limit"] = 128,
            ["function_witnesses"] = evidence.Resolve(ObserverFunctions), ["root_layout_witnesses"] = evidence.RootWitnesses() };
        if (Environment.GetEnvironmentVariable("WWPOSER_OBSERVE_KAWAII_SETTINGS") == "1")
            discovery["kawaii_settings_witnesses"] = evidence.Resolve(
                ("GroupUpdatePhysicsSettings", GameCompat.FunctionGroupUpdatePhysicsSettingsRva), ("GroupSimulate", GameCompat.FunctionGroupSimulateRva));
        cfg["native_graph_discovery"] = discovery;
        StageObserver(cfg, output, "native_graph_discovery.dll");
    }

    private void StageObserver(JsonObject cfg, string output, string dll)
    {
        Directory.CreateDirectory(Path.Combine(output, "logs"));
        cfg["compatibility_id"] = GameCompat.CompatibilityId;
        JsonData.Write(Path.Combine(output, "observe_target.json"), cfg);
        File.Copy(Path.Combine(root, "runtime", "game_integration", dll), Path.Combine(output, "observer_carrier.dll"));
        File.Copy(Path.Combine(root, "runtime", "game_integration", "observer_installer.exe"), Path.Combine(output, "observer_installer.exe"));
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

    internal void AttachPlaybackBinding(JsonObject cfg, JsonObject binding, FunctionEvidence evidence)
    {
        cfg["native_graph_binding"] = binding.DeepClone();
        cfg["native_physics_witnesses"] = evidence.InputWitnesses();
        cfg["native_physics_activation_witnesses"] = evidence.ActivationWitnesses();
        cfg["native_physics_settings_witnesses"] = evidence.Resolve(
            ("GroupEvaluate", GameCompat.FunctionGroupEvaluateRva), ("GroupUpdatePhysicsSettings", GameCompat.FunctionGroupUpdatePhysicsSettingsRva),
            ("GroupSimulate", GameCompat.FunctionGroupSimulateRva), ("GroupApplyResult", GameCompat.FunctionGroupApplyResultRva));
        cfg["native_physics_capsule_witnesses"] = evidence.Resolve(
            ("GroupUpdateCapsuleLimits", GameCompat.FunctionGroupUpdateCapsuleLimitsRva), ("GroupWorldCollision", GameCompat.FunctionGroupWorldCollisionRva),
            ("GroupCollisionCandidate", GameCompat.FunctionGroupCollisionCandidateRva), ("CapsuleContactsA", GameCompat.FunctionCapsuleContactsARva),
            ("CapsuleContactsB", GameCompat.FunctionCapsuleContactsBRva), ("CapsuleContactsC", GameCompat.FunctionCapsuleContactsCRva));
    }
}
