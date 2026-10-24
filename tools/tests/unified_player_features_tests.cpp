// Synthetic in-process memory only: no game process, windows, hooks or engine calls.
#include "../../probes/v20_recon/unified_player_features.h"
#include <cstdlib>
#include <iostream>
#include <memory>

namespace feature=unified_player_features;
namespace play=wwp_playback;
namespace sub=wwp_ue::subtree_rotation;
static unsigned assertions=0,morph_calls=0;
static void Check(bool condition,const char* message){++assertions;if(!condition){std::cerr<<message<<'\n';std::exit(1);}}
static bool Near(double x,double y){return std::fabs(x-y)<.001;}
template<class T>static void Put(uint64_t address,const T& value){std::memcpy((void*)address,&value,sizeof(value));}
template<class T>static T Get(uint64_t address){T value{};std::memcpy(&value,(void*)address,sizeof(value));return value;}
static void Transform(uint64_t address,float x=0,float y=0,float z=0){
    std::array<float,12> values{0,0,0,1,x,y,z,0,1,1,1,0};std::memcpy((void*)address,values.data(),48);}
struct Fixture {
    std::array<std::vector<uint8_t>,8> objects;
    std::array<std::array<uint8_t,24>,8> items{};
    std::array<uint8_t,128> names{};uint64_t item_block=0,name_block=0;
    std::array<uint64_t,1> morph_targets{};std::array<uint8_t,24> weight_row{};
    std::array<sub::TransformBytes,1> first_pose{},second_pose{};
    Go go;Pool pool;::Reader reader{GetCurrentProcess()};
    uint64_t Object(unsigned i){return uint64_t(objects[i].data());}
    Fixture(){
        for(auto& object:objects)object.resize(0x4000);
        item_block=uint64_t(items.data());name_block=uint64_t(names.data());
        go={0,uint64_t(&item_block),unsigned(objects.size())};pool={0,uint64_t(&name_block),0,128};
        Put(name_block+4,uint16_t(5*2));std::memcpy((void*)(name_block+6),"Smile",5);
        for(unsigned i=0;i<objects.size();++i){const auto a=Object(i);Put(a,Object(7)+0x1000);Put(a+12,i);Put(a+16,Object(7));
            Put(uint64_t(items[i].data()),a);Put(uint64_t(items[i].data())+16,100+i);}
        Put(Object(0)+0x2e8,Object(1));Put(Object(0)+0x340,Object(1));Put(Object(0)+0x360,Object(2));Put(Object(1)+0x310,Object(4));
        for(unsigned mesh:{3u,4u}){Put(Object(mesh)+0x5d0,Object(5));Transform(Object(mesh)+0x1e0,mesh==3?100.f:0.f);}
        morph_targets[0]=Object(6);Put(Object(5)+0x368,play::face_snapshot::ArrayHead{uint64_t(morph_targets.data()),1,1});
        Put(Object(3)+0xef0,uint64_t(weight_row.data()));Put(Object(3)+0xef8,uint32_t(1));Put(Object(3)+0xf00,uint8_t(1));
        Put(uint64_t(weight_row.data())+12,.2f);
        Transform(uint64_t(first_pose.data()),20);Transform(uint64_t(second_pose.data()),200);
        for(unsigned mesh:{3u,4u}){const auto pose=mesh==3?uint64_t(first_pose.data()):uint64_t(second_pose.data());
            Put(Object(mesh)+0x630,v20_publish_observer::ArrayHead{pose,1,1});Put(Object(mesh)+0x640,v20_publish_observer::ArrayHead{pose,1,1});
            Put(Object(mesh)+0x6c0,int32_t(0));Put(Object(mesh)+0x6c4,int32_t(0));}
        NativeCamera({0,10,20});
    }
    void NativeCamera(std::array<float,3> position){for(unsigned base:{0x2e50u,0x38f0u}){Put(Object(2)+base,position);Put(Object(2)+base+12,std::array<float,3>{0,0,0});Put(Object(2)+base+24,60.f);}}
    std::array<float,3> CameraPosition(){return Get<std::array<float,3>>(Object(2)+0x2e50);}
};
// A fixture setter implements the one-key map touched by the real FaceFrame.
// The shared callback invokes this local function instead of a game RVA.
static void FixtureMorph(void* mesh,const uint32_t* name,float weight,bool remove){
    ++morph_calls;const auto address=uint64_t(mesh);const auto row=Get<uint64_t>(address+0xef0);
    std::memcpy((void*)row,name,12);Put(row+12,weight);Put(address+0xf00,uint8_t(remove&&weight<=1e-8f?0:1));
}
static void DirectTest(Fixture& f){
    play::local_clip::Clip clip;clip.bone_count=1;clip.frame_count=1;clip.fps=60;clip.duration=1;clip.frames.resize(1);
    clip.frames[0].mask=3;clip.frames[0].translation_delta[0]=5;clip.frames[0].translation_delta[2]=2;
    wwp_ue::full_body::Topology topology;topology.count=1;topology.parents[0]=-1;wwp_ue::full_body::Request request;
    Json cfg={{"native_physics",false},{"physics_enabled",false},{"expression_enabled",false},{"bone_names",{"root"}},{"parents",{-1}},{"physics_model_id","fixture"}};
    auto first=std::make_unique<feature::ActorFeatures>(),second=std::make_unique<feature::ActorFeatures>();
    feature::PrepareActor(*first,f.reader,f.go,f.pool,cfg,{},f.Object(3),f.Object(5),clip,topology,request,42);
    feature::PrepareActor(*second,f.reader,f.go,f.pool,cfg,{},f.Object(4),f.Object(5),clip,topology,request,42);
    Check(first->prepared&&!first->native_physics&&!first->physics_available,"direct mode remains available without an inertia profile");
    play::PlayerState state;state.clip_epoch=42;state.mode=play::PlayerMode::Playing;
    const play::placement::Settings placed{10,20,30,90,false};
    Check(feature::ActorFrame(*first,f.reader,state,placed,true),"direct frame samples and writes the actor");
    auto position=Get<std::array<float,3>>(uint64_t(f.first_pose.data())+16);
    Check(Near(position[0],30)&&Near(position[1],25)&&Near(position[2],32),"yaw rotates authored displacement while manual position and height stay independent");
    state.mode=play::PlayerMode::Paused;Check(feature::ActorFrame(*first,f.reader,state,placed,true),"paused pose can be published again");
    Check(Get<std::array<float,3>>(uint64_t(f.first_pose.data())+16)==position,"repeated paused publication does not accumulate placement");
    Check(feature::ActorFrame(*second,f.reader,state,{-10,0,-5,0,false},true),"second actor accepts its own placement");
    const auto other=Get<std::array<float,3>>(uint64_t(f.second_pose.data())+16);
    Check(Near(other[0],195)&&Near(other[2],-3)&&Get<std::array<float,3>>(uint64_t(f.first_pose.data())+16)==position,"per-actor placement does not mutate another actor");
    Check(feature::ActorPending(*first)&&feature::ActorFrame(*first,f.reader,state,placed,false)&&!feature::ActorPending(*first),"stop drains the owned direct pose ledger");
    Check(Near(Get<float>(uint64_t(f.first_pose.data())+16),20)&&Get<std::array<float,3>>(uint64_t(f.second_pose.data())+16)==other,"restore recovers the first baseline and preserves the second actor");
    Check(feature::ActorFrame(*first,f.reader,state,{10,20,30,90,true},true),"in-place frame is accepted");
    position=Get<std::array<float,3>>(uint64_t(f.first_pose.data())+16);
    Check(Near(position[0],30)&&Near(position[1],20)&&Near(position[2],32),"in-place removes authored horizontal displacement and retains vertical motion");
    Put(uint64_t(f.first_pose.data())+16,std::array<float,3>{777,88,99});
    Check(feature::ActorFrame(*first,f.reader,state,{},false)&&Near(Get<float>(uint64_t(f.first_pose.data())+16),777),"restore preserves newer native position values");
    Put(uint64_t(f.items[3].data())+16,uint32_t(999));
    Check(!feature::ActorFrame(*first,f.reader,state,{},true),"actor replacement invalidates direct writes");Put(uint64_t(f.items[3].data())+16,uint32_t(103));
    feature::ActorFrame(*second,f.reader,state,{},false);
}
static void FaceTest(Fixture& f){
    auto a=std::make_unique<feature::ActorFeatures>();a->objects=f.go;a->pool=f.pool;a->face_enabled=true;a->morph_function=uint64_t(&FixtureMorph);
    const auto captured=CaptureFaceCatalog(f.reader,f.go,f.pool,f.Object(3),f.Object(5),1,a->face_prepared.snapshot);
    Check(captured->morph_count==1&&std::string(captured->morphs[0].name.data())=="Smile","catalog captures live named morph objects in asset order");
    a->face_prepared.count=1;a->face_prepared.names[0]=captured->morphs[0].identity.name;a->face_prepared.mapped[0]=1;a->face_order[0]=0;
    a->face_clip.target_count=1;a->face_clip.frame_count=2;a->face_clip.fps=60;a->face_clip.duration=1./60;a->face_clip.payload.resize(32);
    a->face_clip.payload[0]=.75f;a->face_clip.payload[16]=.25f;play::PlayerState state;state.mode=play::PlayerMode::Playing;
    Check(feature::FaceFrame(*a,f.reader,state,true)&&Near(Get<float>(uint64_t(f.weight_row.data())+12),.75),"face frame samples the clip and calls the mapped setter");
    const unsigned calls=morph_calls;Check(feature::FaceFrame(*a,f.reader,state,true)&&morph_calls==calls,"duplicate expression frames avoid redundant native setter calls");
    state.seconds=1./120;
    Check(feature::FaceFrame(*a,f.reader,state,true)&&Near(Get<float>(uint64_t(f.weight_row.data())+12),.5),"expression weights interpolate at the shared cursor");
    Check(feature::FaceFrame(*a,f.reader,state,false)&&Near(Get<float>(uint64_t(f.weight_row.data())+12),.2)&&!feature::ActorPending(*a),"face stop restores the original override weight");
    feature::FaceFrame(*a,f.reader,state,true);Put(uint64_t(f.weight_row.data())+12,.9f);
    Check(feature::FaceFrame(*a,f.reader,state,false)&&Near(Get<float>(uint64_t(f.weight_row.data())+12),.9),"expression restoration preserves a newer native weight");
    // Regression: a latched native body error must not strand a previously
    // written face override, including when an earlier restore could not run.
    a->prepared=true;a->native_physics=true;
    Check(ww_object::ReadStable(f.reader,f.go,f.Object(3),&a->mesh)&&ww_object::ReadStable(f.reader,f.go,f.Object(5),&a->asset),"fault regression captures feature identities");
    Check(feature::FaceFrame(*a,f.reader,state,true)&&feature::ActorPending(*a),"native fault regression starts with an owned face override");
    unsigned closes=0;std::atomic<unsigned> latched_native_error{11};
    Put(uint64_t(f.items[3].data())+16,uint32_t(999));
    if(latched_native_error.load())Check(!feature::RestoreActorAfterFault(*a,f.reader,state,{},[&]{++closes;})&&feature::ActorPending(*a),"failed feature restore remains pending after native admission closes");
    Put(uint64_t(f.items[3].data())+16,uint32_t(103));
    if(latched_native_error.load())Check(feature::RestoreActorAfterFault(*a,f.reader,state,{},[&]{++closes;})&&!feature::ActorPending(*a)&&Near(Get<float>(uint64_t(f.weight_row.data())+12),.9),"latched native error permits the later face restore and drains pending ownership");
    const unsigned after_restore=morph_calls;
    if(latched_native_error.load())Check(feature::RestoreActorAfterFault(*a,f.reader,state,{},[&]{++closes;})&&closes==3&&morph_calls==after_restore,"repeated permanent native errors remain restore-only and do not rewrite expressions");
}
static void CameraTest(Fixture& f){
    auto c=std::make_unique<feature::CameraFeatures>();Json cfg={{"camera_clip_enabled",false},{"camera_mode",0}};
    feature::PrepareCamera(*c,f.reader,f.go,cfg,{},f.Object(0),f.Object(1),{f.Object(3)},{},{},42,GetCurrentThreadId(),0);
    play::PlayerState state;state.mode=play::PlayerMode::Playing;state.placement_x=25;
    Check(feature::CameraFrame(*c,f.reader,state,true,f.Object(2)),"follow camera frame accepts the captured local chain");
    Check(Near(f.CameraPosition()[0],125)&&Near(f.CameraPosition()[1],10),"follow camera translates the native view to the selected actor and placement");
    f.NativeCamera({0,10,20});state.mode=play::PlayerMode::Paused;
    Check(feature::CameraFrame(*c,f.reader,state,true,f.Object(2))&&Near(f.CameraPosition()[0],125),"paused follow camera does not accumulate offsets");
    state.camera_mode=1;f.NativeCamera({0,11,30});
    Check(feature::CameraFrame(*c,f.reader,state,true,f.Object(2))&&Near(f.CameraPosition()[0],125)&&Near(f.CameraPosition()[1],10),"fixed mode captures the previously displayed scene view");
    f.NativeCamera({500,30,60});Check(feature::CameraFrame(*c,f.reader,state,true,f.Object(2))&&Near(f.CameraPosition()[0],125),"fixed mode remains fixed after native camera movement");
    Check(feature::CameraPending(*c)&&feature::CameraFrame(*c,f.reader,state,false,f.Object(2))&&!feature::CameraPending(*c),"camera stop drains every owned POV field");
    Check(Near(f.CameraPosition()[0],500)&&Near(f.CameraPosition()[1],30),"camera restoration returns to the latest native baseline");
    c->clip_available=true;c->clip.duration=1;c->clip.source_keys.resize(1);c->clip.source_keys[0].distance=-10;c->clip.source_keys[0].fov=45;
    state.camera_mode=2;state.placement_x=0;f.NativeCamera({0,10,20});
    Check(feature::CameraFrame(*c,f.reader,state,true,f.Object(2)),"VMD mode samples a camera source key");
    Check(Near(f.CameraPosition()[0],100)&&Near(f.CameraPosition()[2],-10)&&Near(Get<float>(f.Object(2)+0x2e68),45),"VMD sample uses the selected actor anchor and lens");
    Put(f.Object(2)+0x2e50,std::array<float,3>{888,77,66});
    Check(feature::CameraFrame(*c,f.reader,state,false,f.Object(2))&&Near(f.CameraPosition()[0],888),"camera restore preserves a newer native camera field");
    state.camera_mode=0;Put(uint64_t(f.items[3].data())+16,uint32_t(999));
    Check(!feature::CameraFrame(*c,f.reader,state,true,f.Object(2)),"camera anchor identity replacement is rejected");
    Put(uint64_t(f.items[3].data())+16,uint32_t(103));feature::CameraFrame(*c,f.reader,state,false,f.Object(2));
    Check(feature::UninstallCamera(*c),"an uninstalled camera can close cleanly after preparation");
}
struct CallbackFixture {Fixture* memory;unsigned original=0,maintenance=0;bool ordered=true,allow=true;};
static CallbackFixture* callback_fixture=nullptr;
static void SyntheticCameraOriginal(void*,float){++callback_fixture->original;callback_fixture->memory->NativeCamera({0,10,20});}
static bool SyntheticMaintenance(void* user){auto& value=*static_cast<CallbackFixture*>(user);++value.maintenance;
    value.ordered=value.ordered&&value.original==value.maintenance;return value.allow;}
