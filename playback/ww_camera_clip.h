#pragma once
// Worker-loadable immutable MMD camera keys; no engine APIs or writes.
// Exact continuous Bezier sampling of the source keys holds the final key.
#include "ww_camera_math.h"
#include <filesystem>
#include <fstream>
namespace wwp_playback { namespace camera_clip {
constexpr uint32_t kHeaderBytes=128,kRecordBytes=40,kSourceKeyBytes=136;
constexpr uint32_t kCutStart=1,kHoldEnd=2;
struct Record {Vec3 target,rotation;float distance=-45,fov=30;uint32_t perspective=1,flags=0;};
static_assert(sizeof(Record)==kRecordBytes,"WWCAM001 source record layout");
struct Clip {
    uint32_t frame_count=0,source_key_count=0;
    double duration=0;bool adjacent_cuts=true;
    std::vector<mmd::CameraKey> source_keys;
};
// WWCAM001 v2: header, 60 fps baked records, source keys. Runtime samples keys.
inline Clip ReadFile(const std::filesystem::path& path){
    std::ifstream file(path,std::ios::binary);std::array<uint8_t,kHeaderBytes> header{};
    if(!file.read((char*)header.data(),header.size())||std::memcmp(header.data(),"WWCAM001",8))throw std::runtime_error("invalid camera clip header");
    auto u32=[&](unsigned offset){uint32_t v=0;std::memcpy(&v,header.data()+offset,4);return v;};
    Clip clip;clip.frame_count=u32(16);std::memcpy(&clip.duration,header.data()+24,8);clip.source_key_count=u32(36);clip.adjacent_cuts=u32(108)!=0;
    file.seekg(std::streamoff(kHeaderBytes+uint64_t(clip.frame_count)*kRecordBytes));clip.source_keys.resize(clip.source_key_count);
    for(auto& key:clip.source_keys){std::array<uint8_t,kSourceKeyBytes> row{};if(!file.read((char*)row.data(),row.size()))throw std::runtime_error("truncated camera source keys");
        std::memcpy(&key.frame,row.data(),4);std::memcpy(&key.distance,row.data()+4,4);std::memcpy(&key.fov,row.data()+8,4);std::memcpy(&key.target,row.data()+12,12);std::memcpy(&key.rotation,row.data()+24,12);
        uint32_t projection=0;std::memcpy(&projection,row.data()+36,4);key.perspective=projection!=0;std::memcpy(key.curves.data(),row.data()+40,96);
    }
    return clip;
}
inline bool Sample(const Clip& clip,double seconds,const ww_camera::Settings& settings,ww_camera::Frame* out)noexcept{
    if(clip.source_keys.empty())return false;
    const auto key=mmd::SampleCamera(clip.source_keys,(std::max)(0.,(std::min)(seconds,clip.duration))*30,clip.adjacent_cuts);
    return ww_camera::Place(key,seconds,settings,out);
}
}} // namespace
