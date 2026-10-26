using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json.Nodes;
using WWPoser.Services;

internal static class NativeObserverRecoveryChecks
{
    private const string ResultFile = "native_identity_bootstrap.json";
    private static readonly TimeSpan ShortBudget = TimeSpan.FromMilliseconds(150);

    private static JsonObject Installer(string stage = "carrier_seen", bool loaded = true, bool disarmed = true, int protocol = 1) =>
        new() { ["stage"] = stage, ["loaded"] = loaded, ["disarmed"] = disarmed, ["observer_early_stop_protocol"] = protocol };
    private static JsonObject Observer(bool restored = true, bool quiet = true, string status = "complete") =>
        new() { ["status"] = status, ["all_hooks_restored"] = restored, ["callbacks_quiescent"] = quiet };
    private static void SaveInstaller(string directory, JsonObject value) => JsonData.Write(Path.Combine(directory, "installer_result.json"), value);
    private static void SaveObserver(string directory, JsonObject value) => JsonData.Write(Path.Combine(directory, ResultFile), value);
    private static void Marker(string directory) => JsonData.Write(Path.Combine(directory, "logs", "carrier_status.json"), new JsonObject { ["status"] = "starting" });
    private static async Task Until(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!condition()) await Task.Delay(10, timeout.Token);
    }

    // A real, harmless child process exercises the same Process-owning completion
    // used by the elevated installer. No Windows, game, or hook API is invoked.
    internal static async Task<int> RunInstallerFixtureAsync(string directory)
    {
        SaveInstaller(directory, Installer("armed", false, false));
        File.WriteAllText(Path.Combine(directory, "installer-started.flag"), "");
        await Until(() => File.Exists(Path.Combine(directory, "installer-release.flag")));
        SaveInstaller(directory, JsonData.ReadObject(Path.Combine(directory, "fixture-final-report.json")));
        return 1;
    }
    private static Task<int> StartInstallerFixture(string directory)
    {
        var executable = Environment.ProcessPath ?? throw new InvalidOperationException("Missing test process path.");
        var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true };
        if (Path.GetFileNameWithoutExtension(executable).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
            start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        start.ArgumentList.Add("--observer-installer-fixture"); start.ArgumentList.Add(directory);
        return CarrierLoader.WaitForInstallationExitAsync(Process.Start(start)!);
    }

    internal static async Task<int> RunAsync(string fixture)
    {
        int checks = 0;
        void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
        async Task Fails<T>(Task task, string message) where T : Exception
        {
            try { await task.WaitAsync(TimeSpan.FromSeconds(10)); }
            catch (T) { checks++; return; }
            throw new Exception(message);
        }
        string Folder(string name)
        {
            var directory = Path.Combine(fixture, "observer-" + name);
            Directory.CreateDirectory(Path.Combine(directory, "logs")); return directory;
        }

        var terminal = Folder("installer-evidence");
        foreach (var stage in new[] { "load_library_failed", "hook_failed", "hook_export_missing", "cancelled_before_hook" })
        {
            SaveInstaller(terminal, Installer(stage, false));
            Check(CarrierLoader.NotLoaded(terminal), "failure before hooking proves no carrier can start: " + stage);
        }
        foreach (var value in new[] { Installer("armed", false, false), Installer("timeout", false), Installer("cancelled", false),
            Installer("hook_failed", false, false), new JsonObject { ["stage"] = "hook_failed", ["disarmed"] = true } })
        {
            SaveInstaller(terminal, value);
            Check(!CarrierLoader.NotLoaded(terminal), "generic not-loaded checks reject intermediate, delayed, or incomplete evidence");
        }
        File.WriteAllText(Path.Combine(terminal, "installer_result.json"), "{\"loaded\":false");
        Check(!CarrierLoader.NotLoaded(terminal), "partially written installer evidence cannot authorize cleanup");

        // Cancelling the actual installer wait retains its process completion.
        // Finish is bounded, never kills it, and can be retried once it exits.
        {
            var directory = Folder("real-process"); Task<int>? installerExit = null;
            var session = new NativeTargetSession(fixture, path => installerExit = StartInstallerFixture(path), _ => false, ShortBudget);
            using var cancel = new CancellationTokenSource();
            var observation = session.ObserveCarrierAsync(directory, ResultFile, 1, cancel.Token);
            try
            {
                await Until(() => File.Exists(Path.Combine(directory, "installer-started.flag")));
                cancel.Cancel();
                await Fails<OperationCanceledException>(observation, "cancelled installation wait must stop observing");
                await Fails<TimeoutException>(session.FinishAsync(), "an armed live installer cannot complete recovery");
                Check(installerExit is { IsCompleted: false }, "recovery timeout does not kill or discard the live installer");
                Check(File.Exists(Path.Combine(directory, "stop.flag")), "stop is published before waiting for installer exit");
                JsonData.Write(Path.Combine(directory, "fixture-final-report.json"), Installer("cancelled", false));
                File.WriteAllText(Path.Combine(directory, "installer-release.flag"), "");
                await installerExit!.WaitAsync(TimeSpan.FromSeconds(10));
                await session.FinishAsync(); checks++;
                SaveInstaller(directory, Installer("armed", false, false));
                await session.FinishAsync(); checks++;
            }
            finally
            {
                cancel.Cancel();
                JsonData.Write(Path.Combine(directory, "fixture-final-report.json"), Installer("cancelled", false));
                File.WriteAllText(Path.Combine(directory, "installer-release.flag"), "");
                if (installerExit is not null) await installerExit.WaitAsync(TimeSpan.FromSeconds(10));
            }
        }

        // Neither Observe's normal path nor Finish's error path may retire an
        // observer merely because its result file exists or says "complete".
        var unsafeResults = new[] {
            Observer(false, true), Observer(true, false), new JsonObject { ["status"] = "complete" },
            new JsonObject { ["status"] = "complete", ["all_hooks_restored"] = "true", ["callbacks_quiescent"] = true }
        };
        for (int i = 0; i < unsafeResults.Length; i++)
        {
            var directory = Folder("unsafe-result-" + i); SaveInstaller(directory, Installer()); Marker(directory); SaveObserver(directory, unsafeResults[i]);
            var session = new NativeTargetSession(fixture, _ => Task.FromResult(0), _ => false, ShortBudget);
            await Fails<InvalidOperationException>(session.ObserveCarrierAsync(directory, ResultFile, 1), "unsafe result retired during observation");
            await Fails<TimeoutException>(session.FinishAsync(), "unsafe result retired during recovery");
            await Fails<InvalidOperationException>(session.ObserveCarrierAsync(directory, ResultFile, 1), "pending ownership was lost after recovery timeout");
            SaveObserver(directory, Observer(status: i % 2 == 0 ? "error" : "partial"));
            await session.FinishAsync(); checks++;
        }
        foreach (var status in new[] { "error", "partial", "complete" })
        {
            var directory = Folder("safe-result-" + status); SaveInstaller(directory, Installer()); SaveObserver(directory, Observer(status: status));
            var session = new NativeTargetSession(fixture, _ => Task.FromResult(0), _ => false, ShortBudget);
            await session.ObserveCarrierAsync(directory, ResultFile, 1); await session.FinishAsync(); checks++;
        }
        {
            var directory = Folder("query-denied-after-cleanup"); SaveInstaller(directory, Installer()); SaveObserver(directory, Observer(status: "error"));
            var session = new NativeTargetSession(fixture, _ => Task.FromResult(1), _ => throw new Win32Exception(5), ShortBudget);
            await Fails<InvalidOperationException>(session.ObserveCarrierAsync(directory, ResultFile, 1), "failed installer unexpectedly accepted");
            await session.FinishAsync(); checks++;
        }

        // Old binaries and malformed capability reports cannot use the early
        // stop protocol. Existing markers/results always require restoration.
        var unsafeInstallers = new[] {
            Installer("timeout", false, protocol: 0), Installer("timeout", false, protocol: 2),
            new JsonObject { ["stage"] = "timeout", ["loaded"] = false, ["disarmed"] = true },
            new JsonObject { ["stage"] = "timeout", ["disarmed"] = true, ["observer_early_stop_protocol"] = 1 },
            Installer("timeout", false, false), Installer("armed", false, false), Installer("timeout", true)
        };
        for (int i = 0; i < unsafeInstallers.Length; i++)
        {
            var directory = Folder("unsafe-installer-" + i); SaveInstaller(directory, unsafeInstallers[i]);
            var session = new NativeTargetSession(fixture, _ => Task.FromResult(1), _ => false, ShortBudget);
            await Fails<InvalidOperationException>(session.ObserveCarrierAsync(directory, ResultFile, 1), "failed installer unexpectedly accepted");
            await Fails<TimeoutException>(session.FinishAsync(), "unproven installer state released pending ownership");
            SaveInstaller(directory, Installer()); SaveObserver(directory, Observer());
            await session.FinishAsync(); checks++;
        }
        foreach (var stage in new[] { "timeout", "cancelled", "load_library_failed", "hook_failed", "hook_export_missing", "cancelled_before_hook" })
        {
            var directory = Folder("safe-installer-" + stage); SaveInstaller(directory, Installer(stage, false));
            var session = new NativeTargetSession(fixture, _ => Task.FromResult(1), _ => false, ShortBudget);
            await Fails<InvalidOperationException>(session.ObserveCarrierAsync(directory, ResultFile, 1), "failed load unexpectedly succeeded");
            await session.FinishAsync(); checks++;
        }
        {
            var directory = Folder("late-marker"); var exited = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            SaveInstaller(directory, Installer("armed", false, false));
            var session = new NativeTargetSession(fixture, _ => exited.Task, _ => false, ShortBudget);
            var observation = session.ObserveCarrierAsync(directory, ResultFile, 1);
            var finish = session.FinishAsync();
            await Until(() => File.Exists(Path.Combine(directory, "stop.flag")));
            Marker(directory); SaveInstaller(directory, Installer("cancelled", false)); exited.SetResult(1);
            await Fails<OperationCanceledException>(observation, "Finish did not cancel the active observation");
            await Fails<TimeoutException>(finish, "marker appearing before installer exit was overlooked");
            SaveObserver(directory, new JsonObject { ["status"] = "error" });
            await Fails<TimeoutException>(session.FinishAsync(), "late marker with incomplete result was accepted");
            SaveObserver(directory, Observer(status: "error"));
            await session.FinishAsync(); checks++;
        }
        {
            var directory = Folder("unsafe-result-without-marker"); SaveInstaller(directory, Installer("cancelled", false));
            SaveObserver(directory, Observer(false, false));
            var session = new NativeTargetSession(fixture, _ => Task.FromResult(1), _ => false, ShortBudget);
            await Fails<InvalidOperationException>(session.ObserveCarrierAsync(directory, ResultFile, 1), "failed load unexpectedly succeeded");
            await Fails<TimeoutException>(session.FinishAsync(), "early-stop capability bypassed an explicitly unsafe result");
            SaveObserver(directory, Observer()); await session.FinishAsync(); checks++;
        }
        {
            var directory = Folder("failed-wait");
            bool gameExited = false;
            var session = new NativeTargetSession(fixture, _ => Task.FromException<int>(new Win32Exception(5)), _ => gameExited, ShortBudget);
            await Fails<Win32Exception>(session.ObserveCarrierAsync(directory, ResultFile, 1), "installer wait error was swallowed");
            await Fails<Win32Exception>(session.FinishAsync(), "failed installer wait lost its recovery ownership");
            await Fails<InvalidOperationException>(session.ObserveCarrierAsync(directory, ResultFile, 1), "wait failure was confused with a launch failure");
            gameExited = true; await session.FinishAsync(); checks++;
            var neverStarted = new NativeTargetSession(fixture, _ => throw new Win32Exception(1223), _ => false, ShortBudget);
            await Fails<Win32Exception>(neverStarted.ObserveCarrierAsync(directory, ResultFile, 1), "launch failure was swallowed");
            await neverStarted.FinishAsync(); checks++;
        }
        Console.WriteLine($"PASS {checks} native observer recovery checks, including a real child-process cancellation; no game or hook APIs.");
        return checks;
    }
}
