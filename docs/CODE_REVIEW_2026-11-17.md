# 源码清理审查报告（2026-11-17）

## 0. 范围、方法与结论口径

**范围**：`git ls-files` 列出的全部源码与脚本（552 个跟踪文件），以当前工作区为准，含 9 个尚未提交的修改文件（CP932 截断名处理，见附录 B）。排除 vendored 的 `core_math/third_party/nlohmann/json.hpp` 和图片。`data/` 下的数据文件只做引用关系检查，不逐条审内容。

**方法**

1. **构建闭包**：以 `tools/player_build_inputs.psd1`、各 `build*.bat` 和测试脚本为入口，计算每个 C++ 翻译单元的 `#include` 闭包，把每个源文件分到三类：**生产（P）**（进入便携包的 DLL/EXE）、**测试（T）**、**退役（L）**（只被 BUILDING.md 声明“不再构建或打包”的 6 个探针 DLL 使用）。
2. **跨文件引用计数**：统计每个函数、字段和常量在 P/T/L 三类文件中的引用次数，找出零引用、只写不读、只被测试或退役代码引用的符号。
3. **配置键生产者/消费者对照**：桌面端、原生端和托管桥之间靠 JSON/TSV 传参。逐个比对写出的键是否有读取方。
4. **人工通读**：`core_math/`、`engine_ue4/`、`playback/`、`probes/v20_recon/` 的生产部分、`runtime/owned_mmd/`、`desktop/WWPoser/Services/`、构建脚本；UI 文件只做抽查。
5. **单行复杂度打分**：每行分值 = 语句数 + 2×三元运算数 + 逻辑运算数/2，并人工复核排名靠前的行。

**分级**

| 级别 | 含义 | 处理建议 |
| --- | --- | --- |
| **A** | 零引用、只写不读，或只被退役代码使用 | 可直接删除 |
| **B** | 旧格式兼容或旧协议分支；现行桌面端不再产生对应输入，但仍有测试锁定 | 与测试一起删除 |
| **C** | 设计层冗余：同一逻辑多份实现，或同一数据走两条通道 | 合并重构 |
| **D** | 单行过密或嵌套三元，可读性问题 | 格式化或拆分 |

**限制**：本报告基于静态审查，未编译也未进游戏运行。部分“无人读取”的 JSON 字段可能用于用户提交的排障记录，删除前请确认它们不在反馈流程中。游戏进程内的防御代码只在确认冗余时才列入可清理项。

---

## 1. 总览（按收益排序）

| # | 发现 | 级别 | 规模 | 位置 |
| --- | --- | --- | --- | --- |
| 1 | 6 个已退役探针的源码、专用头文件和构建脚本仍在仓库中 | A | 约 2,321 行 / 167 KB，另有 6 个 bat | `probes/v20_recon/`、`playback/ww_{mmd_squad,prop_visibility,stage_residency}.h` |
| 2 | 研究、逆向和诊断代码随正式 DLL 发布：内存扫描、字节特征搜索、函数字节采样，还有一个无任何生产者的实验模式 | A/C | 约 300 行 | `owned_mmd_body.h:324-464`、`native_graph_discovery.cpp:37-95,305-319`、`native_identity_bootstrap.cpp:173-191` |
| 3 | 整个文件或单个函数零引用 | A | 5 处 | `OwnedMmdPresentation.cs`、`CharacterRegistry.Resolve`、`ManagedBridge.Probe` 等 |
| 4 | 写出后无人读取的配置和状态字段 | A | 约 60 个键，含一个过期契约文件 | §3.4 |
| 5 | 单人会话、squad、旧 TSV 和旧宿主等兼容分支，现行桌面端已不再产生对应输入 | B | 原生、托管、桌面三端都有 | §4 |
| 6 | FName 解码、GObjects 查表、`Require`、`Pointer`、`Hex`、TArray 头和祖先遍历各有 3–6 份实现 | C | 横跨 10+ 个文件 | §5.1 |
| 7 | 版本相关的 RVA 和偏移绕过 `game_compat` 生成器，硬编码在 5 个以上文件里，其中部分数值重复 | C | 约 30 个常量 | §5.2 |
| 8 | 生成器产出两份相同的 C# 常量类（各 234 个常量），使用方分别只用到 24 和 45 个；布局 JSON 与编译期常量双通道 | C | 约 500 行生成代码 | §5.3 |
| 9 | 控制命令的槽位/组/链约束在 C# 和 C++ 中各手写一份，生成器没有覆盖 | C | 2 处 | §5.1 |
| 10 | 单行嵌套三元、一行十余条语句、单行 700–860 字符 | D | 约 40 处重点 | §6 |

---

## 2. 退役探针与实验代码

### 2.1 已退役 DLL 的完整源码（A）

`docs/BUILDING.md:67` 已声明 `recon_carrier_v20.dll`、`accessory_recon.dll`、`publish_probe_v21.dll`、`puerts_scene_sampler.dll`、`stage_residency.dll`、`mmd_squad_residency.dll` “不再默认构建或打包”。按 include 闭包，下列文件**只**被这些 DLL 使用，不进入任何生产产物；其中只有 `ww_mmd_squad.h` 仍被测试引用。

| 文件 | 行 / 字节 | 说明 |
| --- | --- | --- |
| `probes/v20_recon/publish_probe.cpp` | 462 / 33.9 KB | 旧单人发布探针 |
| `probes/v20_recon/stage_residency.cpp` | 346 / 22.0 KB | 旧舞台驻留 |
| `probes/v20_recon/mmd_squad_runtime.h` | 266 / 20.8 KB | 旧三人小队 |
| `probes/v20_recon/puerts_scene_sampler.cpp` | 131 / 9.7 KB | Puerts 采样 |
| `probes/v20_recon/recon_carrier.cpp`、`accessory_recon_carrier.cpp`、`mmd_squad_residency.cpp` | 131 行 | 载体入口 |
| `csharp_runtime_readonly.h`、`csharp_scene_team_readonly.h`、`csharp_scene_team_sampler_readonly.h`、`mono_{class_cache,managed,runtime}_readonly.h`、`scene_team_native_readonly.h`、`scene_team_stage_runtime.h`、`stage_capture_readonly.h`、`stage_native_readonly.h`、`weapon_capture_readonly.h`、`camera_capture_readonly.h`、`native_mmd_bridge.h` | 约 850 行 | 只读研究头文件 |
| `playback/ww_stage_residency.h`、`playback/ww_prop_visibility.h` | 174 行 | 仅退役代码使用 |
| `playback/ww_mmd_squad.h` | 50 行 | 仅退役代码和 `unified_transport_tests.cpp:103-110` 使用 |
| `probes/v20_recon/build.bat`、`build_accessory_recon.bat`、`build_publish_probe.bat`、`build_puerts_scene_sampler.bat`、`build_stage_residency.bat`、`build_mmd_squad_residency.bat` | 6 个脚本 | 无人调用 |

