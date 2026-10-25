using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using WWPoser.Services;

namespace WWPoser;

public sealed partial class MainWindow
{
    private readonly TextBlock actorCount = Text("", 12, "Muted");
    private readonly TextBlock status = Text("你好！先在左边添加演员，为 TA 选一个角色和一段舞蹈动作吧。", 13, "InkSoft");
    private readonly TextBlock preparation = Text("提前准备会先把动作转换好，之后开始更快；不准备也可以直接开始。", 12, "Muted");
    private readonly TextBlock sessionLabel = Text("还没有开始表演", 13, "Ink", FontWeights.SemiBold);
    private readonly TextBlock time = new() { Text = "00:00.00 / 00:00.00", FontSize = 30, FontWeight = FontWeights.SemiBold };
    private readonly TextBlock modeLabel = Text("等待开始", 12, "AccentPressed"), recordingStatus = Text("录屏尚未开始", 12, "Muted");
    private readonly TextBlock recordingLocation = Text("", 12, "Muted");
    private readonly TextBox camera = new(), audio = new();
    private readonly TextBox speed = new() { Text = "1", Tag = "倍" }, duration = new() { Text = "0", Tag = "秒" }, spacing = new() { Text = "180", Tag = "厘米" };
    private readonly TextBox offset = new() { Text = "0", Tag = "秒" }, volume = new() { Text = "1", Tag = "0–1" };
    private readonly CheckBox loop = new() { Content = "放完后从头再来，一直循环" };
    private readonly CheckBox native = new() { Content = "使用游戏自带的头发与衣物摆动（推荐）", IsChecked = true }, secondary = new() { Content = "额外增强衣物惯性" };
    private readonly ComboBox cameraMode = new() { ItemsSource = new[] { "游戏跟随镜头", "固定镜头", "VMD 镜头轨道" }, SelectedIndex = 0, Width = 240, HorizontalAlignment = HorizontalAlignment.Left };
    private readonly ComboBox placementMode = new() { ItemsSource = new[] { "站位舞台", "原作队形（共用原点）" }, SelectedIndex = 1, Width = 240, HorizontalAlignment = HorizontalAlignment.Left };
    private readonly ComboBox recordingFps = new() { ItemsSource = new[] { "30 FPS", "60 FPS" }, SelectedIndex = 0, Width = 120, Margin = new(0, 0, 8, 0) };
    private readonly ComboBox recordingSize = new() { ItemsSource = new[] { "1080p", "2K（2560×1440）", "原始尺寸" }, SelectedIndex = 0, Width = 190, Margin = new(0, 0, 8, 0) };
    private readonly CheckBox autoRecord = new() { Content = "开始播放时自动录屏", IsChecked = false };
    private readonly CheckBox hideSceneCharacters = new() { Content = "播放时隐藏附近 NPC 和怪物模型", IsChecked = false };
    private readonly Slider progress = new() { Minimum = 0, Maximum = 1, IsMoveToPointEnabled = true, Margin = new(0, 14, 0, 10) };
    private readonly ComboBox placementTarget = new() { DisplayMemberPath = nameof(ActorTarget.Name), Width = 240, HorizontalAlignment = HorizontalAlignment.Left };
    private readonly TextBox liveX = new() { Text = "0", Tag = "厘米" }, liveY = new() { Text = "0", Tag = "厘米" }, liveHeight = new() { Text = "0", Tag = "厘米" }, liveYaw = new() { Text = "0", Tag = "度" };
    private readonly CheckBox liveInPlace = new() { Content = "原地跳（不随动作走动）" };
    private readonly ListBox actorList = new();
    private readonly Border actorEditor = new() { VerticalAlignment = VerticalAlignment.Top };
    private readonly ProgressBar busyBar = new() { IsIndeterminate = true, Visibility = Visibility.Hidden };
    private readonly Ellipse sessionDot = new() { Width = 10, Height = 10, Margin = new(0, 0, 8, 0) };
    private readonly StackPanel readiness = new();
    private readonly ContentControl pageHost = new();
    private readonly List<FrameworkElement> pages = [];
    private readonly List<StepEntry> steps = [];
    private readonly List<(Button Chip, string Value)> speedChips = [];
    private FrameworkElement emptyRoster = null!, idlePanel = null!, livePanel = null!, recordingPanel = null!;
    private FrameworkElement stageMapPanel = null!, cameraFileDrop = null!;
    private Button stopButton = null!, applySettings = null!, recordButton = null!, stopRecordingButton = null!;
    private int page;
    private bool wasLive;

