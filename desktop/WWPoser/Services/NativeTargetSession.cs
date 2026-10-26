namespace WWPoser.Services;

// The owning carrier retains its actors while these bounded observers run.
// A cancelled observation keeps recovery ownership until its observer has
// written its final result; the owner then receives an explicit completion signal.
public sealed class NativeTargetSession
{
    private sealed record PendingObservation(int Pid, string ResultFile, Task<int> InstallerExit);
    private readonly NativeGraphService graph;
    private readonly Func<string, Task<int>> startInstaller;
    private readonly Func<int, bool> gameExited;
    private readonly TimeSpan recoveryTimeout;
    private readonly Dictionary<string, PendingObservation> pending = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim operation = new(1, 1);
    private CancellationTokenSource? activeObservation;

    public NativeTargetSession(string repoRoot) : this(repoRoot, CarrierLoader.StartInstallation, GameProcess.Exited, TimeSpan.FromSeconds(45)) { }
    internal NativeTargetSession(string repoRoot, Func<string, Task<int>> startInstaller, Func<int, bool> gameExited, TimeSpan recoveryTimeout)
    {
        graph = new(repoRoot); this.startInstaller = startInstaller; this.gameExited = gameExited; this.recoveryTimeout = recoveryTimeout;
    }

    private static bool ObserverRestored(string directory, string resultFile)
    {
        var result = CarrierLoader.ReadEvidence(Path.Combine(directory, resultFile));
        return result is not null && Contract.True(result, "all_hooks_restored") && Contract.True(result, "callbacks_quiescent");
    }

    private static bool InstallerDisarmed(JsonObject? result) => result?["stage"] is JsonValue stageValue &&
        stageValue.TryGetValue<string>(out var stage) && stage is "carrier_seen" or "timeout" or "cancelled" or "load_library_failed" or "hook_failed" or "hook_export_missing" or "cancelled_before_hook" &&
        Contract.True(result, "disarmed");

    private static bool FileMissing(string path)
    {
        try { File.GetAttributes(path); return false; }
        catch (Exception error) when (error is FileNotFoundException or DirectoryNotFoundException) { return true; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return false; }
    }

    // Called only after the installer completion was awaited. The native V1
    // guard publishes its marker before checking stop.flag and before touching
    // targets. Thus a still-delayed worker cannot access released actors after
    // this stop -> installer exit -> marker check sequence.
    private bool RecoveryConfirmed(string directory, PendingObservation observation)
    {
        var installer = CarrierLoader.ReadEvidence(Path.Combine(directory, "installer_result.json"));
        if (InstallerDisarmed(installer))
        {
            if (ObserverRestored(directory, observation.ResultFile)) return true;
            if (FileMissing(Path.Combine(directory, observation.ResultFile)) && FileMissing(Path.Combine(directory, "logs", "carrier_status.json")))
            {
                if (CarrierLoader.NotLoaded(directory)) return true;
                if (File.Exists(Path.Combine(directory, "stop.flag")) &&
                    installer!["stage"] is JsonValue stageValue && stageValue.TryGetValue<string>(out var stage) && stage is "timeout" or "cancelled" &&
                    installer["loaded"] is JsonValue loadedValue && loadedValue.TryGetValue<bool>(out var loaded) && !loaded &&
                    installer["observer_early_stop_protocol"] is JsonValue protocol && protocol.TryGetValue<int>(out var version) && version == 1) return true;
            }
        }
        // A safe report is sufficient even when the game denies process queries.
        // Otherwise only an exited game makes unresolved callbacks irrelevant.
        return gameExited(observation.Pid);
    }

