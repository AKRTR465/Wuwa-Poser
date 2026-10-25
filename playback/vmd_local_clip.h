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
#include <cmath>
#include <limits>

namespace wwp_playback { namespace local_clip {
constexpr uint32_t kVersion=1,kMaximumFrames=100000,kMaximumBones=1024;
constexpr size_t kHeaderBytes=160,kBoneBytes=32;
constexpr uint64_t kMaximumFileBytes=1024ull*1024*1024;
constexpr uint32_t kRotation=1,kTranslationDelta=2;
struct BoneCommand {float translation_delta[3]={};float rotation_xyzw[4]={0,0,0,1};uint32_t mask=0;};
static_assert(sizeof(BoneCommand)==32,"WWLOCAL1 bone wire format requires 32 bytes");
struct Clip {uint32_t bone_count=0,frame_count=0,fps=0;double duration=0;
    std::string source_sha256,skeleton_sha256;std::vector<BoneCommand> frames;bool omit_provenance=false;};
template<class T>inline void Write(std::ostream& out,const T& value){out.write((const char*)&value,sizeof(value));}
template<class T>inline T Read(std::istream& in){T value{};if(!in.read((char*)&value,sizeof(value)))throw std::runtime_error("Truncated local clip");return value;}
inline bool IsSha256(const std::string& value) noexcept {
    return value.size()==64&&std::all_of(value.begin(),value.end(),[](char c){
        return (c>='0'&&c<='9')||(c>='a'&&c<='f')||(c>='A'&&c<='F');
    });
}
// Check dimensions before multiplication, allocation, or opening an output file.
// The bone limit matches the runtime's fixed full-body topology capacity.
inline uint64_t PayloadBytes(const Clip& clip) {
    if(!clip.bone_count||clip.bone_count>kMaximumBones||!clip.frame_count||clip.frame_count>kMaximumFrames||!clip.fps)
        throw std::runtime_error("Local clip dimensions exceed supported bounds");
    const double duration=double(clip.frame_count-1)/clip.fps;
    if(!std::isfinite(clip.duration)||clip.duration<0||
        std::fabs(clip.duration-duration)>1e-8*(std::max)(1.,duration))
        throw std::runtime_error("Local clip duration does not match its frames and fps");
    const uint64_t elements=uint64_t(clip.bone_count)*clip.frame_count;
    if(elements>(kMaximumFileBytes-kHeaderBytes)/kBoneBytes||
        elements>uint64_t((std::numeric_limits<size_t>::max)())/sizeof(BoneCommand))
        throw std::runtime_error("Local clip exceeds the 1 GiB file budget");
    if(!clip.omit_provenance&&(!IsSha256(clip.source_sha256)||!IsSha256(clip.skeleton_sha256)))
        throw std::runtime_error("Local clip provenance must contain two SHA-256 hex digests");
    return elements*kBoneBytes;
}
inline void ValidatePayload(const Clip& clip) {
    const auto bytes=PayloadBytes(clip);
    if(clip.frames.size()!=bytes/kBoneBytes)
        throw std::runtime_error("Local clip payload dimensions differ");
    for(size_t i=0;i<clip.frames.size();++i){
        const auto& command=clip.frames[i];const size_t bone=i%clip.bone_count;
        if(command.mask&~(kRotation|kTranslationDelta))
            throw std::runtime_error("Local clip contains an unknown bone mask");
        if((command.mask&kTranslationDelta)&&bone!=0)
            throw std::runtime_error("Local clip translation is only supported on the root bone");
        if(command.mask!=clip.frames[bone].mask)
            throw std::runtime_error("Local clip bone masks must remain constant across frames");
        for(float value:command.translation_delta)if(!std::isfinite(value))
            throw std::runtime_error("Local clip contains a non-finite translation");
        double norm=0;
        for(float value:command.rotation_xyzw){
            if(!std::isfinite(value))throw std::runtime_error("Local clip contains a non-finite rotation");
            norm+=double(value)*value;
        }
        // Unused rotation fields may be zero in older clips. Active rotations
        // must be unit quaternions for the callback's interpolation to be valid.
        if((command.mask&kRotation)&&std::fabs(norm-1.)>1e-3)
            throw std::runtime_error("Local clip contains a non-unit rotation");
    }
}
inline Clip ReadFile(const std::filesystem::path& path) {
    std::ifstream in(path,std::ios::binary|std::ios::ate);
    if(!in)throw std::runtime_error("Cannot open local clip");
    const auto end=in.tellg();
    if(end<0||uint64_t(end)<kHeaderBytes||uint64_t(end)>kMaximumFileBytes)
        throw std::runtime_error("Local clip file size is outside supported bounds");
    const uint64_t fileBytes=uint64_t(end);in.seekg(0);
    std::array<char,8> magic{};in.read(magic.data(),std::streamsize(magic.size()));
    if(!in||std::memcmp(magic.data(),"WWLOCAL1",magic.size())||Read<uint32_t>(in)!=kVersion)
        throw std::runtime_error("Local clip magic or version is invalid");
    Clip clip;clip.bone_count=Read<uint32_t>(in);clip.frame_count=Read<uint32_t>(in);
    clip.fps=Read<uint32_t>(in);clip.duration=Read<double>(in);
    std::array<char,128> hashes{};
    if(!in.read(hashes.data(),std::streamsize(hashes.size())))throw std::runtime_error("Truncated local clip header");
    clip.omit_provenance=std::all_of(hashes.begin(),hashes.end(),[](char c){return c==0;});
    if(!clip.omit_provenance){clip.source_sha256.assign(hashes.data(),64);clip.skeleton_sha256.assign(hashes.data()+64,64);}
    const uint64_t bytes=PayloadBytes(clip);
    if(fileBytes!=kHeaderBytes+bytes)throw std::runtime_error("Local clip file length does not match its dimensions");
    clip.frames.resize(size_t(bytes/kBoneBytes));
    if(!in.read(reinterpret_cast<char*>(clip.frames.data()),std::streamsize(bytes)))
        throw std::runtime_error("Truncated local clip payload");
    if(in.peek()!=std::char_traits<char>::eof())throw std::runtime_error("Local clip has trailing data");
    ValidatePayload(clip);return clip;
}
inline void WriteFile(const std::filesystem::path& path,const Clip& c){
    ValidatePayload(c);
    std::ofstream out(path,std::ios::binary);if(!out)throw std::runtime_error("Cannot create clip");out.write("WWLOCAL1",8);Write(out,kVersion);Write(out,c.bone_count);Write(out,c.frame_count);Write(out,c.fps);Write(out,c.duration);
    if(c.omit_provenance){const std::array<char,128> empty{};out.write(empty.data(),empty.size());}
    else{out.write(c.source_sha256.data(),64);out.write(c.skeleton_sha256.data(),64);}
    out.write((const char*)c.frames.data(),std::streamsize(c.frames.size()*sizeof(BoneCommand)));if(!out)throw std::runtime_error("Cannot save complete clip");}
inline bool Sample(const Clip& clip,double seconds,BoneCommand* out,size_t count) noexcept {
    if(!out||!std::isfinite(seconds)||count!=clip.bone_count||!clip.bone_count||!clip.frame_count||!clip.fps||
        clip.frames.size()!=uint64_t(clip.bone_count)*clip.frame_count)return false;
    double frame=(std::max)(0.,(std::min)(seconds,clip.duration))*clip.fps;size_t a=(std::min)(size_t(frame),size_t(clip.frame_count-1)),b=(std::min)(a+1,size_t(clip.frame_count-1));float t=float(frame-a);
    for(size_t i=0;i<count;++i){const auto& x=clip.frames[a*count+i];const auto& y=clip.frames[b*count+i];auto& z=out[i];z.mask=x.mask;
        for(unsigned k=0;k<3;++k)z.translation_delta[k]=x.translation_delta[k]+(y.translation_delta[k]-x.translation_delta[k])*t;
        auto q=::Quat::Slerp({x.rotation_xyzw[0],x.rotation_xyzw[1],x.rotation_xyzw[2],x.rotation_xyzw[3]},
            {y.rotation_xyzw[0],y.rotation_xyzw[1],y.rotation_xyzw[2],y.rotation_xyzw[3]},t);z.rotation_xyzw[0]=q.x;z.rotation_xyzw[1]=q.y;z.rotation_xyzw[2]=q.z;z.rotation_xyzw[3]=q.w;}
    return true;
}
}} // wwp_playback::local_clip
