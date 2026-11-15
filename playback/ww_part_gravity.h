#pragma once
#include "ww_chain_physics.h"
#include <memory>
#include <stdexcept>
#include <string>
#include <vector>

namespace wwp_playback::part_gravity {
namespace chain=ww_chain;

struct Part {
    uint32_t id=0;
    std::string root;
    float strength=-1;
    uint64_t sequence=0,sampled=0;
    unsigned moved_bones=0;
    bool downward_drape=false;
    chain::Prepared rig;
    std::unique_ptr<chain::Runtime> motion;
};

template<class BoundChains>
bool CoversGroup(const std::vector<Part>& parts,const BoundChains& chains,size_t root_count){
    return root_count&&chains.size()==root_count&&std::all_of(chains.begin(),chains.end(),[&](const auto& chain){
        return std::any_of(parts.begin(),parts.end(),[&](const auto& part){
            if(part.strength<0||chain.root_bone>=part.rig.count)return false;
            const auto& point=part.rig.particles[chain.root_bone];
            return point.enabled&&!point.pinned;
        });
    });
}

template<class Json>
void Prepare(const Json& definitions,const std::vector<std::string>& names,const int32_t* parents,
             std::vector<Part>& output) {
    const auto require=[](bool condition){if(!condition)throw std::invalid_argument("invalid part gravity rig");};
    require(definitions.is_array()&&definitions.size()<=names.size()&&!names.empty()&&names.size()<=chain::kLimit);
    output.reserve(definitions.size());
    const auto bone=[&](const std::string& name){
        const auto found=std::find(names.begin(),names.end(),name);
        require(found!=names.end());return uint32_t(found-names.begin());};
    std::array<bool,chain::kLimit> occupied{};
    for(const auto& def:definitions){
        const uint32_t id=def.at("id");
        const auto root_name=def.at("root").template get<std::string>();
        const auto root=bone(root_name),anchor=bone(def.at("anchor"));
        require(id&&root&&parents[root]==int32_t(anchor)&&
            std::none_of(output.begin(),output.end(),[&](const Part& part){return part.id==id;}));
        output.emplace_back();
        auto& part=output.back();part.id=id;part.root=root_name;part.downward_drape=def.value("downward_drape",false);
        auto& rig=part.rig;rig.count=uint32_t(names.size());
        std::copy_n(parents,names.size(),rig.parents.begin());
        const auto branch=[&](uint32_t start,float frequency,float root_angle){
            require(start&&rig.parents[start]>=0&&!occupied[start]);
            const auto pin=uint32_t(rig.parents[start]);
            auto& pinned=rig.particles[pin];
            pinned.enabled=pinned.pinned=pinned.preserve_pinned_rotation=true;pinned.anchor=pin;
            std::array<bool,chain::kLimit> members{};
            members[start]=true;
            for(uint32_t i=start;i<rig.count;++i){
                if(i!=start&&rig.parents[i]>=0)members[i]=members[uint32_t(rig.parents[i])];
                if(!members[i])continue;
                require(!occupied[i]);occupied[i]=true;
                auto& point=rig.particles[i];
                point.enabled=true;point.anchor=pin;point.frequency=frequency;point.damping=.12f;point.radius_ue=1.6f;
                point.direction_reference=chain::DirectionReference::ParentFrame;point.cone_radians=.09f;
            }
            auto& first=rig.particles[start];
            first.direction_reference=chain::DirectionReference::Authored;first.cone_radians=root_angle;
        };
        const bool inferred=def.value("from_chain",false);
        branch(root,inferred?1.2f:.45f,inferred?.95f:3.095f);
        rig.particles[root].drape_spine=part.downward_drape;
        if(def.contains("transition_roots"))
            for(const auto& transition:def.at("transition_roots"))branch(bone(transition),1.2f,.42f);
        auto next_parent=root;
        for(const auto& entry:def.at("continuation")){
            const auto next=bone(entry);
            require(rig.particles[next].enabled&&rig.parents[next]==int32_t(next_parent));
            rig.particles[next].direction_reference=chain::DirectionReference::ParentTangent;
            rig.particles[next].drape_spine=part.downward_drape;
            rig.particles[next].cone_radians=.70f;next_parent=next;
        }
        for(const auto& capsule:def.at("capsules")){
            require(rig.capsule_count<chain::kCapsules);
            const auto a=bone(capsule.at("start")),b=bone(capsule.at("end"));
            const float radius=capsule.at("radius_cm");
            require(a!=b&&!rig.particles[a].enabled&&!rig.particles[b].enabled&&std::isfinite(radius)&&radius>0&&radius<=50);
            rig.capsules[rig.capsule_count++]={a,b,radius};
        }
    }
    for(const auto& part:output)
        for(uint32_t i=0;i<part.rig.count;++i)
            if(part.rig.particles[i].pinned)require(!occupied[i]);
}
} // namespace wwp_playback::part_gravity
