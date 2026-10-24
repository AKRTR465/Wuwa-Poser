# Read-only morph binding and original-weight capture

`ww_face_snapshot.h` exposes `wwp_playback::face_snapshot::Capture` with a
reader implementing `Read` and `Identify`, plus a Kuro FName decoder callback.
`Identify` must revalidate GObjects item/object/index/serial/class/flags on every
call; this helper does not infer Go identity from cached header bytes.

All offsets and source/observed build hashes are caller inputs. The currently
audited asset enumeration uses SMC SkeletalMesh pointer `+0x5d0`, asset
MorphTargets TArray `+0x368`, and MorphTarget UObject FName12 `+0x18`. Every target
object is identified before/after the capture. The resulting fixed binding keeps
the original asset order, decoded name, FName words and serial identity.

SMC MorphTargetCurves map `+0xef0` is a static native-consumer candidate. The
explicit sparse-map layout controls data/slot counts/capacity, inline or heap
allocation flags, free-list counts and 24-byte key/weight entries. Slot count is
not the live entry count: allocation flags select entries and active count must
equal slots minus free count. Keys compare FName comparison word and number word,
matching the audited native lookup. Hole bytes are not interpreted as weights.

Until the caller independently confirms the complete current-build map layout,
leave `map.current_build_verified=false`. Capture then returns the bounded raw
map header and `weights_decoded=false`, with no fabricated original weights.
Static offsets are not automatic defaults or runtime validation.

The capture rereads map metadata/data/allocation flags, asset pointer array,
all object identities and asset relation before returning. Failure preserves
the caller's output. These reads still need a stable engine phase to exclude
ABA and payload races; they are not an atomic snapshot.

/WX`, including inline/heap flags, poisoned holes, empty maps, count mismatch,
changing headers, invalid reads, missing identities, duplicate keys and Inf.
No game or IDA is accessed by these tests.

测试与中间构建入口已于 2026-10-22 移除；当前构建入口为 `tools/build_player.ps1 -Rebuild`。
