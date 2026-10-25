using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text.Json.Nodes;
using WWPoser.Services;

internal static class MediaLifecycleChecks
{
    internal static async Task<int> RunAsync(string fixture)
    {
        var checks = 0;
        void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
        static async Task Until(Func<bool> condition)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            while (!condition()) await Task.Delay(10, timeout.Token);
        }
        var timeline = Path.Combine(fixture, "music-recovery"); Directory.CreateDirectory(timeline);
        File.WriteAllText(Path.Combine(timeline, "player_status.json"), new JsonObject {
            ["clip_epoch"] = 17UL, ["last_sequence"] = 0UL, ["session_active"] = true, ["mode"] = "playing",
            ["seconds"] = 2d, ["duration_seconds"] = 10d, ["speed"] = 1d, ["loop"] = false, ["restore_requested"] = false }.ToJsonString());
        var good = Path.Combine(fixture, "good.wav"); var bad = Path.Combine(fixture, "unsupported.wav");
        var gate = new object(); bool deviceOpen = false, playing = false, disconnected = false; int goodOpens = 0, seeks = 0; long position = 2000;
        var audio = new MediaService(() => new MediaService.MciDevice(command => {
            lock (gate)
            {
                if (command.StartsWith("open "))
                {
                    if (command.Contains('"' + bad + '"')) throw new Win32Exception(277, "synthetic unsupported audio");
                    if (disconnected) throw new Win32Exception(263, "synthetic disconnected audio");
                    deviceOpen = true; playing = false; goodOpens++; return "";
                }
                if (command.StartsWith("close ")) { deviceOpen = false; playing = false; return ""; }
                if (!deviceOpen || disconnected) throw new Win32Exception(263, "synthetic lost alias");
                if (command.StartsWith("play ")) playing = true;
                if (command.StartsWith("pause ")) playing = false;
                if (command.StartsWith("seek ")) { position = long.Parse(command.Split(' ')[^1]); seeks++; }
                return command.EndsWith(" length") ? "10000" : command.EndsWith(" position") ? position.ToString() : command.EndsWith(" mode") ? playing ? "playing" : "paused" : "";
            }
        }));
        try
        {
            audio.UpdateContext(timeline, new MusicPreset(good)); await audio.StartSynchronizationAsync();
            await Until(() => { lock (gate) return goodOpens == 1 && playing; });
            var rewound = JsonData.ReadObject(Path.Combine(timeline, "player_status.json")); rewound["seconds"] = 0d;
            int seeksBeforeRewind; lock (gate) { position = 250; seeksBeforeRewind = seeks; }
            JsonData.Write(Path.Combine(timeline, "player_status.json"), rewound); audio.RequestResynchronization();
            await Until(() => { lock (gate) return seeks > seeksBeforeRewind && position == 0 && playing; });
            Check(goodOpens == 1, "restart forces music to zero even within the normal drift tolerance, without reopening the song");
            rewound["seconds"] = 2d; JsonData.Write(Path.Combine(timeline, "player_status.json"), rewound);
            lock (gate) position = 2000;
            audio.UpdateContext(timeline, new MusicPreset(bad));
            await Until(() => audio.LastError?.Contains("synthetic unsupported audio") == true);
            Check(audio.LastError is not null, "audio open failures are observable while the worker remains recoverable");
            audio.UpdateContext(timeline, new MusicPreset(good));
            await Until(() => { lock (gate) return goodOpens == 2 && playing && audio.LastError is null; });
            Check(true, "A -> failed B -> A reopens A instead of retaining a closed cached identity");
            lock (gate) disconnected = true;
            await Until(() => audio.LastError is not null);
            lock (gate) disconnected = false;
            await Until(() => { lock (gate) return goodOpens >= 3 && playing && audio.LastError is null; });
            Check(true, "a lost audio device can reopen the same selected file without restarting playback");
        }
        finally { await audio.StopAsync(); }

        static void Publish(RecordingService recorder, RecordingSnapshot value) =>
            typeof(RecordingService).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(recorder, [value]);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstFrame = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finalized = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        RecordingService recorder = null!;
        recorder = new RecordingService(fixture, async (_, _, _, stop) => {
            started.SetResult();
            try
            {
                await firstFrame.Task.WaitAsync(stop);
                Publish(recorder, new("recording", "synthetic first frame")); recorder.ConfirmCaptureReady();
                await Task.Delay(Timeout.Infinite, stop);
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
            stopped.SetResult(); await finalized.Task;
            Publish(recorder, new("saved", "synthetic saved"));
        });
        try
        {
            await recorder.StartAsync(fixture); await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var readiness = recorder.WaitForReadyAsync();
            Check(!readiness.IsCompleted, "starting the capture worker is not first-frame readiness");
            firstFrame.SetResult(); await readiness.WaitAsync(TimeSpan.FromSeconds(5));
            Check(recorder.IsBusy, "a real frame releases readiness while recording remains live");
            var save = recorder.StopAsync(); await stopped.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Check(!save.IsCompleted, "capture stop and video finalization are separate observable stages");
            finalized.SetResult(); await save;
            try { await recorder.WaitForReadyAsync(); throw new Exception("completed capture accepted as live"); }
            catch (IOException) { checks++; }
        }
        finally { firstFrame.TrySetResult(); finalized.TrySetResult(); await recorder.StopAsync(); }

        RecordingService failed = null!;
        failed = new RecordingService(fixture, (_, _, _, _) => { Publish(failed, new("error", "synthetic encoder failure")); return Task.CompletedTask; });
        await failed.StartAsync(fixture);
        try { await failed.WaitForReadyAsync().WaitAsync(TimeSpan.FromSeconds(5)); throw new Exception("failed capture accepted as ready"); }
        catch (IOException error) { Check(error.Message.Contains("synthetic encoder failure"), "startup failure completes readiness with a visible cause"); }
        await failed.StopAsync();
        RecordingService terminalOnly = null!;
        terminalOnly = new RecordingService(fixture, (_, _, _, _) => { Publish(terminalOnly, new("recording", "terminal packet only")); return Task.CompletedTask; });
        await terminalOnly.StartAsync(fixture);
        try { await terminalOnly.WaitForReadyAsync().WaitAsync(TimeSpan.FromSeconds(5)); throw new Exception("terminal progress accepted as first-frame readiness"); }
        catch (IOException) { checks++; }
        await terminalOnly.StopAsync();
        Console.WriteLine($"PASS {checks} audio recovery and recording readiness checks; no native audio or capture devices.");
        return checks;
    }
}
