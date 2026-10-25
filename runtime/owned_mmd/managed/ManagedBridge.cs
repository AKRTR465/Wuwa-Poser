// Public entries run on the game's attached thread. Native pose/face/camera
// restoration completes before Stop returns borrowed actors or destroys owners.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnrealEngine;

namespace WwPoser.ColdLoad
{
    public static class ManagedBridge
    {
        private sealed class Contract
        {
            public FieldInfo Instance, Map, LoadState, NativePointer;
            public FieldInfo PlayerInstance, PlayerId, TeamInstance, TeamPlayers, TeamItems, PlayerGroup, PlayerGroups, GroupRoles;
            public FieldInfo ItemPlayer, ItemRole, ItemCreature, ItemHandle, ItemLocal, ItemGroup, ActorInternal, SkinId, EntityFlags, DataRole, DataPlayer, DataCreature;
            public FieldInfo CharacterInstance, CharacterHandles, ObjectInstance, ObjectList, ObjectVersions;
            public MethodInfo Create, Destroy;
        }
        private sealed class Snapshot
        {
            public FVectorDouble Location;
            public FRotator Rotation;
            public double MeshYawOffset;
            public bool Hidden, ActorTick, MeshVisible, MeshHidden, MeshTick, MeshActive, PauseAnims, NoSkeletonUpdate, AutonomousOnly, UpdateRate, RenderStatic;
            public float ActorInterval, MeshInterval;
            public EVisibilityBasedAnimTickOption AnimTick;
        }
        private sealed class Owner
        {
            public uint Slot;
            public int RequestedRoleId, RequestedSkinId;
            public string AssetName, HoldReason;
            public string FormId = "default", FormStrategy = "role_skin", MainMeshPath = "", AnimClassPath = "";
            public string[] ChildMeshPaths = new string[0];
            public int MorphType, BaseModelId;
            public bool IndependentInstance, FormApplied;
            public UiRoleMorphComponent Morph;
            public List<USkeletalMeshComponent> FormChildren;
            public double X, Y, Z, Yaw;
            public AActor Actor;
            public UiSceneRoleActorManager Manager;
            public TsUiSceneRoleActor Role;
            public UiRoleLoadComponent Loader;
            public UiModelActorComponent ActorComponent;
            public UiModelDataComponent Data;
            public EntityHandle Handle;
            public WorldEntity Entity;
            public CharacterActorComponent BorrowedComponent;
            public int Index;
            public bool Borrowed, HasIndex, HoldAdded, LoadReturned, Shown, DestroyReturned, Released;
            public Snapshot Before;
            public FVectorDouble StageLocation;
            public FRotator StageRotation;
            public USkeletalMeshComponent Mesh;
            public USkeletalMesh Asset;
            public USkeleton Skeleton;
            public UAnimInstance Anim;
            public ulong ActorAddress, MeshAddress;
        }
        private sealed class Candidate
        {
            public int RoleId, SkinId;
            public EntityHandle Handle;
            public WorldEntity Entity;
            public CharacterActorComponent Component;
            public AActor Actor;
            public USkeletalMeshComponent Mesh;
            public string AssetName;
        }
        private sealed class PlayerVisibility
        {
            public Candidate Target;
            public ulong ActorAddress, MeshAddress;
            public string HoldReason;
            public bool ActorHidden, MeshVisible, MeshHidden, HoldAdded, Released;
        }
        private sealed class Stage
        {
            public FVectorDouble Origin;
            public FVectorDouble CameraOffsetFromMesh;
            public bool CameraReferenceCaptured;
            public FRotator Rotation;
            public double MeshYaw;
            public double MeshPitch, MeshRoll;
            public double Floor, RightX, RightY, ForwardX, ForwardY;
            public double HeightCorrection;
            public string HeightSource = "capsule_bottom";
        }
        private static Contract s_contract;
        private static readonly List<Owner> s_owners = new List<Owner>();
        private static string s_session;
        private static bool s_began, s_graphExported, s_maintenanceEnabled;
        private static Stage s_stage;
        private static PlayerVisibility s_playerVisibility;
        private const string SessionVariable = "WWPOSER_OWNED_SESSION_DIR";

