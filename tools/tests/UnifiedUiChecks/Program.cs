using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WWPoser;
using WWPoser.Services;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        string root = Path.GetFullPath(args[0]), output = Path.GetFullPath(args[1]); Directory.CreateDirectory(output);
        _ = new Application(); var window = new MainWindow(root, startPolling: false); int checks = 0;
        T Field<T>(string name) => (T)typeof(MainWindow).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(window)!;
        void Set(string name, object? value) => typeof(MainWindow).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(window, value);
        object? Call(string name, params object?[] values) => typeof(MainWindow).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(window, values);
        void Action(string name) => ((Task)Call(name)!).GetAwaiter().GetResult();
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
        while (actors.Count > 0) { list.SelectedIndex = 0; Action("RemoveActorAsync"); }
        try { Call("Request"); throw new Exception("empty actor list accepted"); }
        catch (TargetInvocationException error) { Check(error.InnerException is InvalidOperationException, "empty list validation changed"); }
        Action("AddActorAsync"); Check(actors.Count == 1 && editor.Child is not null, "cannot add after clearing list");
        Console.WriteLine($"PASS {checks} dynamic UI checks; 1/17/65 actor offscreen PNGs; no window shown, event loop or game access.");
        return 0;
    }
}
