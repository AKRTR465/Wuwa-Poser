#pragma once
#include "../generated/GameCompat.generated.h"
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
#include <algorithm>
#include <stdexcept>
#include "../../core_math/third_party/nlohmann/json.hpp"
#include "../../playback/ue_memory.h"
#include "json_report.h"
#pragma comment(lib,"Psapi.lib")

using Json = nlohmann::json;
using ue_memory::At;
using ue_memory::Hex;
using ue_memory::Pointer;
inline bool Readable(DWORD p) {
    if(p&(PAGE_GUARD|PAGE_NOACCESS))return false;
    p &= 0xff;
    return p==PAGE_READONLY||p==PAGE_READWRITE||p==PAGE_WRITECOPY||
           p==PAGE_EXECUTE_READ||p==PAGE_EXECUTE_READWRITE||p==PAGE_EXECUTE_WRITECOPY;
}
struct Reader {
    HANDLE process;
    bool Read(uint64_t address,void* out,size_t size) {
        if(!size||!address)return false;
        SIZE_T got=0;return ReadProcessMemory(process,(LPCVOID)address,out,size,&got)&&got==size;
    }
    template<class T> bool Get(uint64_t address,T& out) { return Read(address,&out,sizeof(out)); }
};
struct Go {uint64_t address=0,table=0;uint32_t count=0;};
struct Pool {uint64_t address=0,blocks=0;uint32_t current=0,cursor=0;};
inline bool GoShape(const unsigned char* b) {
    uint32_t max=At<uint32_t>(b,ww_game_compat::ObjectArrayMaxElementsOffset),n=At<uint32_t>(b,ww_game_compat::ObjectArrayCountOffset),cap=At<uint32_t>(b,ww_game_compat::ObjectArrayMaxChunksOffset),used=At<uint32_t>(b,ww_game_compat::ObjectArrayNumChunksOffset);
    return Pointer(At<uint64_t>(b,ww_game_compat::ObjectArrayTableOffset))&&cap&&cap<=ww_game_compat::ObjectArrayMaximumChunks&&used&&used<=cap&&
        (uint64_t)max==(uint64_t)cap*ww_game_compat::ObjectArrayElementsPerChunk&&n>=4&&n<=max&&used>=(n+uint64_t(ww_game_compat::ObjectArrayChunkMask))/ww_game_compat::ObjectArrayElementsPerChunk;
}
template<class R> bool ObjectItem(R& r,const Go& go,uint32_t index,uint64_t& item) {
    return ue_memory::ObjectItem([&](uint64_t address,void* out,size_t size){return r.Read(address,out,size);},go.table,go.count,index,item);
}
inline bool Object(Reader& r,const Go& go,uint32_t index,uint64_t& object) {
    uint64_t item=0;
    return ObjectItem(r,go,index,item)&&r.Get(item+ww_game_compat::ObjectItemObjectOffset,object)&&Pointer(object);
}
inline bool ValidateGo(Reader& r,uint64_t address,Go& out) {
    unsigned char b[ww_game_compat::ObjectArrayHeaderBytes];if(!r.Read(address,b,sizeof(b))||!GoShape(b))return false;
    Go g{address,At<uint64_t>(b,ww_game_compat::ObjectArrayTableOffset),At<uint32_t>(b,ww_game_compat::ObjectArrayCountOffset)};int valid=0;
    for(uint32_t i=0;i<(std::min)(g.count,32u);++i) {
        uint64_t obj=0;unsigned char h[ww_game_compat::ObjectNameOffset+ww_game_compat::PointerBytes];
        if(Object(r,g,i,obj)&&r.Read(obj,h,sizeof(h))&&At<uint32_t>(h,ww_game_compat::ObjectIndexOffset)==i&&Pointer(At<uint64_t>(h,ww_game_compat::ObjectClassOffset)))++valid;
    }
    if(valid<3)return false;out=g;return true;
}
// Current-build FName entry: its address, length and narrow/wide encoding.
struct NameEntry {uint64_t address=0;uint32_t length=0;bool wide=false;};
template<class R> bool FindName(R& r,const Pool& pool,uint32_t tagged,NameEntry& out) {
    const uint32_t id=tagged&ww_game_compat::NameTaggedIndexMask,block=id>>ww_game_compat::NameBlockShift;
    if(block>pool.current||block>=ww_game_compat::NamePoolMaximumBlocks)return false;
    uint64_t data=0;if(!r.Get(pool.blocks+(uint64_t)block*ww_game_compat::PointerBytes,data)||!Pointer(data))return false;
    out.address=data+(uint64_t)(id&ww_game_compat::NameBlockMask)*ww_game_compat::NameEntryStride;
    uint16_t header=0;if(!r.Get(out.address+ww_game_compat::NameEntryHeaderOffset,header))return false;
    out.length=header>>ww_game_compat::NameEntryLengthShift;out.wide=(header&ww_game_compat::NameEntryWideFlagMask)!=0;
    return out.length&&out.length<=ww_game_compat::NameMaximumCharacters;
}
// Stable reads reject failed or changing evidence.
template<class R> bool Decode(R& r,const Pool& pool,uint32_t tagged,std::string& out) {
    NameEntry name;if(!FindName(r,pool,tagged,name))return false;
    const uint64_t entry=name.address;const uint32_t length=name.length;
    if(!name.wide) {
        char text[512];if(!r.Read(entry+ww_game_compat::NameEntryTextOffset,text,length))return false;
        for(uint32_t i=0;i<length;++i)if((unsigned char)text[i]<32||(unsigned char)text[i]>126)return false;
        out.assign(text,length);return true;
    }
    wchar_t wide[512];char utf8[2048];
    if(!r.Read(entry+ww_game_compat::NameEntryTextOffset,wide,length*sizeof(wchar_t)))return false;
    int n=WideCharToMultiByte(CP_UTF8,WC_ERR_INVALID_CHARS,wide,length,utf8,sizeof(utf8),nullptr,nullptr);
    if(n<=0)return false;out.assign(utf8,n);return true;
}
// FName index plus its instance number, e.g. "Bone_2".
template<class R> bool FullName(R& r,const Pool& pool,uint32_t tagged,uint32_t number,std::string& out) {
    if(!Decode(r,pool,tagged,out))return false;ue_memory::AppendNameNumber(out,number);return true;
}
inline bool PoolShape(const unsigned char* b) {
    uint32_t current=At<uint32_t>(b,ww_game_compat::NamePoolCurrentBlockOffset),cursor=At<uint32_t>(b,ww_game_compat::NamePoolCursorOffset);
    return current<ww_game_compat::NamePoolMaximumBlocks&&cursor>=ww_game_compat::NamePoolBlocksOffset&&cursor<=ww_game_compat::NamePoolMaximumCursor&&Pointer(At<uint64_t>(b,ww_game_compat::NamePoolBlocksOffset));
}
inline bool ValidatePool(Reader& r,uint64_t address,Pool& out) {
    unsigned char b[ww_game_compat::NamePoolHeaderBytes];if(!r.Read(address,b,sizeof(b))||!PoolShape(b))return false;
    Pool p{address,address+ww_game_compat::NamePoolBlocksOffset,At<uint32_t>(b,ww_game_compat::NamePoolCurrentBlockOffset),At<uint32_t>(b,ww_game_compat::NamePoolCursorOffset)};std::string name;
    if(!Decode(r,p,0,name)||name!="None")return false;out=p;return true;
}
// A GObjects/name-pool pair is the real one when sampled objects decode to
// names and classes, including at least one UE metaclass.
inline bool Consistent(Reader& r,const Go& go,const Pool& pool) {
    unsigned checked=0,decoded=0,metaClasses=0;
    for(uint32_t i=0;i<(std::min)(go.count,1024u)&&checked<64;++i) {
        uint64_t obj=0;unsigned char h[ww_game_compat::ObjectHeaderBytes];
        if(!Object(r,go,i,obj)||!r.Read(obj,h,sizeof(h))||At<uint32_t>(h,ww_game_compat::ObjectIndexOffset)!=i)continue;
        ++checked;uint32_t classId=0;std::string name,cls;
        if(!Decode(r,pool,At<uint32_t>(h,ww_game_compat::ObjectNameOffset),name)||!r.Get(At<uint64_t>(h,ww_game_compat::ObjectClassOffset)+ww_game_compat::ObjectNameOffset,classId)||!Decode(r,pool,classId,cls))continue;
        ++decoded;if(cls=="Class"||cls=="Package"||cls=="Function"||cls=="ScriptStruct"||cls=="Enum")++metaClasses;
    }
    return checked>=4&&decoded*10>=checked*9&&metaClasses>0;
}

// Scans the host image's readable, writable sections for the GObjects table and
// name pool. The chosen pair is reported as the single candidate of each kind.
inline Json ReconProcess(uint64_t image,Go* chosenGo,Pool* chosenPool) {
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
            while(address<stop) {size_t n=(size_t)(std::min)(stop-address,65536ull+ww_game_compat::ObjectArrayHeaderBytes);std::vector<unsigned char> b(n);
                if(!r.Read(address,b.data(),n))throw std::runtime_error("main image read failed");
                for(size_t off=0;off+ww_game_compat::ObjectArrayHeaderBytes<=n;off+=ww_game_compat::PointerBytes) {
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
