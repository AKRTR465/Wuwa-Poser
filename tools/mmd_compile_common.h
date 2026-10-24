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
namespace wwp_mmd_compile {
inline std::string Sha256(const std::filesystem::path& path) {
    BCRYPT_ALG_HANDLE algorithm=nullptr;BCRYPT_HASH_HANDLE hash=nullptr;DWORD size=0,got=0;
    if(BCryptOpenAlgorithmProvider(&algorithm,BCRYPT_SHA256_ALGORITHM,nullptr,0)<0)throw std::runtime_error("SHA provider unavailable");
    BCryptGetProperty(algorithm,BCRYPT_OBJECT_LENGTH,(PUCHAR)&size,sizeof(size),&got,0);std::vector<uint8_t> object(size);std::array<uint8_t,32> digest{};
    if(BCryptCreateHash(algorithm,&hash,object.data(),size,nullptr,0,0)<0){BCryptCloseAlgorithmProvider(algorithm,0);throw std::runtime_error("SHA init failed");}
    std::ifstream file(path,std::ios::binary);std::array<char,65536> block{};
    while(file){file.read(block.data(),block.size());if(file.gcount()&&BCryptHashData(hash,(PUCHAR)block.data(),(ULONG)file.gcount(),0)<0)throw std::runtime_error("SHA update failed");}
    if(!file.eof()||BCryptFinishHash(hash,digest.data(),(ULONG)digest.size(),0)<0)throw std::runtime_error("SHA final failed");
    BCryptDestroyHash(hash);BCryptCloseAlgorithmProvider(algorithm,0);std::string text;static const char hex[]="0123456789abcdef";
    for(auto byte:digest){text+=hex[byte>>4];text+=hex[byte&15];}return text;
}
inline std::string Decode(const std::string& bytes,unsigned codepage){
    if(bytes.empty())return {};
    const int count=MultiByteToWideChar(codepage,MB_ERR_INVALID_CHARS,bytes.data(),int(bytes.size()),nullptr,0);
    if(!count)throw std::runtime_error("invalid encoded motion name; expected complete CP932 bytes");
    std::wstring wide(size_t(count),L'\0');
    if(MultiByteToWideChar(codepage,MB_ERR_INVALID_CHARS,bytes.data(),int(bytes.size()),wide.data(),count)!=count)
        throw std::runtime_error("motion name decoding failed");
    const int size=WideCharToMultiByte(CP_UTF8,WC_ERR_INVALID_CHARS,wide.data(),count,nullptr,0,nullptr,nullptr);
    if(!size)throw std::runtime_error("motion name UTF8 conversion failed");
    std::string result(size_t(size),'\0');
    if(WideCharToMultiByte(CP_UTF8,WC_ERR_INVALID_CHARS,wide.data(),count,result.data(),size,nullptr,nullptr)!=size)
        throw std::runtime_error("motion name UTF8 conversion failed");
    return result;
}
inline double Number(const wchar_t* text){
    if(!text||!*text)throw std::runtime_error("numeric argument is empty");
    wchar_t* end=nullptr;const double value=std::wcstod(text,&end);
    if(end==text||*end||!std::isfinite(value))throw std::runtime_error("numeric argument is invalid");
    return value;
}
}
