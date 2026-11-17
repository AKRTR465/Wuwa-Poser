using System.Globalization;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WWPoser.Services;

namespace WWPoser;

public sealed partial class MainWindow
{
    private static readonly string[] CapsuleFields = ["native_capsule_radius", "native_capsule_length",
        "native_capsule_offset_x", "native_capsule_offset_y", "native_capsule_offset_z"];
    private readonly StackPanel capsuleInspector = new() { Visibility = Visibility.Collapsed, Margin = new(0, 0, 0, 12) };
    private readonly ComboBox capsulePicker = new() { SelectedValuePath = nameof(CapsuleViewItem.Key), MaxDropDownHeight = 330 };
    private readonly CheckBox capsuleAll = new() { Content = "显示全身胶囊", IsChecked = true };
    private readonly CheckBox capsuleActive = new() { Content = "启用所选胶囊" };
    private readonly List<(TextBox Box, Button Apply)> capsuleFields = [];
    private TextBlock capsuleState = null!;
    private Button capsuleReset = null!;
    private IReadOnlyList<CapsuleViewItem> capsuleItems = [];
    private CapsuleKey? activeCapsule;
    private string? capsuleSession;
    private uint capsuleActor;
    private bool capsuleMode, refreshingCapsulePicker;
    private float[] lastCapsuleValues = [];

    private void AddCapsuleEditorToggle(Panel views)
    {
        var toggle = new CheckBox { Content = "胶囊", Margin = new(6, 0, 0, 4), VerticalAlignment = VerticalAlignment.Center };
        toggle.Click += (_, _) =>
        {
            capsuleMode = toggle.IsChecked == true;
            capsuleInspector.Visibility = capsuleMode ? Visibility.Visible : Visibility.Collapsed;
            if (capsuleMode) SetPhysicsView(true);
            else if (physicsModel is null) SetPhysicsView(modelViewPreferred);
            else RefreshPhysicsSettings();
        };
        physicsModelView.CapsulePickedWithModifiers += (key, additive) =>
        {
            if (CanDraftPhysics) PickDraftCapsule(key, additive);
            else capsulePicker.SelectedValue = key;
        };
        physicsModelView.CapsuleDragged += async (key, field, value) =>
        {
            if (CanDraftPhysics) RunCapsuleDraftAction(() => EditDraftCapsule(key, field, (float)value));
            else await SendCapsuleEditAsync(key, CapsuleFields[field], value);
        };
        physicsModelView.CapsuleRotated += (key, axis, value) =>
            RunCapsuleDraftAction(() => EditDraftCapsule(key, axis + 5, (float)value));
        physicsModelView.CapsulePlaced += (key, offset) =>
            RunCapsuleDraftAction(() => PlaceDraftCapsule(key, offset));
        views.Children.Add(toggle);
    }

