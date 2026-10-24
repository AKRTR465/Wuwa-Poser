// CSharp roster sampling after original camera update. No actor setters,
// inspector listener, module loading, input synthesis or camera-value writes.
#include "csharp_scene_team_sampler_readonly.h"
#include "stage_native_readonly.h"
#include "scene_team_native_readonly.h"
#include "vmt_slot_patch.h"
#include <atomic>

namespace puerts_sampler {
namespace q=ww_object;
namespace patch=v20_vmt_slot_patch;
static HMODULE self=nullptr;static volatile LONG started=0;
using CameraFn=void(*)(void*,float);
static CameraFn original=nullptr;static patch::State hook;
static Go objects;static Pool pool;
static q::Identity controller,local,active,pcm;
static uint64_t image=0;static DWORD window_thread=0;
static uint64_t image_extent=0;
static Json records=Json::array();
static Json native_config;
static csharp_scene_team_sampler::State csharp_state;
static std::atomic<bool> enabled{false},failed{false};
static uint64_t last_engine_frame=0;
static std::atomic<uint32_t> callbacks{0},in_callback{0},sample_count{0};
static std::atomic_flag busy=ATOMIC_FLAG_INIT;
static uint32_t wanted=1,spacing=4;static std::string fault;
static void Require(bool value,const char* message){if(!value)throw std::runtime_error(message);}
static Json CaptureAnchor(Reader& r,const Json& cfg){
    controller=stage_capture::Fresh(r,objects,stage_capture::FromJson(cfg.at("controller_seed")));
    uint64_t pawn=0,camera=0;
    Require(r.Get(controller.object+0x2e8,pawn)&&q::ReadStable(r,objects,pawn,&active),"fresh active actor unavailable");
    q::Identity found;Require(stage_native::LocalControl(r,objects,pool,active,&found,&local),"fresh reciprocal local control absent");
    Require(r.Get(controller.object+0x360,camera)&&q::ReadStable(r,objects,camera,&pcm),"fresh camera manager unavailable");
    uint64_t target=0;Require(r.Get(pcm.vtable+0x790,target)&&target==image+0x542faa0,"camera slot is occupied or differs");
    original=reinterpret_cast<CameraFn>(target);
    DWORD pid=0;window_thread=GetWindowThreadProcessId(reinterpret_cast<HWND>(std::stoull(cfg.at("game_window_handle").get<std::string>(),nullptr,16)),&pid);
    return {{"controller",stage_capture::Token(r,objects,pool,controller)},
        {"local_player",stage_capture::Token(r,objects,pool,local)},{"active_actor",stage_capture::Token(r,objects,pool,active)},
        {"pcm",stage_capture::Token(r,objects,pool,pcm)},{"pcm_lifecycle_weak",pcm.serial==0},
        {"game_window_thread",window_thread},{"binding_authorized",false}};
}
static Json CSharpSample(Reader& r,uint64_t engineFrame,uint32_t callSequence,uint64_t begin){
    const auto& current=csharp_state;
    mono_managed_readonly::View<Reader> view(r,current.mono,current.corelib);
    const auto value=csharp_scene_team_readonly::Capture(view,current.origins);
    auto origin=current.evidence;origin["whole_assembly_hashes_this_capture"]=false;origin["metadata_full_scan_this_capture"]=false;
    origin["metadata_source"]="worker_verified_metadata_with_fresh_class_and_consumed_field_checks";
    Json row={{"call_sequence",callSequence},{"engine_frame",engineFrame},{"thread_id",GetCurrentThreadId()},
        {"ok",true},{"engine_internal_writes_possible",false},{"binding_authorized",false},
        {"source_route",csharp_scene_team_readonly::kSourceRoute},{"source_contract",csharp_scene_team_readonly::kSourceContract},
        {"value",value},{"origin",origin}};
    row["native_join"]=scene_team_native::Capture(r,objects,pool,native_config,value.at("roster"),image,image_extent);
    row["native_join"]["pcm_update_target_rva"]=Hex(uint64_t(original)-image);
    row["value"]["csharp_witness"]["post_native_join_observation_view"]=view.Evidence();
    row["engine_frame_stable"]=true;row["origins_revalidated_after_native_join"]=true;
    row["managed_graph_revalidated_after_native_join"]=true;row["cross_frame_identity_matches"]=sample_count.load()>0;
    row["duration_ms"]=GetTickCount64()-begin;row["reader_bytes"]=r.bytes;return row;
}
static void Camera(void* object,float delta){
    struct Count{Count(){++in_callback;}~Count(){--in_callback;}} count;
    original(object,delta);
    if(!enabled.load(std::memory_order_acquire)||uint64_t(object)!=pcm.object)return;
    const uint32_t frame=++callbacks;
    if(frame<3||frame%spacing||sample_count.load()>=wanted)return;
    if(busy.test_and_set(std::memory_order_acquire))return;
    struct Release{~Release(){busy.clear(std::memory_order_release);}} release;
    try{
        Reader r{GetCurrentProcess()};
        uint64_t engineFrame=0;Require(r.Get(image+0x992f2d8,engineFrame),"engine frame unavailable");
        if(engineFrame==last_engine_frame)return;
        records.push_back(CSharpSample(r,engineFrame,frame,GetTickCount64()));
        last_engine_frame=engineFrame;
        if(++sample_count>=wanted)enabled.store(false,std::memory_order_release);
    }catch(const std::exception& e){fault=e.what();failed.store(true);enabled.store(false,std::memory_order_release);}
}
static DWORD WINAPI Worker(LPVOID){
    wchar_t own[32768]{};GetModuleFileNameW(self,own,32768);const auto dir=std::filesystem::path(own).parent_path();
    FILETIME born{},exit{},kernel{},user{};GetProcessTimes(GetCurrentProcess(),&born,&exit,&kernel,&user);
    Json out={{"schema",1},{"status","error"},{"pid",GetCurrentProcessId()},
        {"process_creation_filetime",uint64_t(born.dwLowDateTime)|(uint64_t(born.dwHighDateTime)<<32)},
        {"source_route",csharp_scene_team_readonly::kSourceRoute},{"source_contract",csharp_scene_team_readonly::kSourceContract},
        {"actor_setter_calls",0},{"camera_value_writes",0},{"binding_authorized",false},
        {"three_person_visual_verified",false}};
    try{
        std::filesystem::create_directories(dir/L"logs");q::Status(dir,{{"status","preparing_csharp_roster_sampler"},{"pid",GetCurrentProcessId()}});
        std::ifstream stream(dir/L"puerts_sampler_config.json");Json cfg;stream>>cfg;
        image=uint64_t(GetModuleHandleW(nullptr));MODULEINFO module{};GetModuleInformation(GetCurrentProcess(),HMODULE(image),&module,sizeof(module));
        image_extent=module.SizeOfImage;
        Reader r{GetCurrentProcess()};
        Require(ValidateGo(r,image+q::Rva(cfg.at("gobjects_rva")),objects)&&ValidatePool(r,image+q::Rva(cfg.at("name_pool_rva")),pool),"sampler anchors differ");
        native_config=cfg;out["anchor"]=CaptureAnchor(r,cfg);
        const HMODULE monoModule=GetModuleHandleW(L"coreclr.dll");MODULEINFO monoInfo{};
        Require(monoModule&&GetModuleInformation(GetCurrentProcess(),monoModule,&monoInfo,sizeof(monoInfo)),"CSharp Mono module unavailable");
        csharp_state=csharp_scene_team_sampler::Discover(r,objects,pool,image,active,uint64_t(monoModule),monoInfo.SizeOfImage);
        out["csharp_origin"]=csharp_state.evidence;out["observed_mono_module_sha256"]=mono_runtime_readonly::pins::kSourceDllSha256;
        wanted=uint32_t(q::Integer(cfg,"sample_count",2,8));spacing=uint32_t(q::Integer(cfg,"sample_spacing_frames",1,120));
        const auto installed=patch::Install(hook,reinterpret_cast<void**>(pcm.vtable+0x790),reinterpret_cast<void*>(original),reinterpret_cast<void*>(&Camera));
        Require(installed.code==patch::Code::Ok,"sampler callback install failed");
        enabled.store(true,std::memory_order_release);
        const auto deadline=GetTickCount64()+10000;
        while(enabled.load()&&GetTickCount64()<deadline&&!std::filesystem::exists(dir/L"stop.flag"))Sleep(20);
        enabled.store(false,std::memory_order_release);
        out["status"]=!failed.load()&&sample_count.load()==wanted?"sampled":"error";
        if(out["status"]=="error")out["reason"]=fault.empty()?"sampling incomplete, stopped or timed out":fault;
    }catch(const std::exception& e){out["reason"]=e.what();enabled.store(false,std::memory_order_release);}
    // Never unload: a cached callback address can remain live until process exit.
    for(;;){
        if(!in_callback.load()&&!busy.test_and_set(std::memory_order_acquire)){
            const auto restored=patch::Restore(hook);out["hook_restore_status"]=patch::Name(restored.code);busy.clear(std::memory_order_release);
            if(!hook.changed&&!hook.protection_pending&&!in_callback.load())break;
        }
        Sleep(100);
    }
    const bool sampled=out.at("status")=="sampled";
    out["samples"]=records;out["sample_count"]=sample_count.load();out["camera_callbacks"]=callbacks.load();
    out["callbacks_quiescent"]=true;
    out["slot_pending"]=false;out["page_protection_pending"]=false;
    out["managed_roster_two_frame_verified"]=sampled;out["native_join_two_frame_verified"]=sampled;
    if(!ww_report::Write(dir/L"puerts_sampler_result.json",out)||!ww_report::Write(dir/L"logs/carrier_status.json",out))return ERROR_WRITE_FAULT;
    return sampled?0:ERROR_INVALID_DATA;
}
}
extern "C" __declspec(dllexport) LRESULT CALLBACK HookProc(int code,WPARAM w,LPARAM l){
    using namespace puerts_sampler;
    if(code>=0&&InterlockedCompareExchange(&started,1,0)==0){wchar_t host[32768]{};GetModuleFileNameW(nullptr,host,32768);
        if(!_wcsicmp(std::filesystem::path(host).filename().c_str(),L"Client-Win64-Shipping.exe")){
            HMODULE pinned=nullptr;if(GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS|GET_MODULE_HANDLE_EX_FLAG_PIN,(LPCWSTR)&HookProc,&pinned)){
                HANDLE thread=CreateThread(nullptr,0,Worker,nullptr,0,nullptr);if(thread)CloseHandle(thread);}}}
    return CallNextHookEx(nullptr,code,w,l);
}
BOOL WINAPI DllMain(HINSTANCE module,DWORD reason,LPVOID){if(reason==DLL_PROCESS_ATTACH){puerts_sampler::self=module;DisableThreadLibraryCalls(module);}return TRUE;}
