#pragma once
// Offline semantic expression bindings. These are named target-morph proposals,
// not fitted facial-bone deltas or permission to use an unverified runtime offset.
#include "vmd_clip.h"
#include <set>

namespace wwp_playback { namespace face {
struct Target {std::string morph;float gain=1;};
struct Binding {std::string source;std::vector<Target> targets;std::string status,reason;};
struct Profile {std::string model;std::vector<std::string> target_morphs;std::vector<Binding> bindings;};
inline std::vector<Target> SemanticTargets(const std::string& source) {
    if(source==u8"あ"||source==u8"あ1")return {{"A",1}};
    if(source==u8"い")return {{"I",1}};if(source==u8"う")return {{"U",1}};
    if(source==u8"え")return {{"E",1}};if(source==u8"お")return {{"O",1}};
    if(source==u8"まばたき")return {{"E_Close",1}};
    if(source==u8"笑い")return {{"E_Smile_L",1},{"E_Smile_R",1}};
    if(source==u8"にこり")return {{"M_Smile_L",1},{"M_Smile_R",1}};
    if(source==u8"ワ")return {{"M_Laugh",1}};
    if(source==u8"口小")return {{"M_OpenSmall",1}};
    if(source==u8"口角上げ")return {{"M_Smile_L",1},{"M_Smile_R",1}};
    if(source==u8"怒り"||source==u8"怒")return {{"B_Anger",1}};
    if(source==u8"困る")return {{"B_Sad",1}};
    if(source==u8"びっくり")return {{"E_Stare",1}};
    if(source==u8"じと目")return {{"E_Insipid",1}};
    if(source==u8"なごみ")return {{"E_Sad",.5f}};
    // Unilateral wink/tears/material effects/pupil scales need matching shapes
    // or a calibrated rig. Do not approximate them by closing both eyes.
    return {};
}
inline Profile Build(const mmd::MotionClip& motion,const std::vector<std::string>& targets,const std::string& model) {
    Profile result;result.model=model;result.target_morphs=targets;std::set<std::string> valid(targets.begin(),targets.end());
    for(const auto& track:motion.morphs){Binding binding;binding.source=track.first;
        if(valid.count(track.first)){binding.targets={{track.first,1}};binding.status="exact_name";binding.reason="Exact asset morph name; runtime morph index remains unverified";}
        else {binding.targets=SemanticTargets(track.first);for(const auto& t:binding.targets)if(!valid.count(t.morph)){binding.targets.clear();break;}
            binding.status=binding.targets.empty()?"unmapped":"semantic_proposal";binding.reason=binding.targets.empty()?"No calibrated matching target shape":
                "Named shape semantic correspondence; authored appearance/gains require visual calibration";}
        result.bindings.push_back(std::move(binding));}return result;
}
struct ExpressionFrame {double seconds=0;std::vector<float> weights;std::array<::Quat,3> source_eye_rotations{};std::array<uint8_t,3> source_eye_present{};};
inline ExpressionFrame Sample(const mmd::MotionClip& motion,const Profile& profile,double seconds) {
    if(!std::isfinite(seconds))throw std::runtime_error("Expression sample time must be finite");ExpressionFrame frame;frame.seconds=(std::max)(0.,seconds);frame.weights.assign(profile.target_morphs.size(),0);
    for(const auto& binding:profile.bindings){auto source=motion.morphs.find(binding.source);if(source==motion.morphs.end())continue;
        const float weight=mmd::SampleMorph(source->second,frame.seconds*30);for(const auto& target:binding.targets){auto at=std::find(profile.target_morphs.begin(),profile.target_morphs.end(),target.morph);
            if(at==profile.target_morphs.end())continue;auto index=size_t(at-profile.target_morphs.begin());frame.weights[index]=(std::max)(frame.weights[index],mmd::Clamp(weight*target.gain,0,1));}}
    const char* eyes[]={u8"両目",u8"左目",u8"右目"};for(unsigned i=0;i<3;++i){auto track=motion.bones.find(eyes[i]);if(track!=motion.bones.end()){
        frame.source_eye_present[i]=1;frame.source_eye_rotations[i]=mmd::SampleBone(track->second,frame.seconds*30).rotation;}}
    return frame;
}
}} // wwp_playback::face
