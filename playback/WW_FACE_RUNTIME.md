# Native morph lifecycle

`ww_face_runtime.h` exposes `Start(reader, prepared, state, scratch, failure)`,
`Frame(reader, writer, weights, count, state, scratch, failure)` and
`Restore(reader, writer, state, scratch, failure)`. Allocate `State` and
`Scratch` outside the game-thread callback and retain both through restoration.
Scratch is required; there is no large stack fallback. One Scratch cannot be
shared by concurrent operations.

The reader must re-read Go object/index/class/flags/serial and the selected mesh
asset relation. The writer is an independently witnessed native SetMorphTarget
adapter. The caller must provide a stable game-thread phase, matching source and
observed build hashes, native-function, complete catalog and mapped-profile
proofs. Before/after reads do not establish an engine phase or exclude ABA.

A nonempty runtime Map-layout verification remains a separate mandatory Start
gate. Empty Map headers and static disassembly cannot grant that gate.
`ObserveMap(reader, prepared, scratch, &scratch->map, failure)` is an experimental
read-only diagnostic observer: it accepts a static current-build layout plus the
strong SMC/asset relation without requiring the nonempty lifecycle gate. Its
success never sets the gate or authorizes a writer. It exposes FName words,
weights, sparse counts and the raw header; always check its return value before
using the output.

Start captures the complete 105-resource catalog and checks it a second time
around original-weight observation. The resulting identities and pointer-array
header are saved in State. Each Frame compares that baseline once before and
once after all mapped calls, and takes coherent full Map observations at those
boundaries. Each individual native call uses a bounded Map-only key/weight
readback with strong SMC/asset checks, stable header/allocation bits and a repeated
matching row. It does not reidentify the 105 MorphTarget resources. Restore
uses those strong relation and Map checks without rereading the resource catalog.

Resource FName bindings may have stable unassigned serials only when the
independently verified catalog explicitly allows them; they are marked weak.
The writable SMC and selected asset identities must have strong serials. No
generation-strong resource lifetime is inferred from serial zero.

Each mapped write is pending before native invocation. Native or readback failure
stops further writes and retains the original backup. Restore only changes values
still matching the last write; it restores the original present value, including
a present zero, or removes an originally absent entry through zero/removeZero.
Independently changed game values are skipped. Starting again cannot overwrite
an active or pending backup. Native insertion can allocate through normal UE
containers even though this helper uses fixed storage and no heap allocation.

`Prepared.mapped` contains only target names resolved from the reviewed profile.
Unmapped target weights are preserved. Remap the immutable expression clip's
alphabetically sorted 105 columns to the live catalog by exact named binding
before passing weights; live asset order differs from the sorted census. Native
FName key equality uses comparisonIndex (word 0) and Number (word 2).

MSVC x64 measured sizes are Snapshot 123,176 bytes, State 41,824 bytes, Scratch
111,352 bytes, MapSnapshot 24,720 bytes and Catalog 28,696 bytes. None of those
large objects is constructed locally by a lifecycle callback. Synthetic
`/O2 /W4 /WX` assembly reserves 104 bytes for Start, 232 for Frame, 184 for
Restore, 120 for ObserveMap, 344 for CatalogPass and 168 for MapValue, counting
saved-register pushes. Strong relation validation uses 200 bytes. No lifecycle
function calls __chkstk. These figures exclude caller, reader/writer, library
and native engine call-stack usage; the live integration still needs its own
stack and FPS verification.

The 18-channel fixture performs exactly 210 MorphTarget identity reads per
Frame and zero per Restore. Global operator-new instrumentation reports zero
allocations during Frame and Restore with a fixed-storage synthetic native
adapter. This proves the fixture/helper behavior, not allocation behavior of an
arbitrary live adapter. Evidence is saved in
`tools/mmd_outputs/face_runtime_tests.log` and
`tools/mmd_outputs/face_runtime_stack.json`. No tests read game memory or call
engine functions.

`MmdFaceFrame` from the main publication source into a self-process fixture.
Ten scenarios and 229 checks pass with the fixed-class callback reader, 105
weak serial-zero MorphTargets, two strong mesh/asset identities, a real allocated
Go chunk, and a native-call stub behind the same SEH wrapper. Saved FName words
and alphabetical profile columns are bound explicitly: 18 mapped source tracks
resolve to 17 unique target keys. The fixture proves bootstrap A=.8 insertion,
readback and deletion before A=.5/.7 sampling, pause, sparse-hole Stop restore,
and pending recovery after SEH faults or failed readback. Global operator-new
instrumentation is zero for the tested callback and Stop. It does not prove
visual appearance or native UE allocator behavior. See
`tools/verification_outputs/mmd_face_integration/test.log` and `evidence.json`.


测试与中间构建入口已于 2026-10-22 移除；当前构建入口为 `tools/build_player.ps1 -Rebuild`。
