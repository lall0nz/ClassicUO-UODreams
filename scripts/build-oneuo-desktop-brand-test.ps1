# LOCAL TEST ONLY: rebranded 0nE UO launcher + freshly rebuilt client (cuo.dll) over the
# existing PVP Desktop shell. NEVER wipe user settings / profiles / logs.
#
# Preserved across redeploy (backup+restore if a full template seed is needed):
#   - launcher.settings.json
#   - Client\settings.json
#   - Client\Data\Profiles\**
#   - Assistant\RazorEnhanced\Profiles\**
#   - Logs\**
#   - Assistant\RazorEnhanced\Scripts\** (user scripts; only binaries are refreshed)
#
# Only binaries/assets required by the new build are overwritten.
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$DesktopDir = "$env:USERPROFILE\Desktop\0nE-UO-Launcher-v1.2.8-brand-test",
    [string]$TemplateDir = "$env:USERPROFILE\Downloads\UODreams-PVP-by-lall0ne-Launcher-v1.2.8",
    [string]$BrandTestClientVersion = "1.4.2",
    [string]$RazorBuildDir = "",
    [switch]$SkipClientRebuild,
    [switch]$SkipRazorRebuild
)

$ErrorActionPreference = "Stop"

function Write-Step($msg) { Write-Host ">> $msg" -ForegroundColor Cyan }

function Test-NativeCuoDll([string]$Path) {
    if (-not (Test-Path $Path)) { return $false }
    try { [System.Reflection.AssemblyName]::GetAssemblyName($Path) | Out-Null; return $false } catch { return $true }
}

function Backup-UserData([string]$Root) {
    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $backupRoot = Join-Path $env:TEMP "oneuo-brand-test-userdata-$stamp"
    New-Item -ItemType Directory -Force -Path $backupRoot | Out-Null
    $manifest = @()

    $paths = @(
        "launcher.settings.json",
        "Client\settings.json",
        "Client\Data\Profiles",
        "Assistant\RazorEnhanced\Profiles",
        "Assistant\RazorEnhanced\Scripts",
        "Logs"
    )

    foreach ($rel in $paths) {
        $src = Join-Path $Root $rel
        if (-not (Test-Path $src)) { continue }
        $dst = Join-Path $backupRoot $rel
        $dstParent = Split-Path $dst -Parent
        if (-not (Test-Path $dstParent)) { New-Item -ItemType Directory -Force -Path $dstParent | Out-Null }
        Copy-Item $src $dst -Recurse -Force
        $manifest += $rel
        Write-Host "  backed up: $rel" -ForegroundColor DarkGray
    }

    return [PSCustomObject]@{ Root = $backupRoot; Items = $manifest }
}

function Restore-UserData($Backup) {
    if ($null -eq $Backup -or [string]::IsNullOrWhiteSpace($Backup.Root) -or -not (Test-Path $Backup.Root)) { return }
    foreach ($rel in $Backup.Items) {
        $src = Join-Path $Backup.Root $rel
        $dst = Join-Path $DesktopDir $rel
        $dstParent = Split-Path $dst -Parent
        if (-not (Test-Path $dstParent)) { New-Item -ItemType Directory -Force -Path $dstParent | Out-Null }
        if (Test-Path $dst) { Remove-Item $dst -Recurse -Force -ErrorAction SilentlyContinue }
        Copy-Item $src $dst -Recurse -Force
        Write-Host "  restored: $rel" -ForegroundColor Green
    }
}

function Copy-FileIfExists([string]$Src, [string]$Dst) {
    if (Test-Path $Src) {
        $parent = Split-Path $Dst -Parent
        if (-not (Test-Path $parent)) { New-Item -ItemType Directory -Force -Path $parent | Out-Null }
        Copy-Item -Force $Src $Dst
        return $true
    }
    return $false
}

