# Aimisi expression candidates

`playback/ww_face_profile.h` generates exact-name/semantic expression bindings
against the existing Aimisi census (105 morph names). The reference VMD has 77
expression channels: 18 get named shape proposals and 59 remain explicitly
unmapped. Unilateral wink, tears, materials and pupil-scale effects are not
replaced by invented bilateral or facial-bone effects. All mappings require
visual calibration and runtime target-name/index verification.

`tools/mmd_outputs/face_bindings_smile_aemeath.json` preserves every source
channel, target list/gain, status and unmapped reason. It binds the VMD, census
and compiled expression binary hashes. The 4,799,552-byte WWEXPR01 binary has
9999 frames at 60fps: 105 target weights plus three source eye quaternions and
presence flags. Source eye rotations are not target-rig rotations; they need
explicit calibration before runtime use. `ww_face_clip.h` loads finite/unit
values on a worker and samples into fixed outputs without allocating.

Static ownership evidence found `USkeletalMesh::MorphTargets` TArray at `+0x368`
through NameUTF8-first descriptor `0x1487b56a0` and the owning FClassParams table.
This array contains MorphTarget objects, not live component weights. The current
SMC `MorphTargetWeights`/`ActiveMorphTargets` fields were not found in reflected
property tables. Their runtime offsets and bone-channel effects remain unverified.
The next integration step is the named SetMorphTarget native function/consumer
path, plus asset MorphTarget-name/index mutual checks; do not treat `+0x368` as
a component writable weight array.

Build `mmd_build_face.bat`; CLI arguments are VMD, census JSON, new profile JSON,
All work here uses saved files, with no game or IDA access.

测试与中间构建入口已于 2026-10-22 移除；当前构建入口为 `tools/build_player.ps1 -Rebuild`。
