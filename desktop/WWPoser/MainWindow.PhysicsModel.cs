using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WWPoser.Services;

namespace WWPoser;

public sealed partial class MainWindow
{
    private readonly string physicsModelCache;
    private readonly PhysicsModelView physicsModelView = new() { Visibility = Visibility.Collapsed };
    private readonly DispatcherTimer modelTimer = new(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(30) };
    private Button toggleModelView = null!;
    private PhysicsPreviewModel? physicsModel;
    private int modelLoadRevision, modelBoneCount, highlightedModelTriangles;
    private HashSet<int> physicsHighlighted = [];
    private bool modelLoading, modelBusy, modelViewPreferred = true;
    private string? modelUnavailable;
    private (string Session, uint Slot, ulong Revision)? modelSubscription;
    private ulong displayedModelFrame;
    private long lastModelSample, nextModelSubscription;

    private void UpdatePhysicsModelPage()
    {
        if (page == 3)
        {
            modelTimer.Tick -= UpdatePhysicsModelFrame;
            modelTimer.Tick += UpdatePhysicsModelFrame;
            modelTimer.Start();
        }
        else
        {
            modelTimer.Stop();
            _ = RefreshModelSubscriptionAsync();
        }
    }

    private void ResetPhysicsModel()
    {
        modelLoadRevision++;
        physicsModel = null; modelLoading = false; modelUnavailable = null;
        modelBoneCount = 0; displayedModelFrame = 0; highlightedModelTriangles = 0;
        physicsModelView.SetModel(null);
        SetPhysicsView(modelViewPreferred);
    }

    private async Task LoadPhysicsModelAsync(OwnedCharacterSelection character, IReadOnlyList<PhysicsBone> bones)
    {
        var revision = modelLoadRevision;
        modelLoading = true;
        RefreshPhysicsSettings();
        try
        {
            var loaded = await Task.Run(() => PhysicsPreviewModel.Load(physicsModelCache, character, bones));
            if (revision != modelLoadRevision || lifetime.IsCancellationRequested) return;
            physicsModel = loaded;
            modelBoneCount = bones.Count;
            modelUnavailable = loaded is null ? "当前皮肤暂无本地 3D 模型" : null;
            physicsModelView.SetModel(loaded);
            highlightedModelTriangles = physicsModelView.Highlight(physicsHighlighted);
        }
        catch (Exception error) when (error is IOException or JsonException or KeyNotFoundException or InvalidOperationException or ArgumentException or IndexOutOfRangeException)
        {
            if (revision != modelLoadRevision || lifetime.IsCancellationRequested) return;
            modelUnavailable = "3D 模型读取失败";
            status.Text = error.Message;
        }
        finally
        {
            if (revision == modelLoadRevision && !lifetime.IsCancellationRequested)
            {
                modelLoading = false;
                SetPhysicsView(modelViewPreferred);
            }
        }
    }

    private void SetPhysicsView(bool preferModel)
    {
        modelViewPreferred = preferModel;
        var visible = preferModel && physicsModel is not null;
        physicsModelView.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        physicsSkeleton.Visibility = visible ? Visibility.Collapsed : Visibility.Visible;
        togglePhysicsPreview.Visibility = visible ? Visibility.Collapsed : Visibility.Visible;
        toggleModelView.IsEnabled = physicsModel is not null;
        toggleModelView.Content = visible ? "看骨架" : "看模型";
        RefreshPhysicsSettings();
    }

    private void RefreshPhysicsModelHeading()
    {
        if (physicsModelView.Visibility != Visibility.Visible)
        {
            if (modelViewPreferred && (modelLoading || modelUnavailable is not null))
                physicsPreviewHeading.Text += modelLoading ? " · 正在载入模型" : " · " + modelUnavailable;
            return;
        }
        var live = sessionControllable && SelectedLivePhysicsActor() is not null;
        var recentFrame = displayedModelFrame != 0 && Environment.TickCount64 - lastModelSample < 1600;
        physicsPreviewHeading.Text = recentFrame && live ? "实时 3D 模型" : live ? "参考姿态 · 等待游戏姿态" : "3D 模型 · 参考姿态";
        if (physicsSelection is not null && highlightedModelTriangles == 0) physicsPreviewHeading.Text += " · 所选骨链无可高亮网格";
    }

    private (string Session, uint Slot)? DesiredModelSubscription()
    {
        if (closing || page != 3 || physicsModelView.Visibility != Visibility.Visible || !sessionControllable || session is not { } directory ||
            player.Snapshot is null || SelectedLivePhysicsActor() is not { } actor) return null;
        return (directory, actor.Slot);
    }

    private async Task RefreshModelSubscriptionAsync()
    {
        if (modelBusy || lifetime.IsCancellationRequested) return;
        var desired = DesiredModelSubscription();
        if (modelSubscription is { } current && desired == (current.Session, current.Slot) || desired is null && modelSubscription is null) return;
        if (desired is not null && Environment.TickCount64 < nextModelSubscription) return;
        modelBusy = true;
        try
        {
            var previous = modelSubscription;
            modelSubscription = null;
            displayedModelFrame = 0;
            if (physicsModel is { } model) physicsModelView.Apply(PhysicsModelView.Deform(model, null));
            physicsModelView.SetCapsulePose(null);
            if (desired is { } target)
            {
                var receipt = await PlaybackControl.SendAsync(target.Session, "model_preview", 1, lifetime.Token, target.Slot);
                if (receipt["model_preview_revision"]?.GetValue<ulong>() is { } sequence && sequence != 0)
                    modelSubscription = (target.Session, target.Slot, sequence);
            }
            else if (previous is { } old && sessionControllable && session == old.Session)
                await PlaybackControl.SendAsync(old.Session, "model_preview", 0, lifetime.Token, old.Slot);
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or TimeoutException or OperationCanceledException)
        {
            nextModelSubscription = Environment.TickCount64 + 1800;
            if (desired is not null && DesiredModelSubscription() == desired && error is not OperationCanceledException) status.Text = error.Message;
        }
        finally
        {
            modelBusy = false;
            if (page != 3 && modelSubscription is not null) _ = RefreshModelSubscriptionAsync();
        }
    }

    private async void UpdatePhysicsModelFrame(object? sender, EventArgs args)
    {
        await RefreshModelSubscriptionAsync();
        if (modelBusy || physicsModel is not { } model || modelSubscription is not { } live || DesiredModelSubscription() != (live.Session, live.Slot))
        {
            if (page == 3 && physicsModelView.Visibility == Visibility.Visible) RefreshPhysicsModelHeading();
            return;
        }
        modelBusy = true;
        try
        {
            var count = modelBoneCount;
            var previous = displayedModelFrame;
            var result = await Task.Run<(PhysicsModelPose Pose, PhysicsModelView.Surface[] Surface)?>(() =>
            {
                var pose = PhysicsModelPose.Read(live.Session, live.Slot, live.Revision, count, previous);
                return pose is null ? null : (pose, PhysicsModelView.Deform(model, pose));
            });
            if (result is { } frame && ReferenceEquals(physicsModel, model) && DesiredModelSubscription() == (live.Session, live.Slot) && modelSubscription == live)
            {
                physicsModelView.Apply(frame.Surface);
                physicsModelView.SetCapsulePose(frame.Pose);
                displayedModelFrame = frame.Pose.Frame;
                lastModelSample = Environment.TickCount64;
            }
            if (page == 3) RefreshPhysicsModelHeading();
        }
        finally
        {
            modelBusy = false;
            if (page != 3 && modelSubscription is not null) _ = RefreshModelSubscriptionAsync();
        }
    }
}
