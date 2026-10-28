using System.ComponentModel;

namespace WWPoser.Services;
public sealed partial class SinglePlaybackService
{
    private static readonly string CarrierStatus = Path.Combine("logs", "carrier_status.json");
    private readonly Dictionary<string, (int Pid, string Result)> observers = new(StringComparer.OrdinalIgnoreCase);
    public string? ActiveSession { get; private set; }

    public event Action<string>? Progress;

    // A fresh current-player observation supplies scene camera identity for an
    // independently owned actor; no addresses are retained in preparation data.
    public async Task<JsonObject> CaptureCameraAsync(string directory, string reportPath, CancellationToken cancellation = default)
    {
        var game = GameProcess.Current();
        var probes = Path.Combine(root, "probes", "v20_recon");
        var scan = Path.Combine(directory, "scan");
        Directory.CreateDirectory(Path.Combine(scan, "logs"));
        File.Copy(Path.Combine(probes, "hook_install_v20.exe"), Path.Combine(scan, "hook_install_v20.exe"));
        File.Copy(Path.Combine(probes, "recon_carrier_v20.dll"), Path.Combine(scan, "recon_carrier_v20.dll"));
        await ObserveAsync(scan, game.Pid, CarrierStatus, s => CarrierAccepted(s, "镜头角色读取失败"), cancellation, go: true);
        var logs = Path.Combine(scan, "logs");
        var cfg = Prepare(new(logs, reportPath, Path.Combine(directory, "identity"), IdentityContext: true, NativePhysics: false));
        var (_, identity) = Identify(logs);
        cfg["expected_process_creation_filetime"] = game.CreationFileTime;
        cfg["game_window_handle"] = game.WindowHandle;
        var accessories = Path.Combine(directory, "accessories");
        Directory.CreateDirectory(Path.Combine(accessories, "logs"));
        File.Copy(Path.Combine(probes, "hook_install_v20.exe"), Path.Combine(accessories, "hook_install_v20.exe"));
        File.Copy(Path.Combine(probes, "accessory_recon.dll"), Path.Combine(accessories, "recon_carrier_v20.dll"));
        JsonData.Write(Path.Combine(accessories, "observe_target.json"), cfg);
        await ObserveAsync(accessories, game.Pid, CarrierStatus, s => CarrierAccepted(s, "镜头读取失败"), cancellation);
        return new JsonObject { ["camera_observation"] = JsonData.ReadObject(Path.Combine(accessories, "camera_readonly.json")), ["controller_address"] = cfg["controller_address"]!.DeepClone(), ["camera_controller_address"] = cfg["controller_address"]!.DeepClone(), ["camera_pawn_address"] = identity["pawn_address"]!.DeepClone() };
    }

    private static async Task<JsonObject> WaitJsonAsync(string path, Func<JsonObject, bool> accepted, int seconds, CancellationToken cancellation)
    {
        var deadline = Environment.TickCount64 + seconds * 1000;
        while (Environment.TickCount64 < deadline)
        {
            if (File.Exists(path) && JsonData.ReadObject(path) is var value && accepted(value))
                return value;
            await Task.Delay(100, cancellation);
        }

        throw new TimeoutException("等待本次结果超时：" + path);
    }

    private static bool ObserverDone(string directory, string result) => File.Exists(Path.Combine(directory, result)) && (result != CarrierStatus || Contract.S(JsonData.ReadObject(Path.Combine(directory, result)), "status") is "finished" or "error");

    private async Task<JsonObject> ObserveAsync(string directory, int pid, string result, Func<JsonObject, bool> accepted, CancellationToken cancellation, bool go = false)
    {
        observers[directory] = (pid, result);
        try { await CarrierLoader.LoadAsync(directory, cancellation); }
        catch (Win32Exception) { observers.Remove(directory); throw; }
        if (go)
            JsonData.AtomicWrite(Path.Combine(directory, "logs", "recon_go.flag"), []);
        var value = await WaitJsonAsync(Path.Combine(directory, result), accepted, 60, cancellation);
        observers.Remove(directory);
        return value;
    }

    private static bool CarrierAccepted(JsonObject status, string failure)
    {
        Contract.Require(Contract.S(status, "status") != "error", failure);
        return Contract.S(status, "status") == "finished";
    }

