using System.Buffers.Binary;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using WWPoser;
using WWPoser.Services;

internal static class PhysicsModelUiChecks
{
    internal static int Run(MainWindow window, string root, string output, string cache, Action<Task> wait, Action<string> render)
    {
        var catalog = OwnedCharacterCatalog.Load(root);
        var parts = new PhysicsPartCatalog(root);
        var loaded = 0;
        var missing = new List<string>();
        foreach (var actor in catalog.Characters)
        {
            if (PhysicsPreviewModel.Load(cache, actor, parts.Read(actor).Bones) is null) missing.Add(actor.CharacterId);
            else loaded++;
        }
        Console.WriteLine($"Physics model cache: {loaded}/{catalog.Characters.Count} selections decoded; missing: {string.Join(", ", missing)}");

        T Field<T>(string name) => UiSessionFixture.Read<T>(window, name);
        object? Call(string name, params object?[] args) => typeof(MainWindow).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, args);
        var original = Field<ComboBox>("physicsActor").SelectedItem!;
        var picker = UiSessionFixture.Read<CharacterPicker>(original, "Character");
        var previous = picker.SelectedItem;
        var character = catalog.Select("role1204-skin81001204");
        // Exercise the editor page against the same cache loader as the portable build.
        typeof(MainWindow).GetField("physicsModelCache", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, cache);
        picker.SelectedItem = character;
        Call("ShowPage", 3);
        wait(WaitFor(() => !Field<bool>("modelLoading")));
        var view = Field<PhysicsModelView>("physicsModelView");
        Check(view.Visibility == Visibility.Visible && Field<TextBlock>("physicsPreviewHeading").Text.Contains("3D 模型"), "real model is the default viewport");
        var model = Field<PhysicsPreviewModel>("physicsModel");
        var camera = UiSessionFixture.Read<OrthographicCamera>(view, "camera");
        var start = camera.Position;
        var width = camera.Width;
        view.Orbit(44, 8); view.Zoom(120);
        Check(camera.Position != start && camera.Width < width, "view drag and wheel alter the 3D camera");
        var groupRow = Field<TreeView>("physicsTree").Items.Cast<TreeViewItem>().SelectMany(source => source.Items.Cast<TreeViewItem>())
            .First(group => group.Items.Cast<TreeViewItem>().Any(row => Equals(row.Header, "Piao_L_xiuZiA01")));
        groupRow.IsExpanded = true;
        var sleeve = groupRow.Items.Cast<TreeViewItem>().First(row => Equals(row.Header, "Piao_L_xiuZiA01"));
        sleeve.IsSelected = true;
        Check(Field<int>("highlightedModelTriangles") > 30 && Field<int>("highlightedModelTriangles") < 1500,
            "left sleeve selection colors a bounded patch in the real mesh");
        render("physics-3d-mortefi");

