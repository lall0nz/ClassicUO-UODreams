# LOCAL TEST ONLY: build NativeAOT PVP client with SDL3 and deploy to Desktop.
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$DesktopDir = "$env:USERPROFILE\Desktop\UODreams-PVP-Beta-SDL3",
    [string]$TemplateDir = "$env:USERPROFILE\Downloads\UODreams-PVP-Launcher-v1.1.0",
    [string]$OfficialCuo = "$env:USERPROFILE\Downloads\ClassicUOLauncher-win-x64-release\ClassicUO",
    [string]$RazorSource = ""
)

$ErrorActionPreference = "Stop"

function Write-Step($msg) { Write-Host ">> $msg" -ForegroundColor Cyan }

function Test-NativeCuoDll([string]$Path) {
    if (-not (Test-Path $Path)) { return $false }
    try {
        [System.Reflection.AssemblyName]::GetAssemblyName($Path) | Out-Null
        return $false
    } catch { return $true }
}

function Copy-BootstrapHostFiles([string]$SourceDir, [string]$TargetDir) {
    Copy-Item -Force "$SourceDir\ClassicUO.exe" "$TargetDir\ClassicUO.exe"
    Copy-Item -Force "$SourceDir\ClassicUO.exe.config" "$TargetDir\ClassicUO.exe.config" -ErrorAction SilentlyContinue
    Copy-Item -Force "$SourceDir\cuoapi.dll" "$TargetDir\cuoapi.dll"
    foreach ($f in @('System.Buffers.dll','System.Memory.dll','System.Numerics.Vectors.dll','System.Runtime.CompilerServices.Unsafe.dll')) {
        if (Test-Path "$SourceDir\$f") { Copy-Item -Force "$SourceDir\$f" "$TargetDir\$f" }
    }
}

function Copy-Sdl3NativeRuntime([string]$TargetDir) {
    $sdl3Dir = Join-Path $RepoRoot "external\x64-sdl3"
    foreach ($name in @('zlib.dll','SDL3.dll','FAudio.dll','FNA3D.dll','libtheorafile.dll')) {
        $src = Join-Path $sdl3Dir $name
        if (-not (Test-Path $src)) { throw "Missing SDL3 runtime file: $src" }
        Copy-Item -Force $src (Join-Path $TargetDir $name)
    }
    $sdl2 = Join-Path $TargetDir 'SDL2.dll'
    if (Test-Path $sdl2) { Remove-Item -Force $sdl2 }
}

