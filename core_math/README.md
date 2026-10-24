# 播放器公共数学

`include/poser_math/` 保留当前编译器与播放器使用的数学头：动作、镜头采样、重定向与双骨 IK。实现为 header-only，正式编译器和 DLL 构建直接包含它们。

测试框架、测试程序和专用构建入口已移除。布料碰撞数学位于 `../playback/math/cloth_collision_math.h`。
