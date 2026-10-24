# Raw camera clip

`ww_camera_clip.h` loads immutable `WWCAM001` version2 files on a worker and
exposes `SampleKey(clip, seconds, mmd::CameraKey*)` and
`Sample(clip, seconds, ww_camera::Settings, ww_camera::Frame*)`. Sampling has no
heap allocation. Source coordinates are raw MMD target XYZ and signed orbit
distance, with authored Euler radians, FOV and step perspective. Runtime world
anchor, source basis and unit scale remain explicit caller settings.

The actual Catch body compiler reports source-to-component quaternion
`(.7071068287,-4.33e-7,-4.33e-7,.7071068287)`, approximately +90 degrees about
X: source Y maps to component Z and source Z to component -Y. Multiply this by
the captured mesh ComponentToWorld rotation. An extra axis reflection is not
part of this rigid basis. Negative camera distance places the camera opposite
its optical forward and looks toward the focus. One thousand UE rotation-matrix
round trips verify forward/up axes and roll with max error3.17e-7.

`ww_camera::Settings.input_fov_vertical` and `aspect_ratio` make lens convention
explicit. The optional conversion to UE horizontal FOV is
`2*atan(tan(vertical/2)*aspect)`; aspect must be a verified value in1..4.
MinimalViewInfo AspectRatio is current static offset+2c (descriptor86145e8,
struct params948f830), corresponding to PCM cached POV+2e7c. Screen dimensions
do not establish the actual viewport aspect. The compiler retains raw FOV;
runtime conversion uses caller evidence. Camera math now passes19checks.

Header128: magic8, version2@8, fps60@12, frames@16, recordBytes40@20,
duration f64@24, sourceLastFrame@32, sourceKeyCount@36, sourceSHA64ASCII@40,
basisTag1(rawMMD)@104, adjacentCuts0/1@108, sourceKeyBytes136@112, reserved12.
The first payload has frameCount40-byte records: target float3, Euler float3,
distance float, FOV float, perspective u32, flags u32 (bit0=cutStart,
bit1=holdEnd). The tail has sourceKeyCount136-byte records: source frame u32,
distance/FOV float, target/Euler float3, perspective u32, six float4 Bezier
curves. Original controls permit exact core Bezier sampling at arbitrary game
callback times, avoiding linear approximation of nonlinear motion. The compiler
and player use version2. Older files without the source-key tail must be compiled
again from the original VMD.

Loader checks hash characters, header version/extent/reserved bytes, counts,
finite values, FOV1..179, perspective bits, known sample flags, strictly ordered
source keys and finite Bezier controls in [0,1]. Source ends hold the final key;
the player should use the longest duration across paired body/face/camera clips.

Build `tools/mmd_build_camera.bat`; invoke
`mmd_camera_compile.exe CAMERA_VMD NEW_PROFILE_JSON NEW_WWCAM [TIMELINE_SECONDS]
[continuous]`. The compiler refuses existing outputs, uses the established
VMD parser's 24-byte camera interpolation order x1,x2,y1,y2 and defaults to
adjacent-frame cuts. It validates and hashes the complete trajectory and source.

Catch Me If You Can has body78.133333s, expression76.7s, camera80.133333s.
The camera has97keys, source lastframe2404,4809 audited samples,31 cuts,
FOV14..58, all perspective. Version2 output205680B SHA
`eb7330c9f3f6ed629c33fa63980778968797929bdba9b37c06107e2eaaccca17`.
The complete import bundle is `tools/mmd_outputs/catch_me_bundle`; its reviewed
face profile retains15 semantic source tracks/16 target keys and withholds18
uncalibrated exact C/L/R names, preserving those source columns in the binary
and the unmodified original audit. README's Linnae adjustment-bone VMD is
retained as an optional source and not applied to Aimisi.

math tests pass13. All4809 grid samples match core with max float rounding error
1.19e-7;100 distributed off-grid times match exactly.1,000 world placement
samples allocate zero heap. Wrong FPS/reserved bytes/record flags/nonfinite
Bezier/truncation reject entire load. Evidence:
`tools/mmd_outputs/camera_clip_tests.log`,
`catch_me_camera_validation.json`, `catch_me_body_preflight.json`.
Body preflight300plans has0fail and max bone-length error3.40e-5.
No test accesses a game or invokes engine camera functions. Visual anchoring
and current camera ownership still require root integration verification.


测试与中间构建入口已于 2026-10-22 移除；当前构建入口为 `tools/build_player.ps1 -Rebuild`。
