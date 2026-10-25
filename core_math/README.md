# 播放器公共数学

`include/poser_math/` 保留当前编译器与播放器使用的数学头：动作、镜头采样、重定向与双骨 IK。实现为 header-only，正式编译器和 DLL 构建直接包含它们。

布料碰撞数学位于 `../playback/math/cloth_collision_math.h`。当前编译器、播放格式与恢复回归位于 `../tools/tests/`；Windows 统一入口是 `tools/run_offline_checks.ps1`，详见[构建与离线检查](../docs/BUILDING.md)。
