#pragma once
#include "shared_readonly.h"
#include <array>
#include <filesystem>
#include <fstream>
#include <unordered_map>

namespace ww_object {
constexpr const char* kLayoutHash="c4956c30be531f3b992eabe42fa92fa9d10186000fe12e7546a11e1179bdac18";
static uint64_t Integer(const Json& j,const char* key,uint64_t low,uint64_t high){
    const auto& v=j.at(key);
    if(!v.is_number_integer()||(!v.is_number_unsigned()&&v.get<int64_t>()<0))throw std::runtime_error(std::string(key)+" must be an unsigned integer");
    const uint64_t n=v.get<uint64_t>();if(n<low||n>high)throw std::runtime_error(std::string(key)+" outside bounds");return n;
}
static uint64_t Rva(const Json& j){
    const auto s=j.get<std::string>();
    if(s.size()<3||s.size()>18||s.substr(0,2)!="0x"||s.find_first_not_of("0123456789abcdefABCDEF",2)!=std::string::npos)throw std::runtime_error("invalid hexadecimal RVA");
    const uint64_t n=std::stoull(s,nullptr,16);if(!n)throw std::runtime_error("zero RVA");return n;
}
struct Identity {uint64_t object=0,item=0,cls=0,outer=0,vtable=0;uint32_t index=0,serial=0,flags=0,item_flags=0;
    std::array<uint32_t,3> name{};};
template<class R>static bool ReadStable(R& r,const Go& go,uint64_t object,Identity* out){
    if(!out||!Pointer(object))return false;unsigned char h[48],item[24];uint64_t chunk=0;
    if(!r.Read(object,h,48))return false;Identity v;v.object=object;v.index=At<uint32_t>(h,12);v.flags=At<uint32_t>(h,8);
    v.cls=At<uint64_t>(h,16);v.outer=At<uint64_t>(h,40);v.vtable=At<uint64_t>(h,0);
    if(v.index>=go.count||(v.flags&0x18000u)||!Pointer(v.cls)||!Pointer(v.vtable)||
       !r.Get(go.table+uint64_t(v.index>>16)*8,chunk)||!Pointer(chunk))return false;
    v.item=chunk+uint64_t(v.index&65535u)*24;
    if(!r.Read(v.item,item,24)||At<uint64_t>(item,0)!=object||(At<uint32_t>(item,8)&0x30000000u))return false;
    v.serial=At<uint32_t>(item,16);v.item_flags=At<uint32_t>(item,8);
    for(unsigned i=0;i<3;++i)v.name[i]=At<uint32_t>(h,24+i*4);
    *out=v;return true;
}
static bool Same(const Identity& a,const Identity& b){
    return a.object==b.object&&a.item==b.item&&a.index==b.index&&a.serial==b.serial&&a.cls==b.cls&&a.outer==b.outer&&a.vtable==b.vtable&&a.name==b.name;
}
static Json Token(const Identity& v,const std::string& name,const std::string& cls){
    return {{"address",Hex(v.object)},{"item_address",Hex(v.item)},{"index",v.index},{"serial",v.serial},
        {"class_address",Hex(v.cls)},{"outer_address",Hex(v.outer)},{"vtable",Hex(v.vtable)},
        {"name_words",v.name},{"name",name},{"class_name",cls},{"strong",v.serial!=0},{"object_flags",v.flags},{"item_flags",v.item_flags}};
}
template<class R>static bool DecodeName(R& r,const Pool& pool,uint32_t tagged,std::string& out){
    // Same current-build FName encoding as tools/recon_readonly.cpp::Decode,
    // Stable reads reject failed or changing evidence.
    const uint32_t id=tagged&0x1fffffffu,block=id>>16;if(block>pool.current||block>=8192)return false;
    uint64_t data=0;if(!r.Get(pool.blocks+uint64_t(block)*8,data)||!Pointer(data))return false;
    const uint64_t entry=data+uint64_t(id&65535u)*4;uint16_t header=0;
    if(!r.Get(entry+4,header))return false;const uint32_t length=header>>1;if(!length||length>511)return false;
    if(!(header&1)){char bytes[512];if(!r.Read(entry+6,bytes,length))return false;
        for(uint32_t i=0;i<length;++i)if(uint8_t(bytes[i])<32||uint8_t(bytes[i])>126)return false;out.assign(bytes,length);return true;}
    wchar_t wide[512];char utf8[2048];if(!r.Read(entry+6,wide,length*sizeof(wchar_t)))return false;
    const int count=WideCharToMultiByte(CP_UTF8,WC_ERR_INVALID_CHARS,wide,length,utf8,sizeof(utf8),nullptr,nullptr);
    if(count<=0)return false;out.assign(utf8,count);return true;
}
template<class R>static std::string Name(R& r,const Pool& p,const Identity& id){
    std::string n;if(!DecodeName(r,p,id.name[0],n))return {};if(id.name[2])n+="_"+std::to_string(id.name[2]-1);return n;
}
template<class R>static std::string ClassName(R& r,const Go& go,const Pool& pool,uint64_t cls){Identity id;return ReadStable(r,go,cls,&id)?Name(r,pool,id):std::string();}

static void Status(const std::filesystem::path& dir,const Json& row){ww_report::Save(dir/L"logs/carrier_status.json",row);}
}
