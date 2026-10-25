# 独立角色形态

在每张演员卡的角色列表中选择形态。默认形态与特殊形态可以同时添加，分别选择身体动作、表情和站位。同一形态也可以添加多次。

当前本地游戏资源快照中登记了以下四组，目录共有 84 个选项（原有 80 个角色／皮肤组合，加 4 个形态）。

| 默认角色 | 新增选项 | 资源依据 | 特殊形态骨骼数 | 静态 UI 原生物理组 |
| --- | --- | --- | ---: | ---: |
| 达妮娅 | 达妮娅 · Dark | `BP_DaniyaDark_Seq_V2` 的身体与 `Up2` 部件 | 354 | 10 |
| 卡提希娅 | 卡提希娅 · 芙露德莉斯 | `RoleMorph` 第 1 行 | 300 | 7 |
| 爱弥斯 | 爱弥斯 · 机甲 | `RoleMorph` 第 2 行 | 319 | 4 |
| 心（配置名称） | 心 · 特殊形态 | `RoleMorph` 第 3 行，`XinyuehuShen` | 498 | 1 |

这张表记录配置和资源存在，不表示所有配置内容均已正式发布。扫描覆盖该快照的全部三行 `RoleMorph` 配置及角色骨架资源；技能中的材质、特效和临时显隐状态尚未穷举为独立形态。

## 实例与动作隔离

四组角色的默认与特殊形态均创建独立演员，不借用编队角色。每个演员有独立的主网格组件、动画实例、动作／表情文件、原生图观察目录和恢复记录。不可变网格资源与动作编译缓存允许复用。全场播放、暂停和进度仍使用同一时间轴。

三种官方特殊形态使用游戏的 `UiRoleMorphComponent.SetMorphType`，让游戏为当前演员切换主网格与动画类。DaniyaDark 使用游戏的模型加载缓存和 `ChangeMesh` 组合部件，以 `Up2` 为主网格、身体为跟随部件，保留主网格的 147 个表情目标。

DaniyaDark 的 Sequence 物理图没有导出自身的 Kawaii 组。MMD 加载时使用同骨架的 `ABP_Performance_Daniya_PC` UI 动画图，并要求当前实例重新观测和匹配原生物理；不会直接把无物理组的 Sequence 图当作已支持原生物理。Dark 的部件组合、跟随姿态、材质和物理视觉效果仍需游戏内确认。爱弥斯机甲没有表情 morph，身体动作和原生物理单独准备。

## 验证状态

已完成四组共八个选择的离线 MMD 编译、形态来源／网格／骨架／动画图签名校验，以及合成引擎对象下的同时加载、重复形态、部件隔离和失败恢复检查。尚未完成这四组的游戏内同时加载与视觉验收。

```powershell
pwsh -NoProfile -File tools/validate_owned_character_catalog.ps1
pwsh -NoProfile -File tools/validate_character_forms.ps1
dotnet run --project tools/tests/ManagedActorBorrowing/ManagedActorBorrowing.csproj
dotnet run --project tools/tests/UnifiedServiceChecks/ServiceChecks.csproj -- --prepare-actors .
```

有本机原始提取文件时，给形态校验脚本添加 `-CheckSources` 可核对来源散列。便携包不包含游戏资产、游戏程序集或这些原始提取文件。
