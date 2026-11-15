#pragma once

#include "../engine_ue4/pose_full_body.h"
#include <vector>

namespace wwp_playback::sleeve_follow {
namespace bone=wwp_ue::subtree_rotation::detail;
using Q=bone::Q;
using V=bone::V;
struct Chain {
    unsigned group=0,root_slot=0,root=0,shoulder=0,shoulder_parent=0;
    Q neutral_shoulder{};
    std::vector<unsigned> bones;
    float follow=1;
    float sampled_follow=1;
    unsigned applied_frames=0;
    double correction_degrees=0,peak_correction_degrees=0;
};

inline Q PartOfRotation(Q rotation,double fraction) {
    if(rotation[3]<0)for(auto& component:rotation)component=-component;
    const double angle=std::acos(std::clamp(rotation[3],-1.,1.));
    if(angle<1.e-8)return {{0,0,0,1}};
    const double factor=std::sin(angle*fraction)/std::sin(angle);
    return {{rotation[0]*factor,rotation[1]*factor,rotation[2]*factor,std::cos(angle*fraction)}};
}

inline bool Apply(Chain& chain,wwp_ue::subtree_rotation::TransformBytes* pose) {
    chain.correction_degrees=0;
    if(chain.follow==1)return true;
    const auto parent=bone::Rotation(pose[chain.shoulder_parent]);
    const auto shoulder=bone::Rotation(pose[chain.shoulder]);
    const auto local=bone::Mul(bone::Conj(parent),shoulder);
    const auto local_undo=bone::Unit(bone::Mul(chain.neutral_shoulder,bone::Conj(local)));
    const auto partial=PartOfRotation(local_undo,1-chain.follow);
    chain.correction_degrees=std::acos(std::clamp(partial[3],-1.,1.))*114.59155902616465;
    const auto component_undo=bone::Mul(bone::Mul(parent,partial),bone::Conj(parent));
    const auto origin=bone::Position(pose[chain.root]);
    for(auto index:chain.bones){
        const auto rotation=bone::Unit(bone::Mul(component_undo,bone::Rotation(pose[index])));
        for(unsigned axis=0;axis<rotation.size();++axis)
            if(!bone::Store(pose[index].data()+ww_game_compat::TransformRotationOffset+axis*sizeof(float),rotation[axis]))return false;
        if(index==chain.root)continue; // The cuff stays on the authored attachment point.
        const auto old=bone::Position(pose[index]);
        const auto relative=bone::Rotate(component_undo,V{{old[0]-origin[0],old[1]-origin[1],old[2]-origin[2]}});
        for(unsigned axis=0;axis<relative.size();++axis)
            if(!bone::Store(pose[index].data()+ww_game_compat::TransformTranslationOffset+axis*sizeof(float),origin[axis]+relative[axis]))return false;
    }
    return true;
}
}