BUILDING.md 写明“需要诊断时可直接运行对应脚本”，所以删除前需要决定这些诊断能力是否还要保留。如果保留，建议整体移到 `research/` 或单独分支，避免它们继续在生产头文件里“拉住”大量只为它们服务的 API（见 §2.2）。删除后还应同步修改 BUILDING.md:67 的说明。

### 2.2 生产头文件里只服务退役代码或测试的 API（A/B）

这些符号所在的头文件会进入正式 DLL，但符号本身没有生产调用方。

| 位置 | 符号 | 实际使用方 |
| --- | --- | --- |
| `playback/player_transport.h:64-67` | `DecodeCommand`（生产走 `DecodeCommandFrame`） | 测试和退役代码 |
| `playback/player_transport.h:96-99,219-225` | `SetInitialCameraMode`、`Drain`、`AcknowledgeRestore` | 只有 `publish_probe.cpp` 和测试 |
| `playback/ww_world_snapshot.h:9-28` | `Manifest`、`Sample`、`Code`、`Valid(Manifest)`、`Pass`、`Capture` | 只有 `publish_probe.cpp`；生产只用 `Decode`、`Position` |
| `playback/ww_restore_policy.h:14-23` | `Pointer`、`Strong`、`Same` | 只被上面的 `world_snapshot` 退役路径调用；生产只用 `Token` 类型 |
| `probes/v20_recon/publish_observer.h:17-21,30-42,57-68` | `TargetIdentity`、`Sample`、`Same`、`Observe` | 只有 `publish_probe.cpp` |
| `probes/v20_recon/shared_readonly.h:36,108,112` | `FileName`、`BytesHex`、`Save(const wchar_t*)` | 退役载体和测试 |
| `probes/v20_recon/object_identity.h:17,67` | `ww_object::Integer`、`ww_object::Status` | 只有 `puerts_scene_sampler.cpp` |
| `playback/native_physics_actor.h:70-74,121` | `external_feed`/`pending`/`force_pending`/`frames`/`error` 指针和 `read_snapshot` 回调分支 | 只有退役的 `native_mmd_bridge.h` 设置；生产只设 `external_writing`、`on_commit` |
| `probes/v20_recon/shared_native_physics.h:283` | `RequestStop` 包装 | 只有退役代码 |
| `probes/v20_recon/unified_player_features.h:296-300` | `RestoreActorAfterFault` | 测试和退役代码 |
| `probes/v20_recon/unified_player_features.h:257` | `ActorFrame` 中的 `if(a.native_physics) return GravityPartFrame(...)` | 生产对原生物理演员直接调用 `GravityPartFrame(...,true)`（`owned_mmd_body.h:284`），只对非原生物理演员调用 `ActorFrame`（`:289`），所以该分支只有测试和退役路径能走到 |
| `playback/native_kawaii_parameters.h:200-203` | 7 参数版 `EvaluateGroup` 重载 | 只有测试 |

### 2.3 随正式 DLL 发布的研究和诊断代码（A/C）

| 位置 | 内容 | 判断 |
| --- | --- | --- |
| `runtime/owned_mmd/native/owned_mmd_body.h:324-464` `ReferenceLayoutCandidates` | 参考骨架读取失败时，扫描网格对象 0x10–0x1200 和资产对象偏移，用 6 段硬编码字节特征在主程序代码段中搜索 GetBoneName、发布函数、Morph、相机和帧计数器，甚至 `Sleep(100)` 采样帧计数；结果写入 `reference_skeleton_diagnostic.json`，并标记 `"execution_authorized":false` | 这是版本迁移用的逆向工具，不属于播放逻辑。它在失败路径执行，会把一次普通失败拉长并扫描整个代码段。建议移到独立诊断工具 |
| `probes/v20_recon/native_graph_discovery.cpp:305-319` `observe_kawaii_settings_only` 模式，以及 `:37-76` `CapturePhysicsSettingsEvidence` 和 `kApplyResult`/`kCapsuleUpdate`/`kLimits`/`kWorldCollision`/`kCollisionCandidate`/`kCapsuleContacts` 常量 | 只导出函数字节的研究模式。`observe_kawaii_settings_only`、`observe_kawaii_collision_code`、`observe_capsule_contact_code` 三个键**在全仓库没有任何生产者** | **A，死代码**。`:48` 还硬编码了 3.8.0 的 SHA |
| `native_graph_discovery.cpp:78-95` `CapturePhysicsSettings` | 由 `WWPOSER_OBSERVE_KAWAII_SETTINGS=1` 打开（`NativeGraphService.cs:73`，GAME_COMPATIBILITY.md:71 有记录） | 文档化的诊断开关，可保留；但它的偏移与 `native_kawaii_parameters.h` 重复，见 §5.2 |
| `probes/v20_recon/native_identity_bootstrap.cpp:173-191` `callback_context_diagnostic` | 失败时转储 proxy 的 64 个字并逐个尝试识别 UObject | 诊断代码 |
| `probes/v20_recon/game_thread_diagnostics.h` 全文件、`game_compat_witness.h:22-41` `Candidate` | 游戏线程登记字段和函数重定位的特征扫描，结果都标记 `execution_authorized=false` | 诊断代码，只在失败路径运行；建议与 `ReferenceLayoutCandidates` 合并为一个可选诊断模块 |
| `desktop/WWPoser/Program.cs:10-11,45-50,29-44,51` | 无文档、无测试调用的 headless 命令：`--import-vmd`、`--save-bookmark`、`--validate-bookmark`、`--render-recording-audio`、`--owned-preparation` | 开发入口；`--prepare-owned-mmd`、`--run-owned-mmd` 在 `runtime/owned_mmd/README.md` 有记录。参数按下标取值，没有边界检查 |

---

## 3. 生产代码中的死代码

### 3.1 函数和类型（A）

