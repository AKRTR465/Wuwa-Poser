// VMD track-name decoding with the offline compilers' CP932 decoder. No game access.
#include "../../core_math/include/poser_math/mmd_motion.h"
#include "../mmd_compile_common.h"
#include <cstdlib>
#include <iostream>
#include <sstream>

static unsigned assertions=0;
static void Check(bool condition,const char* message){++assertions;if(!condition){std::cerr<<message<<'\n';std::exit(1);}}
int main(){
    std::string input="Vocaloid Motion Data 0002";
    input.resize(50,'\0');
    const auto put32=[&](uint32_t value){input.append(reinterpret_cast<const char*>(&value),sizeof(value));};
    const auto key=[&](std::string name,uint32_t frame){
        name.resize(15,'\0');
        input+=name;
        put32(frame);
        input.append(92,'\0');
    };
    const auto read=[&]{
        std::istringstream stream(input);
        mmd::Reader reader(stream,input.size());
        return mmd::ReadVmd(reader,wwp_mmd_compile::Decode);
    };
    const std::string prefix="Bone_Hair092_L";
    put32(4);
    key(prefix,0);
    key(prefix+"\x90",30);
    key(prefix+"\x91",60);
    key(std::string(13,'x')+"\x97\xbc",0);
    for(int section=0;section<4;++section)put32(0);
    const auto clip=read();
    Check(clip.bones.size()==4&&clip.lastFrame==60&&clip.bones.at(prefix).size()==1&&
          clip.bones.at(prefix+u8"\uFFFD" "90").front().frame==30&&
          clip.bones.at(prefix+u8"\uFFFD" "91").front().frame==60&&
          clip.bones.count(std::string(13,'x')+u8"両")==1,
          "a dangling full-width CP932 byte is retained as a distinct track without changing complete names");
    put32(1); // IK frames
    put32(0);
    input.push_back(1);
    put32(1);
    input+=prefix+"\x90";
    input.append(5,'\0'); // Same raw name, but now it ends before the 20-byte boundary.
    input.push_back(1);
    try{
        read();
        Check(false,"a padded malformed CP932 name was accepted");
    }catch(const std::runtime_error& error){
        Check(std::string(error.what()).find("expected complete CP932")!=std::string::npos,
              "a truncated-name cache hit cannot bypass strict decoding of padded names");
    }
    std::cout<<"vmd names: "<<assertions<<" assertions passed\n";
}
