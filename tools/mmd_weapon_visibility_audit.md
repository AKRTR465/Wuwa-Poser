# 当前版本武器/道具隐藏接口审计

本次只做离线 PE、已保存 fresh recon 和代码审计，未读取实时进程或调用游戏函数。游戏 SHA 为 c4956c30be531f3b992eabe42fa92fa9d10186000fe12e7546a11e1179bdac18，runtime 分析 PE SHA 为7480ba1b3fa5efa319c12f29890c949eafa36cf7dd932602774f111a8dcffea5。

机器证据保存在 tools/mmd_outputs/weapon_visibility_audit.json；供后端重新核对的紧凑候选契约在 weapon_visibility_contract.json。tools/mmd_weapon_visibility_audit.py 从 NameUTF8 descriptor、property table、class params、注册函数/名字逐步证明拥有者，未套用 stock 偏移。

## 可用于下一步只读实例审查的当前字段

| 类 | 字段 | 当前静态证据 |
|---|---|---|
| SceneComponent | AttachParent | +0xd0；Object descriptor；GetAttachParent exec RVA0x4d0ec30直接读取this+d0 |
| SceneComponent | AttachSocketName | +0xd8；Name descriptor；本游戏 FName 为12B |
| SceneComponent | AttachChildren | +0xe8 TArray；native SetHiddenInGame读data+e8/count+f0 |
| SceneComponent | bVisible | +0x16e mask0x20；SetBitFunc RVA0x59179a0：OR [rcx+16e],20 |
| SceneComponent | bHiddenInGame | +0x16f mask0x10；SetBitFunc RVA0x59179f0：OR [rcx+16f],10；native隐藏接口再证实 |
| ActorComponent | owning actor | GetOwner exec RVA0x5772c00直接读this+0xa8，返回raw actor指针；非weak布局猜测 |
| StaticMeshComponent | StaticMesh资产 | +0x5e0；Object descriptor拥有者注册名证实 |
| Actor | Owner / RootComponent | +0x120 / +0x1a0；Object descriptor；Owner GetOwner exec RVA0x576a480直接读this+120 |
| Actor | bHidden | +0x60 mask0x20；SetBitFunc RVA0x57644f0；不能在受控Pawn上隐藏以代替武器 |

## Native 隐藏接口

SceneComponent.SetHiddenInGame 的 exec wrapper RVA0x591e6e0（207B）调用 native RVA0x5009450（492B，全 .pdata 范围一致），函数SHA256为6abff1cfdb8797db1b0ee15d87043041ce8e8a94b28e8d38d8d9af1016307503。

候选声明：void (*)(void* sceneComponent, bool hidden, uint8_t propagation)，Windows x64 RCX=this，DL=hidden(0/1)，R8B=propagation。实际手动调用仍未验证，后端须先匹配本游戏SHA和函数完整字节。

- enum0：不递归。若本组件hidden位改变，修改+16f bit10后调用虚函数byte slot+0x4d0通知。
- enum1：DirtyOnly。变化时遍历attached descendants并dirty，保留后代hidden位。
- enum2：Propagate。对子孙调用同native(enum0)修改hidden并dirty。

wrapper将第二个传播bool用 setne r8b;inc r8b，false映射1，true映射2。不能把wrapper的bool语义直接当native第三参数，也不能调用reflection SetBitFunc充当可见性接口。建议后端对每个已证明武器mesh逐实例使用enum0，保存/恢复各自原hidden位，避免隐藏身体或丢失原后代可见状态。

## 现有 recon 对实例的证明

analysis/mmd_player_20261018/fresh_scan_1715/logs/player_relations.json 显示当前受控 Pawn=0x10eaad31a0，BP_CharacterController_C 双向Pawn和AcknowledgedPawn均一致。recon保留的该Pawn mesh只有两项：

- CharacterMesh0，地址0x10d3fa1780，index361319，serial54616，388骨。
- OtherCase1，地址0x10d9911780，index361329，serial54625，115骨。

这两个名字不足以确定武器。扫描统计虽然objecttable397,985槽遍历完整，但matching_mesh_objects891中705因没有可信high-bone-array而被筛除，只保留141个高骨数skeletal候选；StaticMesh与低骨数武器未完整输出。因此该文件不能证明当前Pawn仅有两个组件，更不能因为OtherCase1不是CharacterMesh0就自动隐藏。

## 后端只读选择、隐藏与恢复要求

1. 从当前fresh GObjects和Kuro NamePool全对象表枚举Scene/Static/Skeletal组件，不用骨数组count作为保留门槛。记录index/serial/class/outer/owner+0xa8、asset身份与路径、当前hidden/visible、AttachParent+0xd0和socket+0xd8。
2. 同Pawn owned组件由owner+0xa8、outer链互证；独立weapon actor的mesh不能仅看Outer。递归AttachParent链（有界、防循环、每个节点identity重查）必须回到当前受控Pawn的已确认component。Actor.Owner+120可作额外关系证据，不能替代attachment。
3. 输出当前Pawn/attached完整组件清单，根据weapon资产/实例名称、手/背socket及明确关联选出武器。排除CharacterMesh0、身体衣发/脸、无关联scene物体、召唤分身。OtherCase1需asset+attachment证据后才决定。
4. 游戏线程改变已确认实例可见性；保存原hidden bool并以enum0调用native，写后读回、重查身份、记录结果。播放中原生武器写者可能重新显示，需要协调/重审，不能仅一次隐藏后宣布成功。
5. pause/stop/失效/结束的恢复策略需明确：只恢复曾修改且仍同index/serial/class/attachment的实例，尊重游戏后来改变的状态；恢复失败保留pending，不能复用旧指针强写。后端状态/视觉共同确认武器消失且角色/衣发完整才算验收。

待机基线属于骨姿独立问题：隐藏武器不会消除原生拿武器手臂/待机动画。全身动作应在已验证静止/参考局部姿基础上retarget，未映射骨的native secondary保留由后端具体设计；不能把武器骨scale0或全树清零当隐藏实现。

2026-10-18 静态检查通过：PE摘要固定；AttachParent/AttachChildren/StaticMesh descriptor拥有者与偏移一致；两个Bool setter与mask一致；492B native witness；受控Pawn已有mesh2项。工具仅磁盘分析，不构成运行时实例或手动API调用验证。

## 后续当前实例只读证据

Root独立carrier已生成 accessory_scan_1756/weapons_readonly.json，终态captured，遍历398,364槽、27related组件、0identity变化、0target写。确认 WeaponCase0→R5Sword507Md20001/WeaponProp04、WeaponCase1→R5Scabbard507Md20001/WeaponProp03、OtherCase1→EAimisiWeaponMd10011/WeaponProp02，三者owned Pawn且AttachParent回到CharacterMesh0。因此OtherCase1从早期未知候选提升为已证实武器实例。OtherCase0→EParagliderMd10011/WeaponProp01是滑翔道具，按用户范围独立列为optional_prop并默认隐藏，不标武器。tools/mmd_outputs/weapon_runtime_selection.json 提供weapons3、optional_props1、default_hide_targets4；7个effects只候选。四项在本次采样原hidden已true，激活播放时须重新保存原值并协调原生显示写者，不能仅凭本次true声明武器干扰已解决。

