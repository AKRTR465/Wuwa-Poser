#pragma once
// Initialization-only discovery. No hooks, engine calls, or game writes.
#include "shared_readonly.h"
#include "../../runtime/generated/GameCompat.generated.h"

namespace ww_game_object_anchors {
inline Json Resolve(Reader& reader,uint64_t image,uint64_t preferred_go_rva,uint64_t preferred_pool_rva,Go& objects,Pool& names){
    objects={};names={};Go candidate_go;Pool candidate_pool;
    Json report={{"compatibility_profile",ww_game_compat::ProfileId},{"compatibility_source_sha256",ww_game_compat::SourceSha256},
        {"status","unresolved"},{"runtime_layout_verified",false},{"game_memory_writes",0}};
    const auto valid_rva=[&](uint64_t rva){return rva&&image<=UINT64_MAX-rva;};
    if(valid_rva(preferred_go_rva)&&valid_rva(preferred_pool_rva)&&
       ValidateGo(reader,image+preferred_go_rva,candidate_go)&&ValidatePool(reader,image+preferred_pool_rva,candidate_pool)&&Consistent(reader,candidate_go,candidate_pool)){
        objects=candidate_go;names=candidate_pool;report["method"]="profile_cross_validated";
    }else{
        const auto scan=ReconProcess(image,&candidate_go,&candidate_pool);
        report["scan"]=scan;
        if(scan.value("status",std::string())!="anchors_cross_validated")return report;
        // The scan requires exactly one mutually consistent pair. Validate
        // again before publishing it; discovery cannot authorize stale data.
        Go fresh_go;Pool fresh_pool;
        if(!ValidateGo(reader,candidate_go.address,fresh_go)||!ValidatePool(reader,candidate_pool.address,fresh_pool)||
           fresh_go.table!=candidate_go.table||fresh_go.count!=candidate_go.count||fresh_pool.blocks!=candidate_pool.blocks||
           !Consistent(reader,fresh_go,fresh_pool)){
            report["reason"]="discovered anchors changed during validation";return report;
        }
        objects=fresh_go;names=fresh_pool;report["method"]="unique_scan_cross_validated";
    }
    report["status"]="anchors_cross_validated";
    report["gobjects_rva"]=Hex(objects.address-image);report["name_pool_rva"]=Hex(names.address-image);
    report["object_count"]=objects.count;
    // This verifies the selected anchors against the supported object/name
    // interpretation, not every private animation or calling ABI.
    report["anchor_checks_passed"]=true;
    return report;
}
}
