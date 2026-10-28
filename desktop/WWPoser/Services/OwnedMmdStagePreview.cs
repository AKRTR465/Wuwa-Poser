using System.Diagnostics;

namespace WWPoser.Services;

// A stage preview stands the chosen characters at their start positions with no
// motion. It needs no preparation; Stop/restore is the normal session teardown.
public sealed partial class OwnedMmdPlaybackService
{
    public JsonObject PreviewConfig(PlaybackSessionRequest request)
    {
        PlaybackSessionService.ValidateRequest(request, preview: true);
        request = PlaybackSessionService.AssignSlots(request);
        var catalog = CharacterCatalog(); var actors = new JsonArray();
        foreach (var actor in request.Actors.Where(actor => actor.Enabled))
        {
            var character = catalog.Select(actor.CharacterId);
            var row = new JsonObject { ["slot"] = actor.Slot, ["enabled"] = true, ["character_id"] = character.CharacterId, ["character_display_name"] = character.DisplayName,
                ["role_id"] = (long)character.RoleId, ["skin_id"] = (long)character.SkinId, ["model_id"] = character.ModelId,
                ["mesh_asset_name"] = character.ModelAssetName, ["form"] = character.Form?.DeepClone(), ["independent_instance"] = true };
            SetStage(row, actor.Stage); actors.Add(row);
        }
        var config = new JsonObject { ["schema"] = 3, ["scope"] = "stage_preview", ["preview"] = true, ["session_epoch"] = NewEpoch(), ["actors"] = actors, ["actor_count"] = actors.Count, ["duration_ms"] = 0 };
        ConfigureStage(config, request.SpacingCm, request.FormationNative, request.GroundHeightCm);
        // Preview has no body publication layer to apply the shared height.
        foreach (var actor in ActorConfigs(config)) actor["stage_z_cm"] = Contract.D(actor, "stage_z_cm") + request.GroundHeightCm;
        return config;
    }

    public async Task<JsonObject> PreviewAsync(PlaybackSessionRequest request, CancellationToken cancellation = default)
    {
        if (ActiveSession is not null) throw new InvalidOperationException("请先停止当前演员会话并等待恢复。");
        var config = PreviewConfig(request); var count = Contract.A(config, "actors").Count;
        LaunchStage = "runtime_preparation";
        var game = GameProcess.Current();
        var directory = Path.Combine(root, "playback", "stage_previews", "preview_" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        foreach (var filename in RuntimeFiles) File.Copy(Path.Combine(NativePackageDirectory, filename), Path.Combine(directory, filename));
        JsonData.Write(Path.Combine(directory, "owned_mmd_config.json"), config);
        WriteActorsFile(directory, config);
        JsonData.Write(Path.Combine(directory, "session_config.json"), new JsonObject
        {
            ["session_id"] = Guid.NewGuid().ToString("N"), ["expected_pid"] = game.Pid,
            ["window_handle"] = game.WindowHandle, ["window_thread_id"] = game.WindowThread, ["expected_process_creation_filetime"] = game.CreationFileTime
        });
        await GameProcess.WaitForForegroundAsync(game, cancellation);
        Progress?.Invoke("正在请演员上台预览站位");
        var info = new ProcessStartInfo(Path.Combine(directory, "ownedmmd_lite_installer.exe")) { WorkingDirectory = directory, UseShellExecute = true, Verb = "runas", WindowStyle = ProcessWindowStyle.Hidden };
        info.ArgumentList.Add("--run-session"); info.ArgumentList.Add(directory);
        LaunchStage = "owned_installer_launch";
        installer = Process.Start(info)!;
        ActiveSession = directory;
        try
        {
            while (true)
            {
                if (installer.HasExited)
                {
                    installer.Dispose(); installer = null;
                    var restored = RestorationConfirmed(directory); if (restored) ActiveSession = null;
                    var terminalPath = Path.Combine(directory, "session_result.json");
                    var reason = File.Exists(terminalPath) ? JsonData.ReadObject(terminalPath)["reason"]?.GetValue<string>() : null;
                    return new JsonObject { ["session_dir"] = directory, ["loaded"] = false, ["cleanup_restored"] = restored,
                        ["message"] = "预览没有开始：" + (reason ?? "安装器已退出") + (restored ? "；恢复已完成。" : "；请点「停止并恢复」。") };
                }
                if (File.Exists(Path.Combine(directory, "owned_display_shown.json")))
                {
                    LaunchStage = "stage_preview";
                    return new JsonObject { ["session_dir"] = directory, ["loaded"] = true, ["state"] = "stage_preview", ["actor_count"] = count,
                        ["message"] = $"{count} 位演员已站到设定位置。切回鸣潮看看吧，看完点「结束预览」。" };
                }
                await Task.Delay(100, cancellation);
            }
        }
        catch { RequestStop(directory); throw; }
    }
}
