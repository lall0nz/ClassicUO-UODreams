# Builds GitHub Release assets for UODreams Launcher (PVP or Classic edition).
#
# RELEASE PACKAGES MUST BE "VIRGIN": no personal account or runtime user data.
# Stripped before zipping (Clear-UserClientData): Data/Profiles, Data/Client/JournalLogs,
# settings.json, Logs, user map markers (*.usr). Razor Profiles/Scripts/Backup are stripped separately.
# On client update, ClientRuntimeDownloader backs up and restores the same paths.
# See RELEASE.md for the full policy.
param(
    [string]$Version = "1.1.9",
    [ValidateSet("pvp", "classic")]
    [string]$Edition = "pvp",
    [string]$OfficialCuo = "$env:USERPROFILE\Downloads\ClassicUOLauncher-win-x64-releasee\ClassicUO",
    [string]$RazorEnhancedZip = "",
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

function Copy-Sdl3NativeRuntime([string]$TargetDir, [string]$OfficialDir, [string]$RepoRoot) {
    foreach ($name in @('zlib.dll','SDL3.dll','FAudio.dll','FNA3D.dll','libtheorafile.dll')) {
        $src = Join-Path $OfficialDir $name
        if (-not (Test-Path $src)) {
            $src = Join-Path $RepoRoot "external\x64-sdl3\$name"
        }
        if (-not (Test-Path $src)) {
            throw "Missing SDL3 runtime file: $name (provide -OfficialCuo with SDL3.dll or add external/x64-sdl3/$name)"
        }
        Copy-Item -Force $src (Join-Path $TargetDir $name)
    }

    $sdl2 = Join-Path $TargetDir 'SDL2.dll'
    if (Test-Path $sdl2) {
        Remove-Item -Force $sdl2
    }
}

function Expand-RazorIntoAssistant([string]$ZipPath, [string]$AssistantRazorDir) {
    if (-not (Test-Path $ZipPath)) {
        throw "Custom Razor zip not found: $ZipPath"
    }

    if (Test-Path $AssistantRazorDir) {
        Get-ChildItem $AssistantRazorDir -Force -ErrorAction SilentlyContinue | ForEach-Object {
            Write-Host "Clearing existing assistant Razor artifact: $($_.FullName)" -ForegroundColor DarkYellow
            Remove-Item $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
        }
    } else {
        New-Item -ItemType Directory -Force -Path $AssistantRazorDir | Out-Null
    }

    $tempDir = Join-Path ([IO.Path]::GetTempPath()) ("razor-assistant-" + [guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Force -Path $tempDir | Out-Null
    try {
        Expand-Archive -Path $ZipPath -DestinationPath $tempDir -Force

        if (Test-Path (Join-Path $tempDir "RazorEnhanced.exe")) {
            Remove-RazorUserData $tempDir
            Copy-Item -Path (Join-Path $tempDir "*") -Destination $AssistantRazorDir -Recurse -Force
        } else {
            $sub = Get-ChildItem $tempDir -Directory -Filter "RazorEnhanced*" | Select-Object -First 1
            if (-not $sub) { throw "RazorEnhanced.exe not found inside $ZipPath" }
            Remove-RazorUserData $sub.FullName
            Copy-Item -Path (Join-Path $sub.FullName "*") -Destination $AssistantRazorDir -Recurse -Force
        }
    } finally {
        if (Test-Path $tempDir) {
            Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    if (-not (Test-Path (Join-Path $AssistantRazorDir "RazorEnhanced.exe"))) {
        throw "RazorEnhanced.exe missing after extract to $AssistantRazorDir"
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

function Ensure-Mp3SharpNativeAotCompat([string]$RepoRoot) {
    $proj = Join-Path $RepoRoot "external\MP3Sharp\MP3Sharp\MP3Sharp.csproj"
    if (-not (Test-Path $proj)) { return }

    $content = Get-Content $proj -Raw
    if ($content -notmatch 'netstandard2\.0') { return }

    Write-Host "Patching MP3Sharp for NativeAOT (net8.0 + IsAotCompatible)" -ForegroundColor Yellow
    $content = $content -replace '<TargetFramework>netstandard2\.0</TargetFramework>', '<TargetFramework>net8.0</TargetFramework>'
    if ($content -notmatch 'IsAotCompatible') {
        $content = $content -replace '(<AssemblyName>MP3Sharp</AssemblyName>)', "`$1`r`n    <IsAotCompatible>true</IsAotCompatible>"
    }
    Set-Content -Path $proj -Value $content -NoNewline
}

function Remove-RazorUserData([string]$RazorRoot) {
    foreach ($folder in @("Profiles", "Scripts", "Backup", "_deploy_pending")) {
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
    $pluginsDir = Join-Path $ClientRoot "Data\Plugins"
    if (-not (Test-Path $pluginsDir)) { return $null }

    if (Test-Path (Join-Path $pluginsDir "RazorEnhanced.exe")) {
        return $pluginsDir
    }

    $match = Get-ChildItem $pluginsDir -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like "RazorEnhanced*" } |
        Select-Object -First 1
    if ($match) {
        return $match.FullName
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
    $pluginsDir = Join-Path $ClientRoot "Data\Plugins"
    if (-not (Test-Path $pluginsDir)) { return }

    Get-ChildItem $pluginsDir -Force -ErrorAction SilentlyContinue | ForEach-Object {
        Write-Host "Removing plugin artifact: $($_.FullName)" -ForegroundColor DarkYellow
        Remove-Item $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Test-ClientPluginsEmpty([string]$ClientRoot) {
    $pluginsDir = Join-Path $ClientRoot "Data\Plugins"
    if (-not (Test-Path $pluginsDir)) { return $null }

    return Get-ChildItem $pluginsDir -Force -ErrorAction SilentlyContinue | Select-Object -First 1
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

function Test-UnifiedPvpClientLayout([string]$ClientRoot) {
    $errors = @()

    if (Test-Path (Join-Path $ClientRoot "Bootstrap")) {
        $errors += "Bootstrap folder must not exist in unified PVP layout"
    }
    if (Test-Path (Join-Path $ClientRoot "cuo-modded.exe")) {
        $errors += "cuo-modded.exe must not exist in unified PVP layout"
    }

    $cuoDll = Join-Path $ClientRoot "cuo.dll"
    if (-not (Test-Path $cuoDll)) {
        $errors += "cuo.dll missing"
    } elseif (-not (Test-NativeCuoDll $cuoDll)) {
        $errors += "cuo.dll is not native AOT"
    } elseif ((Get-Item $cuoDll).Length -lt 10MB) {
        $errors += "cuo.dll too small for native modded build"
    }

    if (-not (Test-Path (Join-Path $ClientRoot "ClassicUO.exe"))) {
        $errors += "ClassicUO.exe bootstrap host missing"
    }

    if (-not (Test-Path (Join-Path $ClientRoot "SDL3.dll"))) {
        $errors += "SDL3.dll missing (PVP v1.1.8+ requires SDL3 runtime)"
    }
    if (Test-Path (Join-Path $ClientRoot "SDL2.dll")) {
        $errors += "SDL2.dll must not be present in SDL3 PVP layout"
    }

    $pluginsDir = Join-Path $ClientRoot "Data\Plugins"
    if (Test-Path $pluginsDir) {
        $leftover = Get-ChildItem $pluginsDir -Force -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($leftover) {
            $errors += "Client\Data\Plugins must be empty (Razor lives in Assistant\RazorEnhanced)"
        }
    }

    if ($errors.Count -gt 0) {
        throw "PVP client layout validation failed: $($errors -join '; ')"
    }
}

function Test-LauncherAssistantLayout([string]$LauncherRoot) {
    $razorExe = Join-Path $LauncherRoot "Assistant\RazorEnhanced\RazorEnhanced.exe"
    if (-not (Test-Path $razorExe)) {
        throw "Launcher package missing bundled Razor Enhanced at Assistant\RazorEnhanced\RazorEnhanced.exe"
    }

    foreach ($folder in @("Profiles", "Scripts", "Backup", "_deploy_pending")) {
        $path = Join-Path $LauncherRoot "Assistant\RazorEnhanced\$folder"
        if (Test-Path $path) {
            throw "Launcher package must not ship Razor user data: Assistant\RazorEnhanced\$folder"
        }
    }
}

function Test-ClientPackageVirgin([string]$ClientRoot) {
    $errors = @()

    foreach ($rel in @("Data\Profiles", "Data\Client\JournalLogs", "Bootstrap\Data\Profiles", "Bootstrap\Data\Client\JournalLogs")) {
        $path = Join-Path $ClientRoot $rel
        if (-not (Test-Path $path)) { continue }
        $count = @(Get-ChildItem $path -Recurse -File -ErrorAction SilentlyContinue).Count
        if ($count -gt 0) {
            $errors += "$rel ($count file(s))"
        }
    }

    if ($errors.Count -gt 0) {
        throw "Client package still contains user data: $($errors -join '; ')"
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

    $uoAnimSource = Join-Path $dataRoot "UoAnim"
    if (Test-Path $uoAnimSource) {
        $uoAnimTarget = Join-Path $ClientDir "Data\UoAnim"
        New-Item -ItemType Directory -Force -Path $uoAnimTarget | Out-Null
        robocopy $uoAnimSource $uoAnimTarget /E /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
        Write-Host "Bundled UoAnim (bodyconv, mobtypes) -> $uoAnimTarget" -ForegroundColor Green
    }
}

$RazorEnhancedZip = Resolve-RazorEnhancedZip $RepoRoot $RazorEnhancedZip
$resolvedOfficialCuo = Resolve-OfficialCuoRoot $OfficialCuo
if ($resolvedOfficialCuo) {
    $OfficialCuo = $resolvedOfficialCuo
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
$launcherPackageRoot = Join-Path $OutputDir "launcher-package"
$assistantRazorDir = Join-Path $launcherPackageRoot "Assistant\RazorEnhanced"
$bootstrapDir = Join-Path $clientDir "Bootstrap"
$launcherZip = Join-Path $OutputDir "UODreams-$editionLabel-Launcher-v$Version.zip"
$clientZip = Join-Path $OutputDir "UODreams-$editionLabel-Client-v$Version.zip"
$launcherExe = Join-Path $OutputDir "UODreams Launcher.exe"

if ($Edition -eq "classic" -and -not (Test-Path $OfficialCuo)) {
    throw @"
Official ClassicUO folder not found: $OfficialCuo

Download the official ClassicUO release zip, extract it, then pass -OfficialCuo
pointing at the extracted folder (flat layout or ClassicUO/ subfolder both work):
  https://github.com/ClassicUO/ClassicUO/releases/download/ClassicUO-main-release/ClassicUO-win-x64-release.zip
"@
}

if ($Edition -eq "pvp" -and -not (Test-Path $OfficialCuo)) {
    throw @"
Official ClassicUO SDL3 folder not found: $OfficialCuo

PVP v1.1.8 requires SDL3.dll from the official ClassicUO release. Download and extract:
  https://github.com/ClassicUO/ClassicUO/releases/download/ClassicUO-main-release/ClassicUO-win-x64-release.zip
Then pass -OfficialCuo pointing at the extracted ClassicUO folder.
"@
}

Write-Step "Preparing $editionLabel edition output: $OutputDir"

$manifestPath = Join-Path $RepoRoot "src\ClassicUO.Launcher.Custom\LauncherManifest.cs"
$manifestContent = Get-Content $manifestPath -Raw
if ($Edition -eq "pvp") {
    if ($manifestContent -notmatch '#if LAUNCHER_EDITION_PVP[\s\S]*?LauncherVersion = "' + [regex]::Escape($Version) + '"') {
        throw "LauncherManifest.cs PVP LauncherVersion must be $Version before packaging (found mismatch)."
    }
} else {
    if ($manifestContent -notmatch '#else\s+public const string LauncherVersion = "' + [regex]::Escape($Version) + '"') {
        throw "LauncherManifest.cs Classic LauncherVersion must be $Version before packaging (found mismatch)."
    }
}

if (Test-Path $OutputDir) { Remove-Item $OutputDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $OutputDir, $clientPackageRoot, $clientDir, $launcherPackageRoot | Out-Null

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
    if ($ForceManagedClient) {
        throw "PVP releases require NativeAOT modded cuo.dll. -ForceManagedClient is for local debugging only."
    }

    Ensure-Mp3SharpNativeAotCompat $RepoRoot

    Write-Step "Publishing modded PVP client (NativeAOT / Dust765-style)"
    if (Test-Path $clientOut) { Remove-Item $clientOut -Recurse -Force }
    $aotLog = Join-Path $RepoRoot "bin\aot-publish.log"
    dotnet publish (Join-Path $RepoRoot "src\ClassicUO.Client\ClassicUO.Client.csproj") `
        -c Release -r win-x64 --self-contained true -p:PublishAot=true -o $clientOut 2>&1 | Tee-Object -FilePath $aotLog
    if ($LASTEXITCODE -ne 0) {
        throw "NativeAOT publish failed (exit $LASTEXITCODE). Install VS 'Desktop development with C++' and see $aotLog"
    }

    $nativeDll = Join-Path $clientOut "cuo.dll"
    if (-not (Test-Path $nativeDll)) {
        throw "NativeAOT publish did not produce cuo.dll. See $aotLog"
    }
    if (-not (Test-NativeCuoDll $nativeDll)) {
        throw "cuo.dll is managed, not NativeAOT output. PVP releases require native modded cuo.dll (~14-15 MB). See $aotLog"
    }
    $nativeMb = [math]::Round((Get-Item $nativeDll).Length / 1MB, 1)
    if ($nativeMb -lt 10) {
        throw "cuo.dll is only ${nativeMb} MB; expected native modded build (~14-15 MB). See $aotLog"
    }
    Write-Host "NativeAOT modded cuo.dll ready ($nativeMb MB)" -ForegroundColor Green
    $useUnifiedNative = $true

    Write-Step "Publishing bootstrap host"
    dotnet publish (Join-Path $RepoRoot "src\ClassicUO.Bootstrap\src\ClassicUO.Bootstrap.csproj") `
        -c Release -o $bootstrapOut | Out-Null

    Write-Step "Assembling unified PVP client (SDL3 mods, no bundled Razor in Client)"
    robocopy $clientOut $clientDir /E /XD Bootstrap /XF "ClassicUO.exe" /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
    Remove-BuildArtifacts $clientDir
    Remove-PrebundledRazorPlugins $clientDir
    Clear-ClientPluginsDirectory $clientDir

    Copy-BootstrapHostFiles $bootstrapOut $clientDir
    Copy-Sdl3NativeRuntime $clientDir $OfficialCuo $RepoRoot
    New-Item -ItemType Directory -Force -Path (Join-Path $clientDir "Data\Plugins") | Out-Null
    if (Test-Path "$clientDir\cuo.exe") { Remove-Item "$clientDir\cuo.exe" -Force -ErrorAction SilentlyContinue }
    if (Test-Path "$clientDir\cuo-modded.exe") { Remove-Item "$clientDir\cuo-modded.exe" -Force -ErrorAction SilentlyContinue }
    if (Test-Path $bootstrapDir) {
        Write-Host "Removing stray Bootstrap folder from unified PVP layout" -ForegroundColor DarkYellow
        Remove-Item $bootstrapDir -Recurse -Force -ErrorAction SilentlyContinue
    }

    $leftoverPlugin = Test-ClientPluginsEmpty $clientDir
    if ($leftoverPlugin) {
        throw "PVP client requires an empty Client\Data\Plugins folder. Leftover: $leftoverPlugin"
    }
    Write-Host "Client plugins folder: empty (Razor ships in launcher Assistant\)" -ForegroundColor Green

    Write-Step "Bundling XmlGumps and ExternalImages"
    Copy-ClientBundleData $clientDir $RepoRoot
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
    -p:AssemblyVersion="$Version.0" `
    -p:FileVersion="$Version.0" `
    -o $launcherOut | Out-Null

Write-Step "Stripping user runtime data from client package"
Clear-UserClientData $clientDir
Test-ClientPackageVirgin $clientDir
if ($Edition -eq "pvp") {
    Test-UnifiedPvpClientLayout $clientDir
}

Write-Step "Creating $([IO.Path]::GetFileName($clientZip))"
if (Test-Path $clientZip) { Remove-Item $clientZip -Force }
Compress-Archive -Path $clientPackageRoot\* -DestinationPath $clientZip -CompressionLevel Optimal

Write-Step "Creating launcher executable"
$publishedExe = Get-ChildItem $launcherOut -Filter "UODreams Launcher.exe" | Select-Object -First 1
if (-not $publishedExe) { throw "Launcher exe not found in $launcherOut" }
Copy-Item -Force $publishedExe.FullName (Join-Path $launcherPackageRoot "UODreams Launcher.exe")
Copy-Item -Force $publishedExe.FullName $launcherExe

if ($Edition -eq "pvp") {
    Write-Step "Bundling Razor Enhanced P.E. in Assistant\RazorEnhanced"
    Expand-RazorIntoAssistant $RazorEnhancedZip $assistantRazorDir
    Test-LauncherAssistantLayout $launcherPackageRoot
    Write-Host "Bundled Razor: $assistantRazorDir" -ForegroundColor Green
}

Write-Step "Creating $([IO.Path]::GetFileName($launcherZip))"
if (Test-Path $launcherZip) { Remove-Item $launcherZip -Force }
Compress-Archive -Path (Join-Path $launcherPackageRoot "*") -DestinationPath $launcherZip -CompressionLevel Optimal

$launcherMb = [math]::Round((Get-Item $launcherZip).Length / 1MB, 1)
$clientMb = [math]::Round((Get-Item $clientZip).Length / 1MB, 1)
$layout = if ($Edition -eq "classic") { "classic (official unmodded, no Razor)" } else { "pvp unified SDL3 (NativeAOT mods + Razor in Assistant\RazorEnhanced)" }
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
