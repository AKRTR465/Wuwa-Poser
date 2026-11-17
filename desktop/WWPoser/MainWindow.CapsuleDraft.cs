using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WWPoser.Services;

namespace WWPoser;

public sealed partial class MainWindow
{
    private sealed record CapsuleGroupChoice(PhysicsGroup Group, string Label);
    private sealed record CapsuleBoneChoice(string? Name, string Label);
    private sealed record CapsuleDraftRow(CapsuleViewItem View, CapsuleDraftShape Shape)
    {
        public string Label => View.Label;
    }

    private sealed class CapsuleHistory
    {
        public Stack<CapsuleDraftGroup[]> Undo { get; } = new();
        public Stack<CapsuleDraftGroup[]> Redo { get; } = new();
    }

    private readonly StackPanel capsuleDraftTools = new();
    private readonly List<FrameworkElement> capsuleRotationFields = [];
    private readonly ComboBox capsuleGroupPicker = new() { DisplayMemberPath = nameof(CapsuleGroupChoice.Label) };
    private readonly ComboBox capsuleBonePicker = new() { DisplayMemberPath = nameof(CapsuleBoneChoice.Label), IsTextSearchEnabled = true };
    private readonly ListBox capsuleDraftList = new() { DisplayMemberPath = nameof(CapsuleDraftRow.Label), SelectionMode = SelectionMode.Extended, MaxHeight = 152 };
    private readonly RadioButton capsuleMove = new() { Content = "移动", IsChecked = true, Margin = new(0, 4, 8, 4) };
    private readonly RadioButton capsuleRotate = new() { Content = "旋转", Margin = new(0, 4, 8, 4) };
    private readonly RadioButton capsulePlace = new() { Content = "放置", Margin = new(0, 4, 0, 4) };
    private readonly Dictionary<(ActorCard, string), CapsuleHistory> capsuleHistory = [];
    private readonly Dictionary<CapsuleKey, CapsuleDraftRow> capsuleDraftRows = [];
    private readonly HashSet<Guid> selectedDraftCapsules = [];
    private PhysicsPartProfile? capsuleDraftProfile;
    private CapsuleStockCatalog? capsuleStockCatalog;
    private PhysicsSelection? capsuleTreeSelection;
    private CapsuleGroupChoice[] capsuleGroupChoices = [];
    private IReadOnlyList<CapsuleDraftGroup> capsuleStock = [];
    private CapsuleDraftRow[] capsuleListRows = [];
    private string? capsuleStockError;
    private bool refreshingCapsuleDraft;
    private Guid? primaryDraftCapsule;
    private Button capsuleDraftAdd = null!, capsuleDraftDuplicate = null!, capsuleDraftCopy = null!, capsuleDraftPaste = null!, capsuleDraftSplit = null!,
        capsuleDraftMerge = null!, capsuleDraftDelete = null!, capsuleDraftReset = null!, capsuleDraftUndo = null!, capsuleDraftRedo = null!;

