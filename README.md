# Littlegods DLC5 Installer

A simple installer for **Call of Duty: Black Ops II Zombies DLC5**, with a graphical interface for **Windows and Linux** and a console client for **Termux/Android**.

<img width="1919" height="1078" alt="image" src="https://github.com/user-attachments/assets/d07f66a5-7e28-4803-b1c1-82372c597141" />
<img width="1919" height="1079" alt="image" src="https://github.com/user-attachments/assets/7091cbf5-1cbb-45ed-8732-9b44a7269333" />


## Features

- Simple graphical installer for Call of Duty: Black Ops II Zombies DLC5.
- Compatible with Windows and Linux.
- Termux/Android portable console client for ARM64, ARMv7, x86_64, and x86 devices.
- Graphical interface built with Avalonia and .NET 8.
- Support for Spanish, English, and Portuguese.
- Light, dark, and system themes.
- Automatic installation using pre-packaged DLC5 files.
- No need to manually move or copy files.
- Files are installed directly into the Plutonium `AppData` storage directory.
- Keeps the base game installation untouched.
- Manual folder selection on Linux.
- Mandatory validation of the `Plutonium/storage/t6` path.
- Installation, repair, and uninstallation through a manifest.
- Manifest preview before downloading.
- Dedicated BETA1 manifest viewer with the exact Plutonium and base-game paths.
- BETA1 migration flow inside Install: remove only files listed in the BETA1 manifest, then install BETA2. This release adds the ability to delete BETA1 files safely before migration.
- Base-game folder selection is required before removing BETA1 files.
- Repair and uninstall are enabled only when files from the current manifest are detected in Plutonium.
- Real-time progress bar showing completed files and the current file.
- Safe cancellation with cleanup of files downloaded during the operation.
- Littlegods logo integrated into the application.
- Discord: `discord.littlegod.space`

## How It Works

The DLC5 files are distributed in a pre-packaged format and are installed directly into the user's Plutonium storage directory.

There is no need to manually extract files, move them into the base game directory, or modify the original Call of Duty: Black Ops II installation.

The installer automatically handles the required files and places them inside:

```text
Plutonium/storage/t6
```

This allows the DLC5 installation to remain separate from the base game while working directly from the Plutonium storage environment.

## Valid Paths

The selected folder must contain these segments, regardless of what path comes before them:

```text
Plutonium/storage/t6
```

### Linux

```text
/home/user/.local/share/Steam/steamapps/compatdata/.../Plutonium/storage/t6
```

### Windows

```text
C:\Users\User\AppData\Local\Plutonium\storage\t6
```

## Termux / Android

Termux uses the console client because Avalonia's desktop window is not available in a standard Termux session. The Termux client reuses the same manifest, download, install, repair, uninstall, cancellation, and cleanup logic.

### Install requirements

```bash
pkg update
pkg install dotnet-runtime-8.0
termux-setup-storage
```

Grant storage access when Android asks for permission. A typical Android path is:

```text
/sdcard/Plutonium/storage/t6
```

### Run from source

```bash
dotnet run --project LittlegodsDlcInstallerTermux/LittlegodsDlcInstallerTermux.csproj -- manifest
dotnet run --project LittlegodsDlcInstallerTermux/LittlegodsDlcInstallerTermux.csproj -- install --path /sdcard/Plutonium/storage/t6
dotnet run --project LittlegodsDlcInstallerTermux/LittlegodsDlcInstallerTermux.csproj -- repair --path /sdcard/Plutonium/storage/t6
dotnet run --project LittlegodsDlcInstallerTermux/LittlegodsDlcInstallerTermux.csproj -- uninstall --path /sdcard/Plutonium/storage/t6
```

Press `Ctrl+C` during a download to cancel it. New files from that operation and partial files are removed automatically.

### Download the universal Termux package

The portable package is available in the [latest release](https://github.com/andr3xcl/littlegods-dlc-installer/releases):

```text
LittlegodsDlcInstallerTermux_Universal.zip
```

On the Android device:

```bash
pkg install dotnet-runtime-8.0 wget unzip
wget https://github.com/andr3xcl/littlegods-dlc-installer/releases/latest/download/LittlegodsDlcInstallerTermux_Universal.zip
unzip LittlegodsDlcInstallerTermux_Universal.zip
cd LittlegodsDlcInstallerTermux_Universal
./LittlegodsDlcInstallerTermux help
./LittlegodsDlcInstallerTermux install --path /sdcard/Plutonium/storage/t6
```

Verify the download:

```bash
sha256sum LittlegodsDlcInstallerTermux_Universal.zip
```

Expected SHA-256:

```text
The checksum is published with each release because it changes whenever the package is rebuilt.
```

### Build the universal Termux package

```bash
pkg install dotnet-sdk-8.0
./build-termux.sh
```

The output is generated at `produccion/termux/LittlegodsDlcInstallerTermux_Universal.zip`.

If Termux says `Unable to locate package`, refresh the package lists first:

```bash
pkg update
pkg upgrade
pkg install dotnet-runtime-8.0
```

## Run from Source

Requires the .NET 8 SDK.

```bash
~/.dotnet/dotnet run --project LittlegodsDlcInstallerGui/LittlegodsDlcInstallerGui.csproj
```

### Windows PowerShell

```powershell
dotnet run --project LittlegodsDlcInstallerGui/LittlegodsDlcInstallerGui.csproj
```

## Build

Development build:

```bash
~/.dotnet/dotnet build LittlegodsDlcInstallerGui/LittlegodsDlcInstallerGui.csproj -c Debug
```

## Production Packages

The script generates both formats inside `produccion/`:

```bash
APPIMAGETOOL=/path/to/appimagetool ./build-production.sh
```

### Generated Files

- `produccion/LittlegodsDlcInstallerGui.exe` — Windows x64.
- `produccion/LittlegodsDlcInstaller-x86_64.AppImage` — Linux x64.
- `produccion/Littlegods-logo.jpg` — Project logo.

The Windows executable is self-contained, and the AppImage includes the required runtime.

### Termux Package

Build the portable Termux package with:

```bash
./build-termux.sh
```

The package is generated at:

```text
produccion/termux/LittlegodsDlcInstallerTermux_Universal.zip
```

It contains the Termux launcher, the .NET application, and the shared manifest-based install, repair, uninstall, and cancellation logic.

## Release Checklist

Before publishing a release, build all distributable packages and verify their formats:

```bash
APPIMAGETOOL=/path/to/appimagetool ./build-production.sh
./build-termux.sh
file produccion/LittlegodsDlcInstallerGui.exe produccion/LittlegodsDlcInstaller-x86_64.AppImage
```

The release should include the Windows executable, Linux AppImage, and `produccion/termux/LittlegodsDlcInstallerTermux_Universal.zip`.

## License and Content

This repository contains the installer and its interface. Downloaded files are obtained from the manifest configured by the project.
