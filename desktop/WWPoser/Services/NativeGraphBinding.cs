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
        var identities = Contract.O(observation, "identity");
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
            // Linked-owner conversions and group updates do not delimit evaluation cycles.
            bool CycleEvent(JsonNode e)
            {
                var kind = Contract.S(e, "hook_kind");
                return Contract.I(e, "frame_id") == Contract.I(frame!, "id") && kind != "group_update" &&
                    !(kind == "local_to_component" && Contract.S(phases[Contract.I(e, "id")], "owner_role") == "linked");
            }
            var cycles = events.Where(CycleEvent).GroupBy(e => JsonData.CanonicalDigest(
                Contract.Select(phases[Contract.I(e, "id")], ["proxy", "main_proxy", "frame", "counter", "main_frame", "main_counter"])));
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
                var eligible = converters.Where(c => bases.All(b => Encloses(b, c)) && eval.All(e => Precedes(c, e))).ToArray();
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
        return new JsonObject
        {
            ["schema"] = 1,
            ["kind"] = "native_graph_binding",
            ["profile_id"] = profile["profile_id"]!.DeepClone(),
            ["bone_count"] = profile["bone_count"]!.DeepClone(),
            ["observed_frames"] = verifiedFrames.Count,
            ["target"] = Contract.Select(Contract.N(identities, "mesh"), ["address", "index", "serial"]),
            ["main_anim"] = Contract.Select(main, ["address", "index", "serial"]),
            ["converter"] = new JsonObject { ["offset"] = node - mainAddress },
            ["curve_groups"] = activation.Groups
        };
    }

    private static ulong Sequence(JsonNode e, string key) => Contract.Number(Contract.N(e, key));
    private static bool Encloses(JsonNode outer, JsonNode inner) => Contract.Equal(outer["thread"], inner["thread"]) &&
        Sequence(outer, "entry_sequence") < Sequence(inner, "entry_sequence") && Sequence(inner, "exit_sequence") < Sequence(outer, "exit_sequence");
    private static bool Precedes(JsonNode first, JsonNode next) => Contract.Equal(first["thread"], next["thread"]) &&
        Sequence(first, "exit_sequence") < Sequence(next, "entry_sequence");
    private static (JsonArray Groups, bool Complete) Activation(JsonObject candidate, Dictionary<ulong, JsonObject> groups)
    {
        var curves = groups.Values.Where(p => Contract.I(Contract.O(p, "node_fields"), "alpha_type") == GameCompat.AlphaTypeCurve)
            .Select(p => Contract.S(Contract.O(p, "node_fields"), "curve_name")).Order().ToArray();
        // An exported animation graph whose actual nodes have the same group count
        // and curve names, with every curve group simulating and fully rooted.
        bool SameSignature(JsonNode source)
        {
            var signature = Contract.O(source, "group_signature");
            return Contract.S(source, "kind") is "seq_export" or "pc_export" or "ui_export" && Contract.S(source, "evidence_kind") == "actual_nodes" &&
                Contract.I(signature, "kawaii_group_count") == groups.Count &&
                Contract.A(signature, "curve_names").Select(x => x!.GetValue<string>()).Order().SequenceEqual(curves);
        }
        bool CurveGroupsUsable(JsonNode source) => Contract.A(candidate, "candidate_groups")
            .Where(g => Contract.Equal(g!["source_id"], source["sha256"]) && Contract.S(g!, "alpha_input_type") == "Curve")
            .All(g => Contract.True(g!, "simulate_default") && Contract.A(g!, "unmatched_root_bones").Count == 0 && Contract.S(g!, "extraction_kind") == "actual_nodes");
        var matches = Contract.A(candidate, "candidate_sources").Where(s => SameSignature(s!) && CurveGroupsUsable(s!)).ToArray();
        if (groups.Count == 0 || matches.Length == 0 || groups.Values.Any(p => !Contract.True(Contract.O(p, "node_fields"), "root_bones_verified")))
            return (new(), false);
        static string Row(JsonNode g)
        {
            var input = Contract.S(g, "alpha_input_type");
            var alpha = input == "Curve" ? g["alpha_curve"]?.ToJsonString(JsonData.Compact) ?? "null" : "natural";
            var roots = string.Join(",", Contract.A(g, "root_bones").Select(x => x!.GetValue<string>()).Order());
            return Contract.S(g, "node_type") + "|" + input + "|" + alpha + "|" + roots;
        }
        static string Sig(IEnumerable<JsonNode?> rows) => string.Join(";", rows.Select(g => Row(g!)).Order());
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
            output.Add(new JsonObject
            {
                ["offset"] = p["offset_from_main"]!.DeepClone(), ["alpha_type"] = alphaType,
                ["curve_name"] = f["curve_name"]?.DeepClone() ?? JsonValue.Create("None"), ["curve_name_words"] = f["curve_name_words"]!.DeepClone(),
                ["root_bones"] = f["root_bones"]!.DeepClone()
            });
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
