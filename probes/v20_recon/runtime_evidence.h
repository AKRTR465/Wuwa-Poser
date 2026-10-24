#pragma once
// Current-process evidence primitives used by the CSharp read-only pipeline.
#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#ifndef NOMINMAX
#define NOMINMAX
#endif
#include <windows.h>
#include <bcrypt.h>
#include <array>
#include <cstdint>
#include <cstring>
#include <stdexcept>
#include <string>
#pragma comment(lib,"bcrypt.lib")
namespace runtime_evidence {
using Address=uint64_t;
inline void Require(bool condition,const char* reason) {
    if(!condition) throw std::runtime_error(reason);
}
inline bool Pointer(Address value) {
    return value>=0x10000 && value<0x0000800000000000ull && !(value&7);
}
inline bool Span(Address address,size_t bytes) {
    return address>=0x10000 && bytes && address<0x0000800000000000ull &&
        bytes<=0x0000800000000000ull-address;
}
inline std::string Sha256(const void* data,size_t bytes) {
    Require(bytes<=0xffffffffull,"hash size exceeded");
    struct Handles {
        BCRYPT_ALG_HANDLE alg=nullptr; BCRYPT_HASH_HANDLE hash=nullptr;
        ~Handles(){if(hash)BCryptDestroyHash(hash);if(alg)BCryptCloseAlgorithmProvider(alg,0);}
    } h;
    Require(BCryptOpenAlgorithmProvider(&h.alg,BCRYPT_SHA256_ALGORITHM,nullptr,0)>=0,"hash provider failed");
    Require(BCryptCreateHash(h.alg,&h.hash,nullptr,0,nullptr,0,0)>=0,"hash initialization failed");
    if(bytes) Require(BCryptHashData(h.hash,const_cast<PUCHAR>(static_cast<const unsigned char*>(data)),static_cast<ULONG>(bytes),0)>=0,"hash update failed");
    unsigned char digest[32]{};
    Require(BCryptFinishHash(h.hash,digest,sizeof(digest),0)>=0,"hash finish failed");
    constexpr char hex[]="0123456789abcdef";std::string out(64,'0');
    for(size_t i=0;i<32;++i){out[2*i]=hex[digest[i]>>4];out[2*i+1]=hex[digest[i]&15];}
    return out;
}
template<class R> inline void Read(R& reader,Address address,void* out,size_t bytes) {
    Require(Span(address,bytes) && reader.Read(address,out,bytes),"memory unavailable");
}
template<class T,class R> inline T Get(R& reader,Address address) {
    T value{};Read(reader,address,&value,sizeof(value));return value;
}
} // namespace runtime_evidence
