#pragma once
#include "../runtime/generated/GameCompat.generated.h"
// Current native getter-backed FTransform48 decoding.
#include <array>
#include <cstdint>
#include <cstring>
namespace wwp_playback { namespace world_snapshot {
struct Transform {std::array<float,4> rotation{};std::array<float,3> translation{},scale{};};
inline Transform Decode(const std::array<uint8_t,ww_game_compat::TransformBytes>& b){Transform t;
    std::memcpy(t.rotation.data(),b.data()+ww_game_compat::TransformRotationOffset,ww_game_compat::TransformRotationBytes);
    std::memcpy(t.translation.data(),b.data()+ww_game_compat::TransformTranslationOffset,ww_game_compat::VectorBytes);
    std::memcpy(t.scale.data(),b.data()+ww_game_compat::TransformScaleOffset,ww_game_compat::VectorBytes);return t;}
}} // namespace wwp_playback::world_snapshot
