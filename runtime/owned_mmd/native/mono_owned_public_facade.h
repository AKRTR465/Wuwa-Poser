#pragma once
// Bind the public Mono exports and the helper's static entries once.
#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#include <windows.h>
#include <array>
#include <cstdint>
#include <stdexcept>
#include <string>
#include "owned_recovery.h"

namespace mono_owned {
inline void Require(bool value,const char* message){if(!value)throw std::runtime_error(message);}
struct Assembly;struct Image;struct Klass;struct Method;struct Object;
enum class Action:size_t {BeginTarget,CheckLoaded,ShowAtLocal,Stop,CheckReleased,MaintainStage};
inline constexpr std::array<const char*,6> kActionNames={"BeginTarget","CheckLoaded","ShowAtLocal","Stop","CheckReleased","MaintainStage"};
class Bridge {
    owned_recovery::InvocationBoundary entry_;
    Object* (__cdecl* invoke_)(Method*,void*,void**,Object**)=nullptr;
    std::array<Method*,6> methods_{};Method* bind_session_=nullptr;
    template<class F>static F Export(HMODULE module,const char* name){const auto entry=GetProcAddress(module,name);Require(entry,"public Mono export unavailable");return reinterpret_cast<F>(entry);}
    void Bind(const std::string& helper){
        if(bind_session_)return;
        const auto module=GetModuleHandleW(L"coreclr.dll");Require(module,"Mono runtime unavailable");
        const auto open=Export<Assembly* (__cdecl*)(const char*,int32_t*)>(module,"mono_assembly_open");
        const auto image=Export<Image* (__cdecl*)(Assembly*)>(module,"mono_assembly_get_image");
        const auto find_class=Export<Klass* (__cdecl*)(Image*,const char*,const char*)>(module,"mono_class_from_name");
        const auto find_method=Export<Method* (__cdecl*)(Klass*,const char*,int32_t)>(module,"mono_class_get_method_from_name");
        invoke_=Export<Object* (__cdecl*)(Method*,void*,void**,Object**)>(module,"mono_runtime_invoke");
        int32_t status=-1;const auto assembly=open(helper.c_str(),&status);Require(assembly&&status==0,"owned helper open failed");
        const auto klass=find_class(image(assembly),"WwPoser.ColdLoad","ManagedBridge");Require(klass,"owned helper class unavailable");
        for(size_t i=0;i<methods_.size();++i){methods_[i]=find_method(klass,kActionNames[i],0);Require(methods_[i],"loaded actor helper lacks a required action; restart the game once for this version");}
        // Mono keeps the first loaded helper for the game process; an older one lacks BindSession.
        bind_session_=find_method(klass,"BindSession",0);Require(bind_session_,"loaded owned helper lacks session isolation; restart the game once");
    }
    void Call(Method* method){Object* exception=nullptr;invoke_(method,nullptr,nullptr,&exception);Require(!exception,"owned helper raised managed exception");}
public:
    bool BeginEntered()const noexcept{return entry_.begin_entered;}
    void Run(const std::string& helper,Action action){
        // Same-session binding is idempotent. Another live session rejects
        // here, before its owners could receive this caller's Stop action.
        // Once that session has recovered, an empty rejected caller can bind
        // and complete its own cleanup without retaining a dead-end worker.
        entry_.Run(action==Action::BeginTarget,[&]{Bind(helper);},[&]{Call(bind_session_);},[&]{Call(methods_[static_cast<size_t>(action)]);});
    }
};
}
