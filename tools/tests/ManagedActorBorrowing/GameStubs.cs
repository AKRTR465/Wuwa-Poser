// Synthetic engine/business objects. No game assemblies or native API calls.
using UnrealEngine;
namespace UnrealEngine
{
    public class UnrealObject { private static long next = 100; internal IntPtr _NativePtr = new IntPtr(++next); public bool Valid = true; public virtual bool IsValid() => Valid; }
    public class UObject : UnrealObject { public string Name = "object"; public string GetName() => Name; }
    public struct FVectorDouble { public double X, Y, Z; public FVectorDouble(double x, double y, double z) { X = x; Y = y; Z = z; } }
    public struct FVector { public float X, Y, Z; public FVector(float x, float y, float z) { X = x; Y = y; Z = z; } }
    public struct FRotator { public float Pitch, Yaw, Roll; }
    public struct FHitResult { }
    public enum EVisibilityBasedAnimTickOption { AlwaysTickPoseAndRefreshBones, AlwaysTickPose, OnlyTickMontagesWhenNotRendered, OnlyTickPoseWhenRendered }
    public class AActor : UObject
    {
        public FVectorDouble Location; public FRotator Rotation; public bool bHidden = true, Tick; public float Interval = .5f; public int FailLocations;
        public FVectorDouble D_K2_GetActorLocation() => Location; public FRotator K2_GetActorRotation() => Rotation;
        public FVector K2_GetActorLocation() => new((float)(Location.X - UGameplayStatics.WorldOrigin.X), (float)(Location.Y - UGameplayStatics.WorldOrigin.Y), (float)(Location.Z - UGameplayStatics.WorldOrigin.Z));
        public bool D_K2_SetActorLocationAndRotation(FVectorDouble location, FRotator rotation, bool sweep, ref FHitResult hit, bool teleport) { if (FailLocations > 0) { FailLocations--; return false; } Location = location; Rotation = rotation; return true; }
        public bool IsActorTickEnabled() => Tick; public float GetActorTickInterval() => Interval;
        public void SetActorHiddenInGame(bool hidden) => bHidden = hidden; public void SetActorTickEnabled(bool enabled) => Tick = enabled; public void SetActorTickInterval(float value) => Interval = value;
        public FVector GetActorRightVector() { var yaw = Rotation.Yaw * Math.PI / 180; return new((float)-Math.Sin(yaw), (float)Math.Cos(yaw), 0); }
        public FVector GetActorForwardVector() { var yaw = Rotation.Yaw * Math.PI / 180; return new((float)Math.Cos(yaw), (float)Math.Sin(yaw), 0); }
    }
    public class ACharacter : AActor { public UCapsuleComponent CapsuleComponent; private USkeletalMeshComponent mesh; public USkeletalMeshComponent Mesh { get => mesh; set { mesh = value; if (value != null) value.Owner = this; } } public ACharacter() { CapsuleComponent = new() { Owner = this }; } }
    public class UCapsuleComponent : UObject { public AActor Owner; public double HalfHeight = 90; public FVectorDouble D_K2_GetComponentLocation() => Owner.Location; public double D_GetScaledCapsuleHalfHeight() => HalfHeight; }
    public class USkeletalMesh : UObject { public USkeleton Skeleton = new(); }
    public class USkeleton : UObject { }
    public class UAnimInstance : UObject { }
    public class USkeletalMeshComponent : UObject
    {
        public AActor Owner; public FRotator RelativeRotation; public FVectorDouble RelativeLocation;
        public FVectorDouble D_K2_GetComponentLocation() => new(Owner.Location.X + RelativeLocation.X, Owner.Location.Y + RelativeLocation.Y, Owner.Location.Z + RelativeLocation.Z);
        public FVector K2_GetComponentLocation() { var position = D_K2_GetComponentLocation(); return new((float)(position.X - UGameplayStatics.WorldOrigin.X), (float)(position.Y - UGameplayStatics.WorldOrigin.Y), (float)(position.Z - UGameplayStatics.WorldOrigin.Z)); }
        public FRotator K2_GetComponentRotation() => new() { Yaw = (float)Math.IEEERemainder((Owner?.Rotation.Yaw ?? 0) + RelativeRotation.Yaw, 360) };
        public USkeletalMesh SkeletalMesh; public UAnimInstance Anim = new();
        public bool bVisible, bHiddenInGame = true, Tick, Active, bPauseAnims = true, bNoSkeletonUpdate = true, bOnlyAllowAutonomousTickPose = true, bEnableUpdateRateOptimizations = true, bRenderStatic = true;
        public float Interval = .7f; public EVisibilityBasedAnimTickOption VisibilityBasedAnimTickOption = EVisibilityBasedAnimTickOption.OnlyTickPoseWhenRendered; public int FailVisibilityRestores;
        public UAnimInstance GetAnimInstance() => Anim; public bool IsComponentTickEnabled() => Tick; public float GetComponentTickInterval() => Interval; public bool IsActive() => Active;
        public void SetActive(bool value, bool reset) { Active = value; Tick = value; } public void SetVisibility(bool value, bool propagate) { if (!value && FailVisibilityRestores > 0) { FailVisibilityRestores--; throw new InvalidOperationException("synthetic visibility restore failure"); } bVisible = value; }
        public void SetHiddenInGame(bool value, bool propagate) => bHiddenInGame = value; public void SetComponentTickEnabled(bool value) => Tick = value; public void SetComponentTickInterval(float value) => Interval = value;
    }
    public static class UGameplayStatics { public static ACharacter Pawn; public static AActor Controller = new(); public static FVectorDouble WorldOrigin; public static AActor GetPlayerPawn(AActor context, int index) => Pawn; public static AActor GetPlayerController(AActor context, int index) => Controller; }
    public static class UKuroRenderingRuntimeBPPluginBPLibrary { public static void SetActorUISceneRendering(AActor actor, bool value) { } }
}
public enum EUiModelUseWay { RoleInRoleView }
public enum EUiModelLoadState { Empty, Loading, Loaded }
public class UiSceneRoleActorManager
{
    public static UiSceneRoleActorManager _instance = new(); public Dictionary<int, TsUiSceneRoleActor> UiSceneRoleActorMap = new(); public List<TsUiSceneRoleActor> Created = new(), Destroyed = new(); public int FailDestroys; public TsUiSceneRoleActor ReturnExisting; public int ReturnLatestAfter = int.MaxValue;
    public TsUiSceneRoleActor CreateUiSceneRoleActor(EUiModelUseWay way) { if (ReturnExisting != null) return ReturnExisting; if (Created.Count >= ReturnLatestAfter) return Created[^1]; var role = new TsUiSceneRoleActor { Index = Created.Count + 1 }; Created.Add(role); UiSceneRoleActorMap.Add(role.Index, role); return role; }
    public bool DestroyUiSceneRoleActor(int index) { if (FailDestroys > 0) { FailDestroys--; return false; } var role = UiSceneRoleActorMap[index]; Destroyed.Add(role); UiSceneRoleActorMap.Remove(index); role.Valid = false; return true; }
}
public class TsUiSceneRoleActor : AActor { public int Index; public UiModelBase Model; public TsUiSceneRoleActor() { Model = new(this); } public int GetRoleActorIndex() => Index; }
public class UiModelBase
{
    public UiRoleLoadComponent Loader; public UiModelActorComponent Actor = new(); public UiModelDataComponent Data = new();
    public UiModelBase(AActor owner) { Actor.Actor = owner; Loader = new() { Model = this }; }
    public T GetComponent<T>() { object value = typeof(T) == typeof(UiRoleLoadComponent) ? Loader : typeof(T) == typeof(UiModelActorComponent) ? Actor : Data; return (T)value; }
}
public class UiRoleLoadComponent { public UiModelBase Model; public void LoadModelByRoleConfigId(int role, int skin, bool value, object callback) { Model.Actor.MainMeshComponent = new() { Owner = Model.Actor.Actor, SkeletalMesh = new() { Name = $"Role{role}Skin{skin}" } }; Model.Data.ModelLoadStateInternal = EUiModelLoadState.Loaded; } }
public class UiModelActorComponent { public AActor Actor; public USkeletalMeshComponent MainMeshComponent; }
public class UiModelDataComponent { public EUiModelLoadState ModelLoadStateInternal; public void SetVisible(bool value) { } }
public enum ETeamGroupType { Default }
public class PlayerInfoModel { public static PlayerInfoModel _Instance = new(); public int Id = 7; }
public class SceneTeamModel { public static SceneTeamModel _Instance = new(); public Dictionary<int, SceneTeamPlayer> TeamPlayerMap = new(); public List<SceneTeamItem> TeamItems = new(); }
public class SceneTeamPlayer { public ETeamGroupType? CurrentGroupType = ETeamGroupType.Default; public Dictionary<ETeamGroupType, SceneTeamGroup> GroupMap = new(); }
public class SceneTeamGroup { public List<SceneTeamRole> RoleList = new(); }
public class SceneTeamRole { public int RoleId; public long CreatureDataId; }
public class SceneTeamItem { public int PlayerId = 7, ConfigId; public long CreatureDataId; public EntityHandle EntityHandleInternal; public bool IsMyRoleInternal = true; public ETeamGroupType GroupType; }
public class ObjectBase { public int Id, Index; }
public class Entity : ObjectBase { public int FlagInternal = 0x10; public Dictionary<Type, object> Components = new(); public T GetComponent<T>() where T : class => Components.TryGetValue(typeof(T), out var value) ? (T)value : null; }
public class WorldEntity : Entity { }
public class EntityHandle { public WorldEntity Entity; public int Id, Index; public long CreatureDataId; public bool PendingRemoving; public Dictionary<string, int> HoldEntityMap = new(); public bool AddHoldEntity(string key) { HoldEntityMap[key] = 1; return true; } public bool RemoveHoldEntity(string key) => HoldEntityMap.Remove(key); }
public class BaseActorComponent { public AActor ActorInternal; }
public class CharacterActorComponent : BaseActorComponent { public USkeletalMeshComponent SkeletalMesh; }
public class CreatureDataComponent { public int SkinId, RoleId, PlayerId; public long CreatureDataId; }
public class CharacterModel { public static CharacterModel _Instance = new(); public List<EntityHandle> Handles = new(); }
public class ObjectSystem { public static ObjectSystem _instance = new(); public List<ObjectBase> Objects = new(); public List<int> Versions = new(); }