    private async Task Observe(string directory, string resultFile, int pid, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        if (pending.Count != 0) throw new InvalidOperationException("上一次原生观察器尚未确认退出，请先停止恢复。");
        // StartInstallation throws synchronously only if starting the process
        // failed. Once started, retain its completion even if waiting fails.
        var installation = startInstaller(directory);
        pending.Add(directory, new(pid, resultFile, installation));
        CarrierLoader.EnsureSuccess(directory, await installation.WaitAsync(TimeSpan.FromSeconds(45), cancellation));
        var path = Path.Combine(directory, resultFile); var deadline = Environment.TickCount64 + 30000;
        while (!File.Exists(path))
        {
            if (Environment.TickCount64 > deadline) throw new TimeoutException("等待本次原生图观察结果超时：" + directory);
            await Task.Delay(100, cancellation);
        }
        if (!InstallerDisarmed(CarrierLoader.ReadEvidence(Path.Combine(directory, "installer_result.json"))) || !ObserverRestored(directory, resultFile))
            throw new InvalidOperationException("原生观察器结果未确认挂钩恢复与回调退出；保留恢复记录以便重试。");
        pending.Remove(directory);
    }

    private async Task<T> WithObservationAsync<T>(Func<CancellationToken, Task<T>> observe, CancellationToken cancellation)
    {
        await operation.WaitAsync(cancellation);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        activeObservation = linked;
        try { return await observe(linked.Token); }
        finally { activeObservation = null; operation.Release(); }
    }

    // Exercise the same ownership/serialization boundary without preparing
    // game-specific graph inputs in portable lifecycle checks.
    internal Task ObserveCarrierAsync(string directory, string resultFile, int pid, CancellationToken cancellation = default) =>
        WithObservationAsync(async token => { await Observe(directory, resultFile, pid, token); return true; }, cancellation);

    public Task<JsonObject> ObserveAsync(string targetConfigPath, string outputDirectory, int pid, CancellationToken cancellation) =>
        WithObservationAsync(async token =>
        {
            Directory.CreateDirectory(outputDirectory);
            var registration = Path.Combine(outputDirectory, "identity");
            graph.PrepareIdentity(targetConfigPath, registration, pid);
            await Observe(registration, "native_identity_bootstrap.json", pid, token);
            var promotedPath = Path.Combine(outputDirectory, "promoted_target.json");
            JsonData.Write(promotedPath, NativeGraphService.PromoteTargetIdentity(targetConfigPath, registration));
            var discovery = Path.Combine(outputDirectory, "graph");
            graph.PrepareDiscovery(promotedPath, discovery, pid);
            await Observe(discovery, "native_graph_discovery.json", pid, token);
            var playback = JsonData.ReadObject(Path.Combine(discovery, "observe_target.json"));
            graph.AttachPlaybackBinding(playback, graph.Bind(discovery), pid);
            File.Copy(Path.Combine(discovery, "native_graph_discovery.json"), Path.Combine(outputDirectory, "native_graph_discovery.json"));
            File.Copy(Path.Combine(discovery, "observe_target.json"), Path.Combine(outputDirectory, "native_graph_source_config.json"));
            JsonData.Write(Path.Combine(outputDirectory, "native_playback.json"), playback);
            return playback;
        }, cancellation);

    public async Task FinishAsync(CancellationToken cancellation = default)
    {
        try { activeObservation?.Cancel(); } catch (ObjectDisposedException) { }
        await operation.WaitAsync(cancellation);
        try
        {
            foreach (var (directory, observation) in pending.ToArray())
            {
                JsonData.AtomicWrite(Path.Combine(directory, "stop.flag"), []);
                var deadline = Environment.TickCount64 + (long)recoveryTimeout.TotalMilliseconds;
                try { await observation.InstallerExit.WaitAsync(recoveryTimeout, cancellation); }
                catch (Exception) when (observation.InstallerExit.IsFaulted && gameExited(observation.Pid))
                {
                    // A failed wait is not proof of process exit, but the dead
                    // game no longer has any actors or callbacks to retain.
                    pending.Remove(directory); continue;
                }
                catch (TimeoutException) { throw new TimeoutException("临时安装器尚未退出；保留本次原生观察器与角色以便重试停止恢复。"); }
                bool Restored() => RecoveryConfirmed(directory, observation);
                while (!Restored() && Environment.TickCount64 < deadline)
                    await Task.Delay(100, cancellation);
                if (!Restored())
                    throw new TimeoutException("原生观察器尚未确认挂钩恢复与回调退出；保留恢复记录以便重试停止恢复。");
                pending.Remove(directory);
            }
        }
        finally { operation.Release(); }
    }
}
