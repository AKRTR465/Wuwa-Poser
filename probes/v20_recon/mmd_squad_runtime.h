#pragma once
// Included inside stage_host. File IO/preparation is worker-only. Game-thread callbacks own all pose state.
namespace sub=wwp_ue::subtree_rotation;
namespace body=wwp_ue::full_body;
namespace play=wwp_playback;
namespace squad=play::ww_mmd_squad;
struct SquadActor {
    uint64_t role=0;bool enabled=false,body_prepared=false,physics_enabled=false;
    play::local_clip::Clip clip;body::Topology topology;body::Request request;
    std::array<uint8_t,1024> position_mask{},rotation_mask{};
    std::array<std::array<float,3>,1024> positions{};
    std::array<std::array<float,4>,1024> rotations{};
    play::placement::Settings placement;
    uint64_t frames=0,last_engine_frame=0;
    double last_seconds=0;
    q::Identity main_anim;
    native_animation_target::AuthoritySnapshot authority;
    v20_shared_native_physics::Actor native;
    unified_player_features::ActorFeatures features;
    Json target_config;
    std::atomic<bool> native_prepared{false};
};
static std::array<SquadActor,3> dancers;
static unified_player_features::CameraFeatures dance_camera;
static unsigned dance_camera_anchor=0;
static squad::SharedTimeline dance_clock;
static play::SpscCommandQueue<64> dance_commands;
static v20_vmt_slot_patch::State dance_hook;
static void(*dance_original)(void*)=nullptr;
static std::atomic<unsigned> dance_callbacks{0};
static bool dance_initialized=false,dance_started=false,dance_autoplay=true;
static uint64_t dance_epoch=0,dance_initial_sequence=0,dance_last_wire=0;
static double dance_speed=1;static bool dance_loop=false;
static std::string dance_error;
static Json dance_config;
static std::filesystem::path dance_directory;
static std::atomic<bool> dance_native_installed{false};
static void SquadStopNative(){for(auto& m:dancers)if(m.native_prepared)v20_shared_native_physics::CloseAdmission(m.native);}
static bool SquadNeedsNative(){for(const auto& m:dancers)if(m.enabled&&m.features.native_physics)return true;return false;}
static Json SquadReadJson(const std::filesystem::path& path){std::ifstream input(path,std::ios::binary);Json value;
    if(!input||!(input>>value))throw std::runtime_error("squad native input file missing");return value;}
static play::local_clip::Clip SquadReadClip(const std::filesystem::path& path){
    std::ifstream in(path,std::ios::binary);std::array<char,play::local_clip::kHeaderBytes> header{};
    scene_team_stage::Require(in&&in.read(header.data(),header.size())&&!std::memcmp(header.data(),"WWLOCAL1",8),"squad clip unreadable");
    play::local_clip::Clip clip;std::memcpy(&clip.bone_count,header.data()+12,4);std::memcpy(&clip.frame_count,header.data()+16,4);
    std::memcpy(&clip.fps,header.data()+20,4);std::memcpy(&clip.duration,header.data()+24,8);
    clip.source_sha256.assign(header.data()+32,64);clip.skeleton_sha256.assign(header.data()+96,64);
    clip.frames.resize(size_t(clip.bone_count)*clip.frame_count);
    scene_team_stage::Require(bool(in.read(reinterpret_cast<char*>(clip.frames.data()),std::streamsize(clip.frames.size()*play::local_clip::kBoneBytes))),"squad clip payload truncated");
    return clip;
}
static void SquadPublish(void*);
static void DanceFault(const char* reason){dance_error=reason;dance_clock.FaultStop();Issue(st::Code::IdentityChanged,true);}
static bool SquadPending(){for(const auto& m:dancers)if((m.native_prepared&&!v20_shared_native_physics::Restored(m.native))||
    unified_player_features::ActorPending(m.features))return true;return unified_player_features::CameraPending(dance_camera);}
