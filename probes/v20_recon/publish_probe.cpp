// Current MMD player at the animation publication boundary.
// One shared-slot backend; all mutations retain per-resource restoration.
#include "shared_readonly.h"
#include "publish_observer.h"
#include "vmt_slot_patch.h"
#include "../../engine_ue4/pose_subtree_rotation.h"
#include "../../engine_ue4/pose_full_body.h"
#include "../../playback/vmd_local_clip.h"
#include "../../playback/player_transport.h"
#include "../../playback/ww_mmd_placement.h"
#include "../../playback/native_kawaii_pose.h"
#include "../../playback/ww_physics_profile.h"
#include "../../playback/ww_chain_physics_profile.h"
#include "../../playback/ww_prop_visibility.h"
#include "../../playback/ww_face_runtime.h"
#include "../../playback/ww_face_clip.h"
#include "../../playback/ww_camera_clip.h"
#include "../../playback/ww_camera_runtime.h"
#include "../../playback/ww_world_snapshot.h"
#include "unified_player_features.h"
#include <atomic>
#include <fstream>
#pragma comment(lib,"user32.lib")
namespace observer=v20_publish_observer;
namespace subtree=wwp_ue::subtree_rotation;
static HMODULE ownModule=nullptr;static volatile LONG started=0;
static std::atomic<bool> safeToUnload{true};
static void(*originalPublish)(void*)=nullptr;
static observer::TargetIdentity targetIdentity;
static uint64_t targetItem=0;static std::atomic<bool> observing{false};
static std::atomic<unsigned> active{0};
static std::atomic<bool> poseWriting{false};
static std::atomic<unsigned> poseWrites{0},poseWriteFailures{0};
static uint64_t controllerAddress=0;
static uint64_t expectedMeshAsset=0,expectedSkeletonAsset=0;
static uint32_t runtimeBoneCount=0,runtimeExpressionCount=0;
static std::atomic<DWORD> poseThread{0};static std::atomic_flag poseBusy=ATOMIC_FLAG_INIT;
// Only the selected target's protected pose work, not shared-slot forwarding.
static std::atomic<unsigned> poseWorkActive{0};
namespace fullbody=wwp_ue::full_body;
namespace playback=wwp_playback;
static bool mmdInitialized=false;
static playback::local_clip::Clip mmdClip;
static fullbody::Topology mmdTopology;
static fullbody::Request mmdRequest;
static std::array<uint8_t,1024> mmdCanonicalMask{};
static std::array<std::array<float,3>,1024> mmdCanonicalPositions{};
static std::array<uint8_t,1024> mmdCanonicalRotationMask{};
static std::array<std::array<float,4>,1024> mmdCanonicalRotations{};
static playback::Transport mmdTransport;
static playback::SpscCommandQueue<64> mmdCommands;
static std::atomic<unsigned> mmdFrames{0},mmdPlanFailures{0},mmdLastPlanCode{0},mmdLastPlanBone{UINT32_MAX};
static std::atomic<unsigned> mmdStopReason{0};
static std::atomic<uint64_t> mmdStateSequence{0},mmdStateRevision{0};
static std::atomic<double> mmdStateSeconds{0},mmdStateSpeed{1};
static std::atomic<double> mmdStateTimeLimit{0},mmdStateElapsed{0};
static std::atomic<bool> mmdStateLimitReached{false},mmdSessionEnded{false};
static std::atomic<unsigned> mmdStateMode{0};
static std::atomic<bool> mmdStateLoop{false},mmdStateRestore{false};
static std::atomic<bool> mmdStatePhysics{false};
static std::atomic<double> mmdStatePlacementX{0},mmdStatePlacementY{0},mmdStatePlacementHeight{0},mmdStatePlacementYaw{0};
static std::atomic<bool> mmdStatePlacementInPlace{false};
// Native pose input has its own evaluation-thread scratch and a coherent,
// atomic-only transport snapshot. Engine object APIs stay on publication.
static bool mmdNativePhysics=false;
static uint64_t mmdClipEpoch=0;
static std::atomic<uint64_t> mmdExportVersion{0};
static std::atomic<bool> mmdNativeFeed{false},mmdNativePending{false};
static std::atomic<unsigned> mmdNativeForcePending{0};
static std::atomic<unsigned> mmdNativeError{0},mmdNativeFrames{0};
static void RecordPlaybackFault(unsigned reason) noexcept {
    unsigned empty=0;mmdStopReason.compare_exchange_strong(empty,reason);
    mmdNativeFeed.store(false,std::memory_order_release);poseWriting.store(false,std::memory_order_release);
}
static const char* PlaybackFaultName(unsigned code) noexcept {
    static const char* names[]={"none","controlled_target_changed","prop_preflight_failed","notify_preflight_failed",
        "animation_time_preflight_failed","face_preflight_failed","native_input_failed","camera_failed",
        "body_read_failed","body_sample_failed","body_plan_failed","body_write_failed","physics_unavailable","physics_step_failed"};
    return code<sizeof(names)/sizeof(names[0])?names[code]:"runtime_failed";
}
static double mmdTimelineDuration=0;
static unified_player_features::CameraFeatures mmdSceneCamera;
static auto& mmdCameraPrepared=mmdSceneCamera.prepared;
static auto& mmdCameraAvailable=mmdSceneCamera.available;
static auto& mmdCameraVmt=mmdSceneCamera.hook;
static auto& mmdCameraObserving=mmdSceneCamera.observing;
static auto& mmdCameraPending=mmdSceneCamera.pending;
static auto& mmdCameraActive=mmdSceneCamera.callbacks;
static auto& mmdCameraWorkActive=mmdSceneCamera.work;
static auto& mmdCameraBusy=mmdSceneCamera.busy;
static auto& originalCameraUpdate=mmdSceneCamera.original;
static std::atomic<unsigned> mmdCameraMode{0};
static bool mmdPhysicsAvailable=false;
static std::string mmdPhysicsUnavailableReason;
namespace props=playback::ww_prop_visibility;
static props::Prepared mmdProps;
static props::State mmdPropState;
static std::atomic<bool> mmdPropPending{false};
static std::atomic<bool> mmdRestorePendingPublished{false};
static bool mmdPropsAvailable=false;
static void(*setSkipNotifiesNative)(void*,bool)=nullptr;
static bool mmdNotifyCaptured=false,mmdNotifyOriginal=false,mmdNotifyOurs=false;
static std::atomic<bool> mmdNotifyPending{false};
static bool mmdFreezeAnimationTime=false,mmdAnimRateCaptured=false,mmdAnimRateOurs=false;
static float mmdAnimRateOriginal=1;
static std::atomic<bool> mmdAnimRatePending{false};
static bool CallSetSkipNotifies(uint64_t mesh,bool skip){
    __try{setSkipNotifiesNative((void*)mesh,skip);return true;}
    __except(EXCEPTION_EXECUTE_HANDLER){return false;}
}
static unified_player_features::ActorFeatures mmdActorFeatures;
static Go mmdFaceObjects;
static Pool mmdFacePool;
static std::atomic<bool> mmdFacePending{false};
static void(*setHiddenNative)(void*,bool,uint8_t)=nullptr;
static bool CallSetHidden(uint64_t address,bool hidden) {
    __try{setHiddenNative((void*)address,hidden,0);return true;}
    __except(EXCEPTION_EXECUTE_HANDLER){return false;}
}
static void ExportMmdState() {
    mmdExportVersion.fetch_add(1,std::memory_order_acq_rel);
    const auto& s=mmdTransport.State();mmdStateSequence.store(s.last_sequence);
    mmdStateRevision.store(s.revision);mmdStateSeconds.store(s.seconds);mmdStateSpeed.store(s.speed);
    mmdStateMode.store((unsigned)s.mode);mmdStateLoop.store(s.loop);mmdStateRestore.store(s.restore_requested);
    mmdStateTimeLimit.store(s.time_limit_seconds);mmdStateElapsed.store(s.session_elapsed_seconds);mmdStateLimitReached.store(s.time_limit_reached);
    mmdStatePhysics.store(s.physics_enabled);
    mmdCameraMode.store(s.camera_mode);
    mmdStatePlacementX.store(s.placement_x);mmdStatePlacementY.store(s.placement_y);
    mmdStatePlacementHeight.store(s.placement_height);mmdStatePlacementYaw.store(s.placement_yaw_degrees);
    mmdStatePlacementInPlace.store(s.placement_in_place);
    mmdExportVersion.fetch_add(1,std::memory_order_release);
}
static playback::placement::Settings MmdPlacement(const playback::PlayerState& state) noexcept {
    return {state.placement_x,state.placement_y,state.placement_height,state.placement_yaw_degrees,state.placement_in_place};
}
// Publication is the sole writer. Readers use only atomics and retain their
// previous snapshot when a concurrent export prevents a coherent read.
static bool ReadMmdState(playback::PlayerState* output) noexcept {
    for(unsigned attempt=0;attempt<3;++attempt){
        const uint64_t version=mmdExportVersion.load(std::memory_order_acquire);if(version&1)continue;
        playback::PlayerState state;
        state.last_sequence=mmdStateSequence.load();state.revision=mmdStateRevision.load();
        state.seconds=mmdStateSeconds.load();state.speed=mmdStateSpeed.load();
        state.mode=playback::PlayerMode(mmdStateMode.load());state.loop=mmdStateLoop.load();
        state.restore_requested=mmdStateRestore.load();state.physics_enabled=mmdStatePhysics.load();
        state.time_limit_seconds=mmdStateTimeLimit.load();state.session_elapsed_seconds=mmdStateElapsed.load();state.time_limit_reached=mmdStateLimitReached.load();
        state.camera_mode=mmdCameraMode.load();
        state.placement_x=mmdStatePlacementX.load();state.placement_y=mmdStatePlacementY.load();
        state.placement_height=mmdStatePlacementHeight.load();state.placement_yaw_degrees=mmdStatePlacementYaw.load();
        state.placement_in_place=mmdStatePlacementInPlace.load();
        if(version==mmdExportVersion.load(std::memory_order_acquire)){*output=state;return true;}
    }
    return false;
}
struct NativeReader {
    bool Read(uint64_t address,void* out,size_t size) {
        SIZE_T got=0;return ReadProcessMemory(GetCurrentProcess(),(LPCVOID)address,out,size,&got)&&got==size;
    }
    bool Identify(uint64_t object,observer::TargetIdentity* out) {
        unsigned char h[48],item[24];
        if(object!=targetIdentity.address||!Read(object,h,sizeof(h))||!Read(targetItem,item,sizeof(item))||
            At<uint64_t>(item,0)!=object||At<uint32_t>(item,16)!=targetIdentity.serial||
            At<uint32_t>(h,12)!=targetIdentity.index||At<uint32_t>(h,8)&0x18000u||At<uint32_t>(item,8)&0x30000000u)return false;
        *out={object,At<uint64_t>(h,16),At<uint64_t>(h,40),At<uint64_t>(h,0),At<uint32_t>(h,12),At<uint32_t>(item,16),true};return true;
    }
};
static bool WriteBytes(uint64_t address,const void* bytes,size_t size) {
    SIZE_T written=0;return WriteProcessMemory(GetCurrentProcess(),(LPVOID)address,bytes,size,&written)&&written==size;
}
static bool Controlled(NativeReader& read) {
    uint64_t c=0,pawn=0,ack=0,mesh=0,skeleton=0;
    return read.Read(targetIdentity.outer_address+0x2e8,&c,8)&&c==controllerAddress&&
        read.Read(c+0x2e8,&pawn,8)&&read.Read(c+0x340,&ack,8)&&pawn==targetIdentity.outer_address&&ack==pawn&&
        read.Read(targetIdentity.address+0x5d0,&mesh,8)&&mesh==expectedMeshAsset&&read.Read(mesh+0xd8,&skeleton,8)&&skeleton==expectedSkeletonAsset;
}
static bool OriginalPoseTargetAlive(NativeReader& read){
    observer::TargetIdentity current;uint64_t mesh=0;
    return read.Identify(targetIdentity.address,&current)&&read.Read(targetIdentity.address+0x5d0,&mesh,8)&&mesh==expectedMeshAsset;
}
static void CameraDetour(void* object,float delta){
    struct ActiveCall {ActiveCall(){++mmdCameraActive;}~ActiveCall(){--mmdCameraActive;}} activeCall;
    originalCameraUpdate(object,delta);
    if(!mmdCameraObserving.load(std::memory_order_acquire)||(uint64_t)object!=mmdCameraPrepared.snapshot.pcm.address)return;
    if(GetCurrentThreadId()!=mmdCameraPrepared.game_thread||mmdCameraBusy.test_and_set(std::memory_order_acquire))return;
    ++mmdCameraWorkActive;
    struct Release {~Release(){mmdCameraBusy.clear(std::memory_order_release);--mmdCameraWorkActive;}} release;
    playback::PlayerState state;if(!ReadMmdState(&state))return;
    NativeReader native;Reader reader{GetCurrentProcess()};
    const bool feed=poseWriting.load()&&Controlled(native);
    if(!unified_player_features::CameraFrame(mmdSceneCamera,reader,state,feed,(uint64_t)object))RecordPlaybackFault(7);
}
static bool UpdateNotifySuppression(NativeReader& read,bool activePose){
    uint8_t flags=0;if(!read.Read(targetIdentity.address+0xa68,&flags,1))return false;const bool current=(flags&1)!=0;
    if(activePose){
        if(!mmdNotifyCaptured){mmdNotifyOriginal=current;mmdNotifyCaptured=true;}
        if(!current){mmdNotifyOurs=true;mmdNotifyPending.store(true);return CallSetSkipNotifies(targetIdentity.address,true);}
        return true;
    }
    if(mmdNotifyCaptured&&mmdNotifyOurs&&current!=mmdNotifyOriginal&&!CallSetSkipNotifies(targetIdentity.address,mmdNotifyOriginal))return false;
    mmdNotifyCaptured=false;mmdNotifyOurs=false;mmdNotifyPending.store(false);return true;
}
static bool UpdateAnimationTime(NativeReader& read,bool activePose){
    if(!mmdFreezeAnimationTime)return true;
    float current=0;if(!read.Read(targetIdentity.address+0xca0,&current,4))return false;
    if(activePose){
        if(!mmdAnimRateCaptured){mmdAnimRateOriginal=current;mmdAnimRateCaptured=true;}
        if(current!=0){mmdAnimRateOurs=true;mmdAnimRatePending.store(true);const float zero=0;return WriteBytes(targetIdentity.address+0xca0,&zero,4);}
        return true;
    }
    if(mmdAnimRateCaptured&&mmdAnimRateOurs&&current==0&&!WriteBytes(targetIdentity.address+0xca0,&mmdAnimRateOriginal,4))return false;
    mmdAnimRateCaptured=false;mmdAnimRateOurs=false;mmdAnimRatePending.store(false);return true;
}
struct PropAdapter {
    NativeReader& reader;
    bool Read(uint64_t a,void* out,size_t n){return reader.Read(a,out,n);}
    bool Identify(uint64_t address,props::Identity* out){
        const props::Identity* expected=nullptr;
        for(unsigned i=0;i<mmdProps.count;++i)if(mmdProps.targets[i].address==address)expected=&mmdProps.targets[i];
        if(!expected)return false;unsigned char header[48],item[24];
        if(!reader.Read(address,header,48)||!reader.Read(expected->item_address,item,24))return false;
        if(At<uint64_t>(item,0)!=address||At<uint32_t>(item,16)!=expected->serial||At<uint32_t>(header,12)!=expected->index||
            (At<uint32_t>(header,8)&0x18000u)||(At<uint32_t>(item,8)&0x30000000u)){*out=*expected;out->present=false;return true;}
        *out={address,expected->item_address,At<uint64_t>(header,16),At<uint64_t>(header,0),At<uint64_t>(header,40),At<uint32_t>(header,12),At<uint32_t>(item,16),true};return true;
    }
    bool SetHidden(uint64_t address,bool hidden){return CallSetHidden(address,hidden);}
};
static bool UpdateProps(NativeReader& read,bool activePose){
    if(!mmdPropsAvailable)return true;PropAdapter adapter{read};
    const auto result=props::Update(adapter,mmdProps,activePose,&mmdPropState);
    mmdPropPending.store(mmdPropState.pending);return result.code==props::Code::Ok;
}
static bool MmdResourcesPending() {
    return mmdRestorePendingPublished.load()||mmdPropPending.load()||mmdFacePending.load()||mmdNotifyPending.load()||
        mmdAnimRatePending.load()||mmdCameraPending.load()||mmdNativePending.load()||mmdNativeForcePending.load();
}
static void MmdFrame(NativeReader& read,const observer::Sample&) {
    const double now=GetTickCount64()/1000.0;
    mmdTransport.Drain(mmdCommands,now);mmdTransport.Tick(now);ExportMmdState();
    const auto& state=mmdTransport.State();Reader reader{GetCurrentProcess()};
    if(state.restore_requested)mmdSessionEnded.store(true,std::memory_order_release);
    if(!poseWriting.load()||state.restore_requested||state.mode==playback::PlayerMode::Stopped){
        mmdNativeFeed.store(false,std::memory_order_release);
        if(!OriginalPoseTargetAlive(read))return;
        unified_player_features::ActorFrame(mmdActorFeatures,reader,state,MmdPlacement(state),false);
        mmdFacePending.store(playback::face_runtime::Pending(mmdActorFeatures.face_state));
        UpdateProps(read,false);UpdateNotifySuppression(read,false);UpdateAnimationTime(read,false);
        if(poseWriting.load()&&!unified_player_features::ActorPending(mmdActorFeatures)&&!mmdPropState.pending&&!mmdNotifyPending.load()&&
            !mmdAnimRatePending.load()&&!mmdCameraPending.load()&&!mmdNativePending.load()&&!mmdNativeForcePending.load()){
            mmdTransport.AcknowledgeRestore();ExportMmdState();}
        return;
    }
    if(!Controlled(read)){RecordPlaybackFault(1);return;}
    if(!UpdateProps(read,true)){RecordPlaybackFault(2);return;}
    if(!UpdateNotifySuppression(read,true)){RecordPlaybackFault(3);return;}
    if(!UpdateAnimationTime(read,true)){RecordPlaybackFault(4);return;}
    const bool ok=unified_player_features::ActorFrame(mmdActorFeatures,reader,state,MmdPlacement(state),true);
    mmdFacePending.store(playback::face_runtime::Pending(mmdActorFeatures.face_state));
    if(!ok){RecordPlaybackFault(10);return;}
    if(mmdNativePhysics)mmdNativeFeed.store(true,std::memory_order_release);
    else {mmdFrames.store(unsigned(mmdActorFeatures.frames));poseWrites.store(unsigned(mmdActorFeatures.writes));}
}
static void MmdPublicationFrame(NativeReader& read,const observer::Sample& sample) {
    if(!sample.accepted){if(poseWriting.load())RecordPlaybackFault(8);return;}
    DWORD thread=GetCurrentThreadId(),expected=0;poseThread.compare_exchange_strong(expected,thread);
    if(poseThread.load()!=thread||poseBusy.test_and_set(std::memory_order_acquire))return;
    poseWorkActive.fetch_add(1,std::memory_order_acq_rel);
    struct Release {~Release(){mmdRestorePendingPublished.store(unified_player_features::ActorPending(mmdActorFeatures));
        poseBusy.clear(std::memory_order_release);poseWorkActive.fetch_sub(1,std::memory_order_release);}} release;
    MmdFrame(read,sample);
}
#include "native_mmd_bridge.h"
static void PublishDetour(void* object) {
    struct ActiveCall {ActiveCall(){++active;}~ActiveCall(){--active;}} activeCall;
    originalPublish(object);
    if(!observing.load(std::memory_order_acquire)||(uint64_t)object!=targetIdentity.address)return;
    NativeReader reader;observer::Sample sample;
    observer::Observe(reader,(uint64_t)object,targetIdentity,observer::kVerifiedLayout,&sample);
    MmdPublicationFrame(reader,sample);
}
static std::wstring Directory() {
    wchar_t path[32768];DWORD n=GetModuleFileNameW(ownModule,path,32768);
    std::wstring value(path,n);return value.substr(0,value.find_last_of(L'\\'));
}
// Desktop polling may briefly hold the destination; the next report replaces it.
static void SaveReport(const std::wstring& path,const Json& value){ww_report::Write(path,value);}
static bool ReadControlFile(const wchar_t* path,playback::CommandWire* output) {
    HANDLE file=CreateFileW(path,GENERIC_READ,FILE_SHARE_READ|FILE_SHARE_WRITE|FILE_SHARE_DELETE,
        nullptr,OPEN_EXISTING,FILE_ATTRIBUTE_NORMAL,nullptr);
    if(file==INVALID_HANDLE_VALUE)return false;
    DWORD bytes=0;const bool ok=ReadFile(file,output->data(),(DWORD)output->size(),&bytes,nullptr)&&bytes==output->size();
    CloseHandle(file);return ok;
}
static bool MmdRecoveryCallbacksActive() {
    // Only target work can still create or release session-owned resources.
    // Shared-slot original calls quiesce separately after slot restoration.
    return poseWorkActive.load(std::memory_order_acquire)||mmdCameraWorkActive.load(std::memory_order_acquire)||
        mmdNativeWorkActive.load(std::memory_order_acquire)||mmdNativeGroupWorkActive.load(std::memory_order_acquire);
}
// Once publication is disabled, callbacks only restore. Wait for in-flight
// publications too before removing their restore route.
static void WaitForMmdRestoration() {
    if(!mmdInitialized)return;
    poseWriting.store(false,std::memory_order_release);mmdNativeFeed.store(false,std::memory_order_release);
    while(MmdResourcesPending()||MmdRecoveryCallbacksActive())Sleep(10);
}
static playback::local_clip::Clip ReadLocalClip(const std::wstring& path){
    return playback::local_clip::ReadFile(std::filesystem::path(path));
}
static uint64_t HexField(const Json& value){return std::stoull(value.get<std::string>(),nullptr,16);}
static void WorkerBody() {
    const std::wstring dir=Directory();Json report={{"status","error"}};
    bool dispatchExposed=false;v20_vmt_slot_patch::State vmt;
    try {
        std::ifstream config(dir+L"\\observe_target.json");Json cfg;config>>cfg;
        const uint64_t image=(uint64_t)GetModuleHandleW(nullptr);
        runtimeBoneCount=cfg.at("expected_bone_count");runtimeExpressionCount=cfg.at("expected_expression_count");
        const std::string profileId=cfg.at("character_profile_id");
        targetIdentity.address=HexField(cfg.at("object"));targetIdentity.index=cfg.at("index");
        targetIdentity.serial=cfg.at("serial");targetItem=HexField(cfg.at("item_address"));
        NativeReader read;unsigned char h[48];if(!read.Read(targetIdentity.address,h,sizeof(h)))throw std::runtime_error("target unreadable");
        targetIdentity.class_address=At<uint64_t>(h,16);targetIdentity.outer_address=At<uint64_t>(h,40);targetIdentity.vtable=At<uint64_t>(h,0);targetIdentity.verified=true;
        controllerAddress=HexField(cfg.at("controller_address"));
        mmdInitialized=true;
        Reader reader{GetCurrentProcess()};
        if(!ValidateGo(reader,image+HexField(cfg.at("gobjects_rva")),mmdFaceObjects)||!ValidatePool(reader,image+HexField(cfg.at("name_pool_rva")),mmdFacePool))throw std::runtime_error("current anchors invalid");
        if(!read.Read(targetIdentity.address+0x5d0,&expectedMeshAsset,8)||!read.Read(expectedMeshAsset+0xd8,&expectedSkeletonAsset,8))throw std::runtime_error("character mesh asset unavailable");
        {
            subtree::Manifest topologyManifest;topologyManifest.expected_count=runtimeBoneCount;
            topologyManifest.expected_names=cfg.at("expected_bone_names").get<std::vector<std::string>>();
            topologyManifest.expected_parents=cfg.at("expected_bone_parents").get<std::vector<int32_t>>();
            unsigned char arrayHead[16];if(!reader.Read(expectedMeshAsset+0x3b8,arrayHead,16))throw std::runtime_error("full reference skeleton unavailable");
            const uint64_t data=At<uint64_t>(arrayHead,0);std::vector<std::string> runtimeNames;std::vector<int32_t> runtimeParents;
            for(unsigned i=0;i<At<uint32_t>(arrayHead,8);++i){unsigned char row[16];std::string name;
                if(!reader.Read(data+i*16ull,row,16)||!Decode(reader,mmdFacePool,At<uint32_t>(row,0),name))throw std::runtime_error("full reference skeleton read failed");
                const uint32_t number=At<uint32_t>(row,8);if(number)name+="_"+std::to_string(number-1);
                runtimeNames.push_back(name);runtimeParents.push_back(At<int32_t>(row,12));}
            if(!fullbody::PrepareTopology(topologyManifest,runtimeNames,runtimeParents,&mmdTopology))throw std::runtime_error("MMD reference topology differs");
        }
        mmdNativePhysics=cfg.at("native_physics");
        mmdFreezeAnimationTime=cfg.at("freeze_native_animation_time");
        if(mmdNativePhysics)PrepareNativeInput(read,cfg,profileId,dir);
        // Native current-build bool setter preserves every other bit.
        setSkipNotifiesNative=(void(*)(void*,bool))(image+0x5009640);
        mmdClip=ReadLocalClip(dir+L"\\motion.wwlocal");
        const auto& mask=cfg.at("canonical_position_mask");const auto& positions=cfg.at("canonical_local_positions");
        const auto& rmask=cfg.at("canonical_rotation_mask");const auto& rotations=cfg.at("canonical_local_rotations");
        for(unsigned i=0;i<runtimeBoneCount;++i){mmdCanonicalMask[i]=mask.at(i).get<uint8_t>();mmdCanonicalPositions[i]=positions.at(i).get<std::array<float,3>>();
            mmdCanonicalRotationMask[i]=rmask.at(i).get<uint8_t>();mmdCanonicalRotations[i]=rotations.at(i).get<std::array<float,4>>();}
        mmdRequest.canonical_position_mask=mmdCanonicalMask.data();mmdRequest.canonical_local_positions=mmdCanonicalPositions.data();
        mmdRequest.canonical_rotation_mask=mmdCanonicalRotationMask.data();mmdRequest.canonical_local_rotations=mmdCanonicalRotations.data();
        if(mmdNativePhysics)PrepareNativeCanonicalOwnership();
        // The same catalog and camera implementation is used for all sources.
        const auto& featureConfig=cfg;
        unified_player_features::PrepareActor(mmdActorFeatures,reader,mmdFaceObjects,mmdFacePool,featureConfig,std::filesystem::path(dir),
            targetIdentity.address,expectedMeshAsset,mmdClip,mmdTopology,mmdRequest,cfg.at("clip_epoch").get<uint64_t>());
        mmdPhysicsAvailable=mmdActorFeatures.physics_available;
        mmdPhysicsUnavailableReason=mmdActorFeatures.physics_unavailable_reason;
        mmdTimelineDuration=mmdClip.duration;
        if(mmdActorFeatures.face_enabled)mmdTimelineDuration=(std::max)(mmdTimelineDuration,mmdActorFeatures.face_clip.duration);
        const auto window=HexField(cfg.at("game_window_handle"));
        unified_player_features::PrepareCamera(mmdSceneCamera,reader,mmdFaceObjects,cfg,std::filesystem::path(dir),controllerAddress,
            targetIdentity.outer_address,{targetIdentity.address},{},{},cfg.at("clip_epoch").get<uint64_t>(),GetWindowThreadProcessId((HWND)window,nullptr),window);
        if(mmdSceneCamera.clip_available)mmdTimelineDuration=(std::max)(mmdTimelineDuration,mmdSceneCamera.clip.duration);
        const uint64_t epoch=cfg.at("clip_epoch");mmdClipEpoch=epoch;mmdTransport.SetClip(epoch,mmdTimelineDuration);
        if(mmdCameraAvailable)mmdTransport.SetInitialCameraMode(cfg.at("camera_mode"));
        mmdTransport.Apply({epoch,1,playback::CommandKind::SetTimeLimit,cfg.value("duration_ms",uint64_t(0))/1000.},GetTickCount64()/1000.);
        if(cfg.at("autoplay").get<bool>())mmdCommands.Push({epoch,2,playback::CommandKind::Play,0});
        ExportMmdState();
        if(cfg.contains("weapon_targets")&&!cfg.at("weapon_targets").empty()){
            setHiddenNative=(void(*)(void*,bool,uint8_t))(image+0x5009450);
            const auto& targets=cfg.at("weapon_targets");mmdProps.count=(std::min)((unsigned)targets.size(),props::kMaxTargets);
            for(unsigned i=0;i<mmdProps.count;++i){const auto& j=targets.at(i);auto& v=mmdProps.targets[i];
                v.address=HexField(j.at("address"));v.item_address=HexField(j.at("item_address"));v.class_address=HexField(j.at("class_address"));
                v.vtable=HexField(j.at("vtable"));v.outer_address=HexField(j.at("outer_address"));v.index=j.at("index");v.serial=j.at("serial");}
            mmdPropsAvailable=true;
        }
        void* location=(void*)(image+HexField(cfg.at("publish_rva")));
        void** slot=(void**)(targetIdentity.vtable+0xab8);void* actualSlot=nullptr;
        if(!read.Read((uint64_t)slot,&actualSlot,8)||actualSlot!=location)throw std::runtime_error("post-animation vtable slot mismatch");
        originalPublish=(void(*)(void*))location;safeToUnload.store(false);observing.store(true);
        auto outcome=v20_vmt_slot_patch::Install(vmt,slot,location,(void*)&PublishDetour);dispatchExposed=outcome.pointer_changed;
        if(outcome.code!=v20_vmt_slot_patch::Code::Ok)throw std::runtime_error("vtable slot installation failed");
        if(mmdNativePhysics)InstallNativeInput(report);
        if(mmdCameraAvailable){
            void** cameraSlot=(void**)(mmdCameraPrepared.snapshot.pcm.vtable+0x790);void* current=nullptr;
            if(!read.Read((uint64_t)cameraSlot,&current,8)||current!=(void*)mmdCameraPrepared.expected_update_target)throw std::runtime_error("camera vtable slot mismatch");
            originalCameraUpdate=(void(*)(void*,float))mmdCameraPrepared.expected_update_target;mmdCameraObserving.store(true);
            if(v20_vmt_slot_patch::Install(mmdCameraVmt,cameraSlot,current,(void*)&CameraDetour).code!=v20_vmt_slot_patch::Code::Ok)throw std::runtime_error("camera vtable installation failed");
        }
        poseWriting.store(true);
        CreateDirectoryW((dir+L"\\logs").c_str(),nullptr);
        report["status"]="mmd_ready";report["physics_available"]=mmdPhysicsAvailable;report["physics_unavailable_reason"]=mmdPhysicsUnavailableReason;
        SaveReport(dir+L"\\logs\\carrier_status.json",report);SaveReport(dir+L"\\publish_result.json",report);
        uint64_t lastQueuedCommand=0;
        while(!mmdSessionEnded.load(std::memory_order_acquire)) {
            if(GetFileAttributesW((dir+L"\\stop.flag").c_str())!=INVALID_FILE_ATTRIBUTES||!poseWriting.load())break;
            Sleep(100);
            playback::CommandWire wire;playback::Command command;
            if(ReadControlFile((dir+L"\\control.dat").c_str(),&wire)&&playback::DecodeCommand(wire,&command)&&command.target_slot<=1&&command.sequence>lastQueuedCommand&&mmdCommands.Push(command))
                lastQueuedCommand=command.sequence;
            playback::PlayerState snapshot;ReadMmdState(&snapshot);
            const char* modes[]={"empty","stopped","playing","paused","ended"};
            SaveReport(dir+L"\\player_status.json",{{"clip_epoch",epoch},{"last_sequence",snapshot.last_sequence},{"revision",snapshot.revision},
                {"seconds",snapshot.seconds},{"duration_seconds",mmdTimelineDuration},{"speed",snapshot.speed},{"loop",snapshot.loop},
                {"time_limit_seconds",snapshot.time_limit_seconds},{"session_elapsed_seconds",snapshot.session_elapsed_seconds},{"time_limit_reached",snapshot.time_limit_reached},
                {"camera_mode",snapshot.camera_mode},
                {"mode",modes[unsigned(snapshot.mode)]},{"restore_requested",snapshot.restore_requested},{"frames",mmdFrames.load()},
                {"session_active",true},{"physics_available",mmdPhysicsAvailable},{"physics_enabled",snapshot.physics_enabled}});
        }
        report["status"]="finished";
        report["end_reason"]=mmdStateLimitReached.load()?"time_limit_reached":"user_stop";
    } catch(const std::exception& e) {report["status"]="error";report["reason"]=e.what();}
    WaitForMmdRestoration();
    if(mmdNativeVmt.changed||mmdNativeVmt.protection_pending||mmdNativeGroupVmt.changed||mmdNativeGroupVmt.protection_pending)StopNativeInput(report);
    mmdCameraObserving.store(false,std::memory_order_release);
    if(mmdCameraVmt.changed||mmdCameraVmt.protection_pending)v20_vmt_slot_patch::Restore(mmdCameraVmt);
    while(mmdCameraActive.load())Sleep(1);
    observing.store(false,std::memory_order_release);
    if(vmt.changed||vmt.protection_pending)v20_vmt_slot_patch::Restore(vmt);
    while(active.load())Sleep(1);
    if(const auto fault=mmdStopReason.load()){report["status"]="error";report["reason"]=PlaybackFaultName(fault);}
    const bool restored=!vmt.changed&&!vmt.protection_pending&&!mmdCameraVmt.changed&&!mmdCameraVmt.protection_pending&&
        !mmdNativeVmt.changed&&!mmdNativeVmt.protection_pending&&!mmdNativeGroupVmt.changed&&!mmdNativeGroupVmt.protection_pending&&
        !active.load()&&!mmdCameraActive.load()&&!mmdNativeActive.load()&&!mmdNativeGroupActive.load()&&
        !MmdRecoveryCallbacksActive()&&!MmdResourcesPending();
    // Shared-slot dispatch can retain a cached function pointer; keep the DLL until process exit.
    report["restoration_complete"]=restored;report["unload_safe"]=restored&&!dispatchExposed;
    report["mmd_frames"]=mmdFrames.load();report["pose_writes"]=poseWrites.load();report["pose_write_failures"]=poseWriteFailures.load();
    report["mmd_plan_failures"]=mmdPlanFailures.load();report["mmd_last_plan_code"]=mmdLastPlanCode.load();report["mmd_last_plan_bone"]=mmdLastPlanBone.load();
    if(mmdNativePhysics)nativeShared::Stats(mmdNativeActor,report);
    playback::PlayerState snapshot;ReadMmdState(&snapshot);
    SaveReport(dir+L"\\player_status.json",{{"clip_epoch",mmdClipEpoch},{"last_sequence",snapshot.last_sequence},{"revision",snapshot.revision},
        {"seconds",snapshot.seconds},{"duration_seconds",mmdTimelineDuration},{"speed",snapshot.speed},{"loop",snapshot.loop},
        {"time_limit_seconds",snapshot.time_limit_seconds},{"session_elapsed_seconds",snapshot.session_elapsed_seconds},{"time_limit_reached",snapshot.time_limit_reached},
        {"camera_mode",snapshot.camera_mode},
        {"mode","closed"},{"restore_requested",false},{"restoration_complete",restored},{"session_active",false},{"result_status",report["status"]},
        {"frames",mmdFrames.load()},{"physics_available",mmdPhysicsAvailable},{"physics_enabled",snapshot.physics_enabled}});
    safeToUnload.store(report["unload_safe"].get<bool>());
    SaveReport(dir+L"\\logs\\carrier_status.json",report);
    SaveReport(dir+L"\\publish_result.json",report);
}
static DWORD WINAPI Worker(LPVOID owned){
    try{WorkerBody();}catch(const std::exception& e){safeToUnload.store(false);OutputDebugStringA(e.what());}
    if(safeToUnload.load())FreeLibraryAndExitThread((HMODULE)owned,0);
    return 1; // retain the owned DLL reference if teardown is uncertain.
}
extern "C" __declspec(dllexport) LRESULT CALLBACK HookProc(int code,WPARAM w,LPARAM l) {
    if(code>=0&&InterlockedCompareExchange(&started,1,0)==0) {
        wchar_t exe[32768];DWORD n=GetModuleFileNameW(nullptr,exe,32768);
        if(n&&!_wcsicmp(FileName(exe).c_str(),L"Client-Win64-Shipping.exe")) {HMODULE owned=nullptr;
            if(GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS,(LPCWSTR)&HookProc,&owned)) {
                HANDLE thread=CreateThread(nullptr,0,Worker,owned,0,nullptr);if(thread)CloseHandle(thread);else FreeLibrary(owned);}}
    }
    return CallNextHookEx(nullptr,code,w,l);
}
BOOL WINAPI DllMain(HINSTANCE self,DWORD reason,LPVOID){if(reason==DLL_PROCESS_ATTACH){ownModule=self;DisableThreadLibraryCalls(self);}return TRUE;}
