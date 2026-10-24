#pragma once
#include "object_identity.h"
#include "../../playback/native_animation_target.h"
#include "native_animation_target_config.h"

// Only the creator or the SceneTeam adapter publishes this permission. A
// discovered graph cannot grant itself authority over a different actor.
namespace native_target_publication {
namespace target=native_animation_target;
// Support assets may receive their first engine serial during identity
// registration; adopt the current generation.
template<class R>bool PromoteRegisteredSupport(R& reader,const Go& go,ww_object::Identity& expected,const Json&){
    ww_object::Identity actual;
    if(!ww_object::ReadStable(reader,go,expected.object,&actual)||!actual.serial)return false;
    expected=actual;return true;
}
inline uint64_t ProcessBirth(){FILETIME born{},exit{},kernel{},user{};
    GetProcessTimes(GetCurrentProcess(),&born,&exit,&kernel,&user);
    return (uint64_t(born.dwHighDateTime)<<32)|born.dwLowDateTime;
}
inline void Publish(target::AuthoritySnapshot& state,const target::AuthorityTarget& identity,bool valid){
    const auto previous=state.sequence;
    InterlockedExchange64(reinterpret_cast<volatile LONG64*>(&state.sequence),LONG64(previous+1));
    state.epoch=identity.epoch;state.policy=uint32_t(identity.policy);state.valid=valid?1u:0u;
    state.actor=identity.actor;state.mesh=identity.mesh;state.mesh_asset=identity.mesh_asset;
    state.skeleton_asset=identity.skeleton_asset;state.main_anim=identity.main_anim;
    state.actor_index=identity.actor_index;state.actor_serial=identity.actor_serial;
    state.mesh_index=identity.mesh_index;state.mesh_serial=identity.mesh_serial;
    MemoryBarrier();InterlockedExchange64(reinterpret_cast<volatile LONG64*>(&state.sequence),LONG64(previous+2));
}
inline void Revoke(target::AuthoritySnapshot& state){const auto sequence=state.sequence;
    InterlockedExchange64(reinterpret_cast<volatile LONG64*>(&state.sequence),LONG64(sequence+1));
    state.valid=0;MemoryBarrier();InterlockedExchange64(reinterpret_cast<volatile LONG64*>(&state.sequence),LONG64(sequence+2));
}
template<class R>Json Token(R& reader,const Go& go,const Pool& pool,const ww_object::Identity& object){
    auto value=ww_object::Token(object,ww_object::Name(reader,pool,object),ww_object::ClassName(reader,go,pool,object.cls));
    value["identity_rechecked"]=true;
    value["class"]=value["class_name"];value["name_verified"]=!value["name"].get<std::string>().empty();
    value["class_name_verified"]=!value["class_name"].get<std::string>().empty();return value;
}
template<class R>Json Export(R& reader,const Go& go,const Pool& pool,const Json& seed,
    const ww_object::Identity& actor,const ww_object::Identity& mesh,const ww_object::Identity& asset,
    const ww_object::Identity& skeleton,const ww_object::Identity& main,target::AuthoritySnapshot& authority,
    target::OwnerPolicy policy,uint64_t epoch){
    target::AuthorityTarget identity{policy,epoch,uint64_t(&authority),actor.object,mesh.object,asset.object,skeleton.object,main.object,
        actor.index,actor.serial,mesh.index,mesh.serial};
    Json cfg=seed;const auto meshToken=Token(reader,go,pool,mesh),actorToken=Token(reader,go,pool,actor);
    cfg["object"]=Hex(mesh.object);cfg["index"]=mesh.index;cfg["serial"]=mesh.serial;cfg["item_address"]=Hex(mesh.item);
    cfg["class_address"]=Hex(mesh.cls);cfg["outer_address"]=Hex(mesh.outer);cfg["vtable"]=Hex(mesh.vtable);
    cfg["name_words"]=mesh.name;cfg["expected_pawn_class"]=actorToken.at("class_name");
    cfg["expected_actor_class"]=actorToken.at("class_name");cfg["expected_component_name"]=meshToken.at("name");
    cfg["gobjects_rva"]="0x99a3ff0";cfg["name_pool_rva"]="0x9967340";
    cfg["source_sha256"]=ww_object::kLayoutHash;cfg["profile_resolution"]="fresh_target_identity";
    cfg["game_pid"]=GetCurrentProcessId();cfg["expected_game_pid"]=GetCurrentProcessId();cfg["expected_process_creation_filetime"]=ProcessBirth();
    cfg["animation_target"]={{"schema",1},{"owner_policy",target::PolicyName(policy)},{"session_epoch",Hex(epoch)},
        {"authority_address",Hex(uint64_t(&authority))},{"process_start_filetime",Hex(ProcessBirth())},
        {"actor",actorToken},{"mesh",meshToken},{"mesh_asset",Token(reader,go,pool,asset)},{"skeleton_asset",Token(reader,go,pool,skeleton)},
        {"main_anim",Token(reader,go,pool,main)}};
    Publish(authority,identity,true);
    return cfg;
}
}
