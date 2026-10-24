#pragma once
// Worker-only read-only inventory. Include after shared_readonly.h.
namespace v20_weapon_capture {
inline bool Identity(Reader& r,const Go& go,const Pool& pool,uint64_t address,Json& out) {
    unsigned char h[48],item[24];uint64_t chunk=0;
    if(!Pointer(address)||!r.Read(address,h,sizeof(h)))return false;
    const uint32_t index=At<uint32_t>(h,12);const uint64_t cls=At<uint64_t>(h,16);
    if(index>=go.count||!r.Get(go.table+(uint64_t)(index>>16)*8,chunk))return false;
    const uint64_t itemAddress=chunk+(uint64_t)(index&65535)*24;
    if(!r.Read(itemAddress,item,sizeof(item))||At<uint64_t>(item,0)!=address)return false;
    uint32_t clsId=0;std::string name,className;
    if(!Decode(r,pool,At<uint32_t>(h,24),name)||!r.Get(cls+24,clsId)||!Decode(r,pool,clsId,className))return false;
    const uint32_t number=At<uint32_t>(h,32);if(number)name+="_"+std::to_string(number-1);
    out={{"address",Hex(address)},{"index",index},{"serial",At<uint32_t>(item,16)},{"item_address",Hex(itemAddress)},
         {"class_address",Hex(cls)},{"class",className},{"name",name},{"outer_address",Hex(At<uint64_t>(h,40))},
         {"vtable",Hex(At<uint64_t>(h,0))},{"name_words",Json::array({At<uint32_t>(h,24),At<uint32_t>(h,28),At<uint32_t>(h,32)})}};
    return true;
}
inline bool KnownScene(const std::string& name) {
    return name=="SceneComponent"||name=="StaticMeshComponent"||name=="SkeletalMeshComponent"||name=="SkinnedMeshComponent";
}
inline Json Asset(Reader& r,const Go& go,const Pool& pool,uint64_t component,const std::string& cls) {
    const uint32_t offset=cls=="StaticMeshComponent"?0x5e0u:cls=="SkeletalMeshComponent"||cls=="SkinnedMeshComponent"?0x5d0u:0u;
    Json result={{"pointer_offset",Hex(offset)}};uint64_t asset=0;Json identity;
    if(!offset||!r.Get(component+offset,asset)||!Identity(r,go,pool,asset,identity))return result;
    result["address"]=Hex(asset);result["identity"]=identity;result["name"]=identity.at("name");return result;
}
inline Json CaptureWeaponReadonly(Reader& r,const Go& go,const Pool& pool,uint64_t bodymesh,uint64_t pawn) {
    Json report={{"status","error"},{"components",Json::array()}};
    Json body,pawnIdentity;
    if(!Identity(r,go,pool,bodymesh,body)||!Identity(r,go,pool,pawn,pawnIdentity)){report["reason"]="body/pawn identity unavailable";return report;}
    report["body_identity"]=body;report["pawn_identity"]=pawnIdentity;
    std::map<uint64_t,std::string> classes;
    for(uint32_t first=0;first<go.count;first+=65536) {
        const uint32_t count=(std::min)(go.count-first,65536u);uint64_t chunk=0;
        std::vector<unsigned char> items((size_t)count*24);
        if(!r.Get(go.table+(uint64_t)(first>>16)*8,chunk)||!r.Read(chunk,items.data(),items.size()))continue;
        for(uint32_t i=0;i<count;++i) {
            const unsigned char* item=items.data()+(size_t)i*24;const uint64_t address=At<uint64_t>(item,0);
            if(!Pointer(address)||(At<uint32_t>(item,8)&0x30000000u))continue;
            unsigned char h[48];if(!r.Read(address,h,sizeof(h))||At<uint32_t>(h,12)!=first+i||(At<uint32_t>(h,8)&0x18000u))continue;
            const uint64_t cp=At<uint64_t>(h,16);auto cached=classes.find(cp);
            if(cached==classes.end()){uint32_t id=0;std::string cls;if(r.Get(cp+24,id))Decode(r,pool,id,cls);cached=classes.emplace(cp,cls).first;}
            const std::string cls=cached->second;if(!KnownScene(cls))continue;
            uint64_t owner=0,parent=0;unsigned char socket[12]={};
            if(!r.Get(address+0xa8,owner)||!r.Get(address+0xd0,parent)||!r.Read(address+0xd8,socket,12))continue;
            std::string socketName;if(Decode(r,pool,At<uint32_t>(socket,0),socketName)){
                const uint32_t number=At<uint32_t>(socket,8);if(number)socketName+="_"+std::to_string(number-1);}
            const bool owned=owner==pawn||At<uint64_t>(h,40)==pawn;
            bool attached=address==bodymesh;Json chain=Json::array();uint64_t current=parent;
            for(unsigned depth=0;current&&depth<16;++depth){
                Json node;if(!Identity(r,go,pool,current,node))break;chain.push_back(node);
                if(current==bodymesh){attached=true;break;}
                if(!KnownScene(node.at("class").get<std::string>())||!r.Get(current+0xd0,current))break;
            }
            if(!owned&&!attached)continue;
            Json identity;if(!Identity(r,go,pool,address,identity))continue;
            report["components"].push_back({{"identity",identity},{"owner_address",Hex(owner)},{"attach_parent",Hex(parent)},{"attachment_chain",chain},
                {"socket_name",socketName},{"socket_fname_words",Json::array({At<uint32_t>(socket,0),At<uint32_t>(socket,4),At<uint32_t>(socket,8)})},
                {"asset",Asset(r,go,pool,address,cls)}});
        }
    }
    report["status"]="captured";return report;
}
} // namespace v20_weapon_capture