    private FrameworkElement BuildCapsuleDraftTools()
    {
        capsuleDraftTools.Visibility = Visibility.Collapsed;
        capsuleDraftTools.Children.Add(Label("物理组"));
        capsuleDraftTools.Children.Add(capsuleGroupPicker);
        capsuleDraftTools.Children.Add(capsuleDraftList);
        capsuleGroupPicker.SelectionChanged += (_, _) =>
        {
            if (refreshingCapsuleDraft) return;
            selectedDraftCapsules.Clear(); primaryDraftCapsule = null;
            capsuleTreeSelection = physicsSelection;
            RefreshPhysicsSettings();
        };
        capsuleDraftList.SelectionChanged += (_, e) =>
        {
            if (refreshingCapsuleDraft) return;
            selectedDraftCapsules.Clear();
            foreach (var row in capsuleDraftList.SelectedItems.OfType<CapsuleDraftRow>())
                selectedDraftCapsules.Add(row.Shape.Id);
            primaryDraftCapsule = e.AddedItems.OfType<CapsuleDraftRow>().LastOrDefault()?.Shape.Id
                ?? capsuleDraftList.SelectedItems.OfType<CapsuleDraftRow>().LastOrDefault()?.Shape.Id;
            lastCapsuleValues = [];
            RefreshPhysicsSettings();
        };

        var actions = new WrapPanel();
        capsuleDraftAdd = AddCapsuleDraftButton(actions, "新增", AddDraftCapsule);
        capsuleDraftDuplicate = AddCapsuleDraftButton(actions, "复制一份", DuplicateDraftCapsules);
        capsuleDraftCopy = AddCapsuleDraftButton(actions, "复制", CopyDraftCapsules);
        capsuleDraftPaste = AddCapsuleDraftButton(actions, "粘贴", PasteDraftCapsules);
        capsuleDraftSplit = AddCapsuleDraftButton(actions, "拆分", SplitDraftCapsules);
        capsuleDraftMerge = AddCapsuleDraftButton(actions, "合并", MergeDraftCapsules);
        capsuleDraftDelete = AddCapsuleDraftButton(actions, "删除", DeleteDraftCapsules);
        capsuleDraftUndo = AddCapsuleDraftButton(actions, "撤销", () => RollCapsuleHistory(undo: true));
        capsuleDraftRedo = AddCapsuleDraftButton(actions, "重做", () => RollCapsuleHistory(undo: false));
        capsuleDraftReset = AddCapsuleDraftButton(actions, "恢复组原厂", ResetDraftCapsuleGroup);
        capsuleDraftTools.Children.Add(actions);
        var transforms = new WrapPanel();
        transforms.Children.Add(capsuleMove);
        transforms.Children.Add(capsuleRotate);
        transforms.Children.Add(capsulePlace);
        capsuleMove.Checked += (_, _) => RefreshPhysicsSettings();
        capsuleRotate.Checked += (_, _) => RefreshPhysicsSettings();
        capsulePlace.Checked += (_, _) => RefreshPhysicsSettings();
        capsuleDraftTools.Children.Add(transforms);
        capsuleDraftTools.Children.Add(Label("驱动骨"));
        capsuleDraftTools.Children.Add(capsuleBonePicker);
        capsuleBonePicker.SelectionChanged += (_, _) =>
        {
            if (!refreshingCapsuleDraft && capsuleBonePicker.SelectedItem is CapsuleBoneChoice bone)
                RunCapsuleDraftAction(() => EditSelectedDraftCapsules(shape => shape with { Bone = bone.Name }));
        };
        return capsuleDraftTools;
    }

    private Button AddCapsuleDraftButton(Panel parent, string label, Action action)
    {
        var button = new Button { Content = label, Padding = new(6, 3, 6, 3), Margin = new(0, 3, 4, 0) };
        button.Click += (_, _) => RunCapsuleDraftAction(action);
        parent.Children.Add(button);
        return button;
    }

    private void SetCapsuleDraftProfile(OwnedCharacterSelection? character, PhysicsPartProfile? profile)
    {
        capsuleDraftProfile = profile;
        capsuleTreeSelection = null;
        capsuleStock = [];
        capsuleStockError = null;
        capsuleListRows = [];
        selectedDraftCapsules.Clear(); primaryDraftCapsule = null;
        capsuleDraftRows.Clear();
        if (character is not null && profile is not null)
        {
            try { capsuleStock = (capsuleStockCatalog ??= new(root)).Read(character, profile.Groups); }
            catch (Exception error) when (error is IOException or InvalidDataException or JsonException)
            { capsuleStockError = error.Message; }
        }
        capsuleGroupChoices = (profile?.Groups ?? []).DistinctBy(GroupSignature, StringComparer.OrdinalIgnoreCase)
            .Select(group => new CapsuleGroupChoice(group,
                (group.Curve?.Replace("Kawaii_alpha_", "", StringComparison.Ordinal) ?? "无曲线") + " · " + group.Node.Split('_').Last()))
            .ToArray();
        refreshingCapsuleDraft = true;
        try
        {
            capsuleGroupPicker.ItemsSource = capsuleGroupChoices;
            capsuleGroupPicker.SelectedIndex = capsuleGroupChoices.Length == 0 ? -1 : 0;
            capsuleDraftList.ItemsSource = capsuleListRows;
            capsuleBonePicker.ItemsSource = new[] { new CapsuleBoneChoice(null, "组件空间") }
                .Concat((profile?.Bones ?? []).Select(bone => new CapsuleBoneChoice(bone.Name, bone.Name))).ToArray();
        }
        finally { refreshingCapsuleDraft = false; }
        physicsModelView.SetCapsuleReference((profile?.Bones ?? []).Select(bone => bone.ReferenceTransform).ToArray(),
            (profile?.Bones ?? []).Select(bone => bone.Parent).ToArray());
        lastCapsuleValues = [];
    }

