// Independent discovery carrier. Fixed vtable slots are temporarily patched.
// Original callbacks run exactly once; no pose/alpha/timing writes, engine
// getter, native UpdateInternal call or DLL unloading.
#include "observer_carrier.h"
#include "native_animation_target_config.h"
#include "object_identity.h"
#include "../../playback/native_kawaii_parameters.h"

namespace graph_carrier {
namespace ng=native_graph_discovery;
namespace oc=observer_carrier;
namespace compat=ww_game_compat;namespace witness=ww_game_compat_witness;
namespace kawaii=wwp_playback::kawaii_parameters;
static HMODULE selfModule=nullptr;static volatile LONG started=0;
static ng::Binding binding;static ng::Recorder recorder;static oc::Hooks hooks;
static std::atomic<bool> observing{false};
static std::atomic<uint64_t> observedMainProxy{0};
static std::array<std::atomic<uint64_t>,ng::kHooks> targetCalls{};
static std::atomic<uint64_t> unreadableTarget{0};
static bool rootLayoutVerified=false;
static bool observePhysicsSettings=false;
static std::vector<std::string> referenceBoneNames;
static Json rootSetJson=Json::array(); // Worker-only decoded cache, after quiescence.
static constexpr uint64_t kRootRvas[]={compat::FunctionGroupInitializeBoneReferencesRva,compat::FunctionGroupInitializeModifyBonesRva};
static constexpr unsigned kRootBytes[]={compat::FunctionGroupInitializeBoneReferencesBytes,compat::FunctionGroupInitializeModifyBonesBytes};
struct Entry {uint64_t sequence=0;int32_t slot=-1;bool read=false,target=false;};
// Diagnostic sample of the group's authored settings and the first/last
// cached simulated bone, enabled by WWPOSER_OBSERVE_KAWAII_SETTINGS=1.
static void CapturePhysicsSettings(ng::Phase& phase) {
    ue_memory::ArrayHead bones,after;
    phase.settings_read=oc::Read(phase.node+kawaii::kNodeSettings,phase.settings.data(),sizeof(phase.settings))&&
        oc::Read(phase.node+kawaii::kInitialized,&phase.settings_cache_initialized,1)&&
        oc::Read(phase.node+kawaii::kRefresh,&phase.settings_refresh_each_evaluation,1);
    if(!phase.settings_read||!oc::Read(phase.node+kawaii::kBoneArray,&bones,sizeof(bones))||!ng::Array(bones))return;
    phase.settings_cache_count=bones.num;
    if(!bones.num||!phase.settings_cache_initialized||!ng::Range(bones.data,size_t(bones.num)*kawaii::kBoneStride))return;
    const auto first=bones.data+kawaii::kBoneSettings;
    const auto last=first+size_t(bones.num-1)*kawaii::kBoneStride;
    phase.settings_cache_read=oc::Read(first,phase.settings_cache_first.data(),sizeof(phase.settings_cache_first))&&
        oc::Read(last,phase.settings_cache_last.data(),sizeof(phase.settings_cache_last))&&
        oc::Read(phase.node+kawaii::kBoneArray,&after,sizeof(after))&&ng::SameHead(bones,after);
}
static void CaptureObservation(unsigned hook,void* node,void* context,ng::Phase& phase,ng::Mapping& mapping,ng::RootSnapshot* roots){
    ng::CapturePhase(oc::Read,binding,ng::Kind(hook),reinterpret_cast<uint64_t>(node),reinterpret_cast<uint64_t>(context),phase,mapping,roots,observedMainProxy.load(std::memory_order_acquire));
    if(phase.type_verified&&phase.owner_role_verified&&phase.owner_is_main)observedMainProxy.store(phase.proxy,std::memory_order_release);
    if(observePhysicsSettings&&phase.type_verified&&phase.owner_role_verified&&
        (hook==unsigned(ng::Kind::GroupUpdate)||hook==unsigned(ng::Kind::GroupEvaluate)))CapturePhysicsSettings(phase);
}
// These helper frames are gone before forwarding the recursive graph call.
// Keeping 1024-bone scratch arrays in Detour would multiply stack use by the
// number of nested skeletal-control nodes in the authored graph.
__declspec(noinline) static Entry BeginObservation(unsigned hook,void* node,void* context){
    Entry entry;entry.sequence=recorder.sequence.fetch_add(1)+1;ng::Phase before;ng::Mapping mapping;ng::RootSnapshot roots;
    CaptureObservation(hook,node,context,before,mapping,rootLayoutVerified?&roots:nullptr);
    entry.read=before.read;entry.target=before.mesh==binding.mesh.address;
    if(entry.target&&entry.read){++targetCalls[hook];entry.slot=recorder.Begin(hook,GetCurrentThreadId(),entry.sequence,before,mapping,false,rootLayoutVerified?&roots:nullptr);}
    return entry;
}
__declspec(noinline) static void EndObservation(unsigned hook,void* node,void* context,Entry entry){
    const uint64_t exit=recorder.sequence.fetch_add(1)+1;ng::Phase after;ng::Mapping mapping;ng::RootSnapshot roots;
    CaptureObservation(hook,node,context,after,mapping,rootLayoutVerified?&roots:nullptr);
    // A L2C/base output context may only be populated by its original. Preserve
    // that distinction: the entry context is null, never fabricated.
    const bool outputContext=hook==unsigned(ng::Kind::LocalToComponent)||hook==unsigned(ng::Kind::GroupBaseCS)||hook==unsigned(ng::Kind::SingleBaseCS);
    if(entry.slot<0&&!entry.read&&after.read&&after.mesh==binding.mesh.address&&outputContext){
        ++targetCalls[hook];entry.slot=recorder.Begin(hook,GetCurrentThreadId(),entry.sequence,after,mapping,true,rootLayoutVerified?&roots:nullptr);
    }
    if((entry.target||after.mesh==binding.mesh.address)&&(!after.read||(!entry.read&&!outputContext)))++unreadableTarget;
    recorder.End(entry.slot,exit,after,mapping,rootLayoutVerified?&roots:nullptr);
}
static void Detour(unsigned hook,void* node,void* context,void* out){
    const DWORD incomingError=GetLastError();++hooks.active;++hooks.calls[hook];
    struct Active {~Active(){--hooks.active;}} activeScope;
    const bool enabled=observing.load(std::memory_order_acquire)&&!recorder.window_closed.load(std::memory_order_acquire);
    Entry entry;if(enabled)entry=BeginObservation(hook,node,context);
    SetLastError(incomingError);
    hooks.Forward(hook,node,context,out);
    const DWORD originalError=GetLastError();
    if(enabled)EndObservation(hook,node,context,entry);
    SetLastError(originalError);
}
using Trampolines=oc::Trampolines<&Detour>;
struct Config {
    unsigned duration_ms=5000,node_limit=ng::kNodes,frame_limit=ng::kFrames;
    uint64_t go_rva=0,pool_rva=0;std::string profile,skeleton_sha;
    unsigned bone_count=0;std::vector<std::string> bone_names;std::vector<int32_t> parents;
    Json root_witnesses;
};
static Config ParseConfig(const Json& j){Config c;
    const auto& d=j.at("native_graph_discovery");
    c.duration_ms=d.at("duration_ms");c.node_limit=d.at("node_limit");c.frame_limit=d.at("frame_limit");
    c.go_rva=oc::Address(j.at("gobjects_rva"));c.pool_rva=oc::Address(j.at("name_pool_rva"));c.profile=j.at("character_profile_id");c.bone_count=j.at("expected_bone_count");
    c.skeleton_sha=j.at("expected_skeleton_sha256");
    c.bone_names=j.at("expected_bone_names").get<std::vector<std::string>>();c.parents=j.at("expected_bone_parents").get<std::vector<int32_t>>();
    c.root_witnesses=d.at("root_layout_witnesses");
    if(!c.bone_count||c.bone_count>ng::kBones||c.parents.size()!=c.bone_count||c.bone_names.size()!=c.bone_count)throw std::runtime_error("reference topology bounds differ");
    return c;
}
static bool FullName(Reader& r,const Pool& pool,const std::array<uint32_t,3>& words,std::string& value){
    return ::FullName(r,pool,words[0],words[2],value)&&!value.empty();
}
static std::array<uint32_t,3> NameWords(const ng::Token& t){return {{At<uint32_t>(t.header.data(),compat::ObjectNameOffset),At<uint32_t>(t.header.data(),compat::ObjectNameOffset+compat::NameWordBytes),At<uint32_t>(t.header.data(),compat::ObjectNameOffset+compat::NameNumberOffset)}};}
static Json TokenJson(Reader& r,const Pool& pool,const ng::Token& t){
    std::string name,className;ng::Token classToken;FullName(r,pool,NameWords(t),name);
    const bool classRead=ng::CaptureToken(oc::Read,binding.go,ng::Class(t),classToken,false);if(classRead)FullName(r,pool,NameWords(classToken),className);
    return {{"address",Hex(t.address)},{"item_address",Hex(t.item)},{"index",ng::Index(t)},{"serial",ng::Serial(t)},
        {"class_address",Hex(ng::Class(t))},{"class_name",className},
        {"outer_address",Hex(At<uint64_t>(t.header.data(),compat::ObjectOuterOffset))},{"vtable",Hex(At<uint64_t>(t.header.data(),compat::ObjectVtableOffset))},
        {"object_flags",At<uint32_t>(t.header.data(),compat::ObjectFlagsOffset)},{"item_flags",At<uint32_t>(t.item_bytes.data(),compat::ObjectItemFlagsOffset)},
        {"name",name},{"name_words",NameWords(t)},{"class_name_words",classRead?Json(NameWords(classToken)):Json(nullptr)}};
}
static Json BindingJson(Reader& r,const Pool& pool,const ng::Binding& b){Json j;
    const char* labels[]={"mesh","pawn","controller","mesh_asset","skeleton_asset","main_anim"};
    const ng::Token* tokens[]={&b.mesh,&b.pawn,&b.controller,&b.mesh_asset,&b.skeleton_asset,&b.main_anim};
    for(unsigned i=0;i<6;++i)j[labels[i]]=tokens[i]->address?TokenJson(r,pool,*tokens[i]):Json(nullptr);
    if(b.authority.policy!=native_animation_target::OwnerPolicy::ControlledPlayer){j["actor"]=j["pawn"];j["owner_policy"]=native_animation_target::PolicyName(b.authority.policy);
        j["session_epoch"]=Hex(b.authority.epoch);j["authority_address"]=Hex(b.authority.authority_address);}
    return j;
}
static ng::Binding Bind(Reader& r,const Pool& pool,const Go& go,const Config& c,const Json& cfg,uint64_t image){
    ng::Binding b;b.image=image;b.go={go.table,go.count};b.bone_count=c.bone_count;std::copy(c.parents.begin(),c.parents.end(),b.parents.begin());
    auto capture=[&](const char* label,uint64_t address,ng::Token& t){if(!ng::CaptureToken(oc::Read,b.go,address,t))
        throw std::runtime_error(std::string("strong UObject identity unavailable: ")+label+" address="+Hex(address));};
    capture("mesh",oc::Address(cfg.at("object")),b.mesh);capture("actor",oc::Address(cfg.at("outer_address")),b.pawn);
    if(native_animation_target_config::Policy(cfg)==native_animation_target::OwnerPolicy::ControlledPlayer)capture("controller",oc::Address(cfg.at("controller_address")),b.controller);
    uint64_t asset=0,skeleton=0,main=0;
    if(!oc::Read(b.mesh.address+compat::ComponentAssetOffset,&asset,compat::PointerBytes)||!oc::Read(asset+compat::AssetSkeletonOffset,&skeleton,compat::PointerBytes)||
       !oc::Read(b.mesh.address+compat::ComponentMainAnimOffset,&main,compat::PointerBytes))throw std::runtime_error("main/assets unavailable");
    capture("mesh_asset",asset,b.mesh_asset);capture("skeleton_asset",skeleton,b.skeleton_asset);capture("main_anim",main,b.main_anim);
    b.authority=native_animation_target_config::Parse(cfg,b.pawn,b.mesh,b.mesh_asset,b.skeleton_asset,b.main_anim);
    ue_memory::ArrayHead h;std::vector<uint8_t> rows(size_t(c.bone_count)*compat::ReferenceBoneInfoBytes);
    if(!oc::Read(asset+compat::AssetRefBoneInfoOffset,&h,sizeof(h))||h.num!=int32_t(c.bone_count)||!oc::Read(h.data,rows.data(),rows.size()))throw std::runtime_error("reference table unavailable");
    for(unsigned i=0;i<c.bone_count;++i){const uint8_t* p=rows.data()+i*compat::ReferenceBoneInfoBytes;std::string bone;
        const std::array<uint32_t,3> words{{At<uint32_t>(p,0),At<uint32_t>(p,compat::NameWordBytes),At<uint32_t>(p,compat::NameNumberOffset)}};
        if(!FullName(r,pool,words,bone)||!ww_object::SameName(bone,c.bone_names[i])||At<int32_t>(p,compat::ReferenceBoneParentOffset)!=c.parents[i])throw std::runtime_error("full reference topology differs");}
    return b;
}
static Json HeadJson(const ue_memory::ArrayHead& h){return {{"data",Hex(h.data)},{"num",h.num},{"capacity",h.max}};}
static Json RootJson(Reader& r,const Pool& pool,const ng::RootSnapshot& roots){
    Json names=Json::array(),wordsList=Json::array(),indices=Json::array();bool decoded=roots.read&&roots.roots.num>0;
    if(roots.read)for(int32_t i=0;i<roots.roots.num;++i){const auto* row=roots.bytes.data()+size_t(i)*ng::kRootStride;
        const std::array<uint32_t,3> words{{At<uint32_t>(row,0),At<uint32_t>(row,compat::NameWordBytes),At<uint32_t>(row,compat::NameNumberOffset)}};
        std::string name;const bool named=FullName(r,pool,words,name);const auto found=std::find(referenceBoneNames.begin(),referenceBoneNames.end(),name);
        const bool matched=named&&found!=referenceBoneNames.end();decoded=decoded&&matched;wordsList.push_back(words);names.push_back(named?Json(name):Json(nullptr));
        indices.push_back(matched?Json(size_t(found-referenceBoneNames.begin())):Json(nullptr));
    }
    const bool verified=rootLayoutVerified&&roots.headers_read&&roots.extended.num==0&&decoded;
    const char* reason=!rootLayoutVerified?"layout_not_verified":!roots.headers_read?"headers_unreadable":roots.extended.num!=0?"extended_root_array_not_empty":
        !roots.read?"root_array_unreadable_or_out_of_bounds":!roots.roots.num?"empty_root_array":!decoded?"root_fname_unresolved_or_absent_from_reference":"verified";
    return {{"root_bones",names},{"root_bones_verified",verified},{"root_bones_reason",reason},{"root_bone_name_words",wordsList},
        {"root_reference_indices",indices},{"root_array",HeadJson(roots.roots)},{"extended_root_array",HeadJson(roots.extended)},
        {"root_layout_verified",rootLayoutVerified},{"root_stride",ng::kRootStride}};
}
static Json PhaseJson(Reader& r,const Pool& pool,const ng::Phase& p,unsigned hook){if(!p.read)return nullptr;
    const bool relative=p.type_verified&&p.owner_verified&&p.owner_role_verified&&p.owner_is_main&&p.node>=binding.main_anim.address&&p.node-binding.main_anim.address<=0x100000;
    Json j={{"node",Hex(p.node)},{"context",Hex(p.context)},{"proxy",Hex(p.proxy)},{"mesh",Hex(p.mesh)},
        {"owner_a0",Hex(p.owner_a0)},{"owner_a8",Hex(p.owner_a8)},{"main_proxy",Hex(p.main_proxy)},{"skeleton",Hex(p.skeleton)},
        {"vtable",Hex(p.vtable)},{"frame",p.frame},{"counter",p.counter},{"main_frame",p.main_frame},{"main_counter",p.main_counter},
        {"type_verified",p.type_verified},{"owner_verified",p.owner_verified},{"owner_role_verified",p.owner_role_verified},
        {"owner_role",p.owner_role_verified?(p.owner_is_main?"main":"linked"):"unverified"},{"owner",p.owner_verified?TokenJson(r,pool,p.owner):Json(nullptr)},
        {"offset_from_main",relative?Json(Hex(p.node-binding.main_anim.address)):Json(nullptr)},{"external_route",p.external_route}};
    const bool fields=p.node_fields_read&&p.type_verified&&hook!=unsigned(ng::Kind::LocalToComponent);
    std::string curve;const bool named=fields&&FullName(r,pool,p.curve_name_words,curve);
    j["node_fields"]={{"verified",fields},{"alpha",fields&&std::isfinite(p.alpha)?Json(p.alpha):Json(nullptr)},{"alpha_type",fields?Json(p.alpha_type):Json(nullptr)},
        {"delta",fields&&std::isfinite(p.delta)?Json(p.delta):Json(nullptr)},{"update_delta",hook==unsigned(ng::Kind::GroupUpdate)&&std::isfinite(p.update_delta)?Json(p.update_delta):Json(nullptr)},
        {"curve_name_offset",Hex(compat::CurveNameOffset)},{"curve_name_words",fields?Json(p.curve_name_words):Json(nullptr)},
        {"curve_name",named?Json(curve):Json(nullptr)},{"curve_semantically_active",fields&&p.alpha_type==compat::AlphaTypeCurve}};
    j["node_fields"]["root_bones"]=Json::array();j["node_fields"]["root_bones_verified"]=false;
    j["node_fields"]["root_bones_reason"]=rootLayoutVerified?"root_snapshot_unavailable":"layout_not_verified";
    if(p.settings_read){
        const auto namedSettings=[](const std::array<float,7>& values){
            return Json{{"damping",values[0]},{"world_damping_location",values[1]},{"world_damping_rotation",values[2]},
                {"stiffness",values[3]},{"radius",values[4]},{"limit_angle_degrees",values[5]},{"half_width",values[6]}};
        };
        j["kawaii_settings"]={{"node_values",namedSettings(p.settings)},{"cache_initialized",p.settings_cache_initialized},
            {"refresh_each_evaluation",p.settings_refresh_each_evaluation},{"cache_count",p.settings_cache_count},
            {"cache_first",p.settings_cache_read?namedSettings(p.settings_cache_first):Json(nullptr)},
            {"cache_last",p.settings_cache_read?namedSettings(p.settings_cache_last):Json(nullptr)}};
    }
    if(p.root_snapshot_id>=0&&size_t(p.root_snapshot_id)<rootSetJson.size()){
        const auto& roots=rootSetJson[size_t(p.root_snapshot_id)];for(auto it=roots.begin();it!=roots.end();++it)j["node_fields"][it.key()]=it.value();
        j["node_fields"]["root_snapshot_id"]=p.root_snapshot_id;
    }
    j["mapping_applicable"]=p.mapping_applicable;if(p.mapping_applicable)j["mapping"]={{"id",p.mapping_id},{"pose_headers_read",p.pose_headers_read},{"headers_read",p.headers_read},
        {"bone_container",Hex(p.bone_container)},{"transforms",HeadJson(p.transforms)},{"flags",HeadJson(p.flags)},{"extra_vectors",HeadJson(p.extra_vectors)},
        {"extra_active",p.extra_active},{"extra_vectors_absent",p.extra_vectors_absent},{"skeleton_to_compact",HeadJson(p.skeleton_to_compact)},
        {"compact_parents",HeadJson(p.compact_parents)},{"weak_mesh",p.weak_mesh},{"weak_skeleton",p.weak_skeleton},{"weak_assets_match",p.weak_assets_match}};
    return j;
}
static void CaptureJson(Reader& r,const Pool& pool,Json& report){
    report["nodes"]=Json::array();report["events"]=Json::array();report["frames"]=Json::array();report["mappings"]=Json::array();
    rootSetJson=Json::array();for(unsigned i=0;i<recorder.root_set_count;++i){auto root=RootJson(r,pool,recorder.root_sets[i]);root["id"]=i;rootSetJson.push_back(root);}
    report["root_signatures"]=rootSetJson;
    for(unsigned i=0;i<recorder.node_count;++i){const auto& n=recorder.nodes[i];const bool relative=n.owner_is_main&&n.address>=binding.main_anim.address&&n.address-binding.main_anim.address<=0x100000;
        report["nodes"].push_back({{"id",i},{"node",Hex(n.address)},{"node_kind",ng::kSpecs[n.first_hook].node_kind},{"vtable",Hex(n.vtable)},
            {"vtable_rva",Hex(n.vtable-binding.image)},{"owner",Hex(n.owner)},{"owner_role",n.owner_is_main?"main":"linked"},
            {"offset_from_main",relative?Json(Hex(n.address-binding.main_anim.address)):Json(nullptr)},{"events",n.events}});}
    unsigned completeFrames=0,incompleteEvents=0;
    for(unsigned i=0;i<recorder.frame_count;++i){const auto& f=recorder.frames[i];const bool full=i>0&&f.closed&&f.pending==0;
        completeFrames+=full;incompleteEvents+=f.pending;report["frames"].push_back({{"id",i},{"engine_frame",f.engine_frame},{"closed_by_next_frame",f.closed},{"complete",full},
            {"first_frame_boundary_partial",i==0},{"events",f.events},{"pending_callbacks",f.pending}});}
    report["complete_frame_count"]=completeFrames;
    report["incomplete_events"]=incompleteEvents;
    for(unsigned i=0;i<recorder.mapping_count;++i){const auto& m=recorder.mappings[i];
        report["mappings"].push_back({{"id",i},{"read",m.read},{"bijective",m.bijective},{"parents_match",m.parents_match},
            {"skeleton_count",m.skeleton_count},{"compact_count",m.compact_count},
            {"skeleton_to_compact",std::vector<int32_t>(m.skeleton_to_compact.begin(),m.skeleton_to_compact.begin()+m.skeleton_count)},
            {"compact_parents",std::vector<int32_t>(m.compact_parents.begin(),m.compact_parents.begin()+m.compact_count)}});}
    for(unsigned i=0;i<recorder.event_count;++i){const auto& e=recorder.events[i];report["events"].push_back({{"id",i},{"hook_kind",ng::kSpecs[e.hook].name},
        {"node_id",e.node_id},{"frame_id",e.frame_id},{"thread",e.thread},{"entry_sequence",e.entry_sequence},{"exit_sequence",e.exit_sequence},
        {"complete",e.complete},{"before",PhaseJson(r,pool,e.before,e.hook)},{"after",e.complete?PhaseJson(r,pool,e.after,e.hook):Json(nullptr)}});}
    report["truncated"]=recorder.node_overflow||recorder.event_overflow||recorder.mapping_overflow||recorder.root_overflow||recorder.contention.load()!=0||recorder.late_frame;
    report["unreadable_target_callbacks"]=unreadableTarget.load();report["callback_identity_failure"]=recorder.identity_failure;
}
static DWORD WINAPI Worker(LPVOID){
    Json report={{"schema",1},{"kind","native_graph_discovery"},{"phase","discovery"},{"status","error"},{"pid",GetCurrentProcessId()},
        {"readonly_animation",true},{"pose_writes",0},{"alpha_writes",0},{"native_update_calls",0},{"engine_getter_calls",0},{"vtable_writes_only",true},
        {"dll_retained_until_process_exit",true},{"hooks",Json::array()},{"all_hooks_restored",true},{"callbacks_quiescent",false},
        {"truncated",false},{"reasons",Json::array()},{"scope","Executed callbacks in one bounded frame window; unexecuted branches are not covered."},
        {"compatibility_profile",compat::ProfileId},{"compatibility_source_sha256",compat::SourceSha256},
        {"checks_performed",Json::array()},{"root_layout_verified",false}};
    oc::Session session;Pool pool;bool bound=false,ran=false;
    try{
        oc::Begin(session,selfModule,L"native_graph_discovery.json",report,"native_graph_discovery","graph_hooks");
        const auto& cfg=session.config;const auto& discovery=cfg.at("native_graph_discovery");
        std::string verificationError;
        const auto config=ParseConfig(cfg);
        report["host_sha256"]=compat::SourceBuildSha256;
        if(discovery.contains("kawaii_settings_witnesses")){
            const std::vector<witness::Function> functions={{"GroupUpdatePhysicsSettings",kawaii::kUpdateRva,kawaii::kUpdateBytes},
                {"GroupSimulate",kawaii::kSimulateRva,kawaii::kSimulateBytes}};
            if(!witness::VerifyFunctions(oc::Read,session.image,session.image_bytes,discovery.at("kawaii_settings_witnesses"),functions,
                report["checks_performed"],"kawaii_settings_layout",verificationError))throw std::runtime_error(verificationError);
            observePhysicsSettings=true;
        }
        const std::vector<witness::Function> rootFunctions={
            {"GroupInitializeBoneReferences",kRootRvas[0],kRootBytes[0]},
            {"GroupInitializeModifyBones",kRootRvas[1],kRootBytes[1]}};
        if(!witness::VerifyFunctions(oc::Read,session.image,session.image_bytes,config.root_witnesses,rootFunctions,report["checks_performed"],"graph_root_layout",verificationError))
            throw std::runtime_error(verificationError);
        rootLayoutVerified=true;report["root_layout_verified"]=true;
        uint64_t goAddress=0,poolAddress=0;
        if(!ww_game_compat_resolution::ResolveImageAddress(session.image,session.image_bytes,config.go_rva,compat::ObjectArrayHeaderBytes,goAddress)||
           !ww_game_compat_resolution::ResolveImageAddress(session.image,session.image_bytes,config.pool_rva,compat::NamePoolHeaderBytes,poolAddress))throw std::runtime_error("graph anchor RVA out of image");
        Reader reader{GetCurrentProcess()};Go go;if(!ValidateGo(reader,goAddress,go)||!ValidatePool(reader,poolAddress,pool))throw std::runtime_error("fresh anchors invalid");
        binding=Bind(reader,pool,go,config,cfg,session.image);bound=true;report["identity"]=BindingJson(reader,pool,binding);
        referenceBoneNames=config.bone_names;
        report["profile_id"]=config.profile;report["bone_count"]=config.bone_count;report["skeleton_sha256"]=config.skeleton_sha;report["watch_mesh"]=Hex(binding.mesh.address);
        report["reference_bone_names"]=config.bone_names;report["reference_bone_parents"]=config.parents;
        recorder.node_limit=config.node_limit;recorder.frame_limit=config.frame_limit;
        hooks.Resolve(session,report);
        // Enable capture only after every required hook is installed, so a
        // recorded frame cannot silently omit a not-yet-installed category.
        hooks.Install(&Trampolines::Address,report);
        observing.store(true,std::memory_order_release);ran=true;report["status"]="observing";ww_report::Save(session.status,report);
        const auto begin=GetTickCount64();
        while(GetTickCount64()-begin<config.duration_ms&&!recorder.window_closed.load()&&!oc::StopRequested(session.directory))Sleep(10);
        report["stopped_by_flag"]=oc::StopRequested(session.directory);
    }catch(const std::exception& e){report["reasons"].push_back(e.what());}
    observing.store(false,std::memory_order_release);
    const bool restored=hooks.Restore(report);
    const bool quiet=hooks.Quiescent();report["all_hooks_restored"]=restored;report["callbacks_quiescent"]=quiet;report["active_callbacks"]=hooks.active.load();
    try{
        // Fixed records are read only after all observed writers have left.
        if(bound){Reader reader{GetCurrentProcess()};if(quiet)CaptureJson(reader,pool,report);else report["reasons"].push_back("callbacks not quiescent; record snapshot withheld");}
        hooks.ReportCalls(report);
        for(unsigned i=0;i<report["hooks"].size();++i)report["hooks"][i]["target_calls"]=targetCalls[i].load();
        if(!restored||!quiet)report["reasons"].push_back("hook restoration or callback quiescence incomplete");
        if(ran&&report["reasons"].empty()){
            const bool complete=!report["truncated"].get<bool>()&&report.value("complete_frame_count",0u)>=3&&report.value("incomplete_events",0u)==0&&
                !recorder.identity_failure&&!unreadableTarget.load();
            report["status"]=complete?"complete":"partial";
            if(!complete)report["reasons"].push_back("bounded observation lacks three complete frames or has identity/capture/truncation gaps");
        }else report["status"]="error";
    }catch(const std::exception& e){report["status"]="error";report["reasons"].push_back(e.what());}
    oc::Save(session,report);
    return 0;
}
} // namespace graph_carrier

// The installer advertises this capability only for a DLL with the guard above.
extern "C" __declspec(dllexport) void WwPoserObserverRecoveryV1() {}
extern "C" __declspec(dllexport) LRESULT CALLBACK HookProc(int code,WPARAM w,LPARAM l){
    if(code>=0)observer_carrier::StartOnce(graph_carrier::started,graph_carrier::Worker,reinterpret_cast<const void*>(&HookProc));
    return CallNextHookEx(nullptr,code,w,l);
}
BOOL WINAPI DllMain(HINSTANCE module,DWORD reason,LPVOID){if(reason==DLL_PROCESS_ATTACH){graph_carrier::selfModule=module;DisableThreadLibraryCalls(module);}return TRUE;}
