# Builds GitHub Release assets for UODreams PVP Launcher (v1.3.x channel).
param(
    [string]$Version = "1.3.1",
    [string]$OfficialCuo = "$env:USERPROFILE\Downloads\UODreams-PVP-by-lall0ne-Launcher-v1.3.0\Client",
    [string]$RazorSourceDir = "$env:USERPROFILE\Downloads\UODreams-PVP-by-lall0ne-Launcher-v1.3.0\Assistant\RazorEnhanced",
    [string]$StockProfileName = "default",
    [string]$PvpProfileName = "Default PVP",
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$OutputDir = "",
    [switch]$SkipPublish,
    [switch]$SkipGitHubRelease,
    # Require Authenticode signing (needs UODreamsCodeSignPfx). Prefer for public releases.
    [switch]$RequireCodeSign
)

$ErrorActionPreference = "Stop"

function Write-Step($msg) { Write-Host ">> $msg" -ForegroundColor Cyan }

function Test-NativeCuoDll([string]$Path) {
    if (-not (Test-Path $Path)) { return $false }
    try { [System.Reflection.AssemblyName]::GetAssemblyName($Path) | Out-Null; return $false } catch { return $true }
}

function Copy-BootstrapHostFiles([string]$SourceDir, [string]$TargetDir) {
    Copy-Item -Force "$SourceDir\ClassicUO.exe" "$TargetDir\ClassicUO.exe"
    Copy-Item -Force "$SourceDir\ClassicUO.exe.config" "$TargetDir\ClassicUO.exe.config" -ErrorAction SilentlyContinue
    Copy-Item -Force "$SourceDir\cuoapi.dll" "$TargetDir\cuoapi.dll"
    foreach ($f in @('System.Buffers.dll','System.Memory.dll','System.Numerics.Vectors.dll','System.Runtime.CompilerServices.Unsafe.dll')) {
        if (Test-Path "$SourceDir\$f") { Copy-Item -Force "$SourceDir\$f" "$TargetDir\$f" }
    }
}

function Copy-Sdl2NativeRuntime([string]$TargetDir, [string]$OfficialDir, [string]$RepoRoot) {
    $sourceDirs = @((Join-Path $RepoRoot "external\x64"), $OfficialDir)
    foreach ($name in @('zlib.dll','SDL2.dll','FAudio.dll','FNA3D.dll','libtheorafile.dll')) {
        $copied = $false
        foreach ($sourceDir in $sourceDirs) {
            if ([string]::IsNullOrWhiteSpace($sourceDir)) { continue }
            $src = Join-Path $sourceDir $name
            if (Test-Path $src) { Copy-Item -Force $src (Join-Path $TargetDir $name); $copied = $true; break }
        }
        if (-not $copied) { throw "Missing SDL2 runtime file: $name" }
    }
    $sdl3 = Join-Path $TargetDir 'SDL3.dll'
    if (Test-Path $sdl3) { Remove-Item -Force $sdl3 }
}

function Ensure-Mp3SharpNativeAotCompat([string]$RepoRoot) {
    $proj = Join-Path $RepoRoot "external\MP3Sharp\MP3Sharp\MP3Sharp.csproj"
    if (-not (Test-Path $proj)) { return }
    $content = Get-Content $proj -Raw
    if ($content -notmatch 'netstandard2\.0') { return }
    Write-Host "Patching MP3Sharp for NativeAOT" -ForegroundColor Yellow
    $content = $content -replace '<TargetFramework>netstandard2\.0</TargetFramework>', '<TargetFramework>net8.0</TargetFramework>'
    if ($content -notmatch 'IsAotCompatible') {
        $content = $content -replace '(<AssemblyName>MP3Sharp</AssemblyName>)', "`$1`r`n    <IsAotCompatible>true</IsAotCompatible>"
    }
    Set-Content -Path $proj -Value $content -NoNewline
}

function Remove-BuildArtifacts([string]$ClientRoot) {
    Get-ChildItem $ClientRoot -Filter "*.pdb" -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
    Get-ChildItem $ClientRoot -Filter "createdump.exe" -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
    if (Test-Path "$ClientRoot\Logs") { Remove-Item "$ClientRoot\Logs" -Recurse -Force -ErrorAction SilentlyContinue }
}

