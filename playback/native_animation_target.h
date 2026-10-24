#pragma once
// Shared permission publication for animation targets. The creator/SceneTeam
// adapter owns the publication and must close it before losing actor ownership.
#include <cstddef>
#include <cstdint>
#include <cstring>

namespace native_animation_target {
enum class OwnerPolicy:uint32_t {ControlledPlayer=1,SceneTeamMember=2,OwnedUiActor=3};
inline const char* PolicyName(OwnerPolicy policy){switch(policy){
    case OwnerPolicy::ControlledPlayer:return "controlled_player";
    case OwnerPolicy::SceneTeamMember:return "scene_team_member";
    case OwnerPolicy::OwnedUiActor:return "owned_ui_actor";
    default:return "invalid";}}
struct alignas(8) AuthoritySnapshot {
    uint64_t sequence=0,epoch=0;
    uint32_t policy=0,valid=0;
    uint64_t actor=0,mesh=0,mesh_asset=0,skeleton_asset=0,main_anim=0;
    uint32_t actor_index=0,actor_serial=0,mesh_index=0,mesh_serial=0;
};
static_assert(sizeof(AuthoritySnapshot)==80&&offsetof(AuthoritySnapshot,actor)==24&&
    offsetof(AuthoritySnapshot,actor_index)==64,"animation authority fixed layout");
struct AuthorityTarget {
    OwnerPolicy policy=OwnerPolicy::ControlledPlayer;
    uint64_t epoch=0,authority_address=0,actor=0,mesh=0,mesh_asset=0,skeleton_asset=0,main_anim=0;
    uint32_t actor_index=0,actor_serial=0,mesh_index=0,mesh_serial=0;
};
template<class Read>bool AuthorityAlive(Read&& read,const AuthorityTarget& target){
    if(target.policy==OwnerPolicy::ControlledPlayer)return true;
    AuthoritySnapshot snapshot{};uint64_t before=0,after=0;
    if(!target.authority_address||!read(target.authority_address,&before,sizeof(before))||!before||(before&1)||
       !read(target.authority_address,&snapshot,sizeof(snapshot))||snapshot.sequence!=before||
       !read(target.authority_address,&after,sizeof(after))||after!=before)return false;
    return snapshot.valid==1&&snapshot.epoch==target.epoch&&snapshot.actor==target.actor&&snapshot.mesh==target.mesh&&snapshot.main_anim==target.main_anim;
}
} // namespace native_animation_target