| 位置 | 内容 |
| --- | --- |
| `desktop/WWPoser/Services/OwnedMmdPresentation.cs`（整个文件，36 行） | `OwnedMmdPresentation.PreparationSummary`、`RuntimeStatus`、`OwnedMmdDisplayStatus` 在全仓库（含测试）零引用。连带 `OwnedMmdPlaybackLaunch.cs:75` 写出的 `static_display` 键也只有它读取 |
| `desktop/WWPoser/Services/CharacterRegistry.cs:16` | `Resolve(mesh, skeleton)` 零调用 |
| `runtime/owned_mmd/managed/ManagedBridge.cs:168` | `Probe()` 不在原生 `kActionNames` 中，也没有其他调用 |
| `probes/v20_recon/native_target_publication.h:13` | `PromoteRegisteredSupport` 零调用，第 4 个参数还是未命名的 `const Json&` |
| `runtime/owned_mmd/native/owned_mmd_body.h:310-312` | `ReadClip` 只是 `clip::ReadFile` 的一行转发 |
| `probes/v20_recon/native_identity_bootstrap.cpp:45` | `allocateCallback` 函数指针是 .cpp 内的 static，测试无法替换，这层间接没有意义 |

### 3.2 只写不读的字段（A）

| 位置 | 字段 |
| --- | --- |
| `core_math/include/poser_math/mmd_retarget.h:230,283` | `Retargeter::clip_` 赋值后从未读取 |
| `core_math/include/poser_math/mmd_arm_twist.h:10,45` | `ArmTwistBinding::endRole` |
| `playback/native_physics_actor.h:58,61,151,153,155` | `Actor::evaluation_thread`、`calls`、`selected` 只递增或赋值；连带 `Access::thread`（`:36`，`shared_native_physics.h:62`）只为前者存在 |
| `playback/ww_camera_snapshot.h:12-16` | `Layout::controller_pawn`、`controller_ack`、`pawn_controller`、`pcm_view_target`、`aspect_ratio`。另外 `Manifest::layout` 恒等于 `kCurrent`，这层可配置是多余的 |
| `playback/ww_world_snapshot.h:9-10` | `Manifest::source_sha256`、`observed_sha256`、四个 `*_verified` |
| `probes/v20_recon/native_graph_discovery.h:96,130,158` | `RootSnapshot::stable` 和 `identity_stable` 被直接赋为 `read`；`Mapping::stable` 同样等于 `read`；`Phase::binding_alive` 恒为 true。这些都是早期“两次读取比对”的残留，下游却仍拿它们判定 `root_array_unstable` 等原因（`native_graph_discovery.cpp:208-211`），这些原因实际上永远不会出现 |
| `probes/v20_recon/face_capture_readonly.h:11` | `Adapter::pool` 未使用 |
| `probes/v20_recon/shared_readonly.h:40-43` | `Reader::bytes` 累加后从未读取 |
| `probes/v20_recon/native_identity_witness.h:7` | `kObjectArrayRva`、`kAllocatorRva` |
| `engine_ue4/pose_subtree_rotation.h:20`、`pose_full_body.h:49` | `subtree_rotation::Code::CountMismatch`、`full_body::Code::InvalidTopology` |
| `playback/ww_face_clip.h:12` | 读取了 `version` 却不校验 |
| `playback/ww_physics_profile.h:11` | `Unavailable` 异常类型从未被单独捕获，用 `std::runtime_error` 即可 |

### 3.3 无用参数和默认值（A）

| 位置 | 问题 |
| --- | --- |
| `playback/ww_camera_math.h:32` | `Place(key, double, ...)` 的 `double` 参数未命名也未使用 |
| `runtime/owned_mmd/native/owned_mmd_body.h:219` | `FormationHeightCorrection(config, const Json&)` 第二个参数未使用 |
| `probes/v20_recon/shared_native_physics.h:207,273-275` | `PrepareBinding` 的 `const std::string&` 未使用，`PrepareFromConfig` 只是把 `profile` 透传过去 |
| `shared_native_physics.h:304` | `Install(Json&)` 参数未使用，调用方要先构造一个空 `Json` 再传入（`owned_mmd_body.h:585`） |
| `shared_native_physics.h:335` | `Uninstall(report, field=...)` 生产调用方丢弃 `report`（`owned_mmd_body.h:678`），`field` 从未传非默认值 |
| `engine_ue4/pose_full_body.h:61` | `PrepareTopology(..., sub::Failure* failure=nullptr)` 所有调用方都不传 |
| `pose_full_body.h:36-37` | `Request::quaternion_tolerance`、`minimum_scale` 从未被改写，等同常量 |
| `playback/native_kawaii_pose.h:17-21,42` | `Layout` 结构的默认值来自生成常量，唯一调用处传 `{}`（`native_physics_actor.h:171`），可配置层多余 |
| 未提交改动 `core_math/include/poser_math/mmd_motion.h:195`、`playback/vmd_clip.h:13` | `decodeFullName = {}` 默认值：现有 5 处调用全部显式传入，见附录 B |

### 3.4 写出后无人读取的配置和状态字段（A，删除前确认不用于排障）

**桌面 → 原生配置**（`grep` 确认只有写入方）：

- `PlaybackAssets.cs:59-60`：`camera_runtime_contract_file`，并把 `data/camera_runtime_contract.json` 复制进**每个**会话。原生端不读这个文件，而它的 `source_hashes` 已经过期：引用的 7 个文件中 4 个已不存在（`ww_camera_runtime_test.cpp`、`ww_camera_runtime_test.bat`、`ww_world_snapshot_test.cpp`、`ww_camera_basis_test.cpp`），另外 3 个的哈希与当前源码不一致。`SceneFiles`（`:7`）也包含它。
- `PlaybackAssets.cs:87`：`physics_profile_file`。
- `PlaybackAssets.cs:39,50-51`：`morph_function_rva`、`morph_function_bytes_hex`。为写这两个键每次都要读 `morph.json`，而原生端实际使用的是编译期常量 `FunctionSetMorphTargetRva`（`unified_player_features.h:88`）。
- `PlaybackAssets.cs:41,44,46`：`expression_profile_file`、`expression_source_sha256`、`gaze_source_sha256`、`gaze_independent_eyes`；`:69-71` 的 `camera_source_sha256`、`camera_fps`、`camera_frame_count`。
- `OwnedMmdPlaybackService.cs:55`：`body_backend="game_native"`。原生状态里另写了一个 `"per_actor"`，两者没有关系。
- `OwnedMmdActorPreparation.cs:101-102,114`：`ownership_preference`（写两次）、`target_slot`（与 `slot` 重复）。
- `NativeGraphService.cs:119-125,137`：`physics_backend`、`freeze_native_animation_time`（只有退役的 `publish_probe` 读取）、`native_physics_activate_curve_groups`、`native_graph_game_pid`、`native_graph_source_config_file`、`native_graph_source_config_sha256`、`native_graph_root_layout_witnesses`（为它每次都计算 `RootWitnesses()`，原生端不读）。
- `NativeGraphBinding.cs:68-98`：`proof` 下 6 个常量 `true`（`unique_main_converter`、`pre_physics_order_verified`、`identity_stable` 等）；原生端只读 `activation_complete`（`native_graph_binding.h:106`），桌面只读 `source_config_sha256`。`converter.vtable_rva`、`function_rva`、`curve_name_offset`、`discovery_file` 同样无人读取。

