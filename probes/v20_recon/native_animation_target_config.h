#pragma once
// Observer-side target parsing for the exported animation target.
#include "native_graph_discovery.h"
#include "../../core_math/third_party/nlohmann/json.hpp"
#include <stdexcept>
#include <string>

namespace native_animation_target_config {
using Json=nlohmann::json;
namespace ng=native_graph_discovery;
namespace nt=native_animation_target;
inline uint64_t Number(const Json& value){
    if(!value.is_string())return value.get<uint64_t>();
    return std::stoull(value.get<std::string>(),nullptr,16);
}
inline nt::OwnerPolicy Policy(const Json& cfg){
    if(!cfg.contains("animation_target"))return nt::OwnerPolicy::ControlledPlayer;
    const auto text=cfg.at("animation_target").at("owner_policy").get<std::string>();
    if(text=="scene_team_member")return nt::OwnerPolicy::SceneTeamMember;
    if(text=="owned_ui_actor")return nt::OwnerPolicy::OwnedUiActor;
    return nt::OwnerPolicy::ControlledPlayer;
}
inline nt::AuthorityTarget Parse(const Json& cfg,const ng::Token& actor,const ng::Token& mesh,
    const ng::Token& asset,const ng::Token& skeleton,const ng::Token& main){
    nt::AuthorityTarget result;result.policy=Policy(cfg);
    if(!cfg.contains("animation_target"))return result;
    const auto& target=cfg.at("animation_target");
    result.epoch=Number(target.at("session_epoch"));result.authority_address=Number(target.at("authority_address"));
    result.actor=actor.address;result.mesh=mesh.address;result.mesh_asset=asset.address;result.skeleton_asset=skeleton.address;result.main_anim=main.address;
    result.actor_index=ng::Index(actor);result.actor_serial=ng::Serial(actor);result.mesh_index=ng::Index(mesh);result.mesh_serial=ng::Serial(mesh);
    return result;
}
} // namespace native_animation_target_config
