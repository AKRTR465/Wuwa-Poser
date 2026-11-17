using System.Numerics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using WWPoser.Services;
using Quaternion = System.Numerics.Quaternion;
using Vector = System.Windows.Vector;

namespace WWPoser;

internal readonly record struct CapsuleKey(uint Group, uint Id);

internal sealed record CapsuleViewItem(CapsuleKey Key, string Label, int Bone, bool Enabled, bool Active,
    float Radius, float Length, Vector3 Offset, Vector3 Rotation)
{
    public override string ToString() => Label;
}

internal sealed partial class PhysicsModelView
{
    private sealed record CapsuleDrag(CapsuleKey Key, int Axis, Point Start, Vector Direction, double UnitsPerPixel, float Original,
        RotationRing? Ring = null)
    {
        public bool Rotating => Ring.HasValue;
        public float Value { get; set; } = Original;
        public double? LastAngle { get; set; }
        public double AngleDelta { get; set; }
    }

    private readonly record struct RotationRing(Point Center, Vector U, Vector V)
    {
        public Point At(double angle) => Center + U * Math.Cos(angle) + V * Math.Sin(angle);
        public Vector Tangent(double angle) => V * Math.Cos(angle) - U * Math.Sin(angle);

        public double? Angle(Point point)
        {
            var determinant = U.X * V.Y - U.Y * V.X;
            if (Math.Abs(determinant) < 200) return null; // Use the tangent while the ring is nearly edge-on.
            var move = point - Center;
            var u = (move.X * V.Y - move.Y * V.X) / determinant;
            var v = (U.X * move.Y - U.Y * move.X) / determinant;
            return u * u + v * v >= .06 ? Math.Atan2(v, u) : null;
        }
    }

    private const int RingSegments = 48;
    private const double RingStep = Math.Tau / RingSegments;
    private readonly CapsuleOverlay capsuleOverlay;
    private readonly Dictionary<int, Matrix4x4> capsuleBind = [];
    private IReadOnlyList<Matrix4x4> capsuleReference = [];
    private IReadOnlyList<int> capsuleParents = [];
    private IReadOnlyList<CapsuleViewItem> visibleCapsules = [];
    private PhysicsModelPose? capsulePose;
    private CapsuleKey? selectedCapsule;
    private HashSet<CapsuleKey> selectedCapsules = [];
    private CapsuleDrag? capsuleDrag;
    private bool canDragCapsule, rotateCapsule, placeCapsule, frameNewCapsules;
    private static readonly Vector3[] LocalAxes = [Vector3.UnitX, Vector3.UnitZ, Vector3.UnitY];
    private static readonly Color[] AxisColors = [Colors.Tomato, Colors.LightGreen, Colors.DeepSkyBlue];
    private static readonly Color[] RotationColors = [AxisColors[1], AxisColors[2], AxisColors[0]];
    public event Action<CapsuleKey>? CapsulePicked;
    public event Action<CapsuleKey, bool>? CapsulePickedWithModifiers;
    public event Action<CapsuleKey, int, double>? CapsuleDragged;
    public event Action<CapsuleKey, int, double>? CapsuleRotated;
    public event Action<CapsuleKey, Vector3>? CapsulePlaced;
    public bool IsCapsuleDragging => capsuleDrag is not null;

    public void SetCapsules(IReadOnlyList<CapsuleViewItem> items, CapsuleKey? selected, bool editable,
        IReadOnlyCollection<CapsuleKey>? selectedKeys = null, bool rotating = false, bool placing = false)
    {
        if (capsuleDrag is { } drag &&
            (!editable || selected != selectedCapsule || rotating != rotateCapsule || placing != placeCapsule ||
                !items.Any(item => item.Key == drag.Key)) &&
            StopCapsuleDrag(false))
            ReleaseMouseCapture();
        var fit = model is null && items.Count > 0 && (frameNewCapsules || visibleCapsules.Count == 0);
        visibleCapsules = items;
        selectedCapsule = selected;
        selectedCapsules = selectedKeys?.ToHashSet() ?? [];
        if (selected is { } primary) selectedCapsules.Add(primary);
        canDragCapsule = editable;
        rotateCapsule = rotating;
        placeCapsule = placing;
        instructions.Text = items.Count == 0 ? "拖动旋转视图 · 滚轮缩放 · 右键拖动平移" :
            !editable ? "点击胶囊 · 滚轮缩放 · 右键拖动平移" :
            placing ? "点击视图放置 · Shift 或 Ctrl 点击选择胶囊" :
            rotating ? "拖动彩色圆环旋转 · Shift 或 Ctrl 点击多选" : "拖动彩色轴移动 · Shift 或 Ctrl 点击多选";
        if (fit) SetView(0);
        capsuleOverlay.InvalidateVisual();
    }

