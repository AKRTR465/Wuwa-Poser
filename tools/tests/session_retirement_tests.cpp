// Synthetic session state and forwarding callbacks; no hooks or game APIs.
#include "../../runtime/owned_mmd/native/n1_common.h"
#include "../../runtime/owned_mmd/native/owned_mmd_body.h"
#include <cstdlib>
#include <exception>
#include <iostream>
#include <limits>

namespace scene=owned_mmd_body;
namespace feature=unified_player_features;
namespace native=v20_shared_native_physics;
namespace play=wwp_playback;
static unsigned assertions=0,bodyCalls=0,cameraCalls=0,converterCalls=0,groupCalls=0;
static void Check(bool condition,const char* message){++assertions;if(!condition){std::cerr<<message<<'\n';std::exit(1);}}
static void Body(void*){++bodyCalls;}
static void Camera(void*,float){++cameraCalls;}
static void Converter(void*,void*){++converterCalls;}
static void Group(void*,void*){++groupCalls;}
template<class Action>static bool Rejected(Action&& action){try{action();return false;}catch(const std::runtime_error&){return true;}}

static void RunTests(){
    auto& r=scene::runtime;auto& hub=native::hub;
    auto actor=std::make_unique<scene::ActorRuntime>();auto& a=*actor;
    a.config=Json{{"character_display_name","synthetic retirement actor"}};
    a.slot=1;a.tokens[scene::Mesh].object=0x10000;a.nativePhysics=false;a.features.prepared=true;a.topology.count=2;
    a.positions.resize(2);a.rotations.resize(2);a.positionMask.resize(2);a.rotationMask.resize(2);
    scene::ValidateCanonicalPose(a);
    a.positionMask.resize(1);Check(Rejected([&]{scene::ValidateCanonicalPose(a);}),"truncated canonical arrays reject before pointer publication");a.positionMask.resize(2);
    a.rotationMask[0]=1;Check(Rejected([&]{scene::ValidateCanonicalPose(a);}),"enabled zero canonical quaternion rejects");a.rotations[0][3]=1;
    scene::ValidateCanonicalPose(a);a.positions[1][2]=std::numeric_limits<float>::infinity();
    Check(Rejected([&]{scene::ValidateCanonicalPose(a);}),"nonfinite canonical position rejects");a.positions[1][2]=0;
    a.motion.frames.resize(32768);a.features.face_clip.payload.resize(32768);
    r.actors.push_back(std::move(actor));scene::FreezeActorIndices();r.cameraAnchor=&a;
    r.cameraFeatures.anchor_meshes.resize(1024);r.cameraFeatures.anchor_identities.resize(1024);
    r.cameraFeatures.anchor_clip_starts.resize(1024);r.cameraFeatures.clip.source_keys.resize(1024);
    r.epoch=2;r.observing=true;r.transport.SetClip(2,12);scene::Start(100);scene::Advance(1,100);
    Check(r.transport.State().mode==play::PlayerMode::Playing&&r.actualPlayStart.load()!=0,"default startup still automatically plays");
    r.transport=play::Transport{};r.actualPlayStart=0;r.lastEngineFrame=0;
    r.epoch=1;r.startPaused=true;r.transport.SetClip(1,12);r.transport.PreparePausedStart();
    scene::Start(100);scene::Advance(1,100);scene::Advance(2,1000);scene::CheckTimeLimit();
    Check(r.transport.State().mode==play::PlayerMode::Paused&&r.transport.State().seconds==0&&r.actualPlayStart.load()==0&&!r.closing.load(),"paused startup holds time zero without a worker deadline");
    Check(!scene::Status().at("ready").get<bool>(),"paused startup waits for actual actor pose publication");
    a.features.frames=1;r.cameraFeatures.available=true;
    Check(!scene::Status().at("ready").get<bool>(),"paused startup waits for the camera's first applied frame too");
    r.cameraFeatures.applied_frames=1;
    Check(scene::Status().at("ready").get<bool>(),"first pose and camera publication release recording readiness");
    r.commands.Push({1,100,play::CommandKind::Play,0});scene::Advance(3,1000);scene::Advance(4,1001);
    Check(r.transport.State().mode==play::PlayerMode::Playing&&r.transport.State().seconds==1&&r.actualPlayStart.load()!=0,"explicit Play after capture readiness starts playback");

    r.original=&Body;r.cameraFeatures.original=&Camera;feature::owned_camera=&r.cameraFeatures;
    hub.original_converter=&Converter;hub.original_group=&Group;
    Json terminal=scene::Status();terminal["cleanup_complete"]=true;terminal["managed_owner_retirement_checked"]=true;
    Check(!scene::ReleaseRetiredPayload(terminal)&&!r.actors.empty(),"live playback cannot release its payload");
    Check(scene::CloseAndRestore(GetTickCount64()+1000),"native recovery closes synthetic callback admission before retirement");
    terminal["managed_owner_retirement_checked"]=false;
    Check(!scene::ReleaseRetiredPayload(terminal),"native completion alone cannot retire managed ownership");terminal["managed_owner_retirement_checked"]=true;
    a.features.face_state.count=1;a.features.face_state.backup[0].pending=true;
    Check(!scene::ReleaseRetiredPayload(terminal),"pending expression restoration retains the payload");a.features.face_state.backup[0].pending=false;
    a.nativeRegistered=true;Check(!scene::ReleaseRetiredPayload(terminal),"native registry reference retains actor storage");a.nativeRegistered=false;
    hub.observing=true;Check(!scene::ReleaseRetiredPayload(terminal),"open native callback admission blocks release");hub.observing=false;
    r.cameraFeatures.work=1;Check(!scene::ReleaseRetiredPayload(terminal),"in-flight camera work blocks release");r.cameraFeatures.work=0;
    terminal=scene::Status();terminal["cleanup_complete"]=true;terminal["managed_owner_retirement_checked"]=true;
    const auto finalActors=terminal.at("actors");
    Check(scene::ReleaseRetiredPayload(terminal)&&terminal.at("retired_payload_released").get<bool>(),"retired session releases its payload after freezing terminal evidence");
    Check(r.actors.empty()&&r.actors.capacity()==0&&r.byMesh.capacity()==0&&r.bySlot.capacity()==0&&!r.cameraAnchor,"actor objects, frames and lookup storage are released");
    Check(r.cameraFeatures.clip.source_keys.capacity()==0&&r.cameraFeatures.anchor_meshes.capacity()==0&&r.cameraFeatures.anchor_identities.capacity()==0&&r.cameraFeatures.anchor_clip_starts.capacity()==0,"camera payload and actor anchors are released");
    Check(scene::Status().at("actor_count")==1&&scene::Status().at("actors")==finalActors,"terminal diagnostics preserve actor counts after payload release");
    scene::Detour(nullptr);feature::CameraDetour(nullptr,0);native::ConverterDetour(nullptr,nullptr);native::GroupDetour(nullptr,nullptr);
    Check(bodyCalls==1&&cameraCalls==1&&converterCalls==1&&groupCalls==1,"delayed detours still forward exactly once without touching retired payload");
    Check(scene::ReleaseRetiredPayload(terminal)&&scene::Status()==terminal,"payload retirement is idempotent");
    std::cout<<"session retirement: "<<assertions<<" assertions passed\n";
}
int main(){
    try{RunTests();return 0;}
    catch(const std::exception& error){
        std::cerr<<"session retirement: unexpected exception after "<<assertions<<" assertions: "<<error.what()<<'\n';
        return 1;
    }
}
