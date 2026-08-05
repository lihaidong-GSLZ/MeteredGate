# 更新记录

## 0.3.0

- 将玩家建筑原型改为直接继承 `LayoutEntityProto`；
- 直接复用 Flat Connector 的布局、端口、图形、图标和原生高度范围；
- 删除 `ZipperProto` 继承以及 Flat Balancer 数据依赖；
- 删除全部 Harmony 补丁和 `0Harmony.dll` 运行库；
- 新增连续 `20 kW` 用电和游戏原生电力 Priority UI；
- Inspector 写操作改为可回放的 `InputCommand`；
- 新增正式 v1 → v2 存档迁移，兼容 0.1.0/0.2.0 建筑并补建电力 consumer；
- 保留 `MeteredGate` Mod ID、`MeteredGate.dll`、`MeteredGate.MeteredGateMod`、`MeteredGate_Entity` 和复制配置键；
- 加固启停生命周期、反序列化归一化、整数溢出、UI 刷新和轮询索引；
- 构建脚本现在同时生成可直接上传 CoI Hub 的 ZIP。

## 0.2.0

- 新增独立高度范围与局部 Harmony 边界补丁；
- 复用原版 Flat Connector 菜单图标；
- 保持正式模组和建筑原型 ID。

## 0.1.0

- 第一个公开版本；
- 新增 `1×1` 定量平面传送带闸门、周期/配额、单件缓冲、轮询输出、Inspector、保存与复制设置。
