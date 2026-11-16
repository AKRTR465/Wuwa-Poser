#pragma once
#include "../../runtime/generated/GameCompat.generated.h"
// Shared lifecycle of the two observer carriers. hook_install_v20 loads a
// carrier through a WH_GETMESSAGE hook; the carrier then patches the fixed
// animation vtable slots, forwards every callback to the engine original
// exactly once, and restores the slots before reporting.
#include "shared_readonly.h"
#include "vmt_slot_patch.h"
#include "native_graph_discovery.h"
#include "game_compat_witness.h"
#include <array>
#include <atomic>
#include <filesystem>
#include <fstream>

namespace observer_carrier {
namespace ng=native_graph_discovery;
namespace compat=ww_game_compat;
namespace witness=ww_game_compat_witness;
using Fn2=void(*)(void*,void*);using Fn3=void(*)(void*,void*,void*);
inline bool Read(uint64_t address,void* out,size_t bytes) noexcept {
    if(!ng::Range(address,bytes))return false;SIZE_T got=0;
    return ReadProcessMemory(GetCurrentProcess(),reinterpret_cast<void*>(address),out,bytes,&got)&&got==bytes;
}
inline uint64_t Address(const Json& j){uint64_t parsed=0;std::string error;
    if(!j.is_string()||!ww_game_compat_resolution::ParseHexU64(j.get<std::string>(),parsed,error))throw std::runtime_error("invalid hexadecimal address: "+error);return parsed;}
inline Json Outcome(const v20_vmt_slot_patch::Outcome& o){return {{"code",v20_vmt_slot_patch::Name(o.code)},{"error",o.error},
    {"pointer_changed",o.pointer_changed},{"protection_restored",o.protection_restored}};}
inline std::filesystem::path ModuleDirectory(HMODULE module){wchar_t path[32768]{};GetModuleFileNameW(module,path,32768);return std::filesystem::path(path).parent_path();}
inline bool StopRequested(const std::filesystem::path& directory){return std::filesystem::exists(directory/L"stop.flag");}
// One distinct engine function per hook kind; two kinds share an evaluator.
inline std::vector<witness::Function> HookFunctions(){
    std::vector<witness::Function> functions;
    for(const auto& spec:ng::kSpecs)if(std::none_of(functions.begin(),functions.end(),[&](const witness::Function& value){return value.rva==spec.function_rva;}))
        functions.push_back({spec.name,spec.function_rva,spec.function_bytes});
    return functions;
}
struct Session {
    std::filesystem::path directory,result,status;
    Json config;uint64_t image=0,image_bytes=0;
};
// Announces the carrier, honors an early stop, and verifies that the staged
// configuration and the hooked functions belong to this build.
inline void Begin(Session& session,HMODULE self,const wchar_t* resultName,Json& report,const char* stage,const char* group){
    session.directory=ModuleDirectory(self);session.result=session.directory/resultName;session.status=session.directory/L"logs"/L"carrier_status.json";
    std::filesystem::create_directories(session.directory/L"logs");ww_report::Save(session.status,report);
    // Recovery publishes stop before checking this atomic marker. A late
    // worker must announce itself, then honor stop before touching targets.
    if(StopRequested(session.directory))throw std::runtime_error("observer cancelled before target access");
    std::ifstream input(session.directory/L"observe_target.json",std::ios::binary);input>>session.config;
    std::string error;
    if(!witness::CompatibleSource(session.config,compat::ProfileId,compat::SourceSha256,report["checks_performed"],error))throw std::runtime_error(error);
    MODULEINFO module{};if(!GetModuleInformation(GetCurrentProcess(),GetModuleHandleW(nullptr),&module,sizeof(module)))throw std::runtime_error("main module unavailable");
    session.image=reinterpret_cast<uint64_t>(module.lpBaseOfDll);session.image_bytes=module.SizeOfImage;report["image_base"]=Hex(session.image);
    if(!witness::VerifyFunctions(Read,session.image,session.image_bytes,session.config.at(stage).at("function_witnesses"),HookFunctions(),report["checks_performed"],group,error))
        throw std::runtime_error(error);
}
inline void Save(const Session& session,Json& report){
    try{ww_report::Save(session.result,report);ww_report::Save(session.status,report);}
    catch(const std::exception& error){report["status"]="error";report["reasons"].push_back(error.what());
        try{ww_report::Save(session.result,report);ww_report::Save(session.status,report);}catch(...){}}
}
struct Hooks {
    std::array<std::atomic<uint64_t>,ng::kHooks> calls{};
    std::array<Fn2,ng::kHooks> originals2{};std::array<Fn3,ng::kHooks> originals3{};
    std::array<v20_vmt_slot_patch::State,ng::kHooks> patches{};std::array<uint64_t,ng::kHooks> slots{};
    std::atomic<unsigned> active{0};
    void Forward(unsigned hook,void* node,void* context,void* out){if(ng::kSpecs[hook].arity==2)originals2[hook](node,context);else originals3[hook](node,context,out);}
    void* Original(unsigned hook) const {return ng::kSpecs[hook].arity==2?reinterpret_cast<void*>(originals2[hook]):reinterpret_cast<void*>(originals3[hook]);}
    // Each fixed slot must still hold its verified engine function.
    void Resolve(const Session& session,Json& report){
        for(unsigned i=0;i<ng::kHooks;++i){const auto& spec=ng::kSpecs[i];uint64_t original=0;
            const bool inImage=spec.vtable_rva<session.image_bytes&&spec.slot_bytes<=session.image_bytes-spec.vtable_rva&&
                ww_game_compat_resolution::ResolveImageAddress(session.image,session.image_bytes,spec.vtable_rva+spec.slot_bytes,compat::PointerBytes,slots[i]);
            const bool read=inImage&&Read(slots[i],&original,compat::PointerBytes),matched=read&&original==session.image+spec.function_rva;
            report["checks_performed"].push_back({{"kind","vtable_slot"},{"label",spec.name},{"performed",inImage},{"read_succeeded",read},{"matched",matched}});
            if(!matched)throw std::runtime_error("fixed hook slot differs");
            if(spec.arity==2)originals2[i]=reinterpret_cast<Fn2>(original);else originals3[i]=reinterpret_cast<Fn3>(original);
            report["hooks"].push_back({{"kind",spec.name},{"node_kind",spec.node_kind},{"rva",Hex(spec.function_rva)},{"vtable_rva",Hex(spec.vtable_rva)},
                {"slot_bytes",spec.slot_bytes},{"witness_bytes",spec.function_bytes},{"arity",spec.arity}});
        }
    }
    void Install(void* (*detour)(unsigned),Json& report){
        for(unsigned i=0;i<ng::kHooks;++i){
            const auto outcome=v20_vmt_slot_patch::Install(patches[i],reinterpret_cast<void**>(slots[i]),Original(i),detour(i));
            report["hooks"][i]["install"]=Outcome(outcome);
            if(outcome.code!=v20_vmt_slot_patch::Code::Ok)throw std::runtime_error("hook installation refused");
        }
    }
    bool Restore(Json& report){bool restored=true;
        for(unsigned i=ng::kHooks;i-->0;){auto& patch=patches[i];if(!patch.slot)continue;
            const auto outcome=v20_vmt_slot_patch::Restore(patch);
            const bool ok=outcome.code==v20_vmt_slot_patch::Code::Ok&&!patch.changed&&!patch.protection_pending;
            report["hooks"][i]["restore"]=Outcome(outcome);report["hooks"][i]["fully_restored"]=ok;restored=restored&&ok;
        }
        return restored;
    }
    // No callback has been inside a detour for 200 ms, within 5 s.
    bool Quiescent() const {const auto begin=GetTickCount64();ULONGLONG quiet=0;
        while(GetTickCount64()-begin<5000){const auto now=GetTickCount64();
            if(active.load())quiet=0;else if(!quiet)quiet=now;else if(now-quiet>=200)return true;Sleep(1);}
        return false;}
    void ReportCalls(Json& report) const {for(unsigned i=0;i<report["hooks"].size();++i)report["hooks"][i]["calls"]=calls[i].load();}
};
// Per-hook entry points with the engine's two- and three-argument signatures.
template<void(*Detour)(unsigned,void*,void*,void*)> struct Trampolines {
    template<unsigned I> static void Call2(void* node,void* context){Detour(I,node,context,nullptr);}
    template<unsigned I> static void Call3(void* node,void* context,void* out){Detour(I,node,context,out);}
    static void* Address(unsigned hook){
        static_assert(ng::kHooks==5&&ng::kSpecs[3].arity==3,"trampoline signatures follow kSpecs");
        static void* const table[ng::kHooks]={reinterpret_cast<void*>(&Call2<0>),reinterpret_cast<void*>(&Call2<1>),
            reinterpret_cast<void*>(&Call2<2>),reinterpret_cast<void*>(&Call3<3>),reinterpret_cast<void*>(&Call2<4>)};
        return table[hook];
    }
};
// Starts the worker once, only inside the game process, and pins the carrier:
// a thread may still hold a detour address after its slot was restored.
inline void StartOnce(volatile LONG& started,LPTHREAD_START_ROUTINE worker,const void* pinned){
    if(InterlockedCompareExchange(&started,1,0)!=0)return;
    wchar_t host[32768]{};GetModuleFileNameW(nullptr,host,32768);HMODULE retained=nullptr;
    if(_wcsicmp(std::filesystem::path(host).filename().c_str(),L"Client-Win64-Shipping.exe")||
        !GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS|GET_MODULE_HANDLE_EX_FLAG_PIN,reinterpret_cast<LPCWSTR>(pinned),&retained))return;
    if(HANDLE thread=CreateThread(nullptr,0,worker,nullptr,0,nullptr))CloseHandle(thread);
}
} // namespace observer_carrier
