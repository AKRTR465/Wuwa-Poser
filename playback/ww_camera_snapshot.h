#pragma once
// Caller-injected camera chain identity and field helpers. No native calls,
// writes, allocation or process APIs.
#include "ww_restore_policy.h"
#include <array>
#include <cstring>
#include <cmath>
namespace wwp_playback { namespace camera_snapshot {
namespace identity=ww_restore;
struct Layout {
    uint32_t controller_pcm,controller_pawn,controller_ack,pawn_controller;
    uint32_t pcm_view_target,pcm_cache,pov;
    uint32_t location,rotation,fov,aspect_ratio;
};
constexpr Layout kCurrent{0x360,0x2e8,0x340,0x2e8,0x1880,0x2e40,0x10,0,0xc,0x18,0x2c};
struct Manifest {
    Layout layout=kCurrent;
    identity::Token controller,pawn,pcm;
    // Only this PCM may use an unassigned lazy serial; its exact live FName is
    // then required instead.
    bool allow_unassigned_pcm_serial=false;
    std::array<uint32_t,3> pcm_name_words{};
};
inline bool SameFields(const identity::Token& a,const identity::Token& b){return a.address==b.address&&a.item_address==b.item_address&&
    a.class_address==b.class_address&&a.vtable==b.vtable&&a.outer_address==b.outer_address&&a.index==b.index&&a.serial==b.serial;}
template<class Reader>bool Field(Reader& r,uint64_t base,uint32_t off,void* out,size_t bytes){return r.Read(base+off,out,bytes);}
template<class Reader>bool PcmNameMatches(Reader& r,const Manifest& m){
    if(m.pcm.serial!=0)return true;
    std::array<uint32_t,3> actual{};
    return Field(r,m.pcm.address,24,actual.data(),12)&&actual==m.pcm_name_words;}
}} // namespace wwp_playback::camera_snapshot
