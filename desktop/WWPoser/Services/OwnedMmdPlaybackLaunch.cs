using System.Diagnostics;
using System.Globalization;

namespace WWPoser.Services;

public sealed partial class OwnedMmdPlaybackService
{
    private readonly NativeTargetSession nativeTarget = new(repoRoot);
    public const string HelperFilename = "WwPoser.ColdLoad.ManagedBridge.OwnedMmdLite.dll";
    private static readonly string[] RuntimeFiles = ["ownedmmd_lite_observer.dll", "ownedmmd_lite_installer.exe", HelperFilename];
    private static readonly string[] SessionOnlyKeys = ["camera_observation", "controller_address", "camera_controller_address", "camera_pawn_address", "native_graph_binding", "animation_target"];
    private string NativePackageDirectory => Directory.Exists(Path.Combine(root, "runtime", "owned_mmd_lite")) ? Path.Combine(root, "runtime", "owned_mmd_lite") : Path.Combine(root, "build", "owned_mmd");
    private Process? installer;
    public string? ActiveSession { get; private set; }
    public string LaunchStage { get; private set; } = "not_started";
    public event Action<string>? Progress;

    // Copies the runtime into a session directory and records the target game.
    private void StageRuntime(string directory, GameIdentity game)
    {
        foreach (var filename in RuntimeFiles) File.Copy(Path.Combine(NativePackageDirectory, filename), Path.Combine(directory, filename));
        JsonData.Write(Path.Combine(directory, "session_config.json"), new JsonObject
        {
            ["session_id"] = Guid.NewGuid().ToString("N"), ["expected_pid"] = game.Pid, ["window_handle"] = game.WindowHandle, ["window_thread_id"] = game.WindowThread
        });
    }
    private async Task<Process> StartInstallerAsync(string directory, GameIdentity game, string message, CancellationToken cancellation)
    {
        await GameProcess.WaitForForegroundAsync(game, cancellation);
        Progress?.Invoke(message);
        var info = new ProcessStartInfo(Path.Combine(directory, "ownedmmd_lite_installer.exe")) { WorkingDirectory = directory, UseShellExecute = true, Verb = "runas", WindowStyle = ProcessWindowStyle.Hidden };
        info.ArgumentList.Add("--run-session"); info.ArgumentList.Add(directory);
        LaunchStage = "owned_installer_launch";
        var process = Process.Start(info)!;
        ActiveSession = directory;
        return process;
    }

