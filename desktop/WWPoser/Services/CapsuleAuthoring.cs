using System.Globalization;
using System.Numerics;

namespace WWPoser.Services;

internal sealed record CapsuleDraftShape(Guid Id, uint? SourceId, string? Bone, int Array, float Radius, float Length,
    Vector3 Offset, Vector3 Rotation, bool Enabled);

internal sealed record CapsuleDraftGroup(string CurveName, IReadOnlyList<string> RootBones, IReadOnlyList<CapsuleDraftShape> Shapes,
    bool ReplaceStock = true, IReadOnlyList<CapsuleDraftShape>? StockBaseline = null);

internal static class CapsuleAuthoring
{
    public const string PropertyName = "capsule_groups";
    public const int MaxGroups = 32;
    public const int MaxShapes = 1024;
    public const float MaxRadius = 100;
    public const float MaxLength = 300;
    public const float MaxOffset = 300;
    public const float MaxRotation = 3600;
    private static readonly StringComparer Names = StringComparer.OrdinalIgnoreCase;

    public static IReadOnlyList<CapsuleDraftGroup> Read(JsonObject? preset)
    {
        if (preset is null || !preset.ContainsKey(PropertyName)) return [];
        return ReadGroups(RequireArray(preset[PropertyName], "胶囊组列表无效。"));
    }

    public static void Write(JsonObject preset, IEnumerable<CapsuleDraftGroup> groups)
    {
        var data = WriteGroups(groups);
        if (data.Count > 0) preset[PropertyName] = data;
        else preset.Remove(PropertyName);
    }

    public static IReadOnlyList<CapsuleDraftGroup> ReadGroups(JsonArray rows)
    {
        Contract.Require(rows.Count <= MaxGroups, "预设中的胶囊组过多。");
        var groups = rows.Select(row =>
        {
            var group = RequireObject(row, "胶囊组无效。");
            var roots = RequireArray(group["root_bones"], "胶囊组的根骨列表无效。");
            Contract.Require(roots.Count <= 1024, "预设中的胶囊组根骨过多。");
            return new CapsuleDraftGroup(
                ReadName(group["curve_name"], "胶囊组名称无效。"),
                roots.Select(root => ReadName(root, "胶囊组的根骨名称无效。")).ToArray(),
                ReadShapes(group["shapes"]),
                !group.ContainsKey("replace_stock") || ReadFlag(group["replace_stock"], "胶囊组替换方式无效。"),
                group.ContainsKey("stock_baseline") ? ReadShapes(group["stock_baseline"]) : null);
        }).ToArray();
        ValidateGroups(groups);
        return groups;
    }

    public static JsonArray WriteGroups(IEnumerable<CapsuleDraftGroup> groups)
    {
        var rows = groups.ToArray();
        ValidateGroups(rows);
        return new JsonArray(rows.Select(group =>
        {
            var entry = new JsonObject
            {
                ["curve_name"] = group.CurveName,
                ["root_bones"] = new JsonArray(group.RootBones.Select(root => (JsonNode)JsonValue.Create(root)!).ToArray()),
                ["shapes"] = WriteShapes(group.Shapes)
            };
            if (!group.ReplaceStock) entry["replace_stock"] = false;
            if (group.StockBaseline is { } baseline) entry["stock_baseline"] = WriteShapes(baseline);
            return (JsonNode)entry;
        }).ToArray());
    }

    public static CapsuleDraftShape ReadShape(JsonObject row)
    {
        var text = ReadName(row["id"], "胶囊 ID 无效。");
        if (!Guid.TryParseExact(text, "N", out var id) || id == Guid.Empty)
            throw new InvalidDataException("胶囊 ID 无效。");
        var source = row["source_id"] is { } sourceId ? ReadUInt(sourceId, "原厂胶囊 ID 无效。") : (uint?)null;
        var array = ReadUInt(row["array"], "胶囊数组无效。");
        Contract.Require(array <= 1, "胶囊数组无效。");
        var shape = new CapsuleDraftShape(id, source, row["bone"] is { } bone ? ReadName(bone, "胶囊驱动骨无效。") : null,
            (int)array, ReadFloat(row["radius"]), ReadFloat(row["length"]), ReadVector(row["offset"]), ReadVector(row["rotation"]),
            ReadFlag(row["enabled"], "胶囊启用状态无效。"));
        Validate(shape);
        return shape;
    }

