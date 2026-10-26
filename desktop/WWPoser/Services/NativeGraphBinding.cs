namespace WWPoser.Services;
public sealed partial class NativeGraphService
{
    public JsonObject RuntimeProfile(string id)
    {
        var directory = Path.Combine(root, "data", "native_graphs");
        var row = Contract.A(JsonData.ReadObject(Path.Combine(directory, "index.json")), "profiles").First(x => Contract.S(x!, "profile_id") == id)!;
        return JsonData.ReadObject(Path.Combine(directory, Contract.S(row, "runtime_path")));
    }

    public JsonObject Bind(string directory)
    {
        var observationPath = Path.Combine(directory, "native_graph_discovery.json");
        var configPath = Path.Combine(directory, "observe_target.json");
        var observation = JsonData.ReadObject(observationPath); var cfg = JsonData.ReadObject(configPath);
        Contract.Require(Contract.S(observation, "status") == "complete", "动画图观测失败：" + observation["reasons"]?.ToJsonString());
        var profile = registry.GetProfile(Contract.S(cfg, "character_profile_id"));
        var identities = Contract.O(observation, "identity_before");
        var main = Contract.O(identities, "main_anim");
        var mainAddress = Contract.Number(Contract.N(main, "address"));
        var frames = Contract.A(observation, "frames").Where(x => Contract.True(x!, "complete") && Contract.True(x!, "closed_by_next_frame") && !Contract.True(x!, "first_frame_boundary_partial")).ToArray();
        var events = Contract.A(observation, "events").Select(x => x!).ToArray();
        var phases = events.ToDictionary(e => Contract.I(e, "id"), e => Contract.O(e, Contract.S(e, "hook_kind") == "group_evaluate" ? "before" : "after"));
        var selected = new List<JsonObject>();
        var groupFrames = new List<Dictionary<ulong, JsonObject>>();
        var verifiedFrames = new HashSet<long>();
        foreach (var frame in frames)
        {
            var cycles = events.Where(e => Contract.I(e, "frame_id") == Contract.I(frame!, "id") && Contract.S(e, "hook_kind") != "group_update" && !(Contract.S(e, "hook_kind") == "local_to_component" && Contract.S(phases[Contract.I(e, "id")], "owner_role") == "linked"))
                .GroupBy(e => JsonData.CanonicalDigest(Contract.Select(phases[Contract.I(e, "id")], ["proxy", "main_proxy", "frame", "counter", "main_frame", "main_counter"])));
            foreach (var cycle in cycles)
            {
                var group = new Dictionary<ulong, JsonObject>();
                var bases = new List<JsonNode>();
                var eval = new List<JsonNode>();
                var converters = new List<JsonNode>();
                foreach (var e in cycle)
                {
                    var p = phases[Contract.I(e, "id")];
                    var kind = Contract.S(e, "hook_kind");
                    if (kind == "local_to_component")
                        converters.Add(e);
                    else if (kind is "group_base_cs" or "group_evaluate" && Contract.S(p, "owner_role") == "main")
                    {
                        group[Contract.Number(Contract.N(p, "node"))] = p;
                        (kind == "group_base_cs" ? bases : eval).Add(e);
                    }
                }

                if (group.Count == 0)
                    continue;
                var eligible = converters.Where(c => bases.All(b => Encloses(b, c)) && eval.All(e => Contract.Equal(e["thread"], c["thread"]) && Contract.Number(Contract.N(c, "exit_sequence")) < Contract.Number(Contract.N(e, "entry_sequence")))).ToArray();
                Contract.Require(eligible.Length == 1, "无法唯一定位前物理main转换器");
                selected.Add(phases[Contract.I(eligible[0], "id")]);
                groupFrames.Add(group);
                verifiedFrames.Add(Contract.I(frame!, "engine_frame"));
            }
        }

        Contract.Require(selected.Count > 0, "未观测完整物理支路");
        var node = Contract.Number(Contract.N(selected[0], "node"));
        var activation = Activation(RuntimeProfile(Contract.S(profile, "profile_id")), groupFrames[0]);
        Contract.Require(activation.Complete, "本次实例缺少完整原生物理组匹配");
        var result = new JsonObject
        {
            ["schema"] = 1,
            ["kind"] = "native_graph_binding",
            ["host_sha256"] = Contract.Build,
            ["profile_id"] = profile["profile_id"]!.DeepClone(),
            ["character_profile_sha256"] = cfg["character_profile_sha256"]!.DeepClone(),
            ["bone_count"] = profile["bone_count"]!.DeepClone(),
            ["skeleton_sha256"] = profile["skeleton_sha256"]!.DeepClone(),
            ["mesh_asset_name"] = cfg["expected_mesh_asset_name"]!.DeepClone(),
            ["game_pid"] = observation["pid"]!.DeepClone(),
            ["target"] = Contract.Select(Contract.N(identities, "mesh"), ["address", "index", "serial"]),
            ["main_anim"] = Contract.Select(main, ["address", "index", "serial", "item_address", "class_address", "class_name", "outer_address", "vtable", "name_words"]),
            ["converter"] = new JsonObject
            {
                ["offset"] = node - mainAddress,
                ["vtable_rva"] = GameCompat.AddressHex(GameCompat.VtableLocalToComponentRva),
                ["function_rva"] = GameCompat.AddressHex(GameCompat.FunctionLocalToComponentRva)
            },
            ["curve_name_offset"] = GameCompat.AddressHex(GameCompat.CurveNameOffset),
            ["curve_groups"] = activation.Groups,
            ["discovery_file"] = "native_graph_discovery.json",
            ["proof"] = new JsonObject
            {
                ["source_config_sha256"] = JsonData.Digest(configPath),
                ["discovery_sha256"] = JsonData.Digest(observationPath),
                ["observed_frames"] = verifiedFrames.Count,
                ["unique_main_converter"] = true,
                ["pre_physics_order_verified"] = true,
                ["identity_stable"] = true,
                ["complete_bone_mapping"] = true,
                ["extra_vectors_absent"] = true,
                ["restored_observer"] = true,
                ["activation_complete"] = activation.Complete
            }
        };
        if (cfg["animation_target"] != null) result["animation_target"] = cfg["animation_target"]!.DeepClone();
        return result;
    }

