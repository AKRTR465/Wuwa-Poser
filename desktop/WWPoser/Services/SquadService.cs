using System.ComponentModel;
using System.Diagnostics;

namespace WWPoser.Services;
public sealed record SquadPlacement(double OffsetX = 0, double OffsetY = 0, double Height = 0, double Yaw = 0, bool InPlace = true);
public sealed record SquadMemberSource(long RoleId, bool Enabled, string? Source, string? FaceSource = null, double? Scale = null, SquadPlacement? Placement = null);
public sealed record SquadPrepareRequest(string StageSession, IReadOnlyList<SquadMemberSource> Members, uint DurationMs = 0, double SpacingCm = 180, double Speed = 1, bool Loop = false, bool Autoplay = true, string? CameraSource = null, int CameraMode = 0, bool NativePhysics = true, bool Physics = false);
public sealed class SquadService(string repoRoot)
{
    private readonly string repo = Path.GetFullPath(repoRoot);
    private readonly CharacterRegistry registry = new(repoRoot);
    private readonly MotionImportService import = new(repoRoot);
    private readonly CSharpRosterService roster = new(repoRoot);
    private readonly NativeTargetSession nativeTargets = new(repoRoot);
    private static readonly string[] Modes = ["empty", "stopped", "playing", "paused", "ended"];
    private string? stageSession;
    public string? ActiveSession { get; private set; }
    public string? ActiveSampler => roster.ActiveSampler;

    public void Configure(string? savedStage) => stageSession = savedStage is null ? null : Path.GetFullPath(savedStage);

    public JsonObject ReadRoster()
    {
        Contract.Require(stageSession is not null, "尚未配置当前队伍来源目录");
        var cfg = JsonData.ReadObject(Path.Combine(stageSession!, "stage_config.json"));
        var profiles = Profiles(cfg);
        var members = new JsonArray();
        for (var i = 0; i < 3; i++)
        {
            var role = Contract.A(Contract.O(cfg, "scene_team_roster"), "members")[i]!;
            var name = Contract.S(profiles[i], "character") switch
            {
                "Aimisi" => "爱弥斯",
                "Daniya" => "达妮娅",
                "LinNai" => "琳奈",
                var other => other
            };
            members.Add(new JsonObject { ["group_role_index"] = role["group_role_index"]?.DeepClone(), ["role_id"] = role["role_id"]?.DeepClone(), ["character"] = name, ["profile_id"] = profiles[i]["profile_id"]?.DeepClone(), ["current"] = role["current"]?.DeepClone() });
        }

        return new JsonObject { ["stage_session"] = stageSession, ["roster"] = cfg["scene_team_roster"]?.DeepClone(), ["members"] = members };
    }

    private List<JsonObject> Profiles(JsonObject cfg) => Contract.A(Contract.O(cfg, "scene_team_preflight"), "members").Select(row => registry.Resolve(Contract.S(row!["mesh_asset"]!, "name"), Contract.S(row["skeleton"]!, "name"))).ToList();

    public async Task<JsonObject> RefreshRosterAsync(CancellationToken cancellation = default)
    {
        Contract.Require(stageSession is not null, "尚未配置当前队伍来源目录");
        stageSession = await roster.RefreshAsync(stageSession!, cancellation);
        return ReadRoster();
    }

    public Task StopSamplerAsync(CancellationToken cancellation = default) => roster.StopSamplerAsync(cancellation);

    private static JsonObject Placement(SquadPlacement? p)
    {
        p ??= new();
        return new() { ["offset_x_cm"] = p.OffsetX, ["offset_y_cm"] = p.OffsetY, ["height_cm"] = p.Height, ["yaw_degrees"] = p.Yaw, ["in_place"] = p.InPlace };
    }

