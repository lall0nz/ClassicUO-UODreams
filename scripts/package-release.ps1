# Builds GitHub Release assets for UODreams Launcher.
param(
    [string]$Version = "1.0",
    [string]$OfficialCuo = "$env:USERPROFILE\Downloads\ClassicUOLauncher-win-x64-release\ClassicUO",
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

function Copy-RazorPlugins([string]$OfficialRoot, [string]$TargetPluginsDir) {
    $officialPlugins = Join-Path $OfficialRoot "Data\Plugins"
    if (-not (Test-Path $officialPlugins)) { return }
    New-Item -ItemType Directory -Force -Path $TargetPluginsDir | Out-Null
    Get-ChildItem $officialPlugins -Directory | Where-Object { $_.Name -like "RazorEnhanced*" } | ForEach-Object {
        $dest = Join-Path $TargetPluginsDir $_.Name
        if (-not (Test-Path $dest)) {
            robocopy $_.FullName $dest /E /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
        }
    }
}

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $RepoRoot "bin\release-v$Version"
}

$clientOut = Join-Path $RepoRoot "bin\client-out"
$bootstrapOut = Join-Path $RepoRoot "bin\bootstrap-out"
$launcherOut = Join-Path $RepoRoot "bin\launcher-single"
$clientPackageRoot = Join-Path $OutputDir "client-package"
$clientDir = Join-Path $clientPackageRoot "Client"
$bootstrapDir = Join-Path $clientDir "Bootstrap"
$launcherZip = Join-Path $OutputDir "UODreams-Launcher-v$Version.zip"
$clientZip = Join-Path $OutputDir "UODreams-Client-v$Version.zip"
$launcherExe = Join-Path $OutputDir "UODreams Launcher.exe"

if (-not (Test-Path $OfficialCuo)) {
    throw @"
Official ClassicUO folder not found: $OfficialCuo

Download ClassicUO official launcher and extract it, then pass -OfficialCuo:
  winget download --id ClassicUO.ClassicUO --accept-source-agreements
  or get: https://www.classicuo.eu/launcher/win-x64/ClassicUOLauncher-win-x64-release.zip
"@
}

Write-Step "Preparing output: $OutputDir"
if (Test-Path $OutputDir) { Remove-Item $OutputDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $OutputDir, $clientPackageRoot, $clientDir | Out-Null

$useUnifiedNative = $false
if (-not $ForceManagedClient) {
    Write-Step "Publishing modded client (NativeAOT / Dust765-style)"
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
    Write-Step "Publishing modded client (managed cuo-modded.exe)"
    dotnet publish (Join-Path $RepoRoot "src\ClassicUO.Client\ClassicUO.Client.csproj") `
        -c Release -r win-x64 --self-contained true -p:PublishAot=false -o $clientOut | Out-Null
}

Write-Step "Publishing bootstrap host"
dotnet publish (Join-Path $RepoRoot "src\ClassicUO.Bootstrap\src\ClassicUO.Bootstrap.csproj") `
    -c Release -o $bootstrapOut | Out-Null

Write-Step "Publishing single-file launcher v$Version"
dotnet publish (Join-Path $RepoRoot "src\ClassicUO.Launcher.Custom\ClassicUO.Launcher.Custom.csproj") `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:Version=$Version.0 `
    -o $launcherOut | Out-Null

Write-Step "Assembling Client package"
robocopy $clientOut $clientDir /E /XD Bootstrap /XF "ClassicUO.exe" /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
Get-ChildItem $clientDir -Filter "*.pdb" -Recurse | Remove-Item -Force -ErrorAction SilentlyContinue
Get-ChildItem $clientDir -Filter "createdump.exe" -Recurse | Remove-Item -Force -ErrorAction SilentlyContinue
if (Test-Path "$clientDir\Logs") { Remove-Item "$clientDir\Logs" -Recurse -Force -ErrorAction SilentlyContinue }

if ($useUnifiedNative) {
    Write-Step "Assembling unified Dust765-style client (mods + Razor)"
    Copy-BootstrapHostFiles $bootstrapOut $clientDir
    Copy-RazorPlugins $OfficialCuo (Join-Path $clientDir "Data\Plugins")
    if (Test-Path "$clientDir\cuo.exe") { Remove-Item "$clientDir\cuo.exe" -Force -ErrorAction SilentlyContinue }
} else {
    Write-Step "Assembling legacy dual-client layout (managed mods + vanilla Razor bootstrap)"
    New-Item -ItemType Directory -Force -Path $bootstrapDir | Out-Null
    if (Test-Path "$clientDir\cuo.exe") {
        Move-Item -Force "$clientDir\cuo.exe" "$clientDir\cuo-modded.exe"
    }
    robocopy $OfficialCuo $bootstrapDir /E /XF settings.json /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
    Copy-BootstrapHostFiles $bootstrapOut $bootstrapDir
}

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
$layout = if ($useUnifiedNative) { "unified (mods+Razor)" } else { "dual (managed + Bootstrap)" }

Write-Host ""
Write-Host "Release assets ready." -ForegroundColor Green
Write-Host "Layout     : $layout"
Write-Host "Launcher zip : $launcherZip ($launcherMb MB)"
Write-Host "Client zip   : $clientZip ($clientMb MB)"
Write-Host ""
Write-Host "Publish to GitHub:" -ForegroundColor Yellow
Write-Host "  gh release create v$Version `"UODreams Launcher v$Version`" ``"
Write-Host "    --repo lall0nz/ClassicUO-UODreams ``"
Write-Host "    `"$launcherZip`" ``"
Write-Host "    `"$clientZip`""