    private void BuildWindow()
    {
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/WWPoser;component/Theme.xaml") });
        Icon = BitmapFrame.Create(new Uri("pack://application:,,,/WWPoser;component/Assets/WuwaPoser.ico"));
        Title = "鸣潮 MMD 播放器"; Width = 1240; Height = 860; MinWidth = 1000; MinHeight = 680;
        FontFamily = new FontFamily("Segoe UI, Microsoft YaHei UI"); FontSize = 13; Background = (Brush)Resources["Paper"];
        TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
        SetResourceReference(ForegroundProperty, "Ink"); time.SetResourceReference(TextBlock.ForegroundProperty, "Ink");
        time.FontFamily = (FontFamily)Resources["NumberFont"];
        SourceInitialized += (_, _) => TintCaption();

        var shell = new Grid(); Content = shell;
        shell.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(260) }); shell.ColumnDefinitions.Add(new ColumnDefinition());
        shell.Children.Add(BuildSidebar());
        var main = new Grid(); Grid.SetColumn(main, 1); shell.Children.Add(main);
        main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); main.RowDefinitions.Add(new RowDefinition()); main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        main.Children.Add(busyBar);
        Grid.SetRow(pageHost, 1); main.Children.Add(pageHost);
        var helper = BuildHelperBar(); Grid.SetRow(helper, 2); main.Children.Add(helper);

        actorList.ItemsSource = actors; actorList.ItemTemplate = (DataTemplate)Resources["ActorItem"]; actorList.SelectionChanged += (_, _) => SelectActor();
        VirtualizingPanel.SetIsVirtualizing(actorList, true); VirtualizingPanel.SetVirtualizationMode(actorList, VirtualizationMode.Recycling);
        ScrollViewer.SetHorizontalScrollBarVisibility(actorList, ScrollBarVisibility.Disabled);
        pages.Add(BuildActorsPage()); pages.Add(BuildStagePage()); pages.Add(BuildPerformPage());
        camera.TextChanged += (_, _) => UpdateGuide(); audio.TextChanged += (_, _) => UpdateGuide(); cameraMode.SelectionChanged += (_, _) => UpdateGuide();
        placementMode.SelectionChanged += (_, _) => { UpdateStageMapVisibility(); UpdateGuide(); };
        hideSceneCharacters.Checked += (_, _) => UpdateGuide(); hideSceneCharacters.Unchecked += (_, _) => UpdateGuide();
        speed.TextChanged += (_, _) => UpdateSpeedChips();
        _ = AddActorAsync(); ShowPage(0); UpdateSpeedChips(); UpdateActions();
    }

    // ── Layout ────────────────────────────────────────────────────────────

    private UIElement BuildSidebar()
    {
        var side = new DockPanel { LastChildFill = false }; side.SetResourceReference(Panel.BackgroundProperty, "Sidebar");
        var brand = new StackPanel { Orientation = Orientation.Horizontal, Margin = new(22, 26, 22, 0) };
        var brandIcon = new Image { Source = BitmapFrame.Create(new Uri("pack://application:,,,/WWPoser;component/Assets/WuwaPoser.png")), Width = 40, Height = 40, VerticalAlignment = VerticalAlignment.Center };
        RenderOptions.SetBitmapScalingMode(brandIcon, BitmapScalingMode.HighQuality); brand.Children.Add(brandIcon);
        var name = new StackPanel { Margin = new(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        name.Children.Add(Text("鸣潮 MMD 播放器", 16, "Ink", FontWeights.SemiBold)); name.Children.Add(Text("让喜欢的角色在游戏里跳起来", 12, "Muted"));
        brand.Children.Add(name); DockPanel.SetDock(brand, Dock.Top); side.Children.Add(brand);
        var welcome = Text("跟着三步走，第一次用也不会迷路。", 12, "InkSoft"); welcome.Margin = new(24, 22, 22, 12); DockPanel.SetDock(welcome, Dock.Top); side.Children.Add(welcome);
        var list = new StackPanel { Margin = new(12, 0, 12, 0) };
        foreach (var (title, index) in new[] { ("演员与动作", 0), ("站位与舞台", 1), ("开始表演", 2) })
        {
            var entry = new StepEntry(index, title); entry.Button.Click += (_, _) => ShowPage(index); steps.Add(entry); list.Children.Add(entry.Button);
        }
        DockPanel.SetDock(list, Dock.Top); side.Children.Add(list);
        var presetControls = BuildPresetControls(); DockPanel.SetDock(presetControls, Dock.Top); side.Children.Add(presetControls);

        var sessionCard = new Border { CornerRadius = new(14), Padding = new(16, 14, 16, 14), Margin = new(14, 0, 14, 18) }; sessionCard.SetResourceReference(Border.BackgroundProperty, "Card");
        var sessionBody = new StackPanel(); var line = new StackPanel { Orientation = Orientation.Horizontal }; line.Children.Add(sessionDot); line.Children.Add(sessionLabel);
        sessionLabel.TextWrapping = TextWrapping.Wrap; sessionLabel.MaxWidth = 190;
        sessionBody.Children.Add(line);
        var note = Text("结束时点「停止并恢复」，队伍会变回原来的样子。", 11, "Muted"); note.Margin = new(18, 6, 0, 0); sessionBody.Children.Add(note);
        sessionCard.Child = sessionBody; DockPanel.SetDock(sessionCard, Dock.Bottom); side.Children.Add(sessionCard);
        return side;
    }

    private UIElement BuildHelperBar()
    {
        var bar = new Border { BorderThickness = new(0, 1, 0, 0), Padding = new(28, 12, 28, 12) };
        bar.SetResourceReference(Border.BackgroundProperty, "Card"); bar.SetResourceReference(Border.BorderBrushProperty, "Line");
        var row = new DockPanel();
        var recording = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new(16, 0, 0, 0) };
        recording.Children.Add(Glyph("\uE714", 13, "Muted", new(0, 0, 6, 0))); recording.Children.Add(recordingStatus);
        DockPanel.SetDock(recording, Dock.Right); row.Children.Add(recording);
        row.Children.Add(Badge("\uE8BD", "Accent", "AccentSoft", 30, 13)); status.Margin = new(12, 0, 0, 0); status.VerticalAlignment = VerticalAlignment.Center; row.Children.Add(status);
        bar.Child = row; return bar;
    }

    private FrameworkElement BuildActorsPage()
    {
        var body = new Grid { Margin = new(36, 0, 36, 0) };
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) }); body.ColumnDefinitions.Add(new ColumnDefinition());

        var roster = new DockPanel();
        var heading = new DockPanel { Margin = new(2, 0, 2, 10) };
        var add = Action("添加演员", AddActorAsync, "\uE710", "SoftButton"); add.Margin = new(0); add.Padding = new(12, 6, 12, 6); DockPanel.SetDock(add, Dock.Right); heading.Children.Add(add);
        var titles = new StackPanel(); titles.Children.Add(Text("演员阵容", 15, "Ink", FontWeights.SemiBold)); titles.Children.Add(actorCount); heading.Children.Add(titles);
        DockPanel.SetDock(heading, Dock.Top); roster.Children.Add(heading);
        var tools = new WrapPanel { Margin = new(0, 8, 0, 0) };
        tools.Children.Add(Action("共用第一人的动作", () =>
        {
            if (actors.Count > 0) foreach (var card in actors.Skip(1)) { card.Body.Text = actors[0].Body.Text; card.Face.Text = actors[0].Face.Text; }
            status.Text = actors.Count > 1 ? "好啦，所有演员都会跳第一位演员的动作。" : "再添加几位演员，就能让大家一起跳同一段动作。"; return Task.CompletedTask;
        }, "\uE8C8", "LinkButton"));
        tools.Children.Add(Action("移除所选演员", RemoveActorAsync, "\uE74D", "LinkButton"));
        DockPanel.SetDock(tools, Dock.Bottom); roster.Children.Add(tools);
        roster.Children.Add(actorList);

        var editorArea = new Grid { Margin = new(20, 0, 0, 0) }; Grid.SetColumn(editorArea, 1);
        emptyRoster = EmptyState("\uE716", "还没有演员", "点击左上角的「添加演员」，邀请第一位角色登台吧。");
        editorArea.Children.Add(emptyRoster);
        editorArea.Children.Add(new ScrollViewer { Content = actorEditor, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new(0, 0, 6, 0) });
        body.Children.Add(roster); body.Children.Add(editorArea);

        var next = NavButton("下一步：站位与舞台", 1, primary: true);
        return Page("第一步 · 演员与动作", "选几位角色，再告诉 TA 们要跳什么。同一个角色可以添加好几次。", body, next, fill: true,
            footnote: "开始时会优先请出当前队伍里的角色，其余演员会自动创建；主控未参演时自动隐藏模型，结束后恢复。演员和动作的更改在下次开始时生效。");
    }

    private FrameworkElement BuildStagePage()
    {
        var content = new StackPanel();
        var cameraBody = new StackPanel();
        cameraBody.Children.Add(Label("镜头怎么拍")); cameraBody.Children.Add(cameraMode);
        cameraBody.Children.Add(Hint("拖入或选择镜头 VMD 后会自动切换到「VMD 镜头轨道」，也可以手动选择跟随或固定镜头。"));
        cameraFileDrop = FileDrop(camera, "镜头 VMD（可选）", "把镜头 .vmd 拖到这里，或点击选择", "VMD 动作|*.vmd", "\uE722", fileSelected: CameraFileSelected);
        cameraBody.Children.Add(Spacer(14)); cameraBody.Children.Add(cameraFileDrop);
        cameraBody.Children.Add(Spacer(14)); cameraBody.Children.Add(Label("队伍怎么站")); cameraBody.Children.Add(placementMode);
        cameraBody.Children.Add(Hint("默认使用「原作队形」：全员从舞台原点出发，保留动作自带的队形。需要手动摆放时，选择「站位舞台」展开下方站位图。"));
        cameraBody.Children.Add(Spacer(14)); cameraBody.Children.Add(hideSceneCharacters);
        cameraBody.Children.Add(Hint("隐藏起点周围 100 米内的 NPC 和怪物，结束或失败时恢复；保留演员、队员和动物。仅隐藏模型，AI、碰撞和头顶 UI 仍按游戏规则运行。"));
        content.Children.Add(Card("镜头", "跟着角色拍，或者使用作品自带的运镜。", "\uE722", cameraBody));
        stageMapPanel = BuildStageMapCard(); UpdateStageMapVisibility(); content.Children.Add(stageMapPanel);

        var musicBody = new StackPanel();
        musicBody.Children.Add(FileDrop(audio, "背景音乐（可选）", "把音乐文件拖到这里，或点击选择", "音频文件|*.wav;*.mp3;*.flac;*.ogg;*.m4a|所有文件|*.*", "\uE8D6"));
        var tune = new WrapPanel { Margin = new(0, 14, 0, 0), VerticalAlignment = VerticalAlignment.Bottom };
        tune.Children.Add(Field("音乐提前 / 推迟", offset, 140)); tune.Children.Add(Field("音量", volume, 120));
        var applyMusic = Action("应用配乐", ApplyMusicAsync, "\uE73E"); applyMusic.VerticalAlignment = VerticalAlignment.Bottom; applyMusic.Margin = new(0, 0, 0, 2); tune.Children.Add(applyMusic);
        musicBody.Children.Add(tune);
        musicBody.Children.Add(Hint("音乐和动作对不上？调一下「提前 / 推迟」：正数让音乐晚一点响。表演中修改后点「应用配乐」即可。"));
        content.Children.Add(Card("背景音乐", "开始表演时会自动跟动作对齐播放。", "\uE8D6", musicBody));

        var physics = new StackPanel(); physics.Children.Add(native); physics.Children.Add(secondary);
        physics.Children.Add(Hint("物理方式在开始表演时生效。游戏自带物理开启时，就不需要额外的衣物惯性。"));
        var more = new Expander { Header = "头发与衣物摆动（一般不用改）", Content = physics };
        content.Children.Add(Card("更多细节", "保持默认就很好看，想折腾的时候再打开。", "\uE713", more));

        var nav = new StackPanel { Orientation = Orientation.Horizontal };
        nav.Children.Add(NavButton("上一步", 0, primary: false)); nav.Children.Add(NavButton("下一步：开始表演", 2, primary: true));
        return Page("第二步 · 站位与舞台", "默认保留动作自带的原作队形。先选镜头和配乐，需要手动摆放时再展开站位舞台。", content, nav);
    }

    private void UpdateStageMapVisibility()
    {
        if (stageMapPanel is not null) stageMapPanel.Visibility = placementMode.SelectedIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void CameraFileSelected(string path)
    {
        if (string.Equals(System.IO.Path.GetExtension(path), ".vmd", StringComparison.OrdinalIgnoreCase)) cameraMode.SelectedIndex = 2;
    }

    private FrameworkElement BuildPerformPage()
    {
        var content = new StackPanel();

        var playStyle = new StackPanel();
        playStyle.Children.Add(Label("播放速度"));
        var chips = new WrapPanel { VerticalAlignment = VerticalAlignment.Center };
        foreach (var (label, value) in new[] { ("慢一点 0.5×", "0.5"), ("正常 1×", "1"), ("快一点 1.5×", "1.5"), ("2×", "2") })
        {
            var chip = new Button { Content = label }; chip.SetResourceReference(StyleProperty, "ChipButton"); chip.Click += (_, _) => speed.Text = value; speedChips.Add((chip, value)); chips.Children.Add(chip);
        }
        speed.Width = 90; speed.Margin = new(6, 0, 0, 0); speed.ToolTip = "也可以自己输入 0.1–4 之间的倍数"; chips.Children.Add(speed);
        playStyle.Children.Add(chips); playStyle.Children.Add(Spacer(12)); playStyle.Children.Add(loop); playStyle.Children.Add(Spacer(10));
        var limit = new WrapPanel { VerticalAlignment = VerticalAlignment.Bottom }; limit.Children.Add(Field("多久后自动结束（0 表示不限）", duration, 200));
        applySettings = Action("应用播放设置", ApplySettingsAsync, "\uE73E", "SoftButton", needsSession: true); applySettings.VerticalAlignment = VerticalAlignment.Bottom; applySettings.Margin = new(0, 0, 0, 2); limit.Children.Add(applySettings);
        playStyle.Children.Add(limit);
        playStyle.Children.Add(Hint("自动结束按开始后的真实时间计算，暂停时也会计时。表演中修改后点「应用播放设置」。"));
        content.Children.Add(Card("播放方式", "速度、循环和时长，随时都能改。", "\uE916", playStyle));

        var rec = new StackPanel(); rec.Children.Add(autoRecord); rec.Children.Add(Spacer(12));
        var recOptions = new WrapPanel(); recOptions.Children.Add(recordingFps); recOptions.Children.Add(recordingSize); rec.Children.Add(recOptions);
        rec.Children.Add(Hint("支持 2K / 60 FPS，默认 NVENC 硬件编码，优先 GPU 直连以降低开销；启动失败会自动回退兼容模式或软件编码。"));
        rec.Children.Add(Hint("开启自动录屏后，播放时开始录制，播完自动保存。循环播放会持续录制，直到手动停止。"));
        var recButtons = new WrapPanel { Margin = new(0, 12, 0, 0) };
        recordButton = Action("录制鸣潮窗口", RecordAsync, "\uE7C8", "PrimaryButton"); recButtons.Children.Add(recordButton);
        stopRecordingButton = Action("停止并保存录屏", StopRecordingAsync, "\uE74E"); recButtons.Children.Add(stopRecordingButton);
        recButtons.Children.Add(Action("打开录屏文件夹", OpenRecordingFolderAsync, "\uE8B7", "LinkButton"));
        rec.Children.Add(recButtons); rec.Children.Add(Hint("只录鸣潮窗口，配乐也会写进视频。录制中更改设置需先停止录屏。"));
        recordingLocation.Text = "保存位置：" + recorder.OutputDirectory; recordingLocation.TextWrapping = TextWrapping.Wrap; recordingLocation.Margin = new(0, 8, 0, 0); rec.Children.Add(recordingLocation);
        recordingPanel = Card("录屏设置", "开始前就能选好，也可以随时手动录制。", "\uE714", rec); content.Children.Add(recordingPanel);

        var idle = new StackPanel();
        var check = new StackPanel(); check.Children.Add(readiness);
        check.Children.Add(Tip("点「开始播放」后请切回鸣潮窗口，演员会出现在你的角色身边。"));
        var start = new StackPanel { Orientation = Orientation.Horizontal, Margin = new(0, 18, 0, 0) };
        var hero = Action("开始播放", StartAsync, "\uE768", "HeroButton"); hero.Margin = new(0, 0, 18, 0); start.Children.Add(hero);
        var extra = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var prepareRow = new StackPanel { Orientation = Orientation.Horizontal };
        prepareRow.Children.Add(Action("准备动作", PrepareAsync, "\uE895", "LinkButton")); prepareRow.Children.Add(Action("载入已准备动作", LoadPreparationAsync, "\uE8B7", "LinkButton"));
        extra.Children.Add(prepareRow); preparation.MaxWidth = 420; preparation.Margin = new(6, 2, 0, 0); extra.Children.Add(preparation);
        start.Children.Add(extra); check.Children.Add(start);
        idle.Children.Add(Card("开始前检查一下", "这些都打勾，就可以开演啦。", "\uE930", check));
        idlePanel = idle; content.Children.Add(idle);

        var live = new StackPanel();
        var stage = new StackPanel();
        var clock = new DockPanel();
        var pill = new Border { CornerRadius = new(11), Padding = new(10, 3, 10, 3), VerticalAlignment = VerticalAlignment.Center, Child = modeLabel };
        pill.SetResourceReference(Border.BackgroundProperty, "AccentSoft"); DockPanel.SetDock(pill, Dock.Right); clock.Children.Add(pill); clock.Children.Add(time);
        stage.Children.Add(clock); stage.Children.Add(progress);
        var transport = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        transport.Children.Add(Action("上一帧", () => SendAsync("step", -1), "\uE892", "RoundButton", needsSession: true));
        transport.Children.Add(Action("播放", () => SendAsync("play"), "\uE768", "RoundPrimaryButton", needsSession: true));
        transport.Children.Add(Action("暂停", () => SendAsync("pause"), "\uE769", "RoundButton", needsSession: true));
        transport.Children.Add(Action("下一帧", () => SendAsync("step", 1), "\uE893", "RoundButton", needsSession: true));
        stage.Children.Add(transport);
        var secondaryRow = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new(0, 14, 0, 0) };
        secondaryRow.Children.Add(Action("跳转到滑块位置", () => SendAsync("seek", progress.Value), "\uE72A", "LinkButton", needsSession: true));
        secondaryRow.Children.Add(Action("保存书签", SaveBookmarkAsync, "\uE74E", "LinkButton", needsSession: true));
        secondaryRow.Children.Add(Action("载入书签", LoadBookmarkAsync, "\uE8E5", "LinkButton", needsSession: true));
        stage.Children.Add(secondaryRow);
        stage.Children.Add(Hint("拖动进度条后点「跳转到滑块位置」；上一帧 / 下一帧每次移动 1/30 秒。书签能记住喜欢的瞬间。"));
        var stopRow = new DockPanel { Margin = new(0, 16, 0, 0) };
        stopButton = Action("停止并恢复", StopAsync, "\uE71A", "DangerButton", stop: true); stopButton.Margin = new(0); DockPanel.SetDock(stopButton, Dock.Right); stopRow.Children.Add(stopButton);
        var stopNote = Text("表演结束后记得点这里，演员会退场，队伍恢复原样。", 12, "Muted"); stopNote.VerticalAlignment = VerticalAlignment.Center; stopRow.Children.Add(stopNote);
        stage.Children.Add(stopRow);
        live.Children.Add(Card("正在表演", "可以切回游戏欣赏，也可以在这里控制。", "\uE768", stage));

        var adjust = new StackPanel();
        adjust.Children.Add(Label("要调整哪位演员")); adjust.Children.Add(placementTarget);
        var fields = new WrapPanel { Margin = new(0, 12, 0, 0) };
        foreach (var (label, box) in new[] { ("向左 / 向右", liveX), ("向前 / 向后", liveY), ("高度", liveHeight), ("面朝方向", liveYaw) }) fields.Children.Add(Field(label, box, 120));
        adjust.Children.Add(fields); adjust.Children.Add(liveInPlace);
        var applyPlace = Action("应用位置与朝向", ApplyPlacementAsync, "\uE73E", "SoftButton", needsSession: true); applyPlace.HorizontalAlignment = HorizontalAlignment.Left; applyPlace.Margin = new(0, 8, 0, 0); adjust.Children.Add(applyPlace);
        live.Children.Add(Card("挪一挪站位", "某位演员站歪了？不用重来，直接在这里调整。", "\uE7C9", new Expander { Header = "展开调整位置与朝向", Content = adjust }));

        livePanel = live; content.Children.Add(live);

        return Page("第三步 · 开始表演", "确认一下准备情况，然后就可以开演了。", content, NavButton("上一步", 1, primary: false));
    }

    // ── Guide state ───────────────────────────────────────────────────────

    private void ShowPage(int index)
    {
        page = index; pageHost.Content = pages[index]; UpdateSteps();
    }

    private void UpdateGuide()
    {
        if (steps.Count == 0) return;
        if (emptyRoster is not null) emptyRoster.Visibility = actors.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        readiness.Children.Clear();
        var playing = actors.Where(card => card.Enabled.IsChecked == true).ToList();
        var missing = playing.Count(card => !card.IsReady);
        CheckRow(playing.Count > 0, playing.Count > 0 ? $"{playing.Count} 位演员会登台表演" : "还没有参与表演的演员，请在第一步添加");
        if (playing.Count > 0) CheckRow(missing == 0, missing == 0 ? "每位演员都有身体动作了" : $"还有 {missing} 位演员没有选身体动作");
        var cameraMissing = cameraMode.SelectedIndex == 2 && string.IsNullOrWhiteSpace(camera.Text);
        CheckRow(!cameraMissing, cameraMissing ? "选了「VMD 镜头轨道」，但还没有选镜头文件" : "镜头：" + cameraMode.SelectedItem);
        var nativeFormation = placementMode.SelectedIndex == 1;
        CheckRow(null, nativeFormation ? "摆放：原作队形（忽略站位图，全员从舞台原点出发）" : "摆放：站位舞台（按站位图各自站位）");
        if (hideSceneCharacters.IsChecked == true) CheckRow(null, "播放时隐藏起点周围 100 米内的 NPC 和怪物模型，停止后恢复");
        var crowded = nativeFormation ? 0 : CrowdedCount(MapActors());
        if (playing.Count > 0) CheckRow(crowded == 0, crowded == 0 ? "站位没有挤在一起" : $"有 {crowded} 位演员站得太近，可以去第二步的站位地图调整");
        CheckRow(null, string.IsNullOrWhiteSpace(audio.Text) ? "背景音乐：没有选择（可以不选）" : "背景音乐：" + System.IO.Path.GetFileName(audio.Text));
        CheckRow(null, "请确认鸣潮已经打开，并且已进入游戏场景");
        steps[0].Update(playing.Count > 0 && missing == 0, playing.Count == 0 ? "先添加一位演员" : missing == 0 ? $"{playing.Count} 位演员都已就绪" : $"还有 {missing} 位缺少动作");
        var extras = new[] { playing.Any(card => card.Stage is not null) ? "站位" : null, string.IsNullOrWhiteSpace(camera.Text) ? null : "镜头", string.IsNullOrWhiteSpace(audio.Text) ? null : "音乐" }.Where(item => item is not null).ToArray();
        steps[1].Update(extras.Length > 0, extras.Length > 0 ? "已设置" + string.Join("、", extras) : "都是可选的，可以跳过");
        steps[2].Update(session is not null, session is not null ? (sessionControllable ? "正在表演中" : "正在结束并恢复") : "检查一下，然后开始");
        UpdateSteps(); if (stageEditor is not null) RefreshStage();
    }

    private void CheckRow(bool? good, string text)
    {
        var (glyph, fg, bg) = good switch { true => ("\uE73E", "Good", "GoodSoft"), false => ("\uE7BA", "Warn", "WarnSoft"), _ => ("\uE946", "Muted", "Sidebar") };
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new(0, 0, 0, 10) };
        row.Children.Add(Badge(glyph, fg, bg, 26, 12)); var label = Text(text, 13, good == false ? "Ink" : "InkSoft"); label.Margin = new(10, 0, 0, 0); label.VerticalAlignment = VerticalAlignment.Center; row.Children.Add(label);
        readiness.Children.Add(row);
    }

    private void UpdateSteps() { foreach (var step in steps) step.SetCurrent(step.Index == page); }

    private void UpdateSpeedChips()
    {
        foreach (var (chip, value) in speedChips)
        {
            var on = speed.Text.Trim() == value;
            chip.SetResourceReference(BackgroundProperty, on ? "Accent" : "Card"); chip.SetResourceReference(BorderBrushProperty, on ? "Accent" : "LineStrong");
            if (on) chip.Foreground = Brushes.White; else chip.SetResourceReference(ForegroundProperty, "InkSoft");
        }
    }

    private void UpdateSessionView()
    {
        var live = session is not null;
        if (idlePanel is not null) { idlePanel.Visibility = live ? Visibility.Collapsed : Visibility.Visible; livePanel.Visibility = live ? Visibility.Visible : Visibility.Collapsed; }
        if (applySettings is not null) applySettings.Visibility = live ? Visibility.Visible : Visibility.Collapsed;
        sessionDot.SetResourceReference(Shape.FillProperty, live ? "Good" : "Faint");
        if (!live && previewSession is not null) { sessionLabel.Text = "正在游戏里预览站位"; sessionDot.SetResourceReference(Shape.FillProperty, "Warn"); }
        else if (!live) { sessionLabel.Text = "还没有开始表演"; modeLabel.Text = "等待开始"; }
        if (live && !wasLive && pages.Count > 2) ShowPage(2);
        wasLive = live; UpdateGuide();
    }

    // ── Building blocks ───────────────────────────────────────────────────

    private Button Action(string label, Func<Task> operation, string? glyph = null, string? style = null, bool stop = false, bool needsSession = false)
    {
        var button = new Button { Content = label, Tag = glyph }; if (style is not null) button.SetResourceReference(StyleProperty, style); actions.Add(button);
        if (needsSession) sessionActions.Add(button);
        button.Click += async (_, _) =>
        {
            if (busy) { if (stop) { operationCancellation?.Cancel(); status.Text = "正在取消操作，并等待恢复完成…"; } return; }
            busy = true; using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token); operationCancellation = cancellation; UpdateActions();
            try { await operation(); }
            catch (OperationCanceledException) { AdoptActiveSession(); status.Text = "操作已取消。可以到「开始表演」页查看状态，或点「停止并恢复」。"; }
            catch (Exception error) { AdoptActiveSession(); status.Text = error.Message; }
            finally { operationCancellation = null; busy = false; UpdateActions(); }
        }; return button;
    }

    private void UpdateActions()
    {
        hideSceneCharacters.IsEnabled = !busy && session is null && previewSession is null && player.ActiveSession is null;
        foreach (var button in actions) button.IsEnabled = !busy;
        foreach (var button in sessionActions) button.IsEnabled = !busy && session is not null && sessionControllable;
        if (loadPresetButton is not null) loadPresetButton.IsEnabled = !busy && session is null && previewSession is null && player.ActiveSession is null && !recorder.IsBusy;
        if (stopButton is not null) stopButton.IsEnabled = (!busy && (session is not null || previewSession is not null || player.ActiveSession is not null)) || (busy && operationCancellation is { IsCancellationRequested: false });
        UpdateRecordingControls(); busyBar.Visibility = busy ? Visibility.Visible : Visibility.Hidden; UpdateSessionView();
    }

    private void UpdateRecordingControls()
    {
        var recording = recorder.IsBusy;
        if (loadPresetButton is not null) loadPresetButton.IsEnabled = !busy && session is null && previewSession is null && player.ActiveSession is null && !recording;
        autoRecord.IsEnabled = !busy && session is null && player.ActiveSession is null;
        recordingFps.IsEnabled = recordingSize.IsEnabled = !busy && !recording;
        if (recordButton is not null) recordButton.IsEnabled = !busy && !recording;
        if (stopRecordingButton is not null) stopRecordingButton.IsEnabled = !busy && recording;
    }

    private Button NavButton(string label, int target, bool primary)
    {
        var button = new Button { Content = label, Tag = primary ? "\uE72A" : "\uE72B" }; if (primary) button.SetResourceReference(StyleProperty, "PrimaryButton");
        button.Click += (_, _) => ShowPage(target); return button;
    }

    private static FrameworkElement Page(string title, string subtitle, UIElement body, UIElement navigation, bool fill = false, string? footnote = null)
    {
        var head = new StackPanel { Margin = new(36, 30, 36, 20) };
        head.Children.Add(Text(title, 24, "Ink", FontWeights.SemiBold)); var sub = Text(subtitle, 13, "Muted"); sub.Margin = new(0, 6, 0, 0); head.Children.Add(sub);
        var foot = new DockPanel { Margin = new(36, 14, 36, 22), LastChildFill = footnote is not null }; DockPanel.SetDock((UIElement)navigation, Dock.Right); foot.Children.Add(navigation);
        if (footnote is not null) { var note = Text(footnote, 12, "Muted"); note.VerticalAlignment = VerticalAlignment.Center; note.Margin = new(0, 0, 24, 0); foot.Children.Add(note); }
        if (fill)
        {
            var grid = new Grid(); grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); grid.RowDefinitions.Add(new RowDefinition()); grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.Children.Add(head); Grid.SetRow((UIElement)body, 1); grid.Children.Add(body); Grid.SetRow(foot, 2); grid.Children.Add(foot); return grid;
        }
        var stack = new StackPanel(); stack.Children.Add(head);
        var inner = new Border { Margin = new(36, 0, 36, 0), MaxWidth = 860, HorizontalAlignment = HorizontalAlignment.Left, Child = body }; stack.Children.Add(inner);
        foot.MaxWidth = 932; foot.HorizontalAlignment = HorizontalAlignment.Stretch; stack.Children.Add(foot);
        return new ScrollViewer { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    }

    private static Border Card(string title, string subtitle, string glyph, UIElement content)
    {
        var card = new Border { CornerRadius = new(16), Padding = new(24, 20, 24, 22), Margin = new(0, 0, 0, 16), BorderThickness = new(1) };
        card.SetResourceReference(Border.BackgroundProperty, "Card"); card.SetResourceReference(Border.BorderBrushProperty, "Line"); card.SetResourceReference(EffectProperty, "SoftShadow");
        var stack = new StackPanel(); var head = new StackPanel { Orientation = Orientation.Horizontal, Margin = new(0, 0, 0, 16) };
        head.Children.Add(Badge(glyph, "Accent", "AccentSoft", 38, 16));
        var words = new StackPanel { Margin = new(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        words.Children.Add(Text(title, 16, "Ink", FontWeights.SemiBold)); words.Children.Add(Text(subtitle, 12, "Muted")); head.Children.Add(words);
        stack.Children.Add(head); stack.Children.Add(content); card.Child = stack; return card;
    }

    private static FrameworkElement EmptyState(string glyph, string title, string text)
    {
        var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, MaxWidth = 320 };
        var badge = Badge(glyph, "Accent", "AccentSoft", 72, 30); badge.HorizontalAlignment = HorizontalAlignment.Center; stack.Children.Add(badge);
        var head = Text(title, 17, "Ink", FontWeights.SemiBold); head.HorizontalAlignment = HorizontalAlignment.Center; head.Margin = new(0, 16, 0, 6); stack.Children.Add(head);
        var body = Text(text, 13, "Muted"); body.TextAlignment = TextAlignment.Center; stack.Children.Add(body); return stack;
    }

    private static Border Tip(string text)
    {
        var tip = new Border { CornerRadius = new(12), Padding = new(14, 11, 14, 11), Margin = new(0, 6, 0, 0) }; tip.SetResourceReference(Border.BackgroundProperty, "WarnSoft");
        var row = new DockPanel(); row.Children.Add(Glyph("\uEA80", 15, "Warn", new(0, 0, 10, 0))); row.Children.Add(Text(text, 13, "InkSoft")); tip.Child = row; return tip;
    }

    private static FrameworkElement FileDrop(TextBox box, string title, string hint, string filter, string glyph, bool required = false, Action<string>? fileSelected = null)
    {
        var zone = new Grid { Cursor = Cursors.Hand, AllowDrop = true, Background = Brushes.Transparent, MinHeight = 66 };
        var outline = new Rectangle { RadiusX = 12, RadiusY = 12, StrokeThickness = 1.5, StrokeDashArray = [4, 3] }; outline.SetResourceReference(Shape.StrokeProperty, "LineStrong"); outline.SetResourceReference(Shape.FillProperty, "Well");
        zone.Children.Add(outline);
        var row = new DockPanel { Margin = new(14, 12, 12, 12) };
        var change = new Button { Content = "选择文件", Tag = "\uE8E5", Margin = new(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        var clear = new Button { Content = "清除", Margin = new(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center }; clear.SetResourceReference(StyleProperty, "LinkButton");
        DockPanel.SetDock(change, Dock.Right); DockPanel.SetDock(clear, Dock.Right); row.Children.Add(change); row.Children.Add(clear);
        var icon = Badge(glyph, "Muted", "Card", 40, 16); row.Children.Add(icon);
        var words = new StackPanel { Margin = new(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        var head = Text(title + (required ? "  · 必选" : ""), 13, "Ink", FontWeights.SemiBold); var detail = Text(hint, 12, "Muted"); detail.TextTrimming = TextTrimming.CharacterEllipsis; detail.TextWrapping = TextWrapping.NoWrap;
        words.Children.Add(head); words.Children.Add(detail); row.Children.Add(words); zone.Children.Add(row);
        void Refresh()
        {
            var has = !string.IsNullOrWhiteSpace(box.Text);
            detail.Text = has ? System.IO.Path.GetFileName(box.Text.Trim()) : hint; detail.ToolTip = has ? box.Text : null;
            detail.SetResourceReference(TextBlock.ForegroundProperty, has ? "Good" : "Muted");
            clear.Visibility = has ? Visibility.Visible : Visibility.Collapsed; change.Content = has ? "更换" : "选择文件";
            icon.SetResourceReference(Border.BackgroundProperty, has ? "GoodSoft" : "Card"); ((TextBlock)icon.Child).SetResourceReference(TextBlock.ForegroundProperty, has ? "Good" : "Muted");
            outline.SetResourceReference(Shape.StrokeProperty, has ? "Good" : "LineStrong");
        }
        void SelectFile(string path) { box.Text = path; fileSelected?.Invoke(path); }
        void Choose() { var dialog = new OpenFileDialog { Filter = filter, Title = title }; if (dialog.ShowDialog() == true) SelectFile(dialog.FileName); }
        box.TextChanged += (_, _) => Refresh(); change.Click += (_, _) => Choose(); clear.Click += (_, _) => box.Text = "";
        zone.MouseLeftButtonUp += (_, _) => Choose();
        zone.DragOver += (_, args) => { args.Effects = args.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None; args.Handled = true; };
        zone.DragEnter += (_, _) => outline.SetResourceReference(Shape.StrokeProperty, "Accent");
        zone.DragLeave += (_, _) => Refresh();
        zone.Drop += (_, args) => { if (args.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files) SelectFile(files[0]); Refresh(); args.Handled = true; };
        Refresh(); return new Border { Margin = new(0, 0, 0, 10), Child = zone };
    }

    private static FrameworkElement Field(string label, TextBox input, double width = 120)
    {
        input.Width = width; var panel = new StackPanel { Margin = new(0, 0, 14, 10), HorizontalAlignment = HorizontalAlignment.Left };
        panel.Children.Add(Label(label)); panel.Children.Add(input); return panel;
    }

    private static TextBlock Label(string text) { var label = Text(text, 12, "InkSoft", FontWeights.SemiBold); label.Margin = new(0, 0, 0, 6); return label; }
    private static TextBlock Hint(string text) { var hint = Text(text, 12, "Muted"); hint.Margin = new(0, 8, 0, 0); return hint; }
    private static FrameworkElement Spacer(double height) => new Border { Height = height };

    private static TextBlock Text(string text, double size, string brush, FontWeight? weight = null)
    {
        var block = new TextBlock { Text = text, FontSize = size, TextWrapping = TextWrapping.Wrap, FontWeight = weight ?? FontWeights.Normal };
        block.SetResourceReference(TextBlock.ForegroundProperty, brush); return block;
    }

    private static TextBlock Glyph(string glyph, double size, string brush, Thickness margin)
    {
        var block = new TextBlock { Text = glyph, FontSize = size, Margin = margin, VerticalAlignment = VerticalAlignment.Center };
        block.SetResourceReference(TextBlock.FontFamilyProperty, "IconFont"); block.SetResourceReference(TextBlock.ForegroundProperty, brush); return block;
    }

    private static Border Badge(string glyph, string foreground, string background, double size, double glyphSize)
    {
        var badge = new Border { Width = size, Height = size, CornerRadius = new(size / 2), VerticalAlignment = VerticalAlignment.Center };
        badge.SetResourceReference(Border.BackgroundProperty, background);
        var icon = Glyph(glyph, glyphSize, foreground, new(0)); icon.HorizontalAlignment = HorizontalAlignment.Center; badge.Child = icon; return badge;
    }

    private void TintCaption()
    {
        // Blend the Windows 11 title bar into the sidebar; older systems ignore the attribute.
        var handle = new WindowInteropHelper(this).Handle; int caption = 0x00E2EAF1, text = 0x00232730;
        _ = DwmSetWindowAttribute(handle, 35, ref caption, sizeof(int)); _ = DwmSetWindowAttribute(handle, 36, ref text, sizeof(int));
    }

    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(nint hwnd, int attribute, ref int value, int size);

    private sealed class StepEntry
    {
        public int Index { get; }
        public Button Button { get; } = new();
        private readonly Border circle = new() { Width = 30, Height = 30, CornerRadius = new(15), VerticalAlignment = VerticalAlignment.Top };
        private readonly TextBlock number, title, detail;
        private bool done, current;

        public StepEntry(int index, string name)
        {
            Index = index; Button.SetResourceReference(StyleProperty, "StepButton");
            number = new TextBlock { Text = (index + 1).ToString(), FontSize = 13, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            circle.Child = number; title = Text(name, 14, "Ink", FontWeights.SemiBold); detail = Text("", 12, "Muted");
            var words = new StackPanel { Margin = new(12, 0, 0, 0) }; words.Children.Add(title); words.Children.Add(detail);
            var row = new DockPanel(); row.Children.Add(circle); row.Children.Add(words); Button.Content = row; Paint();
        }

        public void Update(bool isDone, string text) { done = isDone; detail.Text = text; Paint(); }
        public void SetCurrent(bool value) { current = value; Paint(); }

        private void Paint()
        {
            Button.SetResourceReference(BackgroundProperty, current ? "Card" : "Transparent");
            if (!current) Button.Background = Brushes.Transparent;
            Button.Effect = current ? (System.Windows.Media.Effects.Effect?)new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 14, ShadowDepth = 2, Direction = 270, Opacity = .06 } : null;
            number.Text = done ? "\uE73E" : (Index + 1).ToString();
            if (done) number.SetResourceReference(TextBlock.FontFamilyProperty, "IconFont"); else number.FontFamily = new FontFamily("Segoe UI");
            circle.SetResourceReference(Border.BackgroundProperty, done ? "Good" : current ? "Accent" : "Card");
            if (done || current) number.Foreground = Brushes.White; else number.SetResourceReference(TextBlock.ForegroundProperty, "Muted");
        }
    }

    private sealed class ActorCard : INotifyPropertyChanged
    {
        private int index;
        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action? Changed;
        public string Number => (index + 1).ToString();
        public string CharacterName => (Character.SelectedItem as OwnedCharacterSelection)?.DisplayName ?? MissingCharacterName ?? MissingCharacterId ?? "还没选角色";
        public string? MissingCharacterId { get; private set; }
        public string? MissingCharacterName { get; private set; }
        public long MissingRoleId { get; private set; }
        private readonly TextBlock missingCharacter = Text("", 12, "AccentPressed");
        public void MarkMissingCharacter(string id, long roleId, string? name = null)
        {
            MissingCharacterId = id; MissingRoleId = roleId; MissingCharacterName = name;
            missingCharacter.Text = "缺少角色 / 皮肤 / 形态：" + (name ?? id) + "，请重新选择。";
            missingCharacter.Visibility = Visibility.Visible; Notify(nameof(CharacterName)); Notify(nameof(IsReady));
        }
        public string MotionSummary => string.IsNullOrWhiteSpace(Body.Text) ? "还没有身体动作" : System.IO.Path.GetFileNameWithoutExtension(Body.Text.Trim()) + (string.IsNullOrWhiteSpace(Face.Text) ? "" : " · 含表情");
        public bool IsReady => !string.IsNullOrWhiteSpace(Body.Text) && Character.SelectedItem is not null;
        public bool IsParticipating => Enabled.IsChecked == true;
        public StagePosition? Stage { get; set; }
        public void SetIndex(int value) { index = value; Name.Text = $"演员 {index + 1}"; Notify(nameof(Number)); }
        public Border Panel { get; }
        public TextBlock Name { get; } = Text("", 22, "Ink", FontWeights.SemiBold);
        public CheckBox Enabled { get; } = new() { Content = "参与表演", IsChecked = true, Margin = new(0) };
        public ComboBox Character { get; } = new() { DisplayMemberPath = nameof(OwnedCharacterSelection.DisplayName), SelectedValuePath = nameof(OwnedCharacterSelection.CharacterId), MaxDropDownHeight = 380 };
        public readonly TextBox Body = new(), Face = new(), Scale = new() { Tag = "倍" };
        public readonly TextBox X = new() { Text = "0", Tag = "厘米" }, Y = new() { Text = "0", Tag = "厘米" }, Height = new() { Text = "0", Tag = "厘米" }, Yaw = new() { Text = "0", Tag = "度" };
        public CheckBox InPlace { get; } = new() { Content = "原地跳（不随动作走动）" };

        public ActorCard(int index, OwnedCharacterCatalog catalog)
        {
            this.index = index; Name.Text = $"演员 {index + 1}"; Character.ItemsSource = catalog.Characters; Character.SelectedValue = catalog.DefaultCharacterId;
            Character.SelectionChanged += (_, _) => {
                if (Character.SelectedItem is not null) { MissingCharacterId = MissingCharacterName = null; MissingRoleId = 0; missingCharacter.Visibility = Visibility.Collapsed; }
                Notify(nameof(CharacterName)); Notify(nameof(IsReady)); Changed?.Invoke(); };
            Enabled.Checked += (_, _) => { Notify(nameof(IsParticipating)); Changed?.Invoke(); }; Enabled.Unchecked += (_, _) => { Notify(nameof(IsParticipating)); Changed?.Invoke(); };
            Body.TextChanged += (_, _) => { Notify(nameof(MotionSummary)); Notify(nameof(IsReady)); Changed?.Invoke(); };
            Face.TextChanged += (_, _) => Notify(nameof(MotionSummary));

            var panel = new StackPanel();
            var head = new DockPanel { Margin = new(0, 0, 0, 20) }; DockPanel.SetDock(Enabled, Dock.Right); Enabled.VerticalAlignment = VerticalAlignment.Center; head.Children.Add(Enabled);
            var words = new StackPanel(); words.Children.Add(Name); words.Children.Add(Text("给 TA 选个角色，再挑一段想跳的动作。", 12, "Muted")); head.Children.Add(words);
            panel.Children.Add(head);
            panel.Children.Add(Label("TA 是谁")); panel.Children.Add(Character); missingCharacter.Visibility = Visibility.Collapsed;
            panel.Children.Add(missingCharacter); panel.Children.Add(Spacer(18));
            panel.Children.Add(Label("TA 跳什么"));
            panel.Children.Add(FileDrop(Body, "身体动作 VMD", "把 .vmd 舞蹈动作拖到这里，或点击选择", "VMD 动作|*.vmd", "\uE805", required: true));
            panel.Children.Add(FileDrop(Face, "表情 VMD（可选）", "有表情会更生动，没有也没关系", "VMD 动作|*.vmd", "\uE76E"));
            var placement = new StackPanel();
            placement.Children.Add(Text("想决定 TA 站在哪里？到第二步的「站位地图」里拖一拖就好。这里是在动作本身的坐标里再做的额外偏移，一般保持 0；位移倍率空着时会按角色身高自动适配。", 12, "Muted"));
            var fields = new WrapPanel { Margin = new(0, 12, 0, 0) };
            foreach (var (label, box) in new[] { ("向左 / 向右", X), ("向前 / 向后", Y), ("高度", Height), ("面朝方向", Yaw) }) fields.Children.Add(Field(label, box, 120));
            fields.Children.Add(Field("位移倍率（空着 = 自动）", Scale, 170));
            placement.Children.Add(fields); placement.Children.Add(InPlace);
            panel.Children.Add(Spacer(4)); panel.Children.Add(new Expander { Header = "动作幅度与额外偏移（一般不用改）", Content = placement });
            Panel = new Border { Child = panel, CornerRadius = new(16), Padding = new(26, 22, 26, 22), BorderThickness = new(1), Margin = new(0, 0, 0, 12) };
            Panel.SetResourceReference(Border.BackgroundProperty, "Card"); Panel.SetResourceReference(Border.BorderBrushProperty, "Line"); Panel.SetResourceReference(UIElement.EffectProperty, "SoftShadow");
        }

        private void Notify(string property) => PropertyChanged?.Invoke(this, new(property));
    }
}
