#pragma once
// In-process readers for the GObjects table and Kuro name pool. The anchors are
// located by scanning the main image's writable sections once per scan session.
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <psapi.h>
#include <cstdint>
#include <cstring>
#include <cmath>
#include <cstdio>
#include <vector>
#include <string>
#include <map>
#include <set>
#include <algorithm>
#include <stdexcept>
#include <cwctype>
#include <functional>
#include <cctype>
#include <limits>
#include "../../core_math/third_party/nlohmann/json.hpp"
#include "json_report.h"
#pragma comment(lib,"Psapi.lib")

using Json = nlohmann::json;
static std::string Hex(uint64_t n) { char b[32]; sprintf_s(b,"0x%llx",(unsigned long long)n); return b; }
template<class T> static T At(const unsigned char* p,size_t off) { T v; memcpy(&v,p+off,sizeof(v));return v; }
static bool Pointer(uint64_t p) { return p>=0x10000 && p<0x0000800000000000ull && !(p&7); }
static bool Readable(DWORD p) {
    if(p&(PAGE_GUARD|PAGE_NOACCESS))return false;
    p &= 0xff;
    return p==PAGE_READONLY||p==PAGE_READWRITE||p==PAGE_WRITECOPY||
           p==PAGE_EXECUTE_READ||p==PAGE_EXECUTE_READWRITE||p==PAGE_EXECUTE_WRITECOPY;
}
static std::wstring FileName(const std::wstring& path) {
    size_t at=path.find_last_of(L"\\/");return at==std::wstring::npos?path:path.substr(at+1);
}
struct Reader {
    HANDLE process; uint64_t bytes=0;
    bool Read(uint64_t address,void* out,size_t size) {
        if(!size||!address)return false;
        SIZE_T got=0;BOOL ok=ReadProcessMemory(process,(LPCVOID)address,out,size,&got);bytes+=got;
        return ok&&got==size;
    }
    template<class T> bool Get(uint64_t address,T& out) { return Read(address,&out,sizeof(out)); }
};
struct Go {uint64_t address=0,table=0;uint32_t count=0;};
struct Pool {uint64_t address=0,blocks=0;uint32_t current=0,cursor=0;};
static bool GoShape(const unsigned char* b) {
    uint32_t max=At<uint32_t>(b,16),n=At<uint32_t>(b,20),cap=At<uint32_t>(b,24),used=At<uint32_t>(b,28);
    return Pointer(At<uint64_t>(b,0))&&cap&&cap<=4096&&used&&used<=cap&&
        (uint64_t)max==(uint64_t)cap*65536&&n>=4&&n<=max&&used>=(n+65535ull)/65536;
}
static bool Object(Reader& r,const Go& go,uint32_t index,uint64_t& object) {
    uint64_t chunk=0;
    return index<go.count&&r.Get(go.table+(uint64_t)(index>>16)*8,chunk)&&Pointer(chunk)&&
        r.Get(chunk+(uint64_t)(index&65535)*24,object)&&Pointer(object);
}
static bool ValidateGo(Reader& r,uint64_t address,Go& out) {
    unsigned char b[32];if(!r.Read(address,b,sizeof(b))||!GoShape(b))return false;
    Go g{address,At<uint64_t>(b,0),At<uint32_t>(b,20)};int valid=0;
    for(uint32_t i=0;i<(std::min)(g.count,32u);++i) {
        uint64_t obj=0;unsigned char h[32];
        if(Object(r,g,i,obj)&&r.Read(obj,h,sizeof(h))&&At<uint32_t>(h,12)==i&&Pointer(At<uint64_t>(h,16)))++valid;
    }
    if(valid<3)return false;out=g;return true;
}
static bool Decode(Reader& r,const Pool& pool,uint32_t tagged,std::string& out) {
    uint32_t id=tagged&0x1fffffffu,block=id>>16;
    if(block>pool.current||block>=8192)return false;
    uint64_t data=0;if(!r.Get(pool.blocks+(uint64_t)block*8,data)||!Pointer(data))return false;
    uint64_t entry=data+(uint64_t)(id&65535)*4;
    uint16_t header=0;if(!r.Get(entry+4,header))return false;
    uint32_t length=header>>1;if(!length||length>511)return false;
    if(!(header&1)) {
        char text[512];if(!r.Read(entry+6,text,length))return false;
        for(uint32_t i=0;i<length;++i)if((unsigned char)text[i]<32||(unsigned char)text[i]>126)return false;
        out.assign(text,length);return true;
    }
    wchar_t wide[512];char utf8[2048];
    if(!r.Read(entry+6,wide,length*sizeof(wchar_t)))return false;
    int n=WideCharToMultiByte(CP_UTF8,WC_ERR_INVALID_CHARS,wide,length,utf8,sizeof(utf8),nullptr,nullptr);
    if(n<=0)return false;out.assign(utf8,n);return true;
}
static bool PoolShape(const unsigned char* b) {
    uint32_t current=At<uint32_t>(b,8),cursor=At<uint32_t>(b,12);
    return current<8192&&cursor>=16&&cursor<=0x40000&&Pointer(At<uint64_t>(b,16));
}
static bool ValidatePool(Reader& r,uint64_t address,Pool& out) {
    unsigned char b[24];if(!r.Read(address,b,sizeof(b))||!PoolShape(b))return false;
    Pool p{address,address+16,At<uint32_t>(b,8),At<uint32_t>(b,12)};std::string name;
    if(!Decode(r,p,0,name)||name!="None")return false;out=p;return true;
}
// A GObjects/name-pool pair is the real one when sampled objects decode to
// names and classes, including at least one UE metaclass.
static bool Consistent(Reader& r,const Go& go,const Pool& pool) {
    unsigned checked=0,decoded=0,metaClasses=0;
    for(uint32_t i=0;i<(std::min)(go.count,1024u)&&checked<64;++i) {
        uint64_t obj=0;unsigned char h[48];
        if(!Object(r,go,i,obj)||!r.Read(obj,h,sizeof(h))||At<uint32_t>(h,12)!=i)continue;
        ++checked;uint32_t classId=0;std::string name,cls;
        if(!Decode(r,pool,At<uint32_t>(h,24),name)||!r.Get(At<uint64_t>(h,16)+24,classId)||!Decode(r,pool,classId,cls))continue;
        ++decoded;if(cls=="Class"||cls=="Package"||cls=="Function"||cls=="ScriptStruct"||cls=="Enum")++metaClasses;
    }
    return checked>=4&&decoded*10>=checked*9&&metaClasses>0;
}
static std::string BytesHex(const unsigned char* bytes,size_t n) {
    static const char digits[]="0123456789abcdef";std::string text(n*2,'0');
    for(size_t i=0;i<n;++i){text[i*2]=digits[bytes[i]>>4];text[i*2+1]=digits[bytes[i]&15];}return text;
}
static void Save(const wchar_t* path,const Json& result) { ww_report::Save(path,result); }

