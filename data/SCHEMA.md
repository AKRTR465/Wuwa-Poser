# W7 数据文件 SCHEMA

数据目录：`F:\deeplearning\UAV\ww-poser\data\`；原始导出（全量 UObject JSON）：`F:\deeplearning\UAV\ww-asset-exports\`（镜像 pak 内路径）。

通用约定：
- 所有路径为 pak 内包路径（`Client/Content/Aki/...`，无扩展名即包名，加 `.json` 为导出文件）；
- `points` 为 `[x, y]` 二元组数组，x=链归一化位置(0~1)，y=参数值；
- 骨族分类函数见 `tools/build_outputs.py::classify_bone`（`bip001/skirt/hair/ribbon/chest/hand_twist/pelvis_extra/facial/weapon_prop/prop/other:<族名>`）。

## bone_mapping_v1.json
```
{
  version: "v1",
  generated: ISO 时间,
  basis: { skeletons_compared: N, characters: ["性别/角色", ...], note },
  standard_count: 映射到 MMD 标准骨的骨名数,
  mapping: { "<WW骨名>": { mmd: "MMD日文名"|null, romaji: "罗马字"|null,
                           class: standard|twist_extra|contact_marker|secondary_physics|facial|prop,
                           note } },
  unmapped: { "<骨名>": {...} },   // v1 为空
  gender_presence: { "<性别>": { "<骨名>": 出现型号数 } }
}
```
同名 `.md` 为人读版（标准映射表 L 侧，R 侧 `_R` 镜像；扩展骨模式表）。

## bone_chains.json
```
{
  generated, source,
  characters: [ {
    gender, character, model_id,
    skeleton_asset,           // 导出 JSON 相对路径（无骨架皮肤目录则缺省）
    bone_count, bone_families: { 族名: 骨数 },
    has_kawaii_groups: bool,
    chains_total, chains_valid,   // valid = 根确在本骨架的链（皮肤 ABP 指向别骨架的链 bones=null 计入 total 不计入 valid）
    kawaii_groups: [ {            // 每个匹配到的 ABP_*_Phy 一条（或骨架启发式一条）
      abp: 包路径|null, variant: ABP 文件名|'skeleton_heuristic',
      groups: [ {
        node: "AnimGraphNode_KawaiiPhysicsGroup[_N]"|'heuristic_family_chains',
        root_bones: ["Bone_..."],
        physics: { Damping, WorldDampingLocation, WorldDampingRotation, Stiffness, Radius, LimitAngle, HalfWidth, Shape },
        spherical_limits: [ { driving_bone, radius, limit_type, offset:{X,Y,Z} } ],
        capsule_limits_n, planar_limits_n, exclude_bones, alpha_curve,
        chains: [ { root, type: hair|skirt|ribbon|chest|..., bones: ["root→tip 骨名序列"]|null, note? } ]
      } ]
    } ],
    curves: [],                 // 保留字段（曲线见 kawaii_curves.json）
    unassigned_secondary_bones: ["Bone_..."],   // 未被任何 Kawaii 组覆盖的二级骨（多为链静态父节）
    status: { skeleton?, abp_errors?, chains_source?, abp_note? }
  } ]
}
```

## kawaii_curves.json
```
{
  generated, curves_total,
  characters: [ {
    gender, character,
    curves: [ {
      file: "Qunbai_KawaiiLimitAngle",
      chain_hint: "Qunbai",        // 文件名前缀（空=通用）
      type: skirt|hair|sleeve|coat|strap|bow|cape|hairpin|ear|tie|tail|generic,
      param: LimitAngle|Stiffness,
      asset: 包路径,
      points: [[x,y], ...]|null,  // null=导出缺失（error 说明）
      error?: string
    } ]
  } ]
}
```

## morph_targets.json
```
{
  generated,
  models: [ {
    gender, character, model_id,
    meshes: [ { mesh: 网格资产名, morph_count, head_bone } ],
    morphs: ["去重合并的 MorphTarget 名"],   // 面部+身体（A/E/I/O/U 口型、B/E/M/P 表情、Pupil_*、S_01..、Scda_* 等）
    split_morphs: ["SplitMorphTargets 内名"],  // 身材形变（Scda_*）
    morph_count, split_morph_count,
    status?: 'mesh_export_missing'|'no_mesh_in_dir...'|'<mesh>:export_missing'
  } ]
}
```

## 辅助文件
- `bones_census.json`：每含骨架型号 `{gender,character,model_id,bone_count,bip001_bones[],bip001_count,families}`；
- `crosscheck.json`：`matrix[] = {gender,character,bone_chain_types[],curve_types[],chains_total,curves_n}` —— Skeleton 骨族 ↔ 曲线类型交叉核对；
- `bone_chains.md` / `COVERAGE.md` / `PROGRESS.md`：人读报告；
- `roster_triples.json`：82 个 `[性别,角色,型号Id]`；`search_*.json|txt`：CLI search 原始落盘；`packages.tsv|packages2.tsv`：导出清单；`export_log.tsv`：`状态\t类别\t包路径` 日志。

## 原始导出（ww-asset-exports）字段速查
- `*_Skeleton.json`：顶层 `ReferenceSkeleton.FinalRefBoneInfo[{Name,ParentIndex}]`（骨名+父索引）、`FinalRefBonePose[]`（bind 变换）、`FinalNameToIndexMap`；`Properties.RBFDrivers[]`（DriverBoneName/BaseBoneName 面部骨驱动）、`Sockets[]`。
- 网格 `.json`：内嵌 `MorphTarget` 对象（morph 名单+顶点增量索引）、`Properties.HeadBoneName`、`MorphTargets[]` 引用。
- `ABP_*_Phy.json`：`Default__*_C.Properties.AnimGraphNode_KawaiiPhysicsGroup*`（链/物理/碰撞全参数）。
- `Data/KawaiiCurve/*.json`：`CurveFloat.Properties.FloatCurve.Keys[{Time,Value,...}]`。