**原生 → 桌面状态**（`owned_mmd_body.h:619-664` `Status()`）：

- 成对重复：`actors`/`members`、`slot`/`target_slot`、`frames`/`body_frames`、`session_epoch`/`clip_epoch`、`camera_stage_reference_used`/`camera_stage_floor_used`、`formation_height_correction_cm`/`ground_height_cm`。桌面因此写成 `state["body_frames"] ?? state["frames"]`（`OwnedMmdCommandLine.cs:48`）。
- 常量字段：`model_preview_supported:true`（桌面 `MainWindow.PhysicsModel.cs:107` 仍在检查它）、`body_backend:"per_actor"`、`camera_anchor_coordinate_frame:"world_local"`；`shared_native_physics.h:355,368,372` 的 `physics_backend`、`native_graph_binding_kind`、`native_secondary_motion_evidence_scope`。
- `write_gate_closed` 可由 `writing`/`closing` 推出；另有十余个诊断键（`camera_anchor_position`、`hooks_restored`、`body_elapsed_ms` 等）没有 C# 读取方。
- `n1_observer.cpp:121,145,169,215`：每次都把同一份状态写入 `mmd_status.json` 和 `player_status.json` 两个文件。桌面对 owned 会话只读 `mmd_status.json`（`PlaybackSessionStatus.cs:12`）；`player_status.json` 只用于旧单人格式和测试夹具。
- `n1_observer.cpp:196-199`：`owned_reuse_ready.flag` 在仓库内没有读取方。注释称“旧版 helper 等待它”，但按当前 `Bridge::Bind` 流程，旧 helper 在执行任何动作前就会被 `VerifyCompatibility`/`BindSession` 检查拒绝（`mono_owned_public_facade.h:37-41`）。建议确认没有外部旧版本依赖后删除。

**原生目标导出**（`native_target_publication.h:36-64`）：`expected_pawn_class` 和 `expected_actor_class` 值相同，`expected_game_pid` 与 `game_pid` 相同，`expected_component_name`、`profile_resolution`、`process_start_filetime`、`source_sha256` 都没有读取方；`Token()` 里的 `class` 与 `class_name` 重复。

**布局文件**：`runtime/owned_mmd/native/owned_mmd_layout.json` 共 16 个键，其中 `hook_mode`、`vmt_rva`、`vmt_slot_offset`、`component_owner`、`actor_root`、`component_asset`、`asset_skeleton`、`asset_ref_bone_info`、`component_main_anim` 这 9 个**从未被读取**（另见 §5.3）。

**构建脚本**：`tools/build_player.ps1:58-65` 拷贝 `session_usage/*_owned_mmd_(lite|session).json`，但当前代码中没有任何地方生成或读取这些标记。README 中“会话占用记录”的描述可能也已过期，需要确认。

---

## 4. 兼容层和旧格式分支（B：现行生产不产生，多数被测试锁定）

桌面端现在总是输出新格式：`PrepareRuntime` 一律转换为多演员 `actors` 配置（`OwnedMmdPlaybackLaunch.cs:20-26`）；`WriteActorsFile` 总写 16 列的 `actors.tsv`、`stage_floor.tsv` 和 `scene_visibility.tsv`（`OwnedMmdActorPreparation.cs:49-76`），还会删掉 `camera_controller_address` 等旧键（`OwnedMmdPlaybackLaunch.cs:44,53`）。因此下游的旧格式分支在生产中都走不到。

**原生端**

| 位置 | 旧分支 |
| --- | --- |
| `owned_mmd_body.h:528,534,537-540,543,303,525` | `!multiActor` 单人路径：`Json::array({config})`、`legacyProfile` 参数、`native_input` 目录、`slot` 回退到 `target_slot`、`engine_frame_rva` 缺失回退（布局文件总包含该键） |
| `owned_mmd_body.h:535,547` | `graph.player_controller` 缺失时回退到 `config.camera_controller_address`。ManagedBridge 总会写 `player_controller`，桌面又删掉了旧键，所以这个回退一旦被触发只会抛异常 |
| `owned_mmd_body.h:467` | `ownership` 的双层回退 |
| `n1_observer.cpp:125,129` | `legacyProfile`、`native_input_ready.flag` |
| `unified_player_features.h:93-94` 和 `owned_mmd_body.h:516` | `PrepareActor` 依次接受 `bone_names`/`expected_bone_names`、`parents`/`expected_bone_parents`。桌面写的是 `bone_parents`，两者都不匹配，于是 `InitializeActor` **复制整个 config** 只为注入一个 `expected_bone_parents`。应统一为读取 `bone_parents` |
| `unified_player_features.h:414-423` 及 `owned_mmd_body.h:552-554` 的 `cameraRootStarts` | 没有舞台参考时的“首演员”回退（注释写 “Legacy helpers”）。生产中 `ShowAtLocal` 先于 `Initialize` 写出参考，所以该路径只在测试和旧 helper 中出现；建议低优先级确认后删除 |

**托管桥（ManagedBridge.cs）**

| 位置 | 旧分支 |
| --- | --- |
| `:352-356` | `actors.tsv` 缺失时读取 `owned_character.txt` |
| `:361` | 8 列旧格式 `actors.tsv`（`ManagedActorBorrowing` 测试大量使用 8 列夹具，删除时需要一起迁移） |
| `:195`、`:671` | `scene_visibility.tsv`、`stage_floor.tsv` 缺失时回退（“Old preparations retain their behavior”） |
| `:728` | `Export()` 在单演员时额外写顶层 `actor`/`mesh`/`asset`/`skeleton`/`main_anim`，只服务原生单人路径 |

**桌面端**

