#pragma once
// Worker/offline target preparation and VMD-to-local command compilation.
// Reuses the established core_math Retargeter, source RigEvaluator/IK, T-pose
// calibration, body basis and per-finger/arm-twist handling. Never evaluate it
// on the game publication callback: it allocates vectors internally.
#include "../engine_ue4/pose_full_body.h"
#include "../core_math/include/poser_math/mmd_retarget.h"
#include "../core_math/third_party/nlohmann/json.hpp"
#include <fstream>
#include <memory>
#include <filesystem>

namespace wwp_playback { namespace ww_retarget {
namespace fb=wwp_ue::full_body;
struct PreparedTarget {
    mmd::RetargetProfile profile;
    fb::Topology topology;
    std::string skeleton_name;
    std::array<uint8_t,fb::kLimit> canonical_position_mask{};
    std::array<std::array<float,3>,fb::kLimit> canonical_local_positions{};
    std::array<uint8_t,fb::kLimit> canonical_rotation_mask{};
    std::array<std::array<float,4>,fb::kLimit> canonical_local_rotations{};
    uint32_t metacarpal_finger_chains=0;
};
inline int Role(const std::string& name) {
    static const std::pair<const char*,int> roles[]={
      {"Bip001Pelvis",0},{"Bip001LThigh",1},{"Bip001RThigh",2},{"Bip001LCalf",3},{"Bip001RCalf",4},
      {"Bip001LFoot",5},{"Bip001RFoot",6},{"Bip001Spine",7},{"Bip001Spine1",8},{"Bip001Spine2",54},
      {"Bip001Neck",9},{"Bip001Head",10},{"Bip001LClavicle",11},{"Bip001RClavicle",12},
      {"Bip001LUpperArm",13},{"Bip001RUpperArm",14},{"Bip001LForearm",15},{"Bip001RForearm",16},
      {"Bip001LHand",17},{"Bip001RHand",18},{"Bip001LToe0",19},{"Bip001RToe0",20}};
    for(const auto& role:roles)if(name==role.first)return role.second;
    for(int side=0;side<2;++side)for(int finger=0;finger<5;++finger)for(int joint=0;joint<3;++joint){
        std::string bone=std::string("Bip001")+(side?"R":"L")+"Finger"+std::to_string(finger);
        if(joint)bone+=std::to_string(joint);if(name==bone)return 24+side*15+finger*3+joint;
    }
    return -1;
}
// Some game hands have a palm/metacarpal link before the three phalanges.
// Identify that layout from both hierarchy and anatomy; a fourth tip marker
// on a normal three-joint finger must not shift the semantic roles.
inline uint32_t MapMetacarpalFingers(mmd::RetargetProfile& profile) {
    profile.globals();uint32_t changed=0;
    auto find=[&](const std::string& name){for(size_t i=0;i<profile.bones.size();++i)
        if(profile.bones[i].name==name)return int(i);return -1;};
    for(int side=0;side<2;++side){
        const int hand=profile.roles[17+side];if(hand<0)continue;
        for(int finger=1;finger<5;++finger){
            const auto stem=std::string("Bip001")+(side?"R":"L")+"Finger"+std::to_string(finger);
            const std::array<int,4> chain{find(stem),find(stem+"1"),find(stem+"2"),find(stem+"3")};
            if(std::any_of(chain.begin(),chain.end(),[](int i){return i<0;}))continue;
            bool serial=profile.bones[chain[0]].parent==hand;
            for(int i=1;i<4;++i)serial=serial&&profile.bones[chain[i]].parent==chain[i-1];
            if(!serial)continue;
            const auto wrist=profile.bones[hand].restPos;
            const float palmRoot=Len(profile.bones[chain[0]].restPos-wrist);
            const float knuckle=Len(profile.bones[chain[1]].restPos-wrist);
            const float palmLength=Len(profile.bones[chain[1]].restPos-profile.bones[chain[0]].restPos);
            const float phalanxLength=Len(profile.bones[chain[2]].restPos-profile.bones[chain[1]].restPos);
            if(knuckle<1e-5f||phalanxLength<1e-5f||palmRoot>=knuckle*.35f||palmLength<=phalanxLength*1.5f)continue;
            profile.bones[chain[0]].role=-1;
            const int first=24+side*15+finger*3;
            for(int joint=0;joint<3;++joint)profile.bones[chain[joint+1]].role=first+joint;
            ++changed;
        }
    }
    profile.globals();return changed;
}
inline PreparedTarget ReadTarget(const std::filesystem::path& path) {
    std::ifstream file(path,std::ios::binary);if(!file)throw std::runtime_error("cannot open skeleton export");
    nlohmann::json root;file>>root;const nlohmann::json* skeleton=&root;
    if(root.is_array())for(const auto& item:root)if(item.value("Type","")=="Skeleton"&&item.contains("ReferenceSkeleton"))skeleton=&item;
    const auto& ref=skeleton->at("ReferenceSkeleton");const auto& infos=ref.at("FinalRefBoneInfo");const auto& poses=ref.at("FinalRefBonePose");
    PreparedTarget target;target.skeleton_name=skeleton->value("Name","");
    std::vector<std::string> names;std::vector<int32_t> parents;
    for(size_t i=0;i<infos.size();++i){mmd::TargetBone bone;bone.name=infos[i].at("Name").get<std::string>();bone.parent=infos[i].at("ParentIndex");
        const auto& pose=poses.at(i);const auto& q=pose.at("Rotation");const auto& p=pose.at("Translation");const auto& s=pose.at("Scale3D");
        bone.localRot={q.at("X"),q.at("Y"),q.at("Z"),q.at("W")};bone.localPos={p.at("X"),p.at("Y"),p.at("Z")};bone.localScale={s.at("X"),s.at("Y"),s.at("Z")};
        bone.role=Role(bone.name);names.push_back(bone.name);parents.push_back(bone.parent);target.profile.bones.push_back(bone);}
    wwp_ue::subtree_rotation::Manifest proof;proof.expected_count=uint32_t(names.size());proof.expected_names=names;proof.expected_parents=parents;
    if(!fb::PrepareTopology(proof,names,parents,&target.topology))throw std::runtime_error("invalid complete hierarchy");
    // Target asset coordinates are UE source +Z-up; core_math calibration is
    // parameterized, so preserve that source basis and explicitly pass up Z.
    target.metacarpal_finger_chains=MapMetacarpalFingers(target.profile);
    if(!mmd::MakeCalibrationTPose(target.profile,{0,0,1}))throw std::runtime_error("required humanoid roles absent or T-pose calibration failed");
    for(uint32_t i=0;i<target.topology.count;++i){const auto& bone=target.profile.bones[i];
        target.canonical_local_positions[i]={{bone.localPos.x,bone.localPos.y,bone.localPos.z}};
        target.canonical_local_rotations[i]={{bone.localRot.x,bone.localRot.y,bone.localRot.z,bone.localRot.w}};
        // Explicit default policy: canonical all nonfacial local transforms,
        // including hair/skirt/ribbon/corrective/weapon nodes. Facial_* keeps
        // native expression-driven locals until face playback owns that layer.
        // Root0 is anchored by the runtime session, never reset to disk origin.
        const bool facial=bone.name.rfind("Facial_",0)==0;
        if(i!=0&&!facial){target.canonical_position_mask[i]=1;target.canonical_rotation_mask[i]=1;}}
    return target;
}
class Compiler {
    mmd::RigDefinition source_;
    const PreparedTarget* target_=nullptr;
    mmd::Retargeter retargeter_;
    float geometric_scale_=1;
public:
    void Bind(const mmd::MotionClip& clip,const PreparedTarget& target){
        source_=mmd::StandardRig();target_=&target;
        retargeter_.bind(source_,clip,target.profile);
        const int source_hip=source_.find(mmd::RoleNames()[0]),source_foot=source_.find(mmd::RoleNames()[5]);
        const int target_hip=target.profile.roles[0],target_foot=target.profile.roles[5];
        if(source_hip>=0&&source_foot>=0&&target_hip>=0&&target_foot>=0){
            const float source_length=Len(source_.bones[source_hip].rest-source_.bones[source_foot].rest);
            const float target_length=Len(target.profile.bones[target_hip].restPos-target.profile.bones[target_foot].restPos);
            if(source_length>1e-5f&&target_length>1e-5f)geometric_scale_=target_length/source_length;}
    }
    // The same rigid basis used for root translation and humanoid rotations.
    // Camera target/distance must reuse it before mesh ComponentToWorld.
    Quat SourceToTargetBasis()const{return retargeter_.sourceBasis();}
    // This ratio preserves target UE asset units. It is a geometry estimate, not a claim
    // that the author's source PMX/proportions have been calibrated.
    float GeometricTranslationScale()const{return geometric_scale_;}
    void Sample(double seconds,float scale,fb::LocalFrame* out){
        retargeter_.sample(seconds*30,scale);
        const auto& result=retargeter_.output;out->bone_count=target_->topology.count;
        for(uint32_t i=0;i<out->bone_count;++i){out->bones[i]=fb::BoneCommand{};if(!result.write[i])continue;
            const auto q=NormQ(result.localRot[i]);
            auto& b=out->bones[i];b.mask=fb::RotationOverride;b.rotation_xyzw[0]=q.x;b.rotation_xyzw[1]=q.y;b.rotation_xyzw[2]=q.z;b.rotation_xyzw[3]=q.w;}
        // Retargeted local rotations were computed through calibrated native
        // ancestors (notably Bip001 index1). Freeze those ancestor rotations
        // too, otherwise a live idle parent would be applied a second time.
        // Root0 remains the runtime session anchor supplied to FK.
        for(uint32_t i=0;i<out->bone_count;++i)if(out->bones[i].mask&fb::RotationOverride)
            for(int parent=target_->topology.parents[i];parent>0;parent=target_->topology.parents[uint32_t(parent)]){
                // Parents precede children. A previously visited override's
                // entire ancestor chain has already been closed by this loop.
                auto& command=out->bones[uint32_t(parent)];if(command.mask&fb::RotationOverride)break;
                const auto q=target_->profile.bones[uint32_t(parent)].localRot;
                command.mask|=fb::RotationOverride;command.rotation_xyzw[0]=q.x;command.rotation_xyzw[1]=q.y;command.rotation_xyzw[2]=q.z;command.rotation_xyzw[3]=q.w;}
        // Core output rootOffset is expressed in target model/component axes.
        // root0 has no parent, so its local delta uses that same source basis.
        const auto delta=result.rootOffset;auto& root=out->bones[0];root.mask|=fb::TranslationDelta;root.translation_delta[0]=delta.x;root.translation_delta[1]=delta.y;root.translation_delta[2]=delta.z;
    }
};
}} // namespace wwp_playback::ww_retarget
