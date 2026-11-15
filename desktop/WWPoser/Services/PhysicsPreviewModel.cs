using System.Buffers.Binary;
using System.Numerics;

namespace WWPoser.Services;

internal readonly record struct PhysicsModelVertex(Vector3 Position, Vector3 Normal, Vector4 Weights, int J0, int J1, int J2, int J3);
internal readonly record struct PhysicsModelJoint(int RuntimeBone, Matrix4x4 InverseBind);
internal sealed record PhysicsModelMesh(PhysicsModelJoint[] Joints, PhysicsModelVertex[] Vertices, int[] Triangles);

internal sealed record PhysicsPreviewModel(PhysicsModelMesh[] Meshes)
{
    public static string CacheDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WuwaPoser", "physics_mesh_cache", "wuwa-3.8.0");

    public static PhysicsPreviewModel? Load(string directory, OwnedCharacterSelection character, IReadOnlyList<PhysicsBone> bones)
    {
        var indexPath = Path.Combine(directory, "index.json");
        if (!File.Exists(indexPath)) return null;
        var index = JsonData.ReadObject(indexPath);
        if (index["schema"]?.GetValue<int>() != 1) throw new InvalidDataException("模型缓存版本不受支持。");
        if (index["selections"]?[character.CharacterId] is not JsonObject entry) return null;
        if (Contract.S(entry, "profile_id") != character.ProfileId) throw new InvalidDataException("模型缓存与所选骨架不匹配。");
        var lookup = bones.Select((bone, i) => (bone.Name, i)).ToDictionary(row => row.Name, row => row.i, StringComparer.OrdinalIgnoreCase);
        var meshes = Contract.A(entry, "meshes").SelectMany(row =>
        {
            var file = row!.GetValue<string>();
            if (file != Path.GetFileName(file) || !file.EndsWith(".glb", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("模型文件名无效。");
            return ReadGlb(Path.Combine(directory, file), lookup);
        }).ToArray();
        if (meshes.Length == 0) throw new InvalidDataException("模型缓存没有网格。");
        return new(meshes);
    }

    private static PhysicsModelMesh[] ReadGlb(string path, Dictionary<string, int> bones)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);
        if (stream.Length > 96 * 1024 * 1024 || reader.ReadUInt32() != 0x46546c67 || reader.ReadUInt32() != 2 || reader.ReadUInt32() != stream.Length)
            throw new InvalidDataException("模型文件不是有效的 GLB 2.0。");
        var jsonLength = reader.ReadInt32();
        if (jsonLength <= 0 || jsonLength > stream.Length - 20 || reader.ReadUInt32() != 0x4e4f534a)
            throw new InvalidDataException("GLB 模型目录无效。");
        using var json = JsonDocument.Parse(reader.ReadBytes(jsonLength));
        var length = reader.ReadInt32();
        if (length <= 0 || length != stream.Length - stream.Position - 4 || reader.ReadUInt32() != 0x004e4942)
            throw new InvalidDataException("GLB 网格缓冲区无效。");
        var bytes = reader.ReadBytes(length);
        var data = json.RootElement;
        var nodes = data.GetProperty("nodes");

        var models = new List<PhysicsModelMesh>();
        foreach (var node in nodes.EnumerateArray())
        {
            if (!node.TryGetProperty("skin", out var skinIndex) || !node.TryGetProperty("mesh", out var meshIndex)) continue;
            var skin = data.GetProperty("skins")[skinIndex.GetInt32()];
            var joints = ReadJoints(skin, data, bytes, nodes, bones);
            var vertices = new List<PhysicsModelVertex>();
            var triangles = new List<int>();
            foreach (var primitive in data.GetProperty("meshes")[meshIndex.GetInt32()].GetProperty("primitives").EnumerateArray())
            {
                var attributes = primitive.GetProperty("attributes");
                var positions = Access(data, bytes, attributes, "POSITION", 3);
                var normals = Access(data, bytes, attributes, "NORMAL", 3);
                var weights = Access(data, bytes, attributes, "WEIGHTS_0", 4);
                var indices = Access(data, bytes, attributes, "JOINTS_0", 4);
                var faces = Access(data, bytes, primitive, "indices", 1);
                if (positions.Count != normals.Count || positions.Count != weights.Count || positions.Count != indices.Count || faces.Count % 3 != 0)
                    throw new InvalidDataException("GLB 顶点和权重数量不匹配。");
                var start = vertices.Count;
                for (var i = 0; i < positions.Count; i++)
                {
                    var w = new Vector4(weights.Float(i, 0), weights.Float(i, 1), weights.Float(i, 2), weights.Float(i, 3));
                    var sum = w.X + w.Y + w.Z + w.W;
                    if (!(sum > 0) || Vector4.Min(w, Vector4.Zero) != Vector4.Zero) throw new InvalidDataException("GLB 顶点蒙皮权重无效。");
                    vertices.Add(new(positions.Vector(i), normals.Vector(i), w / sum,
                        indices.Index(i, 0, joints.Length), indices.Index(i, 1, joints.Length), indices.Index(i, 2, joints.Length), indices.Index(i, 3, joints.Length)));
                }
                for (var i = 0; i < faces.Count; i++) triangles.Add(start + faces.Index(i, 0, positions.Count));
            }
            if (vertices.Count == 0 || triangles.Count == 0) throw new InvalidDataException("GLB 网格没有可渲染的面。");
            models.Add(new(joints, vertices.ToArray(), triangles.ToArray()));
        }
        return models.ToArray();
    }

