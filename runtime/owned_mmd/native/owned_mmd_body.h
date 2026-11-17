#pragma once
#include "../../generated/GameCompat.generated.h"
// One scene coordinator, one clock and camera, and any number of stable actors.
// Actor/index storage is prepared before callbacks and never grows while live.
#include "../../../probes/v20_recon/object_identity.h"
#include "../../../probes/v20_recon/game_object_anchors.h"
#include "../../../probes/v20_recon/publish_observer.h"
#include "../../../probes/v20_recon/vmt_slot_patch.h"
#include "../../../playback/vmd_local_clip.h"
#include "../../../playback/player_transport.h"
#include "../../../playback/model_preview_frame.h"
#include "../../../probes/v20_recon/json_report.h"
#include "../../../probes/v20_recon/native_target_publication.h"
#include "../../../probes/v20_recon/shared_native_physics.h"
#include "../../../probes/v20_recon/unified_player_features.h"
#include "../../../engine_ue4/pose_full_body.h"
#include <algorithm>
#include <atomic>
#include <array>
#include <cmath>
#include <filesystem>
#include <fstream>
#include <memory>
#include <mutex>
#include <vector>

namespace owned_mmd_body {
namespace ob=v20_publish_observer;
namespace id=ww_object;
namespace fb=wwp_ue::full_body;
namespace clip=wwp_playback::local_clip;
namespace collision=wwp_playback::kawaii_collision;
namespace play=wwp_playback;
namespace vmt=v20_vmt_slot_patch;
namespace features=unified_player_features;
namespace native=v20_shared_native_physics;
using ue_memory::Require;
enum Object:uint32_t {Actor,Mesh,Asset,Skeleton,MainAnim,Objects};
constexpr const char* kObjectKeys[Objects]={"actor","mesh","asset","skeleton","main_anim"};
enum class Error:uint32_t {None,Arguments,Plan,Install};
struct ActorRuntime {
    uint32_t slot=0;std::filesystem::path directory;Json config;
    bool nativePhysics=true,physicsEnabled=false,nativeRegistered=false;
    native_animation_target::OwnerPolicy ownership=native_animation_target::OwnerPolicy::OwnedUiActor;
    std::array<id::Identity,Objects> tokens{};
    std::vector<std::string> boneNames;std::vector<int32_t> parents;
    std::vector<std::array<float,3>> positions;std::vector<std::array<float,4>> rotations;
    std::vector<uint8_t> positionMask,rotationMask;
    clip::Clip motion;fb::Topology topology;fb::Request request;play::placement::Settings placement;
    features::ActorFeatures features;collision::PrototypeBank capsulePrototypes;native::Actor native;
    native_animation_target::AuthoritySnapshot authority{};
    std::atomic<bool> nativePrepared{false};std::atomic<uint64_t> writes{0};
};
struct Runtime {
    bool initialized=false,startPaused=false,payloadReleased=false;
    Json terminalStatus;
    std::atomic<double> groundHeight{0};
    std::filesystem::path directory;
    uint64_t host=0,epoch=0,lastEngineFrame=0;
    play::Transport transport;play::SpscCommandQueue<64> commands;
    uint64_t initialSequence=0,lastWireSequence=0,lastControlSequence=0;DWORD gameThread=0;
    play::model_preview::Frame modelPreview{};
    Go go{};Pool pool{};
    std::vector<std::unique_ptr<ActorRuntime>> actors;
    std::vector<ActorRuntime*> bySlot,byMesh;ActorRuntime* cameraAnchor=nullptr;
    bool (*maintainScene)(void*)=nullptr;void* maintainUser=nullptr;std::atomic<bool> maintenanceEnabled{false};
    features::CameraFeatures cameraFeatures;vmt::State hook;
    using Publish=void(*)(void*);Publish original=nullptr;
    std::atomic<bool> observing{false},writing{false},closing{false},closed{false},deadlineExpired{false},nativeInstalled{false};
    std::mutex publicationMutex;
    std::atomic<uint32_t> error{0},active{0},work{0},lastControlCode{0};
    std::atomic<uint64_t> rejectedCommands{0},actualPlayStart{0};
};
inline Runtime runtime;
inline void Fail(Error error) noexcept {
    uint32_t empty=0;runtime.error.compare_exchange_strong(empty,uint32_t(error));
    runtime.writing.store(false,std::memory_order_release);runtime.closing.store(true,std::memory_order_release);
}
inline ::Reader CurrentProcess(){return ::Reader{GetCurrentProcess()};}
inline ActorRuntime* FindSlot(uint32_t slot) noexcept {
    const auto& list=runtime.bySlot;const auto at=std::lower_bound(list.begin(),list.end(),slot,[](const ActorRuntime* a,uint32_t key){return a->slot<key;});
    return at!=list.end()&&(*at)->slot==slot?*at:nullptr;
}
inline ActorRuntime* FindMesh(uint64_t mesh) noexcept {
    const auto& list=runtime.byMesh;const auto at=std::lower_bound(list.begin(),list.end(),mesh,[](const ActorRuntime* a,uint64_t key){return a->tokens[Mesh].object<key;});
    return at!=list.end()&&(*at)->tokens[Mesh].object==mesh?*at:nullptr;
}
inline void FreezeActorIndices(){auto& r=runtime;
    Require(!r.observing.load(),"cannot change a live actor registry");r.bySlot.clear();r.byMesh.clear();
    r.bySlot.reserve(r.actors.size());r.byMesh.reserve(r.actors.size());
    for(auto& actor:r.actors){Require(actor->slot!=0,"actor slot must be positive");r.bySlot.push_back(actor.get());r.byMesh.push_back(actor.get());}
    std::sort(r.bySlot.begin(),r.bySlot.end(),[](const auto* a,const auto* b){return a->slot<b->slot;});
    std::sort(r.byMesh.begin(),r.byMesh.end(),[](const auto* a,const auto* b){return a->tokens[Mesh].object<b->tokens[Mesh].object;});
    for(size_t i=1;i<r.bySlot.size();++i)Require(r.bySlot[i-1]->slot!=r.bySlot[i]->slot,"duplicate actor slot");
    for(size_t i=1;i<r.byMesh.size();++i)Require(r.byMesh[i-1]->tokens[Mesh].object!=r.byMesh[i]->tokens[Mesh].object,"one game actor cannot occupy multiple slots");
}
inline bool Routed(play::CommandKind kind){return play::IsActorCommand(kind)||kind==play::CommandKind::SetGroundHeight;}
inline void ApplyActorCommand(ActorRuntime& actor,const play::Command& command){switch(command.kind){
    case play::CommandKind::SetPhysics:actor.physicsEnabled=command.value!=0;break;
    case play::CommandKind::SetPlacementX:actor.placement.x=command.value;break;
    case play::CommandKind::SetPlacementY:actor.placement.y=command.value;break;
    case play::CommandKind::SetPlacementHeight:actor.placement.height=command.value;break;
    case play::CommandKind::SetPlacementYaw:actor.placement.yaw_degrees=command.value;break;
    case play::CommandKind::SetPlacementInPlace:actor.placement.in_place=command.value!=0;break;
    case play::CommandKind::ResetPlacement:actor.placement={};break;
    default:break;
}}
// The targeted actor when it has prepared native physics and, for group
// commands, a verified group (and chain) to edit.
inline ActorRuntime* NativeTarget(const play::Command& command,bool group,unsigned chain=0){
    auto* actor=FindSlot(command.target_slot);
    if(!actor||!actor->nativePrepared)return nullptr;
    return !group||native::CanSetParameter(actor->native,command.target_group,chain)?actor:nullptr;
}
inline play::ControlCode ApplyNativeCommand(const play::Command& command,double now){auto& r=runtime;
    const auto accept=[&]{return r.transport.AcceptRoutedCommand(command,now);};
    constexpr auto invalid=play::ControlCode::InvalidCommand,ok=play::ControlCode::Ok;
    if(play::NativeCapsuleCommand(command.kind)){
        auto* actor=NativeTarget(command,true);if(!actor||!actor->native.capsules_verified)return invalid;
        auto& capsules=actor->native.capsules[command.target_group-1];
        std::lock_guard<std::mutex> lock(capsules.edit);
        const auto shape=std::find_if(capsules.shapes.begin(),capsules.shapes.end(),[&](const auto& s){return s.id==command.target_chain;});
        if(shape==capsules.shapes.end()||(!shape->geometry_available&&command.kind<play::CommandKind::ResetNativeCapsule))return invalid;
        const auto code=accept();if(code!=ok)return code;
        if(command.kind==play::CommandKind::SetNativeCapsuleActive)shape->enabled=command.value!=0;
        else if(command.kind==play::CommandKind::ResetNativeCapsule){shape->enabled=shape->baseline_enabled;shape->edited=0;}
        else{
            const auto field=unsigned(command.kind)-unsigned(play::CommandKind::SetNativeCapsuleRadius);
            shape->requested[field]=float(command.value);shape->edited|=1u<<field;
        }
        return code;
    }
    switch(command.kind){
    case play::CommandKind::SetNativeCapsuleCollision:{
        auto* actor=NativeTarget(command,true);if(!actor||!actor->native.capsules_verified)return invalid;
        auto& capsule=actor->native.capsules[command.target_group-1];
        std::lock_guard<std::mutex> lock(capsule.edit);
        const auto driver=std::find_if(capsule.drivers.begin(),capsule.drivers.end(),[&](const auto& d){return d.bone+1==command.target_chain;});
        if(driver==capsule.drivers.end())return invalid;
        const auto code=accept();if(code==ok)driver->enabled=command.value!=0;
        return code;
    }
    case play::CommandKind::SetNativeGravity:{
        auto* actor=NativeTarget(command,true);if(!actor||!actor->native.gravity_verified)return invalid;
        const auto code=accept();if(code!=ok)return code;
        auto& gravity=actor->native.gravity[command.target_group-1];
        std::lock_guard<std::mutex> lock(gravity.edit);
        gravity.requested=float(command.value);gravity.revision=command.sequence;return code;
    }
    case play::CommandKind::SetPartGravity:{
        auto* actor=NativeTarget(command,false);if(!actor)return invalid;
        auto& parts=actor->features.gravity_parts;
        const auto part=std::find_if(parts.begin(),parts.end(),[&](const auto& item){return item.id==command.target_chain;});
        if(part==parts.end())return invalid;
        const auto code=accept();if(code!=ok)return code;
        part->strength=float(command.value);part->sequence=command.sequence;
        unsigned covered=0;
        for(unsigned i=0;i<actor->native.binding.group_count;++i)
            if(play::part_gravity::CoversGroup(parts,actor->native.settings[i].chains,actor->native.binding.groups[i].root_bones.size()))covered|=1u<<i;
        actor->native.part_gravity_group_mask.store(covered);
        return code;
    }
    case play::CommandKind::SetNativePreview:{
        auto* actor=NativeTarget(command,true);if(!actor)return invalid;
        const auto code=accept();if(code!=ok)return code;
        native::SetPreview(actor->native,command.target_group,command.value!=0);return code;
    }
    case play::CommandKind::SetNativeShoulderFollow:{
        auto* actor=NativeTarget(command,false);if(!actor)return invalid;
        auto& state=actor->native;const auto sleeve=native::FindSleeve(state,command.target_group,command.target_chain);
        if(sleeve==state.sleeves.end())return invalid;
        const auto code=accept();if(code!=ok)return code;
        std::lock_guard<std::mutex> lock(state.sleeve_edit);
        sleeve->follow=float(command.value);sleeve->applied_frames=0;sleeve->peak_correction_degrees=0;return code;
    }
    default:{ // Native damping, stiffness and limit angle.
        auto* actor=NativeTarget(command,true,command.target_chain);if(!actor)return invalid;
        const auto code=accept();if(code!=ok)return code;
        const unsigned parameter=command.kind==play::CommandKind::SetNativeDamping?0:command.kind==play::CommandKind::SetNativeStiffness?1:2;
        native::SetParameter(actor->native,command.target_group,command.target_chain,parameter,float(command.value),command.sequence);
        return code;
    }
    }
}
inline bool NativeCommand(play::CommandKind kind){
    using K=play::CommandKind;
    return play::NativeCapsuleCommand(kind)||play::NativeSettingsCommand(kind)||kind==K::SetNativeCapsuleCollision||kind==K::SetNativeGravity||
        kind==K::SetPartGravity||kind==K::SetNativePreview||kind==K::SetNativeShoulderFollow;
}
inline play::ControlCode ApplyCommand(const play::Command& command,double now){auto& r=runtime;
    if(command.target_slot&&!FindSlot(command.target_slot))return play::ControlCode::InvalidCommand;
    if(command.kind==play::CommandKind::SetModelPreview){
        const auto code=r.transport.AcceptRoutedCommand(command,now);
        if(code!=play::ControlCode::Ok)return code;
        auto& preview=r.modelPreview;
        if(command.value!=0||preview.slot==command.target_slot){
            preview.slot=command.value!=0?command.target_slot:0;preview.revision=command.sequence;preview.count=0;preview.frame=0;
        }
        return code;
    }
    if(NativeCommand(command.kind))return ApplyNativeCommand(command,now);
    if(command.kind==play::CommandKind::SetCameraMode&&command.value==2&&!r.cameraFeatures.clip_available)return play::ControlCode::InvalidCommand;
    if(command.kind==play::CommandKind::SetPhysics&&command.value==1)
        for(const auto& actor:r.actors)
            if((!command.target_slot||actor->slot==command.target_slot)&&(actor->nativePhysics||!actor->features.physics_available))return play::ControlCode::InvalidCommand;
    if(Routed(command.kind)){
        const auto code=r.transport.AcceptRoutedCommand(command,now);if(code!=play::ControlCode::Ok)return code;
        if(command.kind==play::CommandKind::SetGroundHeight){r.groundHeight=command.value;return code;}
        if(command.target_slot)ApplyActorCommand(*FindSlot(command.target_slot),command);
        else for(auto& actor:r.actors)ApplyActorCommand(*actor,command);
        return code;
    }
    return r.transport.Apply(command,now);
}
inline bool Advance(uint64_t engineFrame,double now){auto& r=runtime;if(engineFrame==r.lastEngineFrame)return true;
    r.lastEngineFrame=engineFrame;play::Command command;
    while(r.commands.Pop(command)){
        const auto code=ApplyCommand(command,now);r.lastControlCode.store(uint32_t(code));r.lastControlSequence=command.sequence;
        if(code!=play::ControlCode::Ok)r.rejectedCommands.fetch_add(1);
        else if(command.kind==play::CommandKind::Play&&!r.actualPlayStart.load())r.actualPlayStart.store(GetTickCount64());
    }
    if(r.transport.Tick(now)!=play::ControlCode::Ok){Fail(Error::Plan);return false;}
    if(r.transport.State().restore_requested){r.writing.store(false);r.closing.store(true);}return true;
}
inline play::placement::Settings EffectivePlacement(const ActorRuntime& actor){auto placed=actor.placement;placed.height+=runtime.groundHeight.load();return placed;}
inline double GroundHeight(const Json& config){
    // One explicit offset for both formation modes. Do not additionally apply
    // the previous capsule-gap correction, which would lower native formation twice.
    const auto height=config.value("ground_height_cm",-2.0);
    Require(std::isfinite(height)&&height>=-1000&&height<=1000,"invalid standing height");return height;
}
inline play::PlayerState CameraState(const play::PlayerState& timeline){auto state=timeline;if(const auto* actor=runtime.cameraAnchor){
    state.placement_x=actor->placement.x;state.placement_y=actor->placement.y;state.placement_height=EffectivePlacement(*actor).height;
    state.placement_yaw_degrees=actor->placement.yaw_degrees;state.placement_in_place=actor->placement.in_place;}return state;}
inline void CapturePublishedPreview(ActorRuntime& actor,::Reader& read){
    auto& state=actor.native;
    const auto group=state.preview_group.load(std::memory_order_acquire);
    auto& model=runtime.modelPreview;
    const bool model_requested=model.slot==actor.slot&&model.frame!=runtime.lastEngineFrame;
    if(!group&&!model_requested)return;
    ob::Metadata before,after;
    const auto mesh=actor.features.mesh.object;
    if(!ob::ReadMetadata(read,mesh,before)||before.previous_index<0||before.previous_index>1)return;
    const auto index=unsigned(before.previous_index);
    const auto& head=before.heads[index];
    if(head.num!=int32_t(actor.topology.count)||!ob::ValidHead(head)||
       !read.Read(head.data,actor.features.pose.data(),size_t(head.num)*ww_game_compat::TransformBytes))return;
    // The buffer must not have been republished while it was copied.
    if(!ob::ReadMetadata(read,mesh,after)||after.previous_index!=before.previous_index||after.generation!=before.generation||
       after.heads[index].data!=head.data||after.heads[index].num!=head.num)return;
    if(model_requested&&unsigned(head.num)<=play::model_preview::kMaxBones){
        model.count=0;
        for(int i=0;i<head.num;++i)
            if(!play::model_preview::ReadBone(actor.features.pose[i].data(),model.bones[i]))return;
        model.count=unsigned(head.num);model.frame=runtime.lastEngineFrame;
    }
    if(!group)return;
    std::lock_guard<std::mutex> lock(state.preview_lock);
    if(state.preview_group.load(std::memory_order_relaxed)!=group)return;
    const auto count=state.topology.count;
    state.preview_pose.resize(count);state.preview_frame=runtime.lastEngineFrame;
    const auto& chain=state.settings[group-1].skeleton_chain;
    state.preview_solved_bones=0;
    for(unsigned i=0;i<count;++i){
        const auto value=wwp_ue::subtree_rotation::detail::Position(actor.features.pose[i]);
        state.preview_pose[i]={float(value[0]),float(value[1]),float(value[2])};
        if(!wwp_playback::kawaii_parameters::Finite(state.preview_pose[i])){state.preview_pose.clear();state.preview_solved_bones=0;return;}
        if(i<chain.size()&&chain[i])++state.preview_solved_bones;
    }
}
inline void Detour(void* object){auto& r=runtime;r.active.fetch_add(1,std::memory_order_acq_rel);
    struct Active {~Active(){runtime.active.fetch_sub(1,std::memory_order_release);}} active;
    static thread_local bool publishing=false;
    if(publishing||!r.observing.load(std::memory_order_acquire)||GetCurrentThreadId()!=r.gameThread){r.original(object);return;}
    // Status readers and pose writers share this lock. A busy reader must
    // never cause the native publisher to see an uncorrected frame.
    std::unique_lock<std::mutex> publication(r.publicationMutex);
    auto* actor=r.observing.load(std::memory_order_acquire)?FindMesh(uint64_t(object)):nullptr;
    if(!actor){publication.unlock();r.original(object);return;}
    publishing=true;
    struct Publication {bool& active;~Publication(){active=false;}} callback{publishing};
    r.work.fetch_add(1,std::memory_order_acq_rel);
    struct Work {~Work(){runtime.work.fetch_sub(1,std::memory_order_release);}} work;
    if(actor->native.error.load()||r.cameraFeatures.failed.load())Fail(Error::Plan);
    auto read=CurrentProcess();uint64_t frame=0;const auto now=GetTickCount64()/1000.0;
    if(!read.Get(r.host+ww_game_compat::EngineFrameRva,frame)||!frame)Fail(Error::Plan);else Advance(frame,now);
    auto state=r.transport.State();state.physics_enabled=actor->physicsEnabled;
    const bool feed=r.writing.load()&&!r.closing.load()&&state.mode!=play::PlayerMode::Stopped&&state.mode!=play::PlayerMode::Empty;
    if(actor->nativePhysics&&(!features::ActorAlive(actor->features,read)||
        !features::GravityPartFrame(actor->features,read,state,feed,now,true)))Fail(Error::Plan);
    r.original(object);
    const auto placement=EffectivePlacement(*actor);
    const bool open=feed&&!r.closing.load();
    if(actor->nativePhysics){
        if(!features::FaceFrame(actor->features,read,state,open))Fail(Error::Plan);
        native::Publish(actor->native,state.seconds,placement,open);
    }else if(!features::ActorFrame(actor->features,read,state,placement,feed))Fail(Error::Plan);
    if(open)CapturePublishedPreview(*actor,read);
    features::PublishCamera(r.cameraFeatures,CameraState(state),open);
}
inline bool NativeAuthority(void* user,const native::Access& access){
    const auto& a=*static_cast<ActorRuntime*>(user);const auto mesh=a.tokens[Mesh].object;uint64_t owner=0,asset=0,skeleton=0,main=0;
    return access.Get(mesh+ww_game_compat::ComponentOwnerOffset,owner)&&owner==a.tokens[Actor].object&&
        access.Get(mesh+ww_game_compat::ComponentAssetOffset,asset)&&asset==a.tokens[Asset].object&&
        access.Get(asset+ww_game_compat::AssetSkeletonOffset,skeleton)&&skeleton==a.tokens[Skeleton].object&&
        access.Get(mesh+ww_game_compat::ComponentMainAnimOffset,main)&&main==a.tokens[MainAnim].object;
}
inline void NativeCommitted(void* user,uint32_t count){static_cast<ActorRuntime*>(user)->writes.fetch_add(count);}
inline bool NeedsNative(){for(const auto& actor:runtime.actors)if(actor->nativePhysics)return true;return false;}
inline std::vector<std::string> CapsuleRootNames(std::vector<std::string> roots){
    for(auto& root:roots)root=native::LowerBoneName(std::move(root));
    std::sort(roots.begin(),roots.end());
    roots.erase(std::unique(roots.begin(),roots.end()),roots.end());
    return roots;
}
inline int32_t CapsuleDriverBone(const Json& value,const std::vector<std::string>& bones){
    if(value.is_null())return -1;
    const auto driver=native::LowerBoneName(value.get<std::string>());
    int32_t found=-1;
    for(size_t i=0;i<bones.size();++i)if(native::LowerBoneName(bones[i])==driver){
        Require(found<0,"capsule layout driver bone name is ambiguous");
        found=int32_t(i);
    }
    Require(found>=0,"capsule layout driver bone is missing from the actor skeleton");
    return found;
}
inline collision::AuthoredShape CapsuleLayoutShape(const Json& row,const std::vector<std::string>& bones,unsigned id){
    collision::AuthoredShape result;
    auto& shape=result.shape;
    Require(row.at("array").is_number_unsigned(),"capsule layout array must be A or B");
    const auto array=row.at("array").get<uint64_t>();
    Require(array<2,"capsule layout array must be A or B");
    result.array=unsigned(array);
    if(row.contains("source_id")&&!row.at("source_id").is_null()){
        Require(row.at("source_id").is_number_unsigned(),"capsule layout source ID is invalid");
        const auto source=row.at("source_id").get<uint64_t>();
        Require(source>0&&source<collision::kFirstAuthoredId,"capsule layout source ID is invalid");
        result.source_id=unsigned(source);
    }
    shape.id=id;shape.bone=CapsuleDriverBone(row.at("bone"),bones);
    shape.baseline_enabled=row.value("enabled",true);
    shape.enabled=shape.baseline_enabled;
    Require(row.at("offset").is_array()&&row.at("offset").size()==3&&
        row.at("rotation").is_array()&&row.at("rotation").size()==3,"capsule layout offset or rotation is invalid");
    const auto offset=row.at("offset").get<std::array<float,3>>();
    shape.baseline={row.at("radius").get<float>(),row.at("length").get<float>(),offset[0],offset[1],offset[2]};
    shape.rotation=row.at("rotation").get<std::array<float,3>>();
    shape.geometry_available=collision::ValidGeometry(shape);
    Require(shape.geometry_available,"capsule layout contains out-of-range dimensions, offset, or rotation");
    return result;
}
inline void PrepareCapsuleLayouts(ActorRuntime& actor){
    if(!actor.config.contains("capsule_groups"))return;
    const auto& groups=actor.config.at("capsule_groups");
    Require(groups.is_array()&&groups.size()<=actor.native.binding.group_count,"capsule layout group list is invalid");
    if(groups.empty())return;
    Require(actor.native.capsules_verified,"capsule layouts require verified native collision support");
    for(unsigned i=0;i<actor.native.binding.group_count;++i)actor.native.capsules[i].prototype_bank=&actor.capsulePrototypes;
    for(const auto& row:groups){
        const auto curve=native::LowerBoneName(row.at("curve_name").get<std::string>());
        const auto roots=CapsuleRootNames(row.at("root_bones").get<std::vector<std::string>>());
        auto& binding=actor.native.binding;
        unsigned group=binding.group_count;
        for(unsigned i=0;i<binding.group_count;++i){
            const auto& candidate=binding.groups[i];
            if(native::LowerBoneName(candidate.curve_name)!=curve||CapsuleRootNames(candidate.root_bones)!=roots)continue;
            Require(group==binding.group_count,"capsule layout matches more than one native physics group");
            group=i;
        }
        Require(group<binding.group_count,"capsule layout does not match the actor's native physics group");
        auto& state=actor.native.capsules[group];
        Require(!state.layout,"capsule layout repeats a physics group");
        auto& layout=state.layout.emplace();
        layout.replace_stock=row.value("replace_stock",true);
        const auto& shapes=row.at("shapes");
        Require(shapes.is_array()&&shapes.size()<=2*collision::kMaxCapsulesPerArray,"capsule layout has too many shapes");
        layout.shapes.reserve(shapes.size());
        std::array<size_t,2> counts{};
        for(const auto& shape:shapes){
            auto parsed=CapsuleLayoutShape(shape,actor.boneNames,collision::kFirstAuthoredId+unsigned(layout.shapes.size()));
            Require(++counts[parsed.array]<=collision::kMaxCapsulesPerArray,"capsule layout has too many shapes in one array");
            layout.shapes.push_back(std::move(parsed));
        }
        if(row.contains("stock_baseline")&&!row.at("stock_baseline").is_null()){
            const auto& stock=row.at("stock_baseline");
            Require(stock.is_array()&&stock.size()<=2*collision::kMaxCapsulesPerArray,"capsule layout original baseline is invalid");
            auto& baseline=layout.stock_baseline.emplace();
            baseline.reserve(stock.size());
            std::array<std::array<bool,collision::kMaxCapsulesPerArray>,2> seen{};
            for(const auto& shape:stock){
                auto parsed=CapsuleLayoutShape(shape,actor.boneNames,0);
                Require(parsed.source_id>0&&(parsed.source_id-1)/collision::kArraySpan==parsed.array,"capsule layout original source ID or array differs");
                const auto index=(parsed.source_id-1)%collision::kArraySpan;
                Require(index<collision::kMaxCapsulesPerArray&&!seen[parsed.array][index],"capsule layout original source ID is invalid or duplicated");
                seen[parsed.array][index]=true;
                baseline.push_back(std::move(parsed));
            }
        }
    }
}
inline void PrepareNative(){auto& r=runtime;auto reader=CurrentProcess();
    for(auto& actor:r.actors){auto& a=*actor;if(!a.nativePhysics)continue;
        const auto directory=r.directory/L"native_inputs"/std::to_string(a.slot);
        std::ifstream input(directory/L"native_playback.json");Require(bool(input),"actor native playback input missing");const auto config=Json::parse(input);
        native::PrepareFromConfig(a.native,reader,config,directory,a.motion,a.topology,a.request,r.epoch,&NativeAuthority,&a);
        PrepareCapsuleLayouts(a);
        a.native.on_commit=&NativeCommitted;a.native.external_writing=&r.writing;
        Require(native::RegisterActor(a.native),"actor native registration failed");a.nativeRegistered=true;a.nativePrepared=true;
    }
}
inline void ValidateCanonicalPose(const ActorRuntime& a){
    const auto count=a.topology.count;
    Require(a.positions.size()==count&&a.rotations.size()==count&&a.positionMask.size()==count&&a.rotationMask.size()==count,"actor canonical pose count differs");
    for(size_t i=0;i<count;++i){
        Require(a.positionMask[i]<=1&&a.rotationMask[i]<=1,"actor canonical pose mask invalid");
        for(float value:a.positions[i])Require(std::isfinite(value),"actor canonical position invalid");
        double norm=0;
        for(float value:a.rotations[i]){Require(std::isfinite(value),"actor canonical rotation invalid");norm+=double(value)*value;}
        Require(!a.rotationMask[i]||std::fabs(norm-1)<=.001,"actor canonical rotation is not normalized");
    }
}
// The live reference skeleton must match the prepared names (ignoring ASCII
// case) and parent table exactly.
inline void VerifyReferenceSkeleton(ActorRuntime& a,::Reader& reader){
    ob::ArrayHead ref;std::array<unsigned char,fb::kLimit*ww_game_compat::ReferenceBoneInfoBytes> reference{};
    Require(reader.Get(a.tokens[Asset].object+ww_game_compat::AssetRefBoneInfoOffset,ref)&&ref.num>0&&ref.num==int32_t(a.boneNames.size())&&
        ref.num<=int32_t(fb::kLimit)&&reader.Read(ref.data,reference.data(),size_t(ref.num)*ww_game_compat::ReferenceBoneInfoBytes),"actor reference skeleton unreadable");
    std::vector<std::string> names;std::vector<int32_t> parents;
    for(int32_t i=0;i<ref.num;++i){
        const auto* row=reference.data()+size_t(i)*ww_game_compat::ReferenceBoneInfoBytes;std::string name;
        Require(FullName(reader,runtime.pool,At<uint32_t>(row,0),At<uint32_t>(row,ww_game_compat::NameNumberOffset),name),"actor bone name unavailable");
        const auto& expected=a.boneNames[size_t(i)];
        names.push_back(id::SameName(name,expected)?expected:std::move(name));
        parents.push_back(At<int32_t>(row,ww_game_compat::ReferenceBoneParentOffset));
    }
    Require(names==a.boneNames&&parents==a.parents&&fb::PrepareTopology(a.parents,&a.topology),"actor reference skeleton differs");
}
inline void InitializeActor(ActorRuntime& a,const Json& graph,const Json& profile){auto& r=runtime;const auto& config=a.config;auto reader=CurrentProcess();
    for(uint32_t i=0;i<Objects;++i)Require(id::ReadStable(reader,r.go,id::Rva(graph.at(kObjectKeys[i])),&a.tokens[i]),"actor graph unreadable");
    const auto ownership=graph.at("ownership").get<std::string>();
    Require(ownership=="borrowed"||ownership=="created","unknown actor ownership");
    a.ownership=ownership=="borrowed"?native_animation_target::OwnerPolicy::SceneTeamMember:native_animation_target::OwnerPolicy::OwnedUiActor;
    a.boneNames=config.at("bone_names").get<std::vector<std::string>>();a.parents=config.at("bone_parents").get<std::vector<int32_t>>();
    VerifyReferenceSkeleton(a,reader);
    a.motion=clip::ReadFile(a.directory/config.at("local_clip_file").get<std::string>());
    a.positions=config.at("canonical_local_positions").get<std::vector<std::array<float,3>>>();a.rotations=config.at("canonical_local_rotations").get<std::vector<std::array<float,4>>>();
    a.positionMask=config.at("canonical_position_mask").get<std::vector<uint8_t>>();a.rotationMask=config.at("canonical_rotation_mask").get<std::vector<uint8_t>>();
    ValidateCanonicalPose(a);
    a.request.canonical_local_positions=a.positions.data();a.request.canonical_position_mask=a.positionMask.data();
    a.request.canonical_local_rotations=a.rotations.data();a.request.canonical_rotation_mask=a.rotationMask.data();
    a.nativePhysics=config.value("native_physics",true);a.physicsEnabled=config.value("physics_enabled",false)&&!a.nativePhysics;
    const auto p=config.value("placement",Json::object());
    a.placement={p.value("offset_x_cm",0.0),p.value("offset_y_cm",0.0),p.value("height_cm",0.0),p.value("yaw_degrees",0.0),p.value("in_place",false)};
    features::PrepareActor(a.features,reader,r.go,r.pool,config,a.directory,a.tokens[Mesh].object,a.tokens[Asset].object,a.motion,a.topology,a.request,r.epoch);
    if(!a.nativePhysics)return;
    Json targetConfig={{"character_profile_id",config.at("profile_id")},{"character_profile_sha256",config.at("character_profile_sha256")},
        {"expected_bone_count",a.motion.bone_count},{"expected_bone_names",a.boneNames},{"expected_bone_parents",a.parents},
        {"expected_skeleton_sha256",profile.at("skeleton_sha256")},{"expected_mesh_asset_name",config.at("mesh_asset_name")}};
    auto target=native_target_publication::Export(reader,r.go,r.pool,targetConfig,a.tokens[Actor],a.tokens[Mesh],a.tokens[Asset],a.tokens[Skeleton],a.tokens[MainAnim],a.authority,a.ownership,r.epoch);
    const auto out=r.directory/L"native_targets"/std::to_string(a.slot);std::filesystem::create_directories(out);ww_report::Save(out/L"native_target.json",target);
}
inline void Initialize(const std::filesystem::path& directory,const Json& graph,const Json& config,uint64_t host,uint64_t window){auto& r=runtime;r.initialized=true;
    try{
        r.host=host;r.directory=directory;r.gameThread=GetWindowThreadProcessId(reinterpret_cast<HWND>(window),nullptr);Require(r.gameThread!=0,"game window thread unavailable");
        r.epoch=config.at("session_epoch");
        auto reader=CurrentProcess();
        const auto anchors=ww_game_object_anchors::Resolve(reader,host,ww_game_compat::GObjectsRva,ww_game_compat::NamePoolRva,r.go,r.pool);
        ww_report::Save(directory/L"game_compatibility.json",anchors);
        Require(anchors.value("status",std::string())=="anchors_cross_validated","current object/name anchors unresolved; see game_compatibility.json");
        const auto& configs=config.at("actors");Require(configs.is_array()&&!configs.empty(),"scene has no actors");
        const auto controller=id::Rva(graph.at("player_controller")),pawn=id::Rva(graph.at("player_pawn"));
        double duration=0;r.actors.reserve(configs.size());Json targets=Json::array();
        for(const auto& actorConfig:configs){
            if(!actorConfig.at("enabled").get<bool>())continue;
            auto actor=std::make_unique<ActorRuntime>();auto& a=*actor;a.config=actorConfig;a.slot=actorConfig.at("slot");
            a.directory=directory/std::filesystem::u8path(actorConfig.at("actor_directory").get<std::string>());
            const Json* actorGraph=nullptr;
            for(const auto& row:graph.at("actors"))if(row.at("slot").get<uint32_t>()==a.slot){Require(!actorGraph,"duplicate graph actor slot");actorGraph=&row;}
            Require(actorGraph,"actor graph slot missing");
            std::ifstream file(a.directory/std::filesystem::u8path(actorConfig.at("character_profile_file").get<std::string>()));Require(bool(file),"actor profile missing");
            const auto profile=Json::parse(file);
            r.actors.push_back(std::move(actor));InitializeActor(a,*actorGraph,profile);duration=(std::max)(duration,a.motion.duration);
            if(a.features.face_enabled)duration=(std::max)(duration,a.features.face_clip.duration);
            if(a.nativePhysics)targets.push_back({{"slot",a.slot},{"native_target_file",std::string("native_targets/")+std::to_string(a.slot)+"/native_target.json"},
                {"native_input_directory",std::string("native_inputs/")+std::to_string(a.slot)}});
        }
        Require(!r.actors.empty(),"scene has no enabled actors");FreezeActorIndices();
        r.cameraAnchor=FindSlot(config.at("camera_anchor_slot"));Require(r.cameraAnchor,"camera anchor slot missing");
        // Every actor anchors the camera so the follow target tracks the live
        // formation centroid; the VMD orbit uses the managed stage origin.
        std::vector<uint64_t> cameraMeshes;cameraMeshes.reserve(r.actors.size());
        for(const auto& actor:r.actors)cameraMeshes.push_back(actor->tokens[Mesh].object);
        std::ifstream lifecycleFile(directory/L"managed_actor_lifecycle.json");Require(bool(lifecycleFile),"managed actor lifecycle missing");
        const auto stage=features::ReadStageReference(Json::parse(lifecycleFile));r.groundHeight=GroundHeight(config);
        features::PrepareCamera(r.cameraFeatures,reader,r.go,config,directory,controller,pawn,cameraMeshes,stage,r.epoch,r.gameThread,window);
        if(r.cameraFeatures.clip_available)duration=(std::max)(duration,r.cameraFeatures.clip.duration);
        Require(r.transport.SetClip(r.epoch,duration),"scene timeline configuration failed");
        const auto setting=[&](play::CommandKind kind,double value){Require(r.transport.Apply({r.epoch,++r.initialSequence,kind,value},0)==play::ControlCode::Ok,"scene playback setting invalid");};
        setting(play::CommandKind::SetSpeed,config.value("speed",1.0));setting(play::CommandKind::SetLoop,config.value("loop",false)?1:0);
        setting(play::CommandKind::SetTimeLimit,config.value("duration_ms",0.0)/1000.0);setting(play::CommandKind::SetCameraMode,config.value("camera_mode",0));
        setting(play::CommandKind::SetCameraTrackHeight,config.value("camera_track_height_cm",0.0));
        r.original=reinterpret_cast<Runtime::Publish>(host+ww_game_compat::FunctionPublishRva);
        r.startPaused=config.value("start_paused",false);
        if(r.startPaused)Require(r.transport.PreparePausedStart(),"scene paused startup configuration failed");
        ww_report::Save(directory/L"native_targets.json",{{"session_epoch",r.epoch},{"actors",targets}});
    }catch(...){Fail(Error::Arguments);throw;}
}
inline bool MaintainSceneFrame(void*){auto& r=runtime;if(!r.maintenanceEnabled.load(std::memory_order_acquire)||!r.maintainScene)return true;
    const bool ok=r.maintainScene(r.maintainUser);if(!ok)Fail(Error::Plan);return ok;
}
inline bool InstallSceneMaintenance(bool(*callback)(void*),void* user) noexcept {auto& r=runtime;
    r.maintainScene=callback;r.maintainUser=user;
    r.maintenanceEnabled.store(callback!=nullptr,std::memory_order_release);r.cameraFeatures.on_game_frame=&MaintainSceneFrame;r.cameraFeatures.game_frame_user=nullptr;
    // This same PCM hook also hosts playback camera tracks later. Installing
    // it before graph observation keeps borrowed actors evaluable throughout
    // a large scene's sequential native preparation. Created-only scenes also
    // maintain the visibility lease of a non-performing controlled player.
    return features::InstallCamera(r.cameraFeatures);
}
inline bool Install() noexcept {auto& r=runtime;r.observing.store(true);r.writing.store(false);
    const auto slot=reinterpret_cast<void**>(r.host+ww_game_compat::SlotComponentPublishRva);
    if(vmt::Install(r.hook,slot,reinterpret_cast<void*>(r.original),reinterpret_cast<void*>(&Detour)).code!=vmt::Code::Ok){Fail(Error::Install);return false;}
    try{
        if(NeedsNative()){native::Install();r.nativeInstalled.store(true);}
        if(!r.cameraFeatures.hook.changed&&!features::InstallCamera(r.cameraFeatures)){Fail(Error::Install);return false;}
    }catch(...){Fail(Error::Install);return false;}
    return true;
}
inline bool Failed() noexcept {const auto& r=runtime;if(r.error.load()||r.cameraFeatures.failed.load())return true;for(const auto& actor:r.actors)if(actor->native.error.load())return true;return false;}
inline bool Ready() noexcept {const auto& r=runtime;if(!r.observing.load()||r.closing.load()||r.actors.empty()||Failed())return false;
    for(const auto& actor:r.actors)if(!actor->features.prepared||(actor->nativePhysics&&(!r.nativeInstalled.load()||!actor->native.ready.load())))return false;return true;}
inline void Start() noexcept {auto& r=runtime;r.lastWireSequence=++r.initialSequence;
    if(!r.startPaused&&!r.commands.Push({r.epoch,r.initialSequence,play::CommandKind::Play,0})){Fail(Error::Plan);return;}r.writing.store(true);}
inline void PollCommands(){auto& r=runtime;std::ifstream stream(r.directory/L"control.dat",std::ios::binary);play::CommandWire wire{};play::Command command;
    if(stream&&stream.read(reinterpret_cast<char*>(wire.data()),wire.size())&&play::DecodeCommandFrame(wire,command)&&command.sequence>r.lastWireSequence&&r.commands.Push(command))
        r.lastWireSequence=command.sequence;}
struct WorkerLock {std::lock_guard<std::mutex> lock{runtime.publicationMutex};};
inline void SaveModelPreview(const std::filesystem::path& path,uint64_t& revision,uint64_t& frame){
    play::model_preview::Frame snapshot;
    size_t bytes=0;
    {
        WorkerLock lock;
        const auto& model=runtime.modelPreview;
        if(!model.slot||!model.count||!model.frame||(revision==model.revision&&frame==model.frame))return;
        bytes=offsetof(play::model_preview::Frame,bones)+model.count*sizeof(play::model_preview::Bone);
        std::memcpy(&snapshot,&model,bytes);
    }
    if(ww_report::WriteBytes(path,{reinterpret_cast<const char*>(&snapshot),bytes})){
        revision=snapshot.revision;frame=snapshot.frame;
    }
}
inline play::PlayerState Snapshot(){WorkerLock lock;return runtime.transport.State();}
inline bool FeaturePending(){WorkerLock lock;for(const auto& actor:runtime.actors)if(features::ActorPending(actor->features))return true;return features::CameraPending(runtime.cameraFeatures);}
inline bool NativePending(){for(const auto& actor:runtime.actors)if(actor->nativePrepared&&!native::Restored(actor->native))return true;return false;}
inline void RevokeTargets(){for(auto& actor:runtime.actors)native_target_publication::Revoke(actor->authority);}
// The transport enforces the limit on the game-thread clock, which only
// advances while the actors publish. This wall-clock check still closes a
// session whose publications stalled.
// The transport clock advances only inside the mesh-publish detour. If every
// actor stops publishing (culled or hidden), this worker deadline still ends
// the session at the user-selected residency limit.
inline void CheckTimeLimit(){auto& r=runtime;const auto started=r.actualPlayStart.load();if(!started||r.closing.load())return;const auto state=Snapshot();
    if(state.time_limit_seconds>0&&double(GetTickCount64()-started)/1000.0>=state.time_limit_seconds){r.deadlineExpired.store(true);r.writing.store(false);r.closing.store(true);}}
inline const char* ModeName(play::PlayerMode mode){switch(mode){case play::PlayerMode::Empty:return "empty";case play::PlayerMode::Stopped:return "stopped";
    case play::PlayerMode::Playing:return "playing";case play::PlayerMode::Paused:return "paused";case play::PlayerMode::Ended:return "ended";}return "empty";}
inline bool Finished() noexcept {return runtime.closing.load();}
inline Json ActorStatus(const ActorRuntime& a,bool pending,uint64_t frames,uint64_t writes){
    Json actor={{"slot",a.slot},{"ownership",a.ownership==native_animation_target::OwnerPolicy::SceneTeamMember?"borrowed":"created"},
        {"character_display_name",a.config.value("character_display_name",std::string())},
        {"frames",frames},{"writes",writes},{"native_physics",a.nativePhysics},{"physics_enabled",a.physicsEnabled},{"physics_available",a.features.physics_available},
        {"expression_enabled",a.features.face_enabled},{"restoration_complete",!pending},
        {"placement",{{"offset_x_cm",a.placement.x},{"offset_y_cm",a.placement.y},{"height_cm",a.placement.height},{"yaw_degrees",a.placement.yaw_degrees},{"in_place",a.placement.in_place}}}};
    if(a.nativePrepared){
        native::Stats(a.native,actor);
        bool configured=false,pendingLayout=false;
        for(unsigned i=0;i<a.native.binding.group_count;++i){
            const auto& state=a.native.capsules[i];
            std::lock_guard<std::mutex> lock(state.edit);
            if(!state.layout)continue;
            configured=true;
            pendingLayout|=!state.layout_applied;
            if(state.layout_error&&!actor.contains("capsule_layout_error"))
                actor["capsule_layout_error"]=a.native.binding.groups[i].curve_name+": "+state.layout_error;
        }
        if(configured){
            actor["capsule_layout_pending"]=pendingLayout&&!actor.contains("capsule_layout_error");
            actor["capsule_layout_applied"]=!pendingLayout&&!actor.contains("capsule_layout_error");
        }
    }
    actor["gravity_parts"]=Json::array();
    for(const auto& part:a.features.gravity_parts)
        actor["gravity_parts"].push_back({{"id",part.id},{"root",part.root},{"strength",part.strength},{"sampled_revision",part.sampled},{"moved_bones",part.moved_bones}});
    return actor;
}
inline bool HooksRestored(){const auto& r=runtime;const auto& hub=native::hub;
    for(const auto* patch:{&r.hook,&r.cameraFeatures.hook,&hub.converter_patch,&hub.group_patch,&hub.settings_patch})if(patch->changed||patch->protection_pending)return false;
    return true;
}
inline Json Status(){auto& r=runtime;WorkerLock lock;if(r.payloadReleased)return r.terminalStatus;const auto state=r.transport.State();const auto& hub=native::hub;
    Json actors=Json::array();uint64_t frames=0,writes=0,nativeFrames=0;size_t borrowed=0;uint32_t nativeError=0;
    bool allObserved=!r.actors.empty(),face=false,allPhysics=true,anyNative=false,actorPending=false,layoutsApplied=true;
    for(const auto& entry:r.actors){const auto& a=*entry;
        const auto count=a.nativePhysics?a.native.frames.load():a.features.frames;const auto writeCount=a.nativePhysics?a.writes.load():a.features.writes;
        frames+=count;writes+=writeCount;face=face||a.features.face_enabled;allPhysics=allPhysics&&a.features.physics_available;anyNative=anyNative||a.nativePhysics;allObserved=allObserved&&count>0;
        const bool pending=features::ActorPending(a.features)||(a.nativePrepared&&!native::Restored(a.native));actorPending=actorPending||pending;
        if(a.ownership==native_animation_target::OwnerPolicy::SceneTeamMember)++borrowed;
        if(a.nativePrepared){nativeFrames+=a.native.frames.load();if(a.native.error.load())nativeError=a.native.error.load();}
        auto actor=ActorStatus(a,pending,count,writeCount);
        layoutsApplied=layoutsApplied&&actor.value("capsule_layout_applied",true);
        actors.push_back(std::move(actor));
    }
    const auto& camera=r.cameraFeatures;
    const bool initialPoseReady=allObserved&&(!camera.available||camera.applied_frames.load()!=0);
    const bool active=r.writing.load()&&!r.closing.load();
    Json value={{"schema",3},{"actor_count",r.actors.size()},{"borrowed_count",borrowed},{"created_count",r.actors.size()-borrowed},{"actors",actors},
        {"all_actors_observed",allObserved},{"initialized",r.initialized},{"ready",Ready()&&layoutsApplied&&(!r.startPaused||initialPoseReady)},{"start_paused",r.startPaused},
        {"initial_pose_ready",initialPoseReady},{"failed",Failed()},{"error_code",r.error.load()?r.error.load():nativeError},
        {"restoration_complete",r.closed.load()&&!actorPending&&!features::CameraPending(camera)},{"hooks_restored",HooksRestored()},
        {"callbacks_quiescent",!r.active.load()&&!r.work.load()&&!r.observing.load()&&!camera.callbacks.load()&&!camera.work.load()&&!camera.observing.load()&&
            !hub.active.load()&&!hub.group_active.load()},
        {"clip_epoch",r.epoch},{"frames",frames},{"writes",writes},
        {"seconds",state.seconds},{"duration_seconds",state.duration_seconds},{"speed",state.speed},{"loop",state.loop},
        {"preview_loop_start",state.preview_begin},{"preview_loop_end",state.preview_end},{"mode",r.closed.load()?"closed":ModeName(state.mode)},
        {"model_preview_slot",r.modelPreview.slot},{"model_preview_revision",r.modelPreview.revision},
        {"last_sequence",state.last_sequence},{"revision",state.revision},{"restore_requested",state.restore_requested||r.closing.load()},{"session_active",active},
        {"time_limit_seconds",state.time_limit_seconds},{"session_elapsed_seconds",state.session_elapsed_seconds},{"time_limit_reached",state.time_limit_reached||r.deadlineExpired.load()},
        {"camera_mode",state.camera_mode},{"camera_track_height_cm",state.camera_track_height_cm},{"camera_anchor_slot",r.cameraAnchor?r.cameraAnchor->slot:0},
        {"camera_available",camera.available},{"camera_clip_available",camera.clip_available},{"camera_applied_frames",camera.applied_frames.load()},{"camera_failed",camera.failed.load()},
        {"ground_height_cm",r.groundHeight.load()},{"scene_maintenance",r.maintenanceEnabled.load()},
        {"native_physics",anyNative},{"native_input_frames",nativeFrames},{"native_input_error",nativeError},{"physics_available",allPhysics},{"expression_enabled",face},
        {"last_control_sequence",r.lastControlSequence},{"last_control_code",r.lastControlCode.load()},{"rejected_commands",r.rejectedCommands.load()}};
    play::camera_diagnostics::Snapshot diagnostic;
    if(camera.diagnostics.TryRead(diagnostic)&&diagnostic.anchor_captured){
        value["camera_anchor_position"]=diagnostic.anchor;
        value["camera_source_to_ue_xyzw"]=diagnostic.source_to_ue;
        value["camera_source_unit_scale"]=diagnostic.source_unit_scale;}
    return value;
}
inline bool CloseAndRestore(uint64_t deadline) noexcept {auto& r=runtime;r.writing.store(false);r.closing.store(true);r.nativeInstalled.store(false);
    {WorkerLock lock;auto state=CameraState(r.transport.State());state.restore_requested=true;features::PublishCamera(r.cameraFeatures,state,false);}
    for(auto& actor:r.actors)if(actor->nativePrepared)native::CloseAdmission(actor->native);
    while((FeaturePending()||NativePending())&&GetTickCount64()<deadline)Sleep(10);if(FeaturePending()||NativePending())return false;
    // Keep borrowed actors visible/evaluable until their input and expression
    // ownership has drained. Keep non-performing player visibility leased until
    // that point too. Disable maintenance before managed Stop restores display
    // flags and the borrowed actors' original transform and tick policy.
    r.maintenanceEnabled.store(false,std::memory_order_release);
    while(!features::UninstallCamera(r.cameraFeatures)&&GetTickCount64()<deadline)Sleep(10);
    if(r.cameraFeatures.hook.changed||r.cameraFeatures.hook.protection_pending||r.cameraFeatures.observing.load()||
        r.cameraFeatures.work.load()||features::CameraPending(r.cameraFeatures))return false;
    const bool registered=std::any_of(r.actors.begin(),r.actors.end(),[](const auto& actor){return actor->nativeRegistered;});
    if(registered){
        if(!native::Uninstall())return false;
        for(auto& actor:r.actors)if(actor->nativeRegistered){if(!native::UnregisterActor(actor->native))return false;actor->nativeRegistered=false;}
    }
    while(r.work.load()&&GetTickCount64()<deadline)Sleep(10);if(r.work.load())return false;r.observing.store(false);
    if(r.hook.changed||r.hook.protection_pending){if(vmt::Restore(r.hook).code!=vmt::Code::Ok)return false;}
    while((r.active.load()||r.work.load())&&GetTickCount64()<deadline)Sleep(10);
    const bool complete=!r.hook.changed&&!r.hook.protection_pending&&!r.active.load()&&!r.work.load();r.closed.store(complete);return complete;
}
// Called only after native and managed recovery have completed and the final
// report has been materialized. Keep the pinned forwarding objects/originals:
// a thread can still hold an old detour address after its slot was restored.
inline bool ReleaseRetiredPayload(Json& terminal){auto& r=runtime;WorkerLock lock;auto& hub=native::hub;
    if(r.payloadReleased){terminal=r.terminalStatus;return true;}
    if(!terminal.value("cleanup_complete",false)||!terminal.value("managed_owner_retirement_checked",false))return false;
    const bool idle=r.closed.load()&&!r.observing.load()&&!r.writing.load()&&!r.work.load()&&!r.maintenanceEnabled.load();
    const bool cameraIdle=!r.cameraFeatures.observing.load()&&!r.cameraFeatures.work.load()&&!features::CameraPending(r.cameraFeatures);
    const bool hubIdle=!hub.observing.load()&&!hub.group_observing.load()&&!hub.dispatch_active.load();
    if(!idle||!cameraIdle||!hubIdle||!HooksRestored())return false;
    for(const auto& actor:r.actors)if(actor->nativeRegistered||features::ActorPending(actor->features)||
        (actor->nativePrepared&&!native::Restored(actor->native)))return false;
    std::lock_guard<std::mutex> registryLock(hub.registry.operations);
    if(hub.registry.count.load()||hub.registry.frozen.load())return false;
    terminal["retired_payload_released"]=true;r.terminalStatus=terminal;
    r.cameraAnchor=nullptr;r.maintainScene=nullptr;r.maintainUser=nullptr;
    decltype(r.bySlot){}.swap(r.bySlot);decltype(r.byMesh){}.swap(r.byMesh);decltype(r.actors){}.swap(r.actors);
    decltype(hub.registry.actors){}.swap(hub.registry.actors);
    auto& camera=r.cameraFeatures;
    decltype(camera.anchor_meshes){}.swap(camera.anchor_meshes);decltype(camera.anchor_identities){}.swap(camera.anchor_identities);
    decltype(camera.clip.source_keys){}.swap(camera.clip.source_keys);
    r.payloadReleased=true;return true;
}
} // namespace owned_mmd_body
