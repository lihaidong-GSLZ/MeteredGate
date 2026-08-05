# 更新记录

## 0.3.1

- 修复 `DataOnlyMod.RegisterDependencies` 不能被 override 导致的编译错误；改为显式重新实现 `IMod.RegisterDependencies`；
- Inspector 的周期读秒控制提供 `-30 s`、`-10 s`、`-1 s`、`+1 s`、`+10 s`、`+30 s` 六档，兼顾精细、中速与快速调整；
- 修复 0.3.0 中高度限制实际未被最终建造验证强制执行的问题；
- 新增基于公开 API 的 `MeteredGateHeightValidator`；
- 验证器复现原版预览的相对地形高度计算，并严格检查 Flat Connector 的 `PlacementHeightRange`；
- 范围外的单体放置、Shift 越界、复制、蓝图和移动请求现在会被标记为无效并拒绝提交；
- 保持无 Harmony、无放置器私有字段/方法补丁的架构；
- 修正文档中“原生验证会自动拒绝普通 Proto 范围外位置”的错误说明。

## 0.3.0

- 将玩家建筑原型改为直接继承 `LayoutEntityProto`；
- 复用 Flat Connector 的布局、端口、图形、图标和高度范围数据；
- 删除 `ZipperProto` 继承以及 Flat Balancer 数据依赖；
- 删除全部 Harmony 补丁和 `0Harmony.dll` 运行库；
- 新增连续 `20 kW` 用电和游戏原生电力 Priority UI；
- Inspector 写操作改为可回放的 `InputCommand`；
- 新增正式 v1 → v2 存档迁移，兼容 0.1.0/0.2.0 建筑并补建电力 consumer；
- 保留正式 Mod、程序集、入口类、原型和复制配置键。

## 0.2.0

- 新增独立高度范围与局部 Harmony 边界补丁；
- 复用原版 Flat Connector 菜单图标；
- 保持正式模组和建筑原型 ID。

## 0.1.0

- 第一个公开版本；
- 新增 `1×1` 定量平面传送带闸门、周期/配额、单件缓冲、轮询输出、Inspector、保存与复制设置。
