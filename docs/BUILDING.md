# Windows 源码构建

## 环境

- Windows x64、PowerShell 7。
- .NET 10 SDK（`dotnet --version` 应显示 10.x）。
- Visual Studio 2022 Build Tools，安装 MSVC x64 C++ 工具与 Windows SDK。
- 合法取得、与目标游戏版本一致的本地参考 DLL。它们只用于编译，不会复制到便携包中。

`tools/msvc_env.bat` 默认使用 VS 2022 Build Tools。其他安装位置可设置 `WW_VCVARS64`，指向对应的 `VC\Auxiliary\Build\vcvars64.bat`。

托管桥编译器默认位于 VS 2022 Build Tools 的 `MSBuild\Current\Bin\Roslyn\csc.exe`。如需替换，可先运行 `tools/build_owned_mmd.ps1 -Compiler <路径>`，随后按需构建其他原生工具，并在已有运行包的基础上运行 `build_player.ps1`。

## 参考 DLL

将下面六个文件放到 `local/owned_mmd_references/`，或通过 `-ReferenceDirectory` 指定现有目录：

```text
Aki.Game.dll
Aki.Core.dll
System.Private.CoreLib.dll
UnrealEngine.dll
System.Runtime.dll
System.Collections.dll
```

也可设置 `WWPOSER_MANAGED_REFERENCE_DIR`。仓库不提供游戏程序集，不会自动从第三方下载这些文件。

## 获取录屏依赖

```powershell
pwsh -NoProfile -File tools/prepare_player_dependencies.ps1 -Download
```

依赖按 `data/dependencies/ffmpeg.json` 固定版本和 SHA-256 校验。也可以下载相同归档后传入 `-FfmpegArchive 'D:\Downloads\ffmpeg.zip'`。已有依赖可使用 `-Check` 核对。只进行 C# 或纯原生离线测试时，无需准备录屏依赖。

## 完整构建

在仓库根目录运行：

```powershell
pwsh -NoProfile -File tools/build_player.ps1 -Rebuild -PublicRelease -ReferenceDirectory 'D:\WuwaReferences'
```

`-Rebuild` 编译托管桥、原生运行时、探测工具和 VMD 编译器，随后发布自带 .NET 运行库的 WPF 程序。`-PublicRelease` 排除本地会话占用记录，并在包内记录公开源码地址。输出位于 `dist/ww-mmd-player-csharp_<时间>/`。

可用 `-OutputDirectory` 指定新的便携包目录；`-PublishDirectory` 可单独指定 WPF 中间发布目录。若原生输出已准备好，可省略 `-Rebuild`。已有运行时包还可通过 `-OwnedMmdPackageDirectory` 指定。

校验便携包：

```powershell
.\dist\<便携包目录>\WWPoser.exe --validate-bundle
```

此命令只校验包与角色目录，不启动或操作游戏。

## 离线回归检查

依次运行；两个 WPF 检查项目共享构建目录，应串行执行。

```powershell
dotnet run --project tools/tests/ManagedActorBorrowing/ManagedActorBorrowing.csproj
dotnet run --project tools/tests/UnifiedServiceChecks/ServiceChecks.csproj
dotnet run --project tools/tests/UnifiedUiChecks/UnifiedUiChecks.csproj -- . test_outputs/ui
.\tools\tests\run_unified_player_features_tests.bat
.\tools\tests\run_unified_transport_tests.bat
.\tools\tests\run_dynamic_actor_tests.bat
.\tools\tests\run_owned_recovery_tests.bat
```

这些检查使用合成数据或本进程测试对象，不证明当前游戏版本、全角色物理、录屏或同进程连续播放的视觉效果。游戏运行验证需另行进行。

## 源码目录

| 目录 | 内容 |
| --- | --- |
| `desktop/WWPoser/` | WPF 界面、时间轴、媒体与会话编排 |
| `runtime/owned_mmd/` | 原生加载、托管角色生命周期、物理和恢复逻辑 |
| `probes/v20_recon/` | 运行身份检查、原生图绑定和共享播放功能 |
| `playback/`、`core_math/` | 动作与镜头格式、重定向、IK 和播放数学 |
| `data/` | 角色适配数据、运行契约与第三方依赖锁定信息 |
| `tools/` | 构建脚本、VMD 编译器、回归检查和使用指南 |

公开仓库从当前工作区导出源码快照，不包含本机历史分析目录、运行转储、游戏程序集、个人素材或构建缓存。较早的工程记录中提及的本地证据不随公开源码分发。
