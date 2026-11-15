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
            .Single(slider => slider.Maximum == NativePhysicsPreset.GravityMaximum).Value == 250, "offline gravity enables and exposes its staged strength");
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
        var state = new JsonObject { ["clip_epoch"] = 114ul, ["last_sequence"] = 0ul, ["session_active"] = true,
            ["mode"] = "paused", ["seconds"] = 0d, ["actors"] = new JsonArray(Actor(4, 17, 6)) };
        var statusPath = Path.Combine(directory, "player_status.json");
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
                    var bytes = new byte[64];
                    using (var file = new FileStream(commandPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) file.ReadExactly(bytes);
                    var sequence = BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(24));
                    if (sequence > seen)
                    {
                        seen = sequence;
                        commands.Add((BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(32)), BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(36)),
                            BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(48)), BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(52)), BinaryPrimitives.ReadDoubleLittleEndian(bytes.AsSpan(40))));
                        var status = JsonData.ReadObject(statusPath);
                        status["last_sequence"] = sequence;
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
        return 10;
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
        var statusPath = Path.Combine(directory, "player_status.json");
        var commandPath = Path.Combine(directory, "control.dat");
        var previewPositions = new JsonArray(profile.Bones.Select(bone => (JsonNode)new JsonArray(bone.Position.X, bone.Position.Y, bone.Position.Z)).ToArray());
        var settings = new JsonObject { ["requested"] = new JsonArray(.05, -1d, 65d), ["original"] = new JsonArray(.2, .2, 50d),
            ["effective_first"] = new JsonArray(.05, .2, 65d), ["effective_last"] = new JsonArray(.05, .2, 65d) };
        var gravity = new JsonObject { ["requested"] = 250d, ["original"] = new JsonArray(0d, 0d, 0d), ["effective"] = new JsonArray(0d, 0d, -250d) };
        var group = new JsonObject { ["group_id"] = 7u, ["curve_name"] = "Kawaii_alpha_S03", ["settings"] = settings, ["gravity"] = gravity,
            ["root_bones"] = new JsonArray("Bone_Skirt001_L", "Bone_Skirt001_R") };
        var actor = new JsonObject { ["slot"] = request.Actors[0].Slot, ["native_physics_settings_verified"] = true,
            ["native_physics_groups"] = new JsonArray(group), ["native_preview"] = new JsonObject { ["group_id"] = 7u, ["frame"] = 12ul,
                ["solved_bones"] = 10, ["positions"] = previewPositions } };
        var state = new JsonObject { ["clip_epoch"] = 833ul, ["last_sequence"] = 0ul, ["session_active"] = true,
            ["mode"] = "paused", ["seconds"] = 2d, ["duration_seconds"] = 20d, ["preview_loop_start"] = 1.25d,
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
        var acknowledge = Task.Run(async () => {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            ulong last = 0;
            while (commands.Count < 10)
            {
                if (File.Exists(commandPath))
                {
                    var bytes = new byte[64];
                    using (var f = new FileStream(commandPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) f.ReadExactly(bytes);
                    var sequence = BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(24));
                    if (sequence > last)
                    {
                        last = sequence;
                        var kind = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(32));
                        var value = BinaryPrimitives.ReadDoubleLittleEndian(bytes.AsSpan(40));
                        commands.Add((kind, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(36)), BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(48)), value));
                        if (kind is 19 or 21) settings["requested"]![kind == 19 ? 0 : 2] = value;
                        if (kind == 28) gravity["requested"] = value;
                        state["last_sequence"] = sequence; JsonData.Write(statusPath, state);
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
            wait(acknowledge);
            var slot = request.Actors[0].Slot;
            Check(commands.SequenceEqual(new[] { (19u,slot,7u,-1d), (21u,slot,7u,-1d), (28u,slot,7u,-1d), (3u,0u,0u,1.25), (1u,0u,0u,0d),
                (19u,slot,7u,.05), (21u,slot,7u,65d), (28u,slot,7u,250d), (3u,0u,0u,1.25), (1u,0u,0u,0d) }),
                "A/B resets and reapplies the selected actor's parameters then replays the same loop start");
        }
        finally { UiSessionFixture.Own(window, null); Call("UpdateLiveActors"); Call("UpdateActions"); }
        Check(Field<TextBlock>("physicsPreviewHeading").Text.StartsWith("参考骨架", StringComparison.Ordinal), "closing the session releases the live preview");
        return checks;
    }
}
