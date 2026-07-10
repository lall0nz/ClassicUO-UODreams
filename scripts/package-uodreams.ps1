# Packages a clean UODreams Launcher distribution folder + zip backup.
# Strips user runtime data before packaging — see RELEASE.md and Clear-UserClientData.
param(
    [ValidateSet("pvp", "classic")]
    [string]$Edition = "pvp",
    [string]$OutputDir = "",
    [string]$BackupDir = "$env:USERPROFILE\Desktop",
    [string]$OfficialCuo = "$env:USERPROFILE\Downloads\ClassicUOLauncher-win-x64-release\ClassicUO",
    [string]$RazorEnhancedZip = "",
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

function Copy-NativeRuntimeFiles([string[]]$SourceDirs, [string]$TargetDir) {
    foreach ($name in @('zlib.dll','SDL2.dll','FAudio.dll','FNA3D.dll','libtheorafile.dll')) {
        foreach ($sourceDir in $SourceDirs) {
            $src = Join-Path $sourceDir $name
            if (Test-Path $src) {
                Copy-Item -Force $src (Join-Path $TargetDir $name)
                break
            }
        }
    }
}

function Get-RazorPluginRoots([string]$PluginsDir) {
    $roots = @()
    if (Test-Path (Join-Path $PluginsDir "RazorEnhanced.exe")) {
        $roots += $PluginsDir
    }
    Get-ChildItem $PluginsDir -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like "RazorEnhanced*" } |
        ForEach-Object { $roots += $_.FullName }
    return $roots
}

