#pragma once

#include "native_kawaii_parameters.h"

namespace wwp_playback::kawaii_collision {
namespace parameters=kawaii_parameters;
namespace compat=ww_game_compat;
inline constexpr size_t kCapsuleArrays[]={compat::KawaiiCapsulesAOffset,compat::KawaiiCapsulesBOffset};
inline constexpr size_t kCapsuleIdentity=compat::KawaiiCapsuleIdentityOffset,kCapsuleBytes=compat::KawaiiCapsuleBytes;
// Verified in GroupUpdateCapsuleLimits and the capsule contact functions.
inline constexpr size_t kOffsetLocation=compat::KawaiiCapsuleLocationOffset,kOffsetRotation=compat::KawaiiCapsuleRotationOffset,
    kRadius=compat::KawaiiCapsuleRadiusOffset,kLength=compat::KawaiiCapsuleLengthOffset;
enum Field : unsigned {Radius, Length, OffsetX, OffsetY, OffsetZ, Fields};
struct alignas(16) Capsule {
    std::array<uint8_t,kCapsuleBytes> bytes;
};
static_assert(sizeof(Capsule)==kCapsuleBytes,"verified capsule stride");
struct Driver {
    unsigned bone=0;
    unsigned original=0,active=0;
    bool enabled=true;
};
struct Shape {
    unsigned id=0;
    int32_t bone=-1;
    bool enabled=true,active=true;
    bool geometry_available=false;
    std::array<float,Fields> baseline{},requested{};
    std::array<float,3> rotation{};
    unsigned edited=0;
};
inline constexpr std::array<size_t,Fields> kFieldOffsets{kRadius,kLength,kOffsetLocation,kOffsetLocation+4,kOffsetLocation+8};
inline bool ReadGeometry(const Capsule& capsule,Shape& shape){
    for(unsigned i=0;i<Fields;++i)std::memcpy(&shape.baseline[i],capsule.bytes.data()+kFieldOffsets[i],sizeof(float));
    std::memcpy(shape.rotation.data(),capsule.bytes.data()+kOffsetRotation,sizeof(shape.rotation));
    return shape.baseline[Radius]>=0&&shape.baseline[Radius]<=100&&shape.baseline[Length]>=0&&shape.baseline[Length]<=300&&
        std::all_of(shape.baseline.begin()+OffsetX,shape.baseline.end(),[](float value){return std::isfinite(value)&&std::abs(value)<=300;})&&
        std::all_of(shape.rotation.begin(),shape.rotation.end(),[](float value){return std::isfinite(value)&&std::abs(value)<=3600;});
}
inline bool SameShape(const Shape& a,const Shape& b){return a.id==b.id&&a.bone==b.bone&&a.baseline==b.baseline&&a.rotation==b.rotation;}
inline std::array<float,Fields> Effective(const Shape& shape){
    auto values=shape.baseline;
    for(unsigned i=0;i<Fields;++i)if(shape.edited&(1u<<i))values[i]=shape.requested[i];
    return values;
}
struct Buffer {
    parameters::Array original{},filtered{};
    std::vector<Capsule> records;
    bool swapped=false;
};
struct State {
    mutable std::mutex edit;
    std::vector<Driver> drivers;
    std::vector<Shape> shapes;
    bool available=false;
    std::array<Buffer,2> buffers;
    std::atomic<bool> restore_pending{false};
};

template<class Access> bool Restore(State& state,uintptr_t node,const Access& access) {
    bool all=true;
    for(unsigned i=0;i<state.buffers.size();++i){
        auto& buffer=state.buffers[i];if(!buffer.swapped)continue;
        const auto address=node+kCapsuleArrays[i];parameters::Array current{};
        if(access.Get(address,current)&&(parameters::SameArray(current,buffer.original)||
            (parameters::SameArray(current,buffer.filtered)&&access.write(access.user,address,&buffer.original,sizeof(buffer.original))&&
             access.Get(address,current)&&parameters::SameArray(current,buffer.original))))buffer.swapped=false;
        all=all&&!buffer.swapped;
    }
    state.restore_pending.store(!all,std::memory_order_release);return all;
}

template<class Access,class Map,class Evaluate,class Fault>
void EvaluateCapsules(State& state,uintptr_t node,const Access& access,bool feed,Map&& map,Evaluate&& evaluate,Fault&& fault) {
    if(state.restore_pending.load(std::memory_order_acquire)&&!Restore(state,node,access)){fault();evaluate();return;}
    std::vector<Driver> requested;std::vector<Shape> geometry;bool available=false;
    {std::lock_guard<std::mutex> lock(state.edit);requested=state.drivers;geometry=state.shapes;available=state.available;}
    const bool edits=feed&&(std::any_of(requested.begin(),requested.end(),[](const auto& d){return !d.enabled;})||
        std::any_of(geometry.begin(),geometry.end(),[](const auto& shape){return !shape.enabled||shape.edited;}));
    if(available&&!edits){
        evaluate();
        std::lock_guard<std::mutex> lock(state.edit);for(auto& driver:state.drivers)driver.active=driver.original;
        for(auto& shape:state.shapes)shape.active=true;
        return;
    }
    parameters::Array simulation{};
    std::array<int32_t,1024> compact_to_skeleton{};
    if(!access.Get(node+parameters::kBoneArray,simulation)||!parameters::ValidArray(simulation)||!simulation.num||!map(compact_to_skeleton)){
        evaluate();return;
    }
    std::vector<Driver> observed;
    std::vector<Shape> shapes;
    std::array<bool,2> changed{};
    for(unsigned i=0;i<state.buffers.size();++i){
        auto& buffer=state.buffers[i];buffer.records.clear();
        if(!access.Get(node+kCapsuleArrays[i],buffer.original)||!parameters::ValidArray(buffer.original)){evaluate();return;}
        if(edits)buffer.records.reserve(size_t(buffer.original.num));
        for(int32_t index=0;index<buffer.original.num;++index){
            Capsule record{};
            if(!access.Get(buffer.original.data+size_t(index)*kCapsuleBytes,record)){evaluate();return;}
            std::array<int32_t,2> identity{};
            std::memcpy(identity.data(),record.bytes.data()+kCapsuleIdentity,sizeof(identity));
            const auto bone=parameters::SkeletonIndex(identity,compact_to_skeleton);
            Shape shape;
            shape.id=(i<<16)+unsigned(index)+1;
            shape.bone=bone;
            shape.geometry_available=ReadGeometry(record,shape);
            const auto prior=std::find_if(geometry.begin(),geometry.end(),[&](const auto& entry){return SameShape(entry,shape);});
            bool disabled=edits&&prior!=geometry.end()&&!prior->enabled;
            if(bone>=0){
                const auto setting=std::find_if(requested.begin(),requested.end(),[&](const auto& d){return d.bone==unsigned(bone);});
                disabled=disabled||(edits&&setting!=requested.end()&&!setting->enabled);
                auto driver=std::find_if(observed.begin(),observed.end(),[&](const auto& d){return d.bone==unsigned(bone);});
                if(driver==observed.end())observed.push_back({unsigned(bone),1,disabled?0u:1u,true});
                else {++driver->original;if(!disabled)++driver->active;}
            }
            if(edits&&prior!=geometry.end()&&shape.geometry_available&&prior->edited&&!disabled){
                for(unsigned field=0;field<Fields;++field)if(prior->edited&(1u<<field))
                    std::memcpy(record.bytes.data()+kFieldOffsets[field],&prior->requested[field],sizeof(float));
                changed[i]=true;
            }
            shape.active=!disabled;shapes.push_back(shape);
            if(edits&&!disabled)buffer.records.push_back(record);
        }
    }
    for(unsigned i=0;edits&&i<state.buffers.size();++i){
        auto& buffer=state.buffers[i];
        if(!changed[i]&&buffer.records.size()==size_t(buffer.original.num))continue;
        const auto address=node+kCapsuleArrays[i];parameters::Array current{};
        if(!access.Get(address,current)||!parameters::SameArray(current,buffer.original)||!access.writable(access.user,address,sizeof(current))){Restore(state,node,access);fault();evaluate();return;}
        const auto count=int32_t(buffer.records.size());
        buffer.filtered={count?reinterpret_cast<uintptr_t>(buffer.records.data()):0,count,count};
        buffer.swapped=true;state.restore_pending.store(true,std::memory_order_release);
        if(!access.write(access.user,address,&buffer.filtered,sizeof(buffer.filtered))){Restore(state,node,access);fault();evaluate();return;}
    }
    struct Recovery {
        State& state;uintptr_t node;const Access& access;Fault& fault;
        ~Recovery(){if(state.restore_pending.load(std::memory_order_acquire)&&!Restore(state,node,access))fault();}
    };
    {Recovery recovery{state,node,access,fault};evaluate();}
    std::lock_guard<std::mutex> lock(state.edit);
    for(auto& driver:observed){const auto current=std::find_if(state.drivers.begin(),state.drivers.end(),[&](const auto& d){return d.bone==driver.bone;});
        if(current!=state.drivers.end())driver.enabled=current->enabled;}
    for(auto& shape:shapes){
        const auto current=std::find_if(state.shapes.begin(),state.shapes.end(),[&](const auto& entry){return SameShape(entry,shape);});
        if(current!=state.shapes.end()){shape.enabled=current->enabled;shape.requested=current->requested;shape.edited=current->edited;}
    }
    state.shapes=std::move(shapes);
    state.drivers=std::move(observed);state.available=true;
}
}
