#pragma once
#include "../runtime/generated/GameCompat.generated.h"
// Engine memory primitives shared by the playback helpers and observers. No
// process APIs: callers supply the reader.
#include <cstddef>
#include <cstdint>
#include <cstdio>
#include <cstring>
#include <stdexcept>
#include <string>

namespace ue_memory {
struct ArrayHead {uint64_t data=0;int32_t num=0,max=0;};
static_assert(sizeof(ArrayHead)==ww_game_compat::TArrayHeaderBytes,"x64 TArray header");
inline void Require(bool ok,const char* message){if(!ok)throw std::runtime_error(message);}
inline std::string Hex(uint64_t value){char text[24];std::snprintf(text,sizeof(text),"0x%llx",static_cast<unsigned long long>(value));return text;}
template<class T> T At(const void* bytes,size_t offset){T value{};std::memcpy(&value,static_cast<const uint8_t*>(bytes)+offset,sizeof(value));return value;}
inline bool Range(uint64_t address,size_t bytes){
    return bytes&&address>=ww_game_compat::PointerMinimum&&address<ww_game_compat::PointerMaximum&&bytes<=ww_game_compat::PointerMaximum-address;}
inline bool Pointer(uint64_t address){return Range(address,1)&&!(address&(ww_game_compat::PointerBytes-1));}
// GObjects index -> chunked item address. The item still has to be checked
// against the object it claims to describe.
template<class Read> bool ObjectItem(Read&& read,uint64_t table,uint32_t count,uint32_t index,uint64_t& item){
    uint64_t chunk=0;
    if(index>=count||!read(table+uint64_t(index>>ww_game_compat::ObjectArrayChunkShift)*ww_game_compat::PointerBytes,&chunk,sizeof(chunk))||!Pointer(chunk))return false;
    item=chunk+uint64_t(index&ww_game_compat::ObjectArrayChunkMask)*ww_game_compat::ObjectItemBytes;return true;
}
// Appends the FName number suffix to a decoded base name.
inline void AppendNameNumber(std::string& name,uint32_t number){if(number)name+="_"+std::to_string(number-1);}
} // namespace ue_memory
