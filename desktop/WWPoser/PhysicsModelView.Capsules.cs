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
    private sealed record CapsuleDrag(CapsuleKey Key, int Axis, Point Start, Vector ScreenDirection, double PixelsPerCm, float Original)
    {
        public float Value { get; set; } = Original;
    }

    private readonly CapsuleOverlay capsuleOverlay;
    private readonly Dictionary<int, Matrix4x4> capsuleBind = [];
    private IReadOnlyList<CapsuleViewItem> visibleCapsules = [];
    private PhysicsModelPose? capsulePose;
    private CapsuleKey? selectedCapsule;
    private CapsuleDrag? capsuleDrag;
    private bool canDragCapsule;
    private static readonly Vector3[] LocalAxes = [Vector3.UnitX, Vector3.UnitZ, Vector3.UnitY];
    private static readonly Color[] AxisColors = [Colors.Tomato, Colors.LightGreen, Colors.DeepSkyBlue];
    public event Action<CapsuleKey>? CapsulePicked;
    public event Action<CapsuleKey, int, double>? CapsuleDragged;

    public void SetCapsules(IReadOnlyList<CapsuleViewItem> items, CapsuleKey? selected, bool editable)
    {
        if (!editable || selected != selectedCapsule) StopCapsuleDrag(false);
        visibleCapsules = items;
        selectedCapsule = selected;
        canDragCapsule = editable;
        instructions.Text = items.Count > 0 ? "点击胶囊 · 拖动彩色轴调整偏移 · 滚轮缩放" : "拖动旋转 · 滚轮缩放 · 右键拖动平移";
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

    internal static Vector3 CapsuleAxis(Vector3 rotation)
    {
        // Unreal's rotator is pitch/yaw/roll in degrees and its capsule axis is +Z.
        var half = rotation * (MathF.PI / 360);
        var (sp, cp) = MathF.SinCos(half.X);
        var (sy, cy) = MathF.SinCos(half.Y);
        var (sr, cr) = MathF.SinCos(half.Z);
        var q = new Quaternion(cr * sp * sy - sr * cp * cy, -cr * sp * cy - sr * cp * sy,
            cr * cp * sy - sr * sp * cy, cr * cp * cy + sr * sp * sy);
        var axis = Vector3.Transform(Vector3.UnitZ, q);
        return new(axis.X, axis.Z, axis.Y);
    }

    private Matrix4x4? CapsuleTransform(int bone) => bone < 0 ? Matrix4x4.Identity :
        capsulePose is { } pose && bone < pose.Transforms.Length ? pose.Transforms[bone] :
        capsuleBind.TryGetValue(bone, out var transform) ? transform : null;

    private Vector3 LocalCenter(CapsuleViewItem item)
    {
        var offset = item.Offset;
        if (capsuleDrag is { } drag && drag.Key == item.Key)
            offset = drag.Axis switch { 0 => offset with { X = drag.Value }, 1 => offset with { Y = drag.Value }, _ => offset with { Z = drag.Value } };
        return new Vector3(offset.X, offset.Z, offset.Y) * .01f;
    }

    private Point Screen(Vector3 position)
    {
        var right = Vector3D.CrossProduct(camera.LookDirection, camera.UpDirection); right.Normalize();
        var up = Vector3D.CrossProduct(right, camera.LookDirection); up.Normalize();
        var move = position - center;
        var v = new Vector3D(move.X, move.Y, move.Z);
        var pixels = ActualWidth / Math.Max(.001, camera.Width);
        return new(ActualWidth / 2 + Vector3D.DotProduct(v, right) * pixels, ActualHeight / 2 - Vector3D.DotProduct(v, up) * pixels);
    }

    private static double PointDistance(Point point, Point start, Point end)
    {
        var line = end - start;
        var amount = line.LengthSquared < .0001 ? 0 : Math.Clamp(Vector.Multiply(point - start, line) / line.LengthSquared, 0, 1);
        return (point - (start + line * amount)).Length;
    }

    private Vector AxisPixels(Matrix4x4 transform, Vector3 local, int axis) =>
        Screen(Vector3.Transform(local + LocalAxes[axis] * .01f, transform)) - Screen(Vector3.Transform(local, transform));

    private bool SelectOrDragCapsule(Point point)
    {
        foreach (var item in visibleCapsules.Where(item => item.Key == selectedCapsule && canDragCapsule))
        {
            if (CapsuleTransform(item.Bone) is not { } transform) continue;
            var local = LocalCenter(item);
            var centerPoint = Screen(Vector3.Transform(local, transform));
            var handles = Enumerable.Range(0, 3).Select(axis => (Axis: axis, Direction: AxisPixels(transform, local, axis)))
                .Where(axis => axis.Direction.Length >= .15).Select(axis =>
                {
                    var distance = PointDistance(point, centerPoint + axis.Direction * (12 / axis.Direction.Length),
                        centerPoint + axis.Direction * (36 / axis.Direction.Length));
                    return (axis.Axis, axis.Direction, Distance: distance);
                }).OrderBy(axis => axis.Distance).ToArray();
            if (handles.Length > 0 && handles[0].Distance <= 8)
            {
                var handle = handles[0];
                capsuleDrag = new(item.Key, handle.Axis, point, handle.Direction / handle.Direction.Length,
                    handle.Direction.Length, item.Offset[handle.Axis]);
                CaptureMouse(); return true;
            }
        }
        CapsuleViewItem? picked = null;
        double nearest = double.PositiveInfinity;
        foreach (var item in visibleCapsules)
        {
            if (CapsuleTransform(item.Bone) is not { } transform) continue;
            var local = LocalCenter(item);
            var extent = CapsuleAxis(item.Rotation) * item.Length * .005f;
            var start = Screen(Vector3.Transform(local - extent, transform));
            var end = Screen(Vector3.Transform(local + extent, transform));
            var distance = PointDistance(point, start, end);
            var radius = item.Radius * .01 * ActualWidth / Math.Max(.001, camera.Width);
            if (distance <= Math.Max(7, radius) && distance < nearest) { nearest = distance; picked = item; }
        }
        if (picked is null) return false;
        CapsulePicked?.Invoke(picked.Key);
        return true;
    }

    private bool MoveCapsuleDrag(Point point)
    {
        if (capsuleDrag is not { } drag) return false;
        drag.Value = Math.Clamp(drag.Original + (float)(Vector.Multiply(point - drag.Start, drag.ScreenDirection) / drag.PixelsPerCm), -300, 300);
        capsuleOverlay.InvalidateVisual(); return true;
    }

    private bool StopCapsuleDrag(bool commit)
    {
        if (capsuleDrag is not { } drag) return false;
        capsuleDrag = null;
        capsuleOverlay.InvalidateVisual();
        if (commit && Math.Abs(drag.Value - drag.Original) >= .02) CapsuleDragged?.Invoke(drag.Key, drag.Axis + 2, Math.Round(drag.Value, 3));
        return true;
    }

    private sealed class CapsuleOverlay(PhysicsModelView view) : FrameworkElement
    {
        protected override void OnRender(DrawingContext drawing)
        {
            if (view.model is null) return;
            foreach (var item in view.visibleCapsules.OrderBy(item => item.Key == view.selectedCapsule))
            {
                if (view.CapsuleTransform(item.Bone) is not { } transform) continue;
                DrawCapsule(drawing, item, transform);
            }
        }

        private void DrawCapsule(DrawingContext drawing, CapsuleViewItem item, Matrix4x4 transform)
        {
            var local = view.LocalCenter(item);
            var axis = CapsuleAxis(item.Rotation);
            var tangent = Vector3.Normalize(Vector3.Cross(axis, Math.Abs(axis.Y) < .9f ? Vector3.UnitY : Vector3.UnitX));
            var bitangent = Vector3.Cross(axis, tangent);
            var selected = item.Key == view.selectedCapsule;
            var color = selected ? Color.FromRgb(255, 194, 102) : item.Active ? Color.FromArgb(162, 101, 224, 219) : Color.FromArgb(138, 163, 174, 191);
            var pen = new Pen(new SolidColorBrush(color), selected ? 1.8 : 1);
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
            if (!selected) { drawing.DrawEllipse(pen.Brush, null, middle, 2, 2); return; }
            for (var i = 0; i < 3; i++)
            {
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
