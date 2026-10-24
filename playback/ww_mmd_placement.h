#pragma once
// Component-space Z-up placement of an immutable sampled root. This layer has
// no actor/capsule access, ground queries, allocation, or persistent pose state.
#include "../engine_ue4/pose_full_body.h"

namespace wwp_playback { namespace placement {
struct Settings {
    double x=0,y=0,height=0,yaw_degrees=0;
    bool in_place=false;
};
// Position = captured anchor + component-axis manual offset + yaw(clip delta).
// MakePlan lets a sampled root RotationOverride replace the anchor rotation;
// apply yaw to that winning rotation exactly once, preserving this precedence.
// Outputs may alias inputs.
inline bool Apply(const Settings& settings,const wwp_ue::full_body::Request& anchor,
    const wwp_ue::full_body::BoneCommand& sampled_root,
    wwp_ue::full_body::Request* placed,wwp_ue::full_body::BoneCommand* root) noexcept {
    namespace body=wwp_ue::full_body;
    namespace math=wwp_ue::subtree_rotation::detail;
    auto request=anchor;auto command=sampled_root;
    // Keep the existing default path byte-for-byte, including clip masks and
    // quaternion normalization performed later by the established planner.
    if(settings.x==0&&settings.y==0&&settings.height==0&&settings.yaw_degrees==0&&!settings.in_place){
        *placed=request;*root=command;return true;
    }
    const double manual[]={settings.x,settings.y,settings.height};
    for(unsigned k=0;k<3;++k)request.root_anchor_position[k]+=manual[k];
    const double radians=settings.yaw_degrees*(3.14159265358979323846/180.0);
    if(command.mask&body::TranslationDelta){
        const double x=command.translation_delta[0],y=command.translation_delta[1];
        if(settings.in_place){command.translation_delta[0]=0;command.translation_delta[1]=0;}
        else if(settings.yaw_degrees!=0){const double c=std::cos(radians),s=std::sin(radians);
            const double delta[]={c*x-s*y,s*x+c*y};
            for(unsigned k=0;k<2;++k)command.translation_delta[k]=float(delta[k]);}
    }
    if(settings.yaw_degrees!=0){
        auto basis=anchor.root_anchor_rotation;
        if(command.mask&body::RotationOverride)for(unsigned k=0;k<4;++k)basis[k]=sampled_root.rotation_xyzw[k];
        const math::Q yaw{{0,0,std::sin(radians*.5),std::cos(radians*.5)}};
        const auto rotation=math::Unit(math::Mul(yaw,basis));
        if(command.mask&body::RotationOverride)for(unsigned k=0;k<4;++k)command.rotation_xyzw[k]=float(rotation[k]);
        else request.root_anchor_rotation=rotation;
    }
    *placed=request;*root=command;return true;
}
}} // namespace wwp_playback::placement
