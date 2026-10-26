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
int main(int argc,char** argv){try{
    if(argc!=2)throw std::runtime_error("expected an evidence output directory");
    RequiredSets();PreparationRejects(std::filesystem::path(argv[1]));
    std::cout<<assertions<<" native function witness assertions passed; game_accessed=false; native_calls=0\n";return 0;
}catch(const std::exception& error){std::cerr<<"unexpected exception: "<<error.what()<<'\n';return 1;}}