| 位置 | 旧分支 |
| --- | --- |
| `PlaybackSessionStatus.cs:9-25` | `stage_config.json`（squad）、`player_status.json`（旧单人），以及数字 `mode` 映射。现行 owned 会话总有 `owned_mmd_config.json`，状态的 `mode` 已是字符串。注意多项 UI 和服务测试拿旧单人格式当轻量夹具，需要先迁移夹具 |
| `BookmarkService.cs:6-15,22` | squad/`members`、`observe_target.json` 会话和 `clip_sha256` 旧书签 |
| `PlaybackControl.cs:60` | “Older hosts only acknowledge accepted commands”；当前宿主总会发布 `last_control_sequence` |
| `PlaybackSessionPreparation.cs:41-48,72-77` | 旧准备目录兼容：用魔法字符串 `"explicit CLI --scale override"`（与 `mmd_vmd_cli.cpp:49` 的文本耦合）反推缩放、从 `camera_profile.json` 取镜头源 |
| `PlaybackAssets.cs:49` | `mapped_target_names` 缺失时临时重建（一行 297 字符） |
| `NativeGraphService.cs:23-25,90` | 版本化见证文件不存在或 SHA 不匹配时回退到 `native_kawaii.json`/`native_activation.json`。当前版本的 SHA 一致，回退不可达；而且旧归档中约 80 个标签是研究记录 |
| `MotionImportService.cs:18-20` | `CharacterProfile` 为空时按 `ModelId` 或骨架摘要遍历 index；现有调用方都会传 `CharacterProfile` |
| `BundleValidation.cs:23,35` | schema < 4 的旧清单（BUILDING.md 有记录，可保留） |
| `MainWindow.cs:289`、`build_player.ps1:73-78` | `start-owned-mmd.cmd` → `--owned-mmd` → `SelectOwnedMmdPage()`，只是“若无演员就添加一个”，属于旧独立页面的入口残留 |

**构建脚本**

- `tools/build_owned_mmd.ps1:30,34,49`：前面已经 `if($manifest.schema -ne 2){throw}`，后面又判断 `if($manifest.schema -eq 2)` 和 `$manifest.schema -ne 2`，是 schema 1 时代的残留；`:32` 的报错硬编码 “exactly four files”。`purpose='single_owned_character_native_mmd'`（`:18`，`n1_common.h:9`）的命名也已与多演员现状不符。
- `tools/build_player.ps1:80`：重复执行一次 `git rev-parse HEAD`（`:11` 已取得 `$releaseRevision`）。

**可删除的冗余防御检查（非兼容类）**

| 位置 | 问题 |
| --- | --- |
| `native_graph_discovery.cpp:375` | `report["identity_after"]=report["identity_before"]; report["identity_stable"]=true;` 并未重新读取，却声称身份稳定；`:382` 的完成判定和 `NativeGraphBinding.cs:93` 都依赖这个恒真值。**这是有误导性的假检查**：要么实现真正的复读比对，要么删除这两个字段 |
| `mmd_motion.h:81-84` | `Reader::raw` 先调 `require(n)`，`copy` 内部又调一次 |
| `mmd_rig.h:268-273` | `ikEnabled` 每次都在 `controllers_` 中线性查找；`sample()` 正在遍历 `controllers_` 时调用它，这次查找必然命中 |
| `player_transport.h:57,65,79` | `DecodeCommandFrame`、`DecodeCommand` 和 `Pop` 的 `!output` 检查，调用方都传栈上对象 |
| `vmd_local_clip.h:106-107` | `Sample` 每帧重新校验 `ReadFile`/`ValidatePayload` 已保证的维度 |
| `camera_diagnostics.h:24-28` | 构造函数逐个把已值初始化（`{}`）的原子量存 0，只有 `rotation_[3]=1` 有意义 |
| `ww_face_runtime.h:34` | `Pending` 的 `i<fs::kMaxMorphs`（`count` 在 `Start` 中已受限） |
| `native_physics_actor.h:256` | `Registry::Group` 的 `!index` 检查 |
| `mmd_arm_twist.h:38` | `below` 带 128 次的环路保护，而同功能的 `mmd_retarget.h:321-325` 和 `:398-403` 不带，保护策略不一致 |
| 多个 bat 中的 `/wd4505` | 关闭的是“未引用的 static 函数已移除”警告。`shared_readonly.h`、`object_identity.h` 大量使用头文件 `static` 函数，这个屏蔽恰好隐藏了本报告列出的部分死函数。建议改为 `inline` 后去掉 `/wd4505`，让编译器帮忙发现死代码 |

---

## 5. 冗余与重复实现（C）

### 5.1 同一逻辑的多份实现

