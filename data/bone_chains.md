# WW 全角色二级骨骼物理链清单（bone_chains）

生成：2026-10-05T10:56:24（数据源：Skeleton ReferenceSkeleton + ABP_*_Phy KawaiiPhysicsGroups）

链 = KawaiiPhysicsGroup 的一个 RootBone 起的 root→tip 骨名序列（分支即多链）；
未入组二级骨 = 链静态父节或未模拟骨，列表见 JSON `unassigned_secondary_bones`。

## FemaleM/Aimisi（R2T1AimisiMd10011）

- 骨总数 388；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 114, 'ribbon': 102, 'facial': 20, 'hand_twist': 2, 'chest': 4, 'prop': 1, 'pelvis_extra': 1, 'skirt': 45}
- KawaiiPhysics 组 10 个（ABP 变体 1 个），链 64 条
- KawaiiCurve：hair, skirt, sleeve
- 未入组二级骨：76 根

### ABP_Seq_Aimisi_Phy / AnimGraphNode_KawaiiPhysicsGroup_9

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.25
- Alpha 曲线：Kawaii_alpha_S06

- [头发] Bone_Hair002_M → Bone_Hair003_M
- [头发] Bone_Hair002_R → Bone_Hair003_R
- [头发] Bone_Hair012_R → Bone_Hair013_R
- [头发] Bone_Hair022_R → Bone_Hair023_R → Bone_Hair024_R
- [头发] Bone_Hair062_R → Bone_Hair063_R
- [头发] Bone_Hair032_R → Bone_Hair033_R → Bone_Hair034_R
- [头发] Bone_Hair042_R → Bone_Hair043_R → Bone_Hair044_R
- [头发] Bone_Hair002_L → Bone_Hair003_L
- [头发] Bone_Hair012_L → Bone_Hair013_L → Bone_Hair014_L
- [头发] Bone_Hair022_L → Bone_Hair023_L
- [头发] Bone_Hair033_L → Bone_Hair034_L
- [头发] Bone_Hair042_L → Bone_Hair043_L
- [头发] Bone_Hair171_L → Bone_Hair172_L
- [头发] Bone_Hair061_L → Bone_Hair062_L

### ABP_Seq_Aimisi_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.2 Stiffness=0.15 LimitAngle=50.0 Radius=2.5 HalfWidth=3.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S03

- [头发] Bone_Hair103_M → Bone_Hair104_M → Bone_Hair111_L → Bone_Hair112_L → Bone_Hair113_L → Bone_Hair114_L → Bone_Hair115_L
- [头发] Bone_Hair103_M → Bone_Hair104_M → Bone_Hair121_L → Bone_Hair122_L → Bone_Hair124_L → Bone_Hair151_L → Bone_Hair152_L
- [头发] Bone_Hair103_M → Bone_Hair104_M → Bone_Hair121_L → Bone_Hair122_L → Bone_Hair124_L → Bone_Hair125_L → Bone_Hair127_L
- [头发] Bone_Hair103_M → Bone_Hair104_M → Bone_Hair131_L → Bone_Hair132_L → Bone_Hair133_L → Bone_Hair135_L → Bone_Hair136_L
- [头发] Bone_Hair103_M → Bone_Hair104_M → Bone_Hair131_L → Bone_Hair132_L → Bone_Hair133_L → Bone_Hair141_L → Bone_Hair142_L → Bone_Hair143_L
- [头发] Bone_Hair103_M → Bone_Hair104_M → Bone_Hair131_R → Bone_Hair132_R → Bone_Hair133_R → Bone_Hair134_R → Bone_Hair135_R
- [头发] Bone_Hair103_M → Bone_Hair104_M → Bone_Hair111_R → Bone_Hair112_R → Bone_Hair113_R → Bone_Hair115_R → Bone_Hair116_R
- [头发] Bone_Hair103_M → Bone_Hair104_M → Bone_Hair111_R → Bone_Hair112_R → Bone_Hair113_R → Bone_Hair161_R → Bone_Hair162_R
- [头发] Bone_Hair103_M → Bone_Hair104_M → Bone_Hair121_R → Bone_Hair123_R → Bone_Hair124_R → Bone_Hair125_R → Bone_Hair126_R → Bone_Hair127_R
- [头发] Bone_Hair103_M → Bone_Hair104_M → Bone_Hair121_R → Bone_Hair123_R → Bone_Hair152_R → Bone_Hair153_R → Bone_Hair154_R
- [头发] Bone_Hair103_M → Bone_Hair104_M → Bone_Hair121_R → Bone_Hair123_R → Bone_Hair142_R → Bone_Hair143_R → Bone_Hair144_R → Bone_Hair145_R

### ABP_Seq_Aimisi_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.2 Stiffness=0.04 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S04

- [头发] Bone_Hair073_L → Bone_Hair075_L → Bone_Hair076_L → Bone_Hair077_L
- [头发] Bone_Hair073_L → Bone_Hair081_L → Bone_Hair082_L
- [头发] Bone_Hair073_R → Bone_Hair075_R → Bone_Hair076_R → Bone_Hair077_R
- [头发] Bone_Hair073_R → Bone_Hair081_R → Bone_Hair082_R

### ABP_Seq_Aimisi_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=25.0 Radius=0.7 HalfWidth=2.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao071_L → Bone_Piao073_L
- [飘带(Piao)] Bone_Piao072_R → Bone_Piao075_R
- [飘带(Piao)] Bone_Piao101_L → Bone_Piao102_L
- [飘带(Piao)] Bone_Piao101_R → Bone_Piao102_R
- [飘带(Piao)] Bone_Piao091_L
- [飘带(Piao)] Bone_Piao091_R
- [飘带(Piao)] Bone_Piao281_L
- [飘带(Piao)] Bone_Piao281_R
- [飘带(Piao)] Bone_Piao081_L → Bone_Piao082_L
- [飘带(Piao)] Bone_Piao081_R → Bone_Piao082_R

### ABP_Seq_Aimisi_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=50.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S01

- [裙摆] Bone_Skirt002_R → Bone_Skirt003_R
- [裙摆] Bone_Skirt012_R → Bone_Skirt013_R → Bone_Skirt014_R
- [裙摆] Bone_Skirt002_L → Bone_Skirt003_L
- [裙摆] Bone_Skirt012_L → Bone_Skirt013_L → Bone_Skirt014_L

### ABP_Seq_Aimisi_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=40.0 Radius=4.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S05

- [飘带(Piao)] Bone_Piao182_L → Bone_Piao183_L → Bone_Piao184_L → Bone_Piao185_L

### ABP_Seq_Aimisi_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.3 Stiffness=0.65 LimitAngle=10.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [胸饰] Bone_Chest001_R → Bone_Chest002_R
- [胸饰] Bone_Chest001_L → Bone_Chest002_L

### ABP_Seq_Aimisi_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S02

- [裙摆] Bone_Skirt032_L → Bone_Skirt033_L → Bone_Skirt034_L → Bone_Skirt035_L → Bone_Skirt036_L → Bone_Skirt037_L → Bone_Skirt038_L
- [裙摆] Bone_Skirt032_L → Bone_Skirt033_L → Bone_Skirt034_L → Bone_Skirt035_L → Bone_Skirt051_L → Bone_Skirt052_L
- [裙摆] Bone_Skirt032_R → Bone_Skirt033_R → Bone_Skirt034_R → Bone_Skirt035_R → Bone_Skirt036_R → Bone_Skirt037_R → Bone_Skirt038_R
- [裙摆] Bone_Skirt032_R → Bone_Skirt033_R → Bone_Skirt034_R → Bone_Skirt035_R → Bone_Skirt051_R → Bone_Skirt052_R
- [裙摆] Bone_Skirt042_M → Bone_Skirt043_M → Bone_Skirt044_L → Bone_Skirt045_L → Bone_Skirt046_L → Bone_Skirt047_L
- [裙摆] Bone_Skirt042_M → Bone_Skirt043_M → Bone_Skirt044_R → Bone_Skirt045_R → Bone_Skirt046_R → Bone_Skirt047_R
- [飘带(Piao)] Bone_Piao213_L → Bone_Piao214_L
- [飘带(Piao)] Bone_Piao222_L → Bone_Piao223_L → Bone_Piao224_L → Bone_Piao225_L → Bone_Piao226_L
- [飘带(Piao)] Bone_Piao222_R → Bone_Piao223_R → Bone_Piao224_R
- [飘带(Piao)] Bone_Piao153_L → Bone_Piao154_L → Bone_Piao155_L → Bone_Piao156_L → Bone_Piao157_L → Bone_Piao158_L → Bone_Piao159_L → Bone_Piao160_L
- [飘带(Piao)] Bone_Piao153_R → Bone_Piao154_R → Bone_Piao155_R → Bone_Piao156_R → Bone_Piao157_R → Bone_Piao158_R → Bone_Piao159_R → Bone_Piao160_R
- [飘带(Piao)] Bone_Piao174_L
- [飘带(Piao)] Bone_Piao174_R

### ABP_Seq_Aimisi_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.6 Stiffness=0.15 LimitAngle=15.0 Radius=0.8 HalfWidth=1.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao122_R → Bone_Piao123_R
- [飘带(Piao)] Bone_Piao122_L → Bone_Piao123_L
- [飘带(Piao)] Bone_Piao111_L → Bone_Piao112_L
- [飘带(Piao)] Bone_Piao111_R → Bone_Piao112_R

### ABP_Seq_Aimisi_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=35.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r8.0
- Alpha 曲线：Kawaii_alpha_jianxin

- [头发] Bone_Hair052_M → Bone_Hair053_M → Bone_Hair054_M → Bone_Hair055_M

## FemaleM/Chun（R2T1ChunMd10011）

- 骨总数 195；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 42, 'ribbon': 25, 'chest': 4, 'prop': 1, 'skirt': 24}
- KawaiiPhysics 组 8 个（ABP 变体 1 个），链 33 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：36 根

### ABP_Seq_Chun_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=35.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r8.096541
- Alpha 曲线：None

- [头发] Bone_Hair002_R → Bone_Hair003_R
- [头发] Bone_Hair014_M → Bone_Hair015_M
- [头发] Bone_Hair002_M → Bone_Hair003_M
- [头发] Bone_Hair002_L → Bone_Hair003_L
- [头发] Bone_Hair022_L → Bone_Hair023_L
- [头发] Bone_Hair022_R → Bone_Hair023_R

### ABP_Seq_Chun_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=25.0 Radius=1.8 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao042_L → Bone_Piao043_L
- [飘带(Piao)] Bone_Piao051_L → Bone_Piao052_L
- [飘带(Piao)] Bone_Piao042_R → Bone_Piao043_R → Bone_Piao044_R

### ABP_Seq_Chun_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=35.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [头发] Bone_Hair035_L → Bone_Hair036_L → Bone_Hair037_L → Bone_Hair038_L
- [头发] Bone_Hair035_R → Bone_Hair036_R → Bone_Hair037_R → Bone_Hair038_R

### ABP_Seq_Chun_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=35.0 Radius=3.0 HalfWidth=4.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [裙摆] Bone_Skirt013_L → Bone_Skirt014_L
- [裙摆] Bone_Skirt013_R → Bone_Skirt014_R
- [裙摆] Bone_Skirt023_L → Bone_Skirt024_L
- [裙摆] Bone_Skirt023_R → Bone_Skirt024_R
- [裙摆] Bone_Skirt003_R → Bone_Skirt004_R
- [裙摆] Bone_Skirt003_L → Bone_Skirt004_L

### ABP_Seq_Chun_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.05 Stiffness=0.05 LimitAngle=35.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r8.912348
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao031_L
- [飘带(Piao)] Bone_Piao031_R
- [飘带(Piao)] Bone_Piao012_L
- [飘带(Piao)] Bone_Piao012_R
- [飘带(Piao)] Bone_Piao001_R
- [飘带(Piao)] Bone_Piao001_L
- [飘带(Piao)] Bone_Piao022_L
- [飘带(Piao)] Bone_Piao022_R

### ABP_Seq_Chun_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=35.0 Radius=1.5 HalfWidth=4.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [飘带(Piao)] Bone_Piao073_R
- [飘带(Piao)] Bone_Piao072_L
- [飘带(Piao)] Bone_Piao062_L → Bone_Piao063_L
- [飘带(Piao)] Bone_Piao062_R

### ABP_Seq_Chun_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=35.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r8.912348
- Alpha 曲线：None

- [头发] Bone_Hair042_L → Bone_Hair043_L
- [头发] Bone_Hair042_R → Bone_Hair043_R

### ABP_Seq_Chun_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.5 Stiffness=0.15 LimitAngle=15.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [胸饰] L_ChestBone01 → L_ChestBone02
- [胸饰] R_ChestBone01 → R_ChestBone02

## FemaleM/Fuluoluo（R2T1FuluoluoMd10011）

- 骨总数 213；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 28, 'ribbon': 28, 'chest': 4, 'prop': 1, 'skirt': 53}
- KawaiiPhysics 组 6 个（ABP 变体 2 个），链 25 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：40 根

### ABP_Seq_Fuluoluo_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.1 Stiffness=0.05 LimitAngle=35.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.7174253
- Alpha 曲线：None

- [头发] Bone_Hair006_M → Bone_Hair007_M
- [头发] Bone_Hair010_R → Bone_Hair011_R
- [头发] Bone_Hair018_R → Bone_Hair019_R
- [头发] Bone_Hair002_L → Bone_Hair003_L
- [头发] Bone_Hair014_L → Bone_Hair015_L

### ABP_Seq_Fuluoluo_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.1 Stiffness=0.04 LimitAngle=15.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao001_L
- [胸饰] R_ChestBone01 → R_ChestBone02
- [胸饰] L_ChestBone01 → L_ChestBone02
- [飘带(Piao)] Bone_Piao011_R
- [飘带(Piao)] Bone_Piao013_R
- [飘带(Piao)] Bone_Piao015_R
- [飘带(Piao)] Bone_Piao009_R
- [飘带(Piao)] Bone_Piao007_L

### ABP_Seq_Fuluoluo_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=35.0 Radius=1.5 HalfWidth=3.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 5 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [裙摆] Bone_Skirt008_R → Bone_Skirt009_R → Bone_Skirt063_R → Bone_Skirt010_R → Bone_Skirt055_R → Bone_Skirt056_R → Bone_Skirt057_R
- [裙摆] Bone_Skirt008_R → Bone_Skirt009_R → Bone_Skirt063_R → Bone_Skirt010_R → Bone_Skirt051_R → Bone_Skirt052_R → Bone_Skirt053_R
- [裙摆] Bone_Skirt001_L → Bone_Skirt002_L → Bone_Skirt058_L → Bone_Skirt003_L → Bone_Skirt004_L → Bone_Skirt005_L → Bone_Skirt006_L
- [裙摆] Bone_Skirt001_L → Bone_Skirt002_L → Bone_Piao003_L

### ABP_Seq_Fuluoluo_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.3 LimitAngle=35.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [头发] Bone_Hair034_M → Bone_Hair021_L → Bone_Hair022_L → Bone_Hair023_L → Bone_Hair024_L → Bone_Hair025_L → Bone_Hair026_L
- [头发] Bone_Hair034_M → Bone_Hair028_R → Bone_Hair029_R → Bone_Hair030_R → Bone_Hair031_R → Bone_Hair032_R → Bone_Hair033_R

### ABP_Seq_Fuluoluo_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.15 LimitAngle=35.0 Radius=3.0 HalfWidth=4.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [裙摆] Bone_Skirt033_R → Bone_Skirt034_R → Bone_Skirt035_R → Bone_Skirt036_R
- [裙摆] Bone_Skirt026_L → Bone_Skirt027_L → Bone_Skirt028_L → Bone_Skirt029_L

### ABP_Seq_Fuluoluo_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.1 Stiffness=0.1 LimitAngle=35.0 Radius=2.0 HalfWidth=4.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [裙摆] Bone_Skirt046_R → Bone_Skirt047_R → Bone_Skirt048_R → Bone_Skirt049_R
- [裙摆] Bone_Skirt022_R → Bone_Skirt042_R → Bone_Skirt043_R → Bone_Skirt044_R
- [裙摆] Bone_Skirt022_R → Bone_Skirt038_R → Bone_Skirt039_R → Bone_Skirt040_R
- [裙摆] Bone_Skirt014_L → Bone_Skirt015_L → Bone_Skirt016_L → Bone_Skirt017_L

## FemaleM/Fuluoluo_Lifu（NHT1FuluoluoLifu）

- 骨总数 213；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 28, 'ribbon': 28, 'chest': 4, 'prop': 1, 'skirt': 53}
- KawaiiPhysics 组 1 个（ABP 变体 2 个），链 39 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：114 根
- 状态：{'chains_source': 'skeleton_heuristic (ABP 无 KawaiiPhysicsGroup 节点)'}

### skeleton_heuristic / heuristic_family_chains

- 参数：Damping=None Stiffness=None LimitAngle=None Radius=None HalfWidth=None Shape=None

- [头发] Bone_Hair013_L → Bone_Hair014_L → Bone_Hair015_L
- [头发] Bone_Hair001_L → Bone_Hair002_L → Bone_Hair003_L
- [头发] Bone_Hair005_M → Bone_Hair006_M → Bone_Hair007_M
- [头发] Bone_Hair009_R → Bone_Hair010_R → Bone_Hair011_R
- [头发] Bone_Hair017_R → Bone_Hair018_R → Bone_Hair019_R
- [飘带(Piao)] Bone_Piao001_L
- [头发] Bone_Hair034_M → Bone_Hair021_L → Bone_Hair022_L → Bone_Hair023_L → Bone_Hair024_L → Bone_Hair025_L → Bone_Hair026_L
- [头发] Bone_Hair034_M → Bone_Hair028_R → Bone_Hair029_R → Bone_Hair030_R → Bone_Hair031_R → Bone_Hair032_R → Bone_Hair033_R
- [胸饰] R_ChestBone01 → R_ChestBone02
- [胸饰] L_ChestBone01 → L_ChestBone02
- [飘带(Piao)] Bone_Piao007_L
- [飘带(Piao)] Bone_Piao009_R
- [飘带(Piao)] Bone_Piao011_R
- [飘带(Piao)] Bone_Piao013_R
- [飘带(Piao)] Bone_Piao015_R
- [飘带(Piao)] Bone_Piao031_M → Bone_Piao031_L → Bone_Piao032_L
- [飘带(Piao)] Bone_Piao031_M → Bone_Piao031_R → Bone_Piao032_R
- [飘带(Piao)] Bone_Piao031_M → Bone_Piao041_L → Bone_Piao042_L → Bone_Piao043_L → Bone_Piao044_L
- [飘带(Piao)] Bone_Piao031_M → Bone_Piao041_R → Bone_Piao042_R → Bone_Piao043_R → Bone_Piao044_R
- [飘带(Piao)] Bone_Piao051_M
- [裙摆] Bone_Skirt001_L → Bone_Skirt002_L → Bone_Skirt058_L → Bone_Skirt003_L → Bone_Skirt004_L → Bone_Skirt005_L → Bone_Skirt006_L
- [裙摆] Bone_Skirt001_L → Bone_Skirt002_L → Bone_Piao003_L
- [飘带(Piao)] Bone_Piao003_L
- [裙摆] Bone_Skirt008_R → Bone_Skirt009_R → Bone_Skirt063_R → Bone_Skirt010_R → Bone_Skirt055_R → Bone_Skirt056_R → Bone_Skirt057_R
- [裙摆] Bone_Skirt008_R → Bone_Skirt009_R → Bone_Skirt063_R → Bone_Skirt010_R → Bone_Skirt051_R → Bone_Skirt052_R → Bone_Skirt053_R
- [裙摆] Bone_Skirt012_L → Bone_Skirt013_L → Bone_Skirt059_L → Bone_Skirt014_L → Bone_Skirt015_L → Bone_Skirt016_L → Bone_Skirt017_L
- [裙摆] Bone_Skirt012_L → Bone_Piao005_L
- [飘带(Piao)] Bone_Piao005_L
- [裙摆] Bone_Skirt019_R → Bone_Skirt020_R → Bone_Skirt062_R → Bone_Skirt021_R → Bone_Skirt022_R → Bone_Skirt042_R → Bone_Skirt043_R → Bone_Skirt044_R
- [裙摆] Bone_Skirt019_R → Bone_Skirt020_R → Bone_Skirt062_R → Bone_Skirt021_R → Bone_Skirt022_R → Bone_Skirt038_R → Bone_Skirt039_R → Bone_Skirt040_R
- [裙摆] Bone_Skirt019_R → Bone_Skirt020_R → Bone_Skirt062_R → Bone_Skirt021_R → Bone_Skirt046_R → Bone_Skirt047_R → Bone_Skirt048_R → Bone_Skirt049_R
- [裙摆] Bone_Skirt024_L → Bone_Skirt025_L → Bone_Skirt060_L → Bone_Skirt026_L → Bone_Skirt027_L → Bone_Skirt028_L → Bone_Skirt029_L
- [裙摆] Bone_Skirt031_R → Bone_Skirt032_R → Bone_Skirt061_R → Bone_Skirt033_R → Bone_Skirt034_R → Bone_Skirt035_R → Bone_Skirt036_R
- [飘带(Piao)] Bone_Piao020_L
- [飘带(Piao)] Bone_Piao020_R
- [飘带(Piao)] Bone_Piao022_L
- [飘带(Piao)] Bone_Piao024_L
- [飘带(Piao)] Bone_Piao022_R
- [飘带(Piao)] Bone_Piao024_R

## FemaleM/Jianxin（R2T1JianxinMd10011）

- 骨总数 203；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 50, 'ribbon': 13, 'chest': 4, 'prop': 1, 'skirt': 36}
- KawaiiPhysics 组 9 个（ABP 变体 1 个），链 31 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：19 根

### ABP_Seq_Jianxin_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=45.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 9 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [头发] Bone_Hair021_M → Bone_Hair051_L → Bone_Hair052_L → Bone_Hair053_L → Bone_Hair054_L → Bone_Hair055_L
- [头发] Bone_Hair021_M → Bone_Hair051_L → Bone_Hair900_L → Bone_Hair901_L
- [头发] Bone_Hair021_M → Bone_Hair051_L → Bone_Hair900_L → Bone_Hair902_L
- [头发] Bone_Hair021_M → Bone_Hair051_L → Bone_Hair900_L → Bone_Hair903_L
- [头发] Bone_Hair021_M → Bone_Hair051_L → Bone_Hair900_L → Bone_Hair904_L
- [头发] Bone_Hair021_M → Bone_Hair051_R → Bone_Hair052_R → Bone_Hair053_R → Bone_Hair054_R → Bone_Hair055_R
- [头发] Bone_Hair021_M → Bone_Hair051_R → Bone_Hair900_R → Bone_Hair901_R
- [头发] Bone_Hair021_M → Bone_Hair051_R → Bone_Hair900_R → Bone_Hair902_R
- [头发] Bone_Hair021_M → Bone_Hair051_R → Bone_Hair900_R → Bone_Hair903_R
- [头发] Bone_Hair021_M → Bone_Hair051_R → Bone_Hair900_R → Bone_Hair904_R

### ABP_Seq_Jianxin_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.1 Stiffness=0.03 LimitAngle=30.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.293504
- Alpha 曲线：None

- [头发] Bone_Hair031_R → Bone_Hair032_R
- [头发] Bone_Hair011_R → Bone_Hair012_R → Bone_Hair016_R → Bone_Hair017_R
- [头发] Bone_Hair013_R → Bone_Hair014_R
- [头发] Bone_Hair011_L → Bone_Hair012_L
- [头发] Bone_Hair031_L → Bone_Hair032_L → Bone_Hair033_L
- [头发] Bone_Hair022_L → Bone_Hair023_L
- [头发] Bone_Hair041_L → Bone_Hair042_L

### ABP_Seq_Jianxin_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=30.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r9.645072
- Alpha 曲线：Kawaii_alpha_jianxin

- [头发] Bone_Hair001_M → Bone_Hair002_M → Bone_Hair003_M → Bone_Hair004_M → Bone_Hair005_M → Bone_Hair006_M → Bone_Hair007_M → Bone_Hair008_M → Bone_Hair009_M → Bone_Hair010_M → Bone_Hair011_M

### ABP_Seq_Jianxin_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.1 Stiffness=0.05 LimitAngle=30.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.25
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao001_R → Bone_Piao002_R
- [飘带(Piao)] Bone_Piao001_L → Bone_Piao002_L

### ABP_Seq_Jianxin_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.45 Stiffness=0.45 LimitAngle=25.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [胸饰] L_ChestBone01 → L_ChestBone02
- [胸饰] R_ChestBone01 → R_ChestBone02

### ABP_Seq_Jianxin_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.1 Stiffness=0.07 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao013_R
- [飘带(Piao)] Bone_Piao011_R

### ABP_Seq_Jianxin_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=45.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [裙摆] Bone_Skirt026_R → Bone_Skirt027_R → Bone_Skirt028_R → Bone_Skirt029_R → Bone_Skirt030_R
- [裙摆] Bone_Skirt025_L → Bone_Skirt026_L
- [飘带(Piao)] Bone_Piao025_L → Bone_Piao026_L → Bone_Piao027_L

### ABP_Seq_Jianxin_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.3 Stiffness=0.3 LimitAngle=45.0 Radius=1.5 HalfWidth=4.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [裙摆] Bone_Skirt001_R → Bone_Skirt002_R → Bone_Skirt003_R → Bone_Skirt004_R → Bone_Skirt005_R → Bone_Skirt006_R
- [裙摆] Bone_Skirt001_L → Bone_Skirt002_L → Bone_Skirt003_L → Bone_Skirt004_L

### ABP_Seq_Jianxin_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.2 Stiffness=0.3 LimitAngle=25.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [裙摆] Bone_Skirt013_R → Bone_Skirt014_R → Bone_Skirt015_R
- [裙摆] Bone_Skirt013_L → Bone_Skirt014_L → Bone_Skirt015_L

## FemaleM/Jinxi（R2T1JinxiMd10011）

- 骨总数 196；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 31, 'ribbon': 36, 'chest': 4, 'prop': 1, 'skirt': 25}
- KawaiiPhysics 组 9 个（ABP 变体 2 个），链 28 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：33 根

### ABP_Seq_Jinxi_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.1 Stiffness=0.03 LimitAngle=30.0 Radius=1.1 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.5
- Alpha 曲线：None

- [头发] Bone_Hair038_R → Bone_Hair039_R
- [头发] Bone_Hair030_M → Bone_Hair031_M
- [头发] Bone_Hair034_L → Bone_Hair035_L

### ABP_Seq_Jinxi_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.3 Stiffness=0.2 LimitAngle=30.0 Radius=3.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 10 / 平面碰撞 0
- Alpha 曲线：None

- [头发] Bone_Hair011_L → Bone_Hair012_L → Bone_Hair013_L → Bone_Hair015_L → Bone_Hair016_L → Bone_Hair017_L
- [头发] Bone_Hair021_R → Bone_Hair022_R → Bone_Hair023_R → Bone_Hair025_R → Bone_Hair026_R → Bone_Hair027_R

### ABP_Seq_Jinxi_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.3 Stiffness=0.2 LimitAngle=30.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao012_L → Bone_Piao013_L → Bone_Piao014_L
- [飘带(Piao)] Bone_Piao051_R
- [飘带(Piao)] Bone_Piao048_R

### ABP_Seq_Jinxi_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.1 Stiffness=0.2 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.25
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao029_R → Bone_Piao030_R
- [飘带(Piao)] Bone_Piao025_R → Bone_Piao026_R
- [飘带(Piao)] Bone_Piao032_R
- [飘带(Piao)] Bone_Piao009_L
- [飘带(Piao)] Bone_Piao002_L → Bone_Piao003_L
- [飘带(Piao)] Bone_Piao006_L → Bone_Piao007_L

### ABP_Seq_Jinxi_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=25.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao020_L → Bone_Piao021_L
- [飘带(Piao)] Bone_Piao055_M → Bone_Piao056_M
- [飘带(Piao)] Bone_Piao043_R → Bone_Piao044_R

### ABP_Seq_Jinxi_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.8 Stiffness=0.2 LimitAngle=30.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [胸饰] L_ChestBone01 → L_ChestBone02
- [胸饰] R_ChestBone01 → R_ChestBone02

### ABP_Seq_Jinxi_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.4 Stiffness=0.2 LimitAngle=35.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.25
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：None

- [头发] Bone_Hair006_R → Bone_Hair007_R
- [头发] Bone_Hair002_L → Bone_Hair003_L

### ABP_Seq_Jinxi_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=30.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [裙摆] Bone_Skirt034_R → Bone_Skirt035_R
- [裙摆] Bone_Skirt021_R → Bone_Skirt022_R → Bone_Skirt024_R
- [裙摆] Bone_Skirt015_L → Bone_Skirt016_L
- [裙摆] Bone_Skirt015_L → Bone_Skirt016_L
- [裙摆] Bone_Skirt028_L → Bone_Skirt029_L

### ABP_Seq_Jinxi_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=30.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [裙摆] Bone_Skirt007_R → Bone_Skirt008_R → Bone_Skirt009_R → Bone_Skirt010_R
- [裙摆] Bone_Skirt001_L → Bone_Skirt002_L → Bone_Skirt003_L → Bone_Skirt004_L

## FemaleM/Jinxi（R2T1JinxiMd20011）

- 骨总数 266；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 24, 'ribbon': 100, 'chest': 4, 'prop': 1, 'skirt': 38}
- KawaiiPhysics 组 9 个（ABP 变体 2 个），链 28 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：141 根

### ABP_Seq_Jinxi_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.1 Stiffness=0.03 LimitAngle=30.0 Radius=1.1 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.5
- Alpha 曲线：None

- [头发] Bone_Hair038_R ——（根不在本骨架：root not in this skeleton (variant ABP?)）
- [头发] Bone_Hair030_M ——（根不在本骨架：root not in this skeleton (variant ABP?)）
- [头发] Bone_Hair034_L ——（根不在本骨架：root not in this skeleton (variant ABP?)）

### ABP_Seq_Jinxi_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.3 Stiffness=0.2 LimitAngle=30.0 Radius=3.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 10 / 平面碰撞 0
- Alpha 曲线：None

- [头发] Bone_Hair011_L → Bone_Hair012_L
- [头发] Bone_Hair021_R → Bone_Hair022_R → Bone_Hair023_R → Bone_Hair024_R → Bone_Hair025_R → Bone_Hair026_R → Bone_Hair027_R

### ABP_Seq_Jinxi_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.3 Stiffness=0.2 LimitAngle=30.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao012_L
- [飘带(Piao)] Bone_Piao051_R ——（根不在本骨架：root not in this skeleton (variant ABP?)）
- [飘带(Piao)] Bone_Piao048_R ——（根不在本骨架：root not in this skeleton (variant ABP?)）

### ABP_Seq_Jinxi_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.1 Stiffness=0.2 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.25
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao029_R ——（根不在本骨架：root not in this skeleton (variant ABP?)）
- [飘带(Piao)] Bone_Piao025_R ——（根不在本骨架：root not in this skeleton (variant ABP?)）
- [飘带(Piao)] Bone_Piao032_R ——（根不在本骨架：root not in this skeleton (variant ABP?)）
- [飘带(Piao)] Bone_Piao009_L ——（根不在本骨架：root not in this skeleton (variant ABP?)）
- [飘带(Piao)] Bone_Piao002_L
- [飘带(Piao)] Bone_Piao006_L ——（根不在本骨架：root not in this skeleton (variant ABP?)）

### ABP_Seq_Jinxi_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=25.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao020_L ——（根不在本骨架：root not in this skeleton (variant ABP?)）
- [飘带(Piao)] Bone_Piao055_M ——（根不在本骨架：root not in this skeleton (variant ABP?)）
- [飘带(Piao)] Bone_Piao043_R ——（根不在本骨架：root not in this skeleton (variant ABP?)）

### ABP_Seq_Jinxi_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.8 Stiffness=0.2 LimitAngle=30.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [胸饰] L_ChestBone01 ——（根不在本骨架：root not in this skeleton (variant ABP?)）
- [胸饰] R_ChestBone01 ——（根不在本骨架：root not in this skeleton (variant ABP?)）

### ABP_Seq_Jinxi_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.4 Stiffness=0.2 LimitAngle=35.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.25
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：None

- [头发] Bone_Hair006_R ——（根不在本骨架：root not in this skeleton (variant ABP?)）
- [头发] Bone_Hair002_L

### ABP_Seq_Jinxi_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=30.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [裙摆] Bone_Skirt034_R
- [裙摆] Bone_Skirt021_R ——（根不在本骨架：root not in this skeleton (variant ABP?)）
- [裙摆] Bone_Skirt015_L → Bone_Skirt016_L → Bone_Skirt017_L
- [裙摆] Bone_Skirt015_L → Bone_Skirt016_L → Bone_Skirt017_L
- [裙摆] Bone_Skirt028_L ——（根不在本骨架：root not in this skeleton (variant ABP?)）

### ABP_Seq_Jinxi_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=30.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [裙摆] Bone_Skirt007_R → Bone_Skirt008_R
- [裙摆] Bone_Skirt001_L → Bone_Skirt002_L → Bone_Skirt003_L → Bone_Skirt004_L → Bone_Skirt005_L → Bone_Skirt006_L → Bone_Skirt007_L → Bone_Skirt008_L

## FemaleM/LinNai（R2T1LinnaeaMd20011）

- 骨总数 ?；骨族 ?
- KawaiiPhysics 组 0 个（ABP 变体 0 个），链 0 条
- KawaiiCurve：coat, hair, skirt
- 状态：{'skeleton': 'export_missing', 'abp_skipped_no_skeleton': ['ABP_Seq_LinNai_Phy']}

## FemaleM/LinNai（R2T1LinnaiMd10011）

- 骨总数 398；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 116, 'ribbon': 122, 'other:Prop': 11, 'hand_twist': 2, 'chest': 4, 'skirt': 24, 'other:Other': 13, 'prop': 1, 'other:Toe': 2, 'other:Foot': 2, 'other:Calf': 2}
- KawaiiPhysics 组 9 个（ABP 变体 1 个），链 67 条
- KawaiiCurve：coat, hair, skirt
- 未入组二级骨：126 根

### ABP_Seq_LinNai_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=50.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：Kawaii_alpha_S03

- [飘带(Piao)] Bone_Piao122_L → Bone_Piao123_L → Bone_Piao124_L → Bone_Piao125_L
- [飘带(Piao)] Bone_Piao122_L → Bone_Piao123_L → Bone_Piao124_L → Bone_Piao201_L → Bone_Piao202_L → Bone_Piao204_L
- [飘带(Piao)] Bone_Piao122_L → Bone_Piao123_L → Bone_Piao124_L → Bone_Piao201_L → Bone_Piao202_L → Bone_Piao211_L → Bone_Piao213_L → Bone_Piao214_L
- [飘带(Piao)] Bone_Piao122_L → Bone_Piao123_L → Bone_Piao124_L → Bone_Piao201_L → Bone_Piao202_L → Bone_Piao211_L → Bone_Piao216_L → Bone_Piao217_L
- [飘带(Piao)] Bone_Piao122_L → Bone_Piao123_L → Bone_Piao124_L → Bone_Piao201_L → Bone_Piao202_L → Bone_Piao211_L → Bone_Piao219_L → Bone_Piao220_L
- [飘带(Piao)] Bone_Piao122_L → Bone_Piao123_L → Bone_Piao124_L → Bone_Piao201_L → Bone_Piao202_L → Bone_Piao211_L → Bone_Piao222_L → Bone_Piao223_L
- [飘带(Piao)] Bone_Piao132_L → Bone_Piao133_L → Bone_Piao134_L → Bone_Piao135_L
- [飘带(Piao)] Bone_Piao132_L → Bone_Piao133_L → Bone_Piao191_L → Bone_Piao192_L → Bone_Piao193_L → Bone_Piao194_L
- [飘带(Piao)] Bone_Piao132_L → Bone_Piao133_L → Bone_Piao181_L → Bone_Piao182_L → Bone_Piao183_L → Bone_Piao184_L → Bone_Piao186_L → Bone_Piao187_L → Bone_Piao188_L
- [飘带(Piao)] Bone_Piao142_R → Bone_Piao143_R → Bone_Piao144_R → Bone_Piao145_R

### ABP_Seq_LinNai_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=50.0 Radius=1.2 HalfWidth=5.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S01

- [裙摆] Bone_Skirt002_L → Bone_Skirt003_L → Bone_Skirt004_L
- [裙摆] Bone_Skirt002_R → Bone_Skirt003_R → Bone_Skirt004_R
- [裙摆] Bone_Skirt012_L → Bone_Skirt013_L → Bone_Skirt014_L
- [裙摆] Bone_Skirt012_R → Bone_Skirt013_R → Bone_Skirt014_R
- [飘带(Piao)] Bone_Piao072_L → Bone_Piao073_L

### ABP_Seq_LinNai_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S02

- [裙摆] Bone_Skirt022_L → Bone_Skirt023_L → Bone_Skirt024_L
- [裙摆] Bone_Skirt022_R → Bone_Skirt023_R → Bone_Skirt024_R

### ABP_Seq_LinNai_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.2 Stiffness=0.15 LimitAngle=50.0 Radius=2.5 HalfWidth=3.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S04

- [头发] Bone_Hair203_L → Bone_Hair205_L → Bone_Hair206_L → Bone_Hair207_L → Bone_Hair208_L → Bone_Hair209_L
- [头发] Bone_Hair203_L → Bone_Hair205_L → Bone_Hair206_L → Bone_Hair241_L → Bone_Hair242_L → Bone_Hair243_L
- [头发] Bone_Hair203_L → Bone_Hair205_L → Bone_Hair206_L → Bone_Hair231_L → Bone_Hair232_L → Bone_Hair233_L
- [头发] Bone_Hair203_L → Bone_Hair205_L → Bone_Hair206_L → Bone_Hair182_L → Bone_Hair183_L → Bone_Hair184_L
- [头发] Bone_Hair203_L → Bone_Hair211_L → Bone_Hair213_L → Bone_Hair214_L → Bone_Hair215_L
- [头发] Bone_Hair203_M → Bone_Hair205_M → Bone_Hair206_M → Bone_Hair207_M
- [头发] Bone_Hair204_R → Bone_Hair205_R → Bone_Hair207_R → Bone_Hair208_R → Bone_Hair209_R → Bone_Hair210_R
- [头发] Bone_Hair204_R → Bone_Hair205_R → Bone_Hair207_R → Bone_Hair231_R → Bone_Hair232_R → Bone_Hair233_R
- [头发] Bone_Hair204_R → Bone_Hair205_R → Bone_Hair207_R → Bone_Hair211_R → Bone_Hair212_R → Bone_Hair213_R
- [头发] Bone_Hair204_R → Bone_Hair205_R → Bone_Hair207_R → Bone_Hair251_R → Bone_Hair252_R → Bone_Hair253_R → Bone_Hair254_R
- [飘带(Piao)] Bone_Piao005_L → Bone_Piao006_L → Bone_Piao007_L → Bone_Piao008_L → Bone_Piao009_L → Bone_Piao010_L
- [飘带(Piao)] Bone_Piao005_L → Bone_Piao011_L → Bone_Piao012_L
- [飘带(Piao)] Bone_Piao003_L → Bone_Piao004_L

### ABP_Seq_LinNai_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=30.0 Radius=1.0 HalfWidth=2.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S06

- [飘带(Piao)] Bone_Piao045_M → Bone_Piao046_M → Bone_Piao047_M → Bone_Piao048_M

### ABP_Seq_LinNai_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=0.8 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao271_L
- [飘带(Piao)] Bone_Piao273_L
- [飘带(Piao)] Bone_Piao275_L
- [飘带(Piao)] Bone_Piao277_L
- [飘带(Piao)] Bone_Piao271_R
- [飘带(Piao)] Bone_Piao273_R
- [飘带(Piao)] Bone_Piao275_R
- [飘带(Piao)] Bone_Piao277_R
- [飘带(Piao)] Bone_Piao095_R → Bone_Piao096_R
- [飘带(Piao)] Bone_Piao051_L → Bone_Piao052_L
- [飘带(Piao)] Bone_Piao261_R
- [飘带(Piao)] Bone_Piao261_L
- [飘带(Piao)] Bone_Piao017_L → Bone_Piao018_L
- [飘带(Piao)] Bone_Piao017_R → Bone_Piao018_R

### ABP_Seq_LinNai_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.3 Stiffness=0.65 LimitAngle=10.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [胸饰] Bone_Chest001_L → Bone_Chest002_L
- [胸饰] Bone_Chest001_R → Bone_Chest002_R

### ABP_Seq_LinNai_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.04 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S05

- [头发] Bone_Hair132_R → Bone_Hair133_R → Bone_Hair134_R → Bone_Hair135_R → Bone_Hair136_R
- [头发] Bone_Hair132_L → Bone_Hair133_L → Bone_Hair134_L

### ABP_Seq_LinNai_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.25
- Alpha 曲线：None

- [头发] Bone_Hair02_M → Bone_Hair03_M
- [头发] Bone_Hair12_M → Bone_Hair13_M
- [头发] Bone_Hair21_M → Bone_Hair22_M
- [头发] Bone_Hair62_R → Bone_Hair63_R
- [头发] Bone_Hair51_R → Bone_Hair52_R
- [头发] Bone_Hair72_R → Bone_Hair73_R
- [头发] Bone_Hair42_R → Bone_Hair43_R
- [头发] Bone_Hair82_R → Bone_Hair83_R
- [头发] Bone_Hair92_R → Bone_Hair93_R
- [头发] Bone_Hair102_R → Bone_Hair103_R
- [头发] Bone_Hair112_R → Bone_Hair113_R
- [头发] Bone_Hair122_R → Bone_Hair123_R
- [头发] Bone_Hair152_R → Bone_Hair153_R
- [头发] Bone_Hair42_L
- [头发] Bone_Hair146_L
- [头发] Bone_Hair142_L → Bone_Hair143_L
- [头发] Bone_Hair148_L → Bone_Hair149_L
- [头发] Bone_Hair102_M → Bone_Hair103_M → Bone_Hair104_M

## FemaleM/Lucy（R2T1LucyMd10011）

- 骨总数 326；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 109, 'facial': 20, 'hand_twist': 2, 'other:Hand': 4, 'ribbon': 70, 'chest': 4, 'prop': 1, 'other:Other': 16, 'pelvis_extra': 1}
- KawaiiPhysics 组 19 个（ABP 变体 1 个），链 64 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：79 根

### ABP_Seq_Lucy_Phy / AnimGraphNode_KawaiiPhysicsGroup_18

- 参数：Damping=0.2 Stiffness=0.15 LimitAngle=50.0 Radius=2.0 HalfWidth=3.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r8.964272
- 胶囊碰撞 6 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S03

- [头发] Bone_Hair055_L → Bone_Hair056_L → Bone_Hair057_L → Bone_Hair058_L
- [头发] Bone_Hair171_L → Bone_Hair172_L → Bone_Hair173_L
- [头发] Bone_Hair054_R → Bone_Hair055_R → Bone_Hair056_R → Bone_Hair057_R

### ABP_Seq_Lucy_Phy / AnimGraphNode_KawaiiPhysicsGroup_17

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=25.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：Kawaii_alpha_S02

- [飘带(Piao)] Bone_Piao094_L
- [飘带(Piao)] Bone_Piao092_L

### ABP_Seq_Lucy_Phy / AnimGraphNode_KawaiiPhysicsGroup_16

- 参数：Damping=0.15 Stiffness=0.05 LimitAngle=35.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：Kawaii_alpha_S02

- [飘带(Piao)] Bone_Piao095_L
- [飘带(Piao)] Bone_Piao093_L

### ABP_Seq_Lucy_Phy / AnimGraphNode_KawaiiPhysicsGroup_15

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=15.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：Kawaii_alpha_S02

- [飘带(Piao)] Bone_Piao102_L
- [飘带(Piao)] Bone_Piao104_L

### ABP_Seq_Lucy_Phy / AnimGraphNode_KawaiiPhysicsGroup_14

- 参数：Damping=0.15 Stiffness=0.05 LimitAngle=30.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：Kawaii_alpha_S02

- [飘带(Piao)] Bone_Piao103_L
- [飘带(Piao)] Bone_Piao105_L

### ABP_Seq_Lucy_Phy / AnimGraphNode_KawaiiPhysicsGroup_13

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=35.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：Kawaii_alpha_S02

- [飘带(Piao)] Bone_Piao093_R
- [飘带(Piao)] Bone_Piao095_R

### ABP_Seq_Lucy_Phy / AnimGraphNode_KawaiiPhysicsGroup_12

- 参数：Damping=0.15 Stiffness=0.05 LimitAngle=25.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：Kawaii_alpha_S02

- [飘带(Piao)] Bone_Piao092_R
- [飘带(Piao)] Bone_Piao094_R

### ABP_Seq_Lucy_Phy / AnimGraphNode_KawaiiPhysicsGroup_11

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=30.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：Kawaii_alpha_S02

- [飘带(Piao)] Bone_Piao103_R
- [飘带(Piao)] Bone_Piao105_R

### ABP_Seq_Lucy_Phy / AnimGraphNode_KawaiiPhysicsGroup_10

- 参数：Damping=0.15 Stiffness=0.05 LimitAngle=15.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：Kawaii_alpha_S02

- [飘带(Piao)] Bone_Piao102_R
- [飘带(Piao)] Bone_Piao104_R

### ABP_Seq_Lucy_Phy / AnimGraphNode_KawaiiPhysicsGroup_9

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=15.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 6 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S01

- [飘带(Piao)] Bone_Piao201_L → Bone_Piao202_L
- [飘带(Piao)] Bone_Piao211_L → Bone_Piao212_L
- [飘带(Piao)] Bone_Piao201_R → Bone_Piao202_R
- [飘带(Piao)] Bone_Piao211_R → Bone_Piao212_R
- [飘带(Piao)] Bone_Piao033_M → Bone_Piao034_M
- [飘带(Piao)] Bone_Piao036_L → Bone_Piao037_L
- [飘带(Piao)] Bone_Piao036_R → Bone_Piao037_R

### ABP_Seq_Lucy_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.15 Stiffness=0.05 LimitAngle=20.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 6 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S01

- [飘带(Piao)] Bone_Piao021_L → Bone_Piao022_L
- [飘带(Piao)] Bone_Piao033_L → Bone_Piao034_L
- [飘带(Piao)] Bone_Piao021_R → Bone_Piao022_R
- [飘带(Piao)] Bone_Piao033_R → Bone_Piao034_R
- [飘带(Piao)] Bone_Piao036_M → Bone_Piao037_M
- [飘带(Piao)] Bone_Piao025_L → Bone_Piao026_L
- [飘带(Piao)] Bone_Piao025_R → Bone_Piao026_R

### ABP_Seq_Lucy_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.15 Stiffness=0.05 LimitAngle=20.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S01

- [飘带(Piao)] Bone_Piao016_R → Bone_Piao017_R
- [飘带(Piao)] Bone_Piao003_L → Bone_Piao004_L
- [飘带(Piao)] Bone_Piao013_L → Bone_Piao014_L
- [飘带(Piao)] Bone_Piao003_R

### ABP_Seq_Lucy_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=15.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S01

- [飘带(Piao)] Bone_Piao016_L → Bone_Piao017_L
- [飘带(Piao)] Bone_Piao006_R → Bone_Piao007_R
- [飘带(Piao)] Bone_Piao013_R → Bone_Piao014_R
- [飘带(Piao)] Bone_Piao007_L

### ABP_Seq_Lucy_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.2 Stiffness=0.15 LimitAngle=50.0 Radius=2.0 HalfWidth=3.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.286243
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S05

- [头发] Bone_Hair063_L → Bone_Hair064_L → Bone_Hair065_L
- [头发] Bone_Hair083_L → Bone_Hair084_L
- [头发] Bone_Hair103_R → Bone_Hair104_R
- [头发] Bone_Hair083_R → Bone_Hair084_R

### ABP_Seq_Lucy_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.25 Stiffness=0.15 LimitAngle=50.0 Radius=2.0 HalfWidth=3.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.306873
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S04

- [头发] Bone_Hair002_M → Bone_Hair003_M → Bone_Hair004_M
- [头发] Bone_Hair041_M → Bone_Hair042_M → Bone_Hair043_M
- [头发] Bone_Hair111_L → Bone_Hair112_L → Bone_Hair113_L
- [头发] Bone_Hair022_M → Bone_Hair024_M → Bone_Hair025_M

### ABP_Seq_Lucy_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.2 Stiffness=0.15 LimitAngle=50.0 Radius=2.0 HalfWidth=3.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.306873
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S04

- [头发] Bone_Hair033_M → Bone_Hair034_M → Bone_Hair035_M
- [头发] Bone_Hair012_M → Bone_Hair013_M → Bone_Hair014_M
- [头发] Bone_Hair012_M → Bone_Hair016_M → Bone_Hair017_M → Bone_Hair018_M

### ABP_Seq_Lucy_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.15 LimitAngle=50.0 Radius=2.0 HalfWidth=3.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.629564
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S05

- [头发] Bone_Hair073_L → Bone_Hair074_L → Bone_Hair075_L
- [头发] Bone_Hair073_L → Bone_Hair076_L → Bone_Hair077_L → Bone_Hair078_L
- [头发] Bone_Hair103_L → Bone_Hair104_L
- [头发] Bone_Hair093_L → Bone_Hair094_L
- [头发] Bone_Hair063_R → Bone_Hair064_R → Bone_Hair065_R
- [头发] Bone_Hair063_R → Bone_Hair066_R → Bone_Hair067_R → Bone_Hair068_R

### ABP_Seq_Lucy_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.23 Stiffness=0.15 LimitAngle=50.0 Radius=2.0 HalfWidth=3.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.595159
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S05

- [头发] Bone_Hair093_L → Bone_Hair094_L
- [头发] Bone_Hair095_L → Bone_Hair096_L → Bone_Hair097_L
- [头发] Bone_Hair105_R → Bone_Hair106_R → Bone_Hair107_R
- [头发] Bone_Hair085_R → Bone_Hair086_R → Bone_Hair087_R
- [头发] Bone_Hair073_R → Bone_Hair074_R → Bone_Hair075_R

### ABP_Seq_Lucy_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.23 Stiffness=0.15 LimitAngle=50.0 Radius=2.0 HalfWidth=3.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.595159
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S05

- [飘带(Piao)] Bone_Piao081_L

## FemaleM/Lupa（R2T1LupaMd10011）

- 骨总数 258；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 53, 'hand_twist': 2, 'ribbon': 89, 'chest': 4, 'prop': 1, 'other:Tail': 9, 'pelvis_extra': 1}
- KawaiiPhysics 组 11 个（ABP 变体 1 个），链 49 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：67 根

### ABP_Seq_Lupa_Phy / AnimGraphNode_KawaiiPhysicsGroup_10

- 参数：Damping=0.25 Stiffness=0.15 LimitAngle=50.0 Radius=5.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [other:Tail] Bone_Tail004_M → Bone_Tail005_M → Bone_Tail006_M → Bone_Tail007_M → Bone_Tail008_M → Bone_Tail009_M

### ABP_Seq_Lupa_Phy / AnimGraphNode_KawaiiPhysicsGroup_9

- 参数：Damping=0.2 Stiffness=0.075 LimitAngle=35.0 Radius=2.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.131601
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_jianxin

- [头发] Bone_Hair040_L → Bone_Hair042_L
- [头发] Bone_Hair040_L → Bone_Hair044_L
- [头发] Bone_Hair036_L → Bone_Hair038_L → Bone_Hair046_L → Bone_Hair050_L
- [头发] Bone_Hair036_L → Bone_Hair038_L → Bone_Hair046_L → Bone_Hair052_L
- [头发] Bone_Hair041_R → Bone_Hair043_R
- [头发] Bone_Hair041_R → Bone_Hair045_R
- [头发] Bone_Hair037_R → Bone_Hair039_R → Bone_Hair047_R → Bone_Hair051_R
- [头发] Bone_Hair037_R → Bone_Hair039_R → Bone_Hair047_R → Bone_Hair053_R
- [头发] Bone_Hair033_R
- [头发] Bone_Hair031_R
- [头发] Bone_Hair032_L
- [头发] Bone_Hair030_L
- [头发] Bone_Hair054_R
- [头发] Bone_Hair029_L

### ABP_Seq_Lupa_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.2 Stiffness=0.1 LimitAngle=45.0 Radius=4.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_jianxin

- [飘带(Piao)] Bone_Piao029_L → Bone_Piao032_L → Bone_Piao035_L → Bone_Piao040_L
- [飘带(Piao)] Bone_Piao031_R → Bone_Piao034_R → Bone_Piao039_R → Bone_Piao044_R
- [飘带(Piao)] Bone_Piao033_M
- [飘带(Piao)] Bone_Piao036_L
- [飘带(Piao)] Bone_Piao041_R

### ABP_Seq_Lupa_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.3 Stiffness=0.65 LimitAngle=10.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [胸饰] Bone_Chest001_R → Bone_Chest002_R
- [胸饰] Bone_Chest001_L → Bone_Chest002_L

### ABP_Seq_Lupa_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=30.0 Radius=1.1 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.5
- Alpha 曲线：None

- [头发] Bone_Hair001_M → Bone_Hair002_M → Bone_Hair003_M
- [头发] Bone_Hair025_M → Bone_Hair026_M
- [头发] Bone_Hair006_L → Bone_Hair007_L
- [头发] Bone_Hair020_R → Bone_Hair012_R → Bone_Hair014_R
- [头发] Bone_Hair020_R → Bone_Hair016_R → Bone_Hair018_R
- [头发] Bone_Hair020_R → Bone_Hair022_R → Bone_Hair024_R
- [头发] Bone_Hair020_R → Bone_Hair012_R → Bone_Hair014_R
- [头发] Bone_Hair020_R → Bone_Hair016_R → Bone_Hair018_R
- [头发] Bone_Hair020_R → Bone_Hair022_R → Bone_Hair024_R

### ABP_Seq_Lupa_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：None

- [头发] Bone_Hair003_R → Bone_Hair004_R → Bone_Hair005_R
- [头发] Bone_Hair008_L → Bone_Hair009_L → Bone_Hair010_L
- [头发] Bone_Hair028_R
- [头发] Bone_Hair027_L

### ABP_Seq_Lupa_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.1 Stiffness=0.1 LimitAngle=15.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao018_M → Bone_Piao019_M → Bone_Piao020_M
- [飘带(Piao)] Bone_Piao016_M

### ABP_Seq_Lupa_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.2 Stiffness=0.3 LimitAngle=35.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao049_L → Bone_Piao051_L
- [飘带(Piao)] Bone_Piao050_R → Bone_Piao083_R
- [飘带(Piao)] Bone_Piao050_R → Bone_Piao052_R → Bone_Piao084_R
- [飘带(Piao)] Bone_Piao050_R → Bone_Piao052_R → Bone_Piao064_R → Bone_Piao082_R
- [飘带(Piao)] Bone_Piao086_R → Bone_Piao085_R

### ABP_Seq_Lupa_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=30.0 Radius=1.3 HalfWidth=5.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [飘带(Piao)] Bone_Piao002_L → Bone_Piao003_L → Bone_Piao089_L

### ABP_Seq_Lupa_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=15.0 Radius=1.1 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao011_L → Bone_Piao012_L → Bone_Piao013_L
- [飘带(Piao)] Bone_Piao008_R → Bone_Piao088_R
- [飘带(Piao)] Bone_Piao007_L → Bone_Piao087_L
- [飘带(Piao)] Bone_Piao017_M

### ABP_Seq_Lupa_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.25 Stiffness=0.2 LimitAngle=15.0 Radius=1.1 HalfWidth=5.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao080_R
- [飘带(Piao)] Bone_Piao081_L

## FemaleM/Maxiaofang（R2T1MaxiaofangMd10011）

- 骨总数 182；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 30, 'ribbon': 28, 'chest': 4, 'prop': 1, 'skirt': 20}
- KawaiiPhysics 组 9 个（ABP 变体 1 个），链 32 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：12 根

### ABP_Seq_Maxiaofang_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.18 Stiffness=0.03 LimitAngle=15.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r8.46067
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [头发] Bone_Hair019_R → Bone_Hair020_R
- [头发] Bone_Hair015_L → Bone_Hair016_L
- [头发] Bone_Hair001_L → Bone_Hair002_L → Bone_Hair003_L
- [头发] Bone_Hair005_L → Bone_Hair006_L
- [头发] Bone_Hair011_M → Bone_Hair012_M
- [头发] Bone_Hair008_R → Bone_Hair009_R
- [飘带(Piao)] Bone_Piao023_R → Bone_Piao024_R
- [飘带(Piao)] Bone_Piao020_L → Bone_Piao021_L

### ABP_Seq_Maxiaofang_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.15 Stiffness=0.05 LimitAngle=35.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [裙摆] Bone_Skirt007_R → Bone_Skirt008_R → Bone_Skirt009_R
- [裙摆] Bone_Skirt001_L → Bone_Skirt002_L → Bone_Skirt003_L → Bone_Skirt004_L
- [裙摆] Bone_Skirt011_L → Bone_Skirt012_L → Bone_Skirt013_L → Bone_Skirt014_L → Bone_Skirt015_L

### ABP_Seq_Maxiaofang_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.02 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao038_R
- [飘带(Piao)] Bone_Piao036_L
- [飘带(Piao)] Bone_Piao040_R
- [飘带(Piao)] Bone_Piao043_R
- [飘带(Piao)] Bone_Piao042_L
- [飘带(Piao)] Bone_Piao040_L

### ABP_Seq_Maxiaofang_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.2 Stiffness=0.02 LimitAngle=35.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao004_L → Bone_Piao005_L
- [飘带(Piao)] Bone_Piao008_R → Bone_Piao009_R

### ABP_Seq_Maxiaofang_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.3 Stiffness=0.06 LimitAngle=30.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao013_R → Bone_Piao014_R

### ABP_Seq_Maxiaofang_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.18 Stiffness=0.03 LimitAngle=15.0 Radius=1.25 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.2106705
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [头发] Bone_Hair040_R → Bone_Hair041_R → Bone_Hair042_R
- [头发] Bone_Hair032_R → Bone_Hair033_R → Bone_Hair034_R
- [飘带(Piao)] Bone_Piao032_R → Bone_Piao033_R → Bone_Piao034_R
- [飘带(Piao)] Bone_Piao025_L → Bone_Piao027_L → Bone_Piao028_L
- [飘带(Piao)] Bone_Piao025_L → Bone_Piao030_L
- [头发] Bone_Hair028_L → Bone_Hair029_L → Bone_Hair030_L
- [头发] Bone_Hair036_L → Bone_Hair037_L → Bone_Hair038_L

### ABP_Seq_Maxiaofang_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.18 Stiffness=0.03 LimitAngle=30.0 Radius=1.25 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [头发] Bone_Hair044_R → Bone_Hair045_R → Bone_Hair046_R

### ABP_Seq_Maxiaofang_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.45 Stiffness=0.5 LimitAngle=35.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [胸饰] L_ChestBone01 → L_ChestBone02
- [胸饰] R_ChestBone01 → R_ChestBone02

### ABP_Seq_Maxiaofang_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.15 Stiffness=0.05 LimitAngle=35.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [裙摆] Bone_Skirt024_M → Bone_Skirt025_M
- [裙摆] Bone_Skirt019_R → Bone_Skirt020_R

## FemaleM/Micai（R2T1MicaiMd10011）

- 骨总数 186；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 8, 'hair': 11, 'other:Hair_daiMao001': 1, 'other:Hair_daiMao002': 1, 'other:Hair_daiMao03': 1, 'other:Piao_L_erZhui01': 1, 'other:Piao_L_erZhui02': 1, 'other:Piao_L_erZhui03': 1, 'other:Piao_R_erZhui01': 1, 'other:Piao_R_erZhui02': 1, 'other:Piao_R_erZhui03': 1, 'other:Hair_L_niMao01': 1, 'other:Hair_R_niMao01': 1, 'other:Hair_L_liuhai01': 1, 'other:Hair_L_liuhai02': 1, 'other:Hair_L_liuhai09': 1, 'other:Hair_L_liuhai03': 1, 'other:Hair_L_liuhai04': 1, 'other:Hair_L_liuhai05': 1, 'other:Hair_L_liuhai06': 1, 'other:Hair_R_liuhai01': 1, 'other:Hair_R_liuhai03': 1, 'other:Hair_R_liuhai04': 1, 'other:Hair_R_liuhai02': 1, 'other:Hair_R_liuhai09': 1, 'other:Hair_R_liuhai05': 1, 'other:Hair_R_liuhai06': 1, 'other:Hair_L_liuhai07': 1, 'other:Hair_L_liuhai08': 1, 'other:Hair_R_liuhai07': 1, 'other:Hair_R_liuhai08': 1, 'other:Hair_L_liuhai12': 1, 'other:Hair_L_liuhai13': 1, 'other:Hair_L_liuhai14': 1, 'other:Hair_L_niMao02': 1, 'other:Hair_L_niMao03': 1, 'other:Hair_R_niMao02': 1, 'other:Hair_R_niMao03': 1, 'other:Hair_R_liuhai12': 1, 'other:Hair_R_liuhai13': 1, 'other:Hair_R_liuhai14': 1, 'other:Piao_L_clothA01': 1, 'other:Piao_L_clothA02': 1, 'other:Piao_L_clothA03': 1, 'other:Piao_L_clothA04': 1, 'other:Piao_chest01': 1, 'other:Piao_chest02': 1, 'other:Piao_chest03': 1, 'other:Piao_chest04': 1, 'other:Piao_R_clothA01': 1, 'other:Piao_R_clothA02': 1, 'other:Piao_R_clothA03': 1, 'other:Piao_R_clothA04': 1, 'other:Piao_chest_back01': 1, 'other:Piao_chest_back02': 1, 'other:Piao_L_C01': 1, 'other:Piao_L_C02': 1, 'other:Piao_L_C03': 1, 'other:Piao_L_C04': 1, 'other:Piao_L_C05': 1, 'other:Piao_L_C06': 1, 'other:Piao_L_C07': 1, 'other:Piao_R_C01': 1, 'other:Piao_R_C02': 1, 'other:Piao_R_C03': 1, 'other:Piao_R_C04': 1, 'other:Piao_R_C05': 1, 'other:Piao_R_C06': 1, 'other:Piao_R_C07': 1, 'prop': 1, 'other:Piao_L_bao01': 1, 'other:Piao_R_bao01': 1, 'other:Piao_pelvis': 1, 'other:Piao_L_pelvis01': 1, 'other:Piao_L_pelvis02': 1, 'other:Piao_R_pelvis01': 1, 'other:Piao_R_pelvis02': 1}
- KawaiiPhysics 组 8 个（ABP 变体 1 个），链 30 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：14 根

### ABP_Seq_Micai_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.2 Stiffness=0.03 LimitAngle=25.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.2786465
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [other:Hair_R_niMao01] Hair_R_niMao01
- [other:Hair_R_niMao02] Hair_R_niMao02 → Hair_R_niMao03
- [other:Hair_R_liuhai07] Hair_R_liuhai07 → Hair_R_liuhai08
- [other:Hair_L_liuhai12] Hair_L_liuhai12 → Hair_L_liuhai13 → Hair_L_liuhai14
- [other:Hair_R_liuhai05] Hair_R_liuhai05 → Hair_R_liuhai06
- [other:Hair_R_liuhai02] Hair_R_liuhai02 → Hair_R_liuhai09
- [other:Hair_R_liuhai03] Hair_R_liuhai03 → Hair_R_liuhai04
- [头发] Bone_Hair001_R → Bone_Hair002_R
- [头发] Bone_Hair002_M → Bone_Hair003_M
- [头发] Bone_Hair001_L → Bone_Hair002_L
- [other:Hair_L_liuhai03] Hair_L_liuhai03 → Hair_L_liuhai04
- [other:Hair_L_liuhai02] Hair_L_liuhai02 → Hair_L_liuhai09
- [other:Hair_L_liuhai05] Hair_L_liuhai05 → Hair_L_liuhai06
- [other:Hair_R_liuhai12] Hair_R_liuhai12 → Hair_R_liuhai13 → Hair_R_liuhai14
- [other:Hair_L_liuhai07] Hair_L_liuhai07 → Hair_L_liuhai08
- [other:Hair_L_niMao01] Hair_L_niMao01
- [other:Hair_L_niMao02] Hair_L_niMao02 → Hair_L_niMao03
- [other:Hair_daiMao001] Hair_daiMao001 → Hair_daiMao002 → Hair_daiMao03

### ABP_Seq_Micai_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.07 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [other:Piao_R_clothA01] Piao_R_clothA01 → Piao_R_clothA02 → Piao_R_clothA03 → Piao_R_clothA04
- [other:Piao_L_clothA01] Piao_L_clothA01 → Piao_L_clothA02 → Piao_L_clothA03 → Piao_L_clothA04

### ABP_Seq_Micai_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.3 Stiffness=0.05 LimitAngle=45.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 8 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_R_C03] Piao_R_C03 → Piao_R_C04 → Piao_R_C05 → Piao_R_C06 → Piao_R_C07
- [other:Piao_L_C03] Piao_L_C03 → Piao_L_C04 → Piao_L_C05 → Piao_L_C06 → Piao_L_C07

### ABP_Seq_Micai_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.05 Stiffness=0.02 LimitAngle=45.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_L_pelvis01] Piao_L_pelvis01 → Piao_L_pelvis02
- [other:Piao_R_pelvis01] Piao_R_pelvis01 → Piao_R_pelvis02

### ABP_Seq_Micai_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.3 Stiffness=0.05 LimitAngle=15.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_L_bao01] Piao_L_bao01
- [other:Piao_R_bao01] Piao_R_bao01

### ABP_Seq_Micai_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.3 Stiffness=0.05 LimitAngle=15.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_chest_back01] Piao_chest_back01 → Piao_chest_back02

### ABP_Seq_Micai_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.03 LimitAngle=25.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [other:Piao_L_erZhui01] Piao_L_erZhui01 → Piao_L_erZhui02 → Piao_L_erZhui03
- [other:Piao_R_erZhui01] Piao_R_erZhui01 → Piao_R_erZhui02 → Piao_R_erZhui03

### ABP_Seq_Micai_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.2 Stiffness=0.07 LimitAngle=20.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [other:Piao_chest02] Piao_chest02 → Piao_chest03 → Piao_chest04

## FemaleM/Nvzhu（R2T1PlayerFemaleMd10011）

- 骨总数 197；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'other:Hiar_L_F01': 1, 'other:Hiar_L_F02': 1, 'other:Hiar_L_F03': 1, 'other:Hiar_R_F01': 1, 'other:Hiar_R_F02': 1, 'other:Hiar_R_F03': 1, 'other:Hair_R_daimao01': 1, 'other:Hair_R_daimao02': 1, 'other:Hair_R_daimao03': 1, 'other:Hair_L_chuitiao01': 1, 'other:Hair_L_chuitiao02': 1, 'other:Hair_liuhai01': 1, 'other:Hair_liuhai02': 1, 'other:Hair_liuhai03': 1, 'other:Hair_R_chuitiao01': 1, 'other:Piao_R_hudiejie02': 1, 'other:Piao_R_hudiejie03': 1, 'other:Piao_L_hudiejie02': 1, 'other:Piao_L_hudiejie03': 1, 'other:Hair_M_daimao01': 1, 'other:Hair_M_daimao02': 1, 'other:Hair_M_daimao03': 1, 'other:Piao_L_erzhui': 1, 'other:Piao_R_erzhui': 1, 'other:Hair_M_B00': 1, 'other:Hair_M_B01': 1, 'other:Hair_R_B01': 1, 'hair': 22, 'other:Hair_R_B02': 1, 'other:Hair_R_B03': 1, 'other:Hair_R_B04': 1, 'other:Hair_R_B05': 1, 'other:Hair_R_B06': 1, 'ribbon': 15, 'other:Piao_R_hudiejie04': 1, 'other:Piao_R_hudiejie05': 1, 'other:Piao_L_hudiejie04': 1, 'other:Piao_L_hudiejie05': 1, 'other:Piao_R_lingjie01': 1, 'other:Piao_R_lingjie02': 1, 'other:Piao_R_lingjie03': 1, 'other:Piao_R_lingjie04': 1, 'other:Piao_L_lingjie01': 1, 'other:Piao_L_lingjie02': 1, 'other:Piao_L_lingjie03': 1, 'other:Piao_L_lingjie04': 1, 'chest': 4, 'other:Piao_L_F01': 1, 'prop': 1, 'other:skirt_M_B01': 1, 'other:skirt_M_B02': 1, 'other:skirt_L_MB01': 1, 'other:skirt_L_MB02': 1, 'other:skirt_R_MB01': 1, 'other:skirt_R_MB02': 1, 'other:skrit_L_F01': 1, 'other:skrit_L_F02': 1, 'other:skrit_L_F03': 1, 'other:Piao_F01': 1, 'other:Piao_F02': 1}
- KawaiiPhysics 组 9 个（ABP 变体 4 个），链 36 条
- KawaiiCurve：hair, skirt
- 未入组二级骨：14 根

### ABP_Seq_Nvzhu_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.2 Stiffness=0.06 LimitAngle=45.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 7 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [other:Hair_M_B00] Hair_M_B00 → Hair_M_B01 → Hair_R_B01 → Bone_Hair049 → Bone_Hair050
- [other:Hair_M_B00] Hair_M_B00 → Hair_M_B01 → Hair_R_B01 → Hair_R_B02 → Hair_R_B03 → Hair_R_B04 → Hair_R_B05 → Hair_R_B06
- [other:Hair_M_B00] Hair_M_B00 → Hair_M_B01 → Bone_Piao001_R → Bone_Piao002_R → Bone_Piao003_R → Bone_Piao004_R
- [other:Hair_M_B00] Hair_M_B00 → Hair_M_B01 → Bone_Piao001_L → Bone_Piao004_L → Bone_Piao003_L → Bone_Piao005_L → Bone_Piao006_L

### ABP_Seq_Nvzhu_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.5 Stiffness=0.06 LimitAngle=10.0 Radius=1.2 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.6586704
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [头发] Bone_Hair046 → Bone_Hair047
- [头发] Bone_Hair013_R → Bone_Hair014_R
- [头发] Bone_Hair043 → Bone_Hair044
- [other:Hair_R_daimao01] Hair_R_daimao01 → Hair_R_daimao02 → Hair_R_daimao03
- [other:Hair_M_daimao01] Hair_M_daimao01 → Hair_M_daimao02 → Hair_M_daimao03
- [other:Hiar_R_F01] Hiar_R_F01 → Hiar_R_F02 → Hiar_R_F03
- [头发] Bone_Hair002_R → Bone_Hair003_R
- [other:Hair_liuhai02] Hair_liuhai02 → Hair_liuhai03
- [头发] Bone_Hair002_L → Bone_Hair003_L
- [other:Hiar_L_F01] Hiar_L_F01 → Hiar_L_F02 → Hiar_L_F03
- [other:Piao_L_erzhui] Piao_L_erzhui
- [other:Piao_R_erzhui] Piao_R_erzhui
- [头发] Bone_Hair030_R → Bone_Hair031_R
- [other:Hair_R_chuitiao01] Hair_R_chuitiao01
- [头发] Bone_Hair040 → Bone_Hair041
- [other:Hair_L_chuitiao01] Hair_L_chuitiao01 → Hair_L_chuitiao02
- [头发] Bone_Hair034_R → Bone_Hair035_R → Bone_Hair036_R

### ABP_Seq_Nvzhu_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.05 Stiffness=0.05 LimitAngle=45.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r10.0839205
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao012_L → Bone_Piao013_L
- [飘带(Piao)] Bone_Piao009_L → Bone_Piao010_L
- [other:Piao_L_hudiejie04] Piao_L_hudiejie04 → Piao_L_hudiejie05
- [other:Piao_R_hudiejie04] Piao_R_hudiejie04 → Piao_R_hudiejie05

### ABP_Seq_Nvzhu_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.45 Stiffness=0.05 LimitAngle=45.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [胸饰] L_ChestBone01 → L_ChestBone02
- [胸饰] R_ChestBone01 → R_ChestBone02

### ABP_Seq_Nvzhu_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.1 Stiffness=0.03 LimitAngle=15.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_R_lingjie02] Piao_R_lingjie02 → Piao_R_lingjie03 → Piao_R_lingjie04
- [other:Piao_L_lingjie02] Piao_L_lingjie02 → Piao_L_lingjie03 → Piao_L_lingjie04

### ABP_Seq_Nvzhu_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.3 Stiffness=0.12 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_L_F01] Piao_L_F01
- [other:Piao_F02] Piao_F02

### ABP_Seq_Nvzhu_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.1 Stiffness=0.2 LimitAngle=38.0 Radius=1.0 HalfWidth=6.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [other:skrit_L_F01] skrit_L_F01 → skrit_L_F02 → skrit_L_F03

### ABP_Seq_Nvzhu_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.1 Stiffness=0.2 LimitAngle=38.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：None

- [other:skirt_R_MB01] skirt_R_MB01 → skirt_R_MB02
- [other:skirt_L_MB01] skirt_L_MB01 → skirt_L_MB02

### ABP_Seq_Nvzhu_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.05 Stiffness=0.05 LimitAngle=45.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r10.08392
- Alpha 曲线：None

- [other:Piao_L_hudiejie03] Piao_L_hudiejie03
- [other:Piao_R_hudiejie03] Piao_R_hudiejie03

## FemaleM/Nvzhu（R2T1PlayerFemaleMd20011）

- 骨总数 307；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'other:Hiar_L_F01': 1, 'other:Hiar_L_F02': 1, 'other:Hiar_L_F03': 1, 'other:Hiar_L_F04': 1, 'other:Hiar_R_F01': 1, 'other:Hiar_R_F02': 1, 'other:Hiar_R_F03': 1, 'other:Hair_R_daimao01': 1, 'other:Hair_R_daimao02': 1, 'other:Hair_R_daimao03': 1, 'hair': 56, 'other:Hair_liuhai01': 1, 'other:Hair_liuhai02': 1, 'other:Hair_liuhai03': 1, 'other:Hair_R_chuitiao01': 1, 'other:Hair_R_chuitiao002': 1, 'other:Hair_M_daimao01': 1, 'other:Hair_M_daimao02': 1, 'other:Hair_M_daimao03': 1, 'other:Piao_L_erzhui': 1, 'other:Piao_R_erzhui': 1, 'other:Hair_R_chuitiao004': 1, 'other:Hair_R_chuitiao003': 1, 'ribbon': 86, 'chest': 4, 'prop': 1, 'skirt': 39}
- KawaiiPhysics 组 9 个（ABP 变体 4 个），链 35 条
- KawaiiCurve：hair, skirt
- 未入组二级骨：174 根

### ABP_Seq_Nvzhu_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.2 Stiffness=0.06 LimitAngle=45.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 7 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [other:Hair_M_B00] Hair_M_B00 ——（根不在本骨架：root not in this skeleton (variant ABP?)）

### ABP_Seq_Nvzhu_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.5 Stiffness=0.06 LimitAngle=10.0 Radius=1.2 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.6586704
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [头发] Bone_Hair046 ——（根不在本骨架：root not in this skeleton (variant ABP?)）
- [头发] Bone_Hair013_R ——（根不在本骨架：root not in this skeleton (variant ABP?)）
- [头发] Bone_Hair043 ——（根不在本骨架：root not in this skeleton (variant ABP?)）
- [other:Hair_R_daimao01] Hair_R_daimao01 → Hair_R_daimao02 → Hair_R_daimao03
- [other:Hair_M_daimao01] Hair_M_daimao01 → Hair_M_daimao02 → Hair_M_daimao03
- [other:Hiar_R_F01] Hiar_R_F01 → Hiar_R_F02 → Hiar_R_F03
- [头发] Bone_Hair002_R → Bone_Hair003_R
- [other:Hair_liuhai02] Hair_liuhai02 → Hair_liuhai03
- [头发] Bone_Hair002_L → Bone_Hair003_L
- [other:Hiar_L_F01] Hiar_L_F01 → Hiar_L_F02 → Hiar_L_F03
- [other:Hiar_L_F01] Hiar_L_F01 → Hiar_L_F04
- [other:Piao_L_erzhui] Piao_L_erzhui
- [other:Piao_R_erzhui] Piao_R_erzhui
- [头发] Bone_Hair030_R → Bone_Hair031_R
- [头发] Bone_Hair030_R → Hair_R_chuitiao004 → Hair_R_chuitiao003
- [other:Hair_R_chuitiao01] Hair_R_chuitiao01 → Hair_R_chuitiao002
- [头发] Bone_Hair040 ——（根不在本骨架：root not in this skeleton (variant ABP?)）
- [other:Hair_L_chuitiao01] Hair_L_chuitiao01 ——（根不在本骨架：root not in this skeleton (variant ABP?)）
- [头发] Bone_Hair034_R → Bone_Hair035_R → Bone_Hair036_R

### ABP_Seq_Nvzhu_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.05 Stiffness=0.05 LimitAngle=45.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r10.0839205
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao012_L ——（根不在本骨架：root not in this skeleton (variant ABP?)）
- [飘带(Piao)] Bone_Piao009_L ——（根不在本骨架：root not in this skeleton (variant ABP?)）
- [other:Piao_L_hudiejie04] Piao_L_hudiejie04 ——（根不在本骨架：root not in this skeleton (variant ABP?)）
- [other:Piao_R_hudiejie04] Piao_R_hudiejie04 ——（根不在本骨架：root not in this skeleton (variant ABP?)）

### ABP_Seq_Nvzhu_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.45 Stiffness=0.05 LimitAngle=45.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [胸饰] L_ChestBone01 → L_ChestBone02
- [胸饰] R_ChestBone01 → R_ChestBone02

### ABP_Seq_Nvzhu_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.1 Stiffness=0.03 LimitAngle=15.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_R_lingjie02] Piao_R_lingjie02 ——（根不在本骨架：root not in this skeleton (variant ABP?)）
- [other:Piao_L_lingjie02] Piao_L_lingjie02 ——（根不在本骨架：root not in this skeleton (variant ABP?)）

### ABP_Seq_Nvzhu_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.3 Stiffness=0.12 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_L_F01] Piao_L_F01 ——（根不在本骨架：root not in this skeleton (variant ABP?)）
- [other:Piao_F02] Piao_F02 ——（根不在本骨架：root not in this skeleton (variant ABP?)）

### ABP_Seq_Nvzhu_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.1 Stiffness=0.2 LimitAngle=38.0 Radius=1.0 HalfWidth=6.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [other:skrit_L_F01] skrit_L_F01 ——（根不在本骨架：root not in this skeleton (variant ABP?)）

### ABP_Seq_Nvzhu_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.1 Stiffness=0.2 LimitAngle=38.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：None

- [other:skirt_R_MB01] skirt_R_MB01 ——（根不在本骨架：root not in this skeleton (variant ABP?)）
- [other:skirt_L_MB01] skirt_L_MB01 ——（根不在本骨架：root not in this skeleton (variant ABP?)）

### ABP_Seq_Nvzhu_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.05 Stiffness=0.05 LimitAngle=45.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r10.08392
- Alpha 曲线：None

- [other:Piao_L_hudiejie03] Piao_L_hudiejie03 ——（根不在本骨架：root not in this skeleton (variant ABP?)）
- [other:Piao_R_hudiejie03] Piao_R_hudiejie03 ——（根不在本骨架：root not in this skeleton (variant ABP?)）

## FemaleM/Qianxia（R2T1QianXiaMd10011）

- 骨总数 397；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hand_twist': 2, 'ribbon': 113, 'chest': 4, 'hair': 141, 'prop': 1, 'skirt': 32, 'other:Other': 4, 'pelvis_extra': 1}
- KawaiiPhysics 组 11 个（ABP 变体 1 个），链 75 条
- KawaiiCurve：coat, hair, skirt
- 未入组二级骨：88 根

### ABP_Seq_Qianxia_Phy / AnimGraphNode_KawaiiPhysicsGroup_10

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.25
- Alpha 曲线：None

- [头发] Bone_Hair008_R → Bone_Hair009_R → Bone_Hair010_R
- [头发] Bone_Hair005_R → Bone_Hair006_R
- [头发] Bone_Hair002_R → Bone_Hair003_R
- [头发] Bone_Hair002_M → Bone_Hair003_M
- [头发] Bone_Hair005_M → Bone_Hair006_M
- [头发] Bone_Hair002_L → Bone_Hair003_L
- [头发] Bone_Hair012_L → Bone_Hair013_L → Bone_Hair014_L
- [头发] Bone_Hair015_L → Bone_Hair016_L → Bone_Hair017_L

### ABP_Seq_Qianxia_Phy / AnimGraphNode_KawaiiPhysicsGroup_9

- 参数：Damping=0.2 Stiffness=0.15 LimitAngle=50.0 Radius=2.5 HalfWidth=3.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S03

- [头发] Bone_Hair033_L → Bone_Hair034_L → Bone_Hair035_L
- [头发] Bone_Hair052_L → Bone_Hair061_L → Bone_Hair062_L → Bone_Hair063_L → Bone_Hair064_L → Bone_Hair096_L
- [头发] Bone_Hair052_L → Bone_Hair053_L → Bone_Hair054_L → Bone_Hair055_L → Bone_Hair056_L → Bone_Hair097_L
- [头发] Bone_Hair052_L → Bone_Hair057_L → Bone_Hair058_L → Bone_Hair059_L → Bone_Hair060_L → Bone_Hair091_L
- [头发] Bone_Hair072_L → Bone_Hair085_L → Bone_Hair086_L → Bone_Hair087_L → Bone_Hair088_L → Bone_Hair092_L
- [头发] Bone_Hair072_L → Bone_Hair073_L → Bone_Hair074_L → Bone_Hair075_L → Bone_Hair076_L → Bone_Hair095_L
- [头发] Bone_Hair072_L → Bone_Hair077_L → Bone_Hair078_L → Bone_Hair079_L → Bone_Hair080_L → Bone_Hair094_L
- [头发] Bone_Hair072_L → Bone_Hair081_L → Bone_Hair082_L → Bone_Hair083_L → Bone_Hair084_L → Bone_Hair093_L
- [头发] Bone_Hair072_R → Bone_Hair073_R → Bone_Hair074_R → Bone_Hair075_R → Bone_Hair076_R → Bone_Hair095_R
- [头发] Bone_Hair072_R → Bone_Hair085_R → Bone_Hair086_R → Bone_Hair087_R → Bone_Hair088_R → Bone_Hair092_R
- [头发] Bone_Hair072_R → Bone_Hair077_R → Bone_Hair078_R → Bone_Hair079_R → Bone_Hair080_R → Bone_Hair094_R
- [头发] Bone_Hair072_R → Bone_Hair081_R → Bone_Hair082_R → Bone_Hair083_R → Bone_Hair084_R → Bone_Hair093_R
- [头发] Bone_Hair052_R → Bone_Hair053_R → Bone_Hair054_R → Bone_Hair055_R → Bone_Hair056_R → Bone_Hair097_R
- [头发] Bone_Hair052_R → Bone_Hair061_R → Bone_Hair062_R → Bone_Hair063_R → Bone_Hair064_R → Bone_Hair096_R
- [头发] Bone_Hair052_R → Bone_Hair057_R → Bone_Hair058_R → Bone_Hair059_R → Bone_Hair060_R → Bone_Hair091_R
- [头发] Bone_Hair033_R → Bone_Hair034_R → Bone_Hair035_R → Bone_Hair036_R → Bone_Hair037_R → Bone_Hair090_R
- [头发] Bone_Hair043_L → Bone_Hair044_L → Bone_Hair045_L → Bone_Hair046_L → Bone_Hair047_L → Bone_Hair098_L

### ABP_Seq_Qianxia_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=30.0 Radius=1.0 HalfWidth=2.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao048_R → Bone_Piao049_R
- [飘带(Piao)] Bone_Piao048_L → Bone_Piao049_L
- [飘带(Piao)] Bone_Piao043_M → Bone_Piao044_M

### ABP_Seq_Qianxia_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.2 Stiffness=0.04 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：None

- [头发] Bone_Hair023_L → Bone_Hair024_L → Bone_Hair025_L → Bone_Hair026_L
- [头发] Bone_Hair023_R → Bone_Hair024_R → Bone_Hair025_R → Bone_Hair026_R
- [飘带(Piao)] Bone_Piao062_R
- [飘带(Piao)] Bone_Piao064_R
- [飘带(Piao)] Bone_Piao066_R → Bone_Piao067_R
- [飘带(Piao)] Bone_Piao069_R → Bone_Piao070_R

### ABP_Seq_Qianxia_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=25.0 Radius=1.8 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao038_L → Bone_Piao039_L
- [飘带(Piao)] Bone_Piao038_M → Bone_Piao039_M
- [飘带(Piao)] Bone_Piao038_R → Bone_Piao039_R

### ABP_Seq_Qianxia_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=50.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S01

- [裙摆] Bone_Skirt002_R → Bone_Skirt003_R → Bone_Skirt004_R
- [裙摆] Bone_Skirt012_R → Bone_Skirt013_R → Bone_Skirt014_R
- [裙摆] Bone_Skirt002_L → Bone_Skirt003_L → Bone_Skirt004_L
- [裙摆] Bone_Skirt012_L → Bone_Skirt013_L → Bone_Skirt014_L
- [飘带(Piao)] Bone_Piao027_R → Bone_Piao028_R

### ABP_Seq_Qianxia_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=50.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S02

- [飘带(Piao)] Bone_Piao002_L → Bone_Piao003_L → Bone_Piao004_L → Bone_Piao005_L
- [飘带(Piao)] Bone_Piao002_R → Bone_Piao003_R → Bone_Piao004_R → Bone_Piao005_R
- [飘带(Piao)] Bone_Piao012_L → Bone_Piao013_L → Bone_Piao014_L → Bone_Piao015_L
- [飘带(Piao)] Bone_Piao012_L → Bone_Piao013_L → Bone_Piao073_L
- [飘带(Piao)] Bone_Piao012_L → Bone_Piao013_L → Bone_Piao071_L
- [飘带(Piao)] Bone_Piao012_L → Bone_Piao013_L → Bone_Piao072_L
- [飘带(Piao)] Bone_Piao012_R → Bone_Piao013_R → Bone_Piao014_R → Bone_Piao015_R
- [飘带(Piao)] Bone_Piao012_R → Bone_Piao013_R → Bone_Piao071_R
- [飘带(Piao)] Bone_Piao012_R → Bone_Piao013_R → Bone_Piao072_R
- [飘带(Piao)] Bone_Piao012_R → Bone_Piao013_R → Bone_Piao073_R

### ABP_Seq_Qianxia_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=40.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao052_R
- [飘带(Piao)] Bone_Piao053_R
- [飘带(Piao)] Bone_Piao054_R
- [飘带(Piao)] Bone_Piao055_R
- [飘带(Piao)] Bone_Piao052_L
- [飘带(Piao)] Bone_Piao053_L
- [飘带(Piao)] Bone_Piao054_L
- [飘带(Piao)] Bone_Piao055_L

### ABP_Seq_Qianxia_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S01

- [裙摆] Bone_Skirt022_R → Bone_Skirt023_R → Bone_Skirt024_R
- [裙摆] Bone_Skirt032_R → Bone_Skirt033_R → Bone_Skirt034_R
- [裙摆] Bone_Skirt032_L → Bone_Skirt033_L → Bone_Skirt034_L
- [裙摆] Bone_Skirt022_L → Bone_Skirt023_L → Bone_Skirt024_L

### ABP_Seq_Qianxia_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.6 Stiffness=0.15 LimitAngle=15.0 Radius=0.5 HalfWidth=2.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao047_R
- [飘带(Piao)] Bone_Piao046_R
- [飘带(Piao)] Bone_Piao044_R
- [飘带(Piao)] Bone_Piao047_L
- [飘带(Piao)] Bone_Piao046_L
- [飘带(Piao)] Bone_Piao044_L
- [飘带(Piao)] Bone_Piao032_L
- [飘带(Piao)] Bone_Piao032_R

### ABP_Seq_Qianxia_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=50.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S02

- [飘带(Piao)] Bone_Piao022_R → Bone_Piao023_R → Bone_Piao024_R → Bone_Piao025_R
- [飘带(Piao)] Bone_Piao002_M → Bone_Piao003_M → Bone_Piao004_M → Bone_Piao005_M
- [飘带(Piao)] Bone_Piao022_L → Bone_Piao023_L → Bone_Piao024_L → Bone_Piao025_L

## FemaleM/Qianxia（R2T1QianXiaMd20011）

- 骨总数 ?；骨族 ?
- KawaiiPhysics 组 0 个（ABP 变体 0 个），链 0 条
- KawaiiCurve：coat, hair, skirt
- 状态：{'skeleton': 'export_missing', 'abp_skipped_no_skeleton': ['ABP_Seq_Qianxia_Phy']}

## FemaleM/Sanhua（R2T1SanhuaMd10011）

- 骨总数 202；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'other:Piao_L_MaoZi01': 1, 'other:Piao_L_MaoZi02': 1, 'other:Piao_L_MaoZi03': 1, 'other:Piao_LongA01': 1, 'other:Piao_LongA02': 1, 'other:Piao_LongA03': 1, 'other:Piao_LongA04': 1, 'other:Piao_LongA05': 1, 'other:Piao_LongA06': 1, 'other:Piao_LongA07': 1, 'other:Piao_L_ErHuan01': 1, 'ribbon': 2, 'other:Hair_M_LiuHai01': 1, 'other:Hair_M_LiuHai02': 1, 'hair': 10, 'other:Hair_R_fabin01': 1, 'other:Hair_R_fabin02': 1, 'other:Hair_R_fabin03': 1, 'other:Hair_R_fabin04': 1, 'other:Hair_R_fabin005': 1, 'other:Hair_R_fabin006': 1, 'other:Piao_LongB01': 1, 'other:Piao_LongB02': 1, 'other:Piao_LongB03': 1, 'other:Piao_LongB04': 1, 'other:Piao_LongB05': 1, 'other:Piao_LongB06': 1, 'other:Piao_LongB07': 1, 'chest': 4, 'other:Piao_Bone15': 1, 'other:Piao_Bone17': 1, 'prop': 1, 'other:Skirt_L_B01': 1, 'other:Skirt_L_B02': 1, 'other:Skirt_L_B03': 1, 'other:Skirt_L_B04': 1, 'other:Skirt_L_B05': 1, 'other:Skirt_L_B06': 1, 'other:Skirt_L_B07': 1, 'other:Skirt_L_A01': 1, 'other:Skirt_L_A02': 1, 'other:Skirt_L_A03': 1, 'other:Skirt_L_A04': 1, 'other:Skirt_L_A05': 1, 'other:Skirt_L_A06': 1, 'other:Skirt_L_A07': 1, 'other:Piao_R_F01': 1, 'other:Piao_R_F02': 1, 'other:Piao_R_F03': 1, 'other:Piao_R_F04': 1, 'other:Piao_R_F05': 1, 'other:Skirt_R_B01': 1, 'other:Skirt_R_B02': 1, 'other:Skirt_R_B03': 1, 'other:Skirt_R_B04': 1, 'other:Skirt_R_B05': 1, 'other:Skirt_R_B06': 1, 'other:Skirt_R_B07': 1, 'other:Skirt_F_M01': 1, 'other:Skirt_F_M02': 1, 'other:Skirt_F_M03': 1, 'other:Skirt_R_A01': 1, 'other:Skirt_R_A02': 1, 'other:Skirt_R_A03': 1, 'other:Skirt_R_A04': 1, 'other:Skirt_R_A05': 1, 'other:Skirt_R_A06': 1, 'other:Skirt_R_A07': 1, 'other:Piao_L_F01': 1, 'other:Piao_L_F02': 1, 'other:Piao_L_F03': 1, 'other:Piao_L_B01': 1, 'other:Piao_L_B02': 1, 'other:Piao_L_B03': 1, 'other:Piao_L_B04': 1, 'other:Piao_L_B05': 1, 'other:Piao_L_B06': 1, 'other:Piao_R_B01': 1, 'other:Piao_R_B02': 1, 'other:Piao_R_B03': 1, 'other:Piao_R_B04': 1, 'other:Piao_R_B05': 1, 'other:Piao_R_B06': 1, 'other:Skirt_B_M01': 1, 'other:Skirt_B_M02': 1, 'other:Skirt_B_M03': 1, 'other:Skirt_B_M04': 1, 'other:Skirt_B_M05': 1, 'other:Skirt_B_M06': 1, 'other:Skirt_B_M07': 1}
- KawaiiPhysics 组 8 个（ABP 变体 2 个），链 23 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：37 根

### ABP_Seq_Sanhua_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.3 Stiffness=0.05 LimitAngle=30.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r6.5
- Alpha 曲线：None

- [头发] Bone_Hair012 → Bone_Hair013
- [头发] Bone_Hair003 → Bone_Hair009
- [头发] Bone_Hair002 → Bone_Hair007
- [头发] Bone_Hair001 → Bone_Hair004 → Bone_Hair005 → Bone_Hair006
- [other:Hair_M_LiuHai01] Hair_M_LiuHai01 → Hair_M_LiuHai02

### ABP_Seq_Sanhua_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.25 Stiffness=0.03 LimitAngle=35.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 8 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [other:Piao_LongA02] Piao_LongA02 → Piao_LongA03 → Piao_LongA04 → Piao_LongA05 → Piao_LongA06 → Piao_LongA07
- [other:Piao_LongB02] Piao_LongB02 → Piao_LongB03 → Piao_LongB04 → Piao_LongB05 → Piao_LongB06 → Piao_LongB07

### ABP_Seq_Sanhua_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.15 Stiffness=0.04 LimitAngle=35.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：None

- [other:Skirt_R_B04] Skirt_R_B04 → Skirt_R_B05 → Skirt_R_B06 → Skirt_R_B07
- [other:Skirt_B_M04] Skirt_B_M04 → Skirt_B_M05 → Skirt_B_M06 → Skirt_B_M07
- [other:Skirt_L_B04] Skirt_L_B04 → Skirt_L_B05 → Skirt_L_B06 → Skirt_L_B07
- [other:Skirt_L_A04] Skirt_L_A04 → Skirt_L_A05 → Skirt_L_A06 → Skirt_L_A07
- [other:Skirt_R_A04] Skirt_R_A04 → Skirt_R_A05 → Skirt_R_A06 → Skirt_R_A07

### ABP_Seq_Sanhua_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.15 Stiffness=0.04 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [other:Piao_L_B06] Piao_L_B06
- [other:Piao_R_B06] Piao_R_B06

### ABP_Seq_Sanhua_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.5 Stiffness=0.04 LimitAngle=20.0 Radius=1.0 HalfWidth=6.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：None

- [other:Skirt_F_M01] Skirt_F_M01 → Skirt_F_M02 → Skirt_F_M03

### ABP_Seq_Sanhua_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.15 Stiffness=0.04 LimitAngle=35.0 Radius=2.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_R_F03] Piao_R_F03 → Piao_R_F04 → Piao_R_F05

### ABP_Seq_Sanhua_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.15 Stiffness=0.03 LimitAngle=25.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_L_F02] Piao_L_F02 → Piao_L_F03
- [other:Piao_Bone15] Piao_Bone15 → Piao_Bone17

### ABP_Seq_Sanhua_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.3 Stiffness=0.04 LimitAngle=30.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r6.5
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [other:Hair_R_fabin005] Hair_R_fabin005 → Hair_R_fabin006
- [other:Hair_R_fabin02] Hair_R_fabin02 → Hair_R_fabin03 → Hair_R_fabin04
- [other:Piao_L_MaoZi02] Piao_L_MaoZi02 → Piao_L_MaoZi03
- [other:Piao_L_ErHuan01] Piao_L_ErHuan01
- [飘带(Piao)] Bone_Piao001_R → Bone_Piao002_R

## FemaleM/Sanhua（R2T1SanhuaMd20011）

- 骨总数 ?；骨族 ?
- KawaiiPhysics 组 0 个（ABP 变体 0 个），链 0 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 状态：{'skeleton': 'export_missing', 'abp_skipped_no_skeleton': ['ABP_Seq_Sanhua_Phy', 'ABP_Seq_Sanhua_Skin_1_Phy']}

## FemaleM/Shouanren（R2T1ShouanrenMd10011）

- 骨总数 243；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'ribbon': 59, 'hair': 38, 'chest': 4, 'prop': 1, 'skirt': 42}
- KawaiiPhysics 组 10 个（ABP 变体 1 个），链 53 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：53 根

### ABP_Seq_Shouanren_Phy / AnimGraphNode_KawaiiPhysicsGroup_9

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=30.0 Radius=2.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 6 / 平面碰撞 0
- Alpha 曲线：None

- [头发] Bone_Hair053_M → Bone_Hair054_M
- [飘带(Piao)] Bone_Piao023_R → Bone_Piao024_R → Bone_Piao025_R
- [飘带(Piao)] Bone_Piao023_L → Bone_Piao024_L → Bone_Piao025_L

### ABP_Seq_Shouanren_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：None

- [头发] Bone_Hair043_R → Bone_Hair044_R
- [头发] Bone_Hair043_L → Bone_Hair044_L

### ABP_Seq_Shouanren_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.1 Stiffness=0.03 LimitAngle=30.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r9.5
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao001_M
- [飘带(Piao)] Bone_Piao001_R
- [飘带(Piao)] Bone_Piao001_L
- [飘带(Piao)] Bone_Piao062
- [飘带(Piao)] Bone_Piao012_R → Bone_Piao013_R
- [飘带(Piao)] Bone_Piao012_L → Bone_Piao013_L

### ABP_Seq_Shouanren_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=30.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r8.25
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [头发] Bone_Hair002_M → Bone_Hair003_M
- [头发] Bone_Hair002_L
- [头发] Bone_Hair012_L
- [头发] Bone_Hair022_L
- [头发] Bone_Hair032_L
- [头发] Bone_Hair052_L
- [头发] Bone_Hair002_R
- [头发] Bone_Hair012_R
- [头发] Bone_Hair022_R
- [头发] Bone_Hair032_R
- [头发] Bone_Hair052_R

### ABP_Seq_Shouanren_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [裙摆] Bone_Skirt043_R
- [裙摆] Bone_Skirt043_L
- [裙摆] Bone_Skirt013_R → Bone_Skirt014_R → Bone_Skirt015_R
- [裙摆] Bone_Skirt013_L → Bone_Skirt014_L → Bone_Skirt015_L
- [飘带(Piao)] Bone_Piao053_R → Bone_Piao054_R
- [飘带(Piao)] Bone_Piao053_L → Bone_Piao054_L
- [裙摆] Bone_Skirt024_R → Bone_Skirt025_R
- [裙摆] Bone_Skirt024_L → Bone_Skirt025_L

### ABP_Seq_Shouanren_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [裙摆] Bone_Skirt032_R → Bone_Skirt033_R → Bone_Skirt034_R → Bone_Skirt035_R
- [裙摆] Bone_Skirt032_L → Bone_Skirt033_L → Bone_Skirt035_L → Bone_Skirt036_L

### ABP_Seq_Shouanren_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.35 Stiffness=0.36 LimitAngle=15.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [胸饰] Bone_Chest001_R → Bone_Chest002_R
- [胸饰] Bone_Chest001_L → Bone_Chest002_L

### ABP_Seq_Shouanren_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao031_L
- [飘带(Piao)] Bone_Piao032_L
- [飘带(Piao)] Bone_Piao033_L
- [飘带(Piao)] Bone_Piao035_L
- [飘带(Piao)] Bone_Piao036_L
- [飘带(Piao)] Bone_Piao037_L
- [飘带(Piao)] Bone_Piao041_L
- [飘带(Piao)] Bone_Piao031_R
- [飘带(Piao)] Bone_Piao032_R
- [飘带(Piao)] Bone_Piao033_R
- [飘带(Piao)] Bone_Piao035_R
- [飘带(Piao)] Bone_Piao036_R
- [飘带(Piao)] Bone_Piao037_R
- [飘带(Piao)] Bone_Piao041_R
- [飘带(Piao)] Bone_Piao021_M

### ABP_Seq_Shouanren_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=50.0 Radius=1.5 HalfWidth=4.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [裙摆] Bone_Skirt001_R → Bone_Skirt002_R → Bone_Skirt003_R
- [裙摆] Bone_Skirt001_L → Bone_Skirt002_L → Bone_Skirt003_L

### ABP_Seq_Shouanren_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=50.0 Radius=1.5 HalfWidth=4.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [飘带(Piao)] Bone_Piao071_R → Bone_Piao072_R → Bone_Piao073_R → Bone_Piao074_R → Bone_Piao075_R
- [飘带(Piao)] Bone_Piao071_L → Bone_Piao072_L → Bone_Piao073_L → Bone_Piao074_L → Bone_Piao075_L

## FemaleM/Suisui（R2T1SuisuiMd10011）

- 骨总数 320；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 70, 'ribbon': 91, 'facial': 20, 'hand_twist': 2, 'chest': 4, 'prop': 1, 'pelvis_extra': 1, 'skirt': 32}
- KawaiiPhysics 组 9 个（ABP 变体 1 个），链 63 条
- KawaiiCurve：hair, skirt, sleeve, strap
- 未入组二级骨：47 根

### ABP_Seq_Suisui_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.0 HalfWidth=3.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r8.5
- Alpha 曲线：Kawaii_alpha_S07

- [头发] Bone_Hair072_M
- [头发] Bone_Hair082_M → Bone_Hair083_M
- [头发] Bone_Hair152_R
- [头发] Bone_Hair152_L
- [头发] Bone_Hair062_R
- [头发] Bone_Hair062_L
- [头发] Bone_Hair042_R → Bone_Hair043_R
- [头发] Bone_Hair042_L → Bone_Hair043_L
- [头发] Bone_Hair052_R → Bone_Hair053_R
- [头发] Bone_Hair052_L → Bone_Hair053_L
- [头发] Bone_Hair142_R
- [头发] Bone_Hair142_L
- [头发] Bone_Hair032_R
- [头发] Bone_Hair022_R
- [头发] Bone_Hair012_R
- [头发] Bone_Hair002_R
- [头发] Bone_Hair002_L
- [头发] Bone_Hair012_L
- [头发] Bone_Hair022_L
- [头发] Bone_Hair032_L

### ABP_Seq_Suisui_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=30.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 5 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S06

- [飘带(Piao)] Bone_Piao144_R
- [飘带(Piao)] Bone_Piao242_R
- [飘带(Piao)] Bone_Piao152_R
- [飘带(Piao)] Bone_Piao201_M → Bone_Piao202_M
- [飘带(Piao)] Bone_Piao011_L
- [飘带(Piao)] Bone_Piao052_L
- [飘带(Piao)] Bone_Piao042_L → Bone_Piao043_L
- [飘带(Piao)] Bone_Piao211_L
- [飘带(Piao)] Bone_Piao231_L
- [飘带(Piao)] Bone_Piao221_L
- [飘带(Piao)] Bone_Piao072_L → Bone_Piao073_L
- [飘带(Piao)] Bone_Piao241_L
- [飘带(Piao)] Bone_Piao081_L
- [飘带(Piao)] Bone_Piao042_R → Bone_Piao043_R
- [飘带(Piao)] Bone_Piao052_R
- [飘带(Piao)] Bone_Piao231_R
- [飘带(Piao)] Bone_Piao211_R
- [飘带(Piao)] Bone_Piao221_R
- [飘带(Piao)] Bone_Piao072_R → Bone_Piao073_R
- [飘带(Piao)] Bone_Piao083_R
- [飘带(Piao)] Bone_Piao241_R

### ABP_Seq_Suisui_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.3 Stiffness=0.65 LimitAngle=10.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [胸饰] Bone_Chest001_L → Bone_Chest002_L
- [胸饰] Bone_Chest001_R → Bone_Chest002_R

### ABP_Seq_Suisui_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S02

- [飘带(Piao)] Bone_Piao181_L → Bone_Piao182_L → Bone_Piao183_L → Bone_Piao184_L → Bone_Piao185_L → Bone_Piao186_L → Bone_Piao187_L
- [裙摆] Bone_Skirt012_L → Bone_Skirt013_L → Bone_Skirt014_L → Bone_Skirt015_L → Bone_Skirt016_L → Bone_Skirt017_L → Bone_Skirt018_L → Bone_Skirt019_L
- [飘带(Piao)] Bone_Piao191_L → Bone_Piao192_L → Bone_Piao193_L → Bone_Piao194_L → Bone_Piao195_L
- [飘带(Piao)] Bone_Piao191_R → Bone_Piao192_R → Bone_Piao193_R → Bone_Piao194_R → Bone_Piao195_R
- [裙摆] Bone_Skirt012_R → Bone_Skirt013_R → Bone_Skirt014_R → Bone_Skirt015_R → Bone_Skirt016_R → Bone_Skirt017_R → Bone_Skirt018_R → Bone_Skirt019_R

### ABP_Seq_Suisui_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.2 Stiffness=0.15 LimitAngle=30.0 Radius=3.0 HalfWidth=3.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S03

- [头发] Bone_Hair093_M → Bone_Hair094_M → Bone_Hair091_L → Bone_Hair092_L → Bone_Hair093_L → Bone_Hair094_L
- [头发] Bone_Hair093_M → Bone_Hair094_M → Bone_Hair091_L → Bone_Hair092_L → Bone_Hair093_L → Bone_Hair131_L → Bone_Hair132_L → Bone_Hair133_L
- [头发] Bone_Hair093_M → Bone_Hair094_M → Bone_Hair091_R → Bone_Hair092_R → Bone_Hair093_R → Bone_Hair094_R → Bone_Hair101_R → Bone_Hair102_R
- [头发] Bone_Hair093_M → Bone_Hair094_M → Bone_Hair091_R → Bone_Hair092_R → Bone_Hair093_R → Bone_Hair094_R → Bone_Hair111_R → Bone_Hair112_R → Bone_Hair113_R
- [头发] Bone_Hair093_M → Bone_Hair094_M → Bone_Hair091_R → Bone_Hair092_R → Bone_Hair093_R → Bone_Hair094_R → Bone_Hair121_R → Bone_Hair122_R → Bone_Hair123_R
- [头发] Bone_Hair093_M → Bone_Hair094_M → Bone_Hair091_R → Bone_Hair092_R → Bone_Hair131_R → Bone_Hair132_R

### ABP_Seq_Suisui_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.2 Stiffness=0.25 LimitAngle=30.0 Radius=2.0 HalfWidth=3.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S04

- [飘带(Piao)] Bone_Piao092_L → Bone_Piao111_L → Bone_Piao112_L → Bone_Piao113_L → Bone_Piao114_L
- [飘带(Piao)] Bone_Piao092_L → Bone_Piao101_L → Bone_Piao102_L → Bone_Piao103_L
- [飘带(Piao)] Bone_Piao092_R → Bone_Piao111_R → Bone_Piao112_R → Bone_Piao113_R → Bone_Piao114_R
- [飘带(Piao)] Bone_Piao092_R → Bone_Piao101_R → Bone_Piao102_R → Bone_Piao103_R

### ABP_Seq_Suisui_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.1 LimitAngle=25.0 Radius=1.0 HalfWidth=2.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 5 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S05

- [飘带(Piao)] Bone_Piao162_R → Bone_Piao163_R → Bone_Piao164_R → Bone_Piao165_R → Bone_Piao166_R → Bone_Piao167_R
- [飘带(Piao)] Bone_Piao171_R → Bone_Piao172_R → Bone_Piao173_R
- [飘带(Piao)] Bone_Piao022_L → Bone_Piao023_L → Bone_Piao024_L → Bone_Piao025_L

### ABP_Seq_Suisui_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=50.0 Radius=1.2 HalfWidth=6.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S01

- [裙摆] Bone_Skirt001_R → Bone_Skirt002_R → Bone_Skirt003_R → Bone_Skirt004_R → Bone_Skirt005_R → Bone_Skirt006_R → Bone_Skirt007_R

### ABP_Seq_Suisui_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=50.0 Radius=1.2 HalfWidth=6.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S01

- [裙摆] Bone_Skirt001_L → Bone_Skirt002_L → Bone_Skirt003_L → Bone_Skirt004_L → Bone_Skirt005_L → Bone_Skirt006_L → Bone_Skirt007_L

## FemaleM/Taohua（R2T1TaohuaMd10011）

- 骨总数 184；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'other:Hair_M_liuHai01': 1, 'other:Hair_M_liuHai02': 1, 'other:Hair_M_liuHai03': 1, 'other:Piao_toushiA01': 1, 'other:Piao_toushiA02': 1, 'other:Piao_toushiA03': 1, 'other:Piao_toushiB01': 1, 'other:Piao_toushiB02': 1, 'other:Piao_toushiB03': 1, 'other:Hair_daiMao_short01': 1, 'other:Hair_daiMao_short02': 1, 'other:Hair_R_ceBingA01': 1, 'other:Hair_R_ceBingA02': 1, 'other:Hair_R_ceBingA03': 1, 'other:Hair_L_00': 1, 'other:Hair_L_01': 1, 'other:Hair_L_02': 1, 'other:Hair_L_03': 1, 'other:Hair_R_00': 1, 'other:Hair_R_01': 1, 'other:Hair_R_02': 1, 'other:Hair_R_03': 1, 'other:Hair_daiMao_long00': 1, 'other:Hair_daiMao_long01': 1, 'other:Hair_daiMao_long02': 1, 'other:Hair_daiMao_long03': 1, 'other:Hair_L_ceBingA01': 1, 'other:Hair_L_ceBingA02': 1, 'other:Hair_L_ceBingA03': 1, 'other:Hair_L_back00': 1, 'other:Hair_L_back01': 1, 'other:Hair_L_back02': 1, 'other:Hair_R_back00': 1, 'other:Hair_R_back01': 1, 'other:Hair_R_back02': 1, 'other:Hair_M_back01': 1, 'other:Hair_M_back02': 1, 'chest': 4, 'other:Piao_keProp': 1, 'other:Piao_L_chest01': 1, 'other:Piao_L_chest02': 1, 'other:Piao_R_chest01': 1, 'other:Piao_R_chest02': 1, 'other:Piao_L_yiXiuA01': 1, 'other:Piao_L_yiXiuA02': 1, 'other:Piao_L_yiXiuB01': 1, 'other:Piao_L_yiXiuB02': 1, 'other:Piao_L_yiXiuB03': 1, 'other:Piao_R_yiXiuA01': 1, 'other:Piao_R_yiXiuA02': 1, 'other:Piao_R_yiXiuB01': 1, 'other:Piao_R_yiXiuB02': 1, 'other:Piao_R_yiXiuB03': 1, 'prop': 1, 'other:Piao_R_bao01': 1, 'other:Piao_R_bao02': 1, 'other:Skirt_L_A01': 1, 'other:Skirt_L_A02': 1, 'other:Skirt_L_A03': 1, 'other:Piao_L_huDieJie01': 1, 'other:Piao_L_huDieJie02': 1, 'other:Skirt_L_B01': 1, 'other:Skirt_L_B02': 1, 'other:Skirt_L_B03': 1, 'other:Skirt_L_C01': 1, 'other:Skirt_L_C02': 1, 'other:Skirt_L_C03': 1, 'other:Skirt_R_A01': 1, 'other:Skirt_R_A02': 1, 'other:Skirt_R_A03': 1, 'other:Piao_R_huDieJie01': 1, 'other:Piao_R_huDieJie02': 1, 'other:Skirt_R_B01': 1, 'other:Skirt_R_B02': 1, 'other:Skirt_R_B03': 1, 'other:Skirt_R_C01': 1, 'other:Skirt_R_C02': 1, 'other:Skirt_R_C03': 1, 'other:Piao_R_guaShiA01': 1, 'other:Piao_R_guaShiA02': 1, 'other:Piao_R_guaShiA03': 1, 'other:Piao_R_guaShiA04': 1}
- KawaiiPhysics 组 9 个（ABP 变体 1 个），链 31 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：11 根

### ABP_Seq_Taohua_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.2 Stiffness=0.03 LimitAngle=25.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r6.0719833
- Alpha 曲线：None

- [other:Hair_daiMao_short01] Hair_daiMao_short01 → Hair_daiMao_short02
- [other:Hair_daiMao_long00] Hair_daiMao_long00 → Hair_daiMao_long01 → Hair_daiMao_long02 → Hair_daiMao_long03
- [other:Piao_toushiB01] Piao_toushiB01 → Piao_toushiB02 → Piao_toushiB03
- [other:Piao_toushiA01] Piao_toushiA01 → Piao_toushiA02 → Piao_toushiA03
- [other:Hair_M_liuHai01] Hair_M_liuHai01 → Hair_M_liuHai02 → Hair_M_liuHai03
- [other:Hair_R_ceBingA01] Hair_R_ceBingA01 → Hair_R_ceBingA02 → Hair_R_ceBingA03
- [other:Hair_R_back00] Hair_R_back00 → Hair_R_back01 → Hair_R_back02
- [other:Hair_L_ceBingA01] Hair_L_ceBingA01 → Hair_L_ceBingA02 → Hair_L_ceBingA03
- [other:Hair_L_back00] Hair_L_back00 → Hair_L_back01 → Hair_L_back02
- [other:Hair_M_back01] Hair_M_back01 → Hair_M_back02

### ABP_Seq_Taohua_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.4 Stiffness=0.1 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [胸饰] R_ChestBone01 → R_ChestBone02
- [胸饰] L_ChestBone01 → L_ChestBone02

### ABP_Seq_Taohua_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.3 Stiffness=0.05 LimitAngle=25.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：L_ChestBone01@r8.356375
- Alpha 曲线：None

- [other:Piao_R_chest01] Piao_R_chest01 → Piao_R_chest02
- [other:Piao_L_chest01] Piao_L_chest01 → Piao_L_chest02

### ABP_Seq_Taohua_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_R_yiXiuB02] Piao_R_yiXiuB02 → Piao_R_yiXiuB03
- [other:Piao_L_yiXiuB02] Piao_L_yiXiuB02 → Piao_L_yiXiuB03
- [other:Piao_R_yiXiuA02] Piao_R_yiXiuA02
- [other:Piao_L_yiXiuA02] Piao_L_yiXiuA02

### ABP_Seq_Taohua_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.15 Stiffness=0.03 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [other:Skirt_R_C01] Skirt_R_C01 → Skirt_R_C02 → Skirt_R_C03
- [other:Skirt_L_C01] Skirt_L_C01 → Skirt_L_C02 → Skirt_L_C03

### ABP_Seq_Taohua_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.15 Stiffness=0.03 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_R_guaShiA02] Piao_R_guaShiA02 → Piao_R_guaShiA03 → Piao_R_guaShiA04
- [other:Piao_R_bao01] Piao_R_bao01 → Piao_R_bao02

### ABP_Seq_Taohua_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.15 Stiffness=0.05 LimitAngle=35.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_R_bao02] Piao_R_bao02

### ABP_Seq_Taohua_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=45.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [other:Hair_R_00] Hair_R_00 → Hair_R_01 → Hair_R_02 → Hair_R_03
- [other:Hair_L_00] Hair_L_00 → Hair_L_01 → Hair_L_02 → Hair_L_03

### ABP_Seq_Taohua_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.15 Stiffness=0.03 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [other:Skirt_R_B02] Skirt_R_B02 → Skirt_R_B03
- [other:Skirt_R_A02] Skirt_R_A02 → Skirt_R_A03
- [other:Skirt_R_A02] Skirt_R_A02 → Piao_R_huDieJie01 → Piao_R_huDieJie02
- [other:Skirt_L_B02] Skirt_L_B02 → Skirt_L_B03
- [other:Skirt_L_A02] Skirt_L_A02 → Skirt_L_A03
- [other:Skirt_L_A02] Skirt_L_A02 → Piao_L_huDieJie01 → Piao_L_huDieJie02

## FemaleM/TestModel（TestModel01）

- 骨总数 197；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'other:Hiar_L_F01': 1, 'other:Hiar_L_F02': 1, 'other:Hiar_L_F03': 1, 'other:Hiar_R_F01': 1, 'other:Hiar_R_F02': 1, 'other:Hiar_R_F03': 1, 'other:Hair_R_daimao01': 1, 'other:Hair_R_daimao02': 1, 'other:Hair_R_daimao03': 1, 'other:Hair_L_chuitiao01': 1, 'other:Hair_L_chuitiao02': 1, 'other:Hair_liuhai01': 1, 'other:Hair_liuhai02': 1, 'other:Hair_liuhai03': 1, 'other:Hair_R_chuitiao01': 1, 'other:Piao_R_hudiejie02': 1, 'other:Piao_R_hudiejie03': 1, 'other:Piao_L_hudiejie02': 1, 'other:Piao_L_hudiejie03': 1, 'other:Hair_M_daimao01': 1, 'other:Hair_M_daimao02': 1, 'other:Hair_M_daimao03': 1, 'other:Piao_L_erzhui': 1, 'other:Piao_R_erzhui': 1, 'other:Hair_M_B00': 1, 'other:Hair_M_B01': 1, 'other:Hair_R_B01': 1, 'hair': 22, 'other:Hair_R_B02': 1, 'other:Hair_R_B03': 1, 'other:Hair_R_B04': 1, 'other:Hair_R_B05': 1, 'other:Hair_R_B06': 1, 'ribbon': 15, 'other:Piao_R_hudiejie04': 1, 'other:Piao_R_hudiejie05': 1, 'other:Piao_L_hudiejie04': 1, 'other:Piao_L_hudiejie05': 1, 'other:Piao_R_lingjie01': 1, 'other:Piao_R_lingjie02': 1, 'other:Piao_R_lingjie03': 1, 'other:Piao_R_lingjie04': 1, 'other:Piao_L_lingjie01': 1, 'other:Piao_L_lingjie02': 1, 'other:Piao_L_lingjie03': 1, 'other:Piao_L_lingjie04': 1, 'chest': 4, 'other:Piao_L_F01': 1, 'prop': 1, 'other:skirt_M_B01': 1, 'other:skirt_M_B02': 1, 'other:skirt_L_MB01': 1, 'other:skirt_L_MB02': 1, 'other:skirt_R_MB01': 1, 'other:skirt_R_MB02': 1, 'other:skrit_L_F01': 1, 'other:skrit_L_F02': 1, 'other:skrit_L_F03': 1, 'other:Piao_F01': 1, 'other:Piao_F02': 1}
- KawaiiPhysics 组 1 个（ABP 变体 1 个），链 15 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：98 根
- 状态：{'chains_source': 'skeleton_heuristic (ABP 无 KawaiiPhysicsGroup 节点)'}

### skeleton_heuristic / heuristic_family_chains

- 参数：Damping=None Stiffness=None LimitAngle=None Radius=None HalfWidth=None Shape=None

- [头发] Bone_Hair049 → Bone_Hair050
- [飘带(Piao)] Bone_Piao001_R → Bone_Piao002_R → Bone_Piao003_R → Bone_Piao004_R
- [飘带(Piao)] Bone_Piao001_L → Bone_Piao004_L → Bone_Piao003_L → Bone_Piao005_L → Bone_Piao006_L
- [头发] Bone_Hair001_L → Bone_Hair002_L → Bone_Hair003_L
- [头发] Bone_Hair001_R → Bone_Hair002_R → Bone_Hair003_R
- [头发] Bone_Hair013_R → Bone_Hair014_R
- [头发] Bone_Hair025_R → Bone_Hair030_R → Bone_Hair031_R
- [头发] Bone_Hair034_R → Bone_Hair035_R → Bone_Hair036_R
- [头发] Bone_Hair040 → Bone_Hair041
- [头发] Bone_Hair043 → Bone_Hair044
- [头发] Bone_Hair046 → Bone_Hair047
- [飘带(Piao)] Bone_Piao008_L → Bone_Piao009_L → Bone_Piao010_L
- [飘带(Piao)] Bone_Piao011_L → Bone_Piao012_L → Bone_Piao013_L
- [胸饰] R_ChestBone01 → R_ChestBone02
- [胸饰] L_ChestBone01 → L_ChestBone02

## FemaleM/TestModel（TestModel02）

- 骨总数 197；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'other:Hiar_L_F01': 1, 'other:Hiar_L_F02': 1, 'other:Hiar_L_F03': 1, 'other:Hiar_R_F01': 1, 'other:Hiar_R_F02': 1, 'other:Hiar_R_F03': 1, 'other:Hair_R_daimao01': 1, 'other:Hair_R_daimao02': 1, 'other:Hair_R_daimao03': 1, 'other:Hair_L_chuitiao01': 1, 'other:Hair_L_chuitiao02': 1, 'other:Hair_liuhai01': 1, 'other:Hair_liuhai02': 1, 'other:Hair_liuhai03': 1, 'other:Hair_R_chuitiao01': 1, 'other:Piao_R_hudiejie02': 1, 'other:Piao_R_hudiejie03': 1, 'other:Piao_L_hudiejie02': 1, 'other:Piao_L_hudiejie03': 1, 'other:Hair_M_daimao01': 1, 'other:Hair_M_daimao02': 1, 'other:Hair_M_daimao03': 1, 'other:Piao_L_erzhui': 1, 'other:Piao_R_erzhui': 1, 'other:Hair_M_B00': 1, 'other:Hair_M_B01': 1, 'other:Hair_R_B01': 1, 'hair': 22, 'other:Hair_R_B02': 1, 'other:Hair_R_B03': 1, 'other:Hair_R_B04': 1, 'other:Hair_R_B05': 1, 'other:Hair_R_B06': 1, 'ribbon': 15, 'other:Piao_R_hudiejie04': 1, 'other:Piao_R_hudiejie05': 1, 'other:Piao_L_hudiejie04': 1, 'other:Piao_L_hudiejie05': 1, 'other:Piao_R_lingjie01': 1, 'other:Piao_R_lingjie02': 1, 'other:Piao_R_lingjie03': 1, 'other:Piao_R_lingjie04': 1, 'other:Piao_L_lingjie01': 1, 'other:Piao_L_lingjie02': 1, 'other:Piao_L_lingjie03': 1, 'other:Piao_L_lingjie04': 1, 'chest': 4, 'other:Piao_L_F01': 1, 'prop': 1, 'other:skirt_M_B01': 1, 'other:skirt_M_B02': 1, 'other:skirt_L_MB01': 1, 'other:skirt_L_MB02': 1, 'other:skirt_R_MB01': 1, 'other:skirt_R_MB02': 1, 'other:skrit_L_F01': 1, 'other:skrit_L_F02': 1, 'other:skrit_L_F03': 1, 'other:Piao_F01': 1, 'other:Piao_F02': 1}
- KawaiiPhysics 组 1 个（ABP 变体 1 个），链 15 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：98 根
- 状态：{'chains_source': 'skeleton_heuristic (ABP 无 KawaiiPhysicsGroup 节点)'}

### skeleton_heuristic / heuristic_family_chains

- 参数：Damping=None Stiffness=None LimitAngle=None Radius=None HalfWidth=None Shape=None

- [头发] Bone_Hair049 → Bone_Hair050
- [飘带(Piao)] Bone_Piao001_R → Bone_Piao002_R → Bone_Piao003_R → Bone_Piao004_R
- [飘带(Piao)] Bone_Piao001_L → Bone_Piao004_L → Bone_Piao003_L → Bone_Piao005_L → Bone_Piao006_L
- [头发] Bone_Hair001_L → Bone_Hair002_L → Bone_Hair003_L
- [头发] Bone_Hair001_R → Bone_Hair002_R → Bone_Hair003_R
- [头发] Bone_Hair013_R → Bone_Hair014_R
- [头发] Bone_Hair025_R → Bone_Hair030_R → Bone_Hair031_R
- [头发] Bone_Hair034_R → Bone_Hair035_R → Bone_Hair036_R
- [头发] Bone_Hair040 → Bone_Hair041
- [头发] Bone_Hair043 → Bone_Hair044
- [头发] Bone_Hair046 → Bone_Hair047
- [飘带(Piao)] Bone_Piao008_L → Bone_Piao009_L → Bone_Piao010_L
- [飘带(Piao)] Bone_Piao011_L → Bone_Piao012_L → Bone_Piao013_L
- [胸饰] R_ChestBone01 → R_ChestBone02
- [胸饰] L_ChestBone01 → L_ChestBone02

## FemaleM/Xiakong（R2T1XiakongMd10011）

- 骨总数 273；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 10, 'hair': 40, 'hand_twist': 2, 'ribbon': 89, 'chest': 4, 'prop': 1, 'skirt': 34, 'pelvis_extra': 1}
- KawaiiPhysics 组 12 个（ABP 变体 1 个），链 78 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：57 根

### ABP_Seq_Xiakong_Phy / AnimGraphNode_KawaiiPhysicsGroup_11

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=35.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [飘带(Piao)] Bone_Piao016_L → Bone_Piao018_L → Bone_Piao024_L → Bone_Piao030_L → Bone_Piao031_L
- [飘带(Piao)] Bone_Piao016_L → Bone_Piao018_L → Bone_Piao024_L → Bone_Piao033_L → Bone_Piao034_L
- [飘带(Piao)] Bone_Piao016_L → Bone_Piao018_L → Bone_Piao026_L → Bone_Piao027_L → Bone_Piao028_L
- [飘带(Piao)] Bone_Piao016_L → Bone_Piao020_L → Bone_Piao021_L → Bone_Piao022_L
- [飘带(Piao)] Bone_Piao016_R → Bone_Piao018_R → Bone_Piao024_R → Bone_Piao030_R → Bone_Piao031_R
- [飘带(Piao)] Bone_Piao016_R → Bone_Piao018_R → Bone_Piao024_R → Bone_Piao033_R → Bone_Piao034_R
- [飘带(Piao)] Bone_Piao016_R → Bone_Piao018_R → Bone_Piao026_R → Bone_Piao027_R → Bone_Piao028_R
- [飘带(Piao)] Bone_Piao016_R → Bone_Piao020_R → Bone_Piao021_R → Bone_Piao022_R

### ABP_Seq_Xiakong_Phy / AnimGraphNode_KawaiiPhysicsGroup_10

- 参数：Damping=0.5 Stiffness=0.15 LimitAngle=15.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [胸饰] Bone_Chest001_R → Bone_Chest002_R
- [胸饰] Bone_Chest001_L → Bone_Chest002_L

### ABP_Seq_Xiakong_Phy / AnimGraphNode_KawaiiPhysicsGroup_9

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=35.0 Radius=3.0 HalfWidth=4.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [裙摆] Bone_Skirt014_L → Bone_Skirt015_L → Bone_Skirt016_L
- [裙摆] Bone_Skirt014_R → Bone_Skirt015_R → Bone_Skirt016_R
- [裙摆] Bone_Skirt023_L
- [裙摆] Bone_Skirt023_R
- [裙摆] Bone_Skirt003_R → Bone_Skirt004_R
- [裙摆] Bone_Skirt003_L → Bone_Skirt004_L
- [裙摆] Bone_Skirt008_R → Bone_Skirt009_R
- [裙摆] Bone_Skirt008_L → Bone_Skirt009_L

### ABP_Seq_Xiakong_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=25.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao009_L
- [飘带(Piao)] Bone_Piao009_R
- [飘带(Piao)] Bone_Piao003_L
- [飘带(Piao)] Bone_Piao003_R
- [飘带(Piao)] Bone_Piao005_L
- [飘带(Piao)] Bone_Piao005_R
- [飘带(Piao)] Bone_Piao011_L
- [飘带(Piao)] Bone_Piao011_R

### ABP_Seq_Xiakong_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.2 Stiffness=0.15 LimitAngle=30.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao036_L
- [飘带(Piao)] Bone_Piao038_L
- [飘带(Piao)] Bone_Piao036_R
- [飘带(Piao)] Bone_Piao038_R

### ABP_Seq_Xiakong_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.25 Stiffness=0.15 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao073_L
- [飘带(Piao)] Bone_Piao073_R
- [飘带(Piao)] Bone_Piao064_R
- [飘带(Piao)] Bone_Piao074_L
- [飘带(Piao)] Bone_Piao074_R

### ABP_Seq_Xiakong_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.1 Stiffness=0.1 LimitAngle=25.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao068_L
- [飘带(Piao)] Bone_Piao068_R
- [飘带(Piao)] Bone_Piao054_L
- [飘带(Piao)] Bone_Piao054_R
- [飘带(Piao)] Bone_Piao078_L
- [飘带(Piao)] Bone_Piao078_R
- [飘带(Piao)] Bone_Piao042_L
- [飘带(Piao)] Bone_Piao042_R

### ABP_Seq_Xiakong_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=25.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao066_L
- [飘带(Piao)] Bone_Piao066_R
- [飘带(Piao)] Bone_Piao072_L
- [飘带(Piao)] Bone_Piao072_R
- [飘带(Piao)] Bone_Piao051_L
- [飘带(Piao)] Bone_Piao051_R
- [飘带(Piao)] Bone_Piao079_L
- [飘带(Piao)] Bone_Piao079_R
- [飘带(Piao)] Bone_Piao042_L
- [飘带(Piao)] Bone_Piao042_R

### ABP_Seq_Xiakong_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.15 Stiffness=0.05 LimitAngle=25.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao070_L
- [飘带(Piao)] Bone_Piao070_R
- [飘带(Piao)] Bone_Piao056_L
- [飘带(Piao)] Bone_Piao056_R
- [飘带(Piao)] Bone_Piao059_L
- [飘带(Piao)] Bone_Piao059_R
- [飘带(Piao)] Bone_Piao050_L
- [飘带(Piao)] Bone_Piao050_R

### ABP_Seq_Xiakong_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=35.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r8.096541
- Alpha 曲线：None

- [头发] Bone_Hair002_M → Bone_Hair003_M
- [头发] Bone_Hair005_R → Bone_Hair006_R
- [头发] Bone_Hair005_L → Bone_Hair006_L
- [头发] Bone_Hair007_R → Bone_Hair008_R
- [头发] Bone_Hair007_L → Bone_Hair008_L

### ABP_Seq_Xiakong_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=35.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [头发] Bone_Hair019_L → Bone_Hair020_L
- [头发] Bone_Hair019_L → Bone_Hair022_L
- [头发] Bone_Hair018_R → Bone_Hair019_R

### ABP_Seq_Xiakong_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.15 Stiffness=0.15 LimitAngle=20.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r8.616549
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [头发] Bone_Hair012_R → Bone_Hair013_R
- [头发] Bone_Hair012_L
- [头发] Bone_Hair015_R
- [头发] Bone_Hair015_L
- [头发] Bone_Hair010_R → Bone_Hair011_R
- [头发] Bone_Hair011_L → Bone_Hair012_L
- [头发] Bone_Hair014_R
- [头发] Bone_Hair013_L
- [头发] Bone_Hair005_M → Bone_Hair006_M

## FemaleM/Xuanling（R2T1XuanlingMd10011）

- 骨总数 333；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 72, 'ribbon': 92, 'facial': 20, 'hand_twist': 2, 'chest': 4, 'prop': 1, 'skirt': 34, 'other:Other': 8, 'pelvis_extra': 1}
- KawaiiPhysics 组 15 个（ABP 变体 1 个），链 78 条
- KawaiiCurve：hair, skirt, sleeve
- 未入组二级骨：35 根

### ABP_Seq_Xuanling_Phy / AnimGraphNode_KawaiiPhysicsGroup_14

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=0.8 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S03

- [飘带(Piao)] Bone_Piao011_R → Bone_Piao012_R → Bone_Piao013_R → Bone_Piao014_R → Bone_Piao073_R
- [飘带(Piao)] Bone_Piao011_R → Bone_Piao012_R → Bone_Piao013_R → Bone_Piao014_R → Bone_Piao077_R
- [飘带(Piao)] Bone_Piao011_R → Bone_Piao012_R → Bone_Piao013_R → Bone_Piao072_R
- [飘带(Piao)] Bone_Piao011_R → Bone_Piao012_R → Bone_Piao013_R → Bone_Piao076_R
- [飘带(Piao)] Bone_Piao011_R → Bone_Piao012_R → Bone_Piao071_R
- [飘带(Piao)] Bone_Piao011_R → Bone_Piao012_R → Bone_Piao075_R
- [飘带(Piao)] Bone_Piao011_R → Bone_Piao070_R
- [飘带(Piao)] Bone_Piao011_R → Bone_Piao074_R
- [飘带(Piao)] Bone_Piao011_L → Bone_Piao012_L → Bone_Piao013_L → Bone_Piao014_L → Bone_Piao073_L
- [飘带(Piao)] Bone_Piao011_L → Bone_Piao012_L → Bone_Piao013_L → Bone_Piao014_L → Bone_Piao077_L
- [飘带(Piao)] Bone_Piao011_L → Bone_Piao012_L → Bone_Piao013_L → Bone_Piao072_L
- [飘带(Piao)] Bone_Piao011_L → Bone_Piao012_L → Bone_Piao013_L → Bone_Piao076_L
- [飘带(Piao)] Bone_Piao011_L → Bone_Piao012_L → Bone_Piao071_L
- [飘带(Piao)] Bone_Piao011_L → Bone_Piao012_L → Bone_Piao075_L
- [飘带(Piao)] Bone_Piao011_L → Bone_Piao070_L
- [飘带(Piao)] Bone_Piao011_L → Bone_Piao074_L

### ABP_Seq_Xuanling_Phy / AnimGraphNode_KawaiiPhysicsGroup_13

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=50.0 Radius=1.2 HalfWidth=5.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S04

- [裙摆] Bone_Skirt001_R → Bone_Skirt002_R → Bone_Skirt003_R → Bone_Skirt004_R → Bone_Skirt005_R
- [裙摆] Bone_Skirt001_L → Bone_Skirt002_L → Bone_Skirt003_L → Bone_Skirt004_L → Bone_Skirt005_L

### ABP_Seq_Xuanling_Phy / AnimGraphNode_KawaiiPhysicsGroup_12

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S06

- [裙摆] Bone_Skirt011_L → Bone_Skirt012_L → Bone_Skirt013_L → Bone_Skirt014_L → Bone_Skirt015_L → Bone_Skirt016_L
- [裙摆] Bone_Skirt002_M → Bone_Skirt020_R → Bone_Skirt021_R → Bone_Skirt022_R → Bone_Skirt023_R
- [裙摆] Bone_Skirt002_M → Bone_Skirt020_L → Bone_Skirt021_L → Bone_Skirt022_L → Bone_Skirt023_L
- [裙摆] Bone_Skirt011_R → Bone_Skirt012_R → Bone_Skirt013_R → Bone_Skirt014_R → Bone_Skirt015_R → Bone_Skirt016_R
- [飘带(Piao)] Bone_Piao061_L → Bone_Piao062_L → Bone_Piao063_L → Bone_Piao064_L → Bone_Piao065_L → Bone_Piao066_L → Bone_Piao067_L
- [飘带(Piao)] Bone_Piao002_M → Bone_Piao003_M → Bone_Piao004_M
- [飘带(Piao)] Bone_Piao061_R → Bone_Piao062_R → Bone_Piao063_R → Bone_Piao064_R → Bone_Piao065_R → Bone_Piao066_R → Bone_Piao067_R

### ABP_Seq_Xuanling_Phy / AnimGraphNode_KawaiiPhysicsGroup_11

- 参数：Damping=0.15 Stiffness=0.05 LimitAngle=20.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S07

- [飘带(Piao)] Bone_Piao001_R → Bone_Piao002_R
- [飘带(Piao)] Bone_Piao003_L → Bone_Piao004_L
- [飘带(Piao)] Bone_Piao005_R → Bone_Piao006_R

### ABP_Seq_Xuanling_Phy / AnimGraphNode_KawaiiPhysicsGroup_10

- 参数：Damping=0.2 Stiffness=0.15 LimitAngle=50.0 Radius=2.5 HalfWidth=3.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S09

- [飘带(Piao)] Bone_Piao042_R → Bone_Piao043_R → Bone_Piao044_R → Bone_Piao045_R → Bone_Piao046_R → Bone_Piao047_R
- [飘带(Piao)] Bone_Piao042_R → Bone_Piao050_R → Bone_Piao051_R → Bone_Piao052_R → Bone_Piao053_R → Bone_Piao054_R

### ABP_Seq_Xuanling_Phy / AnimGraphNode_KawaiiPhysicsGroup_9

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.6561913
- Alpha 曲线：Kawaii_alpha_S08

- [头发] Bone_Hair010_R → Bone_Hair011_R → Bone_Hair012_R
- [头发] Bone_Hair003_R → Bone_Hair004_R
- [头发] Bone_Hair001_R → Bone_Hair002_R
- [头发] Bone_Hair001_L → Bone_Hair002_L
- [头发] Bone_Hair003_L → Bone_Hair004_L
- [头发] Bone_Hair010_L → Bone_Hair011_L → Bone_Hair012_L
- [头发] Bone_Hair001_M → Bone_Hair002_M → Bone_Hair003_M

### ABP_Seq_Xuanling_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.2 Stiffness=0.04 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r8.197037
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S08

- [头发] Bone_Hair021_R → Bone_Hair022_R → Bone_Hair023_R → Bone_Hair024_R
- [头发] Bone_Hair021_R → Bone_Hair032_R → Bone_Hair033_R
- [飘带(Piao)] Bone_Piao030_R → Bone_Piao031_R → Bone_Piao032_R
- [头发] Bone_Hair090_R → Bone_Hair091_R → Bone_Hair092_R
- [头发] Bone_Hair030_L → Bone_Hair031_L → Bone_Hair032_L → Bone_Hair033_L
- [头发] Bone_Hair040_L → Bone_Hair041_L → Bone_Hair042_L
- [头发] Bone_Hair043_L → Bone_Hair044_L
- [头发] Bone_Hair020_L → Bone_Hair021_L → Bone_Hair022_L → Bone_Hair023_L
- [头发] Bone_Hair020_L → Bone_Hair021_L → Bone_Hair050_L → Bone_Hair051_L
- [头发] Bone_Hair020_L → Bone_Hair021_L → Bone_Hair052_L → Bone_Hair053_L

### ABP_Seq_Xuanling_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.3 Stiffness=0.65 LimitAngle=10.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [胸饰] Bone_Chest001_L → Bone_Chest002_L
- [胸饰] Bone_Chest001_R → Bone_Chest002_R

### ABP_Seq_Xuanling_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=15.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S01

- [飘带(Piao)] Bone_Piao074_L
- [飘带(Piao)] Bone_Piao075_R
- [飘带(Piao)] Bone_Piao072_L
- [飘带(Piao)] Bone_Piao077_R

### ABP_Seq_Xuanling_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.15 Stiffness=0.05 LimitAngle=20.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S01

- [飘带(Piao)] Bone_Piao074_R
- [飘带(Piao)] Bone_Piao076_R
- [飘带(Piao)] Bone_Piao073_L
- [飘带(Piao)] Bone_Piao077_L

### ABP_Seq_Xuanling_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.15 Stiffness=0.05 LimitAngle=20.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S01

- [飘带(Piao)] Bone_Piao070_R
- [飘带(Piao)] Bone_Piao071_L
- [飘带(Piao)] Bone_Piao072_R
- [飘带(Piao)] Bone_Piao076_L

### ABP_Seq_Xuanling_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=15.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S01

- [飘带(Piao)] Bone_Piao070_L
- [飘带(Piao)] Bone_Piao075_L
- [飘带(Piao)] Bone_Piao071_R
- [飘带(Piao)] Bone_Piao073_R

### ABP_Seq_Xuanling_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=50.0 Radius=1.2 HalfWidth=5.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S05

- [飘带(Piao)] Bone_Piao022_R → Bone_Piao023_R → Bone_Piao024_R → Bone_Piao025_R → Bone_Piao026_R → Bone_Piao027_R
- [飘带(Piao)] Bone_Piao022_L → Bone_Piao023_L → Bone_Piao024_L → Bone_Piao025_L → Bone_Piao026_L → Bone_Piao027_L

### ABP_Seq_Xuanling_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=15.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S07

- [飘带(Piao)] Bone_Piao001_L → Bone_Piao002_L
- [飘带(Piao)] Bone_Piao005_L → Bone_Piao006_L
- [飘带(Piao)] Bone_Piao003_R → Bone_Piao004_R

### ABP_Seq_Xuanling_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.2 Stiffness=0.15 LimitAngle=50.0 Radius=2.5 HalfWidth=3.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S02

- [头发] Bone_Hair062_L → Bone_Hair063_L → Bone_Hair064_L → Bone_Hair065_L → Bone_Hair066_L
- [头发] Bone_Hair062_L → Bone_Hair063_L → Bone_Hair064_L → Bone_Hair081_L → Bone_Hair082_L → Bone_Hair083_L
- [头发] Bone_Hair062_L → Bone_Hair063_L → Bone_Hair064_L → Bone_Hair071_L → Bone_Hair072_L
- [头发] Bone_Hair012_M → Bone_Hair013_M
- [头发] Bone_Hair062_R → Bone_Hair063_R → Bone_Hair064_R → Bone_Hair065_R → Bone_Hair066_R
- [头发] Bone_Hair062_R → Bone_Hair063_R → Bone_Hair064_R → Bone_Hair070_R → Bone_Hair071_R → Bone_Hair072_R
- [头发] Bone_Hair062_R → Bone_Hair063_R → Bone_Hair064_R → Bone_Hair073_R → Bone_Hair074_R
- [飘带(Piao)] Bone_Piao003_L → Bone_Piao004_L

## FemaleM/Yangyang（R2T1YangyangMd10011）

- 骨总数 177；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'other:HairLong_Bone01': 1, 'other:HairLong_Bone02': 1, 'other:HairLong_Bone03': 1, 'other:HairLong_Bone04': 1, 'other:HairLong_Bone05': 1, 'other:HairLong_Bone06': 1, 'other:HairLong_Bone20': 1, 'other:HairLong_Bone21': 1, 'other:HairLong_Bone22': 1, 'other:HairLong_Bone23': 1, 'other:HairLong_Bone24': 1, 'other:HairLong_Bone25': 1, 'other:HairF_Bone17': 1, 'other:HairF_Bone18': 1, 'other:HairF_Bone19': 1, 'other:HairF_Bone13': 1, 'other:HairF_Bone14': 1, 'other:HairF_Bone15': 1, 'other:HairF_Bone09': 1, 'other:HairF_Bone10': 1, 'other:HairF_Bone11': 1, 'other:HairF_Bone05': 1, 'other:HairF_Bone06': 1, 'other:HairF_Bone07': 1, 'other:HairF_Bone21': 1, 'other:HairF_Bone22': 1, 'other:HairF_Bone23': 1, 'other:Fadai_Bone01': 1, 'other:Fadai_Bone02': 1, 'other:Fadai_Bone03': 1, 'other:HairF_Bone01': 1, 'other:HairF_Bone02': 1, 'other:HairF_Bone03': 1, 'other:Fadai_Bone08': 1, 'other:Fadai_Bone09': 1, 'other:Fadai_Bone11': 1, 'other:Fadai_Bone12': 1, 'ribbon': 4, 'chest': 4, 'prop': 1, 'other:R_Skirt_Bone09': 1, 'other:R_Skirt_Bone10': 1, 'other:R_Skirt_Bone11': 1, 'other:R_Skirt_Bone12': 1, 'other:R_Skirt_Bone13': 1, 'other:R_Skirt_Bone14': 1, 'other:R_Skirt_Bone19': 1, 'other:R_Skirt_Bone20': 1, 'other:R_Skirt_Bone21': 1, 'other:R_Skirt_Bone27': 1, 'other:R_Skirt_Bone28': 1, 'other:R_Skirt_Bone29': 1, 'other:L_Skirt_Bone09': 1, 'other:L_Skirt_Bone10': 1, 'other:L_Skirt_Bone11': 1, 'other:L_Skirt_Bone12': 1, 'other:L_Skirt_Bone13': 1, 'other:L_Skirt_Bone14': 1, 'other:L_Skirt_Bone19': 1, 'other:L_Skirt_Bone20': 1, 'other:L_Skirt_Bone21': 1, 'other:L_Skirt_Bone22': 1, 'other:L_Skirt_Bone23': 1, 'other:L_Skirt_Bone24': 1, 'other:R_Skirt_Bone38': 1, 'other:R_Skirt_Bone39': 1, 'other:R_Skirt_Bone40': 1, 'other:R_Skirt_Bone41': 1, 'other:L_Skirt_Bone38': 1, 'other:L_Skirt_Bone39': 1, 'other:L_Skirt_Bone40': 1, 'other:L_Skirt_Bone41': 1}
- KawaiiPhysics 组 8 个（ABP 变体 1 个），链 21 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：17 根

### ABP_Seq_Yangyang_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.8 Stiffness=0.45 LimitAngle=30.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [胸饰] L_ChestBone01 → L_ChestBone02
- [胸饰] R_ChestBone01 → R_ChestBone02

### ABP_Seq_Yangyang_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.1 Stiffness=0.05 LimitAngle=30.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r6.6586704
- Alpha 曲线：None

- [other:HairF_Bone17] HairF_Bone17 → HairF_Bone18 → HairF_Bone19
- [other:HairF_Bone14] HairF_Bone14 → HairF_Bone15
- [other:HairF_Bone10] HairF_Bone10 → HairF_Bone11
- [other:HairF_Bone05] HairF_Bone05 → HairF_Bone06 → HairF_Bone07
- [other:HairF_Bone21] HairF_Bone21 → HairF_Bone22 → HairF_Bone23
- [other:HairF_Bone01] HairF_Bone01 → HairF_Bone02 → HairF_Bone03

### ABP_Seq_Yangyang_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.05 Stiffness=0.05 LimitAngle=45.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [other:Fadai_Bone01] Fadai_Bone01 → Fadai_Bone02 → Fadai_Bone03
- [other:Fadai_Bone08] Fadai_Bone08 → Fadai_Bone09
- [other:Fadai_Bone12] Fadai_Bone12

### ABP_Seq_Yangyang_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.1 Stiffness=0.03 LimitAngle=25.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [other:L_Skirt_Bone39] L_Skirt_Bone39 → L_Skirt_Bone40 → L_Skirt_Bone41
- [other:R_Skirt_Bone39] R_Skirt_Bone39 → R_Skirt_Bone40 → R_Skirt_Bone41

### ABP_Seq_Yangyang_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.2 Stiffness=0.12 LimitAngle=35.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao001_R → Bone_Piao003_R
- [飘带(Piao)] Bone_Piao001_R → Bone_Piao005_R

### ABP_Seq_Yangyang_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=35.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：None

- [other:L_Skirt_Bone24] L_Skirt_Bone24
- [other:R_Skirt_Bone29] R_Skirt_Bone29

### ABP_Seq_Yangyang_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.15 Stiffness=0.04 LimitAngle=35.0 Radius=3.0 HalfWidth=7.5 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 5 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [other:L_Skirt_Bone09] L_Skirt_Bone09 → L_Skirt_Bone10 → L_Skirt_Bone11 → L_Skirt_Bone12 → L_Skirt_Bone13 → L_Skirt_Bone14
- [other:R_Skirt_Bone09] R_Skirt_Bone09 → R_Skirt_Bone10 → R_Skirt_Bone11 → R_Skirt_Bone12 → R_Skirt_Bone13 → R_Skirt_Bone14

### ABP_Seq_Yangyang_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.3 Stiffness=0.05 LimitAngle=30.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 9 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [other:HairLong_Bone01] HairLong_Bone01 → HairLong_Bone02 → HairLong_Bone03 → HairLong_Bone04 → HairLong_Bone05 → HairLong_Bone06
- [other:HairLong_Bone20] HairLong_Bone20 → HairLong_Bone21 → HairLong_Bone22 → HairLong_Bone23 → HairLong_Bone24 → HairLong_Bone25

## FemaleM/Yangyang（R2T1YangyangwumaoMd10011）

- 骨总数 ?；骨族 ?
- KawaiiPhysics 组 0 个（ABP 变体 0 个），链 0 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 状态：{'skeleton': 'export_missing', 'abp_skipped_no_skeleton': ['ABP_Seq_Yangyang_Phy']}

## FemaleM/YinLin（R2T1YinlinMd10011）

- 骨总数 190；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 30, 'ribbon': 26, 'other:Other': 4, 'chest': 4, 'prop': 1, 'skirt': 26}
- KawaiiPhysics 组 10 个（ABP 变体 1 个），链 24 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：27 根

### ABP_Seq_YinLin_Phy / AnimGraphNode_KawaiiPhysicsGroup_9

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=35.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r6.5
- Alpha 曲线：None

- [头发] Bone_Hair011_R → Bone_Hair013_R
- [头发] Bone_Hair011_R → Bone_Hair012_R
- [头发] Bone_Hair002_R
- [头发] Bone_Hair002_M → Bone_Hair003_M
- [头发] Bone_Hair012_L → Bone_Hair013_L

### ABP_Seq_YinLin_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.1 Stiffness=0.05 LimitAngle=25.0 Radius=1.8 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [头发] Bone_Hair068_R → Bone_Hair069_R

### ABP_Seq_YinLin_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.1 Stiffness=0.04 LimitAngle=35.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao044_L → Bone_Piao045_L → Bone_Piao046_L
- [飘带(Piao)] Bone_Piao048_L

### ABP_Seq_YinLin_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.1 Stiffness=0.05 LimitAngle=25.0 Radius=1.8 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao001_L
- [飘带(Piao)] Bone_Piao004_L

### ABP_Seq_YinLin_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.1 Stiffness=0.05 LimitAngle=25.0 Radius=1.8 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao008_L → Bone_Piao009_L → Bone_Piao010_L

### ABP_Seq_YinLin_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.1 Stiffness=0.05 LimitAngle=25.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao016_R → Bone_Piao017_R
- [飘带(Piao)] Bone_Piao025_R
- [飘带(Piao)] Bone_Piao022_M
- [飘带(Piao)] Bone_Piao019_L
- [飘带(Piao)] Bone_Piao013_L → Bone_Piao014_L

### ABP_Seq_YinLin_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=15.0 Radius=1.8 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao057_R → Bone_Piao058_R → Bone_Piao059_R
- [飘带(Piao)] Bone_Piao052_L → Bone_Piao053_L → Bone_Piao054_L

### ABP_Seq_YinLin_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=35.0 Radius=1.5 HalfWidth=3.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 5 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [裙摆] Bone_Skirt001_R → Bone_Skirt002_R → Bone_Skirt003_R → Bone_Skirt004_R → Bone_Skirt005_R → Bone_Skirt006_R
- [裙摆] Bone_Skirt001_L → Bone_Skirt002_L → Bone_Skirt003_L → Bone_Skirt004_L → Bone_Skirt005_L → Bone_Skirt006_L

### ABP_Seq_YinLin_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.3 LimitAngle=35.0 Radius=4.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 7 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [头发] Bone_Hair051_M → Bone_Hair052_M → Bone_Hair053_M → Bone_Hair054_M → Bone_Hair031_R → Bone_Hair032_R → Bone_Hair033_R → Bone_Hair041_R → Bone_Hair042_R
- [头发] Bone_Hair051_M → Bone_Hair052_M → Bone_Hair053_M → Bone_Hair054_M → Bone_Hair031_L → Bone_Hair032_L → Bone_Hair033_L → Bone_Hair038_L → Bone_Hair039_L

### ABP_Seq_YinLin_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.05 Stiffness=0.05 LimitAngle=35.0 Radius=1.0 HalfWidth=4.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [裙摆] Bone_Skirt014_R → Bone_Skirt015_R → Bone_Skirt016_R
- [裙摆] Bone_Skirt014_L → Bone_Skirt015_L → Bone_Skirt016_L

## FemaleM/Younuo（R2T1YounuoMd10011）

- 骨总数 379；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 82, 'ribbon': 130, 'hand_twist': 2, 'chest': 4, 'prop': 1, 'skirt': 24, 'other:Toe': 28, 'other:Other': 8, 'pelvis_extra': 1}
- KawaiiPhysics 组 11 个（ABP 变体 1 个），链 41 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：147 根

### ABP_Seq_Younuo_Phy / AnimGraphNode_KawaiiPhysicsGroup_10

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=25.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [飘带(Piao)] Bone_Piao043_M → Bone_Piao044_M → Bone_Piao045_M

### ABP_Seq_Younuo_Phy / AnimGraphNode_KawaiiPhysicsGroup_9

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=25.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 10 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_jianxin

- [头发] Bone_Hair062_L → Bone_Hair063_L
- [头发] Bone_Hair054_L → Bone_Hair055_L → Bone_Hair056_L → Bone_Hair057_L → Bone_Hair058_L → Bone_Hair059_L
- [头发] Bone_Hair054_L → Bone_Hair055_L → Bone_Hair056_L → Bone_Hair091_L → Bone_Hair092_L → Bone_Hair093_L
- [头发] Bone_Hair054_L → Bone_Hair055_L → Bone_Hair081_L → Bone_Hair082_L → Bone_Hair083_L
- [头发] Bone_Hair054_L → Bone_Hair055_L → Bone_Hair071_L → Bone_Hair072_L
- [头发] Bone_Hair062_R → Bone_Hair063_R
- [头发] Bone_Hair054_R → Bone_Hair055_R → Bone_Hair056_R → Bone_Hair057_R → Bone_Hair058_R → Bone_Hair059_R
- [头发] Bone_Hair054_R → Bone_Hair055_R → Bone_Hair056_R → Bone_Hair091_R → Bone_Hair092_R → Bone_Hair093_R
- [头发] Bone_Hair054_R → Bone_Hair055_R → Bone_Hair081_R → Bone_Hair082_R → Bone_Hair083_R
- [头发] Bone_Hair054_R → Bone_Hair055_R → Bone_Hair071_R → Bone_Hair072_R

### ABP_Seq_Younuo_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=30.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao024_L → Bone_Piao025_L
- [飘带(Piao)] Bone_Piao024_R → Bone_Piao025_R

### ABP_Seq_Younuo_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=25.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao033_R → Bone_Piao034_R → Bone_Piao035_R
- [飘带(Piao)] Bone_Piao033_L → Bone_Piao034_L → Bone_Piao035_L

### ABP_Seq_Younuo_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=0.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_jianxin

- [飘带(Piao)] Bone_Piao103_R → Bone_Piao104_R → Bone_Piao105_R → Bone_Piao106_R → Bone_Piao107_R
- [飘带(Piao)] Bone_Piao113_R → Bone_Piao114_R → Bone_Piao115_R → Bone_Piao116_R → Bone_Piao117_R
- [飘带(Piao)] Bone_Piao123_R → Bone_Piao124_R → Bone_Piao125_R → Bone_Piao126_R → Bone_Piao127_R
- [飘带(Piao)] Bone_Piao123_L → Bone_Piao124_L → Bone_Piao125_L → Bone_Piao126_L → Bone_Piao127_L
- [飘带(Piao)] Bone_Piao113_L → Bone_Piao114_L → Bone_Piao115_L → Bone_Piao116_L → Bone_Piao117_L
- [飘带(Piao)] Bone_Piao103_L → Bone_Piao104_L → Bone_Piao105_L → Bone_Piao106_L → Bone_Piao107_L

### ABP_Seq_Younuo_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.25 Stiffness=0.2 LimitAngle=30.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r5.860047
- Alpha 曲线：None

- [头发] Bone_Hair042_R → Bone_Hair043_R
- [头发] Bone_Hair032_R → Bone_Hair033_R
- [头发] Bone_Hair013_R → Bone_Hair014_R
- [头发] Bone_Hair022_R → Bone_Hair023_R → Bone_Hair024_R
- [头发] Bone_Hair003_L → Bone_Hair005_L
- [头发] Bone_Hair003_L → Bone_Hair031_L → Bone_Hair032_L → Bone_Hair033_L
- [头发] Bone_Hair003_L → Bone_Hair041_L → Bone_Hair042_L → Bone_Hair043_L
- [头发] Bone_Hair003_L → Bone_Hair021_L → Bone_Hair022_L → Bone_Hair023_L → Bone_Hair024_L

### ABP_Seq_Younuo_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.3 Stiffness=0.65 LimitAngle=10.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [胸饰] Bone_Chest001_L → Bone_Chest002_L
- [胸饰] Bone_Chest001_R → Bone_Chest002_R

### ABP_Seq_Younuo_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=35.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r8.0
- Alpha 曲线：Kawaii_alpha_jianxin

- [头发] Bone_Hair012_M → Bone_Hair013_M → Bone_Hair014_M

### ABP_Seq_Younuo_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.25 Stiffness=0.2 LimitAngle=30.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [飘带(Piao)] Bone_Piao135_R → Bone_Piao136_R → Bone_Piao137_R → Bone_Piao138_R → Bone_Piao139_R
- [飘带(Piao)] Bone_Piao135_L → Bone_Piao136_L → Bone_Piao137_L → Bone_Piao138_L → Bone_Piao139_L

### ABP_Seq_Younuo_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao091_L → Bone_Piao092_L → Bone_Piao093_L
- [飘带(Piao)] Bone_Piao091_R → Bone_Piao092_R → Bone_Piao093_R
- [飘带(Piao)] Bone_Piao084_M → Bone_Piao085_M

### ABP_Seq_Younuo_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.25 Stiffness=0.2 LimitAngle=30.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r6.75
- Alpha 曲线：None

- [头发] Bone_Hair001_M → Bone_Hair002_M → Bone_Hair004_M → Bone_Hair005_M
- [头发] Bone_Hair001_M → Bone_Hair002_M → Bone_Hair007_M → Bone_Hair008_M
- [头发] Bone_Hair011_L → Bone_Hair012_L → Bone_Hair013_L
- [头发] Bone_Hair001_R → Bone_Hair002_R → Bone_Hair003_R

## FemaleM/Zhezhi（R2T1ZhezhiMd10011）

- 骨总数 220；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 56, 'ribbon': 30, 'other:Other': 10, 'chest': 4, 'prop': 1, 'skirt': 20}
- KawaiiPhysics 组 1 个（ABP 变体 2 个），链 36 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：121 根
- 状态：{'chains_source': 'skeleton_heuristic (ABP 无 KawaiiPhysicsGroup 节点)'}

### skeleton_heuristic / heuristic_family_chains

- 参数：Damping=None Stiffness=None LimitAngle=None Radius=None HalfWidth=None Shape=None

- [头发] Bone_Hair001_M → Bone_Hair002_M → Bone_Hair003_M
- [飘带(Piao)] Bone_Piao021_L
- [飘带(Piao)] Bone_Piao021_R
- [头发] Bone_Hair021_L → Bone_Hair022_L → Bone_Hair023_L → Bone_Hair024_L → Bone_Hair025_L
- [头发] Bone_Hair021_R → Bone_Hair022_R → Bone_Hair023_R → Bone_Hair024_R → Bone_Hair025_R
- [头发] Bone_Hair031_R → Bone_Hair032_R → Bone_Hair033_R → Bone_Hair051_R → Bone_Hair052_R
- [头发] Bone_Hair031_R → Bone_Hair032_R → Bone_Hair033_R → Bone_Hair034_R → Bone_Hair035_R
- [头发] Bone_Hair031_R → Bone_Hair062_R → Bone_Hair063_R → Bone_Hair064_R → Bone_Hair065_R
- [头发] Bone_Hair031_L → Bone_Hair032_L → Bone_Hair033_L → Bone_Hair051_L → Bone_Hair052_L
- [头发] Bone_Hair031_L → Bone_Hair032_L → Bone_Hair033_L → Bone_Hair034_L → Bone_Hair035_L
- [头发] Bone_Hair031_L → Bone_Hair062_L → Bone_Hair063_L → Bone_Hair064_L → Bone_Hair065_L
- [飘带(Piao)] Bone_Piao001_L → Bone_Piao002_L → Bone_Piao003_L
- [飘带(Piao)] Bone_Piao001_R → Bone_Piao002_R → Bone_Piao003_R
- [飘带(Piao)] Bone_Piao011_L → Bone_Piao012_L
- [飘带(Piao)] Bone_Piao011_R → Bone_Piao012_R
- [头发] Bone_Hair002_L → Bone_Hair003_L
- [头发] Bone_Hair012_L → Bone_Hair013_L
- [头发] Bone_Hair002_R → Bone_Hair003_R → Bone_Hair009_R
- [头发] Bone_Hair002_R → Bone_Hair005_R → Bone_Hair006_R
- [头发] Bone_Hair012_R → Bone_Hair013_R
- [头发] Bone_Hair004_R
- [头发] Bone_Hair004_L
- [头发] Bone_Hair016_L → Bone_Hair017_L
- [头发] Bone_Hair014_L → Bone_Hair015_L
- [头发] Bone_Hair007_R → Bone_Hair008_R
- [头发] Bone_Hair014_R → Bone_Hair015_R
- [飘带(Piao)] Bone_Piao041_L → Bone_Piao042_L → Bone_Piao043_L → Bone_Piao044_L → Bone_Piao045_L → Bone_Piao046_L → Bone_Piao047_L
- [飘带(Piao)] Bone_Piao041_R → Bone_Piao042_R → Bone_Piao043_R → Bone_Piao044_R → Bone_Piao045_R → Bone_Piao046_R → Bone_Piao047_R
- [胸饰] L_ChestBone01 → L_ChestBone02
- [胸饰] R_ChestBone01 → R_ChestBone02
- [飘带(Piao)] Bone_Piao051 → Bone_Piao052 → Bone_Piao053
- [裙摆] Bone_Skirt001_R → Bone_Skirt002_R → Bone_Skirt003_R
- [裙摆] Bone_Skirt001_L → Bone_Skirt002_L → Bone_Skirt003_L
- [飘带(Piao)] Bone_Piao061 → Bone_Other004 → Bone_Other005
- [裙摆] Bone_Skirt011_L → Bone_Skirt012_L → Bone_Skirt013_L → Bone_Skirt014_L → Bone_Skirt015_L → Bone_Skirt016_L → Bone_Skirt017_L
- [裙摆] Bone_Skirt011_R → Bone_Skirt012_R → Bone_Skirt013_R → Bone_Skirt014_R → Bone_Skirt015_R → Bone_Skirt016_R → Bone_Skirt017_R

## FemaleMS/Buling（R2T1BulingMd10011）

- 骨总数 283；骨族 {'other:Root': 1, 'bip001': 105, 'weapon_prop': 7, 'hair': 47, 'ribbon': 65, 'other:Prop': 13, 'other:TeethUp': 1, 'other:TeethDown': 1, 'other:Tongue': 5, 'chest': 4, 'prop': 1, 'skirt': 33}
- KawaiiPhysics 组 9 个（ABP 变体 1 个），链 41 条
- KawaiiCurve：hair, skirt, sleeve
- 未入组二级骨：71 根

### ABP_Seq_Buling_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=50.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S02

- [裙摆] Bone_Skirt012_R → Bone_Skirt013_R → Bone_Skirt014_R → Bone_Skirt015_R
- [裙摆] Bone_Skirt022_R → Bone_Skirt023_R → Bone_Skirt024_R
- [裙摆] Bone_Skirt022_L → Bone_Skirt023_L → Bone_Skirt024_L → Bone_Skirt025_L
- [裙摆] Bone_Skirt012_L → Bone_Skirt013_L → Bone_Skirt014_L → Bone_Skirt015_L
- [飘带(Piao)] Bone_Piao083_L → Bone_Piao084_L → Bone_Piao085_L → Bone_Piao086_L → Bone_Piao087_L

### ABP_Seq_Buling_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=30.0 Radius=1.3 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r6.5103235
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [头发] Bone_Hair022_R → Bone_Hair023_R
- [头发] Bone_Hair012_R → Bone_Hair013_R
- [头发] Bone_Hair042_R
- [头发] Bone_Hair002_R
- [头发] Bone_Hair042_M → Bone_Hair043_M
- [头发] Bone_Hair002_M → Bone_Hair003_M
- [头发] Bone_Hair042_L
- [头发] Bone_Hair002_R
- [头发] Bone_Hair002_L
- [头发] Bone_Hair012_L → Bone_Hair013_L
- [头发] Bone_Hair062_L
- [头发] Bone_Hair062_R

### ABP_Seq_Buling_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=25.0 Radius=1.3 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [头发] Bone_Hair032_R → Bone_Hair033_R → Bone_Hair034_R
- [头发] Bone_Hair032_L → Bone_Hair033_L → Bone_Hair034_L

### ABP_Seq_Buling_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=50.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S01

- [头发] Bone_Hair063_M → Bone_Hair064_M → Bone_Hair065_M → Bone_Hair066_M → Bone_Hair067_M → Bone_Hair068_M → Bone_Hair069_M
- [头发] Bone_Hair063_M → Bone_Hair064_M → Bone_Hair065_M → Bone_Hair066_M → Bone_Hair067_M → Bone_Piao101_L → Bone_Piao102_L
- [飘带(Piao)] Bone_Piao004_M → Bone_Piao005_M → Bone_Piao006_M → Bone_Piao007_M → Bone_Piao008_M

### ABP_Seq_Buling_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=25.0 Radius=1.0 HalfWidth=1.5 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S04

- [飘带(Piao)] Bone_Piao032_R → Bone_Piao033_R → Bone_Piao034_R → Bone_Piao035_R
- [飘带(Piao)] Bone_Piao032_L → Bone_Piao033_L → Bone_Piao034_L → Bone_Piao035_L

### ABP_Seq_Buling_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.3 Stiffness=0.2 LimitAngle=50.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：Kawaii_alpha_S06

- [飘带(Piao)] Bone_Piao062_L → Bone_Piao063_L
- [飘带(Piao)] Bone_Piao062_L → Bone_Piao065_L
- [飘带(Piao)] Bone_Piao052_L → Bone_Piao053_L
- [飘带(Piao)] Bone_Piao052_R → Bone_Piao053_R
- [飘带(Piao)] Bone_Piao062_R → Bone_Piao063_R
- [飘带(Piao)] Bone_Piao062_R → Bone_Piao065_R

### ABP_Seq_Buling_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=50.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S03

- [裙摆] Bone_Skirt002_R → Bone_Skirt003_R → Bone_Skirt004_R → Bone_Skirt005_R
- [裙摆] Bone_Skirt002_M → Bone_Skirt003_M → Bone_Skirt004_M
- [裙摆] Bone_Skirt002_L → Bone_Skirt003_L → Bone_Skirt004_L → Bone_Skirt005_L

### ABP_Seq_Buling_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r8.75
- Alpha 曲线：Kawaii_alpha_S05

- [头发] Bone_Hair052_L → Bone_Hair053_L
- [飘带(Piao)] Bone_Piao002_L → Bone_Piao003_L
- [飘带(Piao)] Bone_Piao002_R → Bone_Piao003_R

### ABP_Seq_Buling_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.5 Stiffness=0.085 LimitAngle=50.0 Radius=1.0 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 5 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao091_R
- [飘带(Piao)] Bone_Piao022_M → Bone_Piao023_M
- [飘带(Piao)] Bone_Piao012_L → Bone_Piao013_L
- [飘带(Piao)] Bone_Piao012_R → Bone_Piao013_R
- [飘带(Piao)] Bone_Piao121_L

## FemaleMS/Daniya（R2T1DaniyaMd10011）

- 骨总数 354；骨族 {'other:Root': 1, 'bip001': 105, 'weapon_prop': 7, 'hair': 69, 'ribbon': 123, 'facial': 20, 'chest': 4, 'prop': 1, 'skirt': 24}
- KawaiiPhysics 组 10 个（ABP 变体 2 个），链 69 条
- KawaiiCurve：bow, hair, skirt
- 未入组二级骨：61 根

### ABP_Seq_Daniya_Phy / AnimGraphNode_KawaiiPhysicsGroup_9

- 参数：Damping=0.2 Stiffness=0.15 LimitAngle=50.0 Radius=2.5 HalfWidth=3.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S01

- [头发] Bone_Hair032_L → Bone_Hair033_L → Bone_Hair034_L → Bone_Hair035_L → Bone_Hair036_L
- [头发] Bone_Hair042_L → Bone_Hair043_L → Bone_Hair044_L → Bone_Hair045_L → Bone_Hair046_L
- [头发] Bone_Hair042_R → Bone_Hair043_R → Bone_Hair044_R → Bone_Hair045_R → Bone_Hair046_R
- [头发] Bone_Hair032_R → Bone_Hair033_R → Bone_Hair034_R → Bone_Hair035_R → Bone_Hair036_R

### ABP_Seq_Daniya_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.3 Stiffness=0.65 LimitAngle=10.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [胸饰] Bone_Chest001_L → Bone_Chest002_L
- [胸饰] Bone_Chest001_R → Bone_Chest002_R

### ABP_Seq_Daniya_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.0
- Alpha 曲线：Kawaii_alpha_S08

- [头发] Bone_Hair110_R → Bone_Hair111_R → Bone_Hair112_R
- [头发] Bone_Hair100_R → Bone_Hair101_R
- [头发] Bone_Hair090_R → Bone_Hair091_R → Bone_Hair096_R
- [头发] Bone_Hair081_R → Bone_Hair082_R → Bone_Hair083_R
- [头发] Bone_Hair070_R → Bone_Hair071_R → Bone_Hair072_R
- [头发] Bone_Hair060_R → Bone_Hair061_R → Bone_Hair062_R
- [头发] Bone_Hair061_L → Bone_Hair062_L → Bone_Hair063_L
- [头发] Bone_Hair071_L → Bone_Hair072_L → Bone_Hair073_L
- [头发] Bone_Hair081_L → Bone_Hair082_L → Bone_Hair083_L
- [头发] Bone_Hair081_L → Bone_Piao050_L
- [头发] Bone_Hair090_L → Bone_Hair091_L → Bone_Hair092_L
- [头发] Bone_Hair090_L → Bone_Hair091_L → Bone_Hair092_L
- [头发] Bone_Hair100_L
- [飘带(Piao)] Bone_Piao001_L → Bone_Piao002_L → Bone_Piao003_L
- [飘带(Piao)] Bone_Piao060_L → Bone_Piao061_L
- [飘带(Piao)] Bone_Piao065_L → Bone_Piao066_L
- [头发] Bone_Hair021_L → Bone_Hair022_L → Bone_Hair023_L

### ABP_Seq_Daniya_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.04 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S02

- [头发] Bone_Hair002_R → Bone_Hair003_R → Bone_Hair004_R
- [头发] Bone_Hair002_L → Bone_Hair003_L

### ABP_Seq_Daniya_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.3 Stiffness=0.3 LimitAngle=30.0 Radius=0.8 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao151_R
- [飘带(Piao)] Bone_Piao146_R
- [飘带(Piao)] Bone_Piao141_R
- [飘带(Piao)] Bone_Piao136_R
- [飘带(Piao)] Bone_Piao136_L
- [飘带(Piao)] Bone_Piao141_L
- [飘带(Piao)] Bone_Piao151_L
- [飘带(Piao)] Bone_Piao146_L

### ABP_Seq_Daniya_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=30.0 Radius=1.0 HalfWidth=2.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S03

- [飘带(Piao)] Bone_Piao350_R → Bone_Piao351_R
- [飘带(Piao)] Bone_Piao350_L → Bone_Piao351_L
- [飘带(Piao)] Bone_Piao210_R
- [飘带(Piao)] Bone_Piao210_L
- [飘带(Piao)] Bone_Piao228_R → Bone_Piao229_R
- [飘带(Piao)] Bone_Piao222_R → Bone_Piao223_R
- [飘带(Piao)] Bone_Piao222_L → Bone_Piao223_L
- [飘带(Piao)] Bone_Piao231_L → Bone_Piao232_L
- [飘带(Piao)] Bone_Piao016_R → Bone_Piao017_R → Bone_Piao018_R → Bone_Piao020_R

### ABP_Seq_Daniya_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.4 Stiffness=0.4 LimitAngle=30.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.25
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S07

- [飘带(Piao)] Bone_Piao198_R → Bone_Piao199_R
- [飘带(Piao)] Bone_Piao190_R → Bone_Piao191_R
- [飘带(Piao)] Bone_Piao185_R → Bone_Piao186_R
- [飘带(Piao)] Bone_Piao195_R → Bone_Piao196_R
- [飘带(Piao)] Bone_Piao182_R → Bone_Piao183_R

### ABP_Seq_Daniya_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=50.0 Radius=1.2 HalfWidth=5.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S04

- [裙摆] Bone_Skirt002_R → Bone_Skirt003_R → Bone_Piao130_M → Bone_Piao131_M
- [裙摆] Bone_Skirt002_L → Bone_Skirt003_L

### ABP_Seq_Daniya_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S05

- [裙摆] Bone_Skirt022_L → Bone_Skirt023_L → Bone_Skirt024_L → Bone_Skirt025_L
- [裙摆] Bone_Skirt022_R → Bone_Skirt023_R → Bone_Skirt024_R → Bone_Skirt025_R
- [裙摆] Bone_Skirt012_R → Bone_Skirt013_R → Bone_Skirt014_R
- [裙摆] Bone_Skirt012_L → Bone_Skirt013_L → Bone_Skirt014_L

### ABP_Seq_Daniya_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S06

- [飘带(Piao)] Bone_Piao032_L → Bone_Piao033_L → Bone_Piao034_L → Bone_Piao035_L → Bone_Piao036_L → Bone_Piao100_L
- [飘带(Piao)] Bone_Piao032_L → Bone_Piao033_L → Bone_Piao034_L → Bone_Piao035_L → Bone_Piao036_L → Bone_Piao105_L
- [飘带(Piao)] Bone_Piao032_L → Bone_Piao033_L → Bone_Piao034_L → Bone_Piao035_L → Bone_Piao036_L → Bone_Piao110_L
- [飘带(Piao)] Bone_Piao032_L → Bone_Piao033_L → Bone_Piao034_L → Bone_Piao035_L → Bone_Piao120_L
- [飘带(Piao)] Bone_Piao032_L → Bone_Piao033_L → Bone_Piao034_L → Bone_Piao130_L
- [飘带(Piao)] Bone_Piao032_R → Bone_Piao033_R → Bone_Piao034_R → Bone_Piao035_R → Bone_Piao036_R → Bone_Piao100_R
- [飘带(Piao)] Bone_Piao032_R → Bone_Piao033_R → Bone_Piao034_R → Bone_Piao035_R → Bone_Piao036_R → Bone_Piao105_R
- [飘带(Piao)] Bone_Piao032_R → Bone_Piao033_R → Bone_Piao034_R → Bone_Piao035_R → Bone_Piao036_R → Bone_Piao110_R
- [飘带(Piao)] Bone_Piao032_R → Bone_Piao033_R → Bone_Piao034_R → Bone_Piao035_R → Bone_Piao120_R
- [飘带(Piao)] Bone_Piao032_R → Bone_Piao033_R → Bone_Piao034_R → Bone_Piao130_R
- [飘带(Piao)] Bone_Piao160_L → Bone_Piao161_L → Bone_Piao162_L
- [飘带(Piao)] Bone_Piao160_R → Bone_Piao161_R → Bone_Piao162_R
- [飘带(Piao)] Bone_Piao170_L → Bone_Piao171_L
- [飘带(Piao)] Bone_Piao175_L → Bone_Piao176_L
- [飘带(Piao)] Bone_Piao170_R → Bone_Piao171_R
- [飘带(Piao)] Bone_Piao175_R → Bone_Piao176_R

## FemaleMS/Dengdeng（R2T1DengDengMd10011）

- 骨总数 246；骨族 {'other:Root': 1, 'bip001': 105, 'weapon_prop': 7, 'hair': 45, 'ribbon': 59, 'chest': 4, 'other:Other': 24, 'prop': 1}
- KawaiiPhysics 组 1 个（ABP 变体 2 个），链 51 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：133 根
- 状态：{'chains_source': 'skeleton_heuristic (ABP 无 KawaiiPhysicsGroup 节点)'}

### skeleton_heuristic / heuristic_family_chains

- 参数：Damping=None Stiffness=None LimitAngle=None Radius=None HalfWidth=None Shape=None

- [头发] Bone_Hair015_R → Bone_Hair016_R
- [头发] Bone_Hair012_R → Bone_Hair013_R → Bone_Hair014_R
- [头发] Bone_Hair012_L → Bone_Hair013_L → Bone_Hair014_L
- [头发] Bone_Hair015_L → Bone_Hair016_L
- [头发] Bone_Hair006_M → Bone_Hair008_M
- [头发] Bone_Hair009_R → Bone_Hair011_R
- [头发] Bone_Hair017_L
- [头发] Bone_Hair017_R
- [头发] Bone_Hair019_R → Bone_Hair020_R
- [头发] Bone_Hair019_R → Bone_Hair021_R
- [头发] Bone_Hair019_L → Bone_Hair020_L
- [头发] Bone_Hair019_L → Bone_Hair022_L
- [头发] Bone_Hair019_L → Bone_Hair021_L
- [头发] Bone_Hair025_L → Bone_Hair026_L → Bone_Hair027_L → Bone_Hair028_L → Bone_Hair029_L
- [头发] Bone_Hair025_R → Bone_Hair026_R → Bone_Hair027_R → Bone_Hair028_R → Bone_Hair029_R
- [头发] Bone_Hair023_R → Bone_Hair024_R
- [头发] Bone_Hair023_L → Bone_Hair024_L
- [头发] Bone_Hair004_L → Bone_Hair005_L
- [头发] Bone_Hair002_L → Bone_Hair003_L
- [头发] Bone_Hair030_L → Bone_Hair031_L
- [头发] Bone_Hair030_R → Bone_Hair031_R
- [飘带(Piao)] Bone_Piao050_L → Bone_Piao008_L → Bone_Piao009_L
- [飘带(Piao)] Bone_Piao050_L → Bone_Piao007_L
- [飘带(Piao)] Bone_Piao006_L
- [飘带(Piao)] Bone_Piao050_R → Bone_Piao008_R → Bone_Piao009_R
- [飘带(Piao)] Bone_Piao050_R → Bone_Piao007_R
- [飘带(Piao)] Bone_Piao006_R
- [胸饰] Bone_Chest001_L → Bone_Chest002_L
- [胸饰] Bone_Chest001_R → Bone_Chest002_R
- [飘带(Piao)] Bone_Piao010_R → Bone_Piao011_R
- [飘带(Piao)] Bone_Piao010_L → Bone_Piao011_L
- [飘带(Piao)] Bone_Piao012_L → Bone_Piao013_L
- [飘带(Piao)] Bone_Piao012_R → Bone_Piao013_R
- [飘带(Piao)] Bone_Piao014_L → Bone_Piao015_L
- [飘带(Piao)] Bone_Piao014_R → Bone_Piao015_R
- [飘带(Piao)] Bone_Piao031_M → Bone_Other100_M
- [飘带(Piao)] Bone_Piao031_M → Bone_Piao001_L
- [飘带(Piao)] Bone_Piao031_M → Bone_Piao001_R
- [飘带(Piao)] Bone_Piao031_M → Bone_Piao120_M → Bone_Piao121_M → Bone_Piao122_M
- [飘带(Piao)] Bone_Piao031_M → Bone_Piao120_M → Bone_Piao121_M → Bone_Piao040_R → Bone_Piao041_R → Bone_Piao042_R
- [飘带(Piao)] Bone_Piao031_M → Bone_Piao120_M → Bone_Piao121_M → Bone_Piao040_L → Bone_Piao041_L → Bone_Piao042_L
- [飘带(Piao)] Bone_Piao031_M → Bone_Piao120_M → Bone_Piao003_L
- [飘带(Piao)] Bone_Piao029_R
- [飘带(Piao)] Bone_Piao029_L
- [飘带(Piao)] Bone_Piao026_L → Bone_Piao027_L → Bone_Piao028_L
- [飘带(Piao)] Bone_Piao026_R → Bone_Piao027_R → Bone_Piao028_R
- [飘带(Piao)] Bone_Piao501_L
- [飘带(Piao)] Bone_Piao501_R
- [飘带(Piao)] Bone_Piao101_M → Bone_Piao104_M → Bone_Piao105_M → Bone_Piao106_M → Bone_Piao107_M → Bone_Piao108_M → Bone_Piao109_M → Bone_Piao110_M → Bone_Piao111_M → Bone_Piao112_M → Bone_Piao115_M → Bone_Piao116_M
- [飘带(Piao)] Bone_Piao030_M
- [飘带(Piao)] Bone_Piao050_M

## FemaleMS/Feibi（R2T1FeibiMd10011）

- 骨总数 266；骨族 {'other:Root': 1, 'bip001': 105, 'weapon_prop': 17, 'hair': 66, 'ribbon': 43, 'chest': 4, 'prop': 1, 'skirt': 28, 'other:Other': 1}
- KawaiiPhysics 组 9 个（ABP 变体 1 个），链 37 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：39 根

### ABP_Seq_Feibi_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=30.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 7 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [头发] Bone_Hair094_R → Bone_Hair095_R → Bone_Hair096_R → Bone_Hair097_R
- [头发] Bone_Hair104_R → Bone_Hair105_R → Bone_Hair106_R → Bone_Hair107_R
- [头发] Bone_Hair104_L → Bone_Hair105_L → Bone_Hair106_L → Bone_Hair107_L
- [头发] Bone_Hair094_L → Bone_Hair095_L → Bone_Hair096_L → Bone_Hair097_L

### ABP_Seq_Feibi_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=25.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [裙摆] Bone_Skirt002_R → Bone_Skirt003_R → Bone_Skirt004_R
- [裙摆] Bone_Skirt012_L → Bone_Skirt013_L → Bone_Skirt014_L
- [裙摆] Bone_Skirt002_L → Bone_Skirt003_L → Bone_Skirt004_L
- [裙摆] Bone_Skirt022_L → Bone_Skirt023_L → Bone_Skirt024_L
- [裙摆] Bone_Skirt032_L → Bone_Skirt033_L → Bone_Skirt034_L
- [裙摆] Bone_Skirt032_R → Bone_Skirt033_R → Bone_Skirt034_R
- [裙摆] Bone_Skirt022_R → Bone_Skirt023_R → Bone_Skirt024_R

### ABP_Seq_Feibi_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r6.5
- Alpha 曲线：None

- [头发] Bone_Hair001_M → Bone_Hair002_M
- [头发] Bone_Hair001_L → Bone_Hair002_L
- [头发] Bone_Hair011_L → Bone_Hair012_L
- [头发] Bone_Hair021_L → Bone_Hair022_L
- [头发] Bone_Hair061_L → Bone_Hair062_L
- [头发] Bone_Hair041_L → Bone_Hair042_L → Bone_Hair043_L
- [头发] Bone_Hair031_L → Bone_Hair032_L
- [头发] Bone_Hair001_R → Bone_Hair002_R
- [头发] Bone_Hair011_R → Bone_Hair012_R
- [头发] Bone_Hair021_R → Bone_Hair022_R
- [头发] Bone_Hair061_R → Bone_Hair062_R
- [头发] Bone_Hair031_R → Bone_Hair032_R

### ABP_Seq_Feibi_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r6.5
- Alpha 曲线：None

- [头发] Bone_Hair071_L → Bone_Hair072_L → Bone_Hair073_L
- [头发] Bone_Hair071_R → Bone_Hair072_R → Bone_Hair073_R
- [头发] Bone_Hair081_R → Bone_Hair082_R → Bone_Hair083_R → Bone_Hair084_R → Bone_Hair085_R
- [飘带(Piao)] Bone_Piao011_R → Bone_Piao012_R → Bone_Piao013_R

### ABP_Seq_Feibi_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=35.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r6.5
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao101_M → Bone_Piao102_M → Bone_Piao103_M → Bone_Piao104_M

### ABP_Seq_Feibi_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=35.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r6.5
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao101_R → Bone_Piao102_R
- [飘带(Piao)] Bone_Piao111_R → Bone_Piao112_R
- [飘带(Piao)] Bone_Piao111_L → Bone_Piao112_L
- [飘带(Piao)] Bone_Piao101_L → Bone_Piao102_L

### ABP_Seq_Feibi_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=25.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao044_R → Bone_Piao045_R

### ABP_Seq_Feibi_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.3 Stiffness=0.2 LimitAngle=20.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao061_R → Bone_Piao062_R → Bone_Piao063_R
- [飘带(Piao)] Bone_Piao054_R → Bone_Piao055_R → Bone_Piao056_R → Bone_Piao057_R → Bone_Piao058_R → Bone_Piao059_R → Bone_Piao060_R

### ABP_Seq_Feibi_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.5 Stiffness=0.15 LimitAngle=15.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [胸饰] Bone_Chest001_R → Bone_Chest002_R
- [胸饰] Bone_Chest001_L → Bone_Chest002_L

## FemaleMS/Katixiya（R2T1KatixiyaMd10011）

- 骨总数 257；骨族 {'other:Root': 1, 'bip001': 105, 'weapon_prop': 7, 'hair': 66, 'ribbon': 60, 'other:Other': 5, 'chest': 4, 'prop': 1, 'skirt': 8}
- KawaiiPhysics 组 10 个（ABP 变体 1 个），链 43 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：47 根

### ABP_Seq_Katixiya_Phy / AnimGraphNode_KawaiiPhysicsGroup_9

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=50.0 Radius=1.0 HalfWidth=2.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [飘带(Piao)] Bone_Piao024_R → Bone_Piao025_R → Bone_Piao026_R → Bone_Piao027_R → Bone_Piao028_R
- [飘带(Piao)] Bone_Piao024_L → Bone_Piao025_L → Bone_Piao026_L → Bone_Piao027_L → Bone_Piao028_L

### ABP_Seq_Katixiya_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=50.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [裙摆] Bone_Skirt002_R → Bone_Skirt003_R → Bone_Skirt004_R
- [裙摆] Bone_Skirt002_L → Bone_Skirt003_L → Bone_Skirt004_L

### ABP_Seq_Katixiya_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [飘带(Piao)] Bone_Piao044_R → Bone_Piao045_R
- [飘带(Piao)] Bone_Piao046_R → Bone_Piao047_R → Bone_Piao048_R
- [飘带(Piao)] Bone_Piao044_L → Bone_Piao045_L
- [飘带(Piao)] Bone_Piao046_L → Bone_Piao047_L → Bone_Piao048_L

### ABP_Seq_Katixiya_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.1 LimitAngle=50.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：Kawaii_alpha_jianxin

- [飘带(Piao)] Bone_Piao034_R → Bone_Piao035_R → Bone_Piao036_R → Bone_Piao037_R
- [飘带(Piao)] Bone_Piao034_L → Bone_Piao035_L → Bone_Piao036_L → Bone_Piao037_L

### ABP_Seq_Katixiya_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.5 Stiffness=0.15 LimitAngle=15.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [胸饰] Bone_Chest001_R → Bone_Chest002_R
- [胸饰] Bone_Chest001_L → Bone_Chest002_L

### ABP_Seq_Katixiya_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao002_R → Bone_Piao003_R
- [飘带(Piao)] Bone_Piao002_R → Bone_Piao013_R
- [飘带(Piao)] Bone_Piao002_R → Bone_Piao015_R
- [飘带(Piao)] Bone_Piao002_L → Bone_Piao003_L
- [飘带(Piao)] Bone_Piao002_L → Bone_Piao013_L
- [飘带(Piao)] Bone_Piao002_L → Bone_Piao015_L

### ABP_Seq_Katixiya_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：Kawaii_alpha_chazhi

- [头发] Bone_Hair012_R → Bone_Hair013_R
- [头发] Bone_Hair012_L → Bone_Hair013_L

### ABP_Seq_Katixiya_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.075 LimitAngle=45.0 Radius=0.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao011_L
- [飘带(Piao)] Bone_Piao011_R
- [飘带(Piao)] Bone_Piao013_L
- [飘带(Piao)] Bone_Piao013_R
- [飘带(Piao)] Bone_Piao015_L
- [飘带(Piao)] Bone_Piao015_R

### ABP_Seq_Katixiya_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=0.75 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r6.5
- Alpha 曲线：None

- [头发] Bone_Hair001_R
- [头发] Bone_Hair001_L
- [头发] Bone_Hair002_R
- [头发] Bone_Hair002_L
- [头发] Bone_Hair003_R
- [头发] Bone_Hair003_L
- [头发] Bone_Hair004_R
- [头发] Bone_Hair004_L

### ABP_Seq_Katixiya_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.2 Stiffness=0.1 LimitAngle=50.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：Kawaii_alpha_jianxin

- [头发] Bone_Hair024_R → Bone_Hair025_R → Bone_Hair026_R → Bone_Hair027_R → Bone_Hair028_R
- [头发] Bone_Hair024_R → Bone_Hair025_R → Bone_Hair031_R → Bone_Hair032_R → Bone_Hair033_R
- [头发] Bone_Hair024_R → Bone_Hair025_R → Bone_Hair041_R → Bone_Hair042_R → Bone_Hair043_R
- [头发] Bone_Hair024_M → Bone_Hair025_M → Bone_Hair026_M → Bone_Hair027_M → Bone_Hair028_M
- [头发] Bone_Hair024_L → Bone_Hair025_L → Bone_Hair026_L → Bone_Hair027_L → Bone_Hair028_L
- [头发] Bone_Hair024_L → Bone_Hair025_L → Bone_Hair031_L → Bone_Hair032_L → Bone_Hair033_L
- [头发] Bone_Hair024_L → Bone_Hair025_L → Bone_Hair041_L → Bone_Hair042_L → Bone_Hair043_L
- [头发] Bone_Hair052_R → Bone_Hair053_R → Bone_Hair054_R → Bone_Hair055_R → Bone_Hair056_R
- [头发] Bone_Hair052_L → Bone_Hair053_L → Bone_Hair054_L → Bone_Hair055_L → Bone_Hair056_L

## FemaleMS/Kmola（R2T1KamolaMd10011）

- 骨总数 263；骨族 {'other:Root': 1, 'bip001': 105, 'weapon_prop': 11, 'hair': 57, 'ribbon': 68, 'chest': 4, 'prop': 1, 'skirt': 16}
- KawaiiPhysics 组 17 个（ABP 变体 2 个），链 102 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：50 根

### ABP_Seq_Kmola_Link_Skin01_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=100.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao061_R → Bone_Piao062_R
- [飘带(Piao)] Bone_Piao061_L → Bone_Piao062_L
- [飘带(Piao)] Bone_Piao071_R → Bone_Piao072_R
- [飘带(Piao)] Bone_Piao071_L → Bone_Piao072_L
- [飘带(Piao)] Bone_Piao081_R → Bone_Piao082_R
- [飘带(Piao)] Bone_Piao081_L → Bone_Piao082_L
- [飘带(Piao)] Bone_Piao051_R → Bone_Piao052_R
- [飘带(Piao)] Bone_Piao051_L → Bone_Piao052_L

### ABP_Seq_Kmola_Link_Skin01_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.5 Stiffness=0.15 LimitAngle=15.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [胸饰] Bone_Chest001_R → Bone_Chest002_R
- [胸饰] Bone_Chest001_L → Bone_Chest002_L

### ABP_Seq_Kmola_Link_Skin01_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [头发] Bone_Hair044_R → Bone_Hair045_R → Bone_Hair046_R
- [头发] Bone_Hair044_L → Bone_Hair045_L → Bone_Hair046_L

### ABP_Seq_Kmola_Link_Skin01_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.3 Stiffness=0.2 LimitAngle=20.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao034_R → Bone_Piao035_R
- [飘带(Piao)] Bone_Piao034_L → Bone_Piao035_L
- [飘带(Piao)] Bone_Piao131_R
- [飘带(Piao)] Bone_Piao131_L

### ABP_Seq_Kmola_Link_Skin01_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.2 Stiffness=0.15 LimitAngle=30.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao101_R
- [飘带(Piao)] Bone_Piao101_L
- [飘带(Piao)] Bone_Piao111_R
- [飘带(Piao)] Bone_Piao111_L
- [飘带(Piao)] Bone_Piao111_M → Bone_Piao112_M → Bone_Piao113_M
- [飘带(Piao)] Bone_Piao091_L
- [飘带(Piao)] Bone_Piao091_R
- [飘带(Piao)] Bone_Piao092_L
- [飘带(Piao)] Bone_Piao092_R

### ABP_Seq_Kmola_Link_Skin01_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r6.5
- Alpha 曲线：None

- [头发] Bone_Hair003_M → Bone_Hair004_M → Bone_Hair005_M → Bone_Hair006_M
- [头发] Bone_Hair002_R → Bone_Hair003_R
- [头发] Bone_Hair002_L → Bone_Hair003_L
- [头发] Bone_Hair012_R → Bone_Hair013_R
- [头发] Bone_Hair012_L → Bone_Hair013_L
- [头发] Bone_Hair016_R → Bone_Hair017_R
- [头发] Bone_Hair016_L → Bone_Hair017_L
- [头发] Bone_Hair021_R → Bone_Hair022_R
- [头发] Bone_Hair021_L → Bone_Hair022_L
- [飘带(Piao)] Bone_Piao021_R
- [飘带(Piao)] Bone_Piao021_L

### ABP_Seq_Kmola_Link_Skin01_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao002_R
- [飘带(Piao)] Bone_Piao002_L
- [飘带(Piao)] Bone_Piao012_R
- [飘带(Piao)] Bone_Piao012_L
- [飘带(Piao)] Bone_Piao122_R → Bone_Piao123_R
- [飘带(Piao)] Bone_Piao122_L → Bone_Piao123_L
- [头发] Bone_Hair063_M → Bone_Hair064_M → Bone_Hair065_M
- [头发] Bone_Hair054_L → Bone_Hair055_L → Bone_Hair056_L
- [头发] Bone_Hair054_R → Bone_Hair055_R → Bone_Hair056_R

### ABP_Seq_Kmola_Link_Skin01_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=50.0 Radius=2.0 HalfWidth=6.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [裙摆] Bone_Skirt002_R → Bone_Skirt003_R → Bone_Skirt004_R
- [裙摆] Bone_Skirt002_L → Bone_Skirt003_L → Bone_Skirt004_L

### ABP_Seq_Kmola_Link_Skin01_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=25.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [裙摆] Bone_Skirt013_R → Bone_Skirt014_R
- [裙摆] Bone_Skirt013_L → Bone_Skirt014_L
- [飘带(Piao)] Bone_Piao044_R → Bone_Piao045_R
- [飘带(Piao)] Bone_Piao044_L → Bone_Piao045_L

### ABP_Seq_Kmola_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=25.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [裙摆] Bone_Skirt013_R → Bone_Skirt014_R
- [裙摆] Bone_Skirt013_L → Bone_Skirt014_L
- [裙摆] Bone_Skirt003_R → Bone_Skirt004_R
- [裙摆] Bone_Skirt003_L → Bone_Skirt004_L
- [飘带(Piao)] Bone_Piao044_R → Bone_Piao045_R
- [飘带(Piao)] Bone_Piao044_L → Bone_Piao045_L

### ABP_Seq_Kmola_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=100.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao061_R → Bone_Piao062_R
- [飘带(Piao)] Bone_Piao061_L → Bone_Piao062_L
- [飘带(Piao)] Bone_Piao071_R → Bone_Piao072_R
- [飘带(Piao)] Bone_Piao071_L → Bone_Piao072_L
- [飘带(Piao)] Bone_Piao081_R → Bone_Piao082_R
- [飘带(Piao)] Bone_Piao081_L → Bone_Piao082_L
- [飘带(Piao)] Bone_Piao051_R → Bone_Piao052_R
- [飘带(Piao)] Bone_Piao051_L → Bone_Piao052_L

### ABP_Seq_Kmola_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.5 Stiffness=0.15 LimitAngle=15.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [胸饰] Bone_Chest001_R → Bone_Chest002_R
- [胸饰] Bone_Chest001_L → Bone_Chest002_L

### ABP_Seq_Kmola_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [头发] Bone_Hair044_R → Bone_Hair045_R → Bone_Hair046_R
- [头发] Bone_Hair044_L → Bone_Hair045_L → Bone_Hair046_L

### ABP_Seq_Kmola_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.3 Stiffness=0.2 LimitAngle=20.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao034_R → Bone_Piao035_R
- [飘带(Piao)] Bone_Piao034_L → Bone_Piao035_L
- [飘带(Piao)] Bone_Piao131_R
- [飘带(Piao)] Bone_Piao131_L

### ABP_Seq_Kmola_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.15 LimitAngle=30.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao101_R
- [飘带(Piao)] Bone_Piao101_L
- [飘带(Piao)] Bone_Piao111_R
- [飘带(Piao)] Bone_Piao111_L
- [飘带(Piao)] Bone_Piao111_M → Bone_Piao112_M → Bone_Piao113_M
- [飘带(Piao)] Bone_Piao091_L
- [飘带(Piao)] Bone_Piao091_R
- [飘带(Piao)] Bone_Piao092_L
- [飘带(Piao)] Bone_Piao092_R

### ABP_Seq_Kmola_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r6.5
- Alpha 曲线：None

- [头发] Bone_Hair003_M → Bone_Hair004_M → Bone_Hair005_M → Bone_Hair006_M
- [头发] Bone_Hair002_R → Bone_Hair003_R
- [头发] Bone_Hair002_L → Bone_Hair003_L
- [头发] Bone_Hair012_R → Bone_Hair013_R
- [头发] Bone_Hair012_L → Bone_Hair013_L
- [头发] Bone_Hair016_R → Bone_Hair017_R
- [头发] Bone_Hair016_L → Bone_Hair017_L
- [头发] Bone_Hair021_R → Bone_Hair022_R
- [头发] Bone_Hair021_L → Bone_Hair022_L
- [飘带(Piao)] Bone_Piao021_R
- [飘带(Piao)] Bone_Piao021_L

### ABP_Seq_Kmola_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao002_R
- [飘带(Piao)] Bone_Piao002_L
- [飘带(Piao)] Bone_Piao012_R
- [飘带(Piao)] Bone_Piao012_L
- [飘带(Piao)] Bone_Piao122_R → Bone_Piao123_R
- [飘带(Piao)] Bone_Piao122_L → Bone_Piao123_L
- [头发] Bone_Hair063_M → Bone_Hair064_M → Bone_Hair065_M
- [头发] Bone_Hair054_L → Bone_Hair055_L → Bone_Hair056_L
- [头发] Bone_Hair054_R → Bone_Hair055_R → Bone_Hair056_R

## FemaleMS/Kmola（R2T1KamolaMd20011）

- 骨总数 ?；骨族 ?
- KawaiiPhysics 组 0 个（ABP 变体 0 个），链 0 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 状态：{'skeleton': 'export_missing', 'abp_skipped_no_skeleton': ['ABP_Seq_Kmola_Link_Skin01_Phy', 'ABP_Seq_Kmola_Phy']}

## FemaleMS/Mone（R2T1MoneMd10011）

- 骨总数 412；骨族 {'other:Root': 1, 'bip001': 105, 'weapon_prop': 9, 'ribbon': 131, 'hair': 149, 'chest': 4, 'prop': 1, 'skirt': 12}
- KawaiiPhysics 组 16 个（ABP 变体 1 个），链 70 条
- KawaiiCurve：cape, hair, skirt
- 未入组二级骨：118 根

### ABP_Seq_Mone_Phy / AnimGraphNode_KawaiiPhysicsGroup_15

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=50.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S01

- [裙摆] Bone_Skirt001_L → Bone_Skirt002_L
- [裙摆] Bone_Skirt001_R → Bone_Skirt002_R

### ABP_Seq_Mone_Phy / AnimGraphNode_KawaiiPhysicsGroup_14

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=50.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S03

- [飘带(Piao)] Bone_Piao023_R → Bone_Piao024_R → Bone_Piao025_R → Bone_Piao026_R → Bone_Piao027_R → Bone_Piao028_R
- [飘带(Piao)] Bone_Piao023_L → Bone_Piao024_L → Bone_Piao025_L → Bone_Piao026_L → Bone_Piao027_L → Bone_Piao028_L
- [飘带(Piao)] Bone_Piao033_R → Bone_Piao034_R → Bone_Piao035_R → Bone_Piao036_R
- [飘带(Piao)] Bone_Piao033_L → Bone_Piao034_L → Bone_Piao035_L → Bone_Piao036_L

### ABP_Seq_Mone_Phy / AnimGraphNode_KawaiiPhysicsGroup_13

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=50.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S04

- [飘带(Piao)] Bone_Piao004_L → Bone_Piao005_L → Bone_Piao006_L → Bone_Piao007_L
- [飘带(Piao)] Bone_Piao004_R → Bone_Piao005_R → Bone_Piao006_R → Bone_Piao007_R
- [飘带(Piao)] Bone_Piao014_L → Bone_Piao015_L → Bone_Piao016_L → Bone_Piao017_L
- [飘带(Piao)] Bone_Piao014_R → Bone_Piao015_R → Bone_Piao016_R → Bone_Piao017_R

### ABP_Seq_Mone_Phy / AnimGraphNode_KawaiiPhysicsGroup_12

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=50.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S02

- [飘带(Piao)] Bone_Piao061_L → Bone_Piao062_L → Bone_Piao063_L
- [飘带(Piao)] Bone_Piao061_R → Bone_Piao062_R → Bone_Piao063_R
- [飘带(Piao)] Bone_Piao071_L → Bone_Piao072_L → Bone_Piao073_L
- [飘带(Piao)] Bone_Piao071_R → Bone_Piao072_R → Bone_Piao073_R
- [飘带(Piao)] Bone_Piao081_L → Bone_Piao082_L → Bone_Piao083_L → Bone_Piao084_L → Bone_Piao085_L
- [飘带(Piao)] Bone_Piao081_R → Bone_Piao082_R → Bone_Piao083_R → Bone_Piao084_R → Bone_Piao085_R

### ABP_Seq_Mone_Phy / AnimGraphNode_KawaiiPhysicsGroup_11

- 参数：Damping=0.2 Stiffness=0.15 LimitAngle=50.0 Radius=2.5 HalfWidth=3.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S05

- [头发] Bone_Hair003_L → Bone_Hair004_L → Bone_Hair005_L
- [头发] Bone_Hair007_L → Bone_Hair008_L → Bone_Hair009_L → Bone_Hair011_L
- [头发] Bone_Hair007_L → Bone_Hair008_L → Bone_Hair009_L → Bone_Hair012_L → Bone_Hair013_L
- [头发] Bone_Hair007_L → Bone_Hair008_L → Bone_Hair009_L → Bone_Hair006_L
- [头发] Bone_Hair023_L → Bone_Hair024_L → Bone_Hair025_L → Bone_Hair027_L → Bone_Hair028_L
- [头发] Bone_Hair023_L → Bone_Hair024_L → Bone_Hair025_L → Bone_Hair030_L → Bone_Hair031_L
- [头发] Bone_Hair023_L → Bone_Hair034_L → Bone_Hair035_L
- [头发] Bone_Hair023_R → Bone_Hair024_R → Bone_Hair025_R → Bone_Hair027_R → Bone_Hair028_R
- [头发] Bone_Hair023_R → Bone_Hair024_R → Bone_Hair025_R → Bone_Hair030_R → Bone_Hair031_R
- [头发] Bone_Hair023_R → Bone_Hair034_R → Bone_Hair035_R
- [头发] Bone_Hair003_R → Bone_Hair004_R → Bone_Hair005_R
- [头发] Bone_Hair007_R → Bone_Hair008_R → Bone_Hair009_R → Bone_Hair011_R
- [头发] Bone_Hair007_R → Bone_Hair008_R → Bone_Hair009_R → Bone_Hair012_R → Bone_Hair013_R
- [头发] Bone_Hair007_R → Bone_Hair008_R → Bone_Hair009_R → Bone_Hair006_R

### ABP_Seq_Mone_Phy / AnimGraphNode_KawaiiPhysicsGroup_10

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.25
- Alpha 曲线：Kawaii_alpha_S08

- [头发] Bone_Hair045_L → Bone_Hair046_L
- [头发] Bone_Hair055_L → Bone_Hair056_L
- [头发] Bone_Hair063_R → Bone_Hair064_R
- [头发] Bone_Hair071_L → Bone_Hair072_L

### ABP_Seq_Mone_Phy / AnimGraphNode_KawaiiPhysicsGroup_9

- 参数：Damping=0.1 Stiffness=0.05 LimitAngle=50.0 Radius=1.0 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.25
- Alpha 曲线：Kawaii_alpha_S08

- [头发] Bone_Hair045_R → Bone_Hair046_R
- [头发] Bone_Hair060_L → Bone_Hair061_L
- [头发] Bone_Hair072_R → Bone_Hair073_R
- [头发] Bone_Hair072_R → Bone_Hair080_R
- [头发] Bone_Hair077_L → Bone_Hair078_L
- [头发] Bone_Hair091_R → Bone_Hair092_R → Bone_Hair093_R

### ABP_Seq_Mone_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.15 Stiffness=0.04 LimitAngle=50.0 Radius=1.0 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.25
- Alpha 曲线：Kawaii_alpha_S08

- [头发] Bone_Hair050_R → Bone_Hair051_R
- [头发] Bone_Hair055_R → Bone_Hair056_R
- [头发] Bone_Hair040_R → Bone_Hair041_R
- [头发] Bone_Hair080_L → Bone_Hair081_L

### ABP_Seq_Mone_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.09 Stiffness=0.06 LimitAngle=50.0 Radius=1.0 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.25
- Alpha 曲线：Kawaii_alpha_S08

- [头发] Bone_Hair050_L → Bone_Hair051_L
- [头发] Bone_Hair060_R → Bone_Hair061_R
- [头发] Bone_Hair077_R → Bone_Hair078_R
- [头发] Bone_Hair083_L → Bone_Hair084_L
- [头发] Bone_Hair040_L → Bone_Hair041_L

### ABP_Seq_Mone_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.25
- Alpha 曲线：Kawaii_alpha_S09

- [头发] Bone_Hair0102_R → Bone_Hair0103_R
- [头发] Bone_Hair0147_R → Bone_Hair0148_R
- [头发] Bone_Hair0171_L → Bone_Hair0172_L
- [头发] Bone_Hair0141_L → Bone_Hair0142_L

### ABP_Seq_Mone_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.1 Stiffness=0.05 LimitAngle=50.0 Radius=1.0 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.25
- Alpha 曲线：Kawaii_alpha_S09

- [头发] Bone_Hair0112_R → Bone_Hair0113_R
- [头发] Bone_Hair0151_L → Bone_Hair0152_L
- [头发] Bone_Hair0192_L → Bone_Hair0193_L
- [头发] Bone_Hair0181_L → Bone_Hair0182_L

### ABP_Seq_Mone_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.15 Stiffness=0.04 LimitAngle=50.0 Radius=1.0 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.25
- Alpha 曲线：Kawaii_alpha_S09

- [头发] Bone_Hair0132_R → Bone_Hair0133_R
- [头发] Bone_Hair0161_L → Bone_Hair0162_L
- [头发] Bone_Hair0181_R → Bone_Hair0182_R
- [头发] Bone_Hair0121_L → Bone_Hair0122_L

### ABP_Seq_Mone_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.09 Stiffness=0.06 LimitAngle=50.0 Radius=1.0 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.25
- Alpha 曲线：Kawaii_alpha_S09

- [头发] Bone_Hair0122_R → Bone_Hair0123_R
- [头发] Bone_Hair0143_R → Bone_Hair0144_R
- [头发] Bone_Hair0132_L → Bone_Hair0133_L
- [头发] Bone_Hair0111_L → Bone_Hair0112_L

### ABP_Seq_Mone_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.04 LimitAngle=20.0 Radius=0.5 HalfWidth=0.5 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S06

- [飘带(Piao)] Bone_Piao046_R → Bone_Piao047_R → Bone_Piao048_R
- [飘带(Piao)] Bone_Piao046_L → Bone_Piao047_L → Bone_Piao048_L

### ABP_Seq_Mone_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=30.0 Radius=1.0 HalfWidth=2.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S07

- [飘带(Piao)] Bone_Piao042_M → Bone_Piao043_M → Bone_Piao044_M

### ABP_Seq_Mone_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.2 Stiffness=0.06 LimitAngle=2.0 Radius=2.0 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.25
- Alpha 曲线：Kawaii_alpha_S15

- [weapon_prop] WeaponProp05
- [weapon_prop] WeaponProp06

## FemaleMS/Mone（R2T1MoneMd20011）

- 骨总数 ?；骨族 ?
- KawaiiPhysics 组 0 个（ABP 变体 0 个），链 0 条
- KawaiiCurve：cape, hair, skirt
- 状态：{'skeleton': 'export_missing', 'abp_skipped_no_skeleton': ['ABP_Seq_Mone_Phy']}

## FemaleMS/Rebecca（R2T1RebeccaMd10011）

- 骨总数 361；骨族 {'other:Root': 1, 'bip001': 105, 'weapon_prop': 7, 'hair': 48, 'facial': 20, 'ribbon': 117, 'other:BoneXtra_LHand001': 1, 'other:BoneXtra_LFinger011': 1, 'other:BoneXtra_LFinger012': 1, 'other:BoneXtra_LFinger013': 1, 'other:BoneXtraL_Finger021': 1, 'other:BoneXtraL_Finger022': 1, 'other:BoneXtraL_Finger023': 1, 'other:BoneXtraL_Finger031': 1, 'other:BoneXtraL_Finger032': 1, 'other:BoneXtraL_Finger033': 1, 'other:BoneXtra_LFinger000': 1, 'other:BoneXtra_LFinger001': 1, 'other:BoneXtra_LFinger002': 1, 'other:BoneXtra_RHand001': 1, 'other:BoneXtra_RFinger011': 1, 'other:BoneXtra_RFinger012': 1, 'other:BoneXtra_RFinger013': 1, 'other:BoneXtra_RFinger000': 1, 'other:BoneXtra_RFinger001': 1, 'other:BoneXtra_RFinger002': 1, 'other:BoneXtraR_Finger021': 1, 'other:BoneXtraR_Finger022': 1, 'other:BoneXtraR_Finger023': 1, 'other:BoneXtraR_Finger031': 1, 'other:BoneXtraR_Finger032': 1, 'other:BoneXtraR_Finger033': 1, 'chest': 4, 'skirt': 32, 'prop': 1}
- KawaiiPhysics 组 13 个（ABP 变体 1 个），链 37 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：155 根

### ABP_Seq_Rebecca_Phy / AnimGraphNode_KawaiiPhysicsGroup_12

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=35.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：Kawaii_alpha_S01

- [飘带(Piao)] Bone_Piao200_M
- [飘带(Piao)] Bone_Piao202_M

### ABP_Seq_Rebecca_Phy / AnimGraphNode_KawaiiPhysicsGroup_11

- 参数：Damping=0.15 Stiffness=0.05 LimitAngle=35.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：Kawaii_alpha_S01

- [飘带(Piao)] Bone_Piao200_R
- [飘带(Piao)] Bone_Piao200_L

### ABP_Seq_Rebecca_Phy / AnimGraphNode_KawaiiPhysicsGroup_10

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=20.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：Kawaii_alpha_S02

- [飘带(Piao)] Bone_Piao067_L
- [飘带(Piao)] Bone_Piao073_L

### ABP_Seq_Rebecca_Phy / AnimGraphNode_KawaiiPhysicsGroup_9

- 参数：Damping=0.15 Stiffness=0.05 LimitAngle=15.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：Kawaii_alpha_S02

- [飘带(Piao)] Bone_Piao061_L
- [飘带(Piao)] Bone_Piao079_L

### ABP_Seq_Rebecca_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=20.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：Kawaii_alpha_S02

- [飘带(Piao)] Bone_Piao069_L
- [飘带(Piao)] Bone_Piao075_L

### ABP_Seq_Rebecca_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.15 Stiffness=0.05 LimitAngle=20.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：Kawaii_alpha_S02

- [飘带(Piao)] Bone_Piao063_L
- [飘带(Piao)] Bone_Piao081_L

### ABP_Seq_Rebecca_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=20.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：Kawaii_alpha_S02

- [飘带(Piao)] Bone_Piao079_R
- [飘带(Piao)] Bone_Piao061_R

### ABP_Seq_Rebecca_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.15 Stiffness=0.05 LimitAngle=15.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：Kawaii_alpha_S02

- [飘带(Piao)] Bone_Piao073_R
- [飘带(Piao)] Bone_Piao067_R

### ABP_Seq_Rebecca_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=20.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：Kawaii_alpha_S02

- [飘带(Piao)] Bone_Piao081_R
- [飘带(Piao)] Bone_Piao063_R

### ABP_Seq_Rebecca_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.15 Stiffness=0.05 LimitAngle=20.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：Kawaii_alpha_S02

- [飘带(Piao)] Bone_Piao075_R
- [飘带(Piao)] Bone_Piao069_R

### ABP_Seq_Rebecca_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=15.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r9.690513
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S03

- [飘带(Piao)] Bone_Piao001_M → Bone_Piao002_M
- [飘带(Piao)] Bone_Piao011_R → Bone_Piao012_R
- [飘带(Piao)] Bone_Piao011_L → Bone_Piao012_L

### ABP_Seq_Rebecca_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.15 Stiffness=0.05 LimitAngle=15.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r9.690513
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S03

- [飘带(Piao)] Bone_Piao001_L → Bone_Piao002_L
- [飘带(Piao)] Bone_Piao001_R → Bone_Piao002_R

### ABP_Seq_Rebecca_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.2 Stiffness=0.15 LimitAngle=50.0 Radius=2.5 HalfWidth=3.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r9.400499
- 胶囊碰撞 6 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S04

- [头发] Bone_Hair002_L → Bone_Hair003_L → Bone_Hair041_L → Bone_Hair042_L → Bone_Hair043_L → Bone_Hair044_L
- [头发] Bone_Hair002_L → Bone_Hair003_L → Bone_Hair041_L → Bone_Hair061_L → Bone_Hair062_L
- [头发] Bone_Hair002_L → Bone_Hair003_L → Bone_Hair031_L → Bone_Hair032_L → Bone_Hair033_L → Bone_Hair034_L
- [头发] Bone_Hair002_L → Bone_Hair003_L → Bone_Hair031_L → Bone_Hair021_L → Bone_Hair022_L → Bone_Hair023_L → Bone_Hair024_L
- [头发] Bone_Hair002_L → Bone_Hair003_L → Bone_Hair031_L → Bone_Hair071_L → Bone_Hair072_L → Bone_Hair073_L
- [头发] Bone_Hair002_L → Bone_Hair003_L → Bone_Hair031_L → Bone_Hair051_L → Bone_Hair052_L → Bone_Hair053_L
- [头发] Bone_Hair002_R → Bone_Hair003_R → Bone_Hair041_R → Bone_Hair021_R → Bone_Hair022_R → Bone_Hair023_R → Bone_Hair024_R
- [头发] Bone_Hair002_R → Bone_Hair003_R → Bone_Hair041_R → Bone_Hair061_R → Bone_Hair062_R
- [头发] Bone_Hair002_R → Bone_Hair003_R → Bone_Hair041_R → Bone_Hair042_R → Bone_Hair043_R → Bone_Hair044_R
- [头发] Bone_Hair002_R → Bone_Hair003_R → Bone_Hair031_R → Bone_Hair032_R → Bone_Hair033_R → Bone_Hair034_R
- [头发] Bone_Hair002_R → Bone_Hair003_R → Bone_Hair031_R → Bone_Hair051_R → Bone_Hair052_R → Bone_Hair053_R
- [头发] Bone_Hair002_R → Bone_Hair003_R → Bone_Hair011_R → Bone_Hair012_R

## FemaleMS/Xigelika（R2T1XigelikaMd10011）

- 骨总数 340；骨族 {'other:Root': 1, 'bip001': 105, 'weapon_prop': 7, 'hair': 66, 'ribbon': 94, 'facial': 20, 'chest': 4, 'prop': 1, 'skirt': 42}
- KawaiiPhysics 组 1 个（ABP 变体 2 个），链 71 条
- KawaiiCurve：cape, hair, skirt
- 未入组二级骨：207 根
- 状态：{'chains_source': 'skeleton_heuristic (ABP 无 KawaiiPhysicsGroup 节点)'}

### skeleton_heuristic / heuristic_family_chains

- 参数：Damping=None Stiffness=None LimitAngle=None Radius=None HalfWidth=None Shape=None

- [头发] Bone_Hair031_R → Bone_Hair032_R → Bone_Hair033_R
- [头发] Bone_Hair031_L → Bone_Hair032_L → Bone_Hair033_L
- [头发] Bone_Hair051_R → Bone_Hair052_R → Bone_Hair053_R
- [头发] Bone_Hair081_R → Bone_Hair083_R → Bone_Hair084_R
- [头发] Bone_Hair081_R → Bone_Hair086_R → Bone_Hair087_R
- [头发] Bone_Hair071_R → Bone_Hair072_R → Bone_Hair074_R
- [头发] Bone_Hair061_R → Bone_Hair062_R → Bone_Hair063_R
- [头发] Bone_Hair041_L → Bone_Hair042_L → Bone_Hair043_L
- [飘带(Piao)] Bone_Piao065_L → Bone_Piao066_L
- [飘带(Piao)] Bone_Piao065_R → Bone_Piao066_R
- [飘带(Piao)] Bone_Piao051_R → Bone_Piao124_R
- [飘带(Piao)] Bone_Piao051_R → Bone_Piao125_R
- [飘带(Piao)] Bone_Piao051_R → Bone_Piao126_R
- [飘带(Piao)] Bone_Piao051_R → Bone_Piao127_R
- [飘带(Piao)] Bone_Piao051_L → Bone_Piao125_L
- [飘带(Piao)] Bone_Piao051_L → Bone_Piao124_L
- [飘带(Piao)] Bone_Piao051_L → Bone_Piao126_L
- [飘带(Piao)] Bone_Piao051_L → Bone_Piao127_L
- [头发] Bone_Hair011_L → Bone_Hair012_L → Bone_Hair013_L → Bone_Hair014_L → Bone_Hair015_L → Bone_Hair016_L → Bone_Hair017_L
- [头发] Bone_Hair011_R → Bone_Hair012_R → Bone_Hair013_R → Bone_Hair014_R → Bone_Hair015_R → Bone_Hair016_R → Bone_Hair017_R
- [头发] Bone_Hair088_L → Bone_Hair021_L → Bone_Hair022_L → Bone_Hair023_L → Bone_Hair024_L
- [头发] Bone_Hair088_R → Bone_Hair021_R → Bone_Hair022_R → Bone_Hair023_R → Bone_Hair024_R
- [头发] Bone_Hair089_R → Bone_Hair090_R → Bone_Hair091_R → Bone_Hair092_R → Bone_Hair041_R
- [头发] Bone_Hair001_L → Bone_Hair002_L → Bone_Hair003_L → Bone_Hair004_L → Bone_Hair005_L → Bone_Hair006_L → Bone_Hair007_L
- [头发] Bone_Hair001_R → Bone_Hair002_R → Bone_Hair003_R → Bone_Hair004_R → Bone_Hair005_R → Bone_Hair006_R → Bone_Hair007_R
- [飘带(Piao)] Bone_Piao001_M → Bone_Piao002_M
- [飘带(Piao)] Bone_Piao061_R → Bone_Piao062_R → Bone_Piao063_R → Bone_Piao059_R
- [飘带(Piao)] Bone_Piao061_L → Bone_Piao062_L → Bone_Piao063_L → Bone_Piao059_L
- [飘带(Piao)] Bone_Piao056_L → Bone_Piao057_L → Bone_Piao058_L
- [飘带(Piao)] Bone_Piao056_R → Bone_Piao057_R → Bone_Piao058_R
- [飘带(Piao)] Bone_Piao200_L → Bone_Piao201_L
- [飘带(Piao)] Bone_Piao203_R → Bone_Piao204_R
- [飘带(Piao)] Bone_Piao120_R
- [飘带(Piao)] Bone_Piao001_L → Bone_Piao002_L → Bone_Piao003_L
- [飘带(Piao)] Bone_Piao031_L → Bone_Piao038_L → Bone_Piao039_L → Bone_Piao040_L → Bone_Piao041_L → Bone_Piao042_L → Bone_Piao210_L
- [飘带(Piao)] Bone_Piao031_L → Bone_Piao038_L → Bone_Piao039_L → Bone_Piao040_L → Bone_Piao041_L → Bone_Piao042_L → Bone_Piao215_L
- [飘带(Piao)] Bone_Piao031_L → Bone_Piao038_L → Bone_Piao039_L → Bone_Piao040_L → Bone_Piao041_L → Bone_Piao042_L → Bone_Piao220_L
- [飘带(Piao)] Bone_Piao011_L → Bone_Piao141_L → Bone_Piao142_L
- [飘带(Piao)] Bone_Piao021_L → Bone_Piao022_L → Bone_Piao023_L
- [飘带(Piao)] Bone_Piao011_R → Bone_Piao141_R → Bone_Piao142_R
- [飘带(Piao)] Bone_Piao021_R → Bone_Piao022_R → Bone_Piao023_R
- [飘带(Piao)] Bone_Piao031_R → Bone_Piao038_R → Bone_Piao039_R → Bone_Piao040_R → Bone_Piao041_R → Bone_Piao042_R → Bone_Piao210_R
- [飘带(Piao)] Bone_Piao031_R → Bone_Piao038_R → Bone_Piao039_R → Bone_Piao040_R → Bone_Piao041_R → Bone_Piao042_R → Bone_Piao215_R
- [飘带(Piao)] Bone_Piao031_R → Bone_Piao038_R → Bone_Piao039_R → Bone_Piao040_R → Bone_Piao041_R → Bone_Piao042_R → Bone_Piao220_R
- [飘带(Piao)] Bone_Piao001_R → Bone_Piao002_R → Bone_Piao003_R
- [胸饰] Bone_Chest001_L → Bone_Chest002_L
- [胸饰] Bone_Chest001_R → Bone_Chest002_R
- [飘带(Piao)] Bone_Piao011_M → Bone_Piao012_R → Bone_Piao013_R → Bone_Piao014_R → Bone_Piao015_R
- [裙摆] Bone_Skirt001_R → Bone_Skirt002_R → Bone_Skirt003_R → Bone_Skirt004_R → Bone_Skirt046_R
- [裙摆] Bone_Skirt001_R → Bone_Skirt002_R → Bone_Skirt003_R → Bone_Skirt004_R → Bone_Skirt050_R → Bone_Skirt051_R
- [裙摆] Bone_Skirt001_L → Bone_Skirt002_L → Bone_Skirt003_L → Bone_Skirt004_L → Bone_Skirt046_L
- [裙摆] Bone_Skirt001_L → Bone_Skirt002_L → Bone_Skirt003_L → Bone_Skirt004_L → Bone_Skirt050_L → Bone_Skirt051_L
- [裙摆] Bone_Skirt011_R → Bone_Skirt012_R → Bone_Skirt013_R → Bone_Skirt014_R → Bone_Skirt017_R
- [裙摆] Bone_Skirt011_R → Bone_Skirt012_R → Bone_Skirt013_R → Bone_Skirt014_R → Bone_Skirt038_R → Bone_Skirt019_R
- [裙摆] Bone_Skirt011_R → Bone_Skirt012_R → Bone_Skirt013_R → Bone_Skirt014_R → Bone_Skirt039_R → Bone_Skirt021_R
- [裙摆] Bone_Skirt011_L → Bone_Skirt012_L → Bone_Skirt013_L → Bone_Skirt014_L → Bone_Skirt017_L
- [裙摆] Bone_Skirt011_L → Bone_Skirt012_L → Bone_Skirt013_L → Bone_Skirt014_L → Bone_Skirt038_L → Bone_Skirt021_L
- [裙摆] Bone_Skirt011_L → Bone_Skirt012_L → Bone_Skirt013_L → Bone_Skirt014_L → Bone_Skirt039_L → Bone_Skirt019_L
- [裙摆] Bone_Skirt031_L → Bone_Skirt032_L → Bone_Skirt033_L → Bone_Skirt034_L → Bone_Skirt035_L
- [裙摆] Bone_Skirt031_R → Bone_Skirt032_R → Bone_Skirt033_R → Bone_Skirt034_R → Bone_Skirt035_R
- [飘带(Piao)] Bone_Piao104_L
- [飘带(Piao)] Bone_Piao101_L
- [飘带(Piao)] Bone_Piao106_L
- [飘带(Piao)] Bone_Piao103_L
- [飘带(Piao)] Bone_Piao102_L
- [飘带(Piao)] Bone_Piao105_L
- [飘带(Piao)] Bone_Piao166_L → Bone_Piao167_L → Bone_Piao081_L → Bone_Piao082_L → Bone_Piao084_L → Bone_Piao085_L
- [飘带(Piao)] Bone_Piao165_L
- [飘带(Piao)] Bone_Piao164_L
- [飘带(Piao)] Bone_Piao163_L
- [飘带(Piao)] Bone_Piao078_R → Bone_Piao075_R → Bone_Piao076_R

## FemaleS/Anke（R2T1AnkeMd10011）

- 骨总数 200；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'other:Hair_daiMao01': 1, 'other:Hair_daiMao02': 1, 'other:Hair_daiMao03': 1, 'other:Hair_L_ceBin01': 1, 'other:Hair_L_ceBin02': 1, 'other:Hair_L_chuiTiao01': 1, 'other:Hair_L_chuiTiao02': 1, 'other:Hair_L_chuiTiao03': 1, 'other:Hair_L_liuhai01': 1, 'other:Hair_L_liuhai02': 1, 'other:Hair_L_liuhai03': 1, 'other:Hair_L_long01': 1, 'other:Hair_L_long02': 1, 'other:Hair_L_long03': 1, 'other:Hair_L_long04': 1, 'other:Hair_L_niMao01': 1, 'other:Hair_M_liuhai01': 1, 'other:Hair_M_liuhai02': 1, 'other:Hair_R_chuiTiao01': 1, 'other:Hair_R_chuiTiao02': 1, 'other:Hair_R_chuiTiao03': 1, 'other:Hair_R_liuhai01': 1, 'other:Hair_R_liuhai02': 1, 'other:Hair_R_liuhai03': 1, 'other:Hair_R_long01': 1, 'other:Hair_R_long02': 1, 'other:Hair_R_long03': 1, 'other:Hair_R_long04': 1, 'other:Hair_R_niMao01': 1, 'other:Piao_R_huDieJie01': 1, 'other:Piao_L_huDieJie01': 1, 'other:Piao_L_huDieJie03': 1, 'other:Piao_L_huDieJie04': 1, 'other:Piao_R_huDieJie03': 1, 'other:Piao_R_huDieJie04': 1, 'other:Piao_R_erZhui01': 1, 'other:Piao_L_erZhui01': 1, 'other:Hair_R_ceBin01': 1, 'other:Hair_R_ceBin02': 1, 'hair': 4, 'other:Hair_L_niMaoDn': 1, 'other:Hair_R_niMaoDn': 1, 'other:Piao_M_ChestB01': 1, 'other:Piao_neck_huDieJie01': 1, 'other:Piao_neck_huDieJie02': 1, 'other:Piao_neck_huDieJie03': 1, 'other:Piao_neck_huDieJie04': 1, 'other:Piao_neck_huDieJie05': 1, 'other:Piao_neck_huDieJie08': 1, 'other:Piao_neck_huDieJie07': 1, 'other:Piao_L_piJian01': 1, 'other:Piao_L_piJian02': 1, 'other:Piao_R_piJian01': 1, 'other:Piao_R_piJian02': 1, 'other:Piao_bao01': 1, 'other:Piao_jie01': 1, 'other:Piao_jie02': 1, 'other:Piao_jie03': 1, 'other:Piao_jie04': 1, 'other:Piao_jie05': 1, 'other:Skirt_L_A01': 1, 'other:Skirt_L_A02': 1, 'other:Skirt_L_A03': 1, 'other:Skirt_L_A04': 1, 'other:Skirt_L_A05': 1, 'other:Skirt_R_A04': 1, 'other:Skirt_R_A05': 1, 'other:Skirt_L_B01': 1, 'other:Skirt_L_B02': 1, 'other:Skirt_L_B03': 1, 'other:Skirt_L_C01': 1, 'other:Skirt_L_C02': 1, 'other:Skirt_L_C03': 1, 'other:Skirt_M_F01': 1, 'other:Skirt_M_F02': 1, 'other:Skirt_M_F03': 1, 'other:Skirt_R_B01': 1, 'other:Skirt_R_B02': 1, 'other:Skirt_R_B03': 1, 'other:Skirt_R_C01': 1, 'other:Skirt_R_C02': 1, 'other:Skirt_R_C03': 1, 'other:Skirt_L_yanWei01': 1, 'other:Skirt_L_yanWei02': 1, 'other:Skirt_L_yanWei03': 1, 'other:Skirt_L_yanWei04': 1, 'other:Skirt_R_yanWei01': 1, 'other:Skirt_R_yanWei02': 1, 'other:Skirt_R_yanWei03': 1, 'other:Skirt_R_yanWei04': 1, 'other:Piao_M_skirtB01': 1, 'other:Piao_L_skirtB01': 1, 'other:Piao_L_skirtB02': 1, 'other:Piao_L_skirtB03': 1, 'other:Piao_R_skirtB01': 1, 'other:Piao_R_skirtB02': 1, 'other:Piao_R_skirtB03': 1, 'prop': 1}
- KawaiiPhysics 组 13 个（ABP 变体 1 个），链 41 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：25 根

### ABP_Seq_Anke_Phy / AnimGraphNode_KawaiiPhysicsGroup_12

- 参数：Damping=0.3 Stiffness=0.05 LimitAngle=45.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.9022584
- Alpha 曲线：None

- [other:Hair_daiMao01] Hair_daiMao01 → Hair_daiMao02 → Hair_daiMao03
- [other:Hair_R_niMao01] Hair_R_niMao01
- [other:Hair_L_niMao01] Hair_L_niMao01

### ABP_Seq_Anke_Phy / AnimGraphNode_KawaiiPhysicsGroup_11

- 参数：Damping=0.1 Stiffness=0.05 LimitAngle=25.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_R_erZhui01] Piao_R_erZhui01
- [other:Piao_L_erZhui01] Piao_L_erZhui01

### ABP_Seq_Anke_Phy / AnimGraphNode_KawaiiPhysicsGroup_10

- 参数：Damping=0.3 Stiffness=0.05 LimitAngle=35.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 5 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [other:Hair_R_long01] Hair_R_long01 → Hair_R_long02 → Hair_R_long03 → Hair_R_long04
- [other:Hair_L_long01] Hair_L_long01 → Hair_L_long02 → Hair_L_long03 → Hair_L_long04

### ABP_Seq_Anke_Phy / AnimGraphNode_KawaiiPhysicsGroup_9

- 参数：Damping=0.1 Stiffness=0.05 LimitAngle=45.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r9.240143
- Alpha 曲线：None

- [other:Piao_L_huDieJie01] Piao_L_huDieJie01
- [other:Piao_R_huDieJie01] Piao_R_huDieJie01
- [other:Piao_L_huDieJie03] Piao_L_huDieJie03 → Piao_L_huDieJie04
- [other:Piao_R_huDieJie03] Piao_R_huDieJie03 → Piao_R_huDieJie04

### ABP_Seq_Anke_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.15 Stiffness=0.03 LimitAngle=35.0 Radius=4.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：None

- [other:Skirt_L_A02] Skirt_L_A02 → Skirt_L_A03 → Skirt_L_A04 → Skirt_L_A05
- [other:Skirt_L_A02] Skirt_L_A02 → Skirt_L_A03 → Skirt_R_A04 → Skirt_R_A05

### ABP_Seq_Anke_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.1 Stiffness=0.1 LimitAngle=45.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [other:Skirt_R_yanWei02] Skirt_R_yanWei02 → Skirt_R_yanWei03 → Skirt_R_yanWei04
- [other:Skirt_L_yanWei02] Skirt_L_yanWei02 → Skirt_L_yanWei03 → Skirt_L_yanWei04

### ABP_Seq_Anke_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=25.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_M_ChestB01] Piao_M_ChestB01
- [other:Piao_L_piJian02] Piao_L_piJian02
- [other:Piao_R_piJian02] Piao_R_piJian02

### ABP_Seq_Anke_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.1 Stiffness=0.1 LimitAngle=35.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_bao01] Piao_bao01
- [other:Piao_L_skirtB02] Piao_L_skirtB02 → Piao_L_skirtB03
- [other:Piao_R_skirtB02] Piao_R_skirtB02 → Piao_R_skirtB03

### ABP_Seq_Anke_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.1 Stiffness=0.05 LimitAngle=35.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_neck_huDieJie08] Piao_neck_huDieJie08
- [other:Piao_neck_huDieJie07] Piao_neck_huDieJie07
- [other:Piao_neck_huDieJie04] Piao_neck_huDieJie04 → Piao_neck_huDieJie05
- [other:Piao_neck_huDieJie02] Piao_neck_huDieJie02 → Piao_neck_huDieJie03

### ABP_Seq_Anke_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.3 Stiffness=0.05 LimitAngle=35.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.25
- Alpha 曲线：None

- [other:Hair_R_niMaoDn] Hair_R_niMaoDn
- [other:Hair_R_ceBin01] Hair_R_ceBin01 → Hair_R_ceBin02
- [other:Hair_R_chuiTiao01] Hair_R_chuiTiao01 → Hair_R_chuiTiao02 → Hair_R_chuiTiao03
- [other:Hair_L_niMaoDn] Hair_L_niMaoDn
- [other:Hair_L_ceBin01] Hair_L_ceBin01 → Hair_L_ceBin02
- [other:Hair_L_chuiTiao01] Hair_L_chuiTiao01 → Hair_L_chuiTiao02 → Hair_L_chuiTiao03

### ABP_Seq_Anke_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.3 Stiffness=0.05 LimitAngle=45.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.5
- Alpha 曲线：None

- [other:Hair_L_liuhai01] Hair_L_liuhai01 → Hair_L_liuhai02 → Hair_L_liuhai03
- [other:Hair_R_liuhai01] Hair_R_liuhai01 → Hair_R_liuhai02 → Hair_R_liuhai03
- [other:Hair_M_liuhai01] Hair_M_liuhai01 → Hair_M_liuhai02

### ABP_Seq_Anke_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.1 Stiffness=0.05 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_jie04] Piao_jie04 → Piao_jie05
- [other:Piao_jie02] Piao_jie02 → Piao_jie03

### ABP_Seq_Anke_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.15 Stiffness=0.1 LimitAngle=35.0 Radius=4.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：None

- [other:Skirt_M_F03] Skirt_M_F03
- [other:Skirt_R_C03] Skirt_R_C03
- [other:Skirt_R_B03] Skirt_R_B03
- [other:Skirt_L_C03] Skirt_L_C03
- [other:Skirt_L_B03] Skirt_L_B03

## FemaleS/Jueyuan（R2T1JueyuanMd10011）

- 骨总数 199；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'ribbon': 42, 'hair': 32, 'skirt': 20, 'prop': 1, 'other:Tail': 5}
- KawaiiPhysics 组 9 个（ABP 变体 1 个），链 35 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：25 根

### ABP_Seq_Jueyuan_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=45.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r8.5
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [头发] Bone_Hair021_R → Bone_Hair022_R
- [头发] Bone_Hair021_L → Bone_Hair022_L
- [飘带(Piao)] Bone_Piao041_R → Bone_Piao042_R → Bone_Piao043_R
- [飘带(Piao)] Bone_Piao041_L → Bone_Piao042_L → Bone_Piao043_L

### ABP_Seq_Jueyuan_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.2 Stiffness=0.03 LimitAngle=25.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 9 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [头发] Bone_Hair031_L → Bone_Hair032_L → Bone_Hair033_L → Bone_Hair034_L → Bone_Hair035_L → Bone_Hair036_L
- [头发] Bone_Hair031_R → Bone_Hair032_R → Bone_Hair033_R → Bone_Hair034_R → Bone_Hair035_R → Bone_Hair036_R

### ABP_Seq_Jueyuan_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.1 Stiffness=0.05 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao002_L
- [飘带(Piao)] Bone_Piao013_R
- [飘带(Piao)] Bone_Piao033

### ABP_Seq_Jueyuan_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.1 Stiffness=0.03 LimitAngle=40.0 Radius=3.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：None

- [other:Tail] Bone_Tail002 → Bone_Tail003 → Bone_Tail004 → Bone_Tail005

### ABP_Seq_Jueyuan_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=35.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：None

- [裙摆] Bone_Skirt004_R
- [裙摆] Bone_Skirt004_L
- [裙摆] Bone_Skirt024_L → Bone_Skirt025_L → Bone_Skirt026_L
- [裙摆] Bone_Skirt024_R → Bone_Skirt025_R → Bone_Skirt026_R

### ABP_Seq_Jueyuan_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=35.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao021_M → Bone_Piao022_M

### ABP_Seq_Jueyuan_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.1 Stiffness=0.05 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao061 → Bone_Piao062
- [飘带(Piao)] Bone_Piao064 → Bone_Piao065
- [飘带(Piao)] Bone_Piao067 → Bone_Piao068
- [飘带(Piao)] Bone_Piao070 → Bone_Piao071
- [飘带(Piao)] Bone_Piao073 → Bone_Piao074

### ABP_Seq_Jueyuan_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.05 Stiffness=0.005 LimitAngle=45.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao056_R → Bone_Piao057_R
- [飘带(Piao)] Bone_Piao053_R → Bone_Piao054_R
- [飘带(Piao)] Bone_Piao059_R
- [飘带(Piao)] Bone_Piao051_R
- [飘带(Piao)] Bone_Piao053_L → Bone_Piao054_L
- [飘带(Piao)] Bone_Piao056_L → Bone_Piao057_L
- [飘带(Piao)] Bone_Piao051_L
- [飘带(Piao)] Bone_Piao059_L

### ABP_Seq_Jueyuan_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=45.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r8.5
- Alpha 曲线：None

- [头发] Bone_Hair051_R → Bone_Hair052_R
- [头发] Bone_Hair041_R → Bone_Hair042_R
- [头发] Bone_Hair041 → Bone_Hair042
- [头发] Bone_Hair041_L → Bone_Hair042_L
- [头发] Bone_Hair051_L → Bone_Hair052_L
- [头发] Bone_Hair062_L → Bone_Hair063_L
- [头发] Bone_Hair062_R → Bone_Hair063_R

## FemaleS/Luokeke（R2T1LuokekeMd10011）

- 骨总数 248；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 35, 'other:Prop': 7, 'ribbon': 59, 'skirt': 47, 'prop': 1}
- KawaiiPhysics 组 11 个（ABP 变体 1 个），链 59 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：49 根

### ABP_Seq_Luokeke_Phy / AnimGraphNode_KawaiiPhysicsGroup_10

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：None

- [裙摆] Bone_Skirt004_L → Bone_Skirt006_L → Bone_Skirt007_L → Bone_Skirt009_L → Bone_Skirt010_L
- [裙摆] Bone_Skirt004_L → Bone_Skirt006_L → Bone_Skirt007_L → Bone_Skirt012_L → Bone_Skirt013_L
- [裙摆] Bone_Skirt004_L → Bone_Skirt006_L → Bone_Skirt007_L → Bone_Skirt015_L → Bone_Skirt016_L
- [裙摆] Bone_Skirt004_L → Bone_Skirt018_L → Bone_Skirt020_L → Bone_Skirt021_L
- [裙摆] Bone_Skirt004_L → Bone_Skirt018_L → Bone_Skirt023_L → Bone_Skirt024_L
- [裙摆] Bone_Skirt004_R → Bone_Skirt013_R → Bone_Skirt014_R → Bone_Skirt015_R
- [裙摆] Bone_Skirt004_R → Bone_Skirt005_R → Bone_Skirt006_R → Bone_Skirt007_R
- [裙摆] Bone_Skirt004_R → Bone_Skirt005_R → Bone_Skirt010_R → Bone_Skirt011_R

### ABP_Seq_Luokeke_Phy / AnimGraphNode_KawaiiPhysicsGroup_9

- 参数：Damping=0.2 Stiffness=0.3 LimitAngle=35.0 Radius=2.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：None

- [裙摆] Bone_Skirt302_R → Bone_Skirt303_R
- [裙摆] Bone_Skirt302_L → Bone_Skirt303_L
- [裙摆] Bone_Skirt306_L → Bone_Skirt307_L
- [裙摆] Bone_Skirt310_L → Bone_Skirt311_L
- [裙摆] Bone_Skirt310_R → Bone_Skirt311_R
- [裙摆] Bone_Skirt306_R → Bone_Skirt307_R
- [飘带(Piao)] Bone_Piao035_L

### ABP_Seq_Luokeke_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=35.0 Radius=2.0 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao021_R → Bone_Piao022_R
- [飘带(Piao)] Bone_Piao021_L → Bone_Piao022_L
- [飘带(Piao)] Bone_Piao002_L → Bone_Piao023_L
- [飘带(Piao)] Bone_Piao002_L → Bone_Piao003_L → Bone_Piao004_L
- [飘带(Piao)] Bone_Piao002_R → Bone_Piao023_R
- [飘带(Piao)] Bone_Piao002_R → Bone_Piao003_R → Bone_Piao004_R

### ABP_Seq_Luokeke_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=25.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r6.0
- Alpha 曲线：None

- [other:Prop] Bone_Prop001 → Bone_Prop009 → Bone_Prop010
- [other:Prop] Bone_Prop001 → Bone_Prop006 → Bone_Prop007
- [other:Prop] Bone_Prop001 → Bone_Prop003 → Bone_Prop004

### ABP_Seq_Luokeke_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=35.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.5
- Alpha 曲线：None

- [头发] Bone_Hair012_R
- [头发] Bone_Hair003_R
- [头发] Bone_Hair009_L
- [头发] Bone_Hair015_R

### ABP_Seq_Luokeke_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=45.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r8.5
- Alpha 曲线：None

- [头发] Bone_Hair004_L → Bone_Hair005_L → Bone_Hair006_L → Bone_Hair007_L
- [头发] Bone_Hair007_R → Bone_Hair008_R → Bone_Hair009_R → Bone_Hair010_R
- [头发] Bone_Hair018_R → Bone_Hair019_R
- [头发] Bone_Hair015_L → Bone_Hair016_L

### ABP_Seq_Luokeke_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=45.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao004_M
- [飘带(Piao)] Bone_Piao033_L
- [飘带(Piao)] Bone_Piao033_R
- [飘带(Piao)] Bone_Piao007_M
- [飘带(Piao)] Bone_Piao025_R
- [飘带(Piao)] Bone_Piao025_R

### ABP_Seq_Luokeke_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao039_R
- [飘带(Piao)] Bone_Piao037_R
- [飘带(Piao)] Bone_Piao039_L
- [飘带(Piao)] Bone_Piao037_L

### ABP_Seq_Luokeke_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=35.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r6.0
- Alpha 曲线：None

- [头发] Bone_Hair002_M → Bone_Hair003_M
- [头发] Bone_Hair006_M → Bone_Hair007_M

### ABP_Seq_Luokeke_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=45.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao028_R
- [飘带(Piao)] Bone_Piao030_R
- [飘带(Piao)] Bone_Piao032_R
- [飘带(Piao)] Bone_Piao005_M → Bone_Piao006_M
- [飘带(Piao)] Bone_Piao032_L
- [飘带(Piao)] Bone_Piao030_L
- [飘带(Piao)] Bone_Piao028_L

### ABP_Seq_Luokeke_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=45.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao008_R
- [飘带(Piao)] Bone_Piao011_R
- [飘带(Piao)] Bone_Piao014_R
- [飘带(Piao)] Bone_Piao017_R
- [飘带(Piao)] Bone_Piao008_L
- [飘带(Piao)] Bone_Piao011_L
- [飘带(Piao)] Bone_Piao014_L
- [飘带(Piao)] Bone_Piao017_L

## FemaleS/Suoming（R2T1SuomingMd10011）

- 骨总数 336；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 56, 'ribbon': 85, 'facial': 29, 'hand_twist': 2, 'other:Other': 4, 'other:LUpArmTwist': 1, 'other:RUpArmTwist': 1, 'chest': 4, 'other:Prop': 22, 'prop': 1, 'skirt': 30, 'other:LThighTwist': 1, 'other:RThighTwist': 1}
- KawaiiPhysics 组 10 个（ABP 变体 1 个），链 48 条
- KawaiiCurve：coat, hair, skirt
- 未入组二级骨：80 根

### ABP_Seq_Suoming_Phy / AnimGraphNode_KawaiiPhysicsGroup_9

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.1618786
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S07

- [头发] Bone_Hair002_M
- [头发] Bone_Hair002_R → Bone_Hair003_R
- [头发] Bone_Hair006_M
- [头发] Bone_Hair002_M
- [头发] Bone_Hair004_M
- [头发] Bone_Hair002_L → Bone_Hair003_L
- [头发] Bone_Hair005_L → Bone_Hair006_L
- [头发] Bone_Hair005_R → Bone_Hair006_R

### ABP_Seq_Suoming_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.2 Stiffness=0.04 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r8.3696995
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S09

- [飘带(Piao)] Bone_Piao001_L
- [飘带(Piao)] Bone_Piao001_R
- [头发] Bone_Hair022_L
- [飘带(Piao)] Bone_Piao003_L
- [头发] Bone_Hair022_R
- [飘带(Piao)] Bone_Piao003_R

### ABP_Seq_Suoming_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.2 Stiffness=0.15 LimitAngle=25.0 Radius=0.7 HalfWidth=2.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S08

- [other:Prop] Bone_Prop010_M → Bone_Prop010_R → Bone_Prop011_R
- [other:Prop] Bone_Prop010_M → Bone_Prop010_R → Bone_Prop012_R → Bone_Prop013_R → Bone_Prop014_R
- [other:Prop] Bone_Prop010_M → Bone_Prop010_L → Bone_Prop011_L
- [other:Prop] Bone_Prop010_M → Bone_Prop010_L → Bone_Prop012_L → Bone_Prop013_L → Bone_Prop014_L
- [other:Prop] Bone_Prop010_M → Bone_Prop015_M
- [other:Prop] Bone_Prop010_M → Bone_Prop015_L
- [other:Prop] Bone_Prop010_M → Bone_Prop015_R

### ABP_Seq_Suoming_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=50.0 Radius=1.2 HalfWidth=5.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S04

- [裙摆] Bone_Skirt002_R → Bone_Skirt003_R → Bone_Skirt004_R → Bone_Skirt005_R
- [裙摆] Bone_Skirt002_L → Bone_Skirt003_L → Bone_Skirt004_L → Bone_Skirt005_L

### ABP_Seq_Suoming_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.3 Stiffness=0.65 LimitAngle=10.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [胸饰] Bone_Chest001_R → Bone_Chest002_R
- [胸饰] Bone_Chest001_L → Bone_Chest002_L

### ABP_Seq_Suoming_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S05

- [裙摆] Bone_Skirt011_L → Bone_Skirt012_L → Bone_Skirt013_L → Bone_Skirt014_L
- [裙摆] Bone_Skirt021_L → Bone_Skirt022_L → Bone_Skirt023_L → Bone_Skirt024_L
- [裙摆] Bone_Skirt021_R → Bone_Skirt022_R → Bone_Skirt023_R → Bone_Skirt024_R
- [裙摆] Bone_Skirt011_R → Bone_Skirt012_R → Bone_Skirt013_R → Bone_Skirt014_R

### ABP_Seq_Suoming_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.2 Stiffness=0.1 LimitAngle=35.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r8.681116
- Alpha 曲线：Kawaii_alpha_S06

- [头发] Bone_Hair011_M → Bone_Hair012_M → Bone_Hair013_M → Bone_Hair014_M → Bone_Hair015_M

### ABP_Seq_Suoming_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.15 LimitAngle=50.0 Radius=2.5 HalfWidth=3.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S01

- [头发] Bone_Hair014_L → Bone_Hair015_L → Bone_Hair016_L → Bone_Hair017_L → Bone_Hair018_L → Bone_Hair019_L → Bone_Hair020_L
- [头发] Bone_Hair014_L → Bone_Hair015_L → Bone_Hair016_L → Bone_Hair030_L → Bone_Hair031_L → Bone_Hair032_L
- [头发] Bone_Hair014_R → Bone_Hair015_R → Bone_Hair016_R → Bone_Hair017_R → Bone_Hair018_R → Bone_Hair019_R → Bone_Hair020_R
- [头发] Bone_Hair014_R → Bone_Hair015_R → Bone_Hair016_R → Bone_Hair030_R → Bone_Hair031_R → Bone_Hair032_R

### ABP_Seq_Suoming_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=30.0 Radius=1.0 HalfWidth=2.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S03

- [飘带(Piao)] Bone_Piao093_R → Bone_Piao094_R → Bone_Piao095_R → Bone_Piao096_R
- [飘带(Piao)] Bone_Piao093_L → Bone_Piao094_L → Bone_Piao095_L → Bone_Piao096_L
- [飘带(Piao)] Bone_Piao101_R
- [飘带(Piao)] Bone_Piao101_L

### ABP_Seq_Suoming_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=50.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 9 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S02

- [飘带(Piao)] Bone_Piao038_R → Bone_Piao039_R → Bone_Piao040_R → Bone_Piao041_R
- [飘带(Piao)] Bone_Piao038_L → Bone_Piao039_L → Bone_Piao040_L → Bone_Piao041_L
- [飘带(Piao)] Bone_Piao123_L → Bone_Piao124_L → Bone_Piao125_L → Bone_Piao126_L → Bone_Piao127_L
- [other:Prop] Bone_Prop123_R → Bone_Prop124_R → Bone_Prop125_R → Bone_Prop126_R → Bone_Prop127_R
- [飘带(Piao)] Bone_Piao031_M → Bone_Piao032_M → Bone_Piao033_M → Bone_Piao034_M → Bone_Piao035_M → Bone_Piao036_M
- [飘带(Piao)] Bone_Piao031_M → Bone_Piao032_M → Bone_Piao070_L
- [飘带(Piao)] Bone_Piao031_M → Bone_Piao032_M → Bone_Piao070_R
- [飘带(Piao)] Bone_Piao031_M → Bone_Piao060_L → Bone_Piao061_L → Bone_Piao062_L → Bone_Piao063_L
- [飘带(Piao)] Bone_Piao031_M → Bone_Piao060_R → Bone_Piao061_R → Bone_Piao062_R → Bone_Piao063_R
- [飘带(Piao)] Bone_Piao034_M → Bone_Piao035_M → Bone_Piao036_M

## FemaleS/TestModel（TestModel01）

- 骨总数 ?；骨族 ?
- KawaiiPhysics 组 0 个（ABP 变体 0 个），链 0 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 状态：{'skeleton': 'export_missing'}

## FemaleS/TestModel（TestModel02）

- 骨总数 200；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'other:Hair_daiMao01': 1, 'other:Hair_daiMao02': 1, 'other:Hair_daiMao03': 1, 'other:Hair_L_ceBin01': 1, 'other:Hair_L_ceBin02': 1, 'other:Hair_L_chuiTiao01': 1, 'other:Hair_L_chuiTiao02': 1, 'other:Hair_L_chuiTiao03': 1, 'other:Hair_L_liuhai01': 1, 'other:Hair_L_liuhai02': 1, 'other:Hair_L_liuhai03': 1, 'other:Hair_L_long01': 1, 'other:Hair_L_long02': 1, 'other:Hair_L_long03': 1, 'other:Hair_L_long04': 1, 'other:Hair_L_niMao01': 1, 'other:Hair_M_liuhai01': 1, 'other:Hair_M_liuhai02': 1, 'other:Hair_R_chuiTiao01': 1, 'other:Hair_R_chuiTiao02': 1, 'other:Hair_R_chuiTiao03': 1, 'other:Hair_R_liuhai01': 1, 'other:Hair_R_liuhai02': 1, 'other:Hair_R_liuhai03': 1, 'other:Hair_R_long01': 1, 'other:Hair_R_long02': 1, 'other:Hair_R_long03': 1, 'other:Hair_R_long04': 1, 'other:Hair_R_niMao01': 1, 'other:Piao_R_huDieJie01': 1, 'other:Piao_L_huDieJie01': 1, 'other:Piao_L_huDieJie03': 1, 'other:Piao_L_huDieJie04': 1, 'other:Piao_R_huDieJie03': 1, 'other:Piao_R_huDieJie04': 1, 'other:Piao_R_erZhui01': 1, 'other:Piao_L_erZhui01': 1, 'other:Hair_R_ceBin01': 1, 'other:Hair_R_ceBin02': 1, 'hair': 4, 'other:Hair_L_niMaoDn': 1, 'other:Hair_R_niMaoDn': 1, 'other:Piao_M_ChestB01': 1, 'other:Piao_neck_huDieJie01': 1, 'other:Piao_neck_huDieJie02': 1, 'other:Piao_neck_huDieJie03': 1, 'other:Piao_neck_huDieJie04': 1, 'other:Piao_neck_huDieJie05': 1, 'other:Piao_neck_huDieJie08': 1, 'other:Piao_neck_huDieJie07': 1, 'other:Piao_L_piJian01': 1, 'other:Piao_L_piJian02': 1, 'other:Piao_R_piJian01': 1, 'other:Piao_R_piJian02': 1, 'other:Piao_bao01': 1, 'other:Piao_jie01': 1, 'other:Piao_jie02': 1, 'other:Piao_jie03': 1, 'other:Piao_jie04': 1, 'other:Piao_jie05': 1, 'other:Skirt_L_A01': 1, 'other:Skirt_L_A02': 1, 'other:Skirt_L_A03': 1, 'other:Skirt_L_A04': 1, 'other:Skirt_L_A05': 1, 'other:Skirt_R_A04': 1, 'other:Skirt_R_A05': 1, 'other:Skirt_L_B01': 1, 'other:Skirt_L_B02': 1, 'other:Skirt_L_B03': 1, 'other:Skirt_L_C01': 1, 'other:Skirt_L_C02': 1, 'other:Skirt_L_C03': 1, 'other:Skirt_M_F01': 1, 'other:Skirt_M_F02': 1, 'other:Skirt_M_F03': 1, 'other:Skirt_R_B01': 1, 'other:Skirt_R_B02': 1, 'other:Skirt_R_B03': 1, 'other:Skirt_R_C01': 1, 'other:Skirt_R_C02': 1, 'other:Skirt_R_C03': 1, 'other:Skirt_L_yanWei01': 1, 'other:Skirt_L_yanWei02': 1, 'other:Skirt_L_yanWei03': 1, 'other:Skirt_L_yanWei04': 1, 'other:Skirt_R_yanWei01': 1, 'other:Skirt_R_yanWei02': 1, 'other:Skirt_R_yanWei03': 1, 'other:Skirt_R_yanWei04': 1, 'other:Piao_M_skirtB01': 1, 'other:Piao_L_skirtB01': 1, 'other:Piao_L_skirtB02': 1, 'other:Piao_L_skirtB03': 1, 'other:Piao_R_skirtB01': 1, 'other:Piao_R_skirtB02': 1, 'other:Piao_R_skirtB03': 1, 'prop': 1}
- KawaiiPhysics 组 1 个（ABP 变体 1 个），链 4 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：101 根
- 状态：{'chains_source': 'skeleton_heuristic (ABP 无 KawaiiPhysicsGroup 节点)'}

### skeleton_heuristic / heuristic_family_chains

- 参数：Damping=None Stiffness=None LimitAngle=None Radius=None HalfWidth=None Shape=None

- [头发] Bone_Hair001_L → Bone_Hair002_L
- [头发] Bone_Hair001_L → Hair_L_niMaoDn
- [头发] Bone_Hair001_R → Bone_Hair002_R
- [头发] Bone_Hair001_R → Hair_R_niMaoDn

## FemaleS/Youhu（R2T1YouHuMd10011）

- 骨总数 219；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 36, 'ribbon': 64, 'prop': 1, 'other:Other': 1, 'skirt': 18}
- KawaiiPhysics 组 9 个（ABP 变体 1 个），链 46 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：52 根

### ABP_Seq_Youhu_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao083_R → Bone_Piao084_R → Bone_Piao085_R
- [飘带(Piao)] Bone_Piao083_L → Bone_Piao084_L → Bone_Piao085_L
- [飘带(Piao)] Bone_Piao088_M → Bone_Piao089_M

### ABP_Seq_Youhu_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.2 Stiffness=0.3 LimitAngle=35.0 Radius=2.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：None

- [裙摆] Bone_Skirt013_R
- [飘带(Piao)] Bone_Piao063_R
- [裙摆] Bone_Skirt013_L
- [飘带(Piao)] Bone_Piao063_L
- [裙摆] Bone_Skirt023_R
- [飘带(Piao)] Bone_Piao073_R
- [裙摆] Bone_Skirt023_L
- [飘带(Piao)] Bone_Piao072_L → Bone_Piao073_L
- [飘带(Piao)] Bone_Piao051_R
- [飘带(Piao)] Bone_Piao051_L
- [裙摆] Bone_Skirt003_R
- [裙摆] Bone_Skirt003_L

### ABP_Seq_Youhu_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=35.0 Radius=2.0 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao048_M
- [飘带(Piao)] Bone_Piao045_M → Bone_Piao046_M
- [飘带(Piao)] Bone_Piao100

### ABP_Seq_Youhu_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=25.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao011_M → Bone_Piao012_M → Bone_Piao013_M
- [飘带(Piao)] Bone_Piao011_M → Bone_Piao011_L
- [飘带(Piao)] Bone_Piao011_M → Bone_Piao011_R

### ABP_Seq_Youhu_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=35.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.5
- Alpha 曲线：None

- [头发] Bone_Hair002_R
- [头发] Bone_Hair012_R
- [头发] Bone_Hair022_R
- [头发] Bone_Hair032_R → Bone_Hair033_R
- [头发] Bone_Hair002_L
- [头发] Bone_Hair012_L → Bone_Hair013_L
- [头发] Bone_Hair032_L
- [头发] Bone_Hair022_L

### ABP_Seq_Youhu_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=45.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r8.5
- Alpha 曲线：None

- [头发] Bone_Hair052_R → Bone_Hair053_R → Bone_Hair054_R → Bone_Hair055_R
- [头发] Bone_Hair052_L → Bone_Hair053_L → Bone_Hair054_L → Bone_Hair055_L
- [头发] Bone_Hair042 → Bone_Hair043 → Bone_Hair044
- [飘带(Piao)] Bone_Piao001_L → Bone_Piao003_L
- [飘带(Piao)] Bone_Piao001_L → Bone_Piao002_L
- [飘带(Piao)] Bone_Piao001_L → Bone_Piao004_L
- [飘带(Piao)] Bone_Piao001_R → Bone_Piao003_R
- [飘带(Piao)] Bone_Piao001_R → Bone_Piao002_R
- [飘带(Piao)] Bone_Piao001_R → Bone_Piao004_R

### ABP_Seq_Youhu_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=45.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao031_R
- [飘带(Piao)] Bone_Piao021_R
- [飘带(Piao)] Bone_Piao021_M
- [飘带(Piao)] Bone_Piao021_L
- [飘带(Piao)] Bone_Piao031_L

### ABP_Seq_Youhu_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao092_L
- [飘带(Piao)] Bone_Piao092_R

### ABP_Seq_Youhu_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=35.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r6.0
- Alpha 曲线：None

- [头发] Bone_Hair002_M → Bone_Hair003_M

## FemaleXL/Aogusita（R2T1AogusitaMd10011）

- 骨总数 353；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 74, 'hand_twist': 2, 'ribbon': 64, 'chest': 4, 'prop': 1, 'skirt': 108, 'pelvis_extra': 1}
- KawaiiPhysics 组 10 个（ABP 变体 1 个），链 67 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：91 根

### ABP_Seq_Aogusita_Phy / AnimGraphNode_KawaiiPhysicsGroup_9

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=45.0 Radius=1.0 HalfWidth=2.5 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 5 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [裙摆] Bone_Skirt012_R → Bone_Skirt013_R → Bone_Skirt014_R → Bone_Skirt015_R → Bone_Skirt016_R → Bone_Skirt017_R → Bone_Skirt018_R → Bone_Skirt019_R
- [裙摆] Bone_Skirt012_L → Bone_Skirt013_L → Bone_Skirt014_L → Bone_Skirt015_L → Bone_Skirt016_L → Bone_Skirt017_L → Bone_Skirt018_L → Bone_Skirt019_L

### ABP_Seq_Aogusita_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=45.0 Radius=2.0 HalfWidth=2.5 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 5 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [裙摆] Bone_Skirt002_R → Bone_Skirt003_R → Bone_Skirt004_R → Bone_Skirt005_R → Bone_Skirt006_R → Bone_Skirt007_R
- [裙摆] Bone_Skirt002_M → Bone_Skirt003_M → Bone_Skirt004_M → Bone_Skirt005_M → Bone_Skirt006_M → Bone_Skirt007_M
- [裙摆] Bone_Skirt002_L → Bone_Skirt003_L → Bone_Skirt004_L → Bone_Skirt005_L → Bone_Skirt006_L → Bone_Skirt007_L

### ABP_Seq_Aogusita_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=45.0 Radius=1.0 HalfWidth=2.5 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [裙摆] Bone_Skirt022_L → Bone_Skirt023_L → Bone_Skirt024_L → Bone_Skirt025_L → Bone_Skirt026_L → Bone_Skirt027_L
- [裙摆] Bone_Skirt022_R → Bone_Skirt023_R → Bone_Skirt024_R → Bone_Skirt025_R → Bone_Skirt026_R → Bone_Skirt027_R
- [裙摆] Bone_Skirt035_L → Bone_Skirt036_L → Bone_Skirt037_L
- [裙摆] Bone_Skirt035_R → Bone_Skirt036_R → Bone_Skirt037_R

### ABP_Seq_Aogusita_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao013_L
- [飘带(Piao)] Bone_Piao011_L
- [飘带(Piao)] Bone_Piao009_L
- [飘带(Piao)] Bone_Piao009_R
- [飘带(Piao)] Bone_Piao011_R
- [飘带(Piao)] Bone_Piao013_R
- [飘带(Piao)] Bone_Piao002_R
- [飘带(Piao)] Bone_Piao003_R
- [飘带(Piao)] Bone_Piao004_R
- [飘带(Piao)] Bone_Piao005_R
- [飘带(Piao)] Bone_Piao006_R
- [飘带(Piao)] Bone_Piao007_R
- [飘带(Piao)] Bone_Piao002_L
- [飘带(Piao)] Bone_Piao003_L
- [飘带(Piao)] Bone_Piao004_L
- [飘带(Piao)] Bone_Piao005_L
- [飘带(Piao)] Bone_Piao006_L
- [飘带(Piao)] Bone_Piao007_L

### ABP_Seq_Aogusita_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.0 HalfWidth=2.5 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [裙摆] Bone_Skirt052_M → Bone_Skirt053_L
- [裙摆] Bone_Skirt052_M → Bone_Skirt053_R
- [裙摆] Bone_Skirt043_L
- [裙摆] Bone_Skirt043_R
- [裙摆] Bone_Skirt073_L → Bone_Skirt074_L → Bone_Skirt075_L → Bone_Skirt076_L
- [裙摆] Bone_Skirt083_L
- [裙摆] Bone_Skirt073_R → Bone_Skirt074_R → Bone_Skirt075_R → Bone_Skirt076_R
- [裙摆] Bone_Skirt083_R

### ABP_Seq_Aogusita_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=2.0 HalfWidth=3.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_jianxin

- [飘带(Piao)] Bone_Piao032 → Bone_Piao033 → Bone_Piao034
- [飘带(Piao)] Bone_Piao024 → Bone_Piao025 → Bone_Piao026 → Bone_Piao027 → Bone_Piao028
- [飘带(Piao)] Bone_Piao042 → Bone_Piao043 → Bone_Piao044

### ABP_Seq_Aogusita_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=35.0 Radius=3.5 HalfWidth=3.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [头发] Bone_Hair021_L → Bone_Hair022_L → Bone_Hair023_L → Bone_Hair024_L
- [头发] Bone_Hair025_L → Bone_Hair026_L → Bone_Hair027_L → Bone_Hair028_L
- [头发] Bone_Hair031_L → Bone_Hair032_L → Bone_Hair033_L → Bone_Hair034_L → Bone_Hair035_L
- [头发] Bone_Hair040_L → Bone_Hair041_L → Bone_Hair042_L → Bone_Hair043_L → Bone_Hair044_L
- [头发] Bone_Hair040_L → Bone_Hair045_L → Bone_Hair046_L → Bone_Hair047_L
- [头发] Bone_Hair040_R → Bone_Hair041_R → Bone_Hair042_R → Bone_Hair043_R
- [头发] Bone_Hair040_R → Bone_Hair044_R → Bone_Hair045_R
- [头发] Bone_Hair031_R → Bone_Hair032_R → Bone_Hair033_R → Bone_Hair034_R
- [头发] Bone_Hair031_R → Bone_Hair032_R → Bone_Hair038_R → Bone_Hair039_R
- [头发] Bone_Hair035_R → Bone_Hair036_R → Bone_Hair037_R
- [头发] Bone_Hair021_R → Bone_Hair022_R → Bone_Hair023_R → Bone_Hair024_R → Bone_Hair025_R

### ABP_Seq_Aogusita_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.562018
- Alpha 曲线：None

- [头发] Bone_Hair006_R → Bone_Hair007_R
- [头发] Bone_Hair008_R → Bone_Hair009_R
- [头发] Bone_Hair008_R → Bone_Hair010_R
- [头发] Bone_Hair003_R → Bone_Hair004_R
- [头发] Bone_Hair004_M
- [头发] Bone_Hair006_M
- [头发] Bone_Hair003_L → Bone_Hair004_L
- [头发] Bone_Hair006_L → Bone_Hair007_L
- [头发] Bone_Hair008_L → Bone_Hair009_L
- [头发] Bone_Hair001_L
- [头发] Bone_Hair001_R
- [头发] Bone_Hair011_L

### ABP_Seq_Aogusita_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=30.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [裙摆] Bone_Skirt049_L
- [飘带(Piao)] Bone_Piao017_L
- [飘带(Piao)] Bone_Piao017_R
- [飘带(Piao)] Bone_Piao002_M

### ABP_Seq_Aogusita_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.5 Stiffness=0.15 LimitAngle=10.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [胸饰] Bone_Chest001_R → Bone_Chest002_R
- [胸饰] Bone_Chest001_L → Bone_Chest002_L

## FemaleXL/BaiLian（R2T1BaiLianMd10011）

- 骨总数 197；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'other:Hair_L_chuitiao01': 1, 'other:Hair_L_chuitiao02': 1, 'other:Hair_L_chuitiao03': 1, 'other:Hair_L_chuitiao04': 1, 'other:Hair_L_chuitiao05': 1, 'hair': 12, 'other:Hair_M_liuhai01': 1, 'other:Hair_M_liuhai02': 1, 'other:Hair_M_liuhai03': 1, 'other:Hair_R_chuitiao01': 1, 'other:Hair_R_chuitiao02': 1, 'other:Hair_R_chuitiao03': 1, 'other:Hair_R_chuitiao04': 1, 'other:Hair_R_chuitiao05': 1, 'other:Piao_L_erZhui': 1, 'other:Piao_R_erZhui': 1, 'other:Hair_M_long01': 1, 'other:Hair_M_long02': 1, 'other:Hair_M_long03': 1, 'other:Hair_M_long04': 1, 'other:Hair_M_long05': 1, 'other:Hair_M_long06': 1, 'other:Hair_M_long07': 1, 'other:Hair_M_long08': 1, 'other:Hair_M_longRot': 1, 'other:Piao_L_hair01': 1, 'other:Piao_L_hair02': 1, 'other:Piao_L_hair03': 1, 'other:Piao_L_hair04': 1, 'other:Piao_R_hair01': 1, 'other:Piao_R_hair02': 1, 'other:Piao_R_hair03': 1, 'other:Piao_R_hair04': 1, 'chest': 4, 'other:Ohter': 2, 'other:Piao_L_Arm01': 1, 'other:Piao_L_Arm02': 1, 'other:Piao_L_Arm03': 1, 'other:Piao_L_Arm04': 1, 'other:Piao_L_Arm05': 1, 'other:Piao_L_C01': 1, 'other:Piao_L_C02': 1, 'other:Piao_L_C03': 1, 'other:Piao_L_C04': 1, 'other:Piao_L_C05': 1, 'other:Piao_L_C06': 1, 'other:Piao_L_C07': 1, 'other:Piao_R_Arm01': 1, 'other:Piao_R_Arm02': 1, 'other:Piao_R_Arm04': 1, 'other:Piao_R_Arm05': 1, 'other:Piao_R_Arm03': 1, 'other:Piao_R_C01': 1, 'other:Piao_R_C02': 1, 'other:Piao_R_C03': 1, 'other:Piao_R_C04': 1, 'other:Piao_R_C05': 1, 'other:Piao_R_C06': 1, 'other:Piao_R_C07': 1, 'prop': 1, 'other:Piao_R_Bao01': 1, 'other:Piao_R_Bao02': 1, 'other:Piao_R_Bao03': 1, 'other:Piao_L_Bao01': 1, 'other:Piao_L_Bao02': 1, 'other:Piao_L_Bao03': 1, 'ribbon': 9, 'skirt': 8}
- KawaiiPhysics 组 11 个（ABP 变体 1 个），链 30 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：22 根

### ABP_Seq_BaiLian_Phy / AnimGraphNode_KawaiiPhysicsGroup_10

- 参数：Damping=0.3 Stiffness=0.05 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r6.5
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_L_erZhui] Piao_L_erZhui
- [头发] Bone_Hair002_L
- [other:Hair_M_liuhai02] Hair_M_liuhai02 → Hair_M_liuhai03
- [头发] Bone_Hair002_R
- [other:Piao_L_Arm01] Piao_L_Arm01 → Piao_L_Arm02
- [other:Piao_R_Arm01] Piao_R_Arm01 → Piao_R_Arm02
- [头发] Bone_Hair007_R → Bone_Hair008_R → Bone_Hair009_R
- [头发] Bone_Hair005_L → Bone_Hair006_L → Bone_Hair007_L

### ABP_Seq_BaiLian_Phy / AnimGraphNode_KawaiiPhysicsGroup_9

- 参数：Damping=0.5 Stiffness=0.15 LimitAngle=15.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [胸饰] L_ChestBone01 → L_ChestBone02
- [胸饰] R_ChestBone01 → R_ChestBone02

### ABP_Seq_BaiLian_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.2 Stiffness=0.03 LimitAngle=35.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_R_hair03] Piao_R_hair03 → Piao_R_hair04
- [other:Piao_L_hair03] Piao_L_hair03 → Piao_L_hair04

### ABP_Seq_BaiLian_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.2 Stiffness=0.06 LimitAngle=35.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 6 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [other:Hair_M_long01] Hair_M_long01 → Hair_M_long02 → Hair_M_long03 → Hair_M_long04 → Hair_M_long05 → Hair_M_long06 → Hair_M_long07 → Hair_M_long08

### ABP_Seq_BaiLian_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.04 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [other:Hair_L_chuitiao02] Hair_L_chuitiao02 → Hair_L_chuitiao03 → Hair_L_chuitiao04 → Hair_L_chuitiao05
- [other:Hair_R_chuitiao02] Hair_R_chuitiao02 → Hair_R_chuitiao03 → Hair_R_chuitiao04 → Hair_R_chuitiao05

### ABP_Seq_BaiLian_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.2 Stiffness=0.04 LimitAngle=12.5 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_L_Bao01] Piao_L_Bao01 → Piao_L_Bao02 → Piao_L_Bao03
- [other:Piao_R_Bao01] Piao_R_Bao01 → Piao_R_Bao02 → Piao_R_Bao03
- [other:Ohter] Bone_Ohter001_R
- [other:Ohter] Bone_Ohter001_L

### ABP_Seq_BaiLian_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=60.0 Radius=1.5 HalfWidth=4.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [裙摆] Bone_Skirt001_L → Bone_Skirt003_L → Bone_Skirt004_L → Bone_Skirt005_L

### ABP_Seq_BaiLian_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.1 Stiffness=0.03 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_R_C03] Piao_R_C03 → Piao_R_C04 → Piao_R_C05 → Piao_R_C06 → Piao_R_C07
- [other:Piao_L_C03] Piao_L_C03 → Piao_L_C04 → Piao_L_C05 → Piao_L_C06 → Piao_L_C07

### ABP_Seq_BaiLian_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.15 Stiffness=0.05 LimitAngle=35.0 Radius=1.0 HalfWidth=1.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [飘带(Piao)] Bone_Piao001 → Bone_Piao002 → Bone_Piao003 → Bone_Piao004 → Bone_Piao005

### ABP_Seq_BaiLian_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=52.0 Radius=1.5 HalfWidth=4.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [裙摆] Bone_Skirt001_R → Bone_Skirt002_R → Bone_Skirt003_R → Bone_Skirt004_R

### ABP_Seq_BaiLian_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.2 Stiffness=0.04 LimitAngle=15.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_L_Arm03] Piao_L_Arm03
- [other:Piao_L_Arm04] Piao_L_Arm04
- [other:Piao_L_Arm05] Piao_L_Arm05
- [other:Piao_R_Arm03] Piao_R_Arm03
- [other:Piao_R_Arm04] Piao_R_Arm04
- [other:Piao_R_Arm05] Piao_R_Arm05

## FemaleXL/ChangLi（R2T1ChangLiMd10011）

- 骨总数 254；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 55, 'ribbon': 54, 'chest': 4, 'prop': 1, 'skirt': 34, 'other:Weapon': 7}
- KawaiiPhysics 组 10 个（ABP 变体 2 个），链 41 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：65 根

### ABP_Seq_ChangLi_Phy / AnimGraphNode_KawaiiPhysicsGroup_9

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r6.5
- Alpha 曲线：None

- [头发] Bone_Hair001_M → Bone_Hair002_M
- [头发] Bone_Hair004_M → Bone_Hair005_M
- [头发] Bone_Hair082_R → Bone_Hair083_R
- [头发] Bone_Hair082_L → Bone_Hair083_L
- [头发] Bone_Hair093_R
- [头发] Bone_Hair093_L
- [头发] Bone_Hair023_R
- [头发] Bone_Hair023_L
- [头发] Bone_Hair026_R
- [头发] Bone_Hair026_L
- [头发] Bone_Hair007_M → Bone_Hair008_M
- [头发] Bone_Hair103 → Bone_Hair104 → Bone_Hair105

### ABP_Seq_ChangLi_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.3 Stiffness=0.2 LimitAngle=20.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao023_L → Bone_Piao024_L
- [飘带(Piao)] Bone_Piao023_R → Bone_Piao024_R

### ABP_Seq_ChangLi_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.3 Stiffness=0.2 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [头发] Bone_Hair064_M → Bone_Hair065_M → Bone_Hair074_M
- [头发] Bone_Hair064_M → Bone_Hair065_M → Bone_Hair071_M → Bone_Hair072_M
- [头发] Bone_Hair064_M → Bone_Hair065_M → Bone_Hair067_M → Bone_Hair068_M → Bone_Hair069_M

### ABP_Seq_ChangLi_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=35.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao012_M → Bone_Piao013_M
- [飘带(Piao)] Bone_Piao011_R
- [飘带(Piao)] Bone_Piao011_L

### ABP_Seq_ChangLi_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [头发] Bone_Hair043_R → Bone_Hair044_R → Bone_Hair045_R
- [头发] Bone_Hair043_L → Bone_Hair044_L → Bone_Hair045_L

### ABP_Seq_ChangLi_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=52.0 Radius=1.5 HalfWidth=4.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [裙摆] Bone_Skirt021_R → Bone_Skirt022_R → Bone_Skirt023_R
- [裙摆] Bone_Skirt021_L → Bone_Skirt022_L → Bone_Skirt023_L

### ABP_Seq_ChangLi_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.5 Stiffness=0.15 LimitAngle=15.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [胸饰] L_ChestBone01 → L_ChestBone02
- [胸饰] R_ChestBone01 → R_ChestBone02

### ABP_Seq_ChangLi_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.3 LimitAngle=35.0 Radius=0.75 HalfWidth=3.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 5 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [裙摆] Bone_Skirt002_R → Bone_Skirt003_R → Bone_Skirt004_R → Bone_Skirt005_R → Bone_Skirt006_R
- [裙摆] Bone_Skirt002_L → Bone_Skirt003_L → Bone_Skirt004_L → Bone_Skirt005_L → Bone_Skirt006_L
- [飘带(Piao)] Bone_Piao052_R → Bone_Piao053_R → Bone_Piao054_R → Bone_Piao055_R → Bone_Piao056_R → Bone_Piao057_R
- [飘带(Piao)] Bone_Piao052_L → Bone_Piao053_L → Bone_Piao054_L → Bone_Piao055_L → Bone_Piao056_L → Bone_Piao057_L

### ABP_Seq_ChangLi_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.15 Stiffness=0.2 LimitAngle=35.0 Radius=3.0 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [裙摆] Bone_Skirt016_R
- [裙摆] Bone_Skirt016_L
- [裙摆] Bone_Skirt013_M → Bone_Skirt014_M

### ABP_Seq_ChangLi_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.2 Stiffness=0.15 LimitAngle=15.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao062_R
- [飘带(Piao)] Bone_Piao062_L
- [飘带(Piao)] Bone_Piao073_R
- [飘带(Piao)] Bone_Piao073_L
- [飘带(Piao)] Bone_Piao065_R → Bone_Piao066_R
- [飘带(Piao)] Bone_Piao064_L → Bone_Piao065_L → Bone_Piao066_L
- [飘带(Piao)] Bone_Piao069_R → Bone_Piao070_R
- [飘带(Piao)] Bone_Piao069_L → Bone_Piao070_L

## FemaleXL/ChangLi（R2T1ChangLiMd20011）

- 骨总数 ?；骨族 ?
- KawaiiPhysics 组 0 个（ABP 变体 0 个），链 0 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 状态：{'skeleton': 'export_missing', 'abp_skipped_no_skeleton': ['ABP_Seq_ChangLi_Link_Skin01_Phy', 'ABP_Seq_ChangLi_Phy']}

## FemaleXL/Jiabeilina（R2T1CalbrenaMd10011）

- 骨总数 369；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 66, 'ribbon': 157, 'other:Other': 9, 'hand_twist': 2, 'other:Wing': 3, 'chest': 4, 'prop': 1, 'skirt': 28}
- KawaiiPhysics 组 17 个（ABP 变体 1 个），链 77 条
- KawaiiCurve：generic
- 未入组二级骨：99 根

### ABP_Seq_Jiabeilina_Phy / AnimGraphNode_KawaiiPhysicsGroup_16

- 参数：Damping=0.15 Stiffness=0.2 LimitAngle=35.0 Radius=3.0 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：Kawaii_alpha_S03

- [裙摆] Bone_Skirt004_L → Bone_Skirt005_L → Bone_Skirt006_L
- [裙摆] Bone_Skirt003_R → Bone_Skirt004_R → Bone_Skirt005_R → Bone_Skirt006_R
- [裙摆] Bone_Skirt014_L → Bone_Skirt015_L → Bone_Skirt016_L → Bone_Skirt017_L → Bone_Skirt018_L
- [裙摆] Bone_Skirt014_R → Bone_Skirt015_R → Bone_Skirt016_R → Bone_Skirt017_R → Bone_Skirt018_R

### ABP_Seq_Jiabeilina_Phy / AnimGraphNode_KawaiiPhysicsGroup_15

- 参数：Damping=0.15 Stiffness=0.2 LimitAngle=35.0 Radius=3.0 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：Kawaii_alpha_S03

- [飘带(Piao)] Bone_Piao133_R → Bone_Piao134_R → Bone_Piao135_R → Bone_Piao136_R
- [飘带(Piao)] Bone_Piao133_L → Bone_Piao134_L → Bone_Piao135_L → Bone_Piao136_L

### ABP_Seq_Jiabeilina_Phy / AnimGraphNode_KawaiiPhysicsGroup_14

- 参数：Damping=0.1 Stiffness=0.05 LimitAngle=50.0 Radius=2.0 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [飘带(Piao)] Bone_Piao242_L → Bone_Piao243_L
- [飘带(Piao)] Bone_Piao262_M
- [飘带(Piao)] Bone_Piao252_R → Bone_Piao253_R

### ABP_Seq_Jiabeilina_Phy / AnimGraphNode_KawaiiPhysicsGroup_13

- 参数：Damping=0.15 Stiffness=0.04 LimitAngle=50.0 Radius=2.0 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [飘带(Piao)] Bone_Piao252_L → Bone_Piao253_L
- [飘带(Piao)] Bone_Piao242_R → Bone_Piao243_R

### ABP_Seq_Jiabeilina_Phy / AnimGraphNode_KawaiiPhysicsGroup_12

- 参数：Damping=0.09 Stiffness=0.06 LimitAngle=50.0 Radius=2.0 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [飘带(Piao)] Bone_Piao262_L → Bone_Piao263_L
- [飘带(Piao)] Bone_Piao262_R → Bone_Piao263_R

### ABP_Seq_Jiabeilina_Phy / AnimGraphNode_KawaiiPhysicsGroup_11

- 参数：Damping=0.3 Stiffness=0.2 LimitAngle=20.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao113_L → Bone_Piao114_L → Bone_Piao115_L → Bone_Piao116_L
- [飘带(Piao)] Bone_Piao113_R → Bone_Piao114_R → Bone_Piao115_R
- [飘带(Piao)] Bone_Piao073_L → Bone_Piao074_L → Bone_Piao075_L
- [飘带(Piao)] Bone_Piao073_R → Bone_Piao074_R
- [飘带(Piao)] Bone_Piao083_L → Bone_Piao084_L
- [飘带(Piao)] Bone_Piao083_R → Bone_Piao084_R
- [飘带(Piao)] Bone_Piao093_L → Bone_Piao094_L
- [飘带(Piao)] Bone_Piao093_R → Bone_Piao094_R → Bone_Piao095_R
- [飘带(Piao)] Bone_Piao103_L → Bone_Piao104_L
- [飘带(Piao)] Bone_Piao103_R → Bone_Piao104_R → Bone_Piao105_R
- [飘带(Piao)] Bone_Piao065_L → Bone_Piao066_L → Bone_Piao067_L

### ABP_Seq_Jiabeilina_Phy / AnimGraphNode_KawaiiPhysicsGroup_10

- 参数：Damping=0.1 Stiffness=0.05 LimitAngle=40.0 Radius=1.5 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [飘带(Piao)] Bone_Piao021_R → Bone_Piao022_R
- [飘带(Piao)] Bone_Piao157_R → Bone_Piao158_R
- [飘带(Piao)] Bone_Piao011_R → Bone_Piao012_R
- [飘带(Piao)] Bone_Piao011_L → Bone_Piao012_L

### ABP_Seq_Jiabeilina_Phy / AnimGraphNode_KawaiiPhysicsGroup_9

- 参数：Damping=0.15 Stiffness=0.04 LimitAngle=50.0 Radius=2.0 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S04

- [飘带(Piao)] Bone_Piao163_R → Bone_Piao164_R
- [飘带(Piao)] Bone_Piao154_R → Bone_Piao155_R
- [飘带(Piao)] Bone_Piao166_R → Bone_Piao167_R → Bone_Piao168_R
- [飘带(Piao)] Bone_Piao022_L → Bone_Piao023_L

### ABP_Seq_Jiabeilina_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.09 Stiffness=0.06 LimitAngle=50.0 Radius=2.0 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [飘带(Piao)] Bone_Piao160_R → Bone_Piao161_R
- [飘带(Piao)] Bone_Piao151_R → Bone_Piao152_R
- [飘带(Piao)] Bone_Piao170_R → Bone_Piao171_R

### ABP_Seq_Jiabeilina_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.5 Stiffness=0.15 LimitAngle=15.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [胸饰] Bone_Chest001_R → Bone_Chest002_R
- [胸饰] Bone_Chest001_L → Bone_Chest002_L

### ABP_Seq_Jiabeilina_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=40.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S01

- [头发] Bone_Hair022_R → Bone_Hair023_R → Bone_Hair024_R → Bone_Hair025_R
- [头发] Bone_Hair022_R → Bone_Hair031_R → Bone_Hair032_R → Bone_Hair033_R
- [头发] Bone_Hair022_L → Bone_Hair023_L → Bone_Hair024_L → Bone_Hair025_L
- [头发] Bone_Hair022_L → Bone_Hair031_L → Bone_Hair032_L → Bone_Hair033_L

### ABP_Seq_Jiabeilina_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r6.7855124
- Alpha 曲线：None

- [头发] Bone_Hair002_M → Bone_Hair003_M
- [头发] Bone_Hair002_L
- [头发] Bone_Hair002_R → Bone_Hair003_R
- [头发] Bone_Hair012_L → Bone_Hair013_L
- [头发] Bone_Hair012_R → Bone_Hair013_R

### ABP_Seq_Jiabeilina_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.1 Stiffness=0.05 LimitAngle=40.0 Radius=1.5 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r8.489878
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [头发] Bone_Hair101_M → Bone_Hair102_M
- [头发] Bone_Hair091_L → Bone_Hair092_L
- [头发] Bone_Hair081_L → Bone_Hair082_L
- [头发] Bone_Hair051_R → Bone_Hair052_R
- [头发] Bone_Hair043_M → Bone_Hair044_M → Bone_Hair045_M

### ABP_Seq_Jiabeilina_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.15 Stiffness=0.04 LimitAngle=50.0 Radius=2.0 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r8.489878
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [头发] Bone_Hair101_R → Bone_Hair102_R
- [头发] Bone_Hair071_L → Bone_Hair072_L
- [头发] Bone_Hair081_R → Bone_Hair082_R
- [头发] Bone_Hair051_L → Bone_Hair052_L
- [头发] Bone_Hair122_L → Bone_Hair123_L

### ABP_Seq_Jiabeilina_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.09 Stiffness=0.06 LimitAngle=50.0 Radius=2.0 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r8.489878
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [头发] Bone_Hair101_L → Bone_Hair102_L
- [头发] Bone_Hair091_R → Bone_Hair092_R
- [头发] Bone_Hair071_R → Bone_Hair072_R
- [头发] Bone_Hair111_L → Bone_Hair112_L
- [头发] Bone_Hair041_R → Bone_Hair042_R

### ABP_Seq_Jiabeilina_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.3 Stiffness=0.2 LimitAngle=20.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao184_R → Bone_Piao185_R → Bone_Piao186_R → Bone_Piao187_R → Bone_Piao188_R
- [飘带(Piao)] Bone_Piao184_R → Bone_Piao185_R → Bone_Piao209_R → Bone_Piao210_R
- [飘带(Piao)] Bone_Piao184_R → Bone_Piao185_R → Bone_Piao215_R → Bone_Piao216_R → Bone_Piao217_R
- [飘带(Piao)] Bone_Piao184_R → Bone_Piao185_R → Bone_Piao212_R → Bone_Piao213_R
- [飘带(Piao)] Bone_Piao184_R → Bone_Piao185_R → Bone_Piao219_R → Bone_Piao220_R
- [飘带(Piao)] Bone_Piao184_R → Bone_Piao203_R → Bone_Piao204_R
- [飘带(Piao)] Bone_Piao184_R → Bone_Piao206_R → Bone_Piao207_R

### ABP_Seq_Jiabeilina_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=50.0 Radius=1.5 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S02

- [飘带(Piao)] Bone_Piao183_R → Bone_Piao184_R → Bone_Piao185_R → Bone_Piao186_R → Bone_Piao187_R → Bone_Piao188_R
- [飘带(Piao)] Bone_Piao183_R → Bone_Piao184_R → Bone_Piao185_R → Bone_Piao209_R → Bone_Piao210_R
- [飘带(Piao)] Bone_Piao183_R → Bone_Piao184_R → Bone_Piao185_R → Bone_Piao215_R → Bone_Piao216_R → Bone_Piao217_R
- [飘带(Piao)] Bone_Piao183_R → Bone_Piao184_R → Bone_Piao185_R → Bone_Piao212_R → Bone_Piao213_R
- [飘带(Piao)] Bone_Piao183_R → Bone_Piao184_R → Bone_Piao185_R → Bone_Piao219_R → Bone_Piao220_R
- [飘带(Piao)] Bone_Piao183_R → Bone_Piao184_R → Bone_Piao203_R → Bone_Piao204_R
- [飘带(Piao)] Bone_Piao183_R → Bone_Piao184_R → Bone_Piao206_R → Bone_Piao207_R
- [飘带(Piao)] Bone_Piao183_R → Bone_Piao200_R → Bone_Piao201_R
- [飘带(Piao)] Bone_Piao183_R → Bone_Piao197_R → Bone_Piao198_R

## FemaleXL/Kanteleila（R2T1KanteleilaMd10011）

- 骨总数 266；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 48, 'ribbon': 70, 'other:Other': 4, 'chest': 6, 'prop': 1, 'skirt': 38}
- KawaiiPhysics 组 10 个（ABP 变体 1 个），链 48 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：64 根

### ABP_Seq_Kanteleila_Phy / AnimGraphNode_KawaiiPhysicsGroup_9

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=52.0 Radius=1.5 HalfWidth=4.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [裙摆] Bone_Skirt002_R → Bone_Skirt003_R → Bone_Skirt004_R
- [裙摆] Bone_Skirt002_L → Bone_Skirt003_L → Bone_Skirt004_L

### ABP_Seq_Kanteleila_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.15 Stiffness=0.2 LimitAngle=25.0 Radius=3.0 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [裙摆] Bone_Skirt014_L → Bone_Skirt015_L → Bone_Skirt016_L
- [裙摆] Bone_Skirt014_R → Bone_Skirt015_R → Bone_Skirt016_R
- [裙摆] Bone_Skirt026_L → Bone_Skirt027_L → Bone_Skirt028_L → Bone_Skirt029_L
- [裙摆] Bone_Skirt026_R → Bone_Skirt027_R → Bone_Skirt028_R → Bone_Skirt029_R
- [飘带(Piao)] Bone_Piao114_M → Bone_Piao115_M → Bone_Piao116_M
- [飘带(Piao)] Bone_Piao094_L → Bone_Piao095_L → Bone_Piao096_L

### ABP_Seq_Kanteleila_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.15 Stiffness=0.2 LimitAngle=25.0 Radius=2.0 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [飘带(Piao)] Bone_Piao094_L → Bone_Piao095_L → Bone_Piao096_L
- [飘带(Piao)] Bone_Piao100_L → Bone_Piao101_L
- [飘带(Piao)] Bone_Piao098_L
- [飘带(Piao)] Bone_Piao122_L → Bone_Piao123_L

### ABP_Seq_Kanteleila_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.5 Stiffness=0.15 LimitAngle=15.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [胸饰] Bone_Chest001_R → Bone_Chest002_R → Bone_Chest003_R
- [胸饰] Bone_Chest001_L → Bone_Chest002_L → Bone_Chest003_L

### ABP_Seq_Kanteleila_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.1 Stiffness=0.03 LimitAngle=15.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 2 个：Bone_Chest001_R@r5.522346, Bone_Chest001_L@r5.522346
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao035_M → Bone_Piao036_M

### ABP_Seq_Kanteleila_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.3 Stiffness=0.2 LimitAngle=20.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao042_L → Bone_Piao043_L → Bone_Piao044_L
- [飘带(Piao)] Bone_Piao042_R → Bone_Piao043_R → Bone_Piao044_R
- [飘带(Piao)] Bone_Piao052_L → Bone_Piao053_L
- [飘带(Piao)] Bone_Piao052_R → Bone_Piao053_R
- [飘带(Piao)] Bone_Piao062_L → Bone_Piao063_L
- [飘带(Piao)] Bone_Piao062_R → Bone_Piao063_R
- [飘带(Piao)] Bone_Piao072_L → Bone_Piao073_L
- [飘带(Piao)] Bone_Piao072_R → Bone_Piao073_R

### ABP_Seq_Kanteleila_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao021_R → Bone_Piao022_R
- [飘带(Piao)] Bone_Piao021_L → Bone_Piao022_L
- [飘带(Piao)] Bone_Piao002_M → Bone_Piao003_M

### ABP_Seq_Kanteleila_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [头发] Bone_Hair043_R → Bone_Hair044_R → Bone_Hair045_R → Bone_Hair046_R
- [头发] Bone_Hair043_L → Bone_Hair044_L → Bone_Hair045_L → Bone_Hair046_L
- [头发] Bone_Hair053_R → Bone_Hair054_R → Bone_Hair055_R → Bone_Hair056_R
- [头发] Bone_Hair053_L → Bone_Hair054_L → Bone_Hair055_L → Bone_Hair056_L

### ABP_Seq_Kanteleila_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r8.174379
- Alpha 曲线：None

- [头发] Bone_Hair002_M → Bone_Hair003_M
- [头发] Bone_Hair002_L → Bone_Hair003_L
- [头发] Bone_Hair002_R → Bone_Hair003_R
- [头发] Bone_Hair012_L → Bone_Hair013_L
- [头发] Bone_Hair012_R → Bone_Hair013_R
- [头发] Bone_Hair015_R
- [头发] Bone_Hair022_L
- [头发] Bone_Hair022_R → Bone_Hair023_R

### ABP_Seq_Kanteleila_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.25 Stiffness=0.15 LimitAngle=35.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r10.25462
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao002_L
- [飘带(Piao)] Bone_Piao011_L
- [飘带(Piao)] Bone_Piao011_R
- [飘带(Piao)] Bone_Piao013_L
- [飘带(Piao)] Bone_Piao013_R
- [飘带(Piao)] Bone_Piao011_M
- [飘带(Piao)] Bone_Piao013_M
- [飘带(Piao)] Bone_Piao007_L
- [飘带(Piao)] Bone_Piao009_L
- [飘带(Piao)] Bone_Piao005_L

## FemaleXL/Luosela（R2T1LuoselaMd10011）

- 骨总数 323；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 61, 'ribbon': 78, 'facial': 20, 'hand_twist': 2, 'other:Prop': 1, 'chest': 4, 'prop': 1, 'skirt': 52, 'other:Other': 4, 'pelvis_extra': 1}
- KawaiiPhysics 组 10 个（ABP 变体 1 个），链 39 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：76 根

### ABP_Seq_Luosela_Phy / AnimGraphNode_KawaiiPhysicsGroup_9

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=25.0 Radius=0.5 HalfWidth=3.0 Shape=EPhyCollisionShape::Planar
- 球碰撞 1 个：Bip001Head@r9.106802
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S03

- [飘带(Piao)] Bone_Piao081_L
- [飘带(Piao)] Bone_Piao081_R
- [飘带(Piao)] Bone_Piao047_R → Bone_Piao048_R
- [飘带(Piao)] Bone_Piao012_L
- [飘带(Piao)] Bone_Piao014_L
- [飘带(Piao)] Bone_Piao003_M
- [飘带(Piao)] Bone_Piao002_L
- [飘带(Piao)] Bone_Piao002_R
- [飘带(Piao)] Bone_Piao001_R
- [飘带(Piao)] Bone_Piao002_M
- [飘带(Piao)] Bone_Piao001_L

### ABP_Seq_Luosela_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=50.0 Radius=1.2 HalfWidth=5.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S01

- [裙摆] Bone_Skirt002_R → Bone_Skirt003_R → Bone_Skirt004_R → Bone_Skirt005_R → Bone_Skirt006_R → Bone_Skirt007_R → Bone_Skirt008_R
- [裙摆] Bone_Skirt002_L → Bone_Skirt003_L → Bone_Skirt004_L → Bone_Skirt005_L → Bone_Skirt006_L → Bone_Skirt007_L → Bone_Skirt008_L

### ABP_Seq_Luosela_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S02

- [裙摆] Bone_Skirt041_L → Bone_Skirt042_L → Bone_Skirt043_L → Bone_Skirt044_L → Bone_Skirt045_L → Bone_Skirt046_L → Bone_Skirt047_L
- [裙摆] Bone_Skirt041_R → Bone_Skirt042_R → Bone_Skirt043_R → Bone_Skirt044_R → Bone_Skirt045_R → Bone_Skirt046_R → Bone_Skirt047_R

### ABP_Seq_Luosela_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.15 LimitAngle=50.0 Radius=2.5 HalfWidth=3.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S04

- [头发] Bone_Hair042_L → Bone_Hair043_L → Bone_Hair044_L → Bone_Hair045_L → Bone_Hair046_L
- [头发] Bone_Hair042_L → Bone_Hair043_L → Bone_Hair044_L → Bone_Hair047_L → Bone_Hair048_L → Bone_Hair049_L
- [头发] Bone_Hair042_R → Bone_Hair043_R → Bone_Hair044_R → Bone_Hair045_R → Bone_Hair046_R
- [头发] Bone_Hair042_R → Bone_Hair043_R → Bone_Hair044_R → Bone_Hair047_R → Bone_Hair048_R → Bone_Hair049_R
- [头发] Bone_Hair055_L → Bone_Hair051_L → Bone_Hair052_L → Bone_Hair053_L → Bone_Hair054_L
- [头发] Bone_Hair055_R → Bone_Hair051_R → Bone_Hair052_R → Bone_Hair053_R → Bone_Hair054_R
- [头发] Bone_Hair022_M → Bone_Hair023_M → Bone_Hair024_M → Bone_Hair025_M → Bone_Hair026_M → Bone_Hair027_M
- [飘带(Piao)] Bone_Piao005_M → Bone_Piao006_M → Bone_Piao007_M → Bone_Piao009_M → Bone_Piao008_M
- [飘带(Piao)] Bone_Piao005_M → Bone_Piao005_L → Bone_Piao006_L
- [飘带(Piao)] Bone_Piao005_M → Bone_Piao005_R → Bone_Piao006_R
- [飘带(Piao)] Bone_Piao010_M → Bone_Piao011_M → Bone_Piao012_M → Bone_Piao013_M → Bone_Piao014_M
- [飘带(Piao)] Bone_Piao017_L → Bone_Piao018_L → Bone_Piao019_L

### ABP_Seq_Luosela_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=30.0 Radius=1.0 HalfWidth=2.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S06

- [飘带(Piao)] Bone_Piao047_L → Bone_Piao048_L → Bone_Piao049_L → Bone_Piao050_L

### ABP_Seq_Luosela_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=0.8 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S07

- [飘带(Piao)] Bone_Piao111_R → Bone_Piao112_R → Bone_Piao113_R
- [飘带(Piao)] Bone_Piao111_L → Bone_Piao112_L → Bone_Piao113_L

### ABP_Seq_Luosela_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.3 Stiffness=0.65 LimitAngle=10.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [胸饰] Bone_Chest001_L → Bone_Chest002_L
- [胸饰] Bone_Chest001_R → Bone_Chest002_R

### ABP_Seq_Luosela_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.04 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S05

- [头发] Bone_Hair032_L → Bone_Hair033_L → Bone_Hair034_L → Bone_Hair035_L → Bone_Hair036_L

### ABP_Seq_Luosela_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.728486
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S08

- [头发] Bone_Hair002_M → Bone_Hair003_M
- [头发] Bone_Hair002_R → Bone_Hair003_R → Bone_Hair004_R
- [头发] Bone_Hair011_R → Bone_Hair012_R → Bone_Hair013_R
- [头发] Bone_Hair011_L → Bone_Hair012_L → Bone_Hair013_L
- [头发] Bone_Hair002_L → Bone_Hair003_L → Bone_Hair004_L

### ABP_Seq_Luosela_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=50.0 Radius=1.2 HalfWidth=5.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S01

- [裙摆] Bone_Skirt010_L → Bone_Skirt011_L → Bone_Skirt012_L → Bone_Skirt013_L → Bone_Skirt014_L → Bone_Skirt015_L

## FemaleXL/TaiDaoNv（R2T1TaidaonvMd10011）

- 骨总数 372；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 10, 'hair': 52, 'ribbon': 117, 'facial': 20, 'hand_twist': 2, 'chest': 4, 'prop': 1, 'skirt': 70, 'other:Other': 4}
- KawaiiPhysics 组 14 个（ABP 变体 1 个），链 57 条
- KawaiiCurve：bow, hair, skirt
- 未入组二级骨：67 根

### ABP_Seq_TaiDaoNv_Phy / AnimGraphNode_KawaiiPhysicsGroup_13

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=50.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：Kawaii_alpha_S03

- [飘带(Piao)] Bone_Piao102_R → Bone_Piao103_R → Bone_Piao104_R → Bone_Piao105_R
- [飘带(Piao)] Bone_Piao102_L → Bone_Piao103_L → Bone_Piao104_L → Bone_Piao105_L
- [飘带(Piao)] Bone_Piao112_R → Bone_Piao113_R → Bone_Piao114_R → Bone_Piao115_R → Bone_Piao116_R
- [飘带(Piao)] Bone_Piao112_L → Bone_Piao113_L → Bone_Piao114_L → Bone_Piao115_L → Bone_Piao116_L
- [飘带(Piao)] Bone_Piao091_R → Bone_Piao092_R
- [飘带(Piao)] Bone_Piao091_L → Bone_Piao092_L
- [飘带(Piao)] Bone_Piao083_M → Bone_Piao084_M → Bone_Piao085_M

### ABP_Seq_TaiDaoNv_Phy / AnimGraphNode_KawaiiPhysicsGroup_12

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=50.0 Radius=1.2 HalfWidth=5.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S01

- [裙摆] Bone_Skirt001_R → Bone_Skirt002_R → Bone_Skirt003_R → Bone_Skirt004_R → Bone_Skirt005_R → Bone_Skirt006_R → Bone_Skirt007_R
- [裙摆] Bone_Skirt001_L → Bone_Skirt002_L → Bone_Skirt003_L → Bone_Skirt004_L → Bone_Skirt005_L → Bone_Skirt006_L → Bone_Skirt007_L
- [裙摆] Bone_Skirt011_R → Bone_Skirt012_R → Bone_Skirt013_R → Bone_Skirt014_R → Bone_Skirt015_R → Bone_Skirt016_R → Bone_Skirt017_R
- [裙摆] Bone_Skirt011_L → Bone_Skirt012_L → Bone_Skirt013_L → Bone_Skirt014_L → Bone_Skirt015_L → Bone_Skirt016_L → Bone_Skirt017_L
- [飘带(Piao)] Bone_Piao172_R → Bone_Piao173_R → Bone_Piao174_R → Bone_Piao175_R
- [飘带(Piao)] Bone_Piao172_L → Bone_Piao173_L → Bone_Piao174_L → Bone_Piao175_L

### ABP_Seq_TaiDaoNv_Phy / AnimGraphNode_KawaiiPhysicsGroup_11

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S02

- [裙摆] Bone_Skirt023_R → Bone_Skirt024_R → Bone_Skirt025_R → Bone_Skirt026_R → Bone_Skirt027_R
- [裙摆] Bone_Skirt023_L → Bone_Skirt024_L → Bone_Skirt025_L → Bone_Skirt026_L → Bone_Skirt027_L
- [裙摆] Bone_Skirt033_R → Bone_Skirt034_R → Bone_Skirt035_R → Bone_Skirt036_R → Bone_Skirt037_R
- [裙摆] Bone_Skirt033_L → Bone_Skirt034_L → Bone_Skirt035_L → Bone_Skirt036_L → Bone_Skirt037_L
- [裙摆] Bone_Skirt043_L → Bone_Skirt044_L → Bone_Skirt045_L → Bone_Skirt046_L → Bone_Skirt047_L
- [裙摆] Bone_Skirt043_R → Bone_Skirt044_R → Bone_Skirt045_R → Bone_Skirt046_R → Bone_Skirt047_R

### ABP_Seq_TaiDaoNv_Phy / AnimGraphNode_KawaiiPhysicsGroup_10

- 参数：Damping=0.2 Stiffness=0.15 LimitAngle=50.0 Radius=2.5 HalfWidth=3.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S05

- [头发] Bone_Hair053_M → Bone_Hair054_M → Bone_Hair055_M → Bone_Hair056_M → Bone_Hair057_M
- [头发] Bone_Hair053_M → Bone_Hair054_M → Bone_Hair055_M → Bone_Hair061_M → Bone_Hair062_M → Bone_Hair063_M
- [头发] Bone_Hair053_M → Bone_Hair054_M → Bone_Hair055_M → Bone_Hair061_R → Bone_Hair062_R
- [头发] Bone_Hair053_M → Bone_Hair054_M → Bone_Hair061_L → Bone_Hair062_L → Bone_Hair063_L

### ABP_Seq_TaiDaoNv_Phy / AnimGraphNode_KawaiiPhysicsGroup_9

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=30.0 Radius=1.0 HalfWidth=2.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S07

- [飘带(Piao)] Bone_Piao192_M → Bone_Piao193_M → Bone_Piao194_M

### ABP_Seq_TaiDaoNv_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=0.8 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S04

- [飘带(Piao)] Bone_Piao122_R → Bone_Piao123_R → Bone_Piao124_R → Bone_Piao125_R → Bone_Piao126_R
- [飘带(Piao)] Bone_Piao122_R → Bone_Piao123_R → Bone_Piao124_R → Bone_Piao131_R → Bone_Piao132_R → Bone_Piao133_R
- [飘带(Piao)] Bone_Piao122_L → Bone_Piao123_L → Bone_Piao124_L → Bone_Piao125_L → Bone_Piao126_L
- [飘带(Piao)] Bone_Piao122_L → Bone_Piao123_L → Bone_Piao124_L → Bone_Piao131_L → Bone_Piao132_L → Bone_Piao133_L

### ABP_Seq_TaiDaoNv_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.3 Stiffness=0.65 LimitAngle=10.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [胸饰] Bone_Chest001_L → Bone_Chest002_L
- [胸饰] Bone_Chest001_R → Bone_Chest002_R

### ABP_Seq_TaiDaoNv_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.04 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 5 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S06

- [头发] Bone_Hair042_L → Bone_Hair043_L → Bone_Hair044_L → Bone_Hair045_L
- [头发] Bone_Hair042_R → Bone_Hair043_R → Bone_Hair044_R → Bone_Hair045_R
- [飘带(Piao)] Bone_Piao185_R → Bone_Piao186_R
- [飘带(Piao)] Bone_Piao185_L → Bone_Piao186_L

### ABP_Seq_TaiDaoNv_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.537685
- Alpha 曲线：None

- [头发] Bone_Hair032_R → Bone_Hair033_R → Bone_Hair034_R
- [头发] Bone_Hair022_R → Bone_Hair023_R → Bone_Hair024_R
- [头发] Bone_Hair012_R → Bone_Hair013_R
- [头发] Bone_Hair012_R → Bone_Hair001_R → Bone_Hair002_R
- [头发] Bone_Hair002_M → Bone_Hair003_M
- [头发] Bone_Hair002_L → Bone_Hair003_L
- [头发] Bone_Hair022_L → Bone_Hair023_L → Bone_Hair024_L
- [头发] Bone_Hair032_L → Bone_Hair033_L → Bone_Hair034_L

### ABP_Seq_TaiDaoNv_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r9.723853
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao182_L → Bone_Piao183_L
- [飘带(Piao)] Bone_Piao182_R → Bone_Piao183_R

### ABP_Seq_TaiDaoNv_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=15.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao042_R
- [飘带(Piao)] Bone_Piao052_L
- [飘带(Piao)] Bone_Piao061_R → Bone_Piao062_R
- [飘带(Piao)] Bone_Piao061_M → Bone_Piao062_M

### ABP_Seq_TaiDaoNv_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.15 Stiffness=0.05 LimitAngle=20.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao042_L
- [飘带(Piao)] Bone_Piao052_R
- [飘带(Piao)] Bone_Piao061_L → Bone_Piao062_L

### ABP_Seq_TaiDaoNv_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.15 Stiffness=0.05 LimitAngle=20.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao032_R
- [飘带(Piao)] Bone_Piao151_L → Bone_Piao152_L
- [飘带(Piao)] Bone_Piao161_R → Bone_Piao162_R

### ABP_Seq_TaiDaoNv_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=15.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao032_L
- [飘带(Piao)] Bone_Piao151_R → Bone_Piao152_R
- [飘带(Piao)] Bone_Piao161_L → Bone_Piao162_L

## FemaleXL/TestModel（TestModel01）

- 骨总数 197；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'other:Hair_L_chuitiao01': 1, 'other:Hair_L_chuitiao02': 1, 'other:Hair_L_chuitiao03': 1, 'other:Hair_L_chuitiao04': 1, 'other:Hair_L_chuitiao05': 1, 'hair': 12, 'other:Hair_M_liuhai01': 1, 'other:Hair_M_liuhai02': 1, 'other:Hair_M_liuhai03': 1, 'other:Hair_R_chuitiao01': 1, 'other:Hair_R_chuitiao02': 1, 'other:Hair_R_chuitiao03': 1, 'other:Hair_R_chuitiao04': 1, 'other:Hair_R_chuitiao05': 1, 'other:Piao_L_erZhui': 1, 'other:Piao_R_erZhui': 1, 'other:Hair_M_long01': 1, 'other:Hair_M_long02': 1, 'other:Hair_M_long03': 1, 'other:Hair_M_long04': 1, 'other:Hair_M_long05': 1, 'other:Hair_M_long06': 1, 'other:Hair_M_long07': 1, 'other:Hair_M_long08': 1, 'other:Hair_M_longRot': 1, 'other:Piao_L_hair01': 1, 'other:Piao_L_hair02': 1, 'other:Piao_L_hair03': 1, 'other:Piao_L_hair04': 1, 'other:Piao_R_hair01': 1, 'other:Piao_R_hair02': 1, 'other:Piao_R_hair03': 1, 'other:Piao_R_hair04': 1, 'chest': 4, 'other:Ohter': 2, 'other:Piao_L_Arm01': 1, 'other:Piao_L_Arm02': 1, 'other:Piao_L_Arm03': 1, 'other:Piao_L_Arm04': 1, 'other:Piao_L_Arm05': 1, 'other:Piao_L_C01': 1, 'other:Piao_L_C02': 1, 'other:Piao_L_C03': 1, 'other:Piao_L_C04': 1, 'other:Piao_L_C05': 1, 'other:Piao_L_C06': 1, 'other:Piao_L_C07': 1, 'other:Piao_R_Arm01': 1, 'other:Piao_R_Arm02': 1, 'other:Piao_R_Arm04': 1, 'other:Piao_R_Arm05': 1, 'other:Piao_R_Arm03': 1, 'other:Piao_R_C01': 1, 'other:Piao_R_C02': 1, 'other:Piao_R_C03': 1, 'other:Piao_R_C04': 1, 'other:Piao_R_C05': 1, 'other:Piao_R_C06': 1, 'other:Piao_R_C07': 1, 'prop': 1, 'other:Piao_R_Bao01': 1, 'other:Piao_R_Bao02': 1, 'other:Piao_R_Bao03': 1, 'other:Piao_L_Bao01': 1, 'other:Piao_L_Bao02': 1, 'other:Piao_L_Bao03': 1, 'ribbon': 9, 'skirt': 8}
- KawaiiPhysics 组 1 个（ABP 变体 1 个），链 13 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：98 根
- 状态：{'chains_source': 'skeleton_heuristic (ABP 无 KawaiiPhysicsGroup 节点)'}

### skeleton_heuristic / heuristic_family_chains

- 参数：Damping=None Stiffness=None LimitAngle=None Radius=None HalfWidth=None Shape=None

- [头发] Bone_Hair001_L → Bone_Hair002_L
- [头发] Bone_Hair001_R → Bone_Hair002_R
- [头发] Bone_Hair004_L → Bone_Hair005_L → Bone_Hair006_L → Bone_Hair007_L
- [头发] Bone_Hair006_R → Bone_Hair007_R → Bone_Hair008_R → Bone_Hair009_R
- [胸饰] R_ChestBone01 → R_ChestBone02
- [胸饰] L_ChestBone01 → L_ChestBone02
- [飘带(Piao)] Bone_Piao001 → Bone_Piao002 → Bone_Piao003 → Bone_Piao004 → Bone_Piao005
- [裙摆] Bone_Skirt001_L → Bone_Skirt003_L → Bone_Skirt004_L → Bone_Skirt005_L
- [裙摆] Bone_Skirt001_R → Bone_Skirt002_R → Bone_Skirt003_R → Bone_Skirt004_R
- [飘带(Piao)] Bone_Piao011_L
- [飘带(Piao)] Bone_Piao011_R
- [飘带(Piao)] Bone_Piao012_L
- [飘带(Piao)] Bone_Piao012_R

## FemaleXL/Xinyuehu（R2T1XinMd10011）

- 骨总数 489；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 15, 'hair': 84, 'ribbon': 128, 'facial': 29, 'hand_twist': 2, 'other:Other': 8, 'other:Bake': 2, 'other:Clavicle': 2, 'other:Omuscle': 2, 'other:ZSpring_Spine_O01_L': 1, 'other:ZSpring_Spine_F01_R': 1, 'other:ZSpring_Spine_F02_L': 1, 'other:ZSpring_Spine_F02_R': 1, 'other:ZSpring_Spine_I01_L': 1, 'other:ZSpring_Spine_F03_R': 1, 'other:ZSpring_Spine_F01_L': 1, 'other:ZSpring_Spine_I01_R': 1, 'other:ZSpring_Spine_F03_L': 1, 'other:ZSpring_Spine_O01_R': 1, 'prop': 1, 'skirt': 60, 'other:Tail': 10, 'other:Toe': 28, 'other:ZSpring_Calf_B01_L': 1, 'other:ZSpring_Thigh_F03_L': 1, 'other:ZSpring_Thigh_B03_L': 1, 'other:ZSpring_Thigh_F02_L': 1, 'other:ZSpring_Thigh_B02_L': 1, 'other:ZSpring_Thigh_F01_L': 1, 'other:ZSpring_Thigh_B01_L': 1, 'other:ZSpring_Calf_B01_R': 1, 'other:ZSpring_Thigh_F03_R': 1, 'other:ZSpring_Thigh_B03_R': 1, 'other:ZSpring_Thigh_F02_R': 1, 'other:ZSpring_Thigh_B02_R': 1, 'other:ZSpring_Thigh_F01_R': 1, 'other:ZSpring_Thigh_B01_R': 1, 'other:ZSpring_Leg_B_R': 1, 'other:ZSpring_Leg_B_L': 1}
- KawaiiPhysics 组 15 个（ABP 变体 1 个），链 92 条
- KawaiiCurve：ear, hair, skirt, sleeve, tail, tie
- 未入组二级骨：129 根

### ABP_Seq_Xinyuehu_Phy / AnimGraphNode_KawaiiPhysicsGroup_14

- 参数：Damping=0.2 Stiffness=0.04 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r9.487536
- Alpha 曲线：Kawaii_alpha_S08

- [头发] Bone_Hair102_L → Bone_Hair103_L → Bone_Hair104_L
- [头发] Bone_Hair102_R → Bone_Hair103_R → Bone_Hair104_R
- [头发] Bone_Hair111_R → Bone_Hair112_R
- [头发] Bone_Hair111_L → Bone_Hair112_L

### ABP_Seq_Xinyuehu_Phy / AnimGraphNode_KawaiiPhysicsGroup_13

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=50.0 Radius=1.2 HalfWidth=5.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S06

- [裙摆] Bone_Skirt001_L → Bone_Skirt002_L → Bone_Skirt003_L → Bone_Skirt004_L → Bone_Skirt005_L → Bone_Skirt006_L → Bone_Skirt007_L
- [裙摆] Bone_Skirt001_R → Bone_Skirt002_R → Bone_Skirt003_R → Bone_Skirt004_R → Bone_Skirt005_R → Bone_Skirt006_R → Bone_Skirt007_R
- [飘带(Piao)] Bone_Piao101_L → Bone_Piao102_L → Bone_Piao103_L
- [飘带(Piao)] Bone_Piao101_R → Bone_Piao102_R → Bone_Piao103_R

### ABP_Seq_Xinyuehu_Phy / AnimGraphNode_KawaiiPhysicsGroup_12

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=25.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S07

- [裙摆] Bone_Skirt013_R → Bone_Skirt014_R → Bone_Skirt015_R → Bone_Skirt017_R → Bone_Skirt018_R
- [裙摆] Bone_Skirt013_R → Bone_Skirt014_R → Bone_Skirt015_R → Bone_Skirt021_R → Bone_Skirt022_R → Bone_Skirt023_R
- [裙摆] Bone_Skirt013_R → Bone_Skirt014_R → Bone_Skirt015_R → Bone_Skirt031_R → Bone_Skirt032_R
- [裙摆] Bone_Skirt013_R → Bone_Skirt014_R → Bone_Skirt015_R → Bone_Skirt041_R → Bone_Skirt042_R → Bone_Skirt043_R
- [裙摆] Bone_Skirt013_L → Bone_Skirt014_L → Bone_Skirt015_L → Bone_Skirt017_L → Bone_Skirt018_L
- [裙摆] Bone_Skirt013_L → Bone_Skirt014_L → Bone_Skirt015_L → Bone_Skirt021_L → Bone_Skirt022_L → Bone_Skirt023_L
- [裙摆] Bone_Skirt013_L → Bone_Skirt014_L → Bone_Skirt015_L → Bone_Skirt031_L → Bone_Skirt032_L
- [裙摆] Bone_Skirt013_L → Bone_Skirt014_L → Bone_Skirt015_L → Bone_Skirt041_L → Bone_Skirt042_L → Bone_Skirt043_L
- [裙摆] Bone_Skirt053_R → Bone_Skirt054_R → Bone_Skirt055_R → Bone_Skirt056_R
- [裙摆] Bone_Skirt053_L → Bone_Skirt054_L → Bone_Skirt055_L → Bone_Skirt056_L
- [飘带(Piao)] Bone_Piao083_R → Bone_Piao084_R → Bone_Piao085_R → Bone_Piao086_R → Bone_Piao087_R → Bone_Piao088_R → Bone_Piao089_R
- [飘带(Piao)] Bone_Piao083_R → Bone_Piao084_R → Bone_Piao085_R → Bone_Piao091_R → Bone_Piao092_R → Bone_Piao093_R → Bone_Piao094_R
- [飘带(Piao)] Bone_Piao083_L → Bone_Piao084_L → Bone_Piao085_L → Bone_Piao086_L → Bone_Piao087_L → Bone_Piao088_L → Bone_Piao089_L
- [飘带(Piao)] Bone_Piao083_L → Bone_Piao084_L → Bone_Piao085_L → Bone_Piao091_L → Bone_Piao092_L → Bone_Piao093_L → Bone_Piao094_L
- [飘带(Piao)] Bone_Piao071_R → Bone_Piao072_R → Bone_Piao073_R
- [飘带(Piao)] Bone_Piao071_L → Bone_Piao072_L → Bone_Piao073_L

### ABP_Seq_Xinyuehu_Phy / AnimGraphNode_KawaiiPhysicsGroup_11

- 参数：Damping=0.2 Stiffness=0.15 LimitAngle=50.0 Radius=2.5 HalfWidth=3.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S01

- [头发] Bone_Hair132_R → Bone_Hair133_R → Bone_Hair134_R → Bone_Hair135_R → Bone_Hair136_R

### ABP_Seq_Xinyuehu_Phy / AnimGraphNode_KawaiiPhysicsGroup_10

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=30.0 Radius=1.0 HalfWidth=2.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S03

- [飘带(Piao)] Bone_Piao061_R → Bone_Piao062_R
- [飘带(Piao)] Bone_Piao061_L → Bone_Piao062_L
- [飘带(Piao)] Bone_Piao062_M → Bone_Piao063_M → Bone_Piao064_M → Bone_Piao065_M → Bone_Piao066_M

### ABP_Seq_Xinyuehu_Phy / AnimGraphNode_KawaiiPhysicsGroup_9

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=50.0 Radius=0.8 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S10

- [飘带(Piao)] Bone_Piao032_R → Bone_Piao033_R → Bone_Piao034_R
- [飘带(Piao)] Bone_Piao032_R → Bone_Piao033_R → Bone_Piao133_R
- [飘带(Piao)] Bone_Piao032_R → Bone_Piao033_R → Bone_Piao135_R
- [飘带(Piao)] Bone_Piao032_R → Bone_Piao127_R → Bone_Piao128_R
- [飘带(Piao)] Bone_Piao032_R → Bone_Piao130_R → Bone_Piao131_R
- [飘带(Piao)] Bone_Piao032_L → Bone_Piao033_L → Bone_Piao034_L
- [飘带(Piao)] Bone_Piao032_L → Bone_Piao033_L → Bone_Piao133_L
- [飘带(Piao)] Bone_Piao032_L → Bone_Piao033_L → Bone_Piao135_L
- [飘带(Piao)] Bone_Piao032_L → Bone_Piao127_L → Bone_Piao128_L
- [飘带(Piao)] Bone_Piao032_L → Bone_Piao130_L → Bone_Piao131_L
- [飘带(Piao)] Bone_Piao042_R → Bone_Piao044_R → Bone_Piao045_R → Bone_Piao046_R → Bone_Piao047_R
- [飘带(Piao)] Bone_Piao042_L → Bone_Piao044_L → Bone_Piao045_L → Bone_Piao046_L → Bone_Piao047_L

### ABP_Seq_Xinyuehu_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.2 Stiffness=0.65 LimitAngle=20.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [other:ZSpring_Spine_O01_R] ZSpring_Spine_O01_R
- [other:ZSpring_Spine_F03_R] ZSpring_Spine_F03_R
- [other:ZSpring_Spine_I01_R] ZSpring_Spine_I01_R
- [other:ZSpring_Spine_F01_R] ZSpring_Spine_F01_R
- [other:ZSpring_Spine_F02_R] ZSpring_Spine_F02_R

### ABP_Seq_Xinyuehu_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.2 Stiffness=0.04 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S02

- [头发] Bone_Hair072_R → Bone_Hair073_R → Bone_Hair074_R → Bone_Hair075_R → Bone_Hair076_R
- [头发] Bone_Hair072_L → Bone_Hair073_L → Bone_Hair074_L → Bone_Hair075_L → Bone_Hair076_L
- [飘带(Piao)] Bone_Piao002_R → Bone_Piao003_R → Bone_Piao004_R
- [飘带(Piao)] Bone_Piao002_L → Bone_Piao003_L → Bone_Piao004_L

### ABP_Seq_Xinyuehu_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.9737835
- Alpha 曲线：Kawaii_alpha_S05

- [头发] Bone_Hair002_M → Bone_Hair003_M
- [头发] Bone_Hair012_R → Bone_Hair013_R → Bone_Hair044_R → Bone_Hair045_R
- [头发] Bone_Hair022_R → Bone_Hair023_R
- [头发] Bone_Hair053_R → Bone_Hair054_R → Bone_Hair055_R
- [头发] Bone_Hair063_R → Bone_Hair064_R → Bone_Hair065_R
- [头发] Bone_Hair011_L → Bone_Hair012_L
- [头发] Bone_Hair021_L → Bone_Hair022_L
- [头发] Bone_Hair042_L → Bone_Hair043_L → Bone_Hair044_L
- [头发] Bone_Hair053_L → Bone_Hair054_L
- [头发] Bone_Hair062_L → Bone_Hair063_L → Bone_Hair064_L → Bone_Hair065_L

### ABP_Seq_Xinyuehu_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.2 Stiffness=0.65 LimitAngle=20.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [other:ZSpring_Spine_O01_L] ZSpring_Spine_O01_L
- [other:ZSpring_Spine_F03_L] ZSpring_Spine_F03_L
- [other:ZSpring_Spine_I01_L] ZSpring_Spine_I01_L
- [other:ZSpring_Spine_F01_L] ZSpring_Spine_F01_L
- [other:ZSpring_Spine_F02_L] ZSpring_Spine_F02_L

### ABP_Seq_Xinyuehu_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.15 Stiffness=0.05 LimitAngle=20.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S11

- [飘带(Piao)] Bone_Piao127_L → Bone_Piao128_L
- [飘带(Piao)] Bone_Piao130_R → Bone_Piao131_R
- [飘带(Piao)] Bone_Piao133_R
- [飘带(Piao)] Bone_Piao135_L

### ABP_Seq_Xinyuehu_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=15.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S11

- [飘带(Piao)] Bone_Piao127_R → Bone_Piao128_R
- [飘带(Piao)] Bone_Piao130_L → Bone_Piao131_L
- [飘带(Piao)] Bone_Piao133_L
- [飘带(Piao)] Bone_Piao135_R

### ABP_Seq_Xinyuehu_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.973783
- Alpha 曲线：Kawaii_alpha_S04

- [头发] Bone_Hair092_L → Bone_Hair093_L → Bone_Hair094_L
- [头发] Bone_Hair092_R → Bone_Hair093_R → Bone_Hair094_R → Bone_Hair095_R
- [飘带(Piao)] Bone_Piao018_R → Bone_Piao019_R
- [飘带(Piao)] Bone_Piao022_R → Bone_Piao023_R
- [飘带(Piao)] Bone_Piao014_R → Bone_Piao015_R

### ABP_Seq_Xinyuehu_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=0.8 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S12

- [飘带(Piao)] Bone_Piao145_R → Bone_Piao151_R
- [飘带(Piao)] Bone_Piao145_L → Bone_Piao151_L
- [飘带(Piao)] Bone_Piao142_R
- [飘带(Piao)] Bone_Piao142_L
- [飘带(Piao)] Bone_Piao144_R
- [飘带(Piao)] Bone_Piao144_L
- [飘带(Piao)] Bone_Piao141_R
- [飘带(Piao)] Bone_Piao141_L
- [飘带(Piao)] Bone_Piao146_R
- [飘带(Piao)] Bone_Piao146_L
- [飘带(Piao)] Bone_Piao143_R
- [飘带(Piao)] Bone_Piao143_L
- [飘带(Piao)] Bone_Piao147_R
- [飘带(Piao)] Bone_Piao147_L

### ABP_Seq_Xinyuehu_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.2 Stiffness=0.04 LimitAngle=35.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：Kawaii_alpha_S09

- [other:Tail] Bone_Tail003_M → Bone_Tail004_M → Bone_Tail005_M → Bone_Tail006_M → Bone_Tail007_M → Bone_Tail008_M → Bone_Tail009_M → Bone_Tail010_M

## FemaleXL/XinyuehuShen（R2T1ShenXinMd10011）

- 骨总数 498；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 15, 'hair': 119, 'ribbon': 94, 'facial': 29, 'hand_twist': 2, 'other:Other': 8, 'other:Bake': 2, 'other:Clavicle': 2, 'other:Omuscle': 2, 'other:ZSpring_Spine_O01_L': 1, 'other:ZSpring_Spine_F01_R': 1, 'other:ZSpring_Spine_F02_L': 1, 'other:ZSpring_Spine_F02_R': 1, 'other:ZSpring_Spine_I01_L': 1, 'other:ZSpring_Spine_F03_R': 1, 'other:ZSpring_Spine_F01_L': 1, 'other:ZSpring_Spine_I01_R': 1, 'other:ZSpring_Spine_F03_L': 1, 'other:ZSpring_Spine_O01_R': 1, 'prop': 1, 'skirt': 68, 'other:Tail': 10, 'other:Toe': 28, 'other:ZSpring_Calf_B01_L': 1, 'other:ZSpring_Thigh_F03_L': 1, 'other:ZSpring_Thigh_B03_L': 1, 'other:ZSpring_Thigh_F02_L': 1, 'other:ZSpring_Thigh_B02_L': 1, 'other:ZSpring_Thigh_F01_L': 1, 'other:ZSpring_Thigh_B01_L': 1, 'other:ZSpring_Calf_B01_R': 1, 'other:ZSpring_Thigh_F03_R': 1, 'other:ZSpring_Thigh_B03_R': 1, 'other:ZSpring_Thigh_F02_R': 1, 'other:ZSpring_Thigh_B02_R': 1, 'other:ZSpring_Thigh_F01_R': 1, 'other:ZSpring_Thigh_B01_R': 1, 'other:ZSpring_Leg_B_R': 1, 'other:ZSpring_Leg_B_L': 1}
- KawaiiPhysics 组 1 个（ABP 变体 1 个），链 88 条
- KawaiiCurve：ear, hair, skirt, sleeve, tail, tie
- 未入组二级骨：362 根
- 状态：{'chains_source': 'skeleton_heuristic (ABP 无 KawaiiPhysicsGroup 节点)'}

### skeleton_heuristic / heuristic_family_chains

- 参数：Damping=None Stiffness=None LimitAngle=None Radius=None HalfWidth=None Shape=None

- [头发] Bone_Hair001_L → Bone_Hair002_L → Bone_Hair003_L
- [头发] Bone_Hair001_R → Bone_Hair002_R → Bone_Hair003_R → Bone_Hair004_R
- [头发] Bone_Hair011_L → Bone_Hair012_L → Bone_Hair013_L
- [头发] Bone_Hair011_R → Bone_Hair012_R → Bone_Hair013_R → Bone_Hair015_R → Bone_Hair016_R → Bone_Hair017_R
- [头发] Bone_Hair051_L → Bone_Hair052_L → Bone_Hair053_L → Bone_Hair054_L → Bone_Hair055_L → Bone_Hair056_L → Bone_Hair057_L → Bone_Hair058_L
- [头发] Bone_Hair051_R → Bone_Hair052_R → Bone_Hair053_R → Bone_Hair054_R → Bone_Hair055_R → Bone_Hair056_R → Bone_Hair057_R → Bone_Hair058_R
- [头发] Bone_Hair101_L → Bone_Hair102_L → Bone_Hair103_L → Bone_Hair104_L
- [头发] Bone_Hair101_R → Bone_Hair102_R → Bone_Hair103_R → Bone_Hair104_R
- [头发] Bone_Hair111_L → Bone_Hair112_L
- [头发] Bone_Hair111_R → Bone_Hair112_R
- [头发] Bone_Hair081_M → Bone_Hair082_M → Bone_Hair083_M → Bone_Hair084_M → Bone_Hair085_M → Bone_Hair086_M → Bone_Hair087_M
- [头发] Bone_Hair081_M → Bone_Hair082_M → Bone_Hair083_M → Bone_Hair111_M → Bone_Hair112_M → Bone_Hair113_M
- [飘带(Piao)] Bone_Piao001_M
- [头发] Bone_Hair071_L → Bone_Hair072_L → Bone_Hair073_L → Bone_Hair074_L → Bone_Hair075_L → Bone_Hair076_L → Bone_Hair077_L
- [头发] Bone_Hair081_L → Bone_Hair082_L → Bone_Hair083_L → Bone_Hair084_L → Bone_Hair085_L → Bone_Hair086_L
- [头发] Bone_Hair071_R → Bone_Hair072_R → Bone_Hair073_R → Bone_Hair075_R → Bone_Hair076_R → Bone_Hair077_R → Bone_Hair078_R
- [头发] Bone_Hair071_R → Bone_Hair072_R → Bone_Hair073_R → Bone_Hair075_R → Bone_Hair097_R → Bone_Hair098_R → Bone_Hair099_R
- [头发] Bone_Hair071_R → Bone_Hair072_R → Bone_Hair073_R → Bone_Hair094_R → Bone_Hair095_R → Bone_Hair096_R
- [头发] Bone_Hair081_R → Bone_Hair082_R → Bone_Hair083_R → Bone_Hair084_R → Bone_Hair085_R → Bone_Hair086_R
- [头发] Bone_Hair031_L → Bone_Hair032_L → Bone_Hair033_L → Bone_Hair035_L → Bone_Hair036_L → Bone_Hair037_L
- [头发] Bone_Hair031_R → Bone_Hair032_R → Bone_Hair033_R → Bone_Hair035_R → Bone_Hair036_R → Bone_Hair037_R
- [头发] Bone_Hair061_L → Bone_Hair062_L → Bone_Hair064_L → Bone_Hair065_L → Bone_Hair066_L
- [头发] Bone_Hair061_L → Bone_Hair062_L → Bone_Hair064_L → Bone_Hair067_L → Bone_Hair068_L
- [头发] Bone_Hair061_L → Bone_Hair062_L → Bone_Hair064_L → Bone_Hair069_L → Bone_Hair070_L
- [头发] Bone_Hair091_L → Bone_Hair092_L → Bone_Hair093_L → Bone_Hair094_L → Bone_Hair095_L
- [头发] Bone_Hair061_R → Bone_Hair062_R → Bone_Hair064_R → Bone_Hair065_R → Bone_Hair066_R
- [头发] Bone_Hair061_R → Bone_Hair062_R → Bone_Hair091_R → Bone_Hair092_R
- [飘带(Piao)] Bone_Piao011_L → Bone_Piao012_L → Bone_Piao013_L → Bone_Piao014_L → Bone_Piao015_L
- [飘带(Piao)] Bone_Piao011_R → Bone_Piao012_R → Bone_Piao013_R → Bone_Piao014_R → Bone_Piao015_R
- [飘带(Piao)] Bone_Piao031_L → Bone_Piao032_L → Bone_Piao033_L → Bone_Piao034_L
- [飘带(Piao)] Bone_Piao031_L → Bone_Piao032_L → Bone_Piao033_L → Bone_Piao133_L
- [飘带(Piao)] Bone_Piao031_L → Bone_Piao032_L → Bone_Piao033_L → Bone_Piao135_L
- [飘带(Piao)] Bone_Piao031_L → Bone_Piao032_L → Bone_Piao033_L → Bone_Piao021_L → Bone_Piao022_L → Bone_Piao023_L
- [飘带(Piao)] Bone_Piao031_L → Bone_Piao032_L → Bone_Piao127_L → Bone_Piao128_L
- [飘带(Piao)] Bone_Piao031_L → Bone_Piao032_L → Bone_Piao130_L → Bone_Piao131_L
- [飘带(Piao)] Bone_Piao031_L → Bone_Piao121_L
- [飘带(Piao)] Bone_Piao031_L → Bone_Piao124_L
- [飘带(Piao)] Bone_Piao122_L
- [飘带(Piao)] Bone_Piao125_L
- [飘带(Piao)] Bone_Piao141_L
- [飘带(Piao)] Bone_Piao142_L
- [飘带(Piao)] Bone_Piao143_L
- [飘带(Piao)] Bone_Piao144_L
- [飘带(Piao)] Bone_Piao145_L → Bone_Piao151_L
- [飘带(Piao)] Bone_Piao146_L
- [飘带(Piao)] Bone_Piao147_L
- [飘带(Piao)] Bone_Piao031_R → Bone_Piao032_R → Bone_Piao033_R → Bone_Piao034_R
- [飘带(Piao)] Bone_Piao031_R → Bone_Piao032_R → Bone_Piao033_R → Bone_Piao133_R
- [飘带(Piao)] Bone_Piao031_R → Bone_Piao032_R → Bone_Piao033_R → Bone_Piao135_R
- [飘带(Piao)] Bone_Piao031_R → Bone_Piao032_R → Bone_Piao033_R → Bone_Piao021_R → Bone_Piao022_R → Bone_Piao023_R
- [飘带(Piao)] Bone_Piao031_R → Bone_Piao032_R → Bone_Piao127_R → Bone_Piao128_R
- [飘带(Piao)] Bone_Piao031_R → Bone_Piao032_R → Bone_Piao130_R → Bone_Piao131_R
- [飘带(Piao)] Bone_Piao031_R → Bone_Piao121_R
- [飘带(Piao)] Bone_Piao031_R → Bone_Piao124_R
- [飘带(Piao)] Bone_Piao122_R
- [飘带(Piao)] Bone_Piao125_R
- [飘带(Piao)] Bone_Piao141_R
- [飘带(Piao)] Bone_Piao142_R
- [飘带(Piao)] Bone_Piao143_R
- [飘带(Piao)] Bone_Piao144_R
- [飘带(Piao)] Bone_Piao145_R → Bone_Piao151_R
- [飘带(Piao)] Bone_Piao146_R
- [飘带(Piao)] Bone_Piao147_R
- [飘带(Piao)] Bone_Piao051_M → Bone_Piao061_M → Bone_Piao062_M → Bone_Piao063_M → Bone_Piao064_M → Bone_Piao065_M → Bone_Piao066_M
- [飘带(Piao)] Bone_Piao051_M → Bone_Piao061_M → Bone_Piao061_L → Bone_Piao062_L
- [飘带(Piao)] Bone_Piao051_M → Bone_Piao061_M → Bone_Piao061_R → Bone_Piao062_R
- [飘带(Piao)] Bone_Piao051_M → Bone_Piao051_L → Bone_Piao052_L
- [飘带(Piao)] Bone_Piao051_M → Bone_Piao051_R → Bone_Piao052_R
- [飘带(Piao)] Bone_Piao071_L → Bone_Piao072_L
- [飘带(Piao)] Bone_Piao071_R → Bone_Piao072_R
- [飘带(Piao)] Bone_Piao101_L → Bone_Piao102_L → Bone_Piao103_L
- [飘带(Piao)] Bone_Piao101_R → Bone_Piao102_R → Bone_Piao103_R
- [裙摆] Bone_Skirt021_L → Bone_Skirt022_L → Bone_Skirt023_L → Bone_Skirt024_L → Bone_Skirt025_L → Bone_Skirt026_L → Bone_Skirt027_L
- [裙摆] Bone_Skirt001_R → Bone_Skirt002_R → Bone_Skirt003_R → Bone_Skirt004_R → Bone_Skirt005_R → Bone_Skirt006_R → Bone_Skirt007_R
- [裙摆] Bone_Skirt021_R → Bone_Skirt022_R → Bone_Skirt023_R → Bone_Skirt024_R → Bone_Skirt025_R → Bone_Skirt026_R → Bone_Skirt027_R
- [裙摆] Bone_Skirt031_L → Bone_Skirt032_L → Bone_Skirt033_L → Bone_Skirt034_L → Bone_Skirt035_L → Bone_Skirt036_L → Bone_Skirt037_L → Bone_Skirt038_L → Bone_Skirt039_L
- [裙摆] Bone_Skirt031_L → Bone_Skirt032_L → Bone_Skirt033_L → Bone_Skirt034_L → Bone_Skirt035_L → Bone_Piao041_L → Bone_Piao042_L → Bone_Piao043_L → Bone_Piao044_L
- [飘带(Piao)] Bone_Piao041_L → Bone_Piao042_L → Bone_Piao043_L → Bone_Piao044_L
- [裙摆] Bone_Skirt031_R → Bone_Skirt032_R → Bone_Skirt033_R → Bone_Skirt034_R → Bone_Skirt035_R → Bone_Skirt036_R → Bone_Skirt037_R → Bone_Skirt038_R → Bone_Skirt039_R
- [裙摆] Bone_Skirt031_R → Bone_Skirt032_R → Bone_Skirt033_R → Bone_Skirt034_R → Bone_Skirt035_R → Bone_Piao041_R → Bone_Piao042_R → Bone_Piao043_R → Bone_Piao044_R
- [飘带(Piao)] Bone_Piao041_R → Bone_Piao042_R → Bone_Piao043_R → Bone_Piao044_R
- [裙摆] Bone_Skirt011_L → Bone_Skirt012_L → Bone_Skirt013_L → Bone_Skirt014_L → Bone_Skirt015_L → Bone_Skirt016_L → Bone_Skirt017_L → Bone_Skirt018_L → Bone_Skirt019_L
- [裙摆] Bone_Skirt011_L → Bone_Skirt012_L → Bone_Skirt042_L
- [裙摆] Bone_Skirt011_L → Bone_Skirt041_L
- [裙摆] Bone_Skirt011_R → Bone_Skirt012_R → Bone_Skirt013_R → Bone_Skirt014_R → Bone_Skirt015_R → Bone_Skirt016_R → Bone_Skirt017_R → Bone_Skirt018_R → Bone_Skirt019_R
- [裙摆] Bone_Skirt011_R → Bone_Skirt012_R → Bone_Skirt042_R
- [裙摆] Bone_Skirt011_R → Bone_Skirt041_R
- [裙摆] Bone_Skirt001_L → Bone_Skirt002_L → Bone_Skirt003_L → Bone_Skirt004_L → Bone_Skirt005_L → Bone_Skirt006_L → Bone_Skirt007_L

## FemaleXL/Zanni（R2T1ZanniMd10011）

- 骨总数 364；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'ribbon': 109, 'hair': 148, 'hand_twist': 2, 'chest': 4, 'prop': 1, 'pelvis_extra': 1}
- KawaiiPhysics 组 14 个（ABP 变体 2 个），链 66 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：104 根

### ABP_Seq_Zanni_Phy / AnimGraphNode_KawaiiPhysicsGroup_13

- 参数：Damping=0.2 Stiffness=0.3 LimitAngle=35.0 Radius=4.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：None

- [头发] Bone_Hair018_L → Bone_Hair019_L
- [头发] Bone_Hair011_L → Bone_Hair012_L
- [头发] Bone_Hair030_R → Bone_Hair031_R
- [头发] Bone_Hair034_R → Bone_Hair035_R

### ABP_Seq_Zanni_Phy / AnimGraphNode_KawaiiPhysicsGroup_12

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r6.5
- Alpha 曲线：None

- [头发] Bone_Hair050_R → Bone_Hair051_R
- [头发] Bone_Hair052_R → Bone_Hair053_R
- [头发] Bone_Hair054_L → Bone_Hair055_L
- [头发] Bone_Hair046_L → Bone_Hair047_L
- [头发] Bone_Hair041_M → Bone_Hair042_M
- [头发] Bone_Hair043_M → Bone_Hair044_M

### ABP_Seq_Zanni_Phy / AnimGraphNode_KawaiiPhysicsGroup_11

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [头发] Bone_Hair084_R → Bone_Hair085_R → Bone_Hair086_R
- [头发] Bone_Hair075_L → Bone_Hair076_L → Bone_Hair077_L
- [飘带(Piao)] Bone_Piao055_R

### ABP_Seq_Zanni_Phy / AnimGraphNode_KawaiiPhysicsGroup_10

- 参数：Damping=0.1 Stiffness=0.03 LimitAngle=15.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao035_M → Bone_Piao036_M → Bone_Piao037_M → Bone_Piao038_M

### ABP_Seq_Zanni_Phy / AnimGraphNode_KawaiiPhysicsGroup_9

- 参数：Damping=0.2 Stiffness=0.12 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao041_L
- [飘带(Piao)] Bone_Piao042_L

### ABP_Seq_Zanni_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=45.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao046_R → Bone_Piao051_R
- [飘带(Piao)] Bone_Piao070_R → Bone_Piao071_R → Bone_Piao072_R → Bone_Piao073_R
- [飘带(Piao)] Bone_Piao053_R
- [飘带(Piao)] Bone_Piao054_R

### ABP_Seq_Zanni_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [飘带(Piao)] Bone_Piao085_M → Bone_Piao086_M → Bone_Piao087_M → Bone_Piao088_M → Bone_Piao089_M → Bone_Piao090_M → Bone_Piao091_M → Bone_Piao092_M → Bone_Piao114_M → Bone_Piao115_M → Bone_Piao116_M

### ABP_Seq_Zanni_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=35.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao013_L → Bone_Piao014_L → Bone_Piao015_L → Bone_Piao024_L → Bone_Piao025_L
- [飘带(Piao)] Bone_Piao013_R → Bone_Piao014_R → Bone_Piao015_R → Bone_Piao024_R → Bone_Piao025_R
- [飘带(Piao)] Bone_Piao017_L → Bone_Piao018_L → Bone_Piao019_L
- [飘带(Piao)] Bone_Piao017_R → Bone_Piao018_R → Bone_Piao019_R

### ABP_Seq_Zanni_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.5 Stiffness=0.15 LimitAngle=15.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [胸饰] Bone_Chest001_L → Bone_Chest002_L
- [胸饰] Bone_Chest001_R → Bone_Chest002_R → Bone_Piao121_R

### ABP_Seq_Zanni_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.15 Stiffness=0.15 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r6.5
- Alpha 曲线：None

- [头发] Bone_Hair058_R → Bone_Hair059_R → Bone_Hair113_R
- [头发] Bone_Hair062_M → Bone_Hair063_M
- [头发] Bone_Hair060_L → Bone_Hair108_L → Bone_Hair109_L
- [头发] Bone_Hair056_L → Bone_Hair057_L
- [头发] Bone_Hair079_R → Bone_Hair080_R
- [头发] Bone_Hair069_L → Bone_Hair070_L → Bone_Hair071_L
- [头发] Bone_Hair065_L → Bone_Hair066_L → Bone_Hair067_L
- [头发] Bone_Hair036_L → Bone_Hair037_L → Bone_Hair038_L

### ABP_Seq_Zanni_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.1 Stiffness=0.03 LimitAngle=30.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao056_R

### ABP_Seq_Zanni_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.3 LimitAngle=35.0 Radius=4.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：None

- [头发] Bone_Hair143_L → Bone_Hair144_L
- [头发] Bone_Hair140_L
- [头发] Bone_Hair149_L → Bone_Hair152_L → Bone_Hair153_L
- [头发] Bone_Hair149_L → Bone_Hair150_L → Bone_Hair151_L
- [头发] Bone_Hair139_L → Bone_Hair140_L
- [头发] Bone_Hair156_L → Bone_Hair157_L → Bone_Hair158_L
- [头发] Bone_Hair161_R → Bone_Hair162_R
- [头发] Bone_Hair165_R → Bone_Hair166_R → Bone_Hair167_R
- [头发] Bone_Hair163_R → Bone_Hair164_R
- [头发] Bone_Hair173_R → Bone_Hair174_R → Bone_Hair175_R
- [头发] Bone_Hair177_R → Bone_Hair178_R
- [头发] Bone_Hair177_R → Bone_Hair178_R
- [头发] Bone_Hair180_R → Bone_Hair181_R
- [头发] Bone_Hair184_R → Bone_Hair185_R
- [头发] Bone_Hair191_R → Bone_Hair192_R
- [头发] Bone_Hair193_R → Bone_Hair194_R → Bone_Hair195_R
- [头发] Bone_Hair136_L → Bone_Hair137_L

### ABP_Seq_Zanni_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.15 Stiffness=0.15 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r6.5
- Alpha 曲线：None

- [头发] Bone_Hair111_R → Bone_Hair112_R
- [头发] Bone_Hair106_L
- [头发] Bone_Hair102_M → Bone_Hair103_M → Bone_Hair104_M
- [头发] Bone_Hair060_L → Bone_Hair108_L → Bone_Hair109_L
- [头发] Bone_Hair065_L → Bone_Hair066_L → Bone_Hair067_L
- [头发] Bone_Hair117_L → Bone_Hair118_L → Bone_Hair119_L
- [头发] Bone_Hair120_L → Bone_Hair121_L
- [头发] Bone_Hair123_L → Bone_Hair124_L → Bone_Hair125_L
- [头发] Bone_Hair127_L → Bone_Hair128_L
- [头发] Bone_Hair133_R → Bone_Hair134_R
- [头发] Bone_Hair130_R → Bone_Hair131_R
- [头发] Bone_Hair114_R → Bone_Hair115_R → Bone_Hair116_R

### ABP_Seq_Zanni_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.1 Stiffness=0.2 LimitAngle=30.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_jianxin

- [飘带(Piao)] Bone_Piao113_R → Bone_Piao114_R → Bone_Piao115_R → Bone_Piao116_R → Bone_Piao117_R → Bone_Piao118_R → Bone_Piao119_R → Bone_Piao120_R

## FemaleXL/Zanni（R2T1ZanniMd20011）

- 骨总数 ?；骨族 ?
- KawaiiPhysics 组 0 个（ABP 变体 0 个），链 0 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 状态：{'skeleton': 'export_missing', 'abp_skipped_no_skeleton': ['ABP_Seq_Zanni_Phy', 'ABP_Seq_Zanni_Seq_Link_Skin01_Phy']}

## FemaleZ/Fuludelisi（R2T1FuludelisiMd10011）

- 骨总数 300；骨族 {'other:Root': 1, 'bip001': 121, 'hair': 64, 'other:Other': 14, 'ribbon': 62, 'weapon_prop': 10, 'chest': 6, 'prop': 1, 'skirt': 20, 'pelvis_extra': 1}
- KawaiiPhysics 组 1 个（ABP 变体 2 个），链 41 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：168 根
- 状态：{'abp_note': 'gender_level_abp', 'chains_source': 'skeleton_heuristic (ABP 无 KawaiiPhysicsGroup 节点)'}

### skeleton_heuristic / heuristic_family_chains

- 参数：Damping=None Stiffness=None LimitAngle=None Radius=None HalfWidth=None Shape=None

- [头发] Bone_Hair001_L → Bone_Hair003_L
- [头发] Bone_Hair001_R → Bone_Hair003_R
- [头发] Bone_Hair011_R → Bone_Hair013_R
- [头发] Bone_Hair021_R → Bone_Hair022_R → Bone_Hair023_R
- [头发] Bone_Hair011_L → Bone_Hair013_L
- [头发] Bone_Hair021_L → Bone_Hair022_L → Bone_Hair023_L
- [头发] Bone_Hair031_L → Bone_Hair032_L → Bone_Hair033_L → Bone_Hair041_L
- [头发] Bone_Hair031_L → Bone_Hair032_L → Bone_Hair033_L → Bone_Hair051_L
- [头发] Bone_Hair031_R → Bone_Hair032_R → Bone_Hair033_R → Bone_Hair041_R
- [头发] Bone_Hair031_R → Bone_Hair032_R → Bone_Hair033_R → Bone_Hair051_R
- [飘带(Piao)] Bone_Piao001_L → Bone_Piao002_L → Bone_Piao017_L
- [飘带(Piao)] Bone_Piao001_L → Bone_Piao013_L
- [飘带(Piao)] Bone_Piao001_R → Bone_Piao002_R → Bone_Piao017_R
- [飘带(Piao)] Bone_Piao001_R → Bone_Piao013_R
- [头发] Bone_Hair061_L → Bone_Hair062_L → Bone_Hair063_L → Bone_Hair064_L → Bone_Hair065_L → Bone_Hair067_L → Bone_Hair068_L → Bone_Hair069_L
- [头发] Bone_Hair061_L → Bone_Hair062_L → Bone_Hair063_L → Bone_Hair064_L → Bone_Hair065_L → Bone_Hair071_L → Bone_Hair072_L → Bone_Hair073_L
- [头发] Bone_Hair081_L → Bone_Hair082_L → Bone_Hair083_L → Bone_Hair084_L → Bone_Hair085_L → Bone_Hair086_L → Bone_Hair087_L
- [头发] Bone_Hair081_L → Bone_Hair082_L → Bone_Hair083_L → Bone_Hair084_L → Bone_Hair095_L → Bone_Hair096_L
- [头发] Bone_Hair081_R → Bone_Hair082_R → Bone_Hair083_R → Bone_Hair084_R → Bone_Hair085_R → Bone_Hair086_R → Bone_Hair087_R
- [头发] Bone_Hair081_R → Bone_Hair082_R → Bone_Hair083_R → Bone_Hair084_R → Bone_Hair095_R → Bone_Hair096_R
- [头发] Bone_Hair061_R → Bone_Hair062_R → Bone_Hair063_R → Bone_Hair064_R → Bone_Hair065_R → Bone_Hair067_R → Bone_Hair068_R → Bone_Hair069_R
- [头发] Bone_Hair061_R → Bone_Hair062_R → Bone_Hair063_R → Bone_Hair064_R → Bone_Hair065_R → Bone_Hair071_R → Bone_Hair072_R → Bone_Hair073_R
- [胸饰] Bone_Chest001_L → Bone_Chest002_L → Bone_Chest003_L
- [胸饰] Bone_Chest001_R → Bone_Chest002_R → Bone_Chest003_R
- [飘带(Piao)] Bone_Piao051_L → Bone_Piao053_L → Bone_Piao054_L → Bone_Piao055_L → Bone_Piao056_L → Bone_Piao057_L → Bone_Piao058_L
- [飘带(Piao)] Bone_Piao051_R → Bone_Piao053_R → Bone_Piao054_R → Bone_Piao055_R → Bone_Piao056_R → Bone_Piao057_R → Bone_Piao058_R
- [飘带(Piao)] Bone_Piao031_L → Bone_Piao033_L → Bone_Piao034_L → Bone_Piao035_L → Bone_Piao036_L → Bone_Piao037_L
- [飘带(Piao)] Bone_Piao031_R → Bone_Piao033_R → Bone_Piao034_R → Bone_Piao035_R → Bone_Piao036_R → Bone_Piao037_R
- [飘带(Piao)] Bone_Piao021_M → Bone_Piao022_M
- [飘带(Piao)] Bone_Piao051_M → Bone_Piao052_M → Bone_Piao053_M → Bone_Piao054_M → Bone_Piao061_L → Bone_Piao062_L → Bone_Piao070_L → Bone_Piao071_L → Bone_Piao072_L
- [飘带(Piao)] Bone_Piao051_M → Bone_Piao052_M → Bone_Piao053_M → Bone_Piao054_M → Bone_Piao061_L → Bone_Piao062_L → Bone_Piao080_L → Bone_Piao081_L → Bone_Piao082_L → Bone_Piao083_L
- [飘带(Piao)] Bone_Piao051_M → Bone_Piao052_M → Bone_Piao053_M → Bone_Piao054_M → Bone_Piao061_R → Bone_Piao062_R → Bone_Piao070_R → Bone_Piao071_R → Bone_Piao072_R
- [飘带(Piao)] Bone_Piao051_M → Bone_Piao052_M → Bone_Piao053_M → Bone_Piao054_M → Bone_Piao061_R → Bone_Piao062_R → Bone_Piao080_R → Bone_Piao081_R → Bone_Piao082_R → Bone_Piao083_R
- [飘带(Piao)] Bone_Piao051_M → Bone_Piao091_L → Bone_Piao092_L
- [飘带(Piao)] Bone_Piao051_M → Bone_Piao091_R → Bone_Piao092_R
- [裙摆] Bone_Skirt001_L → Bone_Skirt002_L → Bone_Skirt003_L
- [裙摆] Bone_Skirt001_R → Bone_Skirt002_R → Bone_Skirt003_R
- [裙摆] Bone_Skirt011_L → Bone_Skirt012_L → Bone_Skirt013_L
- [裙摆] Bone_Skirt011_R → Bone_Skirt012_R → Bone_Skirt013_R
- [裙摆] Bone_Skirt021_L → Bone_Skirt022_L → Bone_Skirt023_L → Bone_Skirt024_L
- [裙摆] Bone_Skirt021_R → Bone_Skirt022_R → Bone_Skirt023_R → Bone_Skirt024_R

## FemaleZ2/AimisiGD（R2T1AimisiGDMd10011）

- 骨总数 319；骨族 {'other:Root': 1, 'bip001': 60, 'other:ADIhead': 3, 'hair': 61, 'other:Winghaed': 30, 'other:ADIhand': 4, 'weapon_prop': 14, 'other:ADIclavicle': 6, 'other:ADIarm': 6, 'other:ADIelbow': 2, 'other:ADIshoulder': 14, 'other:Wingcalf': 20, 'other:ADIspine': 14, 'other:ADIneck': 14, 'other:ADIwingSpine': 4, 'other:ADIwingSusp': 12, 'other:ADIwing': 24, 'other:ADIheel': 2, 'other:ADIleg': 6, 'other:ADIcalf': 2, 'other:ADIankle': 4, 'other:ADIkneeB': 8, 'other:ADIkneeA': 2, 'other:ADIkneeC': 2, 'other:ADIthigh': 2, 'other:ADIpelvis': 2}
- KawaiiPhysics 组 1 个（ABP 变体 2 个），链 14 条
- KawaiiCurve：hair
- 未入组二级骨：245 根
- 状态：{'chains_source': 'skeleton_heuristic (ABP 无 KawaiiPhysicsGroup 节点)'}

### skeleton_heuristic / heuristic_family_chains

- 参数：Damping=None Stiffness=None LimitAngle=None Radius=None HalfWidth=None Shape=None

- [头发] Bone_Hair001_L → Bone_Hair002_L
- [头发] Bone_Hair001_R → Bone_Hair002_R
- [头发] Bone_Hair011_L → Bone_Hair012_L → Bone_Hair013_L
- [头发] Bone_Hair011_R → Bone_Hair012_R → Bone_Hair013_R
- [头发] Bone_Hair021_L → Bone_Hair022_L → Bone_Hair023_L → Bone_Hair024_L
- [头发] Bone_Hair021_R → Bone_Hair022_R → Bone_Hair023_R → Bone_Hair024_R
- [头发] Bone_Hair031_L → Bone_Hair032_L → Bone_Hair033_L → Bone_Hair034_L
- [头发] Bone_Hair031_R → Bone_Hair032_R → Bone_Hair033_R → Bone_Hair034_R
- [头发] Bone_Hair041_L → Bone_Hair042_L → Bone_Hair043_L → Bone_Hair044_L → Bone_Hair045_L → Bone_Hair046_L → Bone_Hair047_L
- [头发] Bone_Hair041_R → Bone_Hair042_R → Bone_Hair043_R → Bone_Hair044_R → Bone_Hair045_R → Bone_Hair046_R → Bone_Hair047_R
- [头发] Bone_Hair085 → Bone_Hair050 → Bone_Hair051 → Bone_Hair052 → Bone_Hair053 → Bone_Hair054 → Bone_Hair071 → Bone_Hair072 → Bone_Hair073 → Bone_Hair074
- [头发] Bone_Hair085 → Bone_Hair050 → Bone_Hair051 → Bone_Hair052 → Bone_Hair053 → Bone_Hair054 → Bone_Hair071 → Bone_Hair056 → Bone_Hair057 → Bone_Hair058
- [头发] Bone_Hair085 → Bone_Hair050 → Bone_Hair051 → Bone_Hair052 → Bone_Hair053 → Bone_Hair054 → Bone_Hair071 → Bone_Hair081 → Bone_Hair082 → Bone_Hair083
- [头发] Bone_Hair085 → Bone_Hair050 → Bone_Hair051 → Bone_Hair052 → Bone_Hair053 → Bone_Hair054 → Bone_Hair061 → Bone_Hair062 → Bone_Hair063 → Bone_Hair064 → Bone_Hair065

## MaleM/Jingran（R2T1JingranMd10011）

- 骨总数 289；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 52, 'ribbon': 106, 'facial': 29, 'hand_twist': 2, 'prop': 1}
- KawaiiPhysics 组 11 个（ABP 变体 1 个），链 40 条
- KawaiiCurve：hair, skirt
- 未入组二级骨：68 根

### ABP_Seq_Jingran_Phy / AnimGraphNode_KawaiiPhysicsGroup_10

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=50.0 Radius=1.2 HalfWidth=5.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S03

- [飘带(Piao)] Bone_Piao061_L → Bone_Piao062_L → Bone_Piao063_L
- [飘带(Piao)] Bone_Piao061_R → Bone_Piao062_R → Bone_Piao063_R
- [飘带(Piao)] Bone_Piao061_R → Bone_Piao065_R → Bone_Piao066_R → Bone_Piao067_R → Bone_Piao206_R
- [飘带(Piao)] Bone_Piao066_R → Bone_Piao067_R → Bone_Piao206_R
- [飘带(Piao)] Bone_Piao211_R → Bone_Piao212_R → Bone_Piao213_R → Bone_Piao214_R
- [飘带(Piao)] Bone_Piao191_R → Bone_Piao192_R → Bone_Piao193_R
- [飘带(Piao)] Bone_Piao191_L → Bone_Piao192_L → Bone_Piao193_L
- [飘带(Piao)] Bone_Piao185_L → Bone_Piao186_L

### ABP_Seq_Jingran_Phy / AnimGraphNode_KawaiiPhysicsGroup_9

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S04

- [飘带(Piao)] Bone_Piao091_M → Bone_Piao092_M → Bone_Piao093_M → Bone_Piao094_M
- [飘带(Piao)] Bone_Piao081_R → Bone_Piao082_R → Bone_Piao083_R → Bone_Piao084_R → Bone_Piao085_R
- [飘带(Piao)] Bone_Piao081_L → Bone_Piao083_L → Bone_Piao084_L → Bone_Piao085_L → Bone_Piao086_L
- [飘带(Piao)] Bone_Piao081_L → Bone_Piao088_L → Bone_Piao089_L → Bone_Piao090_L → Bone_Piao091_L
- [飘带(Piao)] Bone_Piao071_R → Bone_Piao072_R → Bone_Piao073_R → Bone_Piao074_R → Bone_Piao205_R
- [飘带(Piao)] Bone_Piao071_L → Bone_Piao072_L → Bone_Piao073_L → Bone_Piao074_L

### ABP_Seq_Jingran_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.2 Stiffness=0.15 LimitAngle=50.0 Radius=1.5 HalfWidth=3.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.286243
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S5

- [头发] Bone_Hair002_M → Bone_Hair003_M
- [头发] Bone_Hair002_R
- [头发] Bone_Hair022_L → Bone_Hair023_L

### ABP_Seq_Jingran_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.23 Stiffness=0.15 LimitAngle=50.0 Radius=2.0 HalfWidth=3.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.595159
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S5

- [头发] Bone_Hair002_L
- [头发] Bone_Hair022_R → Bone_Hair023_R

### ABP_Seq_Jingran_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.15 LimitAngle=50.0 Radius=2.5 HalfWidth=3.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S01

- [头发] Bone_Hair062_M → Bone_Hair063_M → Bone_Hair064_M → Bone_Hair065_M → Bone_Hair066_M

### ABP_Seq_Jingran_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.2 Stiffness=0.04 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S02

- [飘带(Piao)] Bone_Piao102_L → Bone_Piao103_L → Bone_Piao104_L → Bone_Piao105_L → Bone_Piao106_L

### ABP_Seq_Jingran_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.15 Stiffness=0.05 LimitAngle=15.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.3612156
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S07

- [飘带(Piao)] Bone_Piao021_R
- [飘带(Piao)] Bone_Piao031_L
- [飘带(Piao)] Bone_Piao230_M

### ABP_Seq_Jingran_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=15.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r6.8586164
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S07

- [飘带(Piao)] Bone_Piao021_L
- [飘带(Piao)] Bone_Piao025_R
- [飘带(Piao)] Bone_Piao232_R
- [飘带(Piao)] Bone_Piao234_L

### ABP_Seq_Jingran_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.15 Stiffness=0.05 LimitAngle=15.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.361216
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S07

- [飘带(Piao)] Bone_Piao025_L
- [飘带(Piao)] Bone_Piao031_R
- [飘带(Piao)] Bone_Piao231_L
- [飘带(Piao)] Bone_Piao233_R

### ABP_Seq_Jingran_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=0.8 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 5 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S6

- [飘带(Piao)] Bone_Piao042_M → Bone_Piao043_M
- [飘带(Piao)] Bone_Piao051_R → Bone_Piao207_R
- [飘带(Piao)] Bone_Piao195_R → Bone_Piao197_R → Bone_Piao198_R
- [飘带(Piao)] Bone_Piao195_R → Bone_Piao200_R
- [飘带(Piao)] Bone_Piao118_R → Bone_Piao119_R → Bone_Piao120_R
- [飘带(Piao)] Bone_Piao132_R → Bone_Piao133_R

### ABP_Seq_Jingran_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=0.8 HalfWidth=2.5 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S6

- [飘带(Piao)] Bone_Piao051_L
- [飘带(Piao)] Bone_Piao045_R

## MaleM/Nanzhu（R2T1PlayerMaleMd10011）

- 骨总数 171；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 45, 'other:Piao_LF_waiTao01': 1, 'other:Piao_LF_waiTao02': 1, 'other:Piao_RF_waiTao01': 1, 'other:Piao_RF_waiTao02': 1, 'other:Piao_waiTao01': 1, 'other:Piao_waiTao02': 1, 'other:Piao_LO_waiTao01': 1, 'other:Piao_LO_waiTao02': 1, 'other:Piao_RO_waiTao01': 1, 'other:Piao_RO_waiTao02': 1, 'other:Piao_M_mingPai01': 1, 'other:Piao_M_mingPai02': 1, 'other:Piao_MB_waiTao01': 1, 'other:Piao_MB_waiTao02': 1, 'other:Piao_L_yiLing01': 1, 'other:Piao_L_yiLing02': 1, 'other:Piao_R_yiLing01': 1, 'other:Piao_R_yiLing02': 1, 'prop': 1, 'other:Piao_L_bao01': 1, 'other:Piao_B_bao01': 1, 'other:Piao_L_guaShi01': 1, 'other:Piao_L_guaShi02': 1, 'other:Piao_L_guaShi03': 1, 'other:Piao_butt01': 1, 'other:Piao_butt02': 1, 'other:Piao_butt03': 1}
- KawaiiPhysics 组 12 个（ABP 变体 4 个），链 56 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：7 根

### ABP_Seq_Nanzhu_Md20011_Link_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_R_yiLing01] Piao_R_yiLing01 → Piao_R_yiLing02
- [other:Piao_L_yiLing01] Piao_L_yiLing01 → Piao_L_yiLing02
- [other:Piao_M_mingPai01] Piao_M_mingPai01 → Piao_M_mingPai02

### ABP_Seq_Nanzhu_Md20011_Link_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_RF_waiTao01] Piao_RF_waiTao01 → Piao_RF_waiTao02 → Piao_waiTao01 → Piao_waiTao02
- [other:Piao_RO_waiTao01] Piao_RO_waiTao01 → Piao_RO_waiTao02
- [other:Piao_LF_waiTao01] Piao_LF_waiTao01 → Piao_LF_waiTao02
- [other:Piao_LO_waiTao01] Piao_LO_waiTao01 → Piao_LO_waiTao02
- [other:Piao_MB_waiTao01] Piao_MB_waiTao01 → Piao_MB_waiTao02

### ABP_Seq_Nanzhu_Md20011_Link_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.2 Stiffness=0.03 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_L_guaShi01] Piao_L_guaShi01 → Piao_L_guaShi02 → Piao_L_guaShi03
- [other:Piao_butt02] Piao_butt02 → Piao_butt03

### ABP_Seq_Nanzhu_Md20011_Link_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.03 LimitAngle=25.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_L_bao01] Piao_L_bao01
- [other:Piao_B_bao01] Piao_B_bao01

### ABP_Seq_Nanzhu_Md20011_Link_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.06 LimitAngle=40.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 7 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [头发] Bone_Hair024_M → Bone_Hair025_M → Bone_Hair026_L → Bone_Hair027_L → Bone_Hair028_L → Bone_Hair029_L → Bone_Hair030_L → Bone_Hair031_L
- [头发] Bone_Hair024_M → Bone_Hair025_M → Bone_Hair037_R → Bone_Hair038_R → Bone_Hair039_R → Bone_Hair040_R → Bone_Hair041_R

### ABP_Seq_Nanzhu_Md20011_Link_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.3 Stiffness=0.04 LimitAngle=20.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r6.75
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [头发] Bone_Hair022_M
- [头发] Bone_Hair004_R
- [头发] Bone_Hair047_R → Bone_Hair048_R → Bone_Hair049_R
- [头发] Bone_Hair054_R → Bone_Hair055_R
- [头发] Bone_Hair069_R → Bone_Hair070_R
- [头发] Bone_Hair057_R → Bone_Hair058_R
- [头发] Bone_Hair060_M → Bone_Hair061_M
- [头发] Bone_Hair063_L → Bone_Hair064_L
- [头发] Bone_Hair066_L → Bone_Hair067_L
- [头发] Bone_Hair014_L → Bone_Hair015_L
- [头发] Bone_Hair018_L → Bone_Hair019_L
- [头发] Bone_Hair006_M → Bone_Hair007_M
- [头发] Bone_Hair051_M → Bone_Hair052_M
- [头发] Bone_Hair010_L → Bone_Hair011_L

### ABP_Seq_Nanzhu_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_R_yiLing01] Piao_R_yiLing01 → Piao_R_yiLing02
- [other:Piao_L_yiLing01] Piao_L_yiLing01 → Piao_L_yiLing02
- [other:Piao_M_mingPai01] Piao_M_mingPai01 → Piao_M_mingPai02

### ABP_Seq_Nanzhu_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_RF_waiTao01] Piao_RF_waiTao01 → Piao_RF_waiTao02 → Piao_waiTao01 → Piao_waiTao02
- [other:Piao_RO_waiTao01] Piao_RO_waiTao01 → Piao_RO_waiTao02
- [other:Piao_LF_waiTao01] Piao_LF_waiTao01 → Piao_LF_waiTao02
- [other:Piao_LO_waiTao01] Piao_LO_waiTao01 → Piao_LO_waiTao02
- [other:Piao_MB_waiTao01] Piao_MB_waiTao01 → Piao_MB_waiTao02

### ABP_Seq_Nanzhu_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.2 Stiffness=0.03 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_L_guaShi01] Piao_L_guaShi01 → Piao_L_guaShi02 → Piao_L_guaShi03
- [other:Piao_butt02] Piao_butt02 → Piao_butt03

### ABP_Seq_Nanzhu_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.03 LimitAngle=25.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_L_bao01] Piao_L_bao01
- [other:Piao_B_bao01] Piao_B_bao01

### ABP_Seq_Nanzhu_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.06 LimitAngle=40.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 7 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [头发] Bone_Hair024_M → Bone_Hair025_M → Bone_Hair026_L → Bone_Hair027_L → Bone_Hair028_L → Bone_Hair029_L → Bone_Hair030_L → Bone_Hair031_L
- [头发] Bone_Hair024_M → Bone_Hair025_M → Bone_Hair037_R → Bone_Hair038_R → Bone_Hair039_R → Bone_Hair040_R → Bone_Hair041_R

### ABP_Seq_Nanzhu_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.3 Stiffness=0.04 LimitAngle=20.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r6.75
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [头发] Bone_Hair022_M
- [头发] Bone_Hair004_R
- [头发] Bone_Hair048_R → Bone_Hair049_R
- [头发] Bone_Hair054_R → Bone_Hair055_R
- [头发] Bone_Hair069_R → Bone_Hair070_R
- [头发] Bone_Hair057_R → Bone_Hair058_R
- [头发] Bone_Hair060_M → Bone_Hair061_M
- [头发] Bone_Hair063_L → Bone_Hair064_L
- [头发] Bone_Hair066_L → Bone_Hair067_L
- [头发] Bone_Hair014_L → Bone_Hair015_L
- [头发] Bone_Hair018_L → Bone_Hair019_L
- [头发] Bone_Hair006_M → Bone_Hair007_M
- [头发] Bone_Hair051_M → Bone_Hair052_M
- [头发] Bone_Hair010_L → Bone_Hair011_L

## MaleM/Nanzhu（R2T1PlayerMaleMd20011）

- 骨总数 ?；骨族 ?
- KawaiiPhysics 组 0 个（ABP 变体 0 个），链 0 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 状态：{'skeleton': 'export_missing', 'abp_skipped_no_skeleton': ['ABP_Seq_Nanzhu_Md20011_Link_Phy', 'ABP_Seq_Nanzhu_Md20011_Phy', 'ABP_Seq_Nanzhu_Phy', 'ABP_Seq_Nanzhu_NPC_Phy']}

## MaleM/TestModel（TestModel01）

- 骨总数 171；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 45, 'other:Piao_LF_waiTao01': 1, 'other:Piao_LF_waiTao02': 1, 'other:Piao_RF_waiTao01': 1, 'other:Piao_RF_waiTao02': 1, 'other:Piao_waiTao01': 1, 'other:Piao_waiTao02': 1, 'other:Piao_LO_waiTao01': 1, 'other:Piao_LO_waiTao02': 1, 'other:Piao_RO_waiTao01': 1, 'other:Piao_RO_waiTao02': 1, 'other:Piao_M_mingPai01': 1, 'other:Piao_M_mingPai02': 1, 'other:Piao_MB_waiTao01': 1, 'other:Piao_MB_waiTao02': 1, 'other:Piao_L_yiLing01': 1, 'other:Piao_L_yiLing02': 1, 'other:Piao_R_yiLing01': 1, 'other:Piao_R_yiLing02': 1, 'prop': 1, 'other:Piao_L_bao01': 1, 'other:Piao_B_bao01': 1, 'other:Piao_L_guaShi01': 1, 'other:Piao_L_guaShi02': 1, 'other:Piao_L_guaShi03': 1, 'other:Piao_butt01': 1, 'other:Piao_butt02': 1, 'other:Piao_butt03': 1}
- KawaiiPhysics 组 1 个（ABP 变体 1 个），链 16 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：72 根
- 状态：{'chains_source': 'skeleton_heuristic (ABP 无 KawaiiPhysicsGroup 节点)'}

### skeleton_heuristic / heuristic_family_chains

- 参数：Damping=None Stiffness=None LimitAngle=None Radius=None HalfWidth=None Shape=None

- [头发] Bone_Hair024_M → Bone_Hair025_M → Bone_Hair026_L → Bone_Hair027_L → Bone_Hair028_L → Bone_Hair029_L → Bone_Hair030_L → Bone_Hair031_L
- [头发] Bone_Hair024_M → Bone_Hair025_M → Bone_Hair037_R → Bone_Hair038_R → Bone_Hair039_R → Bone_Hair040_R → Bone_Hair041_R
- [头发] Bone_Hair021_M → Bone_Hair022_M
- [头发] Bone_Hair009_L → Bone_Hair010_L → Bone_Hair011_L
- [头发] Bone_Hair047_R → Bone_Hair048_R → Bone_Hair049_R
- [头发] Bone_Hair013_L → Bone_Hair014_L → Bone_Hair015_L
- [头发] Bone_Hair051_M → Bone_Hair052_M
- [头发] Bone_Hair017_L → Bone_Hair018_L → Bone_Hair019_L
- [头发] Bone_Hair054_R → Bone_Hair055_R
- [头发] Bone_Hair057_R → Bone_Hair058_R
- [头发] Bone_Hair006_M → Bone_Hair007_M
- [头发] Bone_Hair003_R → Bone_Hair004_R
- [头发] Bone_Hair060_M → Bone_Hair061_M
- [头发] Bone_Hair063_L → Bone_Hair064_L
- [头发] Bone_Hair066_L → Bone_Hair067_L
- [头发] Bone_Hair069_R → Bone_Hair070_R

## MaleM/TestModel（TestModel02）

- 骨总数 171；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 45, 'other:Piao_LF_waiTao01': 1, 'other:Piao_LF_waiTao02': 1, 'other:Piao_RF_waiTao01': 1, 'other:Piao_RF_waiTao02': 1, 'other:Piao_waiTao01': 1, 'other:Piao_waiTao02': 1, 'other:Piao_LO_waiTao01': 1, 'other:Piao_LO_waiTao02': 1, 'other:Piao_RO_waiTao01': 1, 'other:Piao_RO_waiTao02': 1, 'other:Piao_M_mingPai01': 1, 'other:Piao_M_mingPai02': 1, 'other:Piao_MB_waiTao01': 1, 'other:Piao_MB_waiTao02': 1, 'other:Piao_L_yiLing01': 1, 'other:Piao_L_yiLing02': 1, 'other:Piao_R_yiLing01': 1, 'other:Piao_R_yiLing02': 1, 'prop': 1, 'other:Piao_L_bao01': 1, 'other:Piao_B_bao01': 1, 'other:Piao_L_guaShi01': 1, 'other:Piao_L_guaShi02': 1, 'other:Piao_L_guaShi03': 1, 'other:Piao_butt01': 1, 'other:Piao_butt02': 1, 'other:Piao_butt03': 1}
- KawaiiPhysics 组 1 个（ABP 变体 1 个），链 16 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：72 根
- 状态：{'chains_source': 'skeleton_heuristic (ABP 无 KawaiiPhysicsGroup 节点)'}

### skeleton_heuristic / heuristic_family_chains

- 参数：Damping=None Stiffness=None LimitAngle=None Radius=None HalfWidth=None Shape=None

- [头发] Bone_Hair024_M → Bone_Hair025_M → Bone_Hair026_L → Bone_Hair027_L → Bone_Hair028_L → Bone_Hair029_L → Bone_Hair030_L → Bone_Hair031_L
- [头发] Bone_Hair024_M → Bone_Hair025_M → Bone_Hair037_R → Bone_Hair038_R → Bone_Hair039_R → Bone_Hair040_R → Bone_Hair041_R
- [头发] Bone_Hair021_M → Bone_Hair022_M
- [头发] Bone_Hair009_L → Bone_Hair010_L → Bone_Hair011_L
- [头发] Bone_Hair047_R → Bone_Hair048_R → Bone_Hair049_R
- [头发] Bone_Hair013_L → Bone_Hair014_L → Bone_Hair015_L
- [头发] Bone_Hair051_M → Bone_Hair052_M
- [头发] Bone_Hair017_L → Bone_Hair018_L → Bone_Hair019_L
- [头发] Bone_Hair054_R → Bone_Hair055_R
- [头发] Bone_Hair057_R → Bone_Hair058_R
- [头发] Bone_Hair006_M → Bone_Hair007_M
- [头发] Bone_Hair003_R → Bone_Hair004_R
- [头发] Bone_Hair060_M → Bone_Hair061_M
- [头发] Bone_Hair063_L → Bone_Hair064_L
- [头发] Bone_Hair066_L → Bone_Hair067_L
- [头发] Bone_Hair069_R → Bone_Hair070_R

## MaleM/Xiangliyao（R2T1XiangLiYaoMd10011）

- 骨总数 224；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 29, 'other:Other': 24, 'ribbon': 31, 'prop': 1, 'skirt': 40}
- KawaiiPhysics 组 1 个（ABP 变体 2 个），链 34 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：125 根
- 状态：{'chains_source': 'skeleton_heuristic (ABP 无 KawaiiPhysicsGroup 节点)'}

### skeleton_heuristic / heuristic_family_chains

- 参数：Damping=None Stiffness=None LimitAngle=None Radius=None HalfWidth=None Shape=None

- [头发] Bone_Hair031
- [头发] Bone_Hair022_R → Bone_Hair023_R
- [头发] Bone_Hair012_R → Bone_Hair013_R
- [头发] Bone_Hair002_R → Bone_Hair003_R
- [头发] Bone_Hair002_L → Bone_Hair003_L
- [头发] Bone_Hair012_L → Bone_Hair013_L
- [头发] Bone_Hair022_L → Bone_Hair023_L
- [头发] Bone_Hair061_L
- [头发] Bone_Hair059_L
- [头发] Bone_Hair061_R
- [头发] Bone_Hair059_R
- [头发] Bone_Hair053_L → Bone_Hair054_L
- [头发] Bone_Hair053_R → Bone_Hair054_R
- [头发] Bone_Hair056_L → Bone_Hair057_L
- [头发] Bone_Hair050_L
- [头发] Bone_Hair050_R
- [头发] Bone_Hair056_R → Bone_Hair057_R
- [头发] Bone_Hair062_L
- [头发] Bone_Hair063_R
- [飘带(Piao)] Bone_Piao031_R → Bone_Piao032_R → Bone_Piao033_R → Bone_Piao034_R → Bone_Piao035_R
- [飘带(Piao)] Bone_Piao021_L → Bone_Piao022_L
- [飘带(Piao)] Bone_Piao001_L → Bone_Piao002_L → Bone_Piao003_L → Bone_Piao004_L
- [飘带(Piao)] Bone_Piao021_R → Bone_Piao022_R → Bone_Piao023_R
- [飘带(Piao)] Bone_Piao041_M → Bone_Piao041_L → Bone_Piao042_L → Bone_Piao043_L → Bone_Piao044_L → Bone_Piao045_L → Bone_Piao046_L
- [飘带(Piao)] Bone_Piao041_M → Bone_Piao041_R → Bone_Piao042_R → Bone_Piao043_R → Bone_Piao044_R → Bone_Piao045_R → Bone_Piao046_R
- [裙摆] Bone_Skirt011_L → Bone_Skirt012_L → Bone_Skirt013_L → Bone_Skirt014_L → Bone_Skirt015_L → Bone_Skirt016_L → Bone_Skirt017_L
- [裙摆] Bone_Skirt011_R → Bone_Skirt012_R → Bone_Skirt013_R → Bone_Skirt014_R → Bone_Skirt015_R → Bone_Skirt016_R → Bone_Skirt017_R
- [裙摆] Bone_Skirt021_L → Bone_Skirt022_L → Bone_Skirt023_L → Bone_Skirt024_L → Bone_Skirt025_L → Bone_Skirt026_L → Bone_Skirt027_L
- [裙摆] Bone_Skirt021_R → Bone_Skirt022_R → Bone_Skirt023_R → Bone_Skirt024_R → Bone_Skirt025_R → Bone_Skirt026_R → Bone_Skirt027_R
- [裙摆] Bone_Skirt030_L → Bone_Skirt031_L → Bone_Skirt032_L
- [裙摆] Bone_Skirt030_R → Bone_Skirt031_R → Bone_Skirt032_R
- [裙摆] Bone_Skirt001_L → Bone_Skirt002_L → Bone_Skirt003_L
- [裙摆] Bone_Skirt001_R → Bone_Skirt002_R → Bone_Skirt003_R
- [飘带(Piao)] Bone_Piao003_R → Bone_Piao004_R → Bone_Piao005_R → Bone_Piao006_R

## MaleS/Lingyang（R2T1LingyangMd10011）

- 骨总数 197；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 32, 'other:Other': 4, 'ribbon': 54, 'prop': 1, 'other:Tail': 7}
- KawaiiPhysics 组 12 个（ABP 变体 1 个），链 36 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：10 根

### ABP_Seq_Lingyang_Phy / AnimGraphNode_KawaiiPhysicsGroup_11

- 参数：Damping=0.01 Stiffness=0.05 LimitAngle=45.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r8.889595
- Alpha 曲线：None

- [头发] Bone_Hair012_R → Bone_Hair013_R
- [头发] Bone_Hair022_R → Bone_Hair023_R
- [头发] Bone_Hair012_L → Bone_Hair013_L
- [头发] Bone_Hair022_L → Bone_Hair023_L

### ABP_Seq_Lingyang_Phy / AnimGraphNode_KawaiiPhysicsGroup_10

- 参数：Damping=0.1 Stiffness=0.08 LimitAngle=45.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 8 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [头发] Bone_Hair041 → Bone_Hair042 → Bone_Hair043 → Bone_Hair044 → Bone_Hair045

### ABP_Seq_Lingyang_Phy / AnimGraphNode_KawaiiPhysicsGroup_9

- 参数：Damping=0.1 Stiffness=0.05 LimitAngle=30.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [头发] Bone_Hair051 → Bone_Hair052
- [头发] Bone_Hair061_R
- [头发] Bone_Hair061_L

### ABP_Seq_Lingyang_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.2 Stiffness=0.04 LimitAngle=35.0 Radius=1.0 HalfWidth=2.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [飘带(Piao)] Bone_Piao061_R → Bone_Piao062_R → Bone_Piao063_R
- [飘带(Piao)] Bone_Piao061_L → Bone_Piao062_L → Bone_Piao063_L

### ABP_Seq_Lingyang_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.1 Stiffness=0.05 LimitAngle=25.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao001
- [飘带(Piao)] Bone_Piao051_L
- [飘带(Piao)] Bone_Piao051_R
- [飘带(Piao)] Bone_Piao052_R

### ABP_Seq_Lingyang_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.3 Stiffness=0.3 LimitAngle=50.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：None

- [other:Tail] Bone_Tail003 → Bone_Tail004 → Bone_Tail005 → Bone_Tail006 → Bone_Tail007 → Bone_Tail008

### ABP_Seq_Lingyang_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.005 Stiffness=0.05 LimitAngle=35.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao101_R → Bone_Piao102_R → Bone_Piao103_R
- [飘带(Piao)] Bone_Piao151_R → Bone_Piao152_R → Bone_Piao153_R
- [飘带(Piao)] Bone_Piao141_R → Bone_Piao142_R → Bone_Piao143_R
- [飘带(Piao)] Bone_Piao131_R → Bone_Piao132_R → Bone_Piao133_R
- [飘带(Piao)] Bone_Piao121_R → Bone_Piao122_R → Bone_Piao123_R
- [飘带(Piao)] Bone_Piao111_R → Bone_Piao112_R → Bone_Piao113_R

### ABP_Seq_Lingyang_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.005 Stiffness=0.05 LimitAngle=35.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao101_L → Bone_Piao102_L → Bone_Piao104_L
- [飘带(Piao)] Bone_Piao151_L → Bone_Piao152_L → Bone_Piao154_L
- [飘带(Piao)] Bone_Piao141_L → Bone_Piao142_L → Bone_Piao144_L
- [飘带(Piao)] Bone_Piao131_L → Bone_Piao132_L → Bone_Piao134_L
- [飘带(Piao)] Bone_Piao121_L → Bone_Piao122_L → Bone_Piao124_L
- [飘带(Piao)] Bone_Piao111_L → Bone_Piao112_L → Bone_Piao114_L

### ABP_Seq_Lingyang_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.3 Stiffness=0.2 LimitAngle=45.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao034_R → Bone_Piao035_R → Bone_Piao036_R → Bone_Piao037_R

### ABP_Seq_Lingyang_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.02 Stiffness=0.08 LimitAngle=35.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r8.889595
- Alpha 曲线：None

- [头发] Bone_Hair001_R → Bone_Hair002_R → Bone_Hair003_R
- [头发] Bone_Hair001_L → Bone_Hair005_L → Bone_Hair006_L
- [头发] Bone_Hair001_L → Bone_Hair002_L → Bone_Hair003_L

### ABP_Seq_Lingyang_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.015 Stiffness=0.05 LimitAngle=45.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r8.889595
- Alpha 曲线：None

- [头发] Bone_Hair031 → Bone_Hair032 → Bone_Hair033
- [other:Other] Bone_Other002_R
- [other:Other] Bone_Other002_L

### ABP_Seq_Lingyang_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=25.0 Radius=1.5 HalfWidth=1.5 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao161_R
- [飘带(Piao)] Bone_Piao161_L

## MaleS/TestModel（TestModel01）

- 骨总数 197；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 32, 'other:Other': 4, 'ribbon': 54, 'prop': 1, 'other:Tail': 7}
- KawaiiPhysics 组 1 个（ABP 变体 1 个），链 33 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：98 根
- 状态：{'chains_source': 'skeleton_heuristic (ABP 无 KawaiiPhysicsGroup 节点)'}

### skeleton_heuristic / heuristic_family_chains

- 参数：Damping=None Stiffness=None LimitAngle=None Radius=None HalfWidth=None Shape=None

- [头发] Bone_Hair001_R → Bone_Hair002_R → Bone_Hair003_R
- [头发] Bone_Hair001_L → Bone_Hair005_L → Bone_Hair006_L
- [头发] Bone_Hair001_L → Bone_Hair002_L → Bone_Hair003_L
- [头发] Bone_Hair011_L → Bone_Hair012_L → Bone_Hair013_L
- [头发] Bone_Hair011_R → Bone_Hair012_R → Bone_Hair013_R
- [头发] Bone_Hair021_L → Bone_Hair022_L → Bone_Hair023_L
- [头发] Bone_Hair021_R → Bone_Hair022_R → Bone_Hair023_R
- [头发] Bone_Hair031 → Bone_Hair032 → Bone_Hair033
- [头发] Bone_Hair041 → Bone_Hair042 → Bone_Hair043 → Bone_Hair044 → Bone_Hair045
- [头发] Bone_Hair051 → Bone_Hair052
- [头发] Bone_Hair061_L
- [头发] Bone_Hair061_R
- [飘带(Piao)] Bone_Piao001
- [飘带(Piao)] Bone_Piao101_L → Bone_Piao102_L → Bone_Piao104_L
- [飘带(Piao)] Bone_Piao111_L → Bone_Piao112_L → Bone_Piao114_L
- [飘带(Piao)] Bone_Piao121_L → Bone_Piao122_L → Bone_Piao124_L
- [飘带(Piao)] Bone_Piao131_L → Bone_Piao132_L → Bone_Piao134_L
- [飘带(Piao)] Bone_Piao141_L → Bone_Piao142_L → Bone_Piao144_L
- [飘带(Piao)] Bone_Piao151_L → Bone_Piao152_L → Bone_Piao154_L
- [飘带(Piao)] Bone_Piao101_R → Bone_Piao102_R → Bone_Piao103_R
- [飘带(Piao)] Bone_Piao111_R → Bone_Piao112_R → Bone_Piao113_R
- [飘带(Piao)] Bone_Piao121_R → Bone_Piao122_R → Bone_Piao123_R
- [飘带(Piao)] Bone_Piao131_R → Bone_Piao132_R → Bone_Piao133_R
- [飘带(Piao)] Bone_Piao141_R → Bone_Piao142_R → Bone_Piao143_R
- [飘带(Piao)] Bone_Piao151_R → Bone_Piao152_R → Bone_Piao153_R
- [飘带(Piao)] Bone_Piao031_R → Bone_Piao052_R
- [飘带(Piao)] Bone_Piao031_R → Bone_Piao051_R
- [飘带(Piao)] Bone_Piao031_R → Bone_Piao033_R → Bone_Piao034_R → Bone_Piao035_R → Bone_Piao036_R → Bone_Piao037_R
- [飘带(Piao)] Bone_Piao061_R → Bone_Piao062_R → Bone_Piao063_R
- [飘带(Piao)] Bone_Piao061_L → Bone_Piao062_L → Bone_Piao063_L
- [飘带(Piao)] Bone_Piao051_L
- [飘带(Piao)] Bone_Piao161_L
- [飘带(Piao)] Bone_Piao161_R

## MaleXL/Baer（R2T1BaerMd10011）

- 骨总数 200；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 58, 'ribbon': 9, 'other:Piao_L_yanJing01': 1, 'other:Piao_mingPai01': 1, 'other:Piao_L_xiuZiB01': 1, 'other:Piao_L_xiuZiB02': 1, 'other:Piao_L_xiuZiA01': 1, 'other:Piao_L_xiuZiA02': 1, 'other:Piao_L_01': 1, 'other:Piao_L_02': 1, 'other:Piao_L_03': 1, 'other:Piao_R_xiuZiB01': 1, 'other:Piao_R_xiuZiB02': 1, 'other:Piao_R_xiuZiA01': 1, 'other:Piao_R_xiuZiA02': 1, 'other:Piao_R_01': 1, 'other:Piao_R_02': 1, 'other:Piao_R_03': 1, 'prop': 1, 'other:Piao_yaoBao01': 1, 'other:Piao_yaoBao02': 1, 'other:Piao_yaoBao03': 1, 'other:Piao_shiji01': 1, 'other:Skirt_L_01': 1, 'other:Skirt_L_02': 1, 'other:Skirt_L_03': 1, 'other:Skirt_L_04': 1, 'other:Skirt_R_01': 1, 'other:Skirt_R_02': 1, 'other:Skirt_R_03': 1, 'other:Skirt_R_04': 1, 'other:Skirt_M_01': 1, 'other:Skirt_M_02': 1, 'other:Skirt_M_03': 1, 'other:Skirt_M_04': 1, 'other:Tail': 1}
- KawaiiPhysics 组 7 个（ABP 变体 1 个），链 35 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：19 根

### ABP_Seq_Baer_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.04 LimitAngle=35.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：None

- [other:Skirt_L_02] Skirt_L_02 → Skirt_L_03 → Skirt_L_04
- [other:Skirt_R_02] Skirt_R_02 → Skirt_R_03 → Skirt_R_04
- [other:Skirt_M_02] Skirt_M_02 → Skirt_M_03 → Skirt_M_04

### ABP_Seq_Baer_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.25 Stiffness=0.1 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_R_xiuZiB01] Piao_R_xiuZiB01 → Piao_R_xiuZiB02
- [other:Piao_R_xiuZiA01] Piao_R_xiuZiA01 → Piao_R_xiuZiA02

### ABP_Seq_Baer_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=25.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_yaoBao01] Piao_yaoBao01 → Piao_yaoBao02 → Piao_yaoBao03
- [other:Piao_shiji01] Piao_shiji01
- [other:Piao_mingPai01] Piao_mingPai01

### ABP_Seq_Baer_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.1 Stiffness=0.025 LimitAngle=35.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [头发] Bone_Hair080_M → Bone_Hair081_M → Bone_Hair082_M → Bone_Hair083_M → Bone_Hair084_M

### ABP_Seq_Baer_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.1 Stiffness=0.03 LimitAngle=25.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_R_01] Piao_R_01 → Piao_R_02 → Piao_R_03
- [other:Piao_L_01] Piao_L_01 → Piao_L_02 → Piao_L_03

### ABP_Seq_Baer_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.3 Stiffness=0.05 LimitAngle=20.0 Radius=2.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r5.5
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [头发] Bone_Hair004 → Bone_Hair005 → Bone_Hair006
- [头发] Bone_Hair011 → Bone_Hair012 → Bone_Hair013 → Bone_Hair014
- [头发] Bone_Hair024 → Bone_Hair025 → Bone_Hair026 → Bone_Hair027
- [头发] Bone_Hair073 → Bone_Hair075 → Bone_Hair076
- [头发] Bone_Hair073 → Bone_Hair075 → Bone_Hair078
- [头发] Bone_Hair069 → Bone_Hair070
- [头发] Bone_Hair093 → Bone_Hair094
- [头发] Bone_Hair001 → Bone_Hair002
- [头发] Bone_Hair064 → Bone_Hair065 → Bone_Hair066
- [头发] Bone_Hair045 → Bone_Hair046 → Bone_Hair047
- [头发] Bone_Hair049 → Bone_Hair050
- [头发] Bone_Hair042 → Bone_Hair043
- [头发] Bone_Hair008 → Bone_Hair009
- [头发] Bone_Hair032 → Bone_Hair033
- [头发] Bone_Hair017 → Bone_Hair021 → Bone_Hair019
- [头发] Bone_Hair017 → Bone_Hair021 → Bone_Hair022
- [头发] Bone_Hair032 → Bone_Hair033
- [头发] Bone_Hair036 → Bone_Hair037
- [头发] Bone_Hair086 → Bone_Hair087 → Bone_Hair088
- [头发] Bone_Hair086 → Bone_Hair087 → Bone_Hair090 → Bone_Hair091
- [头发] Bone_Hair029 → Bone_Hair030
- [other:Piao_L_yanJing01] Piao_L_yanJing01

### ABP_Seq_Baer_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.25 Stiffness=0.1 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_L_xiuZiA01] Piao_L_xiuZiA01 → Piao_L_xiuZiA02
- [other:Piao_L_xiuZiB01] Piao_L_xiuZiB01 → Piao_L_xiuZiB02

## MaleXL/Bulante（R2T1BulanteMd10011）

- 骨总数 258；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'ribbon': 127, 'hair': 28, 'hand_twist': 2, 'prop': 1, 'pelvis_extra': 1}
- KawaiiPhysics 组 9 个（ABP 变体 1 个），链 77 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：63 根

### ABP_Seq_Bulante_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao025_M → Bone_Piao026_M
- [飘带(Piao)] Bone_Piao013_M → Bone_Piao014_M
- [飘带(Piao)] Bone_Piao017_M → Bone_Piao018_M
- [飘带(Piao)] Bone_Piao021_M → Bone_Piao022_M

### ABP_Seq_Bulante_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.1 Stiffness=0.02 LimitAngle=20.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.5
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [头发] Bone_Hair102_M
- [头发] Bone_Hair110_M
- [头发] Bone_Hair107_M
- [头发] Bone_Hair114_R
- [头发] Bone_Hair109_R
- [头发] Bone_Hair002_R
- [头发] Bone_Hair115_R
- [头发] Bone_Hair111_R
- [头发] Bone_Hair001_R
- [头发] Bone_Hair004_R
- [头发] Bone_Hair005_R
- [头发] Bone_Hair002_M
- [头发] Bone_Hair004_M
- [头发] Bone_Hair003_M
- [头发] Bone_Hair004_L
- [头发] Bone_Hair101_L
- [头发] Bone_Hair116_L
- [头发] Bone_Hair001_L
- [头发] Bone_Hair002_L
- [头发] Bone_Hair113_L
- [头发] Bone_Hair104_M

### ABP_Seq_Bulante_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=25.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao155_L → Bone_Piao156_L
- [飘带(Piao)] Bone_Piao155_R → Bone_Piao156_R

### ABP_Seq_Bulante_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=25.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao102_R → Bone_Piao103_R
- [飘带(Piao)] Bone_Piao106_R
- [飘带(Piao)] Bone_Piao102_M
- [飘带(Piao)] Bone_Piao106_L
- [飘带(Piao)] Bone_Piao102_L → Bone_Piao103_L

### ABP_Seq_Bulante_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.2 Stiffness=0.15 LimitAngle=25.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao112_R
- [飘带(Piao)] Bone_Piao114_R
- [飘带(Piao)] Bone_Piao124_R
- [飘带(Piao)] Bone_Piao120_R
- [飘带(Piao)] Bone_Piao106_M → Bone_Piao107_M
- [飘带(Piao)] Bone_Piao120_L
- [飘带(Piao)] Bone_Piao124_L
- [飘带(Piao)] Bone_Piao114_L
- [飘带(Piao)] Bone_Piao112_L

### ABP_Seq_Bulante_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.2 Stiffness=0.1 LimitAngle=30.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao150_L
- [飘带(Piao)] Bone_Piao148_L
- [飘带(Piao)] Bone_Piao143_L
- [飘带(Piao)] Bone_Piao139_L
- [飘带(Piao)] Bone_Piao137_L
- [飘带(Piao)] Bone_Piao150_R
- [飘带(Piao)] Bone_Piao148_R
- [飘带(Piao)] Bone_Piao143_R
- [飘带(Piao)] Bone_Piao139_R
- [飘带(Piao)] Bone_Piao137_R

### ABP_Seq_Bulante_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao214_L → Bone_Piao215_L → Bone_Piao216_L
- [飘带(Piao)] Bone_Piao209_L → Bone_Piao210_L → Bone_Piao211_L
- [飘带(Piao)] Bone_Piao204_L → Bone_Piao205_L → Bone_Piao206_L
- [飘带(Piao)] Bone_Piao205_R → Bone_Piao206_R → Bone_Piao207_R
- [飘带(Piao)] Bone_Piao209_R → Bone_Piao210_R → Bone_Piao211_R

### ABP_Seq_Bulante_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao218_L
- [飘带(Piao)] Bone_Piao171_R
- [飘带(Piao)] Bone_Piao166_R
- [飘带(Piao)] Bone_Piao166_L
- [飘带(Piao)] Bone_Piao171_L
- [飘带(Piao)] Bone_Piao005_M
- [飘带(Piao)] Bone_Piao009_M
- [飘带(Piao)] Bone_Piao003_M
- [飘带(Piao)] Bone_Piao007_M
- [飘带(Piao)] Bone_Piao132_L
- [飘带(Piao)] Bone_Piao130_L
- [飘带(Piao)] Bone_Piao132_R
- [飘带(Piao)] Bone_Piao130_R

### ABP_Seq_Bulante_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.1 Stiffness=0.05 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao160_R
- [飘带(Piao)] Bone_Piao158_R
- [飘带(Piao)] Bone_Piao164_R
- [飘带(Piao)] Bone_Piao162_R
- [飘带(Piao)] Bone_Piao160_L
- [飘带(Piao)] Bone_Piao158_L
- [飘带(Piao)] Bone_Piao164_L
- [飘带(Piao)] Bone_Piao162_L

## MaleXL/Jiyan（R2T1JiyanMd10011）

- 骨总数 188；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'other:Hair_L_liuHai01': 1, 'other:Hair_L_liuHai02': 1, 'other:Hair_L_liuHai03': 1, 'other:Hair_R_liuHai01': 1, 'other:Hair_R_liuHai02': 1, 'other:Hair_long01': 1, 'other:Hair_Back_01': 1, 'other:Hair_Back_03': 1, 'other:Hair_Back_06': 1, 'other:Hair_Back_02': 1, 'other:Hair_Back_04': 1, 'other:Hair_Back_08': 1, 'other:Hair_Back_09': 1, 'other:Hair_Back_12': 1, 'other:Hair_Back_17': 1, 'other:Hair_Back_22': 1, 'other:Hair_long02': 1, 'other:Hair_long03': 1, 'other:Hair_long04': 1, 'other:Hair_long05': 1, 'other:Hair_long06': 1, 'other:Hair_long07': 1, 'other:Hair_long08': 1, 'other:Hair_long09': 1, 'other:Hair_long10': 1, 'hair': 6, 'other:Piao_L_er01': 1, 'other:Piao_L_er02': 1, 'other:Piao_L_er03': 1, 'other:Piao_R_er01': 1, 'other:Piao_R_er02': 1, 'other:Piao_R_er03': 1, 'other:Hair_F_01': 1, 'other:Hair_F_02': 1, 'other:Hair_F_03': 1, 'other:Hair_F_05': 1, 'other:Hair_F_06': 1, 'other:Hair_F_07': 1, 'other:Hair_F_08': 1, 'other:Hair_F_09': 1, 'other:Hair_F_10': 1, 'other:Piao_L_yiXiu01': 1, 'other:Piao_L_yiXiu02': 1, 'other:Piao_L_yiXiu03': 1, 'other:Piao_L_yiXiu04': 1, 'other:Piao_L_clavicla01': 1, 'other:Piao_L_clavicla02': 1, 'other:Piao_L_clavicla03': 1, 'prop': 1, 'other:Skirt_L_B01': 1, 'other:Skirt_L_B02': 1, 'other:Skirt_L_B03': 1, 'other:Skirt_L_B04': 1, 'other:Skirt_L_B05': 1, 'other:Skirt_L_F01': 1, 'other:Skirt_L_F02': 1, 'other:Skirt_L_F03': 1, 'other:Skirt_L_F04': 1, 'other:Skirt_L_F05': 1, 'other:Piao_L_yaoDai01': 1, 'other:Piao_L_yaoDai02': 1, 'other:Skirt_L_out01': 1, 'other:Skirt_L_out02': 1, 'other:Skirt_L_out03': 1, 'other:Skirt_L_out04': 1, 'other:Skirt_L_out05': 1, 'other:Skirt_R_B01': 1, 'other:Skirt_R_B02': 1, 'other:Skirt_R_B03': 1, 'other:Skirt_R_B04': 1, 'other:Skirt_R_B05': 1, 'other:Skirt_R_F01': 1, 'other:Skirt_R_F02': 1, 'other:Skirt_R_F03': 1, 'other:Skirt_R_F04': 1, 'other:Skirt_R_F05': 1, 'other:Piao_hulu01': 1, 'other:Piao_R_yaoDai01': 1, 'other:Skirt_R_out01': 1, 'other:Skirt_R_out02': 1, 'other:Skirt_R_out03': 1, 'other:Skirt_R_out04': 1, 'other:Skirt_R_out05': 1, 'other:Piao_L_bao01': 1}
- KawaiiPhysics 组 9 个（ABP 变体 1 个），链 36 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：13 根

### ABP_Seq_Jiyan_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.1 Stiffness=0.02 LimitAngle=35.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.25
- Alpha 曲线：None

- [other:Hair_F_06] Hair_F_06
- [other:Hair_F_08] Hair_F_08
- [other:Hair_Back_03] Hair_Back_03 → Hair_Back_06
- [other:Hair_Back_17] Hair_Back_17
- [other:Hair_Back_12] Hair_Back_12
- [other:Hair_Back_22] Hair_Back_22
- [other:Hair_F_01] Hair_F_01
- [other:Hair_F_07] Hair_F_07
- [other:Hair_F_05] Hair_F_05

### ABP_Seq_Jiyan_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.2 Stiffness=0.06 LimitAngle=35.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_L_clavicla02] Piao_L_clavicla02 → Piao_L_clavicla03
- [other:Piao_L_yiXiu02] Piao_L_yiXiu02 → Piao_L_yiXiu03 → Piao_L_yiXiu04

### ABP_Seq_Jiyan_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.22 Stiffness=0.045 LimitAngle=35.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 6 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [other:Hair_long02] Hair_long02 → Hair_long03 → Hair_long04 → Hair_long05 → Hair_long06 → Hair_long07 → Hair_long08 → Hair_long09 → Hair_long10
- [other:Hair_long02] Hair_long02 → Hair_long03 → Bone_Hair001 → Bone_Hair002 → Bone_Hair003 → Bone_Hair004 → Bone_Hair005 → Bone_Hair006

### ABP_Seq_Jiyan_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.1 Stiffness=0.03 LimitAngle=35.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r8.0
- Alpha 曲线：None

- [other:Hair_F_02] Hair_F_02
- [other:Hair_F_09] Hair_F_09
- [other:Hair_F_10] Hair_F_10
- [other:Hair_F_03] Hair_F_03
- [other:Hair_Back_01] Hair_Back_01
- [other:Hair_Back_02] Hair_Back_02
- [other:Hair_Back_08] Hair_Back_08
- [other:Hair_Back_09] Hair_Back_09
- [other:Hair_Back_04] Hair_Back_04

### ABP_Seq_Jiyan_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.5 Stiffness=0.06 LimitAngle=20.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [other:Hair_R_liuHai01] Hair_R_liuHai01 → Hair_R_liuHai02
- [other:Hair_L_liuHai02] Hair_L_liuHai02 → Hair_L_liuHai03

### ABP_Seq_Jiyan_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.3 Stiffness=0.05 LimitAngle=20.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_L_bao01] Piao_L_bao01

### ABP_Seq_Jiyan_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.3 LimitAngle=35.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：None

- [other:Skirt_R_out03] Skirt_R_out03 → Skirt_R_out04 → Skirt_R_out05
- [other:Skirt_R_B03] Skirt_R_B03 → Skirt_R_B04 → Skirt_R_B05
- [other:Skirt_L_B03] Skirt_L_B03 → Skirt_L_B04 → Skirt_L_B05
- [other:Skirt_L_out03] Skirt_L_out03 → Skirt_L_out04 → Skirt_L_out05

### ABP_Seq_Jiyan_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.3 LimitAngle=35.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [other:Skirt_R_F01] Skirt_R_F01 → Skirt_R_F02 → Skirt_R_F03 → Skirt_R_F04 → Skirt_R_F05
- [other:Skirt_R_F01] Skirt_R_F01 → Piao_hulu01
- [other:Skirt_R_F01] Skirt_R_F01 → Piao_R_yaoDai01
- [other:Skirt_L_F01] Skirt_L_F01 → Skirt_L_F02 → Skirt_L_F03 → Skirt_L_F04 → Skirt_L_F05
- [other:Skirt_L_F01] Skirt_L_F01 → Piao_L_yaoDai01 → Piao_L_yaoDai02

### ABP_Seq_Jiyan_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.1 Stiffness=0.03 LimitAngle=35.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [other:Piao_L_er01] Piao_L_er01 → Piao_L_er02 → Piao_L_er03
- [other:Piao_R_er01] Piao_R_er01 → Piao_R_er02 → Piao_R_er03

## MaleXL/Kakaluo（R2T1KakaluoMd10011）

- 骨总数 200；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 63, 'prop': 1, 'ribbon': 37}
- KawaiiPhysics 组 1 个（ABP 变体 2 个），链 28 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：101 根
- 状态：{'chains_source': 'skeleton_heuristic (ABP 无 KawaiiPhysicsGroup 节点)'}

### skeleton_heuristic / heuristic_family_chains

- 参数：Damping=None Stiffness=None LimitAngle=None Radius=None HalfWidth=None Shape=None

- [头发] Bone_Hair001_R → Bone_Hair002_R → Bone_Hair003_R
- [头发] Bone_Hair005_L → Bone_Hair006_L → Bone_Hair007_L
- [头发] Bone_Hair009_L → Bone_Hair010_L → Bone_Hair011_L → Bone_Hair012_L → Bone_Hair013_L
- [头发] Bone_Hair009_L → Bone_Hair010_L → Bone_Hair015_L → Bone_Hair016_L → Bone_Hair017_L
- [头发] Bone_Hair009_L → Bone_Hair020_L → Bone_Hair021_L
- [头发] Bone_Hair024_R → Bone_Hair025_R → Bone_Hair026_R → Bone_Hair027_R
- [头发] Bone_Hair029_R → Bone_Hair030_R → Bone_Hair040_R → Bone_Hair041_R
- [头发] Bone_Hair029_R → Bone_Hair030_R → Bone_Hair031_R → Bone_Hair037_R → Bone_Hair038_R
- [头发] Bone_Hair029_R → Bone_Hair030_R → Bone_Hair031_R → Bone_Hair032_R → Bone_Hair033_R → Bone_Hair034_R
- [头发] Bone_Hair044_R → Bone_Hair045_R
- [头发] Bone_Hair074_R → Bone_Hair075_R → Bone_Hair076_R → Bone_Hair077_R
- [头发] Bone_Hair079_L → Bone_Hair080_L → Bone_Hair081_L → Bone_Hair082_L
- [头发] Bone_Hair089_M → Bone_Hair047_M → Bone_Hair048_M → Bone_Hair049_M → Bone_Hair050_M → Bone_Hair051_M → Bone_Hair052_M
- [头发] Bone_Hair089_M → Bone_Hair047_M → Bone_Hair048_M → Bone_Hair049_M → Bone_Hair065_L → Bone_Hair066_L
- [头发] Bone_Hair089_M → Bone_Hair047_M → Bone_Hair048_M → Bone_Hair049_M → Bone_Hair060_L → Bone_Hair061_L → Bone_Hair062_L
- [头发] Bone_Hair089_M → Bone_Hair047_M → Bone_Hair048_M → Bone_Hair049_M → Bone_Hair055_R → Bone_Hair056_R → Bone_Hair057_R
- [头发] Bone_Hair089_M → Bone_Hair047_M → Bone_Hair048_M → Bone_Hair049_M → Bone_Hair069_R → Bone_Hair070_R → Bone_Hair071_R → Bone_Hair072_R
- [头发] Bone_Hair089_M → Bone_Hair047_M → Bone_Hair084_L → Bone_Hair085_L
- [头发] Bone_Hair089_M → Bone_Hair087_R → Bone_Hair088_R
- [飘带(Piao)] Bone_Piao001_M → Bone_Piao008_M → Bone_Piao009_M → Bone_Piao010_M → Bone_Piao011_M
- [飘带(Piao)] Bone_Piao002_L → Bone_Piao013_L → Bone_Piao014_L → Bone_Piao015_L → Bone_Piao016_L
- [飘带(Piao)] Bone_Piao002_L → Bone_Piao041_L → Bone_Piao043_L
- [飘带(Piao)] Bone_Piao003_R → Bone_Piao027_R → Bone_Piao028_R → Bone_Piao029_R → Bone_Piao030_R
- [飘带(Piao)] Bone_Piao003_R → Bone_Piao042_R → Bone_Piao045_R
- [飘带(Piao)] Bone_Piao004_L → Bone_Piao018_L → Bone_Piao019_L → Bone_Piao020_L → Bone_Piao021_L
- [飘带(Piao)] Bone_Piao005_R → Bone_Piao036_R → Bone_Piao037_R → Bone_Piao038_R → Bone_Piao039_R
- [飘带(Piao)] Bone_Piao006_L → Bone_Piao023_L → Bone_Piao024_L → Bone_Piao025_L
- [飘带(Piao)] Bone_Piao007_R → Bone_Piao032_R → Bone_Piao033_R → Bone_Piao034_R

## MaleXL/Luhesi（R2T1LuhesiMd10011）

- 骨总数 379；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 136, 'ribbon': 52, 'skirt': 91, 'prop': 1}
- KawaiiPhysics 组 6 个（ABP 变体 1 个），链 71 条
- KawaiiCurve：hair, skirt, sleeve
- 未入组二级骨：102 根

### ABP_Seq_Luhesi_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=50.0 Radius=1.2 HalfWidth=5.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S01

- [裙摆] Bone_Skirt002_L → Bone_Skirt003_L → Bone_Skirt004_L → Bone_Skirt005_L → Bone_Skirt006_L → Bone_Skirt007_L → Bone_Skirt008_L → Bone_Skirt009_L → Bone_Skirt010_L → Bone_Skirt011_L
- [裙摆] Bone_Skirt002_L → Bone_Skirt003_L → Bone_Skirt004_L → Bone_Skirt018_L → Bone_Skirt019_L
- [裙摆] Bone_Skirt002_L → Bone_Skirt003_L → Bone_Skirt004_L → Bone_Skirt018_L → Bone_Skirt020_L
- [裙摆] Bone_Skirt002_L → Bone_Skirt003_L → Bone_Skirt004_L → Bone_Skirt017_L
- [裙摆] Bone_Skirt002_L → Bone_Skirt003_L → Bone_Skirt012_L → Bone_Skirt013_L → Bone_Skirt014_L → Bone_Skirt015_L → Bone_Skirt016_L
- [裙摆] Bone_Skirt001_R → Bone_Skirt003_R → Bone_Skirt004_R → Bone_Skirt005_R → Bone_Skirt006_R → Bone_Skirt007_R → Bone_Skirt008_R
- [裙摆] Bone_Skirt001_R → Bone_Skirt003_R → Bone_Skirt004_R → Bone_Skirt017_R
- [裙摆] Bone_Skirt001_R → Bone_Skirt003_R → Bone_Skirt004_R → Bone_Skirt018_R → Bone_Skirt020_R
- [裙摆] Bone_Skirt001_R → Bone_Skirt003_R → Bone_Skirt004_R → Bone_Skirt018_R → Bone_Skirt019_R
- [裙摆] Bone_Skirt061_R → Bone_Skirt064_R → Bone_Skirt065_R → Bone_Skirt066_R → Bone_Skirt067_R
- [裙摆] Bone_Skirt061_R → Bone_Skirt062_R → Bone_Skirt063_R
- [裙摆] Bone_Skirt061_R → Bone_Skirt068_R → Bone_Skirt069_R → Bone_Skirt070_R

### ABP_Seq_Luhesi_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=30.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S02

- [裙摆] Bone_Skirt032_L → Bone_Skirt033_L → Bone_Skirt034_L → Bone_Skirt035_L → Bone_Skirt036_L → Bone_Skirt037_L
- [裙摆] Bone_Skirt039_L → Bone_Skirt040_L → Bone_Skirt041_L → Bone_Skirt042_L
- [裙摆] Bone_Skirt003_M → Bone_Skirt004_M → Bone_Skirt080_R → Bone_Skirt081_R → Bone_Skirt082_R
- [裙摆] Bone_Skirt003_M → Bone_Skirt004_M → Bone_Skirt080_L → Bone_Skirt081_L → Bone_Skirt082_L
- [裙摆] Bone_Skirt032_R → Bone_Skirt033_R → Bone_Skirt034_R → Bone_Skirt035_R → Bone_Skirt036_R → Bone_Skirt037_R
- [裙摆] Bone_Skirt039_R → Bone_Skirt040_R
- [裙摆] Bone_Skirt052_R → Bone_Skirt053_R → Bone_Skirt054_R

### ABP_Seq_Luhesi_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r8.0
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：None

- [头发] Bone_Hair033_R → Bone_Hair034_R
- [头发] Bone_Hair033_R → Bone_Hair035_R → Bone_Hair036_R
- [头发] Bone_Hair033_L → Bone_Hair034_L
- [头发] Bone_Hair033_L → Bone_Hair035_L → Bone_Hair036_L
- [头发] Bone_Hair033_L → Bone_Hair037_L → Bone_Hair038_L → Bone_Hair039_L
- [头发] Bone_Hair021_M → Bone_Hair022_M
- [头发] Bone_Hair004_M
- [头发] Bone_Hair002_M
- [头发] Bone_Hair015_L → Bone_Hair016_L
- [头发] Bone_Hair002_L → Bone_Hair003_L
- [头发] Bone_Hair006_L → Bone_Hair007_L
- [头发] Bone_Hair072_M → Bone_Hair073_M
- [头发] Bone_Hair063_M
- [头发] Bone_Hair047_L → Bone_Hair048_L
- [头发] Bone_Hair072_L
- [头发] Bone_Hair070_L → Bone_Hair071_L
- [头发] Bone_Hair052_M → Bone_Hair053_M
- [头发] Bone_Hair080_M → Bone_Hair081_M
- [头发] Bone_Hair111_R → Bone_Hair112_R
- [头发] Bone_Hair113_R → Bone_Hair114_R
- [头发] Bone_Hair073_R → Bone_Hair074_R
- [头发] Bone_Hair081_R → Bone_Hair082_R
- [头发] Bone_Hair084_R → Bone_Hair085_R
- [头发] Bone_Hair086_R → Bone_Hair087_R
- [头发] Bone_Hair091_R → Bone_Hair092_R
- [头发] Bone_Hair101_R → Bone_Hair102_R
- [头发] Bone_Hair032_M → Bone_Hair033_M
- [头发] Bone_Hair041_M → Bone_Hair042_M

### ABP_Seq_Luhesi_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=40.0 Radius=4.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S04

- [飘带(Piao)] Bone_Piao011_L → Bone_Piao012_L → Bone_Piao013_L → Bone_Piao014_L → Bone_Piao015_L
- [飘带(Piao)] Bone_Piao021_L → Bone_Piao022_L → Bone_Piao023_L

### ABP_Seq_Luhesi_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=1.0 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S03

- [头发] Bone_Hair040_L → Bone_Hair041_L → Bone_Hair042_L → Bone_Hair043_L
- [头发] Bone_Hair040_L → Bone_Hair044_L → Bone_Hair045_L → Bone_Hair046_L
- [头发] Bone_Hair051_L → Bone_Hair052_L → Bone_Hair053_L → Bone_Hair054_L
- [头发] Bone_Hair060_L → Bone_Hair061_L → Bone_Hair062_L → Bone_Hair063_L
- [头发] Bone_Hair060_L → Bone_Hair061_L → Bone_Hair062_L → Bone_Hair067_L → Bone_Hair068_L → Bone_Hair069_L
- [头发] Bone_Hair060_L → Bone_Hair061_L → Bone_Hair064_L → Bone_Hair065_L → Bone_Hair066_L
- [头发] Bone_Hair060_R → Bone_Hair061_R → Bone_Hair062_R → Bone_Hair063_R
- [头发] Bone_Hair060_R → Bone_Hair061_R → Bone_Hair064_R → Bone_Hair065_R → Bone_Hair066_R
- [头发] Bone_Hair060_R → Bone_Hair061_R → Bone_Hair067_R → Bone_Hair068_R
- [头发] Bone_Hair051_L → Bone_Hair052_L → Bone_Hair053_L → Bone_Hair054_L
- [头发] Bone_Hair040_R → Bone_Hair041_R → Bone_Hair042_R → Bone_Hair043_R
- [头发] Bone_Hair040_R → Bone_Hair041_R → Bone_Hair044_R → Bone_Hair045_R
- [头发] Bone_Hair091_M → Bone_Hair092_M → Bone_Hair093_M

### ABP_Seq_Luhesi_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=20.0 Radius=0.8 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S05

- [飘带(Piao)] Bone_Piao041_R
- [飘带(Piao)] Bone_Piao011_R
- [飘带(Piao)] Bone_Piao021_R
- [飘带(Piao)] Bone_Piao031_R
- [飘带(Piao)] Bone_Piao005_R
- [飘带(Piao)] Bone_Piao005_L
- [飘带(Piao)] Bone_Piao004_R
- [飘带(Piao)] Bone_Piao004_L
- [飘带(Piao)] Bone_Piao002_R

## MaleXL/Qiushui（R2T1QiushuiMd10011）

- 骨总数 178；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 8, 'other:Hair_L_ceBingA01': 1, 'other:Hair_L_ceBingA02': 1, 'other:Hair_L_liuHai01': 1, 'other:Hair_L_liuHai02': 1, 'other:Hair_L_liuHai03': 1, 'other:Hair_R_liuHai01': 1, 'other:Hair_R_liuHai02': 1, 'other:Hair_R_liuHai03': 1, 'other:Hair_R_ceBingB01': 1, 'other:Hair_R_ceBingB02': 1, 'other:Hair_L_ceBingB01': 1, 'other:Hair_L_ceBingB02': 1, 'other:Hair_L_back01': 1, 'other:Hair_L_back02': 1, 'other:Hair_R_ceBingC01': 1, 'other:Hair_R_ceBingC02': 1, 'other:Hair_M_bianZi01': 1, 'other:Hair_M_bianZi02': 1, 'other:Hair_M_bianZi03': 1, 'other:Hair_R_back01': 1, 'other:Hair_R_back02': 1, 'prop': 1, 'other:Piao_L_yiLing02': 1, 'other:Piao_L_yiLing03': 1, 'other:Piao_M_yiLing01': 1, 'other:Piao_M_yiLing02': 1, 'other:Piao_R_yiLing02': 1, 'other:Piao_R_yiLing03': 1, 'other:Piao_L_yiLing01': 1, 'other:Piao_R_yiLing01': 1, 'other:Piao_R_backB01': 1, 'other:Piao_R_backB02': 1, 'other:Piao_R_backB03': 1, 'other:Piao_R_backB04': 1, 'other:Piao_R_backB05': 1, 'other:Piao_R_backB06': 1, 'other:Piao_R_backB07': 1, 'other:Piao_R_backB08': 1, 'other:Piao_L_backB01': 1, 'other:Piao_L_backB02': 1, 'other:Piao_L_backB03': 1, 'other:Piao_L_backB04': 1, 'other:Piao_L_backB05': 1, 'other:Piao_L_backB06': 1, 'other:Piao_L_backB07': 1, 'other:Piao_L_backB08': 1, 'other:Piao_R_backA01': 1, 'other:Piao_R_backA02': 1, 'other:Piao_R_backA03': 1, 'other:Piao_R_backA04': 1, 'other:Piao_R_backA05': 1, 'other:Piao_R_backA06': 1, 'other:Piao_R_backA07': 1, 'other:Piao_R_backA08': 1, 'other:Piao_L_backA01': 1, 'other:Piao_L_backA02': 1, 'other:Piao_L_backA03': 1, 'other:Piao_L_backA04': 1, 'other:Piao_L_backA05': 1, 'other:Piao_L_backA06': 1, 'other:Piao_L_backA07': 1, 'other:Piao_L_backA08': 1, 'other:Piao_L_F01': 1, 'other:Piao_L_F02': 1, 'other:Piao_L_F03': 1, 'other:Piao_L_F04': 1, 'other:Piao_L_F05': 1, 'other:Piao_L_F06': 1, 'other:Piao_L_F07': 1, 'other:Piao_R_F01': 1, 'other:Piao_R_F02': 1, 'other:Piao_R_F03': 1, 'other:Piao_R_F04': 1, 'other:Piao_R_F05': 1, 'other:Piao_R_F06': 1, 'other:Piao_R_F07': 1, 'other:L': 1, 'other:R': 1}
- KawaiiPhysics 组 6 个（ABP 变体 1 个），链 25 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：11 根

### ABP_Seq_Qiushui_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.1 Stiffness=0.05 LimitAngle=25.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_L_yiLing01] Piao_L_yiLing01
- [other:Piao_L_yiLing02] Piao_L_yiLing02
- [other:Piao_L_yiLing03] Piao_L_yiLing03
- [other:Piao_M_yiLing02] Piao_M_yiLing02
- [other:Piao_R_yiLing03] Piao_R_yiLing03
- [other:Piao_R_yiLing02] Piao_R_yiLing02
- [other:Piao_R_yiLing01] Piao_R_yiLing01
- [other:Piao_M_yiLing01] Piao_M_yiLing01

### ABP_Seq_Qiushui_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.1 Stiffness=0.05 LimitAngle=20.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [other:R] Bone_R_bao01
- [other:L] Bone_L_bao01

### ABP_Seq_Qiushui_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=15.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.25
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：None

- [other:Hair_L_back01] Hair_L_back01 → Hair_L_back02
- [other:Hair_R_back01] Hair_R_back01 → Hair_R_back02
- [other:Hair_M_bianZi01] Hair_M_bianZi01 → Hair_M_bianZi02 → Hair_M_bianZi03

### ABP_Seq_Qiushui_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=25.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：None

- [other:Hair_L_ceBingA01] Hair_L_ceBingA01 → Hair_L_ceBingA02
- [other:Hair_L_ceBingB01] Hair_L_ceBingB01 → Hair_L_ceBingB02
- [other:Hair_L_liuHai01] Hair_L_liuHai01 → Hair_L_liuHai02 → Hair_L_liuHai03
- [other:Hair_R_liuHai01] Hair_R_liuHai01 → Hair_R_liuHai02 → Hair_R_liuHai03
- [other:Hair_R_ceBingB01] Hair_R_ceBingB01 → Hair_R_ceBingB02
- [other:Hair_R_ceBingC01] Hair_R_ceBingC01 → Hair_R_ceBingC02

### ABP_Seq_Qiushui_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=45.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 8 / 平面碰撞 0
- Alpha 曲线：None

- [other:Piao_L_backA03] Piao_L_backA03 → Piao_L_backA04 → Piao_L_backA05 → Piao_L_backA06 → Piao_L_backA07 → Piao_L_backA08
- [other:Piao_L_backB03] Piao_L_backB03 → Piao_L_backB04 → Piao_L_backB05 → Piao_L_backB06 → Piao_L_backB07 → Piao_L_backB08
- [other:Piao_R_backB03] Piao_R_backB03 → Piao_R_backB04 → Piao_R_backB05 → Piao_R_backB06 → Piao_R_backB07 → Piao_R_backB08
- [other:Piao_R_backA03] Piao_R_backA03 → Piao_R_backA04 → Piao_R_backA05 → Piao_R_backA06 → Piao_R_backA07 → Piao_R_backA08

### ABP_Seq_Qiushui_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.2 Stiffness=0.05 LimitAngle=45.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_chazhi

- [other:Piao_R_F02] Piao_R_F02 → Piao_R_F03 → Piao_R_F04 → Piao_R_F05 → Piao_R_F06 → Piao_R_F07
- [other:Piao_L_F02] Piao_L_F02 → Piao_L_F03 → Piao_L_F04 → Piao_L_F05 → Piao_L_F06 → Piao_L_F07

## MaleXL/Qiuyuan（R2T1QiuyuanMd10011）

- 骨总数 362；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 120, 'ribbon': 72, 'other:Other': 1, 'hand_twist': 2, 'prop': 1, 'skirt': 66, 'pelvis_extra': 1}
- KawaiiPhysics 组 13 个（ABP 变体 1 个），链 72 条
- KawaiiCurve：generic
- 未入组二级骨：99 根

### ABP_Seq_Qiuyuan_Phy / AnimGraphNode_KawaiiPhysicsGroup_12

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=75.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S03

- [裙摆] Bone_Skirt002_L → Bone_Skirt010_L → Bone_Skirt011_L → Bone_Skirt012_L → Bone_Skirt013_L
- [裙摆] Bone_Skirt002_L → Bone_Skirt005_L → Bone_Skirt014_L → Bone_Skirt015_L → Bone_Skirt016_L
- [裙摆] Bone_Skirt001_L → Bone_Skirt006_L → Bone_Skirt007_L → Bone_Skirt008_L → Bone_Skirt009_L
- [裙摆] Bone_Skirt001_R → Bone_Skirt005_R → Bone_Skirt006_R → Bone_Skirt007_R → Bone_Skirt008_R
- [裙摆] Bone_Skirt001_R → Bone_Skirt009_R → Bone_Skirt010_R → Bone_Skirt011_R
- [裙摆] Bone_Skirt002_R → Bone_Skirt012_R → Bone_Skirt013_R → Bone_Skirt014_R → Bone_Skirt015_R
- [裙摆] Bone_Skirt009_R → Bone_Skirt010_R → Bone_Skirt011_R

### ABP_Seq_Qiuyuan_Phy / AnimGraphNode_KawaiiPhysicsGroup_11

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=40.0 Radius=1.5 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [裙摆] Bone_Skirt014_L → Bone_Skirt015_L → Bone_Skirt016_L
- [飘带(Piao)] Bone_Piao005_M → Bone_Piao008_M
- [飘带(Piao)] Bone_Piao006_M → Bone_Piao009_M
- [飘带(Piao)] Bone_Piao017_L → Bone_Piao018_L
- [飘带(Piao)] Bone_Piao020_L → Bone_Piao021_L

### ABP_Seq_Qiuyuan_Phy / AnimGraphNode_KawaiiPhysicsGroup_10

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=35.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S05

- [飘带(Piao)] Bone_Piao029_L
- [飘带(Piao)] Bone_Piao034_L → Bone_Piao035_L
- [飘带(Piao)] Bone_Piao031_L → Bone_Piao032_L
- [飘带(Piao)] Bone_Piao024_L → Bone_Piao025_L → Bone_Piao026_L → Bone_Piao027_L
- [飘带(Piao)] Bone_Piao028_L

### ABP_Seq_Qiuyuan_Phy / AnimGraphNode_KawaiiPhysicsGroup_9

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=35.0 Radius=3.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao020_R
- [飘带(Piao)] Bone_Piao021_R → Bone_Piao022_R
- [飘带(Piao)] Bone_Piao024_R → Bone_Piao025_R
- [飘带(Piao)] Bone_Piao016_R → Bone_Piao017_R
- [飘带(Piao)] Bone_Piao027_R → Bone_Piao028_R
- [飘带(Piao)] Bone_Piao018_R → Bone_Piao019_R

### ABP_Seq_Qiuyuan_Phy / AnimGraphNode_KawaiiPhysicsGroup_8

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=50.0 Radius=1.5 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S02

- [头发] Bone_Hair026_L → Bone_Hair027_L → Bone_Hair028_L
- [头发] Bone_Hair026_L → Bone_Hair029_L → Bone_Hair030_L
- [头发] Bone_Hair017_L → Bone_Hair018_L → Bone_Hair019_L → Bone_Hair020_L → Bone_Hair021_L
- [头发] Bone_Hair017_L → Bone_Hair018_L → Bone_Hair022_L → Bone_Hair023_L
- [头发] Bone_Hair017_L → Bone_Hair018_L → Bone_Hair040_L → Bone_Hair041_L → Bone_Hair042_L
- [头发] Bone_Hair003_M → Bone_Hair004_M → Bone_Hair005_M → Bone_Hair006_M → Bone_Hair007_M → Bone_Hair008_M
- [头发] Bone_Hair003_M → Bone_Hair004_M → Bone_Hair011_M → Bone_Hair012_M
- [头发] Bone_Hair019_R → Bone_Hair020_R → Bone_Hair021_R
- [头发] Bone_Hair019_R → Bone_Hair020_R → Bone_Hair040_R → Bone_Hair041_R

### ABP_Seq_Qiuyuan_Phy / AnimGraphNode_KawaiiPhysicsGroup_7

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=25.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r7.7449355
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：None

- [头发] Bone_Hair037_R → Bone_Hair038_R
- [头发] Bone_Hair029_R
- [头发] Bone_Hair039_R
- [头发] Bone_Hair023_M
- [头发] Bone_Hair021_M → Bone_Hair022_M
- [头发] Bone_Hair024_M → Bone_Hair025_M
- [头发] Bone_Hair035_L → Bone_Hair036_L
- [头发] Bone_Hair031_R → Bone_Hair032_R
- [头发] Bone_Hair033_R → Bone_Hair034_R
- [头发] Bone_Hair035_R → Bone_Hair036_R
- [头发] Bone_Hair033_L → Bone_Hair034_L
- [头发] Bone_Hair031_L → Bone_Hair032_L
- [头发] Bone_Hair037_L → Bone_Hair038_L
- [头发] Bone_Hair030_R

### ABP_Seq_Qiuyuan_Phy / AnimGraphNode_KawaiiPhysicsGroup_6

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=45.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 3 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S01

- [头发] Bone_Hair012_R → Bone_Hair013_R → Bone_Hair014_R → Bone_Hair015_R → Bone_Hair016_R
- [头发] Bone_Hair010_L → Bone_Hair011_L → Bone_Hair012_L → Bone_Hair013_L → Bone_Hair014_L

### ABP_Seq_Qiuyuan_Phy / AnimGraphNode_KawaiiPhysicsGroup_5

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=25.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r9.467909
- Alpha 曲线：None

- [头发] Bone_Hair035_M → Bone_Hair036_M
- [头发] Bone_Hair037_M → Bone_Hair038_M

### ABP_Seq_Qiuyuan_Phy / AnimGraphNode_KawaiiPhysicsGroup_4

- 参数：Damping=0.1 Stiffness=0.05 LimitAngle=40.0 Radius=1.5 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [头发] Bone_Hair014_M → Bone_Hair015_M
- [头发] Bone_Hair022_R → Bone_Hair023_R

### ABP_Seq_Qiuyuan_Phy / AnimGraphNode_KawaiiPhysicsGroup_3

- 参数：Damping=0.15 Stiffness=0.04 LimitAngle=50.0 Radius=2.0 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [头发] Bone_Hair016_M → Bone_Hair017_M
- [头发] Bone_Hair026_R → Bone_Hair027_R
- [头发] Bone_Hair011_R → Bone_Hair028_R

### ABP_Seq_Qiuyuan_Phy / AnimGraphNode_KawaiiPhysicsGroup_2

- 参数：Damping=0.09 Stiffness=0.06 LimitAngle=50.0 Radius=2.0 HalfWidth=1.0 Shape=EPhyCollisionShape::Spherical
- Alpha 曲线：None

- [头发] Bone_Hair024_R → Bone_Hair025_R
- [头发] Bone_Hair011_R → Bone_Hair028_R

### ABP_Seq_Qiuyuan_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.1 LimitAngle=35.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 1 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S01

- [飘带(Piao)] Bone_Piao002_M
- [飘带(Piao)] Bone_Piao008_L
- [飘带(Piao)] Bone_Piao008_R
- [飘带(Piao)] Bone_Piao009_L
- [飘带(Piao)] Bone_Piao009_R
- [飘带(Piao)] Bone_Piao003_M

### ABP_Seq_Qiuyuan_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=40.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Planar
- 胶囊碰撞 2 / 平面碰撞 0
- Alpha 曲线：Kawaii_alpha_S04

- [裙摆] Bone_Skirt027_R → Bone_Skirt028_R → Bone_Skirt029_R → Bone_Skirt030_R
- [裙摆] Bone_Skirt031_R → Bone_Skirt032_R
- [裙摆] Bone_Skirt020_R → Bone_Skirt021_R → Bone_Skirt022_R
- [裙摆] Bone_Skirt023_R → Bone_Skirt024_R
- [裙摆] Bone_Skirt028_L → Bone_Skirt029_L → Bone_Skirt030_L
- [裙摆] Bone_Skirt031_L → Bone_Skirt032_L
- [裙摆] Bone_Skirt019_L → Bone_Skirt020_L → Bone_Skirt021_L → Bone_Skirt022_L
- [裙摆] Bone_Skirt023_L → Bone_Skirt024_L
- [裙摆] Bone_Skirt002_M → Bone_Skirt003_M

## MaleXL/TestModel（TestModel01）

- 骨总数 188；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'other:Hair_L_liuHai01': 1, 'other:Hair_L_liuHai02': 1, 'other:Hair_L_liuHai03': 1, 'other:Hair_R_liuHai01': 1, 'other:Hair_R_liuHai02': 1, 'other:Hair_long01': 1, 'other:Hair_Back_01': 1, 'other:Hair_Back_03': 1, 'other:Hair_Back_06': 1, 'other:Hair_Back_02': 1, 'other:Hair_Back_04': 1, 'other:Hair_Back_08': 1, 'other:Hair_Back_09': 1, 'other:Hair_Back_12': 1, 'other:Hair_Back_17': 1, 'other:Hair_Back_22': 1, 'other:Hair_long02': 1, 'other:Hair_long03': 1, 'other:Hair_long04': 1, 'other:Hair_long05': 1, 'other:Hair_long06': 1, 'other:Hair_long07': 1, 'other:Hair_long08': 1, 'other:Hair_long09': 1, 'other:Hair_long10': 1, 'hair': 6, 'other:Piao_L_er01': 1, 'other:Piao_L_er02': 1, 'other:Piao_L_er03': 1, 'other:Piao_R_er01': 1, 'other:Piao_R_er02': 1, 'other:Piao_R_er03': 1, 'other:Hair_F_01': 1, 'other:Hair_F_02': 1, 'other:Hair_F_03': 1, 'other:Hair_F_05': 1, 'other:Hair_F_06': 1, 'other:Hair_F_07': 1, 'other:Hair_F_08': 1, 'other:Hair_F_09': 1, 'other:Hair_F_10': 1, 'other:Piao_L_yiXiu01': 1, 'other:Piao_L_yiXiu02': 1, 'other:Piao_L_yiXiu03': 1, 'other:Piao_L_yiXiu04': 1, 'other:Piao_L_clavicla01': 1, 'other:Piao_L_clavicla02': 1, 'other:Piao_L_clavicla03': 1, 'prop': 1, 'other:Skirt_L_B01': 1, 'other:Skirt_L_B02': 1, 'other:Skirt_L_B03': 1, 'other:Skirt_L_B04': 1, 'other:Skirt_L_B05': 1, 'other:Skirt_L_F01': 1, 'other:Skirt_L_F02': 1, 'other:Skirt_L_F03': 1, 'other:Skirt_L_F04': 1, 'other:Skirt_L_F05': 1, 'other:Piao_L_yaoDai01': 1, 'other:Piao_L_yaoDai02': 1, 'other:Skirt_L_out01': 1, 'other:Skirt_L_out02': 1, 'other:Skirt_L_out03': 1, 'other:Skirt_L_out04': 1, 'other:Skirt_L_out05': 1, 'other:Skirt_R_B01': 1, 'other:Skirt_R_B02': 1, 'other:Skirt_R_B03': 1, 'other:Skirt_R_B04': 1, 'other:Skirt_R_B05': 1, 'other:Skirt_R_F01': 1, 'other:Skirt_R_F02': 1, 'other:Skirt_R_F03': 1, 'other:Skirt_R_F04': 1, 'other:Skirt_R_F05': 1, 'other:Piao_hulu01': 1, 'other:Piao_R_yaoDai01': 1, 'other:Skirt_R_out01': 1, 'other:Skirt_R_out02': 1, 'other:Skirt_R_out03': 1, 'other:Skirt_R_out04': 1, 'other:Skirt_R_out05': 1, 'other:Piao_L_bao01': 1}
- KawaiiPhysics 组 1 个（ABP 变体 1 个），链 1 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：89 根
- 状态：{'chains_source': 'skeleton_heuristic (ABP 无 KawaiiPhysicsGroup 节点)'}

### skeleton_heuristic / heuristic_family_chains

- 参数：Damping=None Stiffness=None LimitAngle=None Radius=None HalfWidth=None Shape=None

- [头发] Bone_Hair001 → Bone_Hair002 → Bone_Hair003 → Bone_Hair004 → Bone_Hair005 → Bone_Hair006

## MaleXL/TestModel（TestModel02）

- 骨总数 188；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'other:Hair_L_liuHai01': 1, 'other:Hair_L_liuHai02': 1, 'other:Hair_L_liuHai03': 1, 'other:Hair_R_liuHai01': 1, 'other:Hair_R_liuHai02': 1, 'other:Hair_long01': 1, 'other:Hair_Back_01': 1, 'other:Hair_Back_03': 1, 'other:Hair_Back_06': 1, 'other:Hair_Back_02': 1, 'other:Hair_Back_04': 1, 'other:Hair_Back_08': 1, 'other:Hair_Back_09': 1, 'other:Hair_Back_12': 1, 'other:Hair_Back_17': 1, 'other:Hair_Back_22': 1, 'other:Hair_long02': 1, 'other:Hair_long03': 1, 'other:Hair_long04': 1, 'other:Hair_long05': 1, 'other:Hair_long06': 1, 'other:Hair_long07': 1, 'other:Hair_long08': 1, 'other:Hair_long09': 1, 'other:Hair_long10': 1, 'hair': 6, 'other:Piao_L_er01': 1, 'other:Piao_L_er02': 1, 'other:Piao_L_er03': 1, 'other:Piao_R_er01': 1, 'other:Piao_R_er02': 1, 'other:Piao_R_er03': 1, 'other:Hair_F_01': 1, 'other:Hair_F_02': 1, 'other:Hair_F_03': 1, 'other:Hair_F_05': 1, 'other:Hair_F_06': 1, 'other:Hair_F_07': 1, 'other:Hair_F_08': 1, 'other:Hair_F_09': 1, 'other:Hair_F_10': 1, 'other:Piao_L_yiXiu01': 1, 'other:Piao_L_yiXiu02': 1, 'other:Piao_L_yiXiu03': 1, 'other:Piao_L_yiXiu04': 1, 'other:Piao_L_clavicla01': 1, 'other:Piao_L_clavicla02': 1, 'other:Piao_L_clavicla03': 1, 'prop': 1, 'other:Skirt_L_B01': 1, 'other:Skirt_L_B02': 1, 'other:Skirt_L_B03': 1, 'other:Skirt_L_B04': 1, 'other:Skirt_L_B05': 1, 'other:Skirt_L_F01': 1, 'other:Skirt_L_F02': 1, 'other:Skirt_L_F03': 1, 'other:Skirt_L_F04': 1, 'other:Skirt_L_F05': 1, 'other:Piao_L_yaoDai01': 1, 'other:Piao_L_yaoDai02': 1, 'other:Skirt_L_out01': 1, 'other:Skirt_L_out02': 1, 'other:Skirt_L_out03': 1, 'other:Skirt_L_out04': 1, 'other:Skirt_L_out05': 1, 'other:Skirt_R_B01': 1, 'other:Skirt_R_B02': 1, 'other:Skirt_R_B03': 1, 'other:Skirt_R_B04': 1, 'other:Skirt_R_B05': 1, 'other:Skirt_R_F01': 1, 'other:Skirt_R_F02': 1, 'other:Skirt_R_F03': 1, 'other:Skirt_R_F04': 1, 'other:Skirt_R_F05': 1, 'other:Piao_hulu01': 1, 'other:Piao_R_yaoDai01': 1, 'other:Skirt_R_out01': 1, 'other:Skirt_R_out02': 1, 'other:Skirt_R_out03': 1, 'other:Skirt_R_out04': 1, 'other:Skirt_R_out05': 1, 'other:Piao_L_bao01': 1}
- KawaiiPhysics 组 1 个（ABP 变体 1 个），链 1 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：89 根
- 状态：{'chains_source': 'skeleton_heuristic (ABP 无 KawaiiPhysicsGroup 节点)'}

### skeleton_heuristic / heuristic_family_chains

- 参数：Damping=None Stiffness=None LimitAngle=None Radius=None HalfWidth=None Shape=None

- [头发] Bone_Hair001 → Bone_Hair002 → Bone_Hair003 → Bone_Hair004 → Bone_Hair005 → Bone_Hair006

## MaleXL/Yuanwu（R2T1YuanwuMd10011）

- 骨总数 142；骨族 {'other:Root': 1, 'bip001': 91, 'weapon_prop': 7, 'hair': 10, 'ribbon': 26, 'other:Other': 6, 'prop': 1}
- KawaiiPhysics 组 2 个（ABP 变体 1 个），链 5 条
- KawaiiCurve：无（该角色无 Data/KawaiiCurve 目录）
- 未入组二级骨：23 根

### ABP_Seq_Yuanwu_Phy / AnimGraphNode_KawaiiPhysicsGroup_1

- 参数：Damping=0.2 Stiffness=0.2 LimitAngle=35.0 Radius=2.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 胶囊碰撞 4 / 平面碰撞 0
- Alpha 曲线：None

- [飘带(Piao)] Bone_Piao015_R → Bone_Piao016_R → Bone_Piao017_R → Bone_Piao018_R → Bone_Piao019_R
- [飘带(Piao)] Bone_Piao022_R → Bone_Piao023_R → Bone_Piao024_R → Bone_Piao025_R
- [飘带(Piao)] Bone_Piao009_L → Bone_Piao010_L → Bone_Piao011_L → Bone_Piao012_L
- [飘带(Piao)] Bone_Piao002_L → Bone_Piao003_L → Bone_Piao004_L → Bone_Piao005_L → Bone_Piao006_L

### ABP_Seq_Yuanwu_Phy / AnimGraphNode_KawaiiPhysicsGroup

- 参数：Damping=0.3 Stiffness=0.05 LimitAngle=20.0 Radius=1.0 HalfWidth=5.0 Shape=EPhyCollisionShape::Spherical
- 球碰撞 1 个：Bip001Head@r6.75
- Alpha 曲线：None

- [头发] Bone_Hair011_R → Bone_Hair012_R
