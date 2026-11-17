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
int wmain(int argc,wchar_t** argv){if(argc<4||argc>6){std::fprintf(stderr,"usage: mmd_camera_compile CAMERA_VMD NEW_REPORT_JSON NEW_WWCAM [TIMELINE_SECONDS] [continuous]\n");return 2;}
    try{const auto sourceHash=Sha256(argv[1]);const auto clip=vmd::ReadFile(argv[1],Decode);const auto& keys=clip.motion.cameras;if(keys.empty())throw std::runtime_error("VMD has no camera keys");
        const bool cuts=argc<6;const double sourceDuration=keys.back().frame/30.;
        const double timeline=argc>=5?wwp_mmd_compile::Number(argv[4]):sourceDuration;
        const uint32_t frames=wwp_mmd_compile::FrameCount60((std::max)(timeline,sourceDuration));
        if(keys.size()>cc::kMaximumSourceBytes/cc::kSourceKeyBytes)throw std::runtime_error("camera source keys exceed the supported byte budget");
        cc::Header header;
        header.frame_count=frames;header.duration=(frames-1)/60.;
        header.last_source_frame=keys.back().frame;header.source_key_count=uint32_t(keys.size());
        std::copy_n(sourceHash.begin(),header.source_digest.size(),header.source_digest.begin());
        header.adjacent_cuts=cuts;
        std::ofstream file(argv[3],std::ios::binary);
        file.write(reinterpret_cast<const char*>(&header),sizeof(header));
        uint32_t previousSource=0;
        for(uint32_t f=0;f<frames;++f){
            const double sourceFrame=f/2.;const auto key=mmd::SampleCamera(keys,sourceFrame,cuts);
            cc::Record sample;sample.target=key.target;sample.rotation=key.rotation;sample.distance=key.distance;
            sample.fov=key.fov;sample.perspective=key.perspective?1u:0u;
            const auto upper=mmd::Upper(keys,sourceFrame);const auto sourceIndex=upper?upper-1:0;
            if(cuts&&f&&sourceIndex&&sourceIndex!=previousSource&&keys[sourceIndex].frame-keys[sourceIndex-1].frame==1)sample.flags|=cc::kCutStart;
            if(sourceFrame>keys.back().frame)sample.flags|=cc::kHoldEnd;
            previousSource=uint32_t(sourceIndex);file.write((char*)&sample,sizeof(sample));
        }
        for(const auto& key:keys){
            const cc::SourceRecord row{key.frame,key.distance,key.fov,key.target,key.rotation,uint32_t(key.perspective),key.curves};
            file.write(reinterpret_cast<const char*>(&row),sizeof(row));
        }
        if(!file)throw std::runtime_error("camera binary write failed");
        file.close();
        Json report={{"schema",1},
            {"source_camera_vmd",std::filesystem::path(argv[1]).u8string()},
            {"camera_key_count",keys.size()},{"source_last_frame",keys.back().frame},
            {"timeline_duration_seconds",(frames-1)/60.},{"fps",60},{"frame_count",frames},
            {"binary",std::filesystem::path(argv[3]).u8string()},{"binary_sha256",Sha256(argv[3])},
            {"binary_bytes",std::filesystem::file_size(argv[3])},{"binary_version",2},
            {"header_bytes",cc::kHeaderBytes},{"record_bytes",cc::kRecordBytes},{"source_key_bytes",cc::kSourceKeyBytes}};
        std::ofstream output(argv[2]);output<<report.dump(2)<<'\n';if(!output)throw std::runtime_error("camera report failed");
        std::printf("camera keys=%zu frames=%u source_seconds=%.6f timeline_seconds=%.6f\n",keys.size(),frames,sourceDuration,(frames-1)/60.);return 0;
    }catch(const std::exception& e){std::fprintf(stderr,"%s\n",e.what());return 2;}}