    private static string GroupSignature(PhysicsGroup group) =>
        (group.Curve ?? "None") + "\n" + string.Join("\n", group.Chains.Select(chain => chain.Root).Order(StringComparer.OrdinalIgnoreCase));

    private CapsuleDraftGroup DraftCapsuleGroup(PhysicsGroup group, IReadOnlyList<CapsuleDraftGroup> overrides) =>
        overrides.FirstOrDefault(row => CapsuleAuthoring.Matches(row, group)) ??
        capsuleStock.FirstOrDefault(row => CapsuleAuthoring.Matches(row, group)) ??
        new(group.Curve ?? "None", group.Chains.Select(chain => chain.Root).ToArray(), [], ReplaceStock: false);

    private bool RefreshDraftCapsuleEditor(bool editable)
    {
        if (session is not null) return false;
        capsuleDraftTools.Visibility = Visibility.Visible;
        capsulePicker.Visibility = Visibility.Collapsed;
        capsuleReset.Visibility = Visibility.Collapsed;
        foreach (var field in capsuleRotationFields) field.Visibility = Visibility.Visible;
        var selected = SelectedPhysicsActor();
        var allowed = editable && CanDraftPhysics && selected is not null && capsuleDraftProfile is not null;
        var preset = selected is { } actor ? actor.Card.PhysicsPresets.GetValueOrDefault(actor.Character.CharacterId) : null;
        var overrides = CapsuleAuthoring.Read(preset);
        refreshingCapsuleDraft = true;
        try
        {
            if (!ReferenceEquals(capsuleTreeSelection, physicsSelection))
            {
                capsuleTreeSelection = physicsSelection;
                var next = physicsSelection is { } selection
                    ? capsuleGroupChoices.FirstOrDefault(choice =>
                        string.Equals(GroupSignature(choice.Group), GroupSignature(selection.Group), StringComparison.OrdinalIgnoreCase))
                    : null;
                if (next is not null && !ReferenceEquals(capsuleGroupPicker.SelectedItem, next))
                {
                    capsuleGroupPicker.SelectedItem = next;
                    selectedDraftCapsules.Clear();
                    primaryDraftCapsule = null;
                }
            }
            var currentGroup = (capsuleGroupPicker.SelectedItem as CapsuleGroupChoice)?.Group;
            var currentDraft = currentGroup is null ? null : DraftCapsuleGroup(currentGroup, overrides);
            PopulateDraftCapsuleRows(overrides);
            var rows = currentGroup is null ? [] : capsuleDraftRows.Values
                .Where(row => row.View.Key.Group == (uint)capsuleGroupPicker.SelectedIndex + 1).ToArray();
            selectedDraftCapsules.IntersectWith(rows.Select(row => row.Shape.Id));
            if (selectedDraftCapsules.Count == 0 && rows.Length != 0) selectedDraftCapsules.Add(rows[0].Shape.Id);
            if (primaryDraftCapsule is not { } id || !selectedDraftCapsules.Contains(id))
                primaryDraftCapsule = rows.FirstOrDefault(row => selectedDraftCapsules.Contains(row.Shape.Id))?.Shape.Id;
            var primary = rows.FirstOrDefault(row => row.Shape.Id == primaryDraftCapsule);
            if (!rows.SequenceEqual(capsuleListRows))
            {
                capsuleListRows = rows;
                capsuleDraftList.ItemsSource = rows;
            }
            var displayedSelection = capsuleDraftList.SelectedItems.OfType<CapsuleDraftRow>().Select(row => row.Shape.Id).ToHashSet();
            if (!displayedSelection.SetEquals(selectedDraftCapsules))
            {
                capsuleDraftList.SelectedItems.Clear();
                foreach (var row in capsuleListRows.Where(row => selectedDraftCapsules.Contains(row.Shape.Id)))
                    capsuleDraftList.SelectedItems.Add(row);
            }
            var count = selectedDraftCapsules.Count;
            var canEdit = allowed && primary is not null;
            capsuleDraftAdd.IsEnabled = capsuleDraftPaste.IsEnabled = allowed && currentGroup is not null;
            capsuleDraftDuplicate.IsEnabled = capsuleDraftCopy.IsEnabled = capsuleDraftSplit.IsEnabled = capsuleDraftDelete.IsEnabled = canEdit;
            capsuleDraftMerge.IsEnabled = canEdit && count >= 2;
            capsuleDraftReset.IsEnabled = allowed && currentGroup is not null && overrides.Any(row => CapsuleAuthoring.Matches(row, currentGroup));
            var history = selected is { } active ? capsuleHistory.GetValueOrDefault((active.Card, active.Character.CharacterId)) : null;
            capsuleDraftUndo.IsEnabled = allowed && history?.Undo.Count > 0;
            capsuleDraftRedo.IsEnabled = allowed && history?.Redo.Count > 0;
            capsuleBonePicker.IsEnabled = capsuleActive.IsEnabled = canEdit;
            capsuleBonePicker.SelectedItem = primary is null ? null : capsuleBonePicker.Items.OfType<CapsuleBoneChoice>()
                .FirstOrDefault(choice => string.Equals(choice.Name, primary.Shape.Bone, StringComparison.OrdinalIgnoreCase));
            capsuleActive.IsChecked = primary?.Shape.Enabled == true;
            capsuleState.Text = currentDraft is null ? "请选择演员和物理组。" :
                currentDraft.ReplaceStock ? $"胶囊 {rows.Length} 个 · 已选 {count}" :
                $"本组无原厂参考，仅编辑新增项 · 已选 {count}";
            if (capsuleStockError is not null) capsuleState.Text += "\n" + capsuleStockError;
            if (currentDraft is { ReplaceStock: false } && currentDraft.Shapes.Count != 0)
                capsuleState.Text += "\n开演时保留原厂胶囊。";
            var values = primary?.Shape is { } shape
                ? new[] { shape.Radius, shape.Length, shape.Offset.X, shape.Offset.Y, shape.Offset.Z, shape.Rotation.X, shape.Rotation.Y, shape.Rotation.Z }
                : [];
            for (var i = 0; i < capsuleFields.Count; i++)
            {
                var field = capsuleFields[i];
                field.Apply.IsEnabled = field.Box.IsEnabled = canEdit;
                if (!field.Box.IsKeyboardFocusWithin && (i >= lastCapsuleValues.Length || i >= values.Length || lastCapsuleValues[i] != values[i]))
                    field.Box.Text = i < values.Length ? Value(values[i]) : "";
            }
            lastCapsuleValues = values;
            var visible = capsuleAll.IsChecked == true ? capsuleDraftRows.Values : rows.AsEnumerable();
            physicsModelView.SetCapsules(visible.Select(row => row.View).ToArray(), primary?.View.Key, canEdit,
                selectedKeys: rows.Where(row => selectedDraftCapsules.Contains(row.Shape.Id)).Select(row => row.View.Key).ToArray(),
                rotating: capsuleRotate.IsChecked == true, placing: capsulePlace.IsChecked == true);
        }
        finally { refreshingCapsuleDraft = false; }
        return true;
    }

