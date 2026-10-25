#pragma once
// One scene coordinator, one clock and camera, and any number of stable actors.
// Actor/index storage is prepared before callbacks and never grows while live.
#include "../../../probes/v20_recon/object_identity.h"
#include "../../../probes/v20_recon/publish_observer.h"
#include "../../../probes/v20_recon/vmt_slot_patch.h"
#include "../../../playback/vmd_local_clip.h"
#include "../../../playback/player_transport.h"
#include "../../../probes/v20_recon/native_target_publication.h"
#include "../../../probes/v20_recon/shared_native_physics.h"
#include "../../../probes/v20_recon/unified_player_features.h"
#include "../../../engine_ue4/pose_full_body.h"
#include <algorithm>
#include <atomic>
#include <array>
#include <cmath>
#include <filesystem>
#include <memory>
#include <vector>

namespace owned_mmd_body {
namespace ob=v20_publish_observer;
namespace id=ww_object;
namespace fb=wwp_ue::full_body;
namespace sub=wwp_ue::subtree_rotation;
namespace clip=wwp_playback::local_clip;
namespace play=wwp_playback;
namespace vmt=v20_vmt_slot_patch;
namespace features=unified_player_features;
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
    features::ActorFeatures features;v20_shared_native_physics::Actor native;
    native_animation_target::AuthoritySnapshot authority{};
    std::atomic<bool> nativePrepared{false};std::atomic<uint64_t> writes{0};
};
struct Runtime {
    bool initialized=false,multiActor=false,startPaused=false,payloadReleased=false;
    Json terminalStatus;
    std::atomic<double> formationHeightCorrection{0};
    std::filesystem::path directory;
    uint64_t host=0,publishRva=0,slotRva=0,epoch=0,durationMs=0,frameRva=0x992f2d8,lastEngineFrame=0;
    play::Transport transport;play::SpscCommandQueue<64> commands;
    uint64_t initialSequence=0,lastWireSequence=0;DWORD gameThread=0;
    Go go{};Pool pool{};
    std::vector<std::unique_ptr<ActorRuntime>> actors;
    std::vector<ActorRuntime*> bySlot,byMesh;ActorRuntime* cameraAnchor=nullptr;
    bool (*maintainScene)(void*)=nullptr;void* maintainUser=nullptr;std::atomic<bool> maintenanceEnabled{false};
    features::CameraFeatures cameraFeatures;vmt::State hook;
    using Publish=void(*)(void*);Publish original=nullptr;
    std::atomic<bool> observing{false},writing{false},closing{false},closed{false},deadlineExpired{false},nativeInstalled{false};
    std::atomic_flag busy=ATOMIC_FLAG_INIT;
    std::atomic<uint32_t> error{0},active{0},work{0},lastControlCode{0};
    std::atomic<uint64_t> rejectedCommands{0},playStart{0},actualPlayStart{0};
};
inline Runtime runtime;
inline void Require(bool value,const char* message){if(!value)throw std::runtime_error(message);}
inline void Fail(Error error) noexcept {
    uint32_t empty=0;runtime.error.compare_exchange_strong(empty,uint32_t(error));
    runtime.writing.store(false,std::memory_order_release);runtime.closing.store(true,std::memory_order_release);
}
struct Reader {
    bool Read(uint64_t address,void* out,size_t size){SIZE_T count=0;return address&&ReadProcessMemory(GetCurrentProcess(),(LPCVOID)address,out,size,&count)&&count==size;}
    template<class T>bool Get(uint64_t address,T& out){return Read(address,&out,sizeof(out));}
};
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
inline bool Routed(play::CommandKind kind){return kind==play::CommandKind::SetPhysics||kind==play::CommandKind::SetGroundHeight||(kind>=play::CommandKind::SetPlacementX&&kind<=play::CommandKind::ResetPlacement);}
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
inline play::ControlCode ApplyCommand(const play::Command& command,double now){auto& r=runtime;
    if(command.kind==play::CommandKind::SetGroundHeight&&command.target_slot)return play::ControlCode::InvalidCommand;
    if(command.target_slot&&!FindSlot(command.target_slot))return play::ControlCode::InvalidCommand;
    if(command.kind==play::CommandKind::SetCameraMode&&command.value==2&&!r.cameraFeatures.clip_available)return play::ControlCode::InvalidCommand;
    if(Routed(command.kind)){
        const auto code=r.transport.AcceptRoutedCommand(command,now);if(code!=play::ControlCode::Ok)return code;
        if(command.kind==play::CommandKind::SetGroundHeight){r.formationHeightCorrection=command.value;return code;}
        if(command.target_slot)ApplyActorCommand(*FindSlot(command.target_slot),command);
        else for(auto& actor:r.actors)ApplyActorCommand(*actor,command);
        return code;
    }
    return r.transport.Apply(command,now);
}
inline bool Advance(uint64_t engineFrame,double now){auto& r=runtime;if(engineFrame==r.lastEngineFrame)return true;
    r.lastEngineFrame=engineFrame;play::Command command;
    while(r.commands.Pop(&command)){const auto code=ApplyCommand(command,now);r.lastControlCode.store(uint32_t(code));
        if(code!=play::ControlCode::Ok)r.rejectedCommands.fetch_add(1);
        else if(command.kind==play::CommandKind::Play&&!r.actualPlayStart.load())r.actualPlayStart.store(GetTickCount64());}
    if(r.transport.Tick(now)!=play::ControlCode::Ok){Fail(Error::Plan);return false;}
    if(r.transport.State().restore_requested){r.writing.store(false);r.closing.store(true);}return true;
}
inline play::placement::Settings EffectivePlacement(const ActorRuntime& actor){auto placed=actor.placement;placed.height+=runtime.formationHeightCorrection.load();return placed;}
inline double FormationHeightCorrection(const Json& config,const Json&){
    // One explicit offset for both formation modes. Do not additionally apply
    // the previous capsule-gap correction, which would lower native formation twice.
    const auto correction=config.value("ground_height_cm",-2.0);
    Require(std::isfinite(correction)&&correction>=-1000&&correction<=1000,"invalid standing height");return correction;
}
inline play::PlayerState CameraState(const play::PlayerState& timeline){auto state=timeline;if(const auto* actor=runtime.cameraAnchor){
    state.placement_x=actor->placement.x;state.placement_y=actor->placement.y;state.placement_height=EffectivePlacement(*actor).height;
    state.placement_yaw_degrees=actor->placement.yaw_degrees;state.placement_in_place=actor->placement.in_place;}return state;}
