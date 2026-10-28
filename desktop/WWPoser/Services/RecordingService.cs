using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace WWPoser.Services;

public sealed record WindowTarget(nint Hwnd, uint Pid, string Title, int Width, int Height);
public enum RecordingEncoder { NvencD3D11, NvencCompatible, Software, HevcNvencD3D11, HevcNvencCompatible }
public enum RecordingFormat { Compact, Compatible }
public sealed record RecordingSnapshot(string State, string Message, double Elapsed = 0,
    string? OutputPath = null, string? WorkDirectory = null, string? StopReason = null, int? CaptureExitCode = null);

/// <summary>WGC records only the game HWND. Music is rendered from local timeline snapshots.</summary>
public sealed class RecordingService
{
    private readonly object gate = new();
    private readonly string ffmpeg, ffprobe;
    private readonly Func<string, int, int, CancellationToken, Task>? capture;
    private string? session;
    private MusicPreset? preset;
    private CancellationTokenSource? stop;
    private Task? worker;
    private TaskCompletionSource<bool>? captureReady;
    private RecordingSnapshot snapshot = new("idle", "仅录鸣潮窗口");
    public RecordingService(string repositoryRoot) : this(repositoryRoot, null) { }
    internal RecordingService(string repositoryRoot, Func<string, int, int, CancellationToken, Task>? capture)
    {
        ffmpeg = Path.Combine(repositoryRoot, "tools", "recording_bin", "ffmpeg.exe"); ffprobe = Path.Combine(repositoryRoot, "tools", "recording_bin", "ffprobe.exe");
        this.capture = capture; OutputDirectory = DefaultOutputDirectory(repositoryRoot);
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
    public bool IsStopping { get { lock (gate) return worker is { IsCompleted: false } && stop!.IsCancellationRequested; } }
    public void UpdateAudioContext(string? sessionDirectory, MusicPreset? music)
    {
        lock (gate) { session = sessionDirectory; preset = music; }
    }
    public Task StartAsync(string folder, int fps = 30, int height = 1080, RecordingFormat format = RecordingFormat.Compact)
    {
        ValidateSettings(fps, height);
        ValidateFormat(format);
        lock (gate)
        {
            if (worker is { IsCompleted: false }) throw new InvalidOperationException("录屏尚未结束");
            stop?.Dispose(); stop = new CancellationTokenSource();
            snapshot = new RecordingSnapshot("starting", "正在连接鸣潮窗口");
            var ready = captureReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
            var token = stop.Token;
            worker = Task.Run(async () =>
            {
                try
                {
                    if (capture is not null) await capture(Path.GetFullPath(folder), fps, height, token);
                    else await CaptureAsync(Path.GetFullPath(folder), fps, height, format, token);
                }
                catch (Exception error) { Update(Snapshot with { State = "error", Message = error.Message }); }
                finally { ready.TrySetResult(false); }
            });
        }
        return Task.CompletedTask;
    }
    public async Task WaitForReadyAsync(CancellationToken cancellation = default)
    {
        Task<bool> ready; Task pending;
        lock (gate)
        {
            ready = captureReady?.Task ?? throw new InvalidOperationException("录屏尚未开始");
            pending = worker!;
        }
        var firstFrame = await ready.WaitAsync(TimeSpan.FromSeconds(60), cancellation).ConfigureAwait(false);
        lock (gate)
        {
            if (!firstFrame || !ReferenceEquals(worker, pending) || pending.IsCompleted || stop!.IsCancellationRequested || snapshot.State != "recording")
                throw new IOException("录屏未就绪：" + snapshot.Message);
        }
    }
    // Only a live encoder's nonterminal progress packet with a real frame can
    // release automatic playback. Starting a process is not capture readiness.
    internal void ConfirmCaptureReady()
    {
        lock (gate) { if (snapshot.State == "recording") captureReady?.TrySetResult(true); }
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
    private static void ValidateSettings(int fps, int height)
    {
        if (fps is not (30 or 60)) throw new ArgumentOutOfRangeException(nameof(fps));
        if (height is not (0 or 1080 or 1440)) throw new ArgumentOutOfRangeException(nameof(height));
    }
    private static void ValidateFormat(RecordingFormat format)
    { if (format is not (RecordingFormat.Compact or RecordingFormat.Compatible)) throw new ArgumentOutOfRangeException(nameof(format)); }
    internal static bool IsHevc(RecordingEncoder encoder) => encoder is RecordingEncoder.HevcNvencD3D11 or RecordingEncoder.HevcNvencCompatible;
    private static string EncoderName(RecordingEncoder encoder) => encoder == RecordingEncoder.Software ? "libx264" : IsHevc(encoder) ? "hevc_nvenc" : "h264_nvenc";
    private static string EncoderLabel(RecordingEncoder encoder) => encoder switch {
        RecordingEncoder.HevcNvencD3D11 => "HEVC 硬件编码 · GPU 直连", RecordingEncoder.HevcNvencCompatible => "HEVC 硬件编码 · 兼容模式",
        RecordingEncoder.NvencD3D11 => "H.264 硬件编码 · GPU 直连", RecordingEncoder.NvencCompatible => "H.264 硬件编码 · 兼容模式",
        _ => "软件编码（硬件不可用，开销较高）" };
    internal static IReadOnlyList<string> BuildEncodingArguments(int fps, RecordingEncoder encoder = RecordingEncoder.NvencD3D11) => encoder == RecordingEncoder.Software ? new[] {
        "-an", "-c:v", "libx264", "-preset", "veryfast", "-tune", "zerolatency", "-crf", "20", "-bf", "0", "-threads", "4",
        "-colorspace", "bt470bg", "-color_range", "tv", "-r", fps.ToString(CultureInfo.InvariantCulture), "-fps_mode", "cfr" } : IsHevc(encoder) ? new[] {
        // Preserve the existing audio-origin estimate: reordered B frames or
        // lookahead would delay progress timestamps relative to capture time.
        "-an", "-c:v", "hevc_nvenc", "-preset", "p4", "-tune", "ll", "-rc", "vbr", "-cq", "25", "-b:v", "0", "-profile:v", "main",
        "-bf", "0", "-rc-lookahead", "0", "-spatial-aq", "0", "-temporal-aq", "0", "-multipass", "disabled",
        "-g", (fps * 2).ToString(CultureInfo.InvariantCulture), "-colorspace", "bt470bg", "-color_range", "tv",
        "-r", fps.ToString(CultureInfo.InvariantCulture), "-fps_mode", "cfr" } : new[] {
        "-an", "-c:v", "h264_nvenc", "-preset", "p4", "-tune", "ll", "-rc", "constqp", "-qp", "21", "-profile:v", "high",
        "-bf", "0", "-rc-lookahead", "0", "-spatial-aq", "0", "-temporal-aq", "0", "-multipass", "disabled",
        // NVENC's packed-RGB conversion produces limited-range BT.601 YUV.
        // Match the container tags to the bitstream instead of inheriting RGB/full.
        "-colorspace", "bt470bg", "-color_range", "tv", "-r", fps.ToString(CultureInfo.InvariantCulture), "-fps_mode", "cfr" };
    public static IReadOnlyList<string> BuildCaptureArguments(WindowTarget target, string videoPath, int fps = 30, int height = 1080,
        RecordingEncoder encoder = RecordingEncoder.NvencD3D11)
    {
        ValidateSettings(fps, height);
        int width = height switch { 1080 => 1920, 1440 => 2560, _ => -2 }, outputHeight = height == 0 ? -2 : height;
        // WGC scales on the GPU and rounds original dimensions to even pixels.
        // The preferred path keeps D3D11 BGRA surfaces on NVIDIA all the way to NVENC.
        // Compatibility mode still uses NVENC, but allows WGC's default capture adapter.
        string filter = $"gfxcapture=hwnd={(ulong)target.Hwnd}:capture_cursor=0:capture_border=0:display_border=1:width={width}:height={outputHeight}:resize_mode=scale_aspect:max_framerate={fps}:output_fmt=bgra";
        var arguments = new List<string> { "-hide_banner", "-loglevel", "warning", "-n", "-stats_period", "0.1", "-progress", "pipe:1" };
        if (encoder is RecordingEncoder.NvencD3D11 or RecordingEncoder.HevcNvencD3D11)
            arguments.AddRange(["-init_hw_device", "d3d11va=recording:,vendor_id=0x10de", "-filter_hw_device", "recording"]);
        else if (encoder is RecordingEncoder.NvencCompatible or RecordingEncoder.HevcNvencCompatible or RecordingEncoder.Software)
            filter += ",hwdownload,format=bgra" + (encoder == RecordingEncoder.Software ? ",format=yuv420p" : "");
        else throw new ArgumentOutOfRangeException(nameof(encoder));
        // setparams changes frame metadata only, including for hardware frames.
        // Output stream tags otherwise inherit WGC's RGB/full-range properties.
        arguments.AddRange(["-filter_complex", filter + ",setparams=colorspace=bt470bg:range=limited[v]", "-map", "[v]"]);
        arguments.AddRange(BuildEncodingArguments(fps, encoder)); arguments.AddRange(["-f", "matroska", videoPath]); return arguments;
    }
    private async Task CaptureAsync(string folder, int fps, int height, RecordingFormat format, CancellationToken requestStop)
    {
        MusicJournal? journal = null; string? work = null;
        try
        {
            WindowTarget target = GameWindows.Find(); Directory.CreateDirectory(folder);
            string stem = "鸣潮_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + "_" + Guid.NewGuid().ToString("N")[..6];
            string output = Path.Combine(folder, stem + ".mp4"); work = Path.Combine(folder, "." + stem); Directory.CreateDirectory(work);
            Update(new RecordingSnapshot("starting", "正在连接鸣潮窗口", WorkDirectory: work));
            journal = new MusicJournal(Path.Combine(work, "music"), ffmpeg);
            var video = await CaptureVideoAsync(target, work, fps, height, journal, requestStop, format: format);
            if (video is null) { Update(new RecordingSnapshot("idle", "已取消录屏", WorkDirectory: work)); return; }
            string silent = video.Path; double duration = await VideoDurationAsync(silent), origin = video.Origin;
            Update(new RecordingSnapshot("finishing", "正在合成 MMD 音乐并保存 MP4", duration, WorkDirectory: work));
            string journalPath = journal.Finish(duration, origin), wav = Path.Combine(work, "music.wav");
            JsonObject audio = await AudioRenderer.RenderAsync(journalPath, wav, ffmpeg, CancellationToken.None);
            string pendingOutput = Path.Combine(work, "final.mp4");
            var mux = await MediaJson.RunAsync(ffmpeg, BuildMuxArguments(silent, wav, pendingOutput, duration, video.Encoder));
            if (mux.ExitCode != 0) throw new IOException("MP4 合成失败，原始文件保留在 " + work + "：" + mux.Error);
            Update(new RecordingSnapshot("finishing", "正在校验视频并释放临时空间", duration, WorkDirectory: work));
            var verification = await RecordingStorage.VerifyAsync(ffprobe, silent, pendingOutput, duration);
            File.Move(pendingOutput, output);
            var result = new JsonObject { ["schema"] = 2, ["target"] = new JsonObject { ["hwnd"] = (ulong)target.Hwnd, ["pid"] = target.Pid, ["title"] = target.Title, ["width"] = target.Width, ["height"] = target.Height },
                ["duration"] = duration, ["fps"] = fps, ["requested_height"] = height, ["output"] = output, ["audio"] = audio, ["video_origin_estimate"] = origin, ["capture_process_start"] = video.Start,
                ["requested_format"] = format.ToString(), ["video_encoder"] = EncoderName(video.Encoder), ["capture_backend"] = video.Encoder.ToString(), ["capture_attempts"] = video.Attempts,
                ["output_bytes"] = new FileInfo(output).Length, ["verification"] = verification,
                ["capture_exit_code"] = video.ExitCode, ["stop_reason"] = video.Problem ?? "user_stop" };
            string? cleanupWarning = null;
            try { JsonData.Write(Path.Combine(work, "result.json"), result); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { cleanupWarning = error.Message; }
            // Keep interrupted captures recoverable. Only a normal stop and a
            // verified final MP4 allow removing this run's generated media.
            var cleanup = video.ExitCode == 0 && video.Problem is null && cleanupWarning is null ? RecordingStorage.CleanupGeneratedMedia(work) :
                new JsonObject { ["media_retained"] = true, ["reason"] = cleanupWarning is null ? "interrupted_capture" : "metadata_write_failed" };
            result["cleanup"] = cleanup;
            try { JsonData.Write(Path.Combine(work, "result.json"), result); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { cleanupWarning = error.Message; }
            var cleanupErrors = cleanup["errors"] as JsonArray;
            string suffix = cleanupErrors is { Count: > 0 } || cleanupWarning is not null ? " · 部分临时文件或清理日志待处理" : "";
            Update(new RecordingSnapshot("saved", "已保存：" + Path.GetFileName(output) + " · " + EncoderLabel(video.Encoder) + suffix, duration, output, work, video.Problem ?? "user_stop", video.ExitCode));
        }
        catch (Exception error)
        {
            Update(new RecordingSnapshot("error", error.Message, WorkDirectory: work));
            if (work is not null)
                try { File.WriteAllText(Path.Combine(work, "error.txt"), error.ToString(), new UTF8Encoding(false)); }
                catch (Exception diagnosticError) when (diagnosticError is IOException or UnauthorizedAccessException) { }
        }
        finally { journal?.Dispose(); }
    }
    internal static IReadOnlyList<string> BuildMuxArguments(string silent, string wav, string output, double duration, RecordingEncoder encoder)
    {
        var arguments = new List<string> { "-hide_banner", "-loglevel", "error", "-n", "-i", silent, "-i", wav, "-map", "0:v:0", "-map", "1:a:0", "-c:v", "copy" };
        if (IsHevc(encoder)) arguments.AddRange(["-tag:v", "hvc1"]);
        arguments.AddRange(["-c:a", "aac", "-b:a", "192k", "-t", duration.ToString("R", CultureInfo.InvariantCulture), "-movflags", "+faststart", output]);
        return arguments;
    }
    internal sealed record CapturedVideo(string Path, RecordingEncoder Encoder, double Start, double Origin, int ExitCode, string? Problem, JsonArray Attempts);
    // The optional argument/window providers let offline checks exercise actual
    // child-process startup, retry and stop handling using synthetic video only.
    internal async Task<CapturedVideo?> CaptureVideoAsync(WindowTarget target, string work, int fps, int height, MusicJournal journal,
        CancellationToken requestStop, Func<RecordingEncoder, string, IReadOnlyList<string>>? buildArguments = null, Func<string?>? windowProblem = null,
        RecordingFormat format = RecordingFormat.Compact)
    {
        ValidateSettings(fps, height);
        ValidateFormat(format);
        buildArguments ??= (encoder, path) => BuildCaptureArguments(target, path, fps, height, encoder);
        windowProblem ??= () => GameWindows.Problem(target);
        var attempts = new JsonArray();
        var encoders = format == RecordingFormat.Compact ? new[] { RecordingEncoder.HevcNvencD3D11, RecordingEncoder.NvencD3D11, RecordingEncoder.HevcNvencCompatible, RecordingEncoder.NvencCompatible, RecordingEncoder.Software } :
            new[] { RecordingEncoder.NvencD3D11, RecordingEncoder.NvencCompatible, RecordingEncoder.Software };
        foreach (var encoder in encoders)
        {
            if (requestStop.IsCancellationRequested) return null;
            string silent = Path.Combine(work, encoder switch { RecordingEncoder.HevcNvencD3D11 => "video-hevc.mkv", RecordingEncoder.HevcNvencCompatible => "video-hevc-compatible.mkv",
                RecordingEncoder.NvencD3D11 => "video.mkv", RecordingEncoder.NvencCompatible => "video-nvenc-compatible.mkv", _ => "video-software.mkv" });
            Update(new RecordingSnapshot("starting", "正在启动 " + EncoderLabel(encoder), WorkDirectory: work));
            var start = new ProcessStartInfo(ffmpeg) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (string argument in buildArguments(encoder, silent)) start.ArgumentList.Add(argument);
            var sample = AudioSample(); journal.Observe(Seconds(), sample.State, sample.Music, sample.Fresh); string signature = sample.Signature;
            double captureStart = Seconds(); using var process = Process.Start(start)!;
            using var captureLog = new StreamWriter(Path.Combine(work, "capture.log"), true, new UTF8Encoding(false));
            await captureLog.WriteLineAsync("--- " + encoder + " ---"); await captureLog.FlushAsync();
            var packets = new ConcurrentQueue<(double Received, Dictionary<string, string> Values)>();
            var progress = ReadProgressAsync(process.StandardOutput, packets); var errors = CopyErrorsAsync(process.StandardError, captureLog);
            long frameCount = 0; double lastProgress = captureStart, elapsed = 0; double? stopSent = null; string? problem = null;
            var origins = new List<double>();
            void DrainProgress()
            {
                while (packets.TryDequeue(out var packet))
                {
                    if (!packet.Values.TryGetValue("frame", out string? text) || !long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long frames) || frames <= frameCount) continue;
                    packet.Values.TryGetValue("out_time_us", out string? time); double.TryParse(time, NumberStyles.Float, CultureInfo.InvariantCulture, out double micros); elapsed = micros / 1e6;
                    frameCount = frames; lastProgress = packet.Received;
                    if (packet.Values.GetValueOrDefault("progress") != "end" && elapsed > 0 && double.IsFinite(elapsed)) origins.Add(packet.Received - elapsed + 1d / fps);
                    Update(new RecordingSnapshot(stopSent is null ? "recording" : "finishing", stopSent is null ? "正在录制鸣潮 · " + EncoderLabel(encoder) : "正在保存录屏", Math.Max(0, elapsed), WorkDirectory: work));
                    if (stopSent is null && !process.HasExited && packet.Values.GetValueOrDefault("progress") != "end") ConfirmCaptureReady();
                }
            }
            try
            {
                while (true)
                {
                    double now = Seconds(); DrainProgress();
                    sample = AudioSample(); if (sample.Signature != signature) { journal.Observe(now, sample.State, sample.Music, sample.Fresh); signature = sample.Signature; }
                    if (process.HasExited) break;
                    if (stopSent is null)
                    {
                        problem = windowProblem();
                        if (problem is null && ((frameCount == 0 && now - captureStart > 20) || (frameCount > 0 && now - lastProgress > 10))) problem = "未收到持续游戏画面，请使用窗口或无边框模式";
                        if (problem is not null || requestStop.IsCancellationRequested)
                        { try { await process.StandardInput.WriteLineAsync("q"); await process.StandardInput.FlushAsync(); } catch (IOException) { } stopSent = now; }
                    }
                    else if (now - stopSent > 15) throw new IOException("录屏编码器未正常结束，原始文件保留在 " + work);
                    await Task.Delay(50);
                }
                await process.WaitForExitAsync(); await progress; await errors; DrainProgress();
                attempts.Add(new JsonObject { ["backend"] = encoder.ToString(), ["exit_code"] = process.ExitCode, ["frames"] = frameCount,
                    ["process_start"] = captureStart, ["stop_requested"] = stopSent is not null, ["problem"] = problem, ["path"] = silent });
                JsonData.Write(Path.Combine(work, "capture_attempts.json"), new JsonObject { ["attempts"] = attempts.DeepClone() });
                if (frameCount == 0)
                {
                    if (requestStop.IsCancellationRequested) return null;
                    // Retry only failed initialization, never restart after usable frames,
                    // a window failure or a user stop. Failed output/logs remain recoverable.
                    if (process.ExitCode != 0 && stopSent is null) continue;
                    throw new IOException((problem ?? "窗口录制未产生画面") + "，日志保留在 " + work);
                }
                if (!File.Exists(silent)) throw new IOException("窗口录制文件丢失，日志保留在 " + work);
                if ((process.ExitCode != 0 || stopSent is null) && problem is null) problem = "录屏组件提前结束，已保留可用画面和日志";
                return new CapturedVideo(silent, encoder, captureStart, Math.Max(captureStart, origins.Count > 0 ? origins.Min() : captureStart), process.ExitCode, problem, attempts);
            }
            finally
            {
                if (!process.HasExited) { process.Kill(true); await process.WaitForExitAsync(); }
                await Task.WhenAll(progress, errors);
            }
        }
        throw new IOException("录屏编码启动失败，详细日志保留在 " + work);
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
    private static readonly string[] StreamKeys = ["active", "source", "speed", "loop", "duration", "offset", "volume", "epoch", "sequence", "mode"];
    private static bool SameStream(JsonObject left, JsonObject right) => StreamKeys.All(key => JsonNode.DeepEquals(left[key], right[key]));
    private static bool UncertainRead(JsonObject value) => !Contract.True(value, "fresh") && !Contract.True(value, "active") &&
        (value["mode"] is null || value["mode"]!.GetValue<string>() == "playing");
    internal static (List<JsonObject> Events, int BridgedReads) ContinuousTimeline(IReadOnlyList<JsonObject> observations)
    {
        var reliable = new List<JsonObject>(); int bridged = 0;
        for (int i = 0; i < observations.Count; i++)
        {
            var value = observations[i];
            if (UncertainRead(value) && i > 0 && Contract.True(observations[i - 1], "active"))
            {
                int next = i + 1; while (next < observations.Count && UncertainRead(observations[next])) next++;
                // Bridge only short uncertain reads bracketed by the same
                // confirmed playing state and control sequence, never a pause.
                if (next < observations.Count && Contract.True(observations[next], "active") && SameStream(observations[i - 1], observations[next]) &&
                    Contract.D(observations[next], "pts") - Contract.D(value, "pts") <= .25 &&
                    Contract.D(observations[next], "pts") - Contract.D(observations[i - 1], "pts") <= 1)
                { bridged += next - i; i = next - 1; continue; }
            }
            reliable.Add(value);
        }
        var runs = new List<JsonObject>();
        foreach (var value in reliable)
        {
            bool continuous = runs.Count > 0 && SameStream(runs[^1], value);
            if (continuous && Contract.True(value, "active"))
            {
                var first = runs[^1];
                double expected = Contract.D(first, "cursor") + (Contract.D(value, "pts") - Contract.D(first, "pts")) * Contract.D(first, "speed");
                double error = Contract.D(value, "cursor") - expected;
                if (Contract.True(value, "loop") && Contract.D(value, "duration") > 0) error = Math.IEEERemainder(error, Contract.D(value, "duration"));
                // A real clock discontinuity still creates a new anchor. Small
                // status/read jitter must not splice or repeat the music.
                continuous = Math.Abs(error) <= .25;
            }
            if (!continuous) runs.Add(value);
        }
        return (runs, bridged);
    }
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
        int observationCount = events.Count; var plan = ContinuousTimeline(events); events = plan.Events;
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
                        double cursorStart = Contract.D(value, "cursor"), speed = Contract.D(value, "speed"), span = Contract.D(value, "duration"), offset = Contract.D(value, "offset"), volume = Contract.D(value, "volume"); bool loop = Contract.True(value, "loop");
                        if (!double.IsFinite(speed) || speed <= 0) throw new InvalidDataException("音频播放速度无效");
                        if (loop && (!double.IsFinite(span) || span <= 0)) throw new InvalidDataException("音频循环时长无效");
                        string decodeKey = id + "-rate-" + BitConverter.DoubleToInt64Bits(speed).ToString("x16", CultureInfo.InvariantCulture);
                        if (!decoded.TryGetValue(decodeKey, out SourceAudio? source)) { source = await DecodeAsync(sources[id], decodeKey, speed, journalRoot, ffmpeg, cancellation); decoded.Add(decodeKey, source); }
                        long sourceStart = (long)Math.Round((cursorStart - offset) / speed * 48000d, MidpointRounding.ToEven), lastCycle = long.MinValue, cycleStart = sourceStart;
                        for (long first = start; first < end; first += ChunkFrames)
                        {
                            cancellation.ThrowIfCancellationRequested(); int count = (int)Math.Min(ChunkFrames, end - first); Array.Clear(pcm);
                            for (int frame = 0; frame < count; frame++)
                            {
                                long elapsedFrames = first + frame - start;
                                double cursor = cursorStart + elapsedFrames / 48000d * speed;
                                // FFmpeg has already resampled this source for
                                // the run's speed. Round its anchor only once;
                                // rounding half-sample ties per frame can itself
                                // duplicate/skip samples on alternating frames.
                                if (loop)
                                {
                                    long cycle = (long)Math.Floor(cursor / span);
                                    if (cycle != lastCycle) { cycleStart = (long)Math.Round((cursorStart - cycle * span - offset) / speed * 48000d, MidpointRounding.ToEven); lastCycle = cycle; }
                                    cursor %= span;
                                }
                                long sourceFrame = (loop ? cycleStart : sourceStart) + elapsedFrames;
                                if (cursor < offset || sourceFrame < 0 || sourceFrame >= source.Frames || (!loop && span > 0 && cursor >= span)) continue;
                                for (int channel = 0; channel < 2; channel++)
                                {
                                    double sample = source.Sample(sourceFrame, channel) * volume;
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
            ["sample_rate"] = 48000, ["channels"] = 2, ["segments"] = segments, ["observations"] = observationCount, ["bridged_uncertain_reads"] = plan.BridgedReads,
            ["timeline_renderer"] = "continuous_v2", ["resampler"] = "soxr", ["sources_decoded"] = decoded.Count, ["music_frames"] = musicFrames, ["silent_frames"] = total - musicFrames };
    }
    private static async Task<SourceAudio> DecodeAsync(string source, string id, double speed, string folder, string ffmpeg, CancellationToken cancellation)
    {
        string decodedRoot = Path.Combine(folder, "decoded"); Directory.CreateDirectory(decodedRoot);
        string path = Path.Combine(decodedRoot, id + "-48000-stereo.f32");
        const string resample = "aresample=48000:resampler=soxr:precision=28";
        string filter = speed == 1 ? resample : resample + ",asetrate=48000*" + speed.ToString("R", CultureInfo.InvariantCulture) + "," + resample;
        var result = await MediaJson.RunAsync(ffmpeg, new[] { "-nostdin", "-hide_banner", "-loglevel", "error", "-y", "-i", source, "-map", "0:a:0", "-vn", "-af", filter, "-ac", "2", "-ar", "48000", "-f", "f32le", path }, cancellation);
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
