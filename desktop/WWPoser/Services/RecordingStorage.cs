using System.Globalization;

namespace WWPoser.Services;

/// <summary>Verify a remux before publishing it or discarding generated recovery media.</summary>
internal static class RecordingStorage
{
    internal static async Task<JsonObject> VerifyAsync(string ffprobe, string source, string output, double duration)
    {
        async Task<JsonObject> Inspect(string path)
        {
            var probe = await MediaJson.RunAsync(ffprobe, ["-v", "error", "-count_packets", "-show_entries",
                "stream=codec_type,codec_name,width,height,nb_read_packets,duration:format=duration", "-of", "json", path]);
            if (probe.ExitCode != 0 || !string.IsNullOrWhiteSpace(probe.Error)) throw new IOException("视频完整性校验失败，原始文件已保留：" + probe.Error);
            return JsonNode.Parse(probe.Output)?.AsObject() ?? throw new IOException("视频校验未返回结果");
        }
        var original = await Inspect(source);
        var saved = await Inspect(output);
        static JsonObject Stream(JsonObject root, string kind) => root["streams"]!.AsArray().OfType<JsonObject>().SingleOrDefault(item => Contract.S(item, "codec_type") == kind)
            ?? throw new IOException("保存视频缺少" + (kind == "video" ? "画面" : "音频") + "流，原始文件已保留");
        static long Packets(JsonObject stream) => long.TryParse(Contract.S(stream, "nb_read_packets"), NumberStyles.Integer, CultureInfo.InvariantCulture, out long count) ? count : 0;
        var before = Stream(original, "video"); var video = Stream(saved, "video"); var audio = Stream(saved, "audio");
        long packets = Packets(video);
        double actualDuration = double.TryParse(Contract.S(saved["format"]!.AsObject(), "duration"), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) ? parsed : 0;
        double audioDuration = double.TryParse(Contract.S(audio, "duration"), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedAudio) ? parsedAudio : 0;
        // Video is copied, so packet count, codec and dimensions must survive
        // intact. Scan every packet, including the tail, before deleting MKV.
        if (packets <= 0 || packets != Packets(before) || Contract.S(video, "codec_name") != Contract.S(before, "codec_name") ||
            Contract.I(video, "width") != Contract.I(before, "width") || Contract.I(video, "height") != Contract.I(before, "height") ||
            Packets(audio) <= 0 || Contract.S(audio, "codec_name") != "aac" || !double.IsFinite(actualDuration) ||
            !double.IsFinite(audioDuration) || audioDuration <= 0 || Math.Abs(audioDuration - duration) > .1 ||
            !double.IsFinite(duration) || duration <= 0 || Math.Abs(actualDuration - duration) > .25)
            throw new IOException("保存视频的帧包、音轨或时长校验不一致，原始文件已保留");
        return new JsonObject { ["method"] = "full_packet_scan", ["video_packets"] = packets, ["audio_packets"] = Packets(audio), ["duration"] = actualDuration, ["audio_duration"] = audioDuration };
    }

    internal static JsonObject CleanupGeneratedMedia(string work)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(work));
        var errors = new JsonArray(); long released = 0; int deleted = 0;
        var result = new JsonObject { ["errors"] = errors };
        bool Safe(string path)
        {
            // Never follow a junction/symlink outside this run's private folder.
            var full = Path.GetFullPath(path);
            if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return false;
            for (string? current = full; current is not null; current = Path.GetDirectoryName(current))
            {
                if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) return false;
                if (string.Equals(current, root, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
        void Delete(string path)
        {
            try
            {
                if (!Safe(path)) { errors.Add("未清理非本次普通临时文件：" + path); return; }
                if (!File.Exists(path)) return;
                long bytes = new FileInfo(path).Length; File.Delete(path); released += bytes; deleted++;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { errors.Add(error.Message); }
        }
        foreach (string name in new[] { "video.mkv", "video-nvenc-compatible.mkv", "video-software.mkv", "video-hevc.mkv", "video-hevc-compatible.mkv", "music.wav" })
            Delete(Path.Combine(root, name));
        foreach (string name in new[] { "sources", "decoded" })
        {
            string directory = Path.Combine(root, "music", name);
            try
            {
                if (!Safe(directory)) { errors.Add("未清理链接临时目录：" + directory); continue; }
                if (!Directory.Exists(directory)) continue;
                // These are flat generated caches. Leave unknown subdirectories
                // and all journals/logs in place; no recursive deletion is used.
                foreach (string file in Directory.EnumerateFiles(directory)) Delete(file);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { errors.Add(error.Message); }
        }
        result["released_bytes"] = released; result["deleted_files"] = deleted; result["media_retained"] = errors.Count > 0;
        return result;
    }
}
