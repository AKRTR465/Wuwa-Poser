#pragma once
// Offline semantic weights and SOURCE eye quaternions. This representation
// contains no engine morph indices; caller must verify target named shapes.
#include "ww_face_profile.h"
#include <filesystem>
#include <fstream>

namespace wwp_playback { namespace face {
struct Clip {uint32_t target_count=0,frame_count=0,fps=0;double duration=0;std::vector<float> payload;};
inline Clip ReadClip(const std::filesystem::path& path){
    std::ifstream file(path,std::ios::binary);std::array<char,8> magic{};file.read(magic.data(),8);auto Read=[&](auto& x){if(!file.read((char*)&x,sizeof(x)))throw std::runtime_error("truncated expression header");};
    uint32_t version=0;Clip clip;Read(version);Read(clip.target_count);Read(clip.frame_count);Read(clip.fps);Read(clip.duration);
    if(std::memcmp(magic.data(),"WWEXPR01",8))throw std::runtime_error("invalid expression header");
    clip.payload.resize(size_t(uint64_t(clip.target_count+15)*clip.frame_count));
    if(!file.read((char*)clip.payload.data(),std::streamsize(clip.payload.size()*4)))throw std::runtime_error("truncated expressions");
    return clip;
}
inline bool SampleWeights(const Clip& clip,double seconds,float* weights,size_t count)noexcept{
    if(count!=clip.target_count||!clip.frame_count)return false;
    double frame=(std::max)(0.,(std::min)(seconds,clip.duration))*60;size_t a=(std::min)(size_t(frame),size_t(clip.frame_count-1)),b=(std::min)(a+1,size_t(clip.frame_count-1));float t=float(frame-a);
    const size_t stride=clip.target_count+15;const float* x=clip.payload.data()+a*stride;const float* y=clip.payload.data()+b*stride;
    for(size_t i=0;i<count;++i)weights[i]=x[i]+(y[i]-x[i])*t;
    return true;
}
}} // namespace wwp_playback::face
