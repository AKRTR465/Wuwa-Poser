using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WWPoser;
using WWPoser.Services;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        string root = Path.GetFullPath(args[0]), output = Path.GetFullPath(args[1]); Directory.CreateDirectory(output);
        _ = new Application();
        var captures = new ConcurrentQueue<(string Folder, int Fps, int Height)>(); var captureStarted = new SemaphoreSlim(0); int captureStops = 0;
        TaskCompletionSource? finalization = null, captureStopped = null; RecordingSnapshot? captureCompletion = null;
        RecordingService recorder = null!;
        recorder = new RecordingService(root, async (folder, fps, height, stop) => {
            captures.Enqueue((folder, fps, height)); captureStarted.Release();
            using var registration = stop.Register(() => Interlocked.Increment(ref captureStops));
            try { await Task.Delay(Timeout.Infinite, stop).ConfigureAwait(false); }
            catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
            captureStopped?.TrySetResult();
            if (finalization is not null) await finalization.Task.ConfigureAwait(false);
            if (captureCompletion is not null) typeof(RecordingService).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(recorder, new object[] { captureCompletion });
        });
        var window = new MainWindow(root, startPolling: false, recordingService: recorder); int checks = 0;
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(window.Dispatcher));
        T Field<T>(string name) => (T)typeof(MainWindow).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(window)!;
        void Set(string name, object? value) => typeof(MainWindow).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(window, value);
        object? Call(string name, params object?[] values) => typeof(MainWindow).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(window, values);
        void Await(Task task)
        {
            if (!task.IsCompleted)
            {
                var frame = new DispatcherFrame();
                var timeout = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) }; timeout.Tick += (_, _) => frame.Continue = false;
                _ = task.ContinueWith(_ => window.Dispatcher.BeginInvoke(() => frame.Continue = false), TaskScheduler.Default);
                timeout.Start(); Dispatcher.PushFrame(frame); timeout.Stop();
                if (!task.IsCompleted) throw new TimeoutException("offline UI operation did not complete");
            }
            task.GetAwaiter().GetResult();
        }
        void Action(string name, params object?[] values) => Await((Task)Call(name, values)!);
        void Check(bool good, string text) { if (!good) throw new Exception(text); checks++; }
        void Render(string name)
        {
            var view = (FrameworkElement)window.Content; view.Measure(new Size(1120, 1420)); view.Arrange(new Rect(0, 0, 1120, 1420)); view.UpdateLayout();
            var bitmap = new RenderTargetBitmap(1120, 1420, 96, 96, PixelFormats.Pbgra32); var background = new DrawingVisual();
            using (var draw = background.RenderOpen()) draw.DrawRectangle(window.Background, null, new Rect(0, 0, 1120, 1420));
            bitmap.Render(background); bitmap.Render(view); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var file = File.Create(Path.Combine(output, name + ".png")); encoder.Save(file);
        }
        var actors = Field<IList>("actors"); var list = Field<ListBox>("actorList"); var editor = Field<Border>("actorEditor");
        Check(actors.Count == 1, "initial actor missing");
        Check(typeof(MainWindow).GetField("source", BindingFlags.NonPublic | BindingFlags.Instance) is null, "global source mode still present");
        var sessionOnlyLabels = new[] { "播放", "暂停", "上一帧", "下一帧", "跳转到滑块位置", "保存书签", "载入书签", "应用位置与朝向" };
        foreach (string label in new[] { "添加演员", "移除所选演员", "播放", "暂停", "上一帧", "下一帧", "跳转到滑块位置", "保存书签", "载入书签", "应用位置与朝向", "应用配乐", "录制鸣潮窗口" })
            Check(Field<List<Button>>("actions").Any(b => Equals(b.Content, label) && b.IsEnabled == !sessionOnlyLabels.Contains(label)), "missing control or invalid idle state: " + label);
        Render("actors-1");
        var automatic = Field<CheckBox>("autoRecord"); var fps = Field<ComboBox>("recordingFps"); var size = Field<ComboBox>("recordingSize");
        Check(automatic.IsChecked == false && automatic.IsEnabled && fps.IsEnabled && size.IsEnabled && fps.SelectedIndex == 0 && size.SelectedIndex == 0,
            "automatic recording defaults off and recording options are editable before playback");
        var recPanel = Field<FrameworkElement>("recordingPanel"); var performContent = (Panel)recPanel.Parent;
        Check(recPanel.Visibility == Visibility.Visible && ReferenceEquals(Field<FrameworkElement>("idlePanel").Parent, performContent) &&
            performContent.Children.IndexOf(recPanel) < performContent.Children.IndexOf(Field<FrameworkElement>("idlePanel")),
            "recording settings are always outside the live-only panel and precede the start button");
        Check(!Field<Button>("stopRecordingButton").IsEnabled, "idle recording cannot be stopped before it begins");
        Check(Field<TextBlock>("recordingLocation").Text.Contains(recorder.OutputDirectory) && Path.IsPathFullyQualified(recorder.OutputDirectory) &&
            Field<List<Button>>("actions").Any(button => Equals(button.Content, "打开录屏文件夹") && button.IsEnabled),
            "recording settings show the actual absolute output folder and expose an open-folder action");
        Call("ShowPage", 2); Render("recording-before-playback"); Call("ShowPage", 0);
        foreach (int count in new[] { 4, 17, 33, 65 })
        {
            while (actors.Count < count) Action("AddActorAsync");
            Check(actors.Count == count && list.Items.Count == count, "actor list capped");
            list.SelectedIndex = count - 1; var selected = actors[count - 1]!;
            Check(ReferenceEquals(editor.Child, selected.GetType().GetProperty("Panel")!.GetValue(selected)), "selected actor editor is wrong");
            foreach (var card in actors) ((TextBox)card!.GetType().GetField("Body")!.GetValue(card)!).Text = Path.Combine(output, "unused.vmd");
            var request = (PlaybackSessionRequest)Call("Request")!;
            Check(request.Source == PlaybackSource.MixedActors && request.Actors.Count == count, "request split into old source route");
            Check(request.Actors.Select(actor => actor.Slot).Distinct().Count() == count && request.Actors[^1].Slot == count, "actor slots collided or capped");
            Check(request.Actors.Select(actor => actor.CharacterId).Distinct().Count() == 1, "duplicate role requests were dropped");
            if (count is 17 or 65) Render("actors-" + count);
        }
        var before = actors[6]; list.SelectedIndex = 5; Action("RemoveActorAsync");
        Check(actors.Count == 64 && ReferenceEquals(actors[5], before), "remove damaged later actors");
        var afterRemove = (PlaybackSessionRequest)Call("Request")!;
        Check(afterRemove.Actors.Select(a => a.Slot).SequenceEqual(Enumerable.Range(1, 64).Select(i => (uint)i)), "removed actor left duplicate slots");
        var restored = new PlaybackSessionRequest(PlaybackSource.MixedActors, Enumerable.Range(1, 17).Select(i => new PlaybackActorRequest("unused.vmd", CharacterId: afterRemove.Actors[0].CharacterId, Slot: (uint)i)).ToArray());
        Call("SetRequest", restored); Check(actors.Count == 17, "loading preparation truncated actors");
        Check(Field<TextBox>("duration").Text == "0", "duration should default to unlimited");
        Check(Field<string?>("session") is null, "offline rendering started a session");
        Set("session", Path.Combine(output, "nonexistent-offline-session")); Set("activeRequest", restored);
        void Rejected(string expected)
        {
            try { ((Task)Call("ApplySettingsAsync")!).GetAwaiter().GetResult(); throw new Exception("missing active settings validation"); }
            catch (InvalidOperationException error) { Check(error.Message.Contains(expected), "wrong active settings validation"); }
        }
        Field<ComboBox>("cameraMode").SelectedIndex = 2; Rejected("镜头 VMD");
        Field<ComboBox>("cameraMode").SelectedIndex = 0; Field<CheckBox>("native").IsChecked = false; Rejected("物理方式"); Field<CheckBox>("native").IsChecked = true;
        var targetType = typeof(MainWindow).GetNestedType("ActorTarget", BindingFlags.NonPublic)!; var targets = Array.CreateInstance(targetType, 2);
        targets.SetValue(Activator.CreateInstance(targetType, 1u, "A", restored.Actors[0]), 0); targets.SetValue(Activator.CreateInstance(targetType, 33u, "B", restored.Actors[1]), 1);
        var cache = Field<Dictionary<uint, SquadPlacement>>("livePlacements"); cache[1] = new(OffsetX: 101); cache[33] = new(OffsetX: 303);
        var target = Field<ComboBox>("placementTarget"); target.ItemsSource = targets;
        target.SelectedIndex = 0; Check(Field<TextBox>("liveX").Text == "101", "actor A placement lost");
        target.SelectedIndex = 1; Check(Field<TextBox>("liveX").Text == "303", "actor 33 placement lost");
        target.SelectedIndex = 0; Check(Field<TextBox>("liveX").Text == "101", "switching actor restored stale placement");
        // Reproduce the screenshot: native/managed cleanup is complete while
        // frontend session ownership and an audio task are still retained.
        var terminal = Path.Combine(output, "closed-session"); Directory.CreateDirectory(terminal);
        File.WriteAllText(Path.Combine(terminal, "owned_mmd_config.json"), "{}");
        var terminalState = new JsonObject { ["mode"] = "closed", ["session_active"] = false, ["restore_requested"] = true,
            ["restoration_complete"] = true, ["cleanup_complete"] = true, ["seconds"] = 219.25, ["duration_seconds"] = 223.133333,
            ["actor_count"] = 2, ["borrowed_count"] = 0, ["created_count"] = 2 };
        File.WriteAllText(Path.Combine(terminal, "mmd_result.json"), terminalState.ToJsonString());
        void ReleaseProof(bool released) => File.WriteAllText(Path.Combine(terminal, "session_result.json"), new JsonObject {
            ["mmd_restoration_verified"] = true, ["managed_owner_retirement_checked"] = released }.ToJsonString());
        var service = Field<PlaybackSessionService>("player");
        void RetainSession()
        {
            Set("session", terminal); Set("activeRequest", restored); Set("sessionControllable", true);
            typeof(PlaybackSessionService).GetProperty("ActiveSession")!.SetValue(service, terminal);
            Call("UpdateActions");
        }
        ReleaseProof(false); RetainSession(); Action("PollAsync");
        Check(Field<string?>("session") == terminal && Field<Button>("stopButton").IsEnabled,
            "pending managed restoration retains recovery ownership and the stop action");
        Check(Field<List<Button>>("sessionActions").All(button => !button.IsEnabled), "closed sessions disable obsolete transport and placement commands");
        ReleaseProof(true); Action("PollAsync");
        Check(Field<string?>("session") is null && service.ActiveSession is null && Field<PlaybackSessionRequest?>("activeRequest") is null,
            "confirmed terminal cleanup automatically retires frontend and service session ownership");
        Check(Field<FrameworkElement>("idlePanel").Visibility == Visibility.Visible && Field<FrameworkElement>("livePanel").Visibility == Visibility.Collapsed && !Field<TextBlock>("sessionLabel").Text.Contains("正在表演"),
            "confirmed cleanup returns the visible UI to idle");
        RetainSession(); cache[1] = new(OffsetX: 99); target.ItemsSource = targets;
        var media = Field<MediaService>("music");
        void FaultAudio() => typeof(MediaService).GetField("worker", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(media,
            Task.FromException(new Win32Exception(5, "synthetic audio shutdown failure")));
        FaultAudio();
        try { Action("StopCurrentAsync"); throw new Exception("audio failure should be reported"); }
        catch (Win32Exception error) { Check(error.NativeErrorCode == 5, "real audio shutdown failure remains visible"); }
        Check(Field<string?>("session") is null && service.ActiveSession is null && cache.Count == 0 && target.ItemsSource is null,
            "audio shutdown failure cannot strand restored actors in frontend session state");
        Action("StopCurrentAsync"); Check(true, "stop can be retried after the audio task fault was reported");
        RetainSession(); FaultAudio(); Action("PollAsync");
        Check(Field<string?>("session") is null && !Field<bool>("busy") && Field<FrameworkElement>("idlePanel").Visibility == Visibility.Visible,
            "automatic terminal reconciliation releases frontend state even when audio fails");
        Check(Field<TextBlock>("status").Text.Contains("synthetic audio shutdown failure"), "automatic reconciliation reports unexpected audio errors");
        Render("session-restored");
        Set("session", null); Set("activeRequest", null);
        // Exercise the same successful/failed launch completion path without
        // launching a game or capturing any real window.
        JsonObject PlaybackStarted() => new() { ["loaded"] = true, ["session_dir"] = terminal };
        Action("CompletePlaybackStartAsync", PlaybackStarted(), restored); Call("UpdateActions");
        Check(captures.Count == 0 && !recorder.IsBusy, "disabled automatic recording leaves playback unrecorded");
        Action("StopCurrentAsync"); automatic.IsChecked = true; fps.SelectedIndex = 1; size.SelectedIndex = 1;
        Action("CompletePlaybackStartAsync", new JsonObject { ["cleanup_restored"] = true }, restored);
        Check(captures.Count == 0 && Field<string?>("session") is null, "a restored failed launch never starts recording");
        Action("CompletePlaybackStartAsync", new JsonObject { ["loaded"] = false, ["session_dir"] = terminal }, restored);
        Check(captures.Count == 0 && !Field<bool>("sessionControllable"), "a failed launch awaiting restoration never starts recording");
        Action("StopCurrentAsync");
        Action("CompletePlaybackStartAsync", PlaybackStarted(), restored); Await(captureStarted.WaitAsync(TimeSpan.FromSeconds(5))); Call("UpdateActions");
        var selectedCapture = captures.Last();
        Check(captures.Count == 1 && selectedCapture.Fps == 60 && selectedCapture.Height == 0 && selectedCapture.Folder == Path.Combine(root, "playback", "recordings"),
            "successful playback automatically uses the preselected frame rate and original size without a dialog");
        Check(Field<string?>("automaticRecordingSession") == terminal && recorder.IsBusy, "automatic capture belongs to the started playback session");
        Action("StartAutomaticRecordingAsync"); Check(captures.Count == 1, "start completion cannot launch a duplicate recorder");
        Check(!fps.IsEnabled && !size.IsEnabled && !automatic.IsEnabled && !Field<Button>("recordButton").IsEnabled && Field<Button>("stopRecordingButton").IsEnabled,
            "active recording locks captured settings and exposes stop and save");
        Render("recording-playing"); Action("StopCurrentAsync");
        Check(captureStops == 1 && !recorder.IsBusy && Field<string?>("automaticRecordingSession") is null && Field<string?>("session") is null,
            "stopping playback stops and saves its automatic recording");
        Check(fps.IsEnabled && size.IsEnabled && automatic.IsEnabled && automatic.IsChecked == true && fps.SelectedIndex == 1 && size.SelectedIndex == 1,
            "recording preferences remain selected and become editable after stop");
        fps.SelectedIndex = 0; size.SelectedIndex = 0; Action("RecordAsync"); Await(captureStarted.WaitAsync(TimeSpan.FromSeconds(5)));
        Check(captures.Last().Fps == 30 && captures.Last().Height == 1080 && Field<string?>("automaticRecordingSession") is null,
            "manual recording before playback uses the same visible options");
        Action("CompletePlaybackStartAsync", PlaybackStarted(), restored);
        Check(captures.Count == 2 && recorder.IsBusy, "automatic playback reuses a running manual capture instead of starting another");
        Action("StopCurrentAsync"); Check(recorder.IsBusy && captureStops == 1, "playback stop does not take ownership of a manually started recording");
        Action("StopRecordingAsync"); Check(!recorder.IsBusy && captureStops == 2, "manual stop still saves the manual recording");
        Action("CompletePlaybackStartAsync", PlaybackStarted(), restored); Await(captureStarted.WaitAsync(TimeSpan.FromSeconds(5)));
        Await(media.StopAsync()); FaultAudio();
        try { Action("StopCurrentAsync"); throw new Exception("audio shutdown failure should remain visible"); }
        catch (Win32Exception) { Check(captureStops == 3 && !recorder.IsBusy && Field<string?>("session") is null, "audio shutdown failure cannot strand an automatic recording"); }
        Render("recording-after-stop");
        string NewTimelineSession()
        {
            var directory = Path.Combine(output, "recording-end-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "owned_mmd_config.json"), "{}"); return directory;
        }
        void LiveTimeline(string directory, string mode, bool looping = false) => File.WriteAllText(Path.Combine(directory, "mmd_status.json"), new JsonObject {
            ["mode"] = mode, ["loop"] = looping, ["session_active"] = true, ["restore_requested"] = false,
            ["seconds"] = 3d, ["duration_seconds"] = 3d, ["actor_count"] = 2, ["borrowed_count"] = 0, ["created_count"] = 2 }.ToJsonString());
        void RestoredTimeline(string directory)
        {
            File.WriteAllText(Path.Combine(directory, "session_result.json"), "{\"mmd_restoration_verified\":true,\"managed_owner_retirement_checked\":true}");
            File.WriteAllText(Path.Combine(directory, "mmd_result.json"), terminalState.ToJsonString());
        }
        var naturalEnd = NewTimelineSession(); LiveTimeline(naturalEnd, "playing");
        Action("CompletePlaybackStartAsync", new JsonObject { ["loaded"] = true, ["session_dir"] = naturalEnd }, restored);
        Await(captureStarted.WaitAsync(TimeSpan.FromSeconds(5))); var stopsBeforeEnd = captureStops;
        LiveTimeline(naturalEnd, "paused"); Action("PollAsync");
        Check(recorder.IsBusy && captureStops == stopsBeforeEnd && Field<TextBlock>("modeLabel").Text == "已暂停", "pause does not automatically stop recording even when the cursor reaches clip duration");
        LiveTimeline(naturalEnd, "playing", looping: true); Action("PollAsync");
        Check(recorder.IsBusy && captureStops == stopsBeforeEnd, "a looping timeline continues recording at the end boundary");
        LiveTimeline(naturalEnd, "ended", looping: true); Action("PollAsync");
        Check(recorder.IsBusy && captureStops == stopsBeforeEnd && Field<TextBlock>("modeLabel").Text == "动作结束", "loop-enabled state cannot trigger a natural-end save");
        finalization = new(TaskCreationOptions.RunContinuationsAsynchronously); captureStopped = new(TaskCreationOptions.RunContinuationsAsynchronously);
        captureCompletion = new RecordingSnapshot("saved", "已保存：automatic-natural-end.mp4", OutputPath: Path.Combine(output, "automatic-natural-end.mp4"));
        LiveTimeline(naturalEnd, "ended"); var saveAtEnd = (Task)Call("PollAsync")!; Await(captureStopped.Task.WaitAsync(TimeSpan.FromSeconds(5)));
        Check(!saveAtEnd.IsCompleted && recorder.IsBusy && Field<bool>("busy") && Field<string?>("automaticRecordingSession") is null &&
            Field<TextBlock>("status").Text.Contains("正在自动保存"), "natural end cancels capture once and awaits encoding and saving without reporting success early");
        finalization.SetResult(); Await(saveAtEnd);
        Check(!recorder.IsBusy && !Field<bool>("busy") && captureStops == stopsBeforeEnd + 1 && Field<TextBlock>("status").Text.Contains("已保存：automatic-natural-end.mp4"),
            "natural end completes automatic saving and reports the saved filename");
        Check(Field<string?>("session") == naturalEnd && Field<FrameworkElement>("livePanel").Visibility == Visibility.Visible && !File.Exists(Path.Combine(naturalEnd, "stop.flag")),
            "saving the finished recording preserves the actors' last pose and restoration ownership");
        Action("PollAsync"); Action("PollAsync");
        Check(captureStops == stopsBeforeEnd + 1, "repeated ended polls never finalize the same recording twice");
        Check(Equals(Field<TextBlock>("recordingStatus").ToolTip, captureCompletion.OutputPath), "saved recording exposes its complete file path");
        Render("recording-saved-at-end"); finalization = null; captureStopped = null; captureCompletion = null;
        RestoredTimeline(naturalEnd); Action("StopCurrentAsync");
        var manualEnd = NewTimelineSession(); LiveTimeline(manualEnd, "playing");
        Action("RecordAsync"); Await(captureStarted.WaitAsync(TimeSpan.FromSeconds(5)));
        Action("CompletePlaybackStartAsync", new JsonObject { ["loaded"] = true, ["session_dir"] = manualEnd }, restored);
        var manualStops = captureStops; LiveTimeline(manualEnd, "ended"); Action("PollAsync");
        Check(recorder.IsBusy && captureStops == manualStops, "natural playback end does not terminate a manually started recording");
        Action("StopRecordingAsync"); RestoredTimeline(manualEnd); Action("StopCurrentAsync");
        var errorEnd = NewTimelineSession(); LiveTimeline(errorEnd, "playing");
        Action("CompletePlaybackStartAsync", new JsonObject { ["loaded"] = true, ["session_dir"] = errorEnd }, restored); Await(captureStarted.WaitAsync(TimeSpan.FromSeconds(5)));
        captureCompletion = new RecordingSnapshot("error", "synthetic disk full"); LiveTimeline(errorEnd, "ended"); Action("PollAsync");
        Check(Field<TextBlock>("status").Text.Contains("保存失败：synthetic disk full") && !Field<bool>("busy") && Field<string?>("session") == errorEnd,
            "automatic save errors remain visible and do not discard actor restoration ownership");
        captureCompletion = null; RestoredTimeline(errorEnd); Action("StopCurrentAsync");
        while (actors.Count > 0) { list.SelectedIndex = 0; Action("RemoveActorAsync"); }
        try { Call("Request"); throw new Exception("empty actor list accepted"); }
        catch (TargetInvocationException error) { Check(error.InnerException is InvalidOperationException, "empty list validation changed"); }
        Action("AddActorAsync"); Check(actors.Count == 1 && editor.Child is not null, "cannot add after clearing list");
        Console.WriteLine($"PASS {checks} dynamic UI checks; offscreen actor and recording views, synthetic capture lifecycle; no window shown or game access.");
        return 0;
    }
}
