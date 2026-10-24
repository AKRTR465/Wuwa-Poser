#pragma once
// Matches data/native_identity_registration_contract.json.
#include <cstdint>
namespace native_identity_witness {
inline constexpr const char* kSha="76bc5b93e2eb08f08b83481459ebafe16dfce2523d1324ff6397c7ceb5f56626";
inline constexpr uint64_t kObjectArrayRva=0x99a3fe0,kAllocatorRva=0x33f11d0;
struct Function {const char* label;const char* rva;unsigned bytes;const char* sha256;};
inline constexpr Function kFunctions[]={
    {"FUObjectArray_AllocateSerialNumber","0x33f11d0",169,"d061ffb1798bb5262881a0842045a842350288a0a34a07728bb43bf0c70a106e"},
    {"FWeakObjectPtr_AssignObject","0x3404280",59,"165872ef14eebb05c725a39e381bf60d31d5b8e5beb16cc4f33627eff3eb2b1b"}};
}
