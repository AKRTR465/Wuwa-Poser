// Three-person quiet stage: two selected inactive residents, native main player.
// Commands are filesystem flags; never synthesizes input or controls the UI.
#include "object_identity.h"
#include "stage_capture_readonly.h"
#include "../../playback/ww_stage_residency.h"
#include "vmt_slot_patch.h"
#include "scene_team_stage_runtime.h"
#include <atomic>
#ifdef WWP_MMD_SQUAD
#include "../../playback/vmd_local_clip.h"
#include "../../playback/ww_mmd_squad.h"
#include "../../playback/ww_mmd_placement.h"
#include "../../engine_ue4/pose_full_body.h"
#include "shared_native_physics.h"
#include "native_target_publication.h"
#include "unified_player_features.h"
#endif

namespace stage_host {
namespace q=ww_object;
namespace st=wwp_playback::ww_stage_residency;
static HMODULE self=nullptr;static volatile LONG started=0;
using CameraFn=void(*)(void*,float);
using HiddenFn=void(*)(void*,bool);
using VisibilityFn=void(*)(void*,bool,uint8_t);
using LocationFn=bool(*)(void*,const float*,bool,void*,bool);
static CameraFn original_camera=nullptr;
static HiddenFn set_actor_hidden=nullptr;
static VisibilityFn set_visible=nullptr,set_mesh_hidden=nullptr;
static LocationFn set_location=nullptr;
static Go objects;static Pool pool;static q::Identity controller,pcm,active_actor,active_root;
static uint64_t image=0,session_id=0,active_address=0;
static unsigned publish_calls_this_callback=0;
static constexpr DWORD kStartCommandPollMs=20,kWaitingStatusIntervalMs=500;
static std::atomic<bool> scene_ready{false};
static DWORD window_thread=0;static std::array<float,3> active_origin{};
static std::atomic<bool> observing{false},publishing{false},pending{false},fault{false};
static std::atomic<unsigned> callbacks{0},in_callback{0},stage_frames{0},last_error{0},last_member{0};
static std::atomic<unsigned> first_fault{0};
static std::atomic<uint64_t> native_calls{0};static std::atomic_flag busy=ATOMIC_FLAG_INIT;
struct UnlockBusy {bool held=true;~UnlockBusy(){if(held)busy.clear(std::memory_order_release);}void Release(){busy.clear(std::memory_order_release);held=false;}};
static std::atomic<uint64_t> residency_started_tick{0},residency_stopped_tick{0},resident_readback_frames{0};
static v20_vmt_slot_patch::State hook;
struct Resident {q::Identity actor,mesh,root,asset,skeleton;st::Ledger ledger;st::Desired desired;st::Values latest;};
static std::array<Resident,2> residents;
static std::array<Resident,3> scene_members;
#ifdef WWP_MMD_SQUAD
static bool SquadPending();
static bool SquadMemberEnabled(unsigned);
static void SquadPrepare(const Json&,const std::filesystem::path&);
static void SquadInitialize(Reader&,const Json&);
static void SquadInstall();
static void SquadCamera(Reader&);
static void SquadPoll(const std::filesystem::path&);
static void SquadReport(Json&);
static bool SquadCleanup(Json&);
static void SquadExportTargets();
static void SquadPrepareNative();
static void SquadStopNative();
static bool SquadNeedsNative();
#endif
static void Camera(void* object,float delta);
static bool LedgerPending(){return st::Pending(residents[0].ledger)||st::Pending(residents[1].ledger)
#ifdef WWP_MMD_SQUAD
    ||SquadPending()
#endif
    ;}
static void Issue(st::Code code,bool fatal){
    last_error.store(unsigned(code));
    if(fatal){unsigned empty=0;first_fault.compare_exchange_strong(empty,unsigned(code));fault.store(true);publishing.store(false);
#ifdef WWP_MMD_SQUAD
        SquadStopNative();
#endif
    }
}
static const char* PublicationStatus(){return resident_readback_frames.load()?"resident":"starting";}
static bool SameNow(Reader& r,const q::Identity& expected){q::Identity actual;return q::ReadStable(r,objects,expected.object,&actual)&&q::Same(expected,actual);}
static st::ReadValue ReadField(Reader& r,const Resident& m,st::Field f){
    st::ReadValue v;
    if(f==st::Field::RootPosition)v.read=r.Read(m.root.object+0x1f0,v.value.position.data(),12);
    else{const uint64_t object=f==st::Field::ActorHidden?m.actor.object:m.mesh.object;
        const uint32_t off=f==st::Field::ActorHidden?0x60:f==st::Field::MeshVisible?0x16e:0x16f;
        const uint8_t mask=f==st::Field::MeshHiddenInGame?16:32;uint8_t a=0;
        v.read=r.Read(object+off,&a,1);v.value.flag=(a&mask)!=0;
    }return v;
}
static st::Values Values(Reader& r,const Resident& m){st::Values v;for(unsigned i=0;i<st::kFieldCount;++i)v[i]=ReadField(r,m,st::Field(i));return v;}
static bool Native(Resident& m,const st::WriteRequest& request){
    ++native_calls;
    __try{
        switch(request.field){
        case st::Field::ActorHidden:set_actor_hidden((void*)m.actor.object,request.desired.flag);break;
        case st::Field::MeshVisible:set_visible((void*)m.mesh.object,request.desired.flag,0);break;
        case st::Field::MeshHiddenInGame:set_mesh_hidden((void*)m.mesh.object,request.desired.flag,0);break;
        case st::Field::RootPosition:return set_location((void*)m.actor.object,request.desired.position.data(),false,nullptr,true);
        }
        return true;
    }__except(EXCEPTION_EXECUTE_HANDLER){return false;}
}
static void Execute(Reader& r,Resident& m,const st::Plan& plan,bool publish){
    // Place first, then expose the actor, so an actor is not revealed at an old
    // combat position. Restore keeps the same deterministic ordering.
    for(unsigned slot:std::array<unsigned,4>{3,1,2,0})for(unsigned n=0;n<plan.count;++n){const auto& request=plan.writes[n];
        if(st::Index(request.field)!=slot)continue;
        if(publish&&(!publishing.load()||publish_calls_this_callback>=1))return;
        if(publish){uint8_t suppression=0;
            if(!r.Get(m.actor.object+0x61,suppression)||(suppression&1u)){Issue(st::Code::ValueUnknown,true);return;}}
        st::MarkAttempt(&m.ledger,request);pending.store(true);
        if(publish)++publish_calls_this_callback;
        const bool called=Native(m,request);
        if(st::Confirm(&m.ledger,request,ReadField(r,m,request.field),called)!=st::Code::Ok||!called){Issue(st::Code::ReadbackFailed,true);return;}
    }
}
static void Camera(void* object,float delta){
    struct Count{Count(){++in_callback;}~Count(){--in_callback;}} count;
    original_camera(object,delta);
    if(!observing.load(std::memory_order_acquire)||uint64_t(object)!=pcm.object)return;
    ++callbacks;if(GetCurrentThreadId()!=window_thread){Issue(st::Code::WrongThread,false);return;}
    if(busy.test_and_set(std::memory_order_acquire))return;
    struct Release{~Release(){pending.store(LedgerPending());busy.clear(std::memory_order_release);}} release;
    try{
        Reader r{GetCurrentProcess()};uint64_t pawn=0;publish_calls_this_callback=0;std::array<float,3> position{};
        if(!r.Get(controller.object+0x2e8,pawn)||pawn!=active_address||!r.Read(active_root.object+0x1f0,position.data(),12))publishing.store(false);
        for(unsigned k=0;k<3;++k)if(!(std::fabs(position[k]-active_origin[k])<=30))publishing.store(false);
#ifdef WWP_MMD_SQUAD
        SquadCamera(r);
        // Every body ledger drains before any stage visibility/location restore.
        if(!publishing.load()&&SquadPending())return;
#endif
        for(unsigned i=0;i<2;++i){auto& m=residents[i];last_member.store(i);const bool publish=publishing.load();
            bool selected=false;for(bool enabled:m.desired.enabled)selected=selected||enabled;
            if(publish?!selected:(!st::Pending(m.ledger)||m.actor.object==pawn))continue;
            if(!SameNow(r,m.actor)||!SameNow(r,m.mesh)||!SameNow(r,m.root)){
                if(publish)Issue(st::Code::IdentityChanged,true);else m.ledger.entries={};continue;}
            m.latest=Values(r,m);st::Plan plan;
            if(publish)st::PlanPublish(m.latest,m.desired,&m.ledger,&plan);else st::PlanRestore(m.latest,&m.ledger,&plan);
            Execute(r,m,plan,publish);m.latest=Values(r,m);
        }
        bool desired=publishing.load();
        for(const auto& m:residents)for(unsigned k=0;k<st::kFieldCount;++k)
            if(m.desired.enabled[k])desired=desired&&m.ledger.bound&&m.latest[k].read&&st::MatchesDesired(st::Field(k),m.latest[k].value,m.desired.values[k]);
        if(desired)++resident_readback_frames;
        pending.store(LedgerPending());++stage_frames;
    }catch(...){Issue(st::Code::UnknownIdentity,true);}
}
static Resident Member(Reader& r,const Json& row){
    Resident m;m.actor=stage_capture::Fresh(r,objects,stage_capture::FromJson(row.at("actor")));
    m.mesh=stage_capture::Fresh(r,objects,stage_capture::FromJson(row.at("mesh")));m.root=stage_capture::Fresh(r,objects,stage_capture::FromJson(row.at("root")));
    uint64_t asset=0,skeleton=0;
    scene_team_stage::Require(r.Get(m.mesh.object+0x5d0,asset)&&q::ReadStable(r,objects,asset,&m.asset)&&
        r.Get(asset+0xd8,skeleton)&&q::ReadStable(r,objects,skeleton,&m.skeleton),"scene member skeleton unavailable");
    return m;
}
static void InitializeScene(Reader& r,const Json& cfg){
    const auto& seed=cfg.at("scene_team_preflight");
    controller=stage_capture::Fresh(r,objects,stage_capture::FromJson(seed.at("controller")));
    pcm=stage_capture::Fresh(r,objects,stage_capture::FromJson(seed.at("pcm"),false));
    uint64_t camera=0;scene_team_stage::Require(r.Get(controller.object+0x2e8,active_address)&&r.Get(controller.object+0x360,camera)&&camera==pcm.object,"scene control/camera changed since scan");
    const size_t activeIndex=seed.at("active_member_index");
    for(size_t i=0;i<3;++i)scene_members[i]=Member(r,seed.at("members").at(i));
    scene_team_stage::Require(scene_members[activeIndex].actor.object==active_address,"scene current actor changed since scan");
    active_actor=scene_members[activeIndex].actor;active_root=scene_members[activeIndex].root;
    const auto anchor=stage_capture::World(r,active_root);const auto anchorMesh=stage_capture::World(r,scene_members[activeIndex].mesh).at("translation").get<std::array<float,3>>();
    active_origin=anchor.at("translation").get<std::array<float,3>>();
    const auto rotation=anchor.at("rotation_xyzw").get<std::array<float,4>>();
    float dx=2*(rotation[0]*rotation[1]-rotation[3]*rotation[2]),dy=1-2*(rotation[0]*rotation[0]+rotation[2]*rotation[2]);
    const float length=std::sqrt(dx*dx+dy*dy);scene_team_stage::Require(length>=.5f,"scene anchor is not upright");dx/=length;dy/=length;
    const float spacing=cfg.at("spacing_cm");
    unsigned slot=0;
    for(size_t i=0;i<3;++i){if(i==activeIndex)continue;
        auto& m=residents[slot];m=scene_members[i];
        m.desired.enabled.fill(true);m.desired.values[0]=st::Value::Flag(false);m.desired.values[1]=st::Value::Flag(true);m.desired.values[2]=st::Value::Flag(false);
#ifdef WWP_MMD_SQUAD
        m.desired.enabled.fill(SquadMemberEnabled(unsigned(i)));
#endif
        const auto initialRoot=stage_capture::World(r,m.root).at("translation").get<std::array<float,3>>(),initialMesh=stage_capture::World(r,m.mesh).at("translation").get<std::array<float,3>>();
        const float side=slot==0?-spacing:spacing;
        m.desired.values[3]=st::Value::Position(active_origin[0]+dx*side,active_origin[1]+dy*side,initialRoot[2]+anchorMesh[2]-initialMesh[2]);++slot;
    }
#ifdef WWP_MMD_SQUAD
    SquadInitialize(r,seed);
#endif
}
static Json Report(const char* status){
    const uint64_t now=GetTickCount64(),startedTick=residency_started_tick.load(),stoppedTick=residency_stopped_tick.load();
    Json j={{"schema",1},{"status",status},{"pid",GetCurrentProcessId()},{"stage_frames",stage_frames.load()},
        {"heartbeat_tick_ms",now},{"session_id",session_id},{"residency_started_tick_ms",startedTick},
        {"residency_elapsed_ms",startedTick?(stoppedTick?stoppedTick:now)-startedTick:0},
        {"resident_readback_frames",resident_readback_frames.load()},
        {"camera_callbacks",callbacks.load()},{"window_thread",window_thread},{"native_calls",native_calls.load()},
        {"publishing",publishing.load()},{"pending_restore",pending.load()},{"fault",fault.load()},
        {"last_error",st::Name(st::Code(last_error.load()))},{"first_fault",st::Name(st::Code(first_fault.load()))},{"last_member",last_member.load()},
        {"scene_initialized",scene_ready.load()},{"csharp_initial_stable_frames",scene_ready.load()?2:0},{"residents",Json::array()}};
    if(!busy.test_and_set(std::memory_order_acquire)){
        UnlockBusy unlock;
        const auto copy=residents;
#ifdef WWP_MMD_SQUAD
        SquadReport(j);
#endif
        unlock.Release();
        for(const auto& m:copy){Json fields=Json::array();for(unsigned k=0;k<4;++k){const auto& e=m.ledger.entries[k];
            fields.push_back({{"field",k},{"pending",e.pending},{"captured",e.captured},
                {"original_flag",e.original.flag},{"last_written_flag",e.last_written.flag},{"current_flag",m.latest[k].value.flag},
                {"original_position",e.original.position},{"last_written_position",e.last_written.position},{"current_position",m.latest[k].value.position}});}
            j["residents"].push_back({{"actor",Hex(m.actor.object)},{"actor_index",m.actor.index},{"mesh",Hex(m.mesh.object)},
                {"attempts",m.ledger.attempts},{"confirmed_writes",m.ledger.confirmed_writes},{"confirmed_restores",m.ledger.confirmed_restores},
                {"preserved_native_changes",m.ledger.preserved_native_changes},{"fields",fields}});}
    }return j;
}
static void Save(const std::filesystem::path& p,const Json& j){ww_report::Save(p,j);}
static bool HasFlag(const std::filesystem::path& dir,const wchar_t* flag){return GetFileAttributesW((dir/flag).c_str())!=INVALID_FILE_ATTRIBUTES;}
static bool InspectPending(bool* out){
    if(busy.test_and_set(std::memory_order_acquire))return false;
    *out=LedgerPending();pending.store(*out);busy.clear(std::memory_order_release);return true;
}
static bool CleanupComplete(const v20_vmt_slot_patch::State& state,v20_vmt_slot_patch::Code code,bool quiescent){
    return !state.changed&&!state.protection_pending&&quiescent&&
        (code==v20_vmt_slot_patch::Code::Ok||code==v20_vmt_slot_patch::Code::NotInstalled);
}
#ifdef WWP_MMD_SQUAD
#include "mmd_squad_runtime.h"
#endif
static DWORD WINAPI Worker(LPVOID){
    session_id=(std::max)(uint64_t(1),uint64_t(GetTickCount64()));
    wchar_t own[32768]{};GetModuleFileNameW(self,own,32768);const auto dir=std::filesystem::path(own).parent_path();Json result;
    try{
        std::filesystem::create_directories(dir/L"logs");Save(dir/L"logs/carrier_status.json",{{"status","preparing_stage"},{"pid",GetCurrentProcessId()},{"session_id",session_id}});
        std::ifstream stream(dir/L"stage_config.json");Json cfg;if(!stream||!(stream>>cfg))throw std::runtime_error("stage_config.json missing");
#ifdef WWP_MMD_SQUAD
        // Transport owns the editable session limit, including pauses and
        // unlimited duration. Do not leave a second immutable wall-clock cap.
        const uint64_t residencyDuration=0;
#else
        const uint64_t residencyDuration=cfg.at("residency_duration_ms");
#endif
        image=uint64_t(GetModuleHandleW(nullptr));Reader r{GetCurrentProcess()};
        if(!ValidateGo(r,image+q::Rva(cfg.at("gobjects_rva")),objects)||!ValidatePool(r,image+q::Rva(cfg.at("name_pool_rva")),pool))throw std::runtime_error("stage anchors differ");
        DWORD windowPid=0;window_thread=GetWindowThreadProcessId((HWND)q::Rva(cfg.at("game_window_handle")),&windowPid);
        if(!window_thread||windowPid!=GetCurrentProcessId())throw std::runtime_error("game window/thread owner differs");
        set_actor_hidden=(HiddenFn)(image+0x4d995a0);set_visible=(VisibilityFn)(image+0x500bc20);set_mesh_hidden=(VisibilityFn)(image+0x5009450);set_location=(LocationFn)(image+0x4d92370);
#ifdef WWP_MMD_SQUAD
        SquadPrepare(cfg,dir);
#endif
        InitializeScene(r,cfg);
#ifdef WWP_MMD_SQUAD
        SquadInstall();
#endif
        original_camera=(CameraFn)(image+0x542faa0);
        const auto installed=v20_vmt_slot_patch::Install(hook,(void**)(pcm.vtable+0x790),(void*)original_camera,(void*)&Camera);
        if(installed.code!=v20_vmt_slot_patch::Code::Ok)throw std::runtime_error("stage camera dispatch install failed");
        observing.store(true);scene_ready.store(true,std::memory_order_release);const uint64_t readyStart=GetTickCount64();
#ifdef WWP_MMD_SQUAD
        if(SquadNeedsNative()){
            SquadExportTargets();
            const auto nativeReadyDeadline=GetTickCount64()+90000;
            while(!HasFlag(dir,L"native_inputs_ready.flag")&&!HasFlag(dir,L"stop.flag")&&!fault.load()&&GetTickCount64()<nativeReadyDeadline)Sleep(20);
            scene_team_stage::Require(HasFlag(dir,L"native_inputs_ready.flag")&&!HasFlag(dir,L"stop.flag")&&!fault.load(),"squad native graph preparation failed");
            SquadPrepareNative();
        }
#endif
        Save(dir/L"stage_status.json",Report("ready_waiting_start"));
        bool startRequested=false;
        uint64_t lastWaitingStatus=GetTickCount64();
        while(GetTickCount64()-readyStart<300000&&!HasFlag(dir,L"stop.flag")&&!fault.load()){
            if(HasFlag(dir,L"start.flag")){startRequested=true;break;}
            const uint64_t now=GetTickCount64();
            if(now-lastWaitingStatus>=kWaitingStatusIntervalMs){Save(dir/L"stage_status.json",Report("ready_waiting_start"));lastWaitingStatus=now;}
            Sleep(kStartCommandPollMs);
        }
        std::string endReason=HasFlag(dir,L"stop.flag")?"cancelled_before_start":"ready_timeout";
        if(fault.load())throw std::runtime_error("stage authority failed before start");
        if(startRequested&&!HasFlag(dir,L"stop.flag")){
            const uint64_t begin=GetTickCount64();residency_started_tick.store(begin);
            publishing.store(true);
            while((!residencyDuration||GetTickCount64()-begin<residencyDuration)&&publishing.load()&&!fault.load()&&!HasFlag(dir,L"stop.flag")){
#ifdef WWP_MMD_SQUAD
                SquadPoll(dir);
#endif
                Save(dir/L"stage_status.json",Report(PublicationStatus()));
                Sleep(100);
            }
            endReason=HasFlag(dir,L"stop.flag")?"user_stop":fault.load()?"runtime_fault":residencyDuration&&GetTickCount64()-begin>=residencyDuration?"duration_elapsed":"transport_stopped_or_authority_changed";
            publishing.store(false);residency_stopped_tick.store(GetTickCount64());
        }
        publishing.store(false);const uint64_t restoreStart=GetTickCount64();Sleep(100);
#ifdef WWP_MMD_SQUAD
        SquadStopNative();
#endif
        bool actual=true;
        while(!InspectPending(&actual)||actual){
            Save(dir/L"stage_status.json",Report(GetTickCount64()-restoreStart<8000?"restoring":"restore_pending"));Sleep(250);
        }
        result=Report("finished");result["end_reason"]=endReason;
    }catch(const std::exception& e){publishing.store(false);Sleep(100);result=Report("error");result["reason"]=e.what();}
    publishing.store(false);
#ifdef WWP_MMD_SQUAD
    SquadStopNative();
#endif
    if(residency_started_tick.load()&&!residency_stopped_tick.load())residency_stopped_tick.store(GetTickCount64());
    const bool workerFailed=fault.load()||result.value("status",std::string())=="error";
    const std::string endReason=result.value("end_reason",workerFailed?std::string("worker_error"):std::string("stopped"));
    const std::string reason=result.value("reason",std::string());
    // Keep cleanup alive until all owned state and the callback dispatch are
    // settled. A drained ledger alone does not prove the hook was restored.
    for(;;){
        bool inspected=false,actualPending=true;
        if(!in_callback.load()&&!busy.test_and_set(std::memory_order_acquire)){
            actualPending=LedgerPending();pending.store(actualPending);
            if(!actualPending)observing.store(false);
            busy.clear(std::memory_order_release);inspected=true;
        }
        result=Report(inspected&&!actualPending?"cleanup_pending":"restore_pending");
        result["end_reason"]=endReason;if(!reason.empty())result["reason"]=reason;
        result["cleanup_complete"]=false;result["restore_service_retained"]=true;
        if(inspected&&!actualPending){
            const auto restored=v20_vmt_slot_patch::Restore(hook);
            result["hook_restore"]={{"code",v20_vmt_slot_patch::Name(restored.code)},{"changed",hook.changed},{"protection_pending",hook.protection_pending}};
            const bool quiescent=in_callback.load()==0;result["callbacks_quiescent"]=quiescent;
            if(CleanupComplete(hook,restored.code,quiescent)
#ifdef WWP_MMD_SQUAD
                &&(!HasFlag(dir,L"native_observation_started.flag")||HasFlag(dir,L"native_observers_restored.flag"))
                &&SquadCleanup(result)
#endif
                ){
                result["status"]=workerFailed?"error":"finished";
                result["cleanup_complete"]=true;result["restore_service_retained"]=false;break;
            }
        }
        ww_report::Write(dir/L"stage_status.json",result);
        Sleep(500);
    }
    result["pending_restore"]=pending.load();
    try{Save(dir/L"stage_result.json",result);Save(dir/L"stage_status.json",result);}catch(...){return ERROR_WRITE_FAULT;}
    return 0;
}
} // namespace stage_host

extern "C" __declspec(dllexport) LRESULT CALLBACK HookProc(int code,WPARAM w,LPARAM l){
    using namespace stage_host;
    if(code>=0&&InterlockedCompareExchange(&started,1,0)==0){wchar_t host[32768]{};GetModuleFileNameW(nullptr,host,32768);
        if(!_wcsicmp(std::filesystem::path(host).filename().c_str(),L"Client-Win64-Shipping.exe")){
            HMODULE pin=nullptr;if(GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS|GET_MODULE_HANDLE_EX_FLAG_PIN,(LPCWSTR)&HookProc,&pin)){
                HANDLE t=CreateThread(nullptr,0,Worker,nullptr,0,nullptr);if(t)CloseHandle(t);}}}
    return CallNextHookEx(nullptr,code,w,l);
}
BOOL WINAPI DllMain(HINSTANCE module,DWORD reason,LPVOID){if(reason==DLL_PROCESS_ATTACH){stage_host::self=module;DisableThreadLibraryCalls(module);}return TRUE;}
