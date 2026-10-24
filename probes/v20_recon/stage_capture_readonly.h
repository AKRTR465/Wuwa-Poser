#pragma once
#include "object_identity.h"
// Fresh read-only revalidation of three actors selected by background scanning.
// No native calls or object lifetime changes.
#include "../../playback/ww_world_snapshot.h"
namespace stage_capture {
namespace q=ww_object;
static q::Identity FromJson(const Json& j,bool=true){
    q::Identity t;t.object=std::stoull(j.at("address").get<std::string>(),nullptr,16);
    t.item=std::stoull(j.at("item_address").get<std::string>(),nullptr,16);
    t.cls=std::stoull(j.at("class_address").get<std::string>(),nullptr,16);
    t.outer=std::stoull(j.at("outer_address").get<std::string>(),nullptr,16);
    t.vtable=std::stoull(j.at("vtable").get<std::string>(),nullptr,16);
    t.index=j.at("index");t.serial=j.at("serial");t.name=j.at("name_words").get<std::array<uint32_t,3>>();
    t.flags=j.value("object_flags",0u);t.item_flags=j.value("item_flags",0u);return t;
}
static Json Token(Reader& r,const Go& go,const Pool& pool,const q::Identity& t){
    return q::Token(t,q::Name(r,pool,t),q::ClassName(r,go,pool,t.cls));
}
static q::Identity Fresh(Reader& r,const Go& go,const q::Identity& expected){
    q::Identity actual;const bool read=q::ReadStable(r,go,expected.object,&actual);
    if(!read||!q::Same(expected,actual))
        throw std::runtime_error("stage identity differs: "+Json{{"expected",q::Token(expected,"","")},
            {"readable_stable",read},{"observed",read?q::Token(actual,"",""):Json(nullptr)}}.dump());return actual;
}
static Json World(Reader& r,const q::Identity& component){
    std::array<uint8_t,48> a{};
    if(!r.Read(component.object+0x1e0,a.data(),48))throw std::runtime_error("stage world transform unreadable");
    const auto t=wwp_playback::world_snapshot::Decode(a);
    return {{"rotation_xyzw",t.rotation},{"translation",t.translation},{"scale",t.scale},{"raw_hex",BytesHex(a.data(),48)}};
}
static Json Member(Reader& r,const Go& go,const Pool& pool,const Json& binding){
    const auto actor=Fresh(r,go,FromJson(binding.at("actor"))),mesh=Fresh(r,go,FromJson(binding.at("mesh")));
    uint64_t rootAddress=0,assetAddress=0; q::Identity root,asset;
    if(!r.Get(actor.object+0x1a0,rootAddress)||!q::ReadStable(r,go,rootAddress,&root)||
       !r.Get(mesh.object+0x5d0,assetAddress)||!q::ReadStable(r,go,assetAddress,&asset))throw std::runtime_error("stage root/mesh asset unavailable");
    uint8_t af[2]{},mf[2]{},an[2]{};float rate=0;uint32_t generation=0;uint64_t moveTarget=0;
    if(!r.Read(actor.object+0x60,af,2)||!r.Read(mesh.object+0x16e,mf,2)||!r.Read(mesh.object+0xcb8,an,2)||
       !r.Get(mesh.object+0xca0,rate)||!r.Get(mesh.object+0x6c8,generation)||!r.Get(root.vtable+0x4f0,moveTarget))throw std::runtime_error("stage flags unavailable");
    return {{"actor",Token(r,go,pool,actor)},{"mesh",Token(r,go,pool,mesh)},{"root",Token(r,go,pool,root)},
        {"mesh_asset",Token(r,go,pool,asset)},{"actor_world",World(r,root)},{"mesh_world",World(r,mesh)},
        {"flags",{{"actor_hidden",bool(af[0]&32)},{"actor_0x61",af[1]},{"actor_unhide_suppression_bit",bool(af[1]&1)},
            {"mesh_visible",bool(mf[0]&32)},{"mesh_hidden_in_game",bool(mf[1]&16)},
            {"no_skeleton_update",bool(an[0]&64)},{"animation_paused",bool(an[1]&2)},{"global_anim_rate_scale",rate}}},
        {"publication_generation",generation},{"static_layout_build",q::kLayoutHash},{"root_move_target_address",Hex(moveTarget)}};
}
static Json Capture(Reader& r,const Go& go,const Pool& pool,const Json& config,uint64_t image){
    const auto controller=Fresh(r,go,FromJson(config.at("controller")));
    uint64_t pawn=0,pcmAddress=0;
    if(!r.Get(controller.object+0x2e8,pawn)||!r.Get(controller.object+0x360,pcmAddress))throw std::runtime_error("stage control unreadable");
    q::Identity pcm;if(!q::ReadStable(r,go,pcmAddress,&pcm))throw std::runtime_error("stage camera identity unavailable");
    Json out={{"schema",1},{"status","captured"},{"readonly",true},{"game_memory_writes",0},{"native_calls",0},
        {"pid",GetCurrentProcessId()},{"source_sha256",q::kLayoutHash},{"controller",Token(r,go,pool,controller)},
        {"session_evidence_id",config.at("session_evidence_id")},{"session_evidence_semantics","current_csharp_preparation_evidence_sha256"},
        {"pcm",Token(r,go,pool,pcm)},{"pcm_lifecycle_weak",pcm.serial==0},{"members",Json::array()},
        {"current_actor",Hex(pawn)},{"active_member_index",nullptr},{"two_pass_relations",true}};
    uint64_t levelAddress=0;
    for(size_t i=0;i<3;++i){auto row=Member(r,go,pool,config.at("members").at(i));
        const auto actor=FromJson(row.at("actor"));levelAddress=actor.outer;
        if(actor.object==pawn)out["active_member_index"]=i;
        uint64_t hiddenTarget=0;if(!r.Get(actor.vtable+0x358,hiddenTarget))throw std::runtime_error("actor hide virtual unavailable");
        row["actor_hidden_target_rva"]=Hex(hiddenTarget-image);
        row["root_move_target_rva"]=Hex(std::stoull(row.at("root_move_target_address").get<std::string>(),nullptr,16)-image);out["members"].push_back(row);
    }
    if(out["active_member_index"].is_null())throw std::runtime_error("current actor is not in the stage party");
    q::Identity level;if(!q::ReadStable(r,go,levelAddress,&level))throw std::runtime_error("stage level identity unavailable");
    out["level"]=Token(r,go,pool,level);
    uint64_t update=0;if(!r.Get(pcm.vtable+0x790,update))throw std::runtime_error("camera update virtual unavailable");
    out["pcm_update_target_rva"]=Hex(update-image);out["pcm_vtable_rva"]=Hex(pcm.vtable-image);return out;
}
} // namespace stage_capture
