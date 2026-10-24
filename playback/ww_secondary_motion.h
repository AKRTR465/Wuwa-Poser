#pragma once
// Fixed exported-chain configuration used by the current particle solver.
#include "../engine_ue4/pose_subtree_rotation.h"

namespace wwp_playback { namespace ww_secondary {
namespace sub=wwp_ue::subtree_rotation;
constexpr uint32_t kMaxChains=128,kMaxBones=1024;
struct Chain {
    uint32_t root=UINT32_MAX,anchor=UINT32_MAX;
    std::array<uint8_t,kMaxBones> descendant_mask{};
    struct Settings {float frequency=0,limit=0;} settings;
};
struct Prepared {
    uint32_t bone_count=0,chain_count=0;
    std::array<int32_t,kMaxBones> parents{};
    std::array<Chain,kMaxChains> chains{};
};

}} // namespace wwp_playback::ww_secondary
