using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using WWPoser.Services;

namespace WWPoser;

// Blender-style 3D stage. World axes are the runtime stage axes (ManagedBridge stage):
// origin is the controlled character when playback starts, X its right, Y ahead,
// Z up from its floor, all in centimetres; yaw is clockwise from "ahead" seen from above.
public sealed partial class MainWindow
{
    private const double CrowdedCm = 60;
    private readonly StageViewport stageMap = new() { Height = 480, MinWidth = 380 };
    private readonly TextBox stageX = new() { Tag = "厘米" }, stageY = new() { Tag = "厘米" }, stageZ = new() { Tag = "厘米" }, stageYaw = new() { Tag = "度" };
    private readonly TextBlock stageTitle = Text("", 15, "Ink", FontWeights.SemiBold), stageState = Text("", 12, "Muted"), stageWarning = Text("", 12, "Danger");
    private FrameworkElement stageEditor = null!;
    private Button previewButton = null!, endPreviewButton = null!;
    private readonly TextBlock previewState = Text("", 12, "Muted");
    private bool loadingStageFields;

    private sealed record MapActor(ActorCard Card, StagePosition Position, bool Pinned);

    private IReadOnlyList<MapActor> MapActors()
    {
        var playing = actors.Where(card => card.Enabled.IsChecked == true).ToArray();
        var gap = double.TryParse(spacing.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && double.IsFinite(value) && value >= 0 ? value : 180;
        return playing.Select((card, index) => new MapActor(card, card.Stage ?? OwnedMmdPlaybackService.GridPosition(index, playing.Length, gap), card.Stage is not null)).ToArray();
    }

    private static bool Crowded(MapActor actor, IReadOnlyList<MapActor> placed)
    {
        static double Distance(StagePosition a, double x, double y) => Math.Sqrt((a.X - x) * (a.X - x) + (a.Y - y) * (a.Y - y));
        return Distance(actor.Position, 0, 0) < CrowdedCm || placed.Any(other => !ReferenceEquals(other, actor) && Distance(actor.Position, other.Position.X, other.Position.Y) < CrowdedCm);
    }
    private static int CrowdedCount(IReadOnlyList<MapActor> placed) => placed.Count(actor => Crowded(actor, placed));

    private FrameworkElement BuildStageMapCard()
    {
        stageMap.Actors = MapActors; stageMap.Selected = () => actorList.SelectedItem as ActorCard;
        stageMap.Picked += card => actorList.SelectedItem = card;
        stageMap.Moved += (card, position) => { card.Stage = position; UpdateGuide(); };
        spacing.TextChanged += (_, _) => UpdateGuide();
        actorList.SelectionChanged += (_, _) => RefreshStage();

        var layout = new Grid(); layout.ColumnDefinitions.Add(new ColumnDefinition()); layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250) });
        var viewport = new Border { CornerRadius = new(14), ClipToBounds = true, BorderThickness = new(1), Child = stageMap }; viewport.SetResourceReference(Border.BorderBrushProperty, "Line");
        layout.Children.Add(viewport);

        var side = new StackPanel { Margin = new(20, 0, 0, 0) }; Grid.SetColumn(side, 1);
        side.Children.Add(stageTitle); stageState.Margin = new(0, 2, 0, 12); side.Children.Add(stageState);
        var fields = new StackPanel();
        foreach (var (label, box, axis) in new[] { ("X · 向右（负数向左）", stageX, "AxisX"), ("Y · 向前（负数向后）", stageY, "AxisY"), ("Z · 离地高度", stageZ, "AxisZ"), ("朝向（0 和你同向，180 面向你）", stageYaw, "InkSoft") })
        {
            box.Width = double.NaN; var field = new StackPanel { Margin = new(0, 0, 0, 10) };
            var caption = Label(label); caption.SetResourceReference(TextBlock.ForegroundProperty, axis); field.Children.Add(caption); field.Children.Add(box); fields.Children.Add(field);
            box.LostKeyboardFocus += (_, _) => ApplyStageFields(); box.KeyDown += (_, args) => { if (args.Key == Key.Enter) ApplyStageFields(); };
        }
        var auto = new Button { Content = "这位演员恢复自动站位", HorizontalAlignment = HorizontalAlignment.Left }; auto.SetResourceReference(StyleProperty, "LinkButton");
        auto.Click += (_, _) => { if (actorList.SelectedItem is ActorCard card) { card.Stage = null; UpdateGuide(); } };
        fields.Children.Add(auto); stageEditor = fields; side.Children.Add(fields);
        stageWarning.Margin = new(0, 10, 0, 0); side.Children.Add(stageWarning);
        layout.Children.Add(side);

        var body = new StackPanel(); body.Children.Add(layout);
        var preview = new DockPanel { Margin = new(0, 14, 0, 0) };
        previewButton = Action("在游戏里预览站位", PreviewAsync, "", "PrimaryButton"); DockPanel.SetDock(previewButton, Dock.Left); preview.Children.Add(previewButton);
        endPreviewButton = Action("结束预览", EndPreviewAsync, "", "SoftButton"); DockPanel.SetDock(endPreviewButton, Dock.Left); preview.Children.Add(endPreviewButton);
        previewState.TextWrapping = TextWrapping.Wrap; previewState.VerticalAlignment = VerticalAlignment.Center; previewState.Margin = new(6, 0, 0, 0); preview.Children.Add(previewState);
        body.Children.Add(preview); UpdatePreview();
        var views = new WrapPanel { Margin = new(0, 14, 0, 0) };
        views.Children.Add(Label("视角")); ((FrameworkElement)views.Children[0]).Margin = new(0, 7, 12, 0);
        foreach (var (label, view) in new[] { ("透视", StageView.Perspective), ("顶视 7", StageView.Top), ("正视 1", StageView.Front), ("侧视 3", StageView.Side), ("框选全部 Home", StageView.Frame) })
        {
            var chip = new Button { Content = label }; chip.SetResourceReference(StyleProperty, "ChipButton"); chip.Click += (_, _) => stageMap.SetView(view); views.Children.Add(chip);
        }
        body.Children.Add(views);
        var shapes = new WrapPanel { Margin = new(0, 10, 0, 0) };
        shapes.Children.Add(Label("一键队形")); ((FrameworkElement)shapes.Children[0]).Margin = new(0, 7, 12, 0);
        foreach (var (label, shape) in new (string, Func<int, double, StagePosition[]>?)[] { ("自动排列", null), ("一字排开", Line), ("半圆", Arc), ("V 字", Vee) })
        {
            var chip = new Button { Content = label }; chip.SetResourceReference(StyleProperty, "ChipButton");
            chip.Click += (_, _) => Formation(shape); shapes.Children.Add(chip);
        }
        body.Children.Add(shapes);
        var gap = Field("自动排列时演员之间的距离", spacing, 160); gap.Margin = new(0, 14, 0, 0); body.Children.Add(gap);
        body.Children.Add(Hint("点一下演员选中 TA：拖红 / 绿 / 蓝箭头只沿 X / Y / Z 移动，拖人在地面上移动，拖圆环转朝向。右键或中键拖动旋转视角，Shift + 右键拖动平移，滚轮缩放。"));
        body.Children.Add(Hint("和 Blender 一样：G 抓取（再按 X / Y / Z 锁定轴），R 旋转，左键确认，Esc 或右键取消；1 / 3 / 7 切正视 / 侧视 / 顶视，5 切换正交，Home 框选全部。默认对齐 10 厘米和 15°，按住 Shift 自由移动。"));
        body.Children.Add(Hint("中心的「你」是开始播放那一刻你操控的角色。这里定的是起始站位，在下次开始播放时生效；舞蹈本身带的走位仍会让演员移动，开了「原地跳」的除外。"));
        return Card("站位舞台", "像在 Blender 里一样：点中一个人，拖到你想要的位置。", "\uE707", body);
    }

    private void Formation(Func<int, double, StagePosition[]>? shape)
    {
        var playing = actors.Where(card => card.Enabled.IsChecked == true).ToArray();
        var gap = double.TryParse(spacing.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && double.IsFinite(value) && value > 0 ? value : 180;
        var positions = shape?.Invoke(playing.Length, gap);
        for (var i = 0; i < playing.Length; i++) playing[i].Stage = positions?[i];
        UpdateGuide(); stageMap.SetView(StageView.Frame); status.Text = shape is null ? "已恢复自动排列。" : "队形已摆好，可以再拖动单个演员微调。";
    }

    // Formations stand in front of the controlled character and face it.
    private static StagePosition Facing(double x, double y) => new(Math.Round(x), Math.Round(y), 0, Math.Round(Math.Atan2(-x, -y) * 180 / Math.PI));
    private static StagePosition[] Line(int count, double gap) => Enumerable.Range(0, count).Select(i => Facing((i - (count - 1) * .5) * gap, 300)).ToArray();
    private static StagePosition[] Arc(int count, double gap)
    {
        var radius = Math.Max(250, count * gap / Math.PI); var spread = Math.Min(150, Math.Max(60, count * 25)) * Math.PI / 180;
        return Enumerable.Range(0, count).Select(i => { var angle = count == 1 ? 0 : (i / (count - 1d) - .5) * spread; return Facing(radius * Math.Sin(angle), radius * Math.Cos(angle)); }).ToArray();
    }
    private static StagePosition[] Vee(int count, double gap) => Enumerable.Range(0, count).Select(i =>
    {
        var rank = (i + 1) / 2; var side = i == 0 ? 0 : i % 2 == 1 ? -1 : 1;
        return Facing(side * rank * gap * .7, 220 + rank * gap * .7);
    }).ToArray();

    private void RefreshStage()
    {
        stageMap.InvalidateVisual();
        var placed = MapActors(); var card = actorList.SelectedItem as ActorCard; var current = placed.FirstOrDefault(actor => ReferenceEquals(actor.Card, card));
        var crowded = CrowdedCount(placed);
        stageWarning.Text = crowded > 0 ? $"有 {crowded} 位演员和别人（或你）挨得太近，开始后可能会重叠。" : "";
        if (stageEditor is null) return;
        stageEditor.Visibility = current is null ? Visibility.Collapsed : Visibility.Visible;
        stageTitle.Text = current is null ? (card is null ? "在舞台上点一位演员" : $"演员 {card.Number} 没有参与表演") : $"演员 {card!.Number} · {card.CharacterName}";
        stageState.Text = current is null ? "选中后可以拖动，也可以在这里输入精确位置。" : current.Pinned ? "站位：你指定的位置" : "站位：自动排列（拖动或输入后变成你指定的位置）";
        if (current is null) return;
        loadingStageFields = true;
        stageX.Text = F(current.Position.X); stageY.Text = F(current.Position.Y); stageZ.Text = F(current.Position.Z); stageYaw.Text = F(current.Position.Yaw);
        loadingStageFields = false;
    }

    private void UpdatePreview()
    {
        if (previewButton is null) return;
        var on = previewSession is not null;
        previewButton.Content = on ? "按现在的站位重新预览" : "在游戏里预览站位";
        endPreviewButton.Visibility = on ? Visibility.Visible : Visibility.Collapsed;
        previewState.Text = on ? "演员正站在游戏里一动不动，方便你对照。改了站位后点「重新预览」，看完点「结束预览」。"
            : "不用准备动作：只请角色站到设定位置上，不跳舞。点完请切回鸣潮窗口。预览时每位演员都会另外创建；正式播放时，队伍里有的角色会直接用原角色上台。";
        UpdateSessionView();
    }

    private void ApplyStageFields()
    {
        if (loadingStageFields || actorList.SelectedItem is not ActorCard card || card.Enabled.IsChecked != true) return;
        try { card.Stage = new(Number(stageX, -5000, 5000), Number(stageY, -5000, 5000), Number(stageZ, -1000, 1000), Number(stageYaw, -180, 180)); status.Text = $"演员 {card.Number} 的站位已更新。"; }
        catch (InvalidDataException error) { status.Text = error.Message; }
        UpdateGuide();
    }

    private enum StageView { Perspective, Top, Front, Side, Frame }

    private sealed class StageViewport : FrameworkElement
    {
        private const double FieldOfView = 45, NearPlane = 5, BodyHeight = 140, HeadHeight = 160, GizmoPixels = 78;
        private enum Handle { None, Body, X, Y, Z, Ring }
        private sealed record Camera(Point3D Eye, Vector3D Forward, Vector3D Right, Vector3D Up, double Focal, double Scale, bool Ortho, Point Centre);

        public Func<IReadOnlyList<MapActor>> Actors { get; set; } = () => [];
        public Func<ActorCard?> Selected { get; set; } = () => null;
        public event Action<ActorCard>? Picked;
        public event Action<ActorCard, StagePosition>? Moved;

        private double azimuth = -28, elevation = 30, distance = 900;
        private Point3D target = new(120, 60, 60);
        private bool ortho, framed;
        private Handle hover, drag;
        private ActorCard? dragCard;
        private StagePosition dragStart = new(0, 0);
        private Point3D dragAnchor;
        private double dragAxisStart, dragAngleStart;
        private bool grabbing, orbiting, panning;
        private Point lastMouse;

        public StageViewport() { Focusable = true; ClipToBounds = true; FocusVisualStyle = null; }

        public void SetView(StageView view)
        {
            switch (view)
            {
                case StageView.Perspective: ortho = false; elevation = 30; azimuth = -28; break;
                case StageView.Top: ortho = true; elevation = 89.9; azimuth = 0; break;
                case StageView.Front: ortho = true; elevation = 0; azimuth = 0; break;
                case StageView.Side: ortho = true; elevation = 0; azimuth = -90; break;
            }
            Frame(); InvalidateVisual();
        }

        private void Frame()
        {
            var points = Actors().Select(actor => new Point3D(actor.Position.X, actor.Position.Y, actor.Position.Z)).Append(new Point3D(0, 0, 0)).ToArray();
            double minX = points.Min(p => p.X), maxX = points.Max(p => p.X), minY = points.Min(p => p.Y), maxY = points.Max(p => p.Y), maxZ = points.Max(p => p.Z) + HeadHeight;
            target = new((minX + maxX) / 2, (minY + maxY) / 2, maxZ / 2);
            var radius = Math.Max(160, Math.Sqrt(Math.Pow(maxX - minX, 2) + Math.Pow(maxY - minY, 2) + maxZ * maxZ) / 2 + 60);
            distance = Math.Clamp(radius / Math.Sin(FieldOfView / 2 * Math.PI / 180) * 1.05, 250, 8000); framed = true;
        }

        private Camera View()
        {
            double a = azimuth * Math.PI / 180, e = elevation * Math.PI / 180;
            var forward = new Vector3D(Math.Sin(a) * Math.Cos(e), Math.Cos(a) * Math.Cos(e), -Math.Sin(e)); forward.Normalize();
            var right = new Vector3D(forward.Y, -forward.X, 0); right.Normalize(); var up = Vector3D.CrossProduct(right, forward);
            var focal = ActualHeight / 2 / Math.Tan(FieldOfView / 2 * Math.PI / 180);
            return new(target - forward * distance, forward, right, up, focal, focal / distance, ortho, new(ActualWidth / 2, ActualHeight / 2));
        }

        private static (Point Screen, double Depth)? Project(Camera c, Point3D p)
        {
            var v = p - c.Eye; var depth = Vector3D.DotProduct(v, c.Forward);
            if (!c.Ortho && depth < NearPlane) return null;
            double x = Vector3D.DotProduct(v, c.Right), y = Vector3D.DotProduct(v, c.Up);
            var k = c.Ortho ? c.Scale : c.Focal / depth;
            return (new(c.Centre.X + x * k, c.Centre.Y - y * k), depth);
        }

        private static (Point3D Origin, Vector3D Direction) Ray(Camera c, Point screen)
        {
            double dx = screen.X - c.Centre.X, dy = screen.Y - c.Centre.Y;
            if (c.Ortho) return (c.Eye + c.Right * (dx / c.Scale) - c.Up * (dy / c.Scale), c.Forward);
            var direction = c.Forward + c.Right * (dx / c.Focal) - c.Up * (dy / c.Focal); direction.Normalize(); return (c.Eye, direction);
        }

        private static Point3D? OnPlane(Camera c, Point screen, double height)
        {
            var (origin, direction) = Ray(c, screen);
            if (Math.Abs(direction.Z) < 1e-6) return null;
            var t = (height - origin.Z) / direction.Z; return t <= 0 ? null : origin + direction * t;
        }

        private static double AlongAxis(Camera c, Point screen, Point3D anchor, Vector3D axis)
        {
            // Closest point between the mouse ray and the axis line through the anchor.
            var (origin, direction) = Ray(c, screen); var w = anchor - origin;
            double b = Vector3D.DotProduct(axis, direction), d = Vector3D.DotProduct(axis, w), e = Vector3D.DotProduct(direction, w), denominator = 1 - b * b;
            return Math.Abs(denominator) < 1e-6 ? 0 : (b * e - d) / denominator;
        }

        private static double GizmoLength(Camera c, Point3D at)
        {
            var depth = Vector3D.DotProduct(at - c.Eye, c.Forward);
            return c.Ortho ? GizmoPixels / c.Scale : GizmoPixels * Math.Max(depth, NearPlane) / c.Focal;
        }

        // Keep yaw in (-180, 180] so facing the player reads 180 rather than -180.
        private static double Wrap(double yaw) { var value = ((yaw + 180) % 360 + 360) % 360 - 180; return value == -180 ? 180 : value; }

        private Brush B(string key) => TryFindResource(key) as Brush ?? Brushes.Gray;
        private static Vector3D AxisVector(Handle handle) => handle switch { Handle.X => new(1, 0, 0), Handle.Y => new(0, 1, 0), _ => new(0, 0, 1) };

        // ── Drawing ────────────────────────────────────────────────────

        protected override void OnRender(DrawingContext draw)
        {
            if (ActualWidth < 10 || ActualHeight < 10) return;
            var placed = Actors();
            if (!framed && placed.Count > 0) Frame();
            var c = View();
            draw.DrawRectangle(new LinearGradientBrush(Color.FromRgb(0xFB, 0xF8, 0xF5), Color.FromRgb(0xEF, 0xE7, 0xDE), 90), null, new Rect(0, 0, ActualWidth, ActualHeight));

            var extent = Math.Max(500, Math.Ceiling((placed.Select(a => Math.Max(Math.Abs(a.Position.X), Math.Abs(a.Position.Y))).DefaultIfEmpty(0).Max() + 300) / 100) * 100);
            Pen fine = new(B("Line"), 1), meter = new(B("LineStrong"), 1);
            for (var v = -extent; v <= extent; v += 50)
            {
                if (Math.Abs(v) < .5) continue;
                var pen = Math.Abs(v % 100) < 1 ? meter : fine;
                Line3(draw, c, pen, new(v, -extent, 0), new(v, extent, 0)); Line3(draw, c, pen, new(-extent, v, 0), new(extent, v, 0));
            }
            Line3(draw, c, new Pen(B("AxisX"), 1.6), new(-extent, 0, 0), new(extent, 0, 0));
            Line3(draw, c, new Pen(B("AxisY"), 1.6), new(0, -extent, 0), new(0, extent, 0));

            var selected = Selected();
            var figures = placed.Select(actor => (Actor: (MapActor?)actor, Depth: Project(c, new(actor.Position.X, actor.Position.Y, actor.Position.Z))?.Depth ?? double.MinValue))
                .Append((Actor: null, Depth: Project(c, new(0, 0, 0))?.Depth ?? double.MinValue)).OrderByDescending(item => item.Depth);
            foreach (var (actor, _) in figures)
            {
                if (actor is null) { Figure(draw, c, new(0, 0, 0), 0, B("Ink"), B("Card"), "你", false, true, false); continue; }
                var p = actor.Position; var isSelected = ReferenceEquals(actor.Card, selected);
                var tone = isSelected ? B("Accent") : actor.Pinned ? B("InkSoft") : B("Faint");
                Figure(draw, c, new(p.X, p.Y, p.Z), p.Yaw, tone, isSelected ? B("Accent") : B("Card"), actor.Card.Number, Crowded(actor, placed), actor.Pinned || isSelected, isSelected);
            }
            var chosen = placed.FirstOrDefault(actor => ReferenceEquals(actor.Card, selected));
            if (chosen is not null) Gizmo(draw, c, chosen);
            AxisWidget(draw, c);
            Label(draw, (ortho ? "正交" : "透视") + (grabbing ? (drag is Handle.X or Handle.Y or Handle.Z ? $" · 抓取（锁定 {drag}）" : drag == Handle.Ring ? " · 旋转" : " · 抓取") : ""), new(14, 12), B("Muted"), 11, false);
            Label(draw, "每格 50 厘米 · 粗线 1 米", new(14, ActualHeight - 22), B("Muted"), 11, false);
            if (placed.Count == 0) Label(draw, "还没有参与表演的演员", new(ActualWidth / 2, ActualHeight / 2 + 50), B("Muted"), 12, true);
        }

        private static void Line3(DrawingContext draw, Camera c, Pen pen, Point3D a, Point3D b)
        {
            if (!c.Ortho)
            {
                // Clip against the near plane so lines behind the eye do not wrap around.
                double da = Vector3D.DotProduct(a - c.Eye, c.Forward), db = Vector3D.DotProduct(b - c.Eye, c.Forward);
                if (da < NearPlane && db < NearPlane) return;
                if (da < NearPlane) a += (b - a) * ((NearPlane - da) / (db - da));
                else if (db < NearPlane) b += (a - b) * ((NearPlane - db) / (da - db));
            }
            if (Project(c, a) is { } pa && Project(c, b) is { } pb) draw.DrawLine(pen, pa.Screen, pb.Screen);
        }

        private static void Polygon3(DrawingContext draw, Camera c, Brush? fill, Pen? pen, IEnumerable<Point3D> points)
        {
            var screen = points.Select(p => Project(c, p)).ToArray(); if (screen.Any(p => p is null)) return;
            var geometry = new StreamGeometry();
            using (var path = geometry.Open()) { path.BeginFigure(screen[0]!.Value.Screen, fill is not null, true); path.PolyLineTo(screen.Skip(1).Select(p => p!.Value.Screen).ToList(), true, false); }
            geometry.Freeze(); draw.DrawGeometry(fill, pen, geometry);
        }

        private static IEnumerable<Point3D> Circle(Point3D centre, double radius, int steps = 32) =>
            Enumerable.Range(0, steps).Select(i => { var t = i * 2 * Math.PI / steps; return new Point3D(centre.X + radius * Math.Sin(t), centre.Y + radius * Math.Cos(t), centre.Z); });

        private void Figure(DrawingContext draw, Camera c, Point3D at, double yaw, Brush tone, Brush head, string label, bool crowded, bool solid, bool selected)
        {
            if (at.Z != 0)
            {
                Polygon3(draw, c, new SolidColorBrush(Color.FromArgb(40, 0x6B, 0x4A, 0x35)), null, Circle(new(at.X, at.Y, 0), 20));
                Line3(draw, c, new Pen(B("Faint"), 1.2) { DashStyle = DashStyles.Dash }, new(at.X, at.Y, 0), at);
            }
            if (crowded) Polygon3(draw, c, null, new Pen(B("Danger"), 2) { DashStyle = DashStyles.Dash }, Circle(at, 34));
            var ring = new Pen(tone, selected ? 2.4 : 1.6); if (!solid) ring.DashStyle = DashStyles.Dash;
            Polygon3(draw, c, new SolidColorBrush(Color.FromArgb(selected ? (byte)60 : (byte)28, 0xE2, 0x72, 0x5B)), ring, Circle(at, 22));
            var angle = yaw * Math.PI / 180; Vector3D ahead = new(Math.Sin(angle), Math.Cos(angle), 0), side = new(Math.Cos(angle), -Math.Sin(angle), 0);
            Polygon3(draw, c, tone, null, [at + ahead * 52, at + ahead * 26 + side * 11, at + ahead * 26 - side * 11]);
            var top = at + new Vector3D(0, 0, BodyHeight); var crown = at + new Vector3D(0, 0, HeadHeight);
            Line3(draw, c, new Pen(tone, 4) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, at, top);
            if (Project(c, crown) is not { } h || Project(c, crown + c.Right * 15) is not { } edge) return;
            var radius = Math.Max(9, (edge.Screen - h.Screen).Length);
            draw.DrawEllipse(head, new Pen(tone, 2), h.Screen, radius, radius);
            Label(draw, label, h.Screen, selected ? Brushes.White : B("Ink"), Math.Clamp(radius * .9, 10, 14), true, true);
        }

        private void Gizmo(DrawingContext draw, Camera c, MapActor actor)
        {
            var at = new Point3D(actor.Position.X, actor.Position.Y, actor.Position.Z); var length = GizmoLength(c, at);
            if (Project(c, at) is not { } origin) return;
            var highlight = B("AxisHover");
            Polygon3(draw, c, null, new Pen(hover == Handle.Ring || drag == Handle.Ring ? highlight : B("Muted"), 2), Circle(at, length * .9, 48));
            foreach (var handle in new[] { Handle.X, Handle.Y, Handle.Z })
            {
                var brush = hover == handle || drag == handle ? highlight : B(handle == Handle.X ? "AxisX" : handle == Handle.Y ? "AxisY" : "AxisZ");
                if (Project(c, at + AxisVector(handle) * length) is not { } tip) continue;
                var direction = tip.Screen - origin.Screen; if (direction.Length < 4) continue;
                draw.DrawLine(new Pen(brush, 3), origin.Screen, tip.Screen);
                direction.Normalize(); var normal = new Vector(-direction.Y, direction.X);
                var point = tip.Screen + direction * 12; var geometry = new StreamGeometry();
                using (var path = geometry.Open()) { path.BeginFigure(point, true, true); path.LineTo(tip.Screen + normal * 6, true, false); path.LineTo(tip.Screen - normal * 6, true, false); }
                geometry.Freeze(); draw.DrawGeometry(brush, null, geometry);
                Label(draw, handle.ToString(), point + direction * 9, brush, 11, true, true);
            }
            var centre = hover == Handle.Body || drag == Handle.Body ? highlight : Brushes.White;
            draw.DrawRectangle(centre, new Pen(B("Ink"), 1.2), new Rect(origin.Screen.X - 5, origin.Screen.Y - 5, 10, 10));
        }

        private void AxisWidget(DrawingContext draw, Camera c)
        {
            var centre = new Point(ActualWidth - 42, 42);
            draw.DrawEllipse(new SolidColorBrush(Color.FromArgb(150, 255, 255, 255)), null, centre, 34, 34);
            foreach (var (axis, key, name) in new[] { (new Vector3D(1, 0, 0), "AxisX", "X"), (new Vector3D(0, 1, 0), "AxisY", "Y"), (new Vector3D(0, 0, 1), "AxisZ", "Z") }
                .OrderBy(item => Vector3D.DotProduct(item.Item1, -c.Forward)))
            {
                var end = centre + new Vector(Vector3D.DotProduct(axis, c.Right), -Vector3D.DotProduct(axis, c.Up)) * 24;
                draw.DrawLine(new Pen(B(key), 2), centre, end); draw.DrawEllipse(B(key), null, end, 8, 8);
                Label(draw, name, end, Brushes.White, 10, true, true);
            }
        }

        private void Label(DrawingContext draw, string text, Point at, Brush brush, double size, bool centred, bool middle = false)
        {
            var format = new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(new FontFamily("Segoe UI, Microsoft YaHei UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal), size, brush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            draw.DrawText(format, new Point(centred ? at.X - format.Width / 2 : at.X, middle ? at.Y - format.Height / 2 : at.Y));
        }

        // ── Picking ────────────────────────────────────────────────────

        private static double SegmentDistance(Point p, Point a, Point b)
        {
            var ab = b - a; var t = ab.LengthSquared < 1e-6 ? 0 : Math.Clamp(Vector.Multiply(p - a, ab) / ab.LengthSquared, 0, 1);
            return (p - (a + ab * t)).Length;
        }

        private MapActor? HitActor(Camera c, Point mouse)
        {
            MapActor? best = null; var bestDepth = double.MaxValue;
            foreach (var actor in Actors())
            {
                var at = new Point3D(actor.Position.X, actor.Position.Y, actor.Position.Z);
                if (Project(c, at) is not { } foot || Project(c, at + new Vector3D(0, 0, HeadHeight)) is not { } head) continue;
                var near = SegmentDistance(mouse, foot.Screen, head.Screen) < 12 || (mouse - head.Screen).Length < 16 || (mouse - foot.Screen).Length < 16;
                if (near && foot.Depth < bestDepth) { best = actor; bestDepth = foot.Depth; }
            }
            return best;
        }

        private Handle HitGizmo(Camera c, Point mouse)
        {
            var actor = Actors().FirstOrDefault(a => ReferenceEquals(a.Card, Selected())); if (actor is null) return Handle.None;
            var at = new Point3D(actor.Position.X, actor.Position.Y, actor.Position.Z); var length = GizmoLength(c, at);
            if (Project(c, at) is not { } origin) return Handle.None;
            if ((mouse - origin.Screen).Length < 9) return Handle.Body;
            foreach (var handle in new[] { Handle.X, Handle.Y, Handle.Z })
                if (Project(c, at + AxisVector(handle) * length) is { } tip && SegmentDistance(mouse, origin.Screen, tip.Screen + (tip.Screen - origin.Screen) * .18) < 8) return handle;
            var ring = Circle(at, length * .9, 48).Select(p => Project(c, p)).ToArray();
            for (var i = 0; i < ring.Length; i++)
                if (ring[i] is { } a && ring[(i + 1) % ring.Length] is { } b && SegmentDistance(mouse, a.Screen, b.Screen) < 6) return Handle.Ring;
            return Handle.None;
        }

        // ── Editing ────────────────────────────────────────────────────

        private void BeginEdit(Handle handle, ActorCard card, StagePosition start, Point mouse)
        {
            var c = View(); drag = handle; dragCard = card; dragStart = start; dragAnchor = new(start.X, start.Y, start.Z); framed = true;
            if (handle is Handle.X or Handle.Y or Handle.Z) dragAxisStart = AlongAxis(c, mouse, dragAnchor, AxisVector(handle));
            else if (handle == Handle.Body) dragAnchor = OnPlane(c, mouse, start.Z) ?? dragAnchor;
            else if (OnPlane(c, mouse, start.Z) is { } p) dragAngleStart = Math.Atan2(p.X - start.X, p.Y - start.Y) * 180 / Math.PI;
            InvalidateVisual();
        }

        private void ContinueEdit(Point mouse)
        {
            if (dragCard is null) return;
            var c = View(); var free = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift); var s = dragStart; var anchor = new Point3D(s.X, s.Y, s.Z);
            double Snap(double value) => free ? Math.Round(value) : Math.Round(value / 10) * 10;
            var next = s;
            switch (drag)
            {
                case Handle.X: next = s with { X = Snap(s.X + AlongAxis(c, mouse, anchor, new(1, 0, 0)) - dragAxisStart) }; break;
                case Handle.Y: next = s with { Y = Snap(s.Y + AlongAxis(c, mouse, anchor, new(0, 1, 0)) - dragAxisStart) }; break;
                case Handle.Z: next = s with { Z = Snap(s.Z + AlongAxis(c, mouse, anchor, new(0, 0, 1)) - dragAxisStart) }; break;
                case Handle.Body:
                    if (OnPlane(c, mouse, s.Z) is { } p) next = s with { X = Snap(s.X + p.X - dragAnchor.X), Y = Snap(s.Y + p.Y - dragAnchor.Y) };
                    break;
                case Handle.Ring:
                    if (OnPlane(c, mouse, s.Z) is { } q)
                    {
                        var yaw = s.Yaw + Math.Atan2(q.X - s.X, q.Y - s.Y) * 180 / Math.PI - dragAngleStart;
                        yaw = free ? Math.Round(yaw) : Math.Round(yaw / 15) * 15; next = s with { Yaw = Wrap(yaw) };
                    }
                    break;
            }
            next = next with { X = Math.Clamp(next.X, -5000, 5000), Y = Math.Clamp(next.Y, -5000, 5000), Z = Math.Clamp(next.Z, -1000, 1000) };
            Moved?.Invoke(dragCard, next);
        }

        private void EndEdit(bool cancel)
        {
            if (cancel && dragCard is not null) Moved?.Invoke(dragCard, dragStart);
            drag = Handle.None; dragCard = null; grabbing = false; if (IsMouseCaptured) ReleaseMouseCapture(); InvalidateVisual();
        }

        protected override void OnMouseDown(MouseButtonEventArgs args)
        {
            Focus(); var mouse = args.GetPosition(this); lastMouse = mouse; var c = View();
            if (grabbing) { EndEdit(args.ChangedButton != MouseButton.Left); args.Handled = true; return; }
            if (args.ChangedButton is MouseButton.Right or MouseButton.Middle)
            {
                panning = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift); orbiting = !panning; CaptureMouse(); args.Handled = true; return;
            }
            if (args.ChangedButton != MouseButton.Left) return;
            var handle = HitGizmo(c, mouse); var selected = Actors().FirstOrDefault(a => ReferenceEquals(a.Card, Selected()));
            if (handle != Handle.None && selected is not null) { BeginEdit(handle, selected.Card, selected.Position, mouse); CaptureMouse(); args.Handled = true; return; }
            if (HitActor(c, mouse) is not { } hit) return;
            Picked?.Invoke(hit.Card); BeginEdit(Handle.Body, hit.Card, hit.Position, mouse); CaptureMouse(); args.Handled = true;
        }

        protected override void OnMouseMove(MouseEventArgs args)
        {
            var mouse = args.GetPosition(this); var delta = mouse - lastMouse; lastMouse = mouse;
            if (orbiting)
            {
                azimuth += delta.X * .4; elevation = Math.Clamp(elevation + delta.Y * .4, -10, 89.9); ortho = false; framed = true; InvalidateVisual(); return;
            }
            if (panning)
            {
                var c = View(); var perPixel = c.Ortho ? 1 / c.Scale : distance / c.Focal;
                target += -c.Right * delta.X * perPixel + c.Up * delta.Y * perPixel; framed = true; InvalidateVisual(); return;
            }
            if (drag != Handle.None) { ContinueEdit(mouse); return; }
            var view = View(); var hovered = HitGizmo(view, mouse);
            if (hovered != hover) { hover = hovered; InvalidateVisual(); }
            Cursor = hover != Handle.None || HitActor(view, mouse) is not null ? Cursors.Hand : null;
        }

        protected override void OnMouseUp(MouseButtonEventArgs args)
        {
            if (orbiting || panning) { orbiting = panning = false; ReleaseMouseCapture(); args.Handled = true; return; }
            if (drag != Handle.None && !grabbing && args.ChangedButton == MouseButton.Left) EndEdit(false);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs args)
        {
            distance = Math.Clamp(distance * (args.Delta > 0 ? .88 : 1.14), 150, 8000); framed = true; InvalidateVisual(); args.Handled = true;
        }

        protected override void OnKeyDown(KeyEventArgs args)
        {
            var key = args.Key; var selected = Actors().FirstOrDefault(a => ReferenceEquals(a.Card, Selected()));
            switch (key)
            {
                case Key.G when selected is not null && drag == Handle.None:
                    grabbing = true; BeginEdit(Handle.Body, selected.Card, selected.Position, Mouse.GetPosition(this)); break;
                case Key.R when selected is not null && drag == Handle.None:
                    grabbing = true; BeginEdit(Handle.Ring, selected.Card, selected.Position, Mouse.GetPosition(this)); break;
                case Key.X or Key.Y or Key.Z when grabbing && drag != Handle.Ring && dragCard is not null:
                    var card = dragCard; var start = dragStart; Moved?.Invoke(card, start);
                    BeginEdit(key == Key.X ? Handle.X : key == Key.Y ? Handle.Y : Handle.Z, card, start, Mouse.GetPosition(this)); break;
                case Key.Escape when drag != Handle.None: EndEdit(true); break;
                case Key.Enter when grabbing: EndEdit(false); break;
                case Key.NumPad7 or Key.D7: SetView(StageView.Top); break;
                case Key.NumPad1 or Key.D1: SetView(StageView.Front); break;
                case Key.NumPad3 or Key.D3: SetView(StageView.Side); break;
                case Key.NumPad5 or Key.D5: ortho = !ortho; InvalidateVisual(); break;
                case Key.Home: Frame(); InvalidateVisual(); break;
                default: return;
            }
            args.Handled = true;
        }
    }
}
