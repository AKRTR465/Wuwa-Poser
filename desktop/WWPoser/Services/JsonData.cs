using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

namespace WWPoser.Services;

public static class JsonData
{
    public static readonly JsonSerializerOptions Compact = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    public static readonly JsonSerializerOptions Pretty = new(Compact) { WriteIndented = true };

    public static JsonObject ReadObject(string path) => JsonNode.Parse(File.ReadAllText(path))!.AsObject();

    public static void Write(string path, JsonNode value) => AtomicWrite(path, Encoding.UTF8.GetBytes(value.ToJsonString(Pretty) + "\n"));

    // Native readers keep the destination open briefly; replacement retries through those sharing violations.
    public static void AtomicWrite(string path, ReadOnlySpan<byte> bytes)
    {
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        File.WriteAllBytes(temporary, bytes.ToArray());
        var deadline = Environment.TickCount64 + 1500;
        while (true)
        {
            try { File.Move(temporary, path, true); return; }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException && Environment.TickCount64 < deadline) { Thread.Sleep(25); }
        }
    }

    public static string Digest(string path)
    {
        using var file = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(file));
    }
    public static string CanonicalDigest(JsonNode value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value.ToJsonString(Compact))));
}
