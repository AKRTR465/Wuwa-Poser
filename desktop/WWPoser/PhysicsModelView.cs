using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using WWPoser.Services;

namespace WWPoser;

internal sealed partial class PhysicsModelView : Grid
{
    private readonly Viewport3D viewport = new() { ClipToBounds = true, IsHitTestVisible = false };
    private readonly OrthographicCamera camera = new();
    private readonly Model3DGroup scene = new();
    private readonly TextBlock instructions = new() { Text = "拖动旋转 · 滚轮缩放 · 右键拖动平移", HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Bottom, Margin = new(6, 6, 6, 9), IsHitTestVisible = false, FontSize = 11 };
    private readonly Material baseMaterial = MakeMaterial(Color.FromRgb(163, 185, 209));
    private readonly Material highlightMaterial = MakeMaterial(Color.FromRgb(255, 156, 71));
    private (GeometryModel3D Base, GeometryModel3D Highlight)[] geometry = [];
    private PhysicsPreviewModel? model;
    private Point origin;
    private MouseButton? dragging;
    private Vector3 home, center;
    private double yaw, pitch, span = 2, zoom = 1;

    public PhysicsModelView()
    {
        Focusable = true;
        Background = new SolidColorBrush(Color.FromRgb(26, 34, 47));
        instructions.Foreground = new SolidColorBrush(Color.FromRgb(181, 196, 216));
        viewport.Camera = camera;
        viewport.Children.Add(new ModelVisual3D { Content = scene });
        capsuleOverlay = new CapsuleOverlay(this) { IsHitTestVisible = false, ClipToBounds = true };
        Children.Add(viewport); Children.Add(capsuleOverlay); Children.Add(instructions);
        SizeChanged += (_, _) => UpdateCamera();
        LostMouseCapture += (_, _) => { dragging = null; StopCapsuleDrag(false); };
    }

    public void SetModel(PhysicsPreviewModel? value)
    {
        model = value; scene.Children.Clear(); geometry = []; SetCapsulePose(null);
        if (model is null) { SetView(0); return; }
        scene.Children.Add(new AmbientLight(Color.FromRgb(110, 117, 128)));
        scene.Children.Add(new DirectionalLight(Color.FromRgb(215, 219, 225), new Vector3D(.2, -.55, -.86)));
        scene.Children.Add(new DirectionalLight(Color.FromRgb(98, 114, 144), new Vector3D(-.75, -.45, .5)));
        var low = new Vector3(float.PositiveInfinity);
        var high = new Vector3(float.NegativeInfinity);
        foreach (var mesh in model.Meshes)
            foreach (var vertex in mesh.Vertices)
            {
                low = Vector3.Min(low, vertex.Position);
                high = Vector3.Max(high, vertex.Position);
            }
        home = (low + high) / 2;
        span = Math.Max(.05, (high - low).Length() * 1.15);
        geometry = model.Meshes.Select(_ => (AddGeometry(baseMaterial), AddGeometry(highlightMaterial))).ToArray();
        Apply(Deform(model, null));
        Highlight([]);
        SetView(0);
    }

    private GeometryModel3D AddGeometry(Material material)
    {
        var entry = new GeometryModel3D(new MeshGeometry3D(), material) { BackMaterial = material };
        scene.Children.Add(entry);
        return entry;
    }

    public int Highlight(HashSet<int> selection)
    {
        if (model is null) return 0;
        var selectedTriangles = 0;
        for (var i = 0; i < model.Meshes.Length; i++)
        {
            var mesh = model.Meshes[i];
            var values = mesh.Vertices.Select(v =>
                (selection.Contains(mesh.Joints[v.J0].RuntimeBone) ? v.Weights.X : 0) +
                (selection.Contains(mesh.Joints[v.J1].RuntimeBone) ? v.Weights.Y : 0) +
                (selection.Contains(mesh.Joints[v.J2].RuntimeBone) ? v.Weights.Z : 0) +
                (selection.Contains(mesh.Joints[v.J3].RuntimeBone) ? v.Weights.W : 0)).ToArray();
            var normal = new Int32Collection(mesh.Triangles.Length);
            var accent = new Int32Collection();
            for (var k = 0; k < mesh.Triangles.Length; k += 3)
            {
                var a = mesh.Triangles[k]; var b = mesh.Triangles[k + 1]; var c = mesh.Triangles[k + 2];
                var target = (values[a] + values[b] + values[c]) / 3 >= .025f ? accent : normal;
                target.Add(a); target.Add(b); target.Add(c);
            }
            normal.Freeze(); accent.Freeze();
            SetTriangles(geometry[i].Base, normal, baseMaterial);
            SetTriangles(geometry[i].Highlight, accent, highlightMaterial);
            selectedTriangles += accent.Count / 3;
        }
        return selectedTriangles;
    }

    private static void SetTriangles(GeometryModel3D target, Int32Collection triangles, Material material)
    {
        // WPF draws sequential vertex triples when the index collection is empty.
        // Hide that layer so an empty highlight never contributes geometry.
        target.Material = target.BackMaterial = triangles.Count > 0 ? material : null;
        ((MeshGeometry3D)target.Geometry).TriangleIndices = triangles;
    }

    internal sealed record Surface(Point3DCollection Positions, Vector3DCollection Normals);

