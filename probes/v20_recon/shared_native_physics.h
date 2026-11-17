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
#include <cctype>

namespace v20_shared_native_physics {
namespace core=wwp_playback::native_physics;
namespace settings=wwp_playback::kawaii_parameters;
namespace collision=wwp_playback::kawaii_collision;
namespace gravity=wwp_playback::kawaii_gravity;
namespace graph=v20_native_graph_binding;
using Actor=core::Actor;
using Access=core::Access;
using AuthorityCheck=bool(*)(void*,const Access&);
using ue_memory::Require;
struct HookHub {
    core::Registry registry;
    v20_vmt_slot_patch::State converter_patch,group_patch,settings_patch;
    void (*original_converter)(void*,void*)=nullptr;
    void (*original_group)(void*,void*)=nullptr;
    void (*original_evaluate)(void*,void*,void*)=nullptr;
    void (*update_settings)(void*)=nullptr;
    const void* (*getter)(void*,int32_t)=nullptr;
    bool (*lod)(void*,void*)=nullptr;
    bool (*valid)(void*,void*,void*)=nullptr;
    void (*internal)(void*,void*)=nullptr;
    uint64_t image=0,converter_vtable=0,group_vtable=0;
    std::atomic<bool> observing{false},group_observing{false},settings_observing{false};
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
inline Access MakeAccess(){return {nullptr,&Read,&Write,&Writable,&Getter,&Gate,&Internal};}
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
    unsigned index=0;auto actor=hub.registry.Group(uintptr_t(node),index);if(!actor)return;
    core::ForceGroup(*actor,index,uintptr_t(node),uintptr_t(context),MakeAccess());
}
inline bool RefreshGroupSettings(uintptr_t node){
    __try{hub.update_settings(reinterpret_cast<void*>(node));return true;}
    __except(EXCEPTION_EXECUTE_HANDLER){return false;}
}
inline bool ReadCompactToSkeleton(const Actor& actor,uintptr_t context,const Access& access,std::array<int32_t,1024>& compact_to_skeleton){
    uint64_t container=0;settings::Array indices{};
    const auto count=actor.topology.count;
    if(!access.Get(context+ww_game_compat::AnimContextBoneContainerOffset,container)||!container||
       !access.Get(container+ww_game_compat::BoneContainerSkeletonToCompactOffset,indices)||!settings::ValidArray(indices)||
       indices.num!=int32_t(count))return false;
    std::array<int32_t,1024> skeleton_to_compact{};compact_to_skeleton.fill(-1);
    if(!access.Read(indices.data,skeleton_to_compact.data(),count*sizeof(int32_t)))return false;
    for(uint32_t i=0;i<count;++i){
        const int32_t compact=skeleton_to_compact[i];
        if(compact<0||uint32_t(compact)>=count||compact_to_skeleton[size_t(compact)]!=-1)return false;
        compact_to_skeleton[size_t(compact)]=int32_t(i);
    }
    return true;
}
inline void EvaluateDetour(void* node,void* context,void* out){
    ++hub.group_active;struct Active{~Active(){--hub.group_active;}} callback_scope;
    auto evaluate=[&]{hub.original_evaluate(node,context,out);};
    Dispatch dispatch(hub.settings_observing);if(!dispatch){evaluate();return;}
    unsigned index=0;auto actor=hub.registry.Group(uintptr_t(node),index);
    if(!actor||!actor->settings_verified){evaluate();return;}
    const auto access=MakeAccess();uint64_t proxy=0,mesh=0,owner=0,vtable=0;
    if(!access.Get(uintptr_t(context)+ww_game_compat::AnimContextProxyOffset,proxy)||!proxy||
       !access.Get(proxy+ww_game_compat::AnimProxyMeshOffset,mesh)||mesh!=actor->target.mesh||
       !access.Get(proxy+ww_game_compat::AnimProxyOwnerOffset,owner)||owner!=actor->target.main_anim||
       !access.Get(uintptr_t(node),vtable)||vtable!=actor->group_vtable||!core::Alive(*actor,access)){evaluate();return;}
    auto& group=actor->settings[index];
    const bool feed=core::Feeding(*actor);
    auto map=[&](auto& result){return ReadCompactToSkeleton(*actor,uintptr_t(context),access,result);};
    auto with_capsules=[&]{if(actor->capsules_verified)collision::EvaluateCapsules(actor->capsules[index],uintptr_t(node),access,feed,map,evaluate,
        [&]{core::Fail(*actor,610);});else evaluate();};
    auto with_gravity=[&]{if(actor->gravity_verified)gravity::EvaluateGravity(actor->gravity[index],uintptr_t(node),access,feed,with_capsules,
        [&]{core::Fail(*actor,612);},(actor->part_gravity_group_mask.load()&(1u<<index))!=0);else with_capsules();};
    settings::EvaluateGroup(group,uintptr_t(node),access,feed,with_gravity,
        &RefreshGroupSettings,[&]{core::Fail(*actor,608);},[&]{std::array<int32_t,1024> result{};
            return map(result)&&settings::MapChains(group,uintptr_t(node),access,result);});
}
template<class Reader>inline core::ObjectToken Capture(Reader& r,const Go& go,uint64_t address){
    core::ObjectToken t;t.address=address;std::array<uint8_t,ww_game_compat::ObjectItemBytes> item{};
    Require(r.Read(address,t.header.data(),ww_game_compat::ObjectHeaderBytes),"native actor object unreadable");t.index=At<uint32_t>(t.header.data(),ww_game_compat::ObjectIndexOffset);
    Require(ObjectItem(r,go,t.index,t.item),"native actor object index invalid");
    Require(r.Read(t.item,item.data(),ww_game_compat::ObjectItemBytes),"native actor object item invalid");t.serial=At<uint32_t>(item.data(),ww_game_compat::ObjectItemSerialOffset);return t;
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
    const bool source=ww_game_compat_witness::CompatibleId(cfg,ww_game_compat::CompatibilityId,checks,sourceError);
    const bool input=ww_game_compat_witness::VerifyFunctions(read,image,imageBytes,cfg.value("native_physics_witnesses",Json::array()),InputFunctionSpecs(),checks,"native_input",inputError);
    const bool activation=ww_game_compat_witness::VerifyFunctions(read,image,imageBytes,cfg.value("native_physics_activation_witnesses",Json::array()),ActivationFunctionSpecs(),checks,"native_activation",activationError);
    error=!sourceError.empty()?sourceError:!inputError.empty()?inputError:activationError;
    return source&&input&&activation;
}
template<class Reader>inline void VerifyNativeFunctions(Actor& a,Reader& r,const Json& cfg,const std::filesystem::path& directory){
    a.function_witnesses_verified.store(false,std::memory_order_release);
    a.function_witnesses_required.store(unsigned(InputFunctionSpecs().size()+ActivationFunctionSpecs().size()));
    a.function_witnesses_matched.store(0);
    Json checks=Json::array(),settingsChecks=Json::array(),capsuleChecks=Json::array();std::string error,settingsError,capsuleError;bool verified=false;
    try{
        MODULEINFO module{};
        Require(GetModuleInformation(GetCurrentProcess(),GetModuleHandleW(nullptr),&module,sizeof(module))!=0,"native physics main module unavailable");
        verified=VerifyNativeFunctionSets(r,uint64_t(module.lpBaseOfDll),module.SizeOfImage,cfg,checks,error);
        if(verified&&cfg.contains("native_physics_settings_witnesses")){
            const std::vector<ww_game_compat_witness::Function> specs={
                {"GroupEvaluate",ww_game_compat::FunctionGroupEvaluateRva,ww_game_compat::FunctionGroupEvaluateBytes},
                {"GroupUpdatePhysicsSettings",settings::kUpdateRva,settings::kUpdateBytes},
                {"GroupSimulate",settings::kSimulateRva,settings::kSimulateBytes},
                {"GroupApplyResult",settings::kApplyResultRva,settings::kApplyResultBytes}};
            auto read=[&](uint64_t address,void* out,size_t bytes){return r.Read(address,out,bytes);};
            a.settings_verified=ww_game_compat_witness::VerifyFunctions(read,uint64_t(module.lpBaseOfDll),module.SizeOfImage,
                cfg.at("native_physics_settings_witnesses"),specs,settingsChecks,"native_settings",settingsError);
        }
        if(a.settings_verified&&cfg.contains("native_physics_capsule_witnesses")){
            namespace compat=ww_game_compat;
            const std::vector<ww_game_compat_witness::Function> specs={
                {"GroupUpdateCapsuleLimits",compat::FunctionGroupUpdateCapsuleLimitsRva,compat::FunctionGroupUpdateCapsuleLimitsBytes},
                {"GroupWorldCollision",compat::FunctionGroupWorldCollisionRva,compat::FunctionGroupWorldCollisionBytes},
                {"GroupCollisionCandidate",compat::FunctionGroupCollisionCandidateRva,compat::FunctionGroupCollisionCandidateBytes},
                {"CapsuleContactsA",compat::FunctionCapsuleContactsARva,compat::FunctionCapsuleContactsABytes},
                {"CapsuleContactsB",compat::FunctionCapsuleContactsBRva,compat::FunctionCapsuleContactsBBytes},
                {"CapsuleContactsC",compat::FunctionCapsuleContactsCRva,compat::FunctionCapsuleContactsCBytes}};
            auto read=[&](uint64_t address,void* out,size_t bytes){return r.Read(address,out,bytes);};
            a.capsules_verified=ww_game_compat_witness::VerifyFunctions(read,uint64_t(module.lpBaseOfDll),module.SizeOfImage,
                cfg.at("native_physics_capsule_witnesses"),specs,capsuleChecks,"native_capsule_contacts",capsuleError);
        }
        std::array<float,4> signs{};
        a.gravity_verified=a.settings_verified&&gravity::kConjugateSignsRva+sizeof(signs)<=module.SizeOfImage&&
            r.Read(uint64_t(module.lpBaseOfDll)+gravity::kConjugateSignsRva,signs.data(),sizeof(signs))&&gravity::ConjugateSigns(signs);
    }catch(const std::exception& failure){error=failure.what();}
    unsigned matched=0;for(const auto& row:checks)if(row.value("kind",std::string())=="function_bytes"&&row.value("matched",false))++matched;
    a.function_witnesses_matched.store(matched);
    std::filesystem::create_directories(directory);
    ww_report::Save(directory/L"native_function_checks.json",{{"schema",1},{"compatibility_id",ww_game_compat::CompatibilityId},
        {"verified",verified},{"required_function_count",a.function_witnesses_required.load()},
        {"matched_function_count",matched},{"checks",checks},{"error",error},{"settings_verified",a.settings_verified},
        {"settings_checks",settingsChecks},{"settings_error",settingsError},{"capsules_verified",a.capsules_verified},
        {"capsule_checks",capsuleChecks},{"capsule_error",capsuleError},{"gravity_verified",a.gravity_verified}});
    Require(verified,error.empty()?"native physics function verification failed":error.c_str());
    a.function_witnesses_verified.store(true,std::memory_order_release);
}
template<class Reader>inline void PrepareBinding(Actor& a,Reader& r,const Json& cfg,const std::filesystem::path& directory,AuthorityCheck check,void* user){
    VerifyNativeFunctions(a,r,cfg,directory);
    a.binding=graph::Parse(cfg.at("native_graph_binding"));
    a.image=uint64_t(GetModuleHandleW(nullptr));a.target={};a.target.mesh=a.binding.target.address;a.target.mesh_index=a.binding.target.index;a.target.mesh_serial=a.binding.target.serial;
    a.target.policy=native_animation_target_config::Policy(cfg);a.target.main_anim=a.binding.main_anim.address;
    uint64_t owner=0,asset=0,skeleton=0;
    Require(r.Read(a.target.mesh+ww_game_compat::ComponentOwnerOffset,&owner,sizeof(uint64_t))&&r.Read(a.target.mesh+ww_game_compat::ComponentAssetOffset,&asset,sizeof(uint64_t))&&
        r.Read(asset+ww_game_compat::AssetSkeletonOffset,&skeleton,sizeof(uint64_t)),"native actor asset chain unavailable");
    a.target.actor=owner;a.target.mesh_asset=asset;a.target.skeleton_asset=skeleton;
    std::array<uint8_t,ww_game_compat::ObjectArrayHeaderBytes> go_bytes{};const auto go_address=a.image+graph::Address(cfg.at("gobjects_rva"));
    Require(r.Read(go_address,go_bytes.data(),ww_game_compat::ObjectArrayHeaderBytes),"native actor GObjects unreadable");
    const Go go{go_address,At<uint64_t>(go_bytes.data(),ww_game_compat::ObjectArrayTableOffset),At<uint32_t>(go_bytes.data(),ww_game_compat::ObjectArrayCountOffset)};
    const uint64_t addresses[]={a.target.actor,a.target.mesh,asset,skeleton,a.target.main_anim};for(unsigned i=0;i<5;++i)a.tokens[i]=Capture(r,go,addresses[i]);
    a.target.actor_index=a.tokens[0].index;a.target.actor_serial=a.tokens[0].serial;
    if(cfg.contains("animation_target"))
        a.target=native_animation_target_config::Parse(cfg,DiscoveryToken(a.tokens[0]),DiscoveryToken(a.tokens[1]),DiscoveryToken(a.tokens[2]),DiscoveryToken(a.tokens[3]),DiscoveryToken(a.tokens[4]));
    a.node=a.target.main_anim+a.binding.converter_offset;a.node_vtable=a.image+graph::kConverterVtableRva;a.group_vtable=a.image+graph::kGroupVtableRva;
    a.authority_alive=check;a.authority_user=user;a.prepared=true;
}
inline std::string LowerBoneName(std::string name){for(char& c:name)c=char(std::tolower(static_cast<unsigned char>(c)));return name;}
inline void PrepareSettingsChains(Actor& a,const Json& cfg){
    if(!a.settings_verified||!cfg.contains("expected_bone_names"))return;
    auto names=cfg.at("expected_bone_names").get<std::vector<std::string>>();
    if(names.size()!=a.topology.count)return;
    if(a.capsules_verified)a.physics_bone_names=names;
    for(auto& name:names)name=LowerBoneName(name);
    for(unsigned group=0;group<a.binding.group_count;++group){
        auto& state=a.settings[group];const auto& roots=a.binding.groups[group].root_bones;
        state.skeleton_chain.resize(names.size());
        for(unsigned slot=0;slot<roots.size();++slot){
            const auto found=std::find(names.begin(),names.end(),LowerBoneName(roots[slot]));
            if(found==names.end())continue;
            settings::Chain chain;chain.root_slot=slot+1;chain.root_bone=unsigned(found-names.begin());
            if(state.skeleton_chain[chain.root_bone])continue;
            state.chains.push_back(chain);state.skeleton_chain[chain.root_bone]=static_cast<unsigned short>(state.chains.size());
        }
        for(unsigned bone=1;bone<a.topology.count;++bone)
            if(!state.skeleton_chain[bone])state.skeleton_chain[bone]=state.skeleton_chain[size_t(a.topology.parents[bone])];
    }
}
inline void PrepareSleeves(Actor& a,const Json& cfg){
    if(!a.request.canonical_local_rotations||!cfg.contains("expected_bone_names"))return;
    auto names=cfg.at("expected_bone_names").get<std::vector<std::string>>();
    if(names.size()!=a.topology.count)return;
    for(auto& name:names)name=LowerBoneName(name);
    for(unsigned group=0;group<a.binding.group_count;++group){const auto& roots=a.binding.groups[group].root_bones;
        for(unsigned slot=0;slot<roots.size();++slot){const auto name=LowerBoneName(roots[slot]);
            if(name.find("yixiu")==std::string::npos&&name.find("xiuzi")==std::string::npos&&name.find("sleeve")==std::string::npos)continue;
            const auto found=std::find(names.begin(),names.end(),name);if(found==names.end())continue;
            wwp_playback::sleeve_follow::Chain chain;chain.group=group+1;chain.root_slot=slot+1;chain.root=unsigned(found-names.begin());
            int32_t shoulder=a.topology.parents[chain.root];
            while(shoulder>0&&names[size_t(shoulder)]!="bip001lclavicle"&&names[size_t(shoulder)]!="bip001rclavicle")shoulder=a.topology.parents[size_t(shoulder)];
            if(shoulder<=0)continue;
            chain.shoulder=unsigned(shoulder);chain.shoulder_parent=unsigned(a.topology.parents[chain.shoulder]);
            const auto& neutral=a.request.canonical_local_rotations[chain.shoulder];
            std::copy(neutral.begin(),neutral.end(),chain.neutral_shoulder.begin());
            if(!(std::abs(wwp_playback::sleeve_follow::bone::Norm(chain.neutral_shoulder)-1)<=.001))continue;
            chain.neutral_shoulder=wwp_playback::sleeve_follow::bone::Unit(chain.neutral_shoulder);
            std::array<bool,core::input::kLimit> selected{};selected[chain.root]=true;chain.bones.push_back(chain.root);
            for(unsigned bone=chain.root+1;bone<a.topology.count;++bone)if(selected[unsigned(a.topology.parents[bone])]){
                selected[bone]=true;chain.bones.push_back(bone);}
            if(std::any_of(a.sleeves.begin(),a.sleeves.end(),[&](const auto& other){
                return selected[other.root]||std::binary_search(other.bones.begin(),other.bones.end(),chain.root);}))continue;
            a.sleeves.push_back(std::move(chain));
        }
    }
}
template<class Reader>inline void PrepareFromConfig(Actor& a,Reader& r,const Json& cfg,const std::filesystem::path& directory,
    const wwp_playback::local_clip::Clip& motion,const wwp_ue::full_body::Topology& topology,const wwp_ue::full_body::Request& request,uint64_t epoch,AuthorityCheck check,void* user){
    core::PrepareMotion(a,motion,topology,request,epoch);PrepareBinding(a,r,cfg,directory,check,user);
    Require(a.binding.bone_count==topology.count,"native actor topology differs");
    PrepareSettingsChains(a,cfg);
    PrepareSleeves(a,cfg);
}
inline bool RegisterActor(Actor& a){return a.prepared&&a.function_witnesses_verified.load(std::memory_order_acquire)&&hub.registry.Add(a);}
inline bool UnregisterActor(Actor& a){return hub.registry.Remove(a);}
inline void Publish(Actor& a,double seconds,const wwp_playback::placement::Settings& placement,bool feed){core::Publish(a,seconds,placement,feed);}
inline void CloseAdmission(Actor& a){core::CloseAdmission(a);}
inline bool Restored(const Actor& a){return core::Restored(a);}
inline bool CanSetParameter(const Actor& actor,unsigned group,unsigned chain){
    if(!actor.settings_verified||!group||group>actor.binding.group_count)return false;
    const auto& chains=actor.settings[group-1].chains;
    return !chain||std::any_of(chains.begin(),chains.end(),[&](const auto& row){return row.root_slot==chain;});
}
inline void SetPreview(Actor& actor,unsigned group,bool enabled){
    std::lock_guard<std::mutex> lock(actor.preview_lock);
    if(!enabled&&actor.preview_group.load(std::memory_order_relaxed)!=group)return;
    actor.preview_frame=0;actor.preview_solved_bones=0;actor.preview_pose.clear();
    actor.preview_group.store(enabled?group:0,std::memory_order_release);
}
inline void SetParameter(Actor& actor,unsigned group,unsigned chain,unsigned parameter,float value,uint64_t revision){
    auto& entry=actor.settings[group-1];std::lock_guard<std::mutex> lock(entry.edit);
    if(!chain){entry.requested[parameter]=value;entry.revision=revision;return;}
    for(auto& row:entry.chains)if(row.root_slot==chain){row.requested[parameter]=value;row.revision=revision;return;}
}
inline auto FindSleeve(Actor& actor,unsigned group,unsigned chain){return std::find_if(actor.sleeves.begin(),actor.sleeves.end(),
    [&](const auto& sleeve){return sleeve.group==group&&sleeve.root_slot==chain;});}
inline void Install(){
    std::lock_guard<std::mutex> lifecycle(hub.lifecycle);
    Require(hub.registry.Freeze(),"native shared registry cannot be frozen");
    for(const auto* actor:hub.registry.actors)Require(actor->prepared&&actor->function_witnesses_verified.load(std::memory_order_acquire),"native actor functions were not verified before installation");
    const auto& first=*hub.registry.actors[0];bool groups=false;for(unsigned i=0;i<hub.registry.count;++i)groups|=hub.registry.actors[i]->binding.group_count!=0;
    hub.image=first.image;hub.converter_vtable=first.node_vtable;hub.group_vtable=first.group_vtable;
    hub.original_converter=(void(*)(void*,void*))(hub.image+ww_game_compat::FunctionLocalToComponentRva);hub.original_group=(void(*)(void*,void*))(hub.image+ww_game_compat::FunctionSharedSkeletalControlUpdateRva);
    hub.getter=(const void*(*)(void*,int32_t))(hub.image+ww_game_compat::FunctionComponentSpaceTransformGetterRva);hub.lod=(bool(*)(void*,void*))(hub.image+ww_game_compat::FunctionSkeletalControlLODAllowedRva);
    hub.valid=(bool(*)(void*,void*,void*))(hub.image+ww_game_compat::FunctionGroupIsValidToEvaluateRva);hub.internal=(void(*)(void*,void*))(hub.image+ww_game_compat::FunctionSharedUpdateInternalRva);
    hub.observing.store(true);
    Require(v20_vmt_slot_patch::Install(hub.converter_patch,(void**)(hub.converter_vtable+ww_game_compat::SlotLocalToComponentEvaluateCS),(void*)hub.original_converter,(void*)&ConverterDetour).code==v20_vmt_slot_patch::Code::Ok,"native shared converter installation failed");
    if(groups){hub.group_observing.store(true,std::memory_order_release);
        Require(v20_vmt_slot_patch::Install(hub.group_patch,(void**)(hub.group_vtable+ww_game_compat::SlotSkeletalControlUpdate),(void*)hub.original_group,(void*)&GroupDetour).code==v20_vmt_slot_patch::Code::Ok,"native shared group installation failed");}
    const bool edit=std::any_of(hub.registry.actors.begin(),hub.registry.actors.end(),[](const auto* actor){return actor->settings_verified&&actor->binding.group_count;});
    if(edit){
        hub.original_evaluate=reinterpret_cast<void(*)(void*,void*,void*)>(hub.image+ww_game_compat::FunctionGroupEvaluateRva);
        hub.update_settings=reinterpret_cast<void(*)(void*)>(hub.image+settings::kUpdateRva);
        hub.settings_observing.store(true,std::memory_order_release);
        Require(v20_vmt_slot_patch::Install(hub.settings_patch,(void**)(hub.group_vtable+ww_game_compat::SlotKawaiiGroupEvaluate),
            (void*)hub.original_evaluate,(void*)&EvaluateDetour).code==v20_vmt_slot_patch::Code::Ok,"native settings installation failed");
    }
}
inline bool CloseDispatchForRestore(uint64_t deadline){
    const bool converter=hub.observing.exchange(false),group=hub.group_observing.exchange(false),evaluate=hub.settings_observing.exchange(false);
    while(hub.dispatch_active.load()&&GetTickCount64()<deadline)Sleep(1);
    if(hub.dispatch_active.load()||!hub.registry.RestoredAll()){
        // An admitted callback may have written input before terminal feed
        // closed. Keep natural recovery routing available until it retires.
        hub.observing.store(converter);hub.group_observing.store(group);hub.settings_observing.store(evaluate);return false;}
    return true;
}
inline bool Uninstall(){
    std::lock_guard<std::mutex> lifecycle(hub.lifecycle);
    {std::lock_guard<std::mutex> lock(hub.registry.operations);
        for(unsigned i=0;i<hub.registry.count;++i)core::CloseAdmission(*hub.registry.actors[i]);}
    if(!CloseDispatchForRestore(GetTickCount64()+5000))return false;bool ok=true;
    for(auto* patch:{&hub.converter_patch,&hub.group_patch,&hub.settings_patch})
        if(patch->changed||patch->protection_pending)ok=v20_vmt_slot_patch::Restore(*patch).code==v20_vmt_slot_patch::Code::Ok&&ok;
    const auto deadline=GetTickCount64()+5000;Sleep(200);while((hub.active.load()||hub.group_active.load())&&GetTickCount64()<deadline)Sleep(1);
    ok=ok&&!hub.active.load()&&!hub.group_active.load()&&!hub.dispatch_active.load()&&hub.registry.RestoredAll();if(ok)hub.registry.Thaw();return ok;
}
inline Json SampleSettings(const settings::Values& requested,uint64_t revision,const settings::Sample& sample,bool pending){
    return {{"requested",requested},{"revision",revision},{"sampled_revision",sample.revision},
        {"original",sample.read?Json(sample.original):Json(nullptr)},{"effective_first",sample.effective_read?Json(sample.effective_first):Json(nullptr)},
        {"effective_last",sample.effective_read?Json(sample.effective_last):Json(nullptr)},{"simulated_bones",sample.bones},{"restore_pending",pending}};
}
inline void Stats(const Actor& a,Json& j){
    j["native_physics_settings_verified"]=a.settings_verified;
    j["native_physics_gravity_verified"]=a.gravity_verified;
    if(a.preview_group.load(std::memory_order_acquire)){
        std::lock_guard<std::mutex> lock(a.preview_lock);
        const auto group=a.preview_group.load(std::memory_order_relaxed);
        if(group)j["native_preview"]={{"group_id",group},{"frame",a.preview_frame},{"positions",a.preview_pose},{"solved_bones",a.preview_solved_bones}};
    }
    j["native_function_witnesses_verified"]=a.function_witnesses_verified.load();
    j["native_function_witnesses_required"]=a.function_witnesses_required.load();j["native_function_witnesses_matched"]=a.function_witnesses_matched.load();
    j["native_input_frames"]=a.frames.load();j["native_input_error"]=a.error.load();j["native_restore_pending"]=a.pending.load();
    unsigned curve_count=0;for(unsigned i=0;i<a.binding.group_count;++i)curve_count+=a.binding.groups[i].alpha_type==ww_game_compat::AlphaTypeCurve;
    j["native_physics_group_count"]=a.binding.group_count;j["native_group_force_updates"]=a.group_forced.load();j["native_group_force_pending"]=a.force_pending.load();
    j["native_curve_group_count"]=curve_count;
    unsigned observed_groups=0;for(unsigned i=0;i<a.binding.group_count;++i)if(a.groups[i].forced.load()||a.groups[i].natural_positive_delta_updates.load())++observed_groups;
    const bool activation_observed=a.binding.group_count&&observed_groups==a.binding.group_count;
    j["native_secondary_motion_activation_runtime_observed"]=activation_observed;
    j["native_activation_observed_group_count"]=observed_groups;
    j["native_secondary_motion_activation_status"]=activation_observed?"activation_runtime_observed":observed_groups?"activation_partially_observed":"activation_not_observed";
    j["native_curve_groups"]=Json::array();j["native_physics_groups"]=Json::array();for(unsigned i=0;i<a.binding.group_count;++i){const auto& g=a.groups[i];const auto& b=a.binding.groups[i];
        Json group={{"offset",Hex(b.offset)},{"alpha_type",b.alpha_type},{"activation",b.alpha_type==ww_game_compat::AlphaTypeCurve?"force_when_zero":"observe_natural"},{"curve_name",b.curve_name},{"curve_name_words",b.curve_name_words},
            {"calls",g.calls.load()},{"forced",g.forced.load()},{"alpha",g.alpha.load()},{"delta",g.delta.load()},
            {"natural_positive_delta_updates",g.natural_positive_delta_updates.load()},{"activation_runtime_observed",g.forced.load()!=0||g.natural_positive_delta_updates.load()!=0}};
        group["group_id"]=i+1;group["root_bones"]=b.root_bones;
        if(a.gravity_verified){const auto& state=a.gravity[i];std::lock_guard<std::mutex> lock(state.edit);const auto& sample=state.sample;
            group["gravity"]={{"requested",state.requested},{"revision",state.revision},{"sampled_revision",sample.revision},
                {"original",sample.read?Json(sample.original):Json(nullptr)},{"effective",sample.read?Json(sample.effective):Json(nullptr)},
                {"applied",sample.applied},{"restored",sample.restored},{"restore_pending",state.restore_pending.load()},
                {"suspended_by_parts",sample.suspended_by_parts}};}
        if(!a.sleeves.empty()){
            std::lock_guard<std::mutex> lock(a.sleeve_edit);
            for(const auto& sleeve:a.sleeves)if(sleeve.group==i+1)group["sleeves"].push_back({{"chain_id",sleeve.root_slot},
                {"root",b.root_bones[sleeve.root_slot-1]},{"follow",sleeve.follow},{"sampled_follow",sleeve.sampled_follow},
                {"applied_frames",sleeve.applied_frames},{"peak_correction_degrees",sleeve.peak_correction_degrees}});
        }
        if(a.settings_verified){const auto& entry=a.settings[i];std::lock_guard<std::mutex> lock(entry.edit);
            const bool pending=entry.restore_pending.load();group["settings"]=SampleSettings(entry.requested,entry.revision,entry.sampled,pending);
            for(const auto& chain:entry.chains)group["chains"].push_back({{"root",b.root_bones[chain.root_slot-1]},{"chain_id",chain.root_slot},
                {"settings",SampleSettings(chain.requested,chain.revision,chain.sampled,pending)}});
        }
        if(a.capsules_verified){const auto& state=a.capsules[i];std::lock_guard<std::mutex> lock(state.edit);
            group["capsule_restore_pending"]=state.restore_pending.load();
            group["capsule_shapes"]=Json::array();
            for(const auto& shape:state.shapes){
                Json item={{"id",shape.id},{"bone",shape.bone},{"enabled",shape.enabled},{"active",shape.active},
                    {"driver",shape.bone>=0&&size_t(shape.bone)<a.physics_bone_names.size()?a.physics_bone_names[size_t(shape.bone)]:std::string{}},
                    {"editable",shape.geometry_available}};
                if(shape.id>=collision::kFirstAuthoredId)item["layout_index"]=shape.id-collision::kFirstAuthoredId+1;
                if(shape.geometry_available){item["baseline"]=shape.baseline;item["values"]=collision::Effective(shape);item["rotation"]=shape.rotation;}
                group["capsule_shapes"].push_back(std::move(item));
            }
            if(state.available){group["capsules"]=Json::array();for(const auto& driver:state.drivers)if(driver.bone<a.physics_bone_names.size())
                group["capsules"].push_back({{"driver",a.physics_bone_names[driver.bone]},{"driver_id",driver.bone+1},
                    {"enabled",driver.enabled},{"original_count",driver.original},{"evaluated_count",driver.active}});}
        }
        j["native_physics_groups"].push_back(group);if(b.alpha_type==ww_game_compat::AlphaTypeCurve){group.erase("settings");group.erase("chains");group.erase("capsules");group.erase("capsule_shapes");j["native_curve_groups"].push_back(group);}}
}
} // namespace v20_shared_native_physics
