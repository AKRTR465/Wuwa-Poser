#pragma once
// Fixed-camera post-UpdateCamera overlay. Caller supplies the exact original
// callback phase, live identity reader and field writer. No native calls,
// process APIs, allocations, hooks or clocks in this helper.
#include "ww_camera_snapshot.h"
namespace wwp_playback { namespace camera_runtime {
namespace snap=camera_snapshot;namespace id=ww_restore;
constexpr uint32_t kFields=6;
struct Prepared {
    snap::Manifest snapshot;
    uint64_t session=0;
    uint32_t game_thread=0,last_cache=0x38e0;
    uint64_t expected_update_target=0;
};
struct Phase {uint64_t session=0,pcm=0,update_target=0;uint32_t thread=0;bool after_original_update=false,controlled_authority=false;};
struct Field {uint32_t offset=0,bytes=0;std::array<uint8_t,12> original{},written{};bool pending=false;};
struct Frame {std::array<float,3> location{},rotation{};float fov=60;};
struct State {
    std::array<Field,kFields> fields{};bool active=false,frame_ready=false;
    // A lazy PCM serial may be assigned once within this callback session.
    id::Token promoted_pcm;bool serial_promoted=false;
};
inline bool Pending(const State& s){for(const auto& f:s.fields)if(f.pending)return true;return false;}
inline bool InPhase(const Prepared& p,const Phase& ph){return ph.session==p.session&&ph.thread==p.game_thread&&ph.pcm==p.snapshot.pcm.address&&
    ph.update_target==p.expected_update_target&&ph.after_original_update;}
template<class Reader>bool Strong(Reader& r,const Prepared& p,const Phase& phase,State& s,bool allow_detached){
    if(!InPhase(p,phase))return false;
    const auto& m=p.snapshot;const auto& expected=s.serial_promoted?s.promoted_pcm:m.pcm;
    id::Token pcm,controller;uint64_t controller_pcm=0;
    if(!snap::PcmNameMatches(r,m)||!r.Identify(m.pcm.address,&pcm)||!r.Identify(m.controller.address,&controller))return false;
    const bool promote=!expected.serial&&pcm.serial&&m.allow_unassigned_pcm_serial&&!s.serial_promoted;
    auto observed=pcm;if(promote)observed.serial=0;
    if(!snap::SameFields(expected,observed)||!snap::SameFields(controller,m.controller))return false;
    if(!snap::Field(r,controller.address,m.layout.controller_pcm,&controller_pcm,8))return false;
    if(!allow_detached&&controller_pcm!=pcm.address)return false;
    if(promote){s.promoted_pcm=pcm;s.serial_promoted=true;}
    return true;
}
// Reconcile the last write after the next original native update. If native
// camera replaced a field, release it; never carry previous overlay bytes as a
// new baseline.
template<class Reader>bool ObservePrevious(Reader& r,const Prepared& p,State& s){
    for(uint32_t i=0;i<kFields;++i){auto& field=s.fields[i];if(!field.pending)continue;std::array<uint8_t,12> value{};
        if(!snap::Field(r,p.snapshot.pcm.address,field.offset,value.data(),field.bytes))return false;
        if(std::memcmp(value.data(),field.written.data(),field.bytes))field.pending=false;
    }
    return true;
}
template<class Reader>bool Begin(Reader& r,const Prepared& p,const Phase& phase,State* state)noexcept{
    state->frame_ready=false;
    if(!phase.controlled_authority||!Strong(r,p,phase,*state,false)||!ObservePrevious(r,p,*state))return false;
    const auto& l=p.snapshot.layout;const uint32_t base[2]={l.pcm_cache+l.pov,p.last_cache+l.pov};
    for(uint32_t cache=0;cache<2;++cache)for(uint32_t property=0;property<3;++property){auto& f=state->fields[cache*3+property];
        if(f.pending)continue;
        f.offset=base[cache]+(property==0?l.location:property==1?l.rotation:l.fov);f.bytes=property<2?12:4;
        if(!snap::Field(r,p.snapshot.pcm.address,f.offset,f.original.data(),f.bytes))return false;
    }
    state->active=true;state->frame_ready=true;return true;
}
template<class Reader,class Writer>bool Apply(Reader& r,Writer& writer,const Prepared& p,const Frame& frame,State* state)noexcept{
    if(!state->active||!state->frame_ready)return false;
    state->frame_ready=false;
    for(uint32_t i=0;i<kFields;++i){auto& field=state->fields[i];const auto property=i%3;const void* value=property==0?(const void*)frame.location.data():property==1?(const void*)frame.rotation.data():(const void*)&frame.fov;
        std::array<uint8_t,12> desired{},current{};std::memcpy(desired.data(),value,field.bytes);
        if(!snap::Field(r,p.snapshot.pcm.address,field.offset,current.data(),field.bytes))return false;
        if(!std::memcmp(current.data(),desired.data(),field.bytes))continue;
        field.written=desired;field.pending=true;
        if(!writer.Write(p.snapshot.pcm.address+field.offset,desired.data(),field.bytes))return false;
    }
    return true;
}
// Restoration ignores current pawn control and a detached controller relation.
template<class Reader,class Writer>bool Restore(Reader& r,Writer& writer,const Prepared& p,const Phase& phase,State* state)noexcept{
    state->active=state->frame_ready=false;
    if(!Pending(*state))return true;
    if(!Strong(r,p,phase,*state,true)||!ObservePrevious(r,p,*state))return false;
    for(uint32_t i=0;i<kFields;++i){auto& field=state->fields[i];if(!field.pending)continue;
        if(!writer.Write(p.snapshot.pcm.address+field.offset,field.original.data(),field.bytes))return false;
        field.pending=false;
    }
    return true;
}
}} // namespace wwp_playback::camera_runtime
