using System.Buffers.Binary;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using WWPoser.Services;

static class CompiledMotionValidationChecks
{
    private sealed class Fixture : IDisposable
    {
        public string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), "wwposer-compiled-validation-" + Guid.NewGuid().ToString("N"));
        public string MotionPath => Path.Combine(DirectoryPath, "motion.wwlocal");
        public JsonObject Config { get; private set; } = new();
        public Fixture() { Directory.CreateDirectory(DirectoryPath); Reset(); }
        public void Reset()
        {
            File.WriteAllText(Path.Combine(DirectoryPath, "source.vmd"), "synthetic source identity");
            File.WriteAllText(Path.Combine(DirectoryPath, "character_skeleton.json"), "{}");
            var sourceHash = Digest(Path.Combine(DirectoryPath, "source.vmd"));
            var skeletonHash = Digest(Path.Combine(DirectoryPath, "character_skeleton.json"));
            var profile = new JsonObject
            {
                ["profile_id"] = "synthetic-profile", ["bone_count"] = 2,
                ["bone_names"] = new JsonArray("root", "child"), ["bone_parents"] = new JsonArray(-1, 0), ["skeleton_sha256"] = skeletonHash
            };
            File.WriteAllText(Path.Combine(DirectoryPath, "character_profile.json"), profile.ToJsonString());
            using (var stream = File.Create(MotionPath))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write("WWLOCAL1"u8); writer.Write(1u); writer.Write(2u); writer.Write(2u); writer.Write(60u); writer.Write(1d / 60);
                writer.Write(Encoding.ASCII.GetBytes(sourceHash)); writer.Write(Encoding.ASCII.GetBytes(skeletonHash));
                for (var frame = 0; frame < 2; frame++)
                    for (var bone = 0; bone < 2; bone++)
                    {
                        writer.Write(bone == 0 ? (float)frame * 10 : 0); writer.Write(0f); writer.Write(0f);
                        writer.Write(0f); writer.Write(0f); writer.Write(0f); writer.Write(1f); writer.Write(bone == 0 ? 3u : 1u);
                    }
            }
            Config = new JsonObject
            {
                ["profile_id"] = "synthetic-profile", ["bone_count"] = 2, ["frame_count"] = 2, ["fps"] = 60, ["clip_duration_seconds"] = 1d / 60,
                ["local_clip_file"] = "motion.wwlocal", ["character_profile_file"] = "character_profile.json",
                ["character_skeleton_file"] = "character_skeleton.json", ["source_file"] = "source.vmd",
                ["local_clip_sha256"] = Digest(MotionPath), ["character_profile_sha256"] = Digest(Path.Combine(DirectoryPath, "character_profile.json")),
                ["bone_names"] = new JsonArray("root", "child"), ["bone_parents"] = new JsonArray(-1, 0),
                ["canonical_position_mask"] = new JsonArray(0, 1), ["canonical_rotation_mask"] = new JsonArray(0, 1),
                ["canonical_local_positions"] = new JsonArray(new JsonArray(0, 0, 0), new JsonArray(0, 1, 0)),
                ["canonical_local_rotations"] = new JsonArray(new JsonArray(0, 0, 0, 1), new JsonArray(0, 0, 0, 1))
            };
        }
        public void Motion(Action<byte[]> mutate, bool updateHash = true)
        {
            var bytes = File.ReadAllBytes(MotionPath); mutate(bytes); WriteMotion(bytes, updateHash);
        }
        public void WriteMotion(byte[] bytes, bool updateHash = true)
        {
            File.WriteAllBytes(MotionPath, bytes); if (updateHash) Config["local_clip_sha256"] = Digest(MotionPath);
        }
        public void Dispose() => Directory.Delete(DirectoryPath, true);
    }

    public static int Run()
    {
        var assertions = 0; using var fixture = new Fixture();
        void Accept(string label)
        {
            try { CompiledMotionValidation.Validate(fixture.DirectoryPath, fixture.Config); assertions++; }
            catch (Exception error) { throw new Exception(label, error); }
        }
        void Reject(Action<Fixture> mutate, string label, string? expectedMessage = null)
        {
            fixture.Reset(); mutate(fixture);
            try { CompiledMotionValidation.Validate(fixture.DirectoryPath, fixture.Config); }
            catch (InvalidDataException error)
            {
                if (expectedMessage is not null && !error.Message.Contains(expectedMessage, StringComparison.Ordinal)) throw new Exception(label + ": unexpected failure", error);
                assertions++; return;
            }
            throw new Exception(label + ": invalid compiled input was accepted");
        }
        Accept("normal compiler-shaped clip and immutable inputs remain valid");
        Reject(f => f.Motion(bytes => BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(160), 2), false), "changed finite payload cannot reuse its old hash", "已更改或损坏");
        Reject(f => File.AppendAllText(Path.Combine(f.DirectoryPath, "character_profile.json"), " "), "changed profile bytes cannot reuse its old hash", "角色适配文件已更改");
        Reject(f => File.AppendAllText(Path.Combine(f.DirectoryPath, "source.vmd"), "changed"), "source snapshot is bound by clip provenance");
        Reject(f => File.AppendAllText(Path.Combine(f.DirectoryPath, "character_skeleton.json"), " "), "skeleton snapshot is bound by the profile");
        Reject(f => File.Delete(Path.Combine(f.DirectoryPath, "character_skeleton.json")), "missing skeleton fails before game launch");
        Reject(f => f.Config["local_clip_file"] = "../other.wwlocal", "prepared file paths cannot escape the actor directory");
        Reject(f => f.Config["local_clip_sha256"] = "short", "malformed config hash rejected");
        Reject(f => f.Motion(bytes => bytes[0] = (byte)'X'), "bad magic rejected even with a matching file hash");
        Reject(f => f.Motion(bytes => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), 2)), "future clip version rejected");
        foreach (var offset in new[] { 12, 16, 20 })
            Reject(f => f.Motion(bytes => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), 0)), "zero dimensions rejected");
        Reject(f => f.Motion(bytes => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12), 1025)), "unsupported bone capacity rejected");
        Reject(f => f.Motion(bytes => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16), 100001)), "unsupported frame capacity rejected");
        Reject(f => f.Motion(bytes => { BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12), uint.MaxValue); BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16), uint.MaxValue); }), "overflow dimensions rejected without allocation");
        Reject(f => f.Motion(bytes => { BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12), 1024); BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16), 100000); BinaryPrimitives.WriteDoubleLittleEndian(bytes.AsSpan(24), 99999d / 60); }), "oversized payload rejected without allocation");
        foreach (var duration in new[] { -1d, 2d, double.NaN, double.PositiveInfinity })
            Reject(f => f.Motion(bytes => BinaryPrimitives.WriteDoubleLittleEndian(bytes.AsSpan(24), duration)), "invalid duration rejected");
        Reject(f => f.WriteMotion(File.ReadAllBytes(f.MotionPath)[..159]), "truncated header rejected");
        Reject(f => f.WriteMotion(File.ReadAllBytes(f.MotionPath)[..^1]), "truncated payload rejected");
        Reject(f => f.WriteMotion([.. File.ReadAllBytes(f.MotionPath), 0]), "trailing data rejected");
        Reject(f => f.Motion(bytes => bytes[32] = (byte)'g'), "non-hex provenance rejected");
        Reject(f => f.Motion(bytes => bytes.AsSpan(32, 64).Clear()), "one omitted provenance digest rejected");
        Reject(f => f.Motion(bytes => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(188), 4)), "unknown command mask rejected");
        Reject(f => f.Motion(bytes => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(220), 3)), "translation on non-root bone rejected");
        Reject(f => f.Motion(bytes => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(252), 1)), "changing masks rejected");
        Reject(f => f.Motion(bytes => BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(160), float.NaN)), "NaN payload rejected");
        Reject(f => f.Motion(bytes => BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(184), 0)), "zero active quaternion rejected");
        Reject(f => f.Config["bone_count"] = 3, "config bone count must match clip");
        Reject(f => f.Config["frame_count"] = 3, "config frame count must match clip");
        Reject(f => f.Config["fps"] = 30, "config fps must match clip");
        Reject(f => f.Config["clip_duration_seconds"] = 1, "config duration must match clip");
        Reject(f => f.Config["bone_names"]![1] = "wrong", "config names must match the hashed profile");
        Reject(f => f.Config["bone_parents"]![1] = 1, "invalid topology rejected");
        Reject(f => f.Config["bone_parents"] = new JsonArray(-1), "truncated hierarchy rejected");
        Reject(f => f.Config["profile_id"] = "wrong", "config profile identity must match hashed profile");
        Reject(f => f.Config["canonical_position_mask"] = new JsonArray(0), "truncated canonical masks rejected");
        Reject(f => f.Config["canonical_rotation_mask"]![1] = 2, "unknown canonical mask rejected");
        Reject(f => f.Config["canonical_local_positions"]![1] = new JsonArray(0, 1), "truncated canonical vectors rejected");
        Reject(f => f.Config["canonical_local_positions"]![1]![0] = 1e300, "canonical values must fit finite float fields");
        Reject(f => f.Config["canonical_local_rotations"]![1]![3] = 0, "active canonical quaternion must be unit length");
        fixture.Reset(); fixture.Motion(bytes => bytes.AsSpan(32, 128).Clear()); Accept("explicitly omitted provenance remains compatible with whole-file hash validation");
        fixture.Reset();
        fixture.Motion(bytes => { foreach (var offset in new[] { 192, 256 }) { BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset + 28), 0); BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(offset + 24), 0); } });
        Accept("unused zero quaternion remains compatible");
        fixture.Reset();
        var still = File.ReadAllBytes(fixture.MotionPath)[..224]; BinaryPrimitives.WriteUInt32LittleEndian(still.AsSpan(16), 1); BinaryPrimitives.WriteDoubleLittleEndian(still.AsSpan(24), 0);
        fixture.WriteMotion(still); fixture.Config["frame_count"] = 1; fixture.Config["clip_duration_seconds"] = 0;
        Accept("single-frame zero-duration pose remains compatible");
        Console.WriteLine($"PASS: {assertions} compiled motion validation checks; no game or GUI execution.");
        return assertions;
    }
    private static string Digest(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
}