static void CameraCallbackTest(Fixture& f){
    auto c=std::make_unique<feature::CameraFeatures>();Json config={{"camera_clip_enabled",false},{"camera_mode",0}};
    feature::PrepareCamera(*c,f.reader,f.go,config,{},f.Object(0),f.Object(1),{f.Object(3)},{},{},42,GetCurrentThreadId(),0);
    CallbackFixture events{&f};callback_fixture=&events;c->original=&SyntheticCameraOriginal;c->on_game_frame=&SyntheticMaintenance;c->game_frame_user=&events;c->observing=true;
    auto* previous=feature::owned_camera;feature::owned_camera=c.get();
    play::PlayerState state;state.mode=play::PlayerMode::Playing;state.placement_x=25;feature::PublishCamera(*c,state,true);
    feature::CameraDetour((void*)f.Object(2),.016f);
    Check(events.original==1&&events.maintenance==1&&events.ordered,"scene maintenance runs once after original camera update on the accepted game thread");
    Check(feature::CameraPending(*c)&&Near(f.CameraPosition()[0],125),"maintenance shares the existing camera hook and playback overlay");
    events.allow=false;feature::CameraDetour((void*)f.Object(2),.016f);
    Check(c->failed.load()&&!feature::CameraPending(*c)&&Near(f.CameraPosition()[0],0),"maintenance failure still drains camera ownership instead of skipping restoration");
    c->prepared.game_thread=0;feature::CameraDetour((void*)f.Object(2),.016f);
    Check(events.original==3&&events.maintenance==2,"unaccepted thread forwards original without calling managed maintenance");
    c->prepared.game_thread=GetCurrentThreadId();c->observing=false;feature::CameraDetour((void*)f.Object(2),.016f);
    Check(events.original==4&&events.maintenance==2,"retired camera callbacks retain forwarding and do not maintain actors");
    feature::owned_camera=previous;callback_fixture=nullptr;
}
// Formation scenes must frame the group: the follow camera tracks the live
// centroid of every stage mesh, and the VMD orbit anchors the shared stage
// origin with the measured mesh basis orienting it.
// Without a stage reference the orbit falls back to the first actor's spot.
static void FormationCameraTest(Fixture& f){
    auto c=std::make_unique<feature::CameraFeatures>();Json cfg={{"camera_clip_enabled",false},{"camera_mode",0}};
    feature::PrepareCamera(*c,f.reader,f.go,cfg,{},f.Object(0),f.Object(1),{f.Object(3),f.Object(4)},
        {Vec3{},Vec3{20.f,0.f,0.f}},{},42,GetCurrentThreadId(),0);
    play::PlayerState state;state.mode=play::PlayerMode::Playing;state.placement_x=25;
    f.NativeCamera({0,10,20});
    Check(feature::CameraFrame(*c,f.reader,state,true,f.Object(2)),"follow camera frame accepts the formation anchors");
    Check(Near(f.CameraPosition()[0],75)&&Near(f.CameraPosition()[1],10),"follow camera tracks the live centroid of every stage mesh");
    f.NativeCamera({0,10,20});state.mode=play::PlayerMode::Paused;state.camera_mode=2;state.placement_x=0;
    c->clip_available=true;c->clip.duration=1;c->clip.source_keys.resize(1);c->clip.source_keys[0].distance=-10;c->clip.source_keys[0].fov=45;
    Check(feature::CameraFrame(*c,f.reader,state,true,f.Object(2)),"VMD mode samples a formation camera key");
    Check(Near(f.CameraPosition()[0],100)&&Near(f.CameraPosition()[2],-10)&&Near(Get<float>(f.Object(2)+0x2e68),45),
        "without a stage reference the VMD orbit falls back to the first actor's stage spot");
    state.camera_mode=0;Put(uint64_t(f.items[4].data())+16,uint32_t(999));
    Check(!feature::CameraFrame(*c,f.reader,state,true,f.Object(2)),"any formation anchor identity replacement is rejected");
    Put(uint64_t(f.items[4].data())+16,uint32_t(104));
    Check(feature::CameraFrame(*c,f.reader,state,false,f.Object(2))&&!feature::CameraPending(*c),"formation camera stop drains owned POV fields");
    Check(feature::UninstallCamera(*c),"formation camera can close cleanly after preparation");

    auto staged=std::make_unique<feature::CameraFeatures>();
    Json lifecycle={{"stage_reference_location",{-51520.,-562560.,36999.}},{"stage_reference_floor",36805.}};
    Check(!feature::ReadStageReference(lifecycle).present,"legacy global stage positions must not be accepted as local camera anchors");
    lifecycle["stage_reference_camera_offset_from_mesh"]={-90.f,0.f,5.f};
    const auto reference=feature::ReadStageReference(lifecycle);
    feature::PrepareCamera(*staged,f.reader,f.go,cfg,{},f.Object(0),f.Object(1),{f.Object(3),f.Object(4)},{Vec3{},Vec3{20.f,0.f,0.f}},
        reference,42,GetCurrentThreadId(),0);
    staged->clip_available=true;staged->clip.duration=1;staged->clip.source_keys.resize(1);
    staged->clip.source_keys[0].distance=-10;staged->clip.source_keys[0].fov=45;staged->clip.source_keys[0].target={1.f,0.f,0.f};
    state.camera_mode=2;f.NativeCamera({0,10,20});
    Check(feature::CameraFrame(*staged,f.reader,state,true,f.Object(2)),"VMD mode samples a stage-referenced camera key");
    Check(Near(f.CameraPosition()[0],11)&&Near(f.CameraPosition()[1],0)&&Near(f.CameraPosition()[2],-5),
        "relative stage reference pins local XY and floor Z without importing global coordinates");
    // Rebase every world component while leaving the global stage stationary.
    // The camera must move with local space even after its orbit was captured.
    Transform(f.Object(3)+0x1e0,-1900.f,3000.f,-4000.f);
    Transform(f.Object(4)+0x1e0,-2000.f,3000.f,-4000.f);
    f.NativeCamera({-2000.f,3010.f,-3980.f});
    Check(feature::CameraFrame(*staged,f.reader,state,true,f.Object(2))&&
        Near(f.CameraPosition()[0],-1989)&&Near(f.CameraPosition()[1],3000)&&Near(f.CameraPosition()[2],-4005),
        "an already-captured VMD orbit follows a three-axis world-origin rebase without global stage drift");
    Check(Get<std::array<float,3>>(f.Object(2)+0x38f0)==f.CameraPosition(),"both camera caches receive the same rebased local position");
    Check(feature::CameraFrame(*staged,f.reader,state,false,f.Object(2))&&!feature::CameraPending(*staged),"stage-referenced camera stop drains owned POV fields");
    Check(Near(f.CameraPosition()[0],-2000)&&Near(f.CameraPosition()[1],3010)&&Near(f.CameraPosition()[2],-3980),
        "camera stop restores the rebased native baseline");
    Check(feature::UninstallCamera(*staged),"stage-referenced camera can close cleanly after preparation");
    Transform(f.Object(3)+0x1e0,100.f);Transform(f.Object(4)+0x1e0);
}
int main(){auto fixture=std::make_unique<Fixture>();DirectTest(*fixture);FaceTest(*fixture);CameraTest(*fixture);FormationCameraTest(*fixture);CameraCallbackTest(*fixture);
    std::cout<<assertions<<" synthetic feature assertions passed; game_accessed=false; hooks_installed=false\n";}
