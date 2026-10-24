#pragma once
// Existing-object inventory only. No Mono, engine, allocator or lock calls.
#include "object_identity.h"
#include "runtime_evidence.h"

namespace csharp_runtime_readonly {
namespace util=runtime_evidence;
namespace q=ww_object;
using Address=uint64_t;
using Json=nlohmann::json;
inline constexpr Address kMonoEnvironmentRva=0x9837cb0,kDomainCandidateRva=0x98377a0,kAssemblyMapRva=0x93d23f0;

template<class Reader> std::vector<Address> ClassChain(Reader& reader,Address cls){
    std::vector<Address> entries(static_cast<size_t>(util::Get<int32_t>(reader,cls+0x40))+1);
    util::Read(reader,util::Get<Address>(reader,cls+0x38),entries.data(),entries.size()*sizeof(Address));
    for(auto& entry:entries)entry-=0x38;return entries;
}
template<class Reader> Json ActorClasses(Reader& reader,const Go& go,const Pool& pool,const q::Identity& actor) {
    Json rows=Json::array();unsigned generated=0,redirected=0;
    for(const Address address:ClassChain(reader,actor.cls)){
        q::Identity cls;util::Require(q::ReadStable(reader,go,address,&cls),"CSharp inventory UObject identity invalid");
        bool typeScript=false;for(const Address meta:ClassChain(reader,cls.cls)){q::Identity base;
            typeScript=typeScript||(q::ReadStable(reader,go,meta,&base)&&q::Name(reader,pool,base)=="TypeScriptGeneratedClass");}
        Json row={{"class",q::Token(cls,q::Name(reader,pool,cls),q::ClassName(reader,go,pool,cls.cls))},{"metaclass_is_typescript_generated",typeScript}};
        if(typeScript){++generated;const uint8_t redirect=util::Get<uint8_t>(reader,cls.object+0x3a2);
            row["injection_flag_3a0"]=util::Get<uint8_t>(reader,cls.object+0x3a0);row["redirect_flag_3a2"]=redirect;if(redirect)++redirected;}
        rows.push_back(std::move(row));
    }
    return {{"active_actor",q::Token(actor,q::Name(reader,pool,actor),q::ClassName(reader,go,pool,actor.cls))},{"class_chain",rows},
        {"generated_class_count",generated},{"redirected_class_count",redirected}};
}
template<class Reader> std::string AssemblyName(Reader& reader,const unsigned char* node) {
    std::vector<wchar_t> value(static_cast<size_t>(At<int32_t>(node,8)));util::Read(reader,At<Address>(node,0),value.data(),value.size()*2);
    const int bytes=WideCharToMultiByte(CP_UTF8,0,value.data(),int(value.size())-1,nullptr,0,nullptr,nullptr);std::string result(static_cast<size_t>(bytes),'\0');
    WideCharToMultiByte(CP_UTF8,0,value.data(),int(value.size())-1,result.data(),bytes,nullptr,nullptr);return result;
}
template<class Reader> Json Assemblies(Reader& reader,Address image) {
    const Address map=image+kAssemblyMapRva;std::array<unsigned char,0x50> h{};util::Read(reader,map,h.data(),h.size());
    const Address data=At<Address>(h.data(),0),allocation=At<Address>(h.data(),0x20);const int32_t slots=At<int32_t>(h.data(),8);
    std::vector<uint32_t> bits(static_cast<size_t>((slots+31)/32));
    if(!bits.empty())util::Read(reader,allocation?allocation:map+0x10,bits.data(),bits.size()*4);
    Json entries=Json::array();
    for(int32_t slot=0;slot<slots;++slot){
        if(!(bits[static_cast<size_t>(slot/32)]&(1u<<unsigned(slot%32))))continue;
        std::array<unsigned char,32> node{};util::Read(reader,data+static_cast<Address>(slot)*32,node.data(),node.size());
        entries.push_back({{"key",AssemblyName(reader,node.data())},{"mono_image",Hex(At<Address>(node.data(),16))}});
    }
    return {{"entries",entries}};
}
template<class Reader> Json Capture(Reader& reader,const Go& go,const Pool& pool,Address image,const q::Identity& activeActor) {
    const Address environment=util::Get<Address>(reader,image+kMonoEnvironmentRva);
    const Address runtime=environment?util::Get<Address>(reader,environment+0x18):0;
    const Address domain=util::Get<Address>(reader,image+kDomainCandidateRva);
    return {{"schema",1},{"status","csharp_runtime_inventory_captured"},{"binding_authorized",false},{"game_memory_writes",0},
        {"game_method_calls",0},{"mono_calls",0},{"snapshot_atomic",false},{"observation_bytes_revalidated",true},
        {"mono_environment",{{"global",Hex(image+kMonoEnvironmentRva)},{"object",Hex(environment)},
            {"present",environment!=0},{"runtime_environment",Hex(runtime)},{"runtime_present",runtime!=0}}},
        {"domain_candidate",{{"global",Hex(image+kDomainCandidateRva)},{"pointer",Hex(domain)},{"present",domain!=0}}},
        {"actor_classes",ActorClasses(reader,go,pool,activeActor)},{"loaded_assemblies",Assemblies(reader,image)}};
}
} // namespace csharp_runtime_readonly
