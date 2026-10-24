using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace WWPoser.Services;

public sealed record WindowTarget(nint Hwnd, uint Pid, string Title, int Width, int Height);
public sealed record RecordingSnapshot(string State, string Message, double Elapsed = 0,
    string? OutputPath = null, string? WorkDirectory = null, string? StopReason = null, int? CaptureExitCode = null);

/// <summary>WGC records only the game HWND. Music is rendered from local timeline snapshots.</summary>
public sealed class RecordingService
{
    private readonly object gate = new();
    private readonly string ffmpeg, ffprobe;
    private readonly Func<string, int, int, CancellationToken, Task> capture;
    private string? session;
    private MusicPreset? preset;
    private CancellationTokenSource? stop;
    private Task? worker;
    private RecordingSnapshot snapshot = new("idle", "仅录鸣潮窗口");
    public RecordingService(string repositoryRoot) : this(repositoryRoot, null) { }
    internal RecordingService(string repositoryRoot, Func<string, int, int, CancellationToken, Task>? capture)
    {
        ffmpeg = Path.Combine(repositoryRoot, "tools", "recording_bin", "ffmpeg.exe"); ffprobe = Path.Combine(repositoryRoot, "tools", "recording_bin", "ffprobe.exe");
        this.capture = capture ?? CaptureAsync; OutputDirectory = DefaultOutputDirectory(repositoryRoot);
    }
    public string OutputDirectory { get; }
    internal static string DefaultOutputDirectory(string runtimeRoot)
    {
        var original = Path.GetFullPath(runtimeRoot);
        // Local builds live under dist/<version> or desktop/WWPoser/publish.
        // Share the workspace output across those builds without redirecting
        // a standalone portable package to a stale source_root from its manifest.
        for (var directory = new DirectoryInfo(original); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "desktop", "WWPoser", "WWPoser.csproj")) &&
                File.Exists(Path.Combine(directory.FullName, "data", "characters", "index.json")))
                return Path.Combine(directory.FullName, "playback", "recordings");
        return Path.Combine(original, "playback", "recordings");
    }
    public RecordingSnapshot Snapshot { get { lock (gate) return snapshot; } }
    public bool IsBusy { get { lock (gate) return worker is { IsCompleted: false }; } }
    public void UpdateAudioContext(string? sessionDirectory, MusicPreset? music)
    {
        lock (gate) { session = sessionDirectory; preset = music; }
    }
    public Task StartAsync(string folder, int fps = 30, int height = 1080)
    {
        lock (gate)
        {
            if (worker is { IsCompleted: false }) throw new InvalidOperationException("录屏尚未结束");
            stop?.Dispose(); stop = new CancellationTokenSource();
            snapshot = new RecordingSnapshot("starting", "正在连接鸣潮窗口");
            worker = Task.Run(() => capture(Path.GetFullPath(folder), fps, height, stop.Token));
        }
        return Task.CompletedTask;
    }
    public async Task StopAsync()
    {
        Task? pending;
        lock (gate) { stop?.Cancel(); pending = worker; }
        if (pending is not null) await pending.ConfigureAwait(false);
    }
    private void Update(RecordingSnapshot value) { lock (gate) snapshot = value; }
    private (JsonObject? State, MusicPreset? Music, bool Fresh, string Signature) AudioSample()
    {
        string? folder; MusicPreset? selected;
        lock (gate) { folder = session; selected = preset; }
        JsonObject? state = null; bool fresh = false; string stamp = "none";
        if (folder is not null)
        {
            try { (state, fresh) = MediaJson.ReadTimeline(folder, 1); stamp = state.ToJsonString(); }
            catch (Exception error) when (error is IOException or InvalidDataException or JsonException) { stamp = "unavailable:" + error.Message; }
        }
        return (state, selected, fresh, stamp + "|" + JsonSerializer.Serialize(selected) + "|" + fresh);
    }
    public static IReadOnlyList<string> BuildCaptureArguments(WindowTarget target, string videoPath, int fps = 30, int height = 1080)
    {
        int width = height == 1080 ? 1920 : -2, outputHeight = height == 1080 ? 1080 : -2;
        string filter = $"gfxcapture=hwnd={(ulong)target.Hwnd}:capture_cursor=0:capture_border=0:display_border=1:width={width}:height={outputHeight}:resize_mode=scale_aspect:max_framerate={fps},hwdownload,format=bgra,fps={fps},scale=trunc(iw/2)*2:trunc(ih/2)*2,format=yuv420p[v]";
        return new[] { "-hide_banner", "-loglevel", "warning", "-n", "-stats_period", "0.1", "-progress", "pipe:1", "-filter_complex", filter,
            "-map", "[v]", "-an", "-c:v", "libx264", "-preset", "veryfast", "-tune", "zerolatency", "-crf", "20", "-bf", "0", "-threads", "4",
            "-fps_mode", "cfr", "-f", "matroska", videoPath };
    }
    private async Task CaptureAsync(string folder, int fps, int height, CancellationToken requestStop)
    {
        Process? process = null; MusicJournal? journal = null; string? work = null;
        try
        {
            WindowTarget target = GameWindows.Find(); Directory.CreateDirectory(folder);
            string stem = "鸣潮_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + "_" + Guid.NewGuid().ToString("N")[..6];
            string output = Path.Combine(folder, stem + ".mp4"); work = Path.Combine(folder, "." + stem); Directory.CreateDirectory(work);
            Update(new RecordingSnapshot("starting", "正在连接鸣潮窗口", WorkDirectory: work));
            string silent = Path.Combine(work, "video.mkv"); journal = new MusicJournal(Path.Combine(work, "music"), ffmpeg);
            double captureStart = Seconds(); var sample = AudioSample(); journal.Observe(captureStart, sample.State, sample.Music, sample.Fresh);
            string signature = sample.Signature;
            if (requestStop.IsCancellationRequested) { Update(new RecordingSnapshot("idle", "已取消录屏", WorkDirectory: work)); return; }
            var start = new ProcessStartInfo(ffmpeg) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (string argument in BuildCaptureArguments(target, silent, fps, height)) start.ArgumentList.Add(argument);
            captureStart = Seconds(); process = Process.Start(start)!;
            using var captureLog = new StreamWriter(Path.Combine(work, "capture.log"), false, new UTF8Encoding(false));
            var packets = new ConcurrentQueue<(double Received, Dictionary<string, string> Values)>();
            var progress = ReadProgressAsync(process.StandardOutput, packets);
            var errors = CopyErrorsAsync(process.StandardError, captureLog);
            long frameCount = 0; double lastProgress = captureStart; double? stopSent = null; string? problem = null;
            var origins = new List<double>(); double elapsed = 0;
            while (true)
            {
                double now = Seconds();
                while (packets.TryDequeue(out var packet))
                {
                    if (!packet.Values.TryGetValue("frame", out string? text) || !long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long frames)) continue;
                    packet.Values.TryGetValue("out_time_us", out string? time); double.TryParse(time, NumberStyles.Float, CultureInfo.InvariantCulture, out double micros); elapsed = micros / 1e6;
                    if (frames <= frameCount) continue;
                    frameCount = frames; lastProgress = packet.Received;
                    if (packet.Values.GetValueOrDefault("progress") != "end" && elapsed > 0 && double.IsFinite(elapsed)) origins.Add(packet.Received - elapsed + 1d / fps);
                    Update(new RecordingSnapshot(stopSent is null ? "recording" : "finishing", stopSent is null ? "正在录制鸣潮" : "正在保存录屏", Math.Max(0, elapsed), WorkDirectory: work));
                }
                sample = AudioSample(); if (sample.Signature != signature) { journal.Observe(now, sample.State, sample.Music, sample.Fresh); signature = sample.Signature; }
                if (process.HasExited) break;
                if (stopSent is null)
                {
                    problem = GameWindows.Problem(target);
                    if (problem is null && ((frameCount == 0 && now - captureStart > 20) || (frameCount > 0 && now - lastProgress > 10))) problem = "未收到持续游戏画面，请使用窗口或无边框模式";
                    if (problem is not null || requestStop.IsCancellationRequested)
                    { try { await process.StandardInput.WriteLineAsync("q"); await process.StandardInput.FlushAsync(); } catch (IOException) { } stopSent = now; }
                }
                else if (now - stopSent > 15) throw new IOException("录屏编码器未正常结束，原始文件保留在 " + work);
                await Task.Delay(50);
            }
            await process.WaitForExitAsync(); await progress; await errors;
            if ((process.ExitCode != 0 || stopSent is null) && problem is null) problem = "录屏组件提前结束，已保留可用画面和日志";
            if (!File.Exists(silent) || frameCount == 0 || origins.Count == 0) throw new IOException("窗口录制未产生画面，日志保留在 " + work);
            double duration = await VideoDurationAsync(silent);
            double origin = Math.Max(captureStart, origins.Min());
            Update(new RecordingSnapshot("finishing", "正在合成 MMD 音乐并保存 MP4", duration, WorkDirectory: work));
            string journalPath = journal.Finish(duration, origin), wav = Path.Combine(work, "music.wav");
            JsonObject audio = await AudioRenderer.RenderAsync(journalPath, wav, ffmpeg, CancellationToken.None);
            var mux = await MediaJson.RunAsync(ffmpeg, new[] { "-hide_banner", "-loglevel", "error", "-n", "-i", silent, "-i", wav, "-map", "0:v:0", "-map", "1:a:0", "-c:v", "copy", "-c:a", "aac", "-b:a", "192k", "-t", duration.ToString("R", CultureInfo.InvariantCulture), "-movflags", "+faststart", output });
            if (mux.ExitCode != 0) throw new IOException("MP4 合成失败，原始文件保留在 " + work + "：" + mux.Error);
            JsonData.Write(Path.Combine(work, "result.json"), new JsonObject { ["schema"] = 1, ["target"] = new JsonObject { ["hwnd"] = (ulong)target.Hwnd, ["pid"] = target.Pid, ["title"] = target.Title, ["width"] = target.Width, ["height"] = target.Height },
                ["duration"] = duration, ["fps"] = fps, ["output"] = output, ["audio"] = audio, ["video_origin_estimate"] = origin, ["capture_process_start"] = captureStart,
                ["capture_exit_code"] = process.ExitCode, ["stop_reason"] = problem ?? "user_stop" });
            Update(new RecordingSnapshot("saved", "已保存：" + Path.GetFileName(output), duration, output, work, problem ?? "user_stop", process.ExitCode));
        }
        catch (Exception error)
        {
            Update(new RecordingSnapshot("error", error.Message, WorkDirectory: work));
            if (work is not null) File.WriteAllText(Path.Combine(work, "error.txt"), error.ToString(), new UTF8Encoding(false));
        }
        finally
        {
            journal?.Dispose();
            if (process is not null) { if (!process.HasExited) process.Kill(true); process.Dispose(); }
        }
    }
    private static double Seconds() => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
    private static async Task ReadProgressAsync(StreamReader stream, ConcurrentQueue<(double, Dictionary<string, string>)> packets)
    {
        var values = new Dictionary<string, string>(); string? line;
        while ((line = await stream.ReadLineAsync()) is not null)
        { int split = line.IndexOf('='); if (split < 0) continue; string key = line[..split]; values[key] = line[(split + 1)..]; if (key == "progress") { packets.Enqueue((Seconds(), values)); values = new Dictionary<string, string>(); } }
    }
    private static async Task CopyErrorsAsync(StreamReader reader, StreamWriter writer)
    { string? line; while ((line = await reader.ReadLineAsync()) is not null) { await writer.WriteLineAsync(line); await writer.FlushAsync(); } }
    private async Task<double> VideoDurationAsync(string path)
    {
        var result = await MediaJson.RunAsync(ffprobe, new[] { "-v", "error", "-show_entries", "format=duration", "-of", "json", path });
        return double.Parse(JsonNode.Parse(result.Output)!["format"]!["duration"]!.GetValue<string>(), CultureInfo.InvariantCulture);
    }

    public Task<JsonObject> RenderAudioAsync(string journalPath, string outputWav, CancellationToken cancellation = default) =>
        AudioRenderer.RenderAsync(Path.GetFullPath(journalPath), Path.GetFullPath(outputWav), ffmpeg, cancellation);

    private static class GameWindows
    {
        private delegate bool EnumCallback(nint hwnd, nint parameter);
        [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
        [DllImport("user32.dll", SetLastError = true)] private static extern bool EnumWindows(EnumCallback callback, nint parameter);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint hwnd, out uint pid);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowTextW(nint hwnd, StringBuilder text, int capacity);
        [DllImport("user32.dll")] private static extern bool GetClientRect(nint hwnd, out Rect rect);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint hwnd);
        [DllImport("user32.dll")] private static extern bool IsWindow(nint hwnd);
        [DllImport("user32.dll")] private static extern bool IsIconic(nint hwnd);
        internal static WindowTarget Find()
        {
            var pids = new HashSet<uint>();
            foreach (var process in Process.GetProcessesByName("Client-Win64-Shipping")) using (process) pids.Add((uint)process.Id);
            var targets = new List<WindowTarget>();
            EnumWindows((hwnd, _) => { GetWindowThreadProcessId(hwnd, out uint pid); if (!pids.Contains(pid) || !IsWindowVisible(hwnd)) return true;
                var title = new StringBuilder(512); GetWindowTextW(hwnd, title, 512); if (GetClientRect(hwnd, out Rect rect) && rect.Right - rect.Left >= 320 && rect.Bottom - rect.Top >= 240 && title.Length > 0)
                    targets.Add(new WindowTarget(hwnd, pid, title.ToString(), rect.Right - rect.Left, rect.Bottom - rect.Top)); return true; }, 0);
            if (targets.Count == 0) throw new IOException("请保留一个可见鸣潮游戏窗口");
            var selected = targets.OrderByDescending(x => (long)x.Width * x.Height).First(); string? problem = Problem(selected); if (problem is not null) throw new IOException(problem); return selected;
        }
        internal static string? Problem(WindowTarget target)
        {
            if (!IsWindow(target.Hwnd)) return "鸣潮窗口已关闭";
            return IsIconic(target.Hwnd) ? "鸣潮窗口已最小化，请使用窗口或无边框模式" : null;
        }
    }
}

