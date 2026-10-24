#pragma once
// Detached helpers for the native Kawaii input adapter. Raw address reading,
// writing and native getter execution belong to the caller, which runs on the
// live evaluation thread inside the selected converter callback.
#include <array>
#include <cstddef>
#include <cstdint>
#include <cstring>

namespace wwp_playback { namespace native_kawaii {
constexpr uint32_t kLimit = 1024;
using Transform = std::array<uint8_t, 48>;
struct ArrayHeader { uintptr_t data=0; int32_t num=0, capacity=0; };
static_assert(sizeof(ArrayHeader)==16, "x64 TArray header required");

struct Layout {
    uint32_t proxy=0, pose=8, transforms=8, extra_vectors=0x18;
    uint32_t extra_vectors_active=0x28, bone_container=0x30, flags=0x38;
    uint32_t proxy_mesh=0xc0, skeleton_to_compact=0x80, compact_parents=0x90;
};
enum class Error {
    none, unreadable, bad_count, pointer_mismatch, extra_channel_unsupported, bad_mapping, getter_failed
};
struct View {
    uintptr_t context=0, proxy=0, mesh=0, bone_container=0, pose=0;
    ArrayHeader transforms, flags, extra_vectors, skeleton_to_compact;
    uint8_t extra_active=0;
};
struct Snapshot {
    uint32_t count=0;
    std::array<int32_t,kLimit> skeleton_to_compact{};
    std::array<Transform,kLimit> component{}; // compact order
};
inline bool Header(const ArrayHeader& a, uint32_t count) { return a.data && a.num==static_cast<int32_t>(count); }
inline bool ExtraVectorsAbsent(const ArrayHeader& a, uint8_t mode) {
    // L2C (0x4e0ba10) clears/copies this array independently of the mode byte;
    // only the canonical null array is supported.
    return mode<=1 && a.data==0 && a.num==0 && a.capacity==0;
}
template<class Read>
Error ReadView(uintptr_t context, uintptr_t expected_mesh, uint32_t count, const Layout& l, Read&& read, View* out) {
    View v; v.context=context; v.pose=context+l.pose;
    if(!read(context+l.proxy,&v.proxy,sizeof(v.proxy)) ||
       !read(context+l.bone_container,&v.bone_container,sizeof(v.bone_container)) ||
       !read(context+l.transforms,&v.transforms,sizeof(v.transforms)) ||
       !read(context+l.flags,&v.flags,sizeof(v.flags)) ||
       !read(context+l.extra_vectors,&v.extra_vectors,sizeof(v.extra_vectors)) ||
       !read(context+l.extra_vectors_active,&v.extra_active,sizeof(v.extra_active)) ||
       !read(v.proxy+l.proxy_mesh,&v.mesh,sizeof(v.mesh)) ||
       !read(v.bone_container+l.skeleton_to_compact,&v.skeleton_to_compact,sizeof(v.skeleton_to_compact))) return Error::unreadable;
    if(v.mesh!=expected_mesh) return Error::pointer_mismatch;
    if(!ExtraVectorsAbsent(v.extra_vectors,v.extra_active)) return Error::extra_channel_unsupported;
    if(!Header(v.transforms,count) || !Header(v.flags,count) || !Header(v.skeleton_to_compact,count)) return Error::bad_count;
    *out=v; return Error::none;
}
template<class Read>
Error ReadMapping(const View& v, Read&& read, Snapshot* out) {
    const uint32_t n=static_cast<uint32_t>(v.transforms.num);
    out->count=n;
    if(!read(v.skeleton_to_compact.data,out->skeleton_to_compact.data(),n*sizeof(int32_t))) return Error::unreadable;
    for(uint32_t i=0;i<n;++i) if(out->skeleton_to_compact[i]<0 || out->skeleton_to_compact[i]>=static_cast<int32_t>(n)) return Error::bad_mapping;
    return Error::none;
}
// Native getter may materialize cached CS transforms; never call off-thread.
template<class Getter>
Error Materialize(const View& v, Getter&& getter, Snapshot* snapshot) {
    for(uint32_t i=0;i<snapshot->count;++i) if(!getter(v.pose,static_cast<int32_t>(i),&snapshot->component[i])) return Error::getter_failed;
    return Error::none;
}
// Index-space merge in compact order, not FK: the caller supplies coherent
// descendant component transforms.
inline void PrepareInput(const Snapshot& s, const Transform* skeleton_component, const uint8_t* skeleton_mask, std::array<Transform,kLimit>* out) {
    *out=s.component;
    for(uint32_t i=0;i<s.count;++i) if(skeleton_mask[i]) (*out)[s.skeleton_to_compact[i]]=skeleton_component[i];
}
// Writes the component-space pose and marks every entry component-space.
template<class Write>
bool CommitInput(const View& view, const std::array<Transform,kLimit>& prepared, Write&& write) {
    const uint32_t n=static_cast<uint32_t>(view.transforms.num);
    std::array<uint8_t,kLimit> flags; flags.fill(1);
    return write(view.transforms.data,prepared.data(),size_t(n)*sizeof(Transform)) && write(view.flags.data,flags.data(),size_t(n));
}
}} // namespace wwp_playback::native_kawaii
