#pragma once
#include <windows.h>
#include <filesystem>
#include <string>
#include <stdexcept>
#include "../../core_math/third_party/nlohmann/json.hpp"

namespace ww_report {
// A reader sees either the previous complete report or the new complete report.
inline bool Write(const std::filesystem::path& path,const nlohmann::json& value) {
    const auto bytes=value.dump(2)+"\n";
    const std::filesystem::path temporary=path.wstring()+L".tmp."+
        std::to_wstring(GetCurrentProcessId())+L"."+std::to_wstring(GetCurrentThreadId());
    HANDLE file=CreateFileW(temporary.c_str(),GENERIC_WRITE,0,nullptr,CREATE_ALWAYS,FILE_ATTRIBUTE_NORMAL,nullptr);
    if(file==INVALID_HANDLE_VALUE)return false;
    DWORD written=0;
    const bool complete=bytes.size()<=MAXDWORD&&WriteFile(file,bytes.data(),DWORD(bytes.size()),&written,nullptr)&&written==bytes.size();
    const bool closed=CloseHandle(file)!=FALSE;
    if(complete&&closed){
        // Desktop polling can briefly hold a handle without SHARE_DELETE.
        // Retry only this exact atomic replacement, for at most 15 ms.
        for(unsigned attempt=0;attempt<4;++attempt){
            if(MoveFileExW(temporary.c_str(),path.c_str(),MOVEFILE_REPLACE_EXISTING))return true;
            const DWORD error=GetLastError();
            if((error!=ERROR_SHARING_VIOLATION&&error!=ERROR_ACCESS_DENIED)||attempt==3)break;
            Sleep(5);
        }
    }
    const DWORD error=GetLastError();DeleteFileW(temporary.c_str());SetLastError(error);return false;
}
inline void Save(const std::filesystem::path& path,const nlohmann::json& value) {
    if(!Write(path,value))throw std::runtime_error("Cannot publish complete JSON report: "+path.u8string());
}
}
