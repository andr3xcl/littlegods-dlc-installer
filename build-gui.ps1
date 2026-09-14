param(
    [string]$Out = (Join-Path $PSScriptRoot "produccion")
)
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

$Project = Join-Path $PSScriptRoot "LittlegodsDlcInstallerGui/LittlegodsDlcInstallerGui.csproj"
New-Item -ItemType Directory -Force $Out | Out-Null

dotnet publish $Project -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None -p:DebugSymbols=false -o $Out
if ($LASTEXITCODE -ne 0) { throw "Windows publish failed ($LASTEXITCODE)" }

Write-Output "Windows executable published to $Out"
Get-ChildItem $Out -Filter *.exe | Select-Object FullName, Length