    private void PopulateDraftCapsuleRows(IReadOnlyList<CapsuleDraftGroup> overrides)
    {
        capsuleDraftRows.Clear();
        var bones = capsuleDraftProfile?.Bones.Select((bone, index) => (bone.Name, index))
            .ToDictionary(pair => pair.Name, pair => pair.index, StringComparer.OrdinalIgnoreCase);
        for (var groupIndex = 0; groupIndex < capsuleGroupChoices.Length; groupIndex++)
        {
            var group = capsuleGroupChoices[groupIndex];
            var shapes = DraftCapsuleGroup(group.Group, overrides).Shapes;
            for (var i = 0; i < shapes.Count; i++)
            {
                var shape = shapes[i];
                var key = new CapsuleKey((uint)groupIndex + 1, (uint)i + 1);
                var bone = shape.Bone is null ? -1 : bones?.GetValueOrDefault(shape.Bone, -1) ?? -1;
                var label = $"{shape.Bone ?? "组件"} · {(shape.Array == 0 ? "A" : "B")}{i + 1}";
                var view = new CapsuleViewItem(key, label, bone, shape.Enabled, shape.Enabled, shape.Radius, shape.Length, shape.Offset, shape.Rotation);
                capsuleDraftRows.Add(key, new(view, shape));
            }
        }
    }