function Remove-RazorUserData([string]$RazorRoot) {
    foreach ($folder in @("Profiles", "Scripts", "Backup")) {
        $path = Join-Path $RazorRoot $folder
        if (Test-Path $path) {
            Write-Host "Stripping Razor user data from bundle: $path" -ForegroundColor DarkYellow
            Remove-Item $path -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

function Expand-RazorPluginsZip([string]$ZipPath, [string]$TargetPluginsDir) {
    if (-not (Test-Path $ZipPath)) {
        Write-Host "Custom Razor zip not found: $ZipPath" -ForegroundColor Yellow
        return $false
    }

    if (Test-Path $TargetPluginsDir) {
        Get-ChildItem $TargetPluginsDir -Force -ErrorAction SilentlyContinue | ForEach-Object {
            Write-Host "Clearing existing plugin artifact: $($_.FullName)" -ForegroundColor DarkYellow
            Remove-Item $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
        }
    } else {
        New-Item -ItemType Directory -Force -Path $TargetPluginsDir | Out-Null
    }

    $tempDir = Join-Path ([IO.Path]::GetTempPath()) ("razor-pack-" + [guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Force -Path $tempDir | Out-Null
    try {
        Write-Host "Extracting custom Razor into staging: $tempDir" -ForegroundColor Green
        Expand-Archive -Path $ZipPath -DestinationPath $tempDir -Force

        foreach ($razorRoot in (Get-RazorPluginRoots $tempDir)) {
            Remove-RazorUserData $razorRoot
        }

        Write-Host "Copying sanitized Razor into: $TargetPluginsDir" -ForegroundColor Green
        Copy-Item -Path (Join-Path $tempDir "*") -Destination $TargetPluginsDir -Recurse -Force
    } finally {
        if (Test-Path $tempDir) {
            Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    if (Test-Path (Join-Path $TargetPluginsDir "RazorEnhanced.exe")) {
        return $true
    }

    return $null -ne (
        Get-ChildItem $TargetPluginsDir -Directory -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -like "RazorEnhanced*" } |
            Select-Object -First 1
    )
}

function Test-BundledRazorPlugins([string]$ClientRoot) {
    $pluginsRoots = @(
        (Join-Path $ClientRoot "Data\Plugins"),
        (Join-Path $ClientRoot "Bootstrap\Data\Plugins")
    )

    foreach ($pluginsDir in $pluginsRoots) {
        if (-not (Test-Path $pluginsDir)) { continue }
        if (Test-Path (Join-Path $pluginsDir "RazorEnhanced.exe")) {
            return $pluginsDir
        }
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

function Clear-ClientPluginsDirectory([string]$ClientRoot) {
    $pluginsRoots = @(
        (Join-Path $ClientRoot "Data\Plugins"),
        (Join-Path $ClientRoot "Bootstrap\Data\Plugins")
    )

    foreach ($pluginsDir in $pluginsRoots) {
        if (-not (Test-Path $pluginsDir)) { continue }

        Get-ChildItem $pluginsDir -Force -ErrorAction SilentlyContinue | ForEach-Object {
            Write-Host "Removing plugin artifact: $($_.FullName)" -ForegroundColor DarkYellow
            Remove-Item $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

function Test-ClientPluginsEmpty([string]$ClientRoot) {
    $pluginsRoots = @(
        (Join-Path $ClientRoot "Data\Plugins"),
        (Join-Path $ClientRoot "Bootstrap\Data\Plugins")
    )

    foreach ($pluginsDir in $pluginsRoots) {
        if (-not (Test-Path $pluginsDir)) { continue }
        $leftover = Get-ChildItem $pluginsDir -Force -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($leftover) {
            return $leftover.FullName
        }
    }

    return $null
}

function Remove-BuildArtifacts([string]$ClientRoot) {
    Get-ChildItem $ClientRoot -Filter "*.pdb" -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
    Get-ChildItem $ClientRoot -Filter "createdump.exe" -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
    if (Test-Path "$ClientRoot\Logs") { Remove-Item "$ClientRoot\Logs" -Recurse -Force -ErrorAction SilentlyContinue }
}

function Clear-UserClientData([string]$ClientRoot) {
    foreach ($rel in @(
        "Data\Profiles",
        "Data\Client\JournalLogs",
        "Logs",
        "Bootstrap\Data\Profiles",
        "Bootstrap\Data\Client\JournalLogs",
        "Bootstrap\Logs"
    )) {
        $path = Join-Path $ClientRoot $rel
        if (Test-Path $path) {
            Write-Host "Stripping user client data from bundle: $path" -ForegroundColor DarkYellow
            Remove-Item $path -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    foreach ($rel in @("settings.json", "Bootstrap\settings.json")) {
        $path = Join-Path $ClientRoot $rel
        if (Test-Path $path) {
            Write-Host "Stripping user client settings from bundle: $path" -ForegroundColor DarkYellow
            Remove-Item $path -Force -ErrorAction SilentlyContinue
        }
    }

    foreach ($rel in @("Data\Client", "Bootstrap\Data\Client")) {
        $clientData = Join-Path $ClientRoot $rel
        if (-not (Test-Path $clientData)) { continue }
        Get-ChildItem $clientData -Filter "*.usr" -File -ErrorAction SilentlyContinue | ForEach-Object {
            Write-Host "Stripping user map markers from bundle: $($_.FullName)" -ForegroundColor DarkYellow
            Remove-Item $_.FullName -Force -ErrorAction SilentlyContinue
        }
    }
}

function Resolve-OfficialCuoRoot([string]$Path) {
    if (-not (Test-Path $Path)) { return $null }

    foreach ($marker in @("cuo.exe", "ClassicUO.exe", "cuoapi.dll")) {
        if (Test-Path (Join-Path $Path $marker)) { return $Path }
    }

    $nested = Join-Path $Path "ClassicUO"
    if (Test-Path $nested) {
        $resolved = Resolve-OfficialCuoRoot $nested
        if ($resolved) { return $resolved }
    }

    $sub = Get-ChildItem $Path -Directory -Filter "ClassicUO" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($sub) {
        $resolved = Resolve-OfficialCuoRoot $sub.FullName
        if ($resolved) { return $resolved }
    }

    $cuoExe = Get-ChildItem $Path -Recurse -Filter "cuo.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($cuoExe) { return $cuoExe.Directory.FullName }

    return $null
}

function Resolve-RazorEnhancedZip([string]$RepoRoot, [string]$ExplicitPath) {
    if (-not [string]::IsNullOrWhiteSpace($ExplicitPath)) {
        return $ExplicitPath
    }

    $vendorRazor = Join-Path $RepoRoot "vendor\RazorEnhanced-Custom.zip"
    if (Test-Path $vendorRazor) {
        return $vendorRazor
    }

    $desktopRazor = Join-Path $env:USERPROFILE "Desktop\RazorEnhanced-Custom.zip"
    if (Test-Path $desktopRazor) {
        return $desktopRazor
    }

    return $vendorRazor
}

function Copy-ClientBundleData([string]$ClientDir, [string]$BundleRoot) {
    $dataRoot = Join-Path $BundleRoot "data"
    if (-not (Test-Path $dataRoot)) {
        Write-Host "No client bundle data at $dataRoot - skipping" -ForegroundColor DarkYellow
        return
    }

    $xmlSource = Join-Path $dataRoot "XmlGumps"
    if (Test-Path $xmlSource) {
        $xmlTarget = Join-Path $ClientDir "Data\XmlGumps"
        New-Item -ItemType Directory -Force -Path $xmlTarget | Out-Null
        robocopy $xmlSource $xmlTarget *.xml /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
        Write-Host "Bundled XmlGumps -> $xmlTarget" -ForegroundColor Green
    }

    $extSource = Join-Path $dataRoot "ExternalImages"
    if (Test-Path $extSource) {
        $extTarget = Join-Path $ClientDir "ExternalImages"
        New-Item -ItemType Directory -Force -Path $extTarget | Out-Null
        robocopy $extSource $extTarget /E /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
        Write-Host "Bundled ExternalImages -> $extTarget" -ForegroundColor Green
    }
}

$RazorEnhancedZip = Resolve-RazorEnhancedZip $RepoRoot $RazorEnhancedZip
$resolvedOfficialCuo = Resolve-OfficialCuoRoot $OfficialCuo
if ($resolvedOfficialCuo) {
    $OfficialCuo = $resolvedOfficialCuo
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
    Clear-ClientPluginsDirectory $clientDir
    $leftoverPlugin = Test-ClientPluginsEmpty $clientDir
    if ($leftoverPlugin) {
        throw "Classic edition requires an empty plugins folder. Leftover: $leftoverPlugin"
    }
    Write-Host "Classic plugins folder: empty" -ForegroundColor Green
} else {
    Write-Step "Copying PVP client"
    robocopy $clientOut $clientDir /E /XD Bootstrap /XF "ClassicUO.exe" /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
    Remove-BuildArtifacts $clientDir

    Remove-PrebundledRazorPlugins $clientDir

    $nativeSources = @($clientOut, $OfficialCuo, (Join-Path $RepoRoot "external\x64"))

    if ($useUnifiedNative) {
        Write-Step "Assembling unified Dust765-style client (mods + custom Razor)"
        Copy-BootstrapHostFiles $bootstrapOut $clientDir
        Copy-NativeRuntimeFiles $nativeSources $clientDir
        Expand-RazorPluginsZip $RazorEnhancedZip (Join-Path $clientDir "Data\Plugins") | Out-Null
        if (Test-Path "$clientDir\cuo.exe") { Remove-Item "$clientDir\cuo.exe" -Force -ErrorAction SilentlyContinue }
    } else {
        Write-Step "Assembling legacy dual-client layout (managed mods + Razor bootstrap)"
        New-Item -ItemType Directory -Force -Path $bootstrapDir | Out-Null
        if (Test-Path "$clientDir\cuo.exe") {
            Move-Item -Force "$clientDir\cuo.exe" "$clientDir\cuo-modded.exe"
        }
        Copy-NativeRuntimeFiles $nativeSources $clientDir
        robocopy $OfficialCuo $bootstrapDir /E /XD "Data\Plugins" /XF settings.json /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
        Copy-BootstrapHostFiles $bootstrapOut $bootstrapDir
        Copy-NativeRuntimeFiles @($OfficialCuo, (Join-Path $RepoRoot "external\x64")) $bootstrapDir
        Expand-RazorPluginsZip $RazorEnhancedZip (Join-Path $bootstrapDir "Data\Plugins") | Out-Null
    }

    $bundledRazor = Test-BundledRazorPlugins $clientDir
    if (-not $bundledRazor) {
        throw "PVP edition requires bundled custom RazorEnhanced. Expected RazorEnhanced.exe in plugins folder from $RazorEnhancedZip."
    }
    Write-Host "Bundled Razor: $bundledRazor" -ForegroundColor Green

    Write-Step "Bundling XmlGumps and ExternalImages"
    Copy-ClientBundleData $clientDir $RepoRoot
}

Write-Step "Stripping user runtime data from client folder"
Clear-UserClientData $clientDir

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
