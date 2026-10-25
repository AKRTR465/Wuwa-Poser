using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace WWPoser.Services;

// Validate the immutable files that will be loaded before starting any game-side
// module. The native reader independently enforces the same WWLOCAL1 bounds.
internal static class CompiledMotionValidation
{
    private const int HeaderBytes = 160, BoneBytes = 32, MaximumBones = 1024, MaximumFrames = 100000;
    private const long MaximumFileBytes = 1024L * 1024 * 1024, MaximumProfileBytes = 16L * 1024 * 1024;
    private sealed record Header(uint BoneCount, uint FrameCount, uint Fps, double Duration, string SourceHash, string SkeletonHash);

    public static void Validate(string actorDirectory, JsonObject config)
    {
        var root = Path.GetFullPath(actorDirectory);
        var clipPath = FilePath(root, config, "local_clip_file", "motion.wwlocal");
        var profilePath = FilePath(root, config, "character_profile_file", "character_profile.json");
        var skeletonPath = FilePath(root, config, "character_skeleton_file", "character_skeleton.json");
        var sourcePath = FilePath(root, config, "source_file", "source.vmd");
        var header = ReadClip(clipPath, HashField(config, "local_clip_sha256"));
        Require(Integer(config["bone_count"], "bone_count") == header.BoneCount &&
            Integer(config["frame_count"], "frame_count") == header.FrameCount &&
            Integer(config["fps"], "fps") == header.Fps, "动作文件尺寸与准备配置不一致，请重新准备动作。");
        var duration = Number(config["clip_duration_seconds"], "clip_duration_seconds");
        Require(double.IsFinite(duration) && Math.Abs(duration - header.Duration) <= 1e-8 * Math.Max(1, header.Duration),
            "动作时长与准备配置不一致，请重新准备动作。");

        var profile = ReadProfile(profilePath, HashField(config, "character_profile_sha256"));
        Require(Text(profile["profile_id"], "profile_id") == Text(config["profile_id"], "profile_id") &&
            Integer(profile["bone_count"], "bone_count") == header.BoneCount, "角色适配数据与动作不一致。");
        var names = Array(config, "bone_names", header.BoneCount);
        var parents = Array(config, "bone_parents", header.BoneCount);
        var profileNames = Array(profile, "bone_names", header.BoneCount);
        var profileParents = Array(profile, "bone_parents", header.BoneCount);
        var uniqueNames = new HashSet<string>(StringComparer.Ordinal);
        for (var bone = 0; bone < names.Count; bone++)
        {
            var name = Text(names[bone], "bone_names");
            var parent = Integer(parents[bone], "bone_parents");
            Require(uniqueNames.Add(name) && name == Text(profileNames[bone], "bone_names") &&
                parent == Integer(profileParents[bone], "bone_parents") &&
                (bone == 0 ? parent == -1 : parent >= 0 && parent < bone), "动作骨骼名称或层级与角色适配数据不一致。");
        }
        ValidateCanonical(config, header.BoneCount, "canonical_position_mask", "canonical_local_positions", 3);
        ValidateCanonical(config, header.BoneCount, "canonical_rotation_mask", "canonical_local_rotations", 4);
        var skeletonHash = HashField(profile, "skeleton_sha256");
        Require(Digest(skeletonPath).Equals(skeletonHash, StringComparison.OrdinalIgnoreCase), "准备目录中的骨架文件已更改。");
        // --omit-provenance writes two entirely zeroed fields. It remains a
        // supported format; the complete clip and profile hashes still bind it.
        if (header.SourceHash.Length != 0)
        {
            Require(header.SkeletonHash.Equals(skeletonHash, StringComparison.OrdinalIgnoreCase), "动作绑定的骨架与角色适配数据不一致。");
            Require(Digest(sourcePath).Equals(header.SourceHash, StringComparison.OrdinalIgnoreCase), "准备目录中的源动作与编译结果不一致。");
        }
    }