if (-not (Test-Path $TemplateDir) -and -not (Test-Path $DesktopDir)) {
    throw "Neither Desktop brand-test nor template found. Template: $TemplateDir"
}

# Close anything that could be holding the target files open before we overwrite them.
Get-Process | Where-Object { $_.ProcessName -match "ClassicUO|UODreams|0nE UO Launcher|RazorEnhanced" } |
    ForEach-Object {
        Write-Host "Stopping locking process: $($_.ProcessName) (pid $($_.Id))" -ForegroundColor Yellow
        Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
    }
Start-Sleep -Milliseconds 300

$launcherOut = Join-Path $RepoRoot "bin\launcher-oneuo"
$publishLog = Join-Path $RepoRoot "bin\launcher-oneuo-publish.log"

Write-Step "Publishing 0nE UO launcher (ONEUO branding, PVP update channel)"
if (Test-Path $launcherOut) { Remove-Item $launcherOut -Recurse -Force }
New-Item -ItemType Directory -Force -Path (Split-Path $launcherOut) | Out-Null

dotnet publish (Join-Path $RepoRoot "src\ClassicUO.Launcher.Custom\ClassicUO.Launcher.Custom.csproj") `
    -c Release -r win-x64 --self-contained true `
    -p:LauncherEdition=oneuo `
    -p:Version=$BrandTestClientVersion `
    -p:AssemblyVersion="$BrandTestClientVersion.0" `
    -p:FileVersion="$BrandTestClientVersion.0" `
    -o $launcherOut 2>&1 | Tee-Object -FilePath $publishLog
if ($LASTEXITCODE -ne 0) { throw "Launcher publish failed. See $publishLog" }

$exeName = "0nE UO Launcher.exe"
$builtExe = Join-Path $launcherOut $exeName
if (-not (Test-Path $builtExe)) {
    throw "Expected launcher exe not found: $builtExe"
}

$clientOut = $null
$bootstrapOut = $null
if (-not $SkipClientRebuild) {
    $clientOut = Join-Path $RepoRoot "bin\client-oneuo-test"
    $bootstrapOut = Join-Path $RepoRoot "bin\bootstrap-oneuo-test"
    $clientLog = Join-Path $RepoRoot "bin\client-oneuo-test-publish.log"

    Write-Step "Publishing NativeAOT client (fresh cuo.dll with current source fixes)"
    if (Test-Path $clientOut) { Remove-Item $clientOut -Recurse -Force }
    dotnet publish (Join-Path $RepoRoot "src\ClassicUO.Client\ClassicUO.Client.csproj") `
        -c Release -r win-x64 --self-contained true -p:PublishAot=true -o $clientOut *> $clientLog
    if ($LASTEXITCODE -ne 0) { throw "NativeAOT client publish failed. See $clientLog" }
    $nativeDll = Join-Path $clientOut "cuo.dll"
    if (-not (Test-Path $nativeDll) -or -not (Test-NativeCuoDll $nativeDll)) { throw "NativeAOT cuo.dll invalid. See $clientLog" }
    Write-Host "Fresh cuo.dll ready ($([math]::Round((Get-Item $nativeDll).Length/1MB,1)) MB)" -ForegroundColor Green

    Write-Step "Publishing bootstrap host (ClassicUO.exe)"
    if (Test-Path $bootstrapOut) { Remove-Item $bootstrapOut -Recurse -Force }
    dotnet publish (Join-Path $RepoRoot "src\ClassicUO.Bootstrap\src\ClassicUO.Bootstrap.csproj") `
        -c Release -o $bootstrapOut | Out-Null
}

