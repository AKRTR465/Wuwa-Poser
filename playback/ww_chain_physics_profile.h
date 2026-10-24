#pragma once
#include "ww_chain_physics.h"
#include "ww_physics_profile.h"

namespace wwp_playback { namespace ww_chain {
inline Prepared Prepare(const ww_secondary::Prepared& angular,const std::vector<std::string>& names,
    const sub::TransformBytes* reference) {
    Prepared output;output.count=angular.bone_count;output.parents=angular.parents;
    for(uint32_t c=0;c<angular.chain_count;++c){const auto& chain=angular.chains[c];
        for(uint32_t i=0;i<output.count;++i)if(chain.descendant_mask[i]){auto& p=output.particles[i];p.enabled=true;p.pinned=i==chain.root;p.anchor=chain.anchor;
            p.damping=.1f;p.frequency=chain.settings.frequency;p.cone_radians=chain.settings.limit;p.radius_ue=.5f;}}
    auto index=[&](const char* name){auto found=std::find(names.begin(),names.end(),name);if(found==names.end())throw std::runtime_error("required body collision bone missing");return uint32_t(found-names.begin());};
    auto capsule=[&](const char* a,const char* b,float fraction){
        const auto ai=index(a),bi=index(b);const float radius=float(collision::Length(Position(reference[ai])-Position(reference[bi])))*fraction;
        output.capsules[output.capsule_count++]={ai,bi,(std::max)(1.f,radius)};};
    capsule("Bip001Pelvis","Bip001Spine2",.35f);capsule("Bip001Neck","Bip001Head",.55f);
    capsule("Bip001LThigh","Bip001LCalf",.17f);capsule("Bip001RThigh","Bip001RCalf",.17f);
    for(uint32_t i=0;i<output.count;++i)if(output.particles[i].enabled)
        for(uint32_t k=0;k<output.capsule_count;++k){const auto& c=output.capsules[k];
            if(collision::SegmentDistance(Position(reference[i]),Position(reference[c.a]),Position(reference[c.b]))<c.radius_ue+output.particles[i].radius_ue)
                output.particles[i].collision_exempt_mask|=uint16_t(1u<<k);}
    return output;
}
}} // namespace wwp_playback::ww_chain