    public static Surface[] Deform(PhysicsPreviewModel model, PhysicsModelPose? pose)
    {
        return model.Meshes.Select(mesh =>
        {
            var matrices = new Matrix4x4[mesh.Joints.Length];
            var normals = new Matrix4x4[mesh.Joints.Length];
            for (var i = 0; i < matrices.Length; i++)
            {
                var joint = mesh.Joints[i];
                var skin = pose is null ? Matrix4x4.Identity : joint.InverseBind * pose.Transforms[joint.RuntimeBone];
                matrices[i] = skin;
                normals[i] = Matrix4x4.Invert(skin, out var inverted) ? Matrix4x4.Transpose(inverted) : skin;
            }
            var positions = new Point3DCollection(mesh.Vertices.Length);
            var vectors = new Vector3DCollection(mesh.Vertices.Length);
            foreach (var v in mesh.Vertices)
            {
                var position = v.Position;
                var normal = v.Normal;
                if (pose is not null)
                {
                    position = Vector3.Transform(position, matrices[v.J0]) * v.Weights.X + Vector3.Transform(position, matrices[v.J1]) * v.Weights.Y +
                        Vector3.Transform(position, matrices[v.J2]) * v.Weights.Z + Vector3.Transform(position, matrices[v.J3]) * v.Weights.W;
                    normal = Vector3.TransformNormal(normal, normals[v.J0]) * v.Weights.X + Vector3.TransformNormal(normal, normals[v.J1]) * v.Weights.Y +
                        Vector3.TransformNormal(normal, normals[v.J2]) * v.Weights.Z + Vector3.TransformNormal(normal, normals[v.J3]) * v.Weights.W;
                    normal = normal.LengthSquared() > 1e-10f ? Vector3.Normalize(normal) : v.Normal;
                }
                positions.Add(new(position.X, position.Y, position.Z));
                vectors.Add(new(normal.X, normal.Y, normal.Z));
            }
            positions.Freeze(); vectors.Freeze();
            return new Surface(positions, vectors);
        }).ToArray();
    }

    public void Apply(Surface[] surfaces)
    {
        for (var i = 0; i < geometry.Length; i++)
        {
            var main = (MeshGeometry3D)geometry[i].Base.Geometry;
            var selected = (MeshGeometry3D)geometry[i].Highlight.Geometry;
            main.Positions = selected.Positions = surfaces[i].Positions;
            main.Normals = selected.Normals = surfaces[i].Normals;
        }
    }

    public void SetView(int view)
    {
        if (model is null) MeasureCapsuleReference();
        center = home; zoom = 1; pitch = view == 2 ? .17 : 0;
        yaw = view switch { 1 => Math.PI / 2, 2 => -.45, _ => 0 };
        UpdateCamera();
    }

    internal void Orbit(double dx, double dy)
    {
        yaw = Math.IEEERemainder(yaw - dx * .009, Math.PI * 2);
        pitch = Math.Clamp(pitch + dy * .009, -1.48, 1.48);
        UpdateCamera();
    }

    internal void Zoom(int delta)
    {
        zoom = Math.Clamp(zoom * Math.Pow(1.16, -delta / 120d), .07, 6);
        UpdateCamera();
    }

    private void UpdateCamera()
    {
        var look = new Vector3D(Math.Sin(yaw) * Math.Cos(pitch), Math.Sin(pitch), Math.Cos(yaw) * Math.Cos(pitch));
        var distance = span * 6;
        camera.Position = new Point3D(center.X, center.Y, center.Z) + look * distance;
        camera.LookDirection = -look;
        camera.UpDirection = new(0, 1, 0);
        camera.NearPlaneDistance = span * .01;
        camera.FarPlaneDistance = distance * 3;
        camera.Width = span * zoom * Math.Max(1, ActualWidth / Math.Max(1, ActualHeight));
        capsuleOverlay.InvalidateVisual();
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.ChangedButton is not (MouseButton.Left or MouseButton.Right)) return;
        if (capsuleDrag is not null)
        {
            if (e.ChangedButton == MouseButton.Right && StopCapsuleDrag(false)) ReleaseMouseCapture();
            e.Handled = true;
            return;
        }
        if (dragging is not null) { e.Handled = true; return; }
        Focus();
        origin = e.GetPosition(this);
        var additive = (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) != 0;
        if (e.ChangedButton == MouseButton.Left && SelectOrDragCapsule(origin, additive)) { e.Handled = true; return; }
        dragging = e.ChangedButton;
        CaptureMouse(); e.Handled = true;
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.ChangedButton == MouseButton.Left && capsuleDrag is not null)
        {
            MoveCapsuleDrag(e.GetPosition(this));
            StopCapsuleDrag(true);
            ReleaseMouseCapture();
            e.Handled = true;
            return;
        }
        if (dragging != e.ChangedButton) return;
        ReleaseMouseCapture(); e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (MoveCapsuleDrag(e.GetPosition(this))) { e.Handled = true; return; }
        if (dragging is null) return;
        var position = e.GetPosition(this); var delta = position - origin; origin = position;
        if (dragging == MouseButton.Left) Orbit(delta.X, delta.Y);
        else
        {
            var right = Vector3D.CrossProduct(camera.LookDirection, camera.UpDirection); right.Normalize();
            var up = Vector3D.CrossProduct(right, camera.LookDirection);
            var movement = (up * delta.Y - right * delta.X) * (camera.Width / Math.Max(1, ActualWidth));
            center += new Vector3((float)movement.X, (float)movement.Y, (float)movement.Z);
            UpdateCamera();
        }
        e.Handled = true;
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        if (capsuleDrag is null) Zoom(e.Delta);
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key != Key.Escape || !StopCapsuleDrag(false)) return;
        ReleaseMouseCapture();
        e.Handled = true;
    }

    private static DiffuseMaterial MakeMaterial(Color color)
    {
        var material = new DiffuseMaterial(new SolidColorBrush(color)); material.Freeze(); return material;
    }
}
