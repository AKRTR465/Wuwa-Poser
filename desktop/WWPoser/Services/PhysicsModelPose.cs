using System.Buffers.Binary;
using System.Numerics;

namespace WWPoser.Services;

internal sealed record PhysicsModelPose(ulong Frame, Matrix4x4[] Transforms)
{
    public static PhysicsModelPose? Read(string session, uint slot, ulong revision, int count, ulong previous)
    {
        if (count is <= 0 or > 1024) return null;
        try
        {
            using var stream = new FileStream(Path.Combine(session, "model_pose.dat"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            Span<byte> header = stackalloc byte[32];
            if (stream.Length != header.Length + count * 40) return null;
            stream.ReadExactly(header);
            if (!header[..8].SequenceEqual("WWPPOS1\0"u8) || BinaryPrimitives.ReadUInt32LittleEndian(header[8..]) != slot ||
                BinaryPrimitives.ReadInt32LittleEndian(header[12..]) != count || BinaryPrimitives.ReadUInt64LittleEndian(header[16..]) != revision) return null;
            var frame = BinaryPrimitives.ReadUInt64LittleEndian(header[24..]);
            if (frame == 0 || frame == previous) return null;
            var transforms = new Matrix4x4[count];
            Span<byte> row = stackalloc byte[40];
            Span<float> values = stackalloc float[10];
            for (var i = 0; i < count; i++)
            {
                stream.ReadExactly(row);
                for (var k = 0; k < values.Length; k++)
                {
                    values[k] = BinaryPrimitives.ReadSingleLittleEndian(row[(k * 4)..]);
                    if (!float.IsFinite(values[k])) return null;
                }
                var rotation = new Quaternion(values[3], values[5], values[4], -values[6]);
                if (MathF.Abs(rotation.LengthSquared() - 1) > .01f) return null;
                // The mesh exporter swaps UE Y/Z, reflects quaternion W, and converts cm to m.
                transforms[i] = Matrix4x4.CreateScale(values[7], values[9], values[8]) *
                    Matrix4x4.CreateFromQuaternion(Quaternion.Normalize(rotation)) *
                    Matrix4x4.CreateTranslation(values[0] * .01f, values[2] * .01f, values[1] * .01f);
            }
            return new(frame, transforms);
        }
        catch (IOException) { return null; }
        catch (UnauthorizedAccessException) { return null; }
    }
}
