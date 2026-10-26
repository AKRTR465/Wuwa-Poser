#pragma once
// Preparation-only JSON adapter. Candidate scans are diagnostic and never
// authorize replacing a function address or installing a hook.
#include "game_compat_resolution.h"
#include "../../core_math/third_party/nlohmann/json.hpp"
#include <sstream>

namespace ww_game_compat_witness {
namespace resolution=ww_game_compat_resolution;
using Json=nlohmann::json;
using Address=resolution::Address;
struct Function {const char* label;Address rva;size_t bytes;};
inline std::string Hex(Address value){std::ostringstream text;text<<"0x"<<std::hex<<value;return text.str();}
inline bool CompatibleSource(const Json& cfg,const char* profile,const char* digest,Json& checks,std::string& error){
    error.clear();
    const bool valid=cfg.contains("compatibility_profile")&&cfg["compatibility_profile"].is_string()&&cfg["compatibility_profile"]==profile&&
        cfg.contains("compatibility_source_sha256")&&cfg["compatibility_source_sha256"].is_string()&&cfg["compatibility_source_sha256"]==digest;
    checks.push_back({{"kind","compatibility_source"},{"performed",true},{"matched",valid},{"game_abi_verified",false},
        {"expected_profile",profile},{"expected_source_sha256",digest}});
    if(!valid)error="compatibility configuration and compiled carrier have different sources";return valid;
}
template<class Read> Json Candidate(Read&& read,Address image,Address imageBytes,const Json& witness,const std::vector<uint8_t>& exact){
    Json result={{"execution_authorized",false},{"candidate_requires_review",true},{"complete",false}};
    resolution::Pattern pattern;pattern.bytes=exact;pattern.mask.assign(exact.size(),0xff);std::string error;
    if(witness.contains("signature_bytes_hex")||witness.contains("signature_mask_hex")){
        if(!witness.contains("signature_bytes_hex")||!witness["signature_bytes_hex"].is_string()||
           !witness.contains("signature_mask_hex")||!witness["signature_mask_hex"].is_string()||witness["signature_mask_hex"].get<std::string>().empty()||
           !resolution::ParsePattern(witness["signature_bytes_hex"].get<std::string>(),witness["signature_mask_hex"].get<std::string>(),pattern,error)){
            result["status"]="invalid_signature";result["reason"]=error.empty()?"explicit signature bytes and mask are both required":error;return result;}
        result["pattern_kind"]="explicit_mask";
    }else result["pattern_kind"]="exact_function_bytes";
    std::vector<resolution::Range> ranges;
    if(!resolution::ExecutableRanges(read,image,imageBytes,ranges,error)){result["status"]="invalid_executable_ranges";result["reason"]=error;return result;}
    const auto scan=resolution::FindUnique(read,ranges,pattern);result["status"]=resolution::Name(scan.status);result["complete"]=scan.complete;
    result["matches_lower_bound"]=scan.matches;result["executable_section_count"]=ranges.size();
    if(scan.status==resolution::ScanStatus::Unique){result["candidate_rva"]=Hex(scan.address-image);
        const bool code=std::any_of(ranges.begin(),ranges.end(),[&](const resolution::Range& range){return resolution::Contains(range,scan.address,exact.size());});
        result["full_function_span_executable"]=code;
        result["exact_function_bytes_match"]=code&&resolution::VerifyBytes(read,image,imageBytes,scan.address-image,exact)==resolution::VerifyStatus::Match;}
    return result;
}
template<class Read> bool VerifyFunctions(Read&& read,Address image,Address imageBytes,const Json& witnesses,const std::vector<Function>& required,Json& checks,const char* group,std::string& error){
    error.clear();
    if(!witnesses.is_array()||witnesses.size()!=required.size()||required.empty()){
        error=std::string(group)+": complete function witness set required";
        checks.push_back({{"kind","function_bytes"},{"group",group},{"performed",false},{"matched",false},{"reason",error}});return false;}
    std::vector<Address> seen;std::vector<const Json*> matched(required.size(),nullptr);
    for(const auto& row:witnesses){Address rva=0;std::string parseError;
        if(!row.is_object()||!row.contains("rva")||!row["rva"].is_string()||!resolution::ParseHexU64(row["rva"].get<std::string>(),rva,parseError)||
           std::find(seen.begin(),seen.end(),rva)!=seen.end()){
            error=std::string(group)+": malformed or duplicate function RVA";break;}
        seen.push_back(rva);size_t index=0;for(;index<required.size();++index)if(required[index].rva==rva)break;
        if(index==required.size()){error=std::string(group)+": unexpected function RVA "+Hex(rva);break;}matched[index]=&row;
    }
    if(!error.empty()){
        checks.push_back({{"kind","function_bytes"},{"group",group},{"performed",false},{"matched",false},{"reason",error}});return false;}
    std::vector<resolution::Range> executable;
    if(!resolution::ExecutableRanges(read,image,imageBytes,executable,error)){
        error=std::string(group)+": "+error;
        checks.push_back({{"kind","function_bytes"},{"group",group},{"performed",false},{"matched",false},{"status","invalid_executable_ranges"},{"reason",error}});return false;}
    bool accepted=true;
    for(size_t index=0;index<required.size();++index){const auto& expected=required[index];
        Json check={{"kind","function_bytes"},{"group",group},{"label",expected.label},{"rva",Hex(expected.rva)},
            {"required_bytes",expected.bytes},{"performed",false},{"matched",false},{"execution_authorized",false}};
        std::vector<uint8_t> bytes;std::string parseError;
        if(!matched[index]||!matched[index]->contains("function_bytes_hex")||!(*matched[index])["function_bytes_hex"].is_string()||
           !resolution::ParseHexBytes((*matched[index])["function_bytes_hex"].get<std::string>(),bytes,parseError)||bytes.size()!=expected.bytes){
            accepted=false;check["status"]="invalid_function_bytes";check["reason"]=parseError.empty()?"full expected function byte count required":parseError;
        }else{
            const auto& row=*matched[index];
            if(row.contains("bytes")&&(!row["bytes"].is_number_integer()||(!row["bytes"].is_number_unsigned()&&row["bytes"].get<int64_t>()<0)||row["bytes"].get<uint64_t>()!=bytes.size())){
                accepted=false;check["status"]="invalid_byte_count";check["reason"]="declared byte count differs from function bytes";
            }else{
                const bool code=expected.rva<imageBytes&&std::any_of(executable.begin(),executable.end(),[&](const resolution::Range& range){return resolution::Contains(range,image+expected.rva,bytes.size());});
                const auto status=code?resolution::VerifyBytes(read,image,imageBytes,expected.rva,bytes):resolution::VerifyStatus::InvalidRange;
                check["performed"]=true;check["executable_section_verified"]=code;
                check["status"]=code?resolution::Name(status):"outside_executable_sections";check["matched"]=code&&status==resolution::VerifyStatus::Match;
                check["execution_authorized"]=code&&status==resolution::VerifyStatus::Match;
                if(!code||status!=resolution::VerifyStatus::Match){accepted=false;
                    // A unique candidate is evidence for a human update only.
                    check["relocation_candidate"]=Candidate(read,image,imageBytes,row,bytes);}
            }
        }
        if(!check["matched"].get<bool>()&&error.empty())error=std::string(group)+": function byte verification failed for "+expected.label;
        checks.push_back(std::move(check));
    }
    return accepted;
}
} // namespace ww_game_compat_witness