static bool SquadMemberEnabled(unsigned index){return index<dancers.size()&&dancers[index].enabled;}
static bool StageReady(){
    if(!publishing.load()||!scene_ready.load())return false;
    for(const auto& m:residents)for(unsigned k=0;k<4;++k)if(m.desired.enabled[k]&&(!m.ledger.bound||!m.latest[k].read||
        !st::MatchesDesired(st::Field(k),m.latest[k].value,m.desired.values[k])))return false;
    return true;
}
static bool DanceReady(){
    if(!StageReady())return false;
    for(const auto& actor:dancers)if(actor.enabled&&!actor.body_prepared)return false;
    return true;
}
static bool DanceRouted(play::CommandKind kind){return kind==play::CommandKind::SetPhysics||
    (kind>=play::CommandKind::SetPlacementX&&kind<=play::CommandKind::ResetPlacement);}
static void DanceApplyPlacement(SquadActor& m,const play::Command& command){
    switch(command.kind){
    case play::CommandKind::SetPhysics:m.physics_enabled=command.value!=0;break;
    case play::CommandKind::SetPlacementX:m.placement.x=command.value;break;
    case play::CommandKind::SetPlacementY:m.placement.y=command.value;break;
    case play::CommandKind::SetPlacementHeight:m.placement.height=command.value;break;
    case play::CommandKind::SetPlacementYaw:m.placement.yaw_degrees=command.value;break;
    case play::CommandKind::SetPlacementInPlace:m.placement.in_place=command.value!=0;break;
    case play::CommandKind::ResetPlacement:m.placement={};break;
    default:break;
    }
}
static void DanceAdvance(uint64_t frame){
    if(frame==dance_clock.State().engine_frame)return;
    if(dance_clock.Advance(frame,GetTickCount64()/1000.0)!=squad::ClockCode::Ok){DanceFault("shared_clock_rejected");return;}
    if(dance_autoplay&&!dance_started&&DanceReady()){
        if(dance_clock.Apply({dance_epoch,++dance_initial_sequence,play::CommandKind::Play,0},true)!=squad::ClockCode::Ok){DanceFault("ready_barrier_rejected");return;}
        dance_started=true;
    }
    play::Command command;
    while(dance_commands.Pop(&command)){
        if(DanceRouted(command.kind)){
            if(command.target_slot&&(!SquadMemberEnabled(command.target_slot-1)))continue;
            if(dance_clock.AcceptRoutedCommand(command)==squad::ClockCode::Ok)
                for(unsigned i=0;i<3;++i)if(dancers[i].enabled&&(!command.target_slot||command.target_slot==i+1))DanceApplyPlacement(dancers[i],command);
            continue;
        }
        const auto code=dance_clock.Apply(command,DanceReady());
        if(code==squad::ClockCode::Ok&&command.kind==play::CommandKind::Play)dance_started=true;
        if(code==squad::ClockCode::Ok&&command.kind==play::CommandKind::Stop)publishing.store(false);
    }
    if(dance_clock.State().player.restore_requested)publishing.store(false);
}
// Publication only coordinates the shared timeline. Each member supplies its
// body in its own evaluation callback.
static void DanceFrame(Reader& r,unsigned index){
    auto& m=dancers[index];if(!m.features.prepared)return;
    if(m.native_prepared&&m.native.error.load()){
        // Native errors remain latched. Drain face/direct ownership on every
        // subsequent publication instead of returning before restoration.
        const bool restored=unified_player_features::RestoreActorAfterFault(m.features,r,dance_clock.State().player,m.placement,[&m]{
            DanceFault("native_member_input_failed");v20_shared_native_physics::RequestStop(m.native);});
        if(!restored)DanceFault("actor_feature_restore_failed");
        return;
    }
    if(m.native_prepared)m.body_prepared=m.native.ready.load(std::memory_order_acquire);
    if(!publishing.load()||fault.load()){
        if(!unified_player_features::ActorFrame(m.features,r,dance_clock.State().player,m.placement,false))DanceFault("actor_feature_restore_failed");
        if(m.native_prepared){if(residency_started_tick.load()||fault.load())v20_shared_native_physics::RequestStop(m.native);
            else v20_shared_native_physics::Publish(m.native,0,m.placement,false);}
        return;}
    const uint64_t frame=scene_team_stage::EngineFrame(r,image);DanceAdvance(frame);
    auto state=dance_clock.State().player;state.physics_enabled=m.physics_enabled;
    const bool feed=StageReady()&&dance_started&&!state.restore_requested&&state.mode!=play::PlayerMode::Stopped;
    if(!unified_player_features::ActorFrame(m.features,r,state,m.placement,feed)){DanceFault("actor_feature_frame_failed");return;}
    if(m.native_prepared)v20_shared_native_physics::Publish(m.native,state.seconds,m.placement,feed);
    m.frames=m.native_prepared?m.native.frames.load():m.features.frames;m.last_engine_frame=frame;m.last_seconds=state.seconds;
}
static void SquadPublish(void* object){
    struct Count{Count(){++dance_callbacks;}~Count(){--dance_callbacks;}} count;
    dance_original(object); // Always forward once, outside our mutation lock.
    if(!observing.load()||GetCurrentThreadId()!=window_thread||busy.test_and_set(std::memory_order_acquire))return;
    UnlockBusy unlock;
    if(!dance_initialized)return;
    unsigned index=3;for(unsigned i=0;i<3;++i)if(dancers[i].enabled&&scene_members[i].mesh.object==uint64_t(object)){index=i;break;}
    if(index==3)return;
    try{Reader r{GetCurrentProcess()};DanceFrame(r,index);pending.store(LedgerPending());}
    catch(const std::exception& e){DanceFault(e.what());}
}
static void SquadInitialize(Reader& reader,const Json& snapshot){
    std::array<double,3> durations{};uint8_t enabled_mask=0;
    for(unsigned i=0;i<3;++i){auto& m=dancers[i];if(!m.enabled)continue;
        const auto& native=scene_members[i];const auto& row=snapshot.at("members").at(i);enabled_mask|=uint8_t(1u<<i);
        auto names=row.at("reference_bone_names").get<std::vector<std::string>>();auto parents=row.at("reference_bone_parents").get<std::vector<int32_t>>();
        const auto& config=dance_config.at("members").at(i);
        sub::Manifest manifest;manifest.expected_count=m.clip.bone_count;
        manifest.expected_names=config.at("bone_names").get<std::vector<std::string>>();manifest.expected_parents=config.at("parents").get<std::vector<int32_t>>();
        scene_team_stage::Require(body::PrepareTopology(manifest,names,parents,&m.topology),"squad reference topology mismatch");
        uint64_t main=0;scene_team_stage::Require(reader.Get(native.mesh.object+0x910,main)&&q::ReadStable(reader,objects,main,&m.main_anim),"squad main animation instance unavailable");
        unified_player_features::PrepareActor(m.features,reader,objects,pool,config,dance_directory,native.mesh.object,native.asset.object,
            m.clip,m.topology,m.request,dance_epoch);
        m.body_prepared=!m.features.native_physics;m.physics_enabled=config.value("physics_enabled",false);
        if(m.features.native_physics){const auto& profile=config.at("native_target_profile");
            Json seed={{"character_profile_id",profile.at("profile_id")},{"character_profile_sha256",config.at("character_profile_sha256")},
            {"expected_bone_count",m.clip.bone_count},{"expected_bone_names",names},{"expected_bone_parents",parents},
            {"expected_skeleton_sha256",profile.at("skeleton_sha256")},{"expected_mesh_asset_names",profile.at("mesh_asset_names")},
            {"expected_mesh_asset_name",config.at("mesh_asset_name")},{"expected_skeleton_asset_name",profile.at("skeleton_asset_name")},
            {"game_window_handle",dance_config.at("game_window_handle")}};
            m.target_config=native_target_publication::Export(reader,objects,pool,seed,native.actor,native.mesh,native.asset,native.skeleton,m.main_anim,m.authority,
                native_animation_target::OwnerPolicy::SceneTeamMember,dance_epoch);}
        durations[i]=m.clip.duration;
        if(m.features.face_enabled)durations[i]=(std::max)(durations[i],m.features.face_clip.duration);
    }
    for(unsigned i=0;i<3;++i)if(dancers[i].enabled){dance_camera_anchor=i;break;}
    unified_player_features::PrepareCamera(dance_camera,reader,objects,dance_config,dance_directory,controller.object,active_actor.object,
        {scene_members[dance_camera_anchor].mesh.object},{},{},dance_epoch,window_thread,q::Rva(dance_config.at("game_window_handle")));
    if(dance_camera.clip_available)durations[dance_camera_anchor]=(std::max)(durations[dance_camera_anchor],dance_camera.clip.duration);
    dance_clock.Configure(dance_epoch,durations,enabled_mask);
    dance_clock.Apply({dance_epoch,++dance_initial_sequence,play::CommandKind::SetSpeed,dance_speed},true);
    dance_clock.Apply({dance_epoch,++dance_initial_sequence,play::CommandKind::SetLoop,dance_loop?1.:0.},true);
    dance_clock.Apply({dance_epoch,++dance_initial_sequence,play::CommandKind::SetCameraMode,double(dance_config.value("camera_mode",0))},true);
    dance_clock.Apply({dance_epoch,++dance_initial_sequence,play::CommandKind::SetTimeLimit,dance_config.value("duration_ms",uint64_t(0))/1000.},true);
    dance_initialized=true;
}
static void SquadPrepareTracks(const std::filesystem::path& dir){
    bool any_enabled=false;
    for(unsigned i=0;i<3;++i){auto& m=dancers[i];const auto& source=dance_config.at("members").at(i);
        m.role=source.at("role_id");m.enabled=source.at("enabled");
        any_enabled=any_enabled||m.enabled;
        if(!m.enabled)continue;
        m.clip=SquadReadClip(dir/std::filesystem::u8path(source.at("local_clip_file").get<std::string>()));
        const auto& pm=source.at("canonical_position_mask");const auto& p=source.at("canonical_local_positions");
        const auto& rm=source.at("canonical_rotation_mask");const auto& qvalue=source.at("canonical_local_rotations");
        for(unsigned k=0;k<m.clip.bone_count;++k){m.position_mask[k]=uint8_t(pm.at(k).get<unsigned>());m.rotation_mask[k]=uint8_t(rm.at(k).get<unsigned>());
            m.positions[k]=p.at(k).get<std::array<float,3>>();m.rotations[k]=qvalue.at(k).get<std::array<float,4>>();}
        m.request.canonical_position_mask=m.position_mask.data();
        m.request.canonical_local_positions=m.positions.data();m.request.canonical_rotation_mask=m.rotation_mask.data();m.request.canonical_local_rotations=m.rotations.data();
        const auto& placement=source.at("placement");m.placement={placement.at("offset_x_cm"),placement.at("offset_y_cm"),placement.at("height_cm"),placement.at("yaw_degrees"),placement.at("in_place")};
    }
    scene_team_stage::Require(any_enabled,"squad requires at least one enabled body track");
}
static bool SquadNativeAuthority(void* user,const v20_shared_native_physics::Access& read){
    const auto& member=scene_members[uint32_t(reinterpret_cast<uintptr_t>(user))];uint64_t mesh=0,root=0;
    return read.Get(member.actor.object+0x310,mesh)&&mesh==member.mesh.object&&read.Get(member.actor.object+0x1a0,root)&&root==member.root.object;
}
static void SquadNativeFault(void*,unsigned code){unsigned empty=0;first_fault.compare_exchange_strong(empty,code);
    publishing.store(false,std::memory_order_release);fault.store(true,std::memory_order_release);}