    public async Task<JsonObject> PrepareAsync(SquadPrepareRequest request, CancellationToken cancellation = default)
    {
        var sourceStage = Path.GetFullPath(request.StageSession);
        var cfg = JsonData.ReadObject(Path.Combine(sourceStage, "stage_config.json"));
        var profiles = Profiles(cfg);
        var logical = Contract.A(Contract.O(cfg, "scene_team_roster"), "members");
        var natives = Contract.A(Contract.O(cfg, "scene_team_preflight"), "members");
        var sources = request.Members.ToDictionary(m => m.RoleId);
        var output = Path.Combine(repo, "playback/squad_sessions", "squad_" + DateTime.UtcNow.Ticks + "_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(output, "evidence", "stage", "evidence"));
        File.Copy(Path.Combine(sourceStage, "stage_config.json"), Path.Combine(output, "evidence", "stage", "stage_config.json"));
        File.Copy(Path.Combine(sourceStage, "evidence", "source_config.json"), Path.Combine(output, "evidence", "stage", "evidence", "source_config.json"));
        File.Copy(Path.Combine(repo, "probes/v20_recon/mmd_squad_residency.dll"), Path.Combine(output, "recon_carrier_v20.dll"));
        File.Copy(Path.Combine(repo, "probes/v20_recon/hook_install_v20.exe"), Path.Combine(output, "hook_install_v20.exe"));
        var jobs = new Dictionary<long, Task<JsonObject>>();
        for (var i = 0; i < 3; i++)
        {
            var member = sources[Contract.I(logical[i]!, "role_id")];
            if (member.Enabled)
                jobs[member.RoleId] = import.ImportAsync(new MotionImportRequest(member.Source!, registry.SkeletonPath(profiles[i]), Path.Combine(output, "members", member.RoleId.ToString()), member.Scale, member.FaceSource, Contract.S(profiles[i], "model_id"), registry.ProfilePath(profiles[i]), Contract.S(natives[i]!["mesh_asset"]!, "name"), Path.Combine(repo, "playback/character_cache")), cancellation);
        }

        await Task.WhenAll(jobs.Values);
        var members = new JsonArray();
        double maximum = 0;
        JsonObject? anchorClip = null;
        var assets = new PlaybackAssets(repo);
        for (var i = 0; i < 3; i++)
        {
            var p = profiles[i];
            var role = Contract.I(logical[i]!, "role_id");
            var selected = sources[role];
            var member = new JsonObject
            {
                ["group_role_index"] = logical[i]!["group_role_index"]?.DeepClone(),
                ["role_id"] = role,
                ["entity_id"] = logical[i]!["entity_id"]?.DeepClone(),
                ["current"] = logical[i]!["current"]?.DeepClone(),
                ["profile_id"] = p["profile_id"]?.DeepClone(),
                ["character"] = p["character"]?.DeepClone(),
                ["enabled"] = selected.Enabled,
                ["mesh_asset_name"] = natives[i]!["mesh_asset"]!["name"]?.DeepClone(),
                ["skeleton_sha256"] = p["skeleton_sha256"]?.DeepClone(),
                ["bone_count"] = p["bone_count"]?.DeepClone(),
                ["bone_names"] = p["bone_names"]?.DeepClone(),
                ["parents"] = p["bone_parents"]?.DeepClone(),
                ["placement"] = Placement(selected.Placement),
                ["target_slot"] = i + 1,
                ["native_physics"] = request.NativePhysics,
                ["physics_enabled"] = request.Physics && !request.NativePhysics,
                ["physics_mode"] = request.NativePhysics ? "native" : request.Physics ? "secondary_inertia" : "direct",
                ["physics_model_id"] = p["physics_model_id"]?.DeepClone(),
                ["bone_chains_file"] = "bone_chains.json",
                ["expression_enabled"] = false
            };
            if (!selected.Enabled)
            {
                member["duration_seconds"] = 0.0;
                members.Add(member);
                continue;
            }

            var report = JsonData.ReadObject(Contract.S(jobs[role].Result, "report"));
            var local = Contract.O(report, "compiled_target_clip");
            anchorClip ??= local;
            var prefix = "members/" + role + "/";
            var profilePath = Path.Combine(output, "members", role.ToString(), "character_profile.json");
            File.Copy(registry.ProfilePath(p), profilePath);
            member["local_clip_file"] = prefix + "motion.wwlocal";
            member["character_profile_file"] = prefix + "character_profile.json";
            member["character_profile_sha256"] = JsonData.Digest(profilePath);
            foreach (var(key, sourceKey)in new[] { ("local_clip_sha256", "sha256"), ("fps", "fps"), ("frame_count", "frame_count"), ("duration_seconds", "duration_seconds") })
                member[key] = local[sourceKey]?.DeepClone();
            member["vmd_source_sha256"] = report["source_sha256"]?.DeepClone();
            assets.Expression(member, jobs[role].Result, prefix);
            member["target_slot"] = i + 1;
            foreach (var key in new[] { "canonical_position_mask", "canonical_local_positions", "canonical_rotation_mask", "canonical_local_rotations" })
                if (local.ContainsKey(key))
                    member[key] = local[key]!.DeepClone();
            maximum = Math.Max(maximum, Math.Max(Contract.D(local, "duration_seconds"), member["expression_duration_seconds"]?.GetValue<double>() ?? 0));
            members.Add(member);
        }

        var function = Convert.FromHexString(Contract.S(JsonData.ReadObject(Path.Combine(repo, "data/runtime_contracts/publication.json")), "function_bytes_hex"));
        var epoch = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000000 + (ulong)(DateTime.UtcNow.Ticks % 10000) * 100;
        cfg["mmd_squad"] = new JsonObject
        {
            ["schema"] = 1,
            ["clip_epoch"] = epoch,
            ["duration_seconds"] = maximum,
            ["autoplay"] = request.Autoplay,
            ["speed"] = request.Speed,
            ["loop"] = request.Loop,
            ["members"] = members,
            ["native_physics_writer_enabled"] = true,
            ["body_backend"] = "game_native",
            ["publication_witness"] = new JsonObject
            {
                ["rva"] = "0x4ff3cd0",
                ["bytes"] = function.Length,
                ["function_bytes_hex"] = Convert.ToHexStringLower(function),
                ["sha256"] = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(function))
            }
        };
        cfg["residency_duration_ms"] = request.DurationMs;
        cfg["spacing_cm"] = request.SpacingCm;
        var scene = Contract.O(cfg, "mmd_squad");
        assets.Physics(scene, output, request.NativePhysics, request.Physics);
        await assets.CameraAsync(scene, output, request.CameraSource, request.CameraMode, anchorClip ?? new JsonObject(), cancellation);
        scene["duration_seconds"] = Math.Max(maximum, scene["camera_clip_duration_seconds"]?.GetValue<double>() ?? 0);
        scene["duration_ms"] = request.DurationMs;
        JsonData.Write(Path.Combine(output, "stage_config.json"), cfg);
        return new JsonObject
        {
            ["state"] = "prepared_not_run",
            ["session_dir"] = output,
            ["clip_epoch"] = epoch,
            ["duration_seconds"] = maximum,
            ["members"] = members.DeepClone(),
            ["message"] = sources.Values.Count(m => m.Enabled) + " 名参与队员已准备；核对动作与站位后开始。"
        };
    }

