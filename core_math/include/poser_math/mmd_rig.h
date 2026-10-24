#pragma once
#include "ik_two_bone.h"
#include "mmd_motion.h"
#include <numeric>
#include <set>

namespace mmd {
struct IkLink {
  int bone = -1;
  bool limited = false;
  Vec3 minimum, maximum;
};
struct RigBone {
  std::string name;
  Vec3 rest;
  int parent = -1;
  int grant = -1;
  float grantWeight = 0;
  bool grantRotation = false, grantLocal = false;
  int effector = -1, iterations = 0;
  float angleLimit = 0;
  std::vector<IkLink> links;
};
struct RigDefinition {
  std::vector<RigBone> bones;
  std::vector<int> order;
  std::map<std::string, int> names;
  int find(const std::string &name) const {
    auto i = names.find(Name(name));
    return i == names.end() ? -1 : i->second;
  }
  void finish() {
    std::vector<uint8_t> mark(bones.size());
    std::function<void(int)> visit = [&](int i) {
      if (i < 0 || mark[i])
        return;
      mark[i] = 1;
      visit(bones[i].parent);
      visit(bones[i].grant);
      order.push_back(i);
    };
    for (size_t i = 0; i < bones.size(); i++)
      visit(int(i));
  }
};
// Same numeric roles as Unity HumanBodyBones; independent of game headers.
inline const std::array<const char *, 55> &RoleNames() {
  static const std::array<const char *, 55> n = {
      {u8"下半身",   u8"左足",    u8"右足",    u8"左ひざ",  u8"右ひざ",
       u8"左足首",   u8"右足首",  u8"上半身",  u8"上半身2", u8"首",
       u8"頭",       u8"左肩",    u8"右肩",    u8"左腕",    u8"右腕",
       u8"左ひじ",   u8"右ひじ",  u8"左手首",  u8"右手首",  u8"左つま先",
       u8"右つま先", u8"左目",    u8"右目",    u8"あご",    u8"左親指0",
       u8"左親指1",  u8"左親指2", u8"左人指1", u8"左人指2", u8"左人指3",
       u8"左中指1",  u8"左中指2", u8"左中指3", u8"左薬指1", u8"左薬指2",
       u8"左薬指3",  u8"左小指1", u8"左小指2", u8"左小指3", u8"右親指0",
       u8"右親指1",  u8"右親指2", u8"右人指1", u8"右人指2", u8"右人指3",
       u8"右中指1",  u8"右中指2", u8"右中指3", u8"右薬指1", u8"右薬指2",
       u8"右薬指3",  u8"右小指1", u8"右小指2", u8"右小指3", u8"上半身3"}};
  return n;
}
// The source rest pose is selected manually; a VMD contains no rest skeleton.
inline RigDefinition StandardRig() {
  RigDefinition r;
  auto add = [&](std::string name, std::string parent, Vec3 p) {
    RigBone b;
    b.name = Name(name);
    b.parent = parent.empty() ? -1 : r.find(parent);
    b.rest = p;
    int i = int(r.bones.size());
    r.names[b.name] = i;
    r.bones.push_back(b);
    return i;
  };
  add(u8"全ての親", "", {0, 0, 0});
  add(u8"センター", u8"全ての親", {0, 10, 0});
  add(u8"グルーブ", u8"センター", {0, 10, 0});
  add(u8"腰", u8"グルーブ", {0, 10, 0});
  add(u8"下半身", u8"腰", {0, 10, 0});
  add(u8"上半身", u8"腰", {0, 11.3f, 0});
  add(u8"上半身2", u8"上半身", {0, 13, 0});
  add(u8"上半身3", u8"上半身2", {0, 14, 0});
  add(u8"首", u8"上半身3", {0, 15.6f, 0});
  add(u8"頭", u8"首", {0, 16.6f, 0});
  add(u8"両目", u8"頭", {0, 17.1f, -0.5f});
  for (int side = 0; side < 2; side++) {
    std::string s = side ? u8"右" : u8"左";
    float x = side ? -1.f : 1.f;
    add(s + u8"目", u8"両目", {x * .35f, 17.1f, -.6f});
    add(s + u8"肩P", u8"上半身3", {x * .7f, 14.9f, 0});
    add(s + u8"肩", s + u8"肩P", {x * .9f, 14.9f, 0});
    add(s + u8"肩C", s + u8"肩", {x * 1.7f, 14.8f, 0});
    auto &cancel = r.bones.back();
    cancel.grant = r.find(s + u8"肩P");
    cancel.grantWeight = -1;
    cancel.grantRotation = true;
    cancel.grantLocal = true;
    add(s + u8"腕", s + u8"肩C", {x * 1.7f, 14.8f, 0});
    add(s + u8"腕捩", s + u8"腕", {x * 2.5f, 14, 0});
    add(s + u8"ひじ", s + u8"腕捩", {x * 3.7f, 12.8f, 0});
    add(s + u8"手捩", s + u8"ひじ", {x * 4.6f, 11.9f, 0});
    add(s + u8"手首", s + u8"手捩", {x * 5.6f, 10.9f, 0});
    const char *fingers[] = {u8"親指", u8"人指", u8"中指", u8"薬指", u8"小指"};
    for (int f = 0; f < 5; f++) {
      std::string parent = s + u8"手首";
      for (int j = 0; j < 3; j++) {
        std::string name = s + fingers[f] + std::to_string(f == 0 ? j : j + 1);
        add(name, parent,
            {x * (5.8f + j * .3f), 10.7f - j * .3f, (f - 2) * .17f});
        parent = name;
      }
    }
    add(s + u8"足", u8"下半身", {x * .85f, 9.5f, 0});
    add(s + u8"ひざ", s + u8"足", {x * .85f, 5, -.15f});
    add(s + u8"足首", s + u8"ひざ", {x * .85f, 1, 0});
    add(s + u8"つま先", s + u8"足首", {x * .85f, .25f, -1.2f});
    add(s + u8"足IK親", u8"全ての親", {x * .85f, 1, 0});
    int foot = add(s + u8"足IK", s + u8"足IK親", {x * .85f, 1, 0});
    r.bones[foot].effector = r.find(s + u8"足首");
    r.bones[foot].iterations = 64;
    r.bones[foot].angleLimit = .6f;
    r.bones[foot].links = {
        {r.find(s + u8"ひざ"), true, {-3.13f, 0, 0}, {-.001f, 0, 0}},
        {r.find(s + u8"足"), false, {}, {}}};
    int toe = add(s + u8"つま先IK", s + u8"足IK", {x * .85f, .25f, -1.2f});
    r.bones[toe].effector = r.find(s + u8"つま先");
    r.bones[toe].iterations = 8;
    r.bones[toe].angleLimit = .5f;
    r.bones[toe].links = {{r.find(s + u8"足首"), false, {}, {}}};
  }
  // A thumb is an oblique, out-of-plane chain, not another parallel finger.
  // Approximate an open hand in palm-length units: forward, index-finger side,
  // and palm normal.
  const Vec3 thumb[] = {{.14f,.20f,-.16f}, {.47f,.40f,-.30f}, {.80f,.61f,-.44f}};
  for (int side=0;side<2;++side) {
    const std::string s=side?u8"右":u8"左";
    const Vec3 wrist=r.bones[r.find(s+u8"手首")].rest;
    const Vec3 palm=r.bones[r.find(s+u8"中指1")].rest-wrist;
    const float length=Len(palm);
    const Vec3 forward=Norm(palm);
    Vec3 radial=r.bones[r.find(s+u8"人指1")].rest-r.bones[r.find(s+u8"小指1")].rest;
    radial=Norm(radial-forward*Dot(radial,forward));
    const Vec3 normal=Cross(forward,radial)*(side?-1.f:1.f);
    for (int joint=0;joint<3;++joint) {
      const auto p=thumb[joint];
      r.bones[r.find(s+u8"親指"+std::to_string(joint))].rest=
          wrist+(forward*p.x+radial*p.y+normal*p.z)*length;
    }
  }
  r.finish();
  return r;
}
inline Vec3 EulerXYZ(Quat q) {
  return {
      std::atan2(2 * (q.w * q.x + q.y * q.z), 1 - 2 * (q.x * q.x + q.y * q.y)),
      std::asin(Clamp(2 * (q.w * q.y - q.z * q.x), -1, 1)),
      std::atan2(2 * (q.w * q.z + q.x * q.y), 1 - 2 * (q.y * q.y + q.z * q.z))};
}
inline Quat FromXYZ(Vec3 v) {
  return NormQ(Quat::AxisAngle({0, 0, 1}, v.z) *
               Quat::AxisAngle({0, 1, 0}, v.y) *
               Quat::AxisAngle({1, 0, 0}, v.x));
}
struct RigPose {
  std::vector<Vec3> positions;
  std::vector<Quat> rotations, localRot;
};
class RigEvaluator {
  const RigDefinition *rig_ = nullptr;
  const MotionClip *clip_ = nullptr;
  std::vector<const std::vector<BoneKey> *> tracks_;
  std::vector<std::string> trackNames_;
  std::vector<LocalPose> anim_;
  std::vector<Quat> ik_;
  std::vector<Quat> appendRot_;
  std::vector<int> controllers_;
  std::vector<bool> needed_;
  std::vector<uint8_t> worldDirty_;
  void world(int changed = -1) {
    for (int i : rig_->order) {
      const auto &b = rig_->bones[i];
      // finish() orders both parent and append dependencies before this bone.
      // Append consumers may be outside the edited bone's parenting subtree.
      worldDirty_[i] = changed < 0 || i == changed ||
                      (b.parent >= 0 && worldDirty_[b.parent]) ||
                      (b.grant >= 0 && worldDirty_[b.grant]);
      if (!worldDirty_[i]) continue;
      Quat q = anim_[i].rotation;
      Vec3 p = anim_[i].position;
      appendRot_[i] = {};
      if (b.grant >= 0) {
        const auto &source = rig_->bones[b.grant];
        Quat g = (b.grantLocal || source.grant < 0) ? anim_[b.grant].rotation
                                                    : appendRot_[b.grant];
        g = NormQ(ik_[b.grant] * g);
        if (b.grantRotation) {
          float w = b.grantWeight;
          if (w < 0)
            g = Conj(g);
          appendRot_[i] = Quat::Slerp({}, g, std::fabs(w));
          q = NormQ(q * appendRot_[i]);
        }
      }
      q = NormQ(ik_[i] * q);
      pose.localRot[i] = q;
      if (b.parent >= 0) {
        pose.positions[i] = pose.positions[b.parent] +
                            pose.rotations[b.parent] *
                                (b.rest - rig_->bones[b.parent].rest + p);
        pose.rotations[i] = NormQ(pose.rotations[b.parent] * q);
      } else {
        pose.positions[i] = b.rest + p;
        pose.rotations[i] = q;
      }
    }
  }
  void setWorld(int i, Quat q) {
    int p = rig_->bones[i].parent;
    Quat parent = p >= 0 ? pose.rotations[p] : Quat{};
    Quat local = NormQ(Conj(parent) * q);
    Quat base = NormQ(Conj(ik_[i]) * pose.localRot[i]);
    ik_[i] = NormQ(local * Conj(base));
    world(i);
  }

public:
  RigPose pose;
  void bind(const RigDefinition &rig, const MotionClip &clip,
            const std::vector<int> &outputs) {
    rig_ = &rig;
    clip_ = &clip;
    tracks_.assign(rig.bones.size(), nullptr);
    trackNames_.resize(rig.bones.size());
    anim_.resize(rig.bones.size());
    ik_.resize(rig.bones.size());
    appendRot_.resize(rig.bones.size());
    pose.positions.resize(rig.bones.size());
    pose.rotations.resize(rig.bones.size());
    pose.localRot.resize(rig.bones.size());
    needed_.assign(rig.bones.size(), false);
    worldDirty_.resize(rig.bones.size());
    for (size_t i = 0; i < rig.bones.size(); ++i) {
      trackNames_[i] = rig.bones[i].name;
      auto track = clip.bones.find(trackNames_[i]);
      if (track != clip.bones.end()) tracks_[i] = &track->second;
    }
    std::function<void(int)> need = [&](int i) {
      if (i < 0 || needed_[i])
        return;
      needed_[i] = true;
      need(rig.bones[i].parent);
      need(rig.bones[i].grant);
    };
    for (int i : outputs) need(i);
    controllers_.clear();
    for (size_t i = 0; i < rig.bones.size(); ++i) {
      const auto &b = rig.bones[i];
      bool relevant = false;
      for (const auto &l : b.links)
        relevant = relevant || needed_[l.bone];
      if (b.effector >= 0 && relevant) {
        controllers_.push_back(int(i));
        need(int(i));
      }
    }
  }
  bool hasTrack(int bone) const { return bone >= 0 && size_t(bone) < tracks_.size() && tracks_[bone]; }
  bool ikEnabled(int controller, double frame) const {
    if (std::find(controllers_.begin(), controllers_.end(), controller) == controllers_.end())
      return false;
    // FK-only clips keep IK off; the built-in rig's IK targets need a track.
    return SampleIk(*clip_, trackNames_[controller], frame) && tracks_[controller];
  }
  void sample(double frame) {
    for (size_t i = 0; i < anim_.size(); i++) {
      anim_[i] = tracks_[i] ? SampleBone(*tracks_[i], frame) : LocalPose{};
      ik_[i] = {};
    }
    world();
    // PMX deformation order is relevant when IK chains share links.
    for (int c : controllers_) {
      const auto &controller = rig_->bones[c];
      if (!ikEnabled(c, frame))
        continue;
      const Vec3 target = pose.positions[c];
      if (controller.name == u8"左足IK" || controller.name == u8"右足IK") {
        int a = controller.links[1].bone, b = controller.links[0].bone,
            e = controller.effector;
        Vec3 pa = pose.positions[a], pb = pose.positions[b],
             pc = pose.positions[e];
        int parent = rig_->bones[a].parent;
        Vec3 pole = pa + (parent >= 0 ? pose.rotations[parent] : Quat{}) *
                             Vec3{0, 0, -10};
        Vec3 sa = pa, sb = pb, sc = pc;
        SolveTwoBone(sa, sb, sc, target, pole);
        setWorld(a, NormQ(Quat::FromTo(pb - pa, sb - sa) * pose.rotations[a]));
        setWorld(b, NormQ(Quat::FromTo(pose.positions[e] - pose.positions[b],
                                       sc - sb) *
                          pose.rotations[b]));
        continue;
      }
      for (int iter = 0; iter < controller.iterations; iter++) {
        if (Len(pose.positions[controller.effector] - target) < 1e-4f)
          break;
        for (const auto &link : controller.links) {
          int i = link.bone;
          Vec3 a = pose.positions[controller.effector] - pose.positions[i],
               b = target - pose.positions[i];
          if (Len(a) < 1e-6f || Len(b) < 1e-6f)
            continue;
          Quat d = Quat::FromTo(a, b);
          float angle = Quat::Angle({}, d);
          if (angle > controller.angleLimit && angle > 1e-6f)
            d = Quat::Slerp({}, d, controller.angleLimit / angle);
          int parent = rig_->bones[i].parent;
          Quat pr = parent >= 0 ? pose.rotations[parent] : Quat{};
          Quat local = NormQ(Conj(pr) * d * pose.rotations[i]);
          if (link.limited) {
            Vec3 e = EulerXYZ(local);
            e = {Clamp(e.x, link.minimum.x, link.maximum.x),
                 Clamp(e.y, link.minimum.y, link.maximum.y),
                 Clamp(e.z, link.minimum.z, link.maximum.z)};
            local = FromXYZ(e);
          }
          Quat base = NormQ(Conj(ik_[i]) * pose.localRot[i]);
          ik_[i] = NormQ(local * Conj(base));
          world(i);
        }
      }
    }
  }
};
} // namespace mmd
