#pragma once
#include "../runtime/generated/GameCompat.generated.h"
// Reader-injected, read-only face state capture. Asset morph ordering is decoded
// from live Go-indexed UMorphTarget objects; the sparse override map layout is
// fixed for the current build.
#include <array>
#include <algorithm>
#include <cstdint>
#include <cstddef>
#include <cstring>
#include <cmath>

namespace wwp_playback { namespace face_snapshot {
constexpr uint32_t kUnset=UINT32_MAX,kMaxMorphs=512,kMaxSlots=1024,kMaxHeader=128;
struct Identity {
    uint64_t address=0,class_address=0,outer_address=0;
    uint32_t index=kUnset,serial=0,object_flags=0,item_flags=0;
    std::array<uint32_t,ww_game_compat::NameWordCount> name{};
};
inline bool Same(const Identity& a,const Identity& b){return a.address==b.address&&a.class_address==b.class_address&&a.outer_address==b.outer_address&&
    a.index==b.index&&a.serial==b.serial&&a.name==b.name;}
struct ArrayHead {uint64_t data=0;uint32_t num=0,max=0;};
// TSparseArray<TPair<FName,float>> header inside the component override map.
struct SparseMapLayout {uint32_t header_bytes=ww_game_compat::MorphMapHeaderBytes,data_pointer=ww_game_compat::MorphMapDataOffset,data_num=ww_game_compat::MorphMapSlotsOffset,allocation_pointer=ww_game_compat::MorphMapAllocationPointerOffset,allocation_inline=ww_game_compat::MorphMapAllocationInlineOffset;};
struct Manifest {
    Identity expected_mesh,expected_asset;
    uint32_t mesh_asset_offset=kUnset,asset_morph_array_offset=kUnset,map_offset=kUnset,expected_morphs=0;
    SparseMapLayout map;
};
struct MorphBinding {Identity identity;std::array<char,128> name{};uint32_t asset_index=0;};
struct Weight {std::array<uint32_t,ww_game_compat::NameWordCount> name{};float value=0;};
struct Snapshot {
    Identity mesh,asset;ArrayHead morph_array;uint32_t morph_count=0;
    std::array<MorphBinding,kMaxMorphs> morphs{};
};
namespace detail {
inline uint32_t U32(const uint8_t* p){uint32_t n=0;for(unsigned i=0;i<4;++i)n|=uint32_t(p[i])<<(i*8);return n;}
inline uint64_t U64(const uint8_t* p){uint64_t n=0;for(unsigned i=0;i<8;++i)n|=uint64_t(p[i])<<(i*8);return n;}
}
// Reader.Read copies all bytes; Reader.Identify reads the Go item identity.
// Decode(nameWords,char*,capacity) supplies complete UTF8 and never writes game.
template<class Reader,class Decode>
bool Capture(Reader& reader,Decode&& decode,const Manifest& m,Snapshot* out)noexcept{
    Snapshot& capture=*out;capture=Snapshot{};capture.mesh=m.expected_mesh;capture.asset=m.expected_asset;std::array<uint8_t,ww_game_compat::TArrayHeaderBytes> head{};
    if(!reader.Read(m.expected_asset.address+m.asset_morph_array_offset,head.data(),ww_game_compat::TArrayHeaderBytes))return false;
    capture.morph_array={detail::U64(head.data()+ww_game_compat::TArrayDataOffset),detail::U32(head.data()+ww_game_compat::TArrayNumOffset),detail::U32(head.data()+ww_game_compat::TArrayCapacityOffset)};
    const auto& a=capture.morph_array;if(a.num!=m.expected_morphs||a.num>kMaxMorphs)return false;
    std::array<uint64_t,kMaxMorphs> pointers{};if(a.num&&!reader.Read(a.data,pointers.data(),a.num*ww_game_compat::PointerBytes))return false;
    capture.morph_count=a.num;
    for(uint32_t i=0;i<a.num;++i){auto& binding=capture.morphs[i];binding.asset_index=i;
        if(!reader.Identify(pointers[i],&binding.identity)||!decode(binding.identity.name,binding.name.data(),binding.name.size()))return false;
    }
    return true;
}
}} // namespace wwp_playback::face_snapshot
