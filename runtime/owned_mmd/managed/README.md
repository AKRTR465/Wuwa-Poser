# Mixed actor ownership

`ManagedBridge` executes its public operations on the attached game thread. It
reads `actors.tsv` with sixteen tab-separated columns and no header (the legacy
eight-column format remains readable):

```text
slot, role_id, skin_id, stage_x_cm, stage_y_cm, stage_z_cm, model_asset_name, stage_yaw_degrees,
form_id, form_strategy, morph_type, base_model_id, main_mesh_path, anim_class_path, child_mesh_paths, independent_instance
```

Child mesh paths are separated by `|`. `independent_instance=1` forces creation
even when an exact team actor exists. All defaults and alternates of registered
multi-form families use this flag. `role_morph` uses the actor's own preloaded
`UiRoleMorphComponent`; `mesh_parts` preloads resources in that owner's loader
cache and calls `ChangeMesh` with a face-bearing leader and body follower.
Actor, main mesh, animation instance and child components must remain distinct
between slots. Graph and lifecycle exports include the selected form; created
form actors are checked for identity/form changes during playback maintenance.

`stage_floor.tsv` selects `capsule_bottom` for normal staging or `walkable_floor`
for original choreography, followed by a tab and a small fallback distance in
centimetres (2.5 by default). For original choreography the bridge captures the
walking pawn's existing `CurrentFloor` once, before moving any actor. A valid
walkable sweep uses `FloorDist`; a line result uses `LineDist`. Missing, airborne,
non-finite or out-of-range results use the fallback. The accepted range is 0–5 cm.
The lifecycle records `stage_height_correction_cm` and its source. Native motion
and camera placement add this correction without changing the stored manual
height, source clip or capsule position. Preview actors retain their original
static placement. Normal staging receives no automatic change.

Slots are positive `uint32` values and need not be contiguous. Stage X follows
the local pawn's horizontal right vector, Y follows forward, and Z is measured
from its capsule bottom. Stage yaw is relative to the initial player mesh
heading: gameplay and UI actors can have different mesh-to-actor rotations.
Before moving any actor, the bridge captures the player's mesh world yaw and
each actor's mesh-to-actor yaw offset. The target Actor yaw is reference mesh
yaw + requested stage yaw - that actor's mesh offset, modulo 360. It changes
the Actor transform while preserving the mesh's own component transform. The
stage basis and every borrowed actor's original state are captured before any
actor moves, including across a partially failed `ShowAtLocal` retry.

`managed_actor_lifecycle.json` records the stage reference Actor/mesh yaws,
requested stage yaw, captured mesh yaw offset and applied Actor yaw for
diagnosing a preview. `stage_reference_location` and `stage_reference_floor`
are absolute D_/global coordinates used for actor placement diagnostics. They
must not be copied directly into the local camera POV. After all actors are
placed, the bridge saves `stage_reference_camera_offset_from_mesh`: the global
stage floor point minus the first actor mesh's D_/global position. Native code
adds this translation to that mesh's current local position on every frame,
so the camera remains anchored to the stage across world-origin rebases. The
saved translation is also safe to report after meshes have been released.
Helpers without this field use the native first-actor fallback, ignoring their
absolute global stage coordinates. Orbit orientation uses the first mesh's
measured rotation and source calibration. `stage_reference_right` and
`stage_reference_forward` document the placement plane basis. Models with a
different authored rest-pose forward direction still need asset adaptation.

The current local team is read through existing fields, without invoking the
singleton getters or the `SceneTeamItem.EntityHandle` cache-populating getter:

1. `PlayerInfoModel._Instance.Id` selects
   `SceneTeamModel._Instance.TeamPlayerMap`.
2. `SceneTeamPlayer.CurrentGroupType` and `GroupMap` select the `RoleList`.
3. `SceneTeamModel.TeamItems` matches local player/group, `ConfigId`, and
   `CreatureDataId`; `EntityHandleInternal` supplies the current `WorldEntity`.
4. Handle/entity IDs and indices, `CharacterModel.Handles`,
   `ObjectSystem.Objects/Versions`, and entity initialization flags agree.