function Remove-RazorUserData([string]$RazorRoot) {
    foreach ($folder in @('Profiles','Scripts','Backup')) {
        $path = Join-Path $RazorRoot $folder
        if (Test-Path $path) {
            Write-Host "Stripping Razor user data: $path" -ForegroundColor DarkYellow
            Remove-Item $path -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
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

function Get-GitBranchName([string]$Root) {
    try {
        $branch = git -C $Root rev-parse --abbrev-ref HEAD 2>$null
        if ($branch) { return $branch.Trim() }
    } catch { }
    return "(unknown)"
}

if (-not (Test-Path $TemplateDir)) {
    throw "Template launcher folder not found: $TemplateDir"
}

$clientOut = Join-Path $RepoRoot "bin\dist-sdl3"
$bootstrapOut = Join-Path $RepoRoot "bin\bootstrap-sdl3"
$launcherOut = Join-Path $RepoRoot "bin\launcher-sdl3"
$aotLog = Join-Path $RepoRoot "bin\aot-publish-sdl3.log"
$gitBranch = Get-GitBranchName $RepoRoot

Write-Step "Publishing NativeAOT modded client (SDL3)"
if (Test-Path $clientOut) { Remove-Item $clientOut -Recurse -Force }
dotnet publish (Join-Path $RepoRoot "src\ClassicUO.Client\ClassicUO.Client.csproj") `
    -c Release -r win-x64 --self-contained true -p:PublishAot=true -o $clientOut 2>&1 | Tee-Object -FilePath $aotLog
if ($LASTEXITCODE -ne 0) { throw "NativeAOT publish failed. See $aotLog" }

$nativeDll = Join-Path $clientOut "cuo.dll"
if (-not (Test-NativeCuoDll $nativeDll)) { throw "Expected native cuo.dll in $clientOut" }

Write-Step "Publishing bootstrap host"
if (Test-Path $bootstrapOut) { Remove-Item $bootstrapOut -Recurse -Force }
dotnet publish (Join-Path $RepoRoot "src\ClassicUO.Bootstrap\src\ClassicUO.Bootstrap.csproj") `
    -c Release -o $bootstrapOut | Out-Null

Write-Step "Publishing PVP launcher (SDL2/SDL3 preflight)"
if (Test-Path $launcherOut) { Remove-Item $launcherOut -Recurse -Force }
dotnet publish (Join-Path $RepoRoot "src\ClassicUO.Launcher.Custom\ClassicUO.Launcher.Custom.csproj") `
    -c Release -r win-x64 --self-contained true `
    -p:LauncherEdition=pvp `
    -o $launcherOut | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Launcher publish failed." }

Write-Step "Assembling Desktop test folder: $DesktopDir"
if (Test-Path $DesktopDir) { Remove-Item $DesktopDir -Recurse -Force }
New-Item -ItemType Directory -Path $DesktopDir -Force | Out-Null

# Copy launcher shell from working PVP template (EnhancedMap, settings, etc.)
robocopy $TemplateDir $DesktopDir /E /XD Client /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null

# Replace launcher binaries with freshly built repo launcher
robocopy $launcherOut $DesktopDir /E /XF launcher.settings.json /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null

$clientDir = Join-Path $DesktopDir "Client"
New-Item -ItemType Directory -Path $clientDir -Force | Out-Null

# Client binaries from NativeAOT publish + bootstrap
robocopy $clientOut $clientDir /E /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
Copy-BootstrapHostFiles $bootstrapOut $clientDir

# Replace native runtime with SDL3 set (no SDL2)
Copy-Sdl3NativeRuntime $clientDir

# Preserve template Data (XmlGumps, ExternalImages, etc.) but do not overwrite native cuo.dll
$templateClient = Join-Path $TemplateDir "Client"
$templateData = Join-Path $templateClient "Data"
$clientData = Join-Path $clientDir "Data"
if (Test-Path $templateData) {
    robocopy $templateData $clientData /E /XF cuo.dll settings.json /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
}

# Razor from template or explicit source
$pluginsDir = Join-Path $clientDir "Data\Plugins"
New-Item -ItemType Directory -Path $pluginsDir -Force | Out-Null
$razorSrc = if ($RazorSource -and (Test-Path $RazorSource)) { $RazorSource } else { Join-Path $templateClient "Data\Plugins" }
if (Test-Path $razorSrc) {
    robocopy $razorSrc $pluginsDir /E /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
    Get-ChildItem $pluginsDir -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like 'RazorEnhanced*' } |
        ForEach-Object { Remove-RazorUserData $_.FullName }
    if (Test-Path (Join-Path $pluginsDir "RazorEnhanced.exe")) {
        Remove-RazorUserData $pluginsDir
    }
}

Write-Step "Bundling XmlGumps and ExternalImages from repo"
Copy-ClientBundleData $clientDir $RepoRoot

# Label the beta build
$readme = @"
UODreams PVP — SDL3 BETA (local test only)
==========================================
Built: $(Get-Date -Format 'yyyy-MM-dd HH:mm')
Branch: $gitBranch
Native SDL: SDL3.dll (FNA 26 defaults to SDL3; FNA_PLATFORM_BACKEND=SDL2 removed)
Do NOT use for GitHub releases. Master/SDL2 releases are unchanged.

Launch: run UODreams Launcher.exe from this folder (or ClassicUO.exe inside Client).
"@
Set-Content -Path (Join-Path $DesktopDir "README-SDL3-BETA.txt") -Value $readme -Encoding UTF8

$cuoSize = (Get-Item $nativeDll).Length
$sdlName = if (Test-Path (Join-Path $clientDir "SDL3.dll")) { "SDL3.dll" } else { "(missing)" }

Write-Host ""
Write-Host "SDL3 beta test folder ready:" -ForegroundColor Green
Write-Host "  $DesktopDir"
Write-Host "  cuo.dll: $([math]::Round($cuoSize/1MB,2)) MB ($cuoSize bytes)"
Write-Host "  SDL DLL: $sdlName"
if (Test-Path (Join-Path $clientDir "SDL3.dll")) {
    $sdlSize = (Get-Item (Join-Path $clientDir "SDL3.dll")).Length
    Write-Host "  SDL3.dll: $([math]::Round($sdlSize/1MB,2)) MB"
}
