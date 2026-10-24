// Read-only reconnaissance carrier: locates the object/name anchors and the
// controlled character's main mesh in the current process.
#include "shared_readonly.h"
#include "weapon_capture_readonly.h"

#pragma comment(lib,"user32.lib")
static HMODULE v20Self=nullptr;
static volatile LONG v20Started=0;
static std::wstring V20Directory() {
    wchar_t path[32768];DWORD n=GetModuleFileNameW(v20Self,path,32768);
    std::wstring value(path,n);return value.substr(0,value.find_last_of(L'\\'));
}
static bool V20PlayerControllerClass(const std::string& name) {
    return (name.find("CharacterController")!=std::string::npos||name.find("PlayerController")!=std::string::npos)&&
        name.find("AI")==std::string::npos&&name.find("Summon")==std::string::npos;
}
static bool V20AssetNames(Reader& reader,const Go& go,const Pool& pool,uint64_t object,Json& row) {
    uint64_t asset=0,skeleton=0;Json assetIdentity,skeletonIdentity;
    if(!reader.Get(object+0x5d0,asset)||!v20_weapon_capture::Identity(reader,go,pool,asset,assetIdentity)||
       !reader.Get(asset+0xd8,skeleton)||!v20_weapon_capture::Identity(reader,go,pool,skeleton,skeletonIdentity))return false;
    row["mesh_asset_name"]=assetIdentity.at("name");row["skeleton_asset_name"]=skeletonIdentity.at("name");return true;
}
// Each reciprocal player controller's pawn and exact main CharacterMesh0.
static Json V20PlayerRelations(Reader& reader,const Go& go,const Pool& pool) {
    Json result={{"relations",Json::array()}};std::map<uint64_t,std::string> classes;
    for(uint32_t first=0;first<go.count;first+=65536){uint64_t chunk=0;
        if(!reader.Get(go.table+uint64_t(first>>16)*8,chunk)||!Pointer(chunk))continue;
        const uint32_t count=(std::min)(go.count-first,65536u);std::vector<unsigned char> items(size_t(count)*24);
        if(!reader.Read(chunk,items.data(),items.size()))continue;
        for(uint32_t i=0;i<count;++i){const auto* item=items.data()+size_t(i)*24;
            const uint64_t object=At<uint64_t>(item,0);unsigned char header[48];
            if(!Pointer(object)||(At<uint32_t>(item,8)&0x30000000u)||!reader.Read(object,header,sizeof(header))||
               At<uint32_t>(header,12)!=first+i||(At<uint32_t>(header,8)&0x18000u))continue;
            const uint64_t cls=At<uint64_t>(header,16);auto found=classes.find(cls);
            if(found==classes.end()){uint32_t nameId=0;std::string name;if(reader.Get(cls+24,nameId))Decode(reader,pool,nameId,name);found=classes.emplace(cls,name).first;}
            if(found->second!="SkeletalMeshComponent")continue;std::string name;
            if(!Decode(reader,pool,At<uint32_t>(header,24),name)||name!="CharacterMesh0"||At<uint32_t>(header,32)!=0)continue;
            Json mesh,pawnIdentity,controllerIdentity;const uint64_t actor=At<uint64_t>(header,40);uint64_t controller=0,pawn=0,ack=0;
            if(!v20_weapon_capture::Identity(reader,go,pool,object,mesh)||!v20_weapon_capture::Identity(reader,go,pool,actor,pawnIdentity)||
               !reader.Get(actor+0x2e8,controller)||!v20_weapon_capture::Identity(reader,go,pool,controller,controllerIdentity)||
               !V20PlayerControllerClass(controllerIdentity.at("class"))||!reader.Get(controller+0x2e8,pawn)||pawn!=actor||
               !reader.Get(controller+0x340,ack)||ack!=actor)continue;
            Json row=pawnIdentity;row["controller"]=controllerIdentity;row["reciprocal_pawn"]=true;row["reciprocal_acknowledged_pawn"]=true;
            row["main_mesh_address"]=Hex(object);row["main_mesh_identity"]=mesh;
            if(V20AssetNames(reader,go,pool,object,row))result["relations"].push_back(row);
        }
    }
    return result;
}
static void V20Body() {
    std::wstring dir;Json state={{"status","error"}};
    try {
        dir=V20Directory()+L"\\logs";CreateDirectoryW(dir.c_str(),nullptr);
        state["status"]="waiting_trigger";Save((dir+L"\\carrier_status.json").c_str(),state);
        const std::wstring go=dir+L"\\recon_go.flag",stop=dir+L"\\recon_stop.flag";
        while(GetFileAttributesW(go.c_str())==INVALID_FILE_ATTRIBUTES) {
            if(GetFileAttributesW(stop.c_str())!=INVALID_FILE_ATTRIBUTES)throw std::runtime_error("trigger wait cancelled");Sleep(100);
        }
        state["status"]="scanning";Save((dir+L"\\carrier_status.json").c_str(),state);
        Go objects;Pool pool;Json scan=ReconProcess((uint64_t)GetModuleHandleW(nullptr),&objects,&pool);scan["meshes"]=Json::array();
        if(scan.at("status")=="anchors_cross_validated"){
            Reader reader{GetCurrentProcess()};Json relations=V20PlayerRelations(reader,objects,pool);
            for(const auto& row:relations.at("relations"))scan["meshes"].push_back(row.at("main_mesh_identity"));
            Save((dir+L"\\player_relations.json").c_str(),relations);
        }
        Save((dir+L"\\recon.json").c_str(),scan);
        state["recon_status"]=scan.value("status","");state["status"]="finished";
    } catch(const std::exception& e) {state["status"]="error";state["reason"]=e.what();}
    if(!dir.empty())try {Save((dir+L"\\carrier_status.json").c_str(),state);}catch(...){}
}
static DWORD WINAPI V20Worker(LPVOID owned) {
    DWORD result=0;
    try {V20Body();}catch(...){result=ERROR_WRITE_FAULT;}
    FreeLibraryAndExitThread((HMODULE)owned,result);
}
extern "C" __declspec(dllexport) LRESULT CALLBACK HookProc(int code,WPARAM w,LPARAM l) {
    if(code>=0&&InterlockedCompareExchange(&v20Started,1,0)==0) {
        wchar_t exe[32768];DWORD n=GetModuleFileNameW(nullptr,exe,32768);
        if(n&&!_wcsicmp(FileName(exe).c_str(),L"Client-Win64-Shipping.exe")) {
            HMODULE owned=nullptr;
            if(GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS,(LPCWSTR)&HookProc,&owned)) {
                HANDLE thread=CreateThread(nullptr,0,V20Worker,owned,0,nullptr);
                if(thread)CloseHandle(thread);else FreeLibrary(owned);
            }
        }
    }
    return CallNextHookEx(nullptr,code,w,l);
}
BOOL WINAPI DllMain(HINSTANCE self,DWORD reason,LPVOID) {
    if(reason==DLL_PROCESS_ATTACH) {
        v20Self=self;DisableThreadLibraryCalls(self);
    }
    return TRUE;
}
