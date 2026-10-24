using System.Diagnostics;
using System.Globalization;

namespace WWPoser.Services;

public sealed partial class OwnedMmdPlaybackService
{
    private readonly NativeTargetSession nativeTarget = new(repoRoot);
    public const string HelperFilename = "WwPoser.ColdLoad.ManagedBridge.OwnedMmdLite.dll";
    private static readonly string[] RuntimeFiles = ["ownedmmd_lite_observer.dll", "ownedmmd_lite_installer.exe", HelperFilename, "owned_mmd_layout.json"];
    private string NativePackageDirectory => Directory.Exists(Path.Combine(root, "runtime", "owned_mmd_lite")) ? Path.Combine(root, "runtime", "owned_mmd_lite") : Path.Combine(root, "build", "owned_mmd");
    private Process? installer;
    public string? ActiveSession { get; private set; }
    public string LaunchStage { get; private set; } = "not_started";
    public event Action<string>? Progress;

    private string PrepareRuntime(string prepared, GameIdentity game)
    {
        var config = Config(prepared);
        var legacy = config["actors"] is not JsonArray;
        if (legacy)
        {
            var actor = (JsonObject)config.DeepClone(); actor["slot"] = 1; actor["target_slot"] = 1; actor["actor_directory"] = "actors/1";
            config["actors"] = new JsonArray(actor); config["schema"] = 3; config["scope"] = "mixed_actor_mmd"; config["actor_count"] = 1; config["camera_anchor_slot"] = 1;
            ConfigureStage(config, 180);
        }
        config["session_epoch"] = NewEpoch();
        foreach (var key in new[] { "camera_observation", "controller_address", "camera_controller_address", "camera_pawn_address", "native_graph_binding", "animation_target" }) config.Remove(key);
        var directory = Path.Combine(prepared, "runtime_sessions", "actors_" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        foreach (var filename in RuntimeFiles) File.Copy(Path.Combine(NativePackageDirectory, filename), Path.Combine(directory, filename));
        foreach (var actor in ActorConfigs(config))
        {
            var relative = "actors/" + actor["slot"]!.GetValue<uint>().ToString(CultureInfo.InvariantCulture);
            var source = legacy ? prepared : Path.Combine(prepared, Contract.S(actor, "actor_directory"));
            var destination = Path.Combine(directory, relative); Directory.CreateDirectory(destination);
            actor["actor_directory"] = relative; actor["session_epoch"] = config["session_epoch"]!.DeepClone();
            foreach (var key in new[] { "camera_observation", "controller_address", "camera_controller_address", "camera_pawn_address", "native_graph_binding", "animation_target" }) actor.Remove(key);
            foreach (var filename in new[] { "character_profile.json", "motion.wwlocal" }) File.Copy(Path.Combine(source, filename), Path.Combine(destination, filename));
            foreach (var filename in new[] { "source.vmd", "face_source.vmd", "character_skeleton.json", "compiled_report.json", "expression.wwexpr", "expression_profile.json", "bone_chains.json" })
                if (File.Exists(Path.Combine(source, filename))) File.Copy(Path.Combine(source, filename), Path.Combine(destination, filename));
            JsonData.Write(Path.Combine(destination, "owned_mmd_config.json"), actor);
        }
        foreach (var filename in new[] { "camera.wwcam", "camera_source.vmd", "camera_profile.json", "camera_runtime_contract.json", "bone_chains.json", "unified_session_request.json" })
            if (File.Exists(Path.Combine(prepared, filename))) File.Copy(Path.Combine(prepared, filename), Path.Combine(directory, filename));
        JsonData.Write(Path.Combine(directory, "owned_mmd_config.json"), config);
        WriteActorsFile(directory, config);
        JsonData.Write(Path.Combine(directory, "session_config.json"), new JsonObject
        {
            ["session_id"] = Guid.NewGuid().ToString("N"), ["expected_pid"] = game.Pid,
            ["window_handle"] = game.WindowHandle, ["window_thread_id"] = game.WindowThread, ["expected_process_creation_filetime"] = game.CreationFileTime
        });
        return directory;
    }

    public static JsonObject Status(string directory)
    {
        foreach (var name in new[] { "mmd_result.json", "mmd_status.json" })
            if (File.Exists(Path.Combine(directory, name))) return JsonData.ReadObject(Path.Combine(directory, name));
        if (!File.Exists(Path.Combine(directory, "owned_display_shown.json"))) return new JsonObject { ["ready"] = false, ["state"] = "waiting_for_owned_instance" };
        var shown = JsonData.ReadObject(Path.Combine(directory, "owned_display_shown.json"));
        shown["static_display"] = true; shown["ready"] = false; return shown;
    }

    public static bool Finished(string directory) => File.Exists(Path.Combine(directory, "session_result.json"));

    public async Task<JsonObject> LaunchAsync(string prepared, CancellationToken cancellation = default)
    {
        if (ActiveSession is not null) throw new InvalidOperationException("请先停止当前演员会话并等待恢复。");
        LaunchStage = "runtime_preparation";
        var game = GameProcess.Current();
        var directory = PrepareRuntime(Path.GetFullPath(prepared), game);
        cancellation.ThrowIfCancellationRequested();
        await GameProcess.WaitForForegroundAsync(game, cancellation);
        Progress?.Invoke("正在加载演员：优先使用编队里的原角色，其余另行创建");
        var info = new ProcessStartInfo(Path.Combine(directory, "ownedmmd_lite_installer.exe")) { WorkingDirectory = directory, UseShellExecute = true, Verb = "runas", WindowStyle = ProcessWindowStyle.Hidden };
        info.ArgumentList.Add("--run-session"); info.ArgumentList.Add(directory);
        LaunchStage = "owned_installer_launch";
        installer = Process.Start(info)!;
        ActiveSession = directory;
        LaunchStage = "owned_target_export";
        var nativePrepared = false;
        var routes = NativeTargetRoutes(Config(directory), directory);
        try
        {
            while (true)
            {
                if (installer.HasExited)
                {
                    installer.Dispose(); installer = null;
                    var restored = RestorationConfirmed(directory);
                    if (restored) ActiveSession = null;
                    var terminalPath = Path.Combine(directory, "session_result.json");
                    var reason = File.Exists(terminalPath) ? JsonData.ReadObject(terminalPath)["reason"]?.GetValue<string>() : null;
                    var message = restored ? "角色与动作已停止，恢复结果已确认" : "安装器已退出，尚未确认角色与动作恢复";
                    if (!string.IsNullOrWhiteSpace(reason)) message = "本次启动未完成：" + reason + (restored ? "；恢复已完成。" : "；请继续停止恢复。");
                    return new JsonObject { ["session_dir"] = directory, ["loaded"] = false, ["state"] = "terminal_record_available", ["cleanup_restored"] = restored, ["reason"] = reason, ["message"] = message };
                }
                if (routes.Count > 0 && !nativePrepared && File.Exists(Path.Combine(directory, "native_targets.json")))
                {
                    LaunchStage = "native_graph_observation";
                    JsonData.AtomicWrite(Path.Combine(directory, "native_observation_started.flag"), []);
                    var exported = JsonData.ReadObject(Path.Combine(directory, "native_targets.json"));
                    if (exported["session_epoch"]!.GetValue<ulong>() != Config(directory)["session_epoch"]!.GetValue<ulong>()) throw new InvalidDataException("演员目标属于其他播放会话。");
                    var exportedSlots = Contract.A(exported, "actors").Select(row => row!["slot"]!.GetValue<uint>()).ToArray();
                    if (exportedSlots.Length != routes.Count || !exportedSlots.Order().SequenceEqual(routes.Select(route => route.Slot).Order())) throw new InvalidDataException("原生演员目标集合不完整。");
                    for (var index = 0; index < routes.Count; index++)
                    {
                        var route = routes[index];
                        Progress?.Invoke($"观察演员 {index + 1} / {routes.Count} 的原生物理图");
                        await nativeTarget.ObserveAsync(route.TargetConfigPath, route.OutputDirectory, game.Pid, cancellation);
                    }
                    JsonData.AtomicWrite(Path.Combine(directory, "native_observers_restored.flag"), []);
                    JsonData.AtomicWrite(Path.Combine(directory, "native_inputs_ready.flag"), []);
                    nativePrepared = true;
                    LaunchStage = "native_body_warmup";
                }
                var state = Status(directory);
                if (Contract.True(state, "ready") && Contract.True(state, "session_active")) { LaunchStage = "actors_playing"; return new JsonObject { ["session_dir"] = directory, ["loaded"] = true, ["state"] = "actors_playing", ["actor_count"] = ActorConfigs(Config(directory)).Count }; }
                if (Contract.True(state, "failed")) throw new InvalidOperationException("演员播放器初始化失败，保留会话等待恢复");
                await Task.Delay(100, cancellation);
            }
        }
        catch
        {
            RequestStop(directory);
            await nativeTarget.FinishAsync(CancellationToken.None);
            JsonData.AtomicWrite(Path.Combine(directory, "native_observers_restored.flag"), []);
            throw;
        }
    }

    private static void RequestStop(string directory) => JsonData.AtomicWrite(Path.Combine(directory, "stop.flag"), []);
    public sealed record NativeTargetRoute(uint Slot, string TargetConfigPath, string OutputDirectory);
    public static IReadOnlyList<NativeTargetRoute> NativeTargetRoutes(JsonObject config, string directory) => ActorConfigs(config)
        .Where(actor => actor["enabled"]?.GetValue<bool>() ?? true)
        .Where(actor => actor["native_physics"]?.GetValue<bool>() ?? config["native_physics"]?.GetValue<bool>() ?? true)
        .Select(actor => actor["slot"]?.GetValue<uint>() ?? 1)
        .Select(slot => new NativeTargetRoute(slot, Path.Combine(directory, "native_targets", slot.ToString(CultureInfo.InvariantCulture), "native_target.json"),
            Path.Combine(directory, "native_inputs", slot.ToString(CultureInfo.InvariantCulture)))).ToArray();
    public static bool RestorationConfirmed(string directory)
    {
        if (!File.Exists(Path.Combine(directory, "session_result.json"))) return false;
        var terminal = JsonData.ReadObject(Path.Combine(directory, "session_result.json"));
        return Contract.True(terminal, "mmd_restoration_verified") && Contract.True(terminal, "managed_owner_retirement_checked");
    }
    public async Task FinishCurrentAsync(CancellationToken cancellation = default)
    {
        if (ActiveSession is not { } directory) return;
        RequestStop(directory);
        await nativeTarget.FinishAsync(cancellation);
        JsonData.AtomicWrite(Path.Combine(directory, "native_observers_restored.flag"), []);
        if (installer is not null)
        {
            try { await installer.WaitForExitAsync(cancellation).WaitAsync(TimeSpan.FromSeconds(45), cancellation); }
            catch (TimeoutException) { throw new TimeoutException("角色停止恢复尚未完成；保留本次会话以便重试。"); }
            installer.Dispose(); installer = null;
        }
        else
        {
            var deadline = Environment.TickCount64 + 45000;
            while (!RestorationConfirmed(directory) && Environment.TickCount64 < deadline) await Task.Delay(100, cancellation);
        }
        if (!RestorationConfirmed(directory)) throw new InvalidOperationException("角色或动作恢复尚未确认；保留本次会话以便重试。");
        ActiveSession = null;
    }

    public async Task FinishSessionAsync(string directory, CancellationToken cancellation = default)
    {
        directory = Path.GetFullPath(directory);
        if (ActiveSession is not null && !string.Equals(ActiveSession, directory, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("另一个演员会话仍待恢复。");
        if (RestorationConfirmed(directory)) { ActiveSession = null; return; }
        ActiveSession = directory;
        await FinishCurrentAsync(cancellation);
    }
}
