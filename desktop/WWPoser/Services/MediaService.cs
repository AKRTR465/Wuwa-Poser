using System.Diagnostics;
using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace WWPoser.Services;

public sealed record MusicPreset(string AudioFile, double OffsetSeconds = 0, double Volume = 1);

/// <summary>Decoded PCM music follows the shared playback timeline. No game memory access.</summary>
public sealed class MediaService
{
    private readonly object gate = new();
    private string? session;
    private MusicPreset? preset;
    private CancellationTokenSource? cancellation;
    private Task? worker;
    private string? lastError;
    private long synchronizationRevision;
    private readonly Func<MciDevice> createDevice;
    private readonly Func<string, CancellationToken, Task<string>> prepareAudio;

    public MediaService(string repositoryRoot) : this(() => new MciDevice(), new AudioDecodeService(repositoryRoot).PrepareAsync) { }
    internal MediaService(Func<MciDevice> createDevice) : this(createDevice, (path, _) => Task.FromResult(path)) { }
    internal MediaService(Func<MciDevice> createDevice, Func<string, CancellationToken, Task<string>> prepareAudio)
    { this.createDevice = createDevice; this.prepareAudio = prepareAudio; }
    public Task<string> PrepareAsync(string path, CancellationToken cancellation = default) => prepareAudio(Path.GetFullPath(path), cancellation);
    public string? LastError { get { lock (gate) return lastError; } }
    private void ReportError(string? message) { lock (gate) lastError = message; }

