using System.Text.Json.Nodes;
using UnrealEngine;
using WwPoser.ColdLoad;

var assertions = 0;
void Check(bool condition, string label) { if (!condition) throw new Exception(label); assertions++; }
void Throws(Action action, string label) { try { action(); } catch (InvalidOperationException) { assertions++; return; } throw new Exception(label); }
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
Console.WriteLine($"PASS: {assertions} synthetic managed borrowing assertions; mixed actors, heading offsets, rebased camera references, global placement, saved fields, and restoration retries. No game assemblies loaded.");
