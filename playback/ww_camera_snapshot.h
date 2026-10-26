#pragma once
#include "../runtime/generated/GameCompat.generated.h"
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
constexpr Layout kCurrent{ww_game_compat::ControllerCameraManagerOffset,ww_game_compat::ControllerPawnOffset,ww_game_compat::ControllerAcknowledgedPawnOffset,ww_game_compat::PawnControllerOffset,ww_game_compat::CameraViewTargetOffset,ww_game_compat::CameraCacheOffset,ww_game_compat::CameraPovOffset,ww_game_compat::CameraLocationOffset,ww_game_compat::CameraRotationOffset,ww_game_compat::CameraFovOffset,ww_game_compat::CameraAspectRatioOffset};
struct Manifest {
    Layout layout=kCurrent;
    identity::Token controller,pawn,pcm;
    // Only this PCM may use an unassigned lazy serial; its exact live FName is
    // then required instead.
    bool allow_unassigned_pcm_serial=false;
    std::array<uint32_t,ww_game_compat::NameWordCount> pcm_name_words{};
};
inline bool SameFields(const identity::Token& a,const identity::Token& b){return a.address==b.address&&a.item_address==b.item_address&&
    a.class_address==b.class_address&&a.vtable==b.vtable&&a.outer_address==b.outer_address&&a.index==b.index&&a.serial==b.serial;}
template<class Reader>bool Field(Reader& r,uint64_t base,uint32_t off,void* out,size_t bytes){return r.Read(base+off,out,bytes);}
template<class Reader>bool PcmNameMatches(Reader& r,const Manifest& m){
    if(m.pcm.serial!=0)return true;
    std::array<uint32_t,ww_game_compat::NameWordCount> actual{};
    return Field(r,m.pcm.address,ww_game_compat::ObjectNameOffset,actual.data(),ww_game_compat::NameBytes)&&actual==m.pcm_name_words;}
}} // namespace wwp_playback::camera_snapshot
