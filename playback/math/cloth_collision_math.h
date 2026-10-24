#pragma once
#include <algorithm>
#include <cmath>

namespace eiem_collision {
struct V {
  double x = 0, y = 0, z = 0;
};
inline V operator+(V a, V b) { return {a.x + b.x, a.y + b.y, a.z + b.z}; }
inline V operator-(V a, V b) { return {a.x - b.x, a.y - b.y, a.z - b.z}; }
inline V operator*(V a, double b) { return {a.x * b, a.y * b, a.z * b}; }
inline double Dot(V a, V b) { return a.x * b.x + a.y * b.y + a.z * b.z; }
inline V Cross(V a, V b) {
  return {a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x};
}
inline double Length(V a) { return std::sqrt(Dot(a, a)); }
inline bool Finite(V a) { return std::isfinite(a.x) && std::isfinite(a.y) && std::isfinite(a.z); }
struct Capsule {
  V a, b;
  double ra = 0, rb = 0;
  bool valid = false;
};
inline double SegmentDistance(V p, V a, V b) {
  V d = b - a;
  const double dd = Dot(d, d);
  double t = dd > 1e-12 ? std::clamp(Dot(p - a, d) / dd, 0.0, 1.0) : 0;
  return Length(p - (a + d * t));
}
}