/// <summary>Append-only observed music transitions; selected source bytes remain recoverable.</summary>
public sealed class MusicJournal : IDisposable
{
    public const int SampleRate = 48000;
    private readonly string folder;
    private readonly StreamWriter writer;
    private readonly Dictionary<string, string> sourceCache = new();
    private string? signature;
    private bool closed;
    public string JournalPath { get; }
    public MusicJournal(string directory, string ffmpeg)
    {
        folder = Path.GetFullPath(directory); Directory.CreateDirectory(Path.Combine(folder, "sources"));
        JournalPath = Path.Combine(folder, "audio_journal.jsonl");
        writer = new StreamWriter(JournalPath, false, new UTF8Encoding(false));
        Write(new JsonObject { ["kind"] = "header", ["schema"] = 1, ["sample_rate"] = SampleRate, ["channels"] = 2, ["ffmpeg"] = ffmpeg });
    }
    private void Write(JsonObject value) { writer.WriteLine(value.ToJsonString()); writer.Flush(); }
    private string CacheSource(string filename)
    {
        filename = Path.GetFullPath(filename);
        if (sourceCache.TryGetValue(filename, out string? known)) return known;
        string digest = JsonData.Digest(filename);
        string destination = Path.Combine(folder, "sources", digest + Path.GetExtension(filename).ToLowerInvariant());
        if (!File.Exists(destination)) File.Copy(filename, destination);
        Write(new JsonObject { ["kind"] = "source", ["id"] = digest, ["path"] = Path.GetRelativePath(folder, destination).Replace('\\', '/'), ["original_path"] = filename });
        sourceCache[filename] = digest; return digest;
    }
    public void Observe(double videoPts, JsonObject? state, MusicPreset? music, bool fresh = true)
    {
        var value = new JsonObject { ["kind"] = "event", ["pts"] = videoPts, ["active"] = false, ["source"] = null,
            ["cursor"] = 0d, ["speed"] = 1d, ["loop"] = false, ["duration"] = 0d, ["offset"] = 0d, ["volume"] = 0d, ["fresh"] = fresh };
        if (music is not null && state is not null)
        {
            value["offset"] = music.OffsetSeconds; value["volume"] = music.Volume; value["source"] = CacheSource(music.AudioFile);
            value["cursor"] = Contract.D(state, "seconds"); value["speed"] = Contract.D(state, "speed"); value["duration"] = Contract.D(state, "duration_seconds");
            value["loop"] = Contract.True(state, "loop"); value["epoch"] = state["clip_epoch"]?.DeepClone(); value["sequence"] = state["last_sequence"]?.DeepClone(); value["revision"] = state["revision"]?.DeepClone(); value["mode"] = state["mode"]?.DeepClone();
            value["active"] = fresh && Contract.True(state, "session_active") && Contract.S(state, "mode") == "playing" && !Contract.True(state, "restore_requested");
        }
        var comparison = (JsonObject)value.DeepClone(); comparison.Remove("pts"); string current = comparison.ToJsonString();
        if (current != signature) { Write(value); signature = current; }
    }
    public string Finish(double duration, double origin = 0)
    {
        Dispose();
        var lines = File.ReadAllLines(JournalPath);
        using (var output = new StreamWriter(JournalPath, false, new UTF8Encoding(false)))
        {
            JsonObject? previous = null; bool wrote = false;
            void WriteRecord(JsonObject value) => output.WriteLine(value.ToJsonString());
            void Carry()
            {
                if (previous is null) return; var carried = (JsonObject)previous.DeepClone();
                if (Contract.True(carried, "active")) { double cursor = Contract.D(carried, "cursor") + (origin - Contract.D(carried, "pts")) * Contract.D(carried, "speed");
                    if (Contract.True(carried, "loop")) cursor %= Contract.D(carried, "duration"); carried["cursor"] = cursor; }
                carried["pts"] = 0d; WriteRecord(carried);
            }
            foreach (var line in lines)
            {
                var value = JsonNode.Parse(line)!.AsObject(); string kind = Contract.S(value, "kind");
                if (kind != "event") { if (kind == "header") value["video_origin"] = origin; WriteRecord(value); }
                else if (Contract.D(value, "pts") < origin) previous = value;
                else { if (!wrote && Contract.D(value, "pts") > origin) Carry(); value["pts"] = Contract.D(value, "pts") - origin; WriteRecord(value); wrote = true; }
            }
            if (!wrote) Carry(); WriteRecord(new JsonObject { ["kind"] = "finish", ["duration"] = duration, ["frames"] = (long)Math.Round(duration * SampleRate, MidpointRounding.ToEven) });
        }
        return JournalPath;
    }
    public void Dispose() { if (!closed) { writer.Dispose(); closed = true; } }
}

