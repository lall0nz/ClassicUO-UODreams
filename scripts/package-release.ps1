# Builds GitHub Release assets for UODreams Launcher.
param(
    [string]$Version = "1.0",
    [string]$OfficialCuo = "$env:USERPROFILE\Downloads\ClassicUOLauncher-win-x64-release\ClassicUO",
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$OutputDir = ""
)

$ErrorActionPreference = "Stop"

function Write-Step($msg) { Write-Host ">> $msg" -ForegroundColor Cyan }

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
New-Item -ItemType Directory -Force -Path $OutputDir, $clientPackageRoot, $clientDir, $bootstrapDir | Out-Null

Write-Step "Publishing modded client"
dotnet publish (Join-Path $RepoRoot "src\ClassicUO.Client\ClassicUO.Client.csproj") `
    -c Release -r win-x64 --self-contained true -p:PublishAot=false -o $clientOut | Out-Null

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
if (Test-Path "$clientDir\cuo.exe") {
    Move-Item -Force "$clientDir\cuo.exe" "$clientDir\cuo-modded.exe"
}

Write-Step "Assembling Razor bootstrap stack"
robocopy $OfficialCuo $bootstrapDir /E /XF settings.json /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
Copy-Item -Force "$bootstrapOut\ClassicUO.exe" "$bootstrapDir\ClassicUO.exe"
Copy-Item -Force "$bootstrapOut\ClassicUO.exe.config" "$bootstrapDir\ClassicUO.exe.config" -ErrorAction SilentlyContinue
Copy-Item -Force "$bootstrapOut\cuoapi.dll" "$bootstrapDir\cuoapi.dll"
foreach ($f in @('System.Buffers.dll','System.Memory.dll','System.Numerics.Vectors.dll','System.Runtime.CompilerServices.Unsafe.dll')) {
    if (Test-Path "$bootstrapOut\$f") { Copy-Item -Force "$bootstrapOut\$f" "$bootstrapDir\$f" }
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

Write-Host ""
Write-Host "Release assets ready." -ForegroundColor Green
Write-Host "Launcher zip : $launcherZip ($launcherMb MB)"
Write-Host "Client zip   : $clientZip ($clientMb MB)"
Write-Host ""
Write-Host "Publish to GitHub:" -ForegroundColor Yellow
Write-Host "  gh release create v$Version `"UODreams Launcher v$Version`" ``"
Write-Host "    --repo lall0nz/ClassicUO-UODreams ``"
Write-Host "    `"$launcherZip`" ``"
Write-Host "    `"$clientZip`""
