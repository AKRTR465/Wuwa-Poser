#pragma once
#include "../runtime/generated/GameCompat.generated.h"
// Fixed-camera post-UpdateCamera overlay. Caller supplies the exact original
// callback phase, live identity reader and field writer. No native calls,
// process APIs, allocations, hooks or clocks in this helper.
#include <array>
#include <cstdint>
#include <cstring>
namespace wwp_playback { namespace camera_runtime {
constexpr uint32_t kFields=6;
struct Token {
    uint64_t address=0,item_address=0,class_address=0,vtable=0,outer_address=0;
    uint32_t index=UINT32_MAX,serial=0;
};
inline bool Same(const Token& a,const Token& b){return a.address==b.address&&a.item_address==b.item_address&&
    a.class_address==b.class_address&&a.vtable==b.vtable&&a.outer_address==b.outer_address&&a.index==b.index&&a.serial==b.serial;}
struct Manifest {
    Token controller,pawn,pcm;
    // Only this PCM may use an unassigned lazy serial; its exact live FName is
    // then required instead.
    bool allow_unassigned_pcm_serial=false;
    std::array<uint32_t,ww_game_compat::NameWordCount> pcm_name_words{};
};
struct Prepared {
    Manifest snapshot;
    uint64_t session=0;
    uint32_t game_thread=0;
    uint64_t expected_update_target=0;
};
struct Phase {uint64_t session=0,pcm=0,update_target=0;uint32_t thread=0;bool after_original_update=false,controlled_authority=false;};
struct Field {uint32_t offset=0,bytes=0;std::array<uint8_t,ww_game_compat::VectorBytes> original{},written{};bool pending=false;};
struct Frame {std::array<float,3> location{},rotation{};float fov=60;};
struct State {
    std::array<Field,kFields> fields{};bool active=false,frame_ready=false;
    // A lazy PCM serial may be assigned once within this callback session.
    Token promoted_pcm;bool serial_promoted=false;
};
template<class Reader>bool PcmNameMatches(Reader& r,const Manifest& m){
    if(m.pcm.serial!=0)return true;
    std::array<uint32_t,ww_game_compat::NameWordCount> actual{};
    return r.Read(m.pcm.address+ww_game_compat::ObjectNameOffset,actual.data(),ww_game_compat::NameBytes)&&actual==m.pcm_name_words;}
inline bool Pending(const State& s){for(const auto& f:s.fields)if(f.pending)return true;return false;}
inline bool InPhase(const Prepared& p,const Phase& ph){return ph.session==p.session&&ph.thread==p.game_thread&&ph.pcm==p.snapshot.pcm.address&&
    ph.update_target==p.expected_update_target&&ph.after_original_update;}
template<class Reader>bool Strong(Reader& r,const Prepared& p,const Phase& phase,State& s,bool allow_detached){
    if(!InPhase(p,phase))return false;
    const auto& m=p.snapshot;const auto& expected=s.serial_promoted?s.promoted_pcm:m.pcm;
    Token pcm,controller;uint64_t controller_pcm=0;
    if(!PcmNameMatches(r,m)||!r.Identify(m.pcm.address,&pcm)||!r.Identify(m.controller.address,&controller))return false;
    const bool promote=!expected.serial&&pcm.serial&&m.allow_unassigned_pcm_serial&&!s.serial_promoted;
    auto observed=pcm;if(promote)observed.serial=0;
    if(!Same(expected,observed)||!Same(controller,m.controller))return false;
    if(!r.Read(controller.address+ww_game_compat::ControllerCameraManagerOffset,&controller_pcm,8))return false;
    if(!allow_detached&&controller_pcm!=pcm.address)return false;
    if(promote){s.promoted_pcm=pcm;s.serial_promoted=true;}
    return true;
}
// Reconcile the last write after the next original native update. If native
// camera replaced a field, release it; never carry previous overlay bytes as a
// new baseline.
template<class Reader>bool ObservePrevious(Reader& r,const Prepared& p,State& s){
    for(auto& field:s.fields){if(!field.pending)continue;std::array<uint8_t,ww_game_compat::VectorBytes> value{};
        if(!r.Read(p.snapshot.pcm.address+field.offset,value.data(),field.bytes))return false;
        if(std::memcmp(value.data(),field.written.data(),field.bytes))field.pending=false;
    }
    return true;
}
template<class Reader>bool Begin(Reader& r,const Prepared& p,const Phase& phase,State* state)noexcept{
    state->frame_ready=false;
    if(!phase.controlled_authority||!Strong(r,p,phase,*state,false)||!ObservePrevious(r,p,*state))return false;
    constexpr uint32_t base[2]={ww_game_compat::CameraCacheOffset+ww_game_compat::CameraPovOffset,ww_game_compat::CameraLastCacheOffset+ww_game_compat::CameraPovOffset};
    constexpr uint32_t property_offset[3]={ww_game_compat::CameraLocationOffset,ww_game_compat::CameraRotationOffset,ww_game_compat::CameraFovOffset};
    for(uint32_t cache=0;cache<2;++cache)for(uint32_t property=0;property<3;++property){auto& f=state->fields[cache*3+property];
        if(f.pending)continue;
        f.offset=base[cache]+property_offset[property];f.bytes=property<2?ww_game_compat::VectorBytes:ww_game_compat::FloatBytes;
        if(!r.Read(p.snapshot.pcm.address+f.offset,f.original.data(),f.bytes))return false;
    }
    state->active=true;state->frame_ready=true;return true;
}
template<class Reader,class Writer>bool Apply(Reader& r,Writer& writer,const Prepared& p,const Frame& frame,State* state)noexcept{
    if(!state->active||!state->frame_ready)return false;
    state->frame_ready=false;
    const void* values[3]={frame.location.data(),frame.rotation.data(),&frame.fov};
    for(uint32_t i=0;i<kFields;++i){auto& field=state->fields[i];
        std::array<uint8_t,ww_game_compat::VectorBytes> desired{},current{};std::memcpy(desired.data(),values[i%3],field.bytes);
        if(!r.Read(p.snapshot.pcm.address+field.offset,current.data(),field.bytes))return false;
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
    for(auto& field:state->fields){if(!field.pending)continue;
        if(!writer.Write(p.snapshot.pcm.address+field.offset,field.original.data(),field.bytes))return false;
        field.pending=false;
    }
    return true;
}
}} // namespace wwp_playback::camera_runtime
