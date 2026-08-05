# Metered Gate

- 作者：lihaidong
- 版本：0.3.0
- 游戏版本：Captain of Industry 0.8.6–0.8.6c
- 源代码：https://github.com/lihaidong-GSLZ/MeteredGate

Metered Gate 是一个 `1×1` 的定量平面传送带闸门。玩家可以为每栋建筑分别设置周期长度和每周期放行数量，适合严格限制批次流量的物流场景。

## 功能

- 四个可动态配置的平面传送带输入/输出端口；
- 每栋建筑独立设置周期和配额；
- 未使用配额不会跨周期累积；
- 内部只缓存一个单位，堵塞时不会继续从上游抽取；
- 多个输出之间采用轮询；
- 消耗 `20 kW`，使用游戏原生电力 Priority UI；
- 支持暂停、保存、载入和复制设置；
- 兼容 0.1.0/0.2.0 的正式 `MeteredGate_Entity` 存档。

## 0.3.0 架构

0.3.0 的自定义原型直接继承 `LayoutEntityProto`。模组只从原版 Flat Connector 复用：

- `EntityLayout`：占地、四向动态端口和 `PlacementHeightRange`；
- `Gfx`：模型及工具栏图标。

它**不**继承 `ZipperProto` 或 `MiniZipperProto`。后者不仅影响高度，还会触发运输带切割、Mini Zipper 放置验证和蓝图忽略等内部语义，因此不适合作为玩家建筑基类。

0.3.0 同时删除了 Flat Balancer 数据依赖、Harmony 补丁和 `0Harmony.dll`。高度合法性由连接器布局和游戏原生放置验证处理。

## 周期语义

- 新建筑先等待一个完整周期，首次刷新后才获得配额；
- 配额在货物离开上游并进入一件缓冲时扣除；
- 周期结束时配额重置为设定值，而不是累加；
- 修改周期会关闭当前周期并从零重新计时；
- 提高配额不会在当前周期补发，降低配额会立即截断剩余额度；
- 缺电或暂停时周期与物流都冻结；
- Restart 重新关闭周期，但不会删除已经进入缓冲的货物。

## 存档兼容性

正式身份保持不变：

```text
Mod ID:             MeteredGate
Assembly:           MeteredGate.dll
Primary mod class:  MeteredGate.MeteredGateMod
Prototype ID:       MeteredGate_Entity
Config clone keys:  MeteredGate.CycleSeconds
                    MeteredGate.ItemsPerCycle
```

0.1.0/0.2.0 使用实体存档格式 v1。0.3.0 在对象图载入结束后通过官方 `ElectricityConsumerFactory` 创建新的 consumer，再以 v2 格式保存，因此旧建筑可以获得新增的 20 kW 电力行为。

升级前仍建议备份存档。存档中存在 Metered Gate 建筑时不要移除模组。

## 安装

先删除旧的 `Mods/MeteredGate` 目录，避免 0.2.0 的 `0Harmony.dll` 残留，再解压新版本。最终结构：

```text
Mods/
└── MeteredGate/
    ├── MeteredGate.dll
    ├── manifest.json
    ├── config.json
    ├── readme.txt
    ├── changelog.txt
    ├── LICENSE
    └── THIRD_PARTY_NOTICES.md
```

## 从源码构建

需要已安装 Captain of Industry 和兼容的 .NET SDK。`COI_ROOT` 指向游戏根目录：

```bash
bash build.bash --clean
```

脚本会生成：

```text
dist/MeteredGate/
dist/MeteredGate-0.3.0.zip
```

后者可直接上传 CoI Hub。

## 已知限制

- 只处理平面传送带的单位货物；
- 多个输入共享一件缓冲和一份总配额；
- 不提供原版 Balancer 的优先输入、优先输出或均匀分配选项；
- Shift 快速升降可能让普通 `LayoutEntityProto` 的预览游标暂时跳到连接器范围外，但该位置会被游戏判定为无效，不能建造；
- Gfx 复制仍依赖 CoI 0.8.6 的内部 owner/icon 字段，游戏更新后需要复核。

## 许可证

本项目采用 Captain of Industry Open License (COI-Open)，见 `LICENSE`。
