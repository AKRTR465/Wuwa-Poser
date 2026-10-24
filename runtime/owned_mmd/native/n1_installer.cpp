// --run-session is the only mode that installs a hook.
#include "n1_common.h"
using namespace ui_n1_host;
int wmain(int argc,wchar_t** argv){
    if(argc!=3||wcscmp(argv[1],L"--run-session")){fputs("ownedmmd_lite_installer --run-session SESSION_DIRECTORY\n",stderr);return 2;}
    const auto dir=std::filesystem::absolute(argv[2]);const auto start_tick=GetTickCount64();
    Json out={{"schema",1},{"status","error"},{"runtime_executed",true}};
    HMODULE carrier=nullptr;HHOOK hook=nullptr;HANDLE process=nullptr;int result=1;
    try{
        const auto config=ParseConfig(ReadJson(dir/L"session_config.json"));out=Baseline(config);
        process=OpenProcess(SYNCHRONIZE,FALSE,config.pid);util::Require(process,"target process unavailable");
        carrier=LoadLibraryW((dir/kCarrier).c_str());util::Require(carrier,"local carrier load failed");
        const auto callback=reinterpret_cast<HOOKPROC>(GetProcAddress(carrier,"HookProc"));util::Require(callback,"carrier HookProc unavailable");
        hook=SetWindowsHookExW(WH_GETMESSAGE,callback,carrier,config.thread);util::Require(hook,"thread hook installation failed");
        PostMessageW(config.window,WM_NULL,0,0);
        // Keep the hook until the worker has closed and drained managed dispatch.
        while(!std::filesystem::exists(dir/L"worker_result.json")&&!std::filesystem::exists(dir/L"hook_startup_failed.json"))
            if(WaitForSingleObject(process,50)==WAIT_OBJECT_0){out["target_process_exited"]=true;break;}
    }catch(const std::exception& error){out["reason"]=error.what();}
    if(hook)UnhookWindowsHookEx(hook);if(carrier)FreeLibrary(carrier);if(process)CloseHandle(process);
    try{
        if(std::filesystem::exists(dir/L"hook_startup_failed.json"))out["reason"]=ReadJson(dir/L"hook_startup_failed.json").value("reason",std::string("hook startup failed"));
        if(std::filesystem::exists(dir/L"worker_result.json")){
            const auto facts=ReadJson(dir/L"worker_result.json");out["worker_result"]=facts;
            for(const auto* key:{"status","reason","mmd_started","mmd_restoration_verified","managed_owner_retirement_checked","display_interval_completed","display_elapsed_ms","display_requested_ms","mmd_result"})if(facts.contains(key))out[key]=facts.at(key);
            if(out.at("status")=="owned_mmd_retirement_recorded")result=0;
        }
    }catch(const std::exception& error){out["report_reason"]=error.what();}
    out["installer_lifecycle_elapsed_ms"]=GetTickCount64()-start_tick;
    try{ww_report::Save(dir/L"session_result.json",out);}catch(const std::exception& error){fprintf(stderr,"report write failed: %s\n",error.what());return 1;}return result;
}
