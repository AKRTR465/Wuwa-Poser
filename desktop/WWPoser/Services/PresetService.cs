using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Encodings.Web;

namespace WWPoser.Services;

public sealed record PresetRecordingSettings(bool Automatic = false, int Fps = 30, int Height = 1080, RecordingFormat Format = RecordingFormat.Compact);
public sealed record ShowPreset(string Name, PlaybackSessionRequest Playback, MusicPreset? Music = null,
    PresetRecordingSettings? Recording = null, string[]? ActorNames = null);
public sealed record PresetLoadResult(ShowPreset Preset, IReadOnlyList<string> Warnings);

// Authoring data only: no process identities, compiled clips or live session paths.
public sealed class PresetService(string? cacheDirectory = null)
{
    private const long MaxAsset = 8L * 1024 * 1024 * 1024, MaxTotal = 32L * 1024 * 1024 * 1024;
    private const int MaxManifest = 16 * 1024 * 1024;
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    private readonly string cache = Path.GetFullPath(cacheDirectory ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WuwaPoser", "preset_cache"));
    private sealed record Asset(string Entry, string Sha256, long Bytes);
    private sealed record Document(string Format, int Schema, string AppVersion, DateTimeOffset SavedAt, ShowPreset Preset, Asset[] Assets);

    public async Task SaveAsync(string path, ShowPreset preset, CancellationToken token = default)
    {
        Validate(preset);
        path = Path.GetFullPath(path);
        if (Sources(preset).Any(source => string.Equals(Path.GetFullPath(source), path, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("预设不能覆盖它引用的素材文件。");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".partial";
        try
        {
            var assets = new Dictionary<string, Asset>(StringComparer.Ordinal);
            var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            long total = 0;
            using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 131072, true))
            {
                using var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true);
                foreach (var source in Sources(preset).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    token.ThrowIfCancellationRequested();
                    if (!File.Exists(source)) throw new FileNotFoundException("预设素材不存在，请重新选择或清空该项：" + source, source);
                    await using var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 131072, true);
                    if (input.Length > MaxAsset) throw new InvalidDataException("单个预设素材超过 8 GB。");
                    var hash = Convert.ToHexStringLower(await SHA256.HashDataAsync(input, token));
                    if (!assets.TryGetValue(hash, out var asset))
                    {
                        total += input.Length;
                        if (total > MaxTotal) throw new InvalidDataException("预设素材总量超过 32 GB。");
                        asset = new("assets/" + hash + "/" + SafeName(Path.GetFileName(source)), hash, input.Length);
                        input.Position = 0;
                        await using var entry = zip.CreateEntry(asset.Entry, CompressionLevel.Fastest).Open();
                        await input.CopyToAsync(entry, token);
                        assets.Add(hash, asset);
                    }
                    paths.Add(source, asset.Entry);
                }
                var document = new Document("WWPRESET", 1, typeof(PresetService).Assembly.GetName().Version?.ToString() ?? "", DateTimeOffset.UtcNow,
                    Map(preset, source => paths[source]), assets.Values.ToArray());
                var bytes = JsonSerializer.SerializeToUtf8Bytes(document, Json);
                if (bytes.Length > MaxManifest) throw new InvalidDataException("预设配置过大。");
                await using var manifest = zip.CreateEntry("preset.json").Open();
                await manifest.WriteAsync(bytes, token);
            }
            token.ThrowIfCancellationRequested();
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public async Task<PresetLoadResult> LoadAsync(string path, CancellationToken token = default)
    {
        await using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 131072, true);
        var packageHash = Convert.ToHexStringLower(await SHA256.HashDataAsync(input, token)); input.Position = 0;
        using var zip = new ZipArchive(input, ZipArchiveMode.Read);
        if (zip.Entries.Count > 30001 || zip.Entries.Select(e => e.FullName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != zip.Entries.Count)
            throw new InvalidDataException("预设包含重复条目或条目过多。");
        var manifest = zip.GetEntry("preset.json") ?? throw new InvalidDataException("缺少 preset.json。");
        if (manifest.Length > MaxManifest) throw new InvalidDataException("预设配置过大。");
        Document document;
        using (var data = manifest.Open())
            document = await JsonSerializer.DeserializeAsync<Document>(data, Json, token) ?? throw new InvalidDataException("预设配置为空。");
        if (document.Format != "WWPRESET" || document.Schema != 1) throw new InvalidDataException("此预设格式或版本暂不支持，请更新软件。");
        Validate(document.Preset);
        if (document.Assets is null || document.Assets.Length > 30000) throw new InvalidDataException("素材清单无效。");
        var assets = new Dictionary<string, Asset>(StringComparer.Ordinal);
        long total = 0;
        foreach (var asset in document.Assets)
        {
            if (asset is null || asset.Sha256 is null || asset.Sha256.Length != 64 || asset.Sha256.Any(c => !char.IsAsciiHexDigitLower(c)) ||
                asset.Bytes < 0 || asset.Bytes > MaxAsset || asset.Entry is null)
                throw new InvalidDataException("素材清单无效。");
            var prefix = "assets/" + asset.Sha256 + "/";
            if (!asset.Entry.StartsWith(prefix, StringComparison.Ordinal) || asset.Entry.Length <= prefix.Length ||
                asset.Entry[prefix.Length..] != SafeName(asset.Entry[prefix.Length..]) || !assets.TryAdd(asset.Entry, asset))
                throw new InvalidDataException("素材路径无效或重复。");
            total += asset.Bytes;
            if (total > MaxTotal) throw new InvalidDataException("预设素材总量超过 32 GB。");
        }
        var warnings = new List<string>(); var resolved = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var reference in Sources(document.Preset).Distinct(StringComparer.Ordinal))
        {
            token.ThrowIfCancellationRequested();
            if (!assets.TryGetValue(reference, out var asset)) throw new InvalidDataException("预设引用了清单外的素材：" + reference);
            var entry = zip.GetEntry(reference);
            if (entry is null) { resolved.Add(reference, ""); warnings.Add("缺少素材：" + Path.GetFileName(reference)); continue; }
            if (entry.Length != asset.Bytes) throw new InvalidDataException("素材大小校验失败：" + reference);
            // Only canonical manifest paths are extracted; the ZIP never selects a filesystem target.
            var destination = Path.Combine(cache, packageHash, reference.Replace('/', Path.DirectorySeparatorChar));
            if (!await MatchesAsync(destination, asset, token))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".partial";
                try
                {
                    await using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 131072, true))
                    using (var source = entry.Open()) await source.CopyToAsync(output, token);
                    if (!await MatchesAsync(temporary, asset, token)) throw new InvalidDataException("素材内容校验失败：" + reference);
                    File.Move(temporary, destination, true);
                }
                finally { if (File.Exists(temporary)) File.Delete(temporary); }
            }
            resolved.Add(reference, destination);
        }
        var preset = Map(document.Preset, source => resolved[source]);
        for (int i = 0; i < preset.Playback.Actors.Count; i++)
            if (preset.Playback.Actors[i].Enabled && string.IsNullOrEmpty(preset.Playback.Actors[i].BodySource)) warnings.Add($"演员 {i + 1} 尚未选择身体动作。");
        if (preset.Playback.CameraMode == 2 && string.IsNullOrEmpty(preset.Playback.CameraSource)) warnings.Add("VMD 镜头轨道尚未选择镜头素材。");
        return new(preset, warnings);
    }

    private static async Task<bool> MatchesAsync(string path, Asset asset, CancellationToken token)
    {
        if (!File.Exists(path)) return false;
        await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 131072, true);
        return file.Length == asset.Bytes && Convert.ToHexStringLower(await SHA256.HashDataAsync(file, token)) == asset.Sha256;
    }
    private static IEnumerable<string> Sources(ShowPreset preset) => preset.Playback.Actors.SelectMany(a => new[] { a.BodySource, a.FaceSource, a.GazeSource })
        .Concat([preset.Playback.CameraSource, preset.Music?.AudioFile]).Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!);
    private static ShowPreset Map(ShowPreset preset, Func<string, string> map)
    {
        string? AssetPath(string? path) => string.IsNullOrWhiteSpace(path) ? null : map(path);
        return preset with { Playback = preset.Playback with {
            Actors = preset.Playback.Actors.Select((a, i) => a with { BodySource = AssetPath(a.BodySource) ?? "", FaceSource = AssetPath(a.FaceSource), GazeSource = AssetPath(a.GazeSource), Slot = (uint)i + 1 }).ToArray(),
            CameraSource = AssetPath(preset.Playback.CameraSource) },
            Music = preset.Music is { } music ? music with { AudioFile = AssetPath(music.AudioFile) ?? "" } : null };
    }
    private static string SafeName(string name)
    {
        var result = new string(name.Select(c => c < 32 || "<>:\"/\\|?*".Contains(c) ? '_' : c).ToArray()).TrimEnd('.', ' ');
        if (result.Length > 120) result = result[..90] + Path.GetExtension(result)[..Math.Min(20, Path.GetExtension(result).Length)];
        if (string.IsNullOrEmpty(result) || new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" }.Contains(result.Split('.')[0], StringComparer.OrdinalIgnoreCase)) result = "_" + result;
        return result;
    }
    public static void Validate(ShowPreset preset)
    {
        static void Range(double value, double min, double max) { if (!double.IsFinite(value) || value < min || value > max) throw new InvalidDataException("预设包含超出范围的数值。"); }
        if (preset is null || preset.Playback is not { } p || p.Actors is null || p.Actors.Count > 10000 || preset.Name is null ||
            (preset.ActorNames is { } names && names.Length != p.Actors.Count)) throw new InvalidDataException("预设演员配置无效。");
        p.Validate();
        if (preset.Music is { } music) { Range(music.OffsetSeconds, -3600, 3600); Range(music.Volume, 0, 1); }
        if (preset.Recording is { } recording && (recording.Fps is not (30 or 60) || recording.Height is not (0 or 1080 or 1440) || !Enum.IsDefined(recording.Format))) throw new InvalidDataException("预设录屏参数无效。");
    }
}
