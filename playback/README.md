# 播放控制组件

`player_transport.h` 提供时间轴和预分配 SPSC 控制队列，支持播放、暂停、seek、逐帧、循环、0.25–2 倍速、自然结束保持末帧及停止恢复请求。VMD 加载与游戏状态修改由播放器后端处理。clip duration 使用秒，VMD source frame 为 `seconds * 30`。

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

控制帧固定为 64 字节 little-endian，`EncodeCommand/DecodeCommand` 按字节序列化。字段：0–7 magic `WWPCTL1\0`、8 version=1、12 length=64、16 clip epoch u64、24 sequence u64、32 command u32、40 value IEEE754 binary64、60 FNV-1a checksum；36、48–59 保留为 0。命令依次为 Play=1、Pause=2、SeekSeconds=3、StepFrames=4、SetLoop=5、SetSpeed=6、Stop=7、SetPhysics=8、SetCameraMode=9、SetPlacementX/Y/Height/Yaw/InPlace=10–14、ResetPlacement=15。无载荷命令 value=0；loop/physics/in-place 为 0/1；camera 为 0/1/2（跟随/固定/轨道）；frame step 为整数。epoch 必须与当前 clip 一致，sequence 严格递增。恢复未完成时拒绝重新播放及切换相关设置；设置不会自动开始播放。

`probes/v20_recon/publish_probe.cpp` 的 `ReadControlFile` 在 worker 上使用允许原子替换的 Win32 共享标志读取定长文件，关闭句柄后再解码、入队。外部工具先写临时文件再替换目标路径；中间被覆盖的命令不会自动排队，发送方须等待状态中的 `last_sequence` 确认再发送依赖命令。队列实际可容纳 Capacity−1 条，满时留待下次 worker 重试。回调使用预分配数据，不进行文件 I/O；worker 通过原子状态快照发布 `player_status.json`，不并发读取 `Transport::State()`。旧的未接入 `FileCommandReader` 已移出活跃代码。

坐标约定沿用 `core_math`：column-major、列向量 `M*v`、世界组合 `parent*local`、quat `q*v*conj(q)`；UE 原生行存储只在后端边界转换。运行时契约保存在 `data/runtime_contracts/`，每个新会话须重新核对构建、对象身份、骨表和调用阶段。直接骨姿播放写入组件空间姿态；原生模式把全身动作送入本次观测绑定的原生物理路径。原生模式保持游戏动画时间推进，不能同时冻结动画时间或启用衣物惯性增强。

桌面入口为 `python -B tools/mmd_player_ui.py`。播放器读取当前角色并使用明确的角色契约，不再以爱弥斯默认配置填补缺失身份。动作、表情、镜头、手动位置、音乐与录屏的操作见 [播放器指南](../tools/MMD_PLAYER.md)。更换角色或动作需先恢复旧会话。衣物惯性增强只使用明确导出的链和受支持参数；未提供可用增强时，关闭增强仍可播放身体，请求开启则明确失败。

测试与专用构建入口已移除。当前正式后端由 `tools/build_player.ps1 -Rebuild` 构建；每次加载仍核对当前文件、构建、身份和恢复证明。
