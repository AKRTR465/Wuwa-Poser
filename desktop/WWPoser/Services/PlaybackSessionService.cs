namespace WWPoser.Services;

public enum PlaybackSessionPhase { Idle, Preparing, Loading, Active, Restoring, RecoveryPending }

// One desktop lifecycle; the native service owns the runtime directory and
// confirms restoration. Playing/paused/ended remain native timeline states.
public sealed partial class PlaybackSessionService
{
    public const string RequestFilename = "unified_session_request.json";
    private readonly string root;
    private readonly OwnedMmdPlaybackService owned;
    private readonly SemaphoreSlim operationGate = new(1, 1);
    private CancellationTokenSource? operationCancellation;
    public string? ActiveSession => owned.ActiveSession;
    public string LaunchStage => owned.LaunchStage;
    public PlaybackSessionRequest? ActiveRequest { get; private set; }
    public PlaybackSessionPhase Phase { get; private set; }
    public bool IsPreview { get; private set; }
    public bool IsBusy => operationGate.CurrentCount == 0;
    public (JsonObject State, bool Fresh)? Snapshot { get; private set; }
    public bool Controllable => Phase == PlaybackSessionPhase.Active && !IsPreview && Snapshot is { } snapshot
        && snapshot.Fresh && Contract.True(snapshot.State, "session_active") && !Contract.True(snapshot.State, "restore_requested");
    public event Action<string>? Progress;

    public PlaybackSessionService(string repoRoot, RecordingService? recording = null)
    {
        root = Path.GetFullPath(repoRoot); owned = new(root); Music = new(root); Recorder = recording ?? new(root);
        owned.Progress += message => Progress?.Invoke(message);
    }