| 逻辑 | 份数和位置 | 建议 |
| --- | --- | --- |
| FName 解码（读块、判宽字符、拼 `_N` 后缀） | 5 份：`shared_readonly.h:69` `Decode`、`object_identity.h:49` `DecodeName`、`native_graph_discovery.cpp:161` `FullName`、`native_identity_bootstrap.cpp:27` `AnimationClass` 和 `:60` `TokenName`、`face_capture_readonly.h:35` lambda；后缀拼接还散见于 `owned_mmd_body.h:369,500`、`object_identity.h:63` | 保留一个模板实现 |
| GObjects 下标 → item → serial | 5 份：`shared_readonly.h:55` `Object`、`object_identity.h:29` `ReadStable`、`native_graph_discovery.h:55` `CaptureToken`、`shared_native_physics.h:128` `Capture`、`face_capture_readonly.h:13` `Adapter::Identify` | 合并 |
| `Require(bool,msg)` | 生产中 5 份（`native_graph_binding.h:23`、`runtime_evidence.h:19`、`unified_player_features.h:33`、`mono_owned_public_facade.h:15`、`owned_mmd_body.h:72`），退役代码中另有 6 份 | 合并 |
| `Pointer(uint64)` | 4 份，常量还不一致：`ww_restore_policy.h:14` 和 `runtime_evidence.h:22` 硬编码 `0x10000/0x800000000000`；`shared_readonly.h:29` 和 `publish_observer.h:35` 使用生成常量 | 合并并统一常量 |
| `Hex()` 和十六进制解析 | `shared_readonly.h:27`、`game_compat_witness.h:13` 两个 `Hex`；解析有 `native_graph_binding.h:24` `Address`、`native_animation_target_config.h:12` `Number`、`object_identity.h:22` `Rva`、`native_graph_discovery.cpp:143` 和 `native_identity_bootstrap.cpp:25` 的 `Address`、`Contract.Number`（C#） | 合并 |
| TArray 头结构 | `native_graph_discovery.h:75` `ArrayHead`、`publish_observer.h:22` `ArrayHead`、`ww_face_snapshot.h:22` `ArrayHead`、`native_kawaii_pose.h:14` `ArrayHeader`、`native_kawaii_parameters.h:26` `Array` | 统一为一个 |
| `ExtraVectorsAbsent` | `native_kawaii_pose.h:36` 和 `native_graph_discovery.h:79`，函数体逐字相同 | 删去一份 |
| 祖先遍历 | `mmd_retarget.h:321`（lambda）和 `:398-403`（两段手写 while）、`mmd_arm_twist.h:37` | 一个工具函数 |
| 身份比较 | `ww_camera_snapshot.h:25` `SameFields` 等于去掉 `Strong` 检查的 `ww_restore::Same`；`object_identity.h:41`、`ww_face_snapshot.h:20`、`publish_observer.h:38`、`native_graph_discovery.h:54` 又各有一个 `Same` | 收敛 |
| `BoneCommand` 和掩码 | `vmd_local_clip.h:21-23` 与 `pose_full_body.h:17-24` 的 32 字节布局完全相同，掩码常量也重复。`native_physics_actor.h:183-184` 和 `unified_player_features.h:269-270` 每帧逐骨 `memcpy` 互相转换 | 共用一个类型，可直接 `Sample` 进 `LocalFrame` |
| 两个观察器 DLL 的脚手架 | `native_graph_discovery.cpp` 和 `native_identity_bootstrap.cpp` 重复了 `Read`、`Address`、`Detour` 及 5 个跳板函数、`DetourAddress`、`OriginalAddress`、`Outcome`、`Save`、`Quiescent`、vtable 槽校验循环、`HookProc`、`DllMain` 和 Worker 骨架，约 80 行近乎逐字重复 | 抽一个 `observer_carrier.h` |
| `RegisterSupport` 与 `Register` | `native_identity_bootstrap.h:52-79`，两者约 80% 相同 | 合并 |
| ManagedBridge 场景隐藏 | `SceneVisibility`（`:275-346`）与 `SimpleSceneVisibility`（`:207-252`）的识别、隐藏、恢复和扫描完全平行；`SceneIdentityCurrent`（`:275`）重写了 `HandleRegistered`（`:387`）的注册表校验；`EnableMeshEvaluation`（`:551`）与 `Maintain`（`:690-711`）是同一张 12 项标志表，前者无条件设置、后者带条件设置 | 抽通用的 “visibility lease” 和 “mesh flags” 表 |
| 进程启动 | `OwnedMmdStagePreview.cs:36-73` 与 `OwnedMmdPlaybackLaunch.cs:46,61-65,102-126`：拷贝运行时、写 `session_config.json`、`runas` 启动安装器和轮询退出 | 抽一个共用方法 |
| 控制命令的目标约束 | C# `PlaybackControl.cs:13-21` 与 C++ `player_transport.h:42-50` 手写了**相同**的 slot/group/chain 规则；生成器（`generate_playback_control.ps1`）只覆盖 id、范围和 scope | 把 `target` 规则加进 `control_commands.json` 并一起生成 |
| 时长限制 | `Transport::Tick` 用游戏线程时钟实现 `time_limit`（`player_transport.h:120-125`）；worker 又在 `owned_mmd_body.h:614-615` `CheckTimeLimit` 中用墙钟再实现一遍 | 确认需要双保险，否则保留一份 |
| `ApplyCommand` 分支 | `owned_mmd_body.h:109-209` 中 7 个原生物理分支大多重复 `FindSlot` → `nativePrepared` →（`CanSetParameter`）→ `AcceptRoutedCommand` 这串前置检查 | 抽一个前置函数 |

### 5.2 绕过 `game_compat` 生成器的版本相关常量

项目约定版本相关参数统一放在 `data/game_compat/ww_runtime.json`（BUILDING.md “源码目录”一节）。以下常量写死在源码中，下次游戏更新时容易漏改：

- `playback/native_kawaii_parameters.h:16-21`：`kUpdateRva=0xa5d990` 等 3 个 RVA 和 7 个节点偏移。
- `probes/v20_recon/native_graph_discovery.cpp:38-46`：10 组 RVA；其中 `0xa5d990/0x471`、`0xa5b750/0x18fe`、`0xa51030/0x6af` 与上面**重复**。`:80-94` 还把 `0x108/0x260/0x160/0x1f8/0x1b0/0x28` 写成裸数字，这正是 `kNodeSettings`/`kInitialized`/`kRefresh`/`kBoneArray`/`kBoneStride`/`kBoneSettings`。
- `shared_native_physics.h:185-187`：6 个胶囊相关函数的 RVA，与 `native_graph_discovery.cpp:41-46` 重复。
- `native_kawaii_gravity.h:13`（`kNodeGravity`、`kConjugateSignsRva`）、`native_kawaii_collision.h:7-10`（胶囊数组和字段偏移）。
- `native_graph_discovery.cpp:48`：硬编码的 3.8.0 SHA。
- `owned_mmd_body.h:400-451` 和 `game_thread_diagnostics.h:21-31`：硬编码字节特征（诊断用，见 §2.3）。
- `data/runtime_contracts/native_functions_3_8.json` 中同样有这些 RVA。
- 变换字节偏移：`pose_full_body.h:83,127-128` 和 `ww_chain_physics.h:276,307-308` 硬编码 `16/32/36/40`，而同文件和相邻文件使用生成常量 `TransformTranslationOffset` 等。如果布局变化，这两处不会随之更新。

### 5.3 生成器产物冗余

- `desktop/WWPoser/Generated/GameCompat.generated.cs` 与 `runtime/generated/ManagedCompat.generated.cs` 除命名空间外**完全相同**，各 234 个常量。桌面只用到 24 个，托管桥只用到 45 个。建议在 `ww_runtime.json` 中按使用方标注，或只生成一份并通过链接编译共享。
- `owned_mmd_layout.json` 与编译期常量是双通道：原生代码运行时从 JSON 读取 `publish_rva`、`gobjects_rva` 等，又要求 JSON 与编译期 profile 完全一致（`n1_observer.cpp:75-76`）。结果是 C++ 中对应的 8 个生成常量（`FunctionPublishRva`、`GObjectsRva`、`NamePoolRva`、`SlotComponentPublishRva`、`RegisteredGameThread*Rva`、`VtableSkeletalMeshComponentRva` 等）**一个都没被代码使用**。两条通道保留一条即可。
- `PlaybackCommands.generated.h:75-111` 生成 `value>=0&&value<=0` 这样的表达式，生成器可直接输出 `value==0`。

### 5.4 数据、文档和脚本

