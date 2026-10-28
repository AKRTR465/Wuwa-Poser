using System.Diagnostics;

namespace WWPoser.Services;

public static class OwnedMmdCommandLine
{
    private static string Value(string[] args, string option)
    {
        var index = Array.IndexOf(args, option);
        if (index < 0 || index + 1 >= args.Length) throw new ArgumentException("缺少命令参数：" + option);
        return args[index + 1];
    }

    public static async Task<int> RunAsync(string repo, string[] args)
    {
        var prepared = Path.GetFullPath(Value(args, "--run-owned-mmd"));
        var resultPath = Path.GetFullPath(Value(args, "--result"));
        var config = OwnedMmdPlaybackService.Config(prepared);
        var report = new JsonObject
        {
            ["schema"] = 1, ["command"] = "run_owned_mmd", ["prepared_directory"] = prepared, ["started_at_utc"] = DateTimeOffset.UtcNow.ToString("O"),
            ["clip_duration_seconds"] = config["clip_duration_seconds"]!.DeepClone(), ["requested_duration_ms"] = config["duration_ms"]!.DeepClone(),
            ["speed"] = config["speed"]!.DeepClone(), ["loop"] = config["loop"]!.DeepClone()
        };
        var clock = Stopwatch.StartNew();
        var service = new PlaybackSessionService(repo);
        using var budget = Contract.I(config, "duration_ms") == 0 ? new CancellationTokenSource() : new CancellationTokenSource(TimeSpan.FromMilliseconds(Contract.I(config, "duration_ms")) + TimeSpan.FromMinutes(15));
        void Save(string state)
        {
            report["state"] = state; report["launch_stage"] = service.LaunchStage;
            report["elapsed_ms"] = clock.ElapsedMilliseconds; report["updated_at_utc"] = DateTimeOffset.UtcNow.ToString("O");
            JsonData.Write(resultPath, report);
        }

        try
        {
            Save("starting");
            var launchTask = service.LaunchAsync(prepared, budget.Token);
            while (!launchTask.IsCompleted) { await Task.WhenAny(launchTask, Task.Delay(500)); Save("starting"); }
            var launch = await launchTask;
            var session = launch["session_dir"]!.GetValue<string>();
            report["session_dir"] = session; report["launch_state"] = launch["state"]!.DeepClone();
            while (true)
            {
                var finished = OwnedMmdPlaybackService.Finished(session);
                if (finished) { Save("finishing"); await service.StopAsync(cancellation: budget.Token); }
                var state = OwnedMmdPlaybackService.Status(session);
                report["body_frames"] = (state["body_frames"] ?? state["frames"])?.DeepClone();
                foreach (var key in new[] { "failed", "native_input_frames", "native_input_error", "native_graph_binding_kind", "native_physics_group_count", "native_curve_group_count", "native_physics_activation_complete", "native_activation_observed_group_count", "native_secondary_motion_activation_runtime_observed", "native_secondary_motion_activation_status", "native_physics_groups", "native_curve_groups", "native_group_force_updates" })
                    report[key] = state[key]?.DeepClone();
                if (finished)
                {
                    var terminal = JsonData.ReadObject(Path.Combine(session, "session_result.json"));
                    var completed = terminal["status"]?.GetValue<string>() == "owned_mmd_retirement_recorded" && Contract.True(terminal, "display_interval_completed");
                    report["native_status"] = terminal["status"]?.DeepClone();
                    report["display_elapsed_ms"] = terminal["display_elapsed_ms"]?.DeepClone();
                    report["display_requested_ms"] = terminal["display_requested_ms"]?.DeepClone();
                    report["requested_interval_completed"] = completed;
                    Save(completed ? "completed" : "recovered");
                    return completed ? 0 : 2;
                }
                Save("active"); await Task.Delay(500, budget.Token);
            }
        }
        catch (Exception error)
        {
            report["error"] = error is OperationCanceledException ? "运行超过观察时限，已请求停止恢复。" : error.Message;
            report["error_stack"] = error.ToString();
            report["error_stage"] = service.LaunchStage;
            var recovered = false;
            if (service.ActiveSession is not null)
            {
                try { await service.StopAsync(); recovered = true; }
                catch (Exception cleanupError) { report["recovery_error"] = cleanupError.Message; }
            }
            Save(recovered ? "recovered" : "failed");
            return recovered ? 2 : 1;
        }
    }
}