function Clear-UserLauncherData([string]$LauncherRoot) {
    foreach ($rel in @("launcher.settings.json")) {
        $path = Join-Path $LauncherRoot $rel
        if (Test-Path $path) { Remove-Item $path -Force -ErrorAction SilentlyContinue }
    }
    Get-ChildItem $LauncherRoot -Filter "*.bak" -File -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
    Get-ChildItem $LauncherRoot -Filter "*.bak-*" -File -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
    foreach ($rel in @("Assistant\RazorEnhanced\Logs","Assistant\RazorEnhanced\Log")) {
        $path = Join-Path $LauncherRoot $rel
        if (Test-Path $path) { Remove-Item $path -Recurse -Force -ErrorAction SilentlyContinue }
    }
}

function Resolve-LauncherExeName() {
    return "0nE UO Launcher.exe"
}

function Resolve-LauncherEdition() {
    return "oneuo"
}

function Clear-UserClientData([string]$ClientRoot) {
    foreach ($rel in @("Data\Profiles","Data\Client\JournalLogs","Logs","Bootstrap\Data\Profiles","Bootstrap\Data\Client\JournalLogs","Bootstrap\Logs")) {
        $path = Join-Path $ClientRoot $rel
        if (Test-Path $path) { Remove-Item $path -Recurse -Force -ErrorAction SilentlyContinue }
    }
    foreach ($rel in @("settings.json","Bootstrap\settings.json")) {
        $path = Join-Path $ClientRoot $rel
        if (Test-Path $path) { Remove-Item $path -Force -ErrorAction SilentlyContinue }
    }
    foreach ($rel in @("Data\Client","Bootstrap\Data\Client")) {
        $clientData = Join-Path $ClientRoot $rel
        if (-not (Test-Path $clientData)) { continue }
        Get-ChildItem $clientData -Filter "*.usr" -File -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
    }
    Get-ChildItem $ClientRoot -Filter "*.bak" -File -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
    Get-ChildItem $ClientRoot -Filter "*.bak-*" -File -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
}

function Copy-ClientBundleData([string]$ClientDir, [string]$BundleRoot) {
    $dataRoot = Join-Path $BundleRoot "data"
    if (-not (Test-Path $dataRoot)) { return }
    foreach ($pair in @(@("XmlGumps","Data\XmlGumps"),@("ExternalImages","ExternalImages"),@("UoAnim","Data\UoAnim"))) {
        $src = Join-Path $dataRoot $pair[0]
        if (-not (Test-Path $src)) { continue }
        $dst = Join-Path $ClientDir $pair[1]
        New-Item -ItemType Directory -Force -Path $dst | Out-Null
        robocopy $src $dst /E /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
    }
}

function Resolve-OfficialCuoRoot([string]$Path) {
    if (-not (Test-Path $Path)) { return $null }
    foreach ($marker in @("cuo.exe","ClassicUO.exe","cuoapi.dll","cuo.dll")) {
        if (Test-Path (Join-Path $Path $marker)) { return $Path }
    }
    $nested = Join-Path $Path "ClassicUO"
    if (Test-Path $nested) { return Resolve-OfficialCuoRoot $nested }
    return $null
}

function Resolve-ProfileDir([string]$ProfilesRoot, [string]$ProfileName) {
    $exact = Join-Path $ProfilesRoot $ProfileName
    if (Test-Path $exact) { return $exact }
    $match = Get-ChildItem $ProfilesRoot -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -ieq $ProfileName } | Select-Object -First 1
    if ($match) { return $match.FullName }
    return $null
}

