#pragma once
// Game-thread timeline and preallocated SPSC control queue. No engine, file IO,
// allocation, locks, parser, or callback-side string handling. Clip loading and
// wire-file reading belong to a worker. One producer / one consumer only.
#include <array>
#include <atomic>
#include <cmath>
#include <cstdint>
#include <cstring>
#include <type_traits>
#include "PlaybackCommands.generated.h"

namespace wwp_playback {
enum class PlayerMode : uint32_t {Empty, Stopped, Playing, Paused, Ended};
enum class ControlCode : uint32_t {Ok, InvalidCommand, StaleEpoch, DuplicateSequence, ClockInvalid, RestorePending};
struct Command {uint64_t clip_epoch=0, sequence=0; CommandKind kind=CommandKind::Play; double value=0; uint32_t target_slot=0;};
struct PlayerState {
    uint64_t clip_epoch=0, last_sequence=0, revision=0;
    double duration_seconds=0, seconds=0, speed=1;
    double time_limit_seconds=0, session_elapsed_seconds=0;
    double placement_x=0, placement_y=0, placement_height=0, placement_yaw_degrees=0;
    double camera_track_height_cm=0;
    PlayerMode mode=PlayerMode::Empty;
    uint32_t camera_mode=0;
    bool loop=false, restore_requested=false, physics_enabled=false, placement_in_place=false, time_limit_reached=false;
};
static_assert(std::is_trivially_copyable<Command>::value, "fixed command");
constexpr uint32_t kCommandWireBytes=64;
using CommandWire=std::array<uint8_t,kCommandWireBytes>;
inline uint32_t LoadU32(const uint8_t* p) {uint32_t v=0;for(unsigned i=0;i<4;++i)v|=uint32_t(p[i])<<(i*8);return v;}
inline uint64_t LoadU64(const uint8_t* p) {uint64_t v=0;for(unsigned i=0;i<8;++i)v|=uint64_t(p[i])<<(i*8);return v;}
inline uint32_t WireChecksum(const CommandWire& bytes) {
    uint32_t hash=2166136261u;for(unsigned i=0;i<60;++i){hash^=bytes[i];hash*=16777619u;}return hash;
}
inline bool ValidCommand(const Command& command) {
    return command.clip_epoch&&command.sequence&&ValidCommandArguments(command.kind,command.value,command.target_slot);
}
// A well-formed frame retains its identity even when its arguments are invalid,
// so the scene host can publish a rejection for that sequence.
inline bool DecodeCommandFrame(const CommandWire& wire,Command* output) {
    const char magic[8]={'W','W','P','C','T','L','1','\0'};
    if(!output||std::memcmp(wire.data(),magic,8)||LoadU32(wire.data()+8)!=1||LoadU32(wire.data()+12)!=64||
        LoadU64(wire.data()+48)||LoadU32(wire.data()+56)||
        LoadU32(wire.data()+60)!=WireChecksum(wire))return false;
    Command command{};command.clip_epoch=LoadU64(wire.data()+16);command.sequence=LoadU64(wire.data()+24);
    command.kind=CommandKind(LoadU32(wire.data()+32));command.target_slot=LoadU32(wire.data()+36);const uint64_t bits=LoadU64(wire.data()+40);
    std::memcpy(&command.value,&bits,8);*output=command;return true;
}
inline bool DecodeCommand(const CommandWire& wire,Command* output) {
    Command command;if(!DecodeCommandFrame(wire,&command)||!ValidCommand(command)||!output)return false;
    *output=command;return true;
}
template<unsigned Capacity=64> class SpscCommandQueue {
    static_assert(Capacity>=2&&Capacity<=1024,"bounded control queue");
    std::array<Command,Capacity> slots_{};
    std::atomic<unsigned> write_{0},read_{0};
public:
    bool Push(const Command& command) noexcept {
        const unsigned write=write_.load(std::memory_order_relaxed),next=(write+1)%Capacity;
        if(next==read_.load(std::memory_order_acquire))return false;
        slots_[write]=command;write_.store(next,std::memory_order_release);return true;
    }
    bool Pop(Command* output) noexcept {
        if(!output)return false;const unsigned read=read_.load(std::memory_order_relaxed);
        if(read==write_.load(std::memory_order_acquire))return false;
        *output=slots_[read];read_.store((read+1)%Capacity,std::memory_order_release);return true;
    }
};
class Transport {
    PlayerState state_{};double last_clock_=0;bool clock_valid_=false,session_clock_active_=false;
    ControlCode BeforeCommand(const Command& command,double now) noexcept {
        if(!ValidCommand(command))return ControlCode::InvalidCommand;
        if(command.clip_epoch!=state_.clip_epoch||state_.mode==PlayerMode::Empty)return ControlCode::StaleEpoch;
        if(command.sequence<=state_.last_sequence)return ControlCode::DuplicateSequence;
        const auto tick=Tick(now);if(tick!=ControlCode::Ok)return tick;
        if(state_.restore_requested&&command.kind!=CommandKind::Stop)return ControlCode::RestorePending;
        return ControlCode::Ok;
    }
public:
    const PlayerState& State() const noexcept {return state_;} // game thread only.
    bool SetInitialCameraMode(uint32_t mode) noexcept {
        if(mode>2||state_.mode!=PlayerMode::Stopped||state_.last_sequence||state_.restore_requested)return false;
        state_.camera_mode=mode;return true;
    }
    bool SetClip(uint64_t epoch,double duration) noexcept {
        if(!epoch||!std::isfinite(duration)||duration<0||duration>10000000||state_.restore_requested||
            (state_.mode!=PlayerMode::Empty&&state_.mode!=PlayerMode::Stopped))return false;
        state_=PlayerState{};state_.clip_epoch=epoch;state_.duration_seconds=duration;
        state_.mode=PlayerMode::Stopped;clock_valid_=false;session_clock_active_=false;return true;
    }
    // Host-only startup barrier, configured before callbacks are installed.
    // Hold the first pose without starting the residency clock. A later Play
    // starts the ordinary clock; an ordinary Pause still cannot activate Stop.
    bool PreparePausedStart() noexcept {
        if(state_.mode!=PlayerMode::Stopped||state_.seconds!=0||state_.restore_requested||session_clock_active_)return false;
        state_.mode=PlayerMode::Paused;++state_.revision;return true;
    }
    ControlCode Tick(double now) noexcept {
        if(!std::isfinite(now)||now<0)return ControlCode::ClockInvalid;
        if(!clock_valid_){last_clock_=now;clock_valid_=true;return ControlCode::Ok;}
        if(now<last_clock_)return ControlCode::ClockInvalid;
        const double delta=now-last_clock_;last_clock_=now;
        // Residency is a wall-clock limit. Pausing or reaching the final pose
        // must not bypass a user-selected limit; zero means unlimited.
        if(session_clock_active_){
            state_.session_elapsed_seconds+=delta;
            if(state_.time_limit_seconds>0&&state_.session_elapsed_seconds>=state_.time_limit_seconds){
                state_.time_limit_reached=true;state_.restore_requested=true;state_.mode=PlayerMode::Stopped;
                session_clock_active_=false;++state_.revision;return ControlCode::Ok;
            }
        }
        if(state_.mode!=PlayerMode::Playing)return ControlCode::Ok;
        const double duration=state_.duration_seconds;
        if(!duration){state_.seconds=0;state_.mode=PlayerMode::Ended;++state_.revision;return ControlCode::Ok;}
        if(!delta)return ControlCode::Ok;
        if(state_.loop) {
            const double period=duration/state_.speed;
            state_.seconds=std::fmod(state_.seconds+std::fmod(delta,period)*state_.speed,duration);
        } else if(delta>=(duration-state_.seconds)/state_.speed) {
            state_.seconds=duration;state_.mode=PlayerMode::Ended;
        } else state_.seconds+=delta*state_.speed;
        ++state_.revision;return ControlCode::Ok;
    }
    ControlCode Apply(const Command& command,double now) noexcept {
        const auto ready=BeforeCommand(command,now);if(ready!=ControlCode::Ok)return ready;
        switch(command.kind) {
        case CommandKind::Play:
            if(state_.mode==PlayerMode::Ended)state_.seconds=0;
            if(!session_clock_active_){state_.session_elapsed_seconds=0;state_.time_limit_reached=false;session_clock_active_=true;}
            state_.mode=PlayerMode::Playing;break;
        case CommandKind::Pause:
            if(state_.mode!=PlayerMode::Stopped)state_.mode=PlayerMode::Paused;
            break;
        case CommandKind::SeekSeconds:
            state_.seconds=command.value<state_.duration_seconds?command.value:state_.duration_seconds;
            // A stopped cursor is a selection, not an active pose preview.
            // Only Play may activate a newly loaded or stopped clip.
            if(state_.mode==PlayerMode::Ended)state_.mode=PlayerMode::Paused;
            break;
        case CommandKind::StepFrames: {
            double seconds=state_.seconds+command.value/30.0;
            state_.seconds=seconds<0?0:seconds>state_.duration_seconds?state_.duration_seconds:seconds;
            if(state_.mode!=PlayerMode::Stopped)state_.mode=PlayerMode::Paused;
            break;
        }
        case CommandKind::SetLoop:state_.loop=command.value!=0;break;
        case CommandKind::SetSpeed:state_.speed=command.value;break;
        case CommandKind::SetTimeLimit:state_.time_limit_seconds=command.value;break;
        case CommandKind::SetPhysics:state_.physics_enabled=command.value!=0;break;
        case CommandKind::SetCameraMode:state_.camera_mode=uint32_t(command.value);break;
        case CommandKind::SetCameraTrackHeight:state_.camera_track_height_cm=command.value;break;
        // Placement remains a selection while stopped; only Play activates it.
        // Stop preserves this user selection, while SetClip resets it.
        case CommandKind::SetPlacementX:state_.placement_x=command.value;break;
        case CommandKind::SetPlacementY:state_.placement_y=command.value;break;
        case CommandKind::SetPlacementHeight:state_.placement_height=command.value;break;
        case CommandKind::SetPlacementYaw:state_.placement_yaw_degrees=command.value;break;
        case CommandKind::SetPlacementInPlace:state_.placement_in_place=command.value!=0;break;
        case CommandKind::ResetPlacement:
            state_.placement_x=state_.placement_y=state_.placement_height=state_.placement_yaw_degrees=0;
            state_.placement_in_place=false;break;
        case CommandKind::SetGroundHeight:return ControlCode::InvalidCommand; // Scene host owns the shared height.
        case CommandKind::Stop:
            state_.restore_requested=state_.mode!=PlayerMode::Stopped||state_.seconds!=0||state_.restore_requested;
            state_.seconds=0;state_.mode=PlayerMode::Stopped;session_clock_active_=false;break;
        }
        state_.last_sequence=command.sequence;++state_.revision;return ControlCode::Ok;
    }
    // Actor-specific settings live outside the common clock. Hosts apply the
    // setting under their callback lock after this validates/accepts its order.
    ControlCode AcceptRoutedCommand(const Command& command,double now) noexcept {
        const auto kind=command.kind;
        if(!IsActorCommand(kind)&&kind!=CommandKind::SetGroundHeight)return ControlCode::InvalidCommand;
        const auto ready=BeforeCommand(command,now);if(ready!=ControlCode::Ok)return ready;
        state_.last_sequence=command.sequence;++state_.revision;return ControlCode::Ok;
    }
    template<unsigned Capacity> unsigned Drain(SpscCommandQueue<Capacity>& queue,double now,
                                               ControlCode* last_result=nullptr) noexcept {
        unsigned processed=0;Command command{};
        while(processed<Capacity&&queue.Pop(&command)){const auto code=Apply(command,now);if(last_result)*last_result=code;++processed;}
        return processed;
    }
    void AcknowledgeRestore() noexcept {state_.restore_requested=false;}
};
} // namespace wwp_playback
