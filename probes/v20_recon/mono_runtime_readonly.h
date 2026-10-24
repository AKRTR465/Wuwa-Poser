#pragma once
// Observe existing Mono metadata through Reader::Read only. No Mono API,
// thread attachment, native callback, managed method, or runtime lock call.
#include "runtime_evidence.h"
#include "../../core_math/third_party/nlohmann/json.hpp"
#include <cstdio>

namespace mono_runtime_readonly {
namespace util=runtime_evidence;
using Json=nlohmann::json;
using Address=uint64_t;
namespace pins {inline constexpr const char* kSourceDllSha256="fc205bf3c7baa98a21ab21bcb95b12a0df443eabcfc9aa4455e7ce5864fe41d3";}
inline std::string Hex(Address address){char text[32]{};sprintf_s(text,"0x%llx",static_cast<unsigned long long>(address));return text;}
template<class Reader> std::string CString(Reader& reader,Address address){
    std::string value;if(!address)return value;
    for(;;){const Address next=address+value.size();char chunk[64];const size_t count=(std::min)(sizeof(chunk),size_t(4096-(next&4095)));
        util::Read(reader,next,chunk,count);for(size_t i=0;i<count;++i){if(!chunk[i])return value;value.push_back(chunk[i]);}}
}
template<class Reader> Json Capture(Reader& reader,Address module,Address extent){
    const Address root=util::Get<Address>(reader,module+0x49d3d8);Json entries=Json::array();
    for(Address node=util::Get<Address>(reader,module+0x49d3f0);node;node=util::Get<Address>(reader,node+8)){
        const Address image=util::Get<Address>(reader,util::Get<Address>(reader,node)+0x68);
        entries.push_back({{"image",Hex(image)},{"name",CString(reader,util::Get<Address>(reader,image+0x30))}});
    }
    return {{"schema",1},{"status","existing_mono_inventory_observed"},{"runtime_family","dotnet_mono"},
        {"module",Hex(module)},{"module_extent",Hex(extent)},{"expected_source_dll_sha256",pins::kSourceDllSha256},
        {"root_domain",Hex(root)},{"root_domain_present",root!=0},{"entries",entries},
        {"inventory_passes",2},{"observation_bytes_revalidated",true},{"snapshot_atomic",false},
        {"mono_api_calls",0},{"managed_method_calls",0},{"thread_attach_calls",0},{"runtime_lock_calls",0},
        {"assembly_load_calls",0},{"game_memory_writes",0},{"binding_authorized",false}};
}
} // namespace mono_runtime_readonly