function Get-DefaultProfileScriptRefs([string]$ProfileDir) {
    $scriptingFile = Join-Path $ProfileDir "RazorEnhanced.settings.SCRIPTING"
    if (-not (Test-Path $scriptingFile)) { throw "Missing $scriptingFile" }
    $entries = Get-Content $scriptingFile -Raw | ConvertFrom-Json
    $scripts = @()
    $groups = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in $entries) {
        if ($entry.Filename) { $scripts += [PSCustomObject]@{ Filename = [string]$entry.Filename; Group = [string]$entry.Group } }
        if ($entry.Group) { [void]$groups.Add([string]$entry.Group) }
    }
    $generalFile = Join-Path $ProfileDir "RazorEnhanced.settings.GENERAL"
    if (Test-Path $generalFile) {
        $general = (Get-Content $generalFile -Raw | ConvertFrom-Json)[0]
        foreach ($prop in @('ScriptFoldersPy','ScriptFoldersUos','ScriptFoldersCs')) {
            if ($general.PSObject.Properties.Name -contains $prop -and $general.$prop) {
                try {
                    $folders = $general.$prop | ConvertFrom-Json
                    foreach ($f in $folders) { if ($f) { [void]$groups.Add([string]$f) } }
                } catch { }
            }
        }
    }
    return [PSCustomObject]@{ Scripts = $scripts; Groups = @($groups) }
}

function ConvertTo-RazorSettingsJson([object]$Items, [int]$Depth = 20) {
    $items = @($Items)
    if ($items.Count -eq 0) { return "[]" }
    if ($items.Count -eq 1) {
        return "[`r`n" + (ConvertTo-Json -InputObject $items[0] -Depth $Depth) + "`r`n]"
    }
    return ConvertTo-Json -InputObject $items -Depth $Depth
}

function Sanitize-RazorProfileDir([string]$ProfileDir) {
    foreach ($file in Get-ChildItem $ProfileDir -File -Filter "RazorEnhanced.settings.*") {
        $raw = Get-Content $file.FullName -Raw
        if ($file.Name -eq "RazorEnhanced.settings.GENERAL") {
            $json = $raw | ConvertFrom-Json
            $g = @($json)[0]
            if (-not $g) { throw "GENERAL table is empty in $ProfileDir" }
            $g.CapPath = ""
            $g.VideoPath = ""
            $g.WindowX = 100
            $g.WindowY = 100
            $g.MountSerial = 0
            $g.ScriptCloudToken = ""
            $g.ScriptCloudPublishPassword = ""
            $g.ScriptCloudGuildPassword = $null
            $raw = ConvertTo-RazorSettingsJson $g 20
        } elseif ($file.Name -eq "RazorEnhanced.settings.SCRIPTING") {
            $raw = $raw -replace 'C:\\Users\\[^"\\]+', ''
            $raw = $raw -replace '"FullPath"\s*:\s*"[^"]*"', '"FullPath": ""'
            $json = $raw | ConvertFrom-Json
            foreach ($entry in @($json)) { $entry.FullPath = ""; $entry.Status = "Idle"; $entry.AutoStart = $false }
            $raw = ConvertTo-RazorSettingsJson @($json) 10
        } else {
            $raw = $raw -replace 'C:\\Users\\[^"\\]+', ''
            $raw = $raw -replace '"FullPath"\s*:\s*"[^"]*"', '"FullPath": ""'
        }
        [System.IO.File]::WriteAllText($file.FullName, $raw, (New-Object System.Text.UTF8Encoding $false))
    }
}

function Ensure-RazorProfileBackup([string]$RazorRoot, [string]$ProfileName) {
    $profileDir = Join-Path $RazorRoot "Profiles\$ProfileName"
    $backupDir = Join-Path $RazorRoot "Backup\$ProfileName"
    New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
    Get-ChildItem $profileDir -File -Filter "RazorEnhanced.settings.*" | ForEach-Object {
        Copy-Item $_.FullName (Join-Path $backupDir $_.Name) -Force
    }
}

