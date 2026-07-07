# Validates assistant plugin resolution and launch command lines for UODreams Launcher.
$ErrorActionPreference = "Stop"

$tests = @(
    @{
        Name = "Nessuno"
        Path = ""
        ExpectedDll = $null
        Client = "cuo-modded.exe"
    },
    @{
        Name = "ClassicAssist"
        Path = $env:CLASSICASSIST_PATH
        Candidates = @("ClassicAssist.dll")
        Client = "cuo-modded.exe"
    },
    @{
        Name = "Razor Enhanced"
        Path = $env:RAZOR_PATH
        Candidates = @("RazorEnhanced.exe", "RazorEnhanced.dll")
        Client = "ClassicUO.exe"
    },
    @{
        Name = "Orion"
        Path = "c:\Orion Launcher"
        Candidates = @("OA\OrionAssistant64.dll", "OrionAssistant64.dll")
        Client = "cuo-modded.exe"
    },
    @{
        Name = "UOSteam"
        Path = "c:\Program Files (x86)\UOS"
        Candidates = @("UOS.dll")
        Client = "cuo-modded.exe"
        Expect32Bit = $true
    }
)

function Resolve-PluginDll([string]$assistantPath, [string[]]$candidates) {
    if ([string]::IsNullOrWhiteSpace($assistantPath)) { return $null }
    $path = $assistantPath.Trim().Trim('"')
    if (Test-Path -LiteralPath $path -PathType Leaf) { return (Resolve-Path -LiteralPath $path).Path }
    if (Test-Path -LiteralPath $path -PathType Container) {
        foreach ($name in $candidates) {
            $candidate = Join-Path $path $name
            if (Test-Path -LiteralPath $candidate) { return (Resolve-Path -LiteralPath $candidate).Path }
        }
    }
    return $null
}

function Get-PeMachine([string]$file) {
    $bytes = [IO.File]::ReadAllBytes($file)
    $pe = [BitConverter]::ToInt32($bytes, 0x3C)
    return [BitConverter]::ToUInt16($bytes, $pe + 4)
}

Write-Host "UODreams assistant verification" -ForegroundColor Cyan
Write-Host ""

foreach ($t in $tests) {
    Write-Host "[$($t.Name)]" -ForegroundColor Yellow
    $dll = Resolve-PluginDll $t.Path $t.Candidates
    if ($t.Name -eq "Nessuno") {
        Write-Host "  plugin: (none)"
        Write-Host "  status: OK"
        Write-Host ""
        continue
    }

    if (-not $dll) {
        Write-Host "  plugin: NOT FOUND"
        Write-Host "  status: SKIP (path missing on this machine)"
        Write-Host ""
        continue
    }

    $machine = Get-PeMachine $dll
    $is64 = $machine -eq 0x8664
    $cmd = "cuo-modded.exe -ip login.uodreams.com -port 2593 -uopath <uo> -encryption 0 -plugins `"$dll`""
    if ($t.Client -eq "ClassicUO.exe") {
        $cmd = "ClassicUO.exe (bootstrap) -ip login.uodreams.com -port 2593 -uopath <uo> -encryption 0 -plugins `"$dll`""
    }

    Write-Host "  plugin: $dll"
    Write-Host "  PE machine: 0x$($machine.ToString('X4')) ($(if ($is64) { 'x64 OK' } else { 'x86' }))"
    Write-Host "  client: $($t.Client)"
    Write-Host "  command: $cmd"

    if ($t.Expect32Bit) {
        Write-Host "  status: EXPECTED FAIL (32-bit plugin incompatible with x64 CUO)"
    }
    elseif ($is64) {
        Write-Host "  status: OK (compatible)"
    }
    else {
        Write-Host "  status: WARN (32-bit plugin)"
    }
    Write-Host ""
}
