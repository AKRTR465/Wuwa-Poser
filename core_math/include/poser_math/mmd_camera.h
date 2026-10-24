#pragma once
#include "mmd_motion.h"

namespace mmd {
inline CameraKey SampleCamera(const std::vector<CameraKey> &keys, double frame,
                              bool cuts = true) {
  if (keys.empty()) return {};
  const auto n = Upper(keys, frame);
  if (!n) return keys.front();
  if (n == keys.size()) return keys.back();
  const auto &a = keys[n - 1], &b = keys[n];
  // Adjacent MMD keys commonly mark a cut; keep it crisp at higher game FPS.
  if (cuts && b.frame - a.frame == 1) return a;
  float t = float((frame - a.frame) / double(b.frame - a.frame));
  auto mix = [&](float x, float y, int curve) { return x + (y - x) * Bezier(b.curves[curve], t); };
  CameraKey out = a;
  out.target = {mix(a.target.x,b.target.x,0),mix(a.target.y,b.target.y,1),mix(a.target.z,b.target.z,2)};
  // Preserve authored Euler turns rather than taking a quaternion shortcut.
  out.rotation = {mix(a.rotation.x,b.rotation.x,3),mix(a.rotation.y,b.rotation.y,3),mix(a.rotation.z,b.rotation.z,3)};
  out.distance = mix(a.distance,b.distance,4);
  out.fov = mix(a.fov,b.fov,5);
  return out; // Projection is a step track.
}
inline Quat CameraOrbit(Vec3 e) {
  // MMD camera orbit: negative Y, then negative X, then negative Z.
  return NormQ(Quat::AxisAngle({0,1,0},-e.y) *
               Quat::AxisAngle({1,0,0},-e.x) * Quat::AxisAngle({0,0,1},-e.z));
}
} // namespace mmd
