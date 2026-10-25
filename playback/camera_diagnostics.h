#pragma once
// One camera-thread writer, any number of worker-side diagnostic readers.
// Every shared field is atomic: a retrying reader never races a plain float.
#include <array>
#include <atomic>
#include <cstddef>
#include <cstdint>

namespace wwp_playback { namespace camera_diagnostics {
struct Snapshot {
    bool anchor_captured=false;
    std::array<float,3> anchor{};
    std::array<float,4> source_to_ue{0,0,0,1};
    float source_unit_scale=1;
};

class Mailbox {
    std::atomic<uint64_t> sequence_{0};
    std::atomic<bool> captured_{false};
    std::array<std::atomic<float>,3> anchor_{};
    std::array<std::atomic<float>,4> rotation_{};
    std::atomic<float> scale_{1};
public:
    Mailbox() noexcept {
        for(auto& value:anchor_)value.store(0);
        for(auto& value:rotation_)value.store(0);
        rotation_[3].store(1);
    }
    void Publish(const Snapshot& value) noexcept {
        // Sequential consistency keeps both version checks around all fields.
        sequence_.fetch_add(1);
        captured_.store(value.anchor_captured);
        for(std::size_t i=0;i<anchor_.size();++i)anchor_[i].store(value.anchor[i]);
        for(std::size_t i=0;i<rotation_.size();++i)rotation_[i].store(value.source_to_ue[i]);
        scale_.store(value.source_unit_scale);
        sequence_.fetch_add(1);
    }
    bool TryRead(Snapshot& result) const noexcept {
        for(unsigned attempt=0;attempt<8;++attempt){
            const auto version=sequence_.load();
            if(version&1)continue;
            Snapshot value;
            value.anchor_captured=captured_.load();
            for(std::size_t i=0;i<anchor_.size();++i)value.anchor[i]=anchor_[i].load();
            for(std::size_t i=0;i<rotation_.size();++i)value.source_to_ue[i]=rotation_[i].load();
            value.source_unit_scale=scale_.load();
            if(version==sequence_.load()){result=value;return true;}
        }
        return false;
    }
};
}} // namespace wwp_playback::camera_diagnostics
