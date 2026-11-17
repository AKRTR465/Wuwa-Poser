#pragma once
#include "../generated/GameCompat.generated.h"
// Shared per-actor expression/direct-pose features and scene camera overlay.
// Preparation performs file IO and fresh catalog capture. The caller selects
// the matching skeletal publication phase for its part-gravity input.
#include "object_identity.h"
#include "publish_observer.h"
#include "face_capture_readonly.h"
#include "vmt_slot_patch.h"
#include "../../playback/player_transport.h"
#include "../../playback/camera_diagnostics.h"
#include "../../playback/ww_face_runtime.h"
#include "../../playback/ww_face_clip.h"
#include "../../playback/ww_camera_runtime.h"
#include "../../playback/ww_camera_clip.h"
#include "../../playback/ww_world_snapshot.h"
#include "../../playback/ww_mmd_placement.h"
#include "../../playback/vmd_local_clip.h"
#include "../../playback/ww_chain_physics_profile.h"
#include "../../playback/ww_part_gravity.h"
#include <algorithm>
#include <vector>
#include <atomic>

namespace unified_player_features {
namespace play=wwp_playback;
namespace body=wwp_ue::full_body;
namespace sub=wwp_ue::subtree_rotation;
namespace fr=play::face_runtime;
namespace fs=play::face_snapshot;
namespace cr=play::camera_runtime;
namespace obs=publish_observer;
using ue_memory::Require;
inline bool Write(uint64_t address,const void* source,size_t bytes){SIZE_T n=0;
    return WriteProcessMemory(GetCurrentProcess(),(void*)address,source,bytes,&n)&&n==bytes;}
inline bool MorphCall(uint64_t function,uint64_t mesh,const uint32_t* name,float weight,bool remove){
    __try{reinterpret_cast<void(*)(void*,const uint32_t*,float,bool)>(function)((void*)mesh,name,weight,remove);return true;}
    __except(EXCEPTION_EXECUTE_HANDLER){return false;}
}
struct PoseBackup {obs::ArrayHead head{};sub::FixedPlan plan{};bool valid=false;};
struct ActorFeatures {
    Go objects;Pool pool;ww_object::Identity mesh,asset;
    bool prepared=false,native_physics=true,physics_available=false,face_enabled=false,anchor_captured=false;
    std::string physics_unavailable_reason;
    uint64_t epoch=0,frames=0,writes=0,morph_function=0,physics_sequence=0;
    fr::Prepared face_prepared;fr::State face_state;fr::Scratch face_scratch;play::face::Clip face_clip;
    std::array<uint32_t,fs::kMaxMorphs> face_order{};
    std::array<float,fs::kMaxMorphs> sampled_weights{},weights{};
    const play::local_clip::Clip* clip=nullptr;body::Topology topology;body::Request request;
    body::Scratch body_scratch;body::LocalFrame body_frame;
    std::array<sub::TransformBytes,1024> pose{},secondary_pose{};
    sub::FixedPlan plan,secondary_plan,combined_plan;std::array<PoseBackup,2> backups;
    play::ww_chain::Prepared chain_profile;play::ww_chain::Runtime chain_runtime;play::ww_chain::Scratch chain_scratch;
    std::vector<play::part_gravity::Part> gravity_parts;
};
inline std::filesystem::path FeaturePath(const Json& cfg,const char* field,const std::filesystem::path& dir,const char* fallback){
    return dir/std::filesystem::u8path(cfg.value(field,std::string(fallback)));}
inline bool ActorPending(const ActorFeatures& a){return fr::Pending(a.face_state)||a.backups[0].valid||a.backups[1].valid;}
inline bool ActorAlive(ActorFeatures& a,::Reader& r){ww_object::Identity mesh,asset;uint64_t pointer=0;
    return ww_object::ReadStable(r,a.objects,a.mesh.object,&mesh)&&ww_object::Same(mesh,a.mesh)&&
        ww_object::ReadStable(r,a.objects,a.asset.object,&asset)&&ww_object::Same(asset,a.asset)&&
        r.Get(a.mesh.object+ww_game_compat::ComponentAssetOffset,pointer)&&pointer==a.asset.object;}
inline void PrepareActor(ActorFeatures& a,::Reader& r,Go& go,Pool& pool,const Json& cfg,const std::filesystem::path& directory,
    uint64_t mesh,uint64_t asset,const play::local_clip::Clip& clip,const body::Topology& topology,const body::Request& request,uint64_t epoch){
    a.objects=go;a.pool=pool;a.epoch=epoch;a.clip=&clip;a.topology=topology;a.request=request;
    Require(topology.count&&topology.count<=1024&&topology.count==clip.bone_count,"actor feature topology mismatch");
    Require(ww_object::ReadStable(r,go,mesh,&a.mesh)&&ww_object::ReadStable(r,go,asset,&a.asset),"actor feature identities unavailable");
    a.native_physics=cfg.value("native_physics",true);
    if(a.native_physics&&cfg.contains("gravity_parts")){
        const auto names=cfg.at("bone_names").get<std::vector<std::string>>();
        Require(names.size()==topology.count,"part gravity topology mismatch");
        play::part_gravity::Prepare(cfg.at("gravity_parts"),names,topology.parents.data(),a.gravity_parts);
    }
    a.face_enabled=cfg.value("expression_enabled",cfg.contains("expression_target_names"));
    if(a.face_enabled){
        a.face_clip=play::face::ReadClip(FeaturePath(cfg,"expression_clip_file",directory,"expression.wwexpr"));
        const uint32_t count=cfg.at("expected_expression_count");
        Require(count&&count<=fs::kMaxMorphs&&a.face_clip.target_count==count,"expression target count differs");
        const auto catalog=CaptureFaceCatalog(r,go,pool,mesh,asset,count,a.face_prepared.snapshot);
        const auto names=cfg.at("expression_target_names").get<std::vector<std::string>>();
        const auto mapped=cfg.at("expression_mapped_names").get<std::vector<std::string>>();
        Require(names.size()==count,"expression named catalog differs");a.face_prepared.count=count;
        for(unsigned i=0;i<count;++i){const std::string name=catalog->morphs[i].name.data();
            const auto at=std::find(names.begin(),names.end(),name);Require(at!=names.end(),"expression shape absent from live catalog");
            a.face_order[i]=uint32_t(at-names.begin());a.face_prepared.names[i]=catalog->morphs[i].identity.name;
            a.face_prepared.mapped[i]=std::find(mapped.begin(),mapped.end(),name)!=mapped.end();}
        a.morph_function=uint64_t(GetModuleHandleW(nullptr))+ww_game_compat::FunctionSetMorphTargetRva;
    }
    if(!a.native_physics){
        try{obs::Metadata metadata;Require(obs::ReadMetadata(r,mesh,metadata)&&metadata.previous_index>=0&&metadata.previous_index<2,"direct reference pose unavailable");
            const auto& head=metadata.heads[metadata.previous_index];Require(head.num==int32_t(topology.count)&&r.Read(head.data,a.pose.data(),size_t(topology.count)*ww_game_compat::TransformBytes),"direct reference pose unreadable");
            const auto names=cfg.at("bone_names").get<std::vector<std::string>>();
            const auto parents=cfg.at("bone_parents").get<std::vector<int32_t>>();
            const auto profile=play::ww_secondary::ReadCharacterProfile(FeaturePath(cfg,"bone_chains_file",directory,"bone_chains.json"),names,parents,cfg.at("physics_model_id"));
            a.chain_profile=play::ww_chain::Prepare(profile,names,a.pose.data());a.physics_available=true;
        }catch(const std::exception& e){a.physics_unavailable_reason=e.what();}
        Require(!cfg.value("physics_enabled",false)||a.physics_available,a.physics_unavailable_reason.c_str());
    }
    a.prepared=true;
}
inline bool FaceFrame(ActorFeatures& a,::Reader& r,const play::PlayerState& state,bool feed){
    if(!a.face_enabled)return true;
    face_capture_readonly_detail::Adapter reader{r,a.objects,a.pool};
    struct Writer{uint64_t fn;bool SetWeight(uint64_t m,const uint32_t* n,float v,bool remove){return MorphCall(fn,m,n,v,remove);}} writer{a.morph_function};
    if(!feed)return fr::Restore(reader,writer,&a.face_state,&a.face_scratch);
    if(!a.face_state.active&&!fr::Start(reader,a.face_prepared,&a.face_state,&a.face_scratch))return false;
    const auto count=a.face_prepared.count;
    if(!play::face::SampleWeights(a.face_clip,state.seconds,a.sampled_weights.data(),count))return false;
    for(unsigned i=0;i<count;++i)a.weights[i]=a.sampled_weights[a.face_order[i]];
    return fr::Frame(reader,writer,a.weights.data(),count,&a.face_state,&a.face_scratch);
}
inline bool RestorePoseBuffer(PoseBackup& backup,::Reader& r,const obs::ArrayHead& head){
    if(!backup.valid)return true;
    bool restored=true;
    if(head.data==backup.head.data&&head.num==backup.head.num)for(unsigned k=0;k<backup.plan.count;++k){const auto& p=backup.plan.patches[k];
        const auto address=head.data+uint64_t(p.bone)*ww_game_compat::TransformBytes;std::array<uint8_t,ww_game_compat::TransformRotationBytes> q{};std::array<uint8_t,ww_game_compat::VectorBytes> position{};
        if(!r.Read(address,q.data(),ww_game_compat::TransformRotationBytes)){restored=false;continue;}
        if(!memcmp(q.data(),p.rotated_rotation.data(),ww_game_compat::TransformRotationBytes)&&!Write(address,p.original_rotation.data(),ww_game_compat::TransformRotationBytes))restored=false;
        if(p.position_changed){if(!r.Read(address+ww_game_compat::TransformTranslationOffset,position.data(),ww_game_compat::VectorBytes))restored=false;
            else if(!memcmp(position.data(),p.rotated_position.data(),ww_game_compat::VectorBytes)&&!Write(address+ww_game_compat::TransformTranslationOffset,p.original_position.data(),ww_game_compat::VectorBytes))restored=false;}}
    backup.valid=!restored;
    return restored;
}
inline bool RestorePose(ActorFeatures& a,::Reader& r,const obs::Metadata& metadata){
    const bool first=RestorePoseBuffer(a.backups[0],r,metadata.heads[0]);
    const bool second=RestorePoseBuffer(a.backups[1],r,metadata.heads[1]);
    const bool ok=first&&second;
    if(ok)a.anchor_captured=false;return ok;
}
inline bool WritePosePlan(PoseBackup& backup,const obs::ArrayHead& head,const sub::FixedPlan& plan){
    backup.head=head;backup.plan=plan;backup.valid=plan.count!=0;
    for(unsigned i=0;i<plan.count;++i){const auto& patch=plan.patches[i];const auto address=head.data+uint64_t(patch.bone)*ww_game_compat::TransformBytes;
        if(!Write(address,patch.rotated_rotation.data(),ww_game_compat::TransformRotationBytes)||
           (patch.position_changed&&!Write(address+ww_game_compat::TransformTranslationOffset,patch.rotated_position.data(),ww_game_compat::VectorBytes)))return false;}
    return true;
}
inline void RecoverPoseInput(const PoseBackup& backup,const obs::ArrayHead& head,sub::TransformBytes* pose){
    if(!backup.valid||head.data!=backup.head.data||head.num!=backup.head.num)return;
    for(unsigned i=0;i<backup.plan.count;++i){
        const auto& patch=backup.plan.patches[i];
        auto* bone=pose[patch.bone].data();
        if(!memcmp(bone,patch.rotated_rotation.data(),patch.rotated_rotation.size()))
            memcpy(bone,patch.original_rotation.data(),patch.original_rotation.size());
        auto* position=bone+ww_game_compat::TransformTranslationOffset;
        if(patch.position_changed&&!memcmp(position,patch.rotated_position.data(),patch.rotated_position.size()))
            memcpy(position,patch.original_position.data(),patch.original_position.size());
    }
}
inline bool CommitPartPose(ActorFeatures& a,::Reader& r,const obs::ArrayHead& head,unsigned slot){
    constexpr size_t rotation=ww_game_compat::TransformRotationBytes;
    constexpr size_t position=ww_game_compat::TransformTranslationOffset;
    constexpr size_t extent=position+ww_game_compat::VectorBytes;
    auto& result=a.chain_scratch.output;
    std::copy_n(a.pose.begin(),head.num,result.begin());
    for(unsigned i=0;i<a.combined_plan.count;++i)sub::ApplyPatch(result[a.combined_plan.patches[i].bone].data(),a.combined_plan.patches[i]);
    auto& backup=a.backups[slot];
    backup.head=head;
    backup.plan.count=0;
    bool committed=true;
    for(int32_t i=0;i<head.num;++i){
        const auto* before=a.secondary_pose[i].data();
        const auto* baseline=a.pose[i].data();
        const auto* target=result[i].data();
        const auto* live=before;
        std::array<uint8_t,extent> after{};
        if(committed&&memcmp(before,target,extent)){
            const auto address=head.data+uint64_t(i)*ww_game_compat::TransformBytes;
            const bool move=memcmp(before+position,target+position,ww_game_compat::VectorBytes)!=0;
            committed=Write(address,target,move?extent:rotation);
            live=target;
            if(!committed&&r.Read(address,after.data(),after.size()))live=after.data();
        }
        if(!memcmp(baseline,live,extent))continue;
        backup.plan.patches[backup.plan.count++]=sub::MakePatch(uint32_t(i),baseline,live);
    }
    backup.valid=backup.plan.count!=0;
    return committed;
}
inline bool GravityPartFrame(ActorFeatures& a,::Reader& r,const play::PlayerState& state,bool feed,double clock,bool before_publication=false){
    if(a.gravity_parts.empty())return true;
    const bool enabled=feed&&std::any_of(a.gravity_parts.begin(),a.gravity_parts.end(),[](const auto& part){return part.strength>=0;});
    if(!enabled&&!a.backups[0].valid&&!a.backups[1].valid){
        for(auto& part:a.gravity_parts)if(part.sampled!=part.sequence){
            part.motion.reset();part.moved_bones=0;part.sampled=part.sequence;
        }
        return true;
    }
    obs::Metadata metadata;if(!obs::ReadMetadata(r,a.mesh.object,metadata))return false;
    if(!enabled){
        if(!RestorePose(a,r,metadata))return false;
        for(auto& part:a.gravity_parts){part.motion.reset();part.moved_bones=0;part.sampled=part.sequence;}
        return true;
    }
    if(metadata.previous_index<0||metadata.previous_index>1)return false;
    const auto input=before_publication?metadata.read_index:metadata.previous_index;
    if(input<0||input>1||(before_publication&&input==metadata.previous_index))return false;
    const auto slot=unsigned(input);const auto& head=metadata.heads[slot];
    const auto other=1u-slot;
    const bool aliased=metadata.heads[other].data==head.data;
    if(before_publication&&aliased)return false;
    if(head.num!=int32_t(a.topology.count)||!obs::ValidHead(head)||
        !r.Read(head.data,a.secondary_pose.data(),size_t(head.num)*ww_game_compat::TransformBytes))return false;
    std::copy_n(a.secondary_pose.begin(),head.num,a.pose.begin());
    // Reconstruct the solver input privately. The published buffer stays
    // corrected while the solver is running, including when a slot is reused.
    if(aliased)RecoverPoseInput(a.backups[other],head,a.pose.data());
    RecoverPoseInput(a.backups[slot],head,a.pose.data());
    sub::TransformBytes world;
    if(!r.Read(a.mesh.object+ww_game_compat::ComponentToWorldOffset,world.data(),world.size()))return false;
    const auto rotation=sub::detail::Rotation(world);
    const auto norm=sub::detail::Norm(rotation);
    if(!std::isfinite(norm)||norm<.9||norm>1.1)return false;
    const auto up=sub::detail::Rotate(sub::detail::Conj(sub::detail::Unit(rotation)),{{0,0,1}});
    play::ww_chain::Control control;
    control.enabled=true;control.playing=state.mode==play::PlayerMode::Playing;
    control.settle_while_paused=true;control.align_incoming_frames=true;control.gravity_strength=1;
    control.project_constraint_velocity=true;
    control.link_turn_rate=7;control.rotation_rate=9;
    control.gravity_up={up[0],up[1],up[2]};control.epoch=a.epoch;
    control.clock_seconds=clock;control.cursor_seconds=state.seconds;
    a.combined_plan.count=0;
    for(auto& part:a.gravity_parts){
        if(part.strength<0){part.motion.reset();part.moved_bones=0;continue;}
        if(!part.motion)part.motion.reset(new(std::nothrow) play::ww_chain::Runtime{});
        if(!part.motion)return false;
        control.gravity_ue=-part.strength;
        control.downward_drape=part.downward_drape&&part.strength>0;
        // The component-space capsule tests keep the solved points away from the arm.
        control.collision_priority=false;
        if(!play::ww_chain::Step(part.rig,control,a.pose.data(),a.topology.count,part.motion.get(),&a.chain_scratch,&a.secondary_plan))return false;
        part.moved_bones=a.secondary_plan.count;
        std::copy_n(a.secondary_plan.patches.begin(),a.secondary_plan.count,a.combined_plan.patches.begin()+a.combined_plan.count);
        a.combined_plan.count+=a.secondary_plan.count;
    }
    const bool committed=CommitPartPose(a,r,head,slot);
    if(aliased)a.backups[other].valid=false;
    if(!committed)return false;
    for(auto& part:a.gravity_parts)part.sampled=part.sequence;
    return true;
}
inline bool ActorFrame(ActorFeatures& a,::Reader& r,const play::PlayerState& state,const play::placement::Settings& placement,bool feed){
    // Native-physics actors use GravityPartFrame and FaceFrame directly.
    if(!a.prepared)return false;feed=feed&&!state.restore_requested&&state.mode!=play::PlayerMode::Stopped;
    if(!ActorAlive(a,r))return false;
    if(!FaceFrame(a,r,state,feed))return false;
    obs::Metadata metadata;if(!obs::ReadMetadata(r,a.mesh.object,metadata))return false;
    if(!feed)return RestorePose(a,r,metadata);
    if(metadata.previous_index<0||metadata.previous_index>1)return false;
    const auto slot=unsigned(metadata.previous_index);const auto& head=metadata.heads[slot];const auto count=a.topology.count;
    if(head.num!=int32_t(count)||!r.Read(head.data,a.pose.data(),size_t(count)*ww_game_compat::TransformBytes))return false;
    auto& backup=a.backups[slot];
    RecoverPoseInput(backup,head,a.pose.data());
    if(!a.anchor_captured){a.request.root_anchor_position=sub::detail::Position(a.pose[0]);a.request.root_anchor_rotation=sub::detail::Rotation(a.pose[0]);
        a.request.root_anchor_enabled=a.request.root_rotation_enabled=true;a.anchor_captured=true;}
    if(!play::local_clip::Sample(*a.clip,state.seconds,a.body_frame.bones.data(),count))return false;
    a.body_frame.clip_epoch=a.epoch;a.body_frame.sequence=a.frames+1;a.body_frame.time_seconds=state.seconds;a.body_frame.bone_count=count;
    body::Request placed;body::Failure failure;
    if(!play::placement::Apply(placement,a.request,a.body_frame.bones[0],&placed,&a.body_frame.bones[0])||
        !body::MakePlan(a.topology,placed,a.pose.data(),count,a.body_frame,&a.body_scratch,&a.plan,&failure))return false;
    if(state.physics_enabled&&!a.physics_available)return false;
    if(a.physics_available){
        std::copy_n(a.pose.begin(),count,a.secondary_pose.begin());
        for(unsigned i=0;i<a.plan.count;++i)sub::ApplyPatch(a.secondary_pose[a.plan.patches[i].bone].data(),a.plan.patches[i]);
        play::ww_chain::Control control;control.enabled=state.physics_enabled;control.playing=state.mode==play::PlayerMode::Playing;
        control.epoch=a.epoch;control.clock_seconds=GetTickCount64()/1000.;control.cursor_seconds=state.seconds;
        control.reset=state.last_sequence!=a.physics_sequence;a.physics_sequence=state.last_sequence;
        if(!play::ww_chain::Step(a.chain_profile,control,a.secondary_pose.data(),count,&a.chain_runtime,&a.chain_scratch,&a.secondary_plan))return false;
        if(a.secondary_plan.count){
            for(unsigned i=0;i<a.secondary_plan.count;++i)sub::ApplyPatch(a.secondary_pose[a.secondary_plan.patches[i].bone].data(),a.secondary_plan.patches[i]);
            a.combined_plan.count=0;
            for(unsigned i=0;i<count;++i){const auto patch=sub::MakePatch(i,a.pose[i].data(),a.secondary_pose[i].data());
                if(sub::Changed(patch))a.combined_plan.patches[a.combined_plan.count++]=patch;}
            a.plan=a.combined_plan;
        }
    }
    if(!WritePosePlan(backup,head,a.plan))return false;
    ++a.frames;a.writes+=a.plan.count;return true;
}

// Translation from the first staged mesh to the shared stage floor, in world
// axes. Managed code subtracts two D_/global positions; adding the result to
// the mesh's current local position produces a local camera anchor, including
// after a world-origin rebase. Absolute D_ coordinates cannot anchor the POV.
struct StageReference {Vec3 offset_from_first_mesh{};};
inline StageReference ReadStageReference(const Json& lifecycle){
    const auto offset=lifecycle.at("stage_reference_camera_offset_from_mesh").get<std::vector<float>>();
    Require(offset.size()==3&&std::all_of(offset.begin(),offset.end(),[](float v){return std::isfinite(v);}),"managed camera stage reference malformed");
    return {{offset[0],offset[1],offset[2]}};
}
struct CameraFeatures {
    bool (*on_game_frame)(void*)=nullptr;void* game_frame_user=nullptr;
    Go objects;cr::Prepared prepared;cr::State state;cr::Frame fixed,last;
    // Every stage actor anchors the camera: the follow camera tracks their live
    // centroid; the VMD orbit uses the shared stage origin.
    std::vector<uint64_t> anchor_meshes;std::vector<ww_object::Identity> anchor_identities;
    StageReference stage;
    ww_object::Identity pawn_mesh_identity;
    play::camera_clip::Clip clip;play::ww_camera::Settings settings;Quat source_basis;
    uint64_t pawn_mesh=0;float unit_scale=1;unsigned previous_mode=0;
    bool available=false,clip_available=false,fixed_captured=false,anchor_captured=false,last_ready=false;
    std::atomic<bool> pending{false},failed{false},observing{false},feed{false};
    std::atomic<unsigned> callbacks{0},work{0};std::atomic_flag busy=ATOMIC_FLAG_INIT;
    std::atomic<uint64_t> applied_frames{0};
    std::atomic<uint64_t> version{0};std::atomic<double> seconds{0},x{0},y{0},height{0},yaw{0},track_height{0};
    std::atomic<unsigned> mode{0},player_mode{0};std::atomic<bool> restore{false};
    play::PlayerState last_published;vmt_slot_patch::State hook;void(*original)(void*,float)=nullptr;
    play::camera_diagnostics::Mailbox diagnostics;
};
inline cr::Token CameraToken(const ww_object::Identity& v){return {v.object,v.item,v.cls,v.vtable,v.outer,v.index,v.serial};}
inline void PrepareCamera(CameraFeatures& c,::Reader& r,Go& go,const Json& cfg,const std::filesystem::path& directory,
    uint64_t controller,uint64_t pawn,const std::vector<uint64_t>& meshes,
    const StageReference& stage,uint64_t epoch,DWORD thread,uint64_t window){
    c.objects=go;c.anchor_meshes=meshes;Require(!c.anchor_meshes.empty(),"camera needs at least one stage actor");
    c.stage=stage;
    uint64_t pcm=0;ww_object::Identity controller_id,pawn_id,pcm_id;
    Require(ww_object::ReadStable(r,go,controller,&controller_id)&&ww_object::ReadStable(r,go,pawn,&pawn_id)&&
        r.Get(controller+ww_game_compat::ControllerCameraManagerOffset,pcm)&&ww_object::ReadStable(r,go,pcm,&pcm_id)&&r.Get(pawn+ww_game_compat::PawnMeshOffset,c.pawn_mesh),"camera current chain unavailable");
    c.anchor_identities.clear();
    for(uint64_t mesh:c.anchor_meshes){ww_object::Identity anchor;Require(ww_object::ReadStable(r,go,mesh,&anchor),"camera mesh identities unavailable");c.anchor_identities.push_back(anchor);}
    Require(ww_object::ReadStable(r,go,c.pawn_mesh,&c.pawn_mesh_identity),"camera mesh identities unavailable");
    auto& m=c.prepared.snapshot;m.controller=CameraToken(controller_id);m.pawn=CameraToken(pawn_id);m.pcm=CameraToken(pcm_id);
    m.allow_unassigned_pcm_serial=!pcm_id.serial;m.pcm_name_words=pcm_id.name;c.prepared.session=epoch;c.prepared.game_thread=thread;
    c.prepared.expected_update_target=uint64_t(GetModuleHandleW(nullptr))+ww_game_compat::FunctionUpdateCameraRva;c.available=true;
    c.clip_available=cfg.value("camera_clip_enabled",false);
    if(c.clip_available){c.clip=play::camera_clip::ReadFile(FeaturePath(cfg,"camera_clip_file",directory,"camera.wwcam"));
        const auto basis=cfg.at("camera_source_to_component_rotation_xyzw").get<std::array<float,4>>();c.source_basis={basis[0],basis[1],basis[2],basis[3]};
        c.unit_scale=cfg.at("camera_units_per_mmd_unit").get<float>();RECT rect{};
        Require(GetClientRect((HWND)window,&rect)&&rect.right>rect.left&&rect.bottom>rect.top,"camera game viewport unavailable");
        c.settings.input_fov_vertical=true;c.settings.aspect_ratio=float(rect.right-rect.left)/float(rect.bottom-rect.top);}
    Require(cfg.value("camera_mode",0)!=2||c.clip_available,"VMD camera mode requires a camera clip");
}
struct CameraReader {
    ::Reader& reader;CameraFeatures& camera;
    bool Read(uint64_t a,void* out,size_t n){return reader.Read(a,out,n);}
    bool Identify(uint64_t address,cr::Token* out){ww_object::Identity id;
        if(!ww_object::ReadStable(reader,camera.objects,address,&id))return false;*out=CameraToken(id);return true;}
};
inline bool CameraPending(const CameraFeatures& c){return c.pending.load(std::memory_order_acquire);}
// The engine's own camera values captured before this frame's overlay.
inline cr::Frame OriginalFrame(const cr::State& state){
    cr::Frame frame;
    memcpy(frame.location.data(),state.fields[0].original.data(),ww_game_compat::VectorBytes);
    memcpy(frame.rotation.data(),state.fields[1].original.data(),ww_game_compat::VectorBytes);
    memcpy(&frame.fov,state.fields[2].original.data(),ww_game_compat::FloatBytes);
    return frame;
}
inline bool CameraFrame(CameraFeatures& c,::Reader& native,const play::PlayerState& player,bool feed,uint64_t pcm){
    if(!c.available)return true;
    struct Pending{CameraFeatures& c;~Pending(){c.pending.store(cr::Pending(c.state),std::memory_order_release);}} pending{c};
    struct Diagnostic {CameraFeatures& c;~Diagnostic(){
        c.diagnostics.Publish({c.anchor_captured,{c.settings.anchor.x,c.settings.anchor.y,c.settings.anchor.z},
            {c.settings.source_to_ue.x,c.settings.source_to_ue.y,c.settings.source_to_ue.z,c.settings.source_to_ue.w},c.settings.source_unit_scale});
    }} diagnostic{c};
    CameraReader reader{native,c};struct Writer{bool Write(uint64_t a,const void* p,size_t n){return unified_player_features::Write(a,p,n);}} writer;
    uint64_t pawn=0,ack=0;const bool controlled=native.Get(c.prepared.snapshot.controller.address+ww_game_compat::ControllerPawnOffset,pawn)&&
        native.Get(c.prepared.snapshot.controller.address+ww_game_compat::ControllerAcknowledgedPawnOffset,ack)&&pawn==c.prepared.snapshot.pawn.address&&ack==pawn;
    cr::Phase phase{c.prepared.session,pcm,c.prepared.expected_update_target,GetCurrentThreadId(),true,controlled};
    if(!feed||!controlled||player.mode==play::PlayerMode::Stopped||player.restore_requested){
        const bool ok=cr::Restore(reader,writer,c.prepared,phase,&c.state);c.fixed_captured=c.anchor_captured=c.last_ready=false;c.previous_mode=0;return ok;}
    if(!cr::Begin(reader,c.prepared,phase,&c.state))return false;
    cr::Frame frame;const unsigned mode=player.camera_mode;
    if(mode==1){
        if(!c.fixed_captured||c.previous_mode!=1){c.fixed=c.last_ready?c.last:OriginalFrame(c.state);c.fixed_captured=true;}
        frame=c.fixed;
    }else{
        ww_object::Identity anchor_now,pawn_mesh_now;
        if(!ww_object::ReadStable(native,c.objects,c.pawn_mesh,&pawn_mesh_now)||!ww_object::Same(pawn_mesh_now,c.pawn_mesh_identity))return false;
        // The live mesh centroid the follow camera tracks, and the first
        // actor's mesh that carries the stage reference.
        std::array<double,3> live{};Vec3 first_mesh_position{};Quat world;float unit=1;
        for(size_t i=0;i<c.anchor_meshes.size();++i){
            if(!ww_object::ReadStable(native,c.objects,c.anchor_meshes[i],&anchor_now)||!ww_object::Same(anchor_now,c.anchor_identities[i]))return false;
            std::array<uint8_t,ww_game_compat::TransformBytes> bytes{};if(!native.Read(c.anchor_meshes[i]+ww_game_compat::ComponentToWorldOffset,bytes.data(),ww_game_compat::TransformBytes))return false;
            const auto transform=play::world_snapshot::Decode(bytes);
            for(unsigned k=0;k<3;++k)live[k]+=transform.translation[k];
            if(i==0){
                first_mesh_position={transform.translation[0],transform.translation[1],transform.translation[2]};
                world=Quat{transform.rotation[0],transform.rotation[1],transform.rotation[2],transform.rotation[3]};unit=transform.scale[0];
            }
        }
        const double inverse=1.0/double(c.anchor_meshes.size());
        for(unsigned k=0;k<3;++k)live[k]*=inverse;
        const Vec3 offset=world*Vec3{float(player.placement_x),float(player.placement_y),float(player.placement_height)};
        if(mode==0){std::array<uint8_t,ww_game_compat::TransformBytes> pawn_bytes{};if(!native.Read(c.pawn_mesh+ww_game_compat::ComponentToWorldOffset,pawn_bytes.data(),ww_game_compat::TransformBytes))return false;
            const auto pawn_transform=play::world_snapshot::Decode(pawn_bytes);frame=OriginalFrame(c.state);
            const float delta[]={float(live[0])-pawn_transform.translation[0]+offset.x,float(live[1])-pawn_transform.translation[1]+offset.y,float(live[2])-pawn_transform.translation[2]+offset.z};
            for(unsigned k=0;k<3;++k)frame.location[k]+=delta[k];
        }else if(mode==2){if(!c.clip_available)return false;
            if(!c.anchor_captured){
                // Keep the measured mesh orientation and source calibration.
                c.settings.source_to_ue=NormQ(world*c.source_basis);
                c.settings.source_unit_scale=c.unit_scale*unit;c.anchor_captured=true;}
            // Resolve on each frame so a rebased mesh and POV stay in the same
            // local coordinate space without moving the stage in global space.
            c.settings.anchor=first_mesh_position+c.stage.offset_from_first_mesh;
            auto settings=c.settings;settings.anchor=settings.anchor+offset;settings.track_height_cm=float(player.camera_track_height_cm);play::ww_camera::Frame sampled;
            if(!play::camera_clip::Sample(c.clip,player.seconds,settings,&sampled))return false;
            frame.location={sampled.location_ue.x,sampled.location_ue.y,sampled.location_ue.z};
            frame.rotation={sampled.ue_rotator_degrees.x,sampled.ue_rotator_degrees.y,sampled.ue_rotator_degrees.z};
            frame.fov=sampled.fov_degrees;
        }else return false;
    }
    if(!cr::Apply(reader,writer,c.prepared,frame,&c.state))return false;
    c.last=frame;c.last_ready=true;c.previous_mode=mode;++c.applied_frames;return true;
}
inline void PublishCamera(CameraFeatures& c,const play::PlayerState& state,bool feed){
    c.version.fetch_add(1,std::memory_order_acq_rel);c.seconds.store(state.seconds);c.mode.store(state.camera_mode);c.player_mode.store(unsigned(state.mode));
    c.restore.store(state.restore_requested);c.feed.store(feed);c.x.store(state.placement_x);c.y.store(state.placement_y);c.height.store(state.placement_height);c.yaw.store(state.placement_yaw_degrees);
    c.track_height.store(state.camera_track_height_cm);
    c.version.fetch_add(1,std::memory_order_release);
}
inline CameraFeatures* owned_camera=nullptr;
inline void CameraDetour(void* object,float delta){auto* camera=owned_camera;if(!camera)return;
    auto& c=*camera;++c.callbacks;struct Count{CameraFeatures& c;~Count(){--c.callbacks;}} count{c};c.original(object,delta);
    if(!c.observing.load()||uint64_t(object)!=c.prepared.snapshot.pcm.address||GetCurrentThreadId()!=c.prepared.game_thread||c.busy.test_and_set(std::memory_order_acquire))return;
    ++c.work;struct Work{CameraFeatures& c;~Work(){c.busy.clear(std::memory_order_release);--c.work;}} work{c};
    if(c.on_game_frame&&!c.on_game_frame(c.game_frame_user))c.failed.store(true);
    bool feed=false;auto state=c.last_published;
    for(unsigned attempt=0;attempt<3;++attempt){const auto version=c.version.load(std::memory_order_acquire);if(version&1)continue;
        state.seconds=c.seconds.load();state.camera_mode=c.mode.load();state.mode=play::PlayerMode(c.player_mode.load());state.restore_requested=c.restore.load();
        state.placement_x=c.x.load();state.placement_y=c.y.load();state.placement_height=c.height.load();state.placement_yaw_degrees=c.yaw.load();feed=c.feed.load();
        state.camera_track_height_cm=c.track_height.load();
        if(version==c.version.load(std::memory_order_acquire)){c.last_published=state;break;}state=c.last_published;feed=false;}
    ::Reader r{GetCurrentProcess()};if(!CameraFrame(c,r,state,feed&&!c.failed.load(),uint64_t(object)))c.failed.store(true);
}
inline bool InstallCamera(CameraFeatures& c){if(!c.available)return true;if(owned_camera&&owned_camera!=&c)return false;
    owned_camera=&c;c.original=(void(*)(void*,float))c.prepared.expected_update_target;c.observing.store(true);
    const auto result=vmt_slot_patch::Install(c.hook,(void**)(c.prepared.snapshot.pcm.vtable+ww_game_compat::SlotUpdateCamera),(void*)c.original,(void*)&CameraDetour);
    return result.code==vmt_slot_patch::Code::Ok;
}
inline bool UninstallCamera(CameraFeatures& c){if(!c.available)return true;c.feed.store(false);
    c.observing.store(false);if(c.work.load()||c.callbacks.load()){c.observing.store(true);return false;}
    if(CameraPending(c)){c.observing.store(true);return false;}
    const auto result=vmt_slot_patch::Restore(c.hook);
    if((result.code!=vmt_slot_patch::Code::Ok&&result.code!=vmt_slot_patch::Code::NotInstalled)||c.callbacks.load())return false;
    // The carrier stays loaded. A thread may already hold the detour address
    // after its slot is restored, so retain the original forwarding target.
    return true;
}
} // namespace unified_player_features