// Scans the host image's readable, writable sections for the GObjects table and
// name pool. The chosen pair is reported as the single candidate of each kind.
static Json ReconProcess(uint64_t image,Go* chosenGo,Pool* chosenPool) {
    Json result={{"pid",GetCurrentProcessId()},{"status","unresolved"}};Reader r{GetCurrentProcess()};
    IMAGE_DOS_HEADER dos;IMAGE_NT_HEADERS64 nt;
    if(!r.Get(image,dos)||!r.Get(image+(uint32_t)dos.e_lfanew,nt))throw std::runtime_error("main PE headers unreadable");
    std::vector<IMAGE_SECTION_HEADER> sections(nt.FileHeader.NumberOfSections);
    if(!r.Read(image+(uint32_t)dos.e_lfanew+24+nt.FileHeader.SizeOfOptionalHeader,sections.data(),sections.size()*sizeof(IMAGE_SECTION_HEADER)))throw std::runtime_error("main section table unreadable");
    std::vector<Go> objects;std::vector<Pool> names;
    for(const auto& s:sections) {
        if(!(s.Characteristics&IMAGE_SCN_MEM_WRITE)||!(s.Characteristics&IMAGE_SCN_MEM_READ))continue;
        for(uint64_t address=image+s.VirtualAddress,end=address+s.Misc.VirtualSize;address<end;) {
            MEMORY_BASIC_INFORMATION mbi={};
            if(!VirtualQuery((LPCVOID)address,&mbi,sizeof(mbi)))throw std::runtime_error("main image page query failed");
            const uint64_t stop=(std::min)(end,(uint64_t)mbi.BaseAddress+mbi.RegionSize);
            if(mbi.State!=MEM_COMMIT||!Readable(mbi.Protect)){address=stop;continue;}
            while(address<stop) {size_t n=(size_t)(std::min)(stop-address,65536ull+32);std::vector<unsigned char> b(n);
                if(!r.Read(address,b.data(),n))throw std::runtime_error("main image read failed");
                for(size_t off=0;off+32<=n;off+=8) {
                    Go g;if(GoShape(b.data()+off)&&ValidateGo(r,address+off,g)&&std::none_of(objects.begin(),objects.end(),[&](const Go& x){return x.address==g.address;}))objects.push_back(g);
                    Pool p;if(PoolShape(b.data()+off)&&ValidatePool(r,address+off,p)&&std::none_of(names.begin(),names.end(),[&](const Pool& x){return x.address==p.address;}))names.push_back(p);
                }
                address+=(std::min)(n,(size_t)65536);
            }
        }
    }
    unsigned pairs=0;
    for(const auto& g:objects)for(const auto& p:names)if(Consistent(r,g,p)){++pairs;*chosenGo=g;*chosenPool=p;}
    if(pairs!=1){result["reason"]="zero or multiple object/name pairs";return result;}
    result["gobjects_candidates"]=Json::array({{{"rva",Hex(chosenGo->address-image)},{"count",chosenGo->count}}});
    result["name_pool_candidates"]=Json::array({{{"rva",Hex(chosenPool->address-image)},{"current_block",chosenPool->current},{"cursor",chosenPool->cursor}}});
    result["status"]="anchors_cross_validated";return result;
}
