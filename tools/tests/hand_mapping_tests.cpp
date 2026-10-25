#include "../../playback/ww_retarget.h"
#include <iostream>
#include <stdexcept>
namespace rt=wwp_playback::ww_retarget;
static unsigned checks=0;
static void Check(bool v,const char* message){++checks;if(!v)throw std::runtime_error(message);}
static mmd::RetargetProfile Hand(bool metacarpals,bool tip=true){
    mmd::RetargetProfile p;
    auto add=[&](std::string name,int parent,Vec3 local,int role){mmd::TargetBone b;b.name=name;b.parent=parent;b.localPos=local;b.role=role;p.bones.push_back(b);return int(p.bones.size()-1);};
    for(int side=0;side<2;++side){const auto stem=std::string("Bip001")+(side?"R":"L");
        int hand=add(stem+"Hand",-1,{float(side)*20,0,0},17+side);
        for(int f=0;f<5;++f){const auto name=stem+"Finger"+std::to_string(f);int parent=hand;
            for(int j=0;j<(tip?4:3);++j){const int role=j<3?24+side*15+f*3+j:-1;
                const Vec3 pos=j==0?Vec3{metacarpals?.15f:5.f,0,float(f-2)*.2f}:Vec3{j==1&&metacarpals?5.f:2.f,0,0};
                parent=add(name+(j?std::to_string(j):""),parent,pos,role);}
        }
    }p.globals();return p;
}
int main(){try{
    auto palm=Hand(true);Check(rt::MapMetacarpalFingers(palm)==8,"both hands preserve four non-thumb metacarpals");
    for(int side=0;side<2;++side)for(int f=1;f<5;++f){
        const int first=24+side*15+f*3;
        for(int j=0;j<3;++j)Check(palm.bones[palm.roles[first+j]].name==std::string("Bip001")+(side?"R":"L")+"Finger"+std::to_string(f)+std::to_string(j+1),"MMD phalanges map beyond the palm link");
        const int parent=palm.bones[palm.roles[first]].parent;Check(palm.bones[parent].role==-1,"the palm link is not driven as a finger joint");
    }
    for(int side=0;side<2;++side)Check(palm.bones[palm.roles[24+side*15]].name==std::string("Bip001")+(side?"R":"L")+"Finger0","thumb metacarpal retains its independent semantic role");
    Check(rt::MapMetacarpalFingers(palm)==8,"layout detection remains stable when called again");
    auto normal=Hand(false);Check(rt::MapMetacarpalFingers(normal)==0,"an ordinary finger plus tip marker must not shift roles");
    auto three=Hand(true,false);Check(rt::MapMetacarpalFingers(three)==0,"three-link fingers retain their complete mapping");
    auto broken=Hand(true);broken.bones[broken.roles[28]].parent=broken.roles[17];broken.globals();
    Check(rt::MapMetacarpalFingers(broken)==7,"a broken serial chain is not guessed from names");
    std::cout<<"hand mapping: "<<checks<<" checks passed\n";return 0;
}catch(const std::exception& e){std::cerr<<e.what()<<" (check "<<checks<<")\n";return 1;}}