5. `Entity.GetComponent<CharacterActorComponent>()` supplies the existing
   `BaseActorComponent.ActorInternal` and skeletal mesh. The creature data
   role/player/creature IDs, skin ID, and actual skeletal mesh asset name must
   match the request. A native actor address is borrowed at most once.

Unmatched requests and subsequent duplicate requests use
`UiSceneRoleActorManager.CreateUiSceneRoleActor`; they never substitute another
team member's mesh. Each borrowed entity has a session-specific paired
`AddHoldEntity` / `RemoveHoldEntity` retention reason.

For staging, actor and mesh visibility, active/tick state and intervals are
saved together with transform and animation settings. The mesh uses
`AlwaysTickPoseAndRefreshBones`, with pause, skipped skeleton updates,
autonomous-only pose updates, update-rate optimization, and static rendering
disabled for the session. `Stop` restores every saved value and checks the
readback before returning a borrowed actor. It destroys only newly created
actors, using the original manager-map object reference. Failed restores or
destroys retain ownership for retry. Native body, face, and camera restoration
must complete before calling this managed stop operation.

`MaintainStage` is called from the session's existing camera callback. For
borrowed actors, it checks the same current identity and repairs differing
stage transforms, visibility, and animation/tick settings. It never replaces
the original snapshot. Stop disables this maintenance before attempting any
restoration, including when a failed restore is retried later.

When the controlled pawn's actual Actor/Mesh instance is absent from the staged
performers, `ShowAtLocal` hides its Actor and main mesh by default, including in
standing previews. A created copy of the same role is a separate instance. The
bridge verifies the pawn against the same current-team/entity-version chain,
retains it with a distinct session hold, and saves only its three display flags.
It does not change its transform, collision, ticks or animation policy. Playback
maintenance repairs those display flags in created-only scenes as well as mixed
scenes. A control or mesh replacement stops maintenance without writing to the
replacement. Native callbacks drain before `Stop` restores the original flags,
checks readback and returns the hold. Partial setup and restoration failures
remain retryable; `CheckReleased` and session rebinding also require this display
lease to be restored. `managed_actor_lifecycle.json` records the lease under
`nonperforming_player`, and `all_released` includes it. Previews apply the same
initial hide and stop restoration without installing a playback callback.

Static factory inspection found `UiSceneRoleActorManager.CreateUiSceneRoleActor`
(`Aki.Game`, method `0x06017bee`) increments `CreateIndex`, calls
`ActorSystem.Spawn<TsUiSceneRoleActor>`, initializes the result with the new index
and requested `useWay`, and assigns `UiSceneRoleActorMap[index]`. Its IL has no
lookup keyed by `useWay` and no actor-count bound. `ActorSystem.Spawn<T>`
(`Aki.Core`, method `0x0600001e`) calls `UKuroActorManager.D_SpawnActor` after
validating the class. The native engine allocator and its resource limits are
outside this static evidence. Consequently the bridge snapshots the manager
map before every creation and rejects an already present index or native actor
address before loading, moving, or acquiring ownership of that result. A
rejected foreign result is never destroyed, and a duplicate of an actor already
owned by this session is cleaned through its original owner exactly once.

The names and signatures were checked in the six local reference DLLs listed
in `references.json`. Notable metadata tokens include SceneTeamModel
`0x0200295c`, SceneTeamItem `0x02002953`, EntityHandle `0x020031bb`,
CharacterActorComponent `0x02003041`, BaseActorComponent `0x0200326f`,
CreatureDataComponent `0x020030a0`, and Entity `0x02000075` in their corresponding
Game/Core assemblies. The existing `csharp_scene_team_readonly_contract.json`
provides the independently documented membership chain.

Offline verification:

```powershell
dotnet run --project tools/tests/ManagedActorBorrowing/ManagedActorBorrowing.csproj -p:NuGetAudit=false
```

The fixture compiles the actual bridge against synthetic objects; it does not
load game assemblies or execute game APIs. It covers 8- and 70-actor sessions,
duplicate requests, sparse slots, mixed ownership, fixed stage anchoring,
offscreen animation settings, game-state overwrites, state restoration,
factory reuse/duplicate rejection, cross-session ownership, and retryable cleanup. The
real helper also compiles with the six game references and warnings as errors.
These checks do not establish in-game display, physical simulation, or an
observed hardware capacity.
