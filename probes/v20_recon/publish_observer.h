#pragma once
#include "../../runtime/generated/GameCompat.generated.h"
// Callback-safe sampler of the skeletal publication metadata. No hooks, writes,
// heap allocation or file IO; the caller owns the result.
#include "../../playback/ue_memory.h"

namespace v20_publish_observer {
using ue_memory::ArrayHead;
constexpr uint32_t kMaxBones=4096;
constexpr uint32_t kHeadOffsets[4]{ww_game_compat::ComponentPoseBuffer0Offset,ww_game_compat::ComponentPoseBuffer1Offset,
    ww_game_compat::ComponentPublishedPoseOffset,ww_game_compat::ComponentLocalPoseOffset};
struct Metadata {
    ArrayHead heads[4]{}; // two pose buffers, published, local.
    int32_t read_index = -1, previous_index = -1;
    uint32_t generation = 0;
    uint8_t flags = 0;
};
inline bool ValidHead(const ArrayHead& head) {
    return head.num >= 0 && head.max >= head.num && uint32_t(head.max) <= kMaxBones &&
        (!head.data ? head.num == 0 && head.max == 0 : ue_memory::Pointer(head.data));
}
template<class Reader>
bool ReadMetadata(Reader& reader, uint64_t object, Metadata& values) {
    for (size_t i = 0; i < 4; ++i)
        if (!reader.Read(object + kHeadOffsets[i], &values.heads[i], sizeof(ArrayHead))) return false;
    return reader.Read(object + ww_game_compat::ComponentPoseReadIndexOffset, &values.read_index, 4) &&
        reader.Read(object + ww_game_compat::ComponentPosePreviousIndexOffset, &values.previous_index, 4) &&
        reader.Read(object + ww_game_compat::ComponentPoseGenerationOffset, &values.generation, 4) &&
        reader.Read(object + ww_game_compat::ComponentPoseFlagsOffset, &values.flags, 1);
}
} // namespace v20_publish_observer
