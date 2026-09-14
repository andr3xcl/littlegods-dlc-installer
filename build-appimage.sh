#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="$ROOT/LittlegodsDlcInstallerGui/LittlegodsDlcInstallerGui.csproj"
OUT="${1:-$ROOT/produccion}"
APPDIR="$OUT/LittlegodsDlcInstaller.AppDir"
APPIMAGE="$OUT/LittlegodsDlcInstaller-x86_64.AppImage"
DOTNET_BIN="${DOTNET_BIN:-dotnet}"
APPIMAGETOOL_BIN="${APPIMAGETOOL:-appimagetool}"

if ! "$DOTNET_BIN" --version >/dev/null 2>&1 && [[ -x "$HOME/.dotnet/dotnet" ]]; then
  DOTNET_BIN="$HOME/.dotnet/dotnet"
fi

rm -rf "$APPDIR" "$APPIMAGE"
mkdir -p "$APPDIR/usr/bin" "$APPDIR/usr/share/applications"

"$DOTNET_BIN" publish "$PROJECT" -c Release -r linux-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:DebugType=None -p:DebugSymbols=false -o "$APPDIR/usr/bin"

cat > "$APPDIR/littlegods-dlc-installer.desktop" <<'EOF'
[Desktop Entry]
Type=Application
Name=Littlegods DLC Installer
Exec=LittlegodsDlcInstallerGui
Icon=littlegods-dlc-installer
Categories=Game;
Terminal=false
EOF
cp "$ROOT/littlegods-dlc-installer.png" "$APPDIR/littlegods-dlc-installer.png"
cp "$ROOT/LittlegodsDlcInstallerGui/LittlegodsDlcInstaller.png" "$APPDIR/usr/bin/LittlegodsDlcInstaller.png"
cp "$APPDIR/littlegods-dlc-installer.desktop" "$APPDIR/usr/share/applications/"
cat > "$APPDIR/AppRun" <<'EOF'
#!/usr/bin/env bash
HERE="$(cd "$(dirname "$0")" && pwd)"
exec "$HERE/usr/bin/LittlegodsDlcInstallerGui" "$@"
EOF
chmod +x "$APPDIR/AppRun" "$APPDIR/usr/bin/LittlegodsDlcInstallerGui"

if ! command -v "$APPIMAGETOOL_BIN" >/dev/null 2>&1 && [[ ! -x "$APPIMAGETOOL_BIN" ]]; then
  echo "appimagetool is required to create the final AppImage." >&2
  echo "Install it, then rerun: $0 $OUT" >&2
  exit 2
fi

"$APPIMAGETOOL_BIN" "$APPDIR" "$APPIMAGE"
echo "AppImage created: $APPIMAGE"
