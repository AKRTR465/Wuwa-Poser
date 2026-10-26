#pragma once
#include "../../runtime/generated/GameCompat.generated.h"
// One carrier owns the shared VMT slots. Single/owned/SceneTeam adapters all
// use this hub.
#include "shared_readonly.h"
#include "vmt_slot_patch.h"
#include "native_animation_target_config.h"
#include "game_compat_witness.h"
#include "../../playback/native_physics_actor.h"
#include <filesystem>

namespace v20_shared_native_physics {
namespace core=wwp_playback::native_physics;
namespace graph=v20_native_graph_binding;
using Actor=core::Actor;
using Access=core::Access;
using AuthorityCheck=bool(*)(void*,const Access&);
using Snapshot=core::Snapshot;
struct HookHub {
    core::Registry registry;
    v20_vmt_slot_patch::State converter_patch,group_patch;
    void (*original_converter)(void*,void*)=nullptr;
    void (*original_group)(void*,void*)=nullptr;
    const void* (*getter)(void*,int32_t)=nullptr;
    bool (*lod)(void*,void*)=nullptr;
    bool (*valid)(void*,void*,void*)=nullptr;
    void (*internal)(void*,void*)=nullptr;
    uint64_t image=0,converter_vtable=0,group_vtable=0;
    std::atomic<bool> observing{false},group_observing{false};
    std::atomic<unsigned> active{0},group_active{0};
    std::atomic<unsigned> dispatch_active{0};
    std::mutex lifecycle;
};
inline HookHub hub;
inline bool Read(void*,uintptr_t a,void* out,size_t n){SIZE_T read=0;return ReadProcessMemory(GetCurrentProcess(),(LPCVOID)a,out,n,&read)&&read==n;}
inline bool Write(void*,uintptr_t a,const void* in,size_t n){SIZE_T written=0;return WriteProcessMemory(GetCurrentProcess(),(LPVOID)a,in,n,&written)&&written==n;}
inline bool Writable(void*,uintptr_t a,size_t n){
    if(!a||!n||a>UINTPTR_MAX-n)return false;const auto end=a+n;
    while(a<end){MEMORY_BASIC_INFORMATION info{};if(!VirtualQuery((void*)a,&info,sizeof(info))||info.State!=MEM_COMMIT||
        (info.Protect&(PAGE_NOACCESS|PAGE_GUARD))||!(info.Protect&(PAGE_READWRITE|PAGE_WRITECOPY)))return false;
        const auto next=uintptr_t(info.BaseAddress)+info.RegionSize;if(next<=a)return false;a=next;}return true;
}
inline bool Getter(void*,uintptr_t pose,int32_t bone,core::input::Transform* result){
    __try {const auto p=hub.getter((void*)pose,bone);if(!p)return false;memcpy(result->data(),p,ww_game_compat::TransformBytes);return true;}
    __except(EXCEPTION_EXECUTE_HANDLER){return false;}
}
inline core::Gate Gate(void*,uintptr_t node,uintptr_t proxy,uintptr_t skeleton){
    __try{if(!hub.lod((void*)node,(void*)proxy))return core::Gate::LodDenied;
        return hub.valid((void*)node,(void*)skeleton,(uint8_t*)proxy+ww_game_compat::AnimProxyBoneContainerOffset)?core::Gate::Allowed:core::Gate::ValidityDenied;}
    __except(EXCEPTION_EXECUTE_HANDLER){return core::Gate::Exception;}
}
inline bool Internal(void*,uintptr_t node,uintptr_t context){
    __try{hub.internal((void*)node,(void*)context);return true;}
    __except(EXCEPTION_EXECUTE_HANDLER){return false;}
}
inline Access MakeAccess(){return {nullptr,&Read,&Write,&Writable,&Getter,&Gate,&Internal,GetCurrentThreadId()};}
// Close the registry-use admission before inspecting recovery. A callback
// that read the old gate either enters this counted lease or rechecks closed;
// forwarding the engine original does not require any actor/registry access.
struct Dispatch {
    bool entered=false;
    explicit Dispatch(const std::atomic<bool>& gate){if(!gate.load())return;++hub.dispatch_active;
        if(gate.load())entered=true;else --hub.dispatch_active;}
    ~Dispatch(){if(entered)--hub.dispatch_active;}
    explicit operator bool() const{return entered;}
};
inline void ConverterDetour(void* node,void* context){
    ++hub.active;struct Active{~Active(){--hub.active;}} callback_scope;
    hub.original_converter(node,context); // every target and non-target exactly once
    Dispatch dispatch(hub.observing);if(!dispatch)return;
    auto actor=hub.registry.Input(uintptr_t(node));if(!actor)return;
    core::InputFrame(*actor,uintptr_t(node),uintptr_t(context),MakeAccess());
}
inline void GroupDetour(void* node,void* context){
    ++hub.group_active;struct Active{~Active(){--hub.group_active;}} callback_scope;
    hub.original_group(node,context);
    Dispatch dispatch(hub.group_observing);if(!dispatch)return;
    unsigned index=0;auto actor=hub.registry.Group(uintptr_t(node),&index);if(!actor)return;
    core::ForceGroup(*actor,index,uintptr_t(node),uintptr_t(context),MakeAccess());
}
template<class Reader>inline core::ObjectToken Capture(Reader& r,const Go& go,uint64_t address){
    core::ObjectToken t;t.address=address;uint64_t chunk=0;std::array<uint8_t,ww_game_compat::ObjectItemBytes> item{};
    graph::Require(r.Read(address,t.header.data(),ww_game_compat::ObjectHeaderBytes),"native actor object unreadable");t.index=At<uint32_t>(t.header.data(),ww_game_compat::ObjectIndexOffset);
    graph::Require(r.Read(go.table+uint64_t(t.index>>ww_game_compat::ObjectArrayChunkShift)*ww_game_compat::PointerBytes,&chunk,sizeof(uint64_t)),"native actor object index invalid");
    t.item=chunk+uint64_t(t.index&ww_game_compat::ObjectArrayChunkMask)*ww_game_compat::ObjectItemBytes;
    graph::Require(r.Read(t.item,item.data(),ww_game_compat::ObjectItemBytes),"native actor object item invalid");t.serial=At<uint32_t>(item.data(),ww_game_compat::ObjectItemSerialOffset);return t;
}
inline native_graph_discovery::Token DiscoveryToken(const core::ObjectToken& token){native_graph_discovery::Token result;
    result.address=token.address;result.item=token.item;result.header=token.header;memcpy(result.item_bytes.data(),&token.address,sizeof(uint64_t));memcpy(result.item_bytes.data()+ww_game_compat::ObjectItemSerialOffset,&token.serial,sizeof(token.serial));return result;}
inline const std::vector<ww_game_compat_witness::Function>& InputFunctionSpecs(){
    static const std::vector<ww_game_compat_witness::Function> functions={
        {"LocalToComponent",ww_game_compat::FunctionLocalToComponentRva,ww_game_compat::FunctionLocalToComponentBytes},
        {"ComponentSpaceTransformGetter",ww_game_compat::FunctionComponentSpaceTransformGetterRva,ww_game_compat::FunctionComponentSpaceTransformGetterBytes},
        {"AnimInstanceRootEvaluationCounter",ww_game_compat::FunctionAnimInstanceRootEvaluationCounterRva,ww_game_compat::FunctionAnimInstanceRootEvaluationCounterBytes},
        {"CachedPoseEvaluationCounter",ww_game_compat::FunctionCachedPoseEvaluationCounterRva,ww_game_compat::FunctionCachedPoseEvaluationCounterBytes}};
    return functions;
}
inline const std::vector<ww_game_compat_witness::Function>& ActivationFunctionSpecs(){
    static const std::vector<ww_game_compat_witness::Function> functions={
        {"SharedSkeletalControlUpdate",ww_game_compat::FunctionSharedSkeletalControlUpdateRva,ww_game_compat::FunctionSharedSkeletalControlUpdateBytes},
        {"SkeletalControlLODAllowed",ww_game_compat::FunctionSkeletalControlLODAllowedRva,ww_game_compat::FunctionSkeletalControlLODAllowedBytes},
        {"GroupIsValidToEvaluate",ww_game_compat::FunctionGroupIsValidToEvaluateRva,ww_game_compat::FunctionGroupIsValidToEvaluateBytes},
        {"SharedUpdateInternal",ww_game_compat::FunctionSharedUpdateInternalRva,ww_game_compat::FunctionSharedUpdateInternalBytes}};
    return functions;
}
// Initialization only. The callback path uses the verified addresses and small
// immutable result counters; it never parses JSON or scans executable sections.
template<class Reader>inline bool VerifyNativeFunctionSets(Reader& r,uint64_t image,uint64_t imageBytes,const Json& cfg,Json& checks,std::string& error){
    auto read=[&](uint64_t address,void* out,size_t bytes){return r.Read(address,out,bytes);};
    std::string sourceError,inputError,activationError;
    const bool source=ww_game_compat_witness::CompatibleSource(cfg,ww_game_compat::ProfileId,ww_game_compat::SourceSha256,checks,sourceError);
    const bool input=ww_game_compat_witness::VerifyFunctions(read,image,imageBytes,cfg.value("native_physics_witnesses",Json::array()),InputFunctionSpecs(),checks,"native_input",inputError);
    const bool activation=ww_game_compat_witness::VerifyFunctions(read,image,imageBytes,cfg.value("native_physics_activation_witnesses",Json::array()),ActivationFunctionSpecs(),checks,"native_activation",activationError);
    error=!sourceError.empty()?sourceError:!inputError.empty()?inputError:activationError;
    return source&&input&&activation;
}
template<class Reader>inline void VerifyNativeFunctions(Actor& a,Reader& r,const Json& cfg,const std::filesystem::path& directory){
    a.function_witnesses_verified.store(false,std::memory_order_release);
    a.function_witnesses_required.store(unsigned(InputFunctionSpecs().size()+ActivationFunctionSpecs().size()));
    a.function_witnesses_matched.store(0);
    Json checks=Json::array();std::string error;bool verified=false;
    try{
        MODULEINFO module{};
        graph::Require(GetModuleInformation(GetCurrentProcess(),GetModuleHandleW(nullptr),&module,sizeof(module))!=0,"native physics main module unavailable");
        verified=VerifyNativeFunctionSets(r,uint64_t(module.lpBaseOfDll),module.SizeOfImage,cfg,checks,error);
    }catch(const std::exception& failure){error=failure.what();}
    unsigned matched=0;for(const auto& row:checks)if(row.value("kind",std::string())=="function_bytes"&&row.value("matched",false))++matched;
    a.function_witnesses_matched.store(matched);
    std::filesystem::create_directories(directory);
    ww_report::Save(directory/L"native_function_checks.json",{{"schema",1},{"compatibility_profile",ww_game_compat::ProfileId},
        {"compatibility_source_sha256",ww_game_compat::SourceSha256},{"verified",verified},{"required_function_count",a.function_witnesses_required.load()},
        {"matched_function_count",matched},{"checks",checks},{"error",error},{"candidate_relocation_authorized",false}});
    graph::Require(verified,error.empty()?"native physics function verification failed":error.c_str());
    a.function_witnesses_verified.store(true,std::memory_order_release);
}
template<class Reader>inline void PrepareBinding(Actor& a,Reader& r,const Json& cfg,const std::string&,const std::filesystem::path& directory,AuthorityCheck check,void* user){
    VerifyNativeFunctions(a,r,cfg,directory);
    a.binding=graph::Parse(cfg.at("native_graph_binding"));
    a.image=uint64_t(GetModuleHandleW(nullptr));a.target={};a.target.mesh=a.binding.target.address;a.target.mesh_index=a.binding.target.index;a.target.mesh_serial=a.binding.target.serial;
    a.target.policy=native_animation_target_config::Policy(cfg);a.target.main_anim=a.binding.main_anim.address;
    uint64_t owner=0,asset=0,skeleton=0;
    graph::Require(r.Read(a.target.mesh+ww_game_compat::ComponentOwnerOffset,&owner,sizeof(uint64_t))&&r.Read(a.target.mesh+ww_game_compat::ComponentAssetOffset,&asset,sizeof(uint64_t))&&r.Read(asset+ww_game_compat::AssetSkeletonOffset,&skeleton,sizeof(uint64_t)),"native actor asset chain unavailable");
    a.target.actor=owner;a.target.mesh_asset=asset;a.target.skeleton_asset=skeleton;
    std::array<uint8_t,ww_game_compat::ObjectArrayHeaderBytes> go_bytes{};const auto go_address=a.image+graph::Address(cfg.at("gobjects_rva"));
    graph::Require(r.Read(go_address,go_bytes.data(),ww_game_compat::ObjectArrayHeaderBytes),"native actor GObjects unreadable");
    const Go go{go_address,At<uint64_t>(go_bytes.data(),ww_game_compat::ObjectArrayTableOffset),At<uint32_t>(go_bytes.data(),ww_game_compat::ObjectArrayCountOffset)};
    const uint64_t addresses[]={a.target.actor,a.target.mesh,asset,skeleton,a.target.main_anim};for(unsigned i=0;i<5;++i)a.tokens[i]=Capture(r,go,addresses[i]);
    a.target.actor_index=a.tokens[0].index;a.target.actor_serial=a.tokens[0].serial;
    if(cfg.contains("animation_target"))
        a.target=native_animation_target_config::Parse(cfg,DiscoveryToken(a.tokens[0]),DiscoveryToken(a.tokens[1]),DiscoveryToken(a.tokens[2]),DiscoveryToken(a.tokens[3]),DiscoveryToken(a.tokens[4]));
    a.node=a.target.main_anim+a.binding.converter_offset;a.node_vtable=a.image+graph::kConverterVtableRva;a.group_vtable=a.image+graph::kGroupVtableRva;
    a.authority_alive=check;a.authority_user=user;a.prepared=true;
}
template<class Reader>inline void PrepareFromConfig(Actor& a,Reader& r,const Json& cfg,const std::string& profile,const std::filesystem::path& directory,
    const wwp_playback::local_clip::Clip& motion,const wwp_ue::full_body::Topology& topology,const wwp_ue::full_body::Request& request,uint64_t epoch,AuthorityCheck check,void* user){
    core::PrepareMotion(a,motion,topology,request,epoch);PrepareBinding(a,r,cfg,profile,directory,check,user);
    graph::Require(a.binding.bone_count==topology.count,"native actor topology differs");
}
inline bool RegisterActor(Actor& a){return a.prepared&&a.function_witnesses_verified.load(std::memory_order_acquire)&&hub.registry.Add(a);}
inline bool UnregisterActor(Actor& a){return hub.registry.Remove(a);}
inline void Publish(Actor& a,double seconds,const wwp_playback::placement::Settings& placement,bool feed){core::Publish(a,seconds,placement,feed);}
inline void RequestStop(Actor& a){core::RequestStop(a);}
inline void CloseAdmission(Actor& a){core::CloseAdmission(a);}
inline bool Restored(const Actor& a){return core::Restored(a);}
inline void Install(Json&){
    std::lock_guard<std::mutex> lifecycle(hub.lifecycle);
    graph::Require(hub.registry.Freeze(),"native shared registry cannot be frozen");
    for(const auto* actor:hub.registry.actors)graph::Require(actor->prepared&&actor->function_witnesses_verified.load(std::memory_order_acquire),"native actor functions were not verified before installation");
    const auto& first=*hub.registry.actors[0];bool groups=false;for(unsigned i=0;i<hub.registry.count;++i)groups|=hub.registry.actors[i]->binding.group_count!=0;
    hub.image=first.image;hub.converter_vtable=first.node_vtable;hub.group_vtable=first.group_vtable;
    hub.original_converter=(void(*)(void*,void*))(hub.image+ww_game_compat::FunctionLocalToComponentRva);hub.original_group=(void(*)(void*,void*))(hub.image+ww_game_compat::FunctionSharedSkeletalControlUpdateRva);
    hub.getter=(const void*(*)(void*,int32_t))(hub.image+ww_game_compat::FunctionComponentSpaceTransformGetterRva);hub.lod=(bool(*)(void*,void*))(hub.image+ww_game_compat::FunctionSkeletalControlLODAllowedRva);
    hub.valid=(bool(*)(void*,void*,void*))(hub.image+ww_game_compat::FunctionGroupIsValidToEvaluateRva);hub.internal=(void(*)(void*,void*))(hub.image+ww_game_compat::FunctionSharedUpdateInternalRva);
    hub.observing.store(true);
    graph::Require(v20_vmt_slot_patch::Install(hub.converter_patch,(void**)(hub.converter_vtable+ww_game_compat::SlotLocalToComponentEvaluateCS),(void*)hub.original_converter,(void*)&ConverterDetour).code==v20_vmt_slot_patch::Code::Ok,"native shared converter installation failed");
    if(groups){hub.group_observing.store(true,std::memory_order_release);
        graph::Require(v20_vmt_slot_patch::Install(hub.group_patch,(void**)(hub.group_vtable+ww_game_compat::SlotSkeletalControlUpdate),(void*)hub.original_group,(void*)&GroupDetour).code==v20_vmt_slot_patch::Code::Ok,"native shared group installation failed");}
}
inline bool CloseDispatchForRestore(uint64_t deadline){
    const bool converter=hub.observing.exchange(false),group=hub.group_observing.exchange(false);
    while(hub.dispatch_active.load()&&GetTickCount64()<deadline)Sleep(1);
    if(hub.dispatch_active.load()||!hub.registry.RestoredAll()){
        // An admitted callback may have written input before terminal feed
        // closed. Keep natural recovery routing available until it retires.
        hub.observing.store(converter);hub.group_observing.store(group);return false;}
    return true;
}
inline bool Uninstall(Json& report,const char* field="cleanup_native_vmt_restore"){
    std::lock_guard<std::mutex> lifecycle(hub.lifecycle);
    {std::lock_guard<std::mutex> lock(hub.registry.operations);
        for(unsigned i=0;i<hub.registry.count;++i)core::CloseAdmission(*hub.registry.actors[i]);}
    if(!CloseDispatchForRestore(GetTickCount64()+5000))return false;bool ok=true;
    if(hub.converter_patch.changed||hub.converter_patch.protection_pending){const auto r=v20_vmt_slot_patch::Restore(hub.converter_patch);
        report[field]={{"code",v20_vmt_slot_patch::Name(r.code)},{"protection_restored",r.protection_restored}};ok=r.code==v20_vmt_slot_patch::Code::Ok;}
    if(hub.group_patch.changed||hub.group_patch.protection_pending){const auto r=v20_vmt_slot_patch::Restore(hub.group_patch);
        report["cleanup_native_group_update_vmt_restore"]={{"code",v20_vmt_slot_patch::Name(r.code)},{"protection_restored",r.protection_restored}};ok=ok&&r.code==v20_vmt_slot_patch::Code::Ok;}
    const auto deadline=GetTickCount64()+5000;Sleep(200);while((hub.active.load()||hub.group_active.load())&&GetTickCount64()<deadline)Sleep(1);
    ok=ok&&!hub.active.load()&&!hub.group_active.load()&&!hub.dispatch_active.load()&&hub.registry.RestoredAll();if(ok)hub.registry.Thaw();return ok;
}
inline void Stats(const Actor& a,Json& j){
    j["native_physics"]=true;j["physics_backend"]="game_native";
    j["native_function_witnesses_verified"]=a.function_witnesses_verified.load();
    j["native_function_witnesses_required"]=a.function_witnesses_required.load();j["native_function_witnesses_matched"]=a.function_witnesses_matched.load();
    j["native_input_frames"]=a.frames.load();j["native_input_error"]=a.error.load();j["native_restore_pending"]=a.pending.load();
    unsigned curve_count=0;for(unsigned i=0;i<a.binding.group_count;++i)curve_count+=a.binding.groups[i].alpha_type==ww_game_compat::AlphaTypeCurve;
    j["native_physics_group_count"]=a.binding.group_count;j["native_group_force_updates"]=a.group_forced.load();j["native_group_force_pending"]=a.force_pending.load();
    j["native_curve_group_count"]=curve_count;j["native_physics_activation_complete"]=a.binding.activation_complete;j["native_graph_binding_kind"]="observed_session_binding";
    unsigned observed_groups=0;for(unsigned i=0;i<a.binding.group_count;++i)if(a.groups[i].forced.load()||a.groups[i].natural_positive_delta_updates.load())++observed_groups;
    const bool activation_observed=a.binding.group_count&&a.binding.activation_complete&&observed_groups==a.binding.group_count;
    j["native_secondary_motion_activation_runtime_observed"]=activation_observed;
    j["native_secondary_motion_evidence_scope"]="group_activation_updates_only";j["native_activation_observed_group_count"]=observed_groups;
    j["native_secondary_motion_activation_status"]=activation_observed?"activation_runtime_observed":observed_groups?"activation_partially_observed":"activation_not_observed";
    j["native_curve_groups"]=Json::array();j["native_physics_groups"]=Json::array();for(unsigned i=0;i<a.binding.group_count;++i){const auto& g=a.groups[i];const auto& b=a.binding.groups[i];
        Json group={{"offset",Hex(b.offset)},{"alpha_type",b.alpha_type},{"activation",b.alpha_type==ww_game_compat::AlphaTypeCurve?"force_when_zero":"observe_natural"},{"curve_name",b.curve_name},{"curve_name_words",b.curve_name_words},
            {"calls",g.calls.load()},{"forced",g.forced.load()},{"alpha",g.alpha.load()},{"delta",g.delta.load()},
            {"natural_positive_delta_updates",g.natural_positive_delta_updates.load()},{"activation_runtime_observed",g.forced.load()!=0||g.natural_positive_delta_updates.load()!=0}};
        j["native_physics_groups"].push_back(group);if(b.alpha_type==ww_game_compat::AlphaTypeCurve)j["native_curve_groups"].push_back(group);}
}
} // namespace v20_shared_native_physics
