using System.IO;
using WWPoser.Services;

internal static class RecordingStorageChecks
{
    // No encoder, capture API, game process, or external user files are used.
    internal static int Run(string fixture)
    {
        int checks = 0;
        void Check(bool good, string message) { if (!good) throw new Exception(message); checks++; }
        string work = Path.Combine(fixture, "recording-storage");
        string sources = Path.Combine(work, "music", "sources"), decoded = Path.Combine(work, "music", "decoded");
        Directory.CreateDirectory(sources); Directory.CreateDirectory(decoded);
        var generated = new[] { "video.mkv", "video-nvenc-compatible.mkv", "video-software.mkv", "video-hevc.mkv", "video-hevc-compatible.mkv", "music.wav",
            Path.Combine("music", "sources", "cached.flac"), Path.Combine("music", "decoded", "cached.f32") }.Select(name => Path.Combine(work, name)).ToArray();
        foreach (string path in generated) File.WriteAllBytes(path, [1, 2, 3, 4]);
        string nested = Path.Combine(decoded, "unknown-subdirectory", "keep.f32"); Directory.CreateDirectory(Path.GetDirectoryName(nested)!);
        var retained = new[] { "capture.log", "capture_attempts.json", "result.json", "error.txt", "final.mp4", "unrelated.wav", Path.Combine("music", "audio_journal.jsonl") }
            .Select(name => Path.Combine(work, name)).Append(nested).ToArray();
        foreach (string path in retained) File.WriteAllText(path, "retained evidence");
        string sibling = Path.Combine(fixture, "recording-storage-other-run"); Directory.CreateDirectory(sibling);
        string external = Path.Combine(sibling, "video.mkv"); File.WriteAllText(external, "other recording");
        var result = RecordingStorage.CleanupGeneratedMedia(work);
        Check(generated.All(path => !File.Exists(path)), "successful cleanup removes only this run's generated video/audio and flat source/decode caches");
        Check(retained.All(File.Exists) && File.Exists(external), "cleanup preserves logs, journals, unrelated files, nested directories and another recording");
        Check(result["deleted_files"]!.GetValue<int>() == generated.Length && result["released_bytes"]!.GetValue<long>() == generated.Length * 4 &&
            result["errors"]!.AsArray().Count == 0 && !result["media_retained"]!.GetValue<bool>(), "cleanup accurately reports released bytes and completed generated-file deletions");
        var repeated = RecordingStorage.CleanupGeneratedMedia(work);
        Check(repeated["deleted_files"]!.GetValue<int>() == 0 && repeated["released_bytes"]!.GetValue<long>() == 0 && retained.All(File.Exists),
            "repeating cleanup cannot delete unknown files or claim more released space");

        // Symlink creation may require Windows Developer Mode. If unavailable,
        // the ordinary cleanup checks still run on every platform without UAC.
        string linkedWork = Path.Combine(fixture, "recording-storage-links"); Directory.CreateDirectory(Path.Combine(linkedWork, "music"));
        try
        {
            Directory.CreateSymbolicLink(Path.Combine(linkedWork, "music", "sources"), sibling);
            File.CreateSymbolicLink(Path.Combine(linkedWork, "video.mkv"), external);
            var linked = RecordingStorage.CleanupGeneratedMedia(linkedWork);
            Check(File.Exists(external) && File.ReadAllText(external) == "other recording" && File.Exists(Path.Combine(linkedWork, "video.mkv")) &&
                Directory.Exists(Path.Combine(linkedWork, "music", "sources")), "cleanup never follows a linked video or source-cache directory to external files");
            Check(linked["deleted_files"]!.GetValue<int>() == 0 && linked["media_retained"]!.GetValue<bool>() && linked["errors"]!.AsArray().Count >= 2,
                "refused symlink cleanup reports retained media and diagnostic reasons");
            string rootLink = Path.Combine(fixture, "recording-storage-root-link"); Directory.CreateSymbolicLink(rootLink, sibling);
            var linkedRoot = RecordingStorage.CleanupGeneratedMedia(rootLink);
            Check(File.Exists(external) && linkedRoot["deleted_files"]!.GetValue<int>() == 0 && linkedRoot["media_retained"]!.GetValue<bool>(),
                "a linked work root cannot authorize deletion of its target's media");
        }
        catch (Exception error) when (error is UnauthorizedAccessException or PlatformNotSupportedException or IOException)
        {
            Console.WriteLine("SKIP recording-storage symlink creation: " + error.Message);
        }
        Console.WriteLine($"PASS {checks} recording storage cleanup checks; private synthetic files only.");
        return checks;
    }
}
