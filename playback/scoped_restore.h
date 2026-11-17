#pragma once
#include <utility>

namespace wwp_playback {
// The callback owns recovery state and decides whether a restore is pending.
template<class Restore> class ScopedRestore {
    Restore restore_;
public:
    explicit ScopedRestore(Restore restore):restore_(std::move(restore)){}
    ScopedRestore(const ScopedRestore&)=delete;
    ScopedRestore& operator=(const ScopedRestore&)=delete;
    ~ScopedRestore(){restore_();}
};
}
