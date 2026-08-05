# GitHub 0.3.0 → 0.3.1 更新说明

0.3.1 是针对 0.3.0 高度限制失效的修复版本，并在最终发布源码中合并了周期读秒的精细/快速调整按钮。核心高度改动是新增：

```text
src/MeteredGateHeightValidator.cs
```

验证器使用公开的：

```text
IEntityAdditionValidator<LayoutEntityAddRequest>
```

对 Metered Gate 的最终添加请求执行高度检查。它不恢复 Harmony，也不让 Proto 继承 `MiniZipperProto`。

推荐应用本次交付的 unified diff：

```bash
cd /path/to/MeteredGate
git apply --check /path/to/MeteredGate-0.3.0-to-0.3.1-final.patch
git apply /path/to/MeteredGate-0.3.0-to-0.3.1-final.patch
bash build.bash --clean
```

随后检查：

```bash
git diff --check
git status --short
```

最终 0.3.1 源码还包含 Inspector 的 `-30 s`、`-1 s`、`+1 s`、`+30 s` 周期控制。使用本次重新生成的 0.3.0 → 0.3.1 补丁即可一次性取得高度验证器与 UI 调整。补丁不会修改 `.git`，也不会执行提交和推送。

如果仓库已经应用过早先的 0.3.1 高度补丁，只需改用 `MeteredGate-0.3.1-cycle-controls-only.patch` 增量补上 UI 调整。