internal static class AudioRenderer
{
    private const int ChunkFrames = 65536;
    private sealed class SourceAudio : IDisposable
    {
        private readonly System.IO.MemoryMappedFiles.MemoryMappedFile map;
        private readonly System.IO.MemoryMappedFiles.MemoryMappedViewAccessor view;
        internal long Frames { get; }
        internal SourceAudio(string path)
        {
            Frames = new FileInfo(path).Length / 8;
            map = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, 0, System.IO.MemoryMappedFiles.MemoryMappedFileAccess.Read);
            view = map.CreateViewAccessor(0, 0, System.IO.MemoryMappedFiles.MemoryMappedFileAccess.Read);
        }
        internal double Sample(long frame, int channel) => view.ReadSingle(frame * 8 + channel * 4);
        public void Dispose() { view.Dispose(); map.Dispose(); }
    }
    internal static async Task<JsonObject> RenderAsync(string journalPath, string outputWav, string ffmpeg, CancellationToken cancellation)
    {
        var journalRoot = Path.GetDirectoryName(journalPath)!;
        var sources = new Dictionary<string, string>(); var events = new List<JsonObject>(); JsonObject? finish = null;
        foreach (var line in await File.ReadAllLinesAsync(journalPath, cancellation))
        {
            var value = JsonNode.Parse(line)!.AsObject();
            switch (Contract.S(value, "kind"))
            {
                case "source": sources[Contract.S(value, "id")] = Path.Combine(journalRoot, Contract.S(value, "path")); break;
                case "event": events.Add(value); break;
                case "finish": finish = value; break;
            }
        }
        long total = Contract.I(finish ?? throw new InvalidDataException("音频日志尚未完成"), "frames");
        Directory.CreateDirectory(Path.GetDirectoryName(outputWav)!);
        var decoded = new Dictionary<string, SourceAudio>(); long rendered = 0, musicFrames = 0; int segments = 0;
        try
        {
            using (var output = new FileStream(outputWav, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                WriteWaveHeader(output, total); byte[] pcm = new byte[ChunkFrames * 4];
                void Silence(long count) { Array.Clear(pcm); while (count > 0) { cancellation.ThrowIfCancellationRequested(); int size = (int)Math.Min(ChunkFrames, count); output.Write(pcm, 0, size * 4); count -= size; } }
                for (int i = 0; i < events.Count; i++)
                {
                    cancellation.ThrowIfCancellationRequested(); var value = events[i];
                    long Boundary(JsonObject item) => (long)Math.Min(total, Math.Round(Contract.D(item, "pts") * 48000));
                    long start = Boundary(value), end = i + 1 < events.Count ? Boundary(events[i + 1]) : total;
                    if (start >= total) break; if (end <= start) continue; if (start > rendered) Silence(start - rendered);
                    string? id = value["source"]?.GetValue<string>();
                    if (!Contract.True(value, "active") || id is null || Contract.D(value, "volume") <= 0) Silence(end - start);
                    else
                    {
                        if (!decoded.TryGetValue(id, out SourceAudio? source)) { source = await DecodeAsync(sources[id], id, journalRoot, ffmpeg, cancellation); decoded.Add(id, source); }
                        double cursorStart = Contract.D(value, "cursor"), speed = Contract.D(value, "speed"), span = Contract.D(value, "duration"), offset = Contract.D(value, "offset"), volume = Contract.D(value, "volume"); bool loop = Contract.True(value, "loop");
                        for (long first = start; first < end; first += ChunkFrames)
                        {
                            cancellation.ThrowIfCancellationRequested(); int count = (int)Math.Min(ChunkFrames, end - first); Array.Clear(pcm);
                            for (int frame = 0; frame < count; frame++)
                            {
                                double cursor = cursorStart + (first + frame - start) / 48000d * speed; if (loop) cursor %= span;
                                double position = (cursor - offset) * 48000d;
                                if (position < 0 || position >= source.Frames || (!loop && span > 0 && cursor >= span)) continue;
                                long left = (long)Math.Floor(position), right = Math.Min(left + 1, source.Frames - 1); double fraction = position - left;
                                for (int channel = 0; channel < 2; channel++)
                                {
                                    double sample = (source.Sample(left, channel) * (1 - fraction) + source.Sample(right, channel) * fraction) * volume;
                                    short quantized = (short)Math.Round(Math.Clamp(sample, -1, 1) * 32767d, MidpointRounding.ToEven); BinaryPrimitives.WriteInt16LittleEndian(pcm.AsSpan(frame * 4 + channel * 2, 2), quantized);
                                }
                                musicFrames++;
                            }
                            output.Write(pcm, 0, count * 4);
                        }
                    }
                    rendered = end; segments++;
                }
                if (rendered < total) Silence(total - rendered);
            }
        }
        finally { foreach (var source in decoded.Values) source.Dispose(); }
        return new JsonObject { ["output_wav"] = outputWav, ["journal_path"] = journalPath, ["duration_seconds"] = total / 48000d, ["frames"] = total,
            ["sample_rate"] = 48000, ["channels"] = 2, ["segments"] = segments, ["sources_decoded"] = decoded.Count, ["music_frames"] = musicFrames, ["silent_frames"] = total - musicFrames };
    }
    private static async Task<SourceAudio> DecodeAsync(string source, string id, string folder, string ffmpeg, CancellationToken cancellation)
    {
        string decodedRoot = Path.Combine(folder, "decoded"); Directory.CreateDirectory(decodedRoot);
        string path = Path.Combine(decodedRoot, id + "-48000-stereo.f32");
        var result = await MediaJson.RunAsync(ffmpeg, new[] { "-nostdin", "-hide_banner", "-loglevel", "error", "-y", "-i", source, "-map", "0:a:0", "-vn", "-ac", "2", "-ar", "48000", "-f", "f32le", path }, cancellation);
        if (result.ExitCode != 0) throw new IOException("音乐解码失败：" + result.Error);
        return new SourceAudio(path);
    }
    private static void WriteWaveHeader(Stream output, long frames)
    {
        Span<byte> header = stackalloc byte[44]; "RIFF"u8.CopyTo(header); BinaryPrimitives.WriteUInt32LittleEndian(header[4..8], checked((uint)(frames * 4 + 36)));
        "WAVEfmt "u8.CopyTo(header[8..16]); BinaryPrimitives.WriteUInt32LittleEndian(header[16..20], 16); BinaryPrimitives.WriteUInt16LittleEndian(header[20..22], 1);
        BinaryPrimitives.WriteUInt16LittleEndian(header[22..24], 2); BinaryPrimitives.WriteUInt32LittleEndian(header[24..28], 48000); BinaryPrimitives.WriteUInt32LittleEndian(header[28..32], 192000);
        BinaryPrimitives.WriteUInt16LittleEndian(header[32..34], 4); BinaryPrimitives.WriteUInt16LittleEndian(header[34..36], 16); "data"u8.CopyTo(header[36..40]); BinaryPrimitives.WriteUInt32LittleEndian(header[40..44], checked((uint)(frames * 4))); output.Write(header);
    }
}