- `data/` 下约 **1.9 MB** 的研究产物没有任何代码或脚本引用：`csharp_*_contract.json`、`mono_*_contract.json`、`puerts_*_contract.json`、`stage_*_contract.json`、`scene_team_*`（属于退役探针）、`search_*.json/.txt`、`packages*.tsv`、`export_log.tsv`、`round2_missing.json`、`native_pose_empty_channel_contract.json`、`runtime_publication_template.json`、`kawaii_curves.json`、`crosscheck.json`、`bone_mapping_v1.*` 等。其中 `roster_triples.json`、`bones_census.json`、`morph_targets.json`、`runtime_character_meshes.json` 被 `data/characters/index.json` 的 `sources` 作为溯源记录引用，应保留。
- `data/camera_runtime_contract.json`：`source_hashes` 已过期（见 §3.4），却仍被打包并复制进每个会话。
- 失效引用：`tools/mmd_weapon_visibility_audit.md:5` 指向仓库中不存在的 `tools/mmd_weapon_visibility_audit.py` 和两个 JSON；`object_identity.h:50` 注释引用不存在的 `tools/recon_readonly.cpp`；`runtime_evidence.h:2` 自称 “used by the CSharp read-only pipeline”（该管线已退役）。
- `tools/MMD_SQUAD.md`：9 行历史说明，仍被打包（`player_build_inputs.psd1:23`），可并入 `MMD_PLAYER.md`。
- `docs/XXMI_LATE_ATTACH_RESEARCH_PLAN.md`：状态为“待执行”的研究计划，建议标注为研究文档或移出正式 docs。
- `tools/tests/run_*_tests.bat`：12 个脚本只有名字和编译参数不同，而且 `/WX` 用得不一致（`foot_calibration` 和 `hand_mapping` 没有）。生产构建也不一致：`mmd_build_face.bat`、`mmd_build_vmd_cli.bat`、`build_hook_install.bat` 没有 `/WX`。建议改为一个参数化脚本，并统一警告即错误的策略。

---

## 6. 单行过于复杂的代码（D）

整个代码库都采用“单行多语句”的紧凑风格，不建议一次性重排全部文件。下表只列出明显影响阅读、评审或断点调试的行。

| 位置 | 问题 | 建议 |
| --- | --- | --- |
| `desktop/WWPoser/MainWindow.PhysicsEditor.cs:391` | `idle ? preconfigured ? … : groupValue ? … : … : requested >= 0 ? … : …`，四层嵌套三元 | 改为 `if`/`switch` 表达式并命名中间量 |
| `desktop/WWPoser/MainWindow.StageMap.cs:287` | 字符串拼接中嵌套三层三元 | 先计算 `mode` 文本 |
| `desktop/WWPoser/Services/PlaybackControl.cs:15-20` | 6 分支嵌套三元合成一个布尔条件 | `switch` 表；最好由生成器产出（§5.1） |
| `playback/player_transport.h:43-50` | 同上的 C++ 版本，8 行连续嵌套三元 | `switch(kind)` |
| `runtime/owned_mmd/managed/ManagedBridge.cs:747` | `Restored()` 单行 860 字符、19 个 `&&` | 拆成多行，或做成 `Snapshot.Equals` |
| `ManagedBridge.cs:543` | `Capture()` 单行 764 字符的对象初始化 | 每个属性一行 |
| `ManagedBridge.cs:362` | 16 列 TSV 解析塞进一个对象初始化（516 字符） | 每列一行 |
| `ManagedBridge.cs:728,734,736,821-822` | 手写 JSON，每行 9–11 条 `Append` | 抽 `Pointer`/`Field` 辅助方法，每个字段一行 |
| `ManagedBridge.cs:156,167,412,430,469,480,490,635,692` | 300–450 字符的条件或初始化 | 换行 |
| `desktop/WWPoser/Services/NativeGraphBinding.cs:108` | 744 字符的 LINQ 过滤 | 拆出命名谓词 |
| `NativeGraphBinding.cs:133` | 665 字符的 `JsonObject` 初始化 | 分行 |
| `NativeGraphBinding.cs:29-30,52,104,111` | 300–400 字符的 LINQ 和签名拼接 | 拆分 |
| `runtime/owned_mmd/native/owned_mmd_body.h:296-297` | `NativeAuthority` 两行约 600 字符、9 个 `&&` | 分步 `if` |
| `owned_mmd_body.h:642-643,690-694` | 状态汇总的超长布尔表达式 | 命名局部变量 |
| `owned_mmd_body.h:638-656` | `Status()` 中十余行、每行 5–10 个 JSON 字段 | 每字段一行，并顺带清理 §3.4 的重复字段 |
| `playback/native_physics_actor.h:152,162,223-224` | 7–9 个读调用和比较挤在一个 `if` 中 | 分步读取 |
| `native_physics_actor.h:113-114` | 7 条语句一行 | 分行 |
| `probes/v20_recon/unified_player_features.h:286-289` | 一行构造 patch，含 4 个 `memcpy` 和一个 `if` | 抽 `MakePatch` |
| `unified_player_features.h:386,426` | 3–4 个 `memcpy` 同行 | 分行 |
| `tools/mmd_camera_compile.cpp:18` | 13 条语句写文件头 | 每个字段一行，或用结构体写头 |
| `mmd_camera_compile.cpp:21,27` | 8 条和 7 条语句一行 | 分行 |
| `engine_ue4/pose_full_body.h:66` | `if(i?parents[i]<0\|\|...:parents[i]!=-1)`，条件中嵌套三元 | 拆成两个条件 |
| `pose_full_body.h:104,107` | `for(...)a=…;b=Unit(…)`，第二条语句看起来像在循环体内，实际不在 | 加花括号并分行，**容易误读** |
| `playback/ww_chain_physics.h:276` | `k==0?x:k==1?y:z` 嵌套三元 | 用数组下标或 `V` 的访问器 |
| `ww_chain_physics.h:304` | 6 条语句一行 | 分行 |
| `engine_ue4/pose_subtree_rotation.h:47-48` | `Rotation`、`Position` 单行重复 4 次偏移运算（325 字符） | 循环 |
| `probes/v20_recon/game_compat_resolution.h:21` | `HexDigit` 三层嵌套三元 | 小问题，可接受 |
| `desktop/WWPoser/Services/BookmarkService.cs:14` | 376 字符的 LINQ，含两组 `??` 回退 | 拆分（旧格式清理后自然简化） |
| `runtime/owned_mmd/native/n1_installer.cpp:20` | 三个 `if` 写在同一行 | 分行 |
| `core_math/include/poser_math/mmd_motion.h:222` | 声明加调用写在一行，下方的 `uint8_t(curve[j])` 冗余转换 | 分行 |
| `tools/build_player.ps1`（尤其是 `:89` 的 10 条语句，`:50-56`） | 整个脚本几乎每行多条语句 | 交给 PSScriptAnalyzer 格式化 |
| `tools/validate_owned_character_catalog.ps1:45`、`validate_character_forms.ps1:48` | 每行 7–10 条语句 | 同上 |

---

## 7. 建议的清理顺序

