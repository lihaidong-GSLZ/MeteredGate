#!/usr/bin/env bash
set -euo pipefail

PROJECT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_FILE="$PROJECT_DIR/MeteredGate.csproj"
COI_ROOT="${COI_ROOT:-$HOME/.local/share/Steam/steamapps/common/Captain of Industry}"
MANAGED_DIR="$COI_ROOT/Captain of Industry_Data/Managed"
OUTPUT_DIR="$PROJECT_DIR/bin/Release/net48"
DIST_DIR="$PROJECT_DIR/dist/MeteredGate"

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

case "${1:-}" in
    "") ;;
    --clean)
        rm -rf "$PROJECT_DIR/bin" "$PROJECT_DIR/obj" "$PROJECT_DIR/dist"
        ;;
    *)
        echo "用法：$0 [--clean]" >&2
        exit 2
        ;;
esac

echo "正在编译 Metered Gate 0.2.0……"
echo "项目：$PROJECT_FILE"
echo "游戏目录：$COI_ROOT"

dotnet build "$PROJECT_FILE" \
    --configuration Release \
    --nologo \
    -p:COI_ROOT="$COI_ROOT"

MOD_DLL="$OUTPUT_DIR/MeteredGate.dll"
HARMONY_DLL="$OUTPUT_DIR/0Harmony.dll"

for output in "$MOD_DLL" "$HARMONY_DLL"; do
    if [[ ! -f "$output" ]]; then
        echo "错误：编译结束后没有找到 $output" >&2
        exit 1
    fi
done

rm -rf "$DIST_DIR"
mkdir -p "$DIST_DIR"
cp "$MOD_DLL" "$HARMONY_DLL" \
   "$PROJECT_DIR/manifest.json" \
   "$PROJECT_DIR/config.json" \
   "$PROJECT_DIR/readme.txt" \
   "$PROJECT_DIR/changelog.txt" \
   "$DIST_DIR/"

echo
echo "编译和发行包整理完成："
echo "  模组目录：$DIST_DIR"
echo "  主 DLL：$MOD_DLL"
echo "  补丁运行库：$HARMONY_DLL"
echo
echo "安装前请删除旧的 MeteredGate 目录，再复制新的 dist/MeteredGate。"
