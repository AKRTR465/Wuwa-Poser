// Synthetic mapped code and this test process only. No game, hooks or engine calls.
#include "../../probes/v20_recon/shared_native_physics.h"
#include <cstdlib>
#include <iostream>
#include <memory>

namespace native=v20_shared_native_physics;
namespace witness=ww_game_compat_witness;
static unsigned assertions=0;
static void Check(bool value,const char* message){++assertions;if(!value){std::cerr<<"FAIL: "<<message<<'\n';std::exit(1);}}
struct Image {
    struct Code {uint64_t rva=0;std::vector<uint8_t> bytes;};
    uint64_t base=0x100000,image_bytes=0x6000000;unsigned reads=0;
    std::array<uint8_t,0x200> header{};std::vector<Code> functions;
    template<class T>void Put(size_t offset,T value){std::memcpy(header.data()+offset,&value,sizeof(value));}
    Image(){
        Put<uint16_t>(0,0x5a4d);Put<uint32_t>(60,0x80);Put<uint32_t>(0x80,0x4550);
        Put<uint16_t>(0x84,0x8664);Put<uint16_t>(0x86,1);Put<uint16_t>(0x94,0xf0);
        Put<uint16_t>(0x98,0x20b);Put<uint32_t>(0x98+56,uint32_t(image_bytes));
        Put<uint32_t>(0x188+8,uint32_t(image_bytes-0x1000));Put<uint32_t>(0x188+12,0x1000);
        Put<uint32_t>(0x188+16,uint32_t(image_bytes-0x1000));Put<uint32_t>(0x188+36,0x60000020);
    }
    bool Read(uint64_t address,void* out,size_t bytes){++reads;if(address<base)return false;const auto offset=address-base;
        if(offset<header.size()&&bytes<=header.size()-size_t(offset)){std::memcpy(out,header.data()+size_t(offset),bytes);return true;}
        for(const auto& code:functions)if(offset>=code.rva&&offset-code.rva<code.bytes.size()&&bytes<=code.bytes.size()-size_t(offset-code.rva)){
            std::memcpy(out,code.bytes.data()+size_t(offset-code.rva),bytes);return true;}
        return false;
    }
    Json Rows(const std::vector<witness::Function>& specs){Json rows=Json::array();
        for(const auto& spec:specs){functions.push_back({spec.rva,std::vector<uint8_t>(spec.bytes,uint8_t(functions.size()+1))});
            const auto& bytes=functions.back().bytes;
            rows.push_back({{"rva",witness::Hex(spec.rva)},{"function_bytes_hex",BytesHex(bytes.data(),bytes.size())}});}
        return rows;
    }
};
static unsigned Matched(const Json& checks){unsigned count=0;for(const auto& row:checks)if(row.value("kind",std::string())=="function_bytes"&&row.value("matched",false))++count;return count;}
static Json Source(){return {{"compatibility_profile",ww_game_compat::ProfileId},{"compatibility_source_sha256",ww_game_compat::SourceSha256}};}
static void RequiredSets(){
    Image image;auto cfg=Source();cfg["native_physics_witnesses"]=image.Rows(native::InputFunctionSpecs());
    cfg["native_physics_activation_witnesses"]=image.Rows(native::ActivationFunctionSpecs());
    Json checks=Json::array();std::string error;
    Check(native::InputFunctionSpecs().size()==4&&native::ActivationFunctionSpecs().size()==4,"both required native function sets are complete");
    Check(native::VerifyNativeFunctionSets(image,image.base,image.image_bytes,cfg,checks,error)&&error.empty()&&Matched(checks)==8,"all eight full functions verify before preparation");
    Check(checks.size()==9&&checks[0]["game_abi_verified"]==false,"source consistency and actual function checks remain distinct");
    auto incomplete=cfg;incomplete.erase("native_physics_activation_witnesses");checks=Json::array();
    Check(!native::VerifyNativeFunctionSets(image,image.base,image.image_bytes,incomplete,checks,error)&&error.find("native_activation")!=std::string::npos,"missing activation witnesses reject despite valid input functions");
    auto truncated=cfg;truncated["native_physics_witnesses"][1]["function_bytes_hex"]="01";checks=Json::array();
    Check(!native::VerifyNativeFunctionSets(image,image.base,image.image_bytes,truncated,checks,error)&&Matched(checks)==7,"matching short prefix cannot authorize the native getter");
    image.functions[0].bytes[0]^=0xff;checks=Json::array();
    Check(!native::VerifyNativeFunctionSets(image,image.base,image.image_bytes,cfg,checks,error),"changed configured native function rejects execution");
    Check(checks[1]["execution_authorized"]==false&&checks[1]["relocation_candidate"]["execution_authorized"]==false,"failed byte check and candidate scan cannot authorize a new address");
    image.functions[0].bytes[0]^=0xff;
    auto foreign=cfg;foreign["compatibility_source_sha256"]="different-source";checks=Json::array();
    Check(!native::VerifyNativeFunctionSets(image,image.base,image.image_bytes,foreign,checks,error)&&Matched(checks)==8,"matching code does not bypass compatibility source mismatch");
}
static void PreparationRejects(const std::filesystem::path& output){
    Image image;auto actor=std::make_unique<native::Actor>();bool rejected=false;
    try{native::PrepareBinding(*actor,image,Source(),"synthetic",output,nullptr,nullptr);}catch(const std::exception&){rejected=true;}
    Check(rejected&&!actor->prepared&&!actor->function_witnesses_verified.load(),"preparation refuses missing witnesses before graph binding");
    Check(image.reads==0,"missing witness sets do not touch pose data or execute native functions");
    const auto report=nlohmann::json::parse(std::ifstream(output/L"native_function_checks.json"));
    Check(report["verified"]==false&&report["required_function_count"]==8&&report["matched_function_count"]==0,"failed preparation writes a complete diagnostic report");
    Check(report["candidate_relocation_authorized"]==false,"report preserves the diagnostic-only candidate boundary");
    Check(!native::RegisterActor(*actor),"unverified actor cannot enter the native registry");
    actor->function_witnesses_verified.store(true);
    Check(!native::RegisterActor(*actor),"function verification alone cannot admit an unprepared actor");
}
static void ScopedSettings() {
    namespace parameters=wwp_playback::kawaii_parameters;
    std::array<uint8_t,0xa00> memory{};
    const auto node=reinterpret_cast<uintptr_t>(memory.data());const auto bones=node+0x300;
    native::Access access{};access.user=&memory;
    access.writable=[](void* user,uintptr_t address,size_t bytes){const auto base=reinterpret_cast<uintptr_t>(user);return address>=base&&address-base<=0xa00&&bytes<=0xa00-(address-base);};
    access.read=[](void*,uintptr_t address,void* data,size_t bytes){std::memcpy(data,reinterpret_cast<void*>(address),bytes);return true;};
    access.write=[](void*,uintptr_t address,const void* data,size_t bytes){std::memcpy(reinterpret_cast<void*>(address),data,bytes);return true;};
    const parameters::Settings baseline={.2f,.98f,.95f,.2f,1.2f,50,5};
    const parameters::Array array{bones,2,2};
    const auto write=[&](size_t offset,const auto& value){access.write(access.user,node+offset,&value,sizeof(value));};
    write(parameters::kBoneArray,array);write(parameters::kNodeSettings,baseline);
    write(parameters::kInitialized,uint8_t(1));
    const auto refresh=[&](uintptr_t){parameters::Settings current{};access.Get(node+parameters::kNodeSettings,current);
        write(0x300+parameters::kBoneSettings,current);current[0]*=.5f;
        write(0x300+parameters::kBoneSettings+parameters::kBoneStride,current);return true;};
    refresh(node);parameters::Group group;group.requested={.45f,-1,65};group.revision=43;
    unsigned solves=0,faults=0;
    const auto evaluate=[&]{++solves;uint8_t enabled=0;access.Get(node+parameters::kRefresh,enabled);if(enabled)refresh(node);};
    parameters::EvaluateGroup(group,node,access,true,evaluate,refresh,[&]{++faults;});
    Check(solves==1&&faults==0&&!group.restore_pending.load(),"selected group solves once and retires its writes");
    Check(group.sampled.revision==43&&group.sampled.original==parameters::Values{.2f,.2f,50}&&
        group.sampled.effective_first==parameters::Values{.45f,.2f,65}&&group.sampled.effective_last[0]==.225f,
        "readback captures original values and effective per-bone curve values");
    parameters::Sample returned;parameters::Settings after{};uint8_t refreshFlag=9;
    access.Get(node+parameters::kNodeSettings,after);access.Get(node+parameters::kRefresh,refreshFlag);
    Check(after==baseline&&refreshFlag==0&&parameters::ReadSettingsCache(access,node,returned)&&
        returned.effective_first==parameters::Values{.2f,.2f,50},"authored node values, refresh flag and effective cache are restored after evaluation");
    group.requested[0]=-1;group.revision=44;
    parameters::EvaluateGroup(group,node,access,true,evaluate,refresh,[&]{++faults;});
    Check(group.sampled.effective_first==parameters::Values{.2f,.2f,65},"one-parameter reset preserves another override");
    parameters::EvaluateGroup(group,node,access,false,evaluate,refresh,[&]{++faults;});
    Check(group.sampled.effective_first==parameters::Values{.2f,.2f,50},"closed playback feed evaluates stock physics");

    parameters::Group separate;
    separate.chains.resize(2);separate.skeleton_chain={1,2,0};
    separate.chains[0].requested={.05f,-1,-1};separate.chains[0].revision=45;
    separate.requested={-1,.11f,-1};separate.revision=46;
    write(parameters::kBoneArray,parameters::Array{bones,3,3});
    std::array<int32_t,1024> indices{};indices.fill(-1);
    for(int32_t i=0;i<3;++i){indices[size_t(i)]=i;write(0x300+size_t(i)*parameters::kBoneStride+parameters::kBoneIdentity,std::array<int32_t,2>{0,i});}
    const auto refresh_chains=[&](uintptr_t){parameters::Settings current{};access.Get(node+parameters::kNodeSettings,current);
        for(unsigned i=0;i<3;++i){auto effective=current;effective[0]*=1+float(i);write(0x300+i*parameters::kBoneStride+parameters::kBoneSettings,effective);}return true;};
    const auto map=[&]{return parameters::MapChains(separate,node,access,indices);};
    const auto evaluate_chains=[&]{uint8_t setting=1;access.Get(node+parameters::kRefresh,setting);
        if(setting)refresh_chains(node);
        parameters::Settings first{},second{},unmapped{};
        access.Get(bones+parameters::kBoneSettings,first);access.Get(bones+parameters::kBoneSettings+parameters::kBoneStride,second);
        access.Get(bones+parameters::kBoneSettings+2*parameters::kBoneStride,unmapped);
        Check(parameters::Select(first)==parameters::Values{.05f,.11f,50}&&parameters::Select(second)==parameters::Values{.4f,.11f,50}&&
             parameters::Select(unmapped)==parameters::Values{.6f,.11f,50},"only the selected chain overrides the group's cache; unmapped records retain group settings");};
    parameters::EvaluateGroup(separate,node,access,true,evaluate_chains,refresh_chains,[&]{++faults;},map);
    Check(faults==0&&separate.chains[0].sampled.revision==45&&separate.chains[0].sampled.effective_first[0]==.05f&&
        separate.chains[1].sampled.effective_first[0]==.4f&&!separate.restore_pending.load(),"chain readback retains separate applied revision and values");
    Check(parameters::ReadSettingsCache(access,node,returned)&&returned.effective_first==parameters::Values{.2f,.2f,50},
        "after a chain override the stock per-bone cache is rebuilt");
}
static void SleeveAttachment() {
    namespace sleeve=wwp_playback::sleeve_follow;
    const double root_half=std::sqrt(.5);
    std::array<wwp_ue::subtree_rotation::TransformBytes,5> pose{};
    const auto store=[&](unsigned i,const sleeve::Q& rotation,const sleeve::V& position){
        for(unsigned axis=0;axis<rotation.size();++axis)sleeve::bone::Store(pose[i].data()+ww_game_compat::TransformRotationOffset+axis*4,rotation[axis]);
        for(unsigned axis=0;axis<position.size();++axis)sleeve::bone::Store(pose[i].data()+ww_game_compat::TransformTranslationOffset+axis*4,position[axis]);};
    store(0,{0,0,0,1},{0,0,0});store(1,{0,0,root_half,root_half},{0,0,0});
    store(2,{0,0,root_half,root_half},{2,4,0});store(3,{0,0,root_half,root_half},{2,5,0});store(4,{0,0,0,1},{-2,-4,0});
    const auto untouched=pose; sleeve::Chain chain;
    chain.root=2;chain.shoulder=1;chain.shoulder_parent=0;chain.neutral_shoulder={0,0,0,1};chain.bones={2,3};chain.follow=0;
    Check(sleeve::Apply(chain,pose.data())&&std::abs(chain.correction_degrees-90)<1e-4,"zero follows none of the shoulder rotation");
    const auto next=sleeve::bone::Position(pose[3]);
    Check(sleeve::bone::Position(pose[2])==sleeve::bone::Position(untouched[2])&&std::abs(next[0]-3)<1e-5&&std::abs(next[1]-4)<1e-5,
        "the sleeve rotates around its unchanged cuff attachment point");
    Check(pose[1]==untouched[1]&&pose[4]==untouched[4],"the shoulder and adjacent chain are untouched");
    pose=untouched;chain.follow=.5f;
    Check(sleeve::Apply(chain,pose.data())&&std::abs(chain.correction_degrees-45)<1e-4,"half follow removes half the shoulder rotation");
    pose=untouched;chain.follow=1;
    Check(sleeve::Apply(chain,pose.data())&&pose==untouched,"stock follow is an exact pass-through");
}
static void CapsuleIsolation() {
    namespace collision=wwp_playback::kawaii_collision;
    namespace parameters=wwp_playback::kawaii_parameters;
    alignas(16) std::array<uint8_t,0x900> node{};
    const auto base=reinterpret_cast<uintptr_t>(node.data());
    std::array<collision::Capsule,3> capsules{};
    for(int32_t i=0;i<3;++i){const std::array<int32_t,2> id={0,i};
        std::memcpy(capsules[size_t(i)].bytes.data()+collision::kCapsuleIdentity,id.data(),sizeof(id));}
    native::Access access{};
    access.read=[](void*,uintptr_t address,void* data,size_t bytes){std::memcpy(data,reinterpret_cast<void*>(address),bytes);return true;};
    access.write=[](void*,uintptr_t address,const void* data,size_t bytes){std::memcpy(reinterpret_cast<void*>(address),data,bytes);return true;};
    access.writable=[](void*,uintptr_t,size_t){return true;};
    const parameters::Array original{reinterpret_cast<uintptr_t>(capsules.data()),3,3};
    access.write(nullptr,base+collision::kCapsuleArrays[0],&original,sizeof(original));
    access.write(nullptr,base+parameters::kBoneArray,&original,sizeof(original));
    collision::State state;unsigned faults=0;
    const auto map=[](auto& inverse){inverse.fill(-1);inverse[0]=25;inverse[1]=27;inverse[2]=30;return true;};
    collision::EvaluateCapsules(state,base,access,true,map,[]{},[&]{++faults;});
    Check(state.available&&state.drivers.size()==3,"capsules are identified by driver using the current compact mapping");
    state.drivers[0].enabled=false;
    const auto evaluate=[&]{parameters::Array used{};access.Get(base+collision::kCapsuleArrays[0],used);
        collision::Capsule first{},second{};access.Get(used.data,first);access.Get(used.data+sizeof(first),second);
        Check(used.count==2&&first.bytes==capsules[1].bytes&&second.bytes==capsules[2].bytes,"filter removes only the selected driver's capsule and retains order");};
    collision::EvaluateCapsules(state,base,access,true,map,evaluate,[&]{++faults;});
    parameters::Array restored{};access.Get(base+collision::kCapsuleArrays[0],restored);
    Check(faults==0&&parameters::SameArray(restored,original)&&state.drivers[0].active==0&&state.drivers[1].active==1&&!state.restore_pending.load(),
        "authored capsule header is restored and evaluation counts are isolated");
}
static void GravityIsolation(){
    namespace gravity=wwp_playback::kawaii_gravity;
    std::array<uint8_t,0x300> memory{};
    const auto node=reinterpret_cast<uintptr_t>(memory.data());
    const auto address=node+gravity::kNodeGravity;
    const gravity::Vector stock{20,5,-300};
    std::memcpy(reinterpret_cast<void*>(address),stock.data(),sizeof(stock));
    const auto intact=memory;
    native::Access access{};
    access.writable=[](void*,uintptr_t,size_t){return true;};
    access.read=[](void*,uintptr_t source,void* data,size_t size){std::memcpy(data,reinterpret_cast<void*>(source),size);return true;};
    access.write=[](void*,uintptr_t target,const void* data,size_t size){std::memcpy(reinterpret_cast<void*>(target),data,size);return true;};
    gravity::State state;state.requested=150;state.revision=8;
    gravity::Vector inside{};
    unsigned faults=0,solves=0;
    const auto evaluate=[&]{++solves;access.Get(address,inside);};
    const auto fault=[&]{++faults;};
    gravity::EvaluateGravity(state,node,access,true,evaluate,fault);
    Check(inside==gravity::Vector{0,0,-150}&&memory==intact&&solves==1&&faults==0,
        "gravity changes the world vector only during its own group's evaluation");
    Check(state.sample.revision==8&&state.sample.original==stock&&state.sample.effective==inside&&state.sample.applied==1&&state.sample.restored==1,
        "gravity reports solver input and a byte-verified restoration");
    state.requested=0;gravity::EvaluateGravity(state,node,access,true,evaluate,fault);
    Check(inside==gravity::Vector{}&&state.sample.applied==2,"zero is a custom gravity value");
    state.requested=-1;gravity::EvaluateGravity(state,node,access,true,evaluate,fault);
    Check(inside==stock&&state.sample.applied==2,"reset uses the authored vector including X/Y");
    state.requested=450;gravity::EvaluateGravity(state,node,access,false,evaluate,fault);
    Check(inside==stock&&state.sample.applied==2&&!state.restore_pending.load(),"closed feed uses authored gravity");
}
int main(int argc,char** argv){try{
    if(argc!=2)throw std::runtime_error("expected an evidence output directory");
    RequiredSets();PreparationRejects(std::filesystem::path(argv[1]));ScopedSettings();SleeveAttachment();CapsuleIsolation();GravityIsolation();
    std::cout<<assertions<<" native function witness assertions passed; game_accessed=false; native_calls=0\n";return 0;
}catch(const std::exception& error){std::cerr<<"unexpected exception: "<<error.what()<<'\n';return 1;}}
