# 项目来源与第三方组件

## Wuwa Poser

- 源码：https://github.com/AKRTR465/Wuwa-Poser
- 许可：AGPL-3.0，全文见 [LICENSE](LICENSE)。
- MMD 数学与播放相关工作源自 https://github.com/honxi1/Endfield-Poser 。本项目保留源码中的原始署名与许可。
- README 结构参考：https://github.com/OedoSoldier/Endfield-Poser 。
- 部分手指重定向和扭转骨约定参考了 https://github.com/Sasye/EIEM （AGPL-3.0）；具体引用保留在 `mmd_retarget.h` 与 `mmd_arm_twist.h` 的注释中。

## nlohmann/json 3.11.3

- 作者：Niels Lohmann 与贡献者。
- 许可：MIT，见 [licenses/nlohmann-json-MIT.txt](licenses/nlohmann-json-MIT.txt)。
- 源码：https://github.com/nlohmann/json/tree/v3.11.3
- 使用位置：`core_math/third_party/nlohmann/json.hpp`，原始版权声明保留在文件内。

## .NET 10 / Windows Desktop Runtime

Windows 自包含包携带 Microsoft .NET 运行库。许可与第三方声明见 `licenses/dotnet/`，原始源码及构建信息见：

- https://github.com/dotnet/runtime
- https://github.com/dotnet/wpf
- https://github.com/dotnet/windowsdesktop

## FFmpeg

录屏通过独立的 FFmpeg / ffprobe 程序完成，采用 BtbN Windows x64 GPL 构建。固定版本、文件散列及归档来源见 [data/dependencies/ffmpeg.json](data/dependencies/ffmpeg.json)。便携包中的 `tools/recording_bin/` 包含其 GPL-3.0 许可、来源说明与校验记录。

- FFmpeg 对应源码：https://github.com/FFmpeg/FFmpeg/tree/46d899d7a0
- 本次构建脚本及依赖来源：https://github.com/BtbN/FFmpeg-Builds/tree/63ac3265b0789ea16c7b1f8351cd2ad195a28722
- 构建、依赖下载脚本与补丁位于上述仓库的 `build.sh`、`scripts.d/`、`patches/` 和 `addins/`。

## 游戏与个人素材

游戏、角色、骨架标识及相关素材的权利归各自权利人所有。仓库中的适配数据用于描述运行契约和角色映射，不授予游戏资产的再分发许可。游戏程序集、模型、纹理、个人动作、镜头和音乐不随源码或便携包分发。
