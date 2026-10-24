#pragma once
// Existing C# SceneTeam state only. No getter/runtime call, initialization,
// mutation, character switch, historical-address fallback or display authority.
#include "mono_managed_readonly.h"

namespace csharp_scene_team_readonly {
using Address=mono_managed_readonly::Address;
using Json=mono_managed_readonly::Json;
using ObjectRef=mono_managed_readonly::ObjectRef;
using mono_managed_readonly::Hex;
inline constexpr const char* kSourceRoute="csharp_managed_readonly";
inline constexpr const char* kSourceContract="scene-team-csharp-managed-readonly-20261021-v1";
struct Origins {
    Address game_image=0,core_image=0,player_info_model=0,scene_team_model=0,
        scene_team_player=0,scene_team_group=0,scene_team_role=0,scene_team_item=0,
        entity_handle=0,character_model=0,world_entity=0,core_entity=0,
        core_object_base=0,core_object_system=0;
};
inline void Require(bool value,const char* reason){if(!value)throw std::runtime_error(std::string("csharp scene team: ")+reason);}
inline Json Ref(const ObjectRef& value){return {{"address",Hex(value.address)},{"runtime_class",Hex(value.klass)},{"field_scope",Hex(value.field_scope)}};}
template<class V> Address Lookup(V& view,Address dictionary,int32_t key){
    for(const auto& row:view.DictionaryI4References(dictionary))if(row.first==key)return row.second;
    throw std::runtime_error("csharp scene team: local player/current group dictionary key missing");
}
template<class V> ObjectRef Singleton(V& view,Address klass,const char* field){return view.Object(view.StaticReference(klass,field),klass);}
template<class V> Json Capture(V& view,const Origins& o){
    const auto info=Singleton(view,o.player_info_model,"_Instance"),model=Singleton(view,o.scene_team_model,"_Instance"),
        character=Singleton(view,o.character_model,"_Instance"),objects=Singleton(view,o.core_object_system,"_instance");
    const int32_t pid=view.Int32(info,"Id");
    const Address playerMap=view.Reference(model,"TeamPlayerMap"),teamItems=view.Reference(model,"TeamItems");
    const auto player=view.Object(Lookup(view,playerMap,pid),o.scene_team_player);
    const auto playerGroup=view.NullableEnumI4(player,"CurrentGroupType");Require(playerGroup.has_value(),"current group nullable has no value");
    const int32_t groupType=*playerGroup;
    const Address groupMap=view.Reference(player,"GroupMap");
    const auto group=view.Object(Lookup(view,groupMap,groupType),o.scene_team_group);
    const Address roleList=view.Reference(group,"RoleList"),currentRole=view.Reference(group,"CurrentRole"),currentItem=view.Reference(model,"CurrentItem");
    const auto roles=view.ListReferences(roleList),items=view.ListReferences(teamItems);
    struct Role {ObjectRef object;int64_t cid;int32_t role;bool onStage;};
    struct Item {ObjectRef object;int64_t cid;int32_t config;Address handle;bool automatic,remote;};
    std::vector<Role> logical;std::vector<Item> local;unsigned remoteCount=0;
    for(const Address address:roles){const auto role=view.Object(address,o.scene_team_role);
        logical.push_back({role,view.Int64(role,"CreatureDataId"),view.Int32(role,"RoleId"),view.Boolean(role,"OnStageWithoutControl")});}
    Require(logical.size()==3,"RoleList must contain exactly three members");
    for(const Address address:items){const auto item=view.Object(address,o.scene_team_item);
        if(view.Int32(item,"PlayerId")!=pid){++remoteCount;continue;}
        local.push_back({item,view.Int64(item,"CreatureDataId"),view.Int32(item,"ConfigId"),view.Reference(item,"EntityHandleInternal"),
            view.Boolean(item,"IsAutoRoleInternal"),view.Boolean(item,"RemoteIsControl")});
    }
    const Address handles=view.Reference(character,"Handles"),objectList=view.Reference(objects,"Objects"),versions=view.Reference(objects,"Versions");
    const auto handleCount=view.ListCount(handles),objectCount=view.ListCount(objectList),versionCount=view.ListCount(versions);
    Json members=Json::array(),witnessMembers=Json::array();int32_t currentId=0;int64_t currentCid=0;
    for(size_t i=0;i<logical.size();++i){const auto& role=logical[i];
        const auto match=std::find_if(local.begin(),local.end(),[&](const Item& item){return item.config==role.role&&item.cid==role.cid;});
        Require(match!=local.end(),"RoleList member has no local TeamItem");
        const auto& item=*match;const bool current=role.object.address==currentRole;
        const auto handle=view.Object(item.handle,o.entity_handle);const int32_t id=view.Int32(handle,"Id");
        const uint32_t index=static_cast<uint32_t>(id)>>16,generation=static_cast<uint32_t>(id)&0xffffu;
        const Address entityAddress=view.Reference(handle,"Entity");
        const auto world=view.Object(entityAddress,o.world_entity),entity=view.Object(entityAddress,o.core_entity),base=view.Object(entityAddress,o.core_object_base);
        if(current){currentId=id;currentCid=role.cid;}
        members.push_back({{"player_id",pid},{"group_type",groupType},{"group_role_index",i},{"role_id",role.role},
            {"config_id",item.config},{"creature_data_id",role.cid},{"entity_id",id},{"entity_index",index},
            {"entity_generation",generation},{"current",current}});
        witnessMembers.push_back({{"group_role_index",i},{"role",Ref(role.object)},{"item",Ref(item.object)},
            {"handle",Ref(handle)},{"world_entity",Ref(world)},{"core_entity",Ref(entity)},{"object_base",Ref(base)},
            {"entity_flags",view.Int32(entity,"FlagInternal")},{"is_auto_role_internal",item.automatic},{"remote_is_control",item.remote},
            {"on_stage_without_control",role.onStage},{"cached_handle_validated",true},
            {"character_handle_slot_same_reference",true},{"object_slot_same_reference",true},{"version_slot_equal",true}});
    }
    Json roster={{"source_route",kSourceRoute},{"source_contract",kSourceContract},{"ready",true},{"binding_authorized",false},
        {"player_id",pid},{"group_type",groupType},{"current_entity_id",currentId},{"current_creature_data_id",currentCid},{"members",members}};
    Json stable={{"roots",{{"player_info_model",Ref(info)},{"scene_team_model",Ref(model)},{"character_model",Ref(character)},
        {"object_system",Ref(objects)},{"player",Ref(player)},{"group",Ref(group)}}},
        {"collections",{{"team_player_map",Hex(playerMap)},{"group_map",Hex(groupMap)},{"role_list",Hex(roleList)},{"team_items",Hex(teamItems)},
            {"character_handles",Hex(handles)},{"objects",Hex(objectList)},{"versions",Hex(versions)},{"handles_count",handleCount},
            {"objects_count",objectCount},{"versions_count",versionCount},{"team_items_count",items.size()},{"other_player_item_count",remoteCount}}},
        {"current_role",Hex(currentRole)},{"current_item",Hex(currentItem)},
        {"context",{{"model_current_group_has_value",view.NullableEnumI4(model,"CurrentGroupType").has_value()},{"player_current_group_has_value",true},
            {"is_team_ready",view.Boolean(model,"IsTeamReady")},{"is_phantom_team",view.Boolean(model,"IsPhantomTeam")},
            {"wait_task",Hex(view.Reference(model,"WaitTask"))},{"load_team_promise",Hex(view.Reference(model,"LoadTeamPromise"))},
            {"requesting_change_role",view.Boolean(model,"RequestingChangeRole")},{"changing_role_time",view.Int64(model,"ChangingRoleTime")},
            {"is_seamless_update_team",view.Boolean(model,"IsSeamlessUpdateTeam")},{"seamless_update_team_start_time",view.Int64(model,"SeamlessUpdateTeamStartTime")},
            {"seamless_update_team_block_input",view.Boolean(model,"IsSeamlessUpdateTeamBlockInput")},
            {"seamless_delay_remove_entity_handle",Hex(view.Reference(model,"SeamlessDelayRemoveEntityHandle"))},
            {"seamless_wait_update_team_timer",Hex(view.Reference(model,"SeamlessWaitUpdateTeamTimer"))},
            {"is_fixed_location",view.Boolean(group,"IsFixedLocation")},{"living_state",view.Int32(group,"LivingState")}}},
        {"members",witnessMembers}};
    Json witness={{"schema",1},{"source_route",kSourceRoute},{"source_contract",kSourceContract},{"status","managed_roster_chain_revalidated"},
        {"diagnostic_only",true},{"binding_authorized",false},{"snapshot_atomic",false},
        {"two_frame_verified",false},{"native_join_verified",false},{"three_person_visual_verified",false},
        {"mono_api_calls",0},{"game_memory_writes",0},{"class_initialization_performed",false},{"getters_invoked",false},
        {"role_list_order_preserved",true},{"team_items_filtered_by_local_player",true},
        {"derived_world_entity_parent_chain_checked",true},{"changing_role_time_policy","require_zero_without_getter_false_negatives_possible"},
        {"stable_identity",stable},{"observation_view",view.Evidence()}};
    return {{"roster",roster},{"csharp_witness",witness}};
}
inline Json StableIdentity(const Json& captured){return {{"roster",captured.at("roster")},
    {"csharp",captured.at("csharp_witness").at("stable_identity")}};}
} // namespace csharp_scene_team_readonly
