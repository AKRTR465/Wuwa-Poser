#pragma once
// Observation-only Mono 9 view. Reader has only bool Read(address, out, bytes).
// No runtime calls, initialization, GC coordination, writes, or binding authority.
#include "mono_runtime_readonly.h"
#include <map>
#include <optional>

namespace mono_managed_readonly {
namespace util=runtime_evidence;
using Address=uint64_t; using Json=nlohmann::json;
using mono_runtime_readonly::Hex;
namespace pins {
inline constexpr const char* kCorelibSha256="ecb361f91c749730c2b0dbbee69b2ee62fde662a2f2ab63b948fed00618e1e1d";
inline constexpr unsigned kCodeWitnessCount=60;
}
struct ObjectRef {Address address=0,klass=0,field_scope=0;};

template<class R> class View {
    struct Field {Address type=0;int32_t offset=0;};
    struct Array {Address payload=0,element=0;uint32_t length=0,stride=0;};
    R& reader_;Address mono_=0,corelib_=0;size_t bytes_=0,reads_=0;
    std::map<std::pair<Address,std::string>,Field> fields_;
    template<class T> T Get(Address address){bytes_+=sizeof(T);++reads_;return util::Get<T>(reader_,address);}
    Field GetField(Address scope,const std::string& name){
        const auto key=std::make_pair(scope,name);const auto found=fields_.find(key);if(found!=fields_.end())return found->second;
        const Address definition=Get<uint8_t>(scope+0x1b)==3?Get<Address>(Get<Address>(scope+0xf8)):scope;
        const auto count=Get<uint32_t>(definition+0x108);const auto array=Get<Address>(scope+0xa0);
        for(uint32_t i=0;i<count;++i){const Address field=array+Address(i)*32;++reads_;
            if(mono_runtime_readonly::CString(reader_,Get<Address>(field+8))!=name)continue;
            const Field f{Get<Address>(field),Get<int32_t>(field+24)};fields_.emplace(key,f);return f;}
        throw std::runtime_error("requested_declared_field_missing: "+name);
    }
    Address At(const ObjectRef& object,const std::string& name){return object.address+GetField(object.field_scope,name).offset;}
    Address GenericClass(Address type){return Get<Address>(Get<Address>(type)+0x20);}
    Array ReadArray(Address address){const Address klass=Get<Address>(Get<Address>(address));
        return {address+32,Get<Address>(klass),Get<uint32_t>(address+24),Get<uint32_t>(klass+0x98)};}
    std::pair<Array,uint32_t> ReadList(Address address){const auto object=Object(address,0);
        return {ReadArray(Get<Address>(At(object,"_items"))),static_cast<uint32_t>(Int32(object,"_size"))};}
public:
    View(R& reader,Address monoModule,Address corelibImage):reader_(reader),mono_(monoModule),corelib_(corelibImage){}
    ObjectRef Object(Address address,Address expectedKlass,bool=false){
        const Address klass=Get<Address>(Get<Address>(address));return {address,klass,expectedKlass?expectedKlass:klass};
    }
    Address StaticReference(Address klass,const std::string& name){
        const auto field=GetField(klass,name);const auto vt=Get<Address>(klass+0xd8);
        const auto base=Get<Address>(vt+0x50+Address(Get<int32_t>(klass+0x5c))*8);return Get<Address>(base+static_cast<Address>(field.offset));
    }
    Address Reference(const ObjectRef& object,const std::string& name){return Get<Address>(At(object,name));}
    int32_t Int32(const ObjectRef& object,const std::string& name){return Get<int32_t>(At(object,name));}
    int64_t Int64(const ObjectRef& object,const std::string& name){return Get<int64_t>(At(object,name));}
    bool Boolean(const ObjectRef& object,const std::string& name){return Get<uint8_t>(At(object,name))!=0;}
    std::optional<int32_t> NullableEnumI4(const ObjectRef& object,const std::string& name){
        const auto outer=GetField(object.field_scope,name);const Address nullable=GenericClass(outer.type);
        const Address payload=object.address+outer.offset;
        if(!Get<uint8_t>(payload+GetField(nullable,"hasValue").offset-16))return std::nullopt;
        return Get<int32_t>(payload+GetField(nullable,"value").offset-16);
    }
    uint32_t ListCount(Address address){return ReadList(address).second;}
    std::vector<Address> ListReferences(Address address){const auto [array,size]=ReadList(address);
        std::vector<Address> out;for(uint32_t i=0;i<size;++i)out.push_back(Get<Address>(array.payload+Address(i)*8));return out;}
    std::vector<std::pair<int32_t,Address>> DictionaryI4References(Address address){
        const auto object=Object(address,0);const auto count=Int32(object,"_count");const Address entries=Get<Address>(At(object,"_entries"));
        std::vector<std::pair<int32_t,Address>> out;if(!entries)return out;
        const auto array=ReadArray(entries);const auto next=GetField(array.element,"next"),key=GetField(array.element,"key"),value=GetField(array.element,"value");
        for(uint32_t i=0;i<static_cast<uint32_t>(count);++i){const Address payload=array.payload+Address(i)*array.stride;
            if(Get<int32_t>(payload+next.offset-16)>=-1)out.emplace_back(Get<int32_t>(payload+key.offset-16),Get<Address>(payload+value.offset-16));}
        return out;
    }
    Json Evidence()const{return {{"schema",1},{"diagnostic_only",true},{"binding_authorized",false},{"roster_verified",false},
        {"snapshot_atomic",false},{"observation_bytes_revalidated",true},{"mono_api_calls",0},{"game_memory_writes",0},
        {"class_initialization_performed",false},{"source_module_sha256",mono_runtime_readonly::pins::kSourceDllSha256},{"corelib_sha256",pins::kCorelibSha256},
        {"mono_module",Hex(mono_)},{"corelib_image",Hex(corelib_)},{"code_witness_count",pins::kCodeWitnessCount},
        {"class_identity_checks_are_image_and_token",true},{"static_storage_observed_without_class_initialization",true},
        {"corelib_whole_buffer_hash_this_view",false},{"corelib_source_origin_mode","caller_verified_whole_buffer"},
        {"corelib_external_origin_requires_caller_revalidation",true},{"corelib_external_origin_revalidation_claimed_by_view",false},
        {"reader_bytes_including_revalidation",bytes_},{"reader_calls_including_revalidation",reads_}};}
};
} // namespace mono_managed_readonly
