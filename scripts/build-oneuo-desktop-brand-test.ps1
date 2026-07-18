# LOCAL TEST ONLY: rebranded 0nE UO launcher + freshly rebuilt client (cuo.dll) over the
# existing PVP v1.2.8 Desktop shell. Copies native runtime / Assistant / Data assets from the
# known-working Downloads template, but ALWAYS injects a fresh launcher exe + fresh cuo.dll
# built from current source, so client-side fixes (carpets, options UI, xml gumps, network
# buffer revert, etc.) actually land in the test package instead of reusing an old/possibly
# broken dll from the template.
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$DesktopDir = "$env:USERPROFILE\Desktop\0nE-UO-Launcher-v1.2.8-brand-test",
    [string]$TemplateDir = "$env:USERPROFILE\Downloads\UODreams-PVP-by-lall0ne-Launcher-v1.2.8",
    [switch]$SkipClientRebuild
)

$ErrorActionPreference = "Stop"

function Write-Step($msg) { Write-Host ">> $msg" -ForegroundColor Cyan }

function Test-NativeCuoDll([string]$Path) {
    if (-not (Test-Path $Path)) { return $false }
    try { [System.Reflection.AssemblyName]::GetAssemblyName($Path) | Out-Null; return $false } catch { return $true }
}

if (-not (Test-Path $TemplateDir)) {
    throw "Template launcher folder not found: $TemplateDir"
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

Write-Step "Assembling Desktop test folder: $DesktopDir"
if (Test-Path $DesktopDir) { Remove-Item $DesktopDir -Recurse -Force }
New-Item -ItemType Directory -Path $DesktopDir -Force | Out-Null

# Full Client + Assistant structure from the v1.2.8 release (no Logs / old settings) - gives us
# the known-working SDL2/FNA3D/FAudio native runtime, Data tables, ExternalImages, Assistant.
robocopy (Join-Path $TemplateDir "Client") (Join-Path $DesktopDir "Client") /E /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
if ($LASTEXITCODE -ge 8) { throw "Failed copying Client from template" }

robocopy (Join-Path $TemplateDir "Assistant") (Join-Path $DesktopDir "Assistant") /E /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
if ($LASTEXITCODE -ge 8) { throw "Failed copying Assistant from template" }

$clientDir = Join-Path $DesktopDir "Client"

if (-not $SkipClientRebuild) {
    Write-Step "Injecting freshly built cuo.dll + ClassicUO.exe into test Client"

    # Drop any stray backup dlls copied from the template folder (avoid confusion / stale files).
    Get-ChildItem $clientDir -Filter "cuo.dll.bak-*" -File -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
    Get-ChildItem $clientDir -Filter "*.pdb" -File -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue

    Copy-Item -Force (Join-Path $clientOut "cuo.dll") (Join-Path $clientDir "cuo.dll")
    Copy-Item -Force (Join-Path $bootstrapOut "ClassicUO.exe") (Join-Path $clientDir "ClassicUO.exe")
    foreach ($f in @('cuoapi.dll', 'System.Buffers.dll', 'System.Memory.dll', 'System.Numerics.Vectors.dll', 'System.Runtime.CompilerServices.Unsafe.dll')) {
        $src = Join-Path $bootstrapOut $f
        if (Test-Path $src) { Copy-Item -Force $src (Join-Path $clientDir $f) }
    }

    # cuo.dll must stay native AOT (not a managed reflection-loadable assembly).
    if (-not (Test-NativeCuoDll (Join-Path $clientDir "cuo.dll"))) { throw "Injected cuo.dll is not native AOT" }
    if (Test-Path (Join-Path $clientDir "SDL3.dll")) { throw "SDL3.dll must not be present (SDL2-only stack)" }
}

# Regenerate carpets.txt so the new default IDs (0x28A4-0x28A6) are included even if an older
# generated file was copied from the template (StaticFilters only writes it if missing).
$carpetsFile = Join-Path $clientDir "Data\Client\carpets.txt"
if (Test-Path $carpetsFile) {
    $carpetsRaw = Get-Content $carpetsFile -Raw
    if ($carpetsRaw -notmatch "(?m)^10404$") {
        Write-Step "Refreshing carpets.txt with new IDs (0x28A4-0x28A6 / 10404-10406)"
        Remove-Item -Force $carpetsFile
    }
}

# Fresh launcher binaries only (no virgin settings file — launcher creates defaults on first run).
robocopy $launcherOut $DesktopDir /E /XF launcher.settings.json /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
if ($LASTEXITCODE -ge 8) { throw "Failed copying launcher publish output" }

# Remove any leftover UODreams launcher exe / icon from template if copied by mistake.
$legacyExe = Join-Path $DesktopDir "UODreams Launcher.exe"
if (Test-Path $legacyExe) { Remove-Item -Force $legacyExe }
$legacyIco = Join-Path $DesktopDir "uodreams.ico"
if (Test-Path $legacyIco) { Remove-Item -Force $legacyIco }

$settings = Join-Path $DesktopDir "launcher.settings.json"
if (Test-Path $settings) { Remove-Item -Force $settings }

Write-Host ""
Write-Host "DONE: $DesktopDir" -ForegroundColor Green
Write-Host "Launch: `"$DesktopDir\$exeName`"" -ForegroundColor Green
Write-Host "Title branding: 0nE UO Launcher (PVP update channel unchanged)" -ForegroundColor DarkGray
if (-not $SkipClientRebuild) {
    Write-Host "Client: fresh NativeAOT cuo.dll from current source (network buffer reverted to safe 4096, carpets, options UI, xml gumps, pulse update button)" -ForegroundColor DarkGray
}
