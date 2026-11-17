namespace WWPoser.Services;

// Live geometry edits are replayed on the existing shape. Structural edits still use the layout.
internal static class CapsulePresetEdits
{
    public const string PropertyName = "capsule_edits";
    private const string SetActive = "native_capsule_active";
    public static readonly string[] Fields = ["native_capsule_radius", "native_capsule_length", "native_capsule_offset_x",
        "native_capsule_offset_y", "native_capsule_offset_z"];

    public static bool HasEdits(JsonObject? preset) => preset?[PropertyName] is JsonArray { Count: > 0 };

    private static IEnumerable<JsonObject> Rows(JsonObject preset) => preset.ContainsKey(PropertyName)
        ? Contract.A(preset, PropertyName).Select(row => row as JsonObject ?? throw new InvalidDataException("胶囊参数记录无效。")) : [];

    private static bool MatchGroup(JsonObject row, JsonObject group) => NativePhysicsPreset.Matches(row,
        Contract.S(group, "curve_name"), Contract.A(group, "root_bones").Select(root => root!.GetValue<string>()));

    private static JsonArray Geometry(JsonObject shape, string name)
    {
        var values = Contract.A(shape, name);
        Contract.Require(values.Count == Fields.Length, "胶囊几何参数数量有误。");
        return values;
    }

    private static JsonObject[] Shapes(JsonObject? group) => (group?["capsule_shapes"] as JsonArray ?? []).OfType<JsonObject>().ToArray();

    private static bool MatchGroup(JsonObject row, CapsuleDraftGroup group) => NativePhysicsPreset.Matches(row, group.CurveName, group.RootBones);

    public static bool HasGroupEdits(JsonObject? preset, PhysicsGroup group) => preset is not null &&
        Rows(preset).Any(row => NativePhysicsPreset.Matches(row, group.Curve ?? "None", group.Chains.Select(chain => chain.Root)));

    public static CapsuleDraftGroup Overlay(JsonObject? preset, CapsuleDraftGroup group, bool referenceStock)
    {
        if (preset is null) return group;
        var edits = Rows(preset).Where(row => MatchGroup(row, group)).ToArray();
        if (edits.Length == 0) return group;
        return group with { Shapes = group.Shapes.Select(shape => Apply(shape, edits.FirstOrDefault(row => referenceStock
            ? MatchesStock(row, shape) : row["shape_id"]?.GetValue<string>() == shape.Id.ToString("N")))).ToArray() };

        static bool MatchesStock(JsonObject row, CapsuleDraftShape shape)
        {
            if (shape.SourceId is not uint source || row["stock_id"]?.GetValue<uint>() != source ||
                !string.Equals(Contract.S(row, "driver"), shape.Bone ?? "", StringComparison.OrdinalIgnoreCase)) return false;
            float[] original = [shape.Radius, shape.Length, shape.Offset.X, shape.Offset.Y, shape.Offset.Z];
            return Geometry(row, "baseline").Select((value, i) => MathF.Abs(value!.GetValue<float>() - original[i]) <= .02f).All(equal => equal)
                && Contract.A(row, "rotation").Select((value, i) => MathF.Abs(MathF.IEEERemainder(value!.GetValue<float>() - shape.Rotation[i], 360)) <= .02f).All(equal => equal);
        }

        static CapsuleDraftShape Apply(CapsuleDraftShape shape, JsonObject? edit)
        {
            if (edit is null) return shape;
            var values = Geometry(edit, "values");
            return shape with { Radius = values[0]?.GetValue<float>() ?? shape.Radius, Length = values[1]?.GetValue<float>() ?? shape.Length,
                Offset = new(values[2]?.GetValue<float>() ?? shape.Offset.X, values[3]?.GetValue<float>() ?? shape.Offset.Y,
                    values[4]?.GetValue<float>() ?? shape.Offset.Z), Enabled = edit["enabled"]?.GetValue<bool>() ?? shape.Enabled };
        }
    }

    public static void RemoveBakedEdits(JsonObject preset, CapsuleDraftGroup group, bool replacesStock)
    {
        if (preset[PropertyName] is not JsonArray rows) return;
        foreach (var edit in Rows(preset).Where(row => MatchGroup(row, group) && (replacesStock || row["shape_id"] is not null)).ToArray())
            rows.Remove(edit);
        if (rows.Count == 0) preset.Remove(PropertyName);
    }

