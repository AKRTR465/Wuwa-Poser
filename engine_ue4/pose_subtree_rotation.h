#pragma once
// Pure component-pose patch types and byte helpers. No memory reads/writes,
// hooks, engine calls, target addresses, or implicit index mapping.
#include <array>
#include <cmath>
#include <cstdint>
#include <cstring>
#include <limits>
#include <string>
#include <vector>

namespace wwp_ue { namespace subtree_rotation {
using TransformBytes=std::array<uint8_t,48>;
struct Manifest {
    uint32_t expected_count=0;
    std::vector<std::string> expected_names;
    std::vector<int32_t> expected_parents;
};
enum class Code {None,InvalidManifest,CountMismatch,NameParentMismatch,InvalidTree};
struct Failure {Code code=Code::None;uint32_t bone=UINT32_MAX;};
constexpr uint32_t kFixedLimit=1024;
struct Patch {
    uint32_t bone=UINT32_MAX;
    std::array<uint8_t,16> original_rotation{},rotated_rotation{};
    std::array<uint8_t,12> original_position{},rotated_position{};
    bool position_changed=false;
};
struct FixedPlan {
    uint32_t count=0;
    std::array<Patch,kFixedLimit> patches{};
};
namespace detail {
using V=std::array<double,3>;using Q=std::array<double,4>;
inline uint32_t U32(const uint8_t* b){return uint32_t(b[0])|(uint32_t(b[1])<<8)|(uint32_t(b[2])<<16)|(uint32_t(b[3])<<24);}
inline double Float(const uint8_t* bytes){static_assert(sizeof(float)==4&&std::numeric_limits<float>::is_iec559,"binary32 required");
    const uint32_t bits=U32(bytes);float value;std::memcpy(&value,&bits,4);return value;}
inline bool Store(uint8_t* bytes,double d){if(!std::isfinite(d)||std::fabs(d)>(std::numeric_limits<float>::max)())return false;
    float value=static_cast<float>(d);uint32_t bits;std::memcpy(&bits,&value,4);for(unsigned i=0;i<4;++i)bytes[i]=uint8_t(bits>>(i*8));return true;}
inline Q Mul(const Q& a,const Q& b){return {{a[3]*b[0]+a[0]*b[3]+a[1]*b[2]-a[2]*b[1],
    a[3]*b[1]-a[0]*b[2]+a[1]*b[3]+a[2]*b[0],a[3]*b[2]+a[0]*b[1]-a[1]*b[0]+a[2]*b[3],
    a[3]*b[3]-a[0]*b[0]-a[1]*b[1]-a[2]*b[2]}};}
inline Q Conj(Q q){return {{-q[0],-q[1],-q[2],q[3]}};}
inline double Norm(const Q& q){return std::sqrt(q[0]*q[0]+q[1]*q[1]+q[2]*q[2]+q[3]*q[3]);}
inline Q Unit(Q q){const double n=Norm(q);for(double& x:q)x/=n;return q;}
inline V Rotate(const Q& q,const V& v){const Q result=Mul(Mul(q,{{v[0],v[1],v[2],0}}),Conj(q));return {{result[0],result[1],result[2]}};}
inline Q Rotation(const TransformBytes& b){return {{Float(b.data()),Float(b.data()+4),Float(b.data()+8),Float(b.data()+12)}};}
inline V Position(const TransformBytes& b){return {{Float(b.data()+16),Float(b.data()+20),Float(b.data()+24)}};}
}
}} // namespace wwp_ue::subtree_rotation
