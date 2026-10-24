#pragma once
// Worker-side VMD loading. Reuses core_math's streaming parser; no game memory,
// rig mapping, or writes. CP932 decoding is caller-injected.
#include "../core_math/include/poser_math/mmd_motion.h"
#include "../core_math/include/poser_math/mmd_camera.h"
#include <fstream>
#include <filesystem>
#include <set>

namespace wwp_playback { namespace vmd {
struct LoadedClip {mmd::MotionClip motion;double DurationSeconds()const {return motion.duration();}};
inline LoadedClip ReadFile(const std::filesystem::path& path,const mmd::Decoder& decoder) {
    std::ifstream file(path,std::ios::binary);if(!file)throw std::runtime_error("Cannot open VMD file");
    mmd::Reader reader(file,size_t(std::filesystem::file_size(path)));return {mmd::ReadVmd(reader,decoder)};
}
}} // wwp_playback::vmd
