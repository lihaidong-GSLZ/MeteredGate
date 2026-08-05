# Metered Gate 0.3.0 发布流程

## 1. 保留测试存档

升级测试前复制一份包含 0.2.0 Metered Gate 建筑的存档。不要只用新建存档测试，因为 0.3.0 增加了 v1 → v2 电力 consumer 迁移。

## 2. 干净构建

```bash
bash build.bash --clean
```

构建必须满足：

- 0 个错误；
- 0 个警告；
- `dist/MeteredGate/` 中没有 `0Harmony.dll`；
- 自动生成 `dist/MeteredGate-0.3.0.zip`；
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

按 `TEST_PLAN.md` 完成新建筑、电力、物流、存档迁移、复制和拆除测试。尤其确认日志中存在 v1 迁移信息，且没有序列化、命令处理器或原型注册错误。

## 4. 更新 GitHub

使用本次交付的 GitHub 更新包覆盖仓库工作树，随后检查差异：

```bash
git status --short
git diff --check
git diff --stat
git diff
```

确认删除 `src/MeteredGateHeightPolicy.cs`，并确认没有提交：

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
git commit -m "Release Metered Gate 0.3.0"
git tag -a v0.3.0 -m "Metered Gate 0.3.0"
git push origin main
git push origin v0.3.0
```

GitHub Release：

- tag：`v0.3.0`；
- title：`Metered Gate 0.3.0`；
- 正文：`GITHUB_RELEASE_NOTES.md`；
- 附件：`dist/MeteredGate-0.3.0.zip`。

## 5. 更新 CoI Hub

在原有 Metered Gate 页面新增 `0.3.0` 版本，不要新建另一个 Mod ID。

上传：

```text
dist/MeteredGate-0.3.0.zip
```

页面字段：

- 游戏版本：`0.8.6` – `0.8.6c`；
- Save-game：Add ✓，Remove ✗；
- License：CoI-Open；
- Source code：GitHub 仓库；
- 介绍：`COIHUB_DESCRIPTION.md`；
- Changelog：`COIHUB_CHANGELOG_0.3.0.txt`。

发布后下载一次 Hub 上的文件，核对 ZIP 结构、版本号与 SHA-256，并查看代码扫描是否已去掉 Harmony 标记。
