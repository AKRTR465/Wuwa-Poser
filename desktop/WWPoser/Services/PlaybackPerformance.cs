namespace WWPoser.Services;

public sealed partial class PlaybackSessionService
{
    public MediaService Music { get; }
    public RecordingService Recorder { get; }
    public string? AutomaticRecordingSession { get; private set; }

    public Task<JsonObject> StartPerformanceAsync(PlaybackSessionRequest request, string? preparedDirectory, MusicPreset? music,
        PresetRecordingSettings recording, CancellationToken cancellation = default) => RunAsync(async token =>
    {
        ValidateRequest(request);
        await StopCoreAsync(null, CancellationToken.None);
        if (music is not null)
        {
            Progress?.Invoke("正在准备配乐…");
            await Music.PrepareAsync(music.AudioFile, token);
        }
        if (preparedDirectory is null)
        {
            Phase = PlaybackSessionPhase.Preparing;
            preparedDirectory = Contract.S(await PrepareCoreAsync(request, token), "session_dir");
        }
        Progress?.Invoke("请切回鸣潮，正在加载演员并准备播放…");
        var result = await LaunchCoreAsync(preparedDirectory, request, token, recording.Automatic);
        await CompletePlaybackStartAsync(result, music, recording, token);
        return result;
    }, cancellation);

    internal async Task CompletePlaybackStartAsync(JsonObject result, MusicPreset? music, PresetRecordingSettings recording, CancellationToken cancellation = default)
    {
        if (!Contract.True(result, "loaded"))
        {
            Progress?.Invoke(result["message"]?.GetValue<string>() ?? "播放尚未开始，请先停止并恢复。");
            return;
        }
        if (recording.Automatic && Recorder.IsStopping) await Recorder.StopAsync().WaitAsync(cancellation);
        await ApplyMusicAsync(music);
        if (recording.Automatic && !Recorder.IsBusy) await StartRecordingAsync(recording, automatic: true);
        if (Contract.True(result, "start_paused"))
        {
            Progress?.Invoke("演员已停在动作起点，正在等待录屏首帧…");
            try { await Recorder.WaitForReadyAsync(cancellation); }
            catch (Exception error) when (error is not OperationCanceledException)
            {
                if (AutomaticRecordingSession == ActiveSession) await StopRecordingAsync();
                throw new InvalidOperationException("自动录屏未能就绪，演员仍在动作起点暂停。请检查录屏状态后重试，或停止并恢复：" + error.Message, error);
            }
            await SendAsync(ActiveSession!, "play", cancellation: cancellation);
        }
        Progress?.Invoke(AutomaticRecordingSession == ActiveSession ? "播放已开始，正在自动录制鸣潮窗口。"
            : result["message"]?.GetValue<string>() ?? "播放已开始。");
    }
    public async Task ApplyMusicAsync(MusicPreset? preset)
    {
        if (ActiveSession is null || IsPreview) return;
        Music.UpdateContext(ActiveSession, preset); await Music.StartSynchronizationAsync(); Recorder.UpdateAudioContext(ActiveSession, preset);
    }
    public async Task StartRecordingAsync(PresetRecordingSettings settings, bool automatic = false)
    {
        Directory.CreateDirectory(Recorder.OutputDirectory);
        await Recorder.StartAsync(Recorder.OutputDirectory, settings.Fps, settings.Height, settings.Format);
        AutomaticRecordingSession = automatic ? ActiveSession : null;
    }
    public async Task StopRecordingAsync()
    {
        AutomaticRecordingSession = null; await Recorder.StopAsync();
    }
    public async Task RestartAsync(CancellationToken cancellation = default)
    {
        var current = ActiveSession ?? throw new InvalidOperationException("请先开始播放。");
        await SendAsync(current, "pause", cancellation: cancellation);
        await SendAsync(current, "seek", 0, cancellation: cancellation);
        await SendAsync(current, "play", cancellation: cancellation);
        Music.RequestResynchronization();
    }
    public async Task ApplySettingsAsync(PlaybackSessionRequest settings, CancellationToken cancellation = default)
    {
        var current = ActiveSession ?? throw new InvalidOperationException("请先开始播放。");
        var request = ActiveRequest ?? throw new InvalidOperationException("本次启动未完成；请先停止并恢复。");
        // Validate the complete edit before sending its first command.
        settings.Validate();
        if (settings.NativePhysics != request.NativePhysics)
            throw new InvalidOperationException("物理方式已改变，需要重新开始播放。当前会话的物理方式保持不变。");
        if (settings.CameraMode == 2 && (request.CameraSource is null || settings.CameraSource is null ||
            !string.Equals(Path.GetFullPath(settings.CameraSource), Path.GetFullPath(request.CameraSource), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("当前会话未载入这个镜头 VMD，请重新开始播放以载入镜头素材。");
        if (settings.FormationNative != request.FormationNative)
            throw new InvalidOperationException("摆放方式已改变，站位在开始播放时确定；请先停止并恢复，再重新开始播放。");
        await SendAsync(current, "speed", settings.Speed, cancellation: cancellation);
        await SendAsync(current, "loop", settings.Loop ? 1 : 0, cancellation: cancellation);
        await SendAsync(current, "time_limit", settings.DurationSeconds, cancellation: cancellation);
        await SendAsync(current, "camera", settings.CameraMode, cancellation: cancellation);
        await SendAsync(current, "camera_track_height", settings.CameraTrackHeightCm, cancellation: cancellation);
        await SendAsync(current, "physics", settings.Physics ? 1 : 0, cancellation: cancellation);
    }
    public async Task CloseAsync(CancellationToken cancellation = default)
    {
        var saving = StopRecordingAsync();
        try { await StopAsync(cancellation: cancellation); }
        finally { await saving; }
    }
}
