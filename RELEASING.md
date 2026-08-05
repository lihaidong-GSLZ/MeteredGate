# Metered Gate 0.3.1 发布流程

## 1. 准备回归存档

至少准备两份备份：

- 一份包含 0.1.0/0.2.0 Metered Gate 的 v1 存档，用于测试 v1 → v2 电力 consumer 迁移；
- 一份曾用 0.3.0 测试过高度的存档，用于确认已有范围外建筑不会导致载入异常。

0.3.1 不会自动移动或删除 0.3.0 已经建成的范围外建筑；它只阻止新的放置、复制、蓝图和移动请求。

## 2. 干净构建

```bash
bash build.bash --clean
```

构建必须满足：

- 0 个错误；
- 0 个警告；
- 静态检查全部通过；
- `dist/MeteredGate/` 中没有 `0Harmony.dll`；
- 自动生成 `dist/MeteredGate-0.3.1.zip`；
- ZIP 根目录是 `MeteredGate/`。

预期发行目录：

```text
MeteredGate/
├── MeteredGate.dll
├── manifest.json
├── config.json
├── readme.txt
├── changelog.txt
├── LICENSE
└── THIRD_PARTY_NOTICES.md
```

## 3. 游戏内回归测试

完整执行 `TEST_PLAN.md`。高度测试至少覆盖：

- 范围上下边界；
- 普通升降与 Shift 快速升降；
- 单栋复制和多选复制；
- 蓝图放置；
- 移动已有建筑；
- 普通非 Metered Gate 建筑不受影响。

游标可以暂时越过连接器范围，但范围外预览必须显示无效，并且不能提交建造。

周期 UI 至少覆盖四个按钮的单击、连续点击、1 秒下限、3600 秒上限，以及修改周期后重新从零计时。

## 4. 更新 GitHub

在仓库中应用本次 `0.3.0 → 0.3.1` 补丁，随后检查：

```bash
git status --short
git diff --check
git diff --stat
git diff
```

确认新增：

```text
src/MeteredGateHeightValidator.cs
COIHUB_CHANGELOG_0.3.1.txt
```

不要提交：

```text
bin/
obj/
dist/
0Harmony.dll
游戏程序集
测试存档
```

提交和标签：

```bash
git add -A
git commit -m "Release Metered Gate 0.3.1"
git tag -a v0.3.1 -m "Metered Gate 0.3.1"
git push origin main
git push origin v0.3.1
```

GitHub Release：

- tag：`v0.3.1`；
- title：`Metered Gate 0.3.1`；
- 正文：`GITHUB_RELEASE_NOTES.md`；
- 附件：`dist/MeteredGate-0.3.1.zip`。

## 5. 更新 CoI Hub

在原有 Metered Gate 页面新增 `0.3.1` 版本，不要创建新的 Mod 页面或更改 Mod ID。

上传：

```text
dist/MeteredGate-0.3.1.zip
```

页面字段：

- 游戏版本：`0.8.6` – `0.8.6c`；
- Save-game：Add ✓，Remove ✗；
- License：CoI-Open；
- Source code：GitHub 仓库；
- 介绍：`COIHUB_DESCRIPTION.md`；
- Changelog：`COIHUB_CHANGELOG_0.3.1.txt`。

发布后重新下载 Hub 文件，核对 ZIP 结构、版本号和 SHA-256；再用下载版本完成一次高度越界测试。
