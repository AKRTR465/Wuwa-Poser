#pragma once
// Shared strong identity value types. Runtime resources own their restoration
// rules; no unused parallel policy interpreter is retained here.
#include <array>
#include <cstdint>
#include <cstddef>
#include <cstring>
#include <cmath>
#include <type_traits>
#include <utility>

namespace wwp_playback { namespace ww_restore {
constexpr uint32_t kUnset=UINT32_MAX;
inline bool Pointer(uint64_t x){return x>=0x10000&&x<0x800000000000ull&&!(x&7);}
struct Token {
    uint64_t address=0,item_address=0,class_address=0,vtable=0,outer_address=0;
    uint32_t index=kUnset,serial=0;
    bool independently_verified=false;
};
inline bool Strong(const Token& t){return t.independently_verified&&Pointer(t.address)&&Pointer(t.item_address)&&Pointer(t.class_address)&&
    Pointer(t.vtable)&&(!t.outer_address||Pointer(t.outer_address))&&t.index!=kUnset&&t.serial!=0;}
inline bool Same(const Token& a,const Token& b){return Strong(a)&&Strong(b)&&a.address==b.address&&a.item_address==b.item_address&&
    a.class_address==b.class_address&&a.vtable==b.vtable&&a.outer_address==b.outer_address&&a.index==b.index&&a.serial==b.serial;}
}} // namespace wwp_playback::ww_restore
