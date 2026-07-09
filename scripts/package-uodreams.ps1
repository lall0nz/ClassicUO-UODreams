# Packages a clean UODreams Launcher distribution folder + zip backup.
param(
    [ValidateSet("pvp", "classic")]
    [string]$Edition = "pvp",
    [string]$OutputDir = "",
    [string]$BackupDir = "$env:USERPROFILE\Desktop",
    [string]$OfficialCuo = "$env:USERPROFILE\Downloads\ClassicUOLauncher-win-x64-release\ClassicUO",
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [switch]$ForceManagedClient
)

$ErrorActionPreference = "Stop"

function Write-Step($msg) { Write-Host ">> $msg" -ForegroundColor Cyan }

function Test-NativeCuoDll([string]$Path) {
    if (-not (Test-Path $Path)) { return $false }
    try {
        [System.Reflection.AssemblyName]::GetAssemblyName($Path) | Out-Null
        return $false
    } catch {
        return $true
    }
}

function Copy-BootstrapHostFiles([string]$SourceDir, [string]$TargetDir) {
    Copy-Item -Force "$SourceDir\ClassicUO.exe" "$TargetDir\ClassicUO.exe"
    Copy-Item -Force "$SourceDir\ClassicUO.exe.config" "$TargetDir\ClassicUO.exe.config" -ErrorAction SilentlyContinue
    Copy-Item -Force "$SourceDir\cuoapi.dll" "$TargetDir\cuoapi.dll"
    foreach ($f in @('System.Buffers.dll','System.Memory.dll','System.Numerics.Vectors.dll','System.Runtime.CompilerServices.Unsafe.dll')) {
        if (Test-Path "$SourceDir\$f") { Copy-Item -Force "$SourceDir\$f" "$TargetDir\$f" }
    }
}

function Copy-RazorPlugins([string]$SourcePluginsDir, [string]$TargetPluginsDir) {
    if (-not (Test-Path $SourcePluginsDir)) {
        Write-Host "Razor plugin source not found: $SourcePluginsDir" -ForegroundColor Yellow
        return $false
    }

    New-Item -ItemType Directory -Force -Path $TargetPluginsDir | Out-Null
    $copied = $false
    Get-ChildItem $SourcePluginsDir -Directory | Where-Object { $_.Name -like "RazorEnhanced*" } | ForEach-Object {
        $dest = Join-Path $TargetPluginsDir $_.Name
        if (-not (Test-Path $dest)) {
            Write-Host "Bundling Razor plugin: $($_.Name)" -ForegroundColor Green
            robocopy $_.FullName $dest /E /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
            $copied = $true
        }
    }

    return $copied
}

function Test-BundledRazorPlugins([string]$ClientRoot) {
    $pluginsRoots = @(
        (Join-Path $ClientRoot "Data\Plugins"),
        (Join-Path $ClientRoot "Bootstrap\Data\Plugins")
    )

    foreach ($pluginsDir in $pluginsRoots) {
        if (-not (Test-Path $pluginsDir)) { continue }
        $match = Get-ChildItem $pluginsDir -Directory -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -like "RazorEnhanced*" } |
            Select-Object -First 1
        if ($match) {
            return $match.FullName
        }
    }

    return $null
}

function Remove-PrebundledRazorPlugins([string]$ClientRoot) {
    Get-ChildItem $ClientRoot -Directory -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like "RazorEnhanced*" } |
        ForEach-Object {
            Write-Host "Removing pre-bundled plugin: $($_.FullName)" -ForegroundColor DarkYellow
            Remove-Item $_.FullName -Recurse -Force
        }
}

function Remove-BuildArtifacts([string]$ClientRoot) {
    Get-ChildItem $ClientRoot -Filter "*.pdb" -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
    Get-ChildItem $ClientRoot -Filter "createdump.exe" -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
    if (Test-Path "$ClientRoot\Logs") { Remove-Item "$ClientRoot\Logs" -Recurse -Force -ErrorAction SilentlyContinue }
}

$editionLabel = if ($Edition -eq "pvp") { "PVP" } else { "Classic" }
if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = "$env:USERPROFILE\Desktop\UODreams $editionLabel Launcher"
}

$clientOut = Join-Path $RepoRoot "bin\client-out"
$bootstrapOut = Join-Path $RepoRoot "bin\bootstrap-out"
$launcherOut = Join-Path $RepoRoot "bin\launcher-out-$Edition"
$clientDir = Join-Path $OutputDir "Client"
$bootstrapDir = Join-Path $clientDir "Bootstrap"

if (-not (Test-Path $OfficialCuo)) {
    throw "Official ClassicUO folder not found: $OfficialCuo"
}