function Test-RazorProfileBundle([string]$RazorRoot, [string]$ProfileName) {
    $profileDir = Join-Path $RazorRoot "Profiles\$ProfileName"
    $generalFile = Join-Path $profileDir "RazorEnhanced.settings.GENERAL"
    if (-not (Test-Path $generalFile)) { throw "Missing GENERAL settings for profile '$ProfileName'" }
    $generalRaw = [System.IO.File]::ReadAllText($generalFile)
    if (-not $generalRaw.TrimStart().StartsWith('[')) {
        throw "GENERAL must be a JSON array (DataTable row format); got bare object"
    }
    $general = $generalRaw | ConvertFrom-Json
    if (@($general).Count -lt 1) { throw "GENERAL array is empty" }
    foreach ($file in Get-ChildItem $profileDir -File -Filter "RazorEnhanced.settings.*") {
        try { $null = (Get-Content $file.FullName -Raw | ConvertFrom-Json) }
        catch { throw "Invalid JSON in $($file.Name): $($_.Exception.Message)" }
    }
    $backupDir = Join-Path $RazorRoot "Backup\$ProfileName"
    if (-not (Test-Path $backupDir)) { throw "Missing Backup\$ProfileName folder" }
    $backupGeneral = Join-Path $backupDir "RazorEnhanced.settings.GENERAL"
    if (-not (Test-Path $backupGeneral)) { throw "Missing Backup\$ProfileName\RazorEnhanced.settings.GENERAL" }
    if (-not ([System.IO.File]::ReadAllText($backupGeneral).TrimStart().StartsWith('['))) {
        throw "Backup GENERAL must be a JSON array"
    }
}

function Copy-ReferencedScripts([string]$SourceDir, [string]$ScriptsTarget, [object]$Refs) {
    New-Item -ItemType Directory -Force -Path $ScriptsTarget | Out-Null
    $copiedScripts = @()
    foreach ($script in $Refs.Scripts) {
        $srcFile = Get-ChildItem (Join-Path $SourceDir "Scripts") -Recurse -File -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -ieq $script.Filename } | Select-Object -First 1
        if ($srcFile) {
            Copy-Item $srcFile.FullName (Join-Path $ScriptsTarget $srcFile.Name) -Force
            $copiedScripts += $srcFile.Name
        } elseif ($script.Filename -ieq 'newscript1.py') {
            "# Starter script placeholder`nMisc.Pause(100)`nMisc.SendMessage('UODreams PVP starter script ready.', 65)" |
                Set-Content (Join-Path $ScriptsTarget "newscript1.py") -Encoding UTF8
            $copiedScripts += "newscript1.py"
        } else {
            Write-Host "WARNING: missing script $($script.Filename)" -ForegroundColor Yellow
        }
    }
    $assemblies = Join-Path $SourceDir "Scripts\Assemblies.cfg"
    if (Test-Path $assemblies) { Copy-Item $assemblies (Join-Path $ScriptsTarget "Assemblies.cfg") -Force }
    foreach ($group in $Refs.Groups) {
        if ([string]::IsNullOrWhiteSpace($group)) { continue }
        $groupDir = Join-Path $SourceDir "Scripts\$group"
        if (Test-Path $groupDir) {
            $dstGroup = Join-Path $ScriptsTarget $group
            New-Item -ItemType Directory -Force -Path $dstGroup | Out-Null
            Copy-Item (Join-Path $groupDir "*") $dstGroup -Recurse -Force
            Write-Host "Copied script folder: $group" -ForegroundColor DarkGray
        }
    }
    $starter = Join-Path $ScriptsTarget "newscript1.py"
    if (-not (Test-Path $starter)) {
        "# Starter script placeholder`nMisc.Pause(100)`nMisc.SendMessage('UODreams PVP starter script ready.', 65)" |
            Set-Content $starter -Encoding UTF8
        if ($copiedScripts -notcontains "newscript1.py") { $copiedScripts += "newscript1.py" }
    }
    return $copiedScripts
}

