#include "../../runtime/owned_mmd/native/owned_recovery.h"
#include <cstdlib>
#include <iostream>
#include <stdexcept>

using owned_recovery::Phase;
using owned_recovery::Progress;
static unsigned assertions=0;
static void Check(bool ok,const char* message){++assertions;if(!ok){std::cerr<<message<<'\n';std::exit(1);}}
int main(){
    owned_recovery::InvocationBoundary bindingFailure;unsigned entered=0;
    try{bindingFailure.Run(true,[]{throw std::runtime_error("missing helper action");},[]{},[&]{++entered;});}catch(const std::runtime_error&){}
    Check(!bindingFailure.begin_entered&&entered==0,"missing helper method never enters actor creation");
    owned_recovery::InvocationBoundary rejectedSession;
    try{rejectedSession.Run(true,[]{},[]{throw std::runtime_error("other session owns actors");},[&]{++entered;});}catch(const std::runtime_error&){}
    Check(!rejectedSession.begin_entered&&entered==0,"rejected session cannot enter another session's actor action");
    owned_recovery::InvocationBoundary partialBegin;
    try{partialBegin.Run(true,[]{},[]{},[&]{++entered;throw std::runtime_error("creation failed after partial ownership");});}catch(const std::runtime_error&){}
    Check(partialBegin.begin_entered&&entered==1,"partially entered creation remains subject to managed cleanup");
    Progress noBegin(bindingFailure.begin_entered);unsigned invalidCleanup=0;
    Check(noBegin.Advance(0,[]{return true;},[]{return true;},[&]{++invalidCleanup;return false;},[&]{++invalidCleanup;return false;})&&invalidCleanup==0,"binding failure can retire without retrying unavailable managed cleanup");
    Progress partial(partialBegin.begin_entered);
    Check(!partial.Advance(0,[]{return true;},[]{return true;},[]{return false;},[]{return true;})&&!partial.Complete(),"partial Begin failure cannot bypass actor cleanup");
    Progress progress(true);unsigned nativeCalls=0,observerCalls=0,stopCalls=0,verifyCalls=0;
    bool nativeReady=false,observersReady=false,stopReady=false,releaseReady=false,throwStop=false;
    const auto advance=[&](uint64_t now){return progress.Advance(now,
        [&]{++nativeCalls;return nativeReady;},[&]{++observerCalls;return observersReady;},
        [&]{++stopCalls;if(throwStop)throw std::runtime_error("temporary managed error");return stopReady;},
        [&]{++verifyCalls;return releaseReady;});};
    Check(!advance(1000)&&progress.Current()==Phase::Native,"failed native cleanup retains its phase");
    Check(!progress.Complete()&&observerCalls==0&&stopCalls==0&&verifyCalls==0,"native ownership blocks actor deletion and terminal state");
    progress.Defer(1000);Check(!advance(2999)&&nativeCalls==1,"retry delay prevents rapid native cleanup attempts");
    nativeReady=true;Check(!advance(3000)&&progress.Current()==Phase::Observers,"native completion proceeds to observer cleanup");
    Check(stopCalls==0&&progress.native_restored&&!progress.observers_restored,"observer cleanup blocks actor deletion");
    progress.Defer(3000);observersReady=true;throwStop=true;
    try{advance(5000);Check(false,"managed exception expected");}catch(const std::runtime_error&){}
    Check(progress.Current()==Phase::ManagedStop&&!progress.managed_stopped&&!progress.Complete(),"managed exception keeps dispatch retryable");
    Check(nativeCalls==2&&observerCalls==2,"completed native and observer stages are not repeated");
    progress.Defer(5000);throwStop=false;Check(!advance(7000)&&stopCalls==2,"unsuccessful managed stop is retried");
    Check(verifyCalls==0&&!progress.Complete(),"stop failure cannot authorize release confirmation");
    progress.Defer(7000);stopReady=true;Check(!advance(9000)&&progress.Current()==Phase::ManagedRelease,"successful stop still requires map-removal confirmation");
    Check(stopCalls==3&&verifyCalls==1&&!progress.Complete(),"pending release keeps the recovery owner live");
    progress.Defer(9000);releaseReady=true;Check(advance(11000)&&progress.Complete(),"later cleanup success permits terminal state");
    Check(stopCalls==3&&verifyCalls==2,"release polling does not repeat a successful destruction request");
    Check(advance(11001)&&verifyCalls==2,"completed cleanup is idempotent");

    Progress noActor(false);unsigned unnecessaryManaged=0;
    Check(noActor.Advance(0,[]{return true;},[]{return true;},[&]{++unnecessaryManaged;return false;},[&]{++unnecessaryManaged;return false;}),"startup without an actor can finish after native cleanup");
    Check(unnecessaryManaged==0&&noActor.Complete(),"no-actor startup never invokes managed destruction");
    std::cout<<"owned recovery: "<<assertions<<" assertions passed\n";
}
