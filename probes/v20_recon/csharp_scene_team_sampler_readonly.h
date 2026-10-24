#pragma once
// Source discovery for observation-only C# roster samples. No Mono API,
// managed method, lazy getter, initialization, character setter or display.
#include "csharp_runtime_readonly.h"
#include "mono_runtime_readonly.h"
#include "mono_class_cache_readonly.h"
#include "csharp_scene_team_readonly.h"

namespace csharp_scene_team_sampler {
using Address=uint64_t;using Json=nlohmann::json;
namespace util=runtime_evidence;
inline void Require(bool value,const char* reason){if(!value)throw std::runtime_error(reason);}
struct State {
    Address mono=0,extent=0,corelib=0;
    csharp_scene_team_readonly::Origins origins;
    Json evidence;
};
template<class R> Json RawAnchor(R& reader,Address mono,Address selected){
    std::array<unsigned char,32> types{},fields{};util::Read(reader,selected+0x128,types.data(),32);util::Read(reader,selected+0x168,fields.data(),32);
    return {{"raw",Hex(util::Get<Address>(reader,selected+0x10))},{"length",util::Get<uint32_t>(reader,selected+0x18)},{"flags",util::Get<uint8_t>(reader,selected+0x1c)},
        {"image_updates",util::Get<uint32_t>(reader,selected+0xa20)},{"global_updates",util::Get<uint32_t>(reader,mono+0x4ae66c)},
        {"strings",Hex(util::Get<Address>(reader,selected+0x70))},{"strings_bytes",util::Get<uint32_t>(reader,selected+0x78)},
        {"types_base",Hex(At<Address>(types.data(),0))},{"fields_base",Hex(At<Address>(fields.data(),0))},
        {"types_header_sha256",util::Sha256(types.data(),types.size())},{"fields_header_sha256",util::Sha256(fields.data(),fields.size())}};
}
inline Json AssemblyReport(Address mono,Address image,const Json& anchor,const char* sha256){
    return {{"schema",1},{"status","existing_managed_assembly_bytes_copied"},{"mono_module",Hex(mono)},
        {"expected_source_module_sha256",mono_runtime_readonly::pins::kSourceDllSha256},{"mono_image",Hex(image)},
        {"raw_data",anchor.at("raw")},{"byte_count",anchor.at("length")},{"sha256",sha256},{"second_pass_sha256",sha256},
        {"whole_buffer_passes",2},{"image_header_revalidated",true},{"pe_header_revalidated",true},
        {"metadata_updates_rejected",true},{"dynamic_image_rejected",true},{"snapshot_atomic",false},{"raw_buffer_lifetime_locked",false},
        {"mono_api_calls",0},{"assembly_load_calls",0},{"managed_method_calls",0},{"thread_attach_calls",0},{"runtime_lock_calls",0},
        {"game_memory_writes",0},{"binding_authorized",false}};
}
inline Json MetadataReport(Address image,const Json& anchor){
    return {{"schema",1},{"status","mono_type_field_metadata_captured"},{"source_module_sha256",mono_runtime_readonly::pins::kSourceDllSha256},
        {"image",Hex(image)},{"raw_data",anchor.at("raw")},{"raw_data_len",anchor.at("length")},{"binding_authorized",false},
        {"snapshot_atomic",false},{"class_initialization_performed",false},{"static_field_values_read",false},{"observation_bytes_revalidated",true}};
}
inline Address Image(const Json& ue,const char* name){
    for(const auto& entry:ue.at("loaded_assemblies").at("entries"))if(entry.at("key")==std::string(name)+".dll")return ww_object::Rva(entry.at("mono_image"));
    throw std::runtime_error("CSharp requested assembly missing in UE inventory");
}
inline Address Klass(const Json& layout,const char* name){
    for(const auto& type:layout.at("selected_types"))if(type.at("name")==name)return ww_object::Rva(type.at("klass"));
    throw std::runtime_error("CSharp required class absent");
}
inline std::vector<mono_class_cache_readonly::Type> RequiredTypes(bool game){
    if(game)return {{0x020025ee,"PlayerInfoModel"},{0x0200295c,"SceneTeamModel"},{0x0200294b,"SceneTeamPlayer"},
        {0x0200294c,"SceneTeamGroup"},{0x0200294a,"SceneTeamRole"},{0x02002953,"SceneTeamItem"},
        {0x020031bb,"EntityHandle"},{0x02002e32,"CharacterModel"},{0x02003266,"WorldEntity"}};
    return {{0x02000075,"Entity"},{0x020000b4,"ObjectBase"},{0x020000b6,"ObjectSystem"}};
}
template<class R> State Discover(R& reader,const Go& go,const Pool& pool,Address image,
    const ww_object::Identity& active,Address mono,Address monoExtent){
    State state;state.mono=mono;state.extent=monoExtent;auto& o=state.origins;
    const auto ue=csharp_runtime_readonly::Capture(reader,go,pool,image,active);
    const auto native=mono_runtime_readonly::Capture(reader,mono,monoExtent);
    o.game_image=Image(ue,"Aki.Game");o.core_image=Image(ue,"Aki.Core");state.corelib=Image(ue,"System.Private.CoreLib");
    Json hashes=Json::object(),metadata=Json::object(),layouts=Json::object(),rawAnchors=Json::object();
    const struct {const char* name;Address image;const char* sha256;} sources[]={
        {"Aki.Game.dll",o.game_image,"3a2e4684e387b43f075f60cd5a726f367a1eca1a0dc5389596b11be103ff2c21"},
        {"Aki.Core.dll",o.core_image,"cc80f3b2c130dce51fbdea7b86a0fb44b71a2d1612a69d977b9f796a034e9b6c"},
        {"System.Private.CoreLib.dll",state.corelib,mono_managed_readonly::pins::kCorelibSha256}};
    for(const auto& source:sources){
        rawAnchors[source.name]=RawAnchor(reader,mono,source.image);
        hashes[source.name]=AssemblyReport(mono,source.image,rawAnchors[source.name],source.sha256);
        if(source.image==state.corelib)continue;
        metadata[source.name]=MetadataReport(source.image,rawAnchors[source.name]);
        layouts[source.name]=mono_class_cache_readonly::Capture(reader,source.image,RequiredTypes(source.image==o.game_image));
    }
    const auto& game=layouts.at("Aki.Game.dll");const auto& core=layouts.at("Aki.Core.dll");
    o.player_info_model=Klass(game,"PlayerInfoModel");o.scene_team_model=Klass(game,"SceneTeamModel");
    o.scene_team_player=Klass(game,"SceneTeamPlayer");o.scene_team_group=Klass(game,"SceneTeamGroup");
    o.scene_team_role=Klass(game,"SceneTeamRole");o.scene_team_item=Klass(game,"SceneTeamItem");
    o.entity_handle=Klass(game,"EntityHandle");o.character_model=Klass(game,"CharacterModel");
    o.world_entity=Klass(game,"WorldEntity");o.core_entity=Klass(core,"Entity");
    o.core_object_base=Klass(core,"ObjectBase");o.core_object_system=Klass(core,"ObjectSystem");
    state.evidence={{"schema",1},{"source_route",csharp_scene_team_readonly::kSourceRoute},
        {"source_contract",csharp_scene_team_readonly::kSourceContract},{"diagnostic_only",true},{"binding_authorized",false},
        {"mono_module",Hex(mono)},{"module_extent",Hex(monoExtent)},
        {"game_image",Hex(o.game_image)},{"core_image",Hex(o.core_image)},{"corelib_image",Hex(state.corelib)},
        {"ue_inventory",ue},{"mono_inventory",native},{"assembly_hashes",hashes},
        {"class_layouts",layouts},{"metadata",metadata},{"raw_anchors",rawAnchors},
        {"whole_assembly_hashes_this_capture",true},{"metadata_full_scan_this_capture",true},{"metadata_source","current_full_metadata_scan"},
        {"snapshot_atomic",false},{"mono_api_calls",0},{"game_memory_writes",0}};
    return state;
}
inline Json OriginIdentity(const State& s){
    const auto& o=s.origins;Json storage=Json::object();
    const struct {const char* assembly;const char* name;} required[]={{"Aki.Game.dll","SceneTeamModel"},
        {"Aki.Game.dll","PlayerInfoModel"},{"Aki.Game.dll","CharacterModel"},{"Aki.Core.dll","ObjectSystem"}};
    for(const auto& pin:required)for(const auto& type:s.evidence.at("class_layouts").at(pin.assembly).at("selected_types"))
        if(type.at("name")==pin.name)storage[pin.name]={{"storage",type.at("static_storage")},{"singleton_fields",type.at("fields")}};
    return {{"mono",Hex(s.mono)},{"extent",Hex(s.extent)},{"corelib",Hex(s.corelib)},
        {"game",Hex(o.game_image)},{"core",Hex(o.core_image)},
        {"root_domain",s.evidence.at("mono_inventory").at("root_domain")},
        {"ue_domain",s.evidence.at("ue_inventory").at("domain_candidate")},
        {"ue_environment",s.evidence.at("ue_inventory").at("mono_environment")},{"static_lineage",storage},
        {"raw_anchors",s.evidence.at("raw_anchors")},
        {"classes",{o.player_info_model,o.scene_team_model,o.scene_team_player,o.scene_team_group,o.scene_team_role,
            o.scene_team_item,o.entity_handle,o.character_model,o.world_entity,o.core_entity,o.core_object_base,o.core_object_system}}};
}
} // namespace csharp_scene_team_sampler
