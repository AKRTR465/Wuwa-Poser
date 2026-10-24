#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include "../playback/ww_face_profile.h"
#include "../core_math/third_party/nlohmann/json.hpp"
#include <cstdio>
#include "mmd_compile_common.h"
using Json=nlohmann::json;namespace face=wwp_playback::face;namespace vmd=wwp_playback::vmd;
using wwp_mmd_compile::Decode;
template<class T>static void Write(std::ostream& out,T value){out.write((char*)&value,sizeof(value));}
int wmain(int argc,wchar_t** argv){
    try{std::vector<std::filesystem::path> positional;std::string modelId,meshName;
        for(int i=1;i<argc;++i){const std::wstring arg=argv[i];
            if(arg==L"--model-id"&&i+1<argc)modelId=std::filesystem::path(argv[++i]).string();
            else if(arg==L"--mesh-asset-name"&&i+1<argc)meshName=std::filesystem::path(argv[++i]).string();
            else if(arg.rfind(L"--",0)==0)throw std::runtime_error("unknown face compiler option");
            else positional.emplace_back(argv[i]);}
        if(positional.size()!=4||modelId.empty()||meshName.empty())throw std::runtime_error("usage: mmd_face_compile VMD MORPH_CENSUS_JSON NEW_PROFILE_JSON NEW_EXPR_BINARY --model-id MODEL --mesh-asset-name MESH");
        auto clip=vmd::ReadFile(positional[0],Decode);
        std::ifstream file(positional[1]);if(!file)throw std::runtime_error("morph census unavailable");Json census;file>>census;std::vector<std::string> targets;bool found=false;
        for(const auto& model:census.at("models"))if(model.value("model_id","")==modelId){
            targets=model.contains("mesh_morph_names")&&model.at("mesh_morph_names").contains(meshName)?model.at("mesh_morph_names").at(meshName).get<std::vector<std::string>>():
                model.at("morphs").get<std::vector<std::string>>();
            found=true;}
        if(!found)throw std::runtime_error("selected model missing from morph census: "+modelId);
        auto profile=face::Build(clip.motion,targets,modelId);
        Json bindings=Json::array(),unmapped=Json::array();std::set<std::string> mappedTargets;unsigned mapped=0;
        for(const auto& binding:profile.bindings){Json bindingTargets=Json::array();for(const auto& target:binding.targets){bindingTargets.push_back({{"morph",target.morph},{"gain",target.gain}});mappedTargets.insert(target.morph);}
            bindings.push_back({{"source",binding.source},{"status",binding.status},{"reason",binding.reason},{"targets",bindingTargets}});if(binding.targets.empty())unmapped.push_back(binding.source);else ++mapped;}
        const uint32_t frames=uint32_t(std::ceil(clip.DurationSeconds()*60))+1;
        unsigned eyeTracks=0;for(const char* name:{u8"両目",u8"左目",u8"右目"})eyeTracks+=clip.motion.bones.count(name)?1:0;
        if(!targets.empty()){std::ofstream binary(positional[3],std::ios::binary);if(!binary)throw std::runtime_error("cannot create expression binary");
            binary.write("WWEXPR01",8);Write<uint32_t>(binary,1);Write<uint32_t>(binary,uint32_t(targets.size()));Write<uint32_t>(binary,frames);Write<uint32_t>(binary,60);Write<double>(binary,(frames-1)/60.);
            for(uint32_t f=0;f<frames;++f){auto sample=face::Sample(clip.motion,profile,f/60.);binary.write((const char*)sample.weights.data(),std::streamsize(sample.weights.size()*4));
                for(unsigned i=0;i<3;++i){binary.write((const char*)&sample.source_eye_rotations[i],16);Write<uint32_t>(binary,sample.source_eye_present[i]);}}
            if(!binary)throw std::runtime_error("expression binary write failed");}
        Json report={{"schema",2},{"model_id",profile.model},{"mesh_asset_name",meshName},{"duration_seconds",clip.DurationSeconds()},{"fps",60},{"frames",frames},
            {"target_morph_names",targets},{"mapped_source_tracks",mapped},{"bindings",bindings},{"unmapped_channels",unmapped},
            {"expression_supported",!targets.empty()},{"expression_enabled",!mappedTargets.empty()},
            {"expression_binary",targets.empty()?Json(nullptr):Json(positional[3].string())}};
        std::ofstream output(positional[2]);output<<report.dump(2)<<'\n';if(!output)throw std::runtime_error("expression report write failed");
        std::printf("face model=%s proposed=%u unmapped=%zu eyes=%u targetmorphs=%zu frames=%u\n",modelId.c_str(),mapped,unmapped.size(),eyeTracks,targets.size(),frames);return 0;
    }catch(const std::exception& e){std::fprintf(stderr,"%s\n",e.what());return 2;}}