| 阶段 | 内容 | 风险 | 预计删减 |
| --- | --- | --- | --- |
| **1. 零风险删除** | §3.1、§3.2、§3.3；`observe_kawaii_settings_only` 研究模式；`camera_runtime_contract.json` 的复制和配置键；`OwnedMmdPresentation.cs`；`build_player.ps1` 的 `session_usage` 拷贝 | 低 | 约 400 行 |
| **2. 退役探针出库** | §2.1 的源码和脚本移到 research 分支；同时删除 §2.2 中只服务它们的 API（`world_snapshot` 的 `Capture` 系列、`publish_observer::Observe`、`DecodeCommand`/`Drain`/…、`native_physics_actor` 的 `external_*` 和 `read_snapshot`）；修改 `unified_transport_tests.cpp:103-110` 并更新 BUILDING.md | 低–中 | 约 2,600 行 + 6 个脚本 |
| **3. 旧格式兼容下线** | §4：先把测试夹具从 `player_status.json` 单人格式和 8 列 TSV 迁到新格式，再删除原生、托管和桌面三端的旧分支；统一 `bone_names`/`bone_parents` 的键名 | 中（需要回归） | 约 300 行 |
| **4. 去重合并** | §5.1：先做 FName/GObjects/`Require`/`Pointer`/TArray 这类机械合并；再合并观察器脚手架和场景隐藏；控制命令约束交给生成器 | 中 | 约 500 行 |
| **5. 常量归口** | §5.2 和 §5.3：Kawaii 的 RVA 和偏移进 `ww_runtime.json`；拆分 C# 生成产物；布局 JSON 与编译期常量二选一 | 中 | — |
| **6. 输出瘦身** | §3.4：删除重复和常量状态字段，同时更新桌面端的 `??` 回退 | 低（先确认排障流程不依赖这些字段） | 约 100 行 |
| **7. 可读性** | §6 中列出的行；去掉 `/wd4505`，让编译器帮忙找死函数 | 低 | — |

---

## 附录 A：include 闭包分类（生产代码中仅被测试或退役代码引用的头文件）

- 只被**测试和退役代码**引用（不进入任何生产产物）：`playback/ww_mmd_squad.h`。
- 只被**退役代码**引用：`playback/ww_prop_visibility.h`、`playback/ww_stage_residency.h`，以及 §2.1 列出的全部 `probes/v20_recon/*` 文件。
- 生产闭包中引用计数为 0 的生成常量见 §5.3。

## 附录 B：未提交改动审查（CP932 截断名）

改动文件：`mmd_motion.h`、`vmd_clip.h`、`mmd_compile_common.h`、三个编译器，以及 `unified_player_features_tests.cpp`。

- 拆成 `names` 和 `fullNames` 两个缓存是**必要的**：新增测试的第二段说明了同一原始字节在宽度不同的字段中需要走不同的解码策略，不能共享缓存。
- `ReadVmd(..., const Decoder& decodeFullName = {})` 和 `vmd::ReadFile(..., full_name_decoder = {})` 的默认值，以及 `decodeFullName &&` 空检查，目前**没有调用方使用**（`mmd_vmd_cli.cpp:23`、`mmd_face_compile.cpp:24,27,29`、`mmd_camera_compile.cpp:14`、测试 `:471` 都显式传了两个解码器），可以去掉默认值，让签名强制传入。
- `Decode`、`DecodeFullVmdName` 只差一个布尔参数，可以考虑让 `Decoder` 签名直接带上“是否满宽”的标志，把两个 `std::function` 合并为一个。
- 新测试 `VmdTrackNameTest` 测的是 `core_math` 的 VMD 解析，却放在 `unified_player_features_tests.cpp` 中，并为此引入了 Windows 专用的 `mmd_compile_common.h`。建议移到独立的 VMD 解析测试，或放进 `local_clip_tests.cpp` 一类的纯 C++ 测试，也方便 CI 的 Linux sanitizer 任务覆盖（需要为解码器提供一个可移植的桩）。

---

## 附录 C：执行记录（2026-11-17，未提交）

按 §7 七个阶段全部执行。离线回归、全部 `/WX` 原生构建、13 个原生测试、托管与 UI 检查均通过；**未做游戏内验证**。与建议不同或需要知道的决定：

- **诊断随正式 DLL 移除**（§2.3）：参考骨架候选扫描、硬编码字节特征、`reference_skeleton_diagnostic.json`、`game_thread_diagnostics.h` 已删除。下次版本更新如需要，可从 `f00725c` 恢复。`worker_result.game_thread_check` 仍保留，改用生成常量。
- **退役探针直接出库**（§2.1），未建 research 分支；源码可从 v0.2.9 之前的历史恢复。
- **运行包从 4 个文件减为 3 个**：`owned_mmd_layout.json` 删除，地址只走编译期常量；manifest `purpose` 改为 `owned_mmd_scene`，旧运行包需要重建。
- **兼容生成物合并为两份**：`ww_runtime.json` 分为 `native`/`managed`/`shared` 三区，生成 `runtime/generated/GameCompat.generated.{h,cs}`，桌面与托管桥链接同一份 C#。Kawaii、胶囊、重力相关的 RVA 和偏移都已进入生成器。生成器去掉未使用的 `f32`/`u8`/`u16` 类型和 `runtime_layout`、`evidence` 区。
- **旧格式全部下线**（§4）：`player_status.json`、`stage_*`、8 列 `actors.tsv`、单人准备目录、`--owned-mmd`、WWBOOKMARK1/2、旧宿主命令确认、schema 3 便携包清单、版本化见证缺失时的回退。CLI `--prepare-owned-mmd` 现在生成单演员场景。场景预设（`.wwpreset`）中旧的 `source`/`stageSession`/`recording` 字段仍可读取——这是用户文件，未下线。
- **双重时长限制保留**（§5.1）：传输时钟只在网格发布回调里推进，所有演员都不发布时（被剔除或隐藏）只有 worker 墙钟能结束会话。已在 `CheckTimeLimit` 上注释原因。
- **控制命令目标规则**进入 `control_commands.json` 的 `target` 字段，两端生成同一套校验。会话级和场景级命令现在拒绝非零演员槽位。
- **CP932 改动**（附录 B）：两个解码器合并为 `Decode(bytes, codepage, fullWidth)`，默认参数去掉；VMD 名称测试移到 `tools/tests/vmd_name_tests.cpp`。它依赖 Windows 代码页转换，仍只在 Windows 运行。
- **数据**：§5.4 列出的研究产物，以及 `data/runtime_contracts/` 中除 `native_functions_3_8.json` 外的全部文件、`PROGRESS.md`、`COVERAGE.md`、`bone_chains.md`、`tools/MMD_SQUAD.md`（已并入 `MMD_PLAYER.md`）、`tools/mmd_weapon_visibility_audit.md` 已删除。`data/characters/index.json` 引用的溯源文件保留。
