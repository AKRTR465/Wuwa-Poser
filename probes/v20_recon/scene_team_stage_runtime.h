#pragma once
#include "stage_capture_readonly.h"
namespace scene_team_stage {
static void Require(bool ok,const char* message){if(!ok)throw std::runtime_error(message);}
static uint64_t EngineFrame(Reader& r,uint64_t image){uint64_t frame=0;Require(r.Get(image+0x992f2d8,frame)&&frame,"stage engine frame unavailable");return frame;}
} // namespace scene_team_stage