static void SquadExportTargets(){for(auto& m:dancers)if(m.enabled&&m.features.native_physics){
    const auto directory=dance_directory/L"native_targets"/std::to_string(m.role);std::filesystem::create_directories(directory);
    ww_report::Save(directory/L"native_target.json",m.target_config);
}}
static void SquadPrepareNative(){
    Reader reader{GetCurrentProcess()};
    std::array<Json,3> configs;
    for(unsigned i=0;i<3;++i)if(dancers[i].enabled&&dancers[i].features.native_physics)configs[i]=SquadReadJson(dance_directory/L"native_inputs"/std::to_string(dancers[i].role)/L"native_playback.json");
    while(busy.test_and_set(std::memory_order_acquire))Sleep(1);
    UnlockBusy unlock;
    for(unsigned i=0;i<3;++i){auto& m=dancers[i];if(!m.enabled||!m.features.native_physics)continue;
        const auto& cfg=configs[i];
        v20_shared_native_physics::PrepareFromConfig(m.native,reader,cfg,cfg.at("character_profile_id"),dance_directory/L"native_inputs"/std::to_string(m.role),
            m.clip,m.topology,m.request,dance_epoch,&SquadNativeAuthority,reinterpret_cast<void*>(uintptr_t(i)));
        m.native.external_writing=&publishing;m.native.on_fault=&SquadNativeFault;
        scene_team_stage::Require(v20_shared_native_physics::RegisterActor(m.native),"squad native registration rejected");m.native_prepared=true;
    }
    Json report;v20_shared_native_physics::Install(report);dance_native_installed=true;
    ww_report::Save(dance_directory/L"native_install.json",report);
}
static void SquadPrepare(const Json& cfg,const std::filesystem::path& dir){
    dance_config=cfg.at("mmd_squad");dance_directory=dir;dance_epoch=dance_config.at("clip_epoch");
    dance_config["game_window_handle"]=cfg.at("game_window_handle");
    dance_config["duration_ms"]=dance_config.value("duration_ms",cfg.value("residency_duration_ms",uint64_t(0)));
    for(auto& row:dance_config.at("members")){row["native_physics"]=dance_config.value("native_physics",true);
        row["physics_enabled"]=dance_config.value("physics_enabled",false);}
    for(auto& row:dance_config.at("members"))if(row.at("enabled").get<bool>())row["native_target_profile"]=SquadReadJson(dir/std::filesystem::u8path(row.at("character_profile_file").get<std::string>()));
    dance_autoplay=dance_config.at("autoplay");dance_speed=dance_config.at("speed");dance_loop=dance_config.at("loop");
    SquadPrepareTracks(dir);
}
static void SquadInstall(){
    dance_original=(void(*)(void*))(image+0x4ff3cd0);
    const auto outcome=v20_vmt_slot_patch::Install(dance_hook,(void**)(image+0x843eb78+0xab8),(void*)dance_original,(void*)&SquadPublish);
    scene_team_stage::Require(outcome.code==v20_vmt_slot_patch::Code::Ok,"squad publication slot occupied or installation failed");
}
static void SquadCamera(Reader& r){
    if(!dance_initialized)return;
    if(publishing.load()&&!fault.load()&&StageReady())DanceAdvance(scene_team_stage::EngineFrame(r,image));
    auto state=dance_clock.State().player;const auto& placement=dancers[dance_camera_anchor].placement;
    state.placement_x=placement.x;state.placement_y=placement.y;state.placement_height=placement.height;state.placement_yaw_degrees=placement.yaw_degrees;
    const bool feed=publishing.load()&&!fault.load()&&dance_started&&!state.restore_requested;
    if(!unified_player_features::CameraFrame(dance_camera,r,state,feed,pcm.object))DanceFault("scene_camera_frame_failed");
}
static void SquadPoll(const std::filesystem::path& dir){
    std::ifstream stream(dir/L"control.dat",std::ios::binary);play::CommandWire wire{};play::Command command;
    if(stream&&stream.read((char*)wire.data(),64)&&play::DecodeCommand(wire,&command)&&command.sequence>dance_last_wire&&dance_commands.Push(command))
        dance_last_wire=command.sequence;
}
static void SquadReport(Json& j){
    j["mmd_squad_restore_complete"]=!SquadPending();j["mmd_squad_pending_restore"]=SquadPending();j["mmd_squad_members"]=Json::array();
    const bool face_enabled=std::any_of(dancers.begin(),dancers.end(),[](const SquadActor& m){return m.enabled&&m.features.face_enabled;});
    const auto& s=dance_clock.State();j["mmd_squad"]={{"initialized",dance_initialized},{"started",dance_started},{"error",dance_error},
        {"clip_epoch",s.player.clip_epoch},{"engine_frame",s.engine_frame},{"seconds",s.player.seconds},
        {"duration_seconds",s.player.duration_seconds},{"restore_requested",s.player.restore_requested},{"session_active",publishing.load()},
        {"speed",s.player.speed},{"loop",s.player.loop},{"mode",unsigned(s.player.mode)},{"last_sequence",s.player.last_sequence},
        {"time_limit_seconds",s.player.time_limit_seconds},{"session_elapsed_seconds",s.player.session_elapsed_seconds},{"time_limit_reached",s.player.time_limit_reached},
        {"body_mode",dance_config.value("physics_mode",std::string("native"))},{"camera_mode",s.player.camera_mode},{"face_enabled",face_enabled},{"native_physics_input",dance_native_installed.load()}};
    j["timeline"]=j["mmd_squad"];
    for(const auto& m:dancers){const bool owned=m.native_prepared&&!v20_shared_native_physics::Restored(m.native);
        j["mmd_squad_members"].push_back({{"role_id",m.role},{"enabled",m.enabled},{"frames",m.frames},{"writes",m.frames},
            {"restore_writes",0},{"pose_pending_restore",owned||unified_player_features::ActorPending(m.features)},
            {"restoration_complete",!owned&&!unified_player_features::ActorPending(m.features)},
            {"face_enabled",m.features.face_enabled},{"physics_available",m.features.physics_available},{"physics_enabled",m.physics_enabled},
            {"placement",{{"offset_x_cm",m.placement.x},{"offset_y_cm",m.placement.y},{"height_cm",m.placement.height},{"yaw_degrees",m.placement.yaw_degrees},{"in_place",m.placement.in_place}}},
            {"last_engine_frame",m.last_engine_frame},{"last_seconds",m.last_seconds},{"body_plan_warmup_verified",m.body_prepared}});}
}
static bool SquadCleanup(Json& j){
    for(auto& m:dancers)if(m.native_prepared)v20_shared_native_physics::CloseAdmission(m.native);
    if(dance_native_installed&&!v20_shared_native_physics::Uninstall(j))return false;
    for(auto& m:dancers)if(m.native_prepared){if(!v20_shared_native_physics::UnregisterActor(m.native))return false;
        m.native_prepared.store(false,std::memory_order_release);native_target_publication::Revoke(m.authority);}
    const auto outcome=v20_vmt_slot_patch::Restore(dance_hook);const bool quiet=dance_callbacks.load()==0;
    j["mmd_squad_hook_restore"]={{"code",v20_vmt_slot_patch::Name(outcome.code)},{"changed",dance_hook.changed},{"protection_pending",dance_hook.protection_pending}};
    j["mmd_squad_callbacks_quiescent"]=quiet;
    return !SquadPending()&&CleanupComplete(dance_hook,outcome.code,quiet);
}