    public static JsonObject WriteShape(CapsuleDraftShape shape)
    {
        Validate(shape);
        var row = new JsonObject
        {
            ["id"] = shape.Id.ToString("N"),
            ["bone"] = shape.Bone,
            ["array"] = shape.Array,
            ["radius"] = shape.Radius,
            ["length"] = shape.Length,
            ["offset"] = WriteVector(shape.Offset),
            ["rotation"] = WriteVector(shape.Rotation),
            ["enabled"] = shape.Enabled
        };
        if (shape.SourceId is { } sourceId) row["source_id"] = sourceId;
        return row;
    }

    public static void Validate(CapsuleDraftShape shape)
    {
        Contract.Require(shape.Id != Guid.Empty && shape.Array is 0 or 1, "胶囊 ID 或数组无效。");
        if (shape.SourceId is { } sourceId)
        {
            var first = ((uint)shape.Array << 16) + 1;
            Contract.Require(sourceId >= first && sourceId - first < MaxShapes, "原厂胶囊 ID 与胶囊数组不匹配。");
        }
        Contract.Require(shape.Bone is null || ValidName(shape.Bone), "胶囊驱动骨无效。");
        Contract.Require(shape.Radius is >= 0 and <= MaxRadius && shape.Length is >= 0 and <= MaxLength &&
            InBounds(shape.Offset, MaxOffset) && InBounds(shape.Rotation, MaxRotation), "胶囊尺寸、偏移或旋转超出范围。");
    }

    public static bool Matches(CapsuleDraftGroup group, PhysicsGroup target) =>
        Names.Equals(group.CurveName, target.Curve ?? "None") &&
        group.RootBones.ToHashSet(Names).SetEquals(target.Chains.Select(chain => chain.Root));

    public static bool Matches(CapsuleDraftGroup group, JsonObject target) =>
        NativePhysicsPreset.Matches(target, group.CurveName, group.RootBones);

    public static void ValidateTargets(IEnumerable<CapsuleDraftGroup> groups, PhysicsPartProfile profile)
    {
        var bones = profile.Bones.Select(bone => bone.Name).ToHashSet(Names);
        foreach (var group in groups)
        {
            Contract.Require(profile.Groups.Any(target => Matches(group, target)), "预设中的胶囊组不属于所选角色。");
            Contract.Require(group.Shapes.Concat(group.StockBaseline ?? []).All(shape => shape.Bone is null || bones.Contains(shape.Bone)),
                "预设中的胶囊驱动骨不属于所选角色。");
        }
    }

    public static CapsuleDraftShape[] Split(CapsuleDraftShape shape)
    {
        Validate(shape);
        Contract.Require(shape.Length > 0, "球形胶囊没有可拆分的轴段。");
        var displacement = Axis(shape.Rotation) * (shape.Length / 4);
        var parts = new[]
        {
            shape with { Id = Guid.NewGuid(), Length = shape.Length / 2, Offset = shape.Offset - displacement },
            shape with { Id = Guid.NewGuid(), Length = shape.Length / 2, Offset = shape.Offset + displacement }
        };
        foreach (var part in parts) Validate(part);
        return parts;
    }

    public static CapsuleDraftShape Merge(IReadOnlyList<CapsuleDraftShape> shapes)
    {
        Contract.Require(shapes.Count >= 2, "请至少选中两个胶囊。");
        var first = shapes[0];
        Contract.Require(shapes.All(shape => Names.Equals(shape.Bone, first.Bone) && shape.Array == first.Array),
            "胶囊必须使用相同的驱动骨和数组才能合并。");

        // Bound every source segment and its radius along the first capsule's axis.
        var direction = Axis(first.Rotation);
        var minimum = float.PositiveInfinity;
        var maximum = float.NegativeInfinity;
        var radius = 0f;
        foreach (var shape in shapes)
        {
            Validate(shape);
            var extent = Axis(shape.Rotation) * (shape.Length / 2);
            Include(shape.Offset - extent, shape.Radius);
            Include(shape.Offset + extent, shape.Radius);
        }
        var merged = first with
        {
            Id = Guid.NewGuid(),
            Length = maximum - minimum,
            Radius = radius,
            Offset = first.Offset + direction * ((minimum + maximum) / 2),
            Enabled = shapes.Any(shape => shape.Enabled)
        };
        Validate(merged);
        return merged;

        void Include(Vector3 endpoint, float sourceRadius)
        {
            var delta = endpoint - first.Offset;
            var axial = Vector3.Dot(delta, direction);
            minimum = MathF.Min(minimum, axial);
            maximum = MathF.Max(maximum, axial);
            radius = MathF.Max(radius, (delta - axial * direction).Length() + sourceRadius);
        }
    }

