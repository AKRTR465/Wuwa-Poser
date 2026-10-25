#include "n1_common.h"
#include "mono_owned_public_facade.h"
#include "owned_mmd_body.h"
#include "owned_recovery.h"
#include <atomic>
#include <functional>

namespace owned_carrier {
using namespace ui_n1_host;
namespace owned=mono_owned;
// Single-flight GameThread dispatch: the hook runs at most one requested action.
class Gate {
    static constexpr unsigned kBusy=1,kClosed=2;std::atomic<unsigned> flags_{0};
public:
    bool TryEnter(){unsigned expected=0;return flags_.compare_exchange_strong(expected,kBusy,std::memory_order_acq_rel);}
    void Leave(){flags_.fetch_and(~kBusy,std::memory_order_release);}
    void Close(){flags_.fetch_or(kClosed,std::memory_order_acq_rel);}
    bool Busy()const{return (flags_.load(std::memory_order_acquire)&kBusy)!=0;}
};
static HMODULE self=nullptr;
static volatile LONG started=0;
static std::atomic<uint32_t> request{0},completed{0};
static std::atomic<bool> calls_ready{false};
static Gate gate;
static Config config;
static owned::Action requested_action=owned::Action::BeginTarget;
static bool completed_ok=false;
static std::string completed_reason;
static owned::Bridge bridge;
static std::filesystem::path caller_directory;
static std::string caller_helper;
static bool Cancelled(){return GetFileAttributesW((ModulePath(self).parent_path()/L"stop.flag").c_str())!=INVALID_FILE_ATTRIBUTES;}
static void CallOwned(owned::Action action){
        // Environment is process-wide. Bind it on the serialized GameThread
        // for every operation, so a rejected second session cannot strand the
        // first session's pending Stop/CheckReleased operations.
    util::Require(SetEnvironmentVariableW(L"WWPOSER_OWNED_SESSION_DIR",caller_directory.c_str())!=0,"actor session environment binding failed");
    bridge.Run(caller_helper,action);
}
static bool Invoke(owned::Action action){
    try{CallOwned(action);completed_reason.clear();return true;}
    catch(const std::exception& error){completed_reason=error.what();return false;}
}
static bool MaintainScene(void*){
    // Camera callbacks and explicit managed actions share the same gate. A
    // reentrant camera update during a managed call must not enter Mono again.
    if(!calls_ready.load(std::memory_order_acquire)||!gate.TryEnter())return true;
    struct Leave {~Leave(){gate.Leave();}} leave;
    try{CallOwned(owned::Action::MaintainStage);return true;}catch(...){return false;}
}
static DWORD WINAPI Worker(LPVOID){
    const auto dir=ModulePath(self).parent_path();const auto trial_start=GetTickCount64();
    caller_directory=dir;caller_helper=(dir/kHelper).u8string();
    Json out=Baseline(config);
    bool began=false,loaded=false,shown=false,mmd_started=false,stopped=false,released=false,mmd_restored=false,display_completed=false,preview=false;
    uint64_t display_started=0,display_elapsed=0,display_requested=0;
    std::function<void()> recoveryHeartbeat;
    const auto dispatch=[&](owned::Action action)->bool{
        requested_action=action;const auto current=request.load()+1;request.store(current,std::memory_order_release);
        auto heartbeat=GetTickCount64()+1000;
        while(completed.load(std::memory_order_acquire)!=current){
            PostMessageW(config.window,WM_NULL,0,0);
            if(recoveryHeartbeat&&GetTickCount64()>=heartbeat){recoveryHeartbeat();heartbeat=GetTickCount64()+1000;}
            Sleep(10);
        }
        return completed_ok;
    };
    const auto run=[&](owned::Action action){if(!dispatch(action))throw std::runtime_error(std::string(owned::kActionNames[static_cast<size_t>(action)])+": "+completed_reason);};
    try{
        const auto mmd=ReadJson(dir/L"owned_mmd_config.json");display_requested=mmd.at("duration_ms");
        const auto layout=ReadJson(dir/L"owned_mmd_layout.json");const auto host=reinterpret_cast<Address>(GetModuleHandleW(nullptr));
        util::Require(*reinterpret_cast<const uint32_t*>(host+ww_object::Rva(layout.at("registered_game_thread_id_rva")))==config.thread&&
            *reinterpret_cast<const uint8_t*>(host+ww_object::Rva(layout.at("registered_game_thread_initialized_rva")))==1,"selected window thread is not registered GameThread");
        calls_ready.store(true,std::memory_order_release);
        run(owned::Action::BeginTarget);began=bridge.BeginEntered();
        while(!(loaded=dispatch(owned::Action::CheckLoaded))&&!Cancelled())Sleep(1000);
        util::Require(loaded&&!Cancelled(),"owned role did not finish loading");
        run(owned::Action::ShowAtLocal);shown=true;
        auto shownStatus=Baseline(config);shownStatus["show_returned"]=true;ww_report::Save(dir/L"owned_display_shown.json",shownStatus);
        run(owned::Action::CheckLoaded);
        preview=mmd.value("preview",false);
        // A stage preview only stands the actors at their start transforms:
        // no clip, expression, camera, physics or publication hook is installed.
        if(preview){
            const auto count=mmd.at("actors").size();display_started=GetTickCount64();
            while(!Cancelled()){
                Json state={{"schema",3},{"preview",true},{"mode","preview"},{"ready",true},{"session_active",true},{"actor_count",count},{"borrowed_count",0},{"created_count",count},
                    {"seconds",0.0},{"duration_seconds",0.0},{"message","站位预览中：演员已站到设定位置，不播放动作。"}};
                ww_report::Write(dir/L"mmd_status.json",state);ww_report::Write(dir/L"player_status.json",state);Sleep(250);
            }
            display_elapsed=GetTickCount64()-display_started;
        }else{
            const auto legacyProfile=mmd.contains("actors")?Json::object():ReadJson(dir/L"character_profile.json");
            owned_mmd_body::Initialize(dir,ReadJson(dir/L"owned_mmd_graph.json"),mmd,legacyProfile,layout,host,reinterpret_cast<uint64_t>(config.window));
            util::Require(owned_mmd_body::InstallSceneMaintenance(&MaintainScene,nullptr),"scene maintenance camera installation failed");
            if(owned_mmd_body::NeedsNative()){
                const auto readyFlag=dir/(mmd.contains("actors")?L"native_inputs_ready.flag":L"native_input_ready.flag");
                while(!std::filesystem::exists(readyFlag)&&!Cancelled()&&!owned_mmd_body::Failed())Sleep(20);
                util::Require(!Cancelled()&&!owned_mmd_body::Failed()&&std::filesystem::exists(readyFlag),"scene native graph preparation incomplete");
                owned_mmd_body::PrepareNative();
            }
            util::Require(owned_mmd_body::Install(),"owned MMD publication installation failed");mmd_started=true;
            while(!owned_mmd_body::Ready()&&!owned_mmd_body::Failed()&&!Cancelled())Sleep(10);
            util::Require(owned_mmd_body::Ready(),"owned MMD publication preflight failed");
            display_started=GetTickCount64();owned_mmd_body::Start(display_started);
            auto next_status=display_started;
            while(!owned_mmd_body::Finished()&&!Cancelled()&&!owned_mmd_body::Failed()){
                owned_mmd_body::PollCommands();owned_mmd_body::CheckTimeLimit();
                if(GetTickCount64()>=next_status){const auto state=owned_mmd_body::Status();ww_report::Save(dir/L"mmd_status.json",state);ww_report::Save(dir/L"player_status.json",state);next_status+=250;}Sleep(10);
            }
            display_elapsed=GetTickCount64()-display_started;
            const auto finalState=owned_mmd_body::Status();display_requested=uint64_t(finalState.value("time_limit_seconds",0.0)*1000.0);
            display_completed=finalState.value("time_limit_reached",false)&&!owned_mmd_body::Failed();
        }
    }catch(const std::exception& error){out["reason"]=error.what();}
    began=bridge.BeginEntered();
    // Keep the WH_GETMESSAGE owner and managed dispatch alive throughout
    // recovery. A terminal file would make the installer remove that route.
    owned_recovery::Progress recovery(began);
    std::string recoveryReason;
    recoveryHeartbeat=[&]{
        try{
            auto status=owned_mmd_body::Status();const auto phase=recovery.Current();
            status["mode"]="stopped";status["status"]="restore_pending";status["phase"]="restore_pending";
            status["session_active"]=false;status["restore_requested"]=true;status["restoration_complete"]=false;
            status["native_restoration_complete"]=recovery.native_restored;status["managed_owner_retirement_checked"]=recovery.managed_released;
            status["restore_service_retained"]=true;status["cleanup_complete"]=false;status["recovery_attempts"]=recovery.attempts;
            status["recovery_reason"]=recoveryReason;
            if(out.contains("reason"))status["failure_reason"]=out.at("reason");
            status["message"]=phase==owned_recovery::Phase::Native?"正在等待身体、表情和镜头恢复；恢复服务仍在运行。":
                phase==owned_recovery::Phase::Observers?"正在等待本次原生观察器退出；角色保留到清理完成。":
                phase==owned_recovery::Phase::ManagedStop?"正在移除本次创建的角色；恢复服务仍在运行。":"正在确认角色已从管理器移除；恢复服务仍在运行。";
            ww_report::Write(dir/L"mmd_status.json",status);ww_report::Write(dir/L"player_status.json",status);
        }catch(...){/* Reporting cannot retire the still-needed recovery owner. */}
    };
    while(!recovery.Complete()){
        recoveryHeartbeat();
        try{
            recovery.Advance(GetTickCount64(),
                [&]{const bool ok=owned_mmd_body::CloseAndRestore(GetTickCount64()+1000);if(!ok)recoveryReason="body, expression, camera, or hook restoration remains pending";return ok;},
                [&]{const bool ok=!std::filesystem::exists(dir/L"native_observation_started.flag")||std::filesystem::exists(dir/L"native_observers_restored.flag");
                    if(!ok)recoveryReason="native observation cleanup remains pending";else owned_mmd_body::RevokeTargets();return ok;},
                [&]{run(owned::Action::Stop);return true;},
                [&]{run(owned::Action::CheckReleased);return true;});
        }catch(const std::exception& error){recoveryReason=error.what();}
        if(!recovery.Complete()){
            recovery.Defer(GetTickCount64());recoveryHeartbeat();
            // One attempt per two seconds, with status heartbeats while idle.
            while(GetTickCount64()<recovery.next_attempt_ms){Sleep(250);recoveryHeartbeat();}
        }
    }
    recoveryHeartbeat={};mmd_restored=recovery.native_restored;stopped=!began||recovery.managed_stopped;released=!began||recovery.managed_released;
    out["recovery_attempts"]=recovery.attempts;out["restore_service_retained"]=false;
    if(!recoveryReason.empty())out["last_recovery_issue"]=recoveryReason;
    gate.Close();calls_ready.store(false,std::memory_order_release);
    while(gate.Busy())Sleep(10);
    auto body=owned_mmd_body::Status();
    body["managed_owner_retirement_checked"]=released;body["cleanup_complete"]=recovery.Complete();
    body["restore_service_retained"]=false;body["recovery_attempts"]=recovery.attempts;
    // A helper already loaded by Mono from an older build waits for this flag.
    auto reuse=Baseline(config);reuse["same_process_reuse_ready"]=mmd_restored&&released;reuse["loaded_verified"]=loaded;reuse["stop_returned"]=stopped;reuse["managed_retired"]=released;
    reuse["callbacks_quiescent"]=body.value("callbacks_quiescent",false);
    try{if(mmd_restored&&released)ww_report::Save(dir/L"owned_reuse_ready.flag",reuse);}catch(const std::exception& error){out["reuse_report_error"]=error.what();}
    const bool native_input_observed=body.value("native_input_frames",uint64_t(0))>0&&body.value("native_input_error",uint64_t(1))==0;
    const bool body_input_observed=body.value("all_actors_observed",false)&&(!body.value("native_physics",true)||native_input_observed);
    const bool full=preview&&!out.contains("reason")?shown&&mmd_restored&&released:
        shown&&mmd_started&&mmd_restored&&released&&!body.value("failed",true)&&body.value("frames",uint64_t(0))>0&&body_input_observed;
    out["preview"]=preview;
    out["status"]=full?"owned_mmd_retirement_recorded":stopped&&mmd_restored?"owned_mmd_recovered":"owned_mmd_cleanup_unverified";
    out["mmd_started"]=mmd_started;out["mmd_restoration_verified"]=mmd_restored;out["mmd_result"]=body;out["native_input_observed"]=native_input_observed;
    out["managed_owner_retirement_checked"]=released;out["loaded_graph_checked"]=loaded;
    out["display_started_tick_ms"]=display_started;out["display_elapsed_ms"]=display_elapsed;out["display_requested_ms"]=display_requested;out["display_interval_completed"]=display_completed;
    out["duration_ms"]=GetTickCount64()-trial_start;
    // Reports retain the final actor counts and evidence after heavy per-session
    // buffers are released. The DLL and original forwarding targets stay pinned.
    body["retired_payload_released"]=false;
    owned_mmd_body::ReleaseRetiredPayload(body);out["mmd_result"]=body;
    // worker_result.json is the installer's signal that dispatch is closed and drained.
    try{ww_report::Save(dir/L"mmd_status.json",body);ww_report::Save(dir/L"player_status.json",body);ww_report::Save(dir/L"mmd_result.json",body);ww_report::Save(dir/L"worker_result.json",out);}catch(...){return ERROR_WRITE_FAULT;}
    return full?ERROR_SUCCESS:ERROR_INVALID_DATA;
}
}
extern "C" __declspec(dllexport) LRESULT CALLBACK HookProc(int code,WPARAM w,LPARAM l){
    using namespace owned_carrier;
    if(code>=0&&InterlockedCompareExchange(&started,1,0)==0){const auto dir=ModulePath(self).parent_path();
        try{config=ParseConfig(ReadJson(dir/L"session_config.json"));
            HMODULE retained=nullptr;util::Require(GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS|GET_MODULE_HANDLE_EX_FLAG_PIN,reinterpret_cast<LPCWSTR>(&HookProc),&retained),"owned carrier PIN failed");
            const auto worker=CreateThread(nullptr,0,Worker,nullptr,0,nullptr);util::Require(worker,"owned worker creation failed");CloseHandle(worker);
        }catch(const std::exception& error){gate.Close();auto failed=Baseline(config);failed["reason"]=error.what();ww_report::Write(dir/L"hook_startup_failed.json",failed);}
    }
    if(code>=0&&calls_ready.load(std::memory_order_acquire)&&gate.TryEnter()){
        const auto current=request.load(std::memory_order_acquire);
        if(completed.load(std::memory_order_acquire)<current){completed_ok=Invoke(requested_action);completed.store(current,std::memory_order_release);}
        gate.Leave();
    }
    return CallNextHookEx(nullptr,code,w,l);
}
BOOL WINAPI DllMain(HINSTANCE module,DWORD reason,LPVOID){if(reason==DLL_PROCESS_ATTACH){owned_carrier::self=module;DisableThreadLibraryCalls(module);}return TRUE;}