        var profile = parts.Read(character);
        var bones = Enumerable.Repeat(Matrix4x4.Identity, profile.Bones.Count).ToArray();
        foreach (var joint in model.Meshes[0].Joints)
        {
            if (!Matrix4x4.Invert(joint.InverseBind, out bones[joint.RuntimeBone])) throw new Exception("singular fixture bind matrix");
        }
        var directory = Path.Combine(output, "physics-model-pose");
        Directory.CreateDirectory(directory);
        var wire = new byte[32 + bones.Length * 40];
        "WWPPOS1\0"u8.CopyTo(wire);
        BinaryPrimitives.WriteUInt32LittleEndian(wire.AsSpan(8), 1);
        BinaryPrimitives.WriteInt32LittleEndian(wire.AsSpan(12), bones.Length);
        BinaryPrimitives.WriteUInt64LittleEndian(wire.AsSpan(16), 19);
        BinaryPrimitives.WriteUInt64LittleEndian(wire.AsSpan(24), 22);
        void WritePose()
        {
            for (var i = 0; i < bones.Length; i++)
            {
                if (!Matrix4x4.Decompose(bones[i], out var s, out var q, out var t)) throw new Exception("cannot convert fixture bind matrix");
                float[] values = [t.X * 100, t.Z * 100, t.Y * 100, q.X, q.Z, q.Y, -q.W, s.X, s.Z, s.Y];
                for (var k = 0; k < values.Length; k++) BinaryPrimitives.WriteSingleLittleEndian(wire.AsSpan(32 + i * 40 + k * 4), values[k]);
            }
            File.WriteAllBytes(Path.Combine(directory, "model_pose.dat"), wire);
        }
        WritePose();
        Check(PhysicsModelPose.Read(directory, 2, 19, bones.Length, 0) is null && PhysicsModelPose.Read(directory, 1, 18, bones.Length, 0) is null &&
            PhysicsModelPose.Read(directory, 1, 19, bones.Length, 22) is null, "wire frames from other actors, subscriptions or old frames are rejected");
        var baseline = PhysicsModelView.Deform(model, null)[0].Positions;
        var bind = PhysicsModelView.Deform(model, PhysicsModelPose.Read(directory, 1, 19, bones.Length, 0) ?? throw new Exception("no bind pose"))[0].Positions;
        Check(baseline.Zip(bind).Max(row => (row.First - row.Second).Length) < .0002, "UE component bind pose round-trips through the GLB coordinate transform");
        var chain = profile.Groups.SelectMany(group => group.Chains).First(group => group.Root == "Piao_L_xiuZiA01");
        foreach (var i in chain.Bones) bones[i] *= Matrix4x4.CreateTranslation(.1f, 0, 0);
        WritePose();
        var live = PhysicsModelView.Deform(model, PhysicsModelPose.Read(directory, 1, 19, bones.Length, 0) ?? throw new Exception("no adjusted pose"));
        var changed = baseline.Zip(live[0].Positions).Select(row => (row.First - row.Second).Length).ToArray();
        Check(changed.Max() > .09 && changed.Count(delta => delta > .001) < 1000, "pose changes deform only vertices weighted to the selected sleeve");
        view.Apply(live);
        render("physics-3d-mortefi-sleeve");

        picker.SelectedItem = previous;
        Call("ShowPage", 0);
        return 6;

