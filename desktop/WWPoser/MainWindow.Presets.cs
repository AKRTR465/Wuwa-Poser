using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using WWPoser.Services;

namespace WWPoser;

public sealed partial class MainWindow
{
    private readonly PresetService presets = new();
    private readonly TextBlock presetLabel = Text("尚未保存预设", 12, "Muted");
    private Button? loadPresetButton;
    private string? currentPresetPath, cleanPresetSignature;

    private UIElement BuildPresetControls()
    {
        var panel = new StackPanel { Margin = new(22, 22, 22, 0) };
        panel.Children.Add(Text("整场预设", 14, "Ink", FontWeights.SemiBold));
        presetLabel.Margin = new(0, 5, 0, 10); presetLabel.TextWrapping = TextWrapping.Wrap; panel.Children.Add(presetLabel);
        panel.Children.Add(Action("保存预设", SavePresetAsync, "\uE74E", "SoftButton"));
        loadPresetButton = Action("加载预设", LoadPresetAsync, "\uE8E5", "SoftButton"); panel.Children.Add(loadPresetButton);
        panel.Children.Add(Action("另存为", SavePresetAsAsync, "\uE792", "LinkButton"));
        var hint = Text("演员、动作、表情、目线、镜头、音乐和设置一起保存。", 11, "Muted"); hint.Margin = new(0, 7, 0, 0); panel.Children.Add(hint);
        return panel;
    }
    private ShowPreset CapturePreset() => new(
        currentPresetPath is null ? "未命名表演" : Path.GetFileNameWithoutExtension(currentPresetPath), CapturePlaybackRequest(),
        new MusicPreset(Optional(audio) ?? "", Number(offset, -3600, 3600), Number(volume, 0, 1)),
        new PresetRecordingSettings(autoRecord.IsChecked == true, recordingFps.SelectedIndex == 0 ? 30 : 60,
            recordingSize.SelectedIndex switch { 0 => 1080, 1 => 1440, _ => 0 },
            recordingFormat.SelectedIndex == 0 ? RecordingFormat.Compact : RecordingFormat.Compatible), actors.Select(card => card.CharacterName).ToArray());

