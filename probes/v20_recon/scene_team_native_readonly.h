#pragma once
#include "stage_capture_readonly.h"
#include "stage_native_readonly.h"

// Native join of the C# roster: runtime EntityId -> entity -> Actor, then the
// shared stage member capture. No engine function or writer.
namespace scene_team_native {
namespace q=ww_object;
static constexpr const char* kSourceContract="scene-team-csharp-managed-readonly-20261021-v1";
static constexpr const char* kSourceRoute="csharp_managed_readonly";
static void Require(bool ok,const char* why){if(!ok)throw std::runtime_error(std::string("scene team native: ")+why);}
template<class R> static q::Identity Strong(R& r,const Go& go,uint64_t address){
    q::Identity result;Require(q::ReadStable(r,go,address,&result)&&result.serial!=0,"strong object identity unavailable");return result;
}
static Json Token(Reader& r,const Go& go,const Pool& pool,const q::Identity& id){return stage_capture::Token(r,go,pool,id);}

// The primary lookup key is the roster's runtime EntityId, never Wpo/name/bones.
template<class R> static Json ResolveById(R& r,const Go& go,const Pool& pool,uint64_t image,int32_t expectedId){
    uint64_t model=0;Require(r.Get(image+0x97ff970,model)&&Pointer(model),"entity model unavailable");
    const uint64_t map=model+0x10;std::array<unsigned char,0x50> head{};
    Require(r.Read(map,head.data(),head.size()),"id map unreadable");
    const uint64_t data=At<uint64_t>(head.data(),0),allocation=At<uint64_t>(head.data(),0x20),hash=At<uint64_t>(head.data(),0x40);
    const int32_t slots=At<int32_t>(head.data(),8),hashSize=At<int32_t>(head.data(),0x48);
    const uint64_t hashBase=hash?hash:map+0x38,allocationBase=allocation?allocation:map+0x10;
    int32_t slot=-1;Require(r.Get(hashBase+uint64_t(uint32_t(expectedId)&uint32_t(hashSize-1))*4,slot),"id bucket unreadable");
    uint64_t entity=0,reference=0;
    for(unsigned n=0;slot!=-1&&n<256;++n){
        Require(slot>=0&&slot<slots,"id hash chain invalid");
        uint32_t allocated=0;Require(r.Get(allocationBase+uint64_t(uint32_t(slot)>>5)*4,allocated)&&
            (allocated&(1u<<(uint32_t(slot)&31))),"id slot unallocated");
        std::array<unsigned char,32> entry{};Require(r.Read(data+uint64_t(slot)*32,entry.data(),entry.size()),"id entry unreadable");
        if(At<int32_t>(entry.data(),0)==expectedId){entity=At<uint64_t>(entry.data(),8);reference=At<uint64_t>(entry.data(),16);break;}
        slot=At<int32_t>(entry.data(),24);
    }
    Require(Pointer(entity)&&Pointer(reference),"requested roster EntityId absent or invalid");
    uint64_t holder=0,actorAddress=0;
    Require(r.Get(entity+0x10,holder)&&Pointer(holder)&&r.Get(holder+0x10,actorAddress)&&Pointer(actorAddress),"entity holder reference differs");
    const auto actor=Strong(r,go,actorAddress);
    return {{"status","entity_id_actor_joined"},{"binding_authorized",false},{"entity_id",expectedId},
        {"entity_model",Hex(model)},{"entity",Hex(entity)},{"holder",Hex(holder)},{"reference_control",Hex(reference)},
        {"actor",Token(r,go,pool,actor)},
        {"lookup_evidence_revalidated",true},{"snapshot_atomic",false}};
}
template<class R> static Json InactiveControl(R& r,const Go& go,const Pool& pool,const q::Identity& actor){
    uint64_t address=0;Require(r.Get(actor.object+0x2e8,address),"inactive controller unreadable");
    if(!address)return {{"kind","None"},{"controller",nullptr},{"expected_control_class",nullptr}};
    const auto controller=Strong(r,go,address);
    Require(q::ClassName(r,go,pool,controller.cls)=="KuroAIController","inactive controller is not KuroAIController");
    return {{"kind","AI"},{"controller",Token(r,go,pool,controller)},{"expected_control_class","KuroAIController"},
        {"relation_verified",true}};
}
static Json Fingerprint(const Json& captured){
    Json members=Json::array();
    for(const auto& row:captured.at("members")){Json value;
        for(const char* key:{"actor","mesh","root","mesh_asset","skeleton","profile_id","reference_bone_names","reference_bone_parents",
            "roster_member","native_entity_join","resident_control","actor_hidden_target_rva","root_move_target_rva"})value[key]=row.at(key);
        members.push_back(value);
    }
    return {{"controller",captured.at("controller")},{"local_player",captured.at("local_player")},{"level",captured.at("level")},
        {"pcm",captured.at("pcm")},{"current_actor",captured.at("current_actor")},{"members",members}};
}
template<class R> static Json Capture(R& r,const Go& go,const Pool& pool,const Json& config,const Json& roster,uint64_t image,uint64_t){
    Json members=Json::array();q::Identity pc,local;size_t currentIndex=0;
    for(size_t i=0;i<3;++i){const auto& logical=roster.at("members").at(i);
        auto joined=ResolveById(r,go,pool,image,logical.at("entity_id").get<int32_t>());
        const auto actor=stage_capture::FromJson(joined.at("actor"));
        uint64_t main=0;Require(r.Get(actor.object+0x310,main),"Character.Mesh unreadable");
        const auto mesh=Strong(r,go,main);
        Json row={{"actor",joined.at("actor")},{"mesh",Token(r,go,pool,mesh)},{"roster_member",logical},{"native_entity_join",joined}};
        if(logical.at("current").get<bool>()){
            Require(stage_native::LocalControl(r,go,pool,actor,&pc,&local),"current roster member is not the fresh local Pawn");
            currentIndex=i;row["resident_control"]={{"kind","CurrentPlayer"},{"controller",Token(r,go,pool,pc)},
                {"expected_control_class",q::ClassName(r,go,pool,pc.cls)}};
        }else row["resident_control"]=InactiveControl(r,go,pool,actor);
        members.push_back(row);
    }
    Json captureConfig=config;captureConfig["controller"]=Token(r,go,pool,pc);captureConfig["members"]=members;
    auto captured=stage_capture::Capture(r,go,pool,captureConfig,image);
    Require(captured.at("active_member_index")==currentIndex,"native current member differs from roster");
    for(size_t i=0;i<3;++i){auto& row=captured["members"][i];
        row.update(stage_native::Reference(r,go,pool,stage_capture::FromJson(row.at("mesh")),config.at("profiles")));
        for(const char* key:{"roster_member","native_entity_join","resident_control"})row[key]=members.at(i).at(key);
        
    }
    captured["local_player"]=Token(r,go,pool,local);
    captured["schema"]=1;captured["status"]="scene_team_native_joined";captured["binding_mode"]="scene_team_native_diagnostic";
    captured["binding_authorized"]=false;captured["runtime_origin_verified"]=false;
    captured["snapshot_atomic"]=false;captured["native_join_verified"]=true;captured["two_pass_native_join"]=true;
    captured["owner_chain_used_for_membership"]=false;
    captured["roster_source_route"]=kSourceRoute;captured["roster_source_contract"]=kSourceContract;
    captured["creature_id_policy"]="positive_int64";
    captured["player_id"]=roster.at("player_id");captured["group_type"]=roster.at("group_type");captured["native_fingerprint_payload"]=Fingerprint(captured);
    return captured;
}
} // namespace scene_team_native
