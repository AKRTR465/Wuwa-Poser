// Synthetic in-process memory only: no game process, windows, hooks or engine calls.
#include "../../runtime/game_integration/unified_player_features.h"
#include "../../playback/ww_gaze_profile.h"
#include "../../playback/native_kawaii_parameters.h"
#include <cstdlib>
#include <iostream>
#include <memory>
#include <numeric>
#include <sstream>

namespace feature=unified_player_features;
namespace play=wwp_playback;
namespace sub=wwp_ue::subtree_rotation;
constexpr auto kCurrentPov=ww_game_compat::CameraCacheOffset+ww_game_compat::CameraPovOffset;
constexpr auto kPreviousPov=ww_game_compat::CameraLastCacheOffset+ww_game_compat::CameraPovOffset;
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
        morph_targets[0]=Object(6);Put(Object(5)+ww_game_compat::AssetMorphArrayOffset,play::face_snapshot::ArrayHead{uint64_t(morph_targets.data()),1,1});
        Put(Object(3)+0xef0,uint64_t(weight_row.data()));Put(Object(3)+0xef8,uint32_t(1));Put(Object(3)+0xf00,uint8_t(1));
        Put(uint64_t(weight_row.data())+12,.2f);
        Transform(uint64_t(first_pose.data()),20);Transform(uint64_t(second_pose.data()),200);
        for(unsigned mesh:{3u,4u}){const auto pose=mesh==3?uint64_t(first_pose.data()):uint64_t(second_pose.data());
            Put(Object(mesh)+0x630,publish_observer::ArrayHead{pose,1,1});Put(Object(mesh)+0x640,publish_observer::ArrayHead{pose,1,1});
            Put(Object(mesh)+0x6c0,int32_t(0));Put(Object(mesh)+0x6c4,int32_t(0));}
        NativeCamera({0,10,20});
    }
    void NativeCamera(std::array<float,3> position){for(unsigned base:{kCurrentPov,kPreviousPov}){Put(Object(2)+base,position);Put(Object(2)+base+12,std::array<float,3>{0,0,0});Put(Object(2)+base+24,60.f);}}
    std::array<float,3> CameraPosition(){return Get<std::array<float,3>>(Object(2)+kCurrentPov);}
};
// A fixture setter implements the one-key map touched by the real FaceFrame.
// The shared callback invokes this local function instead of a game RVA.
static void FixtureMorph(void* mesh,const uint32_t* name,float weight,bool remove){
    ++morph_calls;const auto address=uint64_t(mesh);const auto row=Get<uint64_t>(address+0xef0);
    std::memcpy((void*)row,name,12);Put(row+12,weight);Put(address+0xf00,uint8_t(remove&&weight<=1e-8f?0:1));
}
static void DirectTest(Fixture& f){
    Check(ww_object::SameName("Facial_FaceLine_a_01_M","Facial_FaceLine_A_01_M")&&
        !ww_object::SameName("Facial_FaceLine_a_01_M","Facial_FaceLine_A_02_M"),"reference names ignore display case and retain numeric suffixes");
    play::local_clip::Clip clip;clip.bone_count=1;clip.frame_count=1;clip.fps=60;clip.duration=1;clip.frames.resize(1);
    clip.frames[0].mask=3;clip.frames[0].translation_delta[0]=5;clip.frames[0].translation_delta[2]=2;
    wwp_ue::full_body::Topology topology;topology.count=1;topology.parents[0]=-1;wwp_ue::full_body::Request request;
    Json cfg={{"native_physics",false},{"physics_enabled",false},{"expression_enabled",false},{"bone_names",{"root"}},{"bone_parents",{-1}},{"physics_model_id","fixture"}};
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
    // The scene host closes the feed on a native fault and keeps restoring.
    Put(uint64_t(f.items[3].data())+16,uint32_t(999));
    Check(!feature::FaceFrame(*a,f.reader,state,false)&&feature::ActorPending(*a),"failed feature restore remains pending after native admission closes");
    Put(uint64_t(f.items[3].data())+16,uint32_t(103));
    Check(feature::FaceFrame(*a,f.reader,state,false)&&!feature::ActorPending(*a)&&Near(Get<float>(uint64_t(f.weight_row.data())+12),.9),"latched native error permits the later face restore and drains pending ownership");
    const unsigned after_restore=morph_calls;
    Check(feature::FaceFrame(*a,f.reader,state,false)&&morph_calls==after_restore,"repeated permanent native errors remain restore-only and do not rewrite expressions");
}
static void CameraTest(Fixture& f){
    auto c=std::make_unique<feature::CameraFeatures>();Json cfg={{"camera_clip_enabled",false},{"camera_mode",0}};
    feature::PrepareCamera(*c,f.reader,f.go,cfg,{},f.Object(0),f.Object(1),{f.Object(3)},{},42,GetCurrentThreadId(),0);
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
    Check(Near(f.CameraPosition()[0],100)&&Near(f.CameraPosition()[2],-10)&&Near(Get<float>(f.Object(2)+(kCurrentPov+ww_game_compat::CameraFovOffset)),45),"VMD sample uses the selected actor anchor and lens");
    Put(f.Object(2)+kCurrentPov,std::array<float,3>{888,77,66});
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
    feature::PrepareCamera(*c,f.reader,f.go,config,{},f.Object(0),f.Object(1),{f.Object(3)},{},42,GetCurrentThreadId(),0);
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
// A zero stage offset anchors the orbit on the first actor's mesh.
static void FormationCameraTest(Fixture& f){
    auto c=std::make_unique<feature::CameraFeatures>();Json cfg={{"camera_clip_enabled",false},{"camera_mode",0}};
    feature::PrepareCamera(*c,f.reader,f.go,cfg,{},f.Object(0),f.Object(1),{f.Object(3),f.Object(4)},{},42,GetCurrentThreadId(),0);
    play::PlayerState state;state.mode=play::PlayerMode::Playing;state.placement_x=25;
    f.NativeCamera({0,10,20});
    Check(feature::CameraFrame(*c,f.reader,state,true,f.Object(2)),"follow camera frame accepts the formation anchors");
    Check(Near(f.CameraPosition()[0],75)&&Near(f.CameraPosition()[1],10),"follow camera tracks the live centroid of every stage mesh");
    f.NativeCamera({0,10,20});state.mode=play::PlayerMode::Paused;state.camera_mode=2;state.placement_x=0;
    c->clip_available=true;c->clip.duration=1;c->clip.source_keys.resize(1);c->clip.source_keys[0].distance=-10;c->clip.source_keys[0].fov=45;
    Check(feature::CameraFrame(*c,f.reader,state,true,f.Object(2)),"VMD mode samples a formation camera key");
    Check(Near(f.CameraPosition()[0],100)&&Near(f.CameraPosition()[2],-10)&&Near(Get<float>(f.Object(2)+(kCurrentPov+ww_game_compat::CameraFovOffset)),45),
        "a zero stage offset anchors the VMD orbit on the first actor's mesh");
    state.camera_mode=0;Put(uint64_t(f.items[4].data())+16,uint32_t(999));
    Check(!feature::CameraFrame(*c,f.reader,state,true,f.Object(2)),"any formation anchor identity replacement is rejected");
    Put(uint64_t(f.items[4].data())+16,uint32_t(104));
    Check(feature::CameraFrame(*c,f.reader,state,false,f.Object(2))&&!feature::CameraPending(*c),"formation camera stop drains owned POV fields");
    Check(feature::UninstallCamera(*c),"formation camera can close cleanly after preparation");

    auto staged=std::make_unique<feature::CameraFeatures>();
    Json lifecycle={{"stage_reference_location",{-51520.,-562560.,36999.}},{"stage_reference_floor",36805.}};
    bool missingReference=false;try{feature::ReadStageReference(lifecycle);}catch(const std::exception&){missingReference=true;}
    Check(missingReference,"global stage positions alone are never accepted as local camera anchors");
    lifecycle["stage_reference_camera_offset_from_mesh"]={-90.f,0.f,5.f};
    const auto reference=feature::ReadStageReference(lifecycle);
    feature::PrepareCamera(*staged,f.reader,f.go,cfg,{},f.Object(0),f.Object(1),{f.Object(3),f.Object(4)},reference,42,GetCurrentThreadId(),0);
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
    Check(Get<std::array<float,3>>(f.Object(2)+kPreviousPov)==f.CameraPosition(),"both camera caches receive the same rebased local position");
    Check(feature::CameraFrame(*staged,f.reader,state,false,f.Object(2))&&!feature::CameraPending(*staged),"stage-referenced camera stop drains owned POV fields");
    Check(Near(f.CameraPosition()[0],-2000)&&Near(f.CameraPosition()[1],3010)&&Near(f.CameraPosition()[2],-3980),
        "camera stop restores the rebased native baseline");
    Check(feature::UninstallCamera(*staged),"stage-referenced camera can close cleanly after preparation");
    Transform(f.Object(3)+0x1e0,100.f);Transform(f.Object(4)+0x1e0);
}
static void CameraTrackHeightTest(Fixture& f){
    auto c=std::make_unique<feature::CameraFeatures>();Json cfg={{"camera_clip_enabled",false},{"camera_mode",0}};
    feature::PrepareCamera(*c,f.reader,f.go,cfg,{},f.Object(0),f.Object(1),{f.Object(3)},feature::StageReference{{20,30,-40}},42,GetCurrentThreadId(),0);
    c->clip_available=true;c->clip.duration=2;c->source_basis=Quat::AxisAngle({1,0,0},.4f);c->unit_scale=3;
    for(unsigned i=0;i<3;++i){mmd::CameraKey key;key.frame=i*30;key.target={float(i*2),float(i*3),float(i)};key.rotation={.1f*i,.2f*i,.3f*i};key.distance=-10.f-float(i*2);key.fov=35.f+float(i*10);c->clip.source_keys.push_back(key);}
    play::PlayerState state;state.mode=play::PlayerMode::Paused;state.camera_mode=2;
    for(double seconds:{0.,.25,.8,1.4,2.,.25}){
        state.seconds=seconds;state.camera_track_height_cm=0;f.NativeCamera({0,10,20});
        Check(feature::CameraFrame(*c,f.reader,state,true,f.Object(2)),"baseline VMD track samples at any paused cursor");
        const auto base=f.CameraPosition();const auto rotation=Get<std::array<float,3>>(f.Object(2)+(kCurrentPov+ww_game_compat::CameraRotationOffset));const auto fov=Get<float>(f.Object(2)+(kCurrentPov+ww_game_compat::CameraFovOffset));
        state.camera_track_height_cm=125;f.NativeCamera({0,10,20});
        Check(feature::CameraFrame(*c,f.reader,state,true,f.Object(2))&&Near(f.CameraPosition()[0],base[0])&&Near(f.CameraPosition()[1],base[1])&&Near(f.CameraPosition()[2],base[2]+125),"whole-track offset adds the same world Z at every cursor, independent of source rotation and scale");
        Check(Get<std::array<float,3>>(f.Object(2)+(kCurrentPov+ww_game_compat::CameraRotationOffset))==rotation&&Get<float>(f.Object(2)+(kCurrentPov+ww_game_compat::CameraFovOffset))==fov,"track translation preserves authored angles and FOV");
        Check(feature::CameraFrame(*c,f.reader,state,true,f.Object(2))&&Near(f.CameraPosition()[2],base[2]+125),"paused repeated sampling never accumulates track height");
        state.camera_track_height_cm=-75;f.NativeCamera({0,10,20});
        Check(feature::CameraFrame(*c,f.reader,state,true,f.Object(2))&&Near(f.CameraPosition()[2],base[2]-75),"negative height translates the complete track downward");
        Check(Get<std::array<float,3>>(f.Object(2)+kPreviousPov)==f.CameraPosition(),"both camera caches receive the translated track");
    }
    state.camera_track_height_cm=0;f.NativeCamera({0,10,20});feature::CameraFrame(*c,f.reader,state,true,f.Object(2));const auto callbackBase=f.CameraPosition();
    CallbackFixture events{&f};callback_fixture=&events;c->original=&SyntheticCameraOriginal;c->observing=true;auto* previous=feature::owned_camera;feature::owned_camera=c.get();
    state.camera_track_height_cm=80;feature::PublishCamera(*c,state,true);feature::CameraDetour((void*)f.Object(2),.016f);
    Check(Near(f.CameraPosition()[2],callbackBase[2]+80),"track height crosses the worker mailbox and actual camera callback");
    c->observing=false;feature::owned_camera=previous;callback_fixture=nullptr;
    state.camera_mode=0;state.camera_track_height_cm=0;f.NativeCamera({0,10,20});feature::CameraFrame(*c,f.reader,state,true,f.Object(2));const auto follow=f.CameraPosition();
    state.camera_track_height_cm=300;f.NativeCamera({0,10,20});
    Check(feature::CameraFrame(*c,f.reader,state,true,f.Object(2))&&f.CameraPosition()==follow,"track-only offset leaves the game-follow view unchanged");
    state.camera_mode=1;Check(feature::CameraFrame(*c,f.reader,state,true,f.Object(2))&&f.CameraPosition()==follow,"track-only offset leaves the fixed view unchanged");
    f.NativeCamera({11,22,33});Check(feature::CameraFrame(*c,f.reader,state,false,f.Object(2))&&!feature::CameraPending(*c)&&f.CameraPosition()==std::array<float,3>{11,22,33},"stopping restores the current native camera without retaining a track shift");
    Check(feature::UninstallCamera(*c),"track-height camera closes cleanly");
}
static void GazeTest(){
    namespace gaze=play::gaze;
    const std::vector<std::string> targets={"A","Pupil_Up","Pupil_Down","Pupil_R","Pupil_L"};
    const auto profile=gaze::Build(targets);mmd::MotionClip motion;
    auto key=[&](const char* name,Quat q){mmd::BoneKey k;k.rotation=q;motion.bones[name]={k};};
    auto sample=[&](float strength=1){std::vector<float> weights(targets.size(),.7f);gaze::Apply(motion,profile,targets,0,strength,weights);return weights;};
    key(u8"両目",{});auto w=sample();Check(Near(w[0],.7)&&Near(w[1]+w[2]+w[3]+w[4],0),"neutral gaze clears owned directions and preserves mouth morph");
    constexpr float range=.2617993878f;
    key(u8"両目",Quat::AxisAngle({0,1,0},range/2));w=sample();Check(Near(w[3],.5)&&Near(w[4],0),"MMD positive yaw drives right pupil morph");
    auto q=motion.bones[u8"両目"][0].rotation;key(u8"両目",{-q.x,-q.y,-q.z,-q.w});Check(Near(sample()[3],.5),"quaternion sign does not flip gaze");
    key(u8"両目",Quat::AxisAngle({1,0,0},-range));w=sample();Check(Near(w[2],1)&&Near(w[1],0),"negative pitch drives down only");
    key(u8"両目",Quat::AxisAngle({0,0,1},range));w=sample();Check(Near(w[1]+w[2]+w[3]+w[4],0),"eye roll does not invent pupil translation");
    key(u8"両目",Quat::AxisAngle({0,1,0},range/4));key(u8"左目",Quat::AxisAngle({0,1,0},range/4));key(u8"右目",Quat::AxisAngle({0,1,0},range/4));
    Check(Near(sample()[3],.5)&&Near(sample(3)[3],1),"common and per-eye rotations compose before bounded strength");
    const std::vector<std::string> independent={"Pupil_Up","Pupil_Down","Pupil_L","Pupil_R","Pupil_Up_L","Pupil_Down_L","Pupil_Up_R","Pupil_Down_R","Pupil_In_L","Pupil_Out_L","Pupil_In_R","Pupil_Out_R"};
    const auto split=gaze::Build(independent);motion.bones.clear();key(u8"左目",Quat::AxisAngle({0,1,0},range));key(u8"右目",Quat::AxisAngle({0,1,0},-range));
    std::vector<float> weights(independent.size(),.8f);gaze::Apply(motion,split,independent,0,1,weights);
    Check(split.independent&&Near(weights[8],1)&&Near(weights[10],1)&&Near(weights[9]+weights[11]+weights[2]+weights[3],0),"independent gaze preserves convergence without double-driving common shapes");
    Check(gaze::Build({"A"}).channels.empty(),"missing pupil targets are unsupported, not guessed");
}
static void ClothingProfilesTest(const std::filesystem::path& data) {
    for(const auto* id:{"femalems--lili--r2t1lilimd10011","femalexl--taidaonv--r2t1taidaonvmd20011"}) {
        std::ifstream input(data/"characters"/"profiles"/(std::string(id)+".json"),std::ios::binary);
        Json profile;input>>profile;
        const auto names=profile.at("bone_names").get<std::vector<std::string>>();
        const auto parents=profile.at("bone_parents").get<std::vector<int32_t>>();
        const auto angular=play::ww_secondary::ReadCharacterProfile(data/"bone_chains.json",names,parents,
            profile.at("physics_model_id").get<std::string>());
        Check(angular.bone_count==names.size()&&angular.chain_count>0,"3.8 clothing profiles bind to their exact skeletons");
        std::vector<sub::TransformBytes> pose(names.size());
        for(size_t i=0;i<pose.size();++i)Transform(uint64_t(pose[i].data()),float(i));
        const auto prepared=play::ww_chain::Prepare(angular,names,pose.data());
        Check(prepared.capsule_count==4&&std::any_of(prepared.particles.begin(),prepared.particles.end(),
            [](const auto& particle){return particle.enabled&&!particle.pinned;}),
            "3.8 clothing profiles produce simulated descendants and collision capsules");
    }
}
static void PartContactTurnLimitTest(){
    namespace chain=play::ww_chain;
    chain::Prepared rig;
    rig.count=4;
    rig.parents={-1,0,0,0};
    rig.particles[0].enabled=rig.particles[0].pinned=true;
    rig.particles[1].enabled=true;
    rig.particles[1].anchor=0;
    rig.particles[1].cone_radians=1;
    rig.capsule_count=1;
    rig.capsules[0]={2,3,2};
    std::array<sub::TransformBytes,4> pose;
    Transform(uint64_t(pose[0].data()));
    Transform(uint64_t(pose[1].data()),10);
    Transform(uint64_t(pose[2].data()),10,10);
    Transform(uint64_t(pose[3].data()),10,10,3);
    auto runtime=std::make_unique<chain::Runtime>();
    auto scratch=std::make_unique<chain::Scratch>();
    auto plan=std::make_unique<sub::FixedPlan>();
    chain::Control control;
    control.enabled=control.playing=true;
    control.link_turn_rate=6;
    control.gravity_ue=0;
    control.project_constraint_velocity=true;
    Check(chain::Step(rig,control,pose.data(),rig.count,runtime.get(),scratch.get(),plan.get()),
        "sleeve initializes outside the arm obstacle");
    Transform(uint64_t(pose[2].data()),10);
    Transform(uint64_t(pose[3].data()),10,0,3);
    for(unsigned frame=1;frame<=6;++frame){
        const auto previous=chain::Unit(runtime->position[1]);
        control.clock_seconds=control.cursor_seconds=frame/60.;
        Check(chain::Step(rig,control,pose.data(),rig.count,runtime.get(),scratch.get(),plan.get()),
            "sleeve solves after the arm moves across its path");
        const auto current=runtime->position[1];
        Check(chain::collision::Dot(previous,chain::Unit(current))>=std::cos(.10001)&&
            Near(chain::collision::Length(current),10),"contact honors the turn rate and preserves link length");
        if(frame==1)Check(chain::collision::Length(current-runtime->previous[1])<1e-7,
            "a position-only contact correction cannot introduce velocity into a stationary sleeve");
    }
    Check(chain::collision::SegmentDistance(runtime->position[1],{10,0,0},{10,0,3})>=2.699,
        "contact clears the arm over successive frames");
}
static void SelectedPartGravityTest(const std::filesystem::path& data) {
    const std::string profile_id="femalems--lili--r2t1lilimd10011";
    const auto profile=Json::parse(std::ifstream(data/"characters"/"profiles"/(profile_id+".json")));
    const auto definitions=Json::parse(std::ifstream(data/"characters"/"part_gravity.json"));
    const auto names=profile.at("bone_names").get<std::vector<std::string>>();
    const auto parents=profile.at("bone_parents").get<std::vector<int32_t>>();
    const auto joint=[&](const char* name){return uint32_t(std::find(names.begin(),names.end(),name)-names.begin());};
    const auto anchor=joint("Bone_Piao200_L"),first=joint("Bone_Piao201_L"),edge=joint("Bone_Piao214_L"),right=joint("Bone_Piao201_R");
    const auto transition=joint("Bone_Piao219_L"),transition_anchor=uint32_t(parents[transition]);
    auto fixture=std::make_unique<Fixture>();
    auto actor=std::make_unique<feature::ActorFeatures>();
    actor->mesh.object=fixture->Object(3);
    actor->epoch=13;
    actor->topology.count=uint32_t(names.size());
    play::part_gravity::Prepare(definitions.at("profiles").at(profile_id),names,parents.data(),actor->gravity_parts);
    auto& part=actor->gravity_parts.front();
    auto& opposite=actor->gravity_parts.at(1);
    Check(part.rig.particles[anchor].pinned&&part.rig.particles[edge].enabled&&!part.rig.particles[right].enabled,
        "the left sleeve includes its edge and fixes its seam without selecting the right sleeve");
    std::vector<sub::TransformBytes> pose(names.size());
    for(size_t i=0;i<pose.size();++i)Transform(uint64_t(pose[i].data()),float(i),50);
    Transform(uint64_t(pose[anchor].data()),0,0,0);
    for(uint32_t index=0;index<names.size();++index)
        if((part.rig.particles[index].enabled&&!part.rig.particles[index].pinned)||
           (opposite.rig.particles[index].enabled&&!opposite.rig.particles[index].pinned)){
            const auto parent=sub::detail::Position(pose[uint32_t(parents[index])]);
            const auto& particle=part.rig.particles[index].enabled?part.rig.particles[index]:opposite.rig.particles[index];
            const bool branch=particle.direction_reference==play::ww_chain::DirectionReference::ParentFrame;
            Transform(uint64_t(pose[index].data()),float(parent[0])+5,float(parent[1])+(branch?5.f:0.f),0);
        }
    const auto original=pose;
    const auto head=publish_observer::ArrayHead{uint64_t(pose.data()),int32_t(names.size()),int32_t(names.size())};
    Put(actor->mesh.object+ww_game_compat::ComponentPoseBuffer0Offset,head);
    play::PlayerState state;state.mode=play::PlayerMode::Paused;
    part.strength=980;part.sequence=32;
    std::array<play::kawaii_parameters::Chain,2> shared_group{};
    shared_group[0].root_bone=first;shared_group[1].root_bone=right;
    Check(!play::part_gravity::CoversGroup(actor->gravity_parts,shared_group,2),
        "enabling only one sleeve leaves the shared native group override available");
    for(int step=0;step<100;++step)
        if(!feature::GravityPartFrame(*actor,fixture->reader,state,true,double(step)/60))
            Check(false,"part gravity frame publishes");
    Check(part.sampled==32&&part.moved_bones==11,"part output includes the main chain, edge branches, and proximal cloth");
    Check(pose[anchor]==original[anchor]&&pose[transition_anchor]==original[transition_anchor]&&pose[right]==original[right],
        "publication keeps both anchors and the opposite sleeve byte-identical");
    Check(sub::detail::Position(pose[first])[2]<-4&&pose[edge]!=original[edge],"gravity reaches the first free bone and the edge follows it");
    Check(sub::detail::Position(pose[transition])[2]<-1,"the additional cloth below the seam participates");
    for(uint32_t i=0;i<names.size();++i)
        if(!part.rig.particles[i].enabled)
            Check(pose[i]==original[i],"no writes outside the selected part");
    const auto published=pose;
    const auto world=actor->mesh.object+ww_game_compat::ComponentToWorldOffset;
    Put(world,std::array<float,4>{});
    Check(!feature::GravityPartFrame(*actor,fixture->reader,state,true,100./60)&&pose==published,
        "a failed solve leaves the last corrected pose published");
    Transform(world,100);
    Put(actor->mesh.object+ww_game_compat::ComponentPoseBuffer1Offset,head);
    Put(actor->mesh.object+ww_game_compat::ComponentPosePreviousIndexOffset,int32_t(1));
    Check(feature::GravityPartFrame(*actor,fixture->reader,state,true,100./60)&&
        pose[anchor]==original[anchor]&&pose[right]==original[right]&&
        !actor->backups[0].valid&&actor->backups[1].valid,
        "an aliased buffer retains one restoration ledger and preserves unowned bones");
    Put(actor->mesh.object+ww_game_compat::ComponentPosePreviousIndexOffset,int32_t(0));
    opposite.strength=980;opposite.sequence=33;
    Check(play::part_gravity::CoversGroup(actor->gravity_parts,shared_group,2)&&
        !play::part_gravity::CoversGroup(actor->gravity_parts,shared_group,3),
        "only fully matched native groups give precedence to their active parts");
    for(int step=100;step<200;++step)
        if(!feature::GravityPartFrame(*actor,fixture->reader,state,true,double(step)/60))
            Check(false,"both part gravity frames publish");
    Check(part.moved_bones==11&&opposite.moved_bones==11&&sub::detail::Position(pose[right])[2]<-4,
        "both sleeves independently contribute to the same published frame");
    for(uint32_t i=0;i<names.size();++i)
        if((!part.rig.particles[i].enabled||part.rig.particles[i].pinned)&&
           (!opposite.rig.particles[i].enabled||opposite.rig.particles[i].pinned))
            Check(pose[i]==original[i],"both sleeves preserve their attachment transforms and the rest of the pose");
    const auto previous=sub::detail::Rotation(pose[edge]);
    const auto abrupt=Quat::AxisAngle({1,0,0},2.8f);
    Put(uint64_t(pose[edge].data()),std::array<float,4>{abrupt.x,abrupt.y,abrupt.z,abrupt.w});
    Check(feature::GravityPartFrame(*actor,fixture->reader,state,true,200./60),"publishes through an abrupt input rotation");
    const auto current=sub::detail::Rotation(pose[edge]);
    Check(std::abs(std::inner_product(previous.begin(),previous.end(),current.begin(),0.))>=std::cos(.0751),
        "a part's published rotation cannot flip in one 60 Hz frame");
    // A fresh native frame retires the abrupt input used for this check.
    pose[edge]=original[edge];
    const auto previous_publication=pose;
    auto next_pose=original;
    const auto next_head=publish_observer::ArrayHead{uint64_t(next_pose.data()),int32_t(names.size()),int32_t(names.size())};
    Put(actor->mesh.object+ww_game_compat::ComponentPoseBuffer1Offset,next_head);
    Put(actor->mesh.object+ww_game_compat::ComponentPosePreviousIndexOffset,int32_t(1));
    part.strength=opposite.strength=120;
    state.seconds=5; // seek must publish the constrained pose immediately
    Check(feature::GravityPartFrame(*actor,fixture->reader,state,true,201./60)&&
        sub::detail::Position(next_pose[first])[2]<-2&&sub::detail::Position(next_pose[right])[2]<-2,
        "low gravity keeps both sleeves down on the first published frame after a seek");
    Check(pose==previous_publication,"processing the next buffer preserves the previous corrected frame");
    part.strength=-1;part.sequence=34;
    Check(feature::GravityPartFrame(*actor,fixture->reader,state,true,202./60)&&part.sampled==34&&
        next_pose[first]==original[first]&&next_pose[edge]==original[edge]&&next_pose[right]!=original[right],
        "disabling one part restores its native pose while the opposite sleeve keeps settling");
    part.strength=opposite.strength=-1;part.sequence=opposite.sequence=34;
    Check(feature::GravityPartFrame(*actor,fixture->reader,state,true,4)&&part.sampled==34&&opposite.sampled==34&&
        pose==original&&next_pose==original&&!feature::ActorPending(*actor),"turning off both parts restores both original buffers");
    Put(actor->mesh.object+ww_game_compat::ComponentPosePreviousIndexOffset,int32_t(0));
    Put(actor->mesh.object+ww_game_compat::ComponentPoseReadIndexOffset,int32_t(1));
    part.strength=60;part.sequence=35;
    Check(feature::GravityPartFrame(*actor,fixture->reader,state,true,5,true)&&
        part.sampled==35&&next_pose[first]!=original[first]&&pose==original,
        "before publication gravity updates only the ready input and preserves render history");
    Check(feature::GravityPartFrame(*actor,fixture->reader,state,false,6,true)&&next_pose==original,
        "stop restores the pending publication input");
    Put(actor->mesh.object+ww_game_compat::ComponentPoseBuffer1Offset,head);
    Check(!feature::GravityPartFrame(*actor,fixture->reader,state,true,7,true)&&pose==original,
        "before publication gravity does not write through an aliased history buffer");
}
static void PartGravityCatalogTest(const std::filesystem::path& data) {
    const auto rows=Json::parse(std::ifstream(data/"characters"/"part_gravity.json")).at("profiles");
    for(auto it=rows.begin();it!=rows.end();++it){
        const auto profile=Json::parse(std::ifstream(data/"characters"/"profiles"/(it.key()+".json")));
        const auto names=profile.at("bone_names").get<std::vector<std::string>>();
        const auto parents=profile.at("bone_parents").get<std::vector<int32_t>>();
        std::vector<play::part_gravity::Part> parts;
        play::part_gravity::Prepare(it.value(),names,parents.data(),parts);
        Check(parts.size()==it.value().size(),"all catalog parts bind to the exported skeleton");
        for(const auto& part:parts){
            const bool left=part.root.find("_L")!=std::string::npos;
            bool confined=true;
            for(uint32_t i=0;i<part.rig.count;++i)
                if(part.rig.particles[i].enabled&&!part.rig.particles[i].pinned&&
                    (names[i].find("Bip001")==0||names[i].find(left?"_R":"_L")!=std::string::npos))confined=false;
            Check(confined,"catalog parts never deform body bones or the opposite side");
        }
    }
}
int wmain(int argc,wchar_t** argv){
    auto fixture=std::make_unique<Fixture>();DirectTest(*fixture);FaceTest(*fixture);GazeTest();CameraTest(*fixture);FormationCameraTest(*fixture);CameraCallbackTest(*fixture);CameraTrackHeightTest(*fixture);
    Check(argc==3,"expected the repository root and build directory");
    const auto data=std::filesystem::path(argv[1])/L"data";
    ClothingProfilesTest(data);
    PartContactTurnLimitTest();
    SelectedPartGravityTest(data);
    PartGravityCatalogTest(data);
    std::cout<<assertions<<" feature assertions passed; game_accessed=false; hooks_installed=false\n";}
