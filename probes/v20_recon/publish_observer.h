#pragma once
#include "../../runtime/generated/GameCompat.generated.h"
// Callback-safe sampler of the skeletal publication metadata. No hooks, writes,
// heap allocation or file IO; the caller owns the result.
#include <cstdint>
#include <cstddef>

namespace v20_publish_observer {

struct OffsetManifest {
    uint32_t buffer0, buffer1, published, local;
    uint32_t read_index, previous_index, generation, flags;
    uint32_t max_bones;
};
constexpr OffsetManifest kVerifiedLayout{ww_game_compat::ComponentPoseBuffer0Offset, ww_game_compat::ComponentPoseBuffer1Offset, ww_game_compat::ComponentPublishedPoseOffset, ww_game_compat::ComponentLocalPoseOffset,
                                        ww_game_compat::ComponentPoseReadIndexOffset, ww_game_compat::ComponentPosePreviousIndexOffset, ww_game_compat::ComponentPoseGenerationOffset, ww_game_compat::ComponentPoseFlagsOffset, 4096};
struct TargetIdentity {
    uint64_t address = 0, class_address = 0, outer_address = 0, vtable = 0;
    uint32_t index = UINT32_MAX, serial = 0;
    bool verified = false;
};
struct ArrayHead {uint64_t data = 0; int32_t num = 0, max = 0;};
static_assert(sizeof(ArrayHead) == ww_game_compat::TArrayHeaderBytes, "TArray head layout");
struct Metadata {
    ArrayHead heads[4]{}; // 0:+630, 1:+640, 2:+680 published, 3:+950 local.
    int32_t read_index = -1, previous_index = -1;
    uint32_t generation = 0;
    uint8_t flags = 0;
};
struct Sample {
    TargetIdentity identity{};
    Metadata values{};
    bool accepted = false;
};
inline bool Pointer(uint64_t address) {
    return address >= ww_game_compat::PointerMinimum && address < ww_game_compat::PointerMaximum && !(address & (ww_game_compat::PointerBytes-1));
}
inline bool Same(const TargetIdentity& a, const TargetIdentity& b) {
    return a.address == b.address && a.class_address == b.class_address &&
        a.outer_address == b.outer_address && a.vtable == b.vtable &&
        a.index == b.index && a.serial == b.serial && a.verified == b.verified;
}
inline bool ValidHead(const ArrayHead& head, uint32_t max_bones) {
    return head.num >= 0 && head.max >= head.num && uint32_t(head.max) <= max_bones &&
        (!head.data ? head.num == 0 && head.max == 0 : Pointer(head.data));
}
template<class Reader>
bool ReadMetadata(Reader& reader, uint64_t object, const OffsetManifest& layout, Metadata& values) {
    const uint32_t offsets[4]{layout.buffer0, layout.buffer1, layout.published, layout.local};
    for (size_t i = 0; i < 4; ++i)
        if (!reader.Read(object + offsets[i], &values.heads[i], sizeof(ArrayHead))) return false;
    return reader.Read(object + layout.read_index, &values.read_index, 4) &&
        reader.Read(object + layout.previous_index, &values.previous_index, 4) &&
        reader.Read(object + layout.generation, &values.generation, 4) &&
        reader.Read(object + layout.flags, &values.flags, 1);
}
// Reader contract: bool Read(uint64_t,void*,size_t), bool Identify(uint64_t,TargetIdentity*).
template<class Reader>
bool Observe(Reader& reader, uint64_t object, const TargetIdentity& target,
             const OffsetManifest& layout, Sample* out) noexcept {
    *out = Sample{};
    if (object != target.address || !reader.Identify(object, &out->identity) || !Same(out->identity, target) ||
        !ReadMetadata(reader, object, layout, out->values)) return false;
    const auto& v = out->values;
    if (v.read_index < 0 || v.read_index > 1 || v.previous_index < 0 || v.previous_index > 1) return false;
    for (const auto& head : v.heads) if (!ValidHead(head, layout.max_bones)) return false;
    return out->accepted = true;
}

} // namespace v20_publish_observer
