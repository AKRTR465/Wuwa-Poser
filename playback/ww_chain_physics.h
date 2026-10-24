#pragma once
// Optional fixed-capacity post-FK particle chains. Body motion drives inertia;
// no clip-specific sine, target writes, allocation, or native-physics toggles.
// A root is pinned to completed body pose; free descendants use damped Verlet,
// shape spring, hard bone length, contact-softened cone and body capsules.
#include "../engine_ue4/pose_subtree_rotation.h"
#include "math/cloth_collision_math.h"
#include "../core_math/include/poser_math/quat_math.h"
#include <algorithm>

namespace wwp_playback { namespace ww_chain {
namespace sub=wwp_ue::subtree_rotation;namespace collision=eiem_collision;
constexpr uint32_t kLimit=1024,kCapsules=16;
using V=collision::V;
struct ParticleConfig {
    bool enabled=false,pinned=false;
    uint32_t anchor=UINT32_MAX;
    float damping=.12f,frequency=2.5f,cone_radians=.6f,radius_ue=.7f;
    uint16_t collision_exempt_mask=0; // authored rest inside capsule cannot be forced outside without changing rig.
};
struct CapsuleBinding {uint32_t a=UINT32_MAX,b=UINT32_MAX;float radius_ue=1;};
struct Prepared {
    uint32_t count=0,capsule_count=0;
    std::array<int32_t,kLimit> parents{};
    std::array<ParticleConfig,kLimit> particles{};
    std::array<CapsuleBinding,kCapsules> capsules{};
};
struct Control {bool enabled=false,playing=false,reset=false;uint64_t epoch=0;double clock_seconds=0,cursor_seconds=0;
    uint32_t iterations=12;float gravity_ue=-980,gravity_strength=.15f;
    // Contact takes priority over the heuristic authored cone when the cone is
    // infeasible. Bone length remains hard.
    bool collision_priority=true;float maximum_collision_cone_radians=1.5707963f;};
struct Runtime {
    std::array<V,kLimit> position{},previous{},target_previous{};
    std::array<sub::detail::Q,kLimit> target_rotation_previous{};
    bool ready=false,was_playing=false;
    uint64_t epoch=0;double clock=0,cursor=0;
};
struct Scratch {
    std::array<V,kLimit> targets{};
    std::array<V,kLimit> before_direction{},after_direction{};
    std::array<Quat,kLimit> correction{};
    std::array<double,kLimit> length{};
    std::array<sub::TransformBytes,kLimit> output{};
    std::array<collision::Capsule,kCapsules> capsules{};
};
inline V Position(const sub::TransformBytes& t){auto p=sub::detail::Position(t);return {p[0],p[1],p[2]};}
inline V Unit(V v,V fallback={1,0,0}){double n=collision::Length(v);return n>1e-12?v*(1/n):fallback;}
inline V Closest(V p,V a,V b){V d=b-a;const double n=collision::Dot(d,d);return a+d*(n>1e-12?std::clamp(collision::Dot(p-a,d)/n,0.,1.):0);}
inline void Reset(Runtime* r){if(r)*r=Runtime{};}
inline void Rebase(const Prepared& p,const Control& c,const sub::TransformBytes* pose,Runtime& r){
    for(uint32_t i=0;i<p.count;++i){r.position[i]=r.previous[i]=r.target_previous[i]=Position(pose[i]);
        r.target_rotation_previous[i]=sub::detail::Rotation(pose[i]);}
    r.ready=true;r.was_playing=c.playing;r.epoch=c.epoch;r.clock=c.clock_seconds;r.cursor=c.cursor_seconds;
}
inline V LimitDirection(V direction,V target,double angle){
    direction=Unit(direction,Unit(target));target=Unit(target);const double dot=std::clamp(collision::Dot(direction,target),-1.,1.);
    if(dot>=std::cos(angle))return direction;
    V tangent=direction-target*dot;if(collision::Length(tangent)<1e-12)tangent=collision::Cross(target,std::fabs(target.z)<.9?V{0,0,1}:V{0,1,0});
    return target*std::cos(angle)+Unit(tangent)*std::sin(angle);
}
inline V ProjectOutsideCapsuleOnSphere(V proposed,V origin,double length,const collision::Capsule& capsule,double radius,V preferred,double cone_radians){
    // Preserve the exact bone-length sphere while moving away from the body.
    // When a sphere/capsule intersection is feasible, find its tangent
    // direction by bounded search rather than push the endpoint off its sphere.
    V direction=Unit(proposed-origin,Unit(preferred));proposed=origin+direction*length;
    const double distance=collision::SegmentDistance(proposed,capsule.a,capsule.b);
    if(distance>=radius)return proposed;
    V nearest=Closest(proposed,capsule.a,capsule.b),normal=Unit(proposed-nearest,Unit(preferred));
    V tangent=normal-direction*collision::Dot(normal,direction);
    if(collision::Length(tangent)<1e-9){const V axis=Unit(capsule.b-capsule.a);tangent=collision::Cross(direction,axis);
        if(collision::Length(tangent)<1e-9)tangent=collision::Cross(direction,std::fabs(direction.z)<.9?V{0,0,1}:V{0,1,0});}
    tangent=Unit(tangent);
    V best=proposed;double best_distance=distance,winning=0,sign=1;
    for(int side=0;side<2;++side)for(unsigned step=1;step<=24;++step){const double theta=3.14159265358979323846*step/24;
        V candidate=origin+LimitDirection(direction*std::cos(theta)+tangent*((side?-1.:1.)*std::sin(theta)),preferred,cone_radians)*length;
        const double d=collision::SegmentDistance(candidate,capsule.a,capsule.b);
        if(d>best_distance){best=candidate;best_distance=d;}
        if(d>=radius){if(!winning||theta<winning){winning=theta;sign=side?-1.:1.;}break;}
    }
    if(winning){double low=0,high=winning;
        for(unsigned i=0;i<20;++i){const double theta=(low+high)*.5;V candidate=origin+LimitDirection(direction*std::cos(theta)+tangent*(sign*std::sin(theta)),preferred,cone_radians)*length;
            if(collision::SegmentDistance(candidate,capsule.a,capsule.b)>=radius)high=theta;else low=theta;}
        return origin+LimitDirection(direction*std::cos(high)+tangent*(sign*std::sin(high)),preferred,cone_radians)*length;
    }
    return best; // Complete cone search is shared across all contacts below.
}
inline uint16_t ContactMask(const Prepared& prepared,uint32_t bone){uint16_t mask=0;const auto& config=prepared.particles[bone];
    for(uint32_t k=0;k<prepared.capsule_count;++k){const auto& capsule=prepared.capsules[k];
        if((config.collision_exempt_mask&(1u<<k))||config.pinned||bone==capsule.a||bone==capsule.b||config.anchor==capsule.a||config.anchor==capsule.b)continue;
        mask|=uint16_t(1u<<k);}return mask;}
inline double ContactClearance(V proposed,const Scratch& scratch,uint16_t mask,double particle_radius){double clearance=(std::numeric_limits<double>::max)();
    for(uint32_t k=0;k<kCapsules;++k)if(mask&(1u<<k)){const auto& capsule=scratch.capsules[k];
        clearance=(std::min)(clearance,collision::SegmentDistance(proposed,capsule.a,capsule.b)-capsule.ra-particle_radius);}return clearance;}
inline V ProjectOutsideAllCapsulesOnSphere(V proposed,V origin,double length,V preferred,double cone_radians,
    const Scratch& scratch,uint16_t mask,double particle_radius){
    const double initial=ContactClearance(proposed,scratch,mask,particle_radius);if(initial>=-1e-7)return proposed;
    const V axis=Unit(preferred),x=Unit(collision::Cross(axis,std::fabs(axis.z)<.9?V{0,0,1}:V{0,1,0})),y=collision::Cross(axis,x);
    auto at=[&](double theta,double phi){return origin+(axis*std::cos(theta)+(x*std::cos(phi)+y*std::sin(phi))*std::sin(theta))*length;};
    double best_theta=0,best_phi=0;V best=at(0,0);double best_clearance=ContactClearance(best,scratch,mask,particle_radius);
    for(unsigned ring=1;ring<=12;++ring)for(unsigned longitude=0;longitude<64;++longitude){const double theta=cone_radians*ring/12,phi=6.283185307179586*longitude/64;
        const auto candidate=at(theta,phi);const double d=ContactClearance(candidate,scratch,mask,particle_radius);
        if(d>best_clearance){best=candidate;best_clearance=d;best_theta=theta;best_phi=phi;}}
    double search_step=(std::max)(cone_radians/12,6.283185307179586/64);
    for(unsigned refinement=0;refinement<12;++refinement){double next_theta=best_theta,next_phi=best_phi;
        for(int a=-1;a<=1;++a)for(int b=-1;b<=1;++b){if(!a&&!b)continue;
            const double theta=std::clamp(best_theta+a*search_step,0.,cone_radians),phi=best_phi+b*search_step;
            const auto candidate=at(theta,phi);const double d=ContactClearance(candidate,scratch,mask,particle_radius);
            if(d>best_clearance){best=candidate;best_clearance=d;next_theta=theta;next_phi=phi;}}
        best_theta=next_theta;best_phi=next_phi;search_step*=.5;}
    if(best_clearance<=initial)return proposed;if(best_clearance<0)return best;
    const V direction=Unit(proposed-origin),escape=Unit(best-origin);double low=0,high=1;
    for(unsigned i=0;i<20;++i){const double t=(low+high)*.5;const V candidate=origin+Unit(direction*(1-t)+escape*t)*length;
        if(ContactClearance(candidate,scratch,mask,particle_radius)>=0)high=t;else low=t;}
    return origin+Unit(direction*(1-high)+escape*high)*length;
}
inline bool Step(const Prepared& p,const Control& c,const sub::TransformBytes* pose,uint32_t count,
    Runtime* runtime,Scratch* scratch,sub::FixedPlan* out) noexcept {
    out->count=0;auto& r=*runtime;auto& s=*scratch;
    if(!c.enabled){Reset(runtime);return true;}
    for(uint32_t i=0;i<count;++i){s.targets[i]=Position(pose[i]);s.output[i]=pose[i];
        s.length[i]=i?collision::Length(s.targets[i]-s.targets[uint32_t(p.parents[i])]):0;}
    bool discontinuity=!r.ready||c.reset||r.epoch!=c.epoch||c.cursor_seconds<r.cursor-1e-5||c.clock_seconds<r.clock||c.clock_seconds-r.clock>.25;
    if(r.ready&&c.cursor_seconds-r.cursor>(std::max)(.25,4*(c.clock_seconds-r.clock)))discontinuity=true;
    if(r.ready)for(uint32_t i=0;i<count;++i)if(p.particles[i].pinned&&collision::Length(s.targets[i]-r.target_previous[i])>150)discontinuity=true;
    if(discontinuity){Rebase(p,c,pose,r);return true;}
    for(uint32_t k=0;k<p.capsule_count;++k){const auto& binding=p.capsules[k];s.capsules[k]={s.targets[binding.a],s.targets[binding.b],binding.radius_ue,binding.radius_ue,true};}
    const double dt=c.clock_seconds-r.clock;
    if(!c.playing||!r.was_playing||c.cursor_seconds==r.cursor||dt<1e-5){
        for(uint32_t i=0;i<count;++i)if(p.particles[i].enabled){const auto anchor=p.particles[i].anchor;
            const auto next_anchor_rotation=sub::detail::Rotation(pose[anchor]);
            const V next_anchor=s.targets[anchor],previous_anchor=r.target_previous[anchor];
            if(next_anchor.x==previous_anchor.x&&next_anchor.y==previous_anchor.y&&next_anchor.z==previous_anchor.z&&next_anchor_rotation==r.target_rotation_previous[anchor]){
                if(p.particles[i].pinned)r.position[i]=r.previous[i]=s.targets[i];continue;}
            const auto rotation=sub::detail::Unit(sub::detail::Mul(sub::detail::Rotation(pose[anchor]),sub::detail::Conj(r.target_rotation_previous[anchor])));
            const auto move=[&](V value){const auto offset=value-r.target_previous[anchor];
                const auto transformed=sub::detail::Rotate(rotation,{{offset.x,offset.y,offset.z}});
                return s.targets[anchor]+V{transformed[0],transformed[1],transformed[2]};};
            r.position[i]=move(r.position[i]);r.previous[i]=move(r.previous[i]);
            if(p.particles[i].pinned)r.position[i]=r.previous[i]=s.targets[i];}
    }else{
        const uint32_t substeps=(std::max)(1u,uint32_t(std::ceil(dt*120)));const double h=dt/substeps;
        for(uint32_t step=0;step<substeps;++step){
            for(uint32_t i=0;i<count;++i){const auto& config=p.particles[i];if(!config.enabled||config.pinned){r.position[i]=r.previous[i]=s.targets[i];continue;}
                const double damping=std::pow((std::max)(0.,1-double(config.damping)),h*60);
                const double omega=6.283185307179586*config.frequency;V acceleration=(s.targets[i]-r.position[i])*(omega*omega);
                acceleration.z+=c.gravity_ue*c.gravity_strength;const V next=r.position[i]+(r.position[i]-r.previous[i])*damping+acceleration*(h*h);
                r.previous[i]=r.position[i];r.position[i]=next;}
            for(uint32_t iteration=0;iteration<c.iterations;++iteration){
                bool pushed=false;double maximum_move=0;
                for(uint32_t i=1;i<count;++i){const auto& config=p.particles[i];if(!config.enabled||config.pinned)continue;
                    const V before=r.position[i];
                    const uint32_t parent=uint32_t(p.parents[i]);V origin=p.particles[parent].enabled?r.position[parent]:s.targets[parent];
                    const V target=s.targets[i]-s.targets[parent];const V direction=LimitDirection(r.position[i]-origin,target,config.cone_radians);
                    r.position[i]=origin+direction*s.length[i];
                    const uint16_t contact_mask=ContactMask(p,i);
                    for(uint32_t k=0;k<p.capsule_count;++k){const auto& capsule=s.capsules[k];
                        if(config.collision_exempt_mask&(1u<<k))continue;
                        if(i==p.capsules[k].a||i==p.capsules[k].b||config.anchor==p.capsules[k].a||config.anchor==p.capsules[k].b)continue;
                        const V nearest=Closest(r.position[i],capsule.a,capsule.b);const V separation=r.position[i]-nearest;
                        const double radius=capsule.ra+config.radius_ue,dist=collision::Length(separation);
                        if(dist<radius-1e-7){r.position[i]=ProjectOutsideCapsuleOnSphere(r.position[i],origin,s.length[i],capsule,radius,target,config.cone_radians);pushed=true;}}
                    if(ContactClearance(r.position[i],s,contact_mask,config.radius_ue)<-1e-7){
                        r.position[i]=ProjectOutsideAllCapsulesOnSphere(r.position[i],origin,s.length[i],target,config.cone_radians,s,contact_mask,config.radius_ue);pushed=true;}
                    if(c.collision_priority&&c.maximum_collision_cone_radians>config.cone_radians&&ContactClearance(r.position[i],s,contact_mask,config.radius_ue)<-1e-7){
                        r.position[i]=ProjectOutsideAllCapsulesOnSphere(r.position[i],origin,s.length[i],target,c.maximum_collision_cone_radians,s,contact_mask,config.radius_ue);pushed=true;}
                    maximum_move=(std::max)(maximum_move,collision::Length(r.position[i]-before));
                }
                // Parent-first length/cone constraints are already exact after
                // one pass without contact; do not repropose the same endpoint.
                if(!pushed||maximum_move<1e-7)break;
            }
        }
    }
    for(uint32_t i=0;i<count;++i)if(p.particles[i].enabled){
        if(!collision::Finite(r.position[i]))return false;
        for(unsigned k=0;k<3;++k){const double v=k==0?r.position[i].x:k==1?r.position[i].y:r.position[i].z;if(!sub::detail::Store(s.output[i].data()+16+k*4,v))return false;}
    }
    // Orient each simulated parent toward the centroid of its direct children.
    // Terminal orientation inherits its parent's directional correction.
    for(uint32_t i=0;i<count;++i){s.before_direction[i]=s.after_direction[i]=V{};s.correction[i]=Quat{};}
    for(uint32_t i=1;i<count;++i)if(p.particles[i].enabled&&p.particles[uint32_t(p.parents[i])].enabled){const auto parent=uint32_t(p.parents[i]);
        s.before_direction[parent]=s.before_direction[parent]+(s.targets[i]-s.targets[parent]);s.after_direction[parent]=s.after_direction[parent]+(r.position[i]-r.position[parent]);}
    for(uint32_t i=0;i<count;++i)if(p.particles[i].enabled){const auto before=s.before_direction[i],after=s.after_direction[i];
        Quat correction;if(collision::Length(before)>1e-6&&collision::Length(after)>1e-6)
            correction=Quat::FromTo({float(before.x),float(before.y),float(before.z)},{float(after.x),float(after.y),float(after.z)});
        else if(i&&p.particles[uint32_t(p.parents[i])].enabled)correction=s.correction[uint32_t(p.parents[i])];
        s.correction[i]=correction;
        // No directional change must preserve the exact input quaternion,
        // including accepted float rounding, on every held or paused frame.
        if(correction.x==0&&correction.y==0&&correction.z==0&&correction.w==1)continue;
        auto q=sub::detail::Rotation(pose[i]);const auto rotated=sub::detail::Unit(sub::detail::Mul({{correction.x,correction.y,correction.z,correction.w}},q));
        for(unsigned k=0;k<4;++k)if(!sub::detail::Store(s.output[i].data()+k*4,rotated[k]))return false;
    }
    r.clock=c.clock_seconds;r.cursor=c.cursor_seconds;r.epoch=c.epoch;r.was_playing=c.playing;for(uint32_t i=0;i<count;++i){r.target_previous[i]=s.targets[i];r.target_rotation_previous[i]=sub::detail::Rotation(pose[i]);}
    out->count=0;
    for(uint32_t i=0;i<count;++i)if(p.particles[i].enabled){sub::Patch patch;patch.bone=i;
        std::memcpy(patch.original_rotation.data(),pose[i].data(),16);std::memcpy(patch.rotated_rotation.data(),s.output[i].data(),16);
        std::memcpy(patch.original_position.data(),pose[i].data()+16,12);std::memcpy(patch.rotated_position.data(),s.output[i].data()+16,12);
        patch.position_changed=patch.original_position!=patch.rotated_position;if(patch.original_rotation!=patch.rotated_rotation||patch.position_changed)out->patches[out->count++]=patch;}
    return true;
}
}} // namespace wwp_playback::ww_chain
