using System.Buffers.Binary;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Controls;
using WWPoser;
using WWPoser.Services;

internal static class PhysicsEditorUiChecks
{
    internal static int RunDraft(MainWindow window, string output, OwnedCharacterSelection character, Action<Task> wait)
    {
        object? Call(string name, params object?[] arguments) => typeof(MainWindow).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, arguments);
        T Field<T>(string name) => UiSessionFixture.Read<T>(window, name);
        void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        Call("ShowPage", 3);
        var skirt = Field<TreeView>("physicsTree").Items.Cast<TreeViewItem>().SelectMany(source => source.Items.Cast<TreeViewItem>())
            .Single(row => Equals(row.Header, "裙摆 · 12"));
        skirt.IsSelected = true;
        Check(Field<Button>("physicsSavePreset").IsEnabled && Field<Button>("physicsLoadPreset").IsEnabled &&
              Field<StackPanel>("physicsDetails").Children.OfType<Slider>().Count(slider => slider.IsEnabled) == 3, "idle editor enables the existing sliders and preset files without a game");
        var gravityToggle = Field<StackPanel>("physicsDetails").Children.OfType<CheckBox>().Single(check => Equals(check.Content, "自定义向下重力"));
        Check(gravityToggle.IsEnabled && gravityToggle.IsChecked == false, "physics group gravity starts with the original value");
        wait((Task)Call("EditPhysicsParameterAsync", "native_gravity", 250d)!);
        Check(gravityToggle.IsChecked == true && Field<StackPanel>("physicsDetails").Children.OfType<Slider>()
            .Single(slider => Equals(slider.ToolTip, "重力强度使用游戏原生数值")).Value == 250, "offline gravity enables and exposes its staged strength");
        wait((Task)Call("EditPhysicsParameterAsync", "native_damping", .37)!);
        skirt.Items.Cast<TreeViewItem>().Single(row => Equals(row.Header, "Bone_Skirt001_L")).IsSelected = true;
        Check(Field<StackPanel>("physicsDetails").Children.OfType<TextBlock>().Any(label => label.Text == "继承整组预设 0.37"),
            "chain inherits the offline group draft");
        wait((Task)Call("EditPhysicsParameterAsync", "native_limit_angle", 24d)!);
        var captured = (PlaybackSessionRequest)Call("CapturePlaybackRequest")!;
        var first = captured.Actors[0];
        var draft = first.PhysicsPreset!;
        Check(draft["groups"]?.AsArray().Count == 2 && draft["gravity"]?[0]?["strength"]?.GetValue<double>() == 250 &&
            !draft.ToJsonString().Contains("group_id"), "draft includes group gravity keyed by stable names");
        var roundTrip = JsonSerializer.Deserialize<PlaybackSessionRequest>(JsonSerializer.Serialize(captured))!;
        Check(JsonNode.DeepEquals(roundTrip.Actors[0].PhysicsPreset, draft), "show and preparation requests round-trip physics presets");
        var directory = Path.Combine(output, "offline-physics-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var draftFile = Path.Combine(directory, "lily.wwphysics");
        JsonData.Write(draftFile, draft);
        wait((Task)Call("EditPhysicsParameterAsync", "native_limit_angle", -1d)!);
        wait((Task)Call("LoadPhysicsPresetFileAsync", draftFile)!);
        Check(Field<StackPanel>("physicsDetails").Children.OfType<TextBlock>().Any(label => label.Text == "预设值 24"), "loading a standalone file updates the idle inspector");

        var otherDraft = NativePhysicsPreset.Create(character, []);
        var targetGroup = new PhysicsGroup("fixture", "node", "Kawaii_alpha_S03",
            [new("Bone_Skirt001_L", "裙摆", []), new("Bone_Skirt001_R", "裙摆", [])], null, null, null);
        NativePhysicsPreset.SetDraftValue(otherDraft, targetGroup, null, 0, .12);
        var request = captured with { Actors = [first with { Slot = 4 }, first with { Slot = 9, PhysicsPreset = otherDraft }] };
        JsonObject Settings() => new() { ["requested"] = new JsonArray(-1d, -1d, -1d) };
        JsonObject Actor(uint slot, uint group, uint chain) => new() { ["slot"] = slot, ["native_physics_settings_verified"] = true,
            ["native_physics_groups"] = new JsonArray(new JsonObject { ["group_id"] = group, ["curve_name"] = "Kawaii_alpha_S03",
                ["root_bones"] = new JsonArray("Bone_Skirt001_L", "Bone_Skirt001_R"), ["settings"] = Settings(), ["gravity"] = new JsonObject { ["requested"] = -1d },
                ["chains"] = new JsonArray(new JsonObject { ["chain_id"] = chain, ["root"] = "Bone_Skirt001_L", ["settings"] = Settings() }) }) };
        var state = new JsonObject { ["clip_epoch"] = 114ul, ["last_sequence"] = 0ul, ["last_control_sequence"] = 0ul, ["last_control_code"] = 0u,
            ["session_active"] = true,             ["mode"] = "paused", ["seconds"] = 0d, ["actors"] = new JsonArray(Actor(4, 17, 6)) };
        var statusPath = Path.Combine(directory, "mmd_status.json");
        var commandPath = Path.Combine(directory, "control.dat");
        JsonData.Write(statusPath, state);
        var service = Field<PlaybackSessionService>("player");
        try { wait(service.ApplyLaunchPhysicsAsync(directory, request, state, CancellationToken.None)); throw new Exception("missing second actor accepted"); }
        catch (InvalidDataException) { Check(!File.Exists(commandPath), "all startup presets resolve before the first native edit"); }

        state["actors"]!.AsArray().Add(Actor(9, 21, 3));
        JsonData.Write(statusPath, state);
        UiSessionFixture.Own(window, directory, request, JsonData.ReadObject(statusPath));
        var commands = new List<(uint Kind, uint Slot, uint Group, uint Chain, double Value)>();
        var ack = Task.Run(async () =>
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            ulong seen = 0;
            while (commands.Count < 5)
            {
                if (File.Exists(commandPath))
                {
                    var bytes = new byte[PlaybackCommands.WireBytes];
                    using (var file = new FileStream(commandPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) file.ReadExactly(bytes);
                    var sequence = BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(24));
                    if (sequence > seen)
                    {
                        seen = sequence;
                        commands.Add((BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(32)), BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(36)),
                            BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(48)), BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(52)), BinaryPrimitives.ReadDoubleLittleEndian(bytes.AsSpan(40))));
                        var status = JsonData.ReadObject(statusPath);
                        status["last_sequence"] = sequence;
                        status["last_control_sequence"] = sequence;
                        JsonData.Write(statusPath, status);
                    }
                }
                await Task.Delay(5, deadline.Token);
            }
        });
        try
        {
            wait(service.ApplyLaunchPhysicsAsync(directory, request, state, CancellationToken.None));
            wait(service.CompletePlaybackStartAsync(new JsonObject { ["loaded"] = true, ["start_paused"] = true }, null, new()));
            wait(ack);
            Check(commands.SequenceEqual(new[] { (19u, 4u, 17u, 0u, .37), (21u, 4u, 17u, 6u, 24d), (28u, 4u, 17u, 0u, 250d), (19u, 9u, 21u, 0u, .12), (1u, 0u, 0u, 0u, 0d) }),
                "startup remaps both actors and applies individual settings before play without a recorder");
        }
        finally { UiSessionFixture.Own(window, null); Call("UpdateLiveActors"); Call("UpdateActions"); }
        wait((Task)Call("EditPhysicsParameterAsync", "native_limit_angle", -1d)!);
        skirt.IsSelected = true;
        wait((Task)Call("EditPhysicsParameterAsync", "native_damping", -1d)!);
        wait((Task)Call("EditPhysicsParameterAsync", "native_gravity", -1d)!);
        Check(((PlaybackSessionRequest)Call("CapturePlaybackRequest")!).Actors[0].PhysicsPreset is null, "reset removes the last authored override");

        var topLevel = Field<TreeView>("physicsTree").Items.Cast<TreeViewItem>().ToArray();
        Check(topLevel.All(row => !row.Header!.ToString()!.StartsWith("部件 ·") && !Equals(row.Header, "骨链部件重力")),
            "part gravity uses native groups and chains as the only tree entries");
        var sleeveGroup = topLevel.SelectMany(row => row.Items.Cast<TreeViewItem>()).Single(row => Equals(row.Header, "双袖 · 8"));
        var leftSleeve = sleeveGroup.Items.Cast<TreeViewItem>().Single(row => Equals(row.Header, "左袖 · Bone_Piao201_L"));
        leftSleeve.IsSelected = true;
        var highlighted = Field<HashSet<int>>("physicsHighlighted");
        var bones = new PhysicsPartCatalog(Field<string>("root")).Read(character).Bones;
        Check(highlighted.Select(index => bones[index].Name).ToHashSet().IsSupersetOf(["Bone_Piao201_L", "Bone_Piao209_L", "Bone_Piao216_L", "Bone_Piao219_L", "Bone_Piao221_L"]) &&
            highlighted.All(index => !bones[index].Name.EndsWith("_R", StringComparison.Ordinal)), "the part selects the left main and edge bones only");
        wait((Task)Call("EditPhysicsParameterAsync", "part_gravity", 980d)!);
        var partDraft = ((PlaybackSessionRequest)Call("CapturePlaybackRequest")!).Actors[0].PhysicsPreset!;
        var livePart = new JsonObject { ["root"] = "Bone_Piao201_L", ["id"] = 7u, ["strength"] = -1d };
        Check(NativePhysicsPreset.Plan(partDraft, character, [], [livePart]).SequenceEqual(
            new[] { new NativePhysicsPreset.Control("part_gravity", 980, 0, 7) }), "part preset routes by stable root independently of native groups");
        var rightSleeve = sleeveGroup.Items.Cast<TreeViewItem>().Single(row => Equals(row.Header, "右袖 · Bone_Piao201_R"));
        rightSleeve.IsSelected = true;
        wait((Task)Call("EditPhysicsParameterAsync", "part_gravity", 650d)!);
        sleeveGroup.IsSelected = true;
        Check(Field<StackPanel>("physicsDetails").Children.OfType<CheckBox>().Single(check => Equals(check.Content, "部件重力")).IsChecked is null,
            "native sleeve group shows the mixed side values");
        highlighted = Field<HashSet<int>>("physicsHighlighted");
        Check(highlighted.Select(index => bones[index].Name).ToHashSet().IsSupersetOf(["Bone_Piao219_L", "Bone_Piao219_R"]),
            "native sleeve group highlights both proximal cloth regions in the same selection");
        wait((Task)Call("EditPhysicsParameterAsync", "part_gravity", 980d)!);
        partDraft = ((PlaybackSessionRequest)Call("CapturePlaybackRequest")!).Actors[0].PhysicsPreset!;
        var rightPart = new JsonObject { ["root"] = "Bone_Piao201_R", ["id"] = 8u, ["strength"] = -1d };
        Check(NativePhysicsPreset.Plan(partDraft, character, [], [livePart, rightPart]).SequenceEqual(
            new[] { new NativePhysicsPreset.Control("part_gravity", 980, 0, 7), new NativePhysicsPreset.Control("part_gravity", 980, 0, 8) }),
            "one edit on the native group sets both sleeves with the same preset value");
        wait((Task)Call("EditPhysicsParameterAsync", "part_gravity", -1d)!);
        Check(((PlaybackSessionRequest)Call("CapturePlaybackRequest")!).Actors[0].PhysicsPreset is null, "restoring part gravity removes its preset override");
        return 17;
    }

    internal static int Run(MainWindow window, string output, OwnedCharacterSelection character,
        PhysicsPartProfile profile, Action<Task> wait, Action<string> render)
    {
        const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic;
        object? Call(string name, params object?[] args) => typeof(MainWindow).GetMethod(name, Members)!.Invoke(window, args);
        T Field<T>(string name) => UiSessionFixture.Read<T>(window, name);
        int checks = 0;
        void Check(bool value, string text) { if (!value) throw new Exception(text); checks++; }
        var request = (PlaybackSessionRequest)Call("CapturePlaybackRequest")!;
        var directory = Path.Combine(output, "physics-ui-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        var statusPath = Path.Combine(directory, "mmd_status.json");
        var commandPath = Path.Combine(directory, "control.dat");
        var previewPositions = new JsonArray(profile.Bones.Select(bone => (JsonNode)new JsonArray(bone.Position.X, bone.Position.Y, bone.Position.Z)).ToArray());
        var settings = new JsonObject { ["requested"] = new JsonArray(.05, -1d, 65d), ["original"] = new JsonArray(.2, .2, 50d),
            ["effective_first"] = new JsonArray(.05, .2, 65d), ["effective_last"] = new JsonArray(.05, .2, 65d) };
        var gravity = new JsonObject { ["requested"] = 250d, ["original"] = new JsonArray(0d, 0d, 0d), ["effective"] = new JsonArray(0d, 0d, -250d) };
        var group = new JsonObject { ["group_id"] = 7u, ["curve_name"] = "Kawaii_alpha_S03", ["settings"] = settings, ["gravity"] = gravity,
            ["root_bones"] = new JsonArray("Bone_Skirt001_L", "Bone_Skirt001_R") };
        var sleeveGroup = new JsonObject { ["group_id"] = 5u, ["curve_name"] = "Kawaii_alpha_S08",
            ["settings"] = new JsonObject { ["requested"] = new JsonArray(-1d, -1d, -1d) },
            ["root_bones"] = new JsonArray("Bone_Piao201_L", "Bone_Piao201_R") };
        var sleeveParts = new JsonArray(
            new JsonObject { ["id"] = 7u, ["root"] = "Bone_Piao201_L", ["strength"] = -1d },
            new JsonObject { ["id"] = 8u, ["root"] = "Bone_Piao201_R", ["strength"] = -1d });
        var actor = new JsonObject { ["slot"] = request.Actors[0].Slot, ["native_physics_settings_verified"] = true,
            ["native_physics_groups"] = new JsonArray(group, sleeveGroup), ["gravity_parts"] = sleeveParts,
            ["native_preview"] = new JsonObject { ["group_id"] = 7u, ["frame"] = 12ul,
                ["solved_bones"] = 10, ["positions"] = previewPositions } };
        var state = new JsonObject { ["clip_epoch"] = 833ul, ["last_sequence"] = 0ul, ["last_control_sequence"] = 0ul, ["last_control_code"] = 0u,
            ["session_active"] = true,             ["mode"] = "paused", ["seconds"] = 2d, ["duration_seconds"] = 20d, ["preview_loop_start"] = 1.25d,
            ["actors"] = new JsonArray(actor) };
        JsonData.Write(statusPath, state);
        UiSessionFixture.Own(window, directory, request, JsonData.ReadObject(statusPath));
        Call("UpdateLiveActors");
        Call("ShowPage", 3);
        var tree = Field<TreeView>("physicsTree");
        var skirt = tree.Items.Cast<TreeViewItem>().SelectMany(source => source.Items.Cast<TreeViewItem>())
            .Single(row => Equals(row.Header, "裙摆 · 12"));
        skirt.IsSelected = true;
        Check(Field<TextBlock>("physicsPreviewHeading").Text.Contains("已发布姿态"), "published skeletal frames reach the actual physics viewport");
        Check(Field<Button>("physicsSavePreset").IsEnabled && Field<Button>("comparePhysics").IsEnabled &&
            request.Actors[0].CharacterId == character.CharacterId, "the live inspector binds to the selected actor and skin");
        render("physics-editor-live-fixture");

        var commands = new List<(uint Kind, uint Slot, uint Group, double Value)>();
        var partCommands = new List<(uint Slot, uint Group, uint Part, double Value)>();
        var acknowledge = Task.Run(async () => {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            ulong last = 0;
            while (commands.Count < 10 || partCommands.Count < 6)
            {
                if (File.Exists(commandPath))
                {
                    var bytes = new byte[PlaybackCommands.WireBytes];
                    using (var f = new FileStream(commandPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) f.ReadExactly(bytes);
                    var sequence = BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(24));
                    if (sequence > last)
                    {
                        last = sequence;
                        var kind = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(32));
                        var value = BinaryPrimitives.ReadDoubleLittleEndian(bytes.AsSpan(40));
                        var slot = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(36));
                        var groupId = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(48));
                        if (kind == 29)
                        {
                            var part = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(52));
                            partCommands.Add((slot, groupId, part, value));
                            var rejected = part == 8 && value == 720;
                            if (!rejected) sleeveParts.Single(row => row!["id"]!.GetValue<uint>() == part)!["strength"] = value;
                            state["last_control_code"] = rejected ? 1u : 0u;
                        }
                        else
                        {
                            commands.Add((kind, slot, groupId, value));
                            if (kind is 19 or 21) settings["requested"]![kind == 19 ? 0 : 2] = value;
                            if (kind == 28) gravity["requested"] = value;
                            state["last_control_code"] = 0u;
                        }
                        state["last_sequence"] = sequence;
                        state["last_control_sequence"] = sequence;
                        JsonData.Write(statusPath, state);
                    }
                }
                await Task.Delay(5, deadline.Token);
            }
        });
        try
        {
            wait((Task)Call("ComparePhysicsAsync")!);
            UiSessionFixture.Own(window, directory, request, JsonData.ReadObject(statusPath));
            wait((Task)Call("ComparePhysicsAsync")!);
            UiSessionFixture.Own(window, directory, request, JsonData.ReadObject(statusPath));
            var bothSleeves = tree.Items.Cast<TreeViewItem>().SelectMany(source => source.Items.Cast<TreeViewItem>())
                .Single(row => Equals(row.Header, "双袖 · 8"));
            bothSleeves.IsSelected = true;
            wait((Task)Call("EditPhysicsParameterAsync", "part_gravity", 980d)!);
            UiSessionFixture.Own(window, directory, request, JsonData.ReadObject(statusPath));
            wait((Task)Call("EditPhysicsParameterAsync", "part_gravity", 720d)!);
            wait(acknowledge);
            var slot = request.Actors[0].Slot;
            Check(commands.SequenceEqual(new[] { (19u,slot,7u,-1d), (21u,slot,7u,-1d), (28u,slot,7u,-1d), (3u,0u,0u,1.25), (1u,0u,0u,0d),
                (19u,slot,7u,.05), (21u,slot,7u,65d), (28u,slot,7u,250d), (3u,0u,0u,1.25), (1u,0u,0u,0d) }),
                "A/B resets and reapplies the selected actor's parameters then replays the same loop start");
            Check(partCommands.SequenceEqual(new[] { (slot,0u,7u,980d), (slot,0u,8u,980d), (slot,0u,7u,720d), (slot,0u,8u,720d),
                (slot,0u,8u,980d), (slot,0u,7u,980d) }) && sleeveParts.All(row => row!["strength"]!.GetValue<double>() == 980),
                "the native sleeve group edits both scoped parts and restores them if the second edit is rejected");
        }
        finally { UiSessionFixture.Own(window, null); Call("UpdateLiveActors"); Call("UpdateActions"); }
        Check(Field<TextBlock>("physicsPreviewHeading").Text.StartsWith("参考骨架", StringComparison.Ordinal), "closing the session releases the live preview");
        return checks;
    }
}
