# Wuwa Poser 鸣潮 3.8.0 适配复盘

2026 年 11 月 14 日，Wuwa Poser v0.2.8 正式发布。本次适配涉及游戏线程、骨架、动画图和镜头缓存。最早的候选版在演员加载前被游戏线程校验拦下，后续版本陆续修复骨架读取和原生物理。VMD 文件一度能加载，游戏画面却仍显示原来的镜头。正式版也修复了这处故障。

## 游戏线程与托管组件

**现象。** 启动时提示 `selected window thread is not registered GameThread`，播放器退出并完成恢复。

**定位与修复。** 旧版写死的线程登记字段在 3.8.0 中已经搬移。三个带诊断文本的检查点都指向新版的线程 ID `0x9698480` 和初始化标记 `0x9698490`。实机读到初始化标记为 1，登记 ID 与窗口线程一致。更新统一兼容配置后，演员可以正常加载。会话日志中的 `worker_result.game_thread_check` 会记录读取值和失败原因，便于区分字段读不到、初始化标记异常和线程 ID 不匹配。

**换包后的另一种启动失败。** 鸣潮进程会保留已加载的 Mono 托管组件。兼容配置更新后，旧组件会因配置摘要不同而拒绝启动，有一轮只显示了通用的 `owned helper raised managed exception`。现在握手失败会明确提示重启鸣潮。离线比对也确认了托管桥的 33 项反射绑定仍能解析，因此保留托管 API，使用 3.8.0 的引用重新构建。[运行配置](../data/game_compat/ww_runtime.json) · [兼容说明](GAME_COMPATIBILITY.md)

## 骨架读取与新角色数据

**现象。** 游戏线程检查通过后，演员能够出现，但播放前报 `actor reference skeleton unreadable` 或 `actor reference skeleton differs`。

**定位与修复。** 用实机角色的网格和骨架资产确认了骨架指针字段从 `0xd8` 移到 `0xe8`。候选数组中有两份数据能匹配同一套骨名和父索引，最终用游戏自身的骨名读取函数确认参考数组位于 `0x3c8`，旧值为 `0x3b8`。同一阶段交叉核对了对象表、名字池、动画发布入口、引擎帧计数以及 Morph 目标数组。帧计数还通过实机采样确认会递增。

| 角色 | 实机差异 | 修复办法 |
| --- | --- | --- |
| 莉莉 | 367 根骨骼和父索引一致，其中 7 个面部骨名只有 ASCII 字母大小写差异 | 校验时忽略骨名的 ASCII 大小写，仍检查骨骼顺序、父索引和数字后缀 |
| 绯雪「月沉霜」 | 同为 367 根骨骼，205 项索引位置与资产导出顺序不同，按骨名核对后父子关系一致 | 按实机网格顺序重排骨名、父索引和参考姿势。姿势按骨名对应，再更新角色与动画图索引的摘要 |

离线核对覆盖 65 个角色配置和 82 个角色与皮肤组合的模型及 UI 动画类映射，另有 4 个独立形态选项。莉莉与绯雪「月沉霜」是本次新增并完成原生物理实机复测的角色。[角色数据说明](CHARACTER_FORMS.md) · [角色索引](../data/characters/index.json)

## 原生物理与关联动画实例

**现象。** 莉莉关闭原生物理后可以播放并恢复，开启后卡在身份登记和动画图准备。最初有 4 个旧版动画函数未通过字节校验。更新入口后，主动画实例能够完成登记，但莉莉的关联动画实例仍触发身份校验失败。

**定位与修复。** 用虚表槽位关系和函数展开记录定位 3 张虚表，再在实机核对 18 个槽位、采集 14 个原生物理相关函数的完整字节。运行时按游戏版本摘要选择函数证据，并在使用前比对实际字节。根据实机指令更新动画代理里的网格、骨架、帧号和计数器字段。对主实例，交叉核对网格、骨架与当前主动画实例。对关联实例，核对其所属网格，再重验同一会话中从主实例观察到的代理。这样可以识别莉莉的主动画实例 `ABP_Performance_Lili_PC_C` 和关联实例 `ABP_Performance_Lili_C`。[3.8.0 原生函数证据](../data/runtime_contracts/native_functions_3_8.json) · [动画图校验实现](../probes/v20_recon/native_graph_discovery.h)