    public static Vector3 Axis(Vector3 rotation)
    {
        var half = rotation * (MathF.PI / 360);
        var (sp, cp) = MathF.SinCos(half.X);
        var (sy, cy) = MathF.SinCos(half.Y);
        var (sr, cr) = MathF.SinCos(half.Z);
        var quaternion = new Quaternion(cr * sp * sy - sr * cp * cy, -cr * sp * cy - sr * cp * sy,
            cr * cp * sy - sr * sp * cy, cr * cp * cy + sr * sp * sy);
        return Vector3.Normalize(Vector3.Transform(Vector3.UnitZ, quaternion));
    }

    private static void ValidateGroups(IReadOnlyList<CapsuleDraftGroup> groups)
    {
        Contract.Require(groups.Count <= MaxGroups, "预设中的胶囊组过多。");
        var targets = new HashSet<(string Curve, string Roots)>();
        var ids = new HashSet<Guid>();
        foreach (var group in groups)
        {
            Contract.Require(ValidName(group.CurveName) && group.RootBones.Count <= 1024 &&
                group.RootBones.All(ValidName) && group.RootBones.ToHashSet(Names).Count == group.RootBones.Count, "胶囊组的名称或根骨无效。");
            var roots = string.Join('\0', group.RootBones.Select(root => root.ToUpperInvariant()).Order(StringComparer.Ordinal));
            Contract.Require(targets.Add((group.CurveName.ToUpperInvariant(), roots)), "胶囊组重复。");
            Contract.Require(group.Shapes.Count <= MaxShapes, "一个物理组的胶囊过多。");
            foreach (var shape in group.Shapes)
            {
                Validate(shape);
                Contract.Require(ids.Add(shape.Id), "胶囊 ID 重复。");
            }
            if (group.StockBaseline is not { } baseline) continue;
            Contract.Require(group.ReplaceStock && baseline.Count <= MaxShapes, "只有完整替换可使用原厂胶囊基线。");
            var originalIds = new HashSet<uint>();
            foreach (var shape in baseline)
            {
                Validate(shape);
                Contract.Require(shape.SourceId is { } id && originalIds.Add(id), "原厂胶囊基线的原始 ID 缺失或重复。");
            }
        }
    }

    private static bool ValidName(string value) =>
        value.Length is > 0 and <= 256 && !string.IsNullOrWhiteSpace(value) && !value.Any(char.IsControl);

    private static bool InBounds(Vector3 vector, float bound) =>
        MathF.Abs(vector.X) <= bound && MathF.Abs(vector.Y) <= bound && MathF.Abs(vector.Z) <= bound;

    private static JsonObject RequireObject(JsonNode? node, string reason) =>
        node as JsonObject ?? throw new InvalidDataException(reason);

    private static JsonArray RequireArray(JsonNode? node, string reason) =>
        node as JsonArray ?? throw new InvalidDataException(reason);

    private static string ReadName(JsonNode? node, string reason) =>
        node is JsonValue value && value.TryGetValue<string>(out var name) && name is not null && ValidName(name)
            ? name : throw new InvalidDataException(reason);

    private static bool ReadFlag(JsonNode? node, string reason) =>
        node is JsonValue value && value.TryGetValue<bool>(out var flag) ? flag : throw new InvalidDataException(reason);

    private static CapsuleDraftShape[] ReadShapes(JsonNode? node)
    {
        var shapes = RequireArray(node, "胶囊列表无效。");
        Contract.Require(shapes.Count <= MaxShapes, "一个物理组的胶囊过多。");
        return shapes.Select(shape => ReadShape(RequireObject(shape, "胶囊条目无效。"))).ToArray();
    }

    private static JsonArray WriteShapes(IEnumerable<CapsuleDraftShape> shapes) =>
        new(shapes.Select(shape => (JsonNode)WriteShape(shape)).ToArray());

    private static uint ReadUInt(JsonNode? node, string reason) =>
        node is JsonValue number && number.GetValueKind() == JsonValueKind.Number &&
        uint.TryParse(number.ToJsonString(), NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            ? value : throw new InvalidDataException(reason);

    private static float ReadFloat(JsonNode? node) =>
        node is JsonValue number && number.GetValueKind() == JsonValueKind.Number &&
        float.TryParse(number.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && float.IsFinite(value)
            ? value : throw new InvalidDataException("胶囊几何数值无效。");

    private static Vector3 ReadVector(JsonNode? node)
    {
        if (node is not JsonArray { Count: 3 } array) throw new InvalidDataException("胶囊向量必须有三个分量。");
        return new(ReadFloat(array[0]), ReadFloat(array[1]), ReadFloat(array[2]));
    }

    private static JsonArray WriteVector(Vector3 value) => new(JsonValue.Create(value.X), JsonValue.Create(value.Y), JsonValue.Create(value.Z));
}
