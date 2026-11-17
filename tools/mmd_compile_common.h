#pragma once
// Shared offline compiler input/output boundary. No game access.
#include <windows.h>
#include <bcrypt.h>
#pragma comment(lib,"bcrypt.lib")
#include <array>
#include <fstream>
#include <filesystem>
#include <string>
#include <vector>
#include <set>
#include <stdexcept>
#include <cwchar>
#include <cmath>
#include <cstdint>
namespace wwp_mmd_compile {
inline std::string Sha256(const std::filesystem::path& path) {
    struct HashHandle {
        BCRYPT_HASH_HANDLE value=nullptr;
        ~HashHandle(){if(value)BCryptDestroyHash(value);}
    } hash;
    if(BCryptCreateHash(BCRYPT_SHA256_ALG_HANDLE,&hash.value,nullptr,0,nullptr,0,0)<0)
        throw std::runtime_error("SHA init failed");
    std::ifstream file(path,std::ios::binary);
    std::array<char,65536> block{};
    while(file){
        file.read(block.data(),block.size());
        if(file.gcount()&&BCryptHashData(hash.value,reinterpret_cast<PUCHAR>(block.data()),ULONG(file.gcount()),0)<0)
            throw std::runtime_error("SHA update failed");
    }
    std::array<uint8_t,32> digest{};
    if(!file.eof()||BCryptFinishHash(hash.value,digest.data(),ULONG(digest.size()),0)<0)
        throw std::runtime_error("SHA final failed");
    std::string text;static const char hex[]="0123456789abcdef";
    for(auto byte:digest){text+=hex[byte>>4];text+=hex[byte&15];}return text;
}
inline uint32_t FrameCount60(double duration){
    const double frames=std::ceil(duration*60)+1;
    if(!(frames>=1&&frames<=UINT32_MAX))throw std::runtime_error("motion exceeds the 60 fps frame budget");
    return uint32_t(frames);
}
// A full-width VMD name may end in a cut-off CP932 lead byte; it is kept as a
// distinct name instead of failing the whole motion.
inline std::string Decode(const std::string& bytes,unsigned codepage,bool fullWidth){
    if(bytes.empty())return {};
    int length=int(bytes.size());
    int count=MultiByteToWideChar(codepage,MB_ERR_INVALID_CHARS,bytes.data(),length,nullptr,0);
    const auto tail=static_cast<unsigned char>(bytes.back());
    bool truncated=false;
    if(!count&&fullWidth&&codepage==932&&length>1&&IsDBCSLeadByteEx(codepage,tail)){
        --length;
        count=MultiByteToWideChar(codepage,MB_ERR_INVALID_CHARS,bytes.data(),length,nullptr,0);
        truncated=count!=0;
    }
    if(!count)throw std::runtime_error("invalid encoded motion name; expected complete CP932 bytes");
    std::wstring wide(size_t(count),L'\0');
    if(MultiByteToWideChar(codepage,MB_ERR_INVALID_CHARS,bytes.data(),length,wide.data(),count)!=count)
        throw std::runtime_error("motion name decoding failed");
    const int size=WideCharToMultiByte(CP_UTF8,WC_ERR_INVALID_CHARS,wide.data(),count,nullptr,0,nullptr,nullptr);
    if(!size)throw std::runtime_error("motion name UTF8 conversion failed");
    std::string result(size_t(size),'\0');
    if(WideCharToMultiByte(CP_UTF8,WC_ERR_INVALID_CHARS,wide.data(),count,result.data(),size,nullptr,nullptr)!=size)
        throw std::runtime_error("motion name UTF8 conversion failed");
    if(truncated){
        // Preserve the incomplete name as a distinct track, including its final byte.
        // Dropping the byte could redirect keys to an existing, shorter name.
        static constexpr char hex[]="0123456789ABCDEF";
        result+=u8"\uFFFD";
        result+=hex[tail>>4];
        result+=hex[tail&15];
    }
    return result;
}
inline double Number(const wchar_t* text){
    if(!text||!*text)throw std::runtime_error("numeric argument is empty");
    wchar_t* end=nullptr;const double value=std::wcstod(text,&end);
    if(end==text||*end||!std::isfinite(value))throw std::runtime_error("numeric argument is invalid");
    return value;
}
}
