# Packages a clean UODreams Launcher distribution folder + zip backup.
param(
    [string]$OutputDir = "$env:USERPROFILE\Desktop\UODreams Launcher",
    [string]$BackupDir = "$env:USERPROFILE\Desktop",
    [string]$OfficialCuo = "$env:USERPROFILE\Downloads\ClassicUOLauncher-win-x64-release\ClassicUO",
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
)

$ErrorActionPreference = "Stop"

function Write-Step($msg) { Write-Host ">> $msg" -ForegroundColor Cyan }

$clientOut = Join-Path $RepoRoot "bin\client-out"
$bootstrapOut = Join-Path $RepoRoot "bin\bootstrap-out"
$launcherOut = Join-Path $RepoRoot "bin\launcher-out"
$clientDir = Join-Path $OutputDir "Client"
$bootstrapDir = Join-Path $clientDir "Bootstrap"

Write-Step "Publishing launcher"
dotnet publish (Join-Path $RepoRoot "src\ClassicUO.Launcher.Custom\ClassicUO.Launcher.Custom.csproj") `
    -c Release -r win-x64 --self-contained true -o $launcherOut | Out-Null

Write-Step "Publishing modded client"
dotnet publish (Join-Path $RepoRoot "src\ClassicUO.Client\ClassicUO.Client.csproj") `
    -c Release -r win-x64 --self-contained true -p:PublishAot=false -o $clientOut | Out-Null

Write-Step "Publishing bootstrap host"
dotnet publish (Join-Path $RepoRoot "src\ClassicUO.Bootstrap\src\ClassicUO.Bootstrap.csproj") `
    -c Release -o $bootstrapOut | Out-Null

if (-not (Test-Path $OfficialCuo)) {
    throw "Official ClassicUO folder not found: $OfficialCuo"
}

Write-Step "Preparing output folder: $OutputDir"
if (Test-Path $OutputDir) {
    Remove-Item $OutputDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $OutputDir, $clientDir, $bootstrapDir | Out-Null

Write-Step "Copying launcher"
robocopy $launcherOut $OutputDir /E /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null

Write-Step "Copying modded client"
robocopy $clientOut $clientDir /E /XD Bootstrap /XF "ClassicUO.exe" /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
Get-ChildItem $clientDir -Filter "*.pdb" -Recurse | Remove-Item -Force -ErrorAction SilentlyContinue
Get-ChildItem $clientDir -Filter "createdump.exe" -Recurse | Remove-Item -Force -ErrorAction SilentlyContinue
if (Test-Path "$clientDir\Logs") { Remove-Item "$clientDir\Logs" -Recurse -Force -ErrorAction SilentlyContinue }
if (Test-Path "$clientDir\cuo.exe") {
    Move-Item -Force "$clientDir\cuo.exe" "$clientDir\cuo-modded.exe"
}

Write-Step "Copying Razor bootstrap stack"
robocopy $OfficialCuo $bootstrapDir /E /XF settings.json /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
Copy-Item -Force "$bootstrapOut\ClassicUO.exe" "$bootstrapDir\ClassicUO.exe"
Copy-Item -Force "$bootstrapOut\ClassicUO.exe.config" "$bootstrapDir\ClassicUO.exe.config" -ErrorAction SilentlyContinue
Copy-Item -Force "$bootstrapOut\cuoapi.dll" "$bootstrapDir\cuoapi.dll"
foreach ($f in @('System.Buffers.dll','System.Memory.dll','System.Numerics.Vectors.dll','System.Runtime.CompilerServices.Unsafe.dll')) {
    if (Test-Path "$bootstrapOut\$f") { Copy-Item -Force "$bootstrapOut\$f" "$bootstrapDir\$f" }
}

Write-Step "Writing launcher settings template"
@{
    Assistant = "Nessuno"
    ClassicAssistPath = ""
    RazorPath = ""
    ClientPath = ""
    UoDirectory = ""
    ShardIp = "login.uodreams.com"
    ShardPort = 2593
    Encryption = 0
} | ConvertTo-Json | Set-Content (Join-Path $OutputDir "launcher.settings.json") -Encoding UTF8

Write-Step "Writing README"
@'
# UODreams Launcher

Client ClassicUO personalizzato per UODreams.

## Avvio
1. Esegui `UODreams Launcher.exe`
2. Se non hai Ultima Online, clicca **Scarica UODreams**
3. Scegli assistente (ClassicAssist / Razor Enhanced) e premi **AVVIA**

## Struttura
- `UODreams Launcher.exe` — launcher
- `Client\cuo-modded.exe` — client moddato (ClassicAssist)
- `Client\Bootstrap\ClassicUO.exe` — client per Razor Enhanced

## Server
- Host: `login.uodreams.com`
- Porta: `2593`
'@ | Set-Content (Join-Path $OutputDir "LEGGIMI.txt") -Encoding UTF8

$stamp = Get-Date -Format "yyyy-MM-dd_HHmm"
$zipPath = Join-Path $BackupDir "UODreams-Launcher-backup-$stamp.zip"
Write-Step "Creating backup zip: $zipPath"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path $OutputDir -DestinationPath $zipPath -CompressionLevel Optimal

$sizeMb = [math]::Round((Get-ChildItem $OutputDir -Recurse -File | Measure-Object Length -Sum).Sum / 1MB, 1)
$fileCount = (Get-ChildItem $OutputDir -Recurse -File).Count
Write-Host ""
Write-Host "Done." -ForegroundColor Green
Write-Host "Package : $OutputDir ($fileCount files, $sizeMb MB)"
Write-Host "Backup  : $zipPath"
