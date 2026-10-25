#include "../../core_math/include/poser_math/mmd_retarget.h"
#include <iostream>
#include <stdexcept>

static unsigned checks=0;
static void Check(bool value,const char* message){++checks;if(!value)throw std::runtime_error(message);}
static mmd::RetargetProfile Fixture(Quat basis,float lean,float pitch,float roll){
    const auto source=mmd::StandardRig();mmd::RetargetProfile target;
    for(size_t i=0;i<source.bones.size();++i){
        const auto& b=source.bones[i];mmd::TargetBone t;t.name=b.name;t.parent=b.parent;
        t.localPos=b.parent<0?b.rest:b.rest-source.bones[b.parent].rest;
        for(int role=0;role<55;++role)if(mmd::Name(mmd::RoleNames()[role])==b.name)t.role=role;
        if(i==0)t.localRot=NormQ(basis*Quat::AxisAngle({1,0,0},lean));
        target.bones.push_back(t);
    }
    target.globals();
    // The body reference can lean while both shoes are independently level.
    for(int side=0;side<2;++side){
        const int foot=target.roles[5+side],parent=target.bones[foot].parent;
        const auto neutral=NormQ(basis*Quat::AxisAngle({0,1,0},side?-.3f:.3f)*
            Quat::AxisAngle({1,0,0},pitch)*Quat::AxisAngle({0,0,1},side?-roll:roll));
        target.bones[foot].localRot=NormQ(Conj(target.bones[parent].restRot)*neutral);target.globals();
    }
    return target;
}
static void PreserveFoot(Quat basis,float lean,float pitch,float roll){
    auto target=Fixture(basis,lean,pitch,roll);const Vec3 up=basis*Vec3{0,1,0};
    std::array<Quat,2> before{target.bones[target.roles[5]].restRot,target.bones[target.roles[6]].restRot};
    Check(mmd::MakeCalibrationTPose(target,up),"complete calibration must succeed");
    const Vec3 axes[]={{1,0,0},{0,1,0},{0,0,1}};
    for(int side=0;side<2;++side){
        const auto after=target.bones[target.roles[5+side]].restRot;
        for(const auto axis:axes)Check(std::fabs(Dot(before[side]*axis,up)-Dot(after*axis,up))<2e-4f,
            "calibration must preserve the original shoe pitch and roll, allowing only yaw");
        const auto a=target.bones[target.roles[19+side]].restPos-target.bones[target.roles[5+side]].restPos;
        const Vec3 across=target.bones[target.roles[13]].restPos-target.bones[target.roles[14]].restPos;
        const auto forward=Norm(Cross(up,Norm(across-up*Dot(across,up))));
        Check(Dot(Norm(a-up*Dot(a,up)),forward)>.9999f,"shoe yaw must face the calibrated body forward");
    }
    auto second=target;
    Check(mmd::MakeCalibrationTPose(second,up),"a calibrated pose must remain calibratable");
    for(int side=0;side<2;++side)Check(Quat::Angle(target.bones[target.roles[5+side]].restRot,
        second.bones[second.roles[5+side]].restRot)<.002f,"recalibration must not accumulate foot tilt");
    // The fix preserves a neutral frame; it must still transfer authored pitch.
    auto rig=mmd::StandardRig();mmd::MotionClip clip;
    mmd::BoneKey center;center.frame=0;clip.bones[mmd::Name(u8"センター")].push_back(center);
    for(int side=0;side<2;++side){mmd::BoneKey key;key.frame=0;key.rotation=Quat::AxisAngle({1,0,0},.25f);
        clip.bones[mmd::Name(std::string(side?u8"右":u8"左")+u8"足首")].push_back(key);}
    mmd::Retargeter retarget;retarget.bind(rig,clip,target);retarget.sample(0,1);
    for(int side=0;side<2;++side)Check(Quat::Angle(target.bones[target.roles[5+side]].restRot,
        retarget.output.worldRot[target.roles[5+side]])>.2f,"authored foot rotation must remain active");
}
int main(){try{
    for(const auto basis:{Quat{},Quat::AxisAngle({1,0,0},1.57079632679f)})
        for(float lean:{-.35f,0.f,.35f})
            for(const auto pitchRoll:{Vec3{},Vec3{.12f,-.08f,0}})
                PreserveFoot(basis,lean,pitchRoll.x,pitchRoll.y);
    auto incomplete=Fixture({},.3f,0,0);incomplete.bones[incomplete.roles[5]].role=-1;incomplete.globals();
    Check(!mmd::MakeCalibrationTPose(incomplete),"incomplete rigs must remain rejected");
    std::cout<<"foot calibration: "<<checks<<" checks passed\n";return 0;
}catch(const std::exception& e){std::cerr<<"foot calibration failed: "<<e.what()<<" (check "<<checks<<")\n";return 1;}}
