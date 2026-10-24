#pragma once
// Fixed-capacity prop visibility lifecycle. No engine/API calls except supplied
// adapter; no heap, strings, locks or files in Update.
// Adapter: bool Read(address,void*,size); bool Identify(address,Identity*);
// bool SetHidden(address,bool hidden) bound to the native setter with
// NoPropagation. Identify returns true+present=false only when
// destruction/replacement is positively known, false for unreadable.
#include <array>
#include <cstddef>
#include <cstdint>
#include <cstring>

namespace wwp_playback { namespace ww_prop_visibility {
constexpr uint32_t kMaxTargets=16,kUnset=UINT32_MAX;
struct Identity {
    uint64_t address=0,item_address=0,class_address=0,vtable=0,outer_address=0;
    uint32_t index=kUnset,serial=0;
    bool present=true;
};
struct Prepared {std::array<Identity,kMaxTargets> targets{};uint32_t count=0;};
struct Entry {bool captured=false,original_hidden=false,ours=false,attempted=false,retired=false;};
struct State {
    std::array<Entry,kMaxTargets> entries{};
    bool active_before=false,pending=false;
};
enum class Code:uint8_t {Ok,ReadFailed,IdentityChanged,SetterFailed};
struct Result {Code code=Code::Ok;uint32_t target=kUnset;};
inline bool Same(const Identity& a,const Identity& b){
    return a.address==b.address&&a.item_address==b.item_address&&a.class_address==b.class_address&&
        a.vtable==b.vtable&&a.outer_address==b.outer_address&&a.index==b.index&&a.serial==b.serial&&b.present;
}
inline bool Pending(const Prepared& p,const State& s){
    for(uint32_t i=0;i<p.count;++i)if(s.entries[i].captured&&!s.entries[i].retired&&(s.entries[i].ours||s.entries[i].attempted))return true;
    return false;
}
template<class Adapter> Code Sample(Adapter& a,const Identity& t,bool* hidden,bool* retired){
    *retired=false;Identity component{};uint8_t flags=0;
    if(!a.Identify(t.address,&component))return Code::ReadFailed;
    if(!Same(t,component)){*retired=true;return Code::IdentityChanged;}
    if(!a.Read(t.address+0x16f,&flags,1))return Code::ReadFailed;
    *hidden=(flags&0x10)!=0;return Code::Ok;
}
template<class Adapter> Result Update(Adapter& a,const Prepared& p,bool active,State* state) noexcept {
    Result out;
    // Capture every original state before activation mutates any target.
    if(active&&!state->active_before){
        std::array<bool,kMaxTargets> originals{};
        for(uint32_t i=0;i<p.count;++i){
            bool retired=false;const auto code=Sample(a,p.targets[i],&originals[i],&retired);
            if(code!=Code::Ok){out.code=code;out.target=i;state->pending=Pending(p,*state);return out;}
        }
        for(uint32_t i=0;i<p.count;++i){state->entries[i]=Entry{};state->entries[i].captured=true;state->entries[i].original_hidden=originals[i];}
        state->active_before=true;
    }
    for(uint32_t i=0;i<p.count;++i){
        Entry& e=state->entries[i];if(!e.captured||e.retired)continue;
        bool hidden=false,retired=false;const auto code=Sample(a,p.targets[i],&hidden,&retired);
        if(retired){e.ours=false;e.attempted=false;e.retired=true;continue;}
        if(code!=Code::Ok){if(out.code==Code::Ok){out.code=code;out.target=i;}continue;}
        const uint64_t address=p.targets[i].address;
        if(active){
            if(hidden){if(e.attempted){e.ours=true;e.attempted=false;}continue;}
            // Game recomputed false since our previous hide. New activation
            // is ours, but at stop a subsequent false is preserved.
            e.ours=false;e.attempted=true;
            if(a.SetHidden(address,true)){e.ours=true;e.attempted=false;}
            else if(out.code==Code::Ok){out.code=Code::SetterFailed;out.target=i;}
        } else {
            if(!e.ours&&!e.attempted){e.captured=false;continue;}
            // Native state already replaced our true, or the baseline equals it.
            if(!hidden||e.original_hidden){e.ours=false;e.attempted=false;e.captured=false;continue;}
            if(a.SetHidden(address,false)){e.ours=false;e.attempted=false;e.captured=false;}
            else if(out.code==Code::Ok){out.code=Code::SetterFailed;out.target=i;}
        }
    }
    if(!active&&!Pending(p,*state))state->active_before=false;
    state->pending=Pending(p,*state);return out;
}
} } // namespace wwp_playback::ww_prop_visibility
