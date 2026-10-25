#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include "../playback/vmd_clip.h"
#include "../playback/vmd_local_clip.h"
#include "../playback/ww_retarget.h"
#include "../core_math/third_party/nlohmann/json.hpp"
#include <cstdio>
#include "mmd_compile_common.h"
using Json=nlohmann::json;
namespace vmd=wwp_playback::vmd;
using wwp_mmd_compile::Decode;
using wwp_mmd_compile::Sha256;
static Json Quat(const ::Quat& q){return Json::array({q.x,q.y,q.z,q.w});}
static void Save(const std::filesystem::path& path,const Json& value){std::ofstream file(path,std::ios::binary);file<<value.dump(2)<<'\n';if(!file)throw std::runtime_error("Cannot write output");}
int wmain(int argc,wchar_t** argv){std::filesystem::path source,output,targetPath,compiledPath;double scale=0;bool omitProvenance=false;
    try {for(int i=1;i<argc;++i){std::wstring arg=argv[i];if(arg==L"--source"&&i+1<argc)source=argv[++i];else if(arg==L"--output"&&i+1<argc)output=argv[++i];
        else if(arg==L"--target-skeleton"&&i+1<argc)targetPath=argv[++i];
        else if(arg==L"--compiled"&&i+1<argc)compiledPath=argv[++i];else if(arg==L"--scale"&&i+1<argc)scale=wwp_mmd_compile::Number(argv[++i]);
        else if(arg==L"--omit-provenance")omitProvenance=true;
        else throw std::runtime_error("Usage: mmd_vmd_cli --source PATH --output NEW_JSON [--target-skeleton EXPORT --compiled NEW_WWLOCAL1 --scale MMD_TO_UE] [--omit-provenance]");}
        if(source.empty()||output.empty())throw std::runtime_error("Source and output required");
        auto clip=vmd::ReadFile(source,Decode);
        Json report={{"schema",1},{"source",Decode(source.string(),CP_ACP)},{"fps",30},{"duration_seconds",clip.DurationSeconds()}};
        if(!omitProvenance)report["source_sha256"]=Sha256(source);
        if(!compiledPath.empty()){
            namespace rt=wwp_playback::ww_retarget;namespace lc=wwp_playback::local_clip;
            auto target=rt::ReadTarget(targetPath);rt::Compiler compiler;compiler.Bind(clip.motion,target);
            const bool estimatedScale=scale==0;if(estimatedScale)scale=compiler.GeometricTranslationScale();
            const double sampledFrames=std::ceil(clip.DurationSeconds()*60)+1;
            if(!std::isfinite(sampledFrames)||sampledFrames<1||sampledFrames>lc::kMaximumFrames)
                throw std::runtime_error("Compiled motion exceeds the supported frame budget");
            lc::Clip local;local.bone_count=target.topology.count;local.fps=60;local.frame_count=uint32_t(sampledFrames);
            local.duration=(local.frame_count-1)/60.;local.omit_provenance=omitProvenance;
            if(!omitProvenance){local.source_sha256=report["source_sha256"].get<std::string>();local.skeleton_sha256=Sha256(targetPath);}
            const auto payloadBytes=lc::PayloadBytes(local);
            local.frames.resize(size_t(payloadBytes/lc::kBoneBytes));
            wwp_ue::full_body::LocalFrame frame;for(uint32_t f=0;f<local.frame_count;++f){compiler.Sample(f/60.,float(scale),&frame);
                for(uint32_t i=0;i<local.bone_count;++i)std::memcpy(&local.frames[uint64_t(f)*local.bone_count+i],&frame.bones[i],sizeof(lc::BoneCommand));}
            lc::WriteFile(compiledPath,local);
            Json names=Json::array(),parents=Json::array(),positions=Json::array(),positionMask=Json::array(),rotations=Json::array(),rotationMask=Json::array();
            for(uint32_t i=0;i<local.bone_count;++i){const auto& bone=target.profile.bones[i];names.push_back(bone.name);parents.push_back(bone.parent);
                positions.push_back(target.canonical_local_positions[i]);positionMask.push_back(target.canonical_position_mask[i]);
                rotations.push_back(target.canonical_local_rotations[i]);rotationMask.push_back(target.canonical_rotation_mask[i]);}
            report["compiled_target_clip"]={{"path",Decode(compiledPath.string(),CP_ACP)},{"fps",60},{"frame_count",local.frame_count},
                {"bone_count",local.bone_count},{"duration_seconds",local.duration},{"source_to_target_scale",scale},
                {"camera_source_to_component_rotation_xyzw",Quat(compiler.SourceToTargetBasis())},
                {"translation_scale_source",estimatedScale?"GenericStandardMmd geometry estimate; source PMX not supplied":"explicit CLI --scale override"},
                {"skeleton_name",target.skeleton_name},{"bone_names",names},{"parents",parents},
                {"canonical_local_positions",positions},{"canonical_position_mask",positionMask},
                {"canonical_local_rotations",rotations},{"canonical_rotation_mask",rotationMask}};
            if(!omitProvenance){report["compiled_target_clip"]["sha256"]=Sha256(compiledPath);report["compiled_target_clip"]["skeleton_sha256"]=local.skeleton_sha256;}
        }
        Save(output,report);
        std::printf("VMD bone_tracks=%zu morph_tracks=%zu IK_tracks=%zu cameras=%zu duration=%.3fs\n",clip.motion.bones.size(),clip.motion.morphs.size(),clip.motion.ik.size(),clip.motion.cameras.size(),clip.DurationSeconds());return 0;
    }catch(const std::exception& e){std::fprintf(stderr,"%s\n",e.what());return 2;}}
