#pragma once
#include "../../runtime/generated/GameCompat.generated.h"
#include "shared_readonly.h"
#include <array>

namespace ww_object {
// Compare the ASCII spelling of UE names independently of display casing.
// Keep suffixes and non-ASCII bytes exact.
inline bool SameName(const std::string& a,const std::string& b){
    const auto fold=[](unsigned char c){return c>='A'&&c<='Z'?c+('a'-'A'):c;};
    return a.size()==b.size()&&std::equal(a.begin(),a.end(),b.begin(),[&](char x,char y){return fold(x)==fold(y);});
}
inline uint64_t Rva(const Json& j){
    const auto s=j.get<std::string>();
    if(s.size()<3||s.size()>18||s.substr(0,2)!="0x"||s.find_first_not_of("0123456789abcdefABCDEF",2)!=std::string::npos)throw std::runtime_error("invalid hexadecimal RVA");
    const uint64_t n=std::stoull(s,nullptr,16);if(!n)throw std::runtime_error("zero RVA");return n;
}
struct Identity {uint64_t object=0,item=0,cls=0,outer=0,vtable=0;uint32_t index=0,serial=0,flags=0,item_flags=0;
    std::array<uint32_t,ww_game_compat::NameWordCount> name{};};
template<class R>bool ReadStable(R& r,const Go& go,uint64_t object,Identity* out){
    if(!Pointer(object))return false;unsigned char h[ww_game_compat::ObjectHeaderBytes],item[ww_game_compat::ObjectItemBytes];
    if(!r.Read(object,h,ww_game_compat::ObjectHeaderBytes))return false;Identity v;v.object=object;v.index=At<uint32_t>(h,ww_game_compat::ObjectIndexOffset);v.flags=At<uint32_t>(h,ww_game_compat::ObjectFlagsOffset);
    v.cls=At<uint64_t>(h,ww_game_compat::ObjectClassOffset);v.outer=At<uint64_t>(h,ww_game_compat::ObjectOuterOffset);v.vtable=At<uint64_t>(h,ww_game_compat::ObjectVtableOffset);
    if((v.flags&ww_game_compat::ObjectInvalidFlagsMask)||!Pointer(v.cls)||!Pointer(v.vtable)||!ObjectItem(r,go,v.index,v.item))return false;
    if(!r.Read(v.item,item,ww_game_compat::ObjectItemBytes)||At<uint64_t>(item,ww_game_compat::ObjectItemObjectOffset)!=object||(At<uint32_t>(item,ww_game_compat::ObjectItemFlagsOffset)&ww_game_compat::ObjectItemInvalidFlagsMask))return false;
    v.serial=At<uint32_t>(item,ww_game_compat::ObjectItemSerialOffset);v.item_flags=At<uint32_t>(item,ww_game_compat::ObjectItemFlagsOffset);
    for(unsigned i=0;i<ww_game_compat::NameWordCount;++i)v.name[i]=At<uint32_t>(h,ww_game_compat::ObjectNameOffset+i*ww_game_compat::NameWordBytes);
    *out=v;return true;
}
inline bool Same(const Identity& a,const Identity& b){
    return a.object==b.object&&a.item==b.item&&a.index==b.index&&a.serial==b.serial&&a.cls==b.cls&&a.outer==b.outer&&a.vtable==b.vtable&&a.name==b.name;
}
inline Json Token(const Identity& v,const std::string& name,const std::string& cls){
    return {{"address",Hex(v.object)},{"item_address",Hex(v.item)},{"index",v.index},{"serial",v.serial},
        {"class_address",Hex(v.cls)},{"outer_address",Hex(v.outer)},{"vtable",Hex(v.vtable)},
        {"name_words",v.name},{"name",name},{"class_name",cls},{"strong",v.serial!=0},{"object_flags",v.flags},{"item_flags",v.item_flags}};
}
template<class R>std::string Name(R& r,const Pool& p,const Identity& id){
    std::string n;return FullName(r,p,id.name[0],id.name[ww_game_compat::NameNumberOffset/ww_game_compat::NameWordBytes],n)?n:std::string();
}
template<class R>std::string ClassName(R& r,const Go& go,const Pool& pool,uint64_t cls){Identity id;return ReadStable(r,go,cls,&id)?Name(r,pool,id):std::string();}
}
