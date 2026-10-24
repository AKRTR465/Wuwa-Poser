#pragma once
#include <cstdint>

// Pure worker-side recovery progression. A failed phase never authorizes a
// later phase or terminal result; successful phases are not replayed.
namespace owned_recovery {
// Failed binding has no actor-side effect. A Begin that actually entered the
// managed method may have created partial ownership even when it throws.
struct InvocationBoundary {
    bool begin_entered=false;
    template<class Bind,class Session,class Action>void Run(bool begin,Bind&& bind,Session&& session,Action&& action){
        bind();session();if(begin)begin_entered=true;action();
    }
};
enum class Phase { Native, Observers, ManagedStop, ManagedRelease, Complete };
struct Progress {
    bool actor_required=false,native_restored=false,observers_restored=false,managed_stopped=false,managed_released=false;
    uint64_t attempts=0,next_attempt_ms=0;
    explicit Progress(bool required):actor_required(required){}
    Phase Current()const noexcept {
        if(!native_restored)return Phase::Native;
        if(!observers_restored)return Phase::Observers;
        if(actor_required&&!managed_stopped)return Phase::ManagedStop;
        if(actor_required&&!managed_released)return Phase::ManagedRelease;
        return Phase::Complete;
    }
    bool Complete()const noexcept{return Current()==Phase::Complete;}
    void Defer(uint64_t now,uint64_t interval_ms=2000)noexcept{next_attempt_ms=now+interval_ms;}
    template<class Native,class Observers,class Stop,class Released>
    bool Advance(uint64_t now,Native&& native,Observers&& observers,Stop&& stop,Released&& released){
        if(Complete())return true;
        if(now<next_attempt_ms)return false;
        ++attempts;
        if(!native_restored){if(!native())return false;native_restored=true;}
        if(!observers_restored){if(!observers())return false;observers_restored=true;}
        if(actor_required&&!managed_stopped){if(!stop())return false;managed_stopped=true;}
        if(actor_required&&!managed_released){if(!released())return false;managed_released=true;}
        return Complete();
    }
};
} // namespace owned_recovery
