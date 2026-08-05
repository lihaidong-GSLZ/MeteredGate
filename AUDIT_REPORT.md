# Metered Gate 0.3.1 代码与程序集审计

## 审计结论

0.3.0 的高度方案存在一个真实缺陷：`EntityLayout.PlacementHeightRange` 会被放置器读取，但普通 `LayoutEntityProto` 到达边界后仍可通过 `LayoutEntityPreview.CanMoveUpDownIfValid()` 继续升降，而且原生最终添加验证并不会自动检查该范围。因此范围外位置可能真正建成。

0.3.1 保持普通 `LayoutEntityProto`、无 Harmony、无 MiniZipper 类型身份的结构，并新增公开 API 验证器作为最终安全边界。

## 高度验证契约

新增类型：

```text
MeteredGateHeightValidator
  : IEntityAdditionValidator<LayoutEntityAddRequest>
```

注册路径：

```text
DataOnlyMod.RegisterDependencies(...)
DependencyResolverBuilder.RegisterDependency<T>().AsAllInterfaces()
```

实际 CoI 0.8.6 程序集确认：

- `IEntityAdditionValidator<T>.CanAdd(T)` 返回 `EntityValidationResult`；
- `EntityValidators` 从 `AllImplementationsOf<IEntityAdditionValidator>` 收集实现，并根据泛型请求类型调用；
- `LayoutEntityAddRequest` 暴露 `Proto`、`Transform`、`Origin` 与布局数据；
- `TerrainManager[Tile2i].Height.TilesHeightRounded` 与原版预览的估算高度计算一致；
- `HeightTilesI - HeightTilesI` 返回 `ThicknessTilesI`；
- `EntityValidationResult.CreateError` 是正式的拒绝添加路径。

验证器算法：

```text
if request.Proto is not MeteredGateProto:
    Success

position = request.Transform.Position
terrainHeight = terrain[position.Xy].Height.TilesHeightRounded
relativeHeight = position.Height - terrainHeight
allowed = proto.Layout.PlacementHeightRange

if allowed.From <= relativeHeight <= allowed.To:
    Success
else:
    CreateError(...)
```

地图外请求先由原生 `LayoutEntityTerrainValidator` 处理。为兼容“调用全部验证器”的路径，自定义验证器在索引 TerrainManager 之前也检查 `TerrainArea.ContainsTile`。

## 设计取舍

没有重新继承 `MiniZipperProto`，因为该类型还会触发：

- Mini Zipper 专用放置 validator；
- 切开既有运输带；
- 自动连接器生成；
- 蓝图过滤和其他类型特判。

没有恢复 Harmony，因为最终合法性可以通过公开 validator API 完成。预览游标仍可能因 Shift 一次跨界，但范围外请求会变为无效并被拒绝，不能建成。

## 其他核心契约

- `MeteredGateProto` 直接继承 `LayoutEntityProto`；
- Flat Connector 只作为 `EntityLayout` 和 `Gfx` 模板；
- 成本取自 Flat Conveyor，`workers: 0`，维护为空；
- 原型声明 20 kW，实体通过官方 `ElectricityConsumerFactory` 接入电力系统；
- 端口的接收/发送余量语义、单件缓冲、配额扣除和 round-robin 逻辑保持正确；
- Inspector 写操作通过 `InputCommand` 调度；
- 周期 UI 的 `-30/-1/+1/+30` 秒按钮只传递整数 delta，复用现有命令、夹紧和重启周期逻辑，不改变存档格式；
- v1 → v2 consumer 迁移与正式持久化 ID 均保持不变。

## 剩余风险

1. `LayoutEntityProto.Gfx` 仍通过浅复制和内部 owner/icon 字段重绑定，升级游戏版本后必须复核。
2. 玩家可在 0.3.0 中已经建成范围外建筑；0.3.1 不主动删除或移动已有实体，只阻止新的添加/复制/移动请求。
3. 当前环境没有 .NET SDK，最终 C# 编译和游戏内验证必须在安装游戏的机器上完成。

## 发布前必须验证

- `bash build.bash --clean` 零警告零错误；
- 高度上下边界、Shift、复制、蓝图和移动测试；
- 普通建筑不受 validator 影响；
- 0.1.0/0.2.0 存档迁移；
- 20 kW、Priority、暂停、堵塞、拆除和再次载入。
