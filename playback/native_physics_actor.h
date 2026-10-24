#pragma once
// Per-instance native animation input. Detached from hooks/process APIs so the
// same evaluation code serves controlled, owned and SceneTeam actors.
#include "native_kawaii_pose.h"
#include "native_animation_target.h"
#include "vmd_local_clip.h"
#include "ww_mmd_placement.h"
#include "../probes/v20_recon/native_graph_binding.h"
#include <atomic>
#include <cmath>
#include <mutex>
#include <vector>

namespace wwp_playback { namespace native_physics {
namespace input=native_kawaii;
namespace graph=v20_native_graph_binding;
namespace body=wwp_ue::full_body;
namespace sub=wwp_ue::subtree_rotation;
enum class Gate:uint32_t {Allowed,LodDenied,ValidityDenied,Exception};
// SharedSkeletalControlUpdate branches around UpdateInternal at alpha <= this
// value (RVA 0x49dd595, scalar at 0x7403660).
constexpr float kNativeRelevantAlpha=1e-8f;
struct Access {
    void* user=nullptr;
    bool (*read)(void*,uintptr_t,void*,size_t)=nullptr;
    bool (*write)(void*,uintptr_t,const void*,size_t)=nullptr;
    bool (*writable)(void*,uintptr_t,size_t)=nullptr;
    bool (*getter)(void*,uintptr_t,int32_t,input::Transform*)=nullptr;
    Gate (*gate)(void*,uintptr_t,uintptr_t,uintptr_t)=nullptr;
    bool (*internal)(void*,uintptr_t,uintptr_t)=nullptr;
    uint64_t thread=0;
    bool Read(uintptr_t a,void* p,size_t n) const {return read&&read(user,a,p,n);}
    template<class T>bool Get(uintptr_t a,T& out) const {return Read(a,&out,sizeof(out));}
};
struct Snapshot {uint64_t epoch=0;double seconds=0;placement::Settings placement;bool feed=false,stop=false;};
struct ObjectToken {uint64_t address=0,item=0;uint32_t index=0,serial=0;std::array<uint8_t,48> header{};};
struct CurveStats {
    std::atomic<unsigned> calls{0},forced{0},natural_positive_delta_updates{0};
    std::atomic<float> alpha{0},delta{0};
    uint64_t pending_frame=0;int16_t pending_counter=0;bool pending_token_valid=false;
};
struct Actor {
    native_animation_target::AuthorityTarget target;
    graph::Binding binding;
    std::array<ObjectToken,5> tokens{}; // actor, mesh, mesh asset, skeleton, main
    uint64_t image=0,node=0,node_vtable=0,group_vtable=0,epoch=0;
    const local_clip::Clip* motion=nullptr;
    body::Topology topology;
    body::Request request,anchored_request;
    std::array<uint8_t,input::kLimit> owned{},canonical_position{},canonical_rotation{},mask{};
    bool prepared=false,anchor=false,token_valid=false;
    std::atomic<uint64_t> proxy{0};
    uint64_t last_frame=0;std::atomic<uint64_t> evaluation_thread{0};
    int16_t last_counter=0;
    std::atomic_flag busy=ATOMIC_FLAG_INIT;
    std::atomic<unsigned> work{0},group_work{0},calls{0},selected{0};
    std::atomic<unsigned> frames{0},error{0},force_pending{0},group_forced{0},warmup_samples{0};
    std::atomic<bool> ready{false};
    uint64_t warmup_frame=0;int16_t warmup_counter=0;bool warmup_token_valid=false;
    std::atomic<unsigned> plan_code{0},plan_bone{UINT32_MAX};
    std::atomic<bool> feed{false},pending{false},admission_closed{false};
    uint64_t pending_frame=0;int16_t pending_counter=0;bool pending_token_valid=false;
    std::atomic<bool>* external_feed=nullptr,*external_writing=nullptr,*external_pending=nullptr;
    std::atomic<unsigned>* external_force_pending=nullptr,*external_frames=nullptr,*external_error=nullptr;
    void* authority_user=nullptr;
    bool (*authority_alive)(void*,const Access&)=nullptr;
    bool (*read_snapshot)(void*,Snapshot*)=nullptr;
    void (*on_fault)(void*,unsigned)=nullptr;
    void (*on_commit)(void*,uint32_t)=nullptr;
    std::atomic<uint64_t> snapshot_sequence{0};
    std::atomic<double> seconds{0},x{0},y{0},height{0},yaw{0};
    std::atomic<bool> in_place{false},stop{false};
    std::array<CurveStats,graph::kMaxGroups> groups{};
    body::Scratch scratch;
    body::LocalFrame frame;
    std::array<local_clip::BoneCommand,input::kLimit> sample{};
    input::Snapshot mapping;
    std::array<input::Transform,input::kLimit> pose{},prepared_pose{};
    sub::FixedPlan plan;
};
inline bool Feeding(const Actor& a){return !a.admission_closed.load(std::memory_order_acquire)&&!a.error.load(std::memory_order_acquire)&&a.feed.load(std::memory_order_acquire)&&
    (!a.external_feed||a.external_feed->load(std::memory_order_acquire))&&
    (!a.external_writing||a.external_writing->load(std::memory_order_acquire));}
inline void Pending(Actor& a,bool value){a.pending.store(value,std::memory_order_release);if(a.external_pending)a.external_pending->store(value,std::memory_order_release);}
inline void ForcePending(Actor& a,unsigned value){a.force_pending.store(value,std::memory_order_release);if(a.external_force_pending)a.external_force_pending->store(value,std::memory_order_release);}
inline void Fail(Actor& a,unsigned code){unsigned empty=0;a.error.compare_exchange_strong(empty,code);
    if(a.external_error){empty=0;a.external_error->compare_exchange_strong(empty,code);}a.feed.store(false,std::memory_order_release);
    a.ready.store(false,std::memory_order_release);
    if(a.external_feed)a.external_feed->store(false,std::memory_order_release);if(a.on_fault)a.on_fault(a.authority_user,code);}
inline void Publish(Actor& a,double seconds,const placement::Settings& p,bool feed){
    // One publication writer per actor; readers retry this seqlock on the evaluation thread.
    a.snapshot_sequence.fetch_add(1,std::memory_order_acq_rel);
    a.seconds.store(seconds);a.x.store(p.x);a.y.store(p.y);a.height.store(p.height);a.yaw.store(p.yaw_degrees);a.in_place.store(p.in_place);
    feed=feed&&!a.admission_closed.load(std::memory_order_acquire)&&!a.error.load(std::memory_order_acquire);a.stop.store(!feed);a.feed.store(feed,std::memory_order_release);a.snapshot_sequence.fetch_add(1,std::memory_order_release);
}
inline void RequestStop(Actor& a){a.feed.store(false,std::memory_order_release);a.stop.store(true,std::memory_order_release);if(a.external_feed)a.external_feed->store(false,std::memory_order_release);}
// Terminal consumer/hub shutdown cannot be reopened by a concurrent transport
// publication. Ordinary transport Stop continues to permit replay.
inline void CloseAdmission(Actor& a){a.admission_closed.store(true,std::memory_order_release);a.ready.store(false,std::memory_order_release);RequestStop(a);}
inline bool ReadSnapshot(Actor& a,Snapshot* out){
    if(a.read_snapshot)return a.read_snapshot(a.authority_user,out);
    for(unsigned retry=0;retry<8;++retry){auto seq=a.snapshot_sequence.load(std::memory_order_acquire);if(seq&1)continue;
        Snapshot s;s.epoch=a.epoch;s.seconds=a.seconds.load();s.placement={a.x.load(),a.y.load(),a.height.load(),a.yaw.load(),a.in_place.load()};
        s.feed=Feeding(a);s.stop=a.stop.load();if(a.snapshot_sequence.load(std::memory_order_acquire)==seq){*out=s;return true;}}
    return false;
}
inline bool Alive(const Actor& a,const Access& r){
    if(!a.prepared||!a.authority_alive||!a.authority_alive(a.authority_user,r))return false;
    auto read=[&](uint64_t p,void* out,size_t n){return r.Read(p,out,n);};
    if(!native_animation_target::AuthorityAlive(read,a.target))return false;
    uint64_t owner=0,asset=0,main=0,vtable=0;
    return r.Get(a.target.mesh+0xa8,owner)&&owner==a.target.actor&&r.Get(a.target.mesh+0x5d0,asset)&&asset==a.target.mesh_asset&&
        r.Get(a.target.mesh+0x910,main)&&main==a.target.main_anim&&r.Get(a.node,vtable)&&vtable==a.node_vtable;
}
inline bool Restored(const Actor& a){return !a.pending.load()&&!a.force_pending.load()&&!a.work.load()&&!a.group_work.load();}
inline void PrepareMotion(Actor& a,const local_clip::Clip& motion,const body::Topology& topology,const body::Request& request,uint64_t epoch){
    a.motion=&motion;a.topology=topology;a.request=request;a.epoch=epoch;a.owned.fill(0);a.canonical_position.fill(0);a.canonical_rotation.fill(0);a.mask.fill(1);
    for(uint32_t i=0;i<topology.count;++i)if(motion.frames[i].mask)for(int32_t k=int32_t(i);k>=0;k=topology.parents[k])a.owned[k]=1;
    for(uint32_t i=0;i<topology.count;++i)if(a.owned[i]){
        if(request.canonical_position_mask)a.canonical_position[i]=request.canonical_position_mask[i];
        if(request.canonical_rotation_mask)a.canonical_rotation[i]=request.canonical_rotation_mask[i];}
    a.admission_closed.store(false,std::memory_order_release);
}
inline void InputFrame(Actor& a,uintptr_t node,uintptr_t context,const Access& r){
    ++a.work;struct Work{Actor& a;~Work(){--a.work;}} work{a};
    ++a.calls;uint64_t proxy=0,mesh=0,anim=0;
    if(!r.Get(context,proxy)||!proxy||!r.Get(proxy+0xc0,mesh)||mesh!=a.target.mesh||!r.Get(proxy+0xa0,anim)||anim!=a.target.main_anim)return;
    if(node!=a.node)return;++a.selected;
    if(a.busy.test_and_set(std::memory_order_acquire)){Fail(a,2);return;}
    struct Release{Actor& a;~Release(){a.busy.clear(std::memory_order_release);}} release{a};a.evaluation_thread=r.thread;
    if(!Alive(a,r)){Fail(a,3);return;}
    // A new main proxy may still name the same UObject. It cannot certify that
    // the previous evaluation context has naturally retired our input writes.
    if(a.proxy&&a.proxy!=proxy){Fail(a,7);return;}a.proxy=proxy;
    Snapshot state;if(!ReadSnapshot(a,&state))return;
    const bool feed=Feeding(a)&&state.feed&&!state.stop;
    uint64_t frame=0;int16_t counter=0;if(!r.Get(proxy+0x528,frame)||!r.Get(proxy+0x530,counter)){Fail(a,6);return;}
    if(!feed&&a.pending.load()){
        // A later natural evaluation retires the previous input write.
        if(a.pending_token_valid&&(frame!=a.pending_frame||counter!=a.pending_counter)){
            a.anchor=false;a.token_valid=false;a.pending_token_valid=false;Pending(a,false);}return;}
    if(a.admission_closed.load(std::memory_order_acquire)||a.error.load())return;
    if(a.token_valid&&frame==a.last_frame&&counter==a.last_counter)return;
    auto read=[&](uintptr_t p,void* out,size_t n){return r.Read(p,out,n);};
    input::View view;
    auto error=input::ReadView(context,mesh,a.topology.count,{},read,&view);if(error!=input::Error::none){Fail(a,100+unsigned(error));return;}
    error=input::ReadMapping(view,read,&a.mapping);if(error!=input::Error::none){Fail(a,200+unsigned(error));return;}
    auto getter=[&](uintptr_t pose,int32_t bone,input::Transform* out){return r.getter(r.user,pose,bone,out);};
    error=input::Materialize(view,getter,&a.mapping);if(error!=input::Error::none){Fail(a,300+unsigned(error));return;}
    for(uint32_t i=0;i<a.topology.count;++i)a.pose[i]=a.mapping.component[a.mapping.skeleton_to_compact[i]];
    if(!a.anchor){a.anchored_request=a.request;
        if(a.request.canonical_position_mask)a.anchored_request.canonical_position_mask=a.canonical_position.data();
        if(a.request.canonical_rotation_mask)a.anchored_request.canonical_rotation_mask=a.canonical_rotation.data();
        a.anchored_request.root_anchor_position=sub::detail::Position(a.pose[0]);a.anchored_request.root_anchor_rotation=sub::detail::Rotation(a.pose[0]);
        a.anchored_request.root_anchor_enabled=true;a.anchored_request.root_rotation_enabled=true;a.anchor=true;}
    if(!local_clip::Sample(*a.motion,state.seconds,a.sample.data(),a.topology.count)){Fail(a,10);return;}
    a.frame.clip_epoch=a.epoch;a.frame.sequence=a.frames.load()+1;a.frame.time_seconds=state.seconds;a.frame.bone_count=a.topology.count;
    for(uint32_t i=0;i<a.topology.count;++i){const auto& src=a.sample[i];auto& dst=a.frame.bones[i];
        memcpy(dst.translation_delta,src.translation_delta,12);memcpy(dst.rotation_xyzw,src.rotation_xyzw,16);dst.mask=src.mask;}
    body::Request placed;body::Failure failure;
    if(!placement::Apply(state.placement,a.anchored_request,a.frame.bones[0],&placed,&a.frame.bones[0])){
        a.plan_code.store(unsigned(body::Code::InvalidFrame));a.plan_bone.store(0);Fail(a,11);return;}
    if(!body::MakePlan(a.topology,placed,a.pose.data(),a.topology.count,a.frame,&a.scratch,&a.plan,&failure)){
        a.plan_code.store(unsigned(failure.code));a.plan_bone.store(failure.bone);Fail(a,11);return;}
    for(uint32_t i=0;i<a.plan.count;++i){const auto& patch=a.plan.patches[i];memcpy(a.pose[patch.bone].data(),patch.rotated_rotation.data(),16);
        if(patch.position_changed)memcpy(a.pose[patch.bone].data()+16,patch.rotated_position.data(),12);}
    input::PrepareInput(a.mapping,a.pose.data(),a.mask.data(),&a.prepared_pose);
    if(!feed){
        if(!a.warmup_token_valid||frame!=a.warmup_frame||counter!=a.warmup_counter){a.warmup_frame=frame;a.warmup_counter=counter;a.warmup_token_valid=true;
            if(++a.warmup_samples>=2)a.ready.store(!a.admission_closed.load(std::memory_order_acquire),std::memory_order_release);}
        a.anchor=false; // capture the real playback anchor on its first input
        return; // preflight materializes native CS caches but makes no input/alpha write
    }
    if(!Feeding(a))return;
    if(!r.writable(r.user,view.transforms.data,size_t(view.transforms.num)*48)||!r.writable(r.user,view.flags.data,size_t(view.flags.num))){Fail(a,12);return;}
    a.pending_frame=frame;a.pending_counter=counter;a.pending_token_valid=true;Pending(a,true);
    auto write=[&](uintptr_t p,const void* in,size_t n){return r.write(r.user,p,in,n);};
    if(!input::CommitInput(view,a.prepared_pose,write)){Fail(a,12);return;}
    a.last_frame=frame;a.last_counter=counter;a.token_valid=true;++a.frames;if(a.external_frames)++*a.external_frames;
    if(a.on_commit)a.on_commit(a.authority_user,a.plan.count);
}
inline void ForceGroup(Actor& a,unsigned index,uintptr_t node,uintptr_t context,const Access& r){
    ++a.group_work;struct Work{Actor& a;~Work(){--a.group_work;}} work{a};
    auto& g=a.groups[index];++g.calls;
    // One actor's input and group scratch/state are never used reentrantly.
    if(a.busy.test_and_set(std::memory_order_acquire)){Fail(a,2);return;}
    struct Release{Actor& a;~Release(){a.busy.clear(std::memory_order_release);}} release{a};
    if(!Alive(a,r)){Fail(a,607);return;}
    const unsigned bit=1u<<index;
    uint64_t proxy=0,skeleton=0;uint8_t type=0,external=0;float alpha=0,dt=0,native_dt=0;
    if(!r.Get(context,proxy)||!r.Get(proxy+0xb8,skeleton)||!r.Get(node+0x2c,type)||
       !r.Get(node+0x28,alpha)||!r.Get(context+0x18,dt)||!r.Get(a.target.mesh+0x145a,external)){Fail(a,602);return;}
    const bool sample=r.Get(node+0x280,native_dt);
    uint64_t frame=0;int16_t counter=0;if(!r.Get(proxy+0x528,frame)||!r.Get(proxy+0x530,counter)){Fail(a,6);return;}
    // A duplicate Update in the same evaluation, or a zero-delta callback,
    // cannot certify that the previous forced update naturally retired.
    if(a.force_pending.load()&bit){if(!g.pending_token_valid||(frame==g.pending_frame&&counter==g.pending_counter)||type!=2||dt<=0)return;
        g.pending_token_valid=false;ForcePending(a,a.force_pending.load()&~bit);}
    if(!Feeding(a))return;
    if(alpha!=0){
        // A sub-threshold alpha skips UpdateInternal; count only updates that
        // copy this context's exact delta.
        if(alpha>kNativeRelevantAlpha&&alpha<=1&&dt>0&&sample&&native_dt==dt&&r.gate(r.user,node,proxy,skeleton)==Gate::Allowed)++g.natural_positive_delta_updates;
        return;}
    if(type!=2||dt<=0)return;
    if(external){Fail(a,603);return;}
    if(r.gate(r.user,node,proxy,skeleton)!=Gate::Allowed)return;
    float one=1;g.pending_frame=frame;g.pending_counter=counter;g.pending_token_valid=true;ForcePending(a,a.force_pending.load()|bit);
    if(!r.write(r.user,node+0x28,&one,4)){Fail(a,604);return;}
    if(!r.internal(r.user,node,context)){r.write(r.user,node+0x28,&alpha,4);Fail(a,605);return;}
    float result_alpha=0,result_dt=0;r.Get(node+0x28,result_alpha);r.Get(node+0x280,result_dt);
    g.alpha.store(result_alpha);g.delta.store(result_dt);++g.forced;++a.group_forced;
}
// Immutable registry while installed.
struct Registry {
    std::vector<Actor*> actors;std::atomic<size_t> count{0};std::atomic<bool> frozen{false};mutable std::mutex operations;
    bool Freeze(){std::lock_guard<std::mutex> lock(operations);if(frozen.load()||!count)return false;frozen.store(true,std::memory_order_release);return true;}
    void Thaw(){std::lock_guard<std::mutex> lock(operations);frozen.store(false,std::memory_order_release);}
    bool Add(Actor& a){std::lock_guard<std::mutex> lock(operations);if(frozen)return false;for(const auto* entry:actors)if(entry==&a)return false;
        actors.push_back(&a);count.store(actors.size(),std::memory_order_release);return true;}
    bool Remove(Actor& a){std::lock_guard<std::mutex> lock(operations);if(frozen)return false;for(size_t i=0;i<actors.size();++i)if(actors[i]==&a){
        actors.erase(actors.begin()+i);count.store(actors.size(),std::memory_order_release);return true;}return false;}
    Actor* Input(uintptr_t node) const {if(!frozen.load(std::memory_order_acquire))return nullptr;for(auto* actor:actors)if(actor->node==node)return actor;return nullptr;}
    Actor* Group(uintptr_t node,unsigned* index) const {if(!index||!frozen.load(std::memory_order_acquire))return nullptr;for(auto* actor:actors){auto& a=*actor;for(unsigned k=0;k<a.binding.group_count;++k)if(a.target.main_anim+a.binding.groups[k].offset==node){*index=k;return &a;}}return nullptr;}
    bool RestoredAll() const {std::lock_guard<std::mutex> lock(operations);for(const auto* actor:actors)if(!Restored(*actor))return false;return true;}
};
}} // namespace wwp_playback::native_physics
