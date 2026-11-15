#pragma once

#include "native_kawaii_parameters.h"

namespace wwp_playback::kawaii_collision {
namespace parameters=kawaii_parameters;
inline constexpr size_t kCapsuleArrays[]={0x180,0x1b8};
inline constexpr size_t kCapsuleIdentity=0x0c,kCapsuleBytes=0x60;
struct alignas(16) Capsule {
    std::array<uint8_t,kCapsuleBytes> bytes;
};
static_assert(sizeof(Capsule)==kCapsuleBytes,"verified capsule stride");
struct Driver {
    unsigned bone=0;
    unsigned original=0,active=0;
    bool enabled=true;
};
struct Buffer {
    parameters::Array original{},filtered{};
    std::vector<Capsule> records;
    bool swapped=false;
};
struct State {
    mutable std::mutex edit;
    std::vector<Driver> drivers;
    bool available=false;
    std::array<Buffer,2> buffers;
    std::atomic<bool> restore_pending{false};
};

template<class Access> bool Restore(State& state,uintptr_t node,const Access& access) {
    bool all=true;
    for(unsigned i=0;i<state.buffers.size();++i){
        auto& buffer=state.buffers[i];if(!buffer.swapped)continue;
        const auto address=node+kCapsuleArrays[i];parameters::Array current{};
        if(access.Get(address,current)&&(parameters::SameArray(current,buffer.original)||
            (parameters::SameArray(current,buffer.filtered)&&access.write(access.user,address,&buffer.original,sizeof(buffer.original))&&
             access.Get(address,current)&&parameters::SameArray(current,buffer.original))))buffer.swapped=false;
        all=all&&!buffer.swapped;
    }
    state.restore_pending.store(!all,std::memory_order_release);return all;
}

template<class Access,class Map,class Evaluate,class Fault>
void EvaluateCapsules(State& state,uintptr_t node,const Access& access,bool feed,Map&& map,Evaluate&& evaluate,Fault&& fault) {
    if(state.restore_pending.load(std::memory_order_acquire)&&!Restore(state,node,access)){fault();evaluate();return;}
    std::vector<Driver> requested;bool available=false;
    {std::lock_guard<std::mutex> lock(state.edit);requested=state.drivers;available=state.available;}
    const bool edits=feed&&std::any_of(requested.begin(),requested.end(),[](const auto& d){return !d.enabled;});
    if(available&&!edits){
        evaluate();
        std::lock_guard<std::mutex> lock(state.edit);for(auto& driver:state.drivers)driver.active=driver.original;
        return;
    }
    parameters::Array simulation{};
    std::array<int32_t,1024> compact_to_skeleton{};
    if(!access.Get(node+parameters::kBoneArray,simulation)||!parameters::ValidArray(simulation)||!simulation.count||!map(compact_to_skeleton)){
        evaluate();return;
    }
    std::vector<Driver> observed;
    for(unsigned i=0;i<state.buffers.size();++i){
        auto& buffer=state.buffers[i];buffer.records.clear();
        if(!access.Get(node+kCapsuleArrays[i],buffer.original)||!parameters::ValidArray(buffer.original)){evaluate();return;}
        if(edits)buffer.records.reserve(size_t(buffer.original.count));
        for(int32_t index=0;index<buffer.original.count;++index){
            Capsule record{};
            if(!access.Get(buffer.original.data+size_t(index)*kCapsuleBytes,record)){evaluate();return;}
            std::array<int32_t,2> identity{};
            std::memcpy(identity.data(),record.bytes.data()+kCapsuleIdentity,sizeof(identity));
            const auto bone=parameters::SkeletonIndex(identity,compact_to_skeleton);
            bool disabled=false;
            if(bone>=0){
                const auto setting=std::find_if(requested.begin(),requested.end(),[&](const auto& d){return d.bone==unsigned(bone);});
                disabled=edits&&setting!=requested.end()&&!setting->enabled;
                auto driver=std::find_if(observed.begin(),observed.end(),[&](const auto& d){return d.bone==unsigned(bone);});
                if(driver==observed.end())observed.push_back({unsigned(bone),1,disabled?0u:1u,!disabled});
                else {++driver->original;if(!disabled)++driver->active;}
            }
            if(edits&&!disabled)buffer.records.push_back(record);
        }
    }
    for(unsigned i=0;edits&&i<state.buffers.size();++i){
        auto& buffer=state.buffers[i];
        if(buffer.records.size()==size_t(buffer.original.count))continue;
        const auto address=node+kCapsuleArrays[i];parameters::Array current{};
        if(!access.Get(address,current)||!parameters::SameArray(current,buffer.original)||!access.writable(access.user,address,sizeof(current))){Restore(state,node,access);fault();evaluate();return;}
        const auto count=int32_t(buffer.records.size());
        buffer.filtered={count?reinterpret_cast<uintptr_t>(buffer.records.data()):0,count,count};
        buffer.swapped=true;state.restore_pending.store(true,std::memory_order_release);
        if(!access.write(access.user,address,&buffer.filtered,sizeof(buffer.filtered))){Restore(state,node,access);fault();evaluate();return;}
    }
    struct Recovery {
        State& state;uintptr_t node;const Access& access;Fault& fault;
        ~Recovery(){if(state.restore_pending.load(std::memory_order_acquire)&&!Restore(state,node,access))fault();}
    };
    {Recovery recovery{state,node,access,fault};evaluate();}
    std::lock_guard<std::mutex> lock(state.edit);
    for(auto& driver:observed){const auto current=std::find_if(state.drivers.begin(),state.drivers.end(),[&](const auto& d){return d.bone==driver.bone;});
        if(current!=state.drivers.end())driver.enabled=current->enabled;}
    state.drivers=std::move(observed);state.available=true;
}
}