$useUnifiedNative = $false
if ($Edition -eq "pvp") {
    if (-not $ForceManagedClient) {
        Write-Step "Publishing modded PVP client (NativeAOT / Dust765-style)"
        if (Test-Path $clientOut) { Remove-Item $clientOut -Recurse -Force }
        $aotLog = Join-Path $RepoRoot "bin\aot-publish.log"
        $aotOk = $true
        try {
            dotnet publish (Join-Path $RepoRoot "src\ClassicUO.Client\ClassicUO.Client.csproj") `
                -c Release -r win-x64 --self-contained true -p:PublishAot=true -o $clientOut 2>&1 | Tee-Object -FilePath $aotLog
        } catch {
            $aotOk = $false
        }
        if ($LASTEXITCODE -ne 0) { $aotOk = $false }
        $nativeDll = Join-Path $clientOut "cuo.dll"
        if ($aotOk -and (Test-Path $nativeDll) -and (Test-NativeCuoDll $nativeDll) -and ((Get-Item $nativeDll).Length -gt 1MB)) {
            $useUnifiedNative = $true
            Write-Host "NativeAOT modded cuo.dll ready" -ForegroundColor Green
        } else {
            Write-Host "NativeAOT unavailable; using managed client" -ForegroundColor Yellow
            if (Test-Path $clientOut) { Remove-Item $clientOut -Recurse -Force }
        }
    }

    if (-not $useUnifiedNative) {
        Write-Step "Publishing modded PVP client (managed cuo-modded.exe)"
        dotnet publish (Join-Path $RepoRoot "src\ClassicUO.Client\ClassicUO.Client.csproj") `
            -c Release -r win-x64 --self-contained true -p:PublishAot=false -o $clientOut | Out-Null
    }

    Write-Step "Publishing bootstrap host"
    dotnet publish (Join-Path $RepoRoot "src\ClassicUO.Bootstrap\src\ClassicUO.Bootstrap.csproj") `
        -c Release -o $bootstrapOut | Out-Null
}

Write-Step "Publishing $editionLabel launcher"
if (Test-Path $launcherOut) { Remove-Item $launcherOut -Recurse -Force }
dotnet publish (Join-Path $RepoRoot "src\ClassicUO.Launcher.Custom\ClassicUO.Launcher.Custom.csproj") `
    -c Release -r win-x64 --self-contained true `
    -p:LauncherEdition=$Edition `
    -o $launcherOut | Out-Null

Write-Step "Preparing output folder: $OutputDir"
if (Test-Path $OutputDir) {
    Remove-Item $OutputDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $OutputDir, $clientDir | Out-Null

Write-Step "Copying launcher"
robocopy $launcherOut $OutputDir /E /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null

if ($Edition -eq "classic") {
    Write-Step "Copying classic (unmodded) client from official ClassicUO"
    robocopy $OfficialCuo $clientDir /E /XF settings.json /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
    Remove-BuildArtifacts $clientDir
    Remove-PrebundledRazorPlugins $clientDir
} else {
    Write-Step "Copying PVP client"
    robocopy $clientOut $clientDir /E /XD Bootstrap /XF "ClassicUO.exe" /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
    Remove-BuildArtifacts $clientDir

    $razorPluginsSource = Join-Path $OfficialCuo "Data\Plugins"
    if ($useUnifiedNative) {
        Write-Step "Assembling unified Dust765-style client (mods + bundled Razor)"
        Copy-BootstrapHostFiles $bootstrapOut $clientDir
        Copy-RazorPlugins $razorPluginsSource (Join-Path $clientDir "Data\Plugins") | Out-Null
        if (Test-Path "$clientDir\cuo.exe") { Remove-Item "$clientDir\cuo.exe" -Force -ErrorAction SilentlyContinue }
    } else {
        Write-Step "Assembling legacy dual-client layout (managed mods + Razor bootstrap)"
        New-Item -ItemType Directory -Force -Path $bootstrapDir | Out-Null
        if (Test-Path "$clientDir\cuo.exe") {
            Move-Item -Force "$clientDir\cuo.exe" "$clientDir\cuo-modded.exe"
        }
        robocopy $OfficialCuo $bootstrapDir /E /XF settings.json /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
        Copy-BootstrapHostFiles $bootstrapOut $bootstrapDir
    }

    $bundledRazor = Test-BundledRazorPlugins $clientDir
    if (-not $bundledRazor) {
        throw "PVP edition requires bundled RazorEnhanced plugins. Expected RazorEnhanced* under $razorPluginsSource."
    }
    Write-Host "Bundled Razor: $bundledRazor" -ForegroundColor Green
}

Write-Step "Writing launcher settings template"
@{
    Assistant = "Nessuno"
    ClassicAssistPath = ""
    RazorPath = ""
    OrionPath = ""
    UOSteamPath = ""
    ClientPath = ""
    UoDirectory = ""
    ShardIp = "login.uodreams.com"
    ShardPort = 2593
    Encryption = 0
    FirstRunCompleted = $false
    DesktopShortcutCreated = $false
} | ConvertTo-Json | Set-Content (Join-Path $OutputDir "launcher.settings.json") -Encoding UTF8

Write-Step "Writing README"
$layout = if ($Edition -eq "classic") {
    "classic (ufficiale, senza mod e senza Razor preinstallato)"
} elseif ($useUnifiedNative) {
    "pvp unificato (mod + Razor preinstallato)"
} else {
    "pvp dual (cuo-modded + Bootstrap con Razor)"
}
@"

# UODreams $editionLabel Launcher

Client ClassicUO per UODreams.
Layout: $layout

## Avvio
1. Esegui ``UODreams Launcher.exe``
2. Se non hai Ultima Online, clicca **Scarica UODreams**
3. Scegli assistente (ClassicAssist / Razor Enhanced) e premi **AVVIA**

## Server
- Host: ``login.uodreams.com``
- Porta: ``2593``
"@ | Set-Content (Join-Path $OutputDir "LEGGIMI.txt") -Encoding UTF8

$stamp = Get-Date -Format "yyyy-MM-dd_HHmm"
$zipPath = Join-Path $BackupDir "UODreams-$editionLabel-Launcher-backup-$stamp.zip"
Write-Step "Creating backup zip: $zipPath"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path $OutputDir -DestinationPath $zipPath -CompressionLevel Optimal

$sizeMb = [math]::Round((Get-ChildItem $OutputDir -Recurse -File | Measure-Object Length -Sum).Sum / 1MB, 1)
$fileCount = (Get-ChildItem $OutputDir -Recurse -File).Count
Write-Host ""
Write-Host "Done ($editionLabel edition). Layout: $layout" -ForegroundColor Green
Write-Host "Package : $OutputDir ($fileCount files, $sizeMb MB)"
Write-Host "Backup  : $zipPath"
