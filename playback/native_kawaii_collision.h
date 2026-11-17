#pragma once

#include "native_kawaii_parameters.h"
#include "scoped_restore.h"
#include <chrono>
#include <optional>

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
    bool baseline_enabled=true;
    bool geometry_available=false;
    std::array<float,Fields> baseline{},requested{};
    std::array<float,3> rotation{};
    unsigned edited=0;
};
inline constexpr std::array<size_t,Fields> kFieldOffsets{kRadius,kLength,kOffsetLocation,kOffsetLocation+4,kOffsetLocation+8};
inline bool ValidGeometry(const Shape& shape){
    return shape.baseline[Radius]>=0&&shape.baseline[Radius]<=100&&shape.baseline[Length]>=0&&shape.baseline[Length]<=300&&
        std::all_of(shape.baseline.begin()+OffsetX,shape.baseline.end(),[](float value){return std::isfinite(value)&&std::abs(value)<=300;})&&
        std::all_of(shape.rotation.begin(),shape.rotation.end(),[](float value){return std::isfinite(value)&&std::abs(value)<=3600;});
}
inline bool ReadGeometry(const Capsule& capsule,Shape& shape){
    for(unsigned i=0;i<Fields;++i)std::memcpy(&shape.baseline[i],capsule.bytes.data()+kFieldOffsets[i],sizeof(float));
    std::memcpy(shape.rotation.data(),capsule.bytes.data()+kOffsetRotation,sizeof(shape.rotation));
    return ValidGeometry(shape);
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
constexpr unsigned kArraySpan=1u<<16;
constexpr unsigned kFirstAuthoredId=2*kArraySpan+1;
constexpr size_t kMaxCapsulesPerArray=1024;
struct AuthoredShape {
    Shape shape;
    unsigned array=0;
    unsigned source_id=0;
};
struct Layout {
    bool replace_stock=true;
    std::vector<AuthoredShape> shapes;
    std::optional<std::vector<AuthoredShape>> stock_baseline;
};
struct StockShape {
    Capsule record;
    Shape shape;
};
// Initialized groups use the same verified capsule evaluator. A sibling
// template may be reused only when the bytes outside its known value fields
// are zero, so no group-local ancillary state crosses that boundary.
struct PrototypeBank {
    std::mutex edit;
    std::array<std::optional<StockShape>,2> records;
    std::atomic<bool> available{false};
};
struct State {
    mutable std::mutex edit;
    std::vector<Driver> drivers;
    std::vector<Shape> shapes;
    bool available=false;
    std::optional<Layout> layout;
    PrototypeBank* prototype_bank=nullptr;
    std::chrono::steady_clock::time_point layout_wait_started{};
    bool layout_applied=false;
    const char* layout_error=nullptr;
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

inline StockShape ReadStockShape(Capsule record,unsigned id,const std::array<int32_t,1024>& compact_to_skeleton){
    std::array<int32_t,2> identity{};
    std::memcpy(identity.data(),record.bytes.data()+kCapsuleIdentity,sizeof(identity));
    StockShape stock{record,{}};
    stock.shape.id=id;
    stock.shape.bone=parameters::SkeletonIndex(identity,compact_to_skeleton);
    stock.shape.geometry_available=ReadGeometry(record,stock.shape);
    return stock;
}
inline constexpr size_t kCachedLocation=kOffsetRotation+3*sizeof(float);
inline constexpr size_t kCachedRotation=kRadius-4*sizeof(float);
static_assert(kOffsetLocation==kCapsuleIdentity+2*sizeof(int32_t)&&kOffsetRotation==kOffsetLocation+3*sizeof(float)&&
    kCachedLocation+3*sizeof(float)<=kCachedRotation&&kLength==kRadius+sizeof(float)&&kLength+sizeof(float)<=kCapsuleBytes,
    "capsule template requires the verified value-field layout");
inline bool PortablePrototype(const StockShape& source){
    const auto& bytes=source.record.bytes;
    const auto zero=[](uint8_t value){return value==0;};
    return source.shape.geometry_available&&
        std::all_of(bytes.begin()+kCachedLocation+3*sizeof(float),bytes.begin()+kCachedRotation,zero)&&
        std::all_of(bytes.begin()+kLength+sizeof(float),bytes.end(),zero);
}
inline void OfferPrototype(PrototypeBank& bank,unsigned array,const StockShape& source){
    std::lock_guard<std::mutex> lock(bank.edit);
    if(!bank.records[array]){
        bank.records[array]=source;
        bank.available.store(true,std::memory_order_release);
    }
}
inline std::optional<StockShape> SiblingPrototype(PrototypeBank* bank,unsigned array){
    if(!bank)return {};
    std::lock_guard<std::mutex> lock(bank->edit);
    return bank->records[array]?bank->records[array]:bank->records[1-array];
}
inline const StockShape* FindPrototype(const std::array<std::vector<StockShape>,2>& stock,const AuthoredShape& row){
    if(row.source_id){
        const auto array=(row.source_id-1)/kArraySpan,index=(row.source_id-1)%kArraySpan;
        if(array<stock.size()&&index<stock[array].size()){
            const auto& source=stock[array][index];
            if(source.shape.geometry_available&&source.shape.bone==row.shape.bone&&array==row.array)return &source;
        }
    }
    const auto find=[&](unsigned array,bool same_bone)->const StockShape*{
        const auto& list=stock[array];
        const auto match=std::find_if(list.begin(),list.end(),[&](const auto& source){
            return source.shape.geometry_available&&(!same_bone||source.shape.bone==row.shape.bone);
        });
        return match==list.end()?nullptr:&*match;
    };
    if(const auto* result=find(row.array,true))return result;
    if(const auto* result=find(1-row.array,true))return result;
    if(const auto* result=find(row.array,false))return result;
    return find(1-row.array,false);
}
inline Capsule AuthoredRecord(const StockShape& source,const Shape& shape,bool sibling){
    auto record=source.record;
    if(sibling||source.shape.bone!=shape.bone){
        // The verified evaluator resolves a negative marker as a signed
        // 31-bit skeleton index. Index -1 selects the component transform.
        const std::array<uint32_t,2> identity{0x80000000u|(uint32_t(shape.bone)&0x7fffffffu),0xffffffffu};
        std::fill_n(record.bytes.begin(),kCapsuleIdentity,uint8_t{});
        std::memcpy(record.bytes.data()+kCapsuleIdentity,identity.data(),sizeof(identity));
    }
    if(sibling){
        // GroupEvaluate refreshes these values for both arrays before its
        // contact routines consume them. Do not retain the sibling's cache.
        const std::array<float,3> location{};
        const std::array<float,4> rotation{0,0,0,1};
        std::memcpy(record.bytes.data()+kCachedLocation,location.data(),sizeof(location));
        std::memcpy(record.bytes.data()+kCachedRotation,rotation.data(),sizeof(rotation));
    }
    const auto effective=Effective(shape);
    for(unsigned i=0;i<Fields;++i)std::memcpy(record.bytes.data()+kFieldOffsets[i],&effective[i],sizeof(float));
    std::memcpy(record.bytes.data()+kOffsetRotation,shape.rotation.data(),sizeof(shape.rotation));
    return record;
}
inline bool MatchesStock(const std::vector<AuthoredShape>& baseline,const std::array<std::vector<StockShape>,2>& stock){
    if(baseline.size()!=stock[0].size()+stock[1].size())return false;
    for(const auto& row:baseline){
        const auto index=(row.source_id-1)%kArraySpan;
        if(index>=stock[row.array].size())return false;
        const auto& shape=stock[row.array][index].shape;
        if(!shape.geometry_available||shape.bone!=row.shape.bone)return false;
        for(unsigned i=0;i<Fields;++i)if(std::abs(shape.baseline[i]-row.shape.baseline[i])>.02f)return false;
        for(unsigned i=0;i<shape.rotation.size();++i)if(std::abs(std::remainder(shape.rotation[i]-row.shape.rotation[i],360.f))>.02f)return false;
    }
    return true;
}
inline bool WaitForLayout(State& state,const char* error){
    std::lock_guard<std::mutex> lock(state.edit);
    const auto now=std::chrono::steady_clock::now();
    state.layout_applied=false;
    if(state.layout_wait_started==std::chrono::steady_clock::time_point{})state.layout_wait_started=now;
    if(now-state.layout_wait_started<std::chrono::seconds(2))return true;
    state.layout_error=error;
    return false;
}

template<class Access,class Map,class Evaluate,class Fault>
void EvaluateCapsules(State& state,uintptr_t node,const Access& access,bool feed,Map&& map,Evaluate&& evaluate,Fault&& fault) {
    if(state.restore_pending.load(std::memory_order_acquire)&&!Restore(state,node,access)){fault();evaluate();return;}
    std::vector<Driver> requested;std::vector<Shape> geometry;bool available=false;
    {std::lock_guard<std::mutex> lock(state.edit);requested=state.drivers;geometry=state.shapes;available=state.available;}
    const bool authoring=feed&&state.layout.has_value();
    const auto unavailable=[&](const char* reason){
        if(authoring&&!WaitForLayout(state,reason))fault();
        evaluate();
    };
    const bool edits=feed&&(authoring||std::any_of(requested.begin(),requested.end(),[](const auto& d){return !d.enabled;})||
        std::any_of(geometry.begin(),geometry.end(),[](const auto& shape){return !shape.enabled||shape.edited;}));
    const bool needs_prototype=state.prototype_bank&&!state.prototype_bank->available.load(std::memory_order_acquire);
    uint8_t initialized=0;
    const bool initialized_stock=(authoring||needs_prototype)&&access.Get(node+parameters::kInitialized,initialized)&&initialized==1;
    if(available&&!edits&&(!needs_prototype||!initialized_stock)){
        evaluate();
        std::lock_guard<std::mutex> lock(state.edit);for(auto& driver:state.drivers)driver.active=driver.original;
        for(auto& shape:state.shapes)shape.active=shape.id<kFirstAuthoredId;
        return;
    }
    if(authoring&&!initialized_stock){
        unavailable("capsule layout requires an initialized native physics group");return;
    }
    parameters::Array simulation{};
    std::array<int32_t,1024> compact_to_skeleton{};
    if(!access.Get(node+parameters::kBoneArray,simulation)||!parameters::ValidArray(simulation)||!simulation.num||!map(compact_to_skeleton)){
        unavailable("capsule layout requires an available native skeleton pose");return;
    }
    std::vector<Driver> observed;
    std::vector<Shape> shapes;
    std::array<bool,2> changed{};
    std::array<std::vector<StockShape>,2> stock;
    const bool replace=authoring&&state.layout->replace_stock;
    const auto prior_shape=[&](const Shape& shape){
        return std::find_if(geometry.begin(),geometry.end(),[&](const auto& entry){return SameShape(entry,shape);});
    };
    const auto observe_driver=[&](const Shape& shape,bool original,bool active){
        if(shape.bone<0)return;
        const auto bone=unsigned(shape.bone);
        auto driver=std::find_if(observed.begin(),observed.end(),[&](const auto& d){return d.bone==bone;});
        if(driver==observed.end())observed.push_back({bone,original?1u:0u,active?1u:0u,true});
        else {if(original)++driver->original;if(active)++driver->active;}
    };
    const auto driver_enabled=[&](int32_t bone){
        if(!edits||bone<0)return true;
        const auto setting=std::find_if(requested.begin(),requested.end(),[&](const auto& d){return d.bone==unsigned(bone);});
        return setting==requested.end()||setting->enabled;
    };
    for(unsigned i=0;i<state.buffers.size();++i){
        auto& buffer=state.buffers[i];buffer.records.clear();
        if(!access.Get(node+kCapsuleArrays[i],buffer.original)||!parameters::ValidArray(buffer.original)){
            unavailable("capsule layout could not read original capsule arrays");return;
        }
        if(edits)buffer.records.reserve(size_t(buffer.original.num));
        bool offered=false;
        for(int32_t index=0;index<buffer.original.num;++index){
            Capsule record{};
            if(!access.Get(buffer.original.data+size_t(index)*kCapsuleBytes,record)){
                unavailable("capsule layout could not read an original capsule record");return;
            }
            const auto original=ReadStockShape(record,i*kArraySpan+unsigned(index)+1,compact_to_skeleton);
            if(needs_prototype&&initialized_stock&&!offered&&PortablePrototype(original)){
                OfferPrototype(*state.prototype_bank,i,original);
                offered=true;
            }
            if(authoring)stock[i].push_back(original);
            auto shape=original.shape;
            if(replace){observe_driver(shape,true,false);continue;}
            const auto prior=prior_shape(shape);
            const bool disabled=!driver_enabled(shape.bone)||(edits&&prior!=geometry.end()&&!prior->enabled);
            observe_driver(shape,true,!disabled);
            if(edits&&prior!=geometry.end()&&shape.geometry_available&&prior->edited&&!disabled){
                for(unsigned field=0;field<Fields;++field)if(prior->edited&(1u<<field))
                    std::memcpy(record.bytes.data()+kFieldOffsets[field],&prior->requested[field],sizeof(float));
                changed[i]=true;
            }
            shape.active=!disabled;shapes.push_back(shape);
            if(edits&&!disabled)buffer.records.push_back(record);
        }
    }
    if(authoring){
        const char* error=nullptr;
        if(const auto& baseline=state.layout->stock_baseline;replace&&baseline&&!MatchesStock(*baseline,stock)){
            unavailable("capsule layout stock baseline does not match this actor's original runtime capsules");return;
        }
        for(const auto& row:state.layout->shapes){
            auto shape=row.shape;
            const auto prior=prior_shape(shape);
            if(prior!=geometry.end()){
                shape.enabled=prior->enabled;shape.requested=prior->requested;shape.edited=prior->edited;
            }
            shape.active=shape.enabled&&driver_enabled(shape.bone);
            observe_driver(shape,false,shape.active);
            shapes.push_back(shape);
            if(!shape.active)continue;
            auto& buffer=state.buffers[row.array];
            if(buffer.records.size()>=kMaxCapsulesPerArray){error="capsule layout exceeds the native array limit";break;}
            const auto* source=FindPrototype(stock,row);
            const auto sibling=source?std::optional<StockShape>{}:SiblingPrototype(state.prototype_bank,row.array);
            if(!source&&!sibling){error="capsule layout requires a compatible stock capsule template on this actor";break;}
            if(shape.bone>=0&&std::find(compact_to_skeleton.begin(),compact_to_skeleton.end(),shape.bone)==compact_to_skeleton.end()){
                error="capsule layout driver bone is unavailable in the active physics pose";break;
            }
            buffer.records.push_back(AuthoredRecord(source?*source:*sibling,shape,!source));
            changed[row.array]=true;
        }
        if(error){unavailable(error);return;}
    }
    const auto failed_swap=[&]{Restore(state,node,access);fault();evaluate();};
    for(unsigned i=0;edits&&i<state.buffers.size();++i){
        auto& buffer=state.buffers[i];
        if(!changed[i]&&buffer.records.size()==size_t(buffer.original.num))continue;
        const auto address=node+kCapsuleArrays[i];parameters::Array current{};
        if(!access.Get(address,current)||!parameters::SameArray(current,buffer.original)||!access.writable(access.user,address,sizeof(current))){failed_swap();return;}
        const auto count=int32_t(buffer.records.size());
        buffer.filtered={count?reinterpret_cast<uintptr_t>(buffer.records.data()):0,count,count};
        buffer.swapped=true;state.restore_pending.store(true,std::memory_order_release);
        if(!access.write(access.user,address,&buffer.filtered,sizeof(buffer.filtered))){failed_swap();return;}
    }
    {
        ScopedRestore recovery([&]{if(state.restore_pending.load(std::memory_order_acquire)&&!Restore(state,node,access))fault();});
        evaluate();
    }
    std::lock_guard<std::mutex> lock(state.edit);
    for(auto& driver:observed){const auto current=std::find_if(state.drivers.begin(),state.drivers.end(),[&](const auto& d){return d.bone==driver.bone;});
        if(current!=state.drivers.end())driver.enabled=current->enabled;}
    for(auto& shape:shapes){
        const auto current=std::find_if(state.shapes.begin(),state.shapes.end(),[&](const auto& entry){return SameShape(entry,shape);});
        if(current!=state.shapes.end()){shape.enabled=current->enabled;shape.requested=current->requested;shape.edited=current->edited;}
    }
    state.shapes=std::move(shapes);
    state.drivers=std::move(observed);state.available=true;
    if(authoring&&!state.restore_pending.load(std::memory_order_acquire)){
        state.layout_wait_started={};
        state.layout_applied=true;
    }
}
}
