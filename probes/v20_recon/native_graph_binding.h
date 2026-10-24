#pragma once
// Worker-side immutable native graph binding derived from the session discovery.
#include "../../core_math/third_party/nlohmann/json.hpp"
#include <array>
#include <cstdint>
#include <stdexcept>
#include <string>

namespace v20_native_graph_binding {
using Json=nlohmann::json;
constexpr uint32_t kMaxGroups=32;
constexpr uint64_t kConverterVtableRva=0x83ec178,kGroupVtableRva=0x7405e28;
struct Identity {uint64_t address=0;uint32_t index=0,serial=0;};
struct CurveGroup {uint32_t offset=0,alpha_type=2;std::array<uint32_t,3> curve_name_words{};std::string curve_name;};
struct Binding {
    Identity target,main_anim;
    uint32_t bone_count=0,converter_offset=0,group_count=0;
    std::array<CurveGroup,kMaxGroups> groups{};
    bool activation_complete=false;
};
inline void Require(bool value,const char* message){if(!value)throw std::runtime_error(message);}
inline uint64_t Address(const Json& j){return j.is_string()?std::stoull(j.get<std::string>(),nullptr,16):j.get<uint64_t>();}
inline uint32_t U32(const Json& j){return uint32_t(Address(j));}
inline Identity ReadIdentity(const Json& j){return {Address(j.at("address")),U32(j.at("index")),U32(j.at("serial"))};}
inline Binding Parse(const Json& j){
    Binding b;b.bone_count=U32(j.at("bone_count"));b.target=ReadIdentity(j.at("target"));b.main_anim=ReadIdentity(j.at("main_anim"));
    b.converter_offset=U32(j.at("converter").at("offset"));
    const auto& groups=j.at("curve_groups");Require(groups.size()<=kMaxGroups,"native graph curve group count exceeds32");
    b.group_count=uint32_t(groups.size());
    for(uint32_t i=0;i<b.group_count;++i){const auto& source=groups[i];auto& group=b.groups[i];
        group.offset=U32(source.at("offset"));group.alpha_type=U32(source.at("alpha_type"));group.curve_name=source.at("curve_name").get<std::string>();
        for(unsigned k=0;k<3;++k)group.curve_name_words[k]=U32(source.at("curve_name_words")[k]);}
    b.activation_complete=j.at("proof").at("activation_complete").get<bool>();
    return b;
}
} // namespace v20_native_graph_binding
