# LOCAL TEST ONLY: PVP beta dev v2.0 — modded client on official SDL3 runtime.
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$DesktopDir = "$env:USERPROFILE\Desktop\beta dev v2.0",
    [string]$TemplateDir = "$env:USERPROFILE\Desktop\UODreams-PVP-Beta-SDL3",
    [string]$OfficialCuo = "$env:USERPROFILE\Downloads\ClassicUOLauncher-win-x64-releasee\ClassicUO",
    [string]$RazorZip = "$env:USERPROFILE\Desktop\RazorEnhanced-Custom.zip"
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

function Copy-Sdl3NativeRuntime([string]$TargetDir, [string]$OfficialDir) {
    foreach ($name in @('zlib.dll','SDL3.dll','FAudio.dll','FNA3D.dll','libtheorafile.dll')) {
        $src = Join-Path $OfficialDir $name
        if (-not (Test-Path $src)) {
            $src = Join-Path $RepoRoot "external\x64-sdl3\$name"
        }
        if (-not (Test-Path $src)) { throw "Missing SDL3 runtime file: $name" }
        Copy-Item -Force $src (Join-Path $TargetDir $name)
    }
    $sdl2 = Join-Path $TargetDir 'SDL2.dll'
    if (Test-Path $sdl2) { Remove-Item -Force $sdl2 }
}

