#pragma once
// Immutable offline-compiled target local edits. Sample performs no allocation
// and does not access engine memory.
#include "../core_math/include/poser_math/quat_math.h"
#include <array>
#include <cstdint>
#include <cstring>
#include <fstream>
#include <filesystem>
#include <stdexcept>
#include <string>
#include <vector>
#include <algorithm>

namespace wwp_playback { namespace local_clip {
constexpr uint32_t kVersion=1,kMaximumFrames=100000;
constexpr size_t kHeaderBytes=160,kBoneBytes=32;
constexpr uint64_t kMaximumFileBytes=1024ull*1024*1024;
constexpr uint32_t kRotation=1,kTranslationDelta=2;
struct BoneCommand {float translation_delta[3]={};float rotation_xyzw[4]={0,0,0,1};uint32_t mask=0;};
static_assert(sizeof(BoneCommand)==32,"WWLOCAL1 bone wire format requires 32 bytes");
struct Clip {uint32_t bone_count=0,frame_count=0,fps=0;double duration=0;
    std::string source_sha256,skeleton_sha256;std::vector<BoneCommand> frames;bool omit_provenance=false;};
template<class T>inline void Write(std::ostream& out,const T& value){out.write((const char*)&value,sizeof(value));}
template<class T>inline T Read(std::istream& in){T value{};if(!in.read((char*)&value,sizeof(value)))throw std::runtime_error("Truncated local clip");return value;}
inline void WriteFile(const std::filesystem::path& path,const Clip& c){
    std::ofstream out(path,std::ios::binary);if(!out)throw std::runtime_error("Cannot create clip");out.write("WWLOCAL1",8);Write(out,kVersion);Write(out,c.bone_count);Write(out,c.frame_count);Write(out,c.fps);Write(out,c.duration);
    if(c.omit_provenance){const std::array<char,128> empty{};out.write(empty.data(),empty.size());}
    else{out.write(c.source_sha256.data(),64);out.write(c.skeleton_sha256.data(),64);}
    out.write((const char*)c.frames.data(),std::streamsize(c.frames.size()*sizeof(BoneCommand)));if(!out)throw std::runtime_error("Cannot save complete clip");}
inline bool Sample(const Clip& clip,double seconds,BoneCommand* out,size_t count) noexcept {
    if(count!=clip.bone_count||!clip.frame_count)return false;
    double frame=(std::max)(0.,(std::min)(seconds,clip.duration))*clip.fps;size_t a=(std::min)(size_t(frame),size_t(clip.frame_count-1)),b=(std::min)(a+1,size_t(clip.frame_count-1));float t=float(frame-a);
    for(size_t i=0;i<count;++i){const auto& x=clip.frames[a*count+i];const auto& y=clip.frames[b*count+i];auto& z=out[i];z.mask=x.mask;
        for(unsigned k=0;k<3;++k)z.translation_delta[k]=x.translation_delta[k]+(y.translation_delta[k]-x.translation_delta[k])*t;
        auto q=::Quat::Slerp({x.rotation_xyzw[0],x.rotation_xyzw[1],x.rotation_xyzw[2],x.rotation_xyzw[3]},
            {y.rotation_xyzw[0],y.rotation_xyzw[1],y.rotation_xyzw[2],y.rotation_xyzw[3]},t);z.rotation_xyzw[0]=q.x;z.rotation_xyzw[1]=q.y;z.rotation_xyzw[2]=q.z;z.rotation_xyzw[3]=q.w;}
    return true;
}
}} // wwp_playback::local_clip
