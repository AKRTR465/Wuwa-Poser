#pragma once
#include "../../runtime/generated/GameCompat.generated.h"
// Identity registration is separate from native_graph_discovery. A weak token
// here is only a candidate for the engine's allocator, never a graph binding.
#include "native_graph_discovery.h"

namespace native_identity_bootstrap {
namespace ng=native_graph_discovery;
constexpr unsigned kOwners=32,kRejections=64;
enum class Code:unsigned {None,ContextChanged,CandidateInvalid,OwnerLimit,NativeRejected,NativeException,IdentityChanged};
inline const char* Name(Code code){switch(code){
    case Code::None:return "none";case Code::ContextChanged:return "callback_context_changed";case Code::CandidateInvalid:return "candidate_identity_invalid";
    case Code::OwnerLimit:return "owner_limit";case Code::NativeRejected:return "allocator_gate_rejected";case Code::NativeException:return "allocator_exception";
    default:return "identity_changed_during_registration";}}
struct Basis {ng::GoView go;uint64_t image=0;ng::Token mesh,pawn,controller,mesh_asset,skeleton_asset,main_candidate;native_animation_target::AuthorityTarget authority;};
struct Context {uint64_t proxy=0,mesh=0,owner=0,owner_alt=0,main_proxy=0,main_owner=0,main_owner_alt=0,main_mesh=0,main_self=0,skeleton=0;};
template<class Read>bool CaptureContext(Read&& read,const Basis& b,uint64_t address,Context& value){
    if(!read(address+ww_game_compat::AnimContextProxyOffset,&value.proxy,sizeof(value.proxy))||!ng::Range(value.proxy,ww_game_compat::AnimProxyIdentityBytes)||!read(value.proxy+ww_game_compat::AnimProxyMeshOffset,&value.mesh,sizeof(uint64_t))||value.mesh!=b.mesh.address)return false;
    return read(value.proxy+ww_game_compat::AnimProxyOwnerOffset,&value.owner,sizeof(uint64_t))&&read(value.proxy+ww_game_compat::AnimProxyOwnerAltOffset,&value.owner_alt,sizeof(uint64_t))&&value.owner==value.owner_alt&&
        read(value.proxy+ww_game_compat::AnimProxyMainProxyOffset,&value.main_proxy,sizeof(uint64_t))&&ng::Range(value.main_proxy,ww_game_compat::AnimProxyIdentityBytes)&&
        read(value.main_proxy+ww_game_compat::AnimProxyOwnerOffset,&value.main_owner,sizeof(uint64_t))&&value.main_owner==b.main_candidate.address&&
        read(value.main_proxy+ww_game_compat::AnimProxyOwnerAltOffset,&value.main_owner_alt,sizeof(uint64_t))&&value.main_owner_alt==value.main_owner&&
        read(value.main_proxy+ww_game_compat::AnimProxyMeshOffset,&value.main_mesh,sizeof(uint64_t))&&value.main_mesh==b.mesh.address&&
        read(value.main_proxy+ww_game_compat::AnimProxyMainProxyOffset,&value.main_self,sizeof(uint64_t))&&value.main_self==value.main_proxy&&
        read(value.proxy+ww_game_compat::AnimProxySkeletonOffset,&value.skeleton,sizeof(uint64_t))&&value.skeleton==b.skeleton_asset.address&&
        (value.owner!=value.main_owner||value.proxy==value.main_proxy);
}
struct Candidate {ng::Token object,klass;};
template<class Read,class ClassAllowed>bool CaptureCandidate(Read&& read,const Basis& b,uint64_t address,Candidate& value,ClassAllowed&& allowed){
    if(!ng::CaptureToken(read,b.go,address,value.object,false)||!ng::CaptureToken(read,b.go,ng::Class(value.object),value.klass,false))return false;
    return ng::Get<uint64_t>(value.object.header.data(),ww_game_compat::ObjectOuterOffset)==b.mesh.address&&ng::Get<uint32_t>(value.object.header.data(),ww_game_compat::ObjectNameOffset)!=0&&
        ng::Get<uint32_t>(value.object.header.data(),ww_game_compat::ObjectNameOffset+ww_game_compat::NameWordBytes)!=0&&allowed(value.klass);
}
struct NativeResult {int32_t serial=0;bool called=false;Code code=Code::NativeRejected;};
struct Owner {ng::Token before,after,klass;bool main=false,allocator_called=false;int32_t returned_serial=0;Code code=Code::None;
    unsigned thread=0;uint64_t tick=0;bool accepted=false;};
struct Rejection {uint64_t owner=0;Code code=Code::None;unsigned thread=0;};
struct State {
    Basis basis;std::atomic_flag gate=ATOMIC_FLAG_INIT;std::atomic<bool> failed{false};
    std::atomic<uint64_t> busy_callbacks{0},target_callbacks{0},rejected_callbacks{0};
    std::array<Owner,kOwners> owners{};std::array<Rejection,kRejections> rejections{};
    std::array<Owner,2> supports{};unsigned support_count=0;
    unsigned owner_limit=kOwners,owner_count=0,rejection_count=0,native_calls=0;bool truncated=false,main_confirmed=false;
    void Reject(uint64_t owner,Code code,unsigned thread){failed.store(true);++rejected_callbacks;
        if(rejection_count<kRejections)rejections[rejection_count++]={owner,code,thread};else truncated=true;}
};
template<class Read,class Allocate>
bool RegisterSupport(State& state,Read&& read,Allocate&& allocate,ng::Token& expected,unsigned thread,uint64_t tick){
    for(unsigned i=0;i<state.support_count;++i)if(state.supports[i].before.address==expected.address)return state.supports[i].accepted;
    if(state.support_count==state.supports.size()){state.Reject(expected.address,Code::OwnerLimit,thread);return false;}
    auto& record=state.supports[state.support_count++];record.thread=thread;record.tick=tick;
    if(!ng::CaptureToken(read,state.basis.go,expected.address,record.before,false)||!ng::CaptureToken(read,state.basis.go,ng::Class(record.before),record.klass,false)){
        record.code=Code::IdentityChanged;state.Reject(expected.address,record.code,thread);return false;}
    if(!ng::Serial(record.before)){const auto returned=allocate(ng::Index(record.before));record.allocator_called=returned.called;record.returned_serial=returned.serial;
        if(returned.called)++state.native_calls;
        if(returned.code!=Code::None){record.code=returned.code;state.Reject(expected.address,record.code,thread);return false;}}
    else record.returned_serial=int32_t(ng::Serial(record.before));
    if(!ng::CaptureToken(read,state.basis.go,expected.address,record.after)){record.code=Code::IdentityChanged;state.Reject(expected.address,record.code,thread);return false;}
    record.accepted=true;expected=record.after;return true;
}
template<class Read,class ClassAllowed,class Allocate>
bool Register(State& state,Read&& read,ClassAllowed&& allowed,Allocate&& allocate,uint64_t address,bool main,unsigned thread,uint64_t tick){
    for(unsigned index=0;index<state.owner_count;++index)if(state.owners[index].before.address==address)return state.owners[index].accepted;
    if(state.owner_count>=state.owner_limit){state.truncated=true;state.Reject(address,Code::OwnerLimit,thread);return false;}
    Candidate before;if(!CaptureCandidate(read,state.basis,address,before,allowed)){state.Reject(address,Code::CandidateInvalid,thread);return false;}
    auto& record=state.owners[state.owner_count++];record.before=before.object;record.klass=before.klass;record.main=main;record.thread=thread;record.tick=tick;
    if(!ng::Serial(before.object)){
        const auto returned=allocate(ng::Index(before.object));record.allocator_called=returned.called;record.returned_serial=returned.serial;
        if(returned.called)++state.native_calls;
        if(returned.code!=Code::None){record.code=returned.code;state.Reject(address,record.code,thread);return false;}
    }else record.returned_serial=int32_t(ng::Serial(before.object));
    if(!ng::CaptureToken(read,state.basis.go,address,record.after)){record.code=Code::IdentityChanged;state.Reject(address,record.code,thread);return false;}
    record.accepted=true;if(main)state.main_confirmed=true;return true;
}
template<class Read,class ClassAllowed,class Allocate>
void Visit(State& state,Read&& read,ClassAllowed&& allowed,Allocate&& allocate,unsigned hook,uint64_t node,uint64_t context,unsigned thread,uint64_t tick){
    uint64_t proxy=0,mesh=0;if(!read(context+ww_game_compat::AnimContextProxyOffset,&proxy,sizeof(proxy))||!ng::Range(proxy,ww_game_compat::AnimProxyMeshOffset+ww_game_compat::PointerBytes)||!read(proxy+ww_game_compat::AnimProxyMeshOffset,&mesh,sizeof(uint64_t))||mesh!=state.basis.mesh.address)return;
    ++state.target_callbacks;
    if(state.gate.test_and_set(std::memory_order_acquire)){++state.busy_callbacks;state.failed.store(true);return;}
    struct Unlock {std::atomic_flag& flag;~Unlock(){flag.clear(std::memory_order_release);}} unlock{state.gate};
    if(state.failed.load())return;
    Context before;uint64_t vtable=0;
    if(!read(node,&vtable,sizeof(uint64_t))||vtable!=state.basis.image+ng::kSpecs[hook].vtable_rva||!CaptureContext(read,state.basis,context,before)){
        state.Reject(0,Code::ContextChanged,thread);return;}
    if(!RegisterSupport(state,read,allocate,state.basis.mesh_asset,thread,tick)||
       !RegisterSupport(state,read,allocate,state.basis.skeleton_asset,thread,tick))return;
    if(!Register(state,read,allowed,allocate,before.main_owner,true,thread,tick))return;
    if(before.owner!=before.main_owner)Register(state,read,allowed,allocate,before.owner,false,thread,tick);
}
inline bool Complete(const State& state){return !state.failed.load()&&state.main_confirmed&&!state.truncated&&!state.busy_callbacks.load();}
} // namespace native_identity_bootstrap
