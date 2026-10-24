#pragma once
// VMD reader and time-domain sampling. No game or Windows dependencies.
#include "quat_math.h"
#include <algorithm>
#include <array>
#include <cstdint>
#include <cstring>
#include <functional>
#include <istream>
#include <map>
#include <stdexcept>
#include <string>
#include <unordered_map>
#include <vector>

namespace mmd {
using Decoder = std::function<std::string(const std::string &, unsigned)>;
inline float Clamp(float v, float a, float b) {
  return (std::max)(a, (std::min)(b, v));
}
struct Reader {
  size_t length = 0;
  std::istream *stream = nullptr;
  std::vector<uint8_t> buffer;
  size_t cursor = 0, buffered = 0;
  size_t offset = 0;
  Reader(std::istream &s, size_t size) : length(size), stream(&s), buffer(64 * 1024) {}
  size_t remaining() const { return length - offset; }
  void require(size_t n) const {
    if (n > remaining())
      throw std::runtime_error("Truncated MMD file");
  }
  void copy(void *destination, size_t n) {
    require(n);
    auto out = static_cast<uint8_t *>(destination);
    while (n) {
      if (cursor == buffered) {
        buffered = (std::min)(remaining(), buffer.size());
        if (!stream->read(reinterpret_cast<char *>(buffer.data()), buffered))
          throw std::runtime_error("Cannot read MMD file (truncated or changed while loading)");
        cursor = 0;
      }
      auto take = (std::min)(n, buffered - cursor);
      if (out) { std::memcpy(out, buffer.data() + cursor, take); out += take; }
      cursor += take; offset += take; n -= take;
    }
  }
  void skip(size_t n) { copy(nullptr, n); }
  template <class T> T read() {
    T v;
    copy(&v, sizeof(T));
    return v;
  }
  uint32_t count(size_t minSize) {
    uint32_t n = read<uint32_t>();
    if (minSize && n > remaining() / minSize)
      throw std::runtime_error("Invalid MMD record count");
    return n;
  }
  float number() {
    float f = read<float>();
    if (!std::isfinite(f))
      throw std::runtime_error("Non-finite MMD value");
    return f;
  }
  Vec3 vec() {
    float x = number(), y = number(), z = number();
    if (std::fabs(x) > 1e6f || std::fabs(y) > 1e6f || std::fabs(z) > 1e6f)
      throw std::runtime_error("MMD vector exceeds supported range");
    return {x, y, z};
  }
  Quat quat() {
    float x = number(), y = number(), z = number(), w = number();
    double quaternionLength = std::sqrt(double(x) * x + double(y) * y + double(z) * z +
                              double(w) * w);
    if (quaternionLength < 1e-12)
      return {};
    return {float(x / quaternionLength), float(y / quaternionLength), float(z / quaternionLength),
            float(w / quaternionLength)};
  }
  std::string raw(size_t n) {
    require(n);
    std::string s(n, '\0');
    copy(s.data(), n);
    return s;
  }
};
// Normalize common ASCII/full-width variants without changing Japanese names.
inline std::string Name(std::string s) {
  const char *from[] = {u8"０", u8"１", u8"２", u8"３", u8"４", u8"５",
                        u8"６", u8"７", u8"８", u8"９", u8"Ｉ", u8"Ｋ"};
  const char *to[] = {"0", "1", "2", "3", "4", "5",
                      "6", "7", "8", "9", "I", "K"};
  for (size_t i = 0; i < 12; i++) {
    size_t p = 0;
    while ((p = s.find(from[i], p)) != s.npos) {
      s.replace(p, std::strlen(from[i]), to[i]);
      p++;
    }
  }
  return s;
}
struct Curve {
  float x1 = 0, y1 = 0, x2 = 1, y2 = 1;
};
inline float Bezier(const Curve &c, float x) {
  x = Clamp(x, 0, 1);
  if (x == 0 || x == 1)
    return x;
  auto b = [](float t, float a, float z) {
    float u = 1 - t;
    return 3 * u * u * t * a + 3 * u * t * t * z + t * t * t;
  };
  float lo = 0, hi = 1, t = x;
  for (int i = 0; i < 22; i++) {
    float bx = b(t, c.x1, c.x2);
    if (std::fabs(bx - x) < 1e-6f)
      break;
    if (bx < x)
      lo = t;
    else
      hi = t;
    t = (lo + hi) * 0.5f;
  }
  return b(t, c.y1, c.y2);
}
struct BoneKey {
  uint32_t frame = 0;
  Vec3 position;
  Quat rotation;
  std::array<Curve, 4> curves;
};
struct MorphKey {
  uint32_t frame = 0;
  float weight = 0;
};
struct IkKey {
  uint32_t frame = 0;
  bool enabled = true;
};
struct CameraKey {
  uint32_t frame = 0;
  float distance = -45, fov = 30;
  Vec3 target, rotation; // VMD Euler radians, not a bone quaternion
  std::array<Curve, 6> curves; // target XYZ, rotation, distance, FOV
  bool perspective = true;
};
struct LocalPose {
  Vec3 position;
  Quat rotation;
};
struct MotionClip {
  std::map<std::string, std::vector<BoneKey>> bones;
  std::map<std::string, std::vector<MorphKey>> morphs;
  std::map<std::string, std::vector<IkKey>> ik;
  std::vector<CameraKey> cameras;
  uint32_t lastFrame = 0;
  double duration() const { return lastFrame / 30.0; }
};
template <class T> inline void SortKeys(std::vector<T> &v) {
  auto less = [](const T &a, const T &b) { return a.frame < b.frame; };
  if (!std::is_sorted(v.begin(), v.end(), less))
    std::stable_sort(v.begin(), v.end(), less);
  size_t n = 0;
  for (const auto &k : v) {
    if (n && v[n - 1].frame == k.frame)
      v[n - 1] = k;
    else
      v[n++] = k;
  }
  v.resize(n);
}
// Light/shadow and visibility keys are not sampled but still extend the
// timeline; ReadVmd records their frames in lastFrame before sorting.
inline void Recount(MotionClip &c) {
  for (auto &kv : c.bones) {
    SortKeys(kv.second);
    if (!kv.second.empty())
      c.lastFrame = (std::max)(c.lastFrame, kv.second.back().frame);
  }
  for (auto &kv : c.morphs) {
    SortKeys(kv.second);
    if (!kv.second.empty())
      c.lastFrame = (std::max)(c.lastFrame, kv.second.back().frame);
  }
  for (auto &kv : c.ik)
    SortKeys(kv.second);
  SortKeys(c.cameras);
  if (!c.cameras.empty())
    c.lastFrame = (std::max)(c.lastFrame, c.cameras.back().frame);
}
// Stream large VMDs so the raw file is never retained alongside all decoded
// tracks. Do not thin keys or discard unknown bones on import.
inline MotionClip ReadVmd(Reader &r, const Decoder &decode) {
  MotionClip c;
  // Dense, baked motions repeat a few hundred CP932 names millions of times.
  std::unordered_map<std::string, std::string> names;
  auto trackName = [&](size_t width) -> const std::string & {
    auto raw = r.raw(width);auto zero = raw.find('\0');
    if (zero != raw.npos) raw.resize(zero);
    auto it = names.find(raw);
    if (it != names.end()) return it->second;
    auto name = Name(decode(raw, 932));
    return names.emplace(std::move(raw), std::move(name)).first->second;
  };
  auto signature = r.raw(30);
  if (signature.compare(0, 25, "Vocaloid Motion Data 0002") != 0)
    throw std::runtime_error("Not a VMD 0002 motion");
  r.skip(20);
  auto n = r.count(111);
  for (uint32_t i = 0; i < n; i++) {
    const auto &name = trackName(15);
    BoneKey k;
    k.frame = r.read<uint32_t>();
    k.position = r.vec();
    k.rotation = r.quat();
    uint8_t curve[64];r.copy(curve, sizeof(curve));
    for (int j = 0; j < 4; j++)
      k.curves[j] = {Clamp(uint8_t(curve[j]) / 127.f, 0, 1),
                     Clamp(uint8_t(curve[j + 4]) / 127.f, 0, 1),
                     Clamp(uint8_t(curve[j + 8]) / 127.f, 0, 1),
                     Clamp(uint8_t(curve[j + 12]) / 127.f, 0, 1)};
    if (!name.empty())
      c.bones[name].push_back(k);
  }
  if (r.remaining()) {
    n = r.count(23);
    for (uint32_t i = 0; i < n; i++) {
      const auto &name = trackName(15);
      MorphKey k;
      k.frame = r.read<uint32_t>();
      k.weight = Clamp(r.number(), 0, 1);
      if (!name.empty())
        c.morphs[name].push_back(k);
    }
  }
  if (r.remaining()) {
    n = r.count(61);
    c.cameras.reserve(n);
    for (uint32_t i = 0; i < n; ++i) {
      CameraKey k;
      k.frame = r.read<uint32_t>();
      k.distance = r.number();
      if (std::fabs(k.distance) > 1e6f)
        throw std::runtime_error("Camera distance exceeds supported range");
      k.target = r.vec();
      k.rotation = r.vec();
      auto curve = r.raw(24);
      for (int j = 0; j < 6; ++j) {
        // Camera bytes are x1,x2,y1,y2, unlike bone interpolation.
        auto unit = [&](int at) { return Clamp(uint8_t(curve[j * 4 + at]) / 127.f, 0, 1); };
        k.curves[j] = {unit(0), unit(2), unit(1), unit(3)};
      }
      uint32_t angle = r.read<uint32_t>();
      auto projection = r.read<uint8_t>();
      if (angle < 1 || angle >= 180 || projection > 1)
        throw std::runtime_error("Invalid VMD camera projection/FOV");
      k.fov = float(angle);
      k.perspective = projection == 0;
      c.cameras.push_back(k);
    }
  }
  for (size_t stride : {size_t(28), size_t(9)}) {
    if (!r.remaining())
      break;
    n = r.count(stride);
    for (uint32_t i = 0; i < n; i++) {
      c.lastFrame = (std::max)(c.lastFrame, r.read<uint32_t>());
      r.skip(stride - 4);
    }
  }
  if (r.remaining()) {
    n = r.count(9);
    for (uint32_t i = 0; i < n; i++) {
      uint32_t f = r.read<uint32_t>();
      c.lastFrame = (std::max)(c.lastFrame, f);
      r.skip(1);
      auto m = r.count(21);
      for (uint32_t j = 0; j < m; j++) {
        const auto &name = trackName(20);
        bool on = r.read<uint8_t>() != 0;
        c.ik[name].push_back({f, on});
      }
    }
  }
  Recount(c);
  return c;
}

template <class K>
inline size_t Upper(const std::vector<K> &keys, double frame) {
  return size_t(
      std::upper_bound(keys.begin(), keys.end(), frame,
                       [](double f, const K &k) { return f < k.frame; }) -
      keys.begin());
}
inline LocalPose SampleBone(const std::vector<BoneKey> &keys, double frame) {
  if (keys.empty())
    return {};
  auto n = Upper(keys, frame);
  if (n == 0)
    return {keys.front().position, keys.front().rotation};
  if (n == keys.size())
    return {keys.back().position, keys.back().rotation};
  const auto &a = keys[n - 1];
  const auto &b = keys[n];
  float t = float((frame - a.frame) / double(b.frame - a.frame));
  return {
      {a.position.x + (b.position.x - a.position.x) * Bezier(b.curves[0], t),
       a.position.y + (b.position.y - a.position.y) * Bezier(b.curves[1], t),
       a.position.z + (b.position.z - a.position.z) * Bezier(b.curves[2], t)},
      Quat::Slerp(a.rotation, b.rotation, Bezier(b.curves[3], t))};
}
inline float SampleMorph(const std::vector<MorphKey> &keys, double frame) {
  if (keys.empty())
    return 0;
  size_t n = Upper(keys, frame);
  if (!n)
    return keys.front().weight;
  if (n == keys.size())
    return keys.back().weight;
  const auto &a = keys[n - 1];
  const auto &b = keys[n];
  return a.weight + (b.weight - a.weight) *
                        float((frame - a.frame) / double(b.frame - a.frame));
}
inline bool SampleIk(const MotionClip &clip, const std::string &name,
                     double frame) {
  auto it = clip.ik.find(name);
  if (it == clip.ik.end())
    return true;
  size_t n = Upper(it->second, frame);
  return n ? it->second[n - 1].enabled : true;
}
inline bool EyeBone(const std::string &n) {
  return n == u8"両目" || n == u8"左目" || n == u8"右目";
}
} // namespace mmd