    private string PresetSignature() => JsonSerializer.Serialize(CapturePreset());
    private void MarkPresetClean() => cleanPresetSignature = PresetSignature();
    private bool HasPresetChanges()
    {
        try { return cleanPresetSignature != PresetSignature(); }
        catch (Exception) { return true; }
    }
    private static string PresetFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WuwaPoser", "Presets");
    private string? ChoosePresetDestination()
    {
        Directory.CreateDirectory(PresetFolder);
        var dialog = new SaveFileDialog { Title = "保存整场表演预设", Filter = "表演预设|*.wwpreset", DefaultExt = ".wwpreset", AddExtension = true,
            FileName = currentPresetPath is null ? "我的表演.wwpreset" : Path.GetFileName(currentPresetPath),
            InitialDirectory = currentPresetPath is null ? PresetFolder : Path.GetDirectoryName(currentPresetPath) };
        return dialog.ShowDialog(this) == true ? dialog.FileName : null;
    }
    private async Task SavePresetAsync()
    {
        if ((currentPresetPath ?? ChoosePresetDestination()) is { } path) await SavePresetFileAsync(path);
    }
    private async Task SavePresetAsAsync()
    {
        if (ChoosePresetDestination() is { } path) await SavePresetFileAsync(path);
    }
    private async Task SavePresetFileAsync(string path)
    {
        var snapshot = CapturePreset() with { Name = Path.GetFileNameWithoutExtension(path) };
        status.Text = "正在打包预设与原始素材…";
        await Task.Run(() => presets.SaveAsync(path, snapshot, Token), Token);
        currentPresetPath = Path.GetFullPath(path);
        // Editors remain usable during file IO; later edits must still count as unsaved.
        cleanPresetSignature = JsonSerializer.Serialize(snapshot);
        presetLabel.Text = Path.GetFileNameWithoutExtension(path); presetLabel.ToolTip = currentPresetPath;
        status.Text = $"预设已保存（{snapshot.Playback.Actors.Count} 位演员）：{currentPresetPath}";
    }
    private void RequirePresetLoadIdle()
    {
        if (session is not null || previewSession is not null || player.ActiveSession is not null || recorder.IsBusy)
            throw new InvalidOperationException("请先停止并恢复表演、结束站位预览和录屏，再加载预设。");
    }
    private async Task LoadPresetAsync()
    {
        RequirePresetLoadIdle();
        var dialog = new OpenFileDialog { Title = "加载整场表演预设", Filter = "表演预设|*.wwpreset", CheckFileExists = true };
        if (dialog.ShowDialog(this) != true) return;
        if (HasPresetChanges())
        {
            var choice = MessageBox.Show(this, "加载会替换当前配置。先保存当前表演吗？", "加载预设", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (choice == MessageBoxResult.Cancel) return;
            if (choice == MessageBoxResult.Yes)
            {
                var path = currentPresetPath ?? ChoosePresetDestination(); if (path is null) return;
                await SavePresetFileAsync(path);
            }
        }
        await LoadPresetFileAsync(dialog.FileName);
    }
    private async Task LoadPresetFileAsync(string path)
    {
        RequirePresetLoadIdle(); status.Text = "正在读取预设并校验素材…";
        var result = await Task.Run(() => presets.LoadAsync(path, Token), Token);
        RequirePresetLoadIdle();
        var preset = result.Preset; var warnings = result.Warnings.ToList();
        SetRequest(preset.Playback);
        for (var i = 0; i < actors.Count; i++)
            if (actors[i].MissingCharacterId is { } missing)
            {
                actors[i].MarkMissingCharacter(missing, actors[i].MissingRoleId, preset.ActorNames?[i]);
                warnings.Add($"演员 {i + 1} 的角色或形态在当前版本不存在：{preset.ActorNames?[i] ?? missing}。请重新选择。");
            }
        var musicPreset = preset.Music ?? new MusicPreset("");
        audio.Text = musicPreset.AudioFile; offset.Text = F(musicPreset.OffsetSeconds); volume.Text = F(musicPreset.Volume);
        var recording = preset.Recording ?? new PresetRecordingSettings(); autoRecord.IsChecked = recording.Automatic;
        recordingFps.SelectedIndex = recording.Fps == 30 ? 0 : 1; recordingSize.SelectedIndex = recording.Height switch { 1080 => 0, 1440 => 1, _ => 2 };
        recordingFormat.SelectedIndex = recording.Format == RecordingFormat.Compact ? 0 : 1;
        preparedDirectory = null; preparation.Text = "已载入预设；开始时会按当前版本准备动作。";
        currentPresetPath = Path.GetFullPath(path); presetLabel.Text = Path.GetFileNameWithoutExtension(path); presetLabel.ToolTip = currentPresetPath;
        MarkPresetClean(); RefreshActorList(); UpdateActions();
        status.Text = warnings.Count == 0 ? $"已载入预设：{presetLabel.Text} · {actors.Count} 位演员，可以继续编辑或开始表演。" :
            $"已载入预设 · {warnings.Count} 项待补全：" + string.Join("；", warnings.Distinct());
        if (warnings.Count > 0) MessageBoxForPresetWarnings(warnings);
    }
    private void MessageBoxForPresetWarnings(List<string> warnings)
    {
        // Offscreen checks never create a modal window. Normal use shows every missing item.
        if (IsVisible) MessageBox.Show(this, string.Join("\n", warnings.Distinct()), "预设需要补全", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    private void RememberLivePlacement(ActorTarget target, SquadPlacement placement)
    {
        livePlacements[target.Slot] = placement;
        if (!liveActorCards.TryGetValue(target.Slot, out var card) || !actors.Contains(card) ||
            card.Character.SelectedValue?.ToString() != target.Actor.CharacterId ||
            !string.Equals(Optional(card.Body) ?? "", target.Actor.BodySource, StringComparison.OrdinalIgnoreCase)) return;
        card.X.Text = F(placement.OffsetX); card.Y.Text = F(placement.OffsetY); card.Height.Text = F(placement.Height);
        card.Yaw.Text = F(placement.Yaw); card.InPlace.IsChecked = placement.InPlace;
    }
}
