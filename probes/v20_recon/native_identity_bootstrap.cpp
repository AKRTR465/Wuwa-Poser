// Separate lifecycle-registration stage. No pose/alpha/time writes; serials
// are assigned only by the engine allocator in callbacks.
#include "shared_readonly.h"
#include "vmt_slot_patch.h"
#include "native_identity_bootstrap.h"
#include "native_identity_witness.h"
#include "native_animation_target_config.h"
#include "game_compat_witness.h"
#include "runtime_evidence.h"
#include "../../runtime/generated/GameCompat.generated.h"
#include <filesystem>
#include <fstream>

namespace identity_carrier {
namespace nb=native_identity_bootstrap;namespace ng=native_graph_discovery;
namespace compat=ww_game_compat;namespace witness=ww_game_compat_witness;
using Fn2=void(*)(void*,void*);using Fn3=void(*)(void*,void*,void*);using Allocator=int32_t(*)(void*,int32_t);
constexpr const char* kBuild="c4956c30be531f3b992eabe42fa92fa9d10186000fe12e7546a11e1179bdac18";
static HMODULE selfModule=nullptr;static volatile LONG started=0;
static nb::State bootstrapState;static Pool namePool;static std::atomic<bool> enabled{false};static std::atomic<unsigned> active{0};
static std::array<std::atomic<uint64_t>,ng::kHooks> calls{};
static std::array<Fn2,ng::kHooks> originals2{};static std::array<Fn3,ng::kHooks> originals3{};
static uint64_t objectArray=0,allocatorAddress=0;
static bool Read(uint64_t address,void* out,size_t bytes)noexcept{if(!ng::Range(address,bytes))return false;SIZE_T got=0;
    return ReadProcessMemory(GetCurrentProcess(),reinterpret_cast<void*>(address),out,bytes,&got)&&got==bytes;}
static uint64_t Address(const Json& value){uint64_t parsed=0;std::string error;
    if(!value.is_string()||!ww_game_compat_resolution::ParseHexU64(value.get<std::string>(),parsed,error))throw std::runtime_error("invalid hexadecimal address: "+error);return parsed;}
static bool AnimationClass(const ng::Token& token)noexcept{
    const uint32_t tagged=ng::Get<uint32_t>(token.header.data(),compat::ObjectNameOffset),id=tagged&compat::NameTaggedIndexMask,block=id>>compat::NameBlockShift;
    if(block>namePool.current)return false;uint64_t data=0;uint16_t header=0;
    if(!Read(namePool.blocks+uint64_t(block)*compat::PointerBytes,&data,compat::PointerBytes))return false;
    const uint64_t entry=data+uint64_t(id&compat::NameBlockMask)*compat::NameEntryStride;if(!Read(entry+compat::NameEntryHeaderOffset,&header,sizeof(header))||(header&compat::NameEntryWideFlagMask))return false;
    const unsigned length=header>>compat::NameEntryLengthShift;if(length<6||length>compat::NameMaximumCharacters)return false;std::array<char,compat::NameMaximumCharacters+1> text{};
    if(!Read(entry+compat::NameEntryTextOffset,text.data(),length))return false;
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
static nb::NativeResult (*allocateCallback)(uint32_t)=AllocateEngine;
__declspec(noinline) static void Visit(unsigned hook,void* node,void* context){
    nb::Visit(bootstrapState,Read,AnimationClass,allocateCallback,hook,reinterpret_cast<uint64_t>(node),reinterpret_cast<uint64_t>(context),GetCurrentThreadId(),GetTickCount64());}
static void Detour(unsigned hook,void* node,void* context,void* out){
    const DWORD incoming=GetLastError();++active;++calls[hook];struct Active{~Active(){--active;}} activeScope;
    const bool run=enabled.load(std::memory_order_acquire);if(run)Visit(hook,node,context);
    SetLastError(incoming);if(ng::kSpecs[hook].arity==2)originals2[hook](node,context);else originals3[hook](node,context,out);
    const DWORD originalError=GetLastError();if(run&&enabled.load(std::memory_order_acquire))Visit(hook,node,context);SetLastError(originalError);
}
static void L2C(void* node,void* context){Detour(0,node,context,nullptr);}static void GroupBase(void* node,void* context){Detour(1,node,context,nullptr);}
static void GroupUpdate(void* node,void* context){Detour(2,node,context,nullptr);}static void GroupEvaluate(void* node,void* context,void* out){Detour(3,node,context,out);}
static void SingleBase(void* node,void* context){Detour(4,node,context,nullptr);}
static void* DetourAddress(unsigned index){switch(index){case 0:return reinterpret_cast<void*>(&L2C);case 1:return reinterpret_cast<void*>(&GroupBase);
    case 2:return reinterpret_cast<void*>(&GroupUpdate);case 3:return reinterpret_cast<void*>(&GroupEvaluate);default:return reinterpret_cast<void*>(&SingleBase);}}
static void* OriginalAddress(unsigned index){return ng::kSpecs[index].arity==2?reinterpret_cast<void*>(originals2[index]):reinterpret_cast<void*>(originals3[index]);}
static std::string TokenName(Reader& reader,const ng::Token& token){std::string name;const auto id=ng::Get<uint32_t>(token.header.data(),compat::ObjectNameOffset),number=ng::Get<uint32_t>(token.header.data(),compat::ObjectNameOffset+compat::NameNumberOffset);
    Decode(reader,namePool,id,name);if(number)name+="_"+std::to_string(number-1);return name;}
static Json TokenJson(Reader& reader,const ng::Token& token){if(!token.address)return nullptr;ng::Token klass;
    const bool namedClass=ng::CaptureToken(Read,bootstrapState.basis.go,ng::Class(token),klass,false);
    return {{"address",Hex(token.address)},{"item_address",Hex(token.item)},{"index",ng::Index(token)},{"serial",ng::Serial(token)},
        {"class_address",Hex(ng::Class(token))},{"class_name",namedClass?TokenName(reader,klass):std::string()},
        {"outer_address",Hex(ng::Get<uint64_t>(token.header.data(),compat::ObjectOuterOffset))},{"vtable",Hex(ng::Get<uint64_t>(token.header.data(),compat::ObjectVtableOffset))},
        {"object_flags",ng::Get<uint32_t>(token.header.data(),compat::ObjectFlagsOffset)},{"item_flags",ng::Get<uint32_t>(token.item_bytes.data(),compat::ObjectItemFlagsOffset)},
        {"name",TokenName(reader,token)},{"name_words",Json::array({ng::Get<uint32_t>(token.header.data(),compat::ObjectNameOffset),ng::Get<uint32_t>(token.header.data(),compat::ObjectNameOffset+compat::NameWordBytes),ng::Get<uint32_t>(token.header.data(),compat::ObjectNameOffset+compat::NameNumberOffset)})},
        {"identity_rechecked",true},{"strong_identity",ng::Serial(token)!=0}};
}
static void BindBasis(const Go& objects,const Json& cfg,uint64_t image){auto& basis=bootstrapState.basis;basis.go={objects.table,objects.count};basis.image=image;
    auto capture=[&](uint64_t address,ng::Token& token,bool strong){if(!ng::CaptureToken(Read,basis.go,address,token,strong))throw std::runtime_error("target identity unavailable");};
    capture(Address(cfg.at("object")),basis.mesh,true);capture(Address(cfg.at("outer_address")),basis.pawn,true);
    if(native_animation_target_config::Policy(cfg)==native_animation_target::OwnerPolicy::ControlledPlayer)capture(Address(cfg.at("controller_address")),basis.controller,true);
    uint64_t asset=0,skeleton=0,main=0;if(!Read(basis.mesh.address+compat::ComponentAssetOffset,&asset,compat::PointerBytes)||!Read(asset+compat::AssetSkeletonOffset,&skeleton,compat::PointerBytes)||!Read(basis.mesh.address+compat::ComponentMainAnimOffset,&main,compat::PointerBytes))throw std::runtime_error("target assets unavailable");
    capture(asset,basis.mesh_asset,false);capture(skeleton,basis.skeleton_asset,false);capture(main,basis.main_candidate,false);
    basis.authority=native_animation_target_config::Parse(cfg,basis.pawn,basis.mesh,basis.mesh_asset,basis.skeleton_asset,basis.main_candidate);
}
static Json Outcome(const v20_vmt_slot_patch::Outcome& value){return {{"code",v20_vmt_slot_patch::Name(value.code)},{"error",value.error},
    {"pointer_changed",value.pointer_changed},{"protection_restored",value.protection_restored}};}
static void Save(const std::filesystem::path& path,const Json& report){ww_report::Save(path,report);}
static bool Quiescent(){const auto begin=GetTickCount64();ULONGLONG quiet=0;while(GetTickCount64()-begin<5000){const auto now=GetTickCount64();if(active.load())quiet=0;
    else if(!quiet)quiet=now;else if(now-quiet>=200)return true;Sleep(1);}return false;}
static DWORD WINAPI Worker(LPVOID){
    wchar_t modulePath[32768]{};GetModuleFileNameW(selfModule,modulePath,32768);const auto directory=std::filesystem::path(modulePath).parent_path();
    const auto configPath=directory/L"observe_target.json",resultPath=directory/L"native_identity_bootstrap.json",statusPath=directory/L"logs"/L"carrier_status.json";
    Json report={{"schema",1},{"kind","native_identity_bootstrap"},{"phase","identity_registration"},{"status","error"},{"pid",GetCurrentProcessId()},
        {"host_sha256",kBuild},{"archive_evidence_sha256",native_identity_witness::kSha},{"readonly",false},{"readonly_animation",true},
        {"pose_writes",0},{"alpha_writes",0},{"manual_serial_writes",0},{"native_update_calls",0},{"engine_getter_calls",0},
        {"vtable_writes_only",false},{"serial_metadata_registration",true},{"registry_metadata_mutation_only",true},
        {"serial_rollback_attempted",false},{"serial_restore_policy","engine_managed_no_rollback"},{"dll_retained_until_process_exit",true},
        {"hooks",Json::array()},{"owners",Json::array()},{"support_registrations",Json::array()},{"rejections",Json::array()},{"reasons",Json::array()},
        {"all_hooks_restored",true},{"callbacks_quiescent",false},{"main_strong_identity_confirmed",false},
        {"compatibility_profile",compat::ProfileId},{"compatibility_source_sha256",compat::SourceSha256},
        {"checks_performed",Json::array()},{"game_image_sha256_verified",false}};
    std::array<v20_vmt_slot_patch::State,ng::kHooks> patches{};std::array<uint64_t,ng::kHooks> hookSlots{};bool ran=false,bound=false;
    try{
        std::filesystem::create_directories(directory/L"logs");Save(statusPath,report);
        // Recovery publishes stop before checking this atomic marker. A late
        // worker must announce itself, then honor stop before touching targets.
        if(std::filesystem::exists(directory/L"stop.flag"))throw std::runtime_error("observer cancelled before target access");
        Json cfg;std::ifstream input(configPath);input>>cfg;
        std::string verificationError;
        if(!witness::CompatibleSource(cfg,compat::ProfileId,compat::SourceSha256,report["checks_performed"],verificationError))throw std::runtime_error(verificationError);
        const auto& stage=cfg.at("native_identity_bootstrap");const unsigned duration=stage.at("duration_ms");
        bootstrapState.owner_limit=stage.at("owner_limit");report["owner_limit"]=bootstrapState.owner_limit;
        MODULEINFO module{};if(!GetModuleInformation(GetCurrentProcess(),GetModuleHandleW(nullptr),&module,sizeof(module)))throw std::runtime_error("module unavailable");
        const uint64_t image=reinterpret_cast<uint64_t>(module.lpBaseOfDll);report["image_base"]=Hex(image);
        report["evidence"]["allocator_witnesses"]=Json::array();
        for(const auto& witness:native_identity_witness::kFunctions)
            report["evidence"]["allocator_witnesses"].push_back({{"label",witness.label},{"rva",witness.rva},{"sha256",witness.sha256},{"bytes",witness.bytes}});
        std::vector<witness::Function> hookFunctions;
        for(const auto& spec:ng::kSpecs)if(std::none_of(hookFunctions.begin(),hookFunctions.end(),[&](const witness::Function& value){return value.rva==spec.function_rva;}))
            hookFunctions.push_back({spec.name,spec.function_rva,spec.function_bytes});
        if(!witness::VerifyFunctions(Read,image,module.SizeOfImage,stage.at("function_witnesses"),hookFunctions,report["checks_performed"],"identity_hooks",verificationError))throw std::runtime_error(verificationError);
        std::ifstream registrationInput(directory/L"native_identity_registration_contract.json",std::ios::binary);
        registrationInput.seekg(0,std::ios::end);const std::streamoff registrationBytes=registrationInput.tellg();
        if(registrationBytes<=0||registrationBytes>8*1024*1024)throw std::runtime_error("staged identity registration contract unavailable or oversized");
        std::string registrationText(static_cast<size_t>(registrationBytes),'\0');registrationInput.seekg(0,std::ios::beg);
        if(!registrationInput.read(registrationText.data(),registrationBytes))throw std::runtime_error("staged identity registration contract truncated");
        const auto registrationDigest=runtime_evidence::Sha256(registrationText.data(),registrationText.size());
        const bool digestMatches=stage.contains("registration_contract_sha256")&&stage["registration_contract_sha256"].is_string()&&stage["registration_contract_sha256"]==registrationDigest;
        report["registration_contract_sha256"]=registrationDigest;report["registration_contract_sha256_verified"]=digestMatches;
        report["registration_contract_sha256_supplied"]=stage.value("registration_contract_sha256",std::string());
        report["registration_source_archive_sha256_supplied"]=stage.value("registration_source_archive_sha256",std::string());
        report["checks_performed"].push_back({{"kind","staged_contract_sha256"},{"performed",true},{"matched",digestMatches}});
        if(!digestMatches)throw std::runtime_error("staged identity registration contract digest differs from configuration");
        const Json registration=Json::parse(registrationText);
        const std::vector<witness::Function> allocatorFunctions={
            {"FUObjectArray_AllocateSerialNumber",compat::FunctionAllocateSerialNumberRva,compat::FunctionAllocateSerialNumberBytes},
            {"FWeakObjectPtr_AssignObject",compat::FunctionAssignWeakObjectRva,compat::FunctionAssignWeakObjectBytes}};
        if(!witness::VerifyFunctions(Read,image,module.SizeOfImage,registration.at("functions"),allocatorFunctions,report["checks_performed"],"identity_allocator",verificationError))throw std::runtime_error(verificationError);
        if(!ww_game_compat_resolution::Contains({image,module.SizeOfImage},image+compat::FunctionAllocateSerialNumberRva,compat::FunctionAllocateSerialNumberBytes))throw std::runtime_error("allocator address out of image");
        allocatorAddress=image+compat::FunctionAllocateSerialNumberRva;
        uint64_t goAddress=0,poolAddress=0;
        if(!ww_game_compat_resolution::ResolveImageAddress(image,module.SizeOfImage,Address(cfg.at("gobjects_rva")),compat::ObjectArrayHeaderBytes,goAddress)||
           !ww_game_compat_resolution::ResolveImageAddress(image,module.SizeOfImage,Address(cfg.at("name_pool_rva")),compat::NamePoolHeaderBytes,poolAddress))throw std::runtime_error("bootstrap anchor RVA out of image");
        Reader reader{GetCurrentProcess()};Go objects;if(!ValidateGo(reader,goAddress,objects)||!ValidatePool(reader,poolAddress,namePool))throw std::runtime_error("bootstrap anchors invalid");
        if(objects.address<compat::ObjectArrayObjectsOffset)throw std::runtime_error("object array base underflow");
        objectArray=objects.address-compat::ObjectArrayObjectsOffset;
        if(!ww_game_compat_resolution::Contains({image,module.SizeOfImage},objectArray,compat::ObjectArrayObjectsOffset+compat::ObjectArrayHeaderBytes))throw std::runtime_error("object array base out of image");
        report["object_array_rva"]=Hex(objectArray-image);report["allocator_rva"]=Hex(allocatorAddress-image);
        BindBasis(objects,cfg,image);bound=true;
        report["controlled_mesh"]=TokenJson(reader,bootstrapState.basis.mesh);report["controlled_pawn"]=TokenJson(reader,bootstrapState.basis.pawn);
        report["controlled_controller"]=TokenJson(reader,bootstrapState.basis.controller);report["main_candidate_before"]=TokenJson(reader,bootstrapState.basis.main_candidate);
        report["owner_policy"]=native_animation_target::PolicyName(bootstrapState.basis.authority.policy);
        if(cfg.contains("animation_target"))report["animation_target"]=cfg.at("animation_target");
        for(unsigned index=0;index<ng::kHooks;++index){const auto& spec=ng::kSpecs[index];uint64_t original=0;
            const bool inImage=spec.vtable_rva<module.SizeOfImage&&spec.slot_bytes<=module.SizeOfImage-spec.vtable_rva&&
                ww_game_compat_resolution::ResolveImageAddress(image,module.SizeOfImage,spec.vtable_rva+spec.slot_bytes,compat::PointerBytes,hookSlots[index]);
            const bool read=inImage&&Read(hookSlots[index],&original,compat::PointerBytes),matched=read&&original==image+spec.function_rva;
            report["checks_performed"].push_back({{"kind","vtable_slot"},{"label",spec.name},{"performed",inImage},{"read_succeeded",read},{"matched",matched}});
            if(!matched)throw std::runtime_error("bootstrap hook slot differs");
            if(spec.arity==2)originals2[index]=reinterpret_cast<Fn2>(original);else originals3[index]=reinterpret_cast<Fn3>(original);
            report["hooks"].push_back({{"kind",spec.name},{"node_kind",spec.node_kind},{"rva",Hex(spec.function_rva)},{"vtable_rva",Hex(spec.vtable_rva)},
                {"slot_bytes",spec.slot_bytes},{"witness_bytes",spec.function_bytes},{"arity",spec.arity}});}
        for(unsigned index=0;index<ng::kHooks;++index){const auto outcome=v20_vmt_slot_patch::Install(patches[index],reinterpret_cast<void**>(hookSlots[index]),OriginalAddress(index),DetourAddress(index));
            report["hooks"][index]["install"]=Outcome(outcome);if(outcome.code!=v20_vmt_slot_patch::Code::Ok)throw std::runtime_error("bootstrap hook installation refused");}
        enabled.store(true,std::memory_order_release);ran=true;const auto begin=GetTickCount64();report["status"]="registering";Save(statusPath,report);
        while(GetTickCount64()-begin<duration&&!bootstrapState.failed.load()&&!std::filesystem::exists(directory/L"stop.flag"))Sleep(10);
        report["stopped_by_flag"]=std::filesystem::exists(directory/L"stop.flag");
    }catch(const std::exception& error){report["reasons"].push_back(error.what());}
    enabled.store(false,std::memory_order_release);bool restored=true;
    for(unsigned index=ng::kHooks;index-->0;){auto& patch=patches[index];if(!patch.slot)continue;
        const auto outcome=v20_vmt_slot_patch::Restore(patch);
        report["hooks"][index]["restore"]=Outcome(outcome);report["hooks"][index]["pointer_restored"]=!patch.changed;report["hooks"][index]["protection_restored"]=!patch.protection_pending;
        restored=restored&&outcome.code==v20_vmt_slot_patch::Code::Ok;}
    const bool quiet=Quiescent();report["all_hooks_restored"]=restored;report["callbacks_quiescent"]=quiet;report["active_callbacks"]=active.load();
    try{Reader reader{GetCurrentProcess()};report["busy_callbacks"]=bootstrapState.busy_callbacks.load();report["target_callbacks"]=bootstrapState.target_callbacks.load();report["rejected_callbacks"]=bootstrapState.rejected_callbacks.load();
        report["truncated"]=true;report["engine_serial_allocator_calls"]=nullptr;
        if(quiet){report["truncated"]=bootstrapState.truncated;report["engine_serial_allocator_calls"]=bootstrapState.native_calls;
            for(unsigned i=0;i<bootstrapState.support_count;++i){const auto& owner=bootstrapState.supports[i];report["support_registrations"].push_back({
                {"role",i==0?"mesh_asset":"skeleton_asset"},{"before",TokenJson(reader,owner.before)},{"after",TokenJson(reader,owner.after)},
                {"status",owner.accepted?(owner.allocator_called?"registered":"already_assigned"):"rejected"},{"allocator_called",owner.allocator_called},
                {"returned_serial",owner.returned_serial},{"reason",nb::Name(owner.code)},{"thread",owner.thread},{"tick_ms",owner.tick}});}
            for(unsigned index=0;index<bootstrapState.owner_count;++index){const auto& owner=bootstrapState.owners[index];report["owners"].push_back({
            {"role",owner.main?"main":"linked"},{"before",TokenJson(reader,owner.before)},{"after",TokenJson(reader,owner.after)},
            {"status",owner.accepted?(owner.allocator_called?"registered":"already_assigned"):"rejected"},{"allocator_called",owner.allocator_called},
            {"returned_serial",owner.returned_serial},{"reason",nb::Name(owner.code)},{"thread",owner.thread},{"tick_ms",owner.tick}});}
            for(unsigned index=0;index<bootstrapState.rejection_count;++index){const auto& rejection=bootstrapState.rejections[index];report["rejections"].push_back({{"owner",Hex(rejection.owner)},{"reason",nb::Name(rejection.code)},{"thread",rejection.thread}});}}
        const bool complete=bound&&quiet&&nb::Complete(bootstrapState);report["main_strong_identity_confirmed"]=complete;report["controlled_relation_rechecked"]=complete&&bootstrapState.basis.authority.policy==native_animation_target::OwnerPolicy::ControlledPlayer;report["target_permission_rechecked"]=complete;
        for(unsigned index=0;index<report["hooks"].size();++index)report["hooks"][index]["calls"]=calls[index].load();
        if(!restored||!quiet)report["reasons"].push_back("bootstrap hook restoration or quiescence incomplete");
        if(!complete)report["reasons"].push_back("no complete strong identity registration for current callback owners");
        report["status"]=ran&&complete&&restored&&report["reasons"].empty()&&!report.value("stopped_by_flag",false)?"complete":"error";
        Save(resultPath,report);Save(statusPath,report);
    }catch(const std::exception& error){report["status"]="error";report["reasons"].push_back(error.what());try{Save(resultPath,report);Save(statusPath,report);}catch(...){return ERROR_WRITE_FAULT;}}
    return 0;
}
} // namespace identity_carrier
// The installer advertises this capability only for a DLL with the guard above.
extern "C" __declspec(dllexport) void WwPoserObserverRecoveryV1() {}
extern "C" __declspec(dllexport) LRESULT CALLBACK HookProc(int code,WPARAM w,LPARAM l){using namespace identity_carrier;
    if(code>=0&&InterlockedCompareExchange(&started,1,0)==0){wchar_t host[32768]{};GetModuleFileNameW(nullptr,host,32768);HMODULE retained=nullptr;
        if(!_wcsicmp(std::filesystem::path(host).filename().c_str(),L"Client-Win64-Shipping.exe")&&
            GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS|GET_MODULE_HANDLE_EX_FLAG_PIN,reinterpret_cast<LPCWSTR>(&HookProc),&retained)){
            HANDLE thread=CreateThread(nullptr,0,Worker,nullptr,0,nullptr);if(thread)CloseHandle(thread);}}
    return CallNextHookEx(nullptr,code,w,l);}
BOOL WINAPI DllMain(HINSTANCE module,DWORD reason,LPVOID){if(reason==DLL_PROCESS_ATTACH){identity_carrier::selfModule=module;DisableThreadLibraryCalls(module);}return TRUE;}
