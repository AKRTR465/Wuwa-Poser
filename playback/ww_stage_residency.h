#pragma once
// Pure two-resident ledger for a three-person stage (the controlled player stays
// native). Captures each original once, plans writes and restores to it.
#include <array>
#include <cstdint>
#include <cstring>
#include <cmath>
#include <algorithm>
#include <limits>

namespace wwp_playback { namespace ww_stage_residency {
constexpr uint32_t kFieldCount=4;
enum class Field:uint8_t {ActorHidden,MeshVisible,MeshHiddenInGame,RootPosition};
enum class Action:uint8_t {Publish,Restore};
enum class Code:uint8_t {Ok,WrongThread,IdentityChanged,ValueUnknown,ReadbackFailed,UnknownIdentity};
inline const char* Name(Code c){switch(c){
    case Code::Ok:return "ok";case Code::WrongThread:return "wrong_thread";case Code::IdentityChanged:return "identity_changed";
    case Code::ValueUnknown:return "value_unknown";case Code::ReadbackFailed:return "readback_failed";case Code::UnknownIdentity:return "unknown_identity";}return "unknown";}
struct Value {
    std::array<float,3> position{};bool flag=false;
    static Value Flag(bool b){Value v;v.flag=b;return v;}
    static Value Position(float x,float y,float z){Value v;v.position={{x,y,z}};return v;}
};
struct ReadValue {Value value;bool read=false;};
using Values=std::array<ReadValue,kFieldCount>;
struct Desired {std::array<Value,kFieldCount> values{};std::array<bool,kFieldCount> enabled{};};
inline uint32_t Index(Field f){return uint32_t(f);}
inline bool Equal(Field f,const Value& a,const Value& b){
    return f==Field::RootPosition?std::memcmp(a.position.data(),b.position.data(),12)==0:a.flag==b.flag;
}
// UE native placement computes delta/parent transforms in float, so a successful
// setter can return a few ULPs away from the requested coordinate. Units are UE
// centimetres: 0.0001 cm floor, up to 8 ULPs, capped at 0.001 cm.
constexpr double kPlacementAbsoluteFloor=0.0001,kPlacementAbsoluteCap=0.001;
constexpr unsigned kPlacementUlps=8;
inline double PlacementUlp(float value){
    const float magnitude=std::fabs(value);
    return double(std::nextafter(magnitude,(std::numeric_limits<float>::infinity)()))-double(magnitude);
}
inline bool NearPlacement(const Value& a,const Value& b){
    for(unsigned k=0;k<3;++k){
        const double ulp=(std::max)(PlacementUlp(a.position[k]),PlacementUlp(b.position[k]));
        const double tolerance=(std::min)(kPlacementAbsoluteCap,(std::max)(kPlacementAbsoluteFloor,kPlacementUlps*ulp));
        if(!(std::fabs(double(a.position[k])-double(b.position[k]))<=tolerance))return false;
    }return true;
}
inline bool MatchesDesired(Field f,const Value& observed,const Value& desired){
    return f==Field::RootPosition?NearPlacement(observed,desired):Equal(f,observed,desired);
}
struct Entry {Value original,last_written;bool captured=false,pending=false;};
struct Ledger {
    std::array<Entry,kFieldCount> entries{};
    uint64_t attempts=0,confirmed_writes=0,confirmed_restores=0,preserved_native_changes=0;
    bool bound=false;
};
struct WriteRequest {Field field=Field::ActorHidden;Action action=Action::Publish;Value before,desired;};
struct Plan {std::array<WriteRequest,kFieldCount> writes{};uint32_t count=0;};
inline bool Pending(const Ledger& l){for(const auto& e:l.entries)if(e.pending)return true;return false;}
inline void Release(Entry& e){e=Entry{};}
inline void Request(Field f,Action action,const Value& before,const Value& desired,Plan& p){p.writes[p.count++]={f,action,before,desired};}
inline void PlanPublish(const Values& current,const Desired& desired,Ledger* l,Plan* p){
    *p=Plan{};l->bound=true;
    for(uint32_t i=0;i<kFieldCount;++i){const auto f=Field(i);if(!desired.enabled[i]||!current[i].read)continue;auto& e=l->entries[i];
        const auto& now=current[i].value;
        if(e.pending&&!Equal(f,now,e.last_written)){Release(e);++l->preserved_native_changes;}
        if(MatchesDesired(f,now,desired.values[i]))continue;
        if(!e.captured){e.original=now;e.captured=true;}
        Request(f,Action::Publish,now,desired.values[i],*p);
    }
}
inline void PlanRestore(const Values& current,Ledger* l,Plan* p){
    *p=Plan{};
    for(uint32_t i=0;i<kFieldCount;++i){const auto f=Field(i);auto& e=l->entries[i];if(!e.pending||!current[i].read)continue;
        const auto& now=current[i].value;
        if(Equal(f,now,e.original)){Release(e);continue;}
        if(!Equal(f,now,e.last_written)){Release(e);++l->preserved_native_changes;continue;}
        Request(f,Action::Restore,now,e.original,*p);
    }
}
inline void MarkAttempt(Ledger* l,const WriteRequest& r){auto& e=l->entries[Index(r.field)];e.last_written=r.desired;e.pending=true;++l->attempts;}
inline Code Confirm(Ledger* l,const WriteRequest& r,const ReadValue& after,bool native_succeeded){
    auto& e=l->entries[Index(r.field)];
    if(!after.read)return Code::ReadbackFailed;
    if(Equal(r.field,after.value,r.desired)||(native_succeeded&&MatchesDesired(r.field,after.value,r.desired))){
        if(r.action==Action::Restore){Release(e);++l->confirmed_restores;}
        else {e.last_written=after.value;++l->confirmed_writes;}
        return Code::Ok;
    }
    // A failed restore may leave the previous complete stage value untouched;
    // keep ownership of it so a later restore can retry.
    if(r.action==Action::Restore&&Equal(r.field,after.value,r.before)){e.last_written=r.before;return Code::ReadbackFailed;}
    if(Equal(r.field,after.value,e.original))Release(e);
    return Code::ReadbackFailed;
}
}} // namespace wwp_playback::ww_stage_residency
