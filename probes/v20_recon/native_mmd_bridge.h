#pragma once
// Single-player adapter using the same Actor/HookHub as owned and SceneTeam.
#include "shared_native_physics.h"
namespace nativeShared=v20_shared_native_physics;
static nativeShared::Actor mmdNativeActor;
static auto& mmdNativeVmt=nativeShared::hub.converter_patch;
static auto& mmdNativeGroupVmt=nativeShared::hub.group_patch;
static auto& mmdNativeActive=nativeShared::hub.active;
static auto& mmdNativeGroupActive=nativeShared::hub.group_active;
static auto& mmdNativeWorkActive=mmdNativeActor.work;
static auto& mmdNativeGroupWorkActive=mmdNativeActor.group_work;
static bool SingleNativeAuthority(void*,const nativeShared::Access& access){
    uint64_t c=0,pawn=0,ack=0;
    return access.Get(targetIdentity.outer_address+0x2e8,c)&&c==controllerAddress&&
        access.Get(c+0x2e8,pawn)&&access.Get(c+0x340,ack)&&pawn==targetIdentity.outer_address&&ack==pawn;
}
static bool SingleNativeSnapshot(void*,nativeShared::Snapshot* out){playback::PlayerState state;
    if(!ReadMmdState(&state))return false;out->epoch=mmdClipEpoch;out->seconds=state.seconds;out->placement=MmdPlacement(state);
    out->feed=mmdNativeFeed.load(std::memory_order_acquire)&&poseWriting.load(std::memory_order_acquire);
    out->stop=state.restore_requested||state.mode==playback::PlayerMode::Stopped||state.mode==playback::PlayerMode::Empty;return true;
}
static void SingleNativeFault(void*,unsigned code){
    if(code==11){++mmdPlanFailures;mmdLastPlanCode.store(mmdNativeActor.plan_code.load());mmdLastPlanBone.store(mmdNativeActor.plan_bone.load());}
    if(code==12)++poseWriteFailures;RecordPlaybackFault(6);
}
static void SingleNativeCommitted(void*,uint32_t patches){++mmdFrames;poseWrites.fetch_add(patches);}
static void PrepareNativeInput(NativeReader& read,const Json& cfg,const std::string& profile,const std::wstring& directory){
    // The shared actor needs its immutable epoch before ownership is prepared.
    mmdClipEpoch=cfg.at("clip_epoch").get<uint64_t>();
    nativeShared::PrepareBinding(mmdNativeActor,read,cfg,profile,std::filesystem::path(directory),&SingleNativeAuthority,nullptr);
    mmdNativeActor.external_feed=&mmdNativeFeed;mmdNativeActor.external_writing=&poseWriting;mmdNativeActor.external_pending=&mmdNativePending;
    mmdNativeActor.external_force_pending=&mmdNativeForcePending;mmdNativeActor.external_frames=&mmdNativeFrames;mmdNativeActor.external_error=&mmdNativeError;
    mmdNativeActor.read_snapshot=&SingleNativeSnapshot;mmdNativeActor.on_fault=&SingleNativeFault;mmdNativeActor.on_commit=&SingleNativeCommitted;
}
static void PrepareNativeCanonicalOwnership(){
    nativeShared::core::PrepareMotion(mmdNativeActor,mmdClip,mmdTopology,mmdRequest,mmdClipEpoch);
    mmdNativeActor.feed.store(true,std::memory_order_release);
}
static void InstallNativeInput(Json& report){
    if(!nativeShared::RegisterActor(mmdNativeActor))throw std::runtime_error("single native actor registry rejected target");
    nativeShared::Install(report);
}
static bool StopNativeInput(Json& report){
    nativeShared::RequestStop(mmdNativeActor);const bool stopped=nativeShared::Uninstall(report);
    if(stopped)nativeShared::UnregisterActor(mmdNativeActor);return stopped;
}
