# Metered Gate 0.3.0 代码与程序集审计

审计目标是把 0.2.0 的玩家建筑从 `ZipperProto + Harmony 高度补丁` 重构为直接使用游戏原生布局契约的普通 `LayoutEntityProto`，同时保持正式存档身份不变。

## 审计输入

- Metered Gate 0.2.0 GitHub 主分支；
- 0.3.0 connector-native 源码；
- Captain of Industry 0.8.6 的 `Mafi.dll`、`Mafi.Core.dll`、`Mafi.Base.dll`、`Mafi.Unity.dll`；
- Unity UIElements 相关程序集。

## 结论

在静态源码和 0.8.6 程序集契约范围内，没有遗留的高严重度或中严重度缺陷。0.3.0 不再使用 Harmony，不再继承 `ZipperProto`/`MiniZipperProto`，也不再从 Flat Balancer 读取数据。

由于审计环境没有 .NET SDK，最终 C# 编译、游戏启动和实际存档迁移仍必须在安装了游戏的机器上验证。

## 已确认的核心契约

### 原型与放置

- `MeteredGateProto` 直接继承 `LayoutEntityProto`；
- Flat Connector 只作为 `EntityLayout` 和 `Gfx` 模板；
- `EntityLayout.PlacementHeightRange` 会被原生放置器读取；
- 不继承 `MiniZipperProto`，避免触发 `MiniZipperValidator`、运输带切割、自动连接器生成和蓝图忽略；
- 不再访问 `StaticEntityMassPlacer` 私有字段或私有升降方法。

### 成本与电力

- 建造成本取自一段 Flat Conveyor；
- `EntityCosts` 使用具名参数，明确指定 `workers: 0`；
- 维护成本为空；
- 原型通过 `IProtoWithPowerConsumption` 声明 `20 kW`；
- 实体通过 `IElectricityConsumingEntity` 和官方 `ElectricityConsumerFactory` 接入电力系统；
- `ElectricityConsumer` 观察实体启用状态和通用 Priority，原生 Priority UI 无需自定义补丁。

### 物流端口

- `LayoutEntity` 负责创建和保存布局端口；
- `ReceiveAsMuchAsFromPort` 返回未接收数量；
- `SendAsMuchAs` 返回未发送数量；
- 内部缓冲最多一件；
- 配额在货物离开上游时扣除；
- 多输出使用 round-robin，输出数量变化时索引会归一化；
- 拆除时缓冲货物通过 `AssetTransactionManager` 返还。

### 命令与 UI

- Inspector 不直接修改模拟状态；
- 所有按钮通过 `InputCommand` 和 `ICommandProcessor<T>` 调度；
- 自定义静态 `Deserialize(BlobReader)` 显式使用 `new`，消除 CS0108 并保留游戏序列化约定；
- UI 使用整数触发器，只在状态或可见整秒变化时刷新。

### 存档迁移

0.1.0/0.2.0 使用 v1，字段顺序为：

1. 基类数据；
2. 版本号；
3. 缓冲物品；
4. 剩余配额；
5. 周期相位；
6. 周期秒数；
7. 每周期配额；
8. round-robin 索引。

0.3.0 的 v2 在版本号后增加 `ElectricityConsumer`。读取 v1 时不在实体反序列化函数内立即创建 consumer，而是注册 `InitPriority.Lowest` 的 `RegisterInitAfterLoad` 回调。

实际程序集显示：

- `ElectricityManager` 以 `High` 优先级恢复自身；
- 已保存的 `ElectricityConsumer` 以 `Low` 优先级执行 `initSelf`；
- v1 迁移使用 `Lowest`，因此在电力系统恢复完成后通过官方 factory 注册 consumer。

正式兼容标识保持为：

```text
Mod ID:             MeteredGate
Assembly:           MeteredGate.dll
Primary mod class:  MeteredGate.MeteredGateMod
Prototype ID:       MeteredGate_Entity
Clone keys:         MeteredGate.CycleSeconds
                    MeteredGate.ItemsPerCycle
```

## 防御性处理

- 周期和配额值在读取配置、复制设置和反序列化时夹紧；
- 调整参数使用饱和加法，避免整数溢出；
- 周期相位按合法周期归一化；
- 剩余配额限制在 `[0, itemsPerCycle]`；
- 重新启用或暂停时清除上一 tick 的供电授权；
- 载入后不恢复瞬时 `m_hasPower`，等待下一次模拟更新重新判断。

## 剩余风险

### 低风险：Gfx 内部字段

为了复用 Flat Connector 的工具栏图标和图形，代码仍会浅复制 `LayoutEntityProto.Gfx`，并通过反射重置 `m_proto`、图标路径和 `IconIsCustom`。这些字段已在 0.8.6 程序集中确认，但游戏版本升级后必须复核。

### 原版 UI 行为：Shift 升降越界预览

普通 `LayoutEntityProto` 的 Shift 快速升降可能让游标暂时跳出连接器布局范围。该位置会被原生验证判定为不可建造。为避免重新引入 Harmony 或 `MiniZipperProto` 副作用，0.3.0 不对这个纯预览行为做私有方法补丁。

### 必须运行验证

发布前仍需验证：

- `bash build.bash --clean` 无警告、无错误；
- 0.2.0 v1 存档载入并补建 consumer；
- v2 保存后再次载入；
- 缺电、Priority、暂停、堵塞、拆除、复制设置；
- CoI Hub 扫描结果不再显示 Harmony。
