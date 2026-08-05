# Metered Gate

- 作者：lihaidong
- 版本：0.2.0
- 源代码：[GitHub 仓库](https://github.com/lihaidong-GSLZ/MeteredGate)

Metered Gate 是一个用于 **Captain of Industry** 的定量物流闸门模组。

它提供一座可架高的 `1×1` 平面传送带建筑。玩家可以设置一个游戏周期以及每个周期允许离开上游的货物数量。它适合需要严格限制批次流量的场景，例如控制核废料从屏蔽储存设施中移出的数量。

## 功能

- `1×1` 建筑，占地与原版 Flat Connector 相近；
- 复用原版 Flat Connector 的模型和菜单图标，不需要额外 AssetBundle；
- 四个可动态配置的平面传送带输入/输出端口；
- 可架高，并通过独立高度策略限制在运输支柱支持的范围内；
- 普通升降和 Shift 快速升降都会在边界处硬夹紧；
- 每栋建筑可以独立调整周期长度和每周期放行数量；
- 未使用的配额不会跨周期累积；
- 内部传输缓冲严格限制为一个单位；
- 物品进入闸门时立即消耗配额；
- 多个输出之间采用简单轮询；
- Inspector 显示周期读条、剩余配额读条和当前状态；
- 支持保存、载入以及复制建筑设置。

## 0.2.0 的主要变化

- 不再从 Flat Balancer 继承 `CanBeElevated`；Balancer 只作为建造成本模板；
- Flat Connector 只作为 `1×1` 布局、动态端口和图形模板；
- 高度范围由 Metered Gate 自己定义，并通过局部 Harmony 补丁执行边界限制；
- 复用原版 Flat Connector 的菜单栏图标；
- 保持正式原型 ID `MeteredGate_Entity` 不变，兼容 0.1.0 存档中的建筑。

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
    ├── 0Harmony.dll
    ├── MeteredGate.dll
    ├── manifest.json
    ├── config.json
    ├── readme.txt
    └── changelog.txt
```

`0Harmony.dll` 是 0.2.0 的必要运行库，不能从安装包中删除。安装新版本前建议删除旧的 `MeteredGate` 目录，再复制完整的新目录，避免残留文件。

## 从源码编译

### 前置条件

- 已安装 Captain of Industry；
- .NET 8 SDK 或更新的兼容 SDK；
- 能访问游戏安装目录中的 `Mafi.dll`、`Mafi.Core.dll`、`Mafi.Base.dll`、`Mafi.Unity.dll` 和 `UnityEngine.UIElementsModule.dll`；
- 首次还原 NuGet 包时需要能够下载 `Lib.Harmony`。

`COI_ROOT` 必须指向游戏根目录，而不是 `Managed` 目录。

执行：

```bash
bash build.bash --clean
```

也可以直接调用：

```bash
dotnet build MeteredGate.csproj \
  --configuration Release \
  -p:COI_ROOT="$COI_ROOT"
```

`build.bash` 会将可安装目录整理到：

```text
dist/MeteredGate/
```

## 配置

`config.json` 只决定**新建建筑**的默认值：

- `default_cycle_seconds`：默认周期秒数；
- `default_items_per_cycle`：默认每周期放行数量。

已经建成的建筑会把自己的设置保存在存档中，不会因全局默认值变化而自动修改。

## 存档兼容性

- 0.2.0 保持模组 ID `MeteredGate` 和建筑原型 ID `MeteredGate_Entity` 不变；
- 可以直接用于包含 0.1.0 Metered Gate 建筑的存档；
- 可以把本模组加入现有存档；
- 存档中仍有 Metered Gate 建筑时，不应移除本模组；
- 首次升级到 0.2.0 前建议备份存档。

## 已知限制

- 仅处理平面传送带使用的单位货物；
- 不提供原版 Balancer 的输入/输出优先级和均匀分配选项；
- 多个输入共享一个缓冲和一份总配额；
- 当前版本不消耗电力和维护；
- 高度补丁依赖 Captain of Industry 0.8.6c 的放置器方法和私有字段名称；
- 图形与图标复制依赖 `LayoutEntityProto.Gfx` 的内部字段；游戏更新后可能需要适配；
- 只验证到 Captain of Industry `0.8.6c`。

## 运行时补丁说明

高度边界使用 Harmony 对游戏放置器做局部补丁。补丁只在当前放置原型为 `MeteredGateProto` 时生效：

- 开始放置时设置独立允许高度范围；
- 到达边界时关闭继续越界移动的后备分支；
- 普通和 Shift 快速升降后执行硬夹紧。

反射只用于复制原版 Flat Connector 的图形、固定菜单图标路径，并清除复制对象中的旧原型所有者引用。模组不访问网络，也不读写任意外部文件。第三方运行库信息见 [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md)。

## 许可证

本项目采用 Captain of Industry Open License (COI-Open)，见 [`LICENSE`](LICENSE)。Harmony 使用 MIT License，见第三方声明。