function Remove-RazorUserData([string]$RazorRoot) {
    foreach ($folder in @('Profiles','Scripts','Backup','_deploy_pending')) {
        $path = Join-Path $RazorRoot $folder
        if (Test-Path $path) {
            Write-Host "Stripping Razor user data: $path" -ForegroundColor DarkYellow
            Remove-Item $path -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

function Expand-RazorFlat([string]$ZipPath, [string]$InstallDir) {
    if (-not (Test-Path $ZipPath)) { throw "Razor zip not found: $ZipPath" }
    if (Test-Path $InstallDir) {
        Get-ChildItem $InstallDir -Force -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    } else {
        New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
    }
    $tempDir = Join-Path ([IO.Path]::GetTempPath()) ("razor-beta-v2-" + [guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Force -Path $tempDir | Out-Null
    try {
        Expand-Archive -Path $ZipPath -DestinationPath $tempDir -Force
        if (Test-Path (Join-Path $tempDir "RazorEnhanced.exe")) {
            Remove-RazorUserData $tempDir
            Copy-Item -Path (Join-Path $tempDir "*") -Destination $InstallDir -Recurse -Force
        } else {
            $sub = Get-ChildItem $tempDir -Directory -Filter "RazorEnhanced*" | Select-Object -First 1
            if (-not $sub) { throw "RazorEnhanced.exe not found inside $ZipPath" }
            Remove-RazorUserData $sub.FullName
            Copy-Item -Path (Join-Path $sub.FullName "*") -Destination $InstallDir -Recurse -Force
        }
    } finally {
        if (Test-Path $tempDir) { Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue }
    }
    if (-not (Test-Path (Join-Path $InstallDir "RazorEnhanced.exe"))) {
        throw "RazorEnhanced.exe missing after extract to $InstallDir"
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

function Clear-UserClientData([string]$ClientRoot) {
    foreach ($rel in @("Data\Profiles","Data\Client\JournalLogs","Logs","Bootstrap")) {
        $path = Join-Path $ClientRoot $rel
        if (Test-Path $path) {
            Write-Host "Stripping user data: $path" -ForegroundColor DarkYellow
            Remove-Item $path -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    foreach ($rel in @("settings.json","Bootstrap\settings.json")) {
        $path = Join-Path $ClientRoot $rel
        if (Test-Path $path) { Remove-Item $path -Force -ErrorAction SilentlyContinue }
    }
}

if (-not (Test-Path $OfficialCuo)) {
    throw "Official ClassicUO folder not found: $OfficialCuo"
}
if (-not (Test-Path $TemplateDir)) {
    throw "Template launcher folder not found: $TemplateDir"
}

$clientOut = Join-Path $RepoRoot "bin\dist-beta-dev-v2"
$bootstrapOut = Join-Path $RepoRoot "bin\bootstrap-beta-dev-v2"
$launcherOut = Join-Path $RepoRoot "bin\launcher-beta-dev-v2"
$aotLog = Join-Path $RepoRoot "bin\aot-publish-beta-dev-v2.log"
$gitBranch = try { (git -C $RepoRoot rev-parse --abbrev-ref HEAD 2>$null).Trim() } catch { "(unknown)" }

Write-Step "Publishing modded PVP client (NativeAOT + SDL3 mods)"
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

Write-Step "Publishing PVP launcher"
if (Test-Path $launcherOut) { Remove-Item $launcherOut -Recurse -Force }
dotnet publish (Join-Path $RepoRoot "src\ClassicUO.Launcher.Custom\ClassicUO.Launcher.Custom.csproj") `
    -c Release -r win-x64 --self-contained true `
    -p:LauncherEdition=pvp `
    -o $launcherOut | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Launcher publish failed." }

Write-Step "Assembling Desktop folder: $DesktopDir"
$settingsBackupPath = Join-Path $DesktopDir "launcher.settings.json"
$settingsBackup = $null
if (Test-Path $settingsBackupPath) {
    $settingsBackup = Get-Content $settingsBackupPath -Raw
}
if (Test-Path $DesktopDir) { Remove-Item $DesktopDir -Recurse -Force }
New-Item -ItemType Directory -Path $DesktopDir -Force | Out-Null

robocopy $TemplateDir $DesktopDir /E /XD Client /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
robocopy $launcherOut $DesktopDir /E /XF launcher.settings.json README-SDL3-BETA.txt /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null

$clientDir = Join-Path $DesktopDir "Client"
New-Item -ItemType Directory -Path $clientDir -Force | Out-Null

robocopy $clientOut $clientDir /E /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
Copy-BootstrapHostFiles $bootstrapOut $clientDir
Copy-Sdl3NativeRuntime $clientDir $OfficialCuo

$templateData = Join-Path $TemplateDir "Client\Data"
$clientData = Join-Path $clientDir "Data"
if (Test-Path $templateData) {
    robocopy $templateData $clientData /E /XF cuo.dll settings.json /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
}

Write-Step "Installing RazorEnhanced in Assistant\RazorEnhanced"
$assistantDir = Join-Path $DesktopDir "Assistant"
$razorDir = Join-Path $assistantDir "RazorEnhanced"
New-Item -ItemType Directory -Force -Path $assistantDir | Out-Null
Expand-RazorFlat $RazorZip $razorDir

Write-Step "Bundling XmlGumps and ExternalImages from repo"
Copy-ClientBundleData $clientDir $RepoRoot

Clear-UserClientData $clientDir
Get-ChildItem $clientDir -Filter "*.pdb" -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
if (Test-Path "$clientDir\cuo.exe") { Remove-Item "$clientDir\cuo.exe" -Force -ErrorAction SilentlyContinue }
if (Test-Path "$clientDir\Bootstrap") { Remove-Item "$clientDir\Bootstrap" -Recurse -Force -ErrorAction SilentlyContinue }

if ($settingsBackup) {
    Set-Content -Path (Join-Path $DesktopDir "launcher.settings.json") -Value $settingsBackup -Encoding UTF8
    Write-Host "Preserved existing launcher.settings.json" -ForegroundColor Green
} else {
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
    } | ConvertTo-Json | Set-Content (Join-Path $DesktopDir "launcher.settings.json") -Encoding UTF8
}

$readme = @"
UODreams PVP — beta dev v2.0 (test locale)
==========================================
Built: $(Get-Date -Format 'yyyy-MM-dd HH:mm')
Branch: $gitBranch
Runtime: SDL3.dll (official ClassicUO native stack)
Client: modded NativeAOT cuo.dll + ClassicUO.exe bootstrap
Razor: Assistant\RazorEnhanced\ (modded P.E. bundled with launcher)

Avvio: UODreams Launcher.exe oppure ClassicUO.exe in Client\
NON usare per release GitHub.
"@
Set-Content -Path (Join-Path $DesktopDir "README-BETA-DEV-v2.0.txt") -Value $readme -Encoding UTF8

$cuoSize = (Get-Item $nativeDll).Length
$sdl3 = Join-Path $clientDir "SDL3.dll"
$sdl2 = Join-Path $clientDir "SDL2.dll"
$razor = Join-Path $razorDir "RazorEnhanced.exe"

Write-Host ""
Write-Host "beta dev v2.0 ready:" -ForegroundColor Green
Write-Host "  Path: $DesktopDir"
Write-Host "  cuo.dll: $([math]::Round($cuoSize/1MB,2)) MB (native modded)"
Write-Host "  SDL3.dll: $(if(Test-Path $sdl3){'YES'}else{'MISSING'})"
Write-Host "  SDL2.dll: $(if(Test-Path $sdl2){'PRESENT (bad)'}else{'absent (ok)'})"
Write-Host "  RazorEnhanced.exe: $(if(Test-Path $razor){'YES'}else{'MISSING'})"
