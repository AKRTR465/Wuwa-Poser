#pragma once
// Worker-only shared vtable slot patch. This affects every instance using the
// table: detour must forward other objects.
#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#include <windows.h>
#include <cstdint>

namespace v20_vmt_slot_patch {
enum class Code : uint8_t {Ok, InvalidArgument, ProtectFailed, ExpectedMismatch, ProtectionRestoreFailed, NotInstalled};
struct State {
    void** slot = nullptr;
    void* expected = nullptr;
    void* desired = nullptr;
    DWORD original_protection = 0;
    bool changed = false;
    bool protection_pending = false;
};
struct Outcome {
    Code code = Code::InvalidArgument;
    DWORD error = 0;
    bool pointer_changed = false, protection_restored = false;
};
inline const char* Name(Code code) {
    switch (code) {
    case Code::Ok:return "ok";
    case Code::InvalidArgument:return "invalid_argument";
    case Code::ProtectFailed:return "protect_failed";
    case Code::ExpectedMismatch:return "expected_mismatch";
    case Code::ProtectionRestoreFailed:return "protection_restore_failed";
    case Code::NotInstalled:return "not_installed";
    }
    return "unknown";
}
inline bool FinishProtection(State& state, DWORD protection, Outcome& outcome) noexcept {
    DWORD ignored = 0;
    if (!VirtualProtect(state.slot, sizeof(void*), protection, &ignored)) {
        outcome.code = Code::ProtectionRestoreFailed; outcome.error = GetLastError();
        state.protection_pending = true; return false;
    }
    state.protection_pending = false; outcome.protection_restored = true; return true;
}
inline Outcome Install(State& state, void** slot, void* expected, void* desired) noexcept {
    Outcome outcome{};
    if (state.changed || state.protection_pending || !slot || !expected || !desired) return outcome;
    DWORD old = 0;
    if (!VirtualProtect(slot, sizeof(void*), PAGE_READWRITE, &old)) {
        outcome.code = Code::ProtectFailed; outcome.error = GetLastError(); return outcome;
    }
    state.slot = slot; state.expected = expected; state.desired = desired;
    state.original_protection = old; state.protection_pending = true;
    outcome.pointer_changed = InterlockedCompareExchangePointer(reinterpret_cast<void* volatile*>(slot), desired, expected) == expected;
    state.changed = outcome.pointer_changed;
    outcome.code = outcome.pointer_changed ? Code::Ok : Code::ExpectedMismatch;
    FinishProtection(state, old, outcome);
    return outcome;
}
// Restore touches the slot only if its current pointer is still this patch's
// desired detour. A third party replacement is never overwritten.
inline Outcome Restore(State& state) noexcept {
    Outcome outcome{};
    if (!state.slot || (!state.changed && !state.protection_pending)) {outcome.code = Code::NotInstalled; return outcome;}
    DWORD old = 0;
    if (!VirtualProtect(state.slot, sizeof(void*), PAGE_READWRITE, &old)) {
        outcome.code = Code::ProtectFailed; outcome.error = GetLastError(); return outcome;
    }
    const DWORD restore_protection = state.protection_pending ? state.original_protection : old;
    outcome.code = Code::Ok;
    if (state.changed) {
        outcome.pointer_changed = InterlockedCompareExchangePointer(reinterpret_cast<void* volatile*>(state.slot), state.expected, state.desired) == state.desired;
        if (outcome.pointer_changed) state.changed = false; else outcome.code = Code::ExpectedMismatch;
    }
    FinishProtection(state, restore_protection, outcome);
    return outcome;
}
} // namespace v20_vmt_slot_patch