    private string PrepareRuntime(string prepared, GameIdentity game, bool startPaused)
    {
        var config = Config(prepared);
        config["session_epoch"] = NewEpoch();
        config["start_paused"] = startPaused;
        var catalog = CharacterCatalog(); var registry = new CharacterRegistry(root);
        foreach (var actor in ActorConfigs(config))
        {
            var selected = catalog.Select(Contract.S(actor, "character_id"));
            Contract.Require(Contract.S(actor, "mesh_asset_name") == selected.ModelAssetName && Contract.S(actor, "profile_id") == selected.ProfileId
                && Contract.I(actor, "role_id") == selected.RoleId && Contract.I(actor, "skin_id") == selected.SkinId
                && Contract.S(actor, "character_profile_sha256") == JsonData.Digest(registry.ProfilePath(registry.GetProfile(selected.ProfileId))), "角色形态或适配数据已更改，请重新准备动作。");
            actor["form"] = selected.Form?.DeepClone(); actor["independent_instance"] = selected.IndependentInstance;
            actor.Remove("gravity_parts");
            if (actor["native_physics"]?.GetValue<bool>() ?? true)
            {
                var parts = PartGravityCatalog.Build(root, selected);
                if (parts.Count != 0) actor["gravity_parts"] = parts;
            }
        }
        foreach (var key in SessionOnlyKeys) config.Remove(key);
        var directory = Path.Combine(prepared, "runtime_sessions", "actors_" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        StageRuntime(directory, game);
        foreach (var actor in ActorConfigs(config))
        {
            var relative = "actors/" + actor["slot"]!.GetValue<uint>().ToString(CultureInfo.InvariantCulture);
            var source = Path.Combine(prepared, Contract.S(actor, "actor_directory"));
            var destination = Path.Combine(directory, relative); Directory.CreateDirectory(destination);
            actor["actor_directory"] = relative; actor["session_epoch"] = config["session_epoch"]!.DeepClone();
            foreach (var key in SessionOnlyKeys) actor.Remove(key);
            PlaybackAssets.CopyFiles(source, destination, PlaybackAssets.ActorFiles);
            CompiledMotionValidation.Validate(destination, actor);
            JsonData.Write(Path.Combine(destination, "owned_mmd_config.json"), actor);
        }
        PlaybackAssets.CopyFiles(prepared, directory, PlaybackAssets.SceneFiles);
        JsonData.Write(Path.Combine(directory, "owned_mmd_config.json"), config);
        WriteActorsFile(directory, config);
        return directory;
    }

    public static JsonObject Status(string directory)
    {
        foreach (var name in new[] { "mmd_result.json", "mmd_status.json" })
            if (File.Exists(Path.Combine(directory, name))) return JsonData.ReadObject(Path.Combine(directory, name));
        return new JsonObject { ["ready"] = false, ["state"] = "waiting_for_owned_instance" };
    }

    public static bool Finished(string directory) => File.Exists(Path.Combine(directory, "session_result.json"));

    internal static string? StartupFailure(JsonObject? terminal)
    {
        if (CapsuleStartupFailure(terminal?["worker_result"]?["mmd_result"]) is { } capsuleFailure)
            return capsuleFailure;
        var reason = terminal?["reason"]?.GetValue<string>();
        if (reason != "selected window thread is not registered GameThread") return reason;
        return terminal?["worker_result"]?["game_thread_check"]?["status"]?.GetValue<string>() switch
        {
            "read_failed" => "无法读取当前兼容配置的游戏线程登记字段",
            "registration_flag_mismatch" => "当前兼容配置读取到的游戏线程登记标记不符合预期",
            "thread_id_mismatch" => "当前兼容配置读取到的游戏线程 ID 与窗口线程 ID 不一致",
            _ => "无法确认当前鸣潮窗口线程是游戏线程"
        };
    }

    private static string? CapsuleStartupFailure(JsonNode? state)
    {
        if (state?["actors"] is not JsonArray actors) return null;
        foreach (var actor in actors.OfType<JsonObject>())
            if (actor["capsule_layout_error"]?.GetValue<string>() is { Length: > 0 } error)
                return $"演员 {actor["slot"]} 的预编辑胶囊未应用：{error}";
        return null;
    }

    public async Task<JsonObject> LaunchAsync(string prepared, CancellationToken cancellation = default, bool startPaused = false)
    {
        if (ActiveSession is not null) throw new InvalidOperationException("请先停止当前演员会话并等待恢复。");
        LaunchStage = "runtime_preparation";
        var game = GameProcess.Current();
        var directory = PrepareRuntime(Path.GetFullPath(prepared), game, startPaused);
        cancellation.ThrowIfCancellationRequested();
        installer = await StartInstallerAsync(directory, game, "正在加载演员：优先使用编队里的原角色，其余另行创建", cancellation);
        LaunchStage = "owned_target_export";
        var nativePrepared = false;
        long? initialPoseWait = null;
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
                    var terminal = File.Exists(terminalPath) ? JsonData.ReadObject(terminalPath) : null;
                    var reason = StartupFailure(terminal) ?? CapsuleStartupFailure(Status(directory));
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
                if (CapsuleStartupFailure(state) is { } layoutFailure) throw new InvalidDataException(layoutFailure);
                if (Contract.True(state, "failed")) throw new InvalidOperationException("演员播放器初始化失败，保留会话等待恢复");
                if (Contract.True(state, "ready") && Contract.True(state, "session_active"))
                {
                    if (startPaused && (Contract.S(state, "mode") != "paused" || Contract.D(state, "seconds") != 0))
                        throw new InvalidDataException("运行模块未停在动作起点；请停止恢复，并在升级后重启游戏。");
                    LaunchStage = startPaused ? "actors_paused" : "actors_playing";
                    return new JsonObject { ["session_dir"] = directory, ["loaded"] = true, ["state"] = LaunchStage, ["start_paused"] = startPaused, ["actor_count"] = ActorConfigs(Config(directory)).Count };
                }
                initialPoseWait = CheckInitialPoseReadyTimeout(startPaused, state, Environment.TickCount64, initialPoseWait);
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

    internal static long? CheckInitialPoseReadyTimeout(bool startPaused, JsonObject state, long now, long? waitingSince)
    {
        // Actor loading and native graph discovery may legitimately take longer.
        // Bound only the first-pose stage after the runtime starts writing.
        if (!startPaused || !Contract.True(state, "session_active") || Contract.True(state, "ready")) return null;
        waitingSince ??= now;
        if (now - waitingSince.Value >= 15000)
            throw new TimeoutException("演员或镜头的首姿态未能在 15 秒内就绪，正在停止并恢复；请等待恢复完成后重试。");
        return waitingSince;
    }

    private static void RequestStop(string directory) => JsonData.AtomicWrite(Path.Combine(directory, "stop.flag"), []);
    public sealed record NativeTargetRoute(uint Slot, string TargetConfigPath, string OutputDirectory);
    public static IReadOnlyList<NativeTargetRoute> NativeTargetRoutes(JsonObject config, string directory) => ActorConfigs(config)
        .Where(actor => Contract.True(actor, "enabled") && (actor["native_physics"]?.GetValue<bool>() ?? true))
        .Select(actor => actor["slot"]!.GetValue<uint>())
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
