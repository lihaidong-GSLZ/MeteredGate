# Metered Gate 0.2.0

0.2.0 重新实现了建筑的高度限制，同时保持正式模组和建筑原型 ID 不变，可继续读取 0.1.0 的 Metered Gate 建筑。

## 主要变化

- Flat Balancer 现在只提供建造成本，不再提供 `CanBeElevated`；
- Metered Gate 继续使用正常的 `ZipperProto` 玩家建筑结构；
- 新增独立高度范围和局部 Harmony 边界补丁；
- 普通升降与 Shift 快速升降都会夹紧到允许范围；
- 菜单图标直接复用原版 Flat Connector；
- 周期、配额、单件缓冲、轮询输出和存档格式保持不变。

## 安装

下载 `MeteredGate-0.2.0.zip`，删除旧的 `MeteredGate` 模组目录，然后将压缩包中的完整 `MeteredGate` 文件夹复制到 Mods 目录。

安装目录必须包含：

```text
MeteredGate.dll
0Harmony.dll
manifest.json
config.json
readme.txt
changelog.txt
```

## 兼容性

已验证 Captain of Industry `0.8.6c`。升级前建议备份存档。
