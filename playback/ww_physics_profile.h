#pragma once
// Worker-only Kawaii asset chain binding. Parameters are conservative optional
// spring approximations, not claims to reproduce Kuro collision constraints.
#include "ww_secondary_motion.h"
#include "../core_math/third_party/nlohmann/json.hpp"
#include <fstream>
#include <filesystem>
#include <map>

namespace wwp_playback { namespace ww_secondary {
struct Unavailable:std::runtime_error {using std::runtime_error::runtime_error;};
inline Prepared ReadCharacterProfile(const std::filesystem::path& chains_path,const std::vector<std::string>& names,
    const std::vector<int32_t>& parents,const std::string& model_id) {
    std::ifstream file(chains_path,std::ios::binary);nlohmann::json root;file>>root;
    const nlohmann::json* character=nullptr;for(const auto& c:root.at("characters"))if(c.value("model_id","")==model_id)character=&c;
    if(!character)throw Unavailable("requested character chain profile missing");
    std::map<std::string,uint32_t> indices;for(uint32_t i=0;i<names.size();++i)indices.emplace(names[i],i);
    Prepared output;output.bone_count=uint32_t(names.size());for(uint32_t i=0;i<parents.size();++i)output.parents[i]=parents[i];
    std::map<uint32_t,Chain::Settings> settings;
    for(const auto& group:character->at("kawaii_groups")){
        if(group.value("variant",std::string())=="skeleton_heuristic")continue;
        for(const auto& node:group.at("groups"))for(const auto& chain:node.at("chains")){
            const auto it=indices.find(chain.at("root").get<std::string>());if(it==indices.end()||!it->second)continue;
            const auto& physics=node.at("physics");
            Chain::Settings config;config.frequency=1.5f+3*physics.at("Stiffness").get<float>();config.limit=physics.at("LimitAngle").get<float>()*(3.14159265358979f/180);
            settings.emplace(it->second,config);
        }
    }
    for(const auto& pair:settings){bool nested=false;for(int32_t p=parents[pair.first];p>=0;p=parents[uint32_t(p)])if(settings.count(uint32_t(p)))nested=true;
        if(nested||output.chain_count>=kMaxChains)continue;
        auto& chain=output.chains[output.chain_count++];chain.root=pair.first;chain.anchor=uint32_t(parents[pair.first]);chain.settings=pair.second;
        for(uint32_t i=0;i<output.bone_count;++i)chain.descendant_mask[i]=uint8_t(i==chain.root||(parents[i]>=0&&chain.descendant_mask[uint32_t(parents[i])]));}
    if(!output.chain_count)throw Unavailable("no observed physics chains are available");
    return output;
}
}} // namespace wwp_playback::ww_secondary
