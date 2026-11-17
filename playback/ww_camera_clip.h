#pragma once
// Worker-loadable immutable MMD camera keys; no engine APIs or writes.
// Exact continuous Bezier sampling of the source keys holds the final key.
#include "ww_camera_math.h"
#include <filesystem>
#include <fstream>
namespace wwp_playback { namespace camera_clip {
constexpr uint32_t kHeaderBytes=128,kRecordBytes=40,kSourceKeyBytes=136;
constexpr uint32_t kCutStart=1,kHoldEnd=2;
constexpr uint64_t kMaximumSourceBytes=1024ull*1024*1024;
constexpr std::array<char,8> kMagic{'W','W','C','A','M','0','0','1'};
struct Header {
    std::array<char,8> magic=kMagic;
    uint32_t version=2,fps=60,frame_count=0,record_bytes=kRecordBytes;
    double duration=0;
    uint32_t last_source_frame=0,source_key_count=0;
    std::array<char,64> source_digest{};
    uint32_t basis=1,adjacent_cuts=1,source_key_bytes=kSourceKeyBytes;
    std::array<uint32_t,3> reserved{};
};
struct Record {Vec3 target,rotation;float distance=-45,fov=30;uint32_t perspective=1,flags=0;};
struct SourceRecord {
    uint32_t frame=0;
    float distance=0,fov=0;
    Vec3 target,rotation;
    uint32_t perspective=0;
    std::array<mmd::Curve,6> curves;
};
static_assert(sizeof(Header)==kHeaderBytes&&offsetof(Header,source_digest)==40,"WWCAM001 header layout");
static_assert(sizeof(Record)==kRecordBytes,"WWCAM001 baked record layout");
static_assert(sizeof(SourceRecord)==kSourceKeyBytes&&offsetof(SourceRecord,curves)==40,"WWCAM001 source key layout");
struct Clip {
    uint32_t frame_count=0,source_key_count=0;
    double duration=0;bool adjacent_cuts=true;
    std::vector<mmd::CameraKey> source_keys;
};
// WWCAM001 v2: header, 60 fps baked records, source keys. Runtime samples keys.
inline Clip ReadFile(const std::filesystem::path& path){
    std::ifstream file(path,std::ios::binary|std::ios::ate);
    const std::streamoff file_bytes=file.tellg();
    Header header;
    if(file_bytes<kHeaderBytes||!file.seekg(0).read(reinterpret_cast<char*>(&header),sizeof(header))||
        header.magic!=kMagic)throw std::runtime_error("invalid camera clip header");
    if(header.version!=2||header.fps!=60||header.record_bytes!=kRecordBytes||header.basis!=1||
        header.adjacent_cuts>1||header.source_key_bytes!=kSourceKeyBytes||header.reserved!=std::array<uint32_t,3>{})
        throw std::runtime_error("unsupported camera clip format");
    Clip clip;
    clip.frame_count=header.frame_count;clip.source_key_count=header.source_key_count;
    clip.duration=header.duration;clip.adjacent_cuts=header.adjacent_cuts!=0;
    const uint64_t source_offset=kHeaderBytes+uint64_t(clip.frame_count)*kRecordBytes;
    const uint64_t source_bytes=uint64_t(clip.source_key_count)*kSourceKeyBytes;
    if(!clip.frame_count||!clip.source_key_count||source_bytes>kMaximumSourceBytes||
        source_offset+source_bytes!=uint64_t(file_bytes)||!std::isfinite(clip.duration)||
        std::abs(clip.duration-(clip.frame_count-1)/60.)>1e-8||header.last_source_frame>double(clip.frame_count-1)/2)
        throw std::runtime_error("invalid camera clip dimensions");
    file.seekg(std::streamoff(source_offset));
    clip.source_keys.reserve(clip.source_key_count);
    const auto unit=[](float value){return value>=0&&value<=1;};
    for(uint32_t i=0;i<clip.source_key_count;++i){
        SourceRecord row;
        if(!file.read(reinterpret_cast<char*>(&row),sizeof(row)))throw std::runtime_error("truncated camera source keys");
        if(row.perspective>1||!std::isfinite(row.distance)||!(row.fov>=1&&row.fov<=179)||
            !ww_camera::Finite(row.target)||!ww_camera::Finite(row.rotation)||
            (!clip.source_keys.empty()&&row.frame<=clip.source_keys.back().frame)||
            !std::all_of(row.curves.begin(),row.curves.end(),[&](const auto& c){return unit(c.x1)&&unit(c.y1)&&unit(c.x2)&&unit(c.y2);}))
            throw std::runtime_error("invalid camera source key");
        mmd::CameraKey key;
        key.frame=row.frame;key.distance=row.distance;key.fov=row.fov;
        key.target=row.target;key.rotation=row.rotation;key.perspective=row.perspective!=0;key.curves=row.curves;
        clip.source_keys.push_back(key);
    }
    if(clip.source_keys.back().frame!=header.last_source_frame||file.peek()!=std::char_traits<char>::eof())
        throw std::runtime_error("camera source extent differs from its header");
    return clip;
}
inline bool Sample(const Clip& clip,double seconds,const ww_camera::Settings& settings,ww_camera::Frame* out)noexcept{
    if(clip.source_keys.empty()||!std::isfinite(seconds))return false;
    const auto key=mmd::SampleCamera(clip.source_keys,(std::max)(0.,(std::min)(seconds,clip.duration))*30,clip.adjacent_cuts);
    return ww_camera::Place(key,settings,out);
}
}} // namespace
