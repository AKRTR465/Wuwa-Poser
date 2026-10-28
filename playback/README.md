# 播放控制组件

`player_transport.h` 提供时间轴和预分配 SPSC 控制队列，支持播放、暂停、seek、逐帧、循环、0.1–4 倍速、自然结束保持末帧及停止恢复请求。VMD 加载与游戏状态修改由播放器后端处理。clip duration 使用秒，VMD source frame 为 `seconds * 30`。

```cpp
wwp_playback::Transport transport;
wwp_playback::SpscCommandQueue<64> commands;
transport.SetClip(clipEpoch, clipDurationSeconds); // worker准备完成、此前已恢复后，在游戏线程发布。
// Worker: DecodeCommand(wire, &command); commands.Push(command);
// 游戏动画结束回调：
transport.Drain(commands, monotonicSeconds);
transport.Tick(monotonicSeconds);
const auto& state = transport.State();
// state.seconds传给已准备好的采样/姿态缓存；暂停也必须继续应用当前帧。
// state.restore_requested 时由后端真正恢复，然后 transport.AcknowledgeRestore()。
```

控制帧固定为 64 字节 little-endian，C# `PlaybackControl.Encode` 与 C++ `DecodeCommand` 按字节序列化。字段：0–7 magic `WWPCTL1\0`、8 version=1、12 length=64、16 clip epoch u64、24 sequence u64、32 command u32、36 target slot u32、40 value IEEE754 binary64、60 FNV-1a checksum；48–59 保留为 0。命令编号、参数类型、单位、范围与作用域集中在 [`control_commands.json`](control_commands.json)，由 `tools/generate_playback_control.ps1` 生成两端定义和[完整命令表](CONTROL_COMMANDS.md)，用 `-Check` 检查产物是否同步。epoch 必须与当前 clip 一致，sequence 严格递增。恢复未完成时拒绝重新播放及切换相关设置；设置不会自动开始播放。

正式宿主 `owned_mmd_body.h` 在 worker 上读取控制帧，以 `DecodeCommandFrame` 验证封装、版本、保留字节和校验和，再将命令入队；游戏回调校验参数和运行条件。损坏帧不确认，合法帧的参数或目标被拒绝时发布明确结果。其他调用者仍可使用 `DecodeCommand` 同时验证封装和静态参数。

外部工具先写临时文件再替换 `control.dat`；中间被覆盖的命令不会自动排队，发送方须串行等待确认。正式宿主在同一状态快照中发布 `last_control_sequence` 与 `last_control_code`（0 接受，非 0 拒绝，枚举见 `ControlCode`）。`last_sequence` 保留“最后成功接受序号”的 V1 含义，拒绝不会推进它。C# 对当前序号的明确拒绝立即报错；旧宿主没有 `last_control_sequence` 时继续按 `last_sequence` 确认，3 秒内没有结果则报超时。队列实际可容纳 Capacity−1 条，满时留待下次 worker 重试。回调使用预分配数据，不进行文件 I/O；worker 在既有回调锁下读取一致状态，再发布状态文件。

坐标约定沿用 `core_math`：column-major、列向量 `M*v`、世界组合 `parent*local`、quat `q*v*conj(q)`；UE 原生行存储只在后端边界转换。运行时契约保存在 `data/runtime_contracts/`，每个新会话须重新核对构建、对象身份、骨表和调用阶段。直接骨姿播放写入组件空间姿态；原生模式把全身动作送入本次观测绑定的原生物理路径。原生模式保持游戏动画时间推进，不能同时冻结动画时间或启用衣物惯性增强。

桌面入口为 C# / WPF 的 `WWPoser.exe` 或便携包中的 `start.cmd`。播放器读取当前角色并使用明确的角色契约，不以默认角色填补缺失身份。动作、表情、镜头、手动位置、音乐与录屏的操作见 [播放器指南](../tools/MMD_PLAYER.md)。更换角色或动作需先恢复旧会话。衣物惯性增强只使用明确导出的链和受支持参数；未提供可用增强时，关闭增强仍可播放身体，请求开启则明确失败。

自动录屏通过 `start_paused` 在 0 秒准备场景，等待身体、镜头与录像首帧确认后发送 Play。准备等待不消耗驻留时限；正式开始后的暂停仍按既有规则计时。

桌面会话由 `PlaybackSessionService` 统一协调准备、加载、运行、恢复及恢复重试；窗口只读取状态并提交操作。运行目录只由 `OwnedMmdPlaybackService` 持有，预览和播放共享停止流程。播放、暂停和自然结束仍以原生时间轴为准，桌面不再维护第二套时间轴状态；过期状态禁用普通控制，停止恢复仍可用。预览没有时间轴心跳，因此按同一份原生与托管恢复证据确认退场。自动录屏属于启动它的表演，手动录屏独立；自然结束只保存自动录屏并保持末帧，停止或关闭时先请求停止录像，再恢复游戏，最后等待视频保存。

编辑请求只包含演员列表、镜头、时间轴与设置。旧 `Source` / `StageSession` 字段在读取时忽略，旧准备目录从配置恢复表情、目线、位置、镜头及可确定的手动缩放；身份无法恢复时要求重新选择。无显式编辑设置的 `--run-owned-mmd` 直接运行已校验的编译准备，不因缺失原始 VMD 而重新编译。运行快照包含自身的身体、表情、目线和镜头素材，回读时优先使用快照文件。

`vmd_local_clip.h` 集中验证 WWLOCAL1 的格式、尺寸、完整载荷、姿态数值及资源上限（1024 骨骼、100000 帧、文件不超过 1 GiB）。加载前桌面端也校验文件散列和角色关联，避免损坏准备数据进入游戏进程。

当前正式后端由 `tools/build_player.ps1 -Rebuild` 构建；回归入口为 `tools/run_offline_checks.ps1`，详见[构建与离线检查](../docs/BUILDING.md)。每次加载仍核对当前文件、构建、身份和恢复证明。
