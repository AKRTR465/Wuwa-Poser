using System.ComponentModel;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using WWPoser.Services;

namespace WWPoser;

public sealed partial class MainWindow : Window
{
    private readonly string root;
    private readonly PlaybackSessionService player;
    private readonly OwnedCharacterCatalog characterCatalog;
    private MediaService music => player.Music;
    private RecordingService recorder => player.Recorder;
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? operationCancellation;
    private readonly List<Button> actions = [];
    private readonly List<Button> sessionActions = [];
    private readonly ObservableCollection<ActorCard> actors = [];
    private string? preparedDirectory, displayedSession;
    private string? session => player.IsPreview ? null : player.ActiveSession;
    private string? previewSession => player.IsPreview ? player.ActiveSession : null;
    private bool sessionControllable => player.Controllable;
    private string? displayedMusicError;
    private PlaybackSessionRequest? activeRequest => player.ActiveRequest;
    private readonly Dictionary<uint, SquadPlacement> livePlacements = [];
    private readonly Dictionary<uint, ActorCard> liveActorCards = [];
    private bool busy, polling, closeReady, closing;
    private CancellationToken Token => operationCancellation?.Token ?? lifetime.Token;

    public MainWindow(string repositoryRoot, bool startPolling = true) : this(repositoryRoot, startPolling, new RecordingService(repositoryRoot)) { }
    internal MainWindow(string repositoryRoot, bool startPolling, RecordingService recordingService)
    {
        root = repositoryRoot; player = new(root, recordingService); characterCatalog = OwnedCharacterCatalog.Load(root);
        player.Progress += message => Dispatcher.Invoke(() => status.Text = message);
        BuildWindow();
        native.Checked += (_, _) => UpdatePhysics(); native.Unchecked += (_, _) => UpdatePhysics();
        placementTarget.SelectionChanged += (_, _) => LoadLivePlacement();
        UpdatePhysics();
        MarkPresetClean();
        if (startPolling)
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            timer.Tick += async (_, _) => await PollAsync(); timer.Start(); Closing += CloseRequested;
        }
    }
    private void UpdatePhysics() { if (native.IsChecked == true) secondary.IsChecked = false; secondary.IsEnabled = native.IsChecked != true; }
    private Task AddActorAsync()
    {
        var card = new ActorCard(actors.Count, characterCatalog);
        card.Changed += RefreshActorList;
        actors.Add(card); RefreshActorList(); actorList.SelectedItem = card; actorList.ScrollIntoView(card);
        return Task.CompletedTask;
    }
    private Task RemoveActorAsync()
    {
        if (actorList.SelectedItem is not ActorCard card) return Task.CompletedTask;
        var index = actorList.SelectedIndex; actorEditor.Child = null; actors.Remove(card);
        card.Changed -= RefreshActorList; RefreshActorList(); actorList.SelectedIndex = Math.Min(index, actors.Count - 1);
        return Task.CompletedTask;
    }
    private void RefreshActorList()
    {
        for (var i = 0; i < actors.Count; i++) actors[i].SetIndex(i);
        actorCount.Text = actors.Count == 0 ? "还没有演员" : $"{actors.Count} 位演员 · {actors.Count(card => card.Enabled.IsChecked == true)} 位参与表演";
        UpdateGuide();
    }
    private void SelectActor()
    {
        actorEditor.Child = null;
        if (actorList.SelectedItem is ActorCard card) actorEditor.Child = card.Panel;
    }
    private PlaybackSessionRequest Request() => BuildRequest(motion: true);
    // A stage preview needs characters and positions, not motion files.
    private PlaybackSessionRequest BuildRequest(bool motion)
    {
        var request = CapturePlaybackRequest(); var selected = request.Actors;
        if (!selected.Any(actor => actor.Enabled)) throw new InvalidOperationException("请选择至少一名参与演员。");
        if (motion && selected.Any(actor => actor.Enabled && string.IsNullOrEmpty(actor.BodySource))) throw new InvalidOperationException("请为参与演员选择身体动作。");
        if (actors.Any(card => card.Enabled.IsChecked == true && card.Character.SelectedItem is null)) throw new InvalidOperationException("请为每名参与演员选择角色与皮肤；预设中缺失的角色需要重新选择。");
        if (motion && cameraMode.SelectedIndex == 2 && Optional(camera) is null) throw new InvalidOperationException("请先选择镜头 VMD。");
        return request;
    }
    private PlaybackSessionRequest CapturePlaybackRequest()
    {
        var selected = actors.Select((card, index) => new PlaybackActorRequest(
            BodySource: Optional(card.Body) ?? "", FaceSource: Optional(card.Face), Scale: string.IsNullOrWhiteSpace(card.Scale.Text) ? null : Number(card.Scale, .000001, 100),
            Placement: new SquadPlacement(Number(card.X, -1000, 1000), Number(card.Y, -1000, 1000), Number(card.Height, -1000, 1000), Number(card.Yaw, -180, 180), card.InPlace.IsChecked == true),
            RoleId: (card.Character.SelectedItem as OwnedCharacterSelection)?.RoleId ?? card.MissingRoleId, Enabled: card.Enabled.IsChecked == true,
            CharacterId: card.Character.SelectedValue?.ToString() ?? card.MissingCharacterId, Slot: checked((uint)index + 1), Stage: card.Stage,
            GazeSource: Optional(card.Gaze), GazeStrength: Number(card.GazeStrength, 0, 3))).ToArray();
        return new(selected, CameraSource: Optional(camera), CameraMode: cameraMode.SelectedIndex, Speed: Number(speed, .1, 4), Loop: loop.IsChecked == true,
            DurationSeconds: Number(duration, 0, 2147483), NativePhysics: native.IsChecked == true, Physics: secondary.IsChecked == true,
            SpacingCm: Number(spacing, 0, 1000), FormationNative: placementMode.SelectedIndex == 1, HideSceneCharacters: hideSceneCharacters.IsChecked == true, GroundHeightCm: Number(groundHeight, -1000, 1000), CameraTrackHeightCm: Number(cameraTrackHeight, -10000, 10000));
    }
    private async Task PrepareAsync()
    {
        var request = Request(); status.Text = "正在准备动作素材…";
        var result = await Task.Run(() => player.PrepareAsync(request, Token), Token);
        preparedDirectory = result["session_dir"]!.GetValue<string>();
        preparation.Text = (result["message"]?.GetValue<string>() ?? "动作方案已准备，开始时绑定演员。") + "\n" + preparedDirectory;
        status.Text = "准备完成，可以开始播放。";
    }
    private Task LoadPreparationAsync()
    {
        var dialog = new OpenFolderDialog { Title = "选择已准备的动作目录" };
        if (dialog.ShowDialog() == true) LoadOwnedPreparation(dialog.FolderName);
        return Task.CompletedTask;
    }
    private async Task StartAsync()
    {
        var request = Request(); var selectedMusic = CaptureMusic(); var recording = CaptureRecording();
        await Task.Run(() => player.StartPerformanceAsync(request, preparedDirectory, selectedMusic, recording, Token), Token);
        UpdateLiveActors();
    }
    private void UpdateLiveActors()
    {
        if (displayedSession == session) return;
        displayedSession = session; livePlacements.Clear(); liveActorCards.Clear(); placementTarget.ItemsSource = null;
        if (session is null || activeRequest is not { } request) return;
        sessionLabel.Text = $"正在表演 · {request.Actors.Count(actor => actor.Enabled)} 位演员同步";
        placementTarget.ItemsSource = request.Actors.Select((actor, index) => new ActorTarget(actor.Slot == 0 ? checked((uint)index + 1) : actor.Slot,
            $"{index + 1}. {characterCatalog.Characters.FirstOrDefault(item => item.CharacterId == actor.CharacterId)?.DisplayName ?? actor.CharacterId}", actor)).Where(item => item.Actor.Enabled).ToArray();
        for (var i = 0; i < request.Actors.Count && i < actors.Count; i++)
            liveActorCards[request.Actors[i].Slot == 0 ? (uint)i + 1 : request.Actors[i].Slot] = actors[i];
        placementTarget.SelectedIndex = 0;
    }
    private async Task PreviewAsync()
    {
        var request = BuildRequest(motion: false);
        status.Text = "请切回鸣潮窗口，演员马上会站到你设定的位置…";
        try
        {
            var result = await Task.Run(() => player.PreviewAsync(request, Token), Token);
            status.Text = result["message"]?.GetValue<string>() ?? "演员已站到设定位置。";
        }
        finally { UpdatePreview(); }
    }
    private Task EndPreviewAsync() => previewSession is null ? Task.CompletedTask : StopCurrentAsync();
    private void RequireSession() { if (session is null) throw new InvalidOperationException("请先开始播放。"); }
    private async Task SendAsync(string command, double value = 0, uint targetSlot = 0) { RequireSession(); await player.SendAsync(session!, command, value, targetSlot, Token); }
    private async Task RestartAsync()
    {
        await player.RestartAsync(Token); progress.Value = 0; status.Text = "已从头开始播放。";
    }
    private async Task ApplySettingsAsync()
    {
        await player.ApplySettingsAsync(CapturePlaybackRequest(), Token); status.Text = "播放设置已应用。";
    }
    private void LoadLivePlacement()
    {
        if (placementTarget.SelectedItem is not ActorTarget target) return;
        var p = livePlacements.GetValueOrDefault(target.Slot) ?? target.Actor.Placement ?? new SquadPlacement();
        liveX.Text = F(p.OffsetX); liveY.Text = F(p.OffsetY); liveHeight.Text = F(p.Height); liveYaw.Text = F(p.Yaw); liveInPlace.IsChecked = p.InPlace;
    }
    private async Task ApplyGroundHeightAsync()
    {
        var height = Number(groundHeight, -1000, 1000);
        await SendAsync("ground_height", height);
        status.Text = "整体离地高度已应用。";
    }
    private async Task ApplyCameraTrackHeightAsync()
    {
        var height = Number(cameraTrackHeight, -10000, 10000);
        if (activeRequest?.CameraSource is null) throw new InvalidOperationException("本次表演未载入 VMD 镜头轨道，请先选择镜头文件并开始表演。");
        await SendAsync("camera_track_height", height);
        status.Text = $"VMD 镜头轨道高度偏移已应用：{F(height)} 厘米。";
    }
    private async Task ShiftCameraTrackHeightAsync(double? delta)
    {
        var height = delta is null ? 0 : Math.Clamp(Number(cameraTrackHeight, -10000, 10000) + delta.Value, -10000, 10000);
        cameraTrackHeight.Text = F(height);
        if (session is not null && sessionControllable) await ApplyCameraTrackHeightAsync();
        else status.Text = $"轨道高度偏移：{F(height)} 厘米，将在开始表演时生效。";
    }
    private async Task ApplyPlacementAsync()
    {
        if (placementTarget.SelectedItem is not ActorTarget target) throw new InvalidOperationException("请选择正在播放的演员。");
        var p = new SquadPlacement(Number(liveX, -1000, 1000), Number(liveY, -1000, 1000), Number(liveHeight, -1000, 1000), Number(liveYaw, -180, 180), liveInPlace.IsChecked == true);
        await SendAsync("offset_x", p.OffsetX, target.Slot); await SendAsync("offset_y", p.OffsetY, target.Slot);
        await SendAsync("height", p.Height, target.Slot); await SendAsync("yaw", p.Yaw, target.Slot);
        await SendAsync("in_place", p.InPlace ? 1 : 0, target.Slot); RememberLivePlacement(target, p); status.Text = "演员位置与朝向已应用。";
    }
    private async Task StopCurrentAsync()
    {
        try { await player.StopAsync(cancellation: lifetime.Token); }
        finally { UpdateLiveActors(); UpdatePreview(); UpdateActions(); }
    }
    private async Task StopAsync()
    {
        status.Text = "正在停止并恢复…"; await StopCurrentAsync(); status.Text = "停止与恢复流程已结束，请确认游戏画面。";
    }
    private MusicPreset? CaptureMusic() => Optional(audio) is { } path ? new(path, Number(offset, -3600, 3600), Number(volume, 0, 1)) : null;
    private PresetRecordingSettings CaptureRecording() => new(autoRecord.IsChecked == true, recordingFps.SelectedIndex == 0 ? 30 : 60,
        recordingSize.SelectedIndex switch { 0 => 1080, 1 => 1440, _ => 0 }, recordingFormat.SelectedIndex == 0 ? RecordingFormat.Compact : RecordingFormat.Compatible);
    private Task ApplyMusicAsync() => player.ApplyMusicAsync(CaptureMusic());
    private async Task RecordAsync()
    {
        await player.StartRecordingAsync(CaptureRecording()); UpdateRecordingControls(); status.Text = "正在录制鸣潮窗口。";
    }
    private async Task StopRecordingAsync()
    {
        try { await player.StopRecordingAsync(); }
        finally { UpdateRecordingControls(); }
    }
    private Task OpenRecordingFolderAsync()
    {
        Directory.CreateDirectory(recorder.OutputDirectory);
        using var folder = Process.Start(new ProcessStartInfo(recorder.OutputDirectory) { UseShellExecute = true });
        return Task.CompletedTask;
    }
    private Task SaveBookmarkAsync()
    {
        RequireSession(); var dialog = new SaveFileDialog { Filter = "动作书签|*.wwpose" };
        if (dialog.ShowDialog() == true) BookmarkService.Snapshot(session!, progress.Value, dialog.FileName); return Task.CompletedTask;
    }
    private async Task LoadBookmarkAsync()
    {
        RequireSession(); var dialog = new OpenFileDialog { Filter = "动作书签|*.wwpose" };
        if (dialog.ShowDialog() == true) await SendAsync("seek", BookmarkService.SeekTime(dialog.FileName, session!));
    }
    private async Task PollAsync()
    {
        var recording = recorder.Snapshot; recordingStatus.Text = recording.Message + (recorder.IsBusy ? $" · {recording.Elapsed:F1} 秒" : "");
        recordingStatus.ToolTip = recording.OutputPath ?? recorder.OutputDirectory; UpdateRecordingControls();
        if (polling || busy || player.ActiveSession is null) return; polling = true;
        try
        {
            var refresh = player.PollAsync(lifetime.Token); UpdateActions(); await refresh;
            if (session is not null && player.Snapshot is { } snapshot)
            {
                var state = snapshot.State; var seconds = Contract.D(state, "seconds"); var length = Contract.D(state, "duration_seconds");
                if (state["actor_count"] is not null)
                    sessionLabel.Text = $"{(sessionControllable ? "正在表演" : snapshot.Fresh ? "正在结束并恢复" : "状态已过期")} · {Contract.I(state, "actor_count")} 位演员（队员 {Contract.I(state, "borrowed_count")} · 新建 {Contract.I(state, "created_count")}）";
                progress.Maximum = Math.Max(length, .001); if (!progress.IsMouseCaptureWithin) progress.Value = seconds;
                var mode = Contract.S(state, "mode");
                modeLabel.Text = snapshot.Fresh ? mode switch { "playing" => "播放中", "paused" => "已暂停", "ended" => "动作结束", "closed" => "会话结束", "stopped" => "已停止", _ => mode } : "状态已过期，可停止恢复";
                time.Text = $"{Clock(seconds)} / {Clock(length)}";
                if (state["message"] is JsonValue message) status.Text = message.GetValue<string>();
                UpdateMusicFeedback();
            }
        }
        catch (Exception error) { status.Text = error.Message; }
        finally { polling = false; UpdateLiveActors(); UpdatePreview(); UpdateActions(); }
    }
    private void UpdateMusicFeedback()
    {
        var error = music.LastError;
        if (error is not null) status.Text = "配乐播放失败：" + error + "。可重新选择音乐后应用配乐。";
        else if (displayedMusicError is not null) status.Text = "配乐播放已恢复。";
        displayedMusicError = error;
    }
    private async Task ClosePlaybackAsync()
    {
        try { await player.CloseAsync(lifetime.Token); }
        finally { UpdateLiveActors(); UpdatePreview(); UpdateActions(); }
    }
    private async void CloseRequested(object? sender, CancelEventArgs args)
    {
        if (closeReady) return; args.Cancel = true;
        if (closing) return;
        closing = true; IsEnabled = false;
        try
        {
            if (busy) { operationCancellation?.Cancel(); status.Text = "正在取消当前操作，完成恢复后将自动关闭…"; }
            // Even an idle window can finish cleanup synchronously. Let WPF
            // leave the first Closing event before requesting the final Close.
            await System.Windows.Threading.Dispatcher.Yield(DispatcherPriority.Background);
            var cleanup = ClosePlaybackAsync();
            while (busy) await Task.Delay(25);
            busy = true; UpdateActions(); status.Text = "正在停止并恢复、保存录屏，完成后将自动关闭…";
            await cleanup; closeReady = true; Close(); lifetime.Cancel();
        }
        catch (Exception error) { closeReady = false; status.Text = "关闭尚未完成：" + error.Message; }
        finally
        {
            closing = false; busy = false;
            if (!closeReady) { IsEnabled = true; UpdateActions(); }
        }
    }
    public void SelectOwnedMmdPage() { if (actors.Count == 0) _ = AddActorAsync(); actorList.SelectedIndex = 0; }
    public void LoadOwnedPreparation(string directory)
    {
        SetRequest(player.ReadPreparation(directory)); preparedDirectory = Path.GetFullPath(directory);
        preparation.Text = "已载入准备目录：" + preparedDirectory;
    }
    private void SetRequest(PlaybackSessionRequest request)
    {
        camera.Text = request.CameraSource ?? ""; cameraMode.SelectedIndex = request.CameraMode; placementMode.SelectedIndex = request.FormationNative ? 1 : 0;
        speed.Text = F(request.Speed); loop.IsChecked = request.Loop; duration.Text = F(request.DurationSeconds);
        native.IsChecked = request.NativePhysics; secondary.IsChecked = request.Physics; spacing.Text = F(request.SpacingCm);
        hideSceneCharacters.IsChecked = request.HideSceneCharacters; groundHeight.Text = F(request.GroundHeightCm);
        cameraTrackHeight.Text = F(request.CameraTrackHeightCm);
        actorEditor.Child = null; foreach (var previous in actors) previous.Changed -= RefreshActorList; actors.Clear();
        var catalog = characterCatalog;
        for (var i = 0; i < request.Actors.Count; i++)
        {
            var actor = request.Actors[i]; var card = new ActorCard(i, catalog); var p = actor.Placement ?? new SquadPlacement();
            card.Body.Text = actor.BodySource; card.Face.Text = actor.FaceSource ?? ""; card.Scale.Text = actor.Scale is { } scale ? F(scale) : "";
            card.Gaze.Text = actor.GazeSource ?? ""; card.GazeStrength.Text = F(actor.GazeStrength);
            card.Enabled.IsChecked = actor.Enabled; card.X.Text = F(p.OffsetX); card.Y.Text = F(p.OffsetY); card.Height.Text = F(p.Height); card.Yaw.Text = F(p.Yaw); card.InPlace.IsChecked = p.InPlace;
            card.Character.SelectedValue = actor.CharacterId; card.Stage = actor.Stage;
            if (card.Character.SelectedItem is null && !string.IsNullOrWhiteSpace(actor.CharacterId)) card.MarkMissingCharacter(actor.CharacterId, actor.RoleId);
            card.Changed += RefreshActorList; actors.Add(card);
        }
        RefreshActorList(); if (actors.Count > 0) actorList.SelectedIndex = 0;
    }
    private static double Number(TextBox box, double low, double high)
    {
        if (!double.TryParse(box.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !double.IsFinite(value) || value < low || value > high)
            throw new InvalidDataException($"请输入 {low}–{high} 范围内的数值：{box.Text}"); return value;
    }
    private static string? Optional(TextBox box) => string.IsNullOrWhiteSpace(box.Text) ? null : Path.GetFullPath(box.Text);
    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static string Clock(double seconds) => TimeSpan.FromSeconds(Math.Max(0, seconds)).ToString(@"mm\:ss\.ff", CultureInfo.InvariantCulture);
    private sealed record ActorTarget(uint Slot, string Name, PlaybackActorRequest Actor);
}