    public void SetCapsulePose(PhysicsModelPose? pose)
    {
        capsulePose = pose;
        if (pose is null)
        {
            capsuleBind.Clear();
            if (model is not null)
                foreach (var joint in model.Meshes.SelectMany(mesh => mesh.Joints))
                    if (!capsuleBind.ContainsKey(joint.RuntimeBone) && Matrix4x4.Invert(joint.InverseBind, out var transform))
                        capsuleBind.Add(joint.RuntimeBone, transform);
        }
        capsuleOverlay.InvalidateVisual();
    }

    public void SetCapsuleReference(IReadOnlyList<Matrix4x4> transforms, IReadOnlyList<int>? parents = null)
    {
        parents ??= [];
        if (capsuleReference.SequenceEqual(transforms) && capsuleParents.SequenceEqual(parents)) return;
        capsuleReference = transforms;
        capsuleParents = parents;
        if (model is null)
        {
            SetView(0);
            frameNewCapsules = true;
        }
        capsuleOverlay.InvalidateVisual();
    }

    private void MeasureCapsuleReference()
    {
        var low = new Vector3(float.PositiveInfinity);
        var high = new Vector3(float.NegativeInfinity);
        void Include(Vector3 position)
        {
            if (!float.IsFinite(position.X) || !float.IsFinite(position.Y) || !float.IsFinite(position.Z)) return;
            low = Vector3.Min(low, position);
            high = Vector3.Max(high, position);
        }
        foreach (var transform in capsuleReference) Include(transform.Translation);
        foreach (var capsule in visibleCapsules)
        {
            if (CapsuleTransform(capsule.Bone) is not { } transform) continue;
            var local = LocalCenter(capsule);
            var extent = CapsuleAxis(capsule.Rotation) * capsule.Length * .005f;
            var radius = capsule.Radius * .01f;
            var thickness = new Vector3(
                new Vector3(transform.M11, transform.M21, transform.M31).Length(),
                new Vector3(transform.M12, transform.M22, transform.M32).Length(),
                new Vector3(transform.M13, transform.M23, transform.M33).Length()) * radius;
            var a = Vector3.Transform(local - extent, transform);
            var b = Vector3.Transform(local + extent, transform);
            Include(Vector3.Min(a, b) - thickness);
            Include(Vector3.Max(a, b) + thickness);
        }
        var bounded = float.IsFinite(low.X) && float.IsFinite(high.X);
        home = bounded ? (low + high) / 2 : Vector3.Zero;
        span = bounded ? Math.Max(.3, (high - low).Length() * 1.25) : 2;
        frameNewCapsules = false;
    }

    internal static Vector3 CapsuleAxis(Vector3 rotation)
    {
        var axis = CapsuleAuthoring.Axis(rotation);
        return new(axis.X, axis.Z, axis.Y);
    }

    private Matrix4x4? CapsuleTransform(int bone) => bone < 0 ? Matrix4x4.Identity :
        capsulePose is { } pose && bone < pose.Transforms.Length ? pose.Transforms[bone] :
        capsuleBind.TryGetValue(bone, out var transform) ? transform :
        bone < capsuleReference.Count ? capsuleReference[bone] : null;

    private Vector3 LocalCenter(CapsuleViewItem item)
    {
        var offset = item.Offset;
        if (capsuleDrag is { Rotating: false } drag && drag.Key == item.Key)
            offset[drag.Axis] = drag.Value;
        return new Vector3(offset.X, offset.Z, offset.Y) * .01f;
    }

