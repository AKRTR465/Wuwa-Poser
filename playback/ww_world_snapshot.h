#pragma once
// Read-only Actor root and mesh ComponentToWorld, current native getter-backed
// FTransform48 offsets. Caller establishes class ancestry and strong tokens.
#include "ww_restore_policy.h"
namespace wwp_playback { namespace world_snapshot {
namespace id=ww_restore;
struct Transform {std::array<float,4> rotation{};std::array<float,3> translation{},scale{};};
struct Manifest {id::Token actor,root,mesh;std::array<uint8_t,32> source_sha256{},observed_sha256{};
    bool actor_class_verified=false,scene_class_verified=false,mesh_class_verified=false,current_layout_verified=false;};
struct Sample {id::Token actor,root,mesh;Transform actor_world,mesh_world;std::array<uint8_t,48> actor_bytes{},mesh_bytes{};
    uint64_t root_pointer=0,mesh_owner=0;bool accepted=false;};
enum class Code:uint8_t {None,InvalidManifest,ReadFailed,IdentityChanged,RelationChanged,NonFinite};
inline bool Valid(const Transform& t){double n=0;for(float x:t.rotation){if(!std::isfinite(x))return false;n+=double(x)*x;}
    if(std::fabs(n-1)>.002)return false;for(float x:t.translation)if(!std::isfinite(x)||std::fabs(x)>1e9f)return false;
    for(float x:t.scale)if(!std::isfinite(x)||std::fabs(x)<1e-8||std::fabs(x)>1e6)return false;return true;}
inline Transform Decode(const std::array<uint8_t,48>& b){Transform t;std::memcpy(t.rotation.data(),b.data(),16);std::memcpy(t.translation.data(),b.data()+16,12);std::memcpy(t.scale.data(),b.data()+32,12);return t;}
inline bool Valid(const Manifest& m){return id::Strong(m.actor)&&id::Strong(m.root)&&id::Strong(m.mesh);}
template<class Reader>Code Pass(Reader& r,const Manifest& m,Sample* out){
    if(!r.Identify(m.actor.address,&out->actor)||!r.Identify(m.root.address,&out->root)||!r.Identify(m.mesh.address,&out->mesh))return Code::ReadFailed;
    if(!id::Same(out->actor,m.actor)||!id::Same(out->root,m.root)||!id::Same(out->mesh,m.mesh))return Code::IdentityChanged;
    if(!r.Read(m.actor.address+0x1a0,&out->root_pointer,8)||!r.Read(m.mesh.address+0xa8,&out->mesh_owner,8)||
       !r.Read(m.root.address+0x1e0,out->actor_bytes.data(),48)||!r.Read(m.mesh.address+0x1e0,out->mesh_bytes.data(),48))return Code::ReadFailed;
    if(out->root_pointer!=m.root.address||out->mesh_owner!=m.actor.address)return Code::RelationChanged;
    out->actor_world=Decode(out->actor_bytes);out->mesh_world=Decode(out->mesh_bytes);return Valid(out->actor_world)&&Valid(out->mesh_world)?Code::None:Code::NonFinite;}
template<class Reader>bool Capture(Reader& r,const Manifest& m,Sample* out,Code* error=nullptr)noexcept{
    Sample a;auto code=Valid(m)?Pass(r,m,&a):Code::InvalidManifest;if(error)*error=code;
    if(code!=Code::None)return false;a.accepted=true;*out=a;return true;}
// Explicit TRS placement from model/component to current UE world; preserve
// signed scale and leave source-MMD basis/unit transport to ww_camera math.
inline std::array<double,3> Position(const Transform& t,const std::array<double,3>& component){
    const double x=component[0]*t.scale[0],y=component[1]*t.scale[1],z=component[2]*t.scale[2];
    const double qx=t.rotation[0],qy=t.rotation[1],qz=t.rotation[2],qw=t.rotation[3];
    const double tx=2*(qy*z-qz*y),ty=2*(qz*x-qx*z),tz=2*(qx*y-qy*x);
    return {{t.translation[0]+x+qw*tx+qy*tz-qz*ty,t.translation[1]+y+qw*ty+qz*tx-qx*tz,t.translation[2]+z+qw*tz+qx*ty-qy*tx}};}
}} // namespace wwp_playback::world_snapshot
