# Metered Gate 0.2.0 发布流程

## 1. 编译并整理安装目录

```bash
bash build.bash --clean
```

首次恢复 `Lib.Harmony` 时需要访问 NuGet。编译成功后，可安装目录位于：

```text
dist/MeteredGate/
```

其中必须包含：

```text
0Harmony.dll
MeteredGate.dll
manifest.json
config.json
readme.txt
changelog.txt
```

## 2. 本地测试

安装前删除旧的 `MeteredGate` 目录，再复制新的完整目录。至少验证：

- 0.1.0 存档可以载入；
- 已有 Metered Gate 的周期与配额设置保持不变；
- 普通升降在最低和最高高度停止；
- Shift 快速升降不会越界；
- 菜单图标与 Flat Connector 一致；
- 输入、输出、单件缓冲、轮询与复制设置正常。

## 3. 制作发行 ZIP

在 `dist` 目录中执行：

```bash
zip -r MeteredGate-0.2.0.zip MeteredGate
```

发行 ZIP 的根目录必须是 `MeteredGate/`，不能直接把 DLL 放在 ZIP 根目录。

## 4. Git 标签

```bash
git add .
git commit -m "Release Metered Gate 0.2.0"
git tag -a v0.2.0 -m "Metered Gate 0.2.0"
git push
git push origin v0.2.0
```

GitHub Release 正文可使用 `GITHUB_RELEASE_NOTES.md`；CoIHub 介绍可使用 `COIHUB_DESCRIPTION.md`。
