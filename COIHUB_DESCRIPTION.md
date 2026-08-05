# Metered Gate 0.3.1

一个 `1×1`、可架高的定量平面传送带闸门。玩家可以分别设置周期长度与每周期放行数量；未使用配额不会累积，内部最多暂存一个单位。

Metered Gate is an elevatable 1×1 logistics node for flat conveyors. Players can configure cycle duration and items released per cycle. Unused quota does not carry over, and the internal buffer holds at most one item.

## 主要功能 / Features

- 四个可动态配置的平面传送带端口；
- 独立周期与配额、进度显示、多输出轮询；
- 周期可用 `-30 s`、`-10 s`、`-1 s`、`+1 s`、`+10 s`、`+30 s` 按钮快速或精细调整；
- 复用原版 Flat Connector 的布局、模型、图标和高度范围；
- 使用游戏公开的实体添加验证 API 严格拒绝范围外建造；
- 连续消耗 `20 kW`，支持游戏原生电力 Priority；
- 不使用 Harmony，也不捆绑 `0Harmony.dll`；
- 兼容 0.1.0/0.2.0 的 `MeteredGate_Entity` 存档。

- Four dynamically configurable flat-conveyor ports;
- Configurable cycle and quota, progress displays, and round-robin outputs;
- Cycle controls for -30 s, -10 s, -1 s, +1 s, +10 s, and +30 s adjustments;
- Reuses the original Flat Connector layout, model, icon, and height range;
- Enforces the range with the public entity-addition validation API;
- Consumes 20 kW continuously and uses native power priority;
- No Harmony dependency or bundled third-party runtime;
- Migrates existing 0.1.0/0.2.0 saves.

## 安装 / Installation

删除旧的 `MeteredGate` 目录后再安装，避免 0.2.0 的 `0Harmony.dll` 残留。升级存档前建议备份。

Delete the old `MeteredGate` folder before installing so the obsolete `0Harmony.dll` from 0.2.0 is not left behind. Back up saves before upgrading.