    private void PickDraftCapsule(CapsuleKey key, bool additive)
    {
        if (!capsuleDraftRows.TryGetValue(key, out var row)) return;
        refreshingCapsuleDraft = true;
        try
        {
            var groupIndex = checked((int)key.Group - 1);
            if (capsuleGroupPicker.SelectedIndex != groupIndex)
            {
                selectedDraftCapsules.Clear();
                capsuleGroupPicker.SelectedIndex = groupIndex;
            }
            if (!additive) selectedDraftCapsules.Clear();
            if (!selectedDraftCapsules.Add(row.Shape.Id) && additive)
                selectedDraftCapsules.Remove(row.Shape.Id);
            primaryDraftCapsule = selectedDraftCapsules.Contains(row.Shape.Id) ? row.Shape.Id : null;
        }
        finally { refreshingCapsuleDraft = false; }
        lastCapsuleValues = [];
        RefreshPhysicsSettings();
    }

    private void RunCapsuleDraftAction(Action action)
    {
        if (!CanDraftPhysics || editingPhysics || !capsuleMode) return;
        try { action(); }
        catch (Exception error) when (error is ArgumentException or InvalidDataException or InvalidOperationException or JsonException
            or System.Runtime.InteropServices.ExternalException)
        { status.Text = error.Message; RefreshPhysicsSettings(); }
    }

    private CapsuleDraftShape[] SelectedDraftCapsules() =>
        capsuleListRows.Where(row => selectedDraftCapsules.Contains(row.Shape.Id)).Select(row => row.Shape).ToArray();

    private void ChangeDraftCapsules(Func<CapsuleDraftGroup, CapsuleDraftGroup?> operation, IEnumerable<Guid>? selection = null)
    {
        if (SelectedPhysicsActor() is not { } actor || capsuleGroupPicker.SelectedItem is not CapsuleGroupChoice selected
            || capsuleDraftProfile is not { } profile) return;
        var preset = actor.Card.PhysicsPresets.GetValueOrDefault(actor.Character.CharacterId)?.DeepClone().AsObject()
            ?? NativePhysicsPreset.Create(actor.Character, []);
        var before = CapsuleAuthoring.Read(preset).ToArray();
        var next = operation(DraftCapsuleGroup(selected.Group, before));
        var result = before.Where(row => !CapsuleAuthoring.Matches(row, selected.Group)).ToList();
        if (next is not null && (next.ReplaceStock || next.Shapes.Count != 0)) result.Add(next);
        CapsuleAuthoring.ValidateTargets(result, profile);
        CapsuleAuthoring.Write(preset, result);
        KeepPhysicsDraft(actor.Card, actor.Character, preset);
        var key = (actor.Card, actor.Character.CharacterId);
        if (!capsuleHistory.TryGetValue(key, out var history)) capsuleHistory.Add(key, history = new());
        history.Undo.Push(before);
        history.Redo.Clear();
        if (selection is not null)
        {
            selectedDraftCapsules.Clear();
            selectedDraftCapsules.UnionWith(selection);
            primaryDraftCapsule = selectedDraftCapsules.Count == 0 ? null : selectedDraftCapsules.First();
        }
        lastCapsuleValues = [];
        RefreshPhysicsSettings();
    }