    public static void Remember(JsonObject preset, JsonObject? launchPreset, JsonObject group, uint liveId)
    {
        var shape = Shapes(group).Single(row => Contract.N(row, "id").GetValue<uint>() == liveId);
        var baseline = Geometry(shape, "baseline");
        var values = Geometry(shape, "values");
        var changes = new JsonArray(values.Select((value, index) => JsonNode.DeepEquals(value, baseline[index]) ? null : value!.DeepClone()).ToArray());
        var enabled = Contract.True(shape, "enabled");
        string? authoredId = null;
        var originalEnabled = true;
        if (shape["layout_index"] is JsonValue layoutIndex)
        {
            var layout = CapsuleAuthoring.Read(launchPreset).Single(row => CapsuleAuthoring.Matches(row, group));
            var authored = layout.Shapes[layoutIndex.GetValue<int>() - 1];
            authoredId = authored.Id.ToString("N");
            originalEnabled = authored.Enabled;
        }

        var rows = Rows(preset).Where(row => !MatchGroup(row, group) || (authoredId is null
            ? row["stock_id"]?.GetValue<uint>() != liveId : row["shape_id"]?.GetValue<string>() != authoredId)).ToList();
        if (changes.Any(value => value is not null) || enabled != originalEnabled)
        {
            var edit = new JsonObject { ["curve_name"] = group["curve_name"]!.DeepClone(),
                ["root_bones"] = group["root_bones"]!.DeepClone(), ["values"] = changes };
            if (authoredId is null)
            {
                edit["stock_id"] = liveId;
                edit["driver"] = shape["driver"]!.DeepClone();
                edit["baseline"] = baseline.DeepClone();
                edit["rotation"] = Contract.A(shape, "rotation").DeepClone();
            }
            else edit["shape_id"] = authoredId;
            if (enabled != originalEnabled) edit["enabled"] = enabled;
            rows.Add(edit);
        }
        if (rows.Count == 0) preset.Remove(PropertyName);
        else preset[PropertyName] = new JsonArray(rows.Select(row => row.DeepClone()).ToArray());
    }

    public static void Validate(JsonObject preset)
    {
        foreach (var row in Rows(preset))
        {
            _ = Contract.S(row, "curve_name");
            foreach (var root in Contract.A(row, "root_bones")) _ = root!.GetValue<string>();
            if (row["shape_id"] is { } authored)
                Contract.Require(Guid.TryParseExact(authored.GetValue<string>(), "N", out _), "胶囊布局 ID 无效。");
            else
            {
                Contract.Require(Contract.N(row, "stock_id").GetValue<uint>() is > 0 and <= 131072, "原厂胶囊 ID 无效。");
                _ = Contract.S(row, "driver");
                var rotation = Contract.A(row, "rotation");
                Contract.Require(rotation.Count == 3, "胶囊旋转参数数量有误。");
                foreach (var field in Geometry(row, "baseline").Concat(rotation)) _ = field!.GetValue<float>();
            }
            if (row["enabled"] is { } enabled) _ = enabled.GetValue<bool>();
            var values = Geometry(row, "values");
            for (var i = 0; i < Fields.Length; i++)
                if (values[i] is { } value) PlaybackCommands.Validate(Fields[i], value.GetValue<double>(), 1, 1, 1);
        }
    }

    public static IEnumerable<NativePhysicsPreset.Control> Plan(JsonObject preset, IReadOnlyList<JsonObject> groups)
    {
        var layouts = CapsuleAuthoring.Read(preset);
        var seen = new HashSet<(uint Group, uint Shape)>();
        foreach (var edit in Rows(preset))
        {
            var group = groups.SingleOrDefault(candidate => MatchGroup(candidate, edit));
            if (group is null) throw new InvalidDataException("预设中的胶囊参数未匹配到本次播放的物理组。");
            var layout = layouts.SingleOrDefault(row => CapsuleAuthoring.Matches(row, group));
            JsonObject? shape;
            if (edit["shape_id"] is JsonValue authored)
            {
                var id = Guid.ParseExact(authored.GetValue<string>(), "N");
                var index = layout?.Shapes.ToList().FindIndex(item => item.Id == id) ?? -1;
                if (index < 0) continue; // The authored shape was removed from the layout.
                shape = Shapes(group).SingleOrDefault(item => item["layout_index"]?.GetValue<int>() == index + 1);
            }
            else
            {
                if (layout?.ReplaceStock == true) continue;
                var stockId = Contract.N(edit, "stock_id").GetValue<uint>();
                shape = Shapes(group).SingleOrDefault(item => Contract.N(item, "id").GetValue<uint>() == stockId &&
                    string.Equals(Contract.S(item, "driver"), Contract.S(edit, "driver"), StringComparison.OrdinalIgnoreCase) &&
                    JsonNode.DeepEquals(item["baseline"], edit["baseline"]) && JsonNode.DeepEquals(item["rotation"], edit["rotation"]));
            }
            if (shape is null || !Contract.True(shape, "editable")) throw new InvalidDataException("预设中的胶囊参数未匹配到本次播放的胶囊。");
            var groupId = Contract.N(group, "group_id").GetValue<uint>();
            var shapeId = Contract.N(shape, "id").GetValue<uint>();
            Contract.Require(seen.Add((groupId, shapeId)), "预设中的胶囊参数重复。");
            var values = Geometry(edit, "values");
            for (var i = 0; i < Fields.Length; i++)
                if (values[i] is { } value) yield return new(Fields[i], value.GetValue<double>(), groupId, shapeId);
            if (edit["enabled"] is { } enabled) yield return new(SetActive, enabled.GetValue<bool>() ? 1 : 0, groupId, shapeId);
        }
    }
}
