#pragma once
// MMD source eye rotations -> named pupil morphs, without target-bone offsets.
#include "ww_face_profile.h"

namespace wwp_playback { namespace gaze {
inline constexpr const char* Eyes[]={u8"両目",u8"左目",u8"右目"};
inline unsigned TrackCount(const mmd::MotionClip& motion){
    unsigned count=0;for(const auto* name:Eyes){auto at=motion.bones.find(name);if(at!=motion.bones.end()&&!at->second.empty())++count;}return count;
}
inline mmd::MotionClip Extract(const mmd::MotionClip& motion){
    mmd::MotionClip result;for(const auto* name:Eyes){auto at=motion.bones.find(name);if(at!=motion.bones.end())result.bones.emplace(name,at->second);}return result;
}
inline double Duration(const mmd::MotionClip& motion){
    uint32_t last=0;for(const auto* name:Eyes){auto at=motion.bones.find(name);if(at!=motion.bones.end()&&!at->second.empty())last=(std::max)(last,at->second.back().frame);}return last/30.;
}
struct Channel {std::string morph;unsigned eye=0;bool horizontal=false;float sign=1;}; // eye: 0 average, 1 left, 2 right
struct Profile {std::vector<Channel> channels;std::vector<std::string> controlled;bool independent=false;};
inline Profile Build(const std::vector<std::string>& targets){
    Profile result;const std::set<std::string> valid(targets.begin(),targets.end());
    auto has=[&](std::initializer_list<const char*> names){for(auto name:names)if(!valid.count(name))return false;return true;};
    result.independent=has({"Pupil_Up_L","Pupil_Down_L","Pupil_Up_R","Pupil_Down_R","Pupil_In_L","Pupil_Out_L","Pupil_In_R","Pupil_Out_R"});
    if(result.independent)result.channels={{"Pupil_Up_L",1,false,1},{"Pupil_Down_L",1,false,-1},{"Pupil_Up_R",2,false,1},{"Pupil_Down_R",2,false,-1},
        {"Pupil_In_L",1,true,1},{"Pupil_Out_L",1,true,-1},{"Pupil_Out_R",2,true,1},{"Pupil_In_R",2,true,-1}};
    else if(has({"Pupil_Up","Pupil_Down","Pupil_L","Pupil_R"}))result.channels={{"Pupil_Up",0,false,1},{"Pupil_Down",0,false,-1},{"Pupil_R",0,true,1},{"Pupil_L",0,true,-1}};
    // Clear any competing authored pupil-direction channels when gaze owns them.
    if(!result.channels.empty())for(auto name:{"Pupil_Up","Pupil_Down","Pupil_L","Pupil_R","Pupil_Up_L","Pupil_Down_L","Pupil_Up_R","Pupil_Down_R","Pupil_In_L","Pupil_Out_L","Pupil_In_R","Pupil_Out_R"})
        if(valid.count(name))result.controlled.emplace_back(name);
    return result;
}
inline Quat Rotation(const mmd::MotionClip& motion,const char* name,double frame){
    auto at=motion.bones.find(name);return at==motion.bones.end()||at->second.empty()?Quat{}:mmd::SampleBone(at->second,frame).rotation;
}
inline void Apply(const mmd::MotionClip& motion,const Profile& profile,const std::vector<std::string>& targets,double seconds,float strength,std::vector<float>& weights){
    if(profile.channels.empty())return;
    const double frame=(std::max)(0.,seconds)*30.;const auto both=Rotation(motion,Eyes[0],frame);
    std::array<Vec3,3> directions{};
    directions[1]=NormQ(both*Rotation(motion,Eyes[1],frame))*Vec3{0,0,-1};
    directions[2]=NormQ(both*Rotation(motion,Eyes[2],frame))*Vec3{0,0,-1};
    directions[0]=Norm(directions[1]+directions[2]);
    auto put=[&](const std::string& name,float value){auto at=std::find(targets.begin(),targets.end(),name);if(at!=targets.end())weights[size_t(at-targets.begin())]=value;};
    for(const auto& name:profile.controlled)put(name,0);
    // Standard MMD rest gaze is -Z, +Y is up, +X is the character's left.
    // 15 source degrees drive a full pupil morph. Strength permits visual fitting.
    constexpr float range=0.2617993878f;
    for(const auto& channel:profile.channels){const auto v=directions[channel.eye];
        const float angle=channel.horizontal?std::atan2(-v.x,-v.z):std::atan2(v.y,std::sqrt(v.x*v.x+v.z*v.z));
        put(channel.morph,mmd::Clamp(angle/range*channel.sign*strength,0,1));}
}
}} // namespace wwp_playback::gaze
