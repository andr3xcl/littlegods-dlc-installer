#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUT="$ROOT/produccion"
PROJECT="$ROOT/LittlegodsDlcInstallerGui/LittlegodsDlcInstallerGui.csproj"
DOTNET_BIN="${DOTNET_BIN:-dotnet}"

if ! "$DOTNET_BIN" --version >/dev/null 2>&1 && [[ -x "$HOME/.dotnet/dotnet" ]]; then
  DOTNET_BIN="$HOME/.dotnet/dotnet"
fi

mkdir -p "$OUT"
find "$OUT" -maxdepth 1 -type f -delete
rm -rf "$OUT/LittlegodsDlcInstaller.AppDir" "$OUT/LittlegodsDlcInstaller-x86_64.AppImage"

"$DOTNET_BIN" publish "$PROJECT" -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:DebugType=None -p:DebugSymbols=false -o "$OUT"
find "$OUT" -maxdepth 1 -name '*.pdb' -delete

"$ROOT/build-appimage.sh" "$OUT"

cp "$ROOT/Boceto_5.jpg" "$OUT/Littlegods-logo.jpg"
echo "Production artifacts created in: $OUT"
ls -lh "$OUT"/*.exe "$OUT"/*.AppImage "$OUT"/Littlegods-logo.jpg