    private async Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellation)
    {
        using var pending = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        lock (operationGate)
        {
            cancellation.ThrowIfCancellationRequested();
            if (!operationGate.Wait(0)) throw new InvalidOperationException("另一个会话操作尚未结束。");
            operationCancellation = pending;
        }
        try { return await operation(pending.Token); }
        catch (OperationCanceledException)
        {
            await StopCoreAsync(null, CancellationToken.None);
            throw;
        }
        finally
        {
            if (ActiveSession is null) ClearSession();
            else if (Phase is PlaybackSessionPhase.Loading or PlaybackSessionPhase.Restoring) Phase = PlaybackSessionPhase.RecoveryPending;
            lock (operationGate) operationCancellation = null;
            operationGate.Release();
        }
    }
    private void RequireIdle()
    {
        if (ActiveSession is not null) throw new InvalidOperationException("请先停止当前演员会话并等待恢复。");
    }
    private void ClearSession()
    {
        ActiveRequest = null; Snapshot = null; IsPreview = false; Phase = PlaybackSessionPhase.Idle;
    }
    public Task<JsonObject> PrepareAsync(PlaybackSessionRequest request, CancellationToken cancellation = default) => RunAsync(async token =>
    {
        RequireIdle(); Phase = PlaybackSessionPhase.Preparing;
        return await PrepareCoreAsync(request, token);
    }, cancellation);

    public Task<JsonObject> LaunchAsync(string preparedDirectory, CancellationToken cancellation = default) => LaunchAsync(preparedDirectory, null, cancellation);
    public Task<JsonObject> LaunchAsync(string preparedDirectory, PlaybackSessionRequest? settings, CancellationToken cancellation = default, bool startPaused = false) => RunAsync(async token =>
    {
        RequireIdle();
        return await LaunchCoreAsync(preparedDirectory, settings, token, startPaused);
    }, cancellation);
    private async Task<JsonObject> LaunchCoreAsync(string preparedDirectory, PlaybackSessionRequest? settings, CancellationToken cancellation, bool startPaused)
    {
        ActiveRequest = settings is null ? null : AssignSlots(settings); IsPreview = false;
        // A frozen preparation can run without its original editable sources.
        // Only explicit authoring settings request comparison/recompilation.
        if (ActiveRequest is { } request)
        {
            ValidateRequest(request); Phase = PlaybackSessionPhase.Preparing;
            if (RequiresPreparation(preparedDirectory, request))
            {
                Progress?.Invoke("素材或角色已更改，正在重新准备");
                var replacement = await PrepareCoreAsync(request, cancellation);
                preparedDirectory = Contract.S(replacement, "session_dir");
            }
            UpdatePreparation(preparedDirectory, request);
        }
        Phase = PlaybackSessionPhase.Loading;
        var result = await owned.LaunchAsync(preparedDirectory, cancellation, startPaused);
        if (ActiveSession is { } directory)
        {
            if (ActiveRequest is not null) WriteRequest(directory, ActiveRequest);
            Phase = Contract.True(result, "loaded") ? PlaybackSessionPhase.Active : PlaybackSessionPhase.RecoveryPending;
            if (Phase == PlaybackSessionPhase.Active) Snapshot = ReadStatus(directory);
        }
        return result;
    }
    public Task<JsonObject> StartAsync(PlaybackSessionRequest request, CancellationToken cancellation = default, bool startPaused = false) => RunAsync(async token =>
    {
        RequireIdle(); Phase = PlaybackSessionPhase.Preparing;
        var prepared = await PrepareCoreAsync(request, token);
        return await LaunchCoreAsync(Contract.S(prepared, "session_dir"), request, token, startPaused);
    }, cancellation);

    public Task<JsonObject> PreviewAsync(PlaybackSessionRequest request, CancellationToken cancellation = default) => RunAsync(async token =>
    {
        if (ActiveSession is not null && !IsPreview) throw new InvalidOperationException("正在表演中；先点「停止并恢复」，再预览站位。");
        await StopCoreAsync(null, CancellationToken.None);
        ValidateRequest(request, preview: true); ActiveRequest = AssignSlots(request); IsPreview = true; Phase = PlaybackSessionPhase.Loading;
        var result = await owned.PreviewAsync(ActiveRequest, token);
        Phase = Contract.True(result, "loaded") ? PlaybackSessionPhase.Active : PlaybackSessionPhase.RecoveryPending;
        return result;
    }, cancellation);

    public (JsonObject State, bool Fresh) ReadStatus(string directory) => PlaybackSessionStatus.Read(directory);
    public async Task<JsonObject> SendAsync(string directory, string kind, double value = 0, uint targetSlot = 0, CancellationToken cancellation = default)
    {
        var result = await PlaybackControl.SendAsync(directory, kind, value, cancellation, targetSlot);
        if (directory == ActiveSession && ActiveRequest is { } request)
            ActiveRequest = kind switch
            {
                "speed" => request with { Speed = value }, "loop" => request with { Loop = value != 0 },
                "time_limit" => request with { DurationSeconds = value }, "camera" => request with { CameraMode = (int)value },
                "physics" => request with { Physics = value != 0 }, "ground_height" => request with { GroundHeightCm = value },
                "camera_track_height" => request with { CameraTrackHeightCm = value }, _ => request
            };
        return result;
    }
    public async Task StopAsync(string? directory = null, CancellationToken cancellation = default)
    {
        lock (operationGate) operationCancellation?.Cancel();
        await operationGate.WaitAsync(cancellation);
        try { await StopCoreAsync(directory, cancellation); }
        finally { operationGate.Release(); }
    }
    private async Task StopCoreAsync(string? directory, CancellationToken cancellation)
    {
        var recordingStop = AutomaticRecordingSession is not null ? StopRecordingAsync() : Task.CompletedTask;
        try
        {
            if ((directory ?? ActiveSession) is { } current)
            {
                Phase = PlaybackSessionPhase.Restoring; Snapshot = null;
                try { await owned.FinishSessionAsync(current, cancellation); }
                catch { Phase = PlaybackSessionPhase.RecoveryPending; throw; }
            }
            // Audio/encoding failure cannot resurrect confirmed game ownership.
            ClearSession(); await Music.StopAsync();
        }
        finally { await recordingStop; }
    }
    public async Task PollAsync(CancellationToken cancellation = default)
    {
        if (!await operationGate.WaitAsync(0, cancellation)) return;
        Task? saving = null; var current = ActiveSession;
        try
        {
            if (current is null) return;
            // Restoration evidence is independent of timeline availability.
            if (OwnedMmdPlaybackService.RestorationConfirmed(current))
            {
                await StopCoreAsync(current, cancellation);
                Progress?.Invoke("表演已结束，演员与镜头已恢复，可以再次开始。");
                return;
            }
            if (!IsPreview)
            {
                Snapshot = null; Snapshot = ReadStatus(current);
                var (state, fresh) = Snapshot.Value;
                if (Contract.True(state, "restore_requested")) Phase = PlaybackSessionPhase.Restoring;
                if (fresh && Contract.S(state, "mode") == "ended" && !Contract.True(state, "loop") && AutomaticRecordingSession == current)
                    saving = StopRecordingAsync();
            }
            // Preview has no timeline heartbeat. Its terminal restoration uses
            // the same native and managed evidence as a playback session.
            if (OwnedMmdPlaybackService.Finished(current)) Phase = PlaybackSessionPhase.RecoveryPending;
        }
        finally { operationGate.Release(); }
        // Saving a completed clip must not hold the lifecycle gate: Stop/Close
        // can restore actors while the encoder finishes.
        if (saving is not null)
        {
            await saving;
            if (ActiveSession == current)
                Progress?.Invoke(Recorder.Snapshot.State == "error" ? "动作已播完，录屏保存失败：" + Recorder.Snapshot.Message
                    : "动作已播完，录屏" + (Recorder.Snapshot.State == "saved" ? Recorder.Snapshot.Message : "已停止。"));
        }
    }
}
