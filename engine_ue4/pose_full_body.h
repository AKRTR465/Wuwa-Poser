#pragma once
// Fixed-capacity component -> local edits -> whole-tree FK planning.
// Outputs reuse subtree FixedPlan patches/restore bytes. No target access,
// allocation, hooks, engine calls, or runtime identity acquisition occurs here.
// TRS operation is q*(scale*v)*conj(q)+position, with parent*local composition.
// Current component scale stays byte-exact. Nonzero signed parent scale is
// required for inverse conversion. Callers either exclude unrepresented shear
// or explicitly select the engine's component FTransform TRS semantics, which
// compose Q/P and retain Scale3D rather than preserve an arbitrary affine shear.
// Same-header pose mutation and applying multiple patches are NOT atomic.
#include "pose_subtree_rotation.h"
#include <algorithm>

namespace wwp_ue { namespace full_body {
namespace sub=subtree_rotation;
constexpr uint32_t kLimit=1024;
enum Mask:uint32_t { RotationOverride=1,TranslationDelta=2 };
// Binary per-bone wire layout agreed with WWLOCAL1: pos3 delta, quat4, mask32.
struct BoneCommand {
    float translation_delta[3]{0,0,0};
    float rotation_xyzw[4]{0,0,0,1};
    uint32_t mask=0;
};
static_assert(sizeof(BoneCommand)==32,"WWLOCAL1 bone command ABI");
struct LocalFrame {
    uint64_t clip_epoch=0,sequence=0;
    double time_seconds=0;
    uint32_t bone_count=0;
    std::array<BoneCommand,kLimit> bones{};
};
struct Topology {
    uint32_t count=0;
    std::array<int32_t,kLimit> parents{};
};
struct Request {
    double quaternion_tolerance=.001;
    double minimum_scale=1e-8;
    // Optional worker-prepared canonical local positions freeze mapped body
    // translation/ancestors while leaving nonmapped secondary locals dynamic.
    const uint8_t* canonical_position_mask=nullptr;
    const std::array<float,3>* canonical_local_positions=nullptr;
    const uint8_t* canonical_rotation_mask=nullptr;
    const std::array<float,4>* canonical_local_rotations=nullptr;
    bool root_anchor_enabled=false;
    std::array<double,3> root_anchor_position{};
    bool root_rotation_enabled=false;
    std::array<double,4> root_anchor_rotation{{0,0,0,1}};
};
enum class Code {None,InvalidTopology,CountMismatch,InvalidFrame,NonFinite,
    NonUnitQuaternion,SingularScale,Overflow};
struct Failure {Code code=Code::None;uint32_t bone=UINT32_MAX;};
struct Scratch {
    std::array<sub::detail::Q,kLimit> current_rotation{},local_rotation{},next_rotation{};
    std::array<sub::detail::V,kLimit> current_position{},scale{},local_position{},next_position{};
    std::array<uint8_t,kLimit> affected{};
};
inline bool Fail(Failure* failure,Code code,uint32_t bone=UINT32_MAX){if(failure)*failure={code,bone};return false;}

// Worker prepares this once from the complete expected names/parents table.
inline bool PrepareTopology(const sub::Manifest& manifest,const std::vector<std::string>& names,
    const std::vector<int32_t>& parents,Topology* out,sub::Failure* failure=nullptr) {
    const auto fail=[&](sub::Code code,uint32_t bone=UINT32_MAX){if(failure)*failure={code,bone};return false;};
    if(!manifest.expected_count||manifest.expected_count>kLimit)return fail(sub::Code::InvalidManifest);
    if(names.size()!=manifest.expected_count||names!=manifest.expected_names||parents!=manifest.expected_parents)return fail(sub::Code::NameParentMismatch);
    Topology value;value.count=manifest.expected_count;
    for(uint32_t i=0;i<value.count;++i){if(i?parents[i]<0||uint32_t(parents[i])>=i:parents[i]!=-1)return fail(sub::Code::InvalidTree,i);value.parents[i]=parents[i];}
    *out=value;return true;
}


// Absolute local rotation overrides are immutable clip output, never an
// accumulating delta. Translation is an offset from THIS current local pose.
// Caller must remove its own last-written bytes before invoking this planner
// only when those exact bytes are still present, as in the proven subtree path.
inline bool MakePlan(const Topology& topology,const Request& request,
    const sub::TransformBytes* current,uint32_t count,const LocalFrame& frame,
    Scratch* scratch,sub::FixedPlan* out,Failure* failure=nullptr) noexcept {
    if(failure)*failure={};
    if(count!=topology.count||frame.bone_count!=count)return Fail(failure,Code::CountMismatch);
    auto& s=*scratch;
    for(uint32_t i=0;i<count;++i){
        auto q=sub::detail::Rotation(current[i]);const auto p=sub::detail::Position(current[i]);
        sub::detail::V scale{{sub::detail::Float(current[i].data()+32),sub::detail::Float(current[i].data()+36),sub::detail::Float(current[i].data()+40)}};
        for(double v:q)if(!std::isfinite(v))return Fail(failure,Code::NonFinite,i);
        for(double v:p)if(!std::isfinite(v))return Fail(failure,Code::NonFinite,i);
        for(double v:scale)if(!std::isfinite(v))return Fail(failure,Code::NonFinite,i);
        const double norm=sub::detail::Norm(q);if(!norm||std::fabs(norm-1)>request.quaternion_tolerance)return Fail(failure,Code::NonUnitQuaternion,i);
        s.current_rotation[i]=sub::detail::Unit(q);s.current_position[i]=p;s.scale[i]=scale;
    }
    for(uint32_t i=0;i<count;++i){
        const int32_t parent=topology.parents[i];const auto& command=frame.bones[i];
        if(parent<0){s.local_rotation[i]=s.current_rotation[i];s.local_position[i]=s.current_position[i];}
        else{
            const auto p=uint32_t(parent);
            s.local_rotation[i]=sub::detail::Unit(sub::detail::Mul(sub::detail::Conj(s.current_rotation[p]),s.current_rotation[i]));
            const auto offset=sub::detail::Rotate(sub::detail::Conj(s.current_rotation[p]),{{s.current_position[i][0]-s.current_position[p][0],
                s.current_position[i][1]-s.current_position[p][1],s.current_position[i][2]-s.current_position[p][2]}});
            for(unsigned k=0;k<3;++k){if(std::fabs(s.scale[p][k])<request.minimum_scale)return Fail(failure,Code::SingularScale,p);
                s.local_position[i][k]=offset[k]/s.scale[p][k];}
        }
        if(request.canonical_position_mask&&request.canonical_position_mask[i])
            for(unsigned k=0;k<3;++k)s.local_position[i][k]=request.canonical_local_positions[i][k];
        if(request.canonical_rotation_mask&&request.canonical_rotation_mask[i]){
            for(unsigned k=0;k<4;++k)s.local_rotation[i][k]=request.canonical_local_rotations[i][k];s.local_rotation[i]=sub::detail::Unit(s.local_rotation[i]);}
        if(i==0&&request.root_anchor_enabled)s.local_position[i]=request.root_anchor_position;
        if(i==0&&request.root_rotation_enabled)s.local_rotation[i]=sub::detail::Unit(request.root_anchor_rotation);
        if(command.mask&RotationOverride){for(unsigned k=0;k<4;++k)s.local_rotation[i][k]=command.rotation_xyzw[k];s.local_rotation[i]=sub::detail::Unit(s.local_rotation[i]);}
        if(command.mask&TranslationDelta)for(unsigned k=0;k<3;++k)s.local_position[i][k]+=command.translation_delta[k];
        const bool translated=(command.mask&TranslationDelta)&&
            (command.translation_delta[0]!=0||command.translation_delta[1]!=0||command.translation_delta[2]!=0);
        s.affected[i]=uint8_t((command.mask&RotationOverride)||translated||(request.canonical_position_mask&&request.canonical_position_mask[i])||
            (request.canonical_rotation_mask&&request.canonical_rotation_mask[i])||
            (i==0&&(request.root_anchor_enabled||request.root_rotation_enabled))||(parent>=0&&s.affected[uint32_t(parent)]));
        if(!s.affected[i]){s.next_rotation[i]=s.current_rotation[i];s.next_position[i]=s.current_position[i];continue;}
        if(parent<0){s.next_rotation[i]=s.local_rotation[i];s.next_position[i]=s.local_position[i];}
        else{
            const auto p=uint32_t(parent);s.next_rotation[i]=sub::detail::Unit(sub::detail::Mul(s.next_rotation[p],s.local_rotation[i]));
            const auto offset=sub::detail::Rotate(s.next_rotation[p],{{s.scale[p][0]*s.local_position[i][0],s.scale[p][1]*s.local_position[i][1],s.scale[p][2]*s.local_position[i][2]}});
            for(unsigned k=0;k<3;++k)s.next_position[i][k]=s.next_position[p][k]+offset[k];
        }
        for(double value:s.next_position[i])if(!std::isfinite(value)||std::fabs(value)>(std::numeric_limits<float>::max)())return Fail(failure,Code::Overflow,i);
    }
    // Validation/preflight completed; output is now safe to overwrite. Each
    // original field is the exact incoming bytes, retaining scale/padding.
    out->count=0;
    for(uint32_t i=0;i<count;++i)if(s.affected[i]){
        sub::Patch patch;patch.bone=i;std::memcpy(patch.original_rotation.data(),current[i].data(),16);
        std::memcpy(patch.original_position.data(),current[i].data()+16,12);
        for(unsigned k=0;k<4;++k)sub::detail::Store(patch.rotated_rotation.data()+k*4,s.next_rotation[i][k]);
        for(unsigned k=0;k<3;++k)sub::detail::Store(patch.rotated_position.data()+k*4,s.next_position[i][k]);
        patch.position_changed=patch.rotated_position!=patch.original_position;
        if(patch.rotated_rotation!=patch.original_rotation||patch.position_changed)out->patches[out->count++]=patch;
    }
    return true;
}

}} // namespace wwp_ue::full_body
