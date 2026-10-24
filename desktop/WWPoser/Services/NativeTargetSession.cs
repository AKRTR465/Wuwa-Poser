using System.ComponentModel;

namespace WWPoser.Services;

// The owning carrier retains its actors while these bounded observers run.
// A cancelled observation keeps recovery ownership until its observer has
// written its final result; the owner then receives an explicit completion signal.
public sealed class NativeTargetSession(string repoRoot)
{
    private readonly NativeGraphService graph = new(repoRoot);
    private readonly Dictionary<string, (int Pid, string ResultFile)> pending = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim operation = new(1, 1);
    private CancellationTokenSource? activeObservation;

    private async Task Observe(string directory, string resultFile, int pid, CancellationToken cancellation)
    {
        pending[directory] = (pid, resultFile);
        try { await CarrierLoader.LoadAsync(directory, cancellation); }
        catch (Win32Exception) { pending.Remove(directory); throw; }
        var path = Path.Combine(directory, resultFile); var deadline = Environment.TickCount64 + 30000;
        while (!File.Exists(path))
        {
            if (Environment.TickCount64 > deadline) throw new TimeoutException("等待本次原生图观察结果超时：" + directory);
            await Task.Delay(100, cancellation);
        }
        pending.Remove(directory);
    }

    public async Task<JsonObject> ObserveAsync(string targetConfigPath, string outputDirectory, int pid, CancellationToken cancellation)
    {
        await operation.WaitAsync(cancellation);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        activeObservation = linked;
        try
        {
            Directory.CreateDirectory(outputDirectory);
            var registration = Path.Combine(outputDirectory, "identity");
            graph.PrepareIdentity(targetConfigPath, registration, pid);
            await Observe(registration, "native_identity_bootstrap.json", pid, linked.Token);
            var promotedPath = Path.Combine(outputDirectory, "promoted_target.json");
            JsonData.Write(promotedPath, NativeGraphService.PromoteTargetIdentity(targetConfigPath, registration));
            var discovery = Path.Combine(outputDirectory, "graph");
            graph.PrepareDiscovery(promotedPath, discovery, pid);
            await Observe(discovery, "native_graph_discovery.json", pid, linked.Token);
            var playback = JsonData.ReadObject(Path.Combine(discovery, "observe_target.json"));
            graph.AttachPlaybackBinding(playback, graph.Bind(discovery), pid);
            File.Copy(Path.Combine(discovery, "native_graph_discovery.json"), Path.Combine(outputDirectory, "native_graph_discovery.json"));
            File.Copy(Path.Combine(discovery, "observe_target.json"), Path.Combine(outputDirectory, "native_graph_source_config.json"));
            JsonData.Write(Path.Combine(outputDirectory, "native_playback.json"), playback);
            return playback;
        }
        finally { activeObservation = null; operation.Release(); }
    }

    public async Task FinishAsync(CancellationToken cancellation = default)
    {
        try { activeObservation?.Cancel(); } catch (ObjectDisposedException) { }
        await operation.WaitAsync(cancellation);
        try
        {
            foreach (var (directory, (pid, resultFile)) in pending.ToArray())
            {
                JsonData.AtomicWrite(Path.Combine(directory, "stop.flag"), []);
                var deadline = Environment.TickCount64 + 20000;
                while (!File.Exists(Path.Combine(directory, resultFile)) && !CarrierLoader.NotLoaded(directory) && !GameProcess.Exited(pid) && Environment.TickCount64 < deadline)
                    await Task.Delay(100, cancellation);
                if (!File.Exists(Path.Combine(directory, resultFile)) && !CarrierLoader.NotLoaded(directory) && !GameProcess.Exited(pid))
                    throw new TimeoutException("原生观察器尚未确认退出；保留恢复记录以便重试。");
                pending.Remove(directory);
            }
        }
        finally { operation.Release(); }
    }
}
