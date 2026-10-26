#pragma once
#include "../../runtime/generated/GameCompat.generated.h"
// Matches data/native_identity_registration_contract.json.
#include <cstdint>
namespace native_identity_witness {
inline constexpr const char* kSha="76bc5b93e2eb08f08b83481459ebafe16dfce2523d1324ff6397c7ceb5f56626";
inline constexpr uint64_t kObjectArrayRva=ww_game_compat::ObjectArrayRva,kAllocatorRva=ww_game_compat::FunctionAllocateSerialNumberRva;
struct Function {const char* label;const char* rva;unsigned bytes;const char* sha256;};
inline constexpr Function kFunctions[]={
    {"FUObjectArray_AllocateSerialNumber",ww_game_compat::FunctionAllocateSerialNumberRvaText,ww_game_compat::FunctionAllocateSerialNumberBytes,ww_game_compat::FunctionAllocateSerialNumberSha256},
    {"FWeakObjectPtr_AssignObject",ww_game_compat::FunctionAssignWeakObjectRvaText,ww_game_compat::FunctionAssignWeakObjectBytes,ww_game_compat::FunctionAssignWeakObjectSha256}};
}
