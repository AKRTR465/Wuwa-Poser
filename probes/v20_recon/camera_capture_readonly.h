#pragma once
// Worker-only current-instance camera chain identities. Reads only.
namespace v20_camera_capture {
inline Json Capture(Reader& r,const Go& go,const Pool& pool,uint64_t controller,uint64_t pawn){
    Json out={{"status","error"}};uint64_t pcm=0;Json c,p,m;
    if(!r.Get(controller+0x360,pcm)||!v20_weapon_capture::Identity(r,go,pool,controller,c)||
       !v20_weapon_capture::Identity(r,go,pool,pawn,p)||!v20_weapon_capture::Identity(r,go,pool,pcm,m)){
        out["reason"]="camera chain identity unavailable";return out;}
    out["controller"]=c;out["pawn"]=p;out["pcm"]=m;out["status"]="captured";return out;
}
}
