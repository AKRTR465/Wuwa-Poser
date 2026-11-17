// Separate lifecycle-registration stage. No pose/alpha/time writes; serials
// are assigned only by the engine allocator in callbacks.
#include "observer_carrier.h"
#include "native_identity_bootstrap.h"
#include "native_animation_target_config.h"

namespace identity_carrier {
namespace nb=native_identity_bootstrap;namespace ng=native_graph_discovery;namespace oc=observer_carrier;
namespace compat=ww_game_compat;namespace witness=ww_game_compat_witness;
using Allocator=int32_t(*)(void*,int32_t);
static HMODULE selfModule=nullptr;static volatile LONG started=0;
static nb::State bootstrapState;static Pool namePool;static std::atomic<bool> enabled{false};static oc::Hooks hooks;
static uint64_t objectArray=0,allocatorAddress=0;
// Callback-side and allocation-free: narrow "ABP_*_C" animation blueprint classes.
static bool AnimationClass(const ng::Token& token)noexcept{
    Reader reader{GetCurrentProcess()};NameEntry name;
    if(!FindName(reader,namePool,At<uint32_t>(token.header.data(),compat::ObjectNameOffset),name)||name.wide||name.length<6)return false;
    std::array<char,compat::NameMaximumCharacters+1> text{};
    if(!oc::Read(name.address+compat::NameEntryTextOffset,text.data(),name.length))return false;
    const auto length=name.length;
    return text[0]=='A'&&text[1]=='B'&&text[2]=='P'&&text[3]=='_'&&text[length-2]=='_'&&text[length-1]=='C';
}
// Leaf SEH wrapper: no C++ objects needing unwind and no manually written slot.
static nb::NativeResult AllocateEngine(uint32_t index)noexcept{
    nb::NativeResult result;
    if(!enabled.load(std::memory_order_acquire)||bootstrapState.failed.load()||index>=bootstrapState.basis.go.count)return result;
    result.called=true; // After this point the already-entered native call is in flight.
    __try{result.serial=reinterpret_cast<Allocator>(allocatorAddress)(reinterpret_cast<void*>(objectArray),int32_t(index));result.code=nb::Code::None;}
    __except(EXCEPTION_EXECUTE_HANDLER){result.code=nb::Code::NativeException;}
    return result;
}
__declspec(noinline) static void Visit(unsigned hook,void* node,void* context){
    nb::Visit(bootstrapState,oc::Read,AnimationClass,AllocateEngine,hook,reinterpret_cast<uint64_t>(node),reinterpret_cast<uint64_t>(context),GetCurrentThreadId(),GetTickCount64());}
static void Detour(unsigned hook,void* node,void* context,void* out){
    const DWORD incoming=GetLastError();++hooks.active;++hooks.calls[hook];struct Active{~Active(){--hooks.active;}} activeScope;
    const bool run=enabled.load(std::memory_order_acquire);if(run)Visit(hook,node,context);
    SetLastError(incoming);hooks.Forward(hook,node,context,out);
    const DWORD originalError=GetLastError();if(run&&enabled.load(std::memory_order_acquire))Visit(hook,node,context);SetLastError(originalError);
}
using Trampolines=oc::Trampolines<&Detour>;
static std::string TokenName(Reader& reader,const ng::Token& token){std::string name;
    FullName(reader,namePool,At<uint32_t>(token.header.data(),compat::ObjectNameOffset),At<uint32_t>(token.header.data(),compat::ObjectNameOffset+compat::NameNumberOffset),name);return name;}
static Json TokenJson(Reader& reader,const ng::Token& token){if(!token.address)return nullptr;ng::Token klass;
    const bool namedClass=ng::CaptureToken(oc::Read,bootstrapState.basis.go,ng::Class(token),klass,false);
    return {{"address",Hex(token.address)},{"item_address",Hex(token.item)},{"index",ng::Index(token)},{"serial",ng::Serial(token)},
        {"class_address",Hex(ng::Class(token))},{"class_name",namedClass?TokenName(reader,klass):std::string()},
        {"outer_address",Hex(At<uint64_t>(token.header.data(),compat::ObjectOuterOffset))},{"vtable",Hex(At<uint64_t>(token.header.data(),compat::ObjectVtableOffset))},
        {"object_flags",At<uint32_t>(token.header.data(),compat::ObjectFlagsOffset)},{"item_flags",At<uint32_t>(token.item_bytes.data(),compat::ObjectItemFlagsOffset)},
        {"name",TokenName(reader,token)},{"name_words",Json::array({At<uint32_t>(token.header.data(),compat::ObjectNameOffset),
            At<uint32_t>(token.header.data(),compat::ObjectNameOffset+compat::NameWordBytes),At<uint32_t>(token.header.data(),compat::ObjectNameOffset+compat::NameNumberOffset)})}};
}
static void BindBasis(const Go& objects,const Json& cfg,uint64_t image){auto& basis=bootstrapState.basis;basis.go={objects.table,objects.count};basis.image=image;
    auto capture=[&](uint64_t address,ng::Token& token,bool strong){if(!ng::CaptureToken(oc::Read,basis.go,address,token,strong))throw std::runtime_error("target identity unavailable");};
    capture(oc::Address(cfg.at("object")),basis.mesh,true);capture(oc::Address(cfg.at("outer_address")),basis.pawn,true);
    if(native_animation_target_config::Policy(cfg)==native_animation_target::OwnerPolicy::ControlledPlayer)capture(oc::Address(cfg.at("controller_address")),basis.controller,true);
    uint64_t asset=0,skeleton=0,main=0;
    if(!oc::Read(basis.mesh.address+compat::ComponentAssetOffset,&asset,compat::PointerBytes)||!oc::Read(asset+compat::AssetSkeletonOffset,&skeleton,compat::PointerBytes)||
       !oc::Read(basis.mesh.address+compat::ComponentMainAnimOffset,&main,compat::PointerBytes))throw std::runtime_error("target assets unavailable");
    capture(asset,basis.mesh_asset,false);capture(skeleton,basis.skeleton_asset,false);capture(main,basis.main_candidate,false);
    basis.authority=native_animation_target_config::Parse(cfg,basis.pawn,basis.mesh,basis.mesh_asset,basis.skeleton_asset,basis.main_candidate);
}
static Json OwnerJson(Reader& reader,const nb::Owner& owner,const char* role){
    return {{"role",role},{"before",TokenJson(reader,owner.before)},{"after",TokenJson(reader,owner.after)},
        {"status",owner.accepted?(owner.allocator_called?"registered":"already_assigned"):"rejected"},{"allocator_called",owner.allocator_called},
        {"returned_serial",owner.returned_serial},{"reason",nb::Name(owner.code)},{"thread",owner.thread},{"tick_ms",owner.tick}};
}
static DWORD WINAPI Worker(LPVOID){
    Json report={{"schema",1},{"kind","native_identity_bootstrap"},{"phase","identity_registration"},{"status","error"},{"pid",GetCurrentProcessId()},
        {"readonly",false},{"readonly_animation",true},
        {"pose_writes",0},{"alpha_writes",0},{"manual_serial_writes",0},{"native_update_calls",0},{"engine_getter_calls",0},
        {"vtable_writes_only",false},{"serial_metadata_registration",true},{"registry_metadata_mutation_only",true},
        {"serial_rollback_attempted",false},{"serial_restore_policy","engine_managed_no_rollback"},{"dll_retained_until_process_exit",true},
        {"hooks",Json::array()},{"owners",Json::array()},{"support_registrations",Json::array()},{"rejections",Json::array()},{"reasons",Json::array()},
        {"all_hooks_restored",true},{"callbacks_quiescent",false},{"main_strong_identity_confirmed",false},
        {"compatibility_id",compat::CompatibilityId},{"checks_performed",Json::array()}};
    oc::Session session;bool ran=false,bound=false;
    try{
        oc::Begin(session,selfModule,L"native_identity_bootstrap.json",report,"native_identity_bootstrap","identity_hooks");
        const auto& cfg=session.config;const auto& stage=cfg.at("native_identity_bootstrap");const unsigned duration=stage.at("duration_ms");
        bootstrapState.owner_limit=stage.at("owner_limit");report["owner_limit"]=bootstrapState.owner_limit;
        const std::vector<witness::Function> allocatorFunctions={
            {"FUObjectArray_AllocateSerialNumber",compat::FunctionAllocateSerialNumberRva,compat::FunctionAllocateSerialNumberBytes},
            {"FWeakObjectPtr_AssignObject",compat::FunctionAssignWeakObjectRva,compat::FunctionAssignWeakObjectBytes}};
        std::string verificationError;
        if(!witness::VerifyFunctions(oc::Read,session.image,session.image_bytes,stage.at("allocator_witnesses"),allocatorFunctions,report["checks_performed"],"identity_allocator",verificationError))
            throw std::runtime_error(verificationError);
        const ww_game_compat_resolution::Range image{session.image,session.image_bytes};
        if(!ww_game_compat_resolution::Contains(image,session.image+compat::FunctionAllocateSerialNumberRva,compat::FunctionAllocateSerialNumberBytes))throw std::runtime_error("allocator address out of image");
        allocatorAddress=session.image+compat::FunctionAllocateSerialNumberRva;
        uint64_t goAddress=0,poolAddress=0;
        if(!ww_game_compat_resolution::ResolveImageAddress(session.image,session.image_bytes,oc::Address(cfg.at("gobjects_rva")),compat::ObjectArrayHeaderBytes,goAddress)||
           !ww_game_compat_resolution::ResolveImageAddress(session.image,session.image_bytes,oc::Address(cfg.at("name_pool_rva")),compat::NamePoolHeaderBytes,poolAddress))throw std::runtime_error("bootstrap anchor RVA out of image");
        Reader reader{GetCurrentProcess()};Go objects;if(!ValidateGo(reader,goAddress,objects)||!ValidatePool(reader,poolAddress,namePool))throw std::runtime_error("bootstrap anchors invalid");
        if(objects.address<compat::ObjectArrayObjectsOffset)throw std::runtime_error("object array base underflow");
        objectArray=objects.address-compat::ObjectArrayObjectsOffset;
        if(!ww_game_compat_resolution::Contains(image,objectArray,compat::ObjectArrayObjectsOffset+compat::ObjectArrayHeaderBytes))throw std::runtime_error("object array base out of image");
        report["object_array_rva"]=Hex(objectArray-session.image);report["allocator_rva"]=Hex(allocatorAddress-session.image);
        BindBasis(objects,cfg,session.image);bound=true;
        report["controlled_mesh"]=TokenJson(reader,bootstrapState.basis.mesh);report["controlled_pawn"]=TokenJson(reader,bootstrapState.basis.pawn);
        report["controlled_controller"]=TokenJson(reader,bootstrapState.basis.controller);report["main_candidate_before"]=TokenJson(reader,bootstrapState.basis.main_candidate);
        report["owner_policy"]=native_animation_target::PolicyName(bootstrapState.basis.authority.policy);
        if(cfg.contains("animation_target"))report["animation_target"]=cfg.at("animation_target");
        hooks.Resolve(session,report);
        hooks.Install(&Trampolines::Address,report);
        enabled.store(true,std::memory_order_release);ran=true;const auto begin=GetTickCount64();report["status"]="registering";ww_report::Save(session.status,report);
        while(GetTickCount64()-begin<duration&&!bootstrapState.failed.load()&&!oc::StopRequested(session.directory))Sleep(10);
        report["stopped_by_flag"]=oc::StopRequested(session.directory);
    }catch(const std::exception& error){report["reasons"].push_back(error.what());}
    enabled.store(false,std::memory_order_release);
    const bool restored=hooks.Restore(report);
    const bool quiet=hooks.Quiescent();report["all_hooks_restored"]=restored;report["callbacks_quiescent"]=quiet;report["active_callbacks"]=hooks.active.load();
    try{Reader reader{GetCurrentProcess()};
        report["busy_callbacks"]=bootstrapState.busy_callbacks.load();report["target_callbacks"]=bootstrapState.target_callbacks.load();report["rejected_callbacks"]=bootstrapState.rejected_callbacks.load();
        report["truncated"]=true;report["engine_serial_allocator_calls"]=nullptr;
        if(quiet){report["truncated"]=bootstrapState.truncated;report["engine_serial_allocator_calls"]=bootstrapState.native_calls;
            for(unsigned i=0;i<bootstrapState.support_count;++i)report["support_registrations"].push_back(OwnerJson(reader,bootstrapState.supports[i],i==0?"mesh_asset":"skeleton_asset"));
            for(unsigned i=0;i<bootstrapState.owner_count;++i){const auto& owner=bootstrapState.owners[i];report["owners"].push_back(OwnerJson(reader,owner,owner.main?"main":"linked"));}
            for(unsigned i=0;i<bootstrapState.rejection_count;++i){const auto& rejection=bootstrapState.rejections[i];
                report["rejections"].push_back({{"owner",Hex(rejection.owner)},{"reason",nb::Name(rejection.code)},{"thread",rejection.thread}});}}
        const bool complete=bound&&quiet&&nb::Complete(bootstrapState);report["main_strong_identity_confirmed"]=complete;
        hooks.ReportCalls(report);
        if(!restored||!quiet)report["reasons"].push_back("bootstrap hook restoration or quiescence incomplete");
        if(!complete)report["reasons"].push_back("no complete strong identity registration for current callback owners");
        report["status"]=ran&&complete&&restored&&report["reasons"].empty()&&!report.value("stopped_by_flag",false)?"complete":"error";
    }catch(const std::exception& error){report["status"]="error";report["reasons"].push_back(error.what());}
    oc::Save(session,report);
    return 0;
}
} // namespace identity_carrier
// The installer advertises this capability only for a DLL with the guard above.
extern "C" __declspec(dllexport) void WwPoserObserverRecoveryV1() {}
extern "C" __declspec(dllexport) LRESULT CALLBACK HookProc(int code,WPARAM w,LPARAM l){
    if(code>=0)observer_carrier::StartOnce(identity_carrier::started,identity_carrier::Worker,reinterpret_cast<const void*>(&HookProc));
    return CallNextHookEx(nullptr,code,w,l);
}
BOOL WINAPI DllMain(HINSTANCE module,DWORD reason,LPVOID){if(reason==DLL_PROCESS_ATTACH){identity_carrier::selfModule=module;DisableThreadLibraryCalls(module);}return TRUE;}
