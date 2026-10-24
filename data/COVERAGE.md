# W7 覆盖报告（COVERAGE）

生成：2026-10-05T10:57:17；工具链：fmodel-cli export-raw → ww-asset-exports JSON → tools/*.py

## 名册口径

- 名册三元组 = `Client/Content/Aki/Character/Role/<性别>/<角色名>/<型号Id>` 下存在 `Model/` 目录的全部条目（search `**/Model/*.uasset`，1561 文件无截断，82 目录）；
- 独立交叉源：`*_Skeleton.uasset` 全枚举 73 个（含 Suoming 双骨架 `_SMM_` 变体）、`ABP_*_Phy.uasset` 73 个、`SplitMorphTargets` 36 资产 —— 均含于 82 目录内，无目录外角色；
- 性别（体型）目录 9 种：{'FemaleM': 31, 'FemaleMS': 11, 'FemaleS': 7, 'FemaleXL': 13, 'FemaleZ': 1, 'FemaleZ2': 1, 'MaleM': 6, 'MaleS': 2, 'MaleXL': 10}；角色目录 67 个，型号 82 个。

## 完成度汇总

| 指标 | 数值 |
|---|---|
| 型号目录（名册） | 82 |
| Skeleton JSON 导出+解析 | 72（其余 10 为无骨架皮肤/材质目录，复用基础型号骨架） |
| 骨链：ABP KawaiiPhysicsGroup 提取 | 54 型号 |
| 骨链：骨架启发式（ABP 无 Kawaii 节点） | 18 型号 |
| KawaiiCurve 曲线 | 134 条（LimitAngle/Stiffness 成对，20 个角色目录；23 个名册行匹配） |
| 网格 morph（face/body） | 71 型号 |
| SplitMorphTargets（body 形变） | 30 型号（36 资产，其余挂在皮肤型号目录） |
| Bip001 骨名词汇（全名册并集） | 72 骨架比对 → 124 骨名，92 映射 MMD 标准，32 归扩展类，0 未分类 |

## 每型号矩阵

| 性别 | 角色 | 型号Id | 骨架 | 骨数 | 链源/链数 | 曲线(点数) | 网格morph | SplitMorph | 备注 |
|---|---|---|---|---|---|---|---|---|---|
| FemaleM | Aimisi | R2T1AimisiMd10011 | Y | 388 | ABP/64 | 6(15pt) | 105 | 15 |  |
| FemaleM | Chun | R2T1ChunMd10011 | Y | 195 | ABP/33 | 0 | 86 | 0 |  |
| FemaleM | Fuluoluo | R2T1FuluoluoMd10011 | Y | 213 | ABP/25 | 0 | 105 | 0 |  |
| FemaleM | Fuluoluo_Lifu | NHT1FuluoluoLifu | Y | 213 | 启发式/39 | 0 | 105 | 0 |  |
| FemaleM | Jianxin | R2T1JianxinMd10011 | Y | 203 | ABP/31 | 0 | 86 | 0 |  |
| FemaleM | Jinxi | R2T1JinxiMd10011 | Y | 196 | ABP/28 | 0 | 86 | 0 |  |
| FemaleM | Jinxi | R2T1JinxiMd20011 | Y | 266 | ABP/28 | 0 | 86 | 0 |  |
| FemaleM | LinNai | R2T1LinnaeaMd20011 | N | - | - | 6(15pt) | 105 | 10 | skeleton=export_missing;abp_skipped_no_skeleton=['ABP_Seq_LinNai_Phy'] |
| FemaleM | LinNai | R2T1LinnaiMd10011 | Y | 398 | ABP/67 | 6(15pt) | 105 | 10 |  |
| FemaleM | Lucy | R2T1LucyMd10011 | Y | 326 | ABP/64 | 0 | 161 | 67 |  |
| FemaleM | Lupa | R2T1LupaMd10011 | Y | 258 | ABP/49 | 0 | 105 | 0 |  |
| FemaleM | Maxiaofang | R2T1MaxiaofangMd10011 | Y | 182 | ABP/32 | 0 | 86 | 0 |  |
| FemaleM | Micai | R2T1MicaiMd10011 | Y | 186 | ABP/30 | 0 | 86 | 0 |  |
| FemaleM | Nvzhu | R2T1PlayerFemaleMd10011 | Y | 197 | ABP/36 | 4(9pt) | 161 | 37 |  |
| FemaleM | Nvzhu | R2T1PlayerFemaleMd20011 | Y | 307 | ABP/35 | 4(9pt) | 105 | 24 |  |
| FemaleM | Qianxia | R2T1QianXiaMd10011 | Y | 397 | ABP/75 | 6(15pt) | 105 | 4 |  |
| FemaleM | Qianxia | R2T1QianXiaMd20011 | N | - | - | 6(15pt) | 105 | 3 | skeleton=export_missing;abp_skipped_no_skeleton=['ABP_Seq_Qianxia_Phy'] |
| FemaleM | Sanhua | R2T1SanhuaMd10011 | Y | 202 | ABP/23 | 0 | 86 | 0 |  |
| FemaleM | Sanhua | R2T1SanhuaMd20011 | N | - | - | 0 | 86 | 0 | skeleton=export_missing;abp_skipped_no_skeleton=['ABP_Seq_Sanhua_Phy', 'ABP_Seq_Sanhua_Skin_1_Phy'] |
| FemaleM | Shouanren | R2T1ShouanrenMd10011 | Y | 243 | ABP/53 | 0 | 86 | 0 |  |
| FemaleM | Suisui | R2T1SuisuiMd10011 | Y | 320 | ABP/63 | 8(20pt) | 161 | 15 |  |
| FemaleM | Taohua | R2T1TaohuaMd10011 | Y | 184 | ABP/31 | 0 | 86 | 0 |  |
| FemaleM | TestModel | TestModel01 | Y | 197 | 启发式/15 | 0 | 0 | 0 |  |
| FemaleM | TestModel | TestModel02 | Y | 197 | 启发式/15 | 0 | 0 | 0 |  |
| FemaleM | Xiakong | R2T1XiakongMd10011 | Y | 273 | ABP/78 | 0 | 105 | 0 |  |
| FemaleM | Xuanling | R2T1XuanlingMd10011 | Y | 333 | ABP/78 | 6(15pt) | 161 | 30 |  |
| FemaleM | Yangyang | R2T1YangyangMd10011 | Y | 177 | ABP/21 | 0 | 161 | 43 |  |
| FemaleM | Yangyang | R2T1YangyangwumaoMd10011 | N | - | - | 0 | 161 | 43 | skeleton=export_missing;abp_skipped_no_skeleton=['ABP_Seq_Yangyang_Phy'] |
| FemaleM | YinLin | R2T1YinlinMd10011 | Y | 190 | ABP/24 | 0 | 105 | 0 |  |
| FemaleM | Younuo | R2T1YounuoMd10011 | Y | 379 | ABP/41 | 0 | 105 | 4 |  |
| FemaleM | Zhezhi | R2T1ZhezhiMd10011 | Y | 220 | 启发式/36 | 0 | 86 | 0 |  |
| FemaleMS | Buling | R2T1BulingMd10011 | Y | 283 | ABP/41 | 6(14pt) | 105 | 0 |  |
| FemaleMS | Daniya | R2T1DaniyaMd10011 | Y | 354 | ABP/69 | 6(15pt) | 161 | 24 |  |
| FemaleMS | Dengdeng | R2T1DengDengMd10011 | Y | 246 | 启发式/51 | 0 | 86 | 0 |  |
| FemaleMS | Feibi | R2T1FeibiMd10011 | Y | 266 | ABP/37 | 0 | 86 | 0 |  |
| FemaleMS | Katixiya | R2T1KatixiyaMd10011 | Y | 257 | ABP/43 | 0 | 86 | 0 |  |
| FemaleMS | Kmola | R2T1KamolaMd10011 | Y | 263 | ABP/102 | 0 | 86 | 0 |  |
| FemaleMS | Kmola | R2T1KamolaMd20011 | N | - | - | 0 | 86 | 0 | skeleton=export_missing;abp_skipped_no_skeleton=['ABP_Seq_Kmola_Link_Skin01_Phy', 'ABP_Seq_Kmola_Phy'] |
| FemaleMS | Mone | R2T1MoneMd10011 | Y | 412 | ABP/70 | 6(15pt) | 107 | 5 |  |
| FemaleMS | Mone | R2T1MoneMd20011 | N | - | - | 6(15pt) | 105 | 5 | skeleton=export_missing;abp_skipped_no_skeleton=['ABP_Seq_Mone_Phy'] |
| FemaleMS | Rebecca | R2T1RebeccaMd10011 | Y | 361 | ABP/37 | 0 | 161 | 80 |  |
| FemaleMS | Xigelika | R2T1XigelikaMd10011 | Y | 340 | 启发式/71 | 6(15pt) | 161 | 24 |  |
| FemaleS | Anke | R2T1AnkeMd10011 | Y | 200 | ABP/41 | 0 | 86 | 0 |  |
| FemaleS | Jueyuan | R2T1JueyuanMd10011 | Y | 199 | ABP/35 | 0 | 86 | 0 |  |
| FemaleS | Luokeke | R2T1LuokekeMd10011 | Y | 248 | ABP/59 | 0 | 86 | 0 |  |
| FemaleS | Suoming | R2T1SuomingMd10011 | Y | 336 | ABP/48 | 6(15pt) | 161 | 13 |  |
| FemaleS | TestModel | TestModel01 | N | - | - | 0 | 0 | 0 | skeleton=export_missing |
| FemaleS | TestModel | TestModel02 | Y | 200 | 启发式/4 | 0 | 0 | 0 |  |
| FemaleS | Youhu | R2T1YouHuMd10011 | Y | 219 | ABP/46 | 0 | 86 | 0 |  |
| FemaleXL | Aogusita | R2T1AogusitaMd10011 | Y | 353 | ABP/67 | 0 | 105 | 0 |  |
| FemaleXL | BaiLian | R2T1BaiLianMd10011 | Y | 197 | ABP/30 | 0 | 86 | 0 |  |
| FemaleXL | ChangLi | R2T1ChangLiMd10011 | Y | 254 | ABP/41 | 0 | 86 | 0 |  |
| FemaleXL | ChangLi | R2T1ChangLiMd20011 | N | - | - | 0 | 86 | 0 | skeleton=export_missing;abp_skipped_no_skeleton=['ABP_Seq_ChangLi_Link_Skin01_Phy', 'ABP_Seq_ChangLi_Phy'] |
| FemaleXL | Jiabeilina | R2T1CalbrenaMd10011 | Y | 369 | ABP/77 | 2(4pt) | 105 | 3 |  |
| FemaleXL | Kanteleila | R2T1KanteleilaMd10011 | Y | 266 | ABP/48 | 0 | 86 | 0 |  |
| FemaleXL | Luosela | R2T1LuoselaMd10011 | Y | 323 | ABP/39 | 0 | 162 | 32 |  |
| FemaleXL | TaiDaoNv | R2T1TaidaonvMd10011 | Y | 372 | ABP/57 | 6(15pt) | 161 | 8 |  |
| FemaleXL | TestModel | TestModel01 | Y | 197 | 启发式/13 | 0 | 0 | 0 |  |
| FemaleXL | Xinyuehu | R2T1XinMd10011 | Y | 489 | ABP/92 | 12(30pt) | 176 | 77 |  |
| FemaleXL | XinyuehuShen | R2T1ShenXinMd10011 | Y | 498 | 启发式/88 | 12(28pt) | 161 | 29 |  |
| FemaleXL | Zanni | R2T1ZanniMd10011 | Y | 364 | ABP/66 | 0 | 86 | 2 |  |
| FemaleXL | Zanni | R2T1ZanniMd20011 | N | - | - | 0 | 86 | 2 | skeleton=export_missing;abp_skipped_no_skeleton=['ABP_Seq_Zanni_Phy', 'ABP_Seq_Zanni_Seq_Link_Skin01_Phy'] |
| FemaleZ | Fuludelisi | R2T1FuludelisiMd10011 | Y | 300 | 启发式/41 | 0 | 105 | 0 | abp_note=gender_level_abp |
| FemaleZ2 | AimisiGD | R2T1AimisiGDMd10011 | Y | 319 | 启发式/14 | 2(5pt) | 0 | 0 |  |
| MaleM | Jingran | R2T1JingranMd10011 | Y | 289 | ABP/40 | 4(10pt) | 161 | 30 |  |
| MaleM | Nanzhu | R2T1PlayerMaleMd10011 | Y | 171 | ABP/56 | 0 | 161 | 39 |  |
| MaleM | Nanzhu | R2T1PlayerMaleMd20011 | N | - | - | 0 | 105 | 30 | skeleton=export_missing;abp_skipped_no_skeleton=['ABP_Seq_Nanzhu_Md20011_Link_Phy', 'ABP_Seq_Nanzhu_Md20011_Phy', 'ABP_Seq_Nanzhu_Phy', 'ABP_Seq_Nanzhu_NPC_Phy'] |
| MaleM | TestModel | TestModel01 | Y | 171 | 启发式/16 | 0 | 0 | 0 |  |
| MaleM | TestModel | TestModel02 | Y | 171 | 启发式/16 | 0 | 0 | 0 |  |
| MaleM | Xiangliyao | R2T1XiangLiYaoMd10011 | Y | 224 | 启发式/34 | 0 | 86 | 0 |  |
| MaleS | Lingyang | R2T1LingyangMd10011 | Y | 197 | ABP/36 | 0 | 86 | 0 |  |
| MaleS | TestModel | TestModel01 | Y | 197 | 启发式/33 | 0 | 0 | 0 |  |
| MaleXL | Baer | R2T1BaerMd10011 | Y | 200 | ABP/35 | 0 | 86 | 0 |  |
| MaleXL | Bulante | R2T1BulanteMd10011 | Y | 258 | ABP/77 | 0 | 86 | 0 |  |
| MaleXL | Jiyan | R2T1JiyanMd10011 | Y | 188 | ABP/36 | 0 | 105 | 0 |  |
| MaleXL | Kakaluo | R2T1KakaluoMd10011 | Y | 200 | 启发式/28 | 0 | 86 | 0 |  |
| MaleXL | Luhesi | R2T1LuhesiMd10011 | Y | 379 | ABP/71 | 6(15pt) | 105 | 9 |  |
| MaleXL | Qiushui | R2T1QiushuiMd10011 | Y | 178 | ABP/25 | 0 | 86 | 0 |  |
| MaleXL | Qiuyuan | R2T1QiuyuanMd10011 | Y | 362 | ABP/72 | 2(4pt) | 105 | 0 |  |
| MaleXL | TestModel | TestModel01 | Y | 188 | 启发式/1 | 0 | 0 | 0 |  |
| MaleXL | TestModel | TestModel02 | Y | 188 | 启发式/1 | 0 | 0 | 0 |  |
| MaleXL | Yuanwu | R2T1YuanwuMd10011 | Y | 142 | ABP/5 | 0 | 86 | 0 |  |

## 结构异常发现

1. **cooked Skeleton 无骨名**：`Properties.BoneTree` 仅 retarget 模式；骨名在顶层 `ReferenceSkeleton.FinalRefBoneInfo`（SCHEMA 见各 JSON）。
2. **KawaiiPhysics 定义在 ABP 而非曲线**：`ABP_Seq_<角色>_Phy` 内 `AnimGraphNode_KawaiiPhysicsGroup[_N]` 持 RootBones/物理参数/球胶囊碰撞；`Data/KawaiiCurve` 只是 LimitAngle/Stiffness 浮点曲线（x=链归一位置 0-1，y=参数值）。
3. **8 个真实角色 ABP 无 Kawaii 节点**（Zhezhi/Dengdeng/Xigelika/AimisiGD/Xiangliyao/Kakaluo/Fuluoluo_Lifu/XinyuehuShen 及 9 个 TestModel）：链改由骨架启发式提取（族根=父不在同族），来源已标注。
4. **目录命名不一致**：LinNai 目录 `R2T1LinnaiMd10011` 内资产叫 `R2T1Linnaea*`；Jiabeilina→`Calbrena`；Fuluoluo_Lifu 的 ABP 在 `Fuluoluo/NHT1FuluoluoLifu/Seq/`；FemaleZ(唯一定居者 Fuludelisi) 的 ABP 在性别级 `Role/FemaleZ/Seq/`。皮肤型号（Md20011 等 10 目录）无自身骨架。
5. **PhysicsAsset 全部为空壳**（仅 ThumbnailInfo）：Kuro 碰撞全在 Kawaii 节点 SphericalLimits（球）+ CapsuleLimits；PT3 碰撞几何应取自 ABP_Phy 而非 PhysicsAsset。
6. **Suoming Md10011 双骨架**（base + `_SMM_`），SMM 变体已导出。
7. **curve sleeve(Xiuzi) 无同名骨族**：袖子曲线对应的袖摆链在骨架中命名并入 `Bone_Piao*`/`Bone_Skirt*` 族（语义映射，见 crosscheck.json）。
8. **Xinyuehu/XinyuehuShen 特殊骨族**：`ZSpring_*`（弹簧骨）、`Tail*`、`Toe*`、`Clavicle`、`Omuscle`；Buling 有 `Teeth/Tongue`；Jiabeilina 有 `Wing` —— 均归 `other:*` 扩展类。
9. **export-mesh/export-anim 不可用**（TypeLoadException），网格顶点/BindPose 未导出；`FinalRefBonePose`（bind 变换）已在 Skeleton JSON 内可离线取用。
10. **KawaiiCurve 按类型高度模板化**（对 W9 至关重要）：134 条曲线中，hair/Stiffness 21 条仅 1 种取值、skirt/LimitAngle 20 条仅 2 种、sleeve 外几乎全为每类型单值——**角色间物理差异不在曲线，而在 ABP 每组 PhysicsSettings 标量**（bone_chains.json 已全量捕获）。W9 翻译器应以「模板曲线形状 × 每组标量」为主输入。

## 失败 / 跳过

- 导出失败 0（export_log.tsv：OK 374+17/FAIL 0）；
- 无骨架皮肤目录 10 个：不阻塞（复用同角色基础型号骨架与链）；
- 网格无 morph：AimisiGD（高达形态）+ TestModel 族（非正式角色）；
- KawaiiCurve 缺失 60 角色目录：游戏本体即无该目录（其 ABP 亦多无 Kawaii 节点或仅用默认参数），如实记录。

## 断点续跑

```bash
bash F:/deeplearning/UAV/ww-poser/data/tools/batch_export.sh 4   # packages.tsv 幂等（已存在 .json 即 SKIP）
cd F:/deeplearning/UAV/ww-poser/data && python tools/build_outputs.py && python tools/gen_chain_md.py && python tools/gen_mapping.py && python tools/gen_coverage.py
```
