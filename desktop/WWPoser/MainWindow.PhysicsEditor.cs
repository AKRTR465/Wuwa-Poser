using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using WWPoser.Services;

namespace WWPoser;

public sealed partial class MainWindow
{
    private PhysicsPartCatalog physicsParts = null!;
    private readonly ComboBox physicsActor = new() { DisplayMemberPath = nameof(ActorCard.PhysicsDisplayName) };
    private readonly TreeView physicsTree = new();
    private readonly StackPanel physicsDetails = new();
    private readonly PhysicsSkeletonView physicsSkeleton = new();
    private readonly List<PhysicsParameterRow> physicsParameters = [];
    private PhysicsSelection? physicsSelection;
    private PhysicsParameterRow? shoulderFollow;
    private PhysicsGravityRow? gravityControls;
    private PhysicsGravityRow? partGravityControls;
    private StackPanel? capsuleControls;
    private readonly Dictionary<uint, (CheckBox Toggle, TextBlock Counts)> capsuleRows = [];
    private Button physicsSavePreset = null!, physicsLoadPreset = null!;
    private Button comparePhysics = null!;
    private Button togglePhysicsPreview = null!;
    private TextBlock physicsPreviewHeading = null!;
    private readonly TextBox physicsLoopStart = new() { Text = "0", Width = 56 }, physicsLoopEnd = new() { Width = 56 };
    private readonly Dictionary<uint, JsonObject> adjustedPhysics = [];

