# 游戏兼容定义与动态发现

## 维护入口

`data/game_compat/ww_runtime.json` 是默认桌面播放器的游戏兼容常量维护源。它包含原生地址、字段与容器布局、函数长度、托管绑定名称及业务状态值。角色骨架、Morph 和原生物理组签名仍由各角色 profile 管理。

修改维护源后运行：

```powershell
./tools/generate_game_compat.ps1
./tools/generate_game_compat.ps1 -Check
```

生成器输出四个文件，不要手工编辑生成物：

- `runtime/generated/GameCompat.generated.h`
- `runtime/generated/ManagedCompat.generated.cs`
- `desktop/WWPoser/Generated/GameCompat.generated.cs`
- `runtime/owned_mmd/native/owned_mmd_layout.json`

`native` 和 `managed` 使用唯一的语义名称；每个条目具有 `type` 和 `value`。支持无符号整数、`i32`、`f32`、`string`、`wstr`，以及从无符号条目派生文本 RVA 的 `hexref`。`runtime_layout` 通过 `@常量名` 引用同一条目，避免部署 JSON 再维护一套数值。

生成物携带配置标识和源文件摘要。此摘要用于发现本工程各 DLL、托管代码、观察器与部署布局之间的配置混用；它不代表游戏内存 ABI 已验证。构建会生成定义，校验和离线 CI 会拒绝过期生成物。改配置后需要重新编译相关模块；直接改变游戏 API 签名或原生结构语义仍需要修改适配代码。

原生入口会在取得演员之前与实际已加载的托管助手握手。Mono 保留的旧助手缺少握手方法、或者其编译来源不同，都会拒绝开始；需要重启游戏以加载新助手。普通打包也要求当前 schema 2 manifest，旧运行时需要重建。

## 已接入的动态发现

正式播放初始化时先对配置的 GObjects/名字池地址进行结构和名称交叉验证。若验证失败，则使用现有只读扫描器搜索主映像可读写区域，要求恰好一组互相一致的候选，再次验证后使用。扫描不会写游戏或调用待识别函数。

解析后的实际 RVA 会写入各演员的 `native_target.json`，继续传给身份登记和原生图观察器。序列号分配器使用的对象数组基址由实际 GObjects 地址及已知结构关系计算，不再退回默认全局地址。

每演员的实例地址、converter/group 节点相对位置和 compact 骨骼映射仍从本次会话获得。跨进程对象指针不会作为固定布局复用。扫描器仍依赖所支持的 UObject/FName 基础编码，不能自动适配任意对象结构变化。

## 原生函数验证与诊断候选

身份登记、图观察和正式原生物理准备会消费其需要的函数字节证据。在安装相关 hook 或调用相关函数之前，核对配置来源、完整字节长度、映像边界和实际代码；不再仅因存在 witness 字段就声称布局已验证。

`data/runtime_contracts` 和 `data/native_identity_registration_contract.json` 保留历史分析证据。C# 按函数语义 label 提取一致的字节证据，并使用统一配置中的执行 RVA。身份登记契约在会话目录中生成部署副本，原始档案不变。

字节不符时，解析器可在 PE 可执行节中搜索精确字节或显式提供的位掩码签名。零结果、多个结果、越界或未完成的读取均不能构成唯一候选。即使找到唯一位置，也只写入诊断，标为需要审查，不能自动授权调用或换 hook。函数位置相同不证明 ABI 或动画阶段相同。

运行报告：

- 会话根的 `game_compatibility.json`：全局锚点来源、实际 RVA、交叉验证结果；只代表锚点验证范围。
- 身份/图观察结果的 `checks_performed`：实际执行的配置来源和代码检查。
- 各演员原生输入目录的 `native_function_checks.json`：正式原生物理函数检查与失败原因。

## 更新后的工作流程

1. 先进行只读发现，保留候选和分析证据。
2. 检查对象布局、函数签名、线程和动画阶段；确认变化后更新统一配置或适配器。
3. 按语义函数更新字节证据，重新生成并构建。
4. 运行离线回归，再单独进行获授权的游戏内画面验收。

把 `FTransform` 大小或某个 offset 改成新值，不会自动改变算法的封送和执行阶段。表情的游戏封装 API 已进行编译可行性检查，本次保留原有表情调用时序。未反射的 AnimProxy/pose/Kawaii 内部布局仍需要分析；当前没有宣称这些字段可以任意跨版本自动求解。

## 验证

```powershell
./tools/tests/game_compat_generation_checks.ps1
./tools/tests/run_game_compat_resolution_tests.bat
./tools/tests/run_native_function_witness_tests.bat
./tools/tests/run_game_object_anchors_tests.bat
./tools/run_offline_checks.ps1
./tools/build_owned_mmd.ps1
```

这些合成测试和编译不启动或注入游戏。`run_offline_checks.ps1` 可通过 `-DotNetExecutable` 指定有 SDK 的 dotnet 路径。现有便携运行时仍部署四个必需文件及其 manifest，兼容来源随 manifest 记录。

旧的单人/编队/采样开发工具仍有部分独立版本常量。默认链路使用的共享实现已迁移；不能据此把所有历史工具都认定为完成动态适配。

## XXMI 晚附加预调研

场景内启用 WWMI 衣服还需要处理既存 DX11 对象、GPU 资源身份、Shader 和资源扩容。实验范围、步骤和验收要求见 [XXMI 晚附加衣服加载预调研方案](XXMI_LATE_ATTACH_RESEARCH_PLAN.md)。
