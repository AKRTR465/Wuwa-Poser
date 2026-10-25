using System.Buffers.Binary;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using WWPoser.Services;

var assertions = 0;
void Check(bool value, string message) { if (!value) throw new Exception(message); assertions++; }
static void Save(string path, JsonObject value) { var next = path + ".next"; File.WriteAllText(next, value.ToJsonString()); File.Move(next, path, true); }
static JsonObject Timeline(string mode = "playing") => new() { ["clip_epoch"] = 123UL, ["last_sequence"] = 0UL, ["session_active"] = true, ["mode"] = mode, ["seconds"] = 2d, ["duration_seconds"] = 10d, ["speed"] = 1d, ["loop"] = false, ["restore_requested"] = false };
var fixture = Path.Combine(AppContext.BaseDirectory, "fixtures-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(fixture);
var localWorkspace = Path.Combine(fixture, "workspace");
Directory.CreateDirectory(Path.Combine(localWorkspace, "desktop", "WWPoser")); Directory.CreateDirectory(Path.Combine(localWorkspace, "data", "characters"));
File.WriteAllText(Path.Combine(localWorkspace, "desktop", "WWPoser", "WWPoser.csproj"), "<Project />"); File.WriteAllText(Path.Combine(localWorkspace, "data", "characters", "index.json"), "{}");
var sharedRecordings = Path.Combine(localWorkspace, "playback", "recordings");
Check(RecordingService.DefaultOutputDirectory(localWorkspace) == sharedRecordings, "source workspace records into its own playback directory");
foreach (var localBuild in new[] { Path.Combine(localWorkspace, "dist", "version-a"), Path.Combine(localWorkspace, "dist", "version-b"), Path.Combine(localWorkspace, "desktop", "WWPoser", "publish") })
    Check(RecordingService.DefaultOutputDirectory(localBuild) == sharedRecordings, "local builds and publish share recordings across versions");
var standalone = Path.Combine(Path.GetTempPath(), "wwposer-portable-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(Path.Combine(standalone, "data", "characters")); File.WriteAllText(Path.Combine(standalone, "data", "characters", "index.json"), "{}");
Save(Path.Combine(standalone, "manifest.json"), new JsonObject { ["source_root"] = localWorkspace });
Check(RecordingService.DefaultOutputDirectory(standalone) == Path.Combine(standalone, "playback", "recordings"), "standalone portable recordings stay local even with a stale source workspace in the manifest");
var wire = PlaybackControl.Encode(123, 45, "time_limit", 0, 3);
Check(wire.Length == 64 && BinaryPrimitives.ReadUInt32LittleEndian(wire.AsSpan(32)) == 16 && BinaryPrimitives.ReadUInt32LittleEndian(wire.AsSpan(36)) == 3, "wire fields");
uint checksum = 2166136261; foreach (var b in wire.AsSpan(0, 60)) checksum = unchecked((checksum ^ b) * 16777619);
Check(BinaryPrimitives.ReadUInt32LittleEndian(wire.AsSpan(60)) == checksum, "wire checksum");
foreach (var slot in new uint[] { 4, 17, 40, 4096, uint.MaxValue }) Check(BinaryPrimitives.ReadUInt32LittleEndian(PlaybackControl.Encode(123, 46, "offset_x", 12, slot).AsSpan(36)) == slot, "dynamic actor slot roundtrip");
var single = Path.Combine(fixture, "single"); Directory.CreateDirectory(single); Save(Path.Combine(single, "player_status.json"), Timeline());
Check(PlaybackSessionStatus.Read(single).State["seconds"]!.GetValue<double>() == 2, "single timeline");
var squad = Path.Combine(fixture, "squad"); Directory.CreateDirectory(squad); Save(Path.Combine(squad, "stage_config.json"), new());
var squadTimeline = Timeline(); squadTimeline["mode"] = 2;
Save(Path.Combine(squad, "stage_status.json"), new JsonObject { ["mmd_squad"] = squadTimeline });
Check(PlaybackSessionStatus.Read(squad).State["mode"]!.GetValue<string>() == "playing", "squad timeline");
var owned = Path.Combine(fixture, "owned"); Directory.CreateDirectory(owned); Save(Path.Combine(owned, "owned_mmd_config.json"), new()); Save(Path.Combine(owned, "mmd_status.json"), Timeline());
Check(PlaybackSessionStatus.Read(owned).State["source_kind"]!.GetValue<string>() == "created_character", "owned timeline");
var desired = MediaService.DesiredAudio(PlaybackSessionStatus.Read(owned).State, .5, 30000);
Check(desired.PositionMilliseconds == 1500 && desired.Playing, "owned music");
// No real audio device is opened. Reproduce MCI's already-closed alias errors
// through the same device shutdown path used by the synchronization worker.
foreach (var missingDeviceCode in new[] { 257, 263 })
{
    var closes = 0;
    var device = new MediaService.MciDevice(command => {
        if (command.StartsWith("close ")) { closes++; throw new Win32Exception(missingDeviceCode, "device already closed"); }
        return command.EndsWith(" length") ? "10000" : "";
    });
    device.Open("offline.wav"); device.Close(); device.Dispose();
    Check(closes == 1, "closing a missing MCI alias is idempotent");
}
{
    var closes = 0;
    var device = new MediaService.MciDevice(command => {
        if (command.StartsWith("close ")) { closes++; throw new Win32Exception(263, "device already gone after failed open"); }
        if (command.StartsWith("set ")) throw new Win32Exception(296, "unsupported time format");
        return "";
    });
    try { device.Open("offline.wav"); throw new Exception("partial MCI open failure was lost"); }
    catch (Win32Exception error) { Check(error.NativeErrorCode == 296, "partial open preserves its original error"); }
    device.Dispose(); Check(closes == 1, "partial open retires the invalid alias before later disposal");
}
{
    var opened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var closes = 0;
    var audio = new MediaService(() => new MediaService.MciDevice(command => {
        if (command.StartsWith("open ")) opened.TrySetResult();
        if (command.StartsWith("close ")) { closes++; throw new Win32Exception(263, "device already closed"); }
        return command.EndsWith(" length") ? "10000" : command.EndsWith(" position") ? "2000" : command.EndsWith(" mode") ? "paused" : "";
    }));
    for (var attempt = 0; attempt < 2; attempt++)
    {
        opened = new(TaskCreationOptions.RunContinuationsAsynchronously);
        audio.UpdateContext(single, new MusicPreset(Path.Combine(fixture, "offline.wav")));
        await audio.StartSynchronizationAsync(); await opened.Task.WaitAsync(TimeSpan.FromSeconds(5)); await audio.StopAsync(); await audio.StopAsync();
    }
    Check(closes == 2, "audio workers stop and restart after their MCI aliases disappear");
}
{
    var opened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var audio = new MediaService(() => new MediaService.MciDevice(command => {
        if (command.StartsWith("open ")) opened.TrySetResult();
        if (command.StartsWith("close ")) throw new Win32Exception(5, "real audio shutdown failure");
        return command.EndsWith(" length") ? "10000" : command.EndsWith(" position") ? "2000" : command.EndsWith(" mode") ? "paused" : "";
    }));
    audio.UpdateContext(single, new MusicPreset(Path.Combine(fixture, "offline.wav")));
    await audio.StartSynchronizationAsync(); await opened.Task.WaitAsync(TimeSpan.FromSeconds(5));
    try { await audio.StopAsync(); throw new Exception("real audio error was swallowed"); }
    catch (Win32Exception error) { Check(error.NativeErrorCode == 5, "unexpected audio close errors remain visible"); }
    await audio.StopAsync(); Check(true, "a reported audio fault does not poison later stop or window close attempts");
}
var commandPath = Path.Combine(single, "control.dat"); var commands = new List<uint>();
var emulator = Task.Run(async () => { ulong previous = 0; while (commands.Count < 2) { if (File.Exists(commandPath)) { var bytes = File.ReadAllBytes(commandPath); var sequence = BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(24)); if (sequence != previous) { previous = sequence; commands.Add(BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(32))); var state = Timeline(); state["last_sequence"] = sequence; Save(Path.Combine(single, "player_status.json"), state); } } await Task.Delay(5); } });
await Task.WhenAll(PlaybackControl.SendAsync(single, "speed", 2), PlaybackControl.SendAsync(single, "loop", 1), emulator).WaitAsync(TimeSpan.FromSeconds(10));
Check(commands.SequenceEqual(new uint[] { 6, 5 }), "serialized command acknowledgement");
var actor = new JsonObject { ["target_slot"] = 1, ["enabled"] = true, ["profile_id"] = "test", ["local_clip_sha256"] = "body-a" };
Save(Path.Combine(squad, "stage_config.json"), new JsonObject { ["mmd_squad"] = new JsonObject { ["members"] = new JsonArray(actor), ["camera_clip_sha256"] = "camera-a" } });
var bookmark = Path.Combine(fixture, "pose.wwpose"); BookmarkService.Snapshot(squad, 3.25, bookmark);
Check(BookmarkService.SeekTime(bookmark, squad) == 3.25, "squad bookmark roundtrip");
actor["local_clip_sha256"] = "body-b"; Save(Path.Combine(squad, "stage_config.json"), new JsonObject { ["mmd_squad"] = new JsonObject { ["members"] = new JsonArray(actor.DeepClone()), ["camera_clip_sha256"] = "camera-a" } });
try { BookmarkService.SeekTime(bookmark, squad); throw new Exception("bookmark accepted another clip"); } catch (InvalidDataException) { }
var vmd = Path.Combine(fixture, "input.vmd"); File.WriteAllText(vmd, "offline input snapshot");
foreach (var count in new[] { 4, 17, 40 })
{
    var slotted = PlaybackSessionService.AssignSlots(new(PlaybackSource.MixedActors, Enumerable.Range(0, count).Select(index => new PlaybackActorRequest(vmd, CharacterId: "same-character", Enabled: index != 1)).ToArray()));
    Check(slotted.Actors.Count == count && slotted.Actors.Select(actor => actor.Slot).Distinct().Count() == count, "dynamic preparation assigns a unique slot to every card");
    Check(!slotted.Actors[1].Enabled && slotted.Actors[1].Slot == 2 && slotted.Actors[2].Slot == 3, "disabled actor retains its slot without renumbering later actors");
}
var sparse = PlaybackSessionService.AssignSlots(new(PlaybackSource.MixedActors, [new(vmd, CharacterId: "same", Slot: 4096), new(vmd, CharacterId: "same"), new(vmd, CharacterId: "same", Slot: 17)]));
Check(sparse.Actors.Select(actor => actor.Slot).SequenceEqual(new uint[] { 4096, 1, 17 }), "non-contiguous supplied slots are preserved");
try { PlaybackSessionService.AssignSlots(new(PlaybackSource.MixedActors, [new(vmd, Slot: 17), new(vmd, Slot: 17)])); throw new Exception("duplicate actor slot accepted"); } catch (InvalidDataException) { assertions++; }
var mixed = Path.Combine(fixture, "mixed"); Directory.CreateDirectory(mixed);
var mixedConfig = new JsonObject { ["schema"] = 3, ["native_physics"] = true, ["actors"] = new JsonArray(sparse.Actors.Select(actor => (JsonNode)new JsonObject { ["slot"] = actor.Slot, ["target_slot"] = actor.Slot, ["character_id"] = actor.CharacterId, ["profile_id"] = "profile", ["local_clip_sha256"] = "body-a", ["expression_clip_sha256"] = "face-a", ["native_physics"] = actor.Slot != 1 }).ToArray()) };
var mixedTimeline = Timeline(); mixedTimeline["actors"] = mixedConfig["actors"]!.DeepClone();
Save(Path.Combine(mixed, "owned_mmd_config.json"), mixedConfig); Save(Path.Combine(mixed, "mmd_status.json"), mixedTimeline);
Check(PlaybackSessionStatus.Read(mixed).State["source_kind"]!.GetValue<string>() == "mixed_actors", "dynamic status uses one mixed source");
var routes = OwnedMmdPlaybackService.NativeTargetRoutes(mixedConfig, mixed);
Check(routes.Select(route => route.Slot).SequenceEqual(new uint[] { 4096, 17 }) && routes.All(route => route.TargetConfigPath.EndsWith(Path.Combine("native_targets", route.Slot.ToString(), "native_target.json"))), "native observations route by slot and selected physics backend");
BookmarkService.Snapshot(mixed, 2.5, bookmark); Check(BookmarkService.SeekTime(bookmark, mixed) == 2.5, "mixed multi-actor bookmark roundtrip");
mixedConfig["actors"]![2]!["expression_clip_sha256"] = "changed-face"; Save(Path.Combine(mixed, "owned_mmd_config.json"), mixedConfig);
try { BookmarkService.SeekTime(bookmark, mixed); throw new Exception("bookmark accepted changed actor face"); } catch (InvalidDataException) { assertions++; }
Save(Path.Combine(mixed, "session_result.json"), new JsonObject { ["mmd_restoration_verified"] = true, ["managed_owner_retirement_checked"] = false });
Check(!OwnedMmdPlaybackService.RestorationConfirmed(mixed), "native restoration alone does not retire borrowed and created actors");
Save(Path.Combine(mixed, "session_result.json"), new JsonObject { ["mmd_restoration_verified"] = true, ["managed_owner_retirement_checked"] = true });
await new OwnedMmdPlaybackService(fixture).FinishSessionAsync(mixed);
Check(OwnedMmdPlaybackService.RestorationConfirmed(mixed), "completed restoration supports an idempotent retry");
Console.WriteLine("PASS: arbitrary actor slots; mixed routing/status/music/bookmarks; legacy status reading; serialized commands; restoration retry. No game or GUI execution.");
if (args.Length >= 2 && args[0] is "--prepare-actors" or "--prepare-owned")
{
    void Vmd(string path, bool face)
    {
        using var file = File.Create(path); using var writer = new BinaryWriter(file);
        void Text(string value, int length) { var bytes = new byte[length]; Encoding.ASCII.GetBytes(value).CopyTo(bytes, 0); writer.Write(bytes); }
        Text("Vocaloid Motion Data 0002", 30); Text("UnifiedCheck", 20); writer.Write(0u);
        writer.Write(face ? 2u : 0u);
        if (face) foreach (var frame in new uint[] { 0, 150 }) { Text("A", 15); writer.Write(frame); writer.Write(frame == 0 ? 0f : 1f); }
        writer.Write(face ? 0u : 2u);
        if (!face) foreach (var frame in new uint[] { 0, 90 }) { writer.Write(frame); writer.Write(-45f); writer.Write(0f); writer.Write(10f); writer.Write(0f); writer.Write(0f); writer.Write(0f); writer.Write(0f); for (var i = 0; i < 6; i++) writer.Write(new byte[] { 20, 20, 107, 107 }); writer.Write(45u); writer.Write((byte)0); }
        writer.Write(0u); writer.Write(0u); writer.Write(0u);
    }
    var facePath = Path.Combine(fixture, "face-5s.vmd"); Vmd(facePath, true);
    var cameraPath = Path.Combine(fixture, "camera-3s.vmd"); Vmd(cameraPath, false);
    var bodyPath = Path.Combine(fixture, "body-2s.vmd");
    using (var file = File.Create(bodyPath))
    using (var writer = new BinaryWriter(file))
    {
        var header = new byte[30]; Encoding.ASCII.GetBytes("Vocaloid Motion Data 0002").CopyTo(header, 0); writer.Write(header); writer.Write(new byte[20]); writer.Write(2u);
        foreach (var frame in new uint[] { 0, 60 })
        {
            var name = new byte[15]; new byte[] { 0x83, 0x5a, 0x83, 0x93, 0x83, 0x5e, 0x81, 0x5b }.CopyTo(name, 0); writer.Write(name); writer.Write(frame);
            writer.Write(0f); writer.Write(frame == 0 ? 0f : 1f); writer.Write(0f); writer.Write(0f); writer.Write(0f); writer.Write(0f); writer.Write(1f);
            for (var i = 0; i < 16; i++) writer.Write(new byte[] { 20, 20, 107, 107 });
        }
        for (var i = 0; i < 5; i++) writer.Write(0u);
    }
    var selectedCharacter = OwnedCharacterCatalog.Load(args[1]).Select().CharacterId;
    var service = new PlaybackSessionService(args[1]);
    var previewConfig = new OwnedMmdPlaybackService(args[1]).PreviewConfig(new PlaybackSessionRequest(PlaybackSource.MixedActors,
        [new PlaybackActorRequest("", CharacterId: selectedCharacter, Stage: new(-120, 340, 25, 90)), new PlaybackActorRequest("", CharacterId: selectedCharacter), new PlaybackActorRequest("", CharacterId: selectedCharacter, Enabled: false)]));
    var previewDirectory = Path.Combine(AppContext.BaseDirectory, "preview-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(previewDirectory);
    OwnedMmdPlaybackService.WriteActorsFile(previewDirectory, previewConfig);
    var previewRows = File.ReadAllLines(Path.Combine(previewDirectory, "actors.tsv")).Select(line => line.Split('	')).ToArray();
    var previewGrid = OwnedMmdPlaybackService.GridPosition(1, 2, 180);
    Check(previewConfig["preview"]!.GetValue<bool>() && previewRows.Length == 2 && previewRows.All(row => row.Length == 16 && row[6] != "" && row[15] == "1"), "preview stands enabled actors only, checks exact meshes, and never borrows team actors");
    Check(previewRows[0][3..6].SequenceEqual(["-120", "340", "25"]) && previewRows[0][7] == "90", "preview uses the chosen stage position");
    Check(double.Parse(previewRows[1][3], System.Globalization.CultureInfo.InvariantCulture) == previewGrid.X && double.Parse(previewRows[1][4], System.Globalization.CultureInfo.InvariantCulture) == previewGrid.Y, "preview uses the automatic grid otherwise");
    Check(File.ReadAllText(Path.Combine(previewDirectory,"stage_floor.tsv")) == "capsule_bottom\t2.5\n", "ordinary stage keeps its original capsule-bottom reference");
    OwnedMmdPlaybackService.ConfigureStage(previewConfig,180,true); OwnedMmdPlaybackService.WriteActorsFile(previewDirectory,previewConfig);
    Check(File.ReadAllText(Path.Combine(previewDirectory,"stage_floor.tsv")) == "walkable_floor\t2.5\n" && OwnedMmdPlaybackService.ActorConfigs(previewConfig).All(actor => actor["stage_z_cm"]!.GetValue<double>() == 0), "original formation requests floor correction in the motion layer without shifting actors");
    Directory.Delete(previewDirectory, true);
    string? lastPreparation = null;
    string? firstPreparation = null;
    foreach (var count in new[] { 4, 17, 40 })
    {
        var actorRequests = Enumerable.Range(0, count).Select(index => new PlaybackActorRequest(bodyPath, facePath, Scale: index == 0 ? .75 : null, Placement: new(OffsetX: index * 2),
            CharacterId: selectedCharacter, Slot: count == 17 ? (uint)(index * 4 + 1) : 0)).ToArray();
        var request = new PlaybackSessionRequest(PlaybackSource.MixedActors, actorRequests, cameraPath, 2, Speed: 3, NativePhysics: false, Physics: true);
        var preparation = await service.PrepareAsync(request); var output = preparation["session_dir"]!.GetValue<string>();
        lastPreparation = output;
        firstPreparation ??= output;
        var cfg = OwnedMmdPlaybackService.Config(output); var preparedActors = OwnedMmdPlaybackService.ActorConfigs(cfg);
        Check(preparedActors.Count == count && preparedActors.Select(actor => actor["slot"]!.GetValue<uint>()).Distinct().Count() == count, "actual dynamic preparation has no actor cap and unique repeated-character slots");
        Check(preparedActors.Select(actor => actor["actor_directory"]!.GetValue<string>()).Distinct().Count() == count, "same-role actors have independent asset directories");
        Check(preparedActors.All(actor => actor["native_physics"]!.GetValue<bool>() == false && actor["physics_enabled"]!.GetValue<bool>()), "per-actor physics options survive preparation");
        Check(preparedActors.All(actor => actor["expression_enabled"]!.GetValue<bool>() && actor["expression_duration_seconds"]!.GetValue<double>() == 5 && File.Exists(Path.Combine(output, actor["actor_directory"]!.GetValue<string>(), "expression.wwexpr"))), "every actor binds its independently addressable expression track");
        Check(cfg["camera_clip_enabled"]!.GetValue<bool>() && cfg["camera_clip_duration_seconds"]!.GetValue<double>() == 3 && cfg["duration_seconds"]!.GetValue<double>() == 5, "one camera and a longest-track shared timeline");
        var actorLines = File.ReadAllLines(Path.Combine(output, "actors.tsv")); var actorBytes = File.ReadAllBytes(Path.Combine(output, "actors.tsv"));
        Check(actorLines.Length == count && actorLines.All(line => line.Split('\t').Length == 16) && !actorBytes.AsSpan().StartsWith(new byte[] { 0xef, 0xbb, 0xbf }), "managed actor list is complete and UTF8 without BOM");
        Check(actorLines.Select(line => uint.Parse(line.Split('\t')[0])).SequenceEqual(preparedActors.Select(actor => actor["slot"]!.GetValue<uint>())), "managed and native slot mapping agrees");
        var adjusted = request with { Speed = 3.5, Loop = true, DurationSeconds = 120, SpacingCm = 220, Actors = actorRequests.Select(actor => actor with { Placement = new(OffsetX: 100) }).ToArray() };
        Check(!service.RequiresPreparation(output, adjusted), "changing runtime settings and placement reuses immutable clips");
        service.UpdatePreparation(output, adjusted);
        Check(service.ReadPreparation(output).Speed == 3.5 && OwnedMmdPlaybackService.ActorConfigs(OwnedMmdPlaybackService.Config(output)).All(actor => actor["placement"]!["offset_x_cm"]!.GetValue<double>() == 100), "updated settings cover all actors rather than just slot one");
        Check(service.RequiresPreparation(output, request with { Actors = actorRequests.Select((actor, index) => index == count - 1 ? actor with { Scale = .5 } : actor).ToArray() }), "changing any actor scale requires a new compilation");
        var nativeConfig = OwnedMmdPlaybackService.Config(output); foreach (var nativeActor in OwnedMmdPlaybackService.ActorConfigs(nativeConfig)) nativeActor["native_physics"] = true;
        Check(OwnedMmdPlaybackService.NativeTargetRoutes(nativeConfig, output).Count == count, "native preparation includes every dynamic actor");
        Console.WriteLine($"PASS: actual cached compilation/preparation for {count} repeated-character actors: {output}");
    }
    assertions += await CharacterFormChecks.RunAsync(args[1], bodyPath, facePath);
    var disabledRequest = new PlaybackSessionRequest(PlaybackSource.MixedActors, [new(bodyPath, CharacterId: selectedCharacter, Slot: 7), new("missing-disabled-input.vmd", Enabled: false, Slot: 9), new(bodyPath, CharacterId: selectedCharacter, Slot: 41)], NativePhysics: false);
    var disabledPrep = await service.PrepareAsync(disabledRequest); var disabledDirectory = disabledPrep["session_dir"]!.GetValue<string>();
    Check(OwnedMmdPlaybackService.ActorConfigs(OwnedMmdPlaybackService.Config(disabledDirectory)).Select(actor => actor["slot"]!.GetValue<uint>()).SequenceEqual(new uint[] { 7, 41 }), "disabled cards are not loaded and later actors retain non-contiguous slots");
    Check(service.ReadPreparation(disabledDirectory).Actors.Count == 3, "saved requests retain disabled cards for later editing");
    using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
    try { await service.PrepareAsync(disabledRequest, cancelled.Token); throw new Exception("cancelled preparation completed"); } catch (OperationCanceledException) { assertions++; }
    Check(service.ActiveSession is null, "cancelled offline preparation does not acquire runtime ownership");
    File.Move(bodyPath, bodyPath + ".moved"); File.Move(facePath, facePath + ".moved"); File.Move(cameraPath, cameraPath + ".moved");
    var restoredRequest = service.ReadPreparation(lastPreparation!);
    Check(restoredRequest.Actors.Count == 40 && restoredRequest.Actors[0].Scale == .75 && restoredRequest.Actors.Select(actor => actor.Slot).Distinct().Count() == 40, "loading saved material preserves actor count scale and slots");
    Check(restoredRequest.Actors.All(actor => File.Exists(actor.BodySource) && File.Exists(actor.FaceSource)) && File.Exists(restoredRequest.CameraSource), "prepared materials remain usable after all external source files move");
    Check(!service.RequiresPreparation(lastPreparation!, restoredRequest), "identical snapshotted bytes reuse compiled clips without the old source paths");
    // Exercise runtime file isolation without launching anything: fake package
    // files and a synthetic GameIdentity are used only by the file-copy method.
    var fakePackage = Path.Combine(fixture, "build", "owned_mmd"); Directory.CreateDirectory(fakePackage);
    foreach (var file in new[] { "ownedmmd_lite_observer.dll", "ownedmmd_lite_installer.exe", OwnedMmdPlaybackService.HelperFilename, "owned_mmd_layout.json" }) File.WriteAllText(Path.Combine(fakePackage, file), "offline package fixture");
    var isolation = new OwnedMmdPlaybackService(fixture);
    foreach (var sourceFile in Directory.EnumerateFiles(Path.Combine(args[1], "data", "characters"), "*", SearchOption.AllDirectories))
    {
        var destination = Path.Combine(fixture, "data", "characters", Path.GetRelativePath(Path.Combine(args[1], "data", "characters"), sourceFile));
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!); File.Copy(sourceFile, destination);
    }
    var prepareRuntime = typeof(OwnedMmdPlaybackService).GetMethod("PrepareRuntime", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
    var sourceConfig = OwnedMmdPlaybackService.Config(firstPreparation!); sourceConfig["camera_controller_address"] = "0xdead"; sourceConfig["native_graph_binding"] = new JsonObject { ["stale"] = true };
    Save(Path.Combine(firstPreparation!, "owned_mmd_config.json"), sourceConfig); File.WriteAllText(Path.Combine(firstPreparation!, "owned_mmd_graph.json"), "stale identity fixture");
    var fakeGame = new GameIdentity(42, 123456, "0x1234", 567);
    var snapshotA = (string)prepareRuntime.Invoke(isolation, [firstPreparation!, fakeGame])!;
    var snapshotB = (string)prepareRuntime.Invoke(isolation, [firstPreparation!, fakeGame])!;
    var snapshotConfigA = OwnedMmdPlaybackService.Config(snapshotA); var snapshotConfigB = OwnedMmdPlaybackService.Config(snapshotB);
    Check(snapshotA != snapshotB && snapshotConfigA["session_epoch"]!.GetValue<ulong>() != snapshotConfigB["session_epoch"]!.GetValue<ulong>() && snapshotConfigA["session_epoch"]!.GetValue<ulong>() != sourceConfig["session_epoch"]!.GetValue<ulong>(), "every runtime snapshot has a fresh directory and session epoch");
    Check(snapshotConfigA["camera_controller_address"] is null && snapshotConfigA["native_graph_binding"] is null && !File.Exists(Path.Combine(snapshotA, "owned_mmd_graph.json")), "runtime snapshots never import prepared controller or actor graph identities");
    Check(OwnedMmdPlaybackService.ActorConfigs(snapshotConfigA).Count == 4 && OwnedMmdPlaybackService.ActorConfigs(snapshotConfigA).All(actor => File.Exists(Path.Combine(snapshotA, actor["actor_directory"]!.GetValue<string>(), "motion.wwlocal"))), "one runtime snapshot retains all actor assets");
    Check(File.ReadAllLines(Path.Combine(snapshotA, "actors.tsv")).Length == 4 && isolation.ActiveSession is null, "runtime file preparation alone does not launch or claim actors");
}
if (args.Length >= 2 && args[0] == "--audio") assertions += await RecordingAudioChecks.RunAsync(args[1], fixture);
var captureIndex = Array.IndexOf(args, "--capture");
if (captureIndex >= 0) assertions += await RecordingCaptureChecks.RunAsync(args[captureIndex + 1], fixture);
Console.WriteLine($"PASS: {assertions} assertions.");
