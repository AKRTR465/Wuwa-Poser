#pragma once
#include "../runtime/generated/GameCompat.generated.h"
#include <array>
#include <cmath>
#include <cstddef>
#include <cstdint>
#include <cstring>

namespace wwp_playback::model_preview {
constexpr unsigned kMaxBones=1024;
using Bone=std::array<float,10>; // component-space XYZ (cm), XYZW quaternion, XYZ scale
struct Frame {
    char magic[8]={'W','W','P','P','O','S','1','\0'};
    uint32_t slot=0,count=0;
    uint64_t revision=0,frame=0;
    std::array<Bone,kMaxBones> bones;
};
static_assert(sizeof(Bone)==40&&offsetof(Frame,bones)==32,"fixed preview wire layout");

inline bool ReadBone(const uint8_t* source,Bone& bone){
    std::memcpy(bone.data(),source+ww_game_compat::TransformTranslationOffset,12);
    std::memcpy(bone.data()+3,source+ww_game_compat::TransformRotationOffset,16);
    std::memcpy(bone.data()+7,source+ww_game_compat::TransformScaleOffset,12);
    for(float component:bone)if(!std::isfinite(component))return false;
    float length=0;for(unsigned i=3;i<7;++i)length+=bone[i]*bone[i];
    return std::fabs(length-1)<.01f;
}
}
