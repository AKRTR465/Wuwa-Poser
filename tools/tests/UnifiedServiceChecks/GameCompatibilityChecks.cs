using System.IO;
using System.Text.Json.Nodes;
using WWPoser.Services;

static class GameCompatibilityChecks
{
    public static int Run(string fixture)
    {
        var checks = 0;
        void Check(bool value, string reason) { if (!value) throw new Exception(reason); checks++; }
        void Reject(Action action, string reason)
        {
            try { action(); }
            catch (InvalidDataException) { checks++; return; }
            throw new Exception(reason);
        }

        var root = Path.Combine(fixture, "compatibility-relocated-functions");
        var contracts = Path.Combine(root, "data", "runtime_contracts");
        var observers = Path.Combine(root, "probes", "v20_recon");
        Directory.CreateDirectory(contracts); Directory.CreateDirectory(observers);
        var labels = new[]
        {
            "ConvertLocalToComponentEvaluate", "SharedSkeletalControlEvaluateCS", "SharedSkeletalControlUpdate", "GroupEvaluate",
            "GroupInitializeBoneReferences", "GroupInitializeModifyBones", "ComponentSpaceTransformGetter",
            "AnimInstanceRootEvaluationCounterWitness", "CachedPoseEvaluationCounterWitness", "SkeletalControlLODAllowed",
            "GroupIsValidToEvaluate", "SharedUpdateInternal"
        };
        var archive = new JsonArray(labels.Select((label, index) => (JsonNode)new JsonObject
        {
            ["label"] = label, ["rva"] = "0xdeadbeef", ["function_bytes_hex"] = index.ToString("x2") + "AABB"
        }).ToArray());
        void SaveArchive() => JsonData.Write(Path.Combine(contracts, "native_kawaii.json"), new JsonObject { ["functions"] = archive.DeepClone() });
        SaveArchive();
        // A duplicated archive may describe the same semantic function from
        // another saved image address. Its bytes must agree, its RVA is unused.
        JsonData.Write(Path.Combine(contracts, "native_activation.json"), new JsonObject
        {
            ["functions"] = new JsonArray(new JsonObject { ["label"] = "SharedSkeletalControlUpdate", ["rva"] = "0xcafebabe", ["function_bytes_hex"] = "02aabb" })
        });
        var service = new NativeGraphService(root);
        var input = service.InputWitnesses();
        var expectedInput = new[]
        {
            GameCompat.FunctionLocalToComponentRva, GameCompat.FunctionComponentSpaceTransformGetterRva,
            GameCompat.FunctionAnimInstanceRootEvaluationCounterRva, GameCompat.FunctionCachedPoseEvaluationCounterRva
        };
        Check(input.Select(row => Contract.Number(row!["rva"]!)).SequenceEqual(expectedInput), "relocated input witnesses use generated compatibility addresses, not saved archive RVAs");
        Check(input[0]!["function_bytes_hex"]!.GetValue<string>() == "00aabb", "semantic byte evidence survives address relocation and normalizes hex");
        Check(service.RootWitnesses().Select(row => Contract.Number(row!["rva"]!)).SequenceEqual(new[]
        {
            GameCompat.FunctionGroupInitializeBoneReferencesRva, GameCompat.FunctionGroupInitializeModifyBonesRva
        }), "root witnesses also use generated addresses");
        Check(service.ActivationWitnesses().Count == 4, "matching duplicated byte evidence is accepted independently of archive addresses");

        File.WriteAllText(Path.Combine(observers, "native_graph_discovery.dll"), "offline fixture");
        File.WriteAllText(Path.Combine(observers, "hook_install_v20.exe"), "offline fixture");
        var source = Path.Combine(root, "target.json"); JsonData.Write(source, new JsonObject());
        var staged = Path.Combine(root, "staged"); service.PrepareDiscovery(source, staged, 123);
        var config = JsonData.ReadObject(Path.Combine(staged, "observe_target.json"));
        Check(Contract.S(config, "compatibility_profile") == GameCompat.ProfileId && Contract.S(config, "compatibility_source_sha256") == GameCompat.SourceSha256,
            "observer staging carries the compiled compatibility provenance");

        File.WriteAllText(Path.Combine(observers, "native_identity_bootstrap.dll"), "offline fixture");
        var identityArchivePath = Path.Combine(root, "data", "native_identity_registration_contract.json");
        var identityFunctions = new JsonArray(
            new JsonObject { ["label"] = "FUObjectArray_AllocateSerialNumber", ["rva"] = "0xdeadbeef", ["function_bytes_hex"] = "1122AABB" },
            new JsonObject { ["label"] = "FWeakObjectPtr_AssignObject", ["rva"] = "0xcafebabe", ["function_bytes_hex"] = "3344CCDD" },
            new JsonObject { ["label"] = "FUObjectArray_AllocateSerialNumber", ["rva"] = "0xfeedface", ["function_bytes_hex"] = "1122aabb" });
        void SaveIdentityArchive() => JsonData.Write(identityArchivePath, new JsonObject
        {
            ["schema"] = 1, ["kind"] = "native_identity_registration_contract", ["snapshot_sha256"] = "saved-image-evidence",
            ["functions"] = identityFunctions.DeepClone()
        });
        SaveIdentityArchive();
        var identityArchiveDigest = JsonData.Digest(identityArchivePath);
        var identityStaged = Path.Combine(root, "identity-staged"); service.PrepareIdentity(source, identityStaged, 123);
        var identityContractPath = Path.Combine(identityStaged, "native_identity_registration_contract.json");
        var identityContract = JsonData.ReadObject(identityContractPath);
        var identityConfig = JsonData.ReadObject(Path.Combine(identityStaged, "observe_target.json"));
        var stagedFunctions = Contract.A(identityContract, "functions");
        Check(stagedFunctions.Select(row => Contract.Number(row!["rva"]!)).SequenceEqual(new[]
        {
            GameCompat.FunctionAllocateSerialNumberRva, GameCompat.FunctionAssignWeakObjectRva
        }), "identity allocator witnesses use generated addresses and collapse matching archived duplicates");
        Check(stagedFunctions[0]!["function_bytes_hex"]!.GetValue<string>() == "1122aabb" && stagedFunctions[1]!["function_bytes_hex"]!.GetValue<string>() == "3344ccdd",
            "identity staging associates saved bytes by semantic function label");
        Check(Contract.S(Contract.O(identityConfig, "native_identity_bootstrap"), "registration_contract_sha256") == JsonData.Digest(identityContractPath),
            "identity configuration hashes the actual staged contract");
        Check(JsonData.Digest(identityArchivePath) == identityArchiveDigest && Contract.S(identityContract, "source_archive_sha256") == identityArchiveDigest,
            "identity staging preserves the original archive and records its provenance");

        var removedIdentity = identityFunctions[1]!.DeepClone(); identityFunctions.RemoveAt(1); SaveIdentityArchive();
        Reject(() => service.PrepareIdentity(source, Path.Combine(root, "identity-missing"), 123), "missing identity semantic byte evidence must be rejected");
        identityFunctions.Insert(1, removedIdentity);
        identityFunctions.Add(new JsonObject { ["label"] = "FWeakObjectPtr_AssignObject", ["rva"] = "0x1234", ["function_bytes_hex"] = "ffff" }); SaveIdentityArchive();
        Reject(() => service.PrepareIdentity(source, Path.Combine(root, "identity-conflict"), 123), "conflicting identity semantic byte evidence must be rejected");

        var missing = archive[0]!.DeepClone(); archive.RemoveAt(0); SaveArchive();
        Reject(() => service.InputWitnesses(), "missing semantic byte evidence must be rejected even when archive RVAs are populated");
        archive.Insert(0, missing); archive.Add(new JsonObject { ["label"] = labels[0], ["rva"] = "0xdeadbeef", ["function_bytes_hex"] = "ffff" }); SaveArchive();
        Reject(() => service.InputWitnesses(), "conflicting byte evidence for one semantic function must be rejected");
        return checks;
    }
}