    private static Header ReadClip(string path, string expectedHash)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.SequentialScan);
        Require(stream.Length >= HeaderBytes && stream.Length <= MaximumFileBytes, "动作文件大小超出支持范围。");
        Span<byte> bytes = stackalloc byte[HeaderBytes]; stream.ReadExactly(bytes);
        Require(bytes[..8].SequenceEqual("WWLOCAL1"u8) && BinaryPrimitives.ReadUInt32LittleEndian(bytes[8..]) == 1,
            "动作文件格式或版本无效。");
        var bones = BinaryPrimitives.ReadUInt32LittleEndian(bytes[12..]);
        var frames = BinaryPrimitives.ReadUInt32LittleEndian(bytes[16..]);
        var fps = BinaryPrimitives.ReadUInt32LittleEndian(bytes[20..]);
        var duration = BinaryPrimitives.ReadDoubleLittleEndian(bytes[24..]);
        Require(bones > 0 && bones <= MaximumBones && frames > 0 && frames <= MaximumFrames && fps > 0,
            "动作骨骼数、帧数或帧率超出支持范围。");
        var expectedDuration = (double)(frames - 1) / fps;
        Require(double.IsFinite(duration) && duration >= 0 && Math.Abs(duration - expectedDuration) <= 1e-8 * Math.Max(1, expectedDuration),
            "动作时长与帧数、帧率不一致。");
        var payloadBytes = (long)bones * frames * BoneBytes;
        Require(payloadBytes <= MaximumFileBytes - HeaderBytes && stream.Length == HeaderBytes + payloadBytes,
            "动作文件长度与声明的骨骼数、帧数不一致。");
        var omitted = true;
        foreach (var value in bytes[32..]) if (value != 0) { omitted = false; break; }
        var sourceHash = omitted ? "" : Encoding.ASCII.GetString(bytes[32..96]);
        var skeletonHash = omitted ? "" : Encoding.ASCII.GetString(bytes[96..160]);
        Require(omitted || IsHash(sourceHash) && IsHash(skeletonHash), "动作来源散列必须是两个完整的 SHA-256 值。");

        using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256); digest.AppendData(bytes);
        var buffer = new byte[65536]; var masks = new uint[bones]; long element = 0, remaining = payloadBytes;
        while (remaining > 0)
        {
            var count = (int)Math.Min(remaining, buffer.Length);
            stream.ReadExactly(buffer.AsSpan(0, count)); digest.AppendData(buffer, 0, count);
            for (var offset = 0; offset < count; offset += BoneBytes, element++)
            {
                var command = buffer.AsSpan(offset, BoneBytes);
                var mask = BinaryPrimitives.ReadUInt32LittleEndian(command[28..]); var bone = (int)(element % bones);
                Require((mask & ~3u) == 0 && ((mask & 2u) == 0 || bone == 0), "动作包含不支持的骨骼命令。");
                if (element < bones) masks[bone] = mask;
                else Require(masks[bone] == mask, "动作的骨骼控制掩码在帧间发生变化。");
                double norm = 0;
                for (var component = 0; component < 7; component++)
                {
                    var value = BinaryPrimitives.ReadSingleLittleEndian(command[(component * 4)..]);
                    Require(float.IsFinite(value), "动作包含无效的平移或旋转数值。");
                    if (component >= 3) norm += (double)value * value;
                }
                Require((mask & 1u) == 0 || Math.Abs(norm - 1) <= 1e-3, "动作包含非单位旋转四元数。");
            }
            remaining -= count;
        }
        Require(stream.ReadByte() == -1, "动作文件含有多余数据。");
        Require(Convert.ToHexStringLower(digest.GetHashAndReset()).Equals(expectedHash, StringComparison.OrdinalIgnoreCase),
            "编译后的动作文件已更改或损坏，请重新准备动作。");
        return new(bones, frames, fps, duration, sourceHash, skeletonHash);
    }

    private static void ValidateCanonical(JsonObject config, uint bones, string maskKey, string valuesKey, int width)
    {
        var masks = Array(config, maskKey, bones); var rows = Array(config, valuesKey, bones);
        for (var bone = 0; bone < masks.Count; bone++)
        {
            var mask = Integer(masks[bone], maskKey);
            Require(mask is 0 or 1 && rows[bone] is JsonArray row && row.Count == width, "标准骨骼姿态的尺寸或掩码无效。");
            var values = (JsonArray)rows[bone]!; double norm = 0;
            foreach (var component in values)
            {
                var value = Number(component, valuesKey);
                Require(double.IsFinite(value) && Math.Abs(value) <= float.MaxValue, "标准骨骼姿态包含无效数值。");
                norm += value * value;
            }
            Require(width != 4 || mask == 0 || Math.Abs(norm - 1) <= 1e-3, "标准骨骼姿态包含非单位旋转四元数。");
        }
    }

    private static JsonObject ReadProfile(string path, string expectedHash)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        Require(stream.Length > 0 && stream.Length <= MaximumProfileBytes, "角色适配文件大小超出支持范围。");
        var bytes = new byte[(int)stream.Length]; stream.ReadExactly(bytes);
        Require(Convert.ToHexStringLower(SHA256.HashData(bytes)).Equals(expectedHash, StringComparison.OrdinalIgnoreCase),
            "准备目录中的角色适配文件已更改。");
        try { return JsonNode.Parse(bytes)?.AsObject() ?? throw new InvalidDataException("角色适配文件为空。"); }
        catch (Exception error) when (error is JsonException or InvalidOperationException)
        { throw new InvalidDataException("角色适配文件不是有效的 JSON 对象。", error); }
    }

    private static string FilePath(string root, JsonObject config, string key, string filename)
    {
        Require(config[key] is null || Text(config[key], key) == filename, "准备配置中的文件名无效：" + key);
        var path = Path.Combine(root, filename);
        Require(File.Exists(path), "准备目录缺少文件：" + filename); return path;
    }
    private static string Digest(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }
    private static JsonArray Array(JsonObject config, string key, uint count)
    {
        if (config[key] is not JsonArray values || values.Count != count) throw new InvalidDataException("准备配置的数组长度无效：" + key);
        return values;
    }
    private static string Text(JsonNode? node, string key)
    {
        if (node is JsonValue value && value.TryGetValue<string>(out var text) && !string.IsNullOrEmpty(text)) return text;
        throw new InvalidDataException("准备配置缺少有效文本字段：" + key);
    }
    private static string HashField(JsonObject config, string key)
    {
        var value = Text(config[key], key); Require(IsHash(value), "准备配置中的 SHA-256 无效：" + key); return value;
    }
    private static bool IsHash(string value) => value.Length == 64 && value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F');
    private static double Number(JsonNode? node, string key)
    {
        if (node is JsonValue value)
        {
            if (value.TryGetValue<double>(out var d)) return d;
            if (value.TryGetValue<float>(out var f)) return f;
            if (value.TryGetValue<int>(out var i)) return i;
            if (value.TryGetValue<uint>(out var u)) return u;
            if (value.TryGetValue<long>(out var l)) return l;
            if (value.TryGetValue<ulong>(out var ul)) return ul;
        }
        throw new InvalidDataException("准备配置缺少有效数值字段：" + key);
    }
    private static long Integer(JsonNode? node, string key)
    {
        var number = Number(node, key);
        Require(double.IsFinite(number) && number >= int.MinValue && number <= uint.MaxValue && Math.Truncate(number) == number,
            "准备配置包含无效整数：" + key); return (long)number;
    }
    private static void Require(bool valid, string message)
    {
        if (!valid) throw new InvalidDataException(message);
    }
}