$razorProj = Join-Path $RepoRoot "external\RazorEnhanced\RazorEnhanced.csproj"
$razorOut = Join-Path $RepoRoot "external\RazorEnhanced\bin\Release\net472"
if (-not $SkipRazorRebuild) {
    if (-not (Test-Path $razorProj)) { throw "Razor project missing: $razorProj" }
    Write-Step "Building Razor Enhanced (antifizzle + Data timeouts)"
    dotnet build $razorProj -c Release -v minimal
    if ($LASTEXITCODE -ne 0) { throw "Razor build failed" }
    $RazorBuildDir = $razorOut
} elseif ([string]::IsNullOrWhiteSpace($RazorBuildDir)) {
    if (Test-Path (Join-Path $razorOut "RazorEnhanced.exe")) {
        $RazorBuildDir = $razorOut
    }
}

Write-Step "Deploying to Desktop (preserve user data): $DesktopDir"

$userBackup = $null
$freshSeed = -not (Test-Path $DesktopDir)

if ($freshSeed) {
    if (-not (Test-Path $TemplateDir)) {
        throw "Desktop folder missing and template not found: $TemplateDir"
    }
    Write-Host "Fresh Desktop folder — seeding from template (no prior user data)." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $DesktopDir -Force | Out-Null
    robocopy (Join-Path $TemplateDir "Client") (Join-Path $DesktopDir "Client") /E /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "Failed copying Client from template" }
    robocopy (Join-Path $TemplateDir "Assistant") (Join-Path $DesktopDir "Assistant") /E /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "Failed copying Assistant from template" }
} else {
    Write-Host "Existing Desktop folder — backup user settings/profiles/Logs, then incremental binary update (NO wipe)." -ForegroundColor Cyan
    $userBackup = Backup-UserData $DesktopDir
}

$clientDir = Join-Path $DesktopDir "Client"
$razorDir = Join-Path $DesktopDir "Assistant\RazorEnhanced"

if (-not $SkipClientRebuild) {
    Write-Step "Injecting freshly built cuo.dll + ClassicUO.exe into test Client"

    Get-ChildItem $clientDir -Filter "cuo.dll.bak-*" -File -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
    Get-ChildItem $clientDir -Filter "*.pdb" -File -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue

    Copy-Item -Force (Join-Path $clientOut "cuo.dll") (Join-Path $clientDir "cuo.dll")
    Copy-Item -Force (Join-Path $bootstrapOut "ClassicUO.exe") (Join-Path $clientDir "ClassicUO.exe")
    foreach ($f in @('cuoapi.dll', 'System.Buffers.dll', 'System.Memory.dll', 'System.Numerics.Vectors.dll', 'System.Runtime.CompilerServices.Unsafe.dll')) {
        $src = Join-Path $bootstrapOut $f
        if (Test-Path $src) { Copy-Item -Force $src (Join-Path $clientDir $f) }
    }

    if (-not (Test-NativeCuoDll (Join-Path $clientDir "cuo.dll"))) { throw "Injected cuo.dll is not native AOT" }
    if (Test-Path (Join-Path $clientDir "SDL3.dll")) { throw "SDL3.dll must not be present (SDL2-only stack)" }
}

Set-Content -Path (Join-Path $clientDir "uodreams-client.version") -Value $BrandTestClientVersion -Encoding ASCII -NoNewline
Write-Host "Client version marker: $BrandTestClientVersion" -ForegroundColor DarkGray

