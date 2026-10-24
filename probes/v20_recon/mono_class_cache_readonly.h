#pragma once
// Cached native class and static storage addresses only. No Mono API calls,
// initialization, static field values, managed object reads, or game writes.
#include "object_identity.h"
#include "mono_runtime_readonly.h"

namespace mono_class_cache_readonly {
namespace util=runtime_evidence;
using Address=uint64_t;using Json=nlohmann::json;
using mono_runtime_readonly::Hex;
struct Type {uint32_t token;const char* name;};
template<class R> Address Find(R& r,Address image,uint32_t token){
    const auto count=util::Get<uint32_t>(r,image+0x850+24);const auto buckets=util::Get<Address>(r,image+0x850+32);
    Address klass=util::Get<Address>(r,buckets+Address(token%count)*8);
    while(klass&&util::Get<uint32_t>(r,klass+0x58)!=token)klass=util::Get<Address>(r,klass+0x110);
    return klass;
}
template<class R> Json Storage(R& r,Address klass){
    Json out={{"status","static_storage_unavailable"},{"address_only",true},{"static_field_values_read",false}};
    const Address vt=util::Get<Address>(r,klass+0xd8);out["vtable"]=Hex(vt);
    if(!vt){out["status"]="vtable_not_initialized";return out;}
    out["initialized_35"]=util::Get<uint8_t>(r,vt+0x35);out["flags_38"]=util::Get<uint8_t>(r,vt+0x38);
    const auto slots=util::Get<int32_t>(r,klass+0x5c);const auto size=util::Get<uint32_t>(r,klass+0x98);
    const Address cell=vt+0x50+static_cast<Address>(slots)*8,base=util::Get<Address>(r,cell);
    out["vtable_slot_count"]=slots;out["static_size"]=size;out["static_base_cell"]=Hex(cell);out["static_base"]=Hex(base);
    if(base&&size)out["status"]="initialized_static_storage_address_captured";return out;
}
template<class R> Json Resolve(R& r,Address image,const Type& type){
    const Address klass=Find(r,image,type.token);
    Json out={{"token",Hex(type.token)},{"name",type.name},{"namespace",""},{"klass",Hex(klass)},{"status","class_not_cached"},
        {"fields",Json::array()},{"class_initialization_performed",false}};
    if(!klass)return out;
    out["kind"]=util::Get<uint8_t>(r,klass+0x1b);out["rank"]=util::Get<uint8_t>(r,klass+0x1a);
    out["metadata_state_20"]=util::Get<uint8_t>(r,klass+0x20);out["layout_flags_28"]=util::Get<uint32_t>(r,klass+0x28);
    const auto storage=Storage(r,klass);out["static_storage"]=storage;
    const Address fields=util::Get<Address>(r,klass+0xa0);const auto count=util::Get<uint32_t>(r,klass+0x108);
    for(uint32_t i=0;i<count;++i){
        const Address field=fields+static_cast<Address>(i)*32;std::array<unsigned char,32> raw{};util::Read(r,field,raw.data(),raw.size());
        const auto name=mono_runtime_readonly::CString(r,At<Address>(raw.data(),8));if(name!="_Instance"&&name!="_instance")continue;
        const Address monoType=At<Address>(raw.data(),0);const auto offset=At<int32_t>(raw.data(),24);const auto typeFlags=util::Get<uint32_t>(r,monoType+8);
        Json row={{"name",name},{"field",Hex(field)},{"parent_raw",Hex(At<Address>(raw.data(),16))},{"offset",offset},
            {"metadata_flags",typeFlags&0xffffu},{"mono_type",Hex(monoType)},{"runtime_type_flags",typeFlags},{"type_code",(typeFlags>>16)&0xffu},
            {"static_field_value_read",false},{"status","static_storage_not_ready"}};
        if(storage.at("status")=="initialized_static_storage_address_captured"){
            row["static_field_address"]=Hex(ww_object::Rva(storage.at("static_base"))+static_cast<Address>(offset));row["status"]="static_field_address_captured";}
        out["fields"].push_back(std::move(row));
    }
    out["status"]="cached_class_field_layout_captured";return out;
}
template<class R> Json Capture(R& r,Address image,const std::vector<Type>& types){
    Json resolved=Json::array();for(const auto& t:types)resolved.push_back(Resolve(r,image,t));
    return {{"schema",1},{"status","mono_cached_class_layout_captured"},{"binding_authorized",false},{"roster_verified",false},
        {"source_module_sha256",mono_runtime_readonly::pins::kSourceDllSha256},{"image",Hex(image)},{"selected_types",resolved},
        {"mono_api_calls",0},{"game_memory_writes",0},{"class_initialization_performed",false},{"static_field_values_read",false},
        {"snapshot_atomic",false},{"observation_bytes_revalidated",true}};
}
} // namespace mono_class_cache_readonly
