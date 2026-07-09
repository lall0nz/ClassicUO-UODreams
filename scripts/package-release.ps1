# Builds GitHub Release assets for UODreams Launcher (PVP or Classic edition).
param(
    [string]$Version = "1.1.4",
    [ValidateSet("pvp", "classic")]
    [string]$Edition = "pvp",
    [string]$OfficialCuo = "$env:USERPROFILE\Downloads\ClassicUOLauncher-win-x64-release\ClassicUO",
    [string]$RazorEnhancedZip = "$env:USERPROFILE\Desktop\RazorEnhanced-Custom.zip",
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$OutputDir = "",
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
    foreach ($folder in @("Profiles", "Backup")) {
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

$editionLabel = if ($Edition -eq "pvp") { "PVP" } else { "Classic" }
$releaseTag = "$Edition-v$Version"

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $RepoRoot "bin\release-$Edition-v$Version"
}

$clientOut = Join-Path $RepoRoot "bin\client-out"
$bootstrapOut = Join-Path $RepoRoot "bin\bootstrap-out"
$launcherOut = Join-Path $RepoRoot "bin\launcher-single-$Edition"
$clientPackageRoot = Join-Path $OutputDir "client-package"
$clientDir = Join-Path $clientPackageRoot "Client"
$bootstrapDir = Join-Path $clientDir "Bootstrap"
$launcherZip = Join-Path $OutputDir "UODreams-$editionLabel-Launcher-v$Version.zip"
$clientZip = Join-Path $OutputDir "UODreams-$editionLabel-Client-v$Version.zip"
$launcherExe = Join-Path $OutputDir "UODreams Launcher.exe"

if (-not (Test-Path $OfficialCuo)) {
    throw @"
Official ClassicUO folder not found: $OfficialCuo

Download ClassicUO official launcher and extract it, then pass -OfficialCuo:
  winget download --id ClassicUO.ClassicUO --accept-source-agreements
  or get: https://www.classicuo.eu/launcher/win-x64/ClassicUOLauncher-win-x64-release.zip
"@
}

Write-Step "Preparing $editionLabel edition output: $OutputDir"
if (Test-Path $OutputDir) { Remove-Item $OutputDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $OutputDir, $clientPackageRoot, $clientDir | Out-Null

$useUnifiedNative = $false

if ($Edition -eq "classic") {
    Write-Step "Assembling classic (unmodded) client from official ClassicUO"
    robocopy $OfficialCuo $clientDir /E /XF settings.json /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
    Remove-BuildArtifacts $clientDir
    Clear-ClientPluginsDirectory $clientDir
    $leftoverPlugin = Test-ClientPluginsEmpty $clientDir
    if ($leftoverPlugin) {
        throw "Classic edition requires an empty plugins folder. Leftover: $leftoverPlugin"
    }
    Write-Host "Classic plugins folder: empty" -ForegroundColor Green
} else {
    $useUnifiedNative = $false
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
        if ($LASTEXITCODE -ne 0) {
            $aotOk = $false
        }
        $nativeDll = Join-Path $clientOut "cuo.dll"
        if ($aotOk -and (Test-Path $nativeDll) -and (Test-NativeCuoDll $nativeDll) -and ((Get-Item $nativeDll).Length -gt 1MB)) {
            $useUnifiedNative = $true
            Write-Host "NativeAOT modded cuo.dll ready ($([math]::Round((Get-Item $nativeDll).Length/1MB,1)) MB)" -ForegroundColor Green
        } else {
            Write-Host "NativeAOT build unavailable (install VS 'Desktop development with C++'). Falling back to managed client." -ForegroundColor Yellow
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

    Write-Step "Assembling PVP Client package"
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
}

Write-Step "Publishing single-file $editionLabel launcher v$Version"
if (Test-Path $launcherOut) { Remove-Item $launcherOut -Recurse -Force }
dotnet publish (Join-Path $RepoRoot "src\ClassicUO.Launcher.Custom\ClassicUO.Launcher.Custom.csproj") `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:LauncherEdition=$Edition `
    -p:Version=$Version `
    -o $launcherOut | Out-Null

Write-Step "Creating $([IO.Path]::GetFileName($clientZip))"
if (Test-Path $clientZip) { Remove-Item $clientZip -Force }
Compress-Archive -Path $clientPackageRoot\* -DestinationPath $clientZip -CompressionLevel Optimal

Write-Step "Creating launcher executable"
$publishedExe = Get-ChildItem $launcherOut -Filter "UODreams Launcher.exe" | Select-Object -First 1
if (-not $publishedExe) { throw "Launcher exe not found in $launcherOut" }
Copy-Item -Force $publishedExe.FullName $launcherExe

Write-Step "Creating $([IO.Path]::GetFileName($launcherZip))"
if (Test-Path $launcherZip) { Remove-Item $launcherZip -Force }
Compress-Archive -Path $launcherExe -DestinationPath $launcherZip -CompressionLevel Optimal

$launcherMb = [math]::Round((Get-Item $launcherZip).Length / 1MB, 1)
$clientMb = [math]::Round((Get-Item $clientZip).Length / 1MB, 1)
$layout = if ($Edition -eq "classic") { "classic (official unmodded, no Razor)" } elseif ($useUnifiedNative) { "pvp unified (mods + bundled Razor)" } else { "pvp dual (managed + Razor bootstrap)" }
$releaseTitle = if ($Edition -eq "pvp") { "UODreams PVP Launcher v$Version" } else { "UODreams Launcher v$Version" }

Write-Host ""
Write-Host "Release assets ready ($editionLabel edition)." -ForegroundColor Green
Write-Host "Layout       : $layout"
Write-Host "Release tag  : $releaseTag"
Write-Host "Release title: $releaseTitle"
Write-Host "Launcher zip : $launcherZip ($launcherMb MB)"
Write-Host "Client zip   : $clientZip ($clientMb MB)"
Write-Host ""
Write-Host "Publish to GitHub:" -ForegroundColor Yellow
Write-Host "  gh release create $releaseTag `"$releaseTitle`" ``"
Write-Host "    --repo lall0nz/ClassicUO-UODreams ``"
Write-Host "    `"$launcherZip`" ``"
Write-Host "    `"$clientZip`""
