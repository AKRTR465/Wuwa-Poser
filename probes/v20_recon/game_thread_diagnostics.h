#pragma once

#include "game_compat_witness.h"
#include <array>

namespace ww_game_thread_diagnostics {
namespace resolution = ww_game_compat_resolution;
using resolution::Address;
using ww_game_compat_witness::Hex;
using ww_game_compat_witness::Json;

struct Signature {
    const char* bytes;
    std::initializer_list<size_t> wildcardOffsets;
    size_t flag, idProvider, threadId, message;
    const char16_t* diagnostic;
};

// Each signature starts at an instruction boundary in the archived build and
// references a distinct diagnostic. Displacements are checked separately.
inline const std::array<Signature,3> signatures{{
    {"4883ec38803d00000000007400ff15000000003b05000000000f94c084c07500803d00000000037200488d050000000041b903000000",
     {6,7,8,9,12,15,16,17,18,21,22,23,24,31,34,35,36,37,40,44,45,46,47},6,15,21,44,
     u"GetGameThreadID called from non-game thread"},
    {"803d0000000000498bf90fb6f2488bd97400ff15000000003b05000000000f94c084c0750033c948890b48894b088d5131488bcbe8000000008b53088d42318943083b430c7e00488bcbe80000000041b862000000488d1500000000",
     {2,3,4,5,17,20,21,22,23,26,27,28,29,36,53,54,55,56,70,75,76,77,78,88,89,90,91},2,20,26,88,
     u"CSharpStackTrace is only available in GameThread"},
    {"803d00000000007400ff15000000003b05000000000f94c084c07500803d00000000037200488d0500000000488944242041b903000000",
     {2,3,4,5,8,11,12,13,14,17,18,19,20,27,30,31,32,33,36,40,41,42,43},2,11,17,40,
     u"UTypeScriptGeneratedClass::execCallJS is not in GameThread."}
}};

inline Address TargetRva(const std::vector<uint8_t>& bytes, Address startRva, size_t offset, size_t suffix = 0) {
    const auto raw = resolution::U32(bytes.data() + offset);
    const auto delta = raw < 0x80000000u ? int64_t(raw) : int64_t(raw) - 0x100000000ll;
    const auto next = int64_t(startRva + offset + 4 + suffix);
    return next + delta >= 0 ? Address(next + delta) : UINT64_MAX;
}

template<class Read>
Json Collect(Read&& read, Address image, Address imageBytes, uint32_t windowThread) {
    Json result = {{"status","unresolved"},{"execution_authorized",false},{"checks",Json::array()}};
    std::vector<resolution::Range> executable;
    std::string error;
    if (!resolution::ExecutableRanges(read, image, imageBytes, executable, error)) {
        result["reason"] = error;
        return result;
    }
    std::array<Address,3> consensus{};
    unsigned verified = 0;
    bool conflict = false;
    for (const auto& signature : signatures) {
        resolution::Pattern pattern;
        if (!resolution::ParsePattern(signature.bytes, "", pattern, error))
            throw std::logic_error("invalid compiled game thread signature");
        for (const auto offset : signature.wildcardOffsets) pattern.mask.at(offset) = 0;
        const auto scan = resolution::FindUnique(read, executable, pattern);
        Json check = {{"status",resolution::Name(scan.status)},{"complete",scan.complete}};
        if (scan.status == resolution::ScanStatus::Unique) {
            const auto rva = scan.address - image;
            check["code_rva"] = Hex(rva);
            std::vector<uint8_t> code(pattern.bytes.size());
            if (!read(scan.address, code.data(), code.size()) || !resolution::Matches(code.data(), pattern)) {
                check["status"] = "candidate_changed";
            } else {
                const std::u16string message(signature.diagnostic);
                std::vector<char16_t> actual(message.size() + 1);
                Address address = 0;
                const auto messageRva = TargetRva(code, rva, signature.message);
                if (resolution::ResolveImageAddress(image, imageBytes, messageRva, actual.size() * sizeof(char16_t), address) &&
                    read(address, actual.data(), actual.size() * sizeof(char16_t)) &&
                    actual.back() == 0 && std::equal(message.begin(), message.end(), actual.begin())) {
                    const std::array<Address,3> fields{TargetRva(code,rva,signature.flag,1),
                        TargetRva(code,rva,signature.idProvider),TargetRva(code,rva,signature.threadId)};
                    if (verified == 0) consensus = fields;
                    else if (fields != consensus) conflict = true;
                    ++verified;
                    check["status"] = "diagnostic_reference_verified";
                } else {
                    check["status"] = "diagnostic_reference_mismatch";
                }
            }
        }
        result["checks"].push_back(std::move(check));
    }
    if (conflict || verified < 2) {
        result["reason"] = conflict ? "verified checks refer to different fields" : "fewer than two diagnostic references verified";
        return result;
    }
    const auto [flagRva, providerRva, threadIdRva] = consensus;
    Address flagAddress = 0, providerAddress = 0, idAddress = 0;
    if (!resolution::ResolveImageAddress(image,imageBytes,flagRva,1,flagAddress) ||
        !resolution::ResolveImageAddress(image,imageBytes,providerRva,8,providerAddress) ||
        !resolution::ResolveImageAddress(image,imageBytes,threadIdRva,4,idAddress) || flagAddress == idAddress ||
        std::any_of(executable.begin(), executable.end(), [&](const auto& range) {
            return resolution::Contains(range,flagAddress,1) || resolution::Contains(range,idAddress,4);
        })) {
        result["reason"] = "referenced fields are outside the expected data range";
        return result;
    }
    result["status"] = "candidate_requires_review";
    result["registered_game_thread_id_rva"] = Hex(threadIdRva);
    result["registered_game_thread_initialized_rva"] = Hex(flagRva);
    result["id_provider_slot_rva"] = Hex(providerRva);
    uint32_t registeredThread = 0;
    uint8_t initialized = 0;
    if (read(idAddress,&registeredThread,sizeof(registeredThread)) && read(flagAddress,&initialized,sizeof(initialized))) {
        result["registered_thread_id"] = registeredThread;
        result["initialized_flag"] = initialized;
        result["window_thread_matches"] = windowThread != 0 && registeredThread == windowThread && initialized == 1;
    }
    return result;
}
} // namespace ww_game_thread_diagnostics