    private FrameworkElement BuildCapsuleInspector()
    {
        capsuleInspector.Children.Add(Label("原生碰撞胶囊"));
        capsuleState = Hint("");
        capsuleInspector.Children.Add(capsuleState);
        capsuleInspector.Children.Add(capsulePicker);
        capsuleInspector.Children.Add(BuildCapsuleDraftTools());
        capsuleInspector.Children.Add(capsuleAll);
        capsuleInspector.Children.Add(capsuleActive);
        capsuleAll.Click += (_, _) => RefreshPhysicsSettings();
        capsulePicker.SelectionChanged += (_, _) =>
        {
            if (refreshingCapsulePicker) return;
            activeCapsule = capsulePicker.SelectedValue is CapsuleKey key ? key : null;
            lastCapsuleValues = [];
            RefreshPhysicsSettings();
        };
        capsuleActive.Click += async (_, _) =>
        {
            if (CanDraftPhysics)
            {
                RunCapsuleDraftAction(() => EditSelectedDraftCapsules(shape => shape with { Enabled = capsuleActive.IsChecked == true }));
                return;
            }
            if (activeCapsule is { } key) await SendCapsuleEditAsync(key, "native_capsule_active", capsuleActive.IsChecked == true ? 1 : 0);
        };

        var labels = new[] { "半径", "长度", "X 偏移", "Y 偏移", "Z 偏移", "俯仰", "偏航", "滚转" };
        for (var i = 0; i < labels.Length; i++)
        {
            var index = i;
            var entry = new DockPanel { Margin = new(0, 2, 0, 2) };
            var apply = new Button { Content = "应用", Padding = new(5, 3, 5, 3), Margin = new(4, 0, 0, 0) };
            var number = new TextBox { MinWidth = 46, VerticalContentAlignment = VerticalAlignment.Center };
            var name = new TextBlock { Text = labels[i], MinWidth = 46, VerticalAlignment = VerticalAlignment.Center };
            DockPanel.SetDock(name, Dock.Left); entry.Children.Add(name);
            DockPanel.SetDock(apply, Dock.Right); entry.Children.Add(apply);
            entry.Children.Add(number);
            apply.Click += async (_, _) => await ApplyCapsuleFieldAsync(index);
            number.KeyDown += async (_, e) =>
            {
                if (e.Key != Key.Enter) return;
                e.Handled = true;
                await ApplyCapsuleFieldAsync(index);
            };
            capsuleFields.Add((number, apply));
            capsuleInspector.Children.Add(entry);
            if (index >= 5) capsuleRotationFields.Add(entry);
        }
        capsuleInspector.Children.Add(Hint("距离用厘米，长度为两端球心间距。旋转用度。"));
        capsuleReset = new Button { Content = "还原所选胶囊", Margin = new(0, 4, 0, 0) };
        capsuleReset.Click += async (_, _) =>
        {
            if (activeCapsule is { } key) await SendCapsuleEditAsync(key, "native_capsule_reset", 0);
        };
        capsuleInspector.Children.Add(capsuleReset);
        return capsuleInspector;
    }

    private void RefreshCapsuleEditor(LivePhysicsTarget? actor, JsonObject? group, bool editable)
    {
        if (!capsuleMode)
        {
            physicsModelView.SetCapsules([], null, false);
            return;
        }
        if (RefreshDraftCapsuleEditor(editable)) return;
        capsuleDraftTools.Visibility = Visibility.Collapsed;
        capsulePicker.Visibility = Visibility.Visible;
        capsuleReset.Visibility = Visibility.Visible;
        foreach (var field in capsuleRotationFields) field.Visibility = Visibility.Collapsed;
        if (capsuleSession != session || capsuleActor != (actor?.Slot ?? 0))
        {
            capsuleSession = session;
            capsuleActor = actor?.Slot ?? 0;
            activeCapsule = null;
            lastCapsuleValues = [];
        }
        var groupId = group?["group_id"]?.GetValue<uint>() ?? 0;
        var items = (actor?.Groups ?? []).SelectMany(CapsulesInGroup).ToArray();
        var listChanged = !capsuleItems.Select(item => (item.Key, item.Label)).SequenceEqual(items.Select(item => (item.Key, item.Label)));
        var current = items.FirstOrDefault(item => item.Key == activeCapsule) ?? items.FirstOrDefault(item => item.Key.Group == groupId) ?? items.FirstOrDefault();
        if (current?.Key != activeCapsule) lastCapsuleValues = [];
        activeCapsule = current?.Key;
        refreshingCapsulePicker = true;
        try
        {
            if (listChanged) capsulePicker.ItemsSource = items;
            if (!Equals(capsulePicker.SelectedValue, activeCapsule)) capsulePicker.SelectedValue = activeCapsule;
        }
        finally { refreshingCapsulePicker = false; }
        capsuleItems = items;
        capsulePicker.IsEnabled = items.Length != 0;
        capsuleActive.IsChecked = current?.Enabled == true;
        capsuleActive.IsEnabled = capsuleReset.IsEnabled = current is not null && editable;
        capsuleState.Text = actor is null ? "开演后读取原生胶囊。" : items.Length == 0 ? "等待原生碰撞求值。" :
            current is { Enabled: true, Active: false } ? $"共 {items.Length} 个 · 所选胶囊受本组开关控制" : $"共 {items.Length} 个原生胶囊";
        if (actor?.CapsuleLayoutError is { } layoutError) capsuleState.Text = "胶囊布局未应用：" + layoutError;
        else if (actor?.CapsuleLayoutPending == true) capsuleState.Text += "\n正在匹配预编辑布局。";
        float[] values = current is null ? [] : [current.Radius, current.Length, current.Offset.X, current.Offset.Y, current.Offset.Z];
        for (var i = 0; i < capsuleFields.Count; i++)
        {
            var field = capsuleFields[i];
            field.Box.IsEnabled = field.Apply.IsEnabled = current is not null && editable;
            if (!field.Box.IsKeyboardFocusWithin && (i >= lastCapsuleValues.Length || i >= values.Length || lastCapsuleValues[i] != values[i]))
                field.Box.Text = i < values.Length ? values[i].ToString("0.###", CultureInfo.InvariantCulture) : "";
        }
        lastCapsuleValues = values;
        var visible = capsuleAll.IsChecked == true ? items : items.Where(item => item.Key.Group == (activeCapsule?.Group ?? groupId)).ToArray();
        physicsModelView.SetCapsules(visible, activeCapsule, editable && current is not null);
    }