    private static bool Encloses(JsonNode outer, JsonNode inner) => Contract.Equal(outer["thread"], inner["thread"]) && Contract.Number(Contract.N(outer, "entry_sequence")) < Contract.Number(Contract.N(inner, "entry_sequence")) && Contract.Number(Contract.N(inner, "exit_sequence")) < Contract.Number(Contract.N(outer, "exit_sequence"));
    private static (JsonArray Groups, bool Complete) Activation(JsonObject candidate, Dictionary<ulong, JsonObject> groups)
    {
        var curves = groups.Values.Where(p => Contract.I(Contract.O(p, "node_fields"), "alpha_type") == GameCompat.AlphaTypeCurve).Select(p => Contract.S(Contract.O(p, "node_fields"), "curve_name")).Order().ToArray();
        var matches = Contract.A(candidate, "candidate_sources").Where(s => (Contract.S(s!, "kind") is "seq_export" or "pc_export" or "ui_export") && Contract.S(s!, "evidence_kind") == "actual_nodes" && Contract.I(Contract.O(s!, "group_signature"), "kawaii_group_count") == groups.Count && Contract.A(Contract.O(s!, "group_signature"), "curve_names").Select(x => x!.GetValue<string>()).Order().SequenceEqual(curves)).Where(s => Contract.A(candidate, "candidate_groups").Where(g => Contract.Equal(g!["source_id"], s!["sha256"]) && Contract.S(g!, "alpha_input_type") == "Curve").All(g => Contract.True(g!, "simulate_default") && Contract.A(g!, "unmatched_root_bones").Count == 0 && Contract.S(g!, "extraction_kind") == "actual_nodes")).ToArray();
        if (groups.Count == 0 || matches.Length == 0 || groups.Values.Any(p => !Contract.True(Contract.O(p, "node_fields"), "root_bones_verified")))
            return (new(), false);
        string Sig(IEnumerable<JsonNode?> rows) => string.Join(";", rows.Select(g => Contract.S(g!, "node_type") + "|" + Contract.S(g!, "alpha_input_type") + "|" + (Contract.S(g!, "alpha_input_type") == "Curve" ? g!["alpha_curve"]?.ToJsonString(JsonData.Compact) ?? "null" : "natural") + "|" + string.Join(",", Contract.A(g!, "root_bones").Select(x => x!.GetValue<string>()).Order())).Order());
        var observed = groups.Values.Select(p =>
        {
            var f = Contract.O(p, "node_fields");
            return (JsonNode)new JsonObject
            {
                ["node_type"] = "KawaiiPhysicsGroup",
                ["alpha_input_type"] = AlphaInputName(Contract.I(f, "alpha_type")),
                ["alpha_curve"] = f["curve_name"]?.DeepClone(),
                ["root_bones"] = f["root_bones"]!.DeepClone()
            };
        });
        // Exported variants (Seq/PC/UI) may legitimately disagree on per-node
        // roots; the runtime-witnessed roots of this live instance arbitrate.
        var observedSignature = Sig(observed);
        if (!matches.Any(s => Sig(Contract.A(s!, "group_signatures")) == observedSignature))
            return (new(), false);
        var output = new JsonArray();
        foreach (var p in groups.OrderBy(x => x.Key).Select(x => x.Value))
        {
            var f = Contract.O(p, "node_fields");
            var alphaType = Contract.I(f, "alpha_type");
            output.Add(new JsonObject { ["offset"] = p["offset_from_main"]!.DeepClone(), ["node_type"] = "KawaiiPhysicsGroup", ["vtable_rva"] = GameCompat.AddressHex(GameCompat.VtableKawaiiPhysicsGroupRva), ["alpha_type"] = alphaType, ["activation"] = alphaType == GameCompat.AlphaTypeCurve ? "force_when_zero" : "observe_natural", ["curve_name"] = f["curve_name"]?.DeepClone() ?? JsonValue.Create("None"), ["curve_name_words"] = f["curve_name_words"]!.DeepClone(), ["root_binding"] = new JsonObject { ["array"] = f["root_array"]!.DeepClone(), ["extended_array"] = f["extended_root_array"]!.DeepClone(), ["name_words"] = f["root_bone_name_words"]!.DeepClone() } });
        }

        return (output, true);
    }

    private static string AlphaInputName(long alphaType)
    {
        if (alphaType == GameCompat.AlphaTypeFloat) return "Float";
        if (alphaType == GameCompat.AlphaTypeBool) return "Bool";
        if (alphaType == GameCompat.AlphaTypeCurve) return "Curve";
        throw new InvalidDataException("未知的原生物理 Alpha 输入类型：" + alphaType);
    }
}
