#pragma once
#include "../../runtime/generated/GameCompat.generated.h"
// Allocation-free observation helpers. Read callbacks never materialize a pose
// or invoke engine getters. Caller owns hook lifetime.
#include <array>
#include <atomic>
#include <cstdint>
#include <cstring>
#include <cmath>
#include "../../playback/native_animation_target.h"

namespace native_graph_discovery {
constexpr unsigned kNodes=128,kFrames=8,kEvents=8192,kMappings=128,kBones=1024,kHooks=5;
constexpr unsigned kRoots=128,kRootSets=256,kRootStride=ww_game_compat::KawaiiRootBytes;
constexpr uint64_t kTop=ww_game_compat::PointerMaximum;
enum class Kind:unsigned {LocalToComponent,GroupBaseCS,GroupUpdate,GroupEvaluate,SingleBaseCS};
struct HookSpec {const char* name;const char* node_kind;uint64_t vtable_rva,function_rva;unsigned slot_bytes,function_bytes,arity;};
constexpr HookSpec kSpecs[kHooks]={
    {"local_to_component","local_to_component",ww_game_compat::VtableLocalToComponentRva,ww_game_compat::FunctionLocalToComponentRva,ww_game_compat::SlotLocalToComponentEvaluateCS,ww_game_compat::FunctionLocalToComponentBytes,2},
    {"group_base_cs","kawaii_group",ww_game_compat::VtableKawaiiPhysicsGroupRva,ww_game_compat::FunctionSharedSkeletalControlEvaluateCSRva,ww_game_compat::SlotSkeletalControlEvaluateCS,ww_game_compat::FunctionSharedSkeletalControlEvaluateCSBytes,2},
    {"group_update","kawaii_group",ww_game_compat::VtableKawaiiPhysicsGroupRva,ww_game_compat::FunctionSharedSkeletalControlUpdateRva,ww_game_compat::SlotSkeletalControlUpdate,ww_game_compat::FunctionSharedSkeletalControlUpdateBytes,2},
    {"group_evaluate","kawaii_group",ww_game_compat::VtableKawaiiPhysicsGroupRva,ww_game_compat::FunctionGroupEvaluateRva,ww_game_compat::SlotKawaiiGroupEvaluate,ww_game_compat::FunctionGroupEvaluateBytes,3},
    {"single_base_cs","kawaii_single",ww_game_compat::VtableKawaiiPhysicsSingleRva,ww_game_compat::FunctionSharedSkeletalControlEvaluateCSRva,ww_game_compat::SlotSkeletalControlEvaluateCS,ww_game_compat::FunctionSharedSkeletalControlEvaluateCSBytes,2}
};
template<class T> inline T Get(const uint8_t* p,unsigned off){T v{};std::memcpy(&v,p+off,sizeof(v));return v;}
inline bool Range(uint64_t address,size_t bytes){return bytes&&address>=ww_game_compat::PointerMinimum&&address<kTop&&bytes<=kTop-address;}
struct GoView {uint64_t table=0;uint32_t count=0;};
struct Token {uint64_t address=0,item=0;std::array<uint8_t,ww_game_compat::ObjectHeaderBytes> header{};std::array<uint8_t,ww_game_compat::ObjectItemBytes> item_bytes{};};
inline uint32_t Index(const Token& t){return Get<uint32_t>(t.header.data(),ww_game_compat::ObjectIndexOffset);}
inline uint32_t Serial(const Token& t){return Get<uint32_t>(t.item_bytes.data(),ww_game_compat::ObjectItemSerialOffset);}
inline uint64_t Class(const Token& t){return Get<uint64_t>(t.header.data(),ww_game_compat::ObjectClassOffset);}
inline bool Same(const Token& a,const Token& b){return a.address==b.address&&a.item==b.item&&a.header==b.header&&a.item_bytes==b.item_bytes;}
template<class Read> bool CaptureToken(Read&& read,const GoView& go,uint64_t address,Token& t,bool strong=true){
    Token v;v.address=address;uint64_t chunk=0;
    if(!Range(address,ww_game_compat::ObjectHeaderBytes)||!read(address,v.header.data(),ww_game_compat::ObjectHeaderBytes)||Index(v)>=go.count||
       !Range(Class(v),ww_game_compat::ObjectHeaderBytes)||(Get<uint32_t>(v.header.data(),ww_game_compat::ObjectFlagsOffset)&ww_game_compat::ObjectInvalidFlagsMask)||
       !read(go.table+uint64_t(Index(v)>>ww_game_compat::ObjectArrayChunkShift)*ww_game_compat::PointerBytes,&chunk,sizeof(uint64_t))||!Range(chunk,ww_game_compat::ObjectItemBytes))return false;
    v.item=chunk+uint64_t(Index(v)&ww_game_compat::ObjectArrayChunkMask)*ww_game_compat::ObjectItemBytes;
    if(!read(v.item,v.item_bytes.data(),ww_game_compat::ObjectItemBytes)||Get<uint64_t>(v.item_bytes.data(),ww_game_compat::ObjectItemObjectOffset)!=address||
       (Get<uint32_t>(v.item_bytes.data(),ww_game_compat::ObjectItemFlagsOffset)&ww_game_compat::ObjectItemInvalidFlagsMask)||(strong&&!Serial(v)))return false;
    t=v;return true;
}
template<class Read> bool Alive(Read&& read,const Token& t){
    std::array<uint8_t,ww_game_compat::ObjectHeaderBytes> h{};std::array<uint8_t,ww_game_compat::ObjectItemBytes> i{};
    return t.address&&Serial(t)&&read(t.address,h.data(),ww_game_compat::ObjectHeaderBytes)&&read(t.item,i.data(),ww_game_compat::ObjectItemBytes)&&h==t.header&&i==t.item_bytes;
}
struct Binding {
    GoView go;uint64_t image=0;uint32_t bone_count=0;
    Token mesh,pawn,controller,mesh_asset,skeleton_asset,main_anim;
    native_animation_target::AuthorityTarget authority;
    std::array<int32_t,kBones> parents{};
};
struct ArrayHead {uint64_t data=0;int32_t num=0,capacity=0;};
static_assert(sizeof(ArrayHead)==ww_game_compat::TArrayHeaderBytes,"x64 TArray");
inline bool Array(const ArrayHead& a){return a.num>=0&&a.num<=int32_t(kBones)&&a.capacity>=a.num&&a.capacity<=int32_t(kBones*8)&&(!a.num||Range(a.data,size_t(a.num)*sizeof(int32_t)));}
inline bool SameHead(const ArrayHead& a,const ArrayHead& b){return a.data==b.data&&a.num==b.num&&a.capacity==b.capacity;}
inline bool ExtraVectorsAbsent(const ArrayHead& a,uint8_t mode){
    // Local-to-component conversion and bone-container setup set the adjacent mode byte
    // independently of array presence. Only observed Boolean modes with a
    // canonical null array are supported; no retained/populated storage.
    return mode<=1&&a.data==0&&a.num==0&&a.capacity==0;
}
struct RootSnapshot {
    ArrayHead roots,extended;bool headers_read=false,read=false,stable=false,identity_stable=false;
    std::array<uint8_t,kRoots*kRootStride> bytes{};
};
inline bool SameRoots(const RootSnapshot& a,const RootSnapshot& b){return SameHead(a.roots,b.roots)&&SameHead(a.extended,b.extended)&&
    a.headers_read==b.headers_read&&a.read==b.read&&a.stable==b.stable&&a.identity_stable==b.identity_stable&&a.bytes==b.bytes;}
template<class Read> void CaptureRoots(Read&& read,uint64_t node,RootSnapshot& out){
    out.headers_read=read(node+ww_game_compat::KawaiiRootsOffset,&out.roots,ww_game_compat::TArrayHeaderBytes)&&read(node+ww_game_compat::KawaiiExtendedRootsOffset,&out.extended,ww_game_compat::TArrayHeaderBytes);
    if(!out.headers_read||!Array(out.roots)||out.roots.num>int32_t(kRoots)||!Array(out.extended)||out.extended.num!=0)return;
    const size_t bytes=size_t(out.roots.num)*kRootStride;
    out.read=!bytes||(Range(out.roots.data,bytes)&&read(out.roots.data,out.bytes.data(),bytes));
    out.stable=out.read;out.identity_stable=out.read;
    // Interpret only each root element's FName; its trailing cache is ignored.
}
struct Mapping {
    unsigned skeleton_count=0,compact_count=0;bool read=false,stable=false,bijective=false,parents_match=false;
    std::array<int32_t,kBones> skeleton_to_compact{},compact_parents{};
};
inline bool SameMapping(const Mapping& a,const Mapping& b){
    return a.skeleton_count==b.skeleton_count&&a.compact_count==b.compact_count&&a.read==b.read&&a.stable==b.stable&&
        a.bijective==b.bijective&&a.parents_match==b.parents_match&&a.skeleton_to_compact==b.skeleton_to_compact&&a.compact_parents==b.compact_parents;
}
struct Phase {
    bool read=false,binding_alive=false,type_verified=false,owner_verified=false,owner_is_main=false,owner_role_verified=false;
    bool mapping_applicable=false,pose_headers_read=false,headers_read=false,extra_vectors_absent=false,weak_assets_match=false,node_fields_read=false;
    uint64_t node=0,context=0,proxy=0,mesh=0,owner_a0=0,owner_a8=0,main_proxy=0,skeleton=0,vtable=0;
    uint64_t frame=0,main_frame=0;int16_t counter=0,main_counter=0;uint64_t bone_container=0;
    Token owner;
    ArrayHead transforms,flags,extra_vectors,skeleton_to_compact,compact_parents;
    std::array<uint32_t,2> weak_mesh{},weak_skeleton{};
    std::array<uint32_t,ww_game_compat::NameWordCount> curve_name_words{};
    uint8_t alpha_type=0,extra_active=0,external_route=0;float alpha=0,delta=0,update_delta=0;
    int32_t mapping_id=-1,root_snapshot_id=-1;
};
template<class Read> bool CapturePhase(Read&& read,const Binding& b,Kind kind,uint64_t node,uint64_t context,Phase& p,Mapping& mapping,RootSnapshot* roots=nullptr){
    p.node=node;p.context=context;const auto& spec=kSpecs[unsigned(kind)];
    if(!read(context+ww_game_compat::AnimContextProxyOffset,&p.proxy,sizeof(p.proxy))||!Range(p.proxy,ww_game_compat::AnimProxyObservationBytes)||!read(p.proxy+ww_game_compat::AnimProxyMeshOffset,&p.mesh,sizeof(uint64_t))||p.mesh!=b.mesh.address)return false;
    if(!read(p.proxy+ww_game_compat::AnimProxyOwnerOffset,&p.owner_a0,sizeof(uint64_t))||!read(p.proxy+ww_game_compat::AnimProxyOwnerAltOffset,&p.owner_a8,sizeof(uint64_t))||!read(p.proxy+ww_game_compat::AnimProxyMainProxyOffset,&p.main_proxy,sizeof(uint64_t))||
       !read(p.proxy+ww_game_compat::AnimProxySkeletonOffset,&p.skeleton,sizeof(uint64_t))||!read(p.proxy+ww_game_compat::AnimProxyFrameOffset,&p.frame,sizeof(uint64_t))||!read(p.proxy+ww_game_compat::AnimProxyCounterOffset,&p.counter,2)||
       !read(node,&p.vtable,sizeof(uint64_t))||!read(b.mesh.address+ww_game_compat::ComponentExternalRouteOffset,&p.external_route,1))return false;
    p.read=true;p.type_verified=p.vtable==b.image+spec.vtable_rva;
    p.binding_alive=true;p.owner_verified=CaptureToken(read,b.go,p.owner_a0,p.owner);
    p.owner_is_main=p.owner_a0==b.main_anim.address;
    uint64_t mainOwner=0,mainMesh=0,mainSelf=0;
    p.owner_role_verified=p.owner_verified&&p.owner_a8==p.owner_a0&&Range(p.main_proxy,ww_game_compat::AnimProxyObservationBytes)&&
        read(p.main_proxy+ww_game_compat::AnimProxyOwnerOffset,&mainOwner,sizeof(uint64_t))&&mainOwner==b.main_anim.address&&
        read(p.main_proxy+ww_game_compat::AnimProxyMeshOffset,&mainMesh,sizeof(uint64_t))&&mainMesh==b.mesh.address&&
        read(p.main_proxy+ww_game_compat::AnimProxyMainProxyOffset,&mainSelf,sizeof(uint64_t))&&mainSelf==p.main_proxy&&
        (!p.owner_is_main||p.proxy==p.main_proxy)&&
        read(p.main_proxy+ww_game_compat::AnimProxyFrameOffset,&p.main_frame,sizeof(uint64_t))&&read(p.main_proxy+ww_game_compat::AnimProxyCounterOffset,&p.main_counter,2);
    if(kind!=Kind::LocalToComponent&&p.type_verified){
        p.node_fields_read=read(node+ww_game_compat::KawaiiAlphaOffset,&p.alpha,4)&&read(node+ww_game_compat::KawaiiAlphaTypeOffset,&p.alpha_type,1)&&
            read(node+ww_game_compat::CurveNameOffset,p.curve_name_words.data(),ww_game_compat::NameBytes)&&read(node+ww_game_compat::KawaiiDeltaOffset,&p.delta,4);
        if(kind==Kind::GroupUpdate)p.node_fields_read=p.node_fields_read&&read(context+ww_game_compat::AnimUpdateContextDeltaOffset,&p.update_delta,sizeof(p.update_delta));
    }
    if(roots&&p.type_verified&&p.binding_alive&&p.owner_verified&&
       (kind==Kind::GroupBaseCS||kind==Kind::GroupUpdate||kind==Kind::GroupEvaluate))CaptureRoots(read,node,*roots);
    p.mapping_applicable=kind!=Kind::GroupUpdate;
    if(p.mapping_applicable){
        p.pose_headers_read=read(context+ww_game_compat::AnimContextTransformsOffset,&p.transforms,ww_game_compat::TArrayHeaderBytes)&&read(context+ww_game_compat::AnimContextFlagsOffset,&p.flags,ww_game_compat::TArrayHeaderBytes)&&read(context+ww_game_compat::AnimContextExtraVectorsOffset,&p.extra_vectors,ww_game_compat::TArrayHeaderBytes)&&
            read(context+ww_game_compat::AnimContextExtraVectorsActiveOffset,&p.extra_active,1)&&read(context+ww_game_compat::AnimContextBoneContainerOffset,&p.bone_container,sizeof(p.bone_container));
        p.headers_read=p.pose_headers_read&&Range(p.bone_container,ww_game_compat::BoneContainerObservationBytes)&&
            read(p.bone_container+ww_game_compat::BoneContainerSkeletonToCompactOffset,&p.skeleton_to_compact,ww_game_compat::TArrayHeaderBytes)&&read(p.bone_container+ww_game_compat::BoneContainerCompactParentsOffset,&p.compact_parents,ww_game_compat::TArrayHeaderBytes)&&
            read(p.bone_container+ww_game_compat::BoneContainerWeakMeshOffset,p.weak_mesh.data(),sizeof(uint64_t))&&read(p.bone_container+ww_game_compat::BoneContainerWeakSkeletonOffset,p.weak_skeleton.data(),sizeof(uint64_t));
        p.extra_vectors_absent=p.headers_read&&ExtraVectorsAbsent(p.extra_vectors,p.extra_active);
        p.weak_assets_match=p.headers_read&&p.weak_mesh==std::array<uint32_t,2>{{Index(b.mesh_asset),Serial(b.mesh_asset)}}&&
            p.weak_skeleton==std::array<uint32_t,2>{{Index(b.skeleton_asset),Serial(b.skeleton_asset)}}&&p.skeleton==b.skeleton_asset.address;
        if(p.headers_read&&Array(p.skeleton_to_compact)&&Array(p.compact_parents)&&p.skeleton_to_compact.num>0&&p.compact_parents.num>0){
            mapping.skeleton_count=unsigned(p.skeleton_to_compact.num);mapping.compact_count=unsigned(p.compact_parents.num);
            mapping.read=read(p.skeleton_to_compact.data,mapping.skeleton_to_compact.data(),mapping.skeleton_count*sizeof(int32_t))&&
                read(p.compact_parents.data,mapping.compact_parents.data(),mapping.compact_count*sizeof(int32_t));
            mapping.stable=mapping.read;
            mapping.bijective=mapping.stable&&mapping.skeleton_count==b.bone_count&&mapping.compact_count==b.bone_count;
            std::array<bool,kBones> used{};
            if(mapping.bijective)for(unsigned i=0;i<b.bone_count;++i){const int32_t c=mapping.skeleton_to_compact[i];
                if(c<0||c>=int32_t(b.bone_count)||used[unsigned(c)]){mapping.bijective=false;break;}used[unsigned(c)]=true;}
            mapping.parents_match=mapping.bijective;
            if(mapping.parents_match)for(unsigned i=0;i<b.bone_count;++i){const int32_t parent=b.parents[i];
                if((i==0?parent!=-1:parent<0||parent>=int32_t(i))||mapping.compact_parents[unsigned(mapping.skeleton_to_compact[i])]!=(parent<0?-1:mapping.skeleton_to_compact[unsigned(parent)])){
                    mapping.parents_match=false;break;}}
        }
    }
    return true;
}
struct Event {
    uint64_t entry_sequence=0,exit_sequence=0;uint32_t thread=0;unsigned hook=0,node_id=0,frame_id=0;
    bool complete=false;Phase before,after;
};
struct Node {uint64_t address=0,vtable=0,owner=0;unsigned first_hook=0;bool owner_is_main=false;uint64_t events=0;};
struct Frame {uint64_t engine_frame=0;bool closed=false;unsigned events=0,pending=0;};
// One short lock protects bookkeeping only; no lock is held across an original
// callback or memory read.
struct Recorder {
    std::atomic_flag gate=ATOMIC_FLAG_INIT;
    std::atomic<uint64_t> sequence{0},contention{0},outside_window{0};
    std::atomic<bool> window_closed{false};
    unsigned node_limit=kNodes,frame_limit=kFrames,node_count=0,frame_count=0,event_count=0,mapping_count=0,root_set_count=0;
    bool node_overflow=false,event_overflow=false,mapping_overflow=false,root_overflow=false,identity_failure=false,late_frame=false;
    std::array<Node,kNodes> nodes{};std::array<Frame,kFrames> frames{};
    std::array<Event,kEvents> events{};std::array<Mapping,kMappings> mappings{};
    std::array<RootSnapshot,kRootSets> root_sets{};
    int32_t Intern(const Mapping& m){if(!m.read)return -1;
        for(unsigned i=0;i<mapping_count;++i)if(SameMapping(m,mappings[i]))return int32_t(i);
        if(mapping_count==kMappings){mapping_overflow=true;return -1;}mappings[mapping_count]=m;return int32_t(mapping_count++);}
    int32_t InternRoots(const RootSnapshot* r){if(!r||!r->headers_read)return -1;
        for(unsigned i=0;i<root_set_count;++i)if(SameRoots(*r,root_sets[i]))return int32_t(i);
        if(root_set_count==kRootSets){root_overflow=true;return -1;}root_sets[root_set_count]=*r;return int32_t(root_set_count++);}
    int32_t Begin(unsigned hook,uint32_t thread,uint64_t entry,Phase p,const Mapping& m,bool after_only=false,const RootSnapshot* roots=nullptr){
        if(gate.test_and_set(std::memory_order_acquire)){++contention;return -1;}
        struct Unlock{std::atomic_flag& g;~Unlock(){g.clear(std::memory_order_release);}} unlock{gate};
        if(!p.read||!p.type_verified||!p.owner_verified||!p.owner_role_verified){identity_failure=true;return -1;}
        unsigned f=0;for(;f<frame_count;++f)if(frames[f].engine_frame==p.main_frame)break;
        if(f==frame_count){
            if(frame_count){if(p.main_frame<frames[frame_count-1].engine_frame){late_frame=true;return -1;}frames[frame_count-1].closed=true;}
            if(frame_count==frame_limit){window_closed.store(true,std::memory_order_release);++outside_window;return -1;}
            frames[f].engine_frame=p.main_frame;++frame_count;
        }else if(f+1<frame_count){late_frame=true;}
        unsigned n=0;for(;n<node_count;++n)if(nodes[n].address==p.node&&nodes[n].vtable==p.vtable&&nodes[n].owner==p.owner_a0)break;
        if(n==node_count){if(node_count==node_limit){node_overflow=true;return -1;}nodes[n]={p.node,p.vtable,p.owner_a0,hook,p.owner_is_main,0};++node_count;}
        if(event_count==kEvents){event_overflow=true;return -1;}
        p.mapping_id=Intern(m);p.root_snapshot_id=InternRoots(roots);auto& e=events[event_count];e.entry_sequence=entry;e.thread=thread;e.hook=hook;e.node_id=n;e.frame_id=f;e.before=p;
        if(after_only)e.before=Phase{};
        ++nodes[n].events;++frames[f].events;++frames[f].pending;return int32_t(event_count++);
    }
    void End(int32_t index,uint64_t exit,Phase p,const Mapping& m,const RootSnapshot* roots=nullptr){
        if(index<0)return;
        if(gate.test_and_set(std::memory_order_acquire)){++contention;return;}
        struct Unlock{std::atomic_flag& g;~Unlock(){g.clear(std::memory_order_release);}} unlock{gate};
        auto& e=events[unsigned(index)];p.mapping_id=Intern(m);p.root_snapshot_id=InternRoots(roots);e.after=p;e.exit_sequence=exit;e.complete=true;
        --frames[e.frame_id].pending;
    }
};
} // namespace native_graph_discovery
