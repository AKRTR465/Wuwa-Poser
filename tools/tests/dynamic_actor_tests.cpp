// Synthetic storage and clocks only. No game process, windows, hooks or engine calls.
#include "../../runtime/owned_mmd/native/n1_common.h"
#include "../../runtime/owned_mmd/native/owned_mmd_body.h"
#include <cstdlib>
#include <iostream>
#include <memory>

namespace scene=owned_mmd_body;
namespace play=wwp_playback;
namespace native=play::native_physics;
static unsigned assertions=0;
static void Check(bool value,const char* message){++assertions;if(!value){std::cerr<<message<<'\n';std::exit(1);}}
static bool Near(double a,double b){return std::fabs(a-b)<1e-8;}
int main(){
    auto& runtime=scene::runtime;constexpr unsigned sampleActors=40;
    native::Registry registry;
    for(unsigned i=0;i<sampleActors;++i){auto actor=std::make_unique<scene::ActorRuntime>();
        actor->slot=i+1;actor->tokens[scene::Mesh].object=0x100000+uint64_t(i)*0x100;
        actor->native.node=0x400000+uint64_t(i)*0x1000;actor->native.target.main_anim=actor->native.node+0x200;
        actor->native.binding.group_count=1;actor->native.binding.groups[0].offset=0x80;
        actor->config=Json{{"character_display_name","synthetic"}};actor->features.prepared=true;actor->native.ready=true;
        actor->ownership=i<3?native_animation_target::OwnerPolicy::SceneTeamMember:native_animation_target::OwnerPolicy::OwnedUiActor;
        Check(registry.Add(actor->native),"registry accepts actors beyond old capacity");runtime.actors.push_back(std::move(actor));}
    runtime.actors.back()->slot=UINT32_MAX;scene::FreezeActorIndices();
    Check(runtime.actors.size()==sampleActors&&registry.count==sampleActors,"dynamic actor counts exceed both old limits");
    Check(scene::FindSlot(UINT32_MAX)==runtime.actors.back().get(),"full uint32 positive slot range resolves");
    Check(scene::FindSlot(0)==nullptr&&scene::FindSlot(41)==nullptr,"broadcast and missing slot are not actors");
    Check(scene::FindMesh(runtime.actors[25]->tokens[scene::Mesh].object)==runtime.actors[25].get(),"publication mesh dispatch resolves actor beyond slot 16");
    Check(!registry.Add(runtime.actors[0]->native),"duplicate native instance rejected");
    Check(registry.Freeze(),"native registry freezes once all actors are registered");
    auto* const registryStorage=registry.actors.data();const auto registryCapacity=registry.actors.capacity();
    bool found=true;for(unsigned pass=0;pass<100;++pass)for(auto& actor:runtime.actors){unsigned group=99;
        found=found&&registry.Input(actor->native.node)==&actor->native&&registry.Group(actor->native.target.main_anim+0x80,&group)==&actor->native&&group==0;}
    Check(found,"frozen input and physics-group dispatch finds every actor");
    Check(registry.actors.data()==registryStorage&&registry.actors.capacity()==registryCapacity,"callback lookup never reallocates registry storage");
    Check(!registry.Remove(runtime.actors[0]->native)&&!registry.Add(runtime.actors[0]->native),"live registry membership stays frozen");
    runtime.nativeInstalled=true;runtime.observing=true;Check(scene::Ready(),"scene barrier opens when every native actor is ready");
    runtime.actors[31]->native.ready=false;Check(!scene::Ready(),"one unready late actor prevents partial scene autoplay");
    runtime.actors[31]->native.ready=true;runtime.observing=false;
    runtime.actors[35]->nativePrepared=true;runtime.actors[35]->native.pending=true;
    Check(scene::NativePending(),"late actor pending physics keeps the whole scene in recovery");
    runtime.actors[35]->native.pending=false;Check(!scene::NativePending(),"scene native recovery clears after every actor retires its input");
    runtime.actors[37]->features.face_state.count=1;runtime.actors[37]->features.face_state.backup[0].pending=true;
    Check(scene::FeaturePending(),"late actor expression ownership also blocks scene retirement");
    runtime.actors[37]->features.face_state.backup[0].pending=false;Check(!scene::FeaturePending(),"scene feature recovery clears after all expressions restore");
    const auto savedSlot=runtime.actors.back()->slot;runtime.actors.back()->slot=1;
    bool duplicateRejected=false;try{scene::FreezeActorIndices();}catch(const std::runtime_error&){duplicateRejected=true;}
    Check(duplicateRejected,"duplicate slots cannot silently select one actor");runtime.actors.back()->slot=savedSlot;
    const auto savedMesh=runtime.actors.back()->tokens[scene::Mesh].object;runtime.actors.back()->tokens[scene::Mesh].object=runtime.actors.front()->tokens[scene::Mesh].object;
    duplicateRejected=false;try{scene::FreezeActorIndices();}catch(const std::runtime_error&){duplicateRejected=true;}
    Check(duplicateRejected,"one borrowed mesh cannot be registered as two actors");runtime.actors.back()->tokens[scene::Mesh].object=savedMesh;scene::FreezeActorIndices();

    Check(runtime.transport.SetClip(77,12),"scene clip configured");
    runtime.commands.Push({77,1,play::CommandKind::Play,0});scene::Advance(1,100);scene::Advance(2,101);
    const auto first=runtime.transport.State().seconds;scene::Advance(2,101.8);
    Check(Near(first,1)&&Near(runtime.transport.State().seconds,first),"all actors in one engine frame share one cursor");
    Check(scene::ApplyCommand({77,2,play::CommandKind::SetPlacementX,50,UINT32_MAX},101)==play::ControlCode::Ok,"high slot actor receives targeted placement");
    Check(runtime.actors.back()->placement.x==50&&runtime.actors.front()->placement.x==0,"targeted placement does not affect other actors");
    Check(scene::ApplyCommand({77,3,play::CommandKind::SetPlacementHeight,20,0},101)==play::ControlCode::Ok,"broadcast placement accepted");
    bool allPlaced=true;for(const auto& actor:runtime.actors)allPlaced=allPlaced&&actor->placement.height==20;
    Check(allPlaced,"broadcast reaches the dynamic collection");
    runtime.actors.front()->nativePhysics=false;runtime.actors.front()->features.physics_available=true;
    Check(scene::ApplyCommand({77,4,play::CommandKind::SetPhysics,1},101)==play::ControlCode::InvalidCommand&&!runtime.actors.front()->physicsEnabled&&runtime.transport.State().last_sequence==3,
        "secondary physics broadcast rejects a native actor before changing any actor or accepting its sequence");
    runtime.actors.front()->nativePhysics=true;runtime.actors.front()->features.physics_available=false;
    const Json originalFormation={{"formation_native",true},{"formation_floor_fallback_cm",2.5}};
    Check(Near(scene::FormationHeightCorrection(originalFormation,{{"stage_height_correction_cm",-2.15}}),-2),"native formation defaults to -2cm without doubling the capsule gap");
    Check(Near(scene::FormationHeightCorrection(originalFormation,Json::object()),-2),"missing legacy floor metadata keeps the same default height");
    Check(Near(scene::FormationHeightCorrection({{"formation_native",false}},{{"stage_height_correction_cm",-2.15}}),-2),"manual stage shares the -2cm default");
    Check(Near(scene::FormationHeightCorrection({{"formation_native",true},{"ground_height_cm",0}},Json::object()),0),"explicit zero height disables the shared offset");
    bool badCorrection=false;try{scene::FormationHeightCorrection({{"ground_height_cm",-1001}},Json::object());}catch(const std::runtime_error&){badCorrection=true;}
    Check(badCorrection,"shared height rejects out-of-range values");
    runtime.formationHeightCorrection=-2.15;runtime.cameraAnchor=runtime.actors.front().get();
    const auto effective=scene::EffectivePlacement(*runtime.cameraAnchor);
    Check(Near(effective.height,17.85)&&Near(runtime.cameraAnchor->placement.height,20),"floor correction is additive to manual height without mutating it");
    bool stableHeight=true;for(unsigned i=0;i<100;++i)stableHeight=stableHeight&&Near(scene::EffectivePlacement(*runtime.cameraAnchor).height,17.85);
    Check(stableHeight,"paused publications never accumulate the formation height correction");
    Check(Near(scene::CameraState(runtime.transport.State()).placement_height,17.85),"camera follows the same corrected height as body and native physics");
    scene::ApplyActorCommand(*runtime.cameraAnchor,{77,0,play::CommandKind::ResetPlacement,0});
    Check(Near(scene::EffectivePlacement(*runtime.cameraAnchor).height,-2.15),"resetting manual placement preserves the stage floor correction");
    scene::ApplyActorCommand(*runtime.cameraAnchor,{77,0,play::CommandKind::SetPlacementHeight,20});
    runtime.formationHeightCorrection=0;
    Check(scene::ApplyCommand({77,4,play::CommandKind::SetPlacementX,1,41},101)==play::ControlCode::InvalidCommand,"unknown slot rejects without falsely acknowledging");
    Check(runtime.transport.State().last_sequence==3,"rejected actor command preserves shared command sequence");
    scene::ApplyCommand({77,4,play::CommandKind::Pause,0},101);scene::Advance(3,500);
    Check(runtime.transport.State().mode==play::PlayerMode::Paused&&Near(runtime.transport.State().seconds,1),"shared pause retains all actor cursors");
    scene::ApplyCommand({77,5,play::CommandKind::SeekSeconds,10},500);scene::ApplyCommand({77,6,play::CommandKind::Play,0},500);scene::Advance(4,503);
    Check(runtime.transport.State().mode==play::PlayerMode::Ended&&!runtime.transport.State().restore_requested,"longest scene track holds its last frame until stopped");
    Check(scene::ApplyCommand({77,7,play::CommandKind::SetGroundHeight,-2},503)==play::ControlCode::Ok,"shared height can change on a held pose");
    Check(Near(scene::EffectivePlacement(*runtime.cameraAnchor).height,18)&&Near(scene::CameraState(runtime.transport.State()).placement_height,18),"shared adjustment reaches body and camera once while preserving per-actor height");
    Check(scene::ApplyCommand({77,8,play::CommandKind::SetGroundHeight,-2,1},503)==play::ControlCode::InvalidCommand,"shared height cannot silently act as a per-actor command");

    runtime.commands.Push({77,8,play::CommandKind::SetPlacementX,1,41});scene::Advance(5,503);
    auto rejected=scene::Status();
    Check(rejected.at("last_control_sequence")==8&&rejected.at("last_control_code")==uint32_t(play::ControlCode::InvalidCommand)&&rejected.at("last_sequence")==7,
        "rejection identifies the processed sequence without acknowledging it as accepted");
    runtime.commands.Push({77,9,play::CommandKind::SetCameraTrackHeight,20});scene::Advance(6,503);
    auto accepted=scene::Status();
    Check(accepted.at("last_control_sequence")==9&&accepted.at("last_control_code")==uint32_t(play::ControlCode::Ok)&&accepted.at("last_sequence")==9&&accepted.at("rejected_commands")==1,
        "a later accepted command publishes its own result instead of inheriting the preceding rejection");
    auto report=scene::Status();Check(report.at("actor_count")==40&&report.at("actors").size()==40,"status reports the actual dynamic actor set");
    Check(report.at("borrowed_count")==3&&report.at("created_count")==37,"status reports actual mixed ownership rather than request assumptions");
    auto authority=std::make_unique<scene::ActorRuntime>();std::vector<uint8_t> mesh(0x920),asset(0xe0);
    authority->ownership=native_animation_target::OwnerPolicy::SceneTeamMember;
    authority->tokens[scene::Mesh].object=uint64_t(mesh.data());authority->tokens[scene::Asset].object=uint64_t(asset.data());
    authority->tokens[scene::Actor].object=101;authority->tokens[scene::Skeleton].object=202;authority->tokens[scene::MainAnim].object=303;
    const auto put=[](uint64_t address,uint64_t value){std::memcpy((void*)address,&value,8);};
    put(uint64_t(mesh.data())+0xa8,101);put(uint64_t(mesh.data())+0x5d0,uint64_t(asset.data()));put(uint64_t(asset.data())+0xd8,202);put(uint64_t(mesh.data())+0x910,303);
    native::Access access;access.read=[](void*,uintptr_t from,void* to,size_t size){std::memcpy(to,(void*)from,size);return true;};
    Check(scene::NativeAuthority(authority.get(),access),"borrowed actor authority uses its own live graph without requiring player control");
    put(uint64_t(mesh.data())+0xa8,404);Check(!scene::NativeAuthority(authority.get(),access),"changed actor owner rejects publication authority");
    unsigned maintenanceCalls=0;
    const auto maintenance=[](void* value){++*static_cast<unsigned*>(value);return true;};
    // No camera target is available in this synthetic fixture, so installation
    // cannot patch a hook. Exercise callback admission for both ownership sets.
    for(auto& actor:runtime.actors)actor->ownership=native_animation_target::OwnerPolicy::OwnedUiActor;
    Check(scene::InstallSceneMaintenance(maintenance,&maintenanceCalls)&&runtime.maintenanceEnabled.load(),"created-only scenes enable player visibility maintenance");
    Check(scene::MaintainSceneFrame(nullptr)&&maintenanceCalls==1,"created-only visibility maintenance reaches the configured game-thread action");
    report=scene::Status();Check(report.at("scene_maintenance")==true&&report.at("borrowed_stage_maintenance")==false,"created-only maintenance is not reported as borrowed-actor maintenance");
    for(unsigned i=0;i<3;++i)runtime.actors[i]->ownership=native_animation_target::OwnerPolicy::SceneTeamMember;
    report=scene::Status();Check(report.at("borrowed_stage_maintenance")==true,"mixed scene continues to report borrowed stage maintenance");
    runtime.actors[37]->features.face_state.backup[0].pending=true;
    Check(!scene::CloseAndRestore(GetTickCount64())&&runtime.maintenanceEnabled.load(),"borrowed evaluation maintenance stays active while late-actor restoration is pending");
    runtime.actors[37]->features.face_state.backup[0].pending=false;
    Check(scene::CloseAndRestore(GetTickCount64())&&!runtime.maintenanceEnabled.load(),"scene disables stage maintenance after every actor and camera have restored");
    Check(scene::MaintainSceneFrame(nullptr)&&maintenanceCalls==1,"retired scene cannot reapply stage state after managed restoration");
    registry.Thaw();Check(registry.Remove(runtime.actors[25]->native)&&registry.count==39,"registry removes any actor after callbacks are drained");
    std::cout<<"dynamic actors: "<<assertions<<" assertions passed; synthetic actors="<<sampleActors<<'\n';
}
