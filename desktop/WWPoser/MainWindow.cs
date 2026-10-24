using System.ComponentModel;
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
    private readonly MediaService music = new();
    private readonly RecordingService recorder;
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? operationCancellation;
    private readonly List<Button> actions = [];
    private readonly List<Button> sessionActions = [];
    private readonly ObservableCollection<ActorCard> actors = [];
    private string? session, preparedDirectory, previewSession;
    private PlaybackSessionRequest? activeRequest;
    private readonly Dictionary<uint, SquadPlacement> livePlacements = [];
    private bool busy, polling, closeReady, sessionControllable;
    private CancellationToken Token => operationCancellation?.Token ?? lifetime.Token;

    public MainWindow(string repositoryRoot, bool startPolling = true)
    {
        root = repositoryRoot; player = new(root); recorder = new(root); characterCatalog = OwnedCharacterCatalog.Load(root);
        player.Progress += message => Dispatcher.Invoke(() => status.Text = message);
        BuildWindow();
        native.Checked += (_, _) => UpdatePhysics(); native.Unchecked += (_, _) => UpdatePhysics();
        placementTarget.SelectionChanged += (_, _) => LoadLivePlacement();
        UpdatePhysics();
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
        var selected = actors.Select((card, index) => new PlaybackActorRequest(
            BodySource: Optional(card.Body) ?? "", FaceSource: Optional(card.Face), Scale: string.IsNullOrWhiteSpace(card.Scale.Text) ? null : Number(card.Scale, .000001, 100),
            Placement: new SquadPlacement(Number(card.X, -1000, 1000), Number(card.Y, -1000, 1000), Number(card.Height, -1000, 1000), Number(card.Yaw, -180, 180), card.InPlace.IsChecked == true),
            RoleId: (card.Character.SelectedItem as OwnedCharacterSelection)?.RoleId ?? 0, Enabled: card.Enabled.IsChecked == true,
            CharacterId: card.Character.SelectedValue?.ToString(), Slot: checked((uint)index + 1), Stage: card.Stage)).ToArray();
        if (!selected.Any(actor => actor.Enabled)) throw new InvalidOperationException("请选择至少一名参与演员。");
        if (motion && selected.Any(actor => actor.Enabled && string.IsNullOrEmpty(actor.BodySource))) throw new InvalidOperationException("请为参与演员选择身体动作。");
        if (selected.Any(actor => actor.Enabled && string.IsNullOrEmpty(actor.CharacterId))) throw new InvalidOperationException("请为每名参与演员选择角色与皮肤。");
        if (motion && cameraMode.SelectedIndex == 2 && Optional(camera) is null) throw new InvalidOperationException("请先选择镜头 VMD。");
        return new(PlaybackSource.MixedActors, selected, CameraSource: Optional(camera), CameraMode: cameraMode.SelectedIndex, Speed: Number(speed, .1, 4), Loop: loop.IsChecked == true,
            DurationSeconds: Number(duration, 0, 2147483), NativePhysics: native.IsChecked == true, Physics: secondary.IsChecked == true,
            SpacingCm: Number(spacing, 0, 1000), FormationNative: placementMode.SelectedIndex == 1);
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
        var request = Request(); if (session is not null || previewSession is not null) await StopCurrentAsync();
        status.Text = "请切回鸣潮，正在加载演员并准备播放…";
        JsonObject result;
        if (preparedDirectory is not null)
        {
            // The service compares compile inputs; simple runtime settings do not recompile.
            result = await Task.Run(() => player.LaunchAsync(preparedDirectory, request, Token), Token);
        }
        else result = await Task.Run(() => player.StartAsync(request, Token), Token);
        if (Contract.True(result, "cleanup_restored")) { status.Text = result["message"]?.GetValue<string>() ?? "本次启动已结束并恢复。"; return; }
        session = result["session_dir"]!.GetValue<string>(); activeRequest = request; sessionControllable = true;
        sessionLabel.Text = $"正在表演 · {request.Actors.Count(actor => actor.Enabled)} 位演员同步";
        var catalog = characterCatalog;
        placementTarget.ItemsSource = request.Actors.Select((actor, index) => new ActorTarget(actor.Slot == 0 ? checked((uint)index + 1) : actor.Slot,
            $"{index + 1}. {catalog.Characters.FirstOrDefault(item => item.CharacterId == actor.CharacterId)?.DisplayName ?? actor.CharacterId}", actor)).Where(item => item.Actor.Enabled).ToArray();
        livePlacements.Clear(); placementTarget.SelectedIndex = 0; await ApplyMusicAsync(); status.Text = result["message"]?.GetValue<string>() ?? "播放已开始。";
    }
    private async Task PreviewAsync()
    {
        if (session is not null) throw new InvalidOperationException("正在表演中；先点「停止并恢复」，再预览站位。");
        var request = BuildRequest(motion: false); await EndPreviewAsync();
        status.Text = "请切回鸣潮窗口，演员马上会站到你设定的位置…";
        try
        {
            var result = await Task.Run(() => player.PreviewAsync(request, Token), Token);
            status.Text = result["message"]?.GetValue<string>() ?? "演员已站到设定位置。";
        }
        finally { previewSession = player.ActiveSession; UpdatePreview(); }
    }
    private async Task EndPreviewAsync()
    {
        if (previewSession is null) return;
        status.Text = "正在请预览的演员退场…";
        await player.StopAsync(previewSession, lifetime.Token); previewSession = null; UpdatePreview(); status.Text = "预览已结束，演员已退场。";
    }
    // After a failed operation, keep whichever session still needs restoring.
    private void AdoptActiveSession()
    {
        if (session is not null || player.ActiveSession is not { } active || active == previewSession) return;
        if (OwnedMmdPlaybackService.Config(active)["preview"]?.GetValue<bool>() == true) { previewSession = active; UpdatePreview(); } else session = active;
    }
    private void RequireSession() { if (session is null) throw new InvalidOperationException("请先开始播放。"); }
    private async Task SendAsync(string command, double value = 0, uint targetSlot = 0) { RequireSession(); await player.SendAsync(session!, command, value, targetSlot, Token); }
    private async Task ApplySettingsAsync()
    {
        RequireSession();
        if (activeRequest is null) throw new InvalidOperationException("本次启动未完成；请先停止并恢复。");
        if ((native.IsChecked == true) != activeRequest.NativePhysics)
            throw new InvalidOperationException("物理方式已改变，需要重新开始播放。当前会话的物理方式保持不变。");
        if (cameraMode.SelectedIndex == 2 && (activeRequest.CameraSource is null ||
            !string.Equals(Optional(camera), Path.GetFullPath(activeRequest.CameraSource), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("当前会话未载入这个镜头 VMD，请重新开始播放以载入镜头素材。");
        if ((placementMode.SelectedIndex == 1) != activeRequest.FormationNative)
            throw new InvalidOperationException("摆放方式已改变，站位在开始播放时确定；请先停止并恢复，再重新开始播放。");
        await SendAsync("speed", Number(speed, .1, 4)); await SendAsync("loop", loop.IsChecked == true ? 1 : 0);
        await SendAsync("time_limit", Number(duration, 0, 2147483)); await SendAsync("camera", cameraMode.SelectedIndex);
        await SendAsync("physics", secondary.IsChecked == true ? 1 : 0); status.Text = "播放设置已应用。";
    }
    private void LoadLivePlacement()
    {
        if (placementTarget.SelectedItem is not ActorTarget target) return;
        var p = livePlacements.GetValueOrDefault(target.Slot) ?? target.Actor.Placement ?? new SquadPlacement();
        liveX.Text = F(p.OffsetX); liveY.Text = F(p.OffsetY); liveHeight.Text = F(p.Height); liveYaw.Text = F(p.Yaw); liveInPlace.IsChecked = p.InPlace;
    }
    private async Task ApplyPlacementAsync()
    {
        if (placementTarget.SelectedItem is not ActorTarget target) throw new InvalidOperationException("请选择正在播放的演员。");
        var p = new SquadPlacement(Number(liveX, -1000, 1000), Number(liveY, -1000, 1000), Number(liveHeight, -1000, 1000), Number(liveYaw, -180, 180), liveInPlace.IsChecked == true);
        await SendAsync("offset_x", p.OffsetX, target.Slot); await SendAsync("offset_y", p.OffsetY, target.Slot);
        await SendAsync("height", p.Height, target.Slot); await SendAsync("yaw", p.Yaw, target.Slot);
        await SendAsync("in_place", p.InPlace ? 1 : 0, target.Slot); livePlacements[target.Slot] = p; status.Text = "演员位置与朝向已应用。";
    }
    private async Task StopCurrentAsync()
    {
        await EndPreviewAsync();
        var current = session ?? player.ActiveSession;
        if (current is null) { await music.StopAsync(); return; }
        var result = await player.StopAsync(current, lifetime.Token);
        if (result["restored"] is JsonValue restored && !restored.GetValue<bool>()) throw new InvalidOperationException(result["message"]?.GetValue<string>() ?? "恢复尚未完成，请查看会话状态。");
        // The game restoration is already confirmed. Clear frontend ownership
        // before awaiting audio shutdown, which can fail independently.
        session = null; activeRequest = null; sessionControllable = false;
        livePlacements.Clear(); placementTarget.ItemsSource = null;
        try { await music.StopAsync(); }
        finally { UpdateActions(); }
    }
    private async Task StopAsync() { status.Text = "正在停止并恢复…"; await StopCurrentAsync(); await player.StopPendingAsync(lifetime.Token); status.Text = "停止与恢复流程已结束，请确认游戏画面。"; }
    private async Task ApplyMusicAsync()
    {
        if (session is null) return;
        var preset = Optional(audio) is { } path ? new MusicPreset(path, Number(offset, -3600, 3600), Number(volume, 0, 1)) : null;
        music.UpdateContext(session, preset); await music.StartSynchronizationAsync(); recorder.UpdateAudioContext(session, preset);
    }
    private async Task RecordAsync()
    {
        var directory = Path.Combine(root, "playback", "recordings"); Directory.CreateDirectory(directory);
        await recorder.StartAsync(directory, recordingFps.SelectedIndex == 0 ? 30 : 60, recordingSize.SelectedIndex == 0 ? 1080 : 0); status.Text = "正在录制鸣潮窗口。";
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
        if (polling || busy || session is null) return; polling = true;
        try
        {
            var state = player.ReadStatus(session); var seconds = Contract.D(state, "seconds"); var length = Contract.D(state, "duration_seconds");
            var controllable = Contract.True(state, "session_active") && !Contract.True(state, "restore_requested");
            if (sessionControllable != controllable) { sessionControllable = controllable; UpdateActions(); }
            if (state["actor_count"] is not null)
                sessionLabel.Text = $"{(controllable ? "正在表演" : "正在结束并恢复")} · {Contract.I(state, "actor_count")} 位演员（队员 {Contract.I(state, "borrowed_count")} · 新建 {Contract.I(state, "created_count")}）";
            progress.Maximum = Math.Max(length, .001); if (!progress.IsMouseCaptureWithin) progress.Value = seconds;
            var mode = Contract.S(state, "mode"); var label = mode switch { "playing" => "播放中", "paused" => "已暂停", "ended" => "动作结束", "closed" => "会话结束", "stopped" => "已停止", _ => mode };
            time.Text = $"{Clock(seconds)} / {Clock(length)}"; modeLabel.Text = label;
            if (state["message"] is JsonValue message) status.Text = message.GetValue<string>();
            // A terminal timeline alone is not release evidence. Keep recovery
            // ownership until both native and managed restoration are confirmed.
            if (mode == "closed" && Contract.True(state, "restoration_complete") && OwnedMmdPlaybackService.RestorationConfirmed(session))
            {
                busy = true; UpdateActions();
                try { await StopCurrentAsync(); status.Text = "表演已结束，演员与镜头已恢复，可以再次开始。"; }
                finally { busy = false; UpdateActions(); }
            }
        }
        catch (Exception error) { status.Text = error.Message; } finally { polling = false; }
    }
    private async void CloseRequested(object? sender, CancelEventArgs args)
    {
        if (closeReady) return; args.Cancel = true;
        if (busy) { operationCancellation?.Cancel(); status.Text = "正在取消当前操作，请等待恢复后关闭。"; return; }
        busy = true; UpdateActions();
        try { await recorder.StopAsync(); await StopCurrentAsync(); await player.StopPendingAsync(lifetime.Token); await music.StopAsync(); closeReady = true; lifetime.Cancel(); Close(); }
        catch (Exception error) { status.Text = "关闭尚未完成：" + error.Message; } finally { busy = false; UpdateActions(); }
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
        actorEditor.Child = null; foreach (var previous in actors) previous.Changed -= RefreshActorList; actors.Clear();
        var catalog = characterCatalog;
        for (var i = 0; i < request.Actors.Count; i++)
        {
            var actor = request.Actors[i]; var card = new ActorCard(i, catalog); var p = actor.Placement ?? new SquadPlacement();
            card.Body.Text = actor.BodySource; card.Face.Text = actor.FaceSource ?? ""; card.Scale.Text = actor.Scale is { } scale ? F(scale) : "";
            card.Enabled.IsChecked = actor.Enabled; card.X.Text = F(p.OffsetX); card.Y.Text = F(p.OffsetY); card.Height.Text = F(p.Height); card.Yaw.Text = F(p.Yaw); card.InPlace.IsChecked = p.InPlace;
            card.Character.SelectedValue = actor.CharacterId; card.Stage = actor.Stage;
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
