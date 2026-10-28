using System.Collections;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using WWPoser;
using WWPoser.Services;

internal static class PresetUiChecks
{
    public static int Run(MainWindow window, string root, string output, Action<Task> wait)
    {
        const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        T Field<T>(string name) => (T)typeof(MainWindow).GetField(name, Private)!.GetValue(window)!;
        void Set(string name, object? value) => typeof(MainWindow).GetField(name, Private)!.SetValue(window, value);
        object? Call(string name, params object?[] values) => typeof(MainWindow).GetMethod(name, Private)!.Invoke(window, values);
        void Run(string name, params object?[] values) => wait((Task)Call(name, values)!);
        int checks = 0;
        void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
        void Rejected(Action run, string message)
        {
            try { run(); }
            catch (Exception e) when (e is InvalidOperationException or InvalidDataException || e is TargetInvocationException { InnerException: InvalidOperationException }) { checks++; return; }
            throw new Exception(message);
        }
        var folder = Path.Combine(output, "preset-fixture"); Directory.CreateDirectory(folder);
        string Material(string name, string bytes) { var path = Path.Combine(folder, name); File.WriteAllText(path, bytes); return path; }
        var body = Material("舞蹈.vmd", "body-a"); var second = Material("其他动作.vmd", "body-b");
        var face = Material("表情.vmd", "face"); var camera = Material("镜头.vmd", "camera"); var music = Material("音乐.flac", "audio");
        var gaze = Material("目线.vmd", "gaze");
        var catalog = OwnedCharacterCatalog.Load(root);
        var dark = catalog.Characters.Single(c => c.RoleId == 1211 && c.Form is not null);
        var normal = catalog.Characters.First(c => c.RoleId == dark.RoleId && c.SkinId == dark.SkinId && c.Form is null);
        var request = new PlaybackSessionRequest(PlaybackSource.MixedActors, [
            new(body, face, .8, new(12, 23, 4, -90, false), normal.RoleId, true, normal.CharacterId, 1, new(123, 456, 7, 45), gaze, .65),
            new(second, Scale: 1.2, Placement: new(-21, -31, 5, 90), RoleId: normal.RoleId, Enabled: false, CharacterId: normal.CharacterId, Slot: 2, Stage: new(-15, -31, 2, -45)),
            new(body, RoleId: dark.RoleId, CharacterId: dark.CharacterId, Slot: 3)
        ], camera, 1, 1.333, true, 123.5, true, false, SpacingCm: 234, FormationNative: true, HideSceneCharacters: true, CameraTrackHeightCm: 145.5);
        Call("SetRequest", request);
        Field<TextBox>("audio").Text = music; Field<TextBox>("offset").Text = "-1.25"; Field<TextBox>("volume").Text = "0.67";
        Field<CheckBox>("autoRecord").IsChecked = true; Field<ComboBox>("recordingFps").SelectedIndex = 1; Field<ComboBox>("recordingSize").SelectedIndex = 1;
        Field<ComboBox>("recordingFormat").SelectedIndex = 1;
        foreach (var label in new[] { "保存预设", "加载预设", "另存为" })
            Check(Field<List<Button>>("actions").Any(b => Equals(b.Content, label) && b.IsEnabled), "preset sidebar control missing: " + label);
        var path = Path.Combine(folder, "完整表演.wwpreset"); Run("SavePresetFileAsync", path);
        Check(!(bool)Call("HasPresetChanges")!, "saved snapshot is clean");
        foreach (var source in new[] { body, second, face, gaze, camera, music }) File.Move(source, source + ".moved", true);
        Call("SetRequest", new PlaybackSessionRequest(PlaybackSource.MixedActors, [])); Field<TextBox>("audio").Text = "";
        Field<CheckBox>("autoRecord").IsChecked = false; Field<ComboBox>("recordingFps").SelectedIndex = 0; Field<ComboBox>("recordingSize").SelectedIndex = 0;
        Field<ComboBox>("recordingFormat").SelectedIndex = 0;
        Set("preparedDirectory", "old-compiled-session"); Run("LoadPresetFileAsync", path);
        var restored = (ShowPreset)Call("CapturePreset")!; var actors = Field<IList>("actors");
        Check(actors.Count == 3 && restored.Playback.Actors[0].CharacterId == normal.CharacterId && restored.Playback.Actors[1].CharacterId == normal.CharacterId && restored.Playback.Actors[2].CharacterId == dark.CharacterId,
            "duplicate actors and Daniya base/dark forms retain separate slots");
        Check(!restored.Playback.Actors[1].Enabled && restored.Playback.Actors[0].BodySource != restored.Playback.Actors[1].BodySource && restored.Playback.Actors[1].FaceSource is null,
            "each actor retains its own enabled, motion and expression state");
        Check(File.ReadAllText(restored.Playback.Actors[0].GazeSource!) == "gaze" && restored.Playback.Actors[0].GazeStrength == .65 && restored.Playback.Actors[1].GazeSource is null,
            "gaze editor preserves each actor's own track and strength through preset loading");
        Check(restored.Playback with { Actors = request.Actors, CameraSource = request.CameraSource } == request && restored.Playback.Actors[0].Placement == request.Actors[0].Placement && restored.Playback.Actors[1].Stage == request.Actors[1].Stage,
            "all playback settings, camera choice, formation and positions restore");
        Check(File.ReadAllText(restored.Playback.Actors[0].BodySource) == "body-a" && File.ReadAllText(restored.Playback.Actors[1].BodySource) == "body-b" && File.ReadAllText(restored.Playback.CameraSource!) == "camera" && File.ReadAllText(restored.Music!.AudioFile) == "audio",
            "loaded editors use bundled material after originals move");
        Check(restored.Recording == new PresetRecordingSettings(true, 60, 1440, RecordingFormat.Compatible) && Field<ComboBox>("recordingFormat").SelectedIndex == 1 && restored.Music?.OffsetSeconds == -1.25 && restored.Music.Volume == .67,
            "audio timing/volume, automatic recording and H.264 compatibility selection restore");
        Check(Field<string?>("preparedDirectory") is null && Field<string?>("session") is null && Field<PlaybackSessionService>("player").ActiveSession is null,
            "loading discards old preparation without launching game actors");
        Check(Field<FrameworkElement>("stageMapPanel").Visibility == Visibility.Collapsed && Field<ComboBox>("cameraMode").SelectedIndex == 1,
            "saved native formation stays collapsed and saved camera is not auto-overridden");
        Check(!(bool)Call("HasPresetChanges")!, "loading establishes clean authoring state");
        Field<ComboBox>("recordingFormat").SelectedIndex = 0; Check((bool)Call("HasPresetChanges")!, "recording format edits count as unsaved"); Field<ComboBox>("recordingFormat").SelectedIndex = 1;
        Field<TextBox>("volume").Text = "0.5"; Check((bool)Call("HasPresetChanges")!, "music edits count as unsaved"); Field<TextBox>("volume").Text = "0.67";
        foreach (var field in new[] { "session", "previewSession" })
        {
            Set(field, "synthetic-owned-session"); Call("UpdateActions");
            Check(!Field<Button>("loadPresetButton").IsEnabled, "loading disabled while " + field + " is owned");
            Rejected(() => Run("LoadPresetFileAsync", path), "active session did not block preset loading"); Set(field, null);
        }
        var recorder = Field<RecordingService>("recorder"); wait(recorder.StartAsync(folder, 30, 1080)); Call("UpdateRecordingControls");
        Check(!Field<ComboBox>("recordingFormat").IsEnabled, "recording locks format alongside captured settings");
        Check(!Field<Button>("loadPresetButton").IsEnabled, "recording disables preset load");
        Rejected(() => Run("LoadPresetFileAsync", path), "recording did not block loading"); wait(recorder.StopAsync()); Call("UpdateActions");
        var unknown = Path.Combine(folder, "unknown.wwpreset");
        wait(new PresetService(Path.Combine(folder, "cache")).SaveAsync(unknown, restored with { Playback = restored.Playback with {
            Actors = [restored.Playback.Actors[0] with { CharacterId = "missing-role-form", RoleId = 999999 }] }, ActorNames = ["不存在的形态"] }));
        Run("LoadPresetFileAsync", unknown); var unknownCard = actors[0]!;
        var character = (ComboBox)unknownCard.GetType().GetProperty("Character")!.GetValue(unknownCard)!;
        Check(character.SelectedItem is null && ((ShowPreset)Call("CapturePreset")!).Playback.Actors[0].CharacterId == "missing-role-form" && Field<TextBlock>("status").Text.Contains("不存在的形态"),
            "missing character stays explicit, is not silently substituted and retains its ID");
        Rejected(() => Call("BuildRequest", false), "unknown role allowed a stage preview");
        character.SelectedValue = normal.CharacterId;
        Check(((PlaybackSessionRequest)Call("BuildRequest", false)!).Actors[0].CharacterId == normal.CharacterId, "manual reselection resolves missing role");
        var invalid = Material("invalid.wwpreset", "not a ZIP"); var previousCard = actors[0];
        Rejected(() => Run("LoadPresetFileAsync", invalid), "invalid archive accepted");
        Check(ReferenceEquals(previousCard, actors[0]), "failed load leaves current actors unchanged");
        Run("LoadPresetFileAsync", path); restored = (ShowPreset)Call("CapturePreset")!;
        var targetType = typeof(MainWindow).GetNestedType("ActorTarget", BindingFlags.NonPublic)!;
        var target = Activator.CreateInstance(targetType, 1u, "first actor", restored.Playback.Actors[0])!;
        var liveCards = Field<IDictionary>("liveActorCards"); liveCards.Clear(); liveCards[1u] = actors[0];
        Call("RememberLivePlacement", target, new SquadPlacement(101, -102, 3.5, 67, false));
        var live = (ShowPreset)Call("CapturePreset")!;
        Check(live.Playback.Actors[0].Placement == new SquadPlacement(101, -102, 3.5, 67, false) && live.Playback.Actors[1].Placement == restored.Playback.Actors[1].Placement,
            "applied live placement persists to only its authoring actor");
        Field<ListBox>("actorList").SelectedIndex = 0; Run("RemoveActorAsync");
        Call("RememberLivePlacement", target, new SquadPlacement(999));
        Check(((ShowPreset)Call("CapturePreset")!).Playback.Actors[0].Placement == restored.Playback.Actors[1].Placement,
            "removing an actor does not redirect its live placement into a duplicate");
        Run("LoadPresetFileAsync", path);
        Console.WriteLine($"PASS {checks} show-preset UI checks.");
        return checks;
    }
}
