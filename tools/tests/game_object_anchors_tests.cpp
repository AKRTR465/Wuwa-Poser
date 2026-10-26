// Synthetic heap-backed PE/object/name data only. Never loads or hooks a game.
#include "../../probes/v20_recon/game_object_anchors.h"
#include <cstdlib>
#include <iostream>
static unsigned checks=0;
static void Check(bool value,const char* reason){++checks;if(!value){std::cerr<<reason<<'\n';std::exit(1);}}
template<class T>static void Put(uint64_t address,const T& value){std::memcpy(reinterpret_cast<void*>(address),&value,sizeof(value));}
struct Fixture {
    std::vector<uint64_t> image=std::vector<uint64_t>(0x3000/8);
    std::array<std::array<uint8_t,48>,8> objects{};
    std::array<std::array<uint8_t,24>,8> items{};
    std::array<uint8_t,128> name_data{};
    uint64_t chunk=uint64_t(items.data());
    uint64_t Base(){return uint64_t(image.data());}
    uint64_t Object(unsigned i){return uint64_t(objects[i].data());}
    Fixture(){
        IMAGE_DOS_HEADER dos{};dos.e_magic=IMAGE_DOS_SIGNATURE;dos.e_lfanew=0x80;Put(Base(),dos);
        IMAGE_NT_HEADERS64 nt{};nt.Signature=IMAGE_NT_SIGNATURE;nt.FileHeader.NumberOfSections=1;
        nt.FileHeader.SizeOfOptionalHeader=sizeof(IMAGE_OPTIONAL_HEADER64);nt.OptionalHeader.Magic=IMAGE_NT_OPTIONAL_HDR64_MAGIC;
        nt.OptionalHeader.SizeOfImage=0x3000;Put(Base()+0x80,nt);
        IMAGE_SECTION_HEADER section{};section.VirtualAddress=0x1000;section.Misc.VirtualSize=0x1000;
        section.Characteristics=IMAGE_SCN_MEM_READ|IMAGE_SCN_MEM_WRITE;Put(Base()+0x80+sizeof(nt),section);
        const auto go=Base()+0x1100;Put(go,uint64_t(&chunk));Put(go+16,uint32_t(65536));Put(go+20,uint32_t(8));Put(go+24,uint32_t(1));Put(go+28,uint32_t(1));
        const auto pool=Base()+0x1200;Put(pool+8,uint32_t(0));Put(pool+12,uint32_t(128));Put(pool+16,uint64_t(name_data.data()));
        Put(uint64_t(name_data.data())+4,uint16_t(8));std::memcpy(name_data.data()+6,"None",4);
        Put(uint64_t(name_data.data())+16,uint16_t(10));std::memcpy(name_data.data()+18,"Class",5);
        for(unsigned i=0;i<objects.size();++i){Put(Object(i),Base()+0x2000);Put(Object(i)+12,uint32_t(i));Put(Object(i)+16,Object(7));
            Put(uint64_t(items[i].data()),Object(i));Put(uint64_t(items[i].data())+16,uint32_t(100+i));}
        Put(Object(7)+24,uint32_t(3));
    }
};
int main(){
    Fixture fixture;Reader reader{GetCurrentProcess()};Go go;Pool pool;
    auto report=ww_game_object_anchors::Resolve(reader,fixture.Base(),0x1100,0x1200,go,pool);
    Check(report.value("method",std::string())=="profile_cross_validated"&&go.address==fixture.Base()+0x1100,"valid profile anchors are cross-validated");
    report=ww_game_object_anchors::Resolve(reader,fixture.Base(),0x2100,0x2200,go,pool);
    Check(report.value("method",std::string())=="unique_scan_cross_validated"&&go.address==fixture.Base()+0x1100&&pool.address==fixture.Base()+0x1200,"relocated anchors are found without configured RVAs");
    Check(report.at("gobjects_rva")=="0x1100"&&report.at("name_pool_rva")=="0x1200"&&!report.at("runtime_layout_verified").get<bool>(),"report distinguishes resolved anchors from complete ABI verification");
    std::memcpy(reinterpret_cast<void*>(fixture.Base()+0x1500),reinterpret_cast<void*>(fixture.Base()+0x1100),32);
    report=ww_game_object_anchors::Resolve(reader,fixture.Base(),0x2100,0x2200,go,pool);
    Check(report.value("status",std::string())=="unresolved"&&!go.address&&!pool.address,"ambiguous object/name pairs are not published");
    std::memset(reinterpret_cast<void*>(fixture.Base()+0x1500),0,32);fixture.name_data[6]='X';
    report=ww_game_object_anchors::Resolve(reader,fixture.Base(),0x1100,0x1200,go,pool);
    Check(report.value("status",std::string())=="unresolved"&&!go.address&&!pool.address,"invalid names reject configured anchors and scan results");
    std::cout<<"PASS: "<<checks<<" object anchor resolution checks; no game access.\n";
}