function Copy-RazorWithProfiles([string]$SourceDir, [string]$TargetDir, [string]$StockName, [string]$PvpName) {
    if (-not (Test-Path $SourceDir)) { throw "Razor source not found: $SourceDir" }
    if (Test-Path $TargetDir) { Remove-Item $TargetDir -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $TargetDir | Out-Null
    $excludeDirs = @('Profiles','Scripts','Backup','_deploy_pending','Logs','Log','_bundled_default','_bundled_default_pvp')
    Get-ChildItem $SourceDir -Force | Where-Object {
        $_.Name -notin $excludeDirs -and $_.Extension -notin @('.ERROR','.log')
    } | ForEach-Object {
        Copy-Item $_.FullName (Join-Path $TargetDir $_.Name) -Recurse -Force
    }

    $srcProfilesRoot = Join-Path $SourceDir "Profiles"
    if (-not (Test-Path $srcProfilesRoot)) { throw "No Profiles folder in Razor source" }

    # 1) Stock "default" - copy untouched (no sanitize, no Backup). Prefer exact folder if present.
    $stockSrc = Resolve-ProfileDir $srcProfilesRoot $StockName
    $shippedStock = $false
    if ($stockSrc) {
        $dstStock = Join-Path $TargetDir "Profiles\$StockName"
        New-Item -ItemType Directory -Force -Path $dstStock | Out-Null
        Copy-Item (Join-Path $stockSrc "*") $dstStock -Recurse -Force
        $shippedStock = $true
        Write-Host "Shipped stock profile '$StockName' untouched (no sanitize)." -ForegroundColor DarkGray
    } else {
        Write-Host "WARNING: stock profile '$StockName' not found - packaging without it." -ForegroundColor Yellow
    }

    # 2) Default PVP - cleaned starter from source "Default PVP" / "default pvp", else fall back to default content.
    $pvpSrc = Resolve-ProfileDir $srcProfilesRoot $PvpName
    if (-not $pvpSrc) { $pvpSrc = Resolve-ProfileDir $srcProfilesRoot "default pvp" }
    if (-not $pvpSrc) { $pvpSrc = $stockSrc }
    if (-not $pvpSrc) { throw "No source profile found for Default PVP (tried '$PvpName', 'default pvp', '$StockName')" }

    $dstPvp = Join-Path $TargetDir "Profiles\$PvpName"
    New-Item -ItemType Directory -Force -Path $dstPvp | Out-Null
    Copy-Item (Join-Path $pvpSrc "*") $dstPvp -Recurse -Force
    Sanitize-RazorProfileDir $dstPvp
    Ensure-RazorProfileBackup $TargetDir $PvpName

    $scriptsTarget = Join-Path $TargetDir "Scripts"
    $refs = Get-DefaultProfileScriptRefs $dstPvp
    $copiedScripts = Copy-ReferencedScripts $SourceDir $scriptsTarget $refs

    Get-ChildItem $TargetDir -Filter "*.ERROR" -File -ErrorAction SilentlyContinue | Remove-Item -Force
    Get-ChildItem $TargetDir -Filter "*.log" -File -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force

    # Pristine Default PVP stock (outside Profiles/) for OTA / startup repair.
    $stockDir = Join-Path $TargetDir "_bundled_default_pvp"
    if (Test-Path $stockDir) { Remove-Item $stockDir -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $stockDir | Out-Null
    Copy-Item (Join-Path $dstPvp "*") $stockDir -Recurse -Force
    Test-RazorProfileBundle $TargetDir $PvpName
    if (-not ([System.IO.File]::ReadAllText((Join-Path $stockDir "RazorEnhanced.settings.GENERAL")).TrimStart().StartsWith('['))) {
        throw "_bundled_default_pvp GENERAL must be a JSON array"
    }
    if ($shippedStock) {
        $stockGeneral = Join-Path $TargetDir "Profiles\$StockName\RazorEnhanced.settings.GENERAL"
        if ((Test-Path $stockGeneral) -and -not ([System.IO.File]::ReadAllText($stockGeneral).TrimStart().StartsWith('['))) {
            throw "Stock default GENERAL must remain a JSON array"
        }
    }

    return [PSCustomObject]@{
        StockProfile = $(if ($shippedStock) { $StockName } else { $null })
        PvpProfile = $PvpName
        Scripts = $copiedScripts
        Groups = $refs.Groups
    }
}

function Test-UnifiedPvpClientLayout([string]$ClientRoot) {
    $errors = @()
    if (Test-Path (Join-Path $ClientRoot "Bootstrap")) { $errors += "Bootstrap folder must not exist" }
    $cuoDll = Join-Path $ClientRoot "cuo.dll"
    if (-not (Test-Path $cuoDll)) { $errors += "cuo.dll missing" }
    elseif (-not (Test-NativeCuoDll $cuoDll)) { $errors += "cuo.dll is not native AOT" }
    if (-not (Test-Path (Join-Path $ClientRoot "ClassicUO.exe"))) { $errors += "ClassicUO.exe missing" }
    if (-not (Test-Path (Join-Path $ClientRoot "SDL2.dll"))) { $errors += "SDL2.dll missing" }
    if (Test-Path (Join-Path $ClientRoot "SDL3.dll")) { $errors += "SDL3.dll must not be shipped" }
    if ($errors.Count -gt 0) { throw ($errors -join '; ') }
}

function New-SourceZip([string]$RepoRoot, [string]$TargetZip) {
    $staging = Join-Path $env:TEMP ("uodreams-source-" + [guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Force -Path $staging | Out-Null
    try {
        robocopy $RepoRoot $staging /E /XD bin obj intermediate .git .vs temp vendor /XF *.user *.suo /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
        if (Test-Path $TargetZip) { Remove-Item $TargetZip -Force }
        Compress-Archive -Path (Join-Path $staging "*") -DestinationPath $TargetZip -CompressionLevel Optimal
    } finally {
        if (Test-Path $staging) { Remove-Item $staging -Recurse -Force -ErrorAction SilentlyContinue }
    }
}

$resolvedOfficialCuo = Resolve-OfficialCuoRoot $OfficialCuo
if ($resolvedOfficialCuo) { $OfficialCuo = $resolvedOfficialCuo }

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $RepoRoot "bin\release-pvp-v$Version"
}

$assetPrefix = "UODreams-PVP-by-lall0ne"
$releaseTag = "v$Version"
$releaseTitle = "0nE UO Launcher v$Version by lall0ne"
$launcherExeName = Resolve-LauncherExeName
$launcherEdition = Resolve-LauncherEdition
$clientZip = Join-Path $OutputDir "$assetPrefix-Client-v$Version.zip"
$launcherZip = Join-Path $OutputDir "$assetPrefix-Launcher-v$Version.zip"
$sourceZip = Join-Path $OutputDir "$assetPrefix-Source-v$Version.zip"

$manifestPath = Join-Path $RepoRoot "src\ClassicUO.Launcher.Custom\LauncherManifest.cs"
if ((Get-Content $manifestPath -Raw) -notmatch 'LauncherVersion = "' + [regex]::Escape($Version) + '"') {
    throw "LauncherManifest version mismatch"
}

if (Test-Path $OutputDir) { Remove-Item $OutputDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

$clientOut = Join-Path $RepoRoot "bin\client-out"
$bootstrapOut = Join-Path $RepoRoot "bin\bootstrap-out"
$launcherOut = Join-Path $RepoRoot "bin\launcher-single-pvp"
$clientPackageRoot = Join-Path $OutputDir "client-package"
$clientDir = Join-Path $clientPackageRoot "Client"
$launcherPackageRoot = Join-Path $OutputDir "launcher-package"
$assistantRazorDir = Join-Path $launcherPackageRoot "Assistant\RazorEnhanced"

Ensure-Mp3SharpNativeAotCompat $RepoRoot

if (-not $SkipPublish) {
    Write-Step "Publishing NativeAOT PVP client"
    if (Test-Path $clientOut) { Remove-Item $clientOut -Recurse -Force }
    $aotLog = Join-Path $RepoRoot "bin\aot-publish.log"
    dotnet publish (Join-Path $RepoRoot "src\ClassicUO.Client\ClassicUO.Client.csproj") `
        -c Release -r win-x64 --self-contained true -p:PublishAot=true -o $clientOut *> $aotLog
    if ($LASTEXITCODE -ne 0) { throw "NativeAOT publish failed. See $aotLog" }
    $nativeDll = Join-Path $clientOut "cuo.dll"
    if (-not (Test-Path $nativeDll) -or -not (Test-NativeCuoDll $nativeDll)) { throw "NativeAOT cuo.dll invalid. See $aotLog" }
    Write-Host "NativeAOT cuo.dll ready ($([math]::Round((Get-Item $nativeDll).Length/1MB,1)) MB)" -ForegroundColor Green

    Write-Step "Publishing bootstrap host"
    dotnet publish (Join-Path $RepoRoot "src\ClassicUO.Bootstrap\src\ClassicUO.Bootstrap.csproj") `
        -c Release -o $bootstrapOut | Out-Null
}

Write-Step "Assembling virgin PVP client package"
New-Item -ItemType Directory -Force -Path $clientPackageRoot, $clientDir | Out-Null
robocopy $clientOut $clientDir /E /XD Bootstrap /XF "ClassicUO.exe" /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
Remove-BuildArtifacts $clientDir
Copy-BootstrapHostFiles $bootstrapOut $clientDir
Copy-Sdl2NativeRuntime $clientDir $OfficialCuo $RepoRoot
New-Item -ItemType Directory -Force -Path (Join-Path $clientDir "Data\Plugins") | Out-Null
if (Test-Path "$clientDir\cuo.exe") { Remove-Item "$clientDir\cuo.exe" -Force }
Copy-ClientBundleData $clientDir $RepoRoot
Clear-UserClientData $clientDir
Set-Content -Path (Join-Path $clientDir "uodreams-client.version") -Value $Version -Encoding ASCII -NoNewline
Test-UnifiedPvpClientLayout $clientDir

$signScript = Join-Path $PSScriptRoot "sign-uodreams-binaries.ps1"
$shouldSign = $RequireCodeSign -or -not [string]::IsNullOrWhiteSpace($env:UODreamsCodeSignPfx)
if ($shouldSign) {
    Write-Step "Authenticode signing client binaries"
    $clientSignPaths = @(
        (Join-Path $clientDir "ClassicUO.exe"),
        (Join-Path $clientDir "cuo.dll")
    ) | Where-Object { Test-Path $_ }
    & $signScript -Paths $clientSignPaths
}

Write-Step "Publishing single-file 0nE UO launcher v$Version"
if (Test-Path $launcherOut) { Remove-Item $launcherOut -Recurse -Force }
dotnet publish (Join-Path $RepoRoot "src\ClassicUO.Launcher.Custom\ClassicUO.Launcher.Custom.csproj") `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:LauncherEdition=$launcherEdition `
    -p:Version=$Version `
    -p:AssemblyVersion="$Version.0" `
    -p:FileVersion="$Version.0" `
    -o $launcherOut | Out-Null

Write-Step "Bundling Razor with stock default + Default PVP"
New-Item -ItemType Directory -Force -Path $launcherPackageRoot | Out-Null
$publishedExe = Get-ChildItem $launcherOut -Filter $launcherExeName | Select-Object -First 1
if (-not $publishedExe) { throw "Launcher exe not found: $launcherExeName" }
Copy-Item -Force $publishedExe.FullName (Join-Path $launcherPackageRoot $launcherExeName)
$razorBundle = Copy-RazorWithProfiles $RazorSourceDir $assistantRazorDir $StockProfileName $PvpProfileName
Clear-UserLauncherData $launcherPackageRoot

if ($shouldSign) {
    Write-Step "Authenticode signing launcher + Razor"
    $launcherExe = Join-Path $launcherPackageRoot $launcherExeName
    $razorExe = Join-Path $assistantRazorDir "RazorEnhanced.exe"
    $signPaths = @($launcherExe)
    if (Test-Path $razorExe) { $signPaths += $razorExe }
    & $signScript -Paths $signPaths
} elseif ($RequireCodeSign) {
    throw "RequireCodeSign was set but signing did not run (missing UODreamsCodeSignPfx?)."
} else {
    Write-Host ">> Code signing skipped (set UODreamsCodeSignPfx or -RequireCodeSign for signed release)." -ForegroundColor Yellow
}

Write-Step "Creating release zips"
if (Test-Path $clientZip) { Remove-Item $clientZip -Force }
Compress-Archive -Path $clientPackageRoot\* -DestinationPath $clientZip -CompressionLevel Optimal
if (Test-Path $launcherZip) { Remove-Item $launcherZip -Force }
Compress-Archive -Path (Join-Path $launcherPackageRoot "*") -DestinationPath $launcherZip -CompressionLevel Optimal

Write-Step "Creating source zip"
New-SourceZip $RepoRoot $sourceZip

$clientMb = [math]::Round((Get-Item $clientZip).Length / 1MB, 1)
$launcherMb = [math]::Round((Get-Item $launcherZip).Length / 1MB, 1)
$sourceMb = [math]::Round((Get-Item $sourceZip).Length / 1MB, 1)

Write-Host ""
Write-Host "Release assets ready." -ForegroundColor Green
Write-Host "Client zip   : $clientZip ($clientMb MB)"
Write-Host "Launcher zip : $launcherZip ($launcherMb MB)"
Write-Host "Source zip   : $sourceZip ($sourceMb MB)"
Write-Host "Stock prof   : $($razorBundle.StockProfile)"
Write-Host "PVP prof     : $($razorBundle.PvpProfile)"
Write-Host "Scripts      : $($razorBundle.Scripts -join ', ')"
Write-Host "Script groups: $($razorBundle.Groups -join ', ')"

$notesPath = Join-Path $OutputDir "release-notes.md"
$signedLine = if ($shouldSign) {
    "- **Code signing Authenticode**: ``$launcherExeName``, ``ClassicUO.exe`` / ``cuo.dll``, ``RazorEnhanced.exe`` firmati (timestamp DigiCert)"
} else {
    "- **Code signing**: non applicato in questo build (configurare ``UODreamsCodeSignPfx``)"
}
$stockLine = if ($razorBundle.StockProfile) {
    "- **Profilo ``default``**: stock Razor intatto - mai sovrascritto da OTA o aggiornamento"
} else {
    "- **Profilo ``default``**: non presente nel bundle sorgente"
}
$notes = @"
# $releaseTitle

## Novita' v$Version

### Launcher 0nE UO
- Branding **0nE UO Launcher** (``LAUNCHER_EDITION_ONEUO``) con logo trasparente e icona dedicata
- Temi launcher con gradienti, selettore tema, pulsanti soft, combo e controlli themed
- Pulsante aggiornamento con stati **Aggiornato** / **Aggiornamento disponibile** (Up to date / Update Available)
- Canale update PVP invariato (``lall0nz/UODreams-PVP-Launcher``)

### Client ClassicUO
- **Hide carpets/rugs**: opzione client-side + IDs aggiuntivi ``0x28A4``-``0x28A6`` in ``carpets.txt``
- Sezione **Mods -> Visual Helpers**: layout e spaziatura migliorati
- **XML Gumps**: posizione default ``200,200``; posizione salvata nel profilo utente
- **Bandage timer**: finestra completa se presente (Show Timer Countdown)
- **NetClient**: buffer di lettura/invio mantenuto a **4096** (safe, no drain aggressivo)
- NativeAOT ``cuo.dll`` + SDL2/OpenGL only (niente SDL3)

### Razor Enhanced / OTA
- **Profilo ``Default PVP``**: copiato solo se mancante all'aggiornamento - **mai** sovrascritto se gia' presente
- Profilo stock ``default`` **mai** toccato da OTA
- ``_bundled_default_pvp`` aggiornato per repair-only (GENERAL corrotto)
- GENERAL JSON in formato DataTable array ``[...]``
- Script Default PVP: $($razorBundle.Scripts.Count) script (cartelle $($razorBundle.Groups -join ' / '))

### Packaging pulito
$signedLine
- Client **virgin**: niente ``Data/Profiles``, ``settings.json``, ``Logs``, ``*.usr``, ``*.bak``
- Launcher **virgin**: niente ``launcher.settings.json``, log Razor, file ``.bak``
- Esclusi ``SDL3.dll`` e artefatti build (``.pdb``, ``createdump.exe``)

## Download

| File | Descrizione |
|------|-------------|
| ``$([IO.Path]::GetFileName($launcherZip))`` | **0nE UO Launcher** + Razor modded |
| ``$([IO.Path]::GetFileName($clientZip))`` | Client ClassicUO modded (virgin) |
| ``$([IO.Path]::GetFileName($sourceZip))`` | Codice sorgente |

Server: ``login.uodreams.com:2593``
"@
[System.IO.File]::WriteAllText($notesPath, $notes, (New-Object System.Text.UTF8Encoding $false))

if (-not $SkipGitHubRelease) {
    Write-Step "Publishing GitHub release $releaseTag"
    gh release create $releaseTag $launcherZip $clientZip $sourceZip `
        --repo lall0nz/UODreams-PVP-Launcher `
        --title $releaseTitle `
        --notes-file $notesPath
    if ($LASTEXITCODE -ne 0) { throw "gh release create failed (exit $LASTEXITCODE)" }
    Write-Host "Published: https://github.com/lall0nz/UODreams-PVP-Launcher/releases/tag/$releaseTag" -ForegroundColor Green
}
