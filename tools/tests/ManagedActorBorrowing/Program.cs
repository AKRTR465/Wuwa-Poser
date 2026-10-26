using System.Text.Json.Nodes;
using UnrealEngine;
using WwPoser.ColdLoad;

var assertions = 0;
void Check(bool condition, string label) { if (!condition) throw new Exception(label); assertions++; }
void Throws(Action action, string label) { try { action(); } catch (InvalidOperationException) { assertions++; return; } throw new Exception(label); }
Environment.SetEnvironmentVariable("WWPOSER_EXPECTED_COMPAT_PROFILE", GameCompat.ProfileId);
Environment.SetEnvironmentVariable("WWPOSER_EXPECTED_COMPAT_SOURCE_SHA256", "outdated-helper-digest");
Throws(ManagedBridge.VerifyCompatibility, "a retained helper with another source cannot pass the native handshake");
Throws(ManagedBridge.BindSession, "source mismatch is rejected before session acquisition");
Environment.SetEnvironmentVariable("WWPOSER_EXPECTED_COMPAT_SOURCE_SHA256", GameCompat.SourceSha256);
Environment.SetEnvironmentVariable("WWPOSER_EXPECTED_COMPAT_PROFILE", "another-profile");
Throws(ManagedBridge.VerifyCompatibility, "profile mismatch cannot pass the native handshake");
Environment.SetEnvironmentVariable("WWPOSER_EXPECTED_COMPAT_PROFILE", GameCompat.ProfileId);
ManagedBridge.VerifyCompatibility(); Check(true, "matching generated helper/native sources pass before actor access");
var root = Path.Combine(AppContext.BaseDirectory, "fixtures-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
var group = new SceneTeamGroup(); var player = new SceneTeamPlayer(); player.GroupMap.Add(ETeamGroupType.Default, group); SceneTeamModel._Instance.TeamPlayerMap.Add(7, player);
(ACharacter Actor, USkeletalMeshComponent Mesh, EntityHandle Handle) TeamActor(int role, int skin, FVectorDouble location)
{
    var actor = new ACharacter { Location = location }; var mesh = new USkeletalMeshComponent { SkeletalMesh = new() { Name = $"Role{role}Skin{skin}" } }; actor.Mesh = mesh;
    var index = CharacterModel._Instance.Handles.Count; var entity = new WorldEntity { Index = index, Id = (index << 16) | 1 };
    entity.Components.Add(typeof(CharacterActorComponent), new CharacterActorComponent { ActorInternal = actor, SkeletalMesh = mesh }); entity.Components.Add(typeof(CreatureDataComponent), new CreatureDataComponent { SkinId = skin, RoleId = role, PlayerId = 7, CreatureDataId = 10000 + role });
    var handle = new EntityHandle { Id = entity.Id, Index = index, Entity = entity, CreatureDataId = 10000 + role };
    CharacterModel._Instance.Handles.Add(handle); ObjectSystem._instance.Objects.Add(entity); ObjectSystem._instance.Versions.Add(1);
    group.RoleList.Add(new() { RoleId = role, CreatureDataId = handle.CreatureDataId }); SceneTeamModel._Instance.TeamItems.Add(new() { ConfigId = role, CreatureDataId = handle.CreatureDataId, EntityHandleInternal = handle });
    return (actor, mesh, handle);
}
var first = TeamActor(10, 100, new(1000, 2000, 90)); var second = TeamActor(20, 200, new(8000, -5000, -1000)); var wrong = TeamActor(30, 300, new(9000, 5000, -1000));
UGameplayStatics.Pawn = first.Actor; first.Actor.bHidden = false;
var slots = new uint[] { 5, 2, 900, 17, 80000, 70000, 30, 45 };
var requested = new (int Role, int Skin)[] { (10,100), (10,100), (20,200), (30,999), (40,400), (40,400), (50,500), (60,600) };
File.WriteAllLines(Path.Combine(root, "actors.tsv"), requested.Select((actor, i) => $"{slots[i]}\t{actor.Role}\t{actor.Skin}\t{(i + 1) * 100}\t0\t0\tRole{actor.Role}Skin{actor.Skin}\t0"));
Environment.SetEnvironmentVariable("WWPOSER_OWNED_SESSION_DIR", root); ManagedBridge.BindSession(); ManagedBridge.BeginTarget();
var manager = UiSceneRoleActorManager._instance;
Check(manager.Created.Count == 6, "eight actors should borrow two and create six");
Check(first.Handle.HoldEntityMap.Count == 1 && second.Handle.HoldEntityMap.Count == 1 && wrong.Handle.HoldEntityMap.Count == 0, "exact role/skin/asset borrowing and one hold per actor");
ManagedBridge.CheckLoaded(); manager.Created[0].FailLocations = 1;
Throws(ManagedBridge.ShowAtLocal, "partial show must report placement failure");
Check(first.Actor.Location.Y == 2100, "first borrowed actor moves once");
ManagedBridge.ShowAtLocal(); ManagedBridge.CheckLoaded();
Check(!first.Actor.bHidden && first.Mesh.bVisible && !first.Mesh.bHiddenInGame && first.Handle.HoldEntityMap.Count == 1, "a participating controlled instance stays visible without a spectator visibility hold");
Check(manager.Created[0].Location.Y == 2200 && second.Actor.Location.Y == 2300, "retry must retain original stage anchor");
Check(first.Actor.Location.Z == 90 && manager.Created[0].Location.Z == 0 && second.Actor.Location.Z == 90, "borrowed capsule and created model feet align");
Check(second.Mesh.VisibilityBasedAnimTickOption == EVisibilityBasedAnimTickOption.AlwaysTickPoseAndRefreshBones && !second.Mesh.bPauseAnims && !second.Mesh.bNoSkeletonUpdate && !second.Mesh.bEnableUpdateRateOptimizations && !second.Mesh.bOnlyAllowAutonomousTickPose && !second.Mesh.bRenderStatic && second.Mesh.Tick && second.Mesh.Active, "hidden borrowed mesh evaluates offscreen");
var graph = JsonNode.Parse(File.ReadAllText(Path.Combine(root, "owned_mmd_graph.json")))!;
Check(graph["actors"]!.AsArray().Count == 8 && graph["actors"]![0]!["slot"]!.GetValue<uint>() == 5 && graph["actors"]![2]!["slot"]!.GetValue<uint>() == 900, "dynamic graph slots");
Check(graph["actors"]![0]!["ownership"]!.GetValue<string>() == "borrowed" && graph["actors"]![1]!["ownership"]!.GetValue<string>() == "created", "duplicates create distinct actors");
Check(graph["actors"]!.AsArray().Select(a => a!["actor"]!.GetValue<string>()).Distinct().Count() == 8, "each graph actor is unique");
first.Actor.Location = new(11, 22, 33); first.Actor.bHidden = true; first.Actor.FailLocations = 1;
second.Actor.bHidden = true; second.Mesh.Tick = false; second.Mesh.bPauseAnims = true; second.Mesh.VisibilityBasedAnimTickOption = EVisibilityBasedAnimTickOption.OnlyTickPoseWhenRendered;
Throws(ManagedBridge.MaintainStage, "one failing actor reports a maintenance error");
Check(!second.Actor.bHidden && second.Mesh.Tick && !second.Mesh.bPauseAnims && second.Mesh.VisibilityBasedAnimTickOption == EVisibilityBasedAnimTickOption.AlwaysTickPoseAndRefreshBones, "maintenance failure does not skip later borrowed actors");
ManagedBridge.MaintainStage(); Check(first.Actor.Location.Y == 2100 && first.Actor.Location.Z == 90 && !first.Actor.bHidden, "game stage overwrite is repaired without replacing the saved original state");
Throws(ManagedBridge.CheckReleased, "active actors are not released");
second.Mesh.FailVisibilityRestores = 1;
Throws(ManagedBridge.Stop, "failed borrowed restoration must report pending");
Check(manager.Destroyed.Count == 6 && manager.UiSceneRoleActorMap.Count == 0, "all created actors still clean despite one failed borrowed restore");
Check(first.Actor.Location.X == 1000 && first.Actor.Location.Y == 2000 && first.Actor.Location.Z == 90 && !first.Actor.bHidden, "current player transform restores");
Check(first.Handle.HoldEntityMap.Count == 0 && second.Handle.HoldEntityMap.Count == 1, "failed actor retains its hold for retry");
Throws(ManagedBridge.CheckReleased, "failed borrowed restoration cannot pass release check");
ManagedBridge.Stop(); ManagedBridge.CheckReleased();
Check(second.Actor.Location.X == 8000 && second.Actor.Location.Y == -5000 && second.Actor.Location.Z == -1000 && second.Actor.bHidden && second.Mesh.bHiddenInGame && !second.Mesh.bVisible, "hidden team actor transform and visibility restore");
Check(!second.Mesh.Tick && !second.Mesh.Active && second.Mesh.Interval == .7f && second.Mesh.bPauseAnims && second.Mesh.bNoSkeletonUpdate && second.Mesh.bEnableUpdateRateOptimizations && second.Mesh.bOnlyAllowAutonomousTickPose && second.Mesh.bRenderStatic && second.Mesh.VisibilityBasedAnimTickOption == EVisibilityBasedAnimTickOption.OnlyTickPoseWhenRendered && !second.Actor.Tick && second.Actor.Interval == .5f, "all animation and tick settings restore");
Check(first.Actor.Valid && second.Actor.Valid && wrong.Actor.Valid && second.Handle.HoldEntityMap.Count == 0, "borrowed actors are never destroyed");
first.Actor.Location = new(400, 500, 600); ManagedBridge.MaintainStage(); Check(first.Actor.Location.X == 400 && first.Actor.Location.Y == 500, "maintenance stays disabled after stop"); first.Actor.Location = new(1000, 2000, 90);
var next = Path.Combine(root, "second"); Directory.CreateDirectory(next); File.WriteAllText(Path.Combine(next, "actors.tsv"), "123\t90\t900\t0\t0\t0\tRole90Skin900\t0\n");
Environment.SetEnvironmentVariable("WWPOSER_OWNED_SESSION_DIR", next); ManagedBridge.BindSession(); ManagedBridge.BeginTarget(); ManagedBridge.CheckLoaded(); ManagedBridge.ShowAtLocal(); ManagedBridge.CheckLoaded();
manager.FailDestroys = 1; Throws(ManagedBridge.Stop, "created destroy failure must remain retryable"); Throws(ManagedBridge.CheckReleased, "created actor remains registered");
ManagedBridge.Stop(); ManagedBridge.CheckReleased(); Check(manager.UiSceneRoleActorMap.Count == 0, "created actor destroy retry succeeds");
var many = Path.Combine(root, "many"); Directory.CreateDirectory(many); File.WriteAllLines(Path.Combine(many, "actors.tsv"), Enumerable.Range(0, 70).Select(i => $"{i + 1}\t10\t100\t{i * 50}\t0\t0\tRole10Skin100\t0"));
Environment.SetEnvironmentVariable("WWPOSER_OWNED_SESSION_DIR", many); ManagedBridge.BindSession(); ManagedBridge.BeginTarget(); ManagedBridge.CheckLoaded(); ManagedBridge.ShowAtLocal(); ManagedBridge.CheckLoaded();
var manyGraph = JsonNode.Parse(File.ReadAllText(Path.Combine(many, "owned_mmd_graph.json")))!;
Check(manyGraph["actors"]!.AsArray().Count == 70 && manyGraph["actors"]!.AsArray().Count(a => a!["ownership"]!.GetValue<string>() == "borrowed") == 1 && manager.UiSceneRoleActorMap.Count == 69, "70 duplicate selections borrow original once and create remaining actors");
ManagedBridge.Stop(); ManagedBridge.CheckReleased(); Check(manager.UiSceneRoleActorMap.Count == 0 && first.Handle.HoldEntityMap.Count == 0, "large dynamic session returns all ownership");
var conflict = Path.Combine(root, "factory-conflict"); Directory.CreateDirectory(conflict); File.WriteAllText(Path.Combine(conflict, "actors.tsv"), "1\t90\t900\t0\t0\t0\tRole90Skin900\t0\n");
var foreign = manager.CreateUiSceneRoleActor(EUiModelUseWay.RoleInRoleView); manager.ReturnExisting = foreign;
Environment.SetEnvironmentVariable("WWPOSER_OWNED_SESSION_DIR", conflict); ManagedBridge.BindSession(); Throws(ManagedBridge.BeginTarget, "factory reuse must be rejected before loading or moving it"); ManagedBridge.Stop(); ManagedBridge.CheckReleased();
Check(manager.UiSceneRoleActorMap.ContainsKey(foreign.Index) && foreign.Valid && !manager.Destroyed.Contains(foreign), "rejected foreign factory actor is never destroyed"); manager.ReturnExisting = null; manager.DestroyUiSceneRoleActor(foreign.Index);
var duplicate = Path.Combine(root, "factory-duplicate"); Directory.CreateDirectory(duplicate); File.WriteAllLines(Path.Combine(duplicate, "actors.tsv"), new[] { "1\t90\t900\t0\t0\t0\tRole90Skin900\t0", "2\t90\t900\t100\t0\t0\tRole90Skin900\t0" });
var destroyedBefore = manager.Destroyed.Count; manager.ReturnLatestAfter = manager.Created.Count + 1;
Environment.SetEnvironmentVariable("WWPOSER_OWNED_SESSION_DIR", duplicate); ManagedBridge.BindSession(); Throws(ManagedBridge.BeginTarget, "repeated factory actor/index within the same session must be rejected"); ManagedBridge.Stop(); ManagedBridge.CheckReleased();
Check(manager.Destroyed.Count == destroyedBefore + 1 && manager.UiSceneRoleActorMap.Count == 0, "factory duplicate cleanup destroys the actual owner exactly once"); manager.ReturnLatestAfter = int.MaxValue;
var sessionA = Path.Combine(root, "race-a"); var sessionB = Path.Combine(root, "race-b"); Directory.CreateDirectory(sessionA); Directory.CreateDirectory(sessionB); File.WriteAllText(Path.Combine(sessionA, "actors.tsv"), "1\t90\t900\t0\t0\t0\tRole90Skin900\t0\n");
Environment.SetEnvironmentVariable("WWPOSER_OWNED_SESSION_DIR", sessionA); ManagedBridge.BindSession(); ManagedBridge.BeginTarget();
Environment.SetEnvironmentVariable("WWPOSER_OWNED_SESSION_DIR", sessionB); Throws(ManagedBridge.BindSession, "new carrier cannot steal active session ownership"); Throws(ManagedBridge.Stop, "new carrier cannot stop another session");
Environment.SetEnvironmentVariable("WWPOSER_OWNED_SESSION_DIR", sessionA); ManagedBridge.BindSession(); ManagedBridge.Stop(); ManagedBridge.CheckReleased();
Environment.SetEnvironmentVariable("WWPOSER_OWNED_SESSION_DIR", sessionB); ManagedBridge.BindSession(); ManagedBridge.Stop(); ManagedBridge.CheckReleased(); Check(manager.UiSceneRoleActorMap.Count == 0, "original session can restore after rejected second carrier");
// Gameplay meshes and UI actor meshes do not have to share a local yaw.
// Include the local pawn as the first borrowed actor, so a partial placement
// retry must keep the mesh reference from before that pawn moved or rotated.
foreach (var anchorYaw in new float[] { 37, 175, -179 })
{
    first.Actor.Rotation = new() { Yaw = anchorYaw }; first.Mesh.RelativeRotation = new() { Yaw = -90 };
    second.Actor.Rotation = new() { Yaw = -121 }; second.Mesh.RelativeRotation = new() { Yaw = 45 };
    var initial = first.Actor.Location; var referenceMeshYaw = first.Mesh.K2_GetComponentRotation().Yaw;
    var headingRoot = Path.Combine(root, "heading-" + anchorYaw); Directory.CreateDirectory(headingRoot);
    var yaws = new double[] { 90, -150, 0, 90, 180, 150, -150 };
    File.WriteAllLines(Path.Combine(headingRoot, "actors.tsv"), yaws.Select((yaw, i) => $"{i + 1}\t{(i == 0 ? 10 : i == 1 ? 20 : 90)}\t{(i == 0 ? 100 : i == 1 ? 200 : 900)}\t{(i - 3) * 125}\t217\t0\t{(i == 0 ? "Role10Skin100" : i == 1 ? "Role20Skin200" : "")}\t{yaw}"));
    var createdStart = manager.Created.Count;
    Environment.SetEnvironmentVariable("WWPOSER_OWNED_SESSION_DIR", headingRoot); ManagedBridge.BindSession(); ManagedBridge.BeginTarget(); ManagedBridge.CheckLoaded();
    var headingActors = new AActor[] { first.Actor, second.Actor }.Concat(manager.Created.Skip(createdStart)).ToArray();
    var headingMeshes = new USkeletalMeshComponent[] { first.Mesh, second.Mesh }.Concat(manager.Created.Skip(createdStart).Select(actor => actor.Model.Actor.MainMeshComponent)).ToArray();
    for (var i = 2; i < headingActors.Length; i++)
    {
        headingActors[i].Rotation = new() { Yaw = 173 };
        headingMeshes[i].RelativeRotation = new() { Yaw = i % 2 == 0 ? 0 : 25 };
    }
    headingActors[2].FailLocations = 1;
    Throws(ManagedBridge.ShowAtLocal, "heading placement failure must permit a retry");
    ManagedBridge.ShowAtLocal();
    for (var i = 0; i < headingActors.Length; i++)
        Check(Math.Abs(Math.IEEERemainder(headingMeshes[i].K2_GetComponentRotation().Yaw - referenceMeshYaw - yaws[i], 360)) < .001, "visible mesh heading must follow requested yaw for borrowed and created actors");
    var lifecycle = JsonNode.Parse(File.ReadAllText(Path.Combine(headingRoot, "managed_actor_lifecycle.json")))!;
    Check(lifecycle["stage_reference_actor_yaw"]!.GetValue<double>() == anchorYaw && lifecycle["stage_reference_mesh_yaw"]!.GetValue<double>() == referenceMeshYaw, "heading diagnostics retain the pre-placement reference");
    Check(lifecycle["actors"]!.AsArray().Select((row, i) => row!["requested_stage_yaw"]!.GetValue<double>() == yaws[i] && Math.Abs(Math.IEEERemainder(row["stage_actor_yaw"]!.GetValue<double>() + row["mesh_yaw_offset"]!.GetValue<double>() - referenceMeshYaw - yaws[i], 360)) < .001).All(value => value), "heading diagnostics expose the actual per-actor compensation");
    var angle = anchorYaw * Math.PI / 180;
    Check(Math.Abs(first.Actor.Location.X - (initial.X - Math.Sin(angle) * -375 + Math.Cos(angle) * 217)) < .001 && Math.Abs(first.Actor.Location.Y - (initial.Y + Math.Cos(angle) * -375 + Math.Sin(angle) * 217)) < .001, "heading correction must preserve the initial stage position basis");
    var origin = lifecycle["stage_reference_location"]!.AsArray().Select(value => value!.GetValue<double>()).ToArray();
    Check(origin.Length == 3 && Math.Abs(origin[0] - initial.X) < .001 && Math.Abs(origin[1] - initial.Y) < .001 && Math.Abs(origin[2] - initial.Z) < .001, "stage reference origin records the pawn's pre-placement world location");
    var forward = lifecycle["stage_reference_forward"]!.AsArray().Select(value => value!.GetValue<double>()).ToArray();
    Check(forward.Length == 2 && Math.Abs(forward[0] - Math.Cos(angle)) < .001 && Math.Abs(forward[1] - Math.Sin(angle)) < .001, "stage reference forward records the pawn heading basis the native camera orbits");
    Check(lifecycle["stage_reference_floor"]!.GetValue<double>() == initial.Z - 90, "stage reference floor records the ground height the camera targets rise from");
    ManagedBridge.Stop(); ManagedBridge.CheckReleased();
    Check(first.Actor.Rotation.Yaw == anchorYaw && second.Actor.Rotation.Yaw == -121 && first.Mesh.RelativeRotation.Yaw == -90 && second.Mesh.RelativeRotation.Yaw == 45, "heading correction restores borrowed Actor rotations without changing mesh offsets");
}
// Camera deltas must survive a nonzero world origin without changing the
// global coordinates used by D_ actor placement. Cover both first-actor paths.
foreach (var worldOrigin in new FVectorDouble[] { new(0, 0, 0), new(-51530, -562570, 36800), new(10000000, -20000000, 3000000) })
foreach (var borrowFirst in new[] { true, false })
{
    UGameplayStatics.WorldOrigin = worldOrigin;
    first.Actor.Location = new(worldOrigin.X + 64, worldOrigin.Y + 8, worldOrigin.Z + 120);
    first.Actor.Rotation = new(); first.Mesh.RelativeRotation = new(); first.Mesh.RelativeLocation = new(0, 0, -77);
    var initial = first.Actor.Location;
    var cameraRoot = Path.Combine(root, "camera-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(cameraRoot);
    File.WriteAllLines(Path.Combine(cameraRoot, "actors.tsv"), new[] {
        $"1\t{(borrowFirst ? 10 : 90)}\t{(borrowFirst ? 100 : 900)}\t120\t30\t0\t{(borrowFirst ? "Role10Skin100" : "Role90Skin900")}\t0",
        "2\t90\t900\t-150\t-45\t0\tRole90Skin900\t0" });
    var createdStart = manager.Created.Count;
    Environment.SetEnvironmentVariable("WWPOSER_OWNED_SESSION_DIR", cameraRoot); ManagedBridge.BindSession(); ManagedBridge.BeginTarget(); ManagedBridge.CheckLoaded(); ManagedBridge.ShowAtLocal();
    var anchorMesh = borrowFirst ? first.Mesh : manager.Created[createdStart].Model.Actor.MainMeshComponent;
    var lifecycle = JsonNode.Parse(File.ReadAllText(Path.Combine(cameraRoot, "managed_actor_lifecycle.json")))!;
    var delta = lifecycle["stage_reference_camera_offset_from_mesh"]!.AsArray().Select(value => value!.GetValue<double>()).ToArray();
    var localMesh = anchorMesh.K2_GetComponentLocation();
    Check(delta.Length == 3 && Math.Abs(localMesh.X + delta[0] - 64) < .001 && Math.Abs(localMesh.Y + delta[1] - 8) < .001 && Math.Abs(localMesh.Z + delta[2] - 30) < .001,
        "camera reference reconstructs the original local stage floor for borrowed and created first actors at nonzero world origins");
    var created = manager.Created.Last();
    Check(created.Location.X == initial.X - 45 && created.Location.Y == initial.Y - 150 && created.Location.Z == initial.Z - 90,
        "camera coordinate conversion preserves D_ global actor placement");
    UGameplayStatics.WorldOrigin = new(worldOrigin.X + 2000, worldOrigin.Y - 3000, worldOrigin.Z + 4000);
    localMesh = anchorMesh.K2_GetComponentLocation();
    Check(Math.Abs(localMesh.X + delta[0] - (64 - 2000)) < .001 && Math.Abs(localMesh.Y + delta[1] - (8 + 3000)) < .001 && Math.Abs(localMesh.Z + delta[2] - (30 - 4000)) < .001,
        "camera reference follows a world-origin rebase without recapturing global coordinates");
    ManagedBridge.Stop(); ManagedBridge.CheckReleased();
    var stopped = JsonNode.Parse(File.ReadAllText(Path.Combine(cameraRoot, "managed_actor_lifecycle.json")))!;
    Check(stopped["stage_reference_camera_offset_from_mesh"]!.ToJsonString() == lifecycle["stage_reference_camera_offset_from_mesh"]!.ToJsonString(),
        "shutdown writes the saved camera reference without reading released actor meshes");
}
UGameplayStatics.WorldOrigin = new(); first.Mesh.RelativeLocation = new();
string VisibilitySession(string name, params string[] rows)
{
    var directory = Path.Combine(root, "visibility-" + name); Directory.CreateDirectory(directory);
    File.WriteAllLines(Path.Combine(directory, "actors.tsv"), rows.Length == 0 ? new[] { "1\t90\t900\t0\t0\t0\tRole90Skin900\t0" } : rows);
    Environment.SetEnvironmentVariable("WWPOSER_OWNED_SESSION_DIR", directory); ManagedBridge.BindSession(); ManagedBridge.BeginTarget(); ManagedBridge.CheckLoaded();
    return directory;
}
void VisiblePlayer()
{
    UGameplayStatics.Pawn = first.Actor; first.Actor.bHidden = false; first.Mesh.bVisible = true; first.Mesh.bHiddenInGame = false;
}
// Preserve all eight original visibility combinations, including a model that
// was already hidden. The non-performer's transform and evaluation are unowned.
for (int flags = 0; flags < 8; flags++)
{
    first.Actor.bHidden = (flags & 1) != 0; first.Mesh.bVisible = (flags & 2) != 0; first.Mesh.bHiddenInGame = (flags & 4) != 0;
    var before = first.Actor.Location; var actorTick = first.Actor.Tick; var meshTick = first.Mesh.Tick; var active = first.Mesh.Active; var paused = first.Mesh.bPauseAnims;
    var directory = VisibilitySession("flags-" + flags);
    Check(first.Actor.bHidden == ((flags & 1) != 0) && first.Mesh.bVisible == ((flags & 2) != 0) && first.Mesh.bHiddenInGame == ((flags & 4) != 0) && first.Handle.HoldEntityMap.Count == 0, "preparation alone does not hide or retain a non-performer");
    ManagedBridge.ShowAtLocal(); ManagedBridge.ShowAtLocal();
    Check(first.Actor.bHidden && !first.Mesh.bVisible && first.Mesh.bHiddenInGame && first.Handle.HoldEntityMap.Count == 1, "created-only playback hides the non-performing player and acquires one hold across retries");
    Check(first.Actor.Location.X == before.X && first.Actor.Location.Y == before.Y && first.Actor.Tick == actorTick && first.Mesh.Tick == meshTick && first.Mesh.Active == active && first.Mesh.bPauseAnims == paused, "hiding does not move the player or change its tick and animation policy");
    first.Actor.bHidden = false; first.Mesh.bVisible = true; first.Mesh.bHiddenInGame = false; first.Actor.Location = new(123, 456, 789);
    ManagedBridge.MaintainStage();
    Check(first.Actor.bHidden && !first.Mesh.bVisible && first.Mesh.bHiddenInGame && first.Actor.Location.X == 123, "created-only scene repairs visibility overwrites without freezing the player transform");
    ManagedBridge.Stop(); ManagedBridge.CheckReleased(); ManagedBridge.Stop();
    Check(first.Actor.bHidden == ((flags & 1) != 0) && first.Mesh.bVisible == ((flags & 2) != 0) && first.Mesh.bHiddenInGame == ((flags & 4) != 0) && first.Handle.HoldEntityMap.Count == 0 && first.Actor.Location.X == 123, "stop restores exact original visibility without restoring an unowned transform");
    var released = JsonNode.Parse(File.ReadAllText(Path.Combine(directory, "managed_actor_lifecycle.json")))!;
    Check(released["all_released"]!.GetValue<bool>() && released["nonperforming_player"]!["restored"]!.GetValue<bool>() && !released["nonperforming_player"]!["retained"]!.GetValue<bool>(), "terminal lifecycle accounts for the visibility lease");
}
VisiblePlayer();
VisibilitySession("same-role-copy", "1\t10\t999\t0\t0\t0\tRole10Skin999\t0"); ManagedBridge.ShowAtLocal();
Check(first.Actor.bHidden && !first.Mesh.bVisible, "a created copy with the same role but different skin does not count as the controlled instance performing");
ManagedBridge.Stop(); ManagedBridge.CheckReleased();
VisiblePlayer();
VisibilitySession("mixed", "1\t20\t200\t0\t0\t0\tRole20Skin200\t0", "2\t90\t900\t100\t0\t0\tRole90Skin900\t0"); ManagedBridge.ShowAtLocal();
Check(first.Actor.bHidden && !second.Actor.bHidden && second.Mesh.bVisible, "mixed playback hides only the non-performing player, preserving the borrowed dancer");
second.Mesh.FailVisibilityRestores = 1; Throws(ManagedBridge.Stop, "borrowed dancer restoration can fail independently");
Check(!first.Actor.bHidden && first.Mesh.bVisible && !first.Mesh.bHiddenInGame && first.Handle.HoldEntityMap.Count == 0, "a dancer restore failure cannot skip player visibility restoration");
ManagedBridge.Stop(); ManagedBridge.CheckReleased();
VisiblePlayer();
VisibilitySession("partial-hide"); first.Mesh.FailVisibilityRestores = 1;
Throws(ManagedBridge.ShowAtLocal, "partial visibility setup failure is recoverable");
Check(first.Actor.bHidden && first.Mesh.bVisible, "failure happens after the actor flag changes and before mesh visibility changes");
ManagedBridge.Stop(); ManagedBridge.CheckReleased();
Check(!first.Actor.bHidden && first.Mesh.bVisible && !first.Mesh.bHiddenInGame && first.Handle.HoldEntityMap.Count == 0, "startup failure restores the partially hidden player");
VisiblePlayer();
var visibilityRetry = VisibilitySession("restore-retry"); ManagedBridge.ShowAtLocal(); first.Mesh.FailVisibilityShows = 1;
Throws(ManagedBridge.Stop, "visibility restoration failure must remain pending");
Check(manager.UiSceneRoleActorMap.Count == 0 && first.Handle.HoldEntityMap.Count == 1, "failed player restore retains its own hold after every created dancer is removed");
Throws(ManagedBridge.CheckReleased, "actors removed alone cannot complete release while player visibility is pending");
var pendingVisibility = JsonNode.Parse(File.ReadAllText(Path.Combine(visibilityRetry, "managed_actor_lifecycle.json")))!;
Check(!pendingVisibility["all_released"]!.GetValue<bool>() && !pendingVisibility["nonperforming_player"]!["restored"]!.GetValue<bool>(), "lifecycle exposes incomplete visibility restoration");
var blockedSession = Path.Combine(root, "visibility-blocked"); Directory.CreateDirectory(blockedSession);
Environment.SetEnvironmentVariable("WWPOSER_OWNED_SESSION_DIR", blockedSession); Throws(ManagedBridge.BindSession, "pending visibility restoration cannot be discarded by a new session");
Environment.SetEnvironmentVariable("WWPOSER_OWNED_SESSION_DIR", visibilityRetry); ManagedBridge.MaintainStage();
Check(!first.Actor.bHidden && !first.Mesh.bHiddenInGame, "maintenance stays disabled during a visibility restoration retry");
ManagedBridge.Stop(); ManagedBridge.CheckReleased();
Check(first.Mesh.bVisible && first.Handle.HoldEntityMap.Count == 0, "retry completes the remaining visibility restoration and releases retention");
VisiblePlayer();
VisibilitySession("identity-change"); ManagedBridge.ShowAtLocal();
var firstComponent = first.Handle.Entity.GetComponent<CharacterActorComponent>();
var replacementMesh = new USkeletalMeshComponent { Owner = first.Actor, bVisible = true, bHiddenInGame = false }; firstComponent.SkeletalMesh = replacementMesh;
Throws(ManagedBridge.MaintainStage, "replacement player mesh is rejected before hiding"); Throws(ManagedBridge.Stop, "restoration never writes to a replacement mesh");
Check(replacementMesh.bVisible && !replacementMesh.bHiddenInGame && first.Handle.HoldEntityMap.Count == 1, "replacement stays untouched and original visibility ownership remains pending");
firstComponent.SkeletalMesh = first.Mesh; ManagedBridge.Stop(); ManagedBridge.CheckReleased();
Check(!first.Actor.bHidden && first.Mesh.bVisible && !first.Mesh.bHiddenInGame, "verified original identity can finish restoration after replacement rejection");
VisiblePlayer();
VisibilitySession("control-change"); ManagedBridge.ShowAtLocal(); UGameplayStatics.Pawn = second.Actor;
var secondHidden = second.Actor.bHidden; var secondVisible = second.Mesh.bVisible;
Throws(ManagedBridge.MaintainStage, "control switch ends maintenance of the captured player");
Check(second.Actor.bHidden == secondHidden && second.Mesh.bVisible == secondVisible, "control switch never hides an unacquired replacement player");
ManagedBridge.Stop(); ManagedBridge.CheckReleased(); UGameplayStatics.Pawn = first.Actor;
Check(!first.Actor.bHidden && first.Mesh.bVisible && !first.Mesh.bHiddenInGame, "cleanup restores the original player even after control changes");
// Two forms and repeated copies must own independent mutable engine objects,
// even when their immutable mesh assets and skeleton are shared.
var formsRoot = Path.Combine(root, "forms"); Directory.CreateDirectory(formsRoot);
var skeleton = new USkeleton();
UiRoleMorphComponent.SpecialMeshes[10] = new() { Name = "Special10", Skeleton = skeleton };
UiRoleLoadComponent.Resources["/Game/Forms/Upper.Upper"] = new USkeletalMesh { Name = "Upper", Skeleton = skeleton };
UiRoleLoadComponent.Resources["/Game/Forms/Body.Body"] = new USkeletalMesh { Name = "Body", Skeleton = skeleton };
UiRoleLoadComponent.Resources["/Game/Forms/Anim.Anim_C"] = new UClass();
string FormRow(int slot, int role, int skin, string mesh, string strategy = "role_skin", string children = "") =>
    $"{slot}\t{role}\t{skin}\t{slot * 100}\t0\t0\t{mesh}\t0\t{strategy}\t{strategy}\t{(strategy == "role_morph" ? 1 : 0)}\t1\t{(strategy == "mesh_parts" ? "/Game/Forms/Upper.Upper" : "")}\t{(strategy == "mesh_parts" ? "/Game/Forms/Anim.Anim_C" : "")}\t{children}\t1";
File.WriteAllLines(Path.Combine(formsRoot, "actors.tsv"), new[] {
    FormRow(1,10,100,"Role10Skin100"), FormRow(2,10,100,"Special10","role_morph"), FormRow(3,10,100,"Special10","role_morph"),
    FormRow(4,20,200,"Upper","mesh_parts","/Game/Forms/Body.Body"), FormRow(5,20,200,"Role20Skin200"), FormRow(6,20,200,"Upper","mesh_parts","/Game/Forms/Body.Body") });
Environment.SetEnvironmentVariable("WWPOSER_OWNED_SESSION_DIR", formsRoot); ManagedBridge.BindSession();
var createdBeforeForms = manager.Created.Count;
ManagedBridge.BeginTarget(); ManagedBridge.CheckLoaded();
var formActors = manager.Created.Skip(createdBeforeForms).ToArray();
Check(formActors.Length == 6 && first.Handle.HoldEntityMap.Count == 0 && second.Handle.HoldEntityMap.Count == 0, "multi-form defaults and variants never borrow a combat actor");
Check(formActors.Select(actor => actor.Model.Actor.MainMeshComponent._NativePtr).Distinct().Count() == 6 && formActors.Select(actor => actor.Model.Actor.MainMeshComponent.Anim._NativePtr).Distinct().Count() == 6, "each form and repeated copy owns a separate mesh component and animation instance");
Check(formActors[0].Model.Actor.MainMeshComponent.SkeletalMesh.Name == "Role10Skin100" && formActors[1].Model.Actor.MainMeshComponent.SkeletalMesh.Name == "Special10" && first.Mesh.SkeletalMesh.Name == "Role10Skin100", "selecting the special form does not change the default form or player");
Check(formActors[3].Model.Actor.MainMeshComponent.SkeletalMesh.Name == "Upper" && formActors[3].Model.Actor.ChildMeshComponentList.Single().SkeletalMesh.Name == "Body", "split form retains its face mesh leader and body follower");
Check(!ReferenceEquals(formActors[3].Model.Actor.ChildMeshComponentList.Single(),formActors[5].Model.Actor.ChildMeshComponentList.Single()), "split-form copies do not share mutable child mesh components");
ManagedBridge.ShowAtLocal(); ManagedBridge.CheckLoaded();
Check(formActors[3].Model.Actor.ChildMeshComponentList.Single().bVisible && formActors[3].Model.Actor.ChildMeshComponentList.Single().Tick, "split form body is visible and evaluates alongside the face leader");
var otherFormMesh = formActors[0].Model.Actor.MainMeshComponent;
formActors[1].Model.Morph.SetMorphType(EUiModelMorphType.默认形态);
Throws(ManagedBridge.MaintainStage, "form changes after binding are reported before accepting another graph");
Check(ReferenceEquals(otherFormMesh, formActors[0].Model.Actor.MainMeshComponent), "a changed form leaves its sibling instance untouched");
ManagedBridge.Stop(); ManagedBridge.CheckReleased();
Check(manager.UiSceneRoleActorMap.Count == 0 && first.Actor.Valid && second.Actor.Valid, "all form instances retire without destroying team actors");
foreach (var failure in new[] { "missing-resource", "wrong-skeleton", "missing-morph" })
{
    var failedRoot = Path.Combine(root, failure); Directory.CreateDirectory(failedRoot);
    if (failure == "missing-resource") UiRoleLoadComponent.Resources.Remove("/Game/Forms/Body.Body");
    else UiRoleLoadComponent.Resources["/Game/Forms/Body.Body"] = new USkeletalMesh { Name = "Body", Skeleton = new() };
    File.WriteAllText(Path.Combine(failedRoot,"actors.tsv"), failure == "missing-morph" ? FormRow(1,999,999,"Missing","role_morph") : FormRow(1,20,200,"Upper","mesh_parts","/Game/Forms/Body.Body"));
    Environment.SetEnvironmentVariable("WWPOSER_OWNED_SESSION_DIR", failedRoot); ManagedBridge.BindSession(); ManagedBridge.BeginTarget();
    Throws(ManagedBridge.CheckLoaded, "incomplete form resources must not silently use a default model: " + failure);
    ManagedBridge.Stop(); ManagedBridge.CheckReleased(); Check(manager.UiSceneRoleActorMap.Count == 0, "failed form creation remains recoverable: " + failure);
}
foreach (var sample in new[] {
    ("sweep", true, true, false, 2.15f, 0f, (byte)1, -2.15, "sweep_floor_distance"),
    ("line", true, true, true, 40f, 1.95f, (byte)1, -1.95, "line_floor_distance"),
    ("zero", true, true, false, 0f, 0f, (byte)1, 0d, "sweep_floor_distance"),
    ("invalid", false, true, false, 2.15f, 0f, (byte)1, -2.5, "fallback"),
    ("far", true, true, false, 25f, 0f, (byte)1, -2.5, "fallback"),
    ("nan", true, true, false, float.NaN, 0f, (byte)1, -2.5, "fallback"),
    ("airborne", true, true, false, 2.15f, 0f, (byte)3, -2.5, "fallback"),
    ("ordinary-stage", true, true, false, 2.15f, 0f, (byte)1, 0d, "capsule_bottom") })
{
    var floorRoot = Path.Combine(root, "floor-"+sample.Item1); Directory.CreateDirectory(floorRoot);
    File.WriteAllText(Path.Combine(floorRoot,"actors.tsv"), "1\t90\t900\t0\t0\t0\tRole90Skin900\t0\n");
    File.WriteAllText(Path.Combine(floorRoot,"stage_floor.tsv"), (sample.Item1 == "ordinary-stage" ? "capsule_bottom" : "walkable_floor")+"\t2.5\n");
    first.Actor.CharacterMovement.MovementMode = sample.Item7;
    first.Actor.CharacterMovement.CurrentFloor = new() { bBlockingHit=sample.Item2, bWalkableFloor=sample.Item3, bLineTrace=sample.Item4, FloorDist=sample.Item5, LineDist=sample.Item6 };
    var pawnZ = first.Actor.Location.Z;
    Environment.SetEnvironmentVariable("WWPOSER_OWNED_SESSION_DIR",floorRoot); ManagedBridge.BindSession(); ManagedBridge.BeginTarget(); ManagedBridge.CheckLoaded(); ManagedBridge.ShowAtLocal();
    var floorLog = JsonNode.Parse(File.ReadAllText(Path.Combine(floorRoot,"managed_actor_lifecycle.json")))!;
    Check(Math.Abs(floorLog["stage_height_correction_cm"]!.GetValue<double>()-sample.Item8)<.00001 && floorLog["stage_height_correction_source"]!.GetValue<string>()==sample.Item9, "formation floor source and bounded correction: "+sample.Item1);
    Check(first.Actor.Location.Z==pawnZ, "formation height correction leaves the player's collision capsule at its engine location");
    first.Actor.CharacterMovement.CurrentFloor.FloorDist = 4;
    ManagedBridge.ShowAtLocal();
    var retryLog = JsonNode.Parse(File.ReadAllText(Path.Combine(floorRoot,"managed_actor_lifecycle.json")))!;
    Check(retryLog["stage_height_correction_cm"]!.GetValue<double>() == floorLog["stage_height_correction_cm"]!.GetValue<double>(), "stage retries keep the initial floor snapshot without accumulating offsets");
    ManagedBridge.Stop(); ManagedBridge.CheckReleased();
}
assertions += SceneVisibilityChecks.Run(root, first.Actor, second.Handle);
assertions += PendingRemovalChecks.Run(root, first.Actor, first.Handle);
Console.WriteLine($"PASS: {assertions} synthetic managed actor assertions; borrowing, forms, floor correction, visibility, and restoration retries. No game assemblies loaded.");
