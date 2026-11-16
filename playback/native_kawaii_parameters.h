#pragma once
#include "../runtime/generated/GameCompat.generated.h"
#include <array>
#include <atomic>
#include <cmath>
#include <cstddef>
#include <cstdint>
#include <cstring>
#include <mutex>
#include <algorithm>
#include <vector>
#include "ue_memory.h"

namespace wwp_playback::kawaii_parameters {
// Layout from the settings updater and solver. The host verifies both
// functions and the calling evaluator before exposing the parameter controls.
namespace compat=ww_game_compat;
inline constexpr uint64_t kUpdateRva=compat::FunctionGroupUpdatePhysicsSettingsRva,kSimulateRva=compat::FunctionGroupSimulateRva,kApplyResultRva=compat::FunctionGroupApplyResultRva;
inline constexpr uint32_t kUpdateBytes=compat::FunctionGroupUpdatePhysicsSettingsBytes,kSimulateBytes=compat::FunctionGroupSimulateBytes,kApplyResultBytes=compat::FunctionGroupApplyResultBytes;
inline constexpr size_t kNodeSettings=compat::KawaiiSettingsOffset,kRefresh=compat::KawaiiSettingsRefreshOffset,kInitialized=compat::KawaiiSettingsInitializedOffset;
inline constexpr size_t kBoneArray=compat::KawaiiSimulatedBonesOffset,kBoneStride=compat::KawaiiSimulatedBoneBytes,kBoneSettings=compat::KawaiiSimulatedBoneSettingsOffset;
inline constexpr size_t kBoneIdentity=compat::KawaiiSimulatedBoneIdentityOffset;
inline constexpr std::array<unsigned,3> kFields={0,3,5};
inline constexpr unsigned kNoChain=UINT32_MAX;
using Settings=std::array<float,7>;
using Values=std::array<float,3>;
using Array=ue_memory::ArrayHead;
struct Sample {
    Values original{},effective_first{},effective_last{};
    uint64_t revision=0;
    uint32_t bones=0;
    bool read=false,effective_read=false;
};
struct Chain {
    unsigned root_slot=0;
    unsigned root_bone=0;
    Values requested={-1,-1,-1};
    uint64_t revision=0;
    Sample sampled;
};
struct BonePatch {
    uintptr_t cache=0;
    unsigned chain=0;
    Settings effective{};
};
struct Group {
    mutable std::mutex edit;
    std::mutex evaluation;
    Values requested={-1,-1,-1};
    uint64_t revision=0;
    Sample sampled;
    std::atomic<bool> restore_pending{false};
    Settings restore_settings{};
    uint8_t restore_refresh=0;
    std::vector<Chain> chains;
    std::vector<unsigned short> skeleton_chain;
    std::vector<BonePatch> patches;
};
inline Values Select(const Settings& values) {
    return {values[kFields[0]],values[kFields[1]],values[kFields[2]]};
}
inline bool Finite(const Values& values) {
    for(float value:values)if(!std::isfinite(value))return false;
    return true;
}
inline bool ValidArray(const Array& a) {
    return a.num>=0&&a.num<=1024&&a.max>=a.num&&a.max<=8192&&(!a.num||a.data>0x10000);
}
inline bool SameArray(const Array& a,const Array& b) {
    return a.data==b.data&&a.num==b.num&&a.max==b.max;
}
inline bool HasOverride(const Values& values) {
    return std::any_of(values.begin(),values.end(),[](float value){return value>=0;});
}
inline Settings WithOverrides(Settings settings,const Values& values) {
    for(unsigned i=0;i<values.size();++i)if(values[i]>=0)settings[kFields[i]]=values[i];
    return settings;
}
inline int32_t SkeletonIndex(const std::array<int32_t,2>& identity,const std::array<int32_t,1024>& compact_to_skeleton) {
    if(identity[0]<0){
        // The signed 31-bit reference index is resolved by GroupApplyResult.
        const uint32_t bits=uint32_t(identity[0])&0x7fffffffu;
        return bits&0x40000000u?int32_t(bits)-0x7fffffff-1:int32_t(bits);
    }
    return identity[1]>=0 && size_t(identity[1])<compact_to_skeleton.size()?compact_to_skeleton[size_t(identity[1])]:-1;
}
template<class Access>
bool MapChains(Group& group,uintptr_t node,const Access& access,const std::array<int32_t,1024>& compact_to_skeleton) {
    Array bones{},after{};uint8_t initialized=0;
    group.patches.clear();
    if(!access.Get(node+kInitialized,initialized)||initialized!=1||!access.Get(node+kBoneArray,bones)||!ValidArray(bones)||!bones.num)return false;
    bool matched=false;
    for(int32_t i=0;i<bones.num;++i){
        const auto record=bones.data+size_t(i)*kBoneStride;
        std::array<int32_t,2> identity{};
        if(!access.Get(record+kBoneIdentity,identity))return false;
        const auto bone=SkeletonIndex(identity,compact_to_skeleton);
        const unsigned chain=bone>=0&&size_t(bone)<group.skeleton_chain.size()?group.skeleton_chain[size_t(bone)]:0;
        matched|=chain!=0;
        group.patches.push_back({record+kBoneSettings,chain?chain-1:kNoChain,{}});
    }
    return matched&&access.Get(node+kBoneArray,after)&&SameArray(bones,after);
}
template<class Access> bool ReadSettingsCache(const Access& access,uintptr_t node,Sample& sample) {
    Array bones{},after{};uint8_t initialized=0;
    if(!access.Get(node+kInitialized,initialized)||initialized!=1||!access.Get(node+kBoneArray,bones)||
       !ValidArray(bones)||!bones.num)return false;
    Settings first{},last{};
    if(!access.Get(bones.data+kBoneSettings,first)||
       !access.Get(bones.data+kBoneSettings+size_t(bones.num-1)*kBoneStride,last)||
       !access.Get(node+kBoneArray,after)||!SameArray(bones,after))return false;
    sample.bones=unsigned(bones.num);sample.effective_first=Select(first);sample.effective_last=Select(last);
    return Finite(sample.effective_first)&&Finite(sample.effective_last);
}
template<class Access,class Refresh> bool Restore(Group& group,uintptr_t node,const Access& access,Refresh&& refresh) {
    Array bones{};Settings actual{};uint8_t actual_refresh=0;
    if(!access.write(access.user,node+kNodeSettings,&group.restore_settings,sizeof(Settings))||
       !access.write(access.user,node+kRefresh,&group.restore_refresh,1)||
       !access.Get(node+kNodeSettings,actual)||std::memcmp(actual.data(),group.restore_settings.data(),sizeof(Settings))||
       !access.Get(node+kRefresh,actual_refresh)||actual_refresh!=group.restore_refresh||
       !access.Get(node+kBoneArray,bones)||!ValidArray(bones)||(bones.num&&!refresh(node)))return false;
    group.restore_pending.store(false,std::memory_order_release);return true;
}

// All touches are within one registered node's evaluation. The game rebuilds
// the effective cache with its own curve logic, both on apply and on restore.
template<class Access,class Evaluate,class Refresh,class Fault,class Map>
void EvaluateGroup(Group& group,uintptr_t node,const Access& access,bool feed,Evaluate&& evaluate,Refresh&& refresh,Fault&& fault,Map&& map) {
    std::lock_guard<std::mutex> evaluation(group.evaluation);
    if(group.restore_pending.load(std::memory_order_acquire)&&!Restore(group,node,access,refresh)){
        fault();evaluate();return;
    }
    Values requested;Sample sample;
    struct Request {Values values;Sample sample;};
    std::vector<Request> chains;
    {std::lock_guard<std::mutex> edit(group.edit);requested=group.requested;sample.revision=group.revision;
        if(std::any_of(group.chains.begin(),group.chains.end(),[](const auto& chain){return chain.revision!=0;})){
            chains.reserve(group.chains.size());
            for(const auto& chain:group.chains){Request request{chain.requested,{}};request.sample.revision=chain.revision;chains.push_back(request);}
        }
    }
    Settings original{};uint8_t refresh_flag=0;
    sample.read=access.Get(node+kNodeSettings,original)&&Finite(Select(original));
    sample.original=Select(original);
    const bool mapped=!chains.empty()&&map();
    const bool chain_overrides=feed&&sample.read&&mapped&&std::any_of(group.patches.begin(),group.patches.end(),[&](const auto& patch){
        return patch.chain!=kNoChain&&HasOverride(chains[patch.chain].values);});
    const bool group_override=feed&&sample.read&&HasOverride(requested);
    const bool changed=chain_overrides||group_override;
    const Settings input=group_override?WithOverrides(original,requested):original;
    if(changed){
        if(!access.Get(node+kRefresh,refresh_flag)||refresh_flag>1||
           !access.writable(access.user,node+kNodeSettings,sizeof(Settings))||!access.writable(access.user,node+kRefresh,1)){
            fault();evaluate();return;
        }
        group.restore_settings=original;group.restore_refresh=refresh_flag;group.restore_pending.store(true,std::memory_order_release);
        const uint8_t update=chain_overrides?0:1;
        if(!access.write(access.user,node+kNodeSettings,&input,sizeof(Settings))||
           (refresh_flag!=update&&!access.write(access.user,node+kRefresh,&update,1))){
            Restore(group,node,access,refresh);fault();evaluate();return;
        }
        if(chain_overrides){
            bool ok=refresh(node);
            for(auto& patch:group.patches)ok=ok&&access.Get(patch.cache,patch.effective);
            for(unsigned i=0;ok&&i<chains.size();++i){
                if(!HasOverride(chains[i].values))continue;
                const auto patch_input=WithOverrides(input,chains[i].values);
                ok=access.write(access.user,node+kNodeSettings,&patch_input,sizeof(Settings))&&refresh(node);
                for(auto& patch:group.patches)if(ok&&patch.chain==i)ok=access.Get(patch.cache,patch.effective);
            }
            ok=ok&&access.write(access.user,node+kNodeSettings,&input,sizeof(Settings));
            for(const auto& patch:group.patches){
                ok=ok&&access.writable(access.user,patch.cache,sizeof(Settings))&&access.write(access.user,patch.cache,&patch.effective,sizeof(Settings));
            }
            if(!ok){Restore(group,node,access,refresh);fault();evaluate();return;}
        }
    }
    struct Recovery {
        Group& group;uintptr_t node;const Access& access;Refresh& refresh;Fault& fault;bool changed;
        ~Recovery(){if(changed&&!Restore(group,node,access,refresh))fault();}
    };
    {
        Recovery recovery{group,node,access,refresh,fault,changed};
        evaluate();
        sample.effective_read=ReadSettingsCache(access,node,sample);
        if(!chains.empty()&&map())for(const auto& patch:group.patches){
            if(patch.chain==kNoChain)continue;
            auto& current=chains[patch.chain].sample;Settings value{};
            if(!access.Get(patch.cache,value)||!Finite(Select(value)))continue;
            if(!current.effective_read){current.effective_first=Select(value);current.effective_read=true;}
            current.effective_last=Select(value);current.read=sample.read;current.original=sample.original;++current.bones;
        }
    }
    if(changed&&!sample.effective_read)fault();
    {std::lock_guard<std::mutex> edit(group.edit);group.sampled=sample;
        for(unsigned i=0;i<chains.size();++i)if(chains[i].sample.effective_read)group.chains[i].sampled=chains[i].sample;
    }
}
}