    private void RollCapsuleHistory(bool undo)
    {
        if (SelectedPhysicsActor() is not { } actor || !capsuleHistory.TryGetValue((actor.Card, actor.Character.CharacterId), out var history)) return;
        var from = undo ? history.Undo : history.Redo;
        var to = undo ? history.Redo : history.Undo;
        if (!from.TryPop(out var layout)) return;
        var preset = actor.Card.PhysicsPresets.GetValueOrDefault(actor.Character.CharacterId)?.DeepClone().AsObject()
            ?? NativePhysicsPreset.Create(actor.Character, []);
        to.Push(CapsuleAuthoring.Read(preset).ToArray());
        CapsuleAuthoring.Write(preset, layout);
        KeepPhysicsDraft(actor.Card, actor.Character, preset);
        lastCapsuleValues = [];
        RefreshPhysicsSettings();
    }

    private void AddDraftCapsule()
    {
        var selected = SelectedDraftCapsules().FirstOrDefault();
        var bone = selected is not null ? selected.Bone :
            (capsuleGroupPicker.SelectedItem as CapsuleGroupChoice)?.Group.Chains.FirstOrDefault()?.Root;
        var shape = new CapsuleDraftShape(Guid.NewGuid(), selected?.SourceId, bone, selected?.Array ?? 0, 6, 16, selected?.Offset ?? default, default, true);
        ChangeDraftCapsules(group => group with { Shapes = group.Shapes.Append(shape).ToArray() }, [shape.Id]);
        capsuleMove.IsChecked = true;
    }

    private void DuplicateDraftCapsules()
    {
        var copies = SelectedDraftCapsules().Select(shape => shape with { Id = Guid.NewGuid() }).ToArray();
        if (copies.Length == 0) return;
        ChangeDraftCapsules(group => group with { Shapes = group.Shapes.Concat(copies).ToArray() }, copies.Select(shape => shape.Id));
        capsuleMove.IsChecked = true;
    }

    private void CopyDraftCapsules()
    {
        var shapes = SelectedDraftCapsules();
        if (shapes.Length == 0) return;
        var rows = new JsonArray(shapes.Select(shape => (JsonNode)CapsuleAuthoring.WriteShape(shape)).ToArray());
        Clipboard.SetText(new JsonObject { ["kind"] = "wwposer_capsules", ["shapes"] = rows }.ToJsonString());
    }

    private void PasteDraftCapsules()
    {
        if (!Clipboard.ContainsText()) throw new InvalidDataException("剪贴板中没有胶囊。");
        var clipboard = JsonNode.Parse(Clipboard.GetText()) as JsonObject;
        if (clipboard?["kind"]?.GetValue<string>() != "wwposer_capsules" || clipboard["shapes"] is not JsonArray rows || rows.Count == 0)
            throw new InvalidDataException("剪贴板中没有 WW Poser 胶囊。");
        var copies = rows.Select(row => CapsuleAuthoring.ReadShape(row!.AsObject()) with { Id = Guid.NewGuid(), SourceId = null }).ToArray();
        ChangeDraftCapsules(group => group with { Shapes = group.Shapes.Concat(copies).ToArray() }, copies.Select(shape => shape.Id));
    }

    private void SplitDraftCapsules()
    {
        var selected = SelectedDraftCapsules();
        if (selected.Length == 0) return;
        var split = selected.SelectMany(CapsuleAuthoring.Split).ToArray();
        ChangeDraftCapsules(group => group with { Shapes = group.Shapes.Where(shape => !selectedDraftCapsules.Contains(shape.Id)).Concat(split).ToArray() },
            split.Select(shape => shape.Id));
    }

    private void MergeDraftCapsules()
    {
        var selected = SelectedDraftCapsules();
        if (selected.Length < 2) return;
        var merged = CapsuleAuthoring.Merge(selected);
        ChangeDraftCapsules(group => group with { Shapes = group.Shapes.Where(shape => !selectedDraftCapsules.Contains(shape.Id)).Append(merged).ToArray() },
            [merged.Id]);
    }