        private static FieldInfo Field(Type owner, string name)
        {
            var field = owner.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (field == null) throw new MissingFieldException(owner.FullName, name);
            return field;
        }
        private static MethodInfo Method(Type owner, string name, params Type[] parameters)
        {
            var method = owner.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, parameters, null);
            if (method == null) throw new MissingMethodException(owner.FullName, name);
            return method;
        }
        private static Contract Bind()
        {
            if (s_contract != null) return s_contract;
            var value = new Contract();
            value.Instance = Field(typeof(UiSceneRoleActorManager), "_instance"); value.Map = Field(typeof(UiSceneRoleActorManager), "UiSceneRoleActorMap");
            value.LoadState = Field(typeof(UiModelDataComponent), "ModelLoadStateInternal"); value.NativePointer = Field(typeof(UnrealObject), "_NativePtr");
            value.Create = Method(typeof(UiSceneRoleActorManager), "CreateUiSceneRoleActor", typeof(EUiModelUseWay)); value.Destroy = Method(typeof(UiSceneRoleActorManager), "DestroyUiSceneRoleActor", typeof(int));
            // Declared field scopes follow the verified SceneTeam business chain.
            value.PlayerInstance = Field(typeof(PlayerInfoModel), "_Instance"); value.PlayerId = Field(typeof(PlayerInfoModel), "Id");
            value.TeamInstance = Field(typeof(SceneTeamModel), "_Instance"); value.TeamPlayers = Field(typeof(SceneTeamModel), "TeamPlayerMap");
            value.TeamItems = Field(typeof(SceneTeamModel), "TeamItems"); value.PlayerGroup = Field(typeof(SceneTeamPlayer), "CurrentGroupType");
            value.PlayerGroups = Field(typeof(SceneTeamPlayer), "GroupMap"); value.GroupRoles = Field(typeof(SceneTeamGroup), "RoleList");
            value.ItemPlayer = Field(typeof(SceneTeamItem), "PlayerId"); value.ItemRole = Field(typeof(SceneTeamItem), "ConfigId");
            value.ItemCreature = Field(typeof(SceneTeamItem), "CreatureDataId"); value.ItemHandle = Field(typeof(SceneTeamItem), "EntityHandleInternal");
            value.ItemLocal = Field(typeof(SceneTeamItem), "IsMyRoleInternal"); value.ItemGroup = Field(typeof(SceneTeamItem), "GroupType");
            value.ActorInternal = Field(typeof(BaseActorComponent), "ActorInternal"); value.SkinId = Field(typeof(CreatureDataComponent), "SkinId");
            value.EntityFlags = Field(typeof(Entity), "FlagInternal"); value.DataRole = Field(typeof(CreatureDataComponent), "RoleId"); value.DataPlayer = Field(typeof(CreatureDataComponent), "PlayerId"); value.DataCreature = Field(typeof(CreatureDataComponent), "CreatureDataId");
            value.CharacterInstance = Field(typeof(CharacterModel), "_Instance"); value.CharacterHandles = Field(typeof(CharacterModel), "Handles");
            var objectSystem = typeof(Entity).Assembly.GetType("ObjectSystem", true);
            value.ObjectInstance = Field(objectSystem, "_instance"); value.ObjectList = Field(objectSystem, "Objects"); value.ObjectVersions = Field(objectSystem, "Versions");
            s_contract = value; return value;
        }
        private static Dictionary<int, TsUiSceneRoleActor> Map(Owner owner) { return (Dictionary<int, TsUiSceneRoleActor>)Bind().Map.GetValue(owner.Manager); }
        private static bool Valid(UnrealObject value) { return !Object.ReferenceEquals(value, null) && value.IsValid(); }
        private static ulong Address(UnrealObject wrapper) { return (ulong)((IntPtr)Bind().NativePointer.GetValue(wrapper)).ToInt64(); }
        private static bool AllReleased() { foreach (var owner in s_owners) if (!owner.Released) return false; return s_playerVisibility == null || s_playerVisibility.Released; }
        public static void Probe() { Bind(); }
        public static void VerifySession()
        {
            var caller = Environment.GetEnvironmentVariable(SessionVariable);
            if (String.IsNullOrEmpty(caller) || s_session == null || !String.Equals(Path.GetFullPath(caller), s_session, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Actor operation belongs to another session.");
        }
        public static void BindSession()
        {
            var next = Path.GetFullPath(Environment.GetEnvironmentVariable(SessionVariable));
            if (String.Equals(next, s_session, StringComparison.OrdinalIgnoreCase)) return;
            if (!AllReleased()) throw new InvalidOperationException("Previous actors still require restoration.");
            s_owners.Clear(); s_session = next; s_began = false; s_graphExported = false; s_maintenanceEnabled = false; s_stage = null; s_playerVisibility = null;
        }
        private static List<Owner> Requests()
        {
            var requests = new List<Owner>(); var path = Path.Combine(s_session, "actors.tsv");
            if (!File.Exists(path))
            {
                var old = File.ReadAllLines(Path.Combine(s_session, "owned_character.txt"), Encoding.UTF8);
                requests.Add(new Owner { Slot = 1, RequestedRoleId = Int32.Parse(old[1], CultureInfo.InvariantCulture), RequestedSkinId = Int32.Parse(old[2], CultureInfo.InvariantCulture), X = 150, AssetName = "" }); return requests;
            }
            var slots = new HashSet<uint>();
            foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
            {
                if (String.IsNullOrWhiteSpace(line)) continue; var columns = line.Split('\t');
                if (columns.Length != 8 && columns.Length != 16) throw new InvalidDataException("actors.tsv requires eight legacy or sixteen form-aware columns.");
                var owner = new Owner { Slot = UInt32.Parse(columns[0], CultureInfo.InvariantCulture), RequestedRoleId = Int32.Parse(columns[1], CultureInfo.InvariantCulture), RequestedSkinId = Int32.Parse(columns[2], CultureInfo.InvariantCulture), X = Double.Parse(columns[3], CultureInfo.InvariantCulture), Y = Double.Parse(columns[4], CultureInfo.InvariantCulture), Z = Double.Parse(columns[5], CultureInfo.InvariantCulture), AssetName = columns[6], Yaw = Double.Parse(columns[7], CultureInfo.InvariantCulture) };
                if (columns.Length == 16)
                {
                    owner.FormId = columns[8]; owner.FormStrategy = columns[9]; owner.MorphType = Int32.Parse(columns[10], CultureInfo.InvariantCulture);
                    if (String.IsNullOrEmpty(owner.FormId)) throw new InvalidDataException("Missing form ID.");
                    foreach (var ch in owner.FormId) if (!(ch >= 'a' && ch <= 'z') && !(ch >= '0' && ch <= '9') && ch != '-' && ch != '_') throw new InvalidDataException("Invalid form ID.");
                    owner.BaseModelId = Int32.Parse(columns[11], CultureInfo.InvariantCulture); owner.MainMeshPath = columns[12]; owner.AnimClassPath = columns[13];
                    owner.ChildMeshPaths = String.IsNullOrEmpty(columns[14]) ? new string[0] : columns[14].Split('|');
                    if (columns[15] != "0" && columns[15] != "1") throw new InvalidDataException("Invalid independent actor flag.");
                    owner.IndependentInstance = columns[15] == "1";
                    if (owner.FormStrategy != "role_skin" && owner.FormStrategy != "role_morph" && owner.FormStrategy != "mesh_parts") throw new InvalidDataException("Unknown actor form strategy.");
                    if (owner.FormStrategy != "role_skin" && (!owner.IndependentInstance || owner.BaseModelId <= 0 || String.IsNullOrEmpty(owner.AssetName))) throw new InvalidDataException("Forms require a separately owned actor and exact mesh.");
                    if (owner.FormStrategy == "role_morph" && owner.MorphType != 1) throw new InvalidDataException("Unsupported role morph type.");
                    if (owner.FormStrategy == "mesh_parts" && (!AssetPath(owner.MainMeshPath) || !AssetPath(owner.AnimClassPath) || owner.ChildMeshPaths.Length == 0)) throw new InvalidDataException("Incomplete form mesh composition.");
                    foreach (var child in owner.ChildMeshPaths) if (!AssetPath(child)) throw new InvalidDataException("Invalid form child mesh path.");
                }
                if (owner.Slot == 0 || !slots.Add(owner.Slot)) throw new InvalidDataException("Actor slots must be distinct positive numbers."); requests.Add(owner);
            }
            if (requests.Count == 0) throw new InvalidDataException("No actors requested."); return requests;
        }
        private static bool AssetPath(string value) { return !String.IsNullOrEmpty(value) && value.StartsWith("/Game/", StringComparison.Ordinal) && value.IndexOfAny(new char[] { '\t', '\r', '\n', '|', '\\' }) < 0 && !value.Contains(".."); }
        private static bool HandleCurrent(EntityHandle handle, WorldEntity entity)
        {
            if (handle == null || entity == null || handle.PendingRemoving || !Object.ReferenceEquals(handle.Entity, entity) || handle.Id != entity.Id || handle.Index != entity.Index) return false;
            var c = Bind(); var flags = Convert.ToInt32(c.EntityFlags.GetValue(entity), CultureInfo.InvariantCulture);
            if ((flags & 0x10) == 0 || (flags & 0x80) != 0 || (uint)handle.Index != ((uint)handle.Id >> 16)) return false;
            var character = c.CharacterInstance.GetValue(null); var objects = c.ObjectInstance.GetValue(null); if (character == null || objects == null) return false;
            var handles = (List<EntityHandle>)c.CharacterHandles.GetValue(character); var values = (List<ObjectBase>)c.ObjectList.GetValue(objects); var versions = (List<int>)c.ObjectVersions.GetValue(objects); var index = handle.Index;
            return index >= 0 && index < handles.Count && index < values.Count && index < versions.Count && Object.ReferenceEquals(handles[index], handle) && Object.ReferenceEquals(values[index], entity) && versions[index] == (handle.Id & 65535);
        }
        private static List<Candidate> CurrentTeam()
        {
            var result = new List<Candidate>(); var c = Bind(); var info = c.PlayerInstance.GetValue(null); var team = c.TeamInstance.GetValue(null); if (info == null || team == null) return result;
            var playerId = (int)c.PlayerId.GetValue(info); var players = (Dictionary<int, SceneTeamPlayer>)c.TeamPlayers.GetValue(team); SceneTeamPlayer player;
            if (!players.TryGetValue(playerId, out player)) return result; var groupValue = c.PlayerGroup.GetValue(player); if (groupValue == null) return result;
            var groupType = (ETeamGroupType)groupValue; var groups = (Dictionary<ETeamGroupType, SceneTeamGroup>)c.PlayerGroups.GetValue(player); SceneTeamGroup group;
            if (!groups.TryGetValue(groupType, out group)) return result;
            var roles = (List<SceneTeamRole>)c.GroupRoles.GetValue(group); var items = (List<SceneTeamItem>)c.TeamItems.GetValue(team);
            foreach (var role in roles)
            {
                if (role == null) continue; SceneTeamItem matched = null;
                foreach (var item in items)
                {
                    if (item == null || (int)c.ItemPlayer.GetValue(item) != playerId || !(bool)c.ItemLocal.GetValue(item) || (ETeamGroupType)c.ItemGroup.GetValue(item) != groupType || (int)c.ItemRole.GetValue(item) != role.RoleId || (long)c.ItemCreature.GetValue(item) != role.CreatureDataId) continue;
                    if (matched != null) { matched = null; break; } matched = item;
                }
                if (matched == null) continue; var handle = (EntityHandle)c.ItemHandle.GetValue(matched);
                if (handle == null || handle.CreatureDataId != role.CreatureDataId || !HandleCurrent(handle, handle.Entity)) continue;
                var entity = handle.Entity; var component = entity.GetComponent<CharacterActorComponent>(); var data = entity.GetComponent<CreatureDataComponent>(); if (component == null || data == null) continue;
                if ((int)c.DataRole.GetValue(data) != role.RoleId || (int)c.DataPlayer.GetValue(data) != playerId || (long)c.DataCreature.GetValue(data) != role.CreatureDataId) continue;
                var actor = (AActor)c.ActorInternal.GetValue(component); var mesh = component.SkeletalMesh; if (!Valid(actor) || !Valid(mesh) || !Valid(mesh.SkeletalMesh)) continue;
                result.Add(new Candidate { RoleId = role.RoleId, SkinId = (int)c.SkinId.GetValue(data), Handle = handle, Entity = entity, Component = component, Actor = actor, Mesh = mesh, AssetName = mesh.SkeletalMesh.GetName() });
            }
            return result;
        }
        public static void BeginTarget()
        {
            VerifySession(); if (s_began) return; s_began = true; var requests = Requests(); var candidates = CurrentTeam(); var used = new HashSet<ulong>(); var contract = Bind();
            foreach (var owner in requests)
            {
                s_owners.Add(owner); Candidate candidate = null;
                if (!owner.IndependentInstance) foreach (var option in candidates) if (option.RoleId == owner.RequestedRoleId && option.SkinId == owner.RequestedSkinId && String.Equals(option.AssetName, owner.AssetName, StringComparison.Ordinal) && !used.Contains(Address(option.Actor)) && HandleCurrent(option.Handle, option.Entity)) { candidate = option; break; }
                if (candidate != null)
                {
                    owner.Borrowed = true; owner.Actor = candidate.Actor; owner.Mesh = candidate.Mesh; owner.Handle = candidate.Handle; owner.Entity = candidate.Entity; owner.BorrowedComponent = candidate.Component;
                    owner.HoldReason = "WWPoser:" + s_session + ":" + owner.Slot.ToString(CultureInfo.InvariantCulture);
                    owner.Handle.AddHoldEntity(owner.HoldReason); owner.HoldAdded = owner.Handle.HoldEntityMap.ContainsKey(owner.HoldReason);
                    if (!owner.HoldAdded) throw new InvalidOperationException("Could not retain requested team actor.");
                    owner.LoadReturned = true; used.Add(Address(owner.Actor));
                }
                else
                {
                    owner.Manager = (UiSceneRoleActorManager)contract.Instance.GetValue(null);
                    var before = new Dictionary<int, TsUiSceneRoleActor>(Map(owner));
                    var role = (TsUiSceneRoleActor)contract.Create.Invoke(owner.Manager, new object[] { EUiModelUseWay.RoleInRoleView });
                    if (!Valid(role)) throw new InvalidOperationException("Actor factory did not return a valid actor.");
                    var index = role.GetRoleActorIndex(); var address = Address(role); var duplicate = before.ContainsKey(index) || used.Contains(address);
                    foreach (var existing in before.Values) if (!Object.ReferenceEquals(existing, null) && Address(existing) == address) duplicate = true;
                    if (duplicate) { owner.Released = true; throw new InvalidOperationException("Actor factory returned an existing actor or occupied index; it was not acquired for this slot."); }
                    TsUiSceneRoleActor registered;
                    if (!Map(owner).TryGetValue(index, out registered) || !Object.ReferenceEquals(registered, role)) { owner.Released = true; throw new InvalidOperationException("Actor factory registration does not match its returned actor."); }
                    owner.Role = role; owner.Actor = role; owner.Index = index; owner.HasIndex = true; used.Add(address); var model = owner.Role.Model;
                    owner.Loader = model.GetComponent<UiRoleLoadComponent>(); owner.ActorComponent = model.GetComponent<UiModelActorComponent>(); owner.Data = model.GetComponent<UiModelDataComponent>();
                    if (owner.FormStrategy == "mesh_parts")
                    {
                        model.GetComponent<UiRoleDataComponent>().SetRoleConfigId(owner.RequestedRoleId, owner.RequestedSkinId);
                        var resources = new List<string> { owner.MainMeshPath, owner.AnimClassPath }; resources.AddRange(owner.ChildMeshPaths);
                        owner.Loader.LoadModelByModelId(owner.BaseModelId, true, null, resources);
                    }
                    else owner.Loader.LoadModelByRoleConfigId(owner.RequestedRoleId, owner.RequestedSkinId, true, null);
                    owner.LoadReturned = true;
                }
            }
            WriteLifecycle();
        }
        private static void Loaded(Owner owner)
        {
            if (owner.Borrowed)
            {
                if (!HandleCurrent(owner.Handle, owner.Entity) || !Valid(owner.Actor) || !Valid(owner.Mesh) || Address((AActor)Bind().ActorInternal.GetValue(owner.BorrowedComponent)) != Address(owner.Actor) || Address(owner.BorrowedComponent.SkeletalMesh) != Address(owner.Mesh)) throw new InvalidOperationException("Borrowed team actor changed.");
            }
            else
            {
                if (!owner.LoadReturned || (int)(EUiModelLoadState)Bind().LoadState.GetValue(owner.Data) != 2) throw new InvalidOperationException("Role model is still loading.");
                ApplyForm(owner); owner.Mesh = owner.ActorComponent.MainMeshComponent;
            }
            if (!Valid(owner.Actor) || !Valid(owner.Mesh)) throw new InvalidOperationException("Actor resources are unavailable.");
            var asset = owner.Mesh.SkeletalMesh; var skeleton = asset == null ? null : asset.Skeleton; var anim = owner.Mesh.GetAnimInstance();
            if (!Valid(asset) || !Valid(skeleton) || !Valid(anim)) throw new InvalidOperationException("Actor resources are still loading.");
            if (!String.IsNullOrEmpty(owner.AssetName) && !String.Equals(asset.GetName(), owner.AssetName, StringComparison.Ordinal)) throw new InvalidOperationException("Loaded actor uses a different requested mesh asset.");
            if (owner.ActorAddress != 0 && (owner.ActorAddress != Address(owner.Actor) || owner.MeshAddress != Address(owner.Mesh) || Address(owner.Asset) != Address(asset) || Address(owner.Skeleton) != Address(skeleton) || Address(owner.Anim) != Address(anim))) throw new InvalidOperationException("Actor graph changed during preparation.");
            owner.Asset = asset; owner.Skeleton = skeleton; owner.Anim = anim; owner.ActorAddress = Address(owner.Actor); owner.MeshAddress = Address(owner.Mesh);
            if (owner.FormStrategy == "mesh_parts")
            {
                var children = owner.ActorComponent.ChildMeshComponentList;
                if (children == null || children.Count != owner.ChildMeshPaths.Length) throw new InvalidOperationException("Form child mesh count differs.");
                var components = new HashSet<ulong>(); components.Add(owner.MeshAddress);
                for (var index = 0; index < children.Count; index++)
                {
                    var child = children[index]; var expectedName = owner.ChildMeshPaths[index].Substring(owner.ChildMeshPaths[index].LastIndexOf('.') + 1);
                    if (!Valid(child) || !Valid(child.SkeletalMesh) || child.SkeletalMesh.GetName() != expectedName || Address(child) == owner.MeshAddress || (owner.FormChildren != null && Address(child) != Address(owner.FormChildren[index]))) throw new InvalidOperationException("Form child mesh identity differs.");
                    if (!components.Add(Address(child))) throw new InvalidOperationException("Form parts share a mesh component.");
                    foreach (var other in s_owners) if (!Object.ReferenceEquals(other, owner) && !other.Released)
                    {
                        if (other.MeshAddress == Address(child)) throw new InvalidOperationException("Form child belongs to another actor slot.");
                        if (other.FormChildren != null) foreach (var sibling in other.FormChildren) if (Address(sibling) == Address(child)) throw new InvalidOperationException("Actor slots share a form child.");
                    }
                }
                if (owner.FormChildren == null) owner.FormChildren = new List<USkeletalMeshComponent>(children);
            }
            foreach (var other in s_owners)
                if (!Object.ReferenceEquals(other, owner) && !other.Released && other.MeshAddress != 0 && (other.ActorAddress == owner.ActorAddress || other.MeshAddress == owner.MeshAddress || Address(other.Anim) == Address(owner.Anim)))
                    throw new InvalidOperationException("Actor slots must not share an actor, mesh component or animation instance.");
        }
        private static void ApplyForm(Owner owner)
        {
            if (owner.FormApplied)
            {
                if (owner.Morph != null && (int)owner.Morph.GetMorphType() != owner.MorphType) throw new InvalidOperationException("Actor form changed after preparation.");
                return;
            }
            if (owner.FormStrategy == "role_morph")
            {
                owner.Morph = owner.Role.Model.GetComponent<UiRoleMorphComponent>();
                if (owner.Morph == null) throw new InvalidOperationException("Role morph component is unavailable.");
                if ((int)owner.Morph.GetMorphType() != owner.MorphType && !owner.Morph.SetMorphType((EUiModelMorphType)owner.MorphType)) throw new InvalidOperationException("Requested role form was not loaded.");
                if ((int)owner.Morph.GetMorphType() != owner.MorphType) throw new InvalidOperationException("Requested role form differs.");
            }
            else if (owner.FormStrategy == "mesh_parts")
            {
                var main = owner.Loader.GetLoadedResource(owner.MainMeshPath) as USkeletalMesh;
                var anim = owner.Loader.GetLoadedResource(owner.AnimClassPath) as UClass;
                if (!Valid(main) || !Valid(main.Skeleton) || !Valid(anim)) throw new InvalidOperationException("Form resources are still loading.");
                var children = new List<USkeletalMesh>();
                foreach (var path in owner.ChildMeshPaths)
                {
                    var child = owner.Loader.GetLoadedResource(path) as USkeletalMesh;
                    if (!Valid(child) || !Valid(child.Skeleton) || Address(child.Skeleton) != Address(main.Skeleton)) throw new InvalidOperationException("Form child mesh has a different skeleton.");
                    children.Add(child);
                }
                owner.ActorComponent.ChangeMesh(main, anim, children, null, 0);
            }
            owner.FormApplied = true;
        }
        public static void CheckLoaded()
        {
            VerifySession(); var shown = true; foreach (var owner in s_owners) { Loaded(owner); shown &= owner.Shown; }
            if (shown && !s_graphExported) { Export(); s_graphExported = true; }
        }
        private static Snapshot Capture(Owner owner)
        {
            var mesh = owner.Mesh;
            var rotation = owner.Actor.K2_GetActorRotation();
            return new Snapshot { Location = owner.Actor.D_K2_GetActorLocation(), Rotation = rotation, MeshYawOffset = Math.IEEERemainder(mesh.K2_GetComponentRotation().Yaw - rotation.Yaw, 360), Hidden = owner.Actor.bHidden, ActorTick = owner.Actor.IsActorTickEnabled(), ActorInterval = owner.Actor.GetActorTickInterval(), MeshVisible = mesh.bVisible, MeshHidden = mesh.bHiddenInGame, MeshTick = mesh.IsComponentTickEnabled(), MeshInterval = mesh.GetComponentTickInterval(), MeshActive = mesh.IsActive(), PauseAnims = mesh.bPauseAnims, NoSkeletonUpdate = mesh.bNoSkeletonUpdate, AutonomousOnly = mesh.bOnlyAllowAutonomousTickPose, UpdateRate = mesh.bEnableUpdateRateOptimizations, RenderStatic = mesh.bRenderStatic, AnimTick = mesh.VisibilityBasedAnimTickOption };
        }
        private static void EnableEvaluation(Owner owner)
        {
            owner.Actor.SetActorHiddenInGame(false); owner.Actor.SetActorTickEnabled(true); owner.Actor.SetActorTickInterval(0);
            EnableMeshEvaluation(owner.Mesh);
            if (owner.FormChildren != null) foreach (var child in owner.FormChildren) EnableMeshEvaluation(child);
        }
        private static void EnableMeshEvaluation(USkeletalMeshComponent mesh)
        {
            mesh.SetActive(true, false); mesh.SetVisibility(true, false); mesh.SetHiddenInGame(false, false); mesh.SetComponentTickEnabled(true); mesh.SetComponentTickInterval(0);
            mesh.bPauseAnims = false; mesh.bNoSkeletonUpdate = false; mesh.bOnlyAllowAutonomousTickPose = false; mesh.bEnableUpdateRateOptimizations = false; mesh.bRenderStatic = false;
            mesh.VisibilityBasedAnimTickOption = EVisibilityBasedAnimTickOption.AlwaysTickPoseAndRefreshBones;
        }
        private static void ValidatePlayerVisibility(PlayerVisibility saved)
        {
            var target = saved.Target; var character = target.Actor as ACharacter;
            if (!HandleCurrent(target.Handle, target.Entity) || !Valid(target.Actor) || !Valid(target.Mesh) ||
                !Valid(character) || !Valid(character.Mesh) || Address(character.Mesh) != saved.MeshAddress ||
                !Object.ReferenceEquals(target.Entity.GetComponent<CharacterActorComponent>(), target.Component) ||
                Address(target.Actor) != saved.ActorAddress || Address(target.Mesh) != saved.MeshAddress ||
                !Valid((AActor)Bind().ActorInternal.GetValue(target.Component)) || !Valid(target.Component.SkeletalMesh) ||
                Address((AActor)Bind().ActorInternal.GetValue(target.Component)) != saved.ActorAddress || Address(target.Component.SkeletalMesh) != saved.MeshAddress)
                throw new InvalidOperationException("Non-performing player identity changed; no replacement model will be modified.");
        }
        private static void HidePlayerModel(PlayerVisibility saved)
        {
            ValidatePlayerVisibility(saved);
            var pawn = UGameplayStatics.GetPlayerPawn(s_owners[0].Actor, 0);
            if (!Valid(pawn) || Address(pawn) != saved.ActorAddress) throw new InvalidOperationException("Controlled player changed during staging.");
            if (!saved.Target.Actor.bHidden) saved.Target.Actor.SetActorHiddenInGame(true);
            if (saved.Target.Mesh.bVisible) saved.Target.Mesh.SetVisibility(false, false);
            if (!saved.Target.Mesh.bHiddenInGame) saved.Target.Mesh.SetHiddenInGame(true, false);
        }
        private static void HideNonPerformingPlayer()
        {
            if (s_playerVisibility != null) { if (!s_playerVisibility.Released) HidePlayerModel(s_playerVisibility); return; }
            var pawn = UGameplayStatics.GetPlayerPawn(s_owners[0].Actor, 0) as ACharacter;
            if (!Valid(pawn) || !Valid(pawn.Mesh)) throw new InvalidOperationException("Controlled player model is unavailable.");
            var actorAddress = Address(pawn); var meshAddress = Address(pawn.Mesh);
            // Instance identity matters: a created copy of the same character
            // does not make the original controlled model a performer.
            foreach (var owner in s_owners) if (Address(owner.Actor) == actorAddress || Address(owner.Mesh) == meshAddress) return;
            Candidate target = null;
            foreach (var candidate in CurrentTeam()) if (Address(candidate.Actor) == actorAddress && Address(candidate.Mesh) == meshAddress) { target = candidate; break; }
            if (target == null) throw new InvalidOperationException("Cannot verify the non-performing controlled player's team identity.");
            var saved = new PlayerVisibility { Target = target, ActorAddress = actorAddress, MeshAddress = meshAddress,
                ActorHidden = pawn.bHidden, MeshVisible = pawn.Mesh.bVisible, MeshHidden = pawn.Mesh.bHiddenInGame,
                HoldReason = "WWPoser:" + s_session + ":nonperforming-player" };
            // Record ownership before the first mutation so partial failures
            // use the same recovery path as actor setup failures.
            s_playerVisibility = saved;
            target.Handle.AddHoldEntity(saved.HoldReason); saved.HoldAdded = target.Handle.HoldEntityMap.ContainsKey(saved.HoldReason);
            if (!saved.HoldAdded) throw new InvalidOperationException("Could not retain the non-performing player model.");
            HidePlayerModel(saved);
        }
        private static void RestorePlayerVisibility()
        {
            var saved = s_playerVisibility; if (saved == null || saved.Released) return;
            ValidatePlayerVisibility(saved); var target = saved.Target; var errors = new List<string>();
            // Only display flags belong to this lease. Movement, collisions,
            // animation and tick settings of the non-performer remain its own.
            Attempt(delegate { target.Actor.SetActorHiddenInGame(saved.ActorHidden); }, errors, "actor_hidden");
            Attempt(delegate { target.Mesh.SetVisibility(saved.MeshVisible, false); }, errors, "visible");
            Attempt(delegate { target.Mesh.SetHiddenInGame(saved.MeshHidden, false); }, errors, "mesh_hidden");
            Attempt(delegate { if (target.Actor.bHidden != saved.ActorHidden || target.Mesh.bVisible != saved.MeshVisible || target.Mesh.bHiddenInGame != saved.MeshHidden) throw new InvalidOperationException("player visibility readback differs"); }, errors, "readback");
            if (errors.Count != 0) throw new InvalidOperationException(String.Join("; ", errors.ToArray()));
            if (saved.HoldAdded || target.Handle.HoldEntityMap.ContainsKey(saved.HoldReason))
            {
                if (target.Handle.HoldEntityMap.ContainsKey(saved.HoldReason)) target.Handle.RemoveHoldEntity(saved.HoldReason);
                if (target.Handle.HoldEntityMap.ContainsKey(saved.HoldReason)) throw new InvalidOperationException("Player visibility retention is still active.");
                saved.HoldAdded = false;
            }
            saved.Released = true;
        }
        public static void ShowAtLocal()
        {
            VerifySession(); foreach (var owner in s_owners) { Loaded(owner); if (owner.Before == null) owner.Before = Capture(owner); }
            s_maintenanceEnabled = true;
            // Capture once before moving any actor: the first borrowed actor can
            // itself be the local pawn that supplies the stage coordinate frame.
            if (s_stage == null)
            {
                var pawn = UGameplayStatics.GetPlayerPawn(s_owners[0].Actor, 0); var capsule = ((ACharacter)pawn).CapsuleComponent;
                var pawnMesh = ((ACharacter)pawn).Mesh;
                if (!Valid(pawnMesh)) throw new InvalidOperationException("Stage reference mesh is unavailable.");
                var right = pawn.GetActorRightVector(); var forward = pawn.GetActorForwardVector();
                var rightLength = Math.Sqrt((double)right.X * right.X + (double)right.Y * right.Y); var forwardLength = Math.Sqrt((double)forward.X * forward.X + (double)forward.Y * forward.Y);
                if (rightLength == 0 || forwardLength == 0) throw new InvalidOperationException("Stage basis is unavailable.");
                var stageMeshRotation = pawnMesh.K2_GetComponentRotation();
                s_stage = new Stage { Origin = pawn.D_K2_GetActorLocation(), Rotation = pawn.K2_GetActorRotation(), MeshYaw = stageMeshRotation.Yaw, MeshPitch = stageMeshRotation.Pitch, MeshRoll = stageMeshRotation.Roll, Floor = capsule.D_K2_GetComponentLocation().Z - capsule.D_GetScaledCapsuleHalfHeight(), RightX = right.X / rightLength, RightY = right.Y / rightLength, ForwardX = forward.X / forwardLength, ForwardY = forward.Y / forwardLength };
                CaptureFormationFloor((ACharacter)pawn, s_stage);
            }
            foreach (var owner in s_owners)
            {
                if (owner.Shown) continue; var actorHeight = 0.0; var character = owner.Actor as ACharacter;
                if (owner.Borrowed && character != null) actorHeight = character.CapsuleComponent.D_GetScaledCapsuleHalfHeight();
                var destination = new FVectorDouble(s_stage.Origin.X + s_stage.RightX * owner.X + s_stage.ForwardX * owner.Y, s_stage.Origin.Y + s_stage.RightY * owner.X + s_stage.ForwardY * owner.Y, s_stage.Floor + owner.Z + actorHeight);
                // UI actors and gameplay characters can use different mesh-to-actor
                // rotations. Align their mesh bases, not just their Actor yaws.
                // Both the reference and offsets were captured before any actor moved.
                var targetRotation = s_stage.Rotation;
                targetRotation.Yaw = (float)Math.IEEERemainder(s_stage.MeshYaw + owner.Yaw - owner.Before.MeshYawOffset, 360);
                var hit = new FHitResult();
                owner.StageLocation = destination; owner.StageRotation = targetRotation;
                if (!owner.Actor.D_K2_SetActorLocationAndRotation(destination, targetRotation, false, ref hit, true)) throw new InvalidOperationException("Actor placement failed.");
                if (!owner.Borrowed) { UKuroRenderingRuntimeBPPluginBPLibrary.SetActorUISceneRendering(owner.Role, false); owner.Data.SetVisible(true); }
                EnableEvaluation(owner); owner.Shown = true;
            }
            HideNonPerformingPlayer();
            if (!s_stage.CameraReferenceCaptured)
            {
                // Subtract in D_/global space, then let native camera code add
                // this displacement to the first mesh's current local position.
                // The displacement survives world-origin rebases; a captured
                // absolute local coordinate would become stale after a rebase.
                var meshLocation = s_owners[0].Mesh.D_K2_GetComponentLocation();
                s_stage.CameraOffsetFromMesh = new FVectorDouble(s_stage.Origin.X - meshLocation.X, s_stage.Origin.Y - meshLocation.Y, s_stage.Floor - meshLocation.Z);
                s_stage.CameraReferenceCaptured = true;
            }
            WriteLifecycle();
        }
        private static void CaptureFormationFloor(ACharacter pawn, Stage stage)
        {
            var path = Path.Combine(s_session, "stage_floor.tsv");
            if (!File.Exists(path)) return;
            var policy = File.ReadAllText(path, Encoding.UTF8).TrimEnd('\r', '\n').Split('\t');
            if (policy.Length != 2 || (policy[0] != "walkable_floor" && policy[0] != "capsule_bottom")) throw new InvalidDataException("Invalid stage floor policy.");
            if (policy[0] == "capsule_bottom") return;
            var fallback = Double.Parse(policy[1], CultureInfo.InvariantCulture);
            if (Double.IsNaN(fallback) || Double.IsInfinity(fallback) || fallback < 0 || fallback > 5) throw new InvalidDataException("Stage floor fallback is outside the small correction range.");
            stage.HeightCorrection = -fallback; stage.HeightSource = "fallback";
            // Capture before any borrowed pawn is moved. CurrentFloor is the
            // engine's existing floor result; this does not issue a new trace.
            var movement = pawn.CharacterMovement;
            if (!Valid(movement)) return;
            var mode = (byte)movement.MovementMode;
            if (mode != 1 && mode != 2) return; // Walking / NavWalking only.
            var floor = movement.CurrentFloor;
            if (floor == null || !floor.bBlockingHit || !floor.bWalkableFloor) return;
            var distance = floor.bLineTrace ? floor.LineDist : floor.FloorDist;
            if (Double.IsNaN(distance) || Double.IsInfinity(distance) || distance < 0 || distance > 5) return;
            stage.HeightCorrection = -distance; stage.HeightSource = floor.bLineTrace ? "line_floor_distance" : "sweep_floor_distance";
        }
        private static void Maintain(Owner owner)
        {
            if (!HandleCurrent(owner.Handle, owner.Entity) || !Valid(owner.Actor) || !Valid(owner.Mesh) || Address((AActor)Bind().ActorInternal.GetValue(owner.BorrowedComponent)) != owner.ActorAddress || Address(owner.BorrowedComponent.SkeletalMesh) != owner.MeshAddress || Address(owner.Actor) != owner.ActorAddress || Address(owner.Mesh) != owner.MeshAddress) throw new InvalidOperationException("Borrowed actor identity changed during staging.");
            var position = owner.Actor.D_K2_GetActorLocation(); var rotation = owner.Actor.K2_GetActorRotation(); var target = owner.StageLocation; var facing = owner.StageRotation;
            if (!Near(position.X, target.X) || !Near(position.Y, target.Y) || !Near(position.Z, target.Z) || !RotationNear(rotation.Yaw, facing.Yaw) || !RotationNear(rotation.Pitch, facing.Pitch) || !RotationNear(rotation.Roll, facing.Roll))
            {
                var hit = new FHitResult(); if (!owner.Actor.D_K2_SetActorLocationAndRotation(target, facing, false, ref hit, true)) throw new InvalidOperationException("Borrowed stage transform update failed.");
            }
            if (owner.Actor.bHidden) owner.Actor.SetActorHiddenInGame(false);
            if (!owner.Actor.IsActorTickEnabled()) owner.Actor.SetActorTickEnabled(true);
            if (owner.Actor.GetActorTickInterval() != 0) owner.Actor.SetActorTickInterval(0);
            var mesh = owner.Mesh;
            if (!mesh.IsActive()) mesh.SetActive(true, false);
            if (!mesh.bVisible) mesh.SetVisibility(true, false);
            if (mesh.bHiddenInGame) mesh.SetHiddenInGame(false, false);
            if (!mesh.IsComponentTickEnabled()) mesh.SetComponentTickEnabled(true);
            if (mesh.GetComponentTickInterval() != 0) mesh.SetComponentTickInterval(0);
            if (mesh.bPauseAnims) mesh.bPauseAnims = false;
            if (mesh.bNoSkeletonUpdate) mesh.bNoSkeletonUpdate = false;
            if (mesh.bOnlyAllowAutonomousTickPose) mesh.bOnlyAllowAutonomousTickPose = false;
            if (mesh.bEnableUpdateRateOptimizations) mesh.bEnableUpdateRateOptimizations = false;
            if (mesh.bRenderStatic) mesh.bRenderStatic = false;
            if (mesh.VisibilityBasedAnimTickOption != EVisibilityBasedAnimTickOption.AlwaysTickPoseAndRefreshBones) mesh.VisibilityBasedAnimTickOption = EVisibilityBasedAnimTickOption.AlwaysTickPoseAndRefreshBones;
        }
        public static void MaintainStage()
        {
            VerifySession(); if (!s_maintenanceEnabled) return; var errors = new List<string>();
            foreach (var owner in s_owners) if (owner.Borrowed && owner.Shown && !owner.Released) Attempt(delegate { Maintain(owner); }, errors, "slot " + owner.Slot.ToString(CultureInfo.InvariantCulture));
            foreach (var owner in s_owners) if (!owner.Borrowed && owner.IndependentInstance && owner.Shown && !owner.Released) Attempt(delegate { Loaded(owner); }, errors, "form slot " + owner.Slot.ToString(CultureInfo.InvariantCulture));
            if (s_playerVisibility != null && !s_playerVisibility.Released) Attempt(delegate { HidePlayerModel(s_playerVisibility); }, errors, "non-performing player visibility");
            if (errors.Count != 0) throw new InvalidOperationException(String.Join("; ", errors.ToArray()));
        }
        private static void Pointer(StringBuilder output, string name, UnrealObject value) { output.Append('"').Append(name).Append("\":\"0x").Append(Address(value).ToString("x", CultureInfo.InvariantCulture)).Append('"'); }
        private static void Export()
        {
            var context = s_owners[0].Actor; var pawn = UGameplayStatics.GetPlayerPawn(context, 0); var controller = UGameplayStatics.GetPlayerController(context, 0);
            var output = new StringBuilder("{\"schema\":2,\"status\":\"owned_mmd_graph_exported\","); Pointer(output, "player_pawn", pawn); output.Append(','); Pointer(output, "player_controller", controller);
            if (s_owners.Count == 1) { var one = s_owners[0]; output.Append(','); Pointer(output, "actor", one.Actor); output.Append(','); Pointer(output, "mesh", one.Mesh); output.Append(','); Pointer(output, "asset", one.Asset); output.Append(','); Pointer(output, "skeleton", one.Skeleton); output.Append(','); Pointer(output, "main_anim", one.Anim); }
            output.Append(",\"actors\":[");
            for (int i = 0; i < s_owners.Count; i++)
            {
                var owner = s_owners[i]; if (i != 0) output.Append(','); output.Append("{\"slot\":").Append(owner.Slot).Append(",\"ownership\":\"").Append(owner.Borrowed ? "borrowed" : "created").Append("\",");
                output.Append("\"form_id\":\"").Append(owner.FormId).Append("\",\"form_strategy\":\"").Append(owner.FormStrategy).Append("\",");
                Pointer(output, "actor", owner.Actor); output.Append(','); Pointer(output, "mesh", owner.Mesh); output.Append(','); Pointer(output, "asset", owner.Asset); output.Append(','); Pointer(output, "skeleton", owner.Skeleton); output.Append(','); Pointer(output, "main_anim", owner.Anim);
                output.Append(",\"form_children\":[");
                if (owner.FormChildren != null) for (var child = 0; child < owner.FormChildren.Count; child++) { if (child != 0) output.Append(','); output.Append('{'); Pointer(output, "mesh", owner.FormChildren[child]); output.Append(','); Pointer(output, "asset", owner.FormChildren[child].SkeletalMesh); output.Append('}'); }
                output.Append("]}");
            }
            output.Append("]}\n"); File.WriteAllText(Path.Combine(s_session, "owned_mmd_graph.json"), output.ToString(), new UTF8Encoding(false));
        }
        private static void Attempt(Action action, List<string> errors, string operation) { try { action(); } catch (Exception error) { errors.Add(operation + ": " + error.Message); } }
        private static bool Near(double left, double right) { return Math.Abs(left - right) < 0.01; }
        private static bool RotationNear(float left, float right) { return Math.Abs(Math.IEEERemainder(left - right, 360)) < 0.01; }
        private static bool Restored(Owner owner)
        {
            if (owner.Before == null) return true; var saved = owner.Before; var now = Capture(owner);
            return Near(saved.Location.X, now.Location.X) && Near(saved.Location.Y, now.Location.Y) && Near(saved.Location.Z, now.Location.Z) && RotationNear(saved.Rotation.Yaw, now.Rotation.Yaw) && RotationNear(saved.Rotation.Pitch, now.Rotation.Pitch) && RotationNear(saved.Rotation.Roll, now.Rotation.Roll) && saved.Hidden == now.Hidden && saved.ActorTick == now.ActorTick && saved.ActorInterval == now.ActorInterval && saved.MeshVisible == now.MeshVisible && saved.MeshHidden == now.MeshHidden && saved.MeshTick == now.MeshTick && saved.MeshInterval == now.MeshInterval && saved.MeshActive == now.MeshActive && saved.PauseAnims == now.PauseAnims && saved.NoSkeletonUpdate == now.NoSkeletonUpdate && saved.AutonomousOnly == now.AutonomousOnly && saved.UpdateRate == now.UpdateRate && saved.RenderStatic == now.RenderStatic && saved.AnimTick == now.AnimTick;
        }
        private static void RestoreBorrowed(Owner owner)
        {
            if (!HandleCurrent(owner.Handle, owner.Entity) || !Valid(owner.Actor) || !Valid(owner.Mesh) || Address((AActor)Bind().ActorInternal.GetValue(owner.BorrowedComponent)) != Address(owner.Actor) || Address(owner.BorrowedComponent.SkeletalMesh) != Address(owner.Mesh) || (owner.ActorAddress != 0 && (Address(owner.Actor) != owner.ActorAddress || Address(owner.Mesh) != owner.MeshAddress))) throw new InvalidOperationException("Borrowed actor identity no longer matches; no replacement actor will be modified.");
            var errors = new List<string>(); var saved = owner.Before; var mesh = owner.Mesh;
            if (saved != null)
            {
                Attempt(delegate { var hit = new FHitResult(); if (!owner.Actor.D_K2_SetActorLocationAndRotation(saved.Location, saved.Rotation, false, ref hit, true)) throw new InvalidOperationException("transform rejected"); }, errors, "transform");
                Attempt(delegate { mesh.SetActive(saved.MeshActive, false); }, errors, "active");
                Attempt(delegate { mesh.bPauseAnims = saved.PauseAnims; }, errors, "pause_anims");
                Attempt(delegate { mesh.bNoSkeletonUpdate = saved.NoSkeletonUpdate; }, errors, "skeleton_update");
                Attempt(delegate { mesh.bOnlyAllowAutonomousTickPose = saved.AutonomousOnly; }, errors, "autonomous_pose");
                Attempt(delegate { mesh.bEnableUpdateRateOptimizations = saved.UpdateRate; }, errors, "update_rate");
                Attempt(delegate { mesh.bRenderStatic = saved.RenderStatic; }, errors, "render_static");
                Attempt(delegate { mesh.VisibilityBasedAnimTickOption = saved.AnimTick; }, errors, "anim_tick_policy");
                Attempt(delegate { mesh.SetVisibility(saved.MeshVisible, false); }, errors, "visible");
                Attempt(delegate { mesh.SetHiddenInGame(saved.MeshHidden, false); }, errors, "mesh_hidden");
                Attempt(delegate { mesh.SetComponentTickInterval(saved.MeshInterval); mesh.SetComponentTickEnabled(saved.MeshTick); }, errors, "mesh_tick");
                Attempt(delegate { owner.Actor.SetActorTickInterval(saved.ActorInterval); owner.Actor.SetActorTickEnabled(saved.ActorTick); }, errors, "actor_tick");
                Attempt(delegate { owner.Actor.SetActorHiddenInGame(saved.Hidden); }, errors, "actor_hidden");
                Attempt(delegate { if (!Restored(owner)) throw new InvalidOperationException("borrowed state readback differs"); }, errors, "readback");
            }
            if (errors.Count != 0) throw new InvalidOperationException(String.Join("; ", errors.ToArray()));
            if (owner.HoldAdded)
            {
                if (owner.Handle.HoldEntityMap.ContainsKey(owner.HoldReason)) owner.Handle.RemoveHoldEntity(owner.HoldReason);
                if (owner.Handle.HoldEntityMap.ContainsKey(owner.HoldReason)) throw new InvalidOperationException("Borrowed actor retention is still active."); owner.HoldAdded = false;
            }
            owner.Released = true;
        }
        private static void DestroyCreated(Owner owner)
        {
            if (Object.ReferenceEquals(owner.Role, null)) { owner.Released = true; return; }
            if (!owner.HasIndex) { owner.Index = owner.Role.GetRoleActorIndex(); owner.HasIndex = true; }
            var map = Map(owner); TsUiSceneRoleActor registered;
            if (!map.TryGetValue(owner.Index, out registered)) { owner.DestroyReturned = true; owner.Released = true; return; }
            if (!Object.ReferenceEquals(registered, owner.Role)) throw new InvalidOperationException("Created actor index now belongs to another owner.");
            var destroyed = (bool)Bind().Destroy.Invoke(owner.Manager, new object[] { owner.Index });
            if (!destroyed && Map(owner).ContainsKey(owner.Index)) throw new InvalidOperationException("Owned role destroy returned false.");
            owner.DestroyReturned = true; owner.Released = !Map(owner).ContainsKey(owner.Index); if (!owner.Released) throw new InvalidOperationException("Owned role is still registered.");
        }
        public static void Stop()
        {
            VerifySession(); s_maintenanceEnabled = false; var errors = new List<string>();
            foreach (var owner in s_owners) { if (owner.Released) continue; Attempt(delegate { if (owner.Borrowed) RestoreBorrowed(owner); else DestroyCreated(owner); }, errors, "slot " + owner.Slot.ToString(CultureInfo.InvariantCulture)); }
            Attempt(RestorePlayerVisibility, errors, "non-performing player visibility");
            WriteLifecycle(); if (errors.Count != 0) throw new InvalidOperationException(String.Join("; ", errors.ToArray()));
        }
        public static void CheckReleased()
        {
            VerifySession();
            if (s_playerVisibility != null && (!s_playerVisibility.Released || s_playerVisibility.HoldAdded)) throw new InvalidOperationException("Non-performing player visibility still requires restoration.");
            foreach (var owner in s_owners)
                if (!owner.Released || (owner.Borrowed && owner.HoldAdded) || (!owner.Borrowed && owner.HasIndex && Map(owner).ContainsKey(owner.Index))) throw new InvalidOperationException("Actor slot still requires restoration: " + owner.Slot.ToString(CultureInfo.InvariantCulture));
        }
        private static void WriteLifecycle()
        {
            var output = new StringBuilder("{\"schema\":1,\"actors\":[");
            for (int i = 0; i < s_owners.Count; i++)
            {
                var owner = s_owners[i]; if (i != 0) output.Append(',');
                output.Append("{\"slot\":").Append(owner.Slot).Append(",\"role_id\":").Append(owner.RequestedRoleId).Append(",\"skin_id\":").Append(owner.RequestedSkinId).Append(",\"ownership\":\"").Append(owner.Borrowed ? "borrowed" : "created").Append("\",\"shown\":").Append(owner.Shown ? "true" : "false").Append(",\"released\":").Append(owner.Released ? "true" : "false").Append(",\"retained\":").Append(owner.HoldAdded ? "true" : "false");
                output.Append(",\"form_id\":\"").Append(owner.FormId).Append("\",\"form_strategy\":\"").Append(owner.FormStrategy).Append("\",\"form_applied\":").Append(owner.FormApplied ? "true" : "false").Append(",\"independent_instance\":").Append(owner.IndependentInstance ? "true" : "false").Append(",\"morph_type\":").Append(owner.MorphType);
                if (owner.Before != null)
                {
                    output.Append(",\"requested_stage_yaw\":").Append(owner.Yaw.ToString("R", CultureInfo.InvariantCulture));
                    output.Append(",\"mesh_yaw_offset\":").Append(owner.Before.MeshYawOffset.ToString("R", CultureInfo.InvariantCulture));
                    if (owner.Shown) output.Append(",\"stage_actor_yaw\":").Append(owner.StageRotation.Yaw.ToString("R", CultureInfo.InvariantCulture));
                }
                output.Append('}');
            }
            output.Append("],\"all_released\":").Append(AllReleased() ? "true" : "false");
            if (s_playerVisibility != null)
            {
                var saved = s_playerVisibility;
                output.Append(",\"nonperforming_player\":{\"actor\":\"0x").Append(saved.ActorAddress.ToString("x", CultureInfo.InvariantCulture))
                    .Append("\",\"mesh\":\"0x").Append(saved.MeshAddress.ToString("x", CultureInfo.InvariantCulture))
                    .Append("\",\"restored\":").Append(saved.Released ? "true" : "false").Append(",\"retained\":").Append(saved.HoldAdded ? "true" : "false")
                    .Append(",\"original_actor_hidden\":").Append(saved.ActorHidden ? "true" : "false").Append(",\"original_mesh_visible\":").Append(saved.MeshVisible ? "true" : "false")
                    .Append(",\"original_mesh_hidden\":").Append(saved.MeshHidden ? "true" : "false").Append('}');
            }
            if (s_stage != null)
            {
                // Absolute D_ coordinates remain placement diagnostics. Camera
                // code consumes only the saved displacement below, never these
                // global coordinates as an absolute local POV position.
                output.Append(",\"stage_reference_actor_yaw\":").Append(s_stage.Rotation.Yaw.ToString("R", CultureInfo.InvariantCulture))
                    .Append(",\"stage_reference_mesh_yaw\":").Append(s_stage.MeshYaw.ToString("R", CultureInfo.InvariantCulture))
                    .Append(",\"stage_reference_mesh_pitch\":").Append(s_stage.MeshPitch.ToString("R", CultureInfo.InvariantCulture))
                    .Append(",\"stage_reference_mesh_roll\":").Append(s_stage.MeshRoll.ToString("R", CultureInfo.InvariantCulture))
                    .Append(",\"stage_reference_location\":[").Append(s_stage.Origin.X.ToString("R", CultureInfo.InvariantCulture)).Append(',').Append(s_stage.Origin.Y.ToString("R", CultureInfo.InvariantCulture)).Append(',').Append(s_stage.Origin.Z.ToString("R", CultureInfo.InvariantCulture)).Append(']')
                    .Append(",\"stage_reference_floor\":").Append(s_stage.Floor.ToString("R", CultureInfo.InvariantCulture))
                    .Append(",\"stage_height_correction_cm\":").Append(s_stage.HeightCorrection.ToString("R", CultureInfo.InvariantCulture))
                    .Append(",\"stage_height_correction_source\":\"").Append(s_stage.HeightSource).Append('"')
                    .Append(",\"stage_reference_right\":[").Append(s_stage.RightX.ToString("R", CultureInfo.InvariantCulture)).Append(',').Append(s_stage.RightY.ToString("R", CultureInfo.InvariantCulture)).Append(']')
                    .Append(",\"stage_reference_forward\":[").Append(s_stage.ForwardX.ToString("R", CultureInfo.InvariantCulture)).Append(',').Append(s_stage.ForwardY.ToString("R", CultureInfo.InvariantCulture)).Append(']');
                if (s_stage.CameraReferenceCaptured)
                    output.Append(",\"stage_reference_camera_offset_from_mesh\":[").Append(s_stage.CameraOffsetFromMesh.X.ToString("R", CultureInfo.InvariantCulture)).Append(',').Append(s_stage.CameraOffsetFromMesh.Y.ToString("R", CultureInfo.InvariantCulture)).Append(',').Append(s_stage.CameraOffsetFromMesh.Z.ToString("R", CultureInfo.InvariantCulture)).Append(']');
            }
            output.Append("}\n"); File.WriteAllText(Path.Combine(s_session, "managed_actor_lifecycle.json"), output.ToString(), new UTF8Encoding(false));
        }
    }
}