    public async Task StopPendingAsync(CancellationToken cancellation = default)
    {
        foreach (var(directory, (pid, result))in observers.ToArray())
        {
            JsonData.AtomicWrite(Path.Combine(directory, "stop.flag"), []);
            if (Directory.Exists(Path.Combine(directory, "logs")))
                JsonData.AtomicWrite(Path.Combine(directory, "logs", "recon_stop.flag"), []);
            var deadline = Environment.TickCount64 + 15000;
            while (!ObserverDone(directory, result) && !CarrierLoader.NotLoaded(directory) && !GameProcess.Exited(pid) && Environment.TickCount64 < deadline)
                await Task.Delay(100, cancellation);

            observers.Remove(directory);
        }

        if (ActiveSession != null)
            await RecoveryService.FinishCurrentAsync(ActiveSession, cancellation);
        ActiveSession = null;
    }

    public async Task<JsonObject> LaunchAsync(SingleLaunchRequest request, CancellationToken cancellation = default)
    {
        var identity = GameProcess.Current();
        var bundle = Path.Combine(root, "playback", "sessions", "single_" + Guid.NewGuid().ToString("N"));
        var probes = Path.Combine(root, "probes", "v20_recon");
        try
        {
            Progress?.Invoke("读取当前角色与完整骨架");
            var scan = Path.Combine(bundle, "scan");
            Directory.CreateDirectory(Path.Combine(scan, "logs"));
            File.Copy(Path.Combine(probes, "hook_install_v20.exe"), Path.Combine(scan, "hook_install_v20.exe"));
            File.Copy(Path.Combine(probes, "recon_carrier_v20.dll"), Path.Combine(scan, "recon_carrier_v20.dll"));
            await ObserveAsync(scan, identity.Pid, CarrierStatus, s => CarrierAccepted(s, "当前角色读取失败"), cancellation, go: true);
            var scanLogs = Path.Combine(scan, "logs");
            var(profile, identified) = Identify(scanLogs);
            Progress?.Invoke("按当前角色编译动作与表情");
            var imported = await importer.ImportAsync(new(request.BodySource, registry.SkeletonPath(profile), Path.Combine(bundle, "adapted_motion"), request.Scale, request.FaceSource, Contract.S(profile, "model_id"), Contract.S(profile, "profile_id"), Contract.S(identified, "mesh_asset_name"), Path.Combine(root, "playback", "character_cache_csharp"), request.GazeSource, request.GazeStrength), cancellation);
            var report = Contract.S(imported, "report");
            var expressionProfile = Contract.True(imported, "expression_enabled") ? Contract.S(imported, "expression_profile") : null;
            var expressionClip = Contract.True(imported, "expression_enabled") ? Contract.S(imported, "expression_clip") : null;
            string? cameraProfile = null, cameraClip = null;
            if (request.CameraSource != null)
            {
                cameraProfile = Path.Combine(bundle, "camera_profile.json");
                cameraClip = Path.Combine(bundle, "camera.wwcam");
                (await ProcessRunner.RunAsync(Path.Combine(root, "tools", "mmd_outputs", "mmd_camera_compile.exe"), [request.CameraSource, cameraProfile, cameraClip], root, cancellation)).EnsureSuccess("镜头编译失败");
            }

            var bootstrap = Path.Combine(bundle, "identity_preparation");
            var cfg = Prepare(new(scanLogs, report, bootstrap, IdentityContext: true, NativePhysics: false));
            var configPath = Path.Combine(bootstrap, "observe_target.json");
            cfg["expected_process_creation_filetime"] = identity.CreationFileTime;
            cfg["game_window_handle"] = identity.WindowHandle;
            Progress?.Invoke("读取当前武器与镜头");
            var accessories = Path.Combine(bundle, "accessories");
            Directory.CreateDirectory(Path.Combine(accessories, "logs"));
            File.Copy(Path.Combine(probes, "hook_install_v20.exe"), Path.Combine(accessories, "hook_install_v20.exe"));
            File.Copy(Path.Combine(probes, "accessory_recon.dll"), Path.Combine(accessories, "recon_carrier_v20.dll"));
            JsonData.Write(Path.Combine(accessories, "observe_target.json"), cfg);
            await ObserveAsync(accessories, identity.Pid, CarrierStatus, s => CarrierAccepted(s, "附属组件读取失败"), cancellation);
            string? graphDirectory = null;
            if (request.NativePhysics)
            {
                Progress?.Invoke("登记引擎动画实例身份并观测原生物理图");
                var registration = Path.Combine(bundle, "native_identity");
                native.PrepareIdentity(configPath, registration, identity.Pid);
                await ObserveAsync(registration, identity.Pid, "native_identity_bootstrap.json", _ => true, cancellation);
                graphDirectory = Path.Combine(bundle, "native_graph");
                native.PrepareDiscovery(configPath, graphDirectory, identity.Pid);
                await ObserveAsync(graphDirectory, identity.Pid, "native_graph_discovery.json", _ => true, cancellation);
            }

            Progress?.Invoke("冻结本次播放会话");
            var session = Path.Combine(bundle, "player");
            var frozen = Prepare(new(scanLogs, report, session, WeaponTargets: WeaponSelection.Select(accessories, profile), ExpressionProfile: expressionProfile, ExpressionClip: expressionClip, FreezeNativeAnimationTime: request.FreezeNativeAnimationTime, NativePhysics: request.NativePhysics, NativeGraphDirectory: graphDirectory, CameraProfile: cameraProfile, CameraClip: cameraClip, CameraManifest: Path.Combine(accessories, "camera_readonly.json"), DurationMs: request.DurationMs));
            frozen["physics_enabled"] = request.Physics && !request.NativePhysics;
            frozen["physics_mode"] = request.NativePhysics ? "native" : request.Physics ? "secondary_inertia" : "direct";
            frozen["camera_mode"] = request.CameraMode;
            frozen["expected_process_creation_filetime"] = identity.CreationFileTime;
            frozen["game_window_handle"] = identity.WindowHandle;
            JsonData.Write(Path.Combine(session, "observe_target.json"), frozen);
            ActiveSession = session;
            try { await CarrierLoader.LoadAsync(session, cancellation); }
            catch (Win32Exception) { ActiveSession = null; throw; }
            var state = await WaitJsonAsync(Path.Combine(session, "player_status.json"), s =>
            {
                Contract.Require(Contract.True(s, "session_active"), "本次播放器已退出");
                return Contract.S(s, "mode") is "stopped" or "paused" or "playing" or "ended";
            }, 45, cancellation);
            var epoch = Contract.N(state, "clip_epoch").GetValue<ulong>();
            foreach (var(kind, value)in new[] { ("offset_x", request.OffsetX), ("offset_y", request.OffsetY), ("height", request.Height), ("yaw", request.Yaw), ("in_place", request.InPlace ? 1d : 0d), ("speed", request.Speed), ("loop", request.Loop ? 1d : 0d) })
                await PlaybackControl.SendAsync(session, kind, value, cancellation);
            if (request.Physics)
            {
                Contract.Require(Contract.True(state, "physics_available"), "当前角色衣物增强不可用");
                await PlaybackControl.SendAsync(session, "physics", 1, cancellation);
            }

            if (request.CameraMode > 0)
                await PlaybackControl.SendAsync(session, "camera", request.CameraMode, cancellation);
            await PlaybackControl.SendAsync(session, "play", 0, cancellation);
            Progress?.Invoke("已开始播放，等待画面验收");
            return new JsonObject
            {
                ["state"] = "ready",
                ["session_dir"] = session,
                ["clip_epoch"] = epoch,
                ["character_profile_id"] = profile["profile_id"]!.DeepClone()
            };
        }
        catch
        {
            foreach (var directory in observers.Keys)
            {
                JsonData.AtomicWrite(Path.Combine(directory, "stop.flag"), []);
                if (Directory.Exists(Path.Combine(directory, "logs")))
                    JsonData.AtomicWrite(Path.Combine(directory, "logs", "recon_stop.flag"), []);
            }

            if (ActiveSession != null)
                JsonData.AtomicWrite(Path.Combine(ActiveSession, "stop.flag"), []);
            throw;
        }
    }
}
