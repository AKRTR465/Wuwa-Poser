#include "../../playback/player_transport.h"
#include "../../playback/ww_mmd_squad.h"
#include <cmath>
#include <cstdlib>
#include <iostream>

using namespace wwp_playback;
static unsigned assertions=0;
static void Check(bool condition,const char* message){++assertions;if(!condition){std::cerr<<message<<'\n';std::exit(1);}}
static bool Close(double a,double b){return std::fabs(a-b)<1e-8;}
static void Put32(CommandWire& wire,unsigned offset,uint32_t value){for(unsigned n=0;n<4;++n)wire[offset+n]=uint8_t(value>>(8*n));}
static void Put64(CommandWire& wire,unsigned offset,uint64_t value){for(unsigned n=0;n<8;++n)wire[offset+n]=uint8_t(value>>(8*n));}
static CommandWire Wire(Command command){CommandWire wire{};std::memcpy(wire.data(),"WWPCTL1",7);Put32(wire,8,1);Put32(wire,12,64);
    Put64(wire,16,command.clip_epoch);Put64(wire,24,command.sequence);Put32(wire,32,uint32_t(command.kind));Put32(wire,36,command.target_slot);
    uint64_t bits=0;std::memcpy(&bits,&command.value,8);Put64(wire,40,bits);Put32(wire,60,WireChecksum(wire));return wire;}
int main(){
    Command decoded;
    auto wire=Wire({42,1,CommandKind::SetPlacementX,25,3});
    Check(DecodeCommand(wire,&decoded)&&decoded.target_slot==3&&decoded.value==25,"actor slot must survive wire decoding");
    Check(DecodeCommand(Wire({42,1,CommandKind::Play,0}),&decoded)&&decoded.target_slot==0,"legacy untargeted wire remains valid");
    Check(DecodeCommand(Wire({42,1,CommandKind::Play,0,UINT32_MAX}),&decoded)&&decoded.target_slot==UINT32_MAX,"full positive uint32 actor slot range accepted");
    wire[48]=1;Put32(wire,60,WireChecksum(wire));Check(!DecodeCommand(wire,&decoded),"reserved payload bytes remain reserved");
    wire=Wire({42,1,CommandKind::SetTimeLimit,0});wire[40]^=1;Check(!DecodeCommand(wire,&decoded),"modified payload checksum rejected");
    Check(ValidCommand({42,1,CommandKind::SetSpeed,.1})&&ValidCommand({42,1,CommandKind::SetSpeed,4}),"unified speed endpoints accepted");
    Check(!ValidCommand({42,1,CommandKind::SetSpeed,.09})&&!ValidCommand({42,1,CommandKind::SetSpeed,4.01}),"out-of-range speed rejected");

    Transport player;Check(player.SetClip(42,10),"clip configured");
    Check(player.Apply({42,1,CommandKind::Play,0},100)==ControlCode::Ok,"play starts");
    player.Tick(102);Check(Close(player.State().seconds,2),"playing cursor follows elapsed time");
    player.Apply({42,2,CommandKind::Pause,0},102);player.Tick(150);
    Check(player.State().mode==PlayerMode::Paused&&Close(player.State().seconds,2)&&!player.State().restore_requested,"pause retains cursor without restoration");
    player.Apply({42,3,CommandKind::SeekSeconds,7},150);player.Apply({42,4,CommandKind::StepFrames,-1},150);
    Check(Close(player.State().seconds,7-1.0/30)&&player.State().mode==PlayerMode::Paused,"seek and frame step retain paused preview");
    player.Apply({42,5,CommandKind::SetSpeed,4},150);player.Apply({42,6,CommandKind::Play,0},150);player.Tick(151);
    Check(player.State().mode==PlayerMode::Ended&&Close(player.State().seconds,10)&&!player.State().restore_requested,"end retains final pose");
    player.Tick(1000000);Check(player.State().mode==PlayerMode::Ended&&!player.State().restore_requested,"zero limit holds final pose indefinitely");
    Check(player.Apply({41,7,CommandKind::SeekSeconds,1},1000000)==ControlCode::StaleEpoch,"old-session command rejected");
    Check(player.Apply({42,6,CommandKind::SeekSeconds,1},1000000)==ControlCode::DuplicateSequence,"duplicate command rejected");
    Check(Close(player.State().seconds,10),"rejected commands preserve cursor");
    player.Apply({42,7,CommandKind::SetLoop,1},1000000);player.Apply({42,8,CommandKind::Play,0},1000000);player.Tick(1000003);
    Check(Close(player.State().seconds,2)&&player.State().mode==PlayerMode::Playing,"loop uses speed-scaled elapsed time");

    Transport limited;limited.SetClip(99,100);limited.Apply({99,1,CommandKind::SetTimeLimit,5},0);
    limited.Apply({99,2,CommandKind::Play,0},100);limited.Apply({99,3,CommandKind::Pause,0},101);limited.Tick(105);
    Check(limited.State().time_limit_reached&&limited.State().restore_requested&&limited.State().mode==PlayerMode::Stopped,"wall-clock residency expires while paused");
    Check(Close(limited.State().session_elapsed_seconds,5),"time limit begins at first play, not configuration");
    Check(limited.Apply({99,4,CommandKind::Play,0},106)==ControlCode::RestorePending,"expired session cannot bypass restoration");
    limited.AcknowledgeRestore();limited.Apply({99,4,CommandKind::SetTimeLimit,0},106);limited.Apply({99,5,CommandKind::Play,0},106);limited.Tick(10000);
    Check(!limited.State().time_limit_reached&&!limited.State().restore_requested,"explicit unlimited setting permits next restored playback");

    ww_mmd_squad::SharedTimeline squad;squad.Configure(55,{10,20,30},7);squad.Advance(1,100);
    Check(squad.Apply({55,1,CommandKind::Play,0},true)==ww_mmd_squad::ClockCode::Ok,"shared play accepted");
    squad.Advance(2,101);const auto first=squad.State().player.seconds;squad.Advance(2,101.75);
    Check(Close(first,1)&&Close(squad.State().player.seconds,first),"actors in one engine frame share exact cursor");
    Check(squad.AcceptRoutedCommand({55,2,CommandKind::SetPlacementX,80,2})==ww_mmd_squad::ClockCode::Ok,"per-actor command accepted by shared order");
    Check(squad.State().player.last_sequence==2&&squad.State().player.placement_x==0,"routed placement does not mutate shared placement");
    Check(squad.Apply({55,2,CommandKind::SetLoop,1},true)==ww_mmd_squad::ClockCode::CommandRejected,"routed sequence cannot be reused globally");
    Check(squad.AcceptRoutedCommand({55,3,CommandKind::SeekSeconds,4,2})==ww_mmd_squad::ClockCode::CommandRejected,"timeline commands cannot bypass shared clock");
    std::cout<<"unified transport: "<<assertions<<" assertions passed\n";
}
