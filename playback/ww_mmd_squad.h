#pragma once
// Shared timeline for a fixed three-member WW squad. Caller owns hook leases,
// game-thread synchronization and actual restore readbacks.
#include "player_transport.h"
#include <array>
#include <cstdint>

namespace wwp_playback { namespace ww_mmd_squad {
constexpr unsigned kMembers=3;
enum class ClockCode:uint8_t {Ok,NotReady,ClockInvalid,CommandRejected};
struct Snapshot {
    uint64_t engine_frame=0;
    PlayerState player;
};
class SharedTimeline {
    Transport transport_;
    Snapshot snapshot_{};
    double frame_clock_=0;
    void Export() noexcept {snapshot_.player=transport_.State();}
public:
    const Snapshot& State() const noexcept {return snapshot_;} // same owner thread/lock only
    void Configure(uint64_t epoch,const std::array<double,kMembers>& durations,uint8_t enabled) noexcept {
        double maximum=0;
        for(unsigned i=0;i<kMembers;++i)if((enabled&(1u<<i))&&durations[i]>maximum)maximum=durations[i];
        transport_.SetClip(epoch,maximum);snapshot_={};frame_clock_=0;Export();
    }
    ClockCode Advance(uint64_t frame,double now) noexcept {
        // Every actor in this engine frame sees exactly the same timeline. A
        // duplicate callback may arrive later in wall time: do not tick again.
        if(frame==snapshot_.engine_frame)return ClockCode::Ok;
        if(transport_.Tick(now)!=ControlCode::Ok)return ClockCode::ClockInvalid;
        snapshot_.engine_frame=frame;frame_clock_=now;Export();return ClockCode::Ok;
    }
    // Commands use the already captured frame clock, never each actor's
    // independently sampled time. Invalid/stale commands preserve state.
    ClockCode Apply(const Command& command,bool ready) noexcept {
        if(command.kind==CommandKind::Play&&!ready)return ClockCode::NotReady;
        if(transport_.Apply(command,frame_clock_)!=ControlCode::Ok)return ClockCode::CommandRejected;
        Export();return ClockCode::Ok;
    }
    ClockCode AcceptRoutedCommand(const Command& command) noexcept {
        if(transport_.AcceptRoutedCommand(command,frame_clock_)!=ControlCode::Ok)return ClockCode::CommandRejected;
        Export();return ClockCode::Ok;
    }
    void FaultStop() noexcept {
        const auto state=transport_.State();
        transport_.Apply({state.clip_epoch,state.last_sequence+1,CommandKind::Stop,0},frame_clock_);Export();
    }
};
}} // namespace wwp_playback::ww_mmd_squad
