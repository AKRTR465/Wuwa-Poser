#pragma once
// Offline semantic weights and SOURCE eye quaternions. This representation
// contains no engine morph indices; caller must verify target named shapes.
#include "ww_face_snapshot.h"
#include <filesystem>
#include <fstream>
#include <stdexcept>

namespace wwp_playback { namespace face {
constexpr uint32_t kExpressionHeaderBytes=32,kEyeSamples=15,kExpressionFps=60;
constexpr uint64_t kMaximumExpressionBytes=1024ull*1024*1024;
struct Clip {uint32_t target_count=0,frame_count=0,fps=0;double duration=0;std::vector<float> payload;};
inline Clip ReadClip(const std::filesystem::path& path){
    std::ifstream file(path,std::ios::binary|std::ios::ate);
    const std::streamoff file_bytes=file.tellg();
    if(file_bytes<kExpressionHeaderBytes||uint64_t(file_bytes)>kMaximumExpressionBytes)
        throw std::runtime_error("invalid expression file size");
    std::array<char,8> magic{};
    file.seekg(0).read(magic.data(),magic.size());
    auto read=[&](auto& value){
        if(!file.read(reinterpret_cast<char*>(&value),sizeof(value)))throw std::runtime_error("truncated expression header");
    };
    uint32_t version=0;Clip clip;
    read(version);read(clip.target_count);read(clip.frame_count);read(clip.fps);read(clip.duration);
    if(std::memcmp(magic.data(),"WWEXPR01",8)||version!=1)throw std::runtime_error("invalid expression header");
    if(!clip.target_count||clip.target_count>face_snapshot::kMaxMorphs||!clip.frame_count||clip.fps!=kExpressionFps||
        !std::isfinite(clip.duration)||std::abs(clip.duration-(clip.frame_count-1)/double(kExpressionFps))>1e-8)
        throw std::runtime_error("invalid expression clip dimensions");
    const uint64_t stride=clip.target_count+kEyeSamples;
    const uint64_t samples=stride*clip.frame_count,payload_bytes=samples*sizeof(float);
    if(kExpressionHeaderBytes+payload_bytes!=uint64_t(file_bytes))throw std::runtime_error("invalid expression clip extent");
    clip.payload.resize(size_t(samples));
    if(!file.read(reinterpret_cast<char*>(clip.payload.data()),std::streamsize(payload_bytes))||file.peek()!=std::char_traits<char>::eof())
        throw std::runtime_error("expression payload changed while loading");
    // Eye samples are reserved for playback consumers that use them. The
    // weight sampler and game writer consume only the leading morph values.
    for(size_t row=0;row<clip.payload.size();row+=size_t(stride)){
        const auto* weights=clip.payload.data()+row;
        if(!std::all_of(weights,weights+clip.target_count,[](float value){return std::isfinite(value);}))
            throw std::runtime_error("invalid expression weight");
    }
    return clip;
}
inline bool SampleWeights(const Clip& clip,double seconds,float* weights,size_t count)noexcept{
    if(count!=clip.target_count||!clip.frame_count||!std::isfinite(seconds))return false;
    double frame=(std::max)(0.,(std::min)(seconds,clip.duration))*kExpressionFps;size_t a=(std::min)(size_t(frame),size_t(clip.frame_count-1)),b=(std::min)(a+1,size_t(clip.frame_count-1));float t=float(frame-a);
    const size_t stride=clip.target_count+kEyeSamples;const float* x=clip.payload.data()+a*stride;const float* y=clip.payload.data()+b*stride;
    for(size_t i=0;i<count;++i)weights[i]=x[i]+(y[i]-x[i])*t;
    return true;
}
}} // namespace wwp_playback::face
