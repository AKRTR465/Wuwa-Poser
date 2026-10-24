#pragma once
// Fixed caller-owned storage. No helper heap allocation; native UE insertion
// may allocate normally. Caller owns the game-thread phase.
#include "ww_face_snapshot.h"
namespace wwp_playback { namespace face_runtime {
namespace fs=face_snapshot;
struct Prepared {
    fs::Manifest snapshot;
    uint32_t count=0;
    std::array<std::array<uint32_t,3>,fs::kMaxMorphs> names{};
    std::array<uint8_t,fs::kMaxMorphs> mapped{};
};
struct Backup {bool originally_present=false,pending=false;float original=0,last_written=0;};
struct MapSnapshot {
    std::array<uint8_t,fs::kMaxHeader> raw_map_header{};
    uint32_t weight_count=0;
    std::array<fs::Weight,fs::kMaxSlots> weights{};
};
struct State {
    Prepared prepared;bool active=false;uint32_t count=0;
    std::array<Backup,fs::kMaxMorphs> backup{};
};
// Preallocate outside callbacks; do not share across concurrent lifecycles.
struct Scratch {
    MapSnapshot map;
    std::array<uint8_t,fs::kMaxHeader> header{};
    std::array<uint8_t,fs::kMaxSlots*24> payload{};
    std::array<uint8_t,fs::kMaxSlots/8> bits{};
    std::array<uint8_t,24> row{};
};
inline bool Key(const std::array<uint32_t,3>& a,const std::array<uint32_t,3>& b){return a[0]==b[0]&&a[2]==b[2];}
inline bool Close(float a,float b){return std::isfinite(a)&&std::isfinite(b)&&std::fabs(a-b)<=1e-5f;}
inline bool Pending(const State& state){for(uint32_t i=0;i<state.count&&i<fs::kMaxMorphs;++i)if(state.backup[i].pending)return true;return false;}
namespace detail {
template<class Reader>bool Strong(Reader& r,const Prepared& p){
    const auto& m=p.snapshot;fs::Identity mesh,asset;uint64_t asset_pointer=0;
    return r.Identify(m.expected_mesh.address,&mesh)&&r.Identify(m.expected_asset.address,&asset)&&
        fs::Same(mesh,m.expected_mesh)&&fs::Same(asset,m.expected_asset)&&
        r.Read(mesh.address+m.mesh_asset_offset,&asset_pointer,8)&&asset_pointer==asset.address;
}
struct MapHead {uint64_t data=0,heap=0;uint32_t slots=0;};
inline bool Header(const fs::SparseMapLayout& l,const uint8_t* h,MapHead& out){
    out.data=fs::detail::U64(h+l.data_pointer);out.heap=fs::detail::U64(h+l.allocation_pointer);
    out.slots=fs::detail::U32(h+l.data_num);return out.slots<=fs::kMaxSlots;
}
template<class Reader>bool Bits(Reader& r,const fs::SparseMapLayout& l,const MapHead& head,const uint8_t* header,uint8_t* out){
    const size_t bytes=(head.slots+7)/8;if(!bytes)return true;
    if(head.heap)return r.Read(head.heap,out,bytes);std::memcpy(out,header+l.allocation_inline,bytes);return true;
}
inline void Weight(const uint8_t* row,fs::Weight& out){
    for(unsigned k=0;k<3;++k)out.name[k]=fs::detail::U32(row+k*4);
    const uint32_t raw=fs::detail::U32(row+12);std::memcpy(&out.value,&raw,4);
}
template<class Reader>bool MapValue(Reader& r,const Prepared& p,Scratch& s,const std::array<uint32_t,3>& key,float& value,bool& exists){
    const auto& m=p.snapshot;const auto& l=m.map;MapHead head;value=0;exists=false;
    if(!r.Read(m.expected_mesh.address+m.map_offset,s.header.data(),l.header_bytes)||!Header(l,s.header.data(),head)||
        !Bits(r,l,head,s.header.data(),s.bits.data()))return false;
    for(uint32_t i=0;i<head.slots;++i)if(s.bits[i/8]&(1u<<(i%8))){fs::Weight weight;
        if(!r.Read(head.data+uint64_t(i)*24,s.row.data(),24))return false;Weight(s.row.data(),weight);
        if(Key(weight.name,key)){exists=true;value=weight.value;return true;}
    }
    return true;
}
inline bool Written(float value,bool exists,float target){
    // Native removes abs(weight)<=1e-8 with removeZero=true.
    return Close(value,target)&&(target<=1e-8f?!exists:exists);
}
} // detail
// Both buffers must be preallocated; out=&scratch->map is supported.
template<class Reader>bool ObserveMap(Reader& reader,const Prepared& prepared,Scratch* scratch,MapSnapshot* out)noexcept{
    const auto& m=prepared.snapshot;const auto& l=m.map;auto& s=*scratch;detail::MapHead head;
    if(!reader.Read(m.expected_mesh.address+m.map_offset,out->raw_map_header.data(),l.header_bytes)||
        !detail::Header(l,out->raw_map_header.data(),head)||!detail::Bits(reader,l,head,out->raw_map_header.data(),s.bits.data()))return false;
    out->weight_count=0;
    const size_t bytes=size_t(head.slots)*24;if(bytes&&!reader.Read(head.data,s.payload.data(),bytes))return false;
    for(uint32_t i=0;i<head.slots;++i)if(s.bits[i/8]&(1u<<(i%8)))detail::Weight(s.payload.data()+i*24,out->weights[out->weight_count++]);
    return true;
}
inline void Value(const MapSnapshot& snap,const std::array<uint32_t,3>& key,float& value,bool& exists){
    for(uint32_t i=0;i<snap.weight_count;++i)if(Key(snap.weights[i].name,key)){value=snap.weights[i].value;exists=true;return;}
    value=0;exists=false;
}
template<class Reader>bool Start(Reader& reader,const Prepared& prepared,State* out,Scratch* scratch)noexcept{
    if(!detail::Strong(reader,prepared)||!ObserveMap(reader,prepared,scratch,&scratch->map))return false;
    out->prepared=prepared;out->count=prepared.count;out->active=true;
    for(auto& backup:out->backup)backup=Backup{};
    for(uint32_t i=0;i<out->count;++i)if(prepared.mapped[i])Value(scratch->map,prepared.names[i],out->backup[i].original,out->backup[i].originally_present);
    return true;
}
// Mark pending before calling: even a failed adapter may mutate engine Map.
template<class Reader,class Writer>bool Frame(Reader& reader,Writer& writer,const float* weights,uint32_t count,State* state,Scratch* scratch)noexcept{
    if(!detail::Strong(reader,state->prepared)){state->active=false;return false;}
    for(uint32_t i=0;i<count;++i)if(state->prepared.mapped[i]){const float target=(std::max)(0.f,(std::min)(1.f,weights[i]));float current=0;bool exists=false;
        if(!detail::MapValue(reader,state->prepared,*scratch,state->prepared.names[i],current,exists)){state->active=false;return false;}
        if(detail::Written(current,exists,target))continue;
        auto& backup=state->backup[i];backup.pending=true;backup.last_written=target;
        if(!writer.SetWeight(state->prepared.snapshot.expected_mesh.address,state->prepared.names[i].data(),target,true)){state->active=false;return false;}
    }
    return true;
}
template<class Reader,class Writer>bool Restore(Reader& reader,Writer& writer,State* state,Scratch* scratch)noexcept{
    state->active=false;
    if(!Pending(*state))return true;
    if(!detail::Strong(reader,state->prepared))return false;
    for(uint32_t i=0;i<state->count;++i){auto& backup=state->backup[i];if(!backup.pending)continue;float value=0;bool exists=false;
        if(!detail::MapValue(reader,state->prepared,*scratch,state->prepared.names[i],value,exists))return false;
        if(Close(value,backup.last_written)&&
            !writer.SetWeight(state->prepared.snapshot.expected_mesh.address,state->prepared.names[i].data(),backup.original,!backup.originally_present))return false;
        backup.pending=false;
    }
    return true;
}
}} // namespace
