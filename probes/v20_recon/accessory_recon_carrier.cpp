// Read-only carrier for weapon components and the camera chain of the
// controlled character. No native engine calls, visibility changes or writes.
#include "shared_readonly.h"
#include "weapon_capture_readonly.h"
#include "camera_capture_readonly.h"
#include <fstream>
static HMODULE accessorySelf=nullptr;static volatile LONG accessoryStarted=0;
static DWORD WINAPI AccessoryWorker(LPVOID owned){
    wchar_t modulePath[32768];GetModuleFileNameW(accessorySelf,modulePath,32768);
    std::wstring dir(modulePath);dir.resize(dir.find_last_of(L'\\'));
    CreateDirectoryW((dir+L"\\logs").c_str(),nullptr);
    Json report={{"status","error"}};
    try{
        report["status"]="capturing";Save((dir+L"\\logs\\carrier_status.json").c_str(),report);
        std::ifstream config(dir+L"\\observe_target.json");Json cfg;config>>cfg;
        Reader reader{GetCurrentProcess()};Go objects;Pool pool;uint64_t image=(uint64_t)GetModuleHandleW(nullptr);
        if(!ValidateGo(reader,image+std::stoull(cfg.at("gobjects_rva").get<std::string>(),nullptr,16),objects)||
            !ValidatePool(reader,image+std::stoull(cfg.at("name_pool_rva").get<std::string>(),nullptr,16),pool))throw std::runtime_error("anchors differ");
        const uint64_t mesh=std::stoull(cfg.at("object").get<std::string>(),nullptr,16),pawn=std::stoull(cfg.at("outer_address").get<std::string>(),nullptr,16);
        const uint64_t controller=std::stoull(cfg.at("controller_address").get<std::string>(),nullptr,16);
        Json weapon=v20_weapon_capture::CaptureWeaponReadonly(reader,objects,pool,mesh,pawn);Save((dir+L"\\weapons_readonly.json").c_str(),weapon);
        Json camera=v20_camera_capture::Capture(reader,objects,pool,controller,pawn);Save((dir+L"\\camera_readonly.json").c_str(),camera);
        report["status"]="finished";report["weapon_status"]=weapon.value("status","");report["camera_status"]=camera.value("status","");
    }catch(const std::exception& e){report["status"]="error";report["reason"]=e.what();}
    const DWORD result=ww_report::Write(dir+L"\\logs\\carrier_status.json",report)?0:ERROR_WRITE_FAULT;
    FreeLibraryAndExitThread((HMODULE)owned,result);
}
extern "C" __declspec(dllexport) LRESULT CALLBACK HookProc(int code,WPARAM w,LPARAM l){
    if(code>=0&&InterlockedCompareExchange(&accessoryStarted,1,0)==0){wchar_t path[32768];DWORD n=GetModuleFileNameW(nullptr,path,32768);
        if(n&&!_wcsicmp(FileName(path).c_str(),L"Client-Win64-Shipping.exe")){HMODULE owned=nullptr;
            if(GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS,(LPCWSTR)&HookProc,&owned)){HANDLE t=CreateThread(nullptr,0,AccessoryWorker,owned,0,nullptr);if(t)CloseHandle(t);else FreeLibrary(owned);}}}
    return CallNextHookEx(nullptr,code,w,l);
}
BOOL WINAPI DllMain(HINSTANCE self,DWORD reason,LPVOID){if(reason==DLL_PROCESS_ATTACH){accessorySelf=self;DisableThreadLibraryCalls(self);}return TRUE;}