    public static bool Finished(string directory) => File.Exists(Path.Combine(directory, "stage_result.json"));

    public JsonObject Status(string directory)
    {
        var path = Path.Combine(directory, Finished(directory) ? "stage_result.json" : "stage_status.json");
        Contract.Require(File.Exists(path), "等待全队播放器共享时间轴状态");
        var current = JsonData.ReadObject(path);
        var player = (JsonObject)Contract.O(current, "mmd_squad").DeepClone();
        player["mode"] = Modes[Contract.I(player, "mode")];
        return new() { ["status"] = current["status"]?.DeepClone(), ["fault"] = Contract.True(current, "fault"), ["terminal"] = Finished(directory), ["state"] = player };
    }

    private static void StopFlag(string directory) => JsonData.AtomicWrite(Path.Combine(directory, "stop.flag"), []);

    public void Command(string directory, string kind, double value = 0)
        => PlaybackControl.SendAsync(directory, kind, value).GetAwaiter().GetResult();

    public Task<JsonObject> CommandAsync(string directory, string kind, double value = 0, uint targetSlot = 0, CancellationToken cancellation = default)
        => PlaybackControl.SendAsync(directory, kind, value, cancellation, targetSlot);

    public async Task FinishCurrentAsync(string directory, CancellationToken cancellation = default)
    {
        StopFlag(directory);
        await nativeTargets.FinishAsync(cancellation);
        JsonData.AtomicWrite(Path.Combine(directory, "native_observers_restored.flag"), []);
        var watch = Stopwatch.StartNew();
        while (!Finished(directory) && !CarrierLoader.NotLoaded(directory) && watch.Elapsed < TimeSpan.FromSeconds(15))
            await Task.Delay(100, cancellation);
        if (!Finished(directory) || !Contract.True(JsonData.ReadObject(Path.Combine(directory, "stage_result.json")), "cleanup_complete")) throw new TimeoutException("队伍停止恢复尚未完成；保留本次会话以便重试。");
        if (string.Equals(ActiveSession, Path.GetFullPath(directory), StringComparison.OrdinalIgnoreCase))
            ActiveSession = null;
    }