    private Vector3 LocalRotation(CapsuleViewItem item)
    {
        var rotation = item.Rotation;
        if (capsuleDrag is { Rotating: true } drag && drag.Key == item.Key)
            rotation[drag.Axis] = drag.Value;
        return rotation;
    }

    private (Vector3D Right, Vector3D Up) ViewAxes()
    {
        var right = Vector3D.CrossProduct(camera.LookDirection, camera.UpDirection); right.Normalize();
        var up = Vector3D.CrossProduct(right, camera.LookDirection); up.Normalize();
        return (right, up);
    }

    private Point Screen(Vector3 position)
    {
        var (right, up) = ViewAxes();
        var move = position - center;
        var v = new Vector3D(move.X, move.Y, move.Z);
        var pixels = ActualWidth / Math.Max(.001, camera.Width);
        return new(ActualWidth / 2 + Vector3D.DotProduct(v, right) * pixels, ActualHeight / 2 - Vector3D.DotProduct(v, up) * pixels);
    }

    private (Vector3 Origin, Vector3 Direction) ViewRay(Point point)
    {
        var (right, up) = ViewAxes();
        var perPixel = camera.Width / Math.Max(1, ActualWidth);
        var start = camera.Position + (right * (point.X - ActualWidth / 2) - up * (point.Y - ActualHeight / 2)) * perPixel;
        var look = camera.LookDirection;
        return (new((float)start.X, (float)start.Y, (float)start.Z), new((float)look.X, (float)look.Y, (float)look.Z));
    }

    private static double PointDistance(Point point, Point start, Point end)
    {
        var line = end - start;
        var amount = line.LengthSquared < .0001 ? 0 : Math.Clamp(Vector.Multiply(point - start, line) / line.LengthSquared, 0, 1);
        return (point - (start + line * amount)).Length;
    }

    private Vector AxisPixels(Matrix4x4 transform, Vector3 local, int axis) =>
        Screen(Vector3.Transform(local + LocalAxes[axis] * .01f, transform)) - Screen(Vector3.Transform(local, transform));

