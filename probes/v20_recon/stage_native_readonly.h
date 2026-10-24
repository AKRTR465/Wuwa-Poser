#pragma once
#include "stage_capture_readonly.h"
namespace stage_native {
namespace q=ww_object;
static Json Reference(Reader& r,const Go& go,const Pool& pool,const q::Identity& mesh,const Json& profiles){
    uint64_t assetAddress=0,skeletonAddress=0;q::Identity asset,skeleton;
    if(!r.Get(mesh.object+0x5d0,assetAddress)||!q::ReadStable(r,go,assetAddress,&asset)||
       !r.Get(asset.object+0xd8,skeletonAddress)||!q::ReadStable(r,go,skeletonAddress,&skeleton))
        throw std::runtime_error("scan skeleton asset unavailable");
    const auto assetName=q::Name(r,pool,asset),skeletonName=q::Name(r,pool,skeleton);const Json* profile=nullptr;
    for(const auto& p:profiles){const auto& names=p.at("mesh_asset_names");
        if(std::find(names.begin(),names.end(),Json(assetName))!=names.end()&&p.at("skeleton_asset_name")==skeletonName){profile=&p;break;}
    }
    if(!profile)throw std::runtime_error("scan character profile unsupported");
    unsigned char head[16]{};if(!r.Read(asset.object+0x3b8,head,16))throw std::runtime_error("scan reference bones unreadable");
    const uint64_t data=At<uint64_t>(head,0);const uint32_t count=At<uint32_t>(head,8),capacity=At<uint32_t>(head,12);
    if(profile->at("bone_count")!=count)throw std::runtime_error("scan reference bone extent differs");
    std::vector<unsigned char> bones(size_t(count)*16);if(!r.Read(data,bones.data(),bones.size()))throw std::runtime_error("scan reference bone payload unreadable");
    Json names=Json::array(),parents=Json::array();
    for(uint32_t i=0;i<count;++i){const auto* entry=bones.data()+size_t(i)*16;std::string name;
        if(!q::DecodeName(r,pool,At<uint32_t>(entry,0),name))throw std::runtime_error("scan bone name unavailable");
        const uint32_t number=At<uint32_t>(entry,8);if(number)name+="_"+std::to_string(number-1);
        names.push_back(name);parents.push_back(At<int32_t>(entry,12));
    }
    if(names!=profile->at("bone_names")||parents!=profile->at("bone_parents"))throw std::runtime_error("scan full skeleton differs from registered profile");
    return {{"mesh_asset",stage_capture::Token(r,go,pool,asset)},{"skeleton",stage_capture::Token(r,go,pool,skeleton)},
        {"profile_id",profile->at("profile_id")},{"reference_identity_verified",true},{"reference_bone_count",count},
        {"reference_bone_names",names},{"reference_bone_parents",parents},{"reference_raw_hex",BytesHex(bones.data(),bones.size())},
        {"reference_data",Hex(data)},{"reference_capacity",capacity}};
}
static bool LocalControl(Reader& r,const Go& go,const Pool&,const q::Identity& actor,q::Identity* controller,q::Identity* local){
    uint64_t pc=0,player=0;
    return r.Get(actor.object+0x2e8,pc)&&q::ReadStable(r,go,pc,controller)&&
        r.Get(pc+0x338,player)&&q::ReadStable(r,go,player,local);
}
} // namespace stage_native