inline void Detour(void* object){auto& r=runtime;r.active.fetch_add(1,std::memory_order_acq_rel);
    struct Active {~Active(){runtime.active.fetch_sub(1,std::memory_order_release);}} active;
    r.original(object);
    if(!r.observing.load(std::memory_order_acquire)||GetCurrentThreadId()!=r.gameThread)return;
    auto* actor=FindMesh(uint64_t(object));if(!actor||r.busy.test_and_set(std::memory_order_acquire))return;
    r.work.fetch_add(1,std::memory_order_acq_rel);
    struct Work {~Work(){runtime.work.fetch_sub(1,std::memory_order_release);runtime.busy.clear(std::memory_order_release);}} work;
    if(actor->native.error.load()||r.cameraFeatures.failed.load())Fail(Error::Plan);
    Reader read;uint64_t frame=0;if(!read.Get(r.host+r.frameRva,frame)||!frame)Fail(Error::Plan);else Advance(frame,GetTickCount64()/1000.0);
    auto state=r.transport.State();state.physics_enabled=actor->physicsEnabled;
    const bool feed=r.writing.load()&&!r.closing.load()&&state.mode!=play::PlayerMode::Stopped&&state.mode!=play::PlayerMode::Empty;
    ::Reader featureReader{GetCurrentProcess()};
    const auto placement=EffectivePlacement(*actor);
    if(!features::ActorFrame(actor->features,featureReader,state,placement,feed))Fail(Error::Plan);
    if(actor->nativePhysics)v20_shared_native_physics::Publish(actor->native,state.seconds,placement,feed&&!r.closing.load());
    features::PublishCamera(r.cameraFeatures,CameraState(state),feed&&!r.closing.load());
}
inline bool NativeAuthority(void* user,const v20_shared_native_physics::Access& access){
    const auto& a=*static_cast<ActorRuntime*>(user);uint64_t owner=0,asset=0,skeleton=0,main=0;
    return access.Get(a.tokens[Mesh].object+0xa8,owner)&&owner==a.tokens[Actor].object&&access.Get(a.tokens[Mesh].object+0x5d0,asset)&&asset==a.tokens[Asset].object&&
        access.Get(asset+0xd8,skeleton)&&skeleton==a.tokens[Skeleton].object&&access.Get(a.tokens[Mesh].object+0x910,main)&&main==a.tokens[MainAnim].object;
}
inline void NativeCommitted(void* user,uint32_t count){static_cast<ActorRuntime*>(user)->writes.fetch_add(count);}
inline bool NeedsNative(){for(const auto& actor:runtime.actors)if(actor->nativePhysics)return true;return false;}
inline void PrepareNative(){auto& r=runtime;Reader reader;
    for(auto& actor:r.actors){auto& a=*actor;if(!a.nativePhysics)continue;
        const auto directory=r.multiActor?r.directory/L"native_inputs"/std::to_string(a.slot):r.directory/L"native_input";
        std::ifstream input(directory/L"native_playback.json");Require(bool(input),"actor native playback input missing");const auto config=Json::parse(input);
        v20_shared_native_physics::PrepareFromConfig(a.native,reader,config,config.at("character_profile_id"),directory,a.motion,a.topology,a.request,r.epoch,&NativeAuthority,&a);
        a.native.on_commit=&NativeCommitted;a.native.external_writing=&r.writing;
        Require(v20_shared_native_physics::RegisterActor(a.native),"actor native registration failed");a.nativeRegistered=true;a.nativePrepared=true;
    }
}
inline clip::Clip ReadClip(const std::filesystem::path& path){
    return clip::ReadFile(path);
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
inline void InitializeActor(ActorRuntime& a,const Json& graph,const Json& profile,uint64_t window){auto& r=runtime;const auto& config=a.config;Reader reader;
    for(uint32_t i=0;i<Objects;++i)Require(id::ReadStable(reader,r.go,id::Rva(graph.at(kObjectKeys[i])),&a.tokens[i]),"actor graph unreadable");
    const auto ownership=graph.value("ownership",config.value("ownership",std::string("created")));
    Require(ownership=="borrowed"||ownership=="created","unknown actor ownership");
    a.ownership=ownership=="borrowed"?native_animation_target::OwnerPolicy::SceneTeamMember:native_animation_target::OwnerPolicy::OwnedUiActor;
    a.boneNames=config.at("bone_names").get<std::vector<std::string>>();a.parents=config.at("bone_parents").get<std::vector<int32_t>>();
    ob::ArrayHead ref;std::array<unsigned char,fb::kLimit*16> reference{};
    Require(reader.Get(a.tokens[Asset].object+0x3b8,ref)&&ref.num==int32_t(a.boneNames.size())&&ref.num<=int32_t(fb::kLimit)&&reader.Read(ref.data,reference.data(),size_t(ref.num)*16),"actor reference skeleton unreadable");
    std::vector<std::string> names;std::vector<int32_t> parents;
    for(int32_t i=0;i<ref.num;++i){const auto* row=reference.data()+size_t(i)*16;std::string name;Require(id::DecodeName(reader,r.pool,At<uint32_t>(row,0),name),"actor bone name unavailable");
        const auto number=At<uint32_t>(row,8);if(number)name+="_"+std::to_string(number-1);names.push_back(name);parents.push_back(At<int32_t>(row,12));}
    sub::Manifest manifest;manifest.expected_count=uint32_t(a.boneNames.size());manifest.expected_names=a.boneNames;manifest.expected_parents=a.parents;
    Require(fb::PrepareTopology(manifest,names,parents,&a.topology),"actor reference skeleton differs");a.motion=ReadClip(a.directory/config.at("local_clip_file").get<std::string>());
    a.positions=config.at("canonical_local_positions").get<std::vector<std::array<float,3>>>();a.rotations=config.at("canonical_local_rotations").get<std::vector<std::array<float,4>>>();
    a.positionMask=config.at("canonical_position_mask").get<std::vector<uint8_t>>();a.rotationMask=config.at("canonical_rotation_mask").get<std::vector<uint8_t>>();
    ValidateCanonicalPose(a);
    a.request.canonical_local_positions=a.positions.data();a.request.canonical_position_mask=a.positionMask.data();a.request.canonical_local_rotations=a.rotations.data();a.request.canonical_rotation_mask=a.rotationMask.data();
    a.nativePhysics=config.value("native_physics",true);a.physicsEnabled=config.value("physics_enabled",false)&&!a.nativePhysics;
    const auto p=config.value("placement",Json::object());a.placement={p.value("offset_x_cm",0.0),p.value("offset_y_cm",0.0),p.value("height_cm",0.0),p.value("yaw_degrees",0.0),p.value("in_place",false)};
    auto featureConfig=config;featureConfig["expected_bone_parents"]=a.parents;::Reader featureReader{GetCurrentProcess()};
    features::PrepareActor(a.features,featureReader,r.go,r.pool,featureConfig,a.directory,a.tokens[Mesh].object,a.tokens[Asset].object,a.motion,a.topology,a.request,r.epoch);
    if(a.nativePhysics){Json targetConfig={{"character_profile_id",config.at("profile_id")},{"character_profile_sha256",config.at("character_profile_sha256")},
        {"expected_bone_count",a.motion.bone_count},{"expected_bone_names",a.boneNames},{"expected_bone_parents",a.parents},{"expected_skeleton_sha256",profile.at("skeleton_sha256")},
        {"expected_mesh_asset_name",config.at("mesh_asset_name")},{"expected_mesh_asset_names",profile.at("mesh_asset_names")},{"expected_skeleton_asset_name",profile.at("skeleton_asset_name")},{"game_window_handle",Hex(window)}};
        auto target=native_target_publication::Export(reader,r.go,r.pool,targetConfig,a.tokens[Actor],a.tokens[Mesh],a.tokens[Asset],a.tokens[Skeleton],a.tokens[MainAnim],a.authority,a.ownership,r.epoch);
        const auto out=r.multiActor?r.directory/L"native_targets"/std::to_string(a.slot):r.directory;std::filesystem::create_directories(out);ww_report::Save(out/L"native_target.json",target);
    }
}
inline void Initialize(const std::filesystem::path& directory,const Json& graph,const Json& config,const Json& legacyProfile,const Json& layout,uint64_t host,uint64_t window){auto& r=runtime;r.initialized=true;
    try{
        r.host=host;r.directory=directory;r.gameThread=GetWindowThreadProcessId(reinterpret_cast<HWND>(window),nullptr);Require(r.gameThread!=0,"game window thread unavailable");
        r.publishRva=id::Rva(layout.at("publish_rva"));r.slotRva=id::Rva(layout.at("slot_rva"));r.frameRva=layout.contains("engine_frame_rva")?id::Rva(layout.at("engine_frame_rva")):0x992f2d8;
        r.epoch=config.at("session_epoch");r.durationMs=config.value("duration_ms",uint64_t(0));r.multiActor=config.contains("actors");
        Reader reader;unsigned char goBytes[32],poolBytes[24];const auto goAddress=host+id::Rva(layout.at("gobjects_rva")),poolAddress=host+id::Rva(layout.at("name_pool_rva"));
        Require(reader.Read(goAddress,goBytes,sizeof(goBytes))&&GoShape(goBytes)&&reader.Read(poolAddress,poolBytes,sizeof(poolBytes))&&PoolShape(poolBytes),"current object/name anchors invalid");
        r.go={goAddress,At<uint64_t>(goBytes,0),At<uint32_t>(goBytes,20)};r.pool={poolAddress,poolAddress+16,At<uint32_t>(poolBytes,8),At<uint32_t>(poolBytes,12)};
        const auto configs=r.multiActor?config.at("actors"):Json::array({config});Require(configs.is_array()&&!configs.empty(),"scene has no actors");
        double duration=0;r.actors.reserve(configs.size());Json targets=Json::array();
        for(const auto& actorConfig:configs){if(!actorConfig.value("enabled",true))continue;auto actor=std::make_unique<ActorRuntime>();auto& a=*actor;a.config=actorConfig;a.slot=actorConfig.value("slot",actorConfig.value("target_slot",uint32_t(1)));
            a.directory=r.multiActor?directory/std::filesystem::u8path(actorConfig.value("actor_directory",std::string("actors/")+std::to_string(a.slot))):directory;
            const Json* actorGraph=&graph;if(r.multiActor){actorGraph=nullptr;for(const auto& row:graph.at("actors"))if(row.at("slot").get<uint32_t>()==a.slot){Require(!actorGraph,"duplicate graph actor slot");actorGraph=&row;}Require(actorGraph,"actor graph slot missing");}
            Json profile=legacyProfile;if(r.multiActor){std::ifstream file(a.directory/std::filesystem::u8path(actorConfig.value("character_profile_file",std::string("character_profile.json"))));Require(bool(file),"actor profile missing");profile=Json::parse(file);}
            r.actors.push_back(std::move(actor));InitializeActor(a,*actorGraph,profile,window);duration=(std::max)(duration,a.motion.duration);
            if(a.features.face_enabled)duration=(std::max)(duration,a.features.face_clip.duration);
            if(a.nativePhysics)targets.push_back({{"slot",a.slot},{"native_target_file",std::string("native_targets/")+std::to_string(a.slot)+"/native_target.json"},{"native_input_directory",std::string("native_inputs/")+std::to_string(a.slot)}});
        }
        Require(!r.actors.empty(),"scene has no enabled actors");FreezeActorIndices();r.cameraAnchor=FindSlot(config.value("camera_anchor_slot",r.actors.front()->slot));Require(r.cameraAnchor,"camera anchor slot missing");
        ::Reader featureReader{GetCurrentProcess()};const auto controller=id::Rva(graph.contains("player_controller")?graph.at("player_controller"):config.at("camera_controller_address"));
        const auto pawn=id::Rva(graph.contains("player_pawn")?graph.at("player_pawn"):config.at("camera_pawn_address"));
        // Every actor anchors the camera so the follow target tracks the live
        // formation centroid. The VMD orbit prefers the managed stage origin
        // (the shared world reference written when actors were placed); each
        // clip's frame-0 root shift backs the first-actor fallback spot.
        std::vector<uint64_t> cameraMeshes;std::vector<Vec3> cameraRootStarts;cameraMeshes.reserve(r.actors.size());cameraRootStarts.reserve(r.actors.size());
        for(const auto& actor:r.actors){const auto& root=actor->motion.frames.empty()?clip::BoneCommand{}:actor->motion.frames[0];
            cameraMeshes.push_back(actor->tokens[Mesh].object);cameraRootStarts.push_back({root.translation_delta[0],root.translation_delta[1],root.translation_delta[2]});}
        features::StageReference stageReference;
        std::ifstream lifecycleFile(directory/L"managed_actor_lifecycle.json");
        const auto lifecycle=lifecycleFile?Json::parse(lifecycleFile):Json::object();
        stageReference=features::ReadStageReference(lifecycle);r.formationHeightCorrection=FormationHeightCorrection(config,lifecycle);
        features::PrepareCamera(r.cameraFeatures,featureReader,r.go,config,directory,controller,pawn,cameraMeshes,cameraRootStarts,stageReference,r.epoch,r.gameThread,window);
        if(r.cameraFeatures.clip_available)duration=(std::max)(duration,r.cameraFeatures.clip.duration);
        Require(r.transport.SetClip(r.epoch,duration),"scene timeline configuration failed");
        const auto setting=[&](play::CommandKind kind,double value){Require(r.transport.Apply({r.epoch,++r.initialSequence,kind,value},0)==play::ControlCode::Ok,"scene playback setting invalid");};
        setting(play::CommandKind::SetSpeed,config.value("speed",1.0));setting(play::CommandKind::SetLoop,config.value("loop",false)?1:0);
        setting(play::CommandKind::SetTimeLimit,double(r.durationMs)/1000.0);setting(play::CommandKind::SetCameraMode,config.value("camera_mode",0));r.original=reinterpret_cast<Runtime::Publish>(host+r.publishRva);
        r.startPaused=config.value("start_paused",false);
        if(r.startPaused)Require(r.transport.PreparePausedStart(),"scene paused startup configuration failed");
        if(r.multiActor)ww_report::Save(directory/L"native_targets.json",{{"session_epoch",r.epoch},{"actors",targets}});
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
    if(vmt::Install(r.hook,reinterpret_cast<void**>(r.host+r.slotRva),reinterpret_cast<void*>(r.original),reinterpret_cast<void*>(&Detour)).code!=vmt::Code::Ok){Fail(Error::Install);return false;}
    try{if(NeedsNative()){Json report;v20_shared_native_physics::Install(report);r.nativeInstalled.store(true);}if(!r.cameraFeatures.hook.changed&&!features::InstallCamera(r.cameraFeatures)){Fail(Error::Install);return false;}}
    catch(...){Fail(Error::Install);return false;}return true;
}
inline bool Failed() noexcept {const auto& r=runtime;if(r.error.load()||r.cameraFeatures.failed.load())return true;for(const auto& actor:r.actors)if(actor->native.error.load())return true;return false;}
inline bool Ready() noexcept {const auto& r=runtime;if(!r.observing.load()||r.closing.load()||r.actors.empty()||Failed())return false;
    for(const auto& actor:r.actors)if(!actor->features.prepared||(actor->nativePhysics&&(!r.nativeInstalled.load()||!actor->native.ready.load())))return false;return true;}
inline void Start(uint64_t now) noexcept {auto& r=runtime;r.lastWireSequence=++r.initialSequence;
    if(!r.startPaused&&!r.commands.Push({r.epoch,r.initialSequence,play::CommandKind::Play,0})){Fail(Error::Plan);return;}r.playStart.store(now);r.writing.store(true);}
inline void PollCommands(){auto& r=runtime;std::ifstream stream(r.directory/L"control.dat",std::ios::binary);play::CommandWire wire{};play::Command command;
    if(stream&&stream.read(reinterpret_cast<char*>(wire.data()),wire.size())&&play::DecodeCommand(wire,&command)&&command.sequence>r.lastWireSequence&&r.commands.Push(command))r.lastWireSequence=command.sequence;}
struct WorkerLock {WorkerLock(){while(runtime.busy.test_and_set(std::memory_order_acquire))Sleep(0);}~WorkerLock(){runtime.busy.clear(std::memory_order_release);}};
inline play::PlayerState Snapshot(){WorkerLock lock;return runtime.transport.State();}
inline bool FeaturePending(){WorkerLock lock;for(const auto& actor:runtime.actors)if(features::ActorPending(actor->features))return true;return features::CameraPending(runtime.cameraFeatures);}
inline bool NativePending(){for(const auto& actor:runtime.actors)if(actor->nativePrepared&&!v20_shared_native_physics::Restored(actor->native))return true;return false;}
inline void RevokeTargets(){for(auto& actor:runtime.actors)native_target_publication::Revoke(actor->authority);}
inline void CheckTimeLimit(){auto& r=runtime;const auto started=r.actualPlayStart.load();if(!started||r.closing.load())return;const auto state=Snapshot();
    if(state.time_limit_seconds>0&&double(GetTickCount64()-started)/1000.0>=state.time_limit_seconds){r.deadlineExpired.store(true);r.writing.store(false);r.closing.store(true);}}
inline const char* ModeName(play::PlayerMode mode){switch(mode){case play::PlayerMode::Empty:return "empty";case play::PlayerMode::Stopped:return "stopped";
    case play::PlayerMode::Playing:return "playing";case play::PlayerMode::Paused:return "paused";case play::PlayerMode::Ended:return "ended";}return "empty";}
inline bool Finished() noexcept {return runtime.closing.load();}
inline Json Status(){auto& r=runtime;WorkerLock lock;if(r.payloadReleased)return r.terminalStatus;const auto state=r.transport.State();const auto& hub=v20_shared_native_physics::hub;
    Json members=Json::array();uint64_t frames=0,writes=0,nativeFrames=0;size_t borrowed=0,created=0;uint32_t nativeError=0;bool allObserved=!r.actors.empty(),face=false,allPhysics=true,anyNative=false,actorPending=false;
    for(const auto& entry:r.actors){const auto& a=*entry;const auto count=a.nativePhysics?a.native.frames.load():a.features.frames;const auto writeCount=a.nativePhysics?a.writes.load():a.features.writes;
        frames+=count;writes+=writeCount;face=face||a.features.face_enabled;allPhysics=allPhysics&&a.features.physics_available;anyNative=anyNative||a.nativePhysics;allObserved=allObserved&&count>0;
        const bool pending=features::ActorPending(a.features)||(a.nativePrepared&&!v20_shared_native_physics::Restored(a.native));actorPending=actorPending||pending;
        if(a.ownership==native_animation_target::OwnerPolicy::SceneTeamMember)++borrowed;else ++created;
        Json actor={{"slot",a.slot},{"target_slot",a.slot},{"ownership",a.ownership==native_animation_target::OwnerPolicy::SceneTeamMember?"borrowed":"created"},
            {"character_display_name",a.config.value("character_display_name",a.config.value("character",std::string()))},
            {"frames",count},{"writes",writeCount},{"native_physics",a.nativePhysics},{"physics_enabled",a.physicsEnabled},{"physics_available",a.features.physics_available},
            {"expression_enabled",a.features.face_enabled},{"restoration_complete",!pending},{"pose_pending_restore",pending},
            {"placement",{{"offset_x_cm",a.placement.x},{"offset_y_cm",a.placement.y},{"height_cm",a.placement.height},{"yaw_degrees",a.placement.yaw_degrees},{"in_place",a.placement.in_place}}}};
        if(a.nativePrepared){v20_shared_native_physics::Stats(a.native,actor);nativeFrames+=actor.value("native_input_frames",uint64_t(0));if(a.native.error.load())nativeError=a.native.error.load();}members.push_back(std::move(actor));
    }
    const bool initialPoseReady=allObserved&&(!r.cameraFeatures.available||r.cameraFeatures.applied_frames.load()!=0);
    Json value={{"schema",3},{"actor_count",r.actors.size()},{"borrowed_count",borrowed},{"created_count",created},{"actors",members},{"members",members},{"all_actors_observed",allObserved},{"body_only",!face},
        {"initialized",r.initialized},{"ready",Ready()&&(!r.startPaused||initialPoseReady)},{"start_paused",r.startPaused},{"initial_pose_ready",initialPoseReady},{"failed",Failed()},{"error_code",r.error.load()?r.error.load():nativeError},
        {"closing",r.closing.load()},{"writing",r.writing.load()&&!r.closing.load()},{"write_gate_closed",!r.writing.load()||r.closing.load()},
        {"restoration_complete",r.closed.load()&&!actorPending&&!features::CameraPending(r.cameraFeatures)},
        {"hooks_restored",!r.hook.changed&&!r.hook.protection_pending&&!r.cameraFeatures.hook.changed&&!r.cameraFeatures.hook.protection_pending&&!hub.converter_patch.changed&&!hub.group_patch.changed&&!hub.converter_patch.protection_pending&&!hub.group_patch.protection_pending},
        {"callbacks_quiescent",!r.active.load()&&!r.work.load()&&!r.observing.load()&&!r.cameraFeatures.callbacks.load()&&!r.cameraFeatures.work.load()&&!r.cameraFeatures.observing.load()&&!hub.active.load()&&!hub.group_active.load()},
        {"session_epoch",r.epoch},{"clip_epoch",r.epoch},{"frames",frames},{"body_frames",frames},{"writes",writes},{"duration_ms",r.durationMs},
        {"seconds",state.seconds},{"duration_seconds",state.duration_seconds},{"speed",state.speed},{"loop",state.loop},{"mode",r.closed.load()?"closed":ModeName(state.mode)},
        {"last_sequence",state.last_sequence},{"revision",state.revision},{"restore_requested",state.restore_requested||r.closing.load()},{"session_active",r.writing.load()&&!r.closing.load()},
        {"time_limit_seconds",state.time_limit_seconds},{"session_elapsed_seconds",state.session_elapsed_seconds},{"time_limit_reached",state.time_limit_reached||r.deadlineExpired.load()},
        {"camera_mode",state.camera_mode},{"camera_anchor_slot",r.cameraAnchor?r.cameraAnchor->slot:0},{"camera_available",r.cameraFeatures.available},{"camera_clip_available",r.cameraFeatures.clip_available},{"camera_failed",r.cameraFeatures.failed.load()},
        {"camera_stage_reference_used",r.cameraFeatures.stage.present},{"camera_stage_floor_used",r.cameraFeatures.stage.present},
        {"formation_height_correction_cm",r.formationHeightCorrection.load()},{"ground_height_cm",r.formationHeightCorrection.load()},
        {"camera_anchor_coordinate_frame","world_local"},
        {"borrowed_stage_maintenance",r.maintenanceEnabled.load()&&borrowed!=0},{"scene_maintenance",r.maintenanceEnabled.load()},
        {"native_physics",anyNative},{"body_backend","per_actor"},{"native_input_frames",nativeFrames},{"native_input_error",nativeError},{"physics_available",allPhysics},{"expression_enabled",face},
        {"last_control_code",r.lastControlCode.load()},{"rejected_commands",r.rejectedCommands.load()},{"play_started_tick_ms",r.playStart.load()},{"actual_play_started_tick_ms",r.actualPlayStart.load()},{"body_elapsed_ms",r.playStart.load()?GetTickCount64()-r.playStart.load():0}};
    if(!members.empty()){value["placement"]=members[0]["placement"];value["physics_enabled"]=members[0]["physics_enabled"];}
    play::camera_diagnostics::Snapshot camera;
    if(r.cameraFeatures.diagnostics.TryRead(camera)&&camera.anchor_captured){
        value["camera_anchor_position"]=camera.anchor;
        value["camera_source_to_ue_xyzw"]=camera.source_to_ue;
        value["camera_source_unit_scale"]=camera.source_unit_scale;}
    return value;
}
inline bool CloseAndRestore(uint64_t deadline) noexcept {auto& r=runtime;r.writing.store(false);r.closing.store(true);r.nativeInstalled.store(false);
    {WorkerLock lock;auto state=CameraState(r.transport.State());state.restore_requested=true;features::PublishCamera(r.cameraFeatures,state,false);}
    for(auto& actor:r.actors)if(actor->nativePrepared)v20_shared_native_physics::CloseAdmission(actor->native);
    while((FeaturePending()||NativePending())&&GetTickCount64()<deadline)Sleep(10);if(FeaturePending()||NativePending())return false;
    // Keep borrowed actors visible/evaluable until their input and expression
    // ownership has drained. Keep non-performing player visibility leased until
    // that point too. Disable maintenance before managed Stop restores display
    // flags and the borrowed actors' original transform and tick policy.
    r.maintenanceEnabled.store(false,std::memory_order_release);
    while(!features::UninstallCamera(r.cameraFeatures)&&GetTickCount64()<deadline)Sleep(10);
    if(r.cameraFeatures.hook.changed||r.cameraFeatures.hook.protection_pending||r.cameraFeatures.observing.load()||
        r.cameraFeatures.work.load()||features::CameraPending(r.cameraFeatures))return false;
    bool registered=false;for(const auto& actor:r.actors)registered=registered||actor->nativeRegistered;
    if(registered){Json report;if(!v20_shared_native_physics::Uninstall(report))return false;for(auto& actor:r.actors)if(actor->nativeRegistered){if(!v20_shared_native_physics::UnregisterActor(actor->native))return false;actor->nativeRegistered=false;}}
    while(r.work.load()&&GetTickCount64()<deadline)Sleep(10);if(r.work.load())return false;r.observing.store(false);
    if(r.hook.changed||r.hook.protection_pending){if(vmt::Restore(r.hook).code!=vmt::Code::Ok)return false;}
    while((r.active.load()||r.work.load())&&GetTickCount64()<deadline)Sleep(10);
    const bool complete=!r.hook.changed&&!r.hook.protection_pending&&!r.active.load()&&!r.work.load();r.closed.store(complete);return complete;
}
// Called only after native and managed recovery have completed and the final
// report has been materialized. Keep the pinned forwarding objects/originals:
// a thread can still hold an old detour address after its slot was restored.
inline bool ReleaseRetiredPayload(Json& terminal){auto& r=runtime;WorkerLock lock;auto& hub=v20_shared_native_physics::hub;
    if(r.payloadReleased){terminal=r.terminalStatus;return true;}
    if(!terminal.value("cleanup_complete",false)||!terminal.value("managed_owner_retirement_checked",false))return false;
    if(!r.closed.load()||r.observing.load()||r.writing.load()||r.work.load()||r.maintenanceEnabled.load()||
        r.hook.changed||r.hook.protection_pending||r.cameraFeatures.observing.load()||r.cameraFeatures.work.load()||
        r.cameraFeatures.hook.changed||r.cameraFeatures.hook.protection_pending||features::CameraPending(r.cameraFeatures)||
        hub.observing.load()||hub.group_observing.load()||hub.dispatch_active.load()||
        hub.converter_patch.changed||hub.converter_patch.protection_pending||hub.group_patch.changed||hub.group_patch.protection_pending)return false;
    for(const auto& actor:r.actors)if(actor->nativeRegistered||features::ActorPending(actor->features)||
        (actor->nativePrepared&&!v20_shared_native_physics::Restored(actor->native)))return false;
    std::lock_guard<std::mutex> registryLock(hub.registry.operations);
    if(hub.registry.count.load()||hub.registry.frozen.load())return false;
    terminal["retired_payload_released"]=true;r.terminalStatus=terminal;
    r.cameraAnchor=nullptr;r.maintainScene=nullptr;r.maintainUser=nullptr;
    decltype(r.bySlot){}.swap(r.bySlot);decltype(r.byMesh){}.swap(r.byMesh);decltype(r.actors){}.swap(r.actors);
    decltype(hub.registry.actors){}.swap(hub.registry.actors);
    auto& camera=r.cameraFeatures;
    decltype(camera.anchor_meshes){}.swap(camera.anchor_meshes);decltype(camera.anchor_identities){}.swap(camera.anchor_identities);
    decltype(camera.anchor_clip_starts){}.swap(camera.anchor_clip_starts);decltype(camera.clip.source_keys){}.swap(camera.clip.source_keys);
    r.payloadReleased=true;return true;
}
} // namespace owned_mmd_body
