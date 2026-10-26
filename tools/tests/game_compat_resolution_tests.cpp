// Synthetic mapped images only. No game access, injection, hooks or game calls.
#include "../../probes/v20_recon/game_compat_witness.h"
#include <cstdlib>
#include <cstring>
#include <iostream>
#include <limits>

namespace r=ww_game_compat_resolution;
namespace w=ww_game_compat_witness;
using Json=nlohmann::json;
static unsigned assertions=0;
static void Check(bool ok,const char* message){++assertions;if(!ok){std::cerr<<"FAIL: "<<message<<'\n';std::exit(1);}}
struct Memory {
    uint64_t base=0x10000,fail=0;std::vector<uint8_t> bytes;unsigned reads=0;
    explicit Memory(size_t size):bytes(size){}
    bool Read(uint64_t address,void* out,size_t count){++reads;
        if(address<base||address-base>=bytes.size()||!count||count>bytes.size()-(address-base)||
           (fail&&address<=fail&&fail-address<count))return false;
        std::memcpy(out,bytes.data()+size_t(address-base),count);return true;}
    auto Reader(){return [this](uint64_t address,void* out,size_t count){return Read(address,out,count);};}
    void Put16(size_t offset,uint16_t value){bytes.at(offset)=uint8_t(value);bytes.at(offset+1)=uint8_t(value>>8);}
    void Put32(size_t offset,uint32_t value){for(unsigned i=0;i<4;++i)bytes.at(offset+i)=uint8_t(value>>(i*8));}
    void Put(size_t offset,std::initializer_list<uint8_t> value){std::copy(value.begin(),value.end(),bytes.begin()+offset);}
    void PE(){
        Put16(0,0x5a4d);Put32(60,0x80);Put32(0x80,0x00004550);Put16(0x84,0x8664);Put16(0x86,2);Put16(0x94,0xf0);
        Put16(0x98,0x20b);Put32(0x98+56,uint32_t(bytes.size()));
        Put32(0x188+8,0x200);Put32(0x188+12,0x400);Put32(0x188+16,0x200);Put32(0x188+36,0x60000020);
        Put32(0x1b0+8,0x200);Put32(0x1b0+12,0x800);Put32(0x1b0+16,0x200);Put32(0x1b0+36,0xc0000040);
    }
};
static r::Pattern Pattern(const char* bytes,const char* mask=""){r::Pattern p;std::string error;Check(r::ParsePattern(bytes,mask,p,error),"test pattern parses");return p;}
static void Parsing(){
    uint64_t value=9;std::string error;std::vector<uint8_t> bytes;
    Check(r::ParseHexU64("0xFFFFFFFFFFFFFFFF",value,error)&&value==std::numeric_limits<uint64_t>::max(),"maximum integer parses");
    Check(r::ParseHexU64("aBcD",value,error)&&value==0xabcd,"compact mixed case integer parses");
    for(const auto text:{"","0x","-1","+1"," 1","1 ","1g","10000000000000000","0x1junk"})
        Check(!r::ParseHexU64(text,value,error)&&value==0&&!error.empty(),"invalid or overflowing integer rejects without stale result");
    Check(r::ParseHexBytes("00aAFF",bytes,error)&&bytes==std::vector<uint8_t>({0,0xaa,0xff}),"complete mixed case bytes parse");
    for(const auto text:{"","0","0xAA","aa bb","0G","aa\n"})Check(!r::ParseHexBytes(text,bytes,error)&&bytes.empty(),"invalid compact bytes reject without partial output");
    r::Pattern p;Check(!r::ParsePattern("aabb","ff",p,error),"mask length mismatch rejects");
    Check(!r::ParsePattern("aabb","0000",p,error),"all-wildcard signature rejects");
    Check(!r::ValidPattern({{1}, {}}),"malformed typed pattern rejects");
}
static void Verification(){
    Memory memory(8);memory.Put(4,{0xaa,0xbb,0xcc,0xdd});auto read=memory.Reader();const auto bytes=Pattern("aabbccdd").bytes;
    Check(r::VerifyBytes(read,memory.base,memory.bytes.size(),4,bytes)==r::VerifyStatus::Match,"function ending at final image byte verifies");
    Check(r::VerifyBytes(read,memory.base,memory.bytes.size(),3,bytes)==r::VerifyStatus::Mismatch,"changed function bytes reject");
    const auto before=memory.reads;
    Check(r::VerifyBytes(read,memory.base,memory.bytes.size(),5,bytes)==r::VerifyStatus::InvalidRange&&memory.reads==before,"function crossing image boundary is not read");
    Check(r::VerifyBytes(read,memory.base,memory.bytes.size(),8,bytes)==r::VerifyStatus::InvalidRange,"RVA at image end rejects");
    Check(r::VerifyBytes(read,std::numeric_limits<uint64_t>::max()-1,8,0,bytes)==r::VerifyStatus::InvalidRange,"overflowing image range rejects");
    Check(r::VerifyBytes(read,memory.base,memory.bytes.size(),0,{})==r::VerifyStatus::InvalidBytes,"empty function is not verification");
    memory.fail=memory.base+5;Check(r::VerifyBytes(read,memory.base,memory.bytes.size(),4,bytes)==r::VerifyStatus::ReadFailed,"unreadable expected code rejects");
}
static void Scanning(){
    Memory memory(48);auto read=memory.Reader();const auto p=Pattern("aabbccdd");const std::vector<r::Range> ranges={{memory.base,memory.bytes.size()}};
    memory.Put(7,{0xaa,0xbb,0xcc,0xdd});auto scan=r::FindUnique(read,ranges,p,8);
    Check(scan.status==r::ScanStatus::Unique&&scan.address==memory.base+7&&scan.complete,"signature crossing chunk boundary resolves uniquely");
    memory.Put(44,{0xaa,0xbb,0xcc,0xdd});scan=r::FindUnique(read,ranges,p,8);
    Check(scan.status==r::ScanStatus::Ambiguous&&scan.address==0&&scan.matches==2,"duplicate candidate at last legal offset rejects uniqueness");
    std::fill(memory.bytes.begin(),memory.bytes.end(),uint8_t(0));memory.Put(44,{0xaa,0xbb,0xcc,0xdd});scan=r::FindUnique(read,ranges,p,8);
    Check(scan.status==r::ScanStatus::Unique&&scan.address==memory.base+44,"last legal start is scanned");
    memory.bytes[44]=0;Check(r::FindUnique(read,ranges,p,8).status==r::ScanStatus::NotFound,"absent signature has no candidate");
    memory.Put(6,{0xaf,0x12,0x3c,0xdf});const auto masked=Pattern("a00030d0","f000f0f0");
    Check(r::FindUnique(read,ranges,masked,8).status==r::ScanStatus::Unique,"explicit bit masks constrain matching");
    Check(r::FindUnique(read,{{memory.base+6,2},{memory.base+8,2}},masked,8).status==r::ScanStatus::NotFound,"pattern never bridges disjoint declared ranges");
    Check(r::FindUnique(read,{{memory.base,16},{memory.base+8,16}},p).status==r::ScanStatus::InvalidRange,"overlapping ranges reject duplicate counting");
    Check(r::FindUnique(read,{{std::numeric_limits<uint64_t>::max()-1,8}},p).status==r::ScanStatus::InvalidRange,"scan overflow rejects");
    Check(r::FindUnique(read,ranges,{{1},{0}}).status==r::ScanStatus::InvalidPattern,"unconstrained typed signature rejects");
    std::fill(memory.bytes.begin(),memory.bytes.end(),uint8_t(0));memory.Put(0,{0xaa,0xbb,0xcc,0xdd});memory.fail=memory.base+30;scan=r::FindUnique(read,ranges,p,8);
    Check(scan.status==r::ScanStatus::ReadFailed&&scan.address==0&&!scan.complete&&scan.matches==1,"candidate before unreadable tail is never called unique");
}
static void PEAndWitnesses(){
    Memory memory(0x1000);memory.PE();auto read=memory.Reader();std::vector<r::Range> ranges;std::string error;
    Check(r::ExecutableRanges(read,memory.base,memory.bytes.size(),ranges,error)&&ranges.size()==1&&ranges[0].address==memory.base+0x400&&ranges[0].bytes==0x200,"only mapped executable PE32+ section is selected");
    memory.Put(0x440,{0xaa,0xbb,0xcc,0xdd});memory.Put(0x820,{0xaa,0xbb,0xcc,0xdd});
    Check(r::FindUnique(read,ranges,Pattern("aabbccdd")).status==r::ScanStatus::Unique,"same bytes in nonexecutable data do not make code search ambiguous");
    const std::vector<w::Function> required={{"SyntheticFunction",0x440,4}};
    Json witness=Json::array({Json{{"rva","0x440"},{"bytes",4},{"function_bytes_hex","aabbccdd"}}});Json checks=Json::array();
    Check(w::VerifyFunctions(read,memory.base,memory.bytes.size(),witness,required,checks,"synthetic",error)&&checks[0]["matched"]==true,"complete live function bytes authorize the configured address");
    Check(checks[0]["performed"]==true&&checks[0]["executable_section_verified"]==true,"report records actual reads and executable bounds");
    witness[0]["function_bytes_hex"]="aa";checks=Json::array();
    Check(!w::VerifyFunctions(read,memory.base,memory.bytes.size(),witness,required,checks,"synthetic",error)&&checks[0]["performed"]==false,"truncated witness cannot authorize even a matching prefix");
    witness[0]["function_bytes_hex"]="aabbccdd";witness[0]["rva"]="0x440garbage";checks=Json::array();
    Check(!w::VerifyFunctions(read,memory.base,memory.bytes.size(),witness,required,checks,"synthetic",error),"trailing junk in witness RVA rejects");
    witness[0]["rva"]="0x440";witness.push_back(witness[0]);checks=Json::array();
    Check(!w::VerifyFunctions(read,memory.base,memory.bytes.size(),witness,required,checks,"synthetic",error),"extra or duplicate witness rows reject");witness.erase(1);
    memory.bytes[0x440]=0;memory.Put(0x460,{0xaa,0xbb,0xcc,0xdd});checks=Json::array();
    Check(!w::VerifyFunctions(read,memory.base,memory.bytes.size(),witness,required,checks,"synthetic",error),"relocated exact function is refused for execution");
    const auto candidate=checks[0]["relocation_candidate"];
    Check(candidate["status"]=="unique_candidate"&&candidate["candidate_rva"]=="0x460"&&candidate["candidate_requires_review"]==true&&candidate["execution_authorized"]==false&&candidate["exact_function_bytes_match"]==true,"unique relocated function is diagnostic evidence only");
    const std::vector<w::Function> dataRequirement={{"DataImpersonatingCode",0x820,4}};
    witness[0]["rva"]="0x820";checks=Json::array();
    Check(!w::VerifyFunctions(read,memory.base,memory.bytes.size(),witness,dataRequirement,checks,"synthetic",error)&&checks[0]["executable_section_verified"]==false,"matching data bytes cannot authorize function calls");
    Json cfg={{"compatibility_profile","p"},{"compatibility_source_sha256","digest"}};checks=Json::array();
    Check(w::CompatibleSource(cfg,"p","digest",checks,error)&&checks[0]["game_abi_verified"]==false,"same-source check never claims game ABI validation");
    cfg.erase("compatibility_source_sha256");Check(!w::CompatibleSource(cfg,"p","digest",checks,error),"missing compiled-source fingerprint rejects");
    memory.Put32(0x188+12,0xff0);Check(!r::ExecutableRanges(read,memory.base,memory.bytes.size(),ranges,error)&&ranges.empty(),"executable section crossing image end rejects without partial ranges");
    memory.PE();memory.Put32(0x1b0+36,0x60000020);memory.Put32(0x1b0+12,0x500);
    Check(!r::ExecutableRanges(read,memory.base,memory.bytes.size(),ranges,error),"overlapping executable PE sections reject");
    memory.PE();memory.Put32(60,0xff8);Check(!r::ExecutableRanges(read,memory.base,memory.bytes.size(),ranges,error),"truncated NT header rejects");
    memory.PE();memory.Put16(0x86,97);Check(!r::ExecutableRanges(read,memory.base,memory.bytes.size(),ranges,error),"unbounded section count rejects");
    memory.PE();memory.Put32(0x98+56,0x2000);Check(!r::ExecutableRanges(read,memory.base,memory.bytes.size(),ranges,error),"PE and actual image extent mismatch rejects");
}
int main(){try{Parsing();Verification();Scanning();PEAndWitnesses();std::cout<<"game compatibility resolution: "<<assertions<<" assertions passed\n";return 0;}
    catch(const std::exception& error){std::cerr<<"unexpected exception: "<<error.what()<<'\n';return 1;}}
