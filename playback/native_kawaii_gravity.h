#pragma once
#include "../runtime/generated/GameCompat.generated.h"
#include <array>
#include <atomic>
#include <cmath>
#include <cstdint>
#include <cstring>
#include <mutex>

namespace wwp_playback::kawaii_gravity {
// GroupSimulate reads three float components and converts the world vector
// into component space using the conjugated component rotation and inverse scale.
inline constexpr uintptr_t kNodeGravity=ww_game_compat::KawaiiGravityOffset,kConjugateSignsRva=ww_game_compat::GravityConjugateSignsRva;
using Vector=std::array<float,3>;
static_assert(sizeof(Vector)==12,"expected UE FVector3f");

struct Sample {
    Vector original{},effective{};
    uint64_t revision=0,applied=0,restored=0;
    bool read=false,suspended_by_parts=false;
};
struct State {
    mutable std::mutex edit;
    float requested=-1;
    uint64_t revision=0;
    Sample sample;
    Vector saved{};
    std::atomic<bool> restore_pending{false};
};

inline bool Finite(const Vector& vector){
    for(float component:vector)if(!std::isfinite(component))return false;
    return true;
}
inline bool ConjugateSigns(const std::array<float,4>& signs){
    return signs==std::array<float,4>{-1,-1,-1,1}||signs==std::array<float,4>{1,1,1,-1};
}

template<class Access> bool Restore(State& state,uintptr_t address,const Access& access){
    Vector actual{};
    const bool restored=access.write(access.user,address,&state.saved,sizeof(Vector))&&access.Get(address,actual)&&
        std::memcmp(actual.data(),state.saved.data(),sizeof(Vector))==0;
    if(restored)state.restore_pending.store(false,std::memory_order_release);
    return restored;
}

// The caller serializes evaluation of the selected registered node. Publication
// uses the copied telemetry, after the original vector has been restored.
template<class Access,class Evaluate,class Fault>
void EvaluateGravity(State& state,uintptr_t node,const Access& access,bool feed,Evaluate&& evaluate,Fault&& fault,bool covered=false){
    const auto address=node+kNodeGravity;
    if(state.restore_pending.load(std::memory_order_acquire)&&!Restore(state,address,access)){fault();return;}
    float strength=-1;Sample sample;
    {std::lock_guard<std::mutex> lock(state.edit);strength=state.requested;sample=state.sample;sample.revision=state.revision;}
    sample.read=access.Get(address,sample.original)&&Finite(sample.original);
    sample.effective=sample.original;
    sample.suspended_by_parts=feed&&strength>=0&&covered;
    if(feed&&strength>=0&&!covered){
        if(!sample.read||!access.writable(access.user,address,sizeof(Vector))){fault();evaluate();return;}
        const Vector requested{0,0,-strength};
        state.saved=sample.original;
        state.restore_pending.store(true,std::memory_order_release);
        if(!access.write(access.user,address,&requested,sizeof(Vector))||!access.Get(address,sample.effective)||sample.effective!=requested){
            if(!Restore(state,address,access)){fault();return;}
            fault();evaluate();return;
        }
        struct Recovery {
            State& state;uintptr_t address;const Access& access;Fault& fault;Sample& sample;
            ~Recovery(){if(Restore(state,address,access))++sample.restored;else fault();}
        };
        {Recovery recovery{state,address,access,fault,sample};evaluate();++sample.applied;}
    }else evaluate();
    std::lock_guard<std::mutex> lock(state.edit);state.sample=sample;
}
}
