#!/usr/bin/env bash
set -euo pipefail

PROJECT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_FILE="$PROJECT_DIR/MeteredGate.csproj"
COI_ROOT="${COI_ROOT:-$HOME/.local/share/Steam/steamapps/common/Captain of Industry}"
MANAGED_DIR="$COI_ROOT/Captain of Industry_Data/Managed"
OUTPUT_DIR="$PROJECT_DIR/bin/Release/net48"
DIST_ROOT="$PROJECT_DIR/dist"
DIST_DIR="$DIST_ROOT/MeteredGate"
UPLOAD_ZIP="$DIST_ROOT/MeteredGate-0.3.1.zip"

case "${1:-}" in
    "") ;;
    --clean)
        rm -rf "$PROJECT_DIR/bin" "$PROJECT_DIR/obj" "$DIST_ROOT"
        ;;
    *)
        echo "用法：$0 [--clean]" >&2
        exit 2
        ;;
esac

if [[ ! -f "$PROJECT_FILE" ]]; then
    echo "错误：找不到项目文件：$PROJECT_FILE" >&2
    exit 1
fi
if ! command -v dotnet >/dev/null 2>&1; then
    echo "错误：找不到 dotnet 命令。" >&2
    exit 1
fi

required_dlls=(
    Mafi.dll
    Mafi.Core.dll
    Mafi.Base.dll
    Mafi.Unity.dll
    UnityEngine.UIElementsModule.dll
)
for dll in "${required_dlls[@]}"; do
    if [[ ! -f "$MANAGED_DIR/$dll" ]]; then
        echo "错误：找不到游戏程序集：$MANAGED_DIR/$dll" >&2
        echo "当前 COI_ROOT：$COI_ROOT" >&2
        exit 1
    fi
done

python3 "$PROJECT_DIR/tools/static_check.py" "$PROJECT_DIR"

echo "正在编译 Metered Gate 0.3.1……"
echo "项目：$PROJECT_FILE"
echo "游戏目录：$COI_ROOT"

dotnet build "$PROJECT_FILE" \
    --configuration Release \
    --nologo \
    -p:COI_ROOT="$COI_ROOT"

MOD_DLL="$OUTPUT_DIR/MeteredGate.dll"
if [[ ! -f "$MOD_DLL" ]]; then
    echo "错误：编译结束后没有找到 $MOD_DLL" >&2
    exit 1
fi

rm -rf "$DIST_DIR" "$UPLOAD_ZIP"
mkdir -p "$DIST_DIR"
cp "$MOD_DLL" \
   "$PROJECT_DIR/manifest.json" \
   "$PROJECT_DIR/config.json" \
   "$PROJECT_DIR/readme.txt" \
   "$PROJECT_DIR/changelog.txt" \
   "$PROJECT_DIR/LICENSE" \
   "$PROJECT_DIR/THIRD_PARTY_NOTICES.md" \
   "$DIST_DIR/"

# 0.3.1 不分发 Harmony；若旧文件意外进入发行目录则直接失败。
if find "$DIST_DIR" -maxdepth 1 -iname '*Harmony*.dll' -print -quit | grep -q .; then
    echo "错误：发行目录中发现不应存在的 Harmony DLL。" >&2
    exit 1
fi

if command -v zip >/dev/null 2>&1; then
    (
        cd "$DIST_ROOT"
        zip -rq "$(basename "$UPLOAD_ZIP")" MeteredGate
    )
else
    DIST_ROOT="$DIST_ROOT" UPLOAD_ZIP="$UPLOAD_ZIP" python3 - <<'PYZIP'
from pathlib import Path
from zipfile import ZIP_DEFLATED, ZipFile
import os

root = Path(os.environ["DIST_ROOT"])
out = Path(os.environ["UPLOAD_ZIP"])
mod_dir = root / "MeteredGate"
with ZipFile(out, "w", compression=ZIP_DEFLATED) as archive:
    for path in sorted(mod_dir.rglob("*")):
        if path.is_file():
            archive.write(path, path.relative_to(root))
PYZIP
fi

if [[ ! -f "$UPLOAD_ZIP" ]]; then
    echo "错误：没有生成 CoI Hub 上传包：$UPLOAD_ZIP" >&2
    exit 1
fi

echo
echo "编译和发行包整理完成："
echo "  模组目录：$DIST_DIR"
echo "  主 DLL：$MOD_DLL"
echo "  CoI Hub 上传 ZIP：$UPLOAD_ZIP"
echo "  外部补丁运行库：无"
echo
echo "安装前请删除旧的 MeteredGate 目录，避免 0.2.0 的 0Harmony.dll 残留。"
