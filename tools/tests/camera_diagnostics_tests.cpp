#include "../../playback/camera_diagnostics.h"
#include <cstdlib>
#include <iostream>
#include <thread>

namespace camera=wwp_playback::camera_diagnostics;
static unsigned assertions=0;
static void Check(bool condition,const char* message){++assertions;if(!condition){std::cerr<<message<<'\n';std::exit(1);}}
static camera::Snapshot Value(unsigned index){
    const auto n=float(index);
    return {(index&1)!=0,{n,n+1,n+2},{n+3,n+4,n+5,n+6},n+7};
}
static bool Consistent(const camera::Snapshot& value){
    const auto n=value.anchor[0];
    return value.anchor_captured==((unsigned(n)&1)!=0)&&value.anchor[1]==n+1&&value.anchor[2]==n+2&&
        value.source_to_ue[0]==n+3&&value.source_to_ue[1]==n+4&&value.source_to_ue[2]==n+5&&
        value.source_to_ue[3]==n+6&&value.source_unit_scale==n+7;
}
int main(){
    camera::Mailbox mailbox;camera::Snapshot snapshot;
    Check(mailbox.TryRead(snapshot)&&!snapshot.anchor_captured&&snapshot.source_to_ue[3]==1,"unused diagnostic snapshot is initialized");
    mailbox.Publish(Value(1));
    Check(mailbox.TryRead(snapshot)&&Consistent(snapshot),"camera diagnostic snapshot round trips");
    constexpr unsigned iterations=50000;
    std::atomic<bool> start{false},done{false};
    std::thread writer([&]{while(!start.load())std::this_thread::yield();for(unsigned i=2;i<=iterations;++i)mailbox.Publish(Value(i));done.store(true);});
    start.store(true);unsigned observed=0;
    do{if(mailbox.TryRead(snapshot)){Check(Consistent(snapshot),"concurrent diagnostic read must contain one complete camera frame");++observed;}}while(!done.load());
    writer.join();
    Check(observed!=0&&mailbox.TryRead(snapshot)&&snapshot.anchor[0]==float(iterations),"final camera diagnostics remain readable after writer retirement");
    mailbox.Publish({});Check(mailbox.TryRead(snapshot)&&!snapshot.anchor_captured,"restoring camera clears captured diagnostic state");
    std::cout<<"camera diagnostics: "<<assertions<<" assertions passed; concurrent snapshots="<<observed<<'\n';
}