    private FrameworkElement BuildPhysicsEditorPage()
    {
        physicsParts = new(root);
        var layout = new Grid { Margin = new(24, 0, 24, 0), MinHeight = 340 };
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(208) });
        layout.ColumnDefinitions.Add(new ColumnDefinition());
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(184) });

        var parts = new DockPanel { Margin = new(0, 0, 10, 0) };
        var listHeader = new StackPanel { Margin = new(0, 0, 0, 10) };
        listHeader.Children.Add(Label("演员与物理组"));
        listHeader.Children.Add(physicsActor);
        var presets = new WrapPanel { Margin = new(0, 8, 0, 0) };
        physicsSavePreset = new Button { Content = "保存物理预设", Margin = new(0, 0, 4, 0), IsEnabled = false };
        physicsLoadPreset = new Button { Content = "载入物理预设", IsEnabled = false };
        physicsSavePreset.Click += (_, _) => SavePhysicsPreset();
        physicsLoadPreset.Click += async (_, _) => await LoadPhysicsPresetAsync();
        presets.Children.Add(physicsSavePreset); presets.Children.Add(physicsLoadPreset); listHeader.Children.Add(presets);
        DockPanel.SetDock(listHeader, Dock.Top); parts.Children.Add(listHeader);
        parts.Children.Add(physicsTree);
        layout.Children.Add(parts);

        var preview = new DockPanel();
        physicsPreviewHeading = Label("参考骨架");
        DockPanel.SetDock(physicsPreviewHeading, Dock.Top); preview.Children.Add(physicsPreviewHeading);
        var views = new WrapPanel();
        foreach (var (view, label) in new[] { (0, "正面"), (1, "侧面"), (2, "斜视") })
        {
            var button = new Button { Content = label, Margin = new(0, 0, 4, 4), Padding = new(8, 4, 8, 4) };
            button.Click += (_, _) => { physicsSkeleton.SetView(view); physicsModelView.SetView(view); };
            views.Children.Add(button);
        }
        togglePhysicsPreview = new Button { Content = "实时预览", IsEnabled = false, Margin = new(4, 0, 0, 4) };
        togglePhysicsPreview.Click += async (_, _) => await TogglePhysicsPreviewAsync();
        views.Children.Add(togglePhysicsPreview);
        toggleModelView = new Button { Content = "骨架", IsEnabled = false, Margin = new(4, 0, 0, 4) };
        toggleModelView.Click += (_, _) => SetPhysicsView(physicsModelView.Visibility != Visibility.Visible);
        views.Children.Add(toggleModelView);
        AddCapsuleEditorToggle(views);
        DockPanel.SetDock(views, Dock.Top); preview.Children.Add(views);
        var compare = new WrapPanel { Margin = new(0, 8, 0, 0) };
        compare.Children.Add(Field("起点（秒）", physicsLoopStart, 78));
        compare.Children.Add(Field("终点（秒）", physicsLoopEnd, 78));
        var loopSegment = Action("循环片段", LoopPhysicsSegmentAsync, "\uE8EE", "SoftButton", needsSession: true);
        var endSegment = Action("退出循环", () => SendAsync("preview_loop_end"), "\uE711", "SoftButton", needsSession: true);
        loopSegment.VerticalAlignment = endSegment.VerticalAlignment = VerticalAlignment.Bottom;
        compare.Children.Add(loopSegment); compare.Children.Add(endSegment);
        comparePhysics = new Button { Content = "原厂对照", IsEnabled = false, VerticalAlignment = VerticalAlignment.Bottom };
        comparePhysics.Click += async (_, _) => await ComparePhysicsAsync();
        compare.Children.Add(comparePhysics);
        DockPanel.SetDock(compare, Dock.Bottom); preview.Children.Add(compare);
        var modelPanel = new Grid { ClipToBounds = true };
        modelPanel.Children.Add(physicsSkeleton); modelPanel.Children.Add(physicsModelView);
        var viewport = new Border { Child = modelPanel, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(9) };
        viewport.SetResourceReference(Border.BorderBrushProperty, "Line"); viewport.SetResourceReference(Border.BackgroundProperty, "Card");
        preview.Children.Add(viewport);
        Grid.SetColumn(preview, 1); layout.Children.Add(preview);

        var inspectorContents = new StackPanel { Margin = new Thickness(12, 0, 0, 16) };
        inspectorContents.Children.Add(BuildCapsuleInspector());
        inspectorContents.Children.Add(physicsDetails);
        var inspector = new ScrollViewer { Content = inspectorContents, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Grid.SetColumn(inspector, 2); layout.Children.Add(inspector);
        physicsActor.SelectionChanged += (_, _) => LoadPhysicsActor();
        physicsTree.SelectedItemChanged += (_, e) => ShowPhysicsSelection((e.NewValue as TreeViewItem)?.Tag as PhysicsSelection);
        return Page("物理编辑器", "选中物理组或骨链，在模型上查看它影响的部位。", layout, NavButton("返回表演", 2, primary: false), fill: true);
    }

    private void RefreshPhysicsActors()
    {
        physicsActor.ItemsSource = actors;
        var card = physicsActor.SelectedItem as ActorCard ?? actorList.SelectedItem as ActorCard ?? actors.FirstOrDefault();
        if (ReferenceEquals(physicsActor.SelectedItem, card)) LoadPhysicsActor();
        else physicsActor.SelectedItem = card;
    }

    private void LoadPhysicsActor()
    {
        physicsTree.Items.Clear(); physicsSkeleton.SetBones([]); ShowPhysicsSelection(null);
        ResetPhysicsModel();
        if ((physicsActor.SelectedItem as ActorCard)?.Character.SelectedItem is not OwnedCharacterSelection character) return;
        try
        {
            var profile = physicsParts.Read(character);
            physicsSkeleton.SetBones(profile.Bones);
            _ = LoadPhysicsModelAsync(character, profile.Bones);
            var parts = PartGravityCatalog.Build(root, character, profile).OfType<JsonObject>()
                .Select(part => new GravityPart(Contract.S(part, "root"), Contract.True(part, "from_chain") ? null : Contract.S(part, "name"),
                    PartGravityCatalog.Members(part, profile.Bones).Order().ToArray())).ToArray();
            var boneIndices = profile.Bones.Select((bone, index) => (bone.Name, index))
                .ToDictionary(entry => entry.Name, entry => entry.index, StringComparer.OrdinalIgnoreCase);
            foreach (var source in profile.Groups.GroupBy(group => group.Source))
            {
                var branch = new TreeViewItem { Header = source.Key, IsExpanded = true };
                var targets = source.SelectMany(group => group.Chains.Select(chain => (Group: group, Chain: chain)))
                    .Where(target => boneIndices.ContainsKey(target.Chain.Root)).OrderBy(target => boneIndices[target.Chain.Root]).ToArray();
                var assigned = new Dictionary<(PhysicsGroup Group, PhysicsChain Chain), GravityPart>();
                foreach (var part in parts)
                {
                    var target = targets.FirstOrDefault(entry => string.Equals(entry.Chain.Root, part.Root, StringComparison.OrdinalIgnoreCase));
                    if (target.Group is null)
                        target = targets.FirstOrDefault(entry => part.Bones.Contains(boneIndices[entry.Chain.Root]));
                    if (target.Group is not null) assigned.Add(target, part);
                }
                foreach (var group in source)
                {
                    var groupParts = group.Chains.Select(chain => assigned.GetValueOrDefault((group, chain))).OfType<GravityPart>().ToArray();
                    var labels = string.Join("、", group.Chains.Select(chain => assigned.GetValueOrDefault((group, chain))?.Name ?? chain.Part).Distinct());
                    var groupNumber = group.Node.StartsWith("AnimGraphNode_KawaiiPhysicsGroup", StringComparison.Ordinal)
                        ? group.Node["AnimGraphNode_KawaiiPhysicsGroup".Length..].TrimStart('_') : group.Node;
                    var label = labels is "左袖、右袖" or "右袖、左袖" ? "双袖" : labels.Length == 0 ? "物理组" : labels;
                    var item = new TreeViewItem { Header = groupNumber.Length == 0 ? label : $"{label} · {groupNumber}",
                        Tag = new PhysicsSelection(group, null, profile.Bones, groupParts) };
                    foreach (var chain in group.Chains)
                    {
                        var part = assigned.GetValueOrDefault((group, chain));
                        item.Items.Add(new TreeViewItem { Header = part?.Name is { } name ? $"{name} · {chain.Root}" : chain.Root,
                            Tag = new PhysicsSelection(group, chain, profile.Bones, part is null ? [] : [part]) });
                    }
                    branch.Items.Add(item);
                }
                physicsTree.Items.Add(branch);
            }
            if (profile.Groups.Count == 0) physicsDetails.Children.Add(Hint("此角色的动画图未提供可识别的 Kawaii 物理组。"));
        }
        catch (Exception error) when (error is IOException or JsonException or InvalidOperationException or KeyNotFoundException or ArgumentException)
        {
            physicsDetails.Children.Add(Hint("无法读取物理组资料。"));
            physicsDetails.Children.Add(Hint(error.Message));
        }
    }

    private void ShowPhysicsSelection(PhysicsSelection? selection)
    {
        physicsSelection = selection;
        shoulderFollow = null;
        gravityControls = null;
        partGravityControls = null;
        capsuleControls = null; capsuleRows.Clear();
        physicsParameters.Clear();
        physicsDetails.Children.Clear();
        physicsHighlighted = (selection?.Chain?.Bones ?? selection?.Group.Chains.SelectMany(chain => chain.Bones) ?? []).ToHashSet();
        if (selection is not null)
            physicsHighlighted.UnionWith(selection.GravityParts.SelectMany(part => part.Bones));
        physicsSkeleton.Highlight(physicsHighlighted);
        highlightedModelTriangles = physicsModelView.Highlight(physicsHighlighted);
        if (selection is null) { physicsDetails.Children.Add(Hint("选择一个物理组或骨链。")); RefreshPhysicsSettings(); return; }

        var group = selection.Group;
        physicsDetails.Children.Add(Label(selection.Chain is null ? "物理组" : selection.GravityParts.FirstOrDefault()?.Name ?? selection.Chain.Part));
        physicsDetails.Children.Add(Hint(group.Node));
        if (group.Curve is not null) physicsDetails.Children.Add(Hint(group.Curve));
        if (selection.GravityParts.Count != 0)
        {
            AddGravityControls(part: true);
            if (selection.Chain is null)
                physicsDetails.Children.Add(Hint(selection.GravityParts.Count == 1 ? "调整本组关联的部件。" : "一起调整本组关联的部件。"));
        }
        physicsDetails.Children.Add(Spacer(12));
        physicsDetails.Children.Add(Label("资源导出值"));
        physicsDetails.Children.Add(Hint("阻尼　" + Value(group.Damping)));
        physicsDetails.Children.Add(Hint("回正强度　" + Value(group.Stiffness)));
        physicsDetails.Children.Add(Hint("摆角上限　" + Value(group.LimitAngle) + (group.LimitAngle is null ? "" : "°")));
        if (selection.Chain is null) AddGravityControls();
        AddPhysicsParameters(group);
        if (selection.Chain is { } chain)
        {
            physicsDetails.Children.Add(Spacer(12));
            physicsDetails.Children.Add(Label("根骨"));
            physicsDetails.Children.Add(Hint(chain.Root));
            if (chain.Bones.Count > 0 && selection.Bones[chain.Bones[0]].Parent is var parent && parent >= 0)
            {
                physicsDetails.Children.Add(Label("父骨"));
                physicsDetails.Children.Add(Hint(selection.Bones[parent].Name));
            }
            if (chain.Part == "袖子") AddShoulderFollow();
        }
        capsuleControls = new StackPanel { Margin = new(0, 10, 0, 0) };
        physicsDetails.Children.Add(capsuleControls);
        RefreshPhysicsSettings();
    }

    private static readonly (string Label, string Command, double Max, double Step)[] PhysicsParameterDefinitions =
    [
        ("阻尼", "native_damping", 1, .01), ("回正强度", "native_stiffness", 1, .01),
        ("摆角上限", "native_limit_angle", 180, 1)
    ];

    private void AddPhysicsParameters(PhysicsGroup group)
    {
        physicsDetails.Children.Add(Spacer(10));
        physicsDetails.Children.Add(Label("物理参数"));
        double?[] exported = [group.Damping, group.Stiffness, group.LimitAngle];
        for (var index = 0; index < PhysicsParameterDefinitions.Length; index++)
        {
            var definition = PhysicsParameterDefinitions[index];
            var slider = new Slider { Maximum = definition.Max, TickFrequency = definition.Step, IsSnapToTickEnabled = true, IsEnabled = false,
                Value = Math.Clamp(exported[index] ?? 0, 0, definition.Max) };
            var label = Label(definition.Label + "　" + Value(slider.Value));
            slider.ValueChanged += (_, _) => label.Text = definition.Label + "　" + Value(slider.Value);
            var readback = Hint("");
            var buttons = new WrapPanel { Margin = new(0, 2, 0, 8) };
            var apply = new Button { Content = "应用", Margin = new(0, 0, 4, 0) };
            var reset = new Button { Content = "还原" };
            apply.Click += async (_, _) => await EditPhysicsParameterAsync(definition.Command, slider.Value);
            reset.Click += async (_, _) => await EditPhysicsParameterAsync(definition.Command, -1);
            buttons.Children.Add(apply); buttons.Children.Add(reset);
            physicsDetails.Children.Add(label); physicsDetails.Children.Add(slider);
            physicsDetails.Children.Add(readback); physicsDetails.Children.Add(buttons);
            physicsParameters.Add(new(slider, apply, reset, readback));
        }
        RefreshPhysicsSettings();
    }

    private (uint Slot, JsonObject Group)? LivePhysicsGroup(JsonObject state)
    {
        if (physicsSelection is not { } selection || LivePhysicsActor(state) is not { } actor) return null;
        var candidates = actor.Groups.Where(row => NativePhysicsPreset.Matches(row, selection.Group.Curve ?? "None",
            selection.Group.Chains.Select(chain => chain.Root))).ToArray();
        return candidates.Length == 1 ? (actor.Slot, candidates[0]) : null;
    }

    private void AddShoulderFollow()
    {
        physicsDetails.Children.Add(Spacer(10));
        var slider = new Slider { Maximum = 100, TickFrequency = 5, IsSnapToTickEnabled = true, Value = 100, IsEnabled = false };
        var label = Label("肩部旋转跟随　100%");
        slider.ValueChanged += (_, _) => label.Text = $"肩部旋转跟随　{slider.Value:0}%";
        var readback = Hint("");
        var buttons = new WrapPanel { Margin = new(0, 2, 0, 8) };
        var apply = new Button { Content = "应用", Margin = new(0, 0, 4, 0) };
        var reset = new Button { Content = "还原" };
        apply.Click += async (_, _) => await EditPhysicsParameterAsync("native_shoulder_follow", slider.Value / 100);
        reset.Click += async (_, _) => await EditPhysicsParameterAsync("native_shoulder_follow", 1);
        buttons.Children.Add(apply); buttons.Children.Add(reset);
        physicsDetails.Children.Add(label); physicsDetails.Children.Add(slider);
        physicsDetails.Children.Add(readback); physicsDetails.Children.Add(buttons);
        shoulderFollow = new(slider, apply, reset, readback);
        RefreshPhysicsSettings();
    }

    private JsonObject? FindLiveSleeve(JsonObject? group)
    {
        if (physicsSelection?.Chain is not { } chain || group?["sleeves"] is not JsonArray sleeves) return null;
        return sleeves.OfType<JsonObject>().SingleOrDefault(sleeve => string.Equals(sleeve["root"]?.GetValue<string>(), chain.Root, StringComparison.OrdinalIgnoreCase));
    }

    private void AddGravityControls(bool part = false)
    {
        var command = part ? NativePhysicsPreset.PartGravityCommand : NativePhysicsPreset.GravityCommand;
        var toggle = new CheckBox { Content = part ? "部件重力" : "自定义向下重力", Margin = new(0, 12, 0, 3), IsEnabled = false,
            ToolTip = part ? "固定各部件的挂点，让其余骨段参与重力求解" : "修改此原生组的重力参数" };
        var slider = new Slider { Maximum = NativePhysicsPreset.GravityMaximum, TickFrequency = 10, IsSnapToTickEnabled = true,
            Value = part ? 980 : 150, IsEnabled = false, ToolTip = part ? "向下加速度（厘米/秒²）" : "重力强度使用游戏原生数值" };
        var strength = Label("强度　" + Value(slider.Value));
        slider.ValueChanged += (_, _) => strength.Text = "强度　" + Value(slider.Value);
        var readback = Hint("");
        var actions = new WrapPanel { Margin = new(0, 0, 0, 4) };
        var apply = new Button { Content = "应用", Margin = new(0, 0, 4, 0) };
        var reset = new Button { Content = "还原" };
        toggle.Click += async (_, _) => await EditPhysicsParameterAsync(command, toggle.IsChecked == true ? slider.Value : -1);
        apply.Click += async (_, _) => await EditPhysicsParameterAsync(command, slider.Value);
        reset.Click += async (_, _) => await EditPhysicsParameterAsync(command, -1);
        actions.Children.Add(apply); actions.Children.Add(reset);
        foreach (var element in new FrameworkElement[] { toggle, strength, slider, readback, actions }) physicsDetails.Children.Add(element);
        var row = new PhysicsGravityRow(toggle, slider, apply, reset, readback);
        if (part) partGravityControls = row;
        else gravityControls = row;
    }

    private JsonObject? FindLiveParameterTarget(JsonObject? group)
    {
        if (physicsSelection?.Chain is not { } chain) return group;
        return (group?["chains"] as JsonArray)?.OfType<JsonObject>().SingleOrDefault(row =>
            string.Equals(row["root"]?.GetValue<string>(), chain.Root, StringComparison.OrdinalIgnoreCase));
    }

    private (uint Slot, OwnedCharacterSelection Character)? SelectedLivePhysicsActor()
    {
        if (physicsActor.SelectedItem is not ActorCard card || card.Character.SelectedItem is not OwnedCharacterSelection character) return null;
        var slot = liveActorCards.FirstOrDefault(pair => ReferenceEquals(pair.Value, card)).Key;
        if (slot == 0 || activeRequest?.Actors.Select((actor, index) => (Actor: actor, Slot: actor.Slot == 0 ? (uint)index + 1 : actor.Slot))
            .Any(entry => entry.Slot == slot && entry.Actor.Enabled && entry.Actor.CharacterId == character.CharacterId) != true) return null;
        return (slot, character);
    }

    private LivePhysicsTarget? LivePhysicsActor(JsonObject state)
    {
        if (SelectedLivePhysicsActor() is not { } selected) return null;
        var actor = (state["actors"] as JsonArray)?.OfType<JsonObject>().FirstOrDefault(row => row["slot"]?.GetValue<uint>() == selected.Slot);
        return actor?["native_physics_settings_verified"]?.GetValue<bool>() == true && actor["native_physics_groups"] is JsonArray groups
            ? new LivePhysicsTarget(selected.Slot, selected.Character, groups.OfType<JsonObject>().ToArray(), actor["native_preview"] as JsonObject,
                (actor["gravity_parts"] as JsonArray ?? []).OfType<JsonObject>().ToArray()) : null;
    }

    private (ActorCard Card, OwnedCharacterSelection Character)? SelectedPhysicsActor() =>
        physicsActor.SelectedItem is ActorCard card && card.Character.SelectedItem is OwnedCharacterSelection character ? (card, character) : null;

    private bool CanDraftPhysics => session is null && !busy && !player.IsBusy;

    private void RefreshDraftInputs()
    {
        foreach (var row in physicsParameters) row.Slider.IsEnabled = false;
        if (shoulderFollow is { } shoulder) shoulder.Slider.IsEnabled = false;
        RefreshPhysicsSettings();
    }

    private void RefreshPhysicsSettings()
    {
        if (page != 3) return;
        var state = sessionControllable ? player.Snapshot?.State : null;
        var live = state is null ? null : LivePhysicsGroup(state);
        var targets = state is null ? null : LivePhysicsActor(state);
        var idle = CanDraftPhysics && !editingPhysics && SelectedPhysicsActor() is not null;
        var draftActor = SelectedPhysicsActor();
        var draft = draftActor is { } selected ? selected.Card.PhysicsPresets.GetValueOrDefault(selected.Character.CharacterId) : null;
        physicsSavePreset.IsEnabled = physicsLoadPreset.IsEnabled = idle || targets is not null && !editingPhysics && !busy && !player.IsBusy;
        comparePhysics.IsEnabled = targets is not null && !editingPhysics && !busy && !player.IsBusy;
        comparePhysics.Content = targets is not null && adjustedPhysics.ContainsKey(targets.Slot) ? "回到调整值" : "原厂对照";
        togglePhysicsPreview.IsEnabled = live is not null && !editingPhysics;
        var preview = targets?.Preview;
        var selectedPreview = live is not null && preview?["group_id"]?.GetValue<uint>() == live.Value.Group["group_id"]?.GetValue<uint>();
        togglePhysicsPreview.Content = selectedPreview ? "关闭实时预览" : "实时预览";
        if (selectedPreview && preview?["positions"] is JsonArray positions && positions.Count > 0)
        {
            physicsSkeleton.SetLivePose(preview["frame"]!.GetValue<ulong>(), positions);
            var solved = preview["solved_bones"]!.GetValue<int>();
            physicsPreviewHeading.Text = solved > 0 ? $"实时骨架 · 已发布姿态 · 选中组 {solved} 骨" : "实时骨架 · 已发布姿态";
        }
        else
        {
            physicsSkeleton.ClearLivePose();
            physicsPreviewHeading.Text = selectedPreview ? "等待实时骨架" : "参考骨架";
        }
        RefreshPhysicsModelHeading();
        var parameterTarget = FindLiveParameterTarget(live?.Group);
        var settings = parameterTarget?["settings"] as JsonObject;
        var selection = physicsSelection;
        var staged = selection is null ? null : NativePhysicsPreset.DraftValues(draft, selection.Group, selection.Chain?.Root);
        var inherited = selection?.Chain is null ? null : NativePhysicsPreset.DraftValues(draft, selection.Group, null);
        double?[] previewValues = [selection?.Group.Damping, selection?.Group.Stiffness, selection?.Group.LimitAngle];
        var liveEditable = !editingPhysics && !busy && !player.IsBusy;
        if (selection is not null)
        {
            RefreshGroupGravity(selection.Group, draft, live?.Group, idle, liveEditable);
            RefreshPartGravity(selection.GravityParts, draft, targets, idle, liveEditable);
        }
        for (var i = 0; i < physicsParameters.Count; i++)
        {
            var row = physicsParameters[i];
            var wasEnabled = row.Slider.IsEnabled;
            var preconfigured = staged?[i] is >= 0;
            var groupValue = inherited?[i] is >= 0;
            row.Apply.Content = CanDraftPhysics ? "加入预设" : "应用";
            row.Slider.IsEnabled = row.Apply.IsEnabled = idle || parameterTarget is not null && liveEditable;
            row.Reset.IsEnabled = idle ? preconfigured : row.Apply.IsEnabled;
            var original = (settings?["original"] as JsonArray)?[i]?.GetValue<double>();
            var requested = (settings?["requested"] as JsonArray)?[i]?.GetValue<double>();
            var first = (settings?["effective_first"] as JsonArray)?[i]?.GetValue<double>();
            var last = (settings?["effective_last"] as JsonArray)?[i]?.GetValue<double>();
            var initial = idle ? preconfigured ? staged![i] : groupValue ? inherited![i] : previewValues[i] : requested >= 0 ? requested : original;
            if (!wasEnabled && row.Slider.IsEnabled) row.Slider.Value = Math.Clamp(initial ?? 0, 0, row.Slider.Maximum);
            row.Readback.Text = idle ? preconfigured ? "预设值 " + Value(staged![i]) : groupValue ? "继承整组预设 " + Value(inherited![i]) : "开演时使用原值" :
                parameterTarget is null ? "等待匹配本次播放的物理骨链" :
                original is null ? "等待游戏求值" : $"原值 {Value(original)} · 求值 {Value(first)}" + (first == last ? "" : $" / {Value(last)}");
        }
        if (shoulderFollow is { } shoulder)
        {
            var sleeve = FindLiveSleeve(live?.Group);
            var follow = selection?.Chain is { } chain ? NativePhysicsPreset.DraftFollow(draft, selection.Group, chain.Root) : 1;
            var wasEnabled = shoulder.Slider.IsEnabled;
            shoulder.Apply.Content = CanDraftPhysics ? "加入预设" : "应用";
            shoulder.Slider.IsEnabled = shoulder.Apply.IsEnabled = idle || sleeve is not null && liveEditable;
            shoulder.Reset.IsEnabled = idle ? follow != 1 : shoulder.Apply.IsEnabled;
            if (!wasEnabled && shoulder.Slider.IsEnabled) shoulder.Slider.Value = (idle ? follow : sleeve?["follow"]?.GetValue<double>() ?? 1) * 100;
            shoulder.Readback.Text = idle ? follow != 1 ? $"预设值 {follow * 100:0}%" : "开演时使用原值" :
                sleeve?["sampled_follow"] is JsonValue applied ? $"原值 100% · 输入 {applied.GetValue<double>() * 100:0}%" : "等待匹配袖套骨链";
        }
        RefreshCapsules(live?.Group);
        RefreshCapsuleEditor(targets, live?.Group, liveEditable);
    }

    private static JsonObject? FindLiveGravityPart(LivePhysicsTarget? target, string root) =>
        target?.Parts.SingleOrDefault(part => string.Equals(Contract.S(part, "root"), root, StringComparison.OrdinalIgnoreCase));

    private void RefreshGroupGravity(PhysicsGroup group, JsonObject? draft, JsonObject? live, bool idle, bool liveEditable)
    {
        if (gravityControls is not { } row) return;
        var gravity = live?["gravity"] as JsonObject;
        var requested = idle ? NativePhysicsPreset.DraftGravity(draft, group) : gravity?["requested"]?.GetValue<double>() ?? -1;
        var editable = idle || gravity is not null && liveEditable;
        row.Toggle.IsEnabled = editable;
        row.Toggle.IsChecked = requested >= 0;
        row.Apply.Content = idle ? "加入预设" : "应用";
        row.Slider.IsEnabled = row.Apply.IsEnabled = row.Reset.IsEnabled = editable && requested >= 0;
        if (row.LastRequest != requested)
        {
            row.LastRequest = requested;
            row.Slider.Value = requested >= 0 ? requested : 150;
        }
        row.Readback.Text = idle ? requested >= 0 ? "预设值 " + Value(requested) : "开演时使用原值"
            : gravity?["suspended_by_parts"]?.GetValue<bool>() == true ? "部件已覆盖整组，原生重力覆盖已暂停。"
            : gravity is null ? "等待匹配实机重力" : gravity["effective"] is JsonArray effective
            ? $"原值 Z {Value(gravity["original"]?[2]?.GetValue<double>())} · 求值 Z {Value(effective[2]?.GetValue<double>())}"
            : "等待游戏求值";
    }

    private void RefreshPartGravity(IReadOnlyList<GravityPart> parts, JsonObject? draft, LivePhysicsTarget? target, bool idle, bool liveEditable)
    {
        if (partGravityControls is not { } row) return;
        var live = parts.Select(part => FindLiveGravityPart(target, part.Root)).ToArray();
        var values = idle ? NativePhysicsPreset.DraftPartGravity(draft, parts.Select(part => part.Root))
            : live.Select(part => part?["strength"]?.GetValue<double>() ?? -1).ToArray();
        var requested = values.Max();
        var mixed = values.Any(value => value != requested);
        var matched = live.All(part => part is not null);
        var editable = idle || matched && liveEditable;
        row.Toggle.IsEnabled = editable;
        row.Toggle.IsChecked = mixed ? null : requested >= 0;
        row.Apply.Content = idle ? "加入预设" : "应用";
        row.Slider.IsEnabled = row.Apply.IsEnabled = row.Reset.IsEnabled = editable && requested >= 0;
        if (row.LastRequest != requested)
        {
            row.LastRequest = requested;
            row.Slider.Value = requested >= 0 ? requested : 980;
        }
        row.Readback.Text = !idle && !matched ? "等待匹配实机部件"
            : mixed ? "部件强度不同。应用会统一强度。"
            : requested < 0 ? "使用原值"
            : idle ? "预设值 " + Value(requested)
            : $"强度 {Value(requested)} · 已更新 {live.Sum(part => part?["moved_bones"]?.GetValue<int>() ?? 0)} 骨";
    }

    private void RefreshCapsules(JsonObject? group)
    {
        if (capsuleControls is null) return;
        var rows = (group?["capsules"] as JsonArray ?? []).OfType<JsonObject>().ToArray();
        if (!capsuleRows.Keys.ToHashSet().SetEquals(rows.Select(row => row["driver_id"]!.GetValue<uint>())))
        {
            capsuleControls.Children.Clear(); capsuleRows.Clear();
            if (rows.Length > 0) capsuleControls.Children.Add(Label("本组胶囊碰撞"));
            foreach (var row in rows)
            {
                var driver = row["driver"]!.GetValue<string>();
                var id = row["driver_id"]!.GetValue<uint>();
                var label = driver.ToLowerInvariant() switch
                {
                    "bip001lclavicle" => "左肩", "bip001rclavicle" => "右肩", "bip001lupperarm" => "左上臂", "bip001rupperarm" => "右上臂",
                    "bip001lforearm" => "左前臂", "bip001rforearm" => "右前臂", _ => driver
                };
                var toggle = new CheckBox { Content = label, ToolTip = driver };
                var count = Hint("");
                toggle.Click += async (_, _) => await SetCapsuleAsync(id, toggle.IsChecked == true);
                capsuleRows.Add(id, (toggle, count)); capsuleControls.Children.Add(toggle); capsuleControls.Children.Add(count);
            }
        }
        foreach (var row in rows)
        {
            var controls = capsuleRows[row["driver_id"]!.GetValue<uint>()];
            controls.Toggle.IsChecked = row["enabled"]!.GetValue<bool>();
            controls.Toggle.IsEnabled = !editingPhysics && !busy && !player.IsBusy;
            controls.Counts.Text = $"求值 {row["evaluated_count"]} / 原厂 {row["original_count"]}";
        }
    }

    private async Task SetCapsuleAsync(uint driver, bool enabled)
    {
        if (editingPhysics || busy || player.IsBusy || session is not { } directory || player.Snapshot is not { } snapshot ||
            LivePhysicsGroup(snapshot.State) is not { } target) return;
        editingPhysics = true;
        try
        {
            await PlaybackControl.SendAsync(directory, "native_capsule_collision", enabled ? 1 : 0, Token,
                target.Slot, target.Group["group_id"]!.GetValue<uint>(), driver);
        }
        catch (Exception error) { status.Text = error.Message; }
        finally { editingPhysics = false; RefreshPhysicsSettings(); }
    }

    private (string Session, uint Slot, uint Group)? physicsPreviewSource;

    private async Task TogglePhysicsPreviewAsync()
    {
        if (editingPhysics || session is not { } directory || player.Snapshot is not { } snapshot ||
            LivePhysicsGroup(snapshot.State) is not { } target) return;
        var group = target.Group["group_id"]!.GetValue<uint>();
        var previous = physicsPreviewSource;
        var enabled = LivePhysicsActor(snapshot.State)?.Preview?["group_id"]?.GetValue<uint>() != group;
        editingPhysics = true;
        RefreshPhysicsSettings();
        try
        {
            if (previous is { } other && other.Session == directory && other.Slot != target.Slot)
                await PlaybackControl.SendAsync(directory, "native_preview", 0, Token, other.Slot, other.Group);
            await PlaybackControl.SendAsync(directory, "native_preview", enabled ? 1 : 0, Token, target.Slot, group);
            physicsPreviewSource = enabled ? (directory, target.Slot, group) : null;
        }
        catch (Exception error) { status.Text = error.Message; }
        finally { editingPhysics = false; RefreshPhysicsSettings(); }
    }

    private async Task LoopPhysicsSegmentAsync()
    {
        RequireSession();
        var duration = player.Snapshot is { } snapshot ? Contract.D(snapshot.State, "duration_seconds") : 0;
        var start = Number(physicsLoopStart, 0, duration);
        var end = string.IsNullOrWhiteSpace(physicsLoopEnd.Text) ? duration : Number(physicsLoopEnd, 0, duration);
        if (end <= start) throw new InvalidOperationException("循环终点需要晚于起点。");
        await SendAsync("preview_loop_start", start);
        await SendAsync("preview_loop_end", end);
        await SendAsync("play");
    }

    private async Task ComparePhysicsAsync()
    {
        if (editingPhysics || session is not { } directory || player.Snapshot is not { } snapshot || LivePhysicsActor(snapshot.State) is not { } target) return;
        var adjusted = adjustedPhysics.GetValueOrDefault(target.Slot);
        var current = NativePhysicsPreset.Create(target.Character, target.Groups, target.Parts);
        editingPhysics = true;
        RefreshPhysicsSettings();
        try
        {
            await ApplyPhysicsPresetAsync(directory, target, adjusted ?? NativePhysicsPreset.Create(target.Character, []));
            if (session != directory) return;
            if (adjusted is null) adjustedPhysics[target.Slot] = current;
            else adjustedPhysics.Remove(target.Slot);
            var start = snapshot.State["preview_loop_start"]?.GetValue<double>() ?? 0;
            await SendAsync("seek", start);
            await SendAsync("play");
            status.Text = adjusted is null ? "从同一起点播放原厂参数。" : "从同一起点播放已调整的参数。";
        }
        catch (Exception error) { status.Text = error.Message; }
        finally { editingPhysics = false; RefreshPhysicsSettings(); }
    }

    private Task ApplyPhysicsPresetAsync(string directory, LivePhysicsTarget target, JsonObject preset) =>
        NativePhysicsPreset.ApplyAsync(directory, target.Slot, NativePhysicsPreset.Plan(preset, target.Character, target.Groups, target.Parts), Token);

    private static void KeepPhysicsDraft(ActorCard card, OwnedCharacterSelection character, JsonObject preset)
    {
        if (NativePhysicsPreset.HasOverrides(preset)) card.PhysicsPresets[character.CharacterId] = preset;
        else card.PhysicsPresets.Remove(character.CharacterId);
    }

    private void SavePhysicsPreset()
    {
        if (SelectedPhysicsActor() is not { } selected) return;
        var live = sessionControllable && player.Snapshot is { } snapshot ? LivePhysicsActor(snapshot.State) : null;
        if (!CanDraftPhysics && live is null) return;
        var preset = live is not null ? NativePhysicsPreset.Create(live.Character, live.Groups, live.Parts)
            : selected.Card.PhysicsPresets.GetValueOrDefault(selected.Character.CharacterId) ?? NativePhysicsPreset.Create(selected.Character, []);
        var dialog = new SaveFileDialog { Filter = "Kawaii 物理预设|*.wwphysics", FileName = selected.Character.DisplayName + ".wwphysics", AddExtension = true };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            JsonData.Write(dialog.FileName, preset);
            KeepPhysicsDraft(selected.Card, selected.Character, preset);
            status.Text = "已保存该演员的物理参数预设。";
        }
        catch (Exception error) { status.Text = error.Message; }
    }

    private async Task LoadPhysicsPresetAsync()
    {
        if (editingPhysics || SelectedPhysicsActor() is not { } selected) return;
        var directory = session;
        var dialog = new OpenFileDialog { Filter = "Kawaii 物理预设|*.wwphysics" };
        if (dialog.ShowDialog(this) != true || session != directory || SelectedPhysicsActor() != selected) return;
        try { await LoadPhysicsPresetFileAsync(dialog.FileName); }
        catch (Exception error) { status.Text = error.Message; }
    }

    private async Task LoadPhysicsPresetFileAsync(string path)
    {
        if (editingPhysics || SelectedPhysicsActor() is not { } selected) return;
        var preset = JsonData.ReadObject(path);
        NativePhysicsPreset.ValidateDraft(preset, selected.Character.CharacterId, selected.Character.ProfileId);
        if (CanDraftPhysics)
        {
            KeepPhysicsDraft(selected.Card, selected.Character, preset);
            RefreshDraftInputs();
            status.Text = "已载入物理预设。用原生物理开演时会自动应用。";
            return;
        }
        if (session is not { } directory || !sessionControllable || player.Snapshot is not { } snapshot || LivePhysicsActor(snapshot.State) is not { } target) return;
        editingPhysics = true;
        RefreshPhysicsSettings();
        try
        {
            await ApplyPhysicsPresetAsync(directory, target, preset);
            KeepPhysicsDraft(selected.Card, selected.Character, preset);
            adjustedPhysics.Remove(target.Slot);
            status.Text = "已提交预设，参数会在下一次求值时生效。";
        }
        catch (Exception error) { status.Text = error.Message; }
        finally { editingPhysics = false; RefreshPhysicsSettings(); }
    }

    private bool editingPhysics;
    private async Task EditPhysicsParameterAsync(string command, double value)
    {
        if (!editingPhysics && CanDraftPhysics && SelectedPhysicsActor() is { } authoring && physicsSelection is { } selection)
        {
            var preset = authoring.Card.PhysicsPresets.GetValueOrDefault(authoring.Character.CharacterId) ?? NativePhysicsPreset.Create(authoring.Character, []);
            if (command == NativePhysicsPreset.PartGravityCommand && selection.GravityParts.Count != 0)
                NativePhysicsPreset.SetDraftPartGravity(preset, selection.GravityParts.Select(part => part.Root), value);
            else if (command == NativePhysicsPreset.GravityCommand && selection.Chain is null)
                NativePhysicsPreset.SetDraftGravity(preset, selection.Group, value);
            else if (command == "native_shoulder_follow" && selection.Chain is { } sleeveChain)
                NativePhysicsPreset.SetDraftFollow(preset, selection.Group, sleeveChain.Root, value);
            else if (Array.IndexOf(NativePhysicsPreset.Commands, command) is var index && index >= 0)
                NativePhysicsPreset.SetDraftValue(preset, selection.Group, selection.Chain?.Root, index, value);
            KeepPhysicsDraft(authoring.Card, authoring.Character, preset);
            RefreshDraftInputs();
            status.Text = value == -1 || command == "native_shoulder_follow" && value == 1
                ? "已移除此项的预设值。" : "已加入演员的物理预设，用原生物理开演时自动应用。";
            return;
        }
        if (editingPhysics || busy || player.IsBusy || session is not { } directory || !sessionControllable || player.Snapshot is not { } snapshot ||
            LivePhysicsActor(snapshot.State) is not { } target) return;
        if (command == NativePhysicsPreset.PartGravityCommand)
        {
            if (physicsSelection is { } partSelection) await EditPartGravityAsync(directory, target, partSelection.GravityParts, value);
            return;
        }
        var group = LivePhysicsGroup(snapshot.State)?.Group;
        var selected = command switch
        {
            NativePhysicsPreset.GravityCommand => physicsSelection?.Chain is null ? group?["gravity"] as JsonObject : null,
            "native_shoulder_follow" => FindLiveSleeve(group),
            _ => FindLiveParameterTarget(group)
        };
        if (selected is null) return;
        var chain = selected["chain_id"]?.GetValue<uint>() ?? 0;
        editingPhysics = true;
        RefreshPhysicsSettings();
        try
        {
            var received = await PlaybackControl.SendAsync(directory, command, value, Token, target.Slot,
                group!["group_id"]!.GetValue<uint>(), chain);
            var current = LivePhysicsGroup(received)?.Group;
            var revision = received["last_control_sequence"]?.GetValue<ulong>();
            var observed = command switch
            {
                NativePhysicsPreset.GravityCommand => current?["gravity"]?["sampled_revision"]?.GetValue<ulong>() == revision,
                "native_shoulder_follow" => FindLiveSleeve(current)?["sampled_follow"] is JsonValue sample && Math.Abs(sample.GetValue<double>() - value) < .000001,
                _ => FindLiveParameterTarget(current)?["settings"]?["sampled_revision"]?.GetValue<ulong>() == revision
            };
            status.Text = observed
                ? "已读回本次物理求值。" : "物理参数已提交，等待下一次求值。";
        }
        catch (Exception error) { status.Text = error.Message; }
        finally { editingPhysics = false; RefreshPhysicsSettings(); }
    }

    private async Task EditPartGravityAsync(string directory, LivePhysicsTarget target, IReadOnlyList<GravityPart> parts, double value)
    {
        var edits = new List<(uint Id, double Previous)>();
        foreach (var part in parts)
        {
            var live = FindLiveGravityPart(target, part.Root);
            if (live is null) return;
            var previous = Contract.D(live, "strength");
            if (previous != value) edits.Add((Contract.N(live, "id").GetValue<uint>(), previous));
        }
        if (edits.Count == 0) return;
        editingPhysics = true;
        RefreshPhysicsSettings();
        var attempted = new List<(uint Id, double Previous)>();
        try
        {
            foreach (var edit in edits)
            {
                attempted.Add(edit);
                await PlaybackControl.SendAsync(directory, NativePhysicsPreset.PartGravityCommand, value, Token, target.Slot, 0, edit.Id);
            }
            status.Text = "部件重力已提交，等待下一次求值。";
        }
        catch (Exception error)
        {
            var restored = true;
            if (edits.Count > 1 && session == directory && !Token.IsCancellationRequested)
                foreach (var edit in attempted.AsEnumerable().Reverse())
                    try { await PlaybackControl.SendAsync(directory, NativePhysicsPreset.PartGravityCommand, edit.Previous, Token, target.Slot, 0, edit.Id); }
                    catch (Exception) { restored = false; }
            status.Text = error.Message + (restored ? "" : " 部分设置可能已生效，请查看部件状态。");
        }
        finally
        {
            editingPhysics = false;
            RefreshPhysicsSettings();
        }
    }

    private static string Value(double? number) => number?.ToString("0.###", CultureInfo.InvariantCulture) ?? "暂无数据";
    private sealed record GravityPart(string Root, string? Name, IReadOnlyList<int> Bones);
    private sealed record PhysicsSelection(PhysicsGroup Group, PhysicsChain? Chain, IReadOnlyList<PhysicsBone> Bones, IReadOnlyList<GravityPart> GravityParts);
    private sealed record LivePhysicsTarget(uint Slot, OwnedCharacterSelection Character, IReadOnlyList<JsonObject> Groups, JsonObject? Preview, IReadOnlyList<JsonObject> Parts);
    private sealed record PhysicsParameterRow(Slider Slider, Button Apply, Button Reset, TextBlock Readback);
    private sealed record PhysicsGravityRow(CheckBox Toggle, Slider Slider, Button Apply, Button Reset, TextBlock Readback)
    {
        public double? LastRequest { get; set; }
    }

    private sealed class PhysicsSkeletonView : FrameworkElement
    {
        private IReadOnlyList<PhysicsBone> bones = [];
        private System.Numerics.Vector3[]? livePositions;
        private ulong liveFrame;
        private HashSet<int> body = [];
        private HashSet<int> highlighted = [];
        private int view;
        private readonly Pen skeletonPen = new(new SolidColorBrush(Color.FromRgb(115, 127, 144)), 1);
        private readonly Pen selectedPen = new(new SolidColorBrush(Color.FromRgb(186, 75, 37)), 2.6);

        public void SetBones(IReadOnlyList<PhysicsBone> value)
        {
            bones = value;
            livePositions = null; liveFrame = 0;
            body = Enumerable.Range(0, bones.Count).Where(i => bones[i].Name.StartsWith("Bip001", StringComparison.OrdinalIgnoreCase) &&
                !bones[i].Name.Contains("Finger", StringComparison.OrdinalIgnoreCase)).ToHashSet();
            if (body.Count == 0) body = Enumerable.Range(0, bones.Count).ToHashSet();
            InvalidateVisual();
        }
        public void SetLivePose(ulong frame, JsonArray positions)
        {
            if (positions.Count != bones.Count || livePositions is not null && liveFrame == frame) return;
            livePositions = positions.Select(row => new System.Numerics.Vector3(row![0]!.GetValue<float>(), row[1]!.GetValue<float>(), row[2]!.GetValue<float>())).ToArray();
            liveFrame = frame; InvalidateVisual();
        }
        public void ClearLivePose()
        {
            if (livePositions is null) return;
            livePositions = null; liveFrame = 0; InvalidateVisual();
        }
        public void SetView(int value) { view = value; InvalidateVisual(); }
        public void Highlight(IEnumerable<int> indices) { highlighted = indices.ToHashSet(); InvalidateVisual(); }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            if (bones.Count == 0 || ActualWidth <= 32 || ActualHeight <= 32) return;
            var positions = livePositions ?? bones.Select(bone => bone.Position).ToArray();
            var points = positions.Select(position => new Point(view switch
            {
                1 => position.Y, 2 => (position.X + position.Y) * 0.707107, _ => position.X
            }, -position.Z)).ToArray();
            var visible = body.Union(highlighted).Select(i => points[i]).ToArray();
            var left = visible.Min(p => p.X); var top = visible.Min(p => p.Y);
            var width = visible.Max(p => p.X) - left; var height = visible.Max(p => p.Y) - top;
            var scale = Math.Min((ActualWidth - 32) / Math.Max(width, 1), (ActualHeight - 32) / Math.Max(height, 1));
            var x = (ActualWidth - width * scale) / 2; var y = (ActualHeight - height * scale) / 2;
            for (var i = 0; i < points.Length; i++) points[i] = new Point(x + (points[i].X - left) * scale, y + (points[i].Y - top) * scale);
            foreach (var i in body)
                if (body.Contains(bones[i].Parent)) dc.DrawLine(skeletonPen, points[bones[i].Parent], points[i]);
            foreach (var i in highlighted)
            {
                var parent = bones[i].Parent;
                if (parent >= 0 && highlighted.Contains(parent)) dc.DrawLine(selectedPen, points[parent], points[i]);
                dc.DrawEllipse(selectedPen.Brush, null, points[i], 2, 2);
            }
        }
    }
}
