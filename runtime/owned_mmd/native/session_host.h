#pragma once
#include "../../game_integration/object_identity.h"
#include <fstream>

namespace session_host {
using Address=uint64_t;using Json=nlohmann::json;using ue_memory::Require;
inline constexpr const char* kPurpose="owned_mmd_scene";
inline constexpr const wchar_t* kCarrier=L"scene_playback_observer.dll";
inline constexpr const char* kHelper="WwPoser.ScenePlayback.ManagedBridge.dll";
inline std::filesystem::path ModulePath(HMODULE module){wchar_t path[32768]{};GetModuleFileNameW(module,path,32768);return path;}
inline Json ReadJson(const std::filesystem::path& path){std::ifstream f(path,std::ios::binary);Require(bool(f),"JSON file unavailable");return Json::parse(f);}
struct Config {DWORD pid=0,thread=0;HWND window=nullptr;std::string session;};
inline Config ParseConfig(const Json& j){
    Config c;c.pid=j.at("expected_pid");c.thread=j.at("window_thread_id");
    c.window=reinterpret_cast<HWND>(ww_object::Rva(j.at("window_handle")));c.session=j.at("session_id").get<std::string>();return c;
}
inline Json Baseline(const Config& c){return {{"schema",1},{"purpose",kPurpose},{"session_id",c.session},{"expected_pid",c.pid},
    {"window_thread_id",c.thread},{"runtime_executed",true},{"status","error"}};}
}
