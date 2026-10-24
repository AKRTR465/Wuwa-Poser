#pragma once
// VMD camera sampling/placement in explicit UE source/world coordinates.
// Math only: no engine camera writes, overrides, SDK calls or implicit offsets.
#include "../core_math/include/poser_math/mmd_camera.h"

namespace wwp_playback { namespace ww_camera {
struct Settings {
    Quat source_to_ue; // independently calibrated MMD model basis -> UE basis.
    Vec3 anchor; // explicit current/session world anchor in UE units.
    float source_unit_scale=1;
    bool input_fov_vertical=false; // Explicit source lens convention.
    float aspect_ratio=1; // Verified POV/viewport aspect, never screen dimensions.
};
struct Frame {
    Vec3 location_ue;
    Vec3 ue_rotator_degrees; // Pitch,Yaw,Roll; UE camera forward +X, up +Z.
    float fov_degrees=30;
    bool perspective=true;
};
inline bool Finite(Vec3 v){return std::isfinite(v.x)&&std::isfinite(v.y)&&std::isfinite(v.z);}
inline bool Finite(Quat q){return std::isfinite(q.x)&&std::isfinite(q.y)&&std::isfinite(q.z)&&std::isfinite(q.w)&&std::fabs(QuatLen(q)-1)<.001f;}
inline Vec3 Rotator(Quat optical){
    Vec3 forward=Norm(optical*Vec3{0,0,1}),up=Norm(optical*Vec3{0,1,0});
    const float yaw=std::atan2(forward.y,forward.x),pitch=std::atan2(forward.z,std::sqrt(forward.x*forward.x+forward.y*forward.y));
    // Standard UE FRotator basis: yaw about +Z, positive pitch raises +X.
    const Vec3 no_roll_right{-std::sin(yaw),std::cos(yaw),0};
    const Vec3 no_roll_up=Norm(Cross(forward,no_roll_right));
    const float roll=std::atan2(Dot(up,no_roll_right),Dot(up,no_roll_up));
    return {pitch*57.2957795131f,yaw*57.2957795131f,roll*57.2957795131f};
}
inline bool Place(const mmd::CameraKey& key,double,const Settings& settings,Frame* out){
    const float scale=settings.source_unit_scale;
    const Vec3 focus=settings.anchor+settings.source_to_ue*(key.target*scale);
    const Quat optical=NormQ(settings.source_to_ue*mmd::CameraOrbit(key.rotation));
    Frame result;result.location_ue=focus+optical*Vec3{0,0,key.distance*scale};result.perspective=key.perspective;
    const float fov=std::clamp(key.fov,1.f,179.f);
    result.fov_degrees=settings.input_fov_vertical?2*std::atan(std::tan(fov*.00872664625997f)*settings.aspect_ratio)*57.2957795131f:fov;
    result.ue_rotator_degrees=Rotator(optical);
    if(!Finite(result.location_ue)||!Finite(result.ue_rotator_degrees)||!std::isfinite(result.fov_degrees)||result.fov_degrees<1||result.fov_degrees>=180)return false;
    *out=result;return true;
}
}} // namespace wwp_playback::ww_camera
