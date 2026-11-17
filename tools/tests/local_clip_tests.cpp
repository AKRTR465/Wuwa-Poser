#include "../../playback/vmd_local_clip.h"
#include "../../playback/ww_camera_clip.h"
#include "../../playback/ww_face_clip.h"
#include <chrono>
#include <cstdlib>
#include <iostream>
#include <iterator>
#include <limits>

namespace clip=wwp_playback::local_clip;
static unsigned assertions=0;
static void Check(bool value,const char* message){
    ++assertions;if(!value){std::cerr<<message<<'\n';std::exit(1);}
}
template<class T>static void Put(std::vector<char>& bytes,size_t offset,T value){
    std::memcpy(bytes.data()+offset,&value,sizeof(value));
}
static void WriteFixture(const std::filesystem::path& path,const std::vector<char>& bytes){
    std::ofstream out(path,std::ios::binary|std::ios::trunc);
    out.write(bytes.data(),std::streamsize(bytes.size()));
    if(!out)throw std::runtime_error("cannot write test clip");
}
template<class Read> static void RejectFile(const std::filesystem::path& path,const std::vector<char>& bytes,Read read,const char* label){
    WriteFixture(path,bytes);
    try{(void)read(path);Check(false,label);}catch(const std::runtime_error&){Check(true,label);}
}
static void CameraFileChecks(const std::filesystem::path& path){
    namespace camera=wwp_playback::camera_clip;
    constexpr size_t source=camera::kHeaderBytes+3*camera::kRecordBytes;
    std::vector<char> bytes(source+2*camera::kSourceKeyBytes);
    std::memcpy(bytes.data(),"WWCAM001",8);
    Put<uint32_t>(bytes,8,2);Put<uint32_t>(bytes,12,60);Put<uint32_t>(bytes,16,3);
    Put(bytes,20,camera::kRecordBytes);Put(bytes,24,2./60);Put<uint32_t>(bytes,32,1);Put<uint32_t>(bytes,36,2);
    Put<uint32_t>(bytes,104,1);Put<uint32_t>(bytes,108,1);Put(bytes,112,camera::kSourceKeyBytes);
    Put<float>(bytes,source+8,30);Put(bytes,source+camera::kSourceKeyBytes,uint32_t(1));
    Put<float>(bytes,source+camera::kSourceKeyBytes+8,50);
    WriteFixture(path,bytes);
    const auto clip=camera::ReadFile(path);
    Check(clip.frame_count==3&&clip.source_keys.size()==2&&clip.source_keys.back().fov==50,"camera v2 source keys round-trip");
    auto changed=bytes;Put<uint32_t>(changed,8,1);RejectFile(path,changed,camera::ReadFile,"camera rejects mismatched version");
    changed=bytes;Put<uint32_t>(changed,36,10);RejectFile(path,changed,camera::ReadFile,"camera rejects declared keys beyond actual file extent");
    changed=bytes;changed.push_back(0);RejectFile(path,changed,camera::ReadFile,"camera rejects trailing bytes");
    changed=bytes;Put<uint32_t>(changed,source+camera::kSourceKeyBytes,0);RejectFile(path,changed,camera::ReadFile,"camera rejects repeated source frame");
    changed=bytes;Put(changed,source+40,std::numeric_limits<float>::quiet_NaN());RejectFile(path,changed,camera::ReadFile,"camera rejects invalid Bezier curve");
}
static void ExpressionFileChecks(const std::filesystem::path& path){
    namespace expression=wwp_playback::face;
    constexpr size_t header=expression::kExpressionHeaderBytes,stride=(1+expression::kEyeSamples)*sizeof(float);
    std::vector<char> bytes(header+2*stride);
    std::memcpy(bytes.data(),"WWEXPR01",8);
    Put<uint32_t>(bytes,8,1);Put<uint32_t>(bytes,12,1);Put<uint32_t>(bytes,16,2);
    Put(bytes,20,expression::kExpressionFps);Put(bytes,24,1./60);
    Put<float>(bytes,header,.25f);Put<float>(bytes,header+stride,.75f);
    WriteFixture(path,bytes);
    const auto clip=expression::ReadClip(path);
    float weight=0;
    Check(expression::SampleWeights(clip,1./120,&weight,1)&&std::abs(weight-.5f)<1e-5,"expression v1 weights interpolate");
    auto changed=bytes;Put<uint32_t>(changed,16,10);Put(changed,24,9./60);
    RejectFile(path,changed,expression::ReadClip,"expression rejects frames beyond actual file extent");
    changed=bytes;Put(changed,12,wwp_playback::face_snapshot::kMaxMorphs+1);
    RejectFile(path,changed,expression::ReadClip,"expression rejects larger catalogs than the runtime can load");
    changed=bytes;changed.pop_back();RejectFile(path,changed,expression::ReadClip,"expression rejects truncated payload");
    changed=bytes;Put(changed,header,std::numeric_limits<float>::infinity());
    RejectFile(path,changed,expression::ReadClip,"expression rejects non-finite weights");
}
static clip::Clip Fixture(){
    clip::Clip value;value.bone_count=2;value.frame_count=2;value.fps=60;value.duration=1./60;
    value.source_sha256=std::string(64,'a');value.skeleton_sha256=std::string(64,'b');
    value.frames.resize(4);
    for(size_t i=0;i<value.frames.size();++i)value.frames[i].mask=i%2?clip::kRotation:clip::kRotation|clip::kTranslationDelta;
    value.frames[2].translation_delta[0]=10;return value;
}
int main(){
    const auto directory=std::filesystem::temp_directory_path()/
        ("wwposer-local-clip-"+std::to_string(std::chrono::high_resolution_clock::now().time_since_epoch().count()));
    std::filesystem::create_directory(directory);
    struct Cleanup{std::filesystem::path path;~Cleanup(){std::error_code error;std::filesystem::remove_all(path,error);}} cleanup{directory};
    const auto path=directory/"motion.wwlocal";const auto valid=Fixture();clip::WriteFile(path,valid);
    const auto loaded=clip::ReadFile(path);
    Check(loaded.bone_count==2&&loaded.frame_count==2&&loaded.fps==60&&loaded.frames.size()==4,"valid clip dimensions round-trip");
    Check(loaded.source_sha256==valid.source_sha256&&loaded.skeleton_sha256==valid.skeleton_sha256&&!loaded.omit_provenance,"provenance digests round-trip");
    clip::BoneCommand sampled[2];
    Check(clip::Sample(loaded,1./120,sampled,2)&&std::fabs(sampled[0].translation_delta[0]-5)<1e-5,"validated clip interpolates its actual samples");
    Check(!clip::Sample(loaded,std::numeric_limits<double>::quiet_NaN(),sampled,2),"non-finite sampling cursor rejected");
    std::ifstream input(path,std::ios::binary);std::vector<char> bytes((std::istreambuf_iterator<char>(input)),{});input.close();
    const auto reject=[&](const std::vector<char>& changed,const char* label){
        RejectFile(path,changed,clip::ReadFile,label);
    };
    auto changed=bytes;changed[0]='X';reject(changed,"wrong magic rejected");
    changed=bytes;Put<uint32_t>(changed,8,2);reject(changed,"future format version rejected");
    for(const size_t offset:{size_t(12),size_t(16),size_t(20)}){changed=bytes;Put<uint32_t>(changed,offset,0);reject(changed,"zero dimensions rejected");}
    changed=bytes;Put<uint32_t>(changed,12,clip::kMaximumBones+1);reject(changed,"unsupported bone capacity rejected before allocation");
    changed=bytes;Put<uint32_t>(changed,16,clip::kMaximumFrames+1);reject(changed,"unsupported frame count rejected before allocation");
    changed=bytes;Put<uint32_t>(changed,12,UINT32_MAX);Put<uint32_t>(changed,16,UINT32_MAX);reject(changed,"multiplication overflow dimensions rejected");
    changed=bytes;Put<uint32_t>(changed,12,1024);Put<uint32_t>(changed,16,100000);Put<double>(changed,24,99999./60);reject(changed,"oversized payload budget rejected without allocation");
    for(double duration:{-1.,2.,std::numeric_limits<double>::quiet_NaN(),std::numeric_limits<double>::infinity()}){
        changed=bytes;Put<double>(changed,24,duration);reject(changed,"invalid duration rejected");
    }
    reject(std::vector<char>(bytes.begin(),bytes.begin()+159),"truncated header rejected");
    changed=bytes;changed.pop_back();reject(changed,"truncated payload rejected");
    changed=bytes;changed.push_back(0);reject(changed,"trailing payload rejected");
    changed=bytes;changed[32]='g';reject(changed,"non-hex source digest rejected");
    changed=bytes;changed[159]=0;reject(changed,"partially truncated skeleton digest rejected");
    changed=bytes;std::fill(changed.begin()+32,changed.begin()+96,'\0');reject(changed,"one omitted digest cannot bypass provenance validation");
    changed=bytes;Put<uint32_t>(changed,160+28,4);reject(changed,"unknown command mask rejected");
    changed=bytes;Put<uint32_t>(changed,160+32+28,3);reject(changed,"non-root translation rejected");
    changed=bytes;Put<uint32_t>(changed,160+64+28,1);reject(changed,"changing per-bone masks rejected");
    changed=bytes;Put<float>(changed,160,std::numeric_limits<float>::infinity());reject(changed,"infinite translation rejected");
    changed=bytes;Put<float>(changed,160+12,std::numeric_limits<float>::quiet_NaN());reject(changed,"NaN rotation rejected");
    changed=bytes;Put<float>(changed,160+24,0);reject(changed,"zero active quaternion rejected");
    changed=bytes;Put<float>(changed,160+24,2);reject(changed,"non-unit active quaternion rejected");
    auto omitted=valid;omitted.omit_provenance=true;omitted.source_sha256.clear();omitted.skeleton_sha256.clear();
    clip::WriteFile(path,omitted);const auto omittedLoaded=clip::ReadFile(path);
    Check(omittedLoaded.omit_provenance&&omittedLoaded.source_sha256.empty()&&omittedLoaded.skeleton_sha256.empty(),"explicit zero-filled provenance remains supported");
    auto unused=valid;for(size_t i:{size_t(1),size_t(3)}){unused.frames[i].mask=0;unused.frames[i].rotation_xyzw[3]=0;}
    clip::WriteFile(path,unused);Check(clip::ReadFile(path).frames[1].rotation_xyzw[3]==0,"unused zero quaternion remains supported");
    auto still=valid;still.frame_count=1;still.duration=0;still.frames.resize(2);clip::WriteFile(path,still);
    Check(clip::ReadFile(path).duration==0,"one-frame zero-duration pose remains supported");
    auto invalid=valid;invalid.source_sha256="short";const auto absent=directory/"invalid.wwlocal";
    try{clip::WriteFile(absent,invalid);Check(false,"short digest must not read past string storage");}catch(const std::runtime_error&){Check(!std::filesystem::exists(absent),"writer validates before creating an invalid file");}
    invalid=valid;invalid.frames.pop_back();
    try{clip::WriteFile(absent,invalid);Check(false,"writer must reject inconsistent payload");}catch(const std::runtime_error&){Check(!std::filesystem::exists(absent),"writer payload dimensions checked before file creation");}
    CameraFileChecks(directory/"camera.wwcam");
    ExpressionFileChecks(directory/"expression.wwexpr");
    std::cout<<"track file validation: "<<assertions<<" assertions passed\n";
}
