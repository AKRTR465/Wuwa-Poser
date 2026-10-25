using System.Security.Cryptography;
using System.Text;

namespace WWPoser.Services;

/// <summary>Decode local music once; reuse PCM bytes independently of Windows codec associations.</summary>
internal sealed class AudioDecodeService
{
    private readonly string ffmpeg, cacheDirectory;
    internal AudioDecodeService(string repositoryRoot, string? cacheDirectory = null)
    {
        ffmpeg = Path.Combine(Path.GetFullPath(repositoryRoot), "tools", "recording_bin", "ffmpeg.exe");
        this.cacheDirectory = cacheDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WuwaPoser", "audio_cache", "pcm48k-v1");
    }

    internal async Task<string> PrepareAsync(string source, CancellationToken cancellation = default)
    {
        source = Path.GetFullPath(source);
        // Hold a read lease through decoding so the key and decoded source agree.
        await using var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.Asynchronous | FileOptions.SequentialScan);
        var hash = Convert.ToHexStringLower(await SHA256.HashDataAsync(input, cancellation).ConfigureAwait(false));
        cancellation.ThrowIfCancellationRequested();
        Directory.CreateDirectory(cacheDirectory);
        var target = Path.Combine(cacheDirectory, hash + ".wav");
        if (IsPcmWave(target)) return target;
        if (!File.Exists(ffmpeg)) throw new FileNotFoundException("缺少音频解码工具，请完整解压播放器后重试。", ffmpeg);
        var temporary = Path.Combine(cacheDirectory, hash + "." + Guid.NewGuid().ToString("N") + ".partial.wav");
        try
        {
            var result = await MediaJson.RunAsync(ffmpeg, new[] { "-nostdin", "-hide_banner", "-loglevel", "error", "-n", "-i", source,
                "-map", "0:a:0", "-vn", "-sn", "-dn", "-map_metadata", "-1", "-ac", "2", "-ar", "48000", "-c:a", "pcm_s16le", "-rf64", "never", temporary }, cancellation).ConfigureAwait(false);
            cancellation.ThrowIfCancellationRequested();
            if (result.ExitCode != 0 || !IsPcmWave(temporary))
                throw new InvalidDataException("配乐解码失败，请检查 FLAC、WAV 或 MP3 文件是否完整。" +
                    (string.IsNullOrWhiteSpace(result.Error) ? "" : "\n" + result.Error.Trim()[..Math.Min(result.Error.Trim().Length, 1200)]));
            try { File.Move(temporary, target, true); }
            // Another player may have completed the same content-addressed entry.
            catch (IOException) when (IsPcmWave(target)) { }
            return target;
        }
        finally
        {
            try { File.Delete(temporary); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
        }
    }

    internal static bool IsPcmWave(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(stream, Encoding.ASCII);
            if (stream.Length < 44 || Encoding.ASCII.GetString(reader.ReadBytes(4)) != "RIFF") return false;
            if ((long)reader.ReadUInt32() + 8 != stream.Length || Encoding.ASCII.GetString(reader.ReadBytes(4)) != "WAVE") return false;
            bool format = false, data = false;
            while (stream.Position + 8 <= stream.Length)
            {
                var id = Encoding.ASCII.GetString(reader.ReadBytes(4)); var size = reader.ReadUInt32();
                long next = stream.Position + size + (size & 1);
                if (next > stream.Length) return false;
                if (id == "fmt ")
                {
                    if (size < 16 || reader.ReadUInt16() != 1 || reader.ReadUInt16() != 2 || reader.ReadUInt32() != 48000 ||
                        reader.ReadUInt32() != 192000 || reader.ReadUInt16() != 4 || reader.ReadUInt16() != 16) return false;
                    format = true;
                }
                if (id == "data") { if (size == 0 || size % 4 != 0) return false; data = true; }
                stream.Position = next;
            }
            return format && data && stream.Position == stream.Length;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return false; }
    }
}