    private async Task<string> StartAsync(string directory, CancellationToken cancellation)
    {
        var state = JsonData.ReadObject(Path.Combine(directory, "stage_status.json"));
        JsonData.Write(Path.Combine(directory, "start.flag"), new JsonObject { ["schema"] = 1, ["command"] = "start", ["pid"] = state["pid"]?.DeepClone(), ["session_id"] = state["session_id"]?.DeepClone(), ["sent_tick_ms"] = (ulong)Environment.TickCount64 });
        var watch = Stopwatch.StartNew();
        while (watch.Elapsed < TimeSpan.FromSeconds(5))
        {
            var status = Status(directory);
            if (Contract.True(status, "fault") || Contract.True(status, "terminal"))
                throw new InvalidDataException("启动已发送，但宿主失败或进入终态：" + status["status"]);
            if (Contract.S(status, "status") == "resident")
                return "resident_observed";
            await Task.Delay(50, cancellation);
        }

        return "start_unconfirmed";
    }

    public async Task<JsonObject> LaunchAsync(string directory, CancellationToken cancellation = default)
    {
        directory = Path.GetFullPath(directory);
        var cfg = JsonData.ReadObject(Path.Combine(directory, "stage_config.json"));
        var game = GameProcess.Current();
        await GameProcess.WaitForForegroundAsync(game, cancellation);
        ActiveSession = directory;
        try
        {
            await CarrierLoader.LoadAsync(directory, cancellation);
            if (Contract.O(cfg, "mmd_squad")["native_physics"]?.GetValue<bool>() ?? true)
            {
            var nativeDeadline = Environment.TickCount64 + 30000;
            var targets = Contract.A(Contract.O(cfg, "mmd_squad"), "members").Where(row => Contract.True(row!, "enabled")).Select(row => Path.Combine(directory, "native_targets", Contract.I(row!, "role_id").ToString(System.Globalization.CultureInfo.InvariantCulture), "native_target.json")).ToArray();
            while (targets.Any(path => !File.Exists(path)))
            {
                Contract.Require(Environment.TickCount64 < nativeDeadline, "队伍未提供全部当前动画目标");
                await Task.Delay(100, cancellation);
            }

            JsonData.AtomicWrite(Path.Combine(directory, "native_observation_started.flag"), []);
            foreach (var target in targets)
                await nativeTargets.ObserveAsync(target, Path.Combine(directory, "native_inputs", Path.GetFileName(Path.GetDirectoryName(target)!)), game.Pid, cancellation);
            JsonData.AtomicWrite(Path.Combine(directory, "native_observers_restored.flag"), []);
            JsonData.AtomicWrite(Path.Combine(directory, "native_inputs_ready.flag"), []);
            }
            var watch = Stopwatch.StartNew();
            while (watch.Elapsed < TimeSpan.FromSeconds(30))
            {
                if (File.Exists(Path.Combine(directory, "stage_status.json")))
                {
                    var state = JsonData.ReadObject(Path.Combine(directory, "stage_status.json"));
                    if (Contract.True(state, "fault") || Contract.S(state, "status") is "error" or "restore_pending" or "cleanup_pending" or "finished")
                        throw new InvalidDataException("当前两帧队伍绑定失败");
                    if (Contract.S(state, "status") == "ready_waiting_start")
                        break;
                }

                await Task.Delay(100, cancellation);
            }

            var outcome = await StartAsync(directory, cancellation);
            return new JsonObject
            {
                ["state"] = outcome,
                ["session_dir"] = directory,
                ["message"] = outcome == "resident_observed" ? "当前宿主已观察到驻场读回；仍待用户画面验收。" : "启动请求已发送，尚未观察到驻场读回；保留会话等待状态或停止。"
            };
        }
        catch (Win32Exception)
        {
            ActiveSession = null;
            throw;
        }
        catch
        {
            StopFlag(directory);
            if (File.Exists(Path.Combine(directory, "native_observation_started.flag")))
            {
                await nativeTargets.FinishAsync(CancellationToken.None);
                JsonData.AtomicWrite(Path.Combine(directory, "native_observers_restored.flag"), []);
            }
            throw;
        }
    }
}
