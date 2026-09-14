#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="$ROOT/LittlegodsDlcInstallerTermux/LittlegodsDlcInstallerTermux.csproj"
OUT="${1:-$ROOT/produccion/termux}"
DOTNET_BIN="${DOTNET_BIN:-dotnet}"
PACKAGE="$OUT/LittlegodsDlcInstallerTermux_Universal"

if ! "$DOTNET_BIN" --version >/dev/null 2>&1 && [[ -x "$HOME/.dotnet/dotnet" ]]; then
  DOTNET_BIN="$HOME/.dotnet/dotnet"
fi

rm -rf "$OUT"
mkdir -p "$PACKAGE"
"$DOTNET_BIN" publish "$PROJECT" -c Release \
  -p:DebugType=None -p:DebugSymbols=false -o "$PACKAGE"

cat > "$PACKAGE/LittlegodsDlcInstallerTermux" <<'EOF'
#!/data/data/com.termux/files/usr/bin/bash
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
exec dotnet "$SCRIPT_DIR/LittlegodsDlcInstallerTermux.dll" "$@"
EOF
chmod +x "$PACKAGE/LittlegodsDlcInstallerTermux"

(cd "$OUT" && zip -qr "LittlegodsDlcInstallerTermux_Universal.zip" "LittlegodsDlcInstallerTermux_Universal")
echo "Portable Termux package created at: $OUT/LittlegodsDlcInstallerTermux_Universal.zip"