    private void DeleteDraftCapsules()
    {
        if (selectedDraftCapsules.Count == 0) return;
        ChangeDraftCapsules(group => group with { Shapes = group.Shapes.Where(shape => !selectedDraftCapsules.Contains(shape.Id)).ToArray() }, []);
    }

    private void ResetDraftCapsuleGroup() => ChangeDraftCapsules(_ => null, []);

    private void SelectAllDraftCapsules()
    {
        selectedDraftCapsules.UnionWith(capsuleListRows.Select(row => row.Shape.Id));
        RefreshPhysicsSettings();
    }

    private void EditSelectedDraftCapsules(Func<CapsuleDraftShape, CapsuleDraftShape> edit)
    {
        if (selectedDraftCapsules.Count == 0) return;
        ChangeDraftCapsules(group => group with { Shapes = group.Shapes.Select(shape => selectedDraftCapsules.Contains(shape.Id) ? edit(shape) : shape).ToArray() });
    }

    private void EditDraftCapsule(CapsuleKey key, int field, float value)
    {
        if (capsuleDraftRows.TryGetValue(key, out var row) && selectedDraftCapsules.Contains(row.Shape.Id))
        {
            float ReadField(CapsuleDraftShape shape) => field < 5 ? shape.Offset[field - 2] : shape.Rotation[field - 5];
            var delta = value - ReadField(row.Shape);
            EditSelectedDraftCapsules(shape => WithCapsuleField(shape, field, ReadField(shape) + delta));
        }
    }

    private void PlaceDraftCapsule(CapsuleKey key, Vector3 offset)
    {
        if (!capsuleDraftRows.TryGetValue(key, out var row) || !selectedDraftCapsules.Contains(row.Shape.Id)) return;
        var displacement = offset - row.Shape.Offset;
        EditSelectedDraftCapsules(shape => shape with { Offset = shape.Offset + displacement });
        capsuleMove.IsChecked = true;
    }

    private static CapsuleDraftShape WithCapsuleField(CapsuleDraftShape shape, int field, float value) => field switch
    {
        0 => shape with { Radius = value },
        1 => shape with { Length = value },
        2 => shape with { Offset = shape.Offset with { X = value } },
        3 => shape with { Offset = shape.Offset with { Y = value } },
        4 => shape with { Offset = shape.Offset with { Z = value } },
        5 => shape with { Rotation = shape.Rotation with { X = value } },
        6 => shape with { Rotation = shape.Rotation with { Y = value } },
        7 => shape with { Rotation = shape.Rotation with { Z = value } },
        _ => throw new ArgumentOutOfRangeException(nameof(field))
    };

    private void OnCapsuleEditorKeyDown(object sender, KeyEventArgs e)
    {
        if (!capsuleMode || !CanDraftPhysics || physicsModelView.IsCapsuleDragging || e.OriginalSource is TextBox || capsuleBonePicker.IsKeyboardFocusWithin
            || capsuleGroupPicker.IsKeyboardFocusWithin || !physicsModelView.IsKeyboardFocusWithin && !capsuleDraftList.IsKeyboardFocusWithin) return;
        Action? action = (Keyboard.Modifiers, e.Key) switch
        {
            (ModifierKeys.Control, Key.A) => SelectAllDraftCapsules,
            (ModifierKeys.Control, Key.C) => CopyDraftCapsules,
            (ModifierKeys.Control, Key.V) => PasteDraftCapsules,
            (ModifierKeys.Shift, Key.D) => DuplicateDraftCapsules,
            (ModifierKeys.Control, Key.Z) => () => RollCapsuleHistory(undo: true),
            (ModifierKeys.Control, Key.Y) or (ModifierKeys.Control | ModifierKeys.Shift, Key.Z) => () => RollCapsuleHistory(undo: false),
            (ModifierKeys.None, Key.Delete or Key.X) => DeleteDraftCapsules,
            (ModifierKeys.None, Key.G) => () => capsuleMove.IsChecked = true,
            (ModifierKeys.None, Key.R) => () => capsuleRotate.IsChecked = true,
            _ => null
        };
        if (action is null) return;
        e.Handled = true;
        RunCapsuleDraftAction(action);
    }
}
