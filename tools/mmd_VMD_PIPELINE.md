# VMD import and immutable local clip

Worker loading uses the existing bounded streaming `mmd::ReadVmd` and its bone
Bezier/quaternion, morph, IK, visibility and camera samplers. New
`playback/vmd_clip.h` audits the complete wire record counts and timing, including
light/shadow tracks which the core parser intentionally skips. Sampling returns
source MMD bone positions and delta quaternions; it does not calibrate a target
rig or access the game. The caller supplies the CP932 name decoder.

The user reference file has SHA-256
`8e690e7b86a1400fa8b117876af553cd1f7623a6fb441b38b285051e43217566`:
22,040,840 bytes, 195,597 bone keys in 267 tracks, 14,322 morph keys in 77 tracks,
four IK tracks and one visibility record. It has no camera, light or shadow keys.
The last bone frame is 4935 (164.5 seconds); the last morph frame is 4999, making
the full duration 166.633333 seconds. Thirteen nonstandard extension names are
invalid CP932; the CLI preserves their raw bytes in distinct placeholder names
and reports them instead of dropping tracks or guessing their spelling.

Build `tools/mmd_build_vmd_cli.bat`; use:

```
mmd_outputs/mmd_vmd_cli.exe --source <VMD> --output <new report.json> --frames <new source_frames.jsonl> --start 0 --step 0.0166666666667 --count 60
```

The report includes every bone, morph and IK track name/count/time range and five
sampled source frames. Outputs are never overwritten. This import is separate
from target retargeting and the game callback.

`playback/vmd_local_clip.h` defines the worker/runtime boundary. `WWLOCAL1`
little-endian header is exactly 160 bytes: magic[8], version/boneCount/frameCount/
fps uint32, duration double, VMD SHA-256 ASCII[64], skeleton SHA-256 ASCII[64].
Each frame contains boneCount 32-byte BoneCommands (translation delta xyz,
absolute target-local quaternion xyzw, mask uint32). Mask bit 0 enables rotation
override; bit 1 enables translation delta and is allowed only for Root index 0.
All bone masks are constant for the immutable clip. Names/parents/build/runtime
identity gates belong to an independently hashed sidecar and the runtime.

`ReadFile` loads and validates length, bounds, hashes, masks, finite values and
unit quaternions on a worker. `Sample` writes adjacent-frame lerp/slerp samples
into caller-provided fixed output, with no allocation or engine access. The
runtime should keep its currently published local baseline for unmasked bones.
Root translation units and coordinate conversion must be applied by retargeting
before serialization; no callback-time source parsing or rig IK is needed.

To compile the complete target clip:

```
mmd_outputs/mmd_vmd_cli.exe --source <VMD> --output <new report.json> --target-skeleton <raw skeleton export.json> --compiled <new motion.wwlocal>
```

This uses `playback/ww_retarget.h` and the established core Retargeter. The
default translation scale is a geometric leg-length ratio (8.415773 for the
current Aimisi export); `--scale` supplies an explicit override. Generic
StandardMmd source rig proportions remain an assumption because no source PMX
was supplied. Output is already in the target UE asset local axes; do not apply
another axis flip in the loader. The sidecar's `compiled_target_clip` includes
all names/parents, canonical local positions and their mask, hashes, fps and
duration, plus the stated source-rig/calibration limitation.

Reference output: `smile_aemeath_60fps_geometric.wwlocal` contains 9999 frames,
388 bones each, 60 rotation override bones and Root translation. It is
124,147,744 bytes. `smile_fullplan_preflight.json` verifies 300 uniformly sampled
full-body plans with zero failed plans and maximum bone-length drift 0.000016
on a constructed unit-scale target baseline. This is offline validation, not
a claim of runtime visual correctness.

core header emits C4458 (`Reader::quat` local length shadows a member); no core
source has been changed by this pipeline work.

测试与中间构建入口已于 2026-10-22 移除；当前构建入口为 `tools/build_player.ps1 -Rebuild`。