# Razor binaries + Data spell timeouts — never touch Profiles/
if (-not [string]::IsNullOrWhiteSpace($RazorBuildDir) -and (Test-Path (Join-Path $RazorBuildDir "RazorEnhanced.exe"))) {
    Write-Step "Updating Razor binaries (Profiles preserved)"
    if (-not (Test-Path $razorDir)) { New-Item -ItemType Directory -Force -Path $razorDir | Out-Null }
    Copy-Item -Force (Join-Path $RazorBuildDir "RazorEnhanced.exe") (Join-Path $razorDir "RazorEnhanced.exe")
    Copy-FileIfExists (Join-Path $RazorBuildDir "RazorEnhanced.pdb") (Join-Path $razorDir "RazorEnhanced.pdb") | Out-Null
    Copy-FileIfExists (Join-Path $RazorBuildDir "RazorEnhanced.exe.config") (Join-Path $razorDir "RazorEnhanced.exe.config") | Out-Null

    $dataDir = Join-Path $razorDir "Data"
    New-Item -ItemType Directory -Force -Path $dataDir | Out-Null
    foreach ($dataFile in @("Spells.json", "Masteries.json")) {
        $fromBuild = Join-Path $RazorBuildDir "Data\$dataFile"
        $fromSrc = Join-Path $RepoRoot "external\RazorEnhanced\Data\$dataFile"
        if (Test-Path $fromBuild) {
            Copy-Item -Force $fromBuild (Join-Path $dataDir $dataFile)
        } elseif (Test-Path $fromSrc) {
            Copy-Item -Force $fromSrc (Join-Path $dataDir $dataFile)
        }
    }
    Write-Host "Razor Data Spells.json/Masteries.json refreshed" -ForegroundColor DarkGray
} else {
    Write-Host "Skipping Razor binary update (no build dir)." -ForegroundColor Yellow
}

if (Test-Path (Join-Path $razorDir "RazorEnhanced.exe")) {
    Set-Content -Path (Join-Path $razorDir "uodreams-razor.version") -Value $BrandTestClientVersion -Encoding ASCII -NoNewline
    Write-Host "Razor version marker: $BrandTestClientVersion" -ForegroundColor DarkGray
}

# Regenerate carpets.txt so new default IDs are included even if an older file exists.
$carpetsFile = Join-Path $clientDir "Data\Client\carpets.txt"
if (Test-Path $carpetsFile) {
    $carpetsRaw = Get-Content $carpetsFile -Raw
    if ($carpetsRaw -notmatch "(?m)^10404$") {
        Write-Step "Refreshing carpets.txt with new IDs (0x28A4-0x28A6 / 10404-10406)"
        Remove-Item -Force $carpetsFile
    }
}

# Fresh launcher binaries; exclude settings so we never clobber user prefs from publish output.
Write-Step "Updating launcher binaries (settings excluded)"
robocopy $launcherOut $DesktopDir /E /XF launcher.settings.json /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
if ($LASTEXITCODE -ge 8) { throw "Failed copying launcher publish output" }

$legacyExe = Join-Path $DesktopDir "UODreams Launcher.exe"
if (Test-Path $legacyExe) { Remove-Item -Force $legacyExe }
$legacyIco = Join-Path $DesktopDir "uodreams.ico"
if (Test-Path $legacyIco) { Remove-Item -Force $legacyIco }

# Restore any user data that might have been touched (idempotent).
if ($null -ne $userBackup) {
    Write-Step "Restoring preserved user settings/profiles/Logs"
    Restore-UserData $userBackup
    Write-Host "User data preserved from backup: $($userBackup.Root)" -ForegroundColor Green
} elseif ($freshSeed) {
    $settingsPath = Join-Path $DesktopDir "launcher.settings.json"
    if (Test-Path $settingsPath) { Remove-Item -Force $settingsPath }
    Write-Host "Fresh install: no launcher.settings.json (empty client path on first run)" -ForegroundColor DarkGray
}

Write-Host ""
Write-Host "DONE: $DesktopDir" -ForegroundColor Green
Write-Host "Launch: `"$DesktopDir\$exeName`"" -ForegroundColor Green
Write-Host "Title branding: 0nE UO Launcher v$BrandTestClientVersion (PVP update channel unchanged)" -ForegroundColor DarkGray
Write-Host "PRESERVED: launcher.settings.json, Client\settings.json, Client\Data\Profiles, Assistant\RazorEnhanced\Profiles, Logs, Scripts" -ForegroundColor Green
if (-not $SkipClientRebuild) {
    Write-Host "Client: fresh NativeAOT cuo.dll from current source" -ForegroundColor DarkGray
}
