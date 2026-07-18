# Authenticode-signs UODreams PVP binaries when a commercial/OV/EV .pfx is configured.
#
# Required env (or parameters):
#   UODreamsCodeSignPfx       Path to .pfx / .p12 with Code Signing EKU + private key
#   UODreamsCodeSignPassword  PFX password (prefer User env / CI secret; never commit)
#
# Optional:
#   UODreamsCodeSignTimestamp  Timestamp URL (default DigiCert)
#   UODreamsSignTool           Full path to signtool.exe
#
# Self-signed / localhost certs do NOT clear Windows Security "publisher cannot be verified"
# on other PCs. Use a trusted CA cert or Azure Trusted Signing.
param(
    [Parameter(Mandatory = $true)]
    [string[]]$Paths,

    [string]$PfxPath = $env:UODreamsCodeSignPfx,
    [string]$PfxPassword = $env:UODreamsCodeSignPassword,
    [string]$TimestampUrl = $(if ($env:UODreamsCodeSignTimestamp) { $env:UODreamsCodeSignTimestamp } else { "http://timestamp.digicert.com" }),
    [string]$SignTool = $env:UODreamsSignTool,
    [switch]$SkipIfMissing
)

$ErrorActionPreference = "Stop"

function Resolve-SignToolPath([string]$Preferred) {
    if ($Preferred -and (Test-Path $Preferred)) { return (Resolve-Path $Preferred).Path }
    $cmd = Get-Command signtool.exe -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    $kitRoot = "${env:ProgramFiles(x86)}\Windows Kits\10\bin"
    if (Test-Path $kitRoot) {
        $found = Get-ChildItem -Path $kitRoot -Recurse -Filter signtool.exe -ErrorAction SilentlyContinue |
            Where-Object { $_.DirectoryName -match '\\x64$' } |
            Sort-Object FullName -Descending |
            Select-Object -First 1
        if ($found) { return $found.FullName }
    }
    return $null
}

function Test-CodeSigningPfxConfigured {
    return -not [string]::IsNullOrWhiteSpace($env:UODreamsCodeSignPfx)
}

if ([string]::IsNullOrWhiteSpace($PfxPath)) {
    if ($SkipIfMissing) {
        Write-Host ">> Code signing skipped (UODreamsCodeSignPfx not set)." -ForegroundColor Yellow
        return
    }
    throw @"
No code signing PFX configured.

Set:
  `$env:UODreamsCodeSignPfx = 'C:\path\to\codesign.pfx'
  `$env:UODreamsCodeSignPassword = '<pfx-password>'

Then re-run packaging. A commercial OV/EV code signing certificate (or Azure Trusted Signing)
is required to clear Windows Security publisher warnings on other machines.
Self-signed certificates will NOT fix SmartScreen / publisher verification for end users.
"@
}

if (-not (Test-Path $PfxPath)) {
    throw "Code signing PFX not found: $PfxPath"
}

$signToolPath = Resolve-SignToolPath $SignTool
if (-not $signToolPath) {
    throw "signtool.exe not found. Install Windows SDK Signing Tools or set UODreamsSignTool."
}

$existing = @()
foreach ($p in $Paths) {
    if ([string]::IsNullOrWhiteSpace($p)) { continue }
    if (-not (Test-Path $p)) { throw "File to sign not found: $p" }
    $existing += (Resolve-Path $p).Path
}

if ($existing.Count -eq 0) {
    throw "No files to sign."
}

Write-Host ">> Authenticode signing $($existing.Count) file(s) with $(Split-Path $PfxPath -Leaf)" -ForegroundColor Cyan
Write-Host "   signtool: $signToolPath"
Write-Host "   timestamp: $TimestampUrl"

$passArgs = @()
if (-not [string]::IsNullOrEmpty($PfxPassword)) {
    $passArgs = @("/p", $PfxPassword)
}

foreach ($file in $existing) {
    Write-Host "   signing: $file"
    & $signToolPath sign `
        /fd SHA256 `
        /td SHA256 `
        /tr $TimestampUrl `
        /f $PfxPath `
        @passArgs `
        $file
    if ($LASTEXITCODE -ne 0) {
        throw "signtool sign failed for $file (exit $LASTEXITCODE)"
    }
}

Write-Host ">> Verifying signatures" -ForegroundColor Cyan
foreach ($file in $existing) {
    & $signToolPath verify /pa /tw $file
    if ($LASTEXITCODE -ne 0) {
        throw "signtool verify failed for $file (exit $LASTEXITCODE)"
    }
    $sig = Get-AuthenticodeSignature -FilePath $file
    if ($sig.Status -ne "Valid") {
        throw "Get-AuthenticodeSignature status for $file is '$($sig.Status)' (expected Valid). Signer: $($sig.SignerCertificate.Subject)"
    }
    Write-Host "   OK: $(Split-Path $file -Leaf) — $($sig.SignerCertificate.Subject)" -ForegroundColor Green
}