## VMD 镜头加载后画面没有跟随

**现象。** 用户的会话里 `camera_mode=2`，镜头文件已加载，`camera_failed=false`，但画面没有跟随 VMD 轨道。前一轮验收记录了入口和错误状态，漏查了画面实际读取的缓存。

**定位与修复。** 实机核对 `PlayerCameraManager` 的当前帧、上一帧 POV 读取函数，以及引擎写入缓存的函数。两份缓存基址分别从 `0x2e40`、`0x38e0` 改为 `0x2f00`、`0x39d0`。播放器改为写入新版使用的位置，并新增 `camera_applied_frames` 记录镜头应用帧数。用用户原 VMD 复跑 8.079 秒，日志记录应用 184 帧，双人原生物理继续运行，停止后恢复通过。画面构图和轨道效果以实机画面观察为准。[镜头兼容配置](../data/game_compat/ww_runtime.json#L591) · [会话状态实现](../runtime/owned_mmd/native/owned_mmd_body.h#L489)

## 后台复测与正式发布

- **CLI 复测。** 复用准备目录，给会话设定有限播放时长，运行后核对停止和恢复。游戏保持前台即可从后台发起。另修复了单演员准备目录的槽位类型错误，使单人 CLI 路径能正确生成会话。
- **排除启动环境干扰。** 一轮图形界面测试只留下 `installer_result.json`，其中 `stage=hook_failed`、`last_error=126`，没有身份登记报告。改用较短的 CLI 准备目录后，同一修复版成功完成身份登记与播放。该次错误的具体触发条件未单独复现。CLI 在游戏未处于前台时也曾超时，切回游戏后才进入运行验证。站位预览能验证演员加载、摆放和恢复，原生物理和镜头要通过正式播放入口验证。
- **原生物理实机。** 莉莉与绯雪「月沉霜」各完成一次单人测试。交付目录的 6.032 秒双人复测中，两位演员各收到 169 帧原生输入，分别观察到 13 组和 11 组物理节点生效。两位演员都通过函数证据校验，停止后恢复通过。
- **正式包。** 从干净提交 `dc0e2a524e68dc5a354bf8c45bb2db99b19b2151` 重建 Windows x64 便携包，解包后逐一核对 272 个清单文件。GitHub Windows 与 portable 两项 CI 通过，便携包、源码包和校验文件均核对了上传后的 SHA-256。本机的符号链接回归用例因 Windows 权限跳过。桌面快捷方式已切到正式包。[v0.2.8 发布页](https://github.com/AKRTR465/Wuwa-Poser/releases/tag/v0.2.8) · [CI 记录](https://github.com/AKRTR465/Wuwa-Poser/actions/runs/43486235960)

## 兼容配置字段完整差异

基线是 v0.2.7 的 `c2b0d5d`，新值来自 v0.2.8 的 `dc0e2a5`。统一配置共修改 42 个 `native` 字段，其中 28 个 `u64` RVA、11 个 `u32` 布局值、3 个 SHA-256 字符串，另更新顶层 `profile_id`。`managed` 的 49 项配置值及 `runtime_layout`、`evidence` 的引用定义在两个提交间一致。RVA 是相对游戏主模块的地址，结构偏移相对其所属对象，`AnimProxyObservationBytes` 表示读取范围长度。下列地址只对应表中记录的游戏构建。[统一配置差异](https://github.com/AKRTR465/Wuwa-Poser/compare/c2b0d5d...dc0e2a5)

### 游戏线程与对象表

| 字段 | v0.2.7 | v0.2.8 | 用途与核对依据 |
| --- | --- | --- | --- |
| `RegisteredGameThreadIdRva` | `0x992f300` | `0x9698480` | 三处诊断文本交叉定位，实机 ID 与窗口线程匹配 |
| `RegisteredGameThreadInitializedRva` | `0x992f310` | `0x9698490` | 与登记 ID 同批定位，实机标记为 1 |
| `EngineFrameRva` | `0x992f2d8` | `0x9698458` | 四处指令引用一致，实机采样递增 |
| `GObjectsRva` | `0x99a3ff0` | `0x970d150` | UObject 对象表，通过对象索引和名称交叉验证 |
| `NamePoolRva` | `0x9967340` | `0x96d04c0` | FName 名字池，通过对象名称交叉验证 |
| `ObjectArrayRva` | `0x99a3fe0` | `0x970d140` | `FUObjectArray` 基址，由实测 `GObjects` 与结构关系推得，间距为 `0x10` |

### 骨架与动画发布和表情

| 字段 | v0.2.7 | v0.2.8 | 用途与核对依据 |
| --- | --- | --- | --- |
| `AssetSkeletonOffset` | `0xd8` | `0xe8` | 网格资产的骨架对象指针，与托管侧导出对象核对 |
| `AssetRefBoneInfoOffset` | `0x3b8` | `0x3c8` | 参考骨架数组，通过游戏骨名读取函数及完整骨名、父索引核对 |
| `VtableSkeletalMeshComponentRva` | `0x843eb78` | `0x818c708` | 实机骨骼网格组件虚表 |
| `SlotComponentPublishRva` | `0x843f630` | `0x818d1c0` | 虚表中发布槽位的地址，虚表内偏移为 `0xab8` |
| `FunctionPublishRva` | `0x4ff3cd0` | `0x4cfc880` | 动画发布函数，通过函数签名和组件槽位核对 |
| `AssetMorphArrayOffset` | `0x368` | `0x378` | 网格资产的 Morph 目标数组，实机目标名单与角色配置匹配 |
| `FunctionSetMorphTargetRva` | `0x5009b50` | `0x4d13810` | 设置 Morph 权重的入口，签名及其对组件权重表的引用匹配 |

实机还复核了 `ComponentAssetOffset=0x5d0`、`ComponentMainAnimOffset=0x910`、`ComponentMorphMapOffset=0xef0` 和 `SlotComponentPublish=0xab8`。参考骨条目仍为 `0x10` 字节，父索引在条目内的 `0xc`。发布槽位的 RVA 与虚表内的相对偏移是两个不同字段。[骨架只读诊断实现](../runtime/owned_mmd/native/owned_mmd_body.h)

### 原生物理虚表与动画代理

| 字段 | v0.2.7 | v0.2.8 | 用途 |
| --- | --- | --- | --- |
| `VtableLocalToComponentRva` | `0x83ec178` | `0x813b708` | 局部姿势到组件空间的转换节点虚表 |
| `VtableKawaiiPhysicsGroupRva` | `0x7405e28` | `0x71c8828` | Kawaii 组节点虚表 |
| `VtableKawaiiPhysicsSingleRva` | `0x7405cf0` | `0x71c86f0` | Kawaii 单节点虚表 |
| `AnimProxySkeletonOffset` | `0xb8` | `0xc0` | 动画代理的骨架指针 |
| `AnimProxyMeshOffset` | `0xc0` | `0xc8` | 动画代理的网格组件指针 |
| `AnimProxyFrameOffset` | `0x528` | `0x530` | 动画代理求值帧字段 |
| `AnimProxyCounterOffset` | `0x530` | `0x538` | 动画代理求值计数器 |
| `AnimProxyObservationBytes` | `0x538` | `0x540` | 观察器读取范围，覆盖新版计数器 |

`AnimProxyOwnerOffset=0xa0`、`AnimProxyOwnerAltOffset=0xa8`、`AnimProxyMainProxyOffset=0xd0` 和 `AnimProxyBoneContainerOffset=0x5a0` 的配置值沿用。实际修复点是主代理的判定方式。主实例直接验证自己的代理。关联实例优先使用同会话中已观察到的主代理，再核对网格、骨架和实例归属。缺少这份观察记录时，仍检查配置的反向指针关系。[主代理校验](../probes/v20_recon/native_graph_discovery.h#L32)

### 原生物理与身份登记函数

新版函数证据的顶层字段为 `schema`、`source_sha256`、`functions`。下表的 14 个函数各有 `label`、`rva`、`bytes`、`sha256` 和 `function_bytes_hex`。字节数用于发布版的完整代码比对。[新版函数证据](../data/runtime_contracts/native_functions_3_8.json)

| 配置字段 | v0.2.7 | v0.2.8 | 字节数 |
| --- | --- | --- | ---: |
| `FunctionLocalToComponentRva` | `0x4e0ba10` | `0x4b14450` | 542 |
| `FunctionSharedSkeletalControlEvaluateCSRva` | `0x49a34d0` | `0x46a8910` | 253 |
| `FunctionSharedSkeletalControlUpdateRva` | `0x49dd440` | `0x46e2880` | 461 |
| `FunctionGroupEvaluateRva` | `0xa3db40` | `0xa567f0` | 427 |
| `FunctionGroupInitializeBoneReferencesRva` | `0xa40710` | `0xa593c0` | 315 |
| `FunctionGroupInitializeModifyBonesRva` | `0xa3fc60` | `0xa58910` | 2458 |
| `FunctionComponentSpaceTransformGetterRva` | `0xa3f270` | `0xa57f20` | 194 |
| `FunctionAnimInstanceRootEvaluationCounterRva` | `0x4dedaa0` | `0x4af5f30` | 1777 |
| `FunctionCachedPoseEvaluationCounterRva` | `0x4e0d850` | `0x4b16290` | 606 |
| `FunctionSkeletalControlLODAllowedRva` | `0x4e1abc0` | `0x4b23790` | 166 |
| `FunctionGroupIsValidToEvaluateRva` | `0xa40a50` | `0xa59700` | 239 |
| `FunctionSharedUpdateInternalRva` | `0xa44ae0` | `0xa5d4d0` | 78 |
| `FunctionAllocateSerialNumberRva` | `0x33f11d0` | `0x30f3e80` | 169 |
| `FunctionAssignWeakObjectRva` | `0x3404280` | `0x3106f30` | 59 |

### 镜头入口与缓存

| 字段 | v0.2.7 | v0.2.8 | 用途 |
| --- | --- | --- | --- |
| `FunctionUpdateCameraRva` | `0x542faa0` | `0x51d6560` | 游戏镜头更新入口，实机虚表槽位为 `0x790` |
| `CameraViewTargetOffset` | `0x1880` | `0x18e0` | 镜头管理器的视点目标 |
| `CameraCacheOffset` | `0x2e40` | `0x2f00` | 当前帧缓存基址，POV 位于 `0x2f10` |
| `CameraLastCacheOffset` | `0x38e0` | `0x39d0` | 上一帧缓存基址，POV 位于 `0x39e0` |

`CameraPovOffset=0x10`，位置、旋转和 FOV 在 POV 内的偏移分别为 `0x0`、`0xc` 和 `0x18`。这次分别核对了更新入口和两份缓存的读取位置，因而能解释更新回调已运行、画面仍没变的现象。[镜头运行逻辑](../playback/ww_camera_runtime.h)

### 配置标识与完整摘要

| 字段 | v0.2.7 | v0.2.8 |
| --- | --- | --- |
| `profile_id` | `wuwa-current-layout-v1` | `wuwa-3.8.0-physics-check-v1` |
| `SourceBuildSha256` | `c4956c30be531f3b992eabe42fa92fa9d10186000fe12e7546a11e1179bdac18` | `38b3980f0436d8ead42a0ff8c45c183936ed02cd8b4b33944a03316b68cfd887` |
| `FunctionAllocateSerialNumberSha256` | `d061ffb1798bb5262881a0842045a842350288a0a34a07728bb43bf0c70a106e` | `e9a45314af4ec25ab59d048ee1fda1a307f814817b3f1edbe19ecc792d5abd1f` |
| `FunctionAssignWeakObjectSha256` | `165872ef14eebb05c725a39e381bf60d31d5b8e5beb16cc4f33627eff3eb2b1b` | `5e11a1ff463285758fb363bf152428b82239081102613c8af31593e818486139` |
| 生成物中的 `SourceSha256` | `44668b9210086a485c32715fd099e5e4c7a2d242abd5ff2b6d7afa508d88ccd8` | `8cf1df3c896312977a37147ff8a12d50525c325596086d8654106baf0ffce32f` |

`SourceBuildSha256` 对应游戏磁盘 EXE，`SourceSha256` 对应 `ww_runtime.json` 的原始文件字节。后者用于检测 Poser 的桌面程序、托管组件和原生组件是否来自同一份配置。`runtime_layout` 保留 `@字段` 引用，运行时 JSON 通过生成器写入新值。四份生成物是 `desktop/WWPoser/Generated/GameCompat.generated.cs`、`runtime/generated/GameCompat.generated.h`、`runtime/generated/ManagedCompat.generated.cs` 和 `runtime/owned_mmd/native/owned_mmd_layout.json`；2026-11-17 起 `runtime_layout` 已删除，生成物合并为 C++ 和 C# 各一份。[兼容定义与生成方法](GAME_COMPATIBILITY.md)

## 角色与资源字段

| 数据项 | 莉莉 | 绯雪「月沉霜」 |
| --- | --- | --- |
| 选择 ID | `role1512-skin81001512` | `role1108-skin81011108` |
| `role_id` / `skin_id` | `1512` / `81001512` | `1108` / `81011108` |
| 目录中的 `model_id` | `100146` | `1000661` |
| 网格与物理配置模型名 | `R2T1LiliMd10011` | `R2T1TaidaonvMd20011` |
| `profile_id` | `femalems--lili--r2t1lilimd10011` | `femalexl--taidaonv--r2t1taidaonvmd20011` |
| `skeleton_asset_name` | `R2T1LiliMd10011_Skeleton` | `R2T1TaidaonvMd20011_Skeleton` |
| UI 主动画类 | `ABP_Performance_Lili_PC_C` | `ABP_Performance_Feixue_PC_C` |
| `bone_count` / `morph_count` | 367 / 176 | 367 / 161 |
| Kawaii 组数 / 曲线输入组数 | 13 / 12 | 11 / 8 |
| 搜索词 | `li li`、`lily` | `yue chen shuang` |

角色目录从 64 个配置、80 个角色与皮肤组合增至 65、82 个，默认皮肤从 64 增至 65 个，额外皮肤从 16 增至 17 个。加上 4 个独立形态，共 86 个选择。角色索引从 74 增至 76 个 profile，主骨架从 64 增至 66 份。动画图索引从 74 增至 76 条，衣物增强骨链数据从 83 增至 85 条。目录和来源证书更新了新角色的映射、计数和关联摘要。[角色目录](../data/characters/owned_character_catalog.json) · [动画图索引](../data/native_graphs/index.json) · [骨链数据](../data/bone_chains.json)

两份角色 profile 各自记录 `bone_names`、`bone_parents`、`morph_names`、网格和骨架来源。绯雪的 `sources.skeleton_sha256` 指向原始导出的骨架，顶层 `skeleton_sha256` 对应按实机网格顺序重排后的打包骨架。`sources.bone_order` 记录这次重排的依据。[莉莉 profile](../data/characters/profiles/femalems--lili--r2t1lilimd10011.json) · [绯雪 profile](../data/characters/profiles/femalexl--taidaonv--r2t1taidaonvmd20011.json)

## 配套代码与会话字段

| 修改位置 | 本次变化 | 用于定位或验收的字段 |
| --- | --- | --- |
| 游戏线程检查与桌面启动提示 | 加入只读地址候选诊断，正式播放和站位预览共用启动失败翻译 | `worker_result.game_thread_check.status`、`window_thread_id`、`registered_thread_id`、`initialized_flag`、`relocation` |
| 托管兼容握手 | 兼容配置摘要不同会报告需要重启游戏 | `compatibility_profile`、`compatibility_source_sha256` |
| 骨架诊断与骨名比较 | 记录候选数组、骨名读取函数所用偏移、配置读取结果及骨骼差异，播放和图观察使用相同的 ASCII 大小写比较 | `reference_skeleton_diagnostic.json` 中的 `reference_candidates`、`accessor_reference_offset`、`configured_count`、`bone_differences` |
| 原生函数证据选择 | 按游戏构建摘要选择 3.8.0 的证据，身份登记契约也使用当前证据，报告的构建摘要读取统一配置 | `source_sha256`、`function_bytes_hex`、`checks_performed` |
| 身份登记与图观察 | 记录失败时的回调归属。图观察保留已确认的主代理，并在关联实例回调中复核 | `callback_context_diagnostic`、`owner_role_verified`、`main_proxy` |
| 镜头状态 | 从镜头回调计数发布 `camera_applied_frames` | 联合查看 `camera_mode`、`camera_clip_available`、`camera_applied_frames` 和 `camera_failed` |
| 单演员 CLI 兼容 | 旧版单演员准备目录转换为多演员会话时，以 `UInt32` 写入 `slot` 和 `target_slot`，修复后续读取类型冲突 | `slot=1`、`target_slot=1` |

离线回归覆盖线程定位和错误提示、函数证据版本选择、主代理身份及错误骨架拒绝、角色搜索、单演员 CLI 转换、镜头轨道位置与恢复。镜头测试的合成缓存采用生成后的布局值，实机的缓存读写函数证据独立确认了这些值。[离线测试入口](../tools/run_offline_checks.ps1) · [桌面启动与 CLI 兼容实现](../desktop/WWPoser/Services/OwnedMmdPlaybackLaunch.cs)

### 排查时读取的会话报告

| 报告 | 可以确认的内容 |
| --- | --- |
| `session_result.json` | 启动或结束原因、游戏线程检查结果、`mmd_restoration_verified` |
| `game_compatibility.json` | 对象表和名字池通过交叉验证时使用的实际 RVA |
| `reference_skeleton_diagnostic.json` | 参考骨架数组候选、骨名读取函数确认的偏移、逐骨差异和帧计数采样（2026-11-17 起不再生成；需要时从 `f00725c` 恢复该诊断） |
| `native_identity_bootstrap.json` | 主实例和关联实例的登记结果、失败回调的归属和槽位证据 |
| `native_graph_discovery.json` | 本会话采集到的动画图、回调来源和骨骼映射 |
| `native_function_checks.json` | 每位演员进入正式原生物理前的函数证据匹配情况 |
| `mmd_status.json` / `mmd_result.json` | 轨道模式、镜头应用帧数、各演员原生输入帧数和物理组激活统计 |

## 下次更新的复用顺序

1. **先做离线核对。** 比对资源目录、角色与皮肤映射、托管绑定和兼容配置。通过 `tools/generate_game_compat.ps1` 同步生成文件，运行离线回归。
2. **分阶段做实机测试。** 依次验证线程登记、演员加载、骨架与直接播放、单人原生物理、双人原生物理。每次结束后查恢复记录。更新兼容配置后重启游戏，避免 Mono 沿用旧组件。
3. **用有明显运动的 VMD 验收镜头。** 核对轨道模式、应用帧数，并观察位置、朝向、视野变化和停止后的镜头恢复。这样可以发现仅看启动状态时遗漏的画面故障。
4. **用正式产物收尾。** 绑定源码提交，核对正式便携目录和解包文件，确认上传附件摘要后再更新桌面快捷方式。[构建与校验说明](BUILDING.md)
