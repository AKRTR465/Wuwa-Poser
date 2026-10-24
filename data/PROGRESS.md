# W7 进度（WW 骨骼与物理数据导出）—— 已完成

状态：**全部四份数据 + 覆盖报告产出完毕**（2026-10-05）。验收标准三条均满足。

## 产物（F:\deeplearning\UAV\ww-poser\data\）

| 文件 | 内容 |
|---|---|
| bone_mapping_v1.json/.md | 124 个 Bip001 骨名（82 型号并集，9 体型）→ 92 MMD 标准 + 32 扩展类，0 未分类 |
| bone_chains.json/.md | 82 型号：ABP KawaiiPhysicsGroup 链 54 型号 + 骨架启发式 18 型号；链=root→tip 骨名序列+父链隐含于 Skeleton；每组物理参数/球胶囊碰撞 |
| kawaii_curves.json | 134 条曲线（21 角色×类型），全部含 x/y 点列（模板化分析见 COVERAGE #10） |
| morph_targets.json | 82 型号 morph 名单（71 型号有网格 morph，共 36 SplitMorphTargets 资产/30 型号） |
| COVERAGE.md | 82 行完成度矩阵 + 10 条结构异常 + 续跑指针 |
| SCHEMA.md | 全部 JSON 字段定义 |
| crosscheck.json / bones_census.json | 交叉核对矩阵 / 骨名词汇普查 |

## 原始导出（F:\deeplearning\UAV\ww-asset-exports\）

391 包 export-raw JSON（packages.tsv 374 + packages2.tsv 17），FAIL 0：
Skeleton 73（含 Suoming _SMM_）/ 网格 91 / KawaiiCurve 119 / SplitMorphTargets 36 / ABP_*_Phy 73。

## 关键结构发现（摘要，详录 COVERAGE.md）

1. cooked Skeleton 骨名在顶层 `ReferenceSkeleton.FinalRefBoneInfo`（BoneTree 无名）。
2. KawaiiPhysics 真实定义在 `ABP_Seq_*_Phy`（RootBones/PhysicsSettings/SphericalLimits），曲线仅模板。
3. PhysicsAsset 全空壳；碰撞在 Kawaii 节点内（PT3 改取此处）。
4. 目录命名陷阱：Linnai/Linnaea、Calbrena、Fuluoluo_Lifu 的 ABP 在兄弟目录、FemaleZ 的 ABP 在性别级。
5. 8 真实角色 + TestModel 族 ABP 无 Kawaii 节点 → 启发式链兜底。
6. Xinyuehu 系有 ZSpring_*/Tail/Toe 等特殊族；sleeve 曲线对应骨并入 Piao/Skirt 族。

## 复跑方式（幂等）

```bash
bash tools/batch_export.sh 4        # 导出（已存在即 SKIP）
python tools/build_outputs.py && python tools/gen_chain_md.py && python tools/gen_mapping.py && python tools/gen_coverage.py
```