    internal static IEnumerable<CapsuleViewItem> CapsulesInGroup(JsonObject group)
    {
        var groupId = group["group_id"]?.GetValue<uint>() ?? 0;
        if (groupId == 0 || group["capsule_shapes"] is not JsonArray shapes) yield break;
        var curve = group["curve_name"]?.GetValue<string>() ?? "物理组";
        foreach (var shape in shapes.OfType<JsonObject>())
        {
            if (!Contract.True(shape, "editable") || shape["values"] is not JsonArray { Count: 5 } values ||
                shape["rotation"] is not JsonArray { Count: 3 } rotation) continue;
            var driver = shape["driver"]?.GetValue<string>() ?? "";
            var label = driver.ToLowerInvariant() switch
            {
                "bip001lthigh" => "左大腿", "bip001rthigh" => "右大腿", "bip001pelvis" => "骨盆",
                "bip001lforearm" => "左前臂", "bip001rforearm" => "右前臂", "" => "组件空间", _ => driver
            };
            var id = shape["id"]!.GetValue<uint>();
            var suffix = shape["layout_index"]?.GetValue<uint>() is { } layoutIndex
                ? $"预编辑 {layoutIndex}" : (id > 65536 ? "B" : "A") + ((id - 1) % 65536 + 1).ToString(CultureInfo.InvariantCulture);
            yield return new(new(groupId, id), $"{curve.Replace("Kawaii_alpha_", "", StringComparison.Ordinal)} · {label} · {suffix}",
                shape["bone"]!.GetValue<int>(), Contract.True(shape, "enabled"), Contract.True(shape, "active"), values[0]!.GetValue<float>(), values[1]!.GetValue<float>(),
                new Vector3(values[2]!.GetValue<float>(), values[3]!.GetValue<float>(), values[4]!.GetValue<float>()),
                new Vector3(rotation[0]!.GetValue<float>(), rotation[1]!.GetValue<float>(), rotation[2]!.GetValue<float>()));
        }
    }

    private async Task ApplyCapsuleFieldAsync(int field)
    {
        var input = capsuleFields[field].Box.Text;
        if (!double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) &&
            !double.TryParse(input, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
        {
            status.Text = field < 5 ? "请输入以厘米为单位的数值。" : "请输入以度为单位的数值。";
            return;
        }
        if (CanDraftPhysics)
            RunCapsuleDraftAction(() => EditSelectedDraftCapsules(shape => WithCapsuleField(shape, field, (float)value)));
        else if (field < CapsuleFields.Length && activeCapsule is { } key)
            await SendCapsuleEditAsync(key, CapsuleFields[field], value);
    }

    private async Task SendCapsuleEditAsync(CapsuleKey key, string command, double value)
    {
        if (editingPhysics || busy || player.IsBusy || !sessionControllable || session is not { } directory ||
            SelectedLivePhysicsActor() is not { } actor || actor.Slot != capsuleActor || !capsuleItems.Any(item => item.Key == key)) return;
        editingPhysics = true;
        try
        {
            await PlaybackControl.SendAsync(directory, command, value, Token, actor.Slot, key.Group, key.Id);
            lastCapsuleValues = [];
        }
        catch (Exception error) when (error is IOException or TimeoutException or InvalidOperationException or ArgumentException)
        { status.Text = error.Message; }
        finally { editingPhysics = false; RefreshPhysicsSettings(); }
    }
}
