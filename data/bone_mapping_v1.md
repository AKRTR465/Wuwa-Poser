# Bone Mapping v1：WW Bip001 → MMD 标准骨

生成：2026-10-05T10:56:25；比对基础：72 个 Skeleton（9 种体型）

## 结论要点

- WW 全角色共用同一套 3ds Max Biped 命名 `Bip001*`（标准身体骨各角色同构，实测 91 根），映射表可全局通用；
- Nub 末端骨（Biped 常见 `*Nub`）在 WW cooked 骨架中不存在；
- 扭转骨每处 2-3 节：首节映射 MMD 捩り骨，其余节为扩展；
- `Bip001_*_[MFB]` 族为 KawaiiPhysics 接触/碰撞标记骨，MMD 无对应，归扩展；
- 二级物理骨 `Bone_Skirt/Hair/Piao/Chest*` 全部归扩展（另见 bone_chains.json）。

## 标准映射（L 侧；R 侧镜像 `_R`）

| WW 骨名 | MMD 骨名 | romaji | 说明 |
|---|---|---|---|
| Root | 全ての親 | all_parent | 骨架根 |
| Bip001 | センター | center | Biped 中心骨（Pelvis 父） |
| Bip001Pelvis | 下半身 | lower_body | 骨盆 |
| Bip001Spine | 上半身 | upper_body | 脊椎1/3 |
| Bip001Spine1 | 上半身2 | upper_body2 | 脊椎2/3 |
| Bip001Spine2 | 上半身3 | upper_body3 | 脊椎3/3（MMD 常规为2节，此节列扩展位的 VMD 兼容候选） |
| Bip001Neck | 首 | neck |  |
| Bip001Head | 頭 | head | HeadBoneName 同名（网格实测） |
| Bip001LClavicle | 肩L | shoulder_L |  |
| Bip001LUpperArm | 腕L | arm_L |  |
| Bip001LUpArmTwist | 腕捩りL | arm_twist_L | 上臂扭转0（共3节） |
| Bip001LForearm | ひじL | elbow_L |  |
| Bip001LForeTwist | 手捩りL | wrist_twist_L | 前臂扭转0（共2节） |
| Bip001LHand | 手首L | wrist_L |  |
| Bip001LFinger0 | 親指0L | thumb0_L |  |
| Bip001LFinger01 | 親指1L | thumb1_L |  |
| Bip001LFinger02 | 親指2L | thumb2_L |  |
| Bip001LFinger1 | 人指し1L | index1_L |  |
| Bip001LFinger11 | 人指し2L | index2_L |  |
| Bip001LFinger12 | 人指し3L | index3_L |  |
| Bip001LFinger2 | 中指1L | middle1_L |  |
| Bip001LFinger21 | 中指2L | middle2_L |  |
| Bip001LFinger22 | 中指3L | middle3_L |  |
| Bip001LFinger3 | 薬指1L | ring1_L |  |
| Bip001LFinger31 | 薬指2L | ring2_L |  |
| Bip001LFinger32 | 薬指3L | ring3_L |  |
| Bip001LFinger4 | 小指1L | little1_L |  |
| Bip001LFinger41 | 小指2L | little2_L |  |
| Bip001LFinger42 | 小指3L | little3_L |  |
| Bip001LThigh | 足L | leg_L |  |
| Bip001LThighTwist | 足捩りL | leg_twist_L | 大腿扭转0（少数体型有第3节→扩展） |
| Bip001LCalf | ひざL | knee_L |  |
| Bip001LFoot | 足首L | ankle_L |  |
| Bip001LToe0 | つま先L | toe_L | 趾0（母趾位） |
| Bip001LToe01 | つま先II_L | toe2_L | 趾0第2节（少数角色） |
| Bip001LToe1 | 足指1L | toe_f1_L | 趾扇 1（少数角色） |
| Bip001LToe11 | 足指1_2L | toe_f1b_L | 趾1第2节 |
| Bip001LToe2 | 足指2L | toe_f2_L | 趾扇 2 |
| Bip001LToe21 | 足指2_2L | toe_f2b_L | 趾2第2节 |
| Bip001LToe3 | 足指3L | toe_f3_L | 趾扇 3 |
| Bip001LToe31 | 足指3_2L | toe_f3b_L | 趾3第2节 |
| Bip001LToe4 | 足指4L | toe_f4_L | 趾扇 4 |
| Bip001LToe41 | 足指4_2L | toe_f4b_L | 趾4第2节 |
| Bip001LFinger03 | 親指3L | thumb3_L | 拇指第4节（少数角色，MMD 3节→扩展位） |
| Bip001LFinger13 | 人指し4L | index4_L | 食指第4节（同上） |
| Bip001LFinger23 | 中指4L | middle4_L | 中指第4节 |
| Bip001LFinger33 | 薬指4L | ring4_L | 薬指第4节 |
| Bip001LFinger43 | 小指4L | little4_L | 小指第4节 |
| Bip001LForeTwist2 | 手捩り2L | wrist_twist2_L | 前臂扭转2（第3节，少数体型） |
| Bip001LThighTwist2 | 足捩り2L | leg_twist2_L | 大腿扭转2（第3节，少数体型） |
| Bip001Neck1 | 首2 | neck2 | 颈2（少数体型，MMD 单首→扩展位） |

## 扩展骨分类（无 MMD 标准对应）

| 模式 | 类别 | 说明 |
|---|---|---|
| `^Bip001[LR]UpArmTwist[12]$` | twist_extra | 上臂扭转1/2（腕捩り扩展节） |
| `^Bip001[LR]ForeTwist1$` | twist_extra | 前臂扭转1（手捩り扩展节） |
| `^Bip001[LR]ThighTwist1$` | twist_extra | 大腿扭转1（足捩り扩展节） |
| `^Bip001_Shoulder_[LR]_[MFB]$` | contact_marker | 肩部接触标记骨（F前/B后/M中，KawaiiPhysics 碰撞/极限用） |
| `^Bip001_Leg_[LR]_[MFB]$` | contact_marker | 大腿接触标记骨 |
| `^Bip001_[LR]_Knee_[FB]$` | contact_marker | 膝部接触标记骨 |
| `^Bip001_[LR]_Elbow_[FB]$` | contact_marker | 肘部接触标记骨 |
| `^Bip001_[LR]_Calf(Twist)?$` | contact_marker | 小腿碰撞/扭转骨 |
| `^Bone_Skirt.*$` | secondary_physics | 裙摆二级物理骨链（W7 bone_chains） |
| `^Bone_Hair.*$` | secondary_physics | 头发二级物理骨链 |
| `^Bone_Piao.*$` | secondary_physics | 飘带/饰带二级物理骨链 |
| `^Bone_Chest.*$` | secondary_physics | 胸前饰二级物理骨链 |
| `^[RL]_ChestBone.*$` | secondary_physics | 胸前饰骨（少数角色） |
| `^Bone_HandTwist.*$` | secondary_physics | 手部扭转辅助骨 |
| `^Bone_Pelvis.*$` | secondary_physics | 盆骨饰骨 |
| `^Facial_.*$` | facial | 面部辅助骨（颧/颊/口周，非 morph 表情） |
| `^WeaponProp.*$` | prop | 武器挂点 |
| `^HuluProp.*$` | prop | 挂件挂点（如葫芦） |

## 体型覆盖（每体型出现该骨的型号数）

见 bone_mapping_v1.json 的 gender_presence。