    private RotationRing MakeRotationRing(Matrix4x4 transform, Vector3 local, Vector3 rotation, int axis)
    {
        // The native orientation is yaw around +Z, pitch around -Y, then roll around -X.
        var yaw = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, rotation.Y * (MathF.PI / 180));
        var pitch = Quaternion.CreateFromAxisAngle(-Vector3.UnitY, rotation.X * (MathF.PI / 180));
        var direction = axis switch
        {
            0 => Vector3.Transform(-Vector3.UnitY, yaw),
            1 => Vector3.UnitZ,
            _ => Vector3.Transform(-Vector3.UnitX, yaw * pitch)
        };
        var u = Vector3.Normalize(Vector3.Cross(direction, Math.Abs(direction.Z) < .9f ? Vector3.UnitZ : Vector3.UnitX));
        var v = Vector3.Cross(direction, u);
        var middle = Screen(Vector3.Transform(local, transform));
        Vector Project(Vector3 point) => Screen(Vector3.Transform(local + new Vector3(point.X, point.Z, point.Y), transform)) - middle;
        var screenU = Project(u);
        var screenV = Project(v);
        var scale = 40 / Math.Max(1, Math.Max(screenU.Length, screenV.Length));
        return new(middle, screenU * scale, screenV * scale);
    }

    private bool StartCapsuleDrag(CapsuleViewItem item, Matrix4x4 transform, Point point)
    {
        var local = LocalCenter(item);
        var start = Screen(Vector3.Transform(local, transform));
        var nearest = 8d;
        CapsuleDrag? choice = null;
        for (var axis = 0; axis < 3; axis++)
        {
            if (rotateCapsule)
            {
                var ring = MakeRotationRing(transform, local, item.Rotation, axis);
                var previous = ring.At(0);
                for (var segment = 1; segment <= RingSegments; segment++)
                {
                    var end = ring.At(segment * RingStep);
                    var distance = PointDistance(point, previous, end);
                    var tangent = ring.Tangent((segment - .5) * RingStep);
                    previous = end;
                    if (distance >= nearest || tangent.Length < 4) continue;
                    nearest = distance;
                    choice = new(item.Key, axis, point, tangent / tangent.Length, 180 / (Math.PI * tangent.Length), item.Rotation[axis], ring)
                    {
                        LastAngle = ring.Angle(point)
                    };
                }
            }
            else
            {
                var direction = AxisPixels(transform, local, axis);
                var length = direction.Length;
                if (length < .15) continue;
                direction /= length;
                var distance = PointDistance(point, start + direction * 12, start + direction * 36);
                if (distance >= nearest) continue;
                nearest = distance;
                choice = new(item.Key, axis, point, direction, 1 / length, item.Offset[axis]);
            }
        }
        if (choice is null || !CaptureMouse()) return false;
        capsuleDrag = choice;
        return true;
    }

    private static double CapsuleHitDepth(Matrix4x4 transform, Vector3 a, Vector3 b, float radius, Vector3 origin, Vector3 direction)
    {
        if (radius <= 0 || !Matrix4x4.Invert(transform, out var inverse)) return double.PositiveInfinity;
        origin = Vector3.Transform(origin, inverse);
        direction = Vector3.TransformNormal(direction, inverse);
        var radiusSquared = radius * radius;
        static (double Start, double End) Hit(Vector3 offset, Vector3 direction, float radiusSquared)
        {
            var length = direction.LengthSquared();
            var projection = Vector3.Dot(offset, direction);
            var discriminant = projection * projection - length * (offset.LengthSquared() - radiusSquared);
            if (length <= 1e-10 || discriminant < 0) return (double.PositiveInfinity, double.PositiveInfinity);
            var root = Math.Sqrt(discriminant);
            return ((-projection - root) / length, (-projection + root) / length);
        }
        static double Front((double Start, double End) t) => t.Start >= 0 ? t.Start : t.End >= 0 ? t.End : double.PositiveInfinity;
        var first = origin - a;
        var depth = Math.Min(Front(Hit(first, direction, radiusSquared)), Front(Hit(origin - b, direction, radiusSquared)));
        var segment = b - a;
        var segmentLength = segment.Length();
        if (segmentLength < 1e-6) return depth;
        var axis = segment / segmentLength;
        var u = Vector3.Dot(first, axis);
        var v = Vector3.Dot(direction, axis);
        var (enter, exit) = Hit(first - u * axis, direction - v * axis, radiusSquared);
        bool OnSegment(double t)
        {
            var position = u + t * v;
            return t >= 0 && position >= 0 && position <= segmentLength;
        }
        if (OnSegment(enter)) depth = Math.Min(depth, enter);
        if (OnSegment(exit)) depth = Math.Min(depth, exit);
        return depth;
    }

    private float MeshHitDepth(Vector3 origin, Vector3 direction)
    {
        if (model is null) return float.PositiveInfinity;
        var nearest = float.PositiveInfinity;
        for (var mesh = 0; mesh < geometry.Length; mesh++)
        {
            var vertices = ((MeshGeometry3D)geometry[mesh].Base.Geometry).Positions;
            var triangles = model.Meshes[mesh].Triangles;
            Vector3 PointAt(int index)
            {
                var p = vertices[triangles[index]];
                return new((float)p.X, (float)p.Y, (float)p.Z);
            }
            for (var t = 0; t < triangles.Length; t += 3)
            {
                var a = PointAt(t);
                var u = PointAt(t + 1) - a;
                var v = PointAt(t + 2) - a;
                var cross = Vector3.Cross(direction, v);
                var determinant = Vector3.Dot(u, cross);
                if (MathF.Abs(determinant) < 1e-9f) continue;
                var fromA = origin - a;
                var s = Vector3.Dot(fromA, cross) / determinant;
                if (s < 0 || s > 1) continue;
                var normal = Vector3.Cross(fromA, u);
                var r = Vector3.Dot(direction, normal) / determinant;
                if (r < 0 || s + r > 1) continue;
                var depth = Vector3.Dot(v, normal) / determinant;
                if (depth >= camera.NearPlaneDistance && depth <= camera.FarPlaneDistance && depth < nearest)
                    nearest = depth;
            }
        }
        return nearest;
    }

    private bool PlaceCapsule(CapsuleViewItem item, Matrix4x4 transform, Point point)
    {
        if (CapsulePlaced is not { } place || !Matrix4x4.Invert(transform, out var inverse)) return false;
        var (origin, direction) = ViewRay(point);
        var depth = MeshHitDepth(origin, direction);
        if (!float.IsFinite(depth))
        {
            var middle = Vector3.Transform(LocalCenter(item), transform);
            depth = Vector3.Dot(middle - origin, direction);
        }
        var local = Vector3.Transform(origin + direction * depth, inverse);
        place(item.Key, new Vector3(local.X, local.Z, local.Y) * 100);
        return true;
    }

    private bool SelectOrDragCapsule(Point point, bool additive)
    {
        if (!additive && canDragCapsule)
            foreach (var item in visibleCapsules)
                if (item.Key == selectedCapsule && CapsuleTransform(item.Bone) is { } transform &&
                    (placeCapsule ? PlaceCapsule(item, transform, point) : StartCapsuleDrag(item, transform, point)))
                    return true;

        var (origin, direction) = ViewRay(point);
        CapsuleViewItem? picked = null;
        CapsuleViewItem? fallback = null;
        double front = double.PositiveInfinity;
        double nearest = double.PositiveInfinity;
        foreach (var item in visibleCapsules)
        {
            if (CapsuleTransform(item.Bone) is not { } transform) continue;
            var local = LocalCenter(item);
            var extent = CapsuleAxis(item.Rotation) * item.Length * .005f;
            var depth = CapsuleHitDepth(transform, local - extent, local + extent, item.Radius * .01f, origin, direction);
            if (depth < front) { front = depth; picked = item; }
            var start = Screen(Vector3.Transform(local - extent, transform));
            var end = Screen(Vector3.Transform(local + extent, transform));
            var distance = PointDistance(point, start, end);
            if (distance <= 7 && distance < nearest) { nearest = distance; fallback = item; }
        }
        picked ??= fallback;
        if (picked is null) return false;
        if (CapsulePickedWithModifiers is { } handler) handler(picked.Key, additive);
        else CapsulePicked?.Invoke(picked.Key);
        return true;
    }

    private bool MoveCapsuleDrag(Point point)
    {
        if (capsuleDrag is not { } drag) return false;
        var limit = drag.Rotating ? 3600 : 300;
        var delta = Vector.Multiply(point - drag.Start, drag.Direction) * drag.UnitsPerPixel;
        if (drag.Ring is { } ring && drag.LastAngle is { } previous)
        {
            if (ring.Angle(point) is { } angle)
            {
                drag.AngleDelta += Math.IEEERemainder(angle - previous, Math.Tau);
                drag.LastAngle = angle;
            }
            delta = drag.AngleDelta * 180 / Math.PI;
        }
        var value = drag.Original + delta;
        if (delta != 0 && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
        {
            var increment = drag.Rotating ? 5 : 1;
            value = Math.Round(value / increment) * increment;
        }
        drag.Value = (float)Math.Clamp(value, -limit, limit);
        capsuleOverlay.InvalidateVisual(); return true;
    }

    private bool StopCapsuleDrag(bool commit)
    {
        if (capsuleDrag is not { } drag) return false;
        capsuleDrag = null;
        capsuleOverlay.InvalidateVisual();
        if (commit && Math.Abs(drag.Value - drag.Original) >= .02)
        {
            var value = Math.Round(drag.Value, 3);
            if (drag.Rotating) CapsuleRotated?.Invoke(drag.Key, drag.Axis, value);
            else CapsuleDragged?.Invoke(drag.Key, drag.Axis + 2, value);
        }
        return true;
    }

    private sealed class CapsuleOverlay(PhysicsModelView view) : FrameworkElement
    {
        private static readonly Pen ReferencePen = MakeReferencePen();

        protected override void OnRender(DrawingContext drawing)
        {
            if (view.model is null) DrawReferenceSkeleton(drawing);
            foreach (var item in view.visibleCapsules.OrderBy(item => item.Key == view.selectedCapsule ? 2 : view.selectedCapsules.Contains(item.Key) ? 1 : 0))
            {
                if (view.CapsuleTransform(item.Bone) is not { } transform) continue;
                DrawCapsule(drawing, item, transform);
            }
        }

        private static Pen MakeReferencePen()
        {
            var pen = new Pen(new SolidColorBrush(Color.FromArgb(106, 157, 175, 199)), 1);
            pen.Freeze();
            return pen;
        }

        private void DrawReferenceSkeleton(DrawingContext drawing)
        {
            var points = view.capsuleReference.Select((bone, index) =>
                view.Screen(view.CapsuleTransform(index)?.Translation ?? bone.Translation)).ToArray();
            for (var i = 0; i < points.Length; i++)
            {
                var parent = i < view.capsuleParents.Count ? view.capsuleParents[i] : -1;
                if (parent >= 0 && parent < points.Length && parent != i)
                    drawing.DrawLine(ReferencePen, points[parent], points[i]);
                drawing.DrawEllipse(ReferencePen.Brush, null, points[i], 1.35, 1.35);
            }
        }

        private void DrawCapsule(DrawingContext drawing, CapsuleViewItem item, Matrix4x4 transform)
        {
            var local = view.LocalCenter(item);
            var axis = CapsuleAxis(view.LocalRotation(item));
            var tangent = Vector3.Normalize(Vector3.Cross(axis, Math.Abs(axis.Y) < .9f ? Vector3.UnitY : Vector3.UnitX));
            var bitangent = Vector3.Cross(axis, tangent);
            var selected = item.Key == view.selectedCapsule;
            var highlighted = view.selectedCapsules.Contains(item.Key);
            var color = highlighted ? Color.FromRgb(255, 194, 102) : item.Active ? Color.FromArgb(162, 101, 224, 219) : Color.FromArgb(138, 163, 174, 191);
            var pen = new Pen(new SolidColorBrush(color), highlighted ? 1.8 : 1);
            var radius = item.Radius * .01f;
            var half = item.Length * .005f;
            Point Project(Vector3 v) => view.Screen(Vector3.Transform(v, transform));
            for (var side = -1; side <= 1; side += 2)
            {
                var ring = local + axis * (half * side);
                Point Ring(float angle) => Project(ring + (tangent * MathF.Cos(angle) + bitangent * MathF.Sin(angle)) * radius);
                for (var i = 0; i < 20; i++) drawing.DrawLine(pen, Ring(i * MathF.Tau / 20), Ring((i + 1) * MathF.Tau / 20));
                for (var rail = 0; rail < 4; rail++)
                {
                    var radial = rail switch { 0 => tangent, 1 => bitangent, 2 => -tangent, _ => -bitangent };
                    Point Meridian(float angle) => Project(ring + (radial * MathF.Cos(angle) + axis * (side * MathF.Sin(angle))) * radius);
                    for (var i = 0; i < 6; i++) drawing.DrawLine(pen, Meridian(i * MathF.PI / 12), Meridian((i + 1) * MathF.PI / 12));
                    if (side > 0) drawing.DrawLine(pen, Project(ring + radial * radius), Project(local - axis * half + radial * radius));
                }
            }
            var middle = Project(local);
            if (!selected || !view.canDragCapsule || view.placeCapsule)
            {
                var size = selected && view.placeCapsule ? 4 : 2;
                drawing.DrawEllipse(pen.Brush, null, middle, size, size);
                return;
            }
            for (var i = 0; i < 3; i++)
            {
                if (view.rotateCapsule)
                {
                    var ring = view.MakeRotationRing(transform, local, item.Rotation, i);
                    var ringPen = new Pen(new SolidColorBrush(RotationColors[i]), 2);
                    var previous = ring.At(0);
                    for (var segment = 1; segment <= RingSegments; segment++)
                    {
                        var next = ring.At(segment * RingStep);
                        drawing.DrawLine(ringPen, previous, next);
                        previous = next;
                    }
                    continue;
                }
                var direction = view.AxisPixels(transform, local, i);
                if (direction.Length < .15) continue;
                var end = middle + direction * (36 / direction.Length);
                var brush = new SolidColorBrush(AxisColors[i]);
                drawing.DrawLine(new Pen(brush, 2), middle, end);
                drawing.DrawEllipse(brush, null, end, 4, 4);
            }
        }
    }
}
