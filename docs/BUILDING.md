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

`-Rebuild` 编译托管桥、原生运行时、探测工具和 VMD 编译器，随后发布自带 .NET 运行库的 WPF 程序。`-PublicRelease` 要求干净且已提交的源码及 `-Rebuild`，执行离线回归，排除本地会话占用记录，并在包内记录公开源码地址。构建期间源码发生变化会拒绝完成公开包。输出位于 `dist/ww-mmd-player-csharp_<时间>/`。

可用 `-OutputDirectory` 指定新的便携包目录；`-PublishDirectory` 可单独指定 WPF 中间发布目录。本地开发包在原生输出已准备好时可省略 `-Rebuild`；已有运行时包还可通过 `-OwnedMmdPackageDirectory` 指定。公开发布会重新构建该目录，并核对运行时 manifest 的源码提交、干净状态和逐文件 SHA-256。

明确需要快速发布时可加 `-SkipOfflineChecks` 跳过离线回归；默认仍运行回归。此开关保留源码提交约束、全量重新编译及包完整性校验，不代表游戏内效果已验证。

公开发布的自定义输出、中间发布和运行时包目录应位于仓库外或已被 Git 忽略；默认的 `dist/`、`build/` 和 `desktop/WWPoser/publish/` 已忽略。放在仓库内的未忽略目录会使发布检查认为工作树发生了变化。

校验便携包：

```powershell
.\dist\<便携包目录>\WWPoser.exe --validate-bundle
```

此命令只校验包与角色目录，不启动或操作游戏。新包的 schema 4 manifest 为每个文件记录字节数与 SHA-256，校验会拒绝缺失、长度不符、内容损坏和不合法的相对路径。旧 schema 3 清单仍检查文件长度，但没有散列的旧清单不能证明文件内容一致。

`data/` 中的证书和契约使用原始文件字节散列。仓库通过 `.gitattributes` 保留它们既有的字节与换行，避免 `core.autocrlf` 改变校验结果。编辑这些文件后仍需更新相关证书，不能仅转换换行而沿用旧散列。

## 离线回归检查

Windows 完整离线检查可通过统一入口运行，无需游戏参考 DLL、游戏进程或录屏依赖：

```powershell
pwsh -NoProfile -File tools/run_offline_checks.ps1
```

入口会构建三个 VMD 编译器，使用合成素材检查演员准备，并运行下面的回归与数据证书检查。两个 WPF 检查项目共享构建目录，应串行执行。Windows/Linux 的 GitHub Actions 工作流会在提交和 PR 上执行相应离线检查。

也可以分别运行：

```powershell
dotnet run --project tools/tests/ManagedActorBorrowing/ManagedActorBorrowing.csproj
dotnet run --project tools/tests/UnifiedServiceChecks/ServiceChecks.csproj
dotnet run --project tools/tests/UnifiedUiChecks/UnifiedUiChecks.csproj -- . test_outputs/ui
.\tools\tests\run_unified_player_features_tests.bat
.\tools\tests\run_unified_transport_tests.bat
.\tools\tests\run_dynamic_actor_tests.bat
.\tools\tests\run_owned_recovery_tests.bat
.\tools\tests\run_local_clip_tests.bat
.\tools\tests\run_camera_diagnostics_tests.bat
.\tools\tests\run_session_retirement_tests.bat
pwsh -NoProfile -File tools/tests/runtime_manifest_checks.ps1
```

跨平台服务检查直接编译同一份生产服务和合成测试，不依赖 WPF：

```sh
dotnet run --project tools/tests/PortableServiceChecks/PortableServiceChecks.csproj
dotnet run --project tools/tests/ManagedActorBorrowing/ManagedActorBorrowing.csproj
```

`unified_transport_tests.cpp`、`owned_recovery_tests.cpp`、`local_clip_tests.cpp` 和 `camera_diagnostics_tests.cpp` 也支持 C++17 编译器；并发镜头测试需要线程支持（例如 GCC 的 `-pthread`）。会话退役和其余原生测试使用 Windows API 的本进程测试对象。

这些检查使用合成数据或本进程测试对象，不证明当前游戏版本、全角色物理、录屏或同进程连续播放的视觉效果。游戏运行验证需另行进行。

## 源码目录

游戏版本相关的默认运行参数统一维护在 `data/game_compat/ww_runtime.json`。修改后运行 `tools/generate_game_compat.ps1`；生成物校验、动态发现范围及版本更新流程见 [游戏兼容定义](GAME_COMPATIBILITY.md)。

| 目录 | 内容 |
| --- | --- |
| `desktop/WWPoser/` | WPF 界面、时间轴、媒体与会话编排 |
| `runtime/owned_mmd/` | 原生加载、托管角色生命周期、物理和恢复逻辑 |
| `probes/v20_recon/` | 运行身份检查、原生图绑定和共享播放功能 |
| `playback/`、`core_math/` | 动作与镜头格式、重定向、IK 和播放数学 |
| `data/` | 角色适配数据、运行契约与第三方依赖锁定信息 |
| `tools/` | 构建脚本、VMD 编译器、回归检查和使用指南 |

公开仓库从当前工作区导出源码快照，不包含本机历史分析目录、运行转储、游戏程序集、个人素材或构建缓存。较早的工程记录中提及的本地证据不随公开源码分发。