        static void Check(bool result, string message) { if (!result) throw new Exception(message); }
    }

    internal static int ReplayLive(MainWindow window, string root, string output, string frames, Action<Task> wait, Action<string> render)
    {
        var catalog = OwnedCharacterCatalog.Load(root);
        T Field<T>(string name) => UiSessionFixture.Read<T>(window, name);
        object? Call(string name, params object?[] args) => typeof(MainWindow).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, args);
        Call("ShowPage", 3);
        var picker = UiSessionFixture.Read<CharacterPicker>(Field<ComboBox>("physicsActor").SelectedItem!, "Character");
        var previous = picker.SelectedItem;
        foreach (var label in new[] { "lily", "cartethyia" })
        {
            var evidence = JsonData.ReadObject(Path.Combine(frames, label + "-model-evidence.json"));
            var actor = catalog.Select(evidence["role"]!.GetValue<string>());
            var bone = evidence["root"]!.GetValue<string>();
            var liveReport = JsonData.ReadObject(Path.Combine(frames, label + "-live.json"));
            var nativeStatus = JsonData.ReadObject(Path.Combine(liveReport["session_dir"]!.GetValue<string>(), "mmd_status.json"));
            nativeStatus["session_active"] = true;
            nativeStatus["restore_requested"] = false;
            nativeStatus["mode"] = "paused";
            nativeStatus["seconds"] = .43;
            var stock = File.ReadAllBytes(Path.Combine(frames, label + "-stock.pose"));
            var adjusted = File.ReadAllBytes(Path.Combine(frames, label + "-adjusted.pose"));
            var stockFrame = BinaryPrimitives.ReadUInt64LittleEndian(stock.AsSpan(24));
            var adjustedFrame = BinaryPrimitives.ReadUInt64LittleEndian(adjusted.AsSpan(24));
            var directory = Path.Combine(output, "model-replay-" + label + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var statusFile = Path.Combine(directory, "mmd_status.json");
            var poseFile = Path.Combine(directory, "model_pose.dat");
            var commandFile = Path.Combine(directory, "control.dat");
            JsonData.Write(statusFile, nativeStatus);
            picker.SelectedItem = actor;
            Call("ShowPage", 3);
            wait(WaitFor(() => !Field<bool>("modelLoading")));
            var request = (PlaybackSessionRequest)Call("CapturePlaybackRequest")!;
            UiSessionFixture.Own(window, directory, request, JsonData.ReadObject(statusFile));
            Call("UpdateLiveActors");
            var group = Field<TreeView>("physicsTree").Items.Cast<TreeViewItem>().SelectMany(row => row.Items.Cast<TreeViewItem>())
                .First(row => row.Items.Cast<TreeViewItem>().Any(chain => Equals(chain.Header, bone)));
            group.IsExpanded = true;
            group.Items.Cast<TreeViewItem>().First(chain => Equals(chain.Header, bone)).IsSelected = true;
            var view = Field<PhysicsModelView>("physicsModelView");
            view.SetView(2);

            var acknowledgement = Task.Run(async () =>
            {
                ulong last = 0;
                for (var accepted = 0; accepted < 2; accepted++)
                {
                    byte[]? command = null;
                    await WaitFor(() =>
                    {
                        try
                        {
                            using var input = new FileStream(commandFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                            var candidate = new byte[PlaybackCommands.WireBytes];
                            input.ReadExactly(candidate);
                            if (BinaryPrimitives.ReadUInt64LittleEndian(candidate.AsSpan(24)) == last) return false;
                            command = candidate;
                            return true;
                        }
                        catch (IOException) { return false; }
                    });
                    last = BinaryPrimitives.ReadUInt64LittleEndian(command!.AsSpan(24));
                    var kind = BinaryPrimitives.ReadUInt32LittleEndian(command.AsSpan(32));
                    var slot = BinaryPrimitives.ReadUInt32LittleEndian(command.AsSpan(36));
                    var value = BinaryPrimitives.ReadDoubleLittleEndian(command.AsSpan(40));
                    if (kind != 27 || slot != 1 || value != 1 - accepted) throw new Exception("invalid viewport subscription command");
                    nativeStatus["last_control_sequence"] = last;
                    nativeStatus["last_control_code"] = 0u;
                    nativeStatus["model_preview_revision"] = last;
                    nativeStatus["model_preview_slot"] = value == 1 ? 1u : 0u;
                    if (value == 1)
                    {
                        BinaryPrimitives.WriteUInt64LittleEndian(stock.AsSpan(16), last);
                        BinaryPrimitives.WriteUInt64LittleEndian(adjusted.AsSpan(16), last);
                        JsonData.AtomicWrite(poseFile, stock);
                    }
                    JsonData.Write(statusFile, nativeStatus);
                }
            });
            try
            {
                wait(WaitFor(() => Field<ulong>("displayedModelFrame") == stockFrame));
                if (Field<int>("highlightedModelTriangles") <= 0 || !Field<TextBlock>("physicsPreviewHeading").Text.StartsWith("实时 3D 模型"))
                    throw new Exception(label + " native pose missing in editor or selected part has no highlighted surface");
                var geometry = UiSessionFixture.Read<(MeshGeometry3D Base, MeshGeometry3D Highlight)[]>(view, "geometry");
                var start = geometry[0].Base.Positions;
                render("physics-live-" + label + "-stock");
                JsonData.AtomicWrite(poseFile, adjusted);
                wait(WaitFor(() => Field<ulong>("displayedModelFrame") == adjustedFrame));
                var marked = geometry[0].Highlight.TriangleIndices.Distinct().ToArray();
                var delta = marked.Max(i => (geometry[0].Base.Positions[i] - start[i]).Length) * 100;
                if (delta < .1) throw new Exception(label + " highlighted geometry did not deform after native edit");
                render("physics-live-" + label + "-adjusted");
                Console.WriteLine($"{label} runtime frame replay: {marked.Length} highlighted vertices, {delta:F2} cm maximum change");
                Call("ShowPage", 0);
                wait(acknowledgement);
            }
            finally { UiSessionFixture.Own(window, null); Call("UpdateLiveActors"); Call("ShowPage", 0); }
        }
        picker.SelectedItem = previous;
        return 8;
    }

    private static async Task WaitFor(Func<bool> predicate)
    {
        for (var i = 0; i < 420 && !predicate(); i++) await Task.Delay(15);
        if (!predicate()) throw new TimeoutException("model preview check did not receive the expected state");
    }
}
