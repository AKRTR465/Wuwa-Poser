#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include "../playback/vmd_clip.h"
#include "../playback/ww_camera_clip.h"
#include "../core_math/third_party/nlohmann/json.hpp"
#include <cstdio>
#include "mmd_compile_common.h"
using Json=nlohmann::json;namespace vmd=wwp_playback::vmd;namespace cc=wwp_playback::camera_clip;
using wwp_mmd_compile::Decode;
using wwp_mmd_compile::Sha256;
template<class T>void Put(uint8_t* p,size_t offset,T value){std::memcpy(p+offset,&value,sizeof(value));}
int wmain(int argc,wchar_t** argv){if(argc<4||argc>6){std::fprintf(stderr,"usage: mmd_camera_compile CAMERA_VMD NEW_REPORT_JSON NEW_WWCAM [TIMELINE_SECONDS] [continuous]\n");return 2;}
    try{const auto sourceHash=Sha256(argv[1]);const auto clip=vmd::ReadFile(argv[1],Decode);const auto& keys=clip.motion.cameras;if(keys.empty())throw std::runtime_error("VMD has no camera keys");
        const bool cuts=argc<6;const double sourceDuration=keys.back().frame/30.;
        const double timeline=argc>=5?wwp_mmd_compile::Number(argv[4]):sourceDuration;
        const double duration=(std::max)(timeline,sourceDuration);const uint32_t frames=uint32_t(std::ceil(duration*60))+1;
        std::array<uint8_t,cc::kHeaderBytes> header{};std::memcpy(header.data(),"WWCAM001",8);Put(header.data(),8,uint32_t(2));Put(header.data(),12,uint32_t(60));Put(header.data(),16,frames);Put(header.data(),20,cc::kRecordBytes);Put(header.data(),24,(frames-1)/60.);Put(header.data(),32,keys.back().frame);Put(header.data(),36,uint32_t(keys.size()));std::memcpy(header.data()+40,sourceHash.data(),64);Put(header.data(),104,uint32_t(1));Put(header.data(),108,uint32_t(cuts));Put(header.data(),112,cc::kSourceKeyBytes);
        std::ofstream file(argv[3],std::ios::binary);file.write((char*)header.data(),header.size());
        uint32_t previousSource=0;
        for(uint32_t f=0;f<frames;++f){const double sourceFrame=f/2.;const auto key=mmd::SampleCamera(keys,sourceFrame,cuts);cc::Record sample;sample.target=key.target;sample.rotation=key.rotation;sample.distance=key.distance;sample.fov=key.fov;sample.perspective=key.perspective?1u:0u;
            const auto upper=mmd::Upper(keys,sourceFrame);const auto sourceIndex=upper?upper-1:0;
            if(cuts&&f&&sourceIndex&&sourceIndex!=previousSource&&keys[sourceIndex].frame-keys[sourceIndex-1].frame==1)sample.flags|=cc::kCutStart;
            if(sourceFrame>keys.back().frame)sample.flags|=cc::kHoldEnd;
            previousSource=uint32_t(sourceIndex);file.write((char*)&sample,sizeof(sample));
        }
        for(const auto& key:keys){std::array<uint8_t,cc::kSourceKeyBytes> row{};Put(row.data(),0,key.frame);Put(row.data(),4,key.distance);Put(row.data(),8,key.fov);std::memcpy(row.data()+12,&key.target,12);std::memcpy(row.data()+24,&key.rotation,12);Put(row.data(),36,uint32_t(key.perspective));std::memcpy(row.data()+40,key.curves.data(),96);file.write((char*)row.data(),row.size());}
        if(!file)throw std::runtime_error("camera binary write failed");file.close();
        Json report={{"schema",1},{"source_camera_vmd",std::filesystem::path(argv[1]).u8string()},{"source_sha256",sourceHash},{"camera_key_count",keys.size()},{"source_last_frame",keys.back().frame},{"timeline_duration_seconds",(frames-1)/60.},{"fps",60},{"frame_count",frames},
            {"binary",std::filesystem::path(argv[3]).u8string()},{"binary_sha256",Sha256(argv[3])},{"binary_bytes",std::filesystem::file_size(argv[3])},{"binary_version",2},{"header_bytes",cc::kHeaderBytes},{"record_bytes",cc::kRecordBytes},{"source_key_bytes",cc::kSourceKeyBytes}};
        std::ofstream output(argv[2]);output<<report.dump(2)<<'\n';if(!output)throw std::runtime_error("camera report failed");
        std::printf("camera keys=%zu frames=%u source_seconds=%.6f timeline_seconds=%.6f\n",keys.size(),frames,sourceDuration,(frames-1)/60.);return 0;
    }catch(const std::exception& e){std::fprintf(stderr,"%s\n",e.what());return 2;}}
