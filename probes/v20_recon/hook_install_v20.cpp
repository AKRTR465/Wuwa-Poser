#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <cstdio>
#include <string>
#include <fstream>
#pragma comment(lib,"user32.lib")
int wmain(int argc,wchar_t** argv) {
    if(argc!=3||wcscmp(argv[1],L"--timeout-seconds")){fprintf(stderr,"usage: hook_install_v20 --timeout-seconds 1..60\n");return 2;}
    unsigned long timeout=wcstoul(argv[2],nullptr,10);
    wchar_t exe[32768];DWORD n=GetModuleFileNameW(nullptr,exe,32768);
    std::wstring dir(exe,n);dir.resize(dir.find_last_of(L'\\'));
    auto report=[&](const char* stage,bool loaded,bool unhooked,DWORD error) {
        std::ofstream file(dir+L"\\installer_result.json",std::ios::trunc);
        file<<"{\"stage\":\""<<stage<<"\",\"loaded\":"<<(loaded?"true":"false")
            <<",\"disarmed\":"<<(unhooked?"true":"false")<<",\"last_error\":"<<error<<"}\n";
    };
    HMODULE dll=LoadLibraryW((dir+L"\\recon_carrier_v20.dll").c_str());
    if(!dll){DWORD error=GetLastError();report("load_library_failed",false,true,error);fprintf(stderr,"LoadLibrary failed: %lu\n",error);return 1;}
    auto callback=(HOOKPROC)GetProcAddress(dll,"HookProc");if(!callback){FreeLibrary(dll);return 1;}
    std::wstring marker=dir+L"\\logs\\carrier_status.json";
    HHOOK hook=SetWindowsHookExW(WH_GETMESSAGE,callback,dll,0);
    if(!hook){DWORD error=GetLastError();report("hook_failed",false,true,error);fprintf(stderr,"SetWindowsHookEx failed: %lu\n",error);FreeLibrary(dll);return 1;}
    report("armed",false,false,0);
    printf("hook armed for %lu seconds\n",timeout);bool loaded=false;
    ULONGLONG start=GetTickCount64();
    while(GetTickCount64()-start<timeout*1000ull) {
        if(GetFileAttributesW(marker.c_str())!=INVALID_FILE_ATTRIBUTES){loaded=true;break;}Sleep(100);
    }
    BOOL unhooked=UnhookWindowsHookEx(hook);DWORD error=unhooked?0:GetLastError();
    printf("loaded=%d disarmed=%d error=%lu\n",loaded?1:0,unhooked?1:0,error);
    report(loaded?"carrier_seen":"timeout",loaded,unhooked,error);
    FreeLibrary(dll);return loaded&&unhooked?0:1;
}