    public void UpdateContext(string? sessionDirectory, MusicPreset? music)
    {
        lock (gate) { session = sessionDirectory; preset = music; }
    }
    public void RequestResynchronization() { lock (gate) synchronizationRevision++; }
    public Task StartSynchronizationAsync()
    {
        lock (gate)
        {
            if (worker is { IsCompleted: false }) return Task.CompletedTask;
            cancellation?.Dispose(); cancellation = new CancellationTokenSource();
            worker = Task.Run(() => SynchronizeAsync(cancellation.Token));
        }
        return Task.CompletedTask;
    }
    public async Task StopAsync()
    {
        Task? pending;
        lock (gate) { cancellation?.Cancel(); pending = worker; }
        try { if (pending is not null) await pending.ConfigureAwait(false); }
        finally
        {
            lock (gate)
            {
                if (ReferenceEquals(worker, pending))
                {
                    worker = null; cancellation?.Dispose(); cancellation = null;
                    session = null; preset = null; lastError = null;
                }
            }
        }
    }
    public static (long PositionMilliseconds, bool Playing, double Speed) DesiredAudio(JsonObject state, double offset, long lengthMilliseconds)
    {
        double seconds = Contract.D(state, "seconds"), speed = Contract.D(state, "speed");
        long position = (long)Math.Round((seconds - offset) * 1000, MidpointRounding.ToEven);
        bool playing = Contract.True(state, "session_active") && Contract.S(state, "mode") == "playing" &&
            !Contract.True(state, "restore_requested") && position >= 0 && position < lengthMilliseconds;
        return (Math.Clamp(position, 0, lengthMilliseconds), playing, speed);
    }
    private async Task SynchronizeAsync(CancellationToken stop)
    {
        using var device = createDevice();
        string? opened = null; ulong? epoch = null; long length = 0, synchronizedRevision = 0;
        try
        {
            while (!stop.IsCancellationRequested)
            {
                string? selected; MusicPreset? music; long requestedRevision;
                lock (gate) { selected = session; music = preset; requestedRevision = synchronizationRevision; }
                try
                {
                    if (selected is null || music is null)
                    {
                        device.Pause();
                    }
                    else
                    {
                        string filename = Path.GetFullPath(music.AudioFile);
                        if (opened != filename)
                        {
                            // Retire our cached identity before opening another file.
                            // A failed B must not leave A marked open after A was closed.
                            opened = null; epoch = null; length = 0;
                            device.Close();
                            var decoded = await PrepareAsync(filename, stop).ConfigureAwait(false);
                            stop.ThrowIfCancellationRequested();
                            // A different song/session may have been selected while decoding.
                            lock (gate) { if (session != selected || preset != music) continue; }
                            length = device.Open(decoded); opened = filename;
                        }
                        device.SetVolume(music.Volume);
                        var (state, fresh) = MediaJson.ReadTimeline(selected, 5);
                        if (!Contract.True(state, "session_active") || !fresh) device.Pause();
                        else
                        {
                            var desired = DesiredAudio(state, music.OffsetSeconds, length);
                            device.SetSpeed(desired.Speed); ulong currentEpoch = state["clip_epoch"]!.GetValue<ulong>();
                            if (epoch != currentEpoch || synchronizedRevision != requestedRevision || Math.Abs(device.Position - desired.PositionMilliseconds) > (desired.Playing ? 350 : 10))
                            { device.Pause(); device.Seek(desired.PositionMilliseconds); epoch = currentEpoch; synchronizedRevision = requestedRevision; }
                            if (desired.Playing) { if (!device.IsPlaying) { device.Seek(desired.PositionMilliseconds); device.Play(); } }
                            else device.Pause();
                        }
                    }
                    ReportError(null);
                }
                catch (Exception error) when (error is not OperationCanceledException)
                {
                    opened = null; epoch = null; length = 0;
                    var message = error.Message;
                    try { device.Close(); }
                    catch (Exception cleanup) when (cleanup is not OperationCanceledException) { message += "；音频设备重置失败：" + cleanup.Message; }
                    ReportError(message);
                    // Keep the worker recoverable, but do not spin on unsupported
                    // files or a disconnected device. A later selection can reopen it.
                    await Task.Delay(400, stop).ConfigureAwait(false);
                }
                await Task.Delay(100, stop).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
    }

    internal sealed class MciDevice : IDisposable
    {
        [DllImport("winmm.dll", CharSet = CharSet.Unicode)] private static extern uint mciSendStringW(string command, StringBuilder output, uint length, IntPtr callback);
        [DllImport("winmm.dll", CharSet = CharSet.Unicode)] private static extern bool mciGetErrorStringW(uint code, StringBuilder output, uint length);
        private readonly string alias = "wwp_audio_" + Guid.NewGuid().ToString("N");
        private readonly Func<string, string> sendCommand;
        private readonly CommandThread? nativeCommands;
        private bool opened; private double speed = 1; private int? volume;
        internal MciDevice(Func<string, string>? sendCommand = null)
        {
            if (sendCommand is not null) this.sendCommand = sendCommand;
            else { nativeCommands = new CommandThread(); this.sendCommand = nativeCommands.Send; }
        }
        private string Command(string command) => sendCommand(command);
        private static string NativeCommand(string command)
        {
            var output = new StringBuilder(512); uint code = mciSendStringW(command, output, 512, IntPtr.Zero);
            if (code != 0) { var error = new StringBuilder(512); mciGetErrorStringW(code, error, 512); throw new System.ComponentModel.Win32Exception((int)code, "音频设备错误：" + error); }
            return output.ToString();
        }
        public long Open(string path)
        {
            // The default WAV driver has no setaudio/variable-speed support.
            // All production inputs are PCM WAV, handled by the digital-video driver.
            Command($"open \"{path}\" type mpegvideo alias {alias}"); opened = true; speed = 1; volume = null;
            try { Command($"set {alias} time format milliseconds"); return long.Parse(Command($"status {alias} length"), CultureInfo.InvariantCulture); }
            catch { try { Close(); } catch (System.ComponentModel.Win32Exception) { } throw; }
        }
        public long Position => long.Parse(Command($"status {alias} position"), CultureInfo.InvariantCulture);
        public bool IsPlaying => opened && Command($"status {alias} mode") == "playing";
        public void Pause() { if (IsPlaying) Command($"pause {alias}"); }
        public void Seek(long milliseconds) => Command($"seek {alias} to {milliseconds}");
        public void Play() => Command($"play {alias}");
        public void SetSpeed(double value) { if (value == speed) return; Command($"set {alias} speed {(int)Math.Round(value * 1000)}"); speed = value; }
        public void SetVolume(double value)
        {
            int current = (int)Math.Round(value * 1000); if (volume == current) return;
            if (current != 1000 || volume is not null) Command($"setaudio {alias} volume to {current}");
            volume = current;
        }
        public void Close()
        {
            if (!opened) return;
            try { Command($"close {alias}"); }
            // mciapi.h: INVALID_DEVICE_ID / INVALID_DEVICE_NAME mean the alias
            // is already gone. Repeated cleanup has succeeded in that case.
            catch (System.ComponentModel.Win32Exception error) when (error.NativeErrorCode is 257 or 263) { }
            opened = false; speed = 1; volume = null;
        }
        public void Dispose() { try { Close(); } finally { nativeCommands?.Dispose(); } }

        // The MPEGVideo driver needs a stable STA/COM owner. Async continuations
        // run on different pool threads; marshal all native commands to one owner.
        private sealed class CommandThread : IDisposable
        {
            [DllImport("ole32.dll")] private static extern int CoInitializeEx(nint reserved, uint flags);
            [DllImport("ole32.dll")] private static extern void CoUninitialize();
            private readonly BlockingCollection<(string Command, TaskCompletionSource<string> Result)> commands = new();
            private readonly Thread thread;
            private int disposed;
            internal CommandThread()
            {
                if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("实时音频播放需要 Windows。");
                thread = new Thread(Run) { IsBackground = true, Name = "WuwaPoser music" };
                thread.SetApartmentState(ApartmentState.STA); thread.Start();
            }
            private void Run()
            {
                var initialized = CoInitializeEx(0, 2);
                try
                {
                    foreach (var request in commands.GetConsumingEnumerable())
                    {
                        try
                        {
                            Marshal.ThrowExceptionForHR(initialized);
                            request.Result.TrySetResult(NativeCommand(request.Command));
                        }
                        catch (Exception error) { request.Result.TrySetException(error); }
                    }
                }
                finally { if (initialized >= 0) CoUninitialize(); }
            }
            internal string Send(string command)
            {
                var result = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
                commands.Add((command, result)); return result.Task.GetAwaiter().GetResult();
            }
            public void Dispose()
            {
                if (Interlocked.Exchange(ref disposed, 1) != 0) return;
                commands.CompleteAdding(); thread.Join(); commands.Dispose();
            }
        }
    }
}

internal static class MediaJson
{
    internal static (JsonObject State, bool Fresh) ReadTimeline(string session, double maximumAge)
        => PlaybackSessionStatus.Read(session, maximumAge);
    internal static async Task<(int ExitCode, string Output, string Error)> RunAsync(string executable, IEnumerable<string> arguments, CancellationToken cancellation = default)
    {
        var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, RedirectStandardInput = true };
        foreach (string argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!; process.StandardInput.Close();
        Task<string> output = process.StandardOutput.ReadToEndAsync(cancellation), error = process.StandardError.ReadToEndAsync(cancellation);
        try { await process.WaitForExitAsync(cancellation); return (process.ExitCode, await output, await error); }
        catch { if (!process.HasExited) { process.Kill(true); await process.WaitForExitAsync(); } throw; }
    }
}