    private static PhysicsModelJoint[] ReadJoints(JsonElement skin, JsonElement data, byte[] bytes, JsonElement nodes, Dictionary<string, int> bones)
    {
        var indices = skin.GetProperty("joints").EnumerateArray().Select(node => node.GetInt32()).ToArray();
        var inverse = Access(data, bytes, skin, "inverseBindMatrices", 16);
        if (indices.Length != inverse.Count || indices.Length == 0) throw new InvalidDataException("GLB 骨骼绑定数量不匹配。");
        return indices.Select((node, i) =>
        {
            var name = nodes[node].GetProperty("name").GetString() ?? "";
            if (!bones.TryGetValue(name, out var slot)) throw new InvalidDataException("GLB 骨骼未在当前角色骨架中登记：" + name);
            return new PhysicsModelJoint(slot, inverse.Matrix(i));
        }).ToArray();
    }

    private static BufferAccessor Access(JsonElement data, byte[] bytes, JsonElement node, string property, int columns)
    {
        var a = data.GetProperty("accessors")[node.GetProperty(property).GetInt32()];
        var view = data.GetProperty("bufferViews")[a.GetProperty("bufferView").GetInt32()];
        var type = a.GetProperty("componentType").GetInt32();
        var size = type switch { 5121 => 1, 5123 => 2, 5125 or 5126 => 4, _ => throw new InvalidDataException("GLB 数据类型不受支持。") };
        var count = a.GetProperty("count").GetInt32();
        var offset = view.TryGetProperty("byteOffset", out var outer) ? outer.GetInt32() : 0;
        var inner = a.TryGetProperty("byteOffset", out var innerOffset) ? innerOffset.GetInt32() : 0;
        var stride = view.TryGetProperty("byteStride", out var field) ? field.GetInt32() : columns * size;
        var extent = (long)inner + (long)(count - 1) * stride + columns * size;
        if (count <= 0 || stride < columns * size || offset < 0 || inner < 0 || extent > view.GetProperty("byteLength").GetInt32() || offset + extent > bytes.Length)
            throw new InvalidDataException("GLB 访问范围超出网格缓冲区。");
        return new(bytes, offset + inner, stride, type, size, count);
    }

    private sealed record BufferAccessor(byte[] Data, int Offset, int Stride, int Type, int Size, int Count)
    {
        private ReadOnlySpan<byte> Element(int row, int column) => Data.AsSpan(Offset + row * Stride + column * Size, Size);

        public float Float(int row, int column)
        {
            if (Type != 5126) throw new InvalidDataException("GLB 顶点数据应使用浮点数。");
            var value = BinaryPrimitives.ReadSingleLittleEndian(Element(row, column));
            return float.IsFinite(value) ? value : throw new InvalidDataException("GLB 数据包含无效数值。");
        }

        public int Index(int row, int column, int limit)
        {
            var value = Type switch
            {
                5121 => Element(row, column)[0],
                5123 => BinaryPrimitives.ReadUInt16LittleEndian(Element(row, column)),
                5125 => BinaryPrimitives.ReadUInt32LittleEndian(Element(row, column)),
                _ => throw new InvalidDataException("GLB 索引数据无效。")
            };
            return value < limit ? (int)value : throw new InvalidDataException("GLB 索引超出范围。");
        }

        public Vector3 Vector(int row) => new(Float(row, 0), Float(row, 1), Float(row, 2));

        public Matrix4x4 Matrix(int row) => new(
            Float(row, 0), Float(row, 1), Float(row, 2), Float(row, 3), Float(row, 4), Float(row, 5), Float(row, 6), Float(row, 7),
            Float(row, 8), Float(row, 9), Float(row, 10), Float(row, 11), Float(row, 12), Float(row, 13), Float(row, 14), Float(row, 15));
    }
}
