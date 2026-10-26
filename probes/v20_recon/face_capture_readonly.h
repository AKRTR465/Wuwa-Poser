#pragma once
#include "../../runtime/generated/GameCompat.generated.h"
// Include after shared_readonly.h's Reader/Go/Pool/Decode helpers.
// Reads the live mesh asset morph catalog; no native engine calls or writes.
#include "../../playback/ww_face_snapshot.h"
#include <memory>

namespace face_capture_readonly_detail {
namespace fs=wwp_playback::face_snapshot;
struct Adapter {
    Reader& reader;const Go& objects;const Pool& pool;
    bool Read(uint64_t address,void* out,size_t n){return reader.Read(address,out,n);}
    bool Identify(uint64_t address,fs::Identity* out) {
        unsigned char header[ww_game_compat::ObjectHeaderBytes],item[ww_game_compat::ObjectItemBytes];
        if(!reader.Read(address,header,sizeof(header)))return false;uint32_t index=At<uint32_t>(header,ww_game_compat::ObjectIndexOffset);
        uint64_t chunk=0;
        if(index>=objects.count||!reader.Get(objects.table+(uint64_t)(index>>ww_game_compat::ObjectArrayChunkShift)*ww_game_compat::PointerBytes,chunk))return false;
        const uint64_t itemAddress=chunk+(uint64_t)(index&ww_game_compat::ObjectArrayChunkMask)*ww_game_compat::ObjectItemBytes;if(!reader.Read(itemAddress,item,sizeof(item))||At<uint64_t>(item,ww_game_compat::ObjectItemObjectOffset)!=address)return false;
        fs::Identity id;id.address=address;id.index=index;id.serial=At<uint32_t>(item,ww_game_compat::ObjectItemSerialOffset);id.item_flags=At<uint32_t>(item,ww_game_compat::ObjectItemFlagsOffset);
        id.object_flags=At<uint32_t>(header,ww_game_compat::ObjectFlagsOffset);id.class_address=At<uint64_t>(header,ww_game_compat::ObjectClassOffset);id.outer_address=At<uint64_t>(header,ww_game_compat::ObjectOuterOffset);
        for(unsigned k=0;k<ww_game_compat::NameWordCount;++k)id.name[k]=At<uint32_t>(header,ww_game_compat::ObjectNameOffset+k*ww_game_compat::NameWordBytes);
        *out=id;return true;
    }
};
}

// Captures the mesh/asset identities and asset morph names in asset order.
static std::unique_ptr<wwp_playback::face_snapshot::Snapshot> CaptureFaceCatalog(Reader& reader,Go& objects,Pool& pool,uint64_t mesh,uint64_t asset,uint32_t morphCount,
    wwp_playback::face_snapshot::Manifest& manifest) {
    namespace fs=wwp_playback::face_snapshot;using namespace face_capture_readonly_detail;
    Adapter adapter{reader,objects,pool};
    if(!adapter.Identify(mesh,&manifest.expected_mesh)||!adapter.Identify(asset,&manifest.expected_asset))throw std::runtime_error("face identities unavailable");
    manifest.mesh_asset_offset=ww_game_compat::ComponentAssetOffset;manifest.asset_morph_array_offset=ww_game_compat::AssetMorphArrayOffset;
    manifest.map_offset=ww_game_compat::ComponentMorphMapOffset;manifest.expected_morphs=morphCount;
    auto decode=[&](const std::array<uint32_t,3>& words,char* out,size_t size){std::string name;
        if(!Decode(reader,pool,words[0],name))return false;if(words[2])name+="_"+std::to_string(words[2]-1);
        if(name.size()+1>size)return false;memcpy(out,name.c_str(),name.size()+1);return true;};
    auto captured=std::make_unique<fs::Snapshot>();
    if(!fs::Capture(adapter,decode,manifest,captured.get()))throw std::runtime_error("face catalog unavailable");
    return captured;
}
