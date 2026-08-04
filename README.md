# Metered Gate

- 作者：lihaidong
- 源代码：[GitHub 仓库](https://github.com/lihaidong-GSLZ/MeteredGate)

Metered Gate 是一个用于 **Captain of Industry** 的定量物流闸门模组。

它提供一座可架高的 `1×1` 平面传送带建筑。玩家可以设置一个游戏周期以及每个周期允许离开上游的货物数量。它适合需要严格限制批次流量的场景，例如控制核废料从屏蔽储存设施中移出的数量。

## 功能

- `1×1` 建筑，占地与原版 Flat Connector 相近。
- 复用原版 Flat Connector 的模型，不需要额外 AssetBundle。
- 四个可动态配置的平面传送带输入/输出端口。
- 可像原版平面物流连接器一样架高。
- 每栋建筑可以独立调整周期长度。
- 每栋建筑可以独立调整每周期放行数量。
- 未使用的配额不会跨周期累积。
- 内部传输缓冲严格限制为一个单位。
- 物品进入闸门时立即消耗配额。
- 多个输出之间采用简单轮询。
- Inspector 显示周期读条、剩余配额读条和当前状态。
- 支持保存、载入以及复制建筑设置。

## 周期语义

本模组使用游戏原生的 `Mafi.Duration`：

- `Duration.FromSec(...)` 定义周期长度；
- `Duration.OneTick` 推进一个模拟 tick；
- 游戏暂停时周期停止；
- 游戏加速时周期随模拟时间加速；
- 单独暂停建筑时，周期和物流都冻结。

周期结束时，剩余配额被**重置**为设定值，而不是在旧值上累加。因此没有用完的额度不会形成后续爆发。

## 安装

将发行 ZIP 解压到 Captain of Industry 的 `Mods` 目录，最终目录结构应为：

```text
Mods/
└── MeteredGate/
    ├── MeteredGate.dll
    ├── manifest.json
    ├── config.json
    ├── readme.txt
    └── changelog.txt
```

## 从源码编译

### 前置条件

- 已安装 Captain of Industry；
- .NET 8 SDK 或更新的兼容 SDK；
- 能访问游戏安装目录中的以下程序集：
  - `Mafi.dll`
  - `Mafi.Core.dll`
  - `Mafi.Base.dll`
  - `Mafi.Unity.dll`
  - `UnityEngine.UIElementsModule.dll`

`COI_ROOT` 必须指向游戏根目录，而不是 `Managed` 目录。例如：

```text
Steam/steamapps/common/Captain of Industry
```

### Release 编译

在仓库根目录执行：

```bash
dotnet build MeteredGate.csproj \
  --configuration Release \
  -p:COI_ROOT="$COI_ROOT"
```

也可以直接把路径写在命令中：

```bash
dotnet build MeteredGate.csproj \
  --configuration Release \
  -p:COI_ROOT="$HOME/.local/share/Steam/steamapps/common/Captain of Industry"
```

编译产物位于：

```text
bin/Release/net48/
```

本工程没有自动部署 Target，也不包含编译或部署脚本。编译不会修改游戏的 Mods 目录。

## 配置

`config.json` 只决定**新建建筑**的默认值：

- `default_cycle_seconds`：默认周期秒数；
- `default_items_per_cycle`：默认每周期放行数量。

已经建成的建筑将自己的设置保存在存档中，不会因为全局默认值变化而自动修改。

## 存档兼容性

- 可以把本模组加入现有存档；
- 存档中存在 Metered Gate 建筑时，不应移除本模组；
- 建议首次使用和版本更新前备份存档。

`0.1.0` 是第一个公开版本。此前本地测试包使用过更高的临时版本号，但未公开发布；本模组内部存档格式仍为第 1 版。使用旧测试存档载入 `0.1.0` 前仍建议备份，因为 manifest 版本号会表现为一次“降级”。

## 已知限制

- 仅处理平面传送带使用的单位货物；
- 不提供原版 Balancer 的输入/输出优先级和均匀分配选项；
- 多个输入共享一个缓冲和一份总配额；
- 当前版本不消耗电力和维护；
- 图形复制依赖对游戏内部 `m_proto` 字段的反射，游戏更新后可能需要适配；
- 只验证到 Captain of Industry `0.8.6c`。

## 安全扫描说明

代码中的反射只用于复制原版 Flat Connector 的图形配置，并清除复制对象中的旧原型所有者引用。本模组不使用 Harmony，不访问网络，也不读写任意外部文件。

## 许可证

本项目采用 Captain of Industry Open License (COI-Open)，见 [`LICENSE`](LICENSE)。
