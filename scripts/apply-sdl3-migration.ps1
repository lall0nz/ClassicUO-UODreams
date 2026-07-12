# LOCAL TEST ONLY: migrate PVP client sources from Dust765-Light SDL3 reference.
# Run on feature/sdl3-beta-local branch only — does not touch master/SDL2 releases.
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$RefRoot = (Join-Path $RepoRoot "temp-sources\Dust765-Light\Dust765-Light-1.1.1.24"),
    [string]$OfficialCuo = "$env:USERPROFILE\Downloads\ClassicUOLauncher-win-x64-release\ClassicUO"
)

$ErrorActionPreference = "Stop"

function Write-Step($msg) { Write-Host ">> $msg" -ForegroundColor Cyan }

$srcRoot = Join-Path $RepoRoot "src"
$refSrc = Join-Path $RefRoot "src"

if (-not (Test-Path $refSrc)) {
    throw "SDL3 reference tree not found: $refSrc"
}

# Files with full SDL3 port in Dust765-Light (safe to overlay; UODreams-specific logic lives elsewhere).
$overlayFiles = @(
    "ClassicUO.Client\GameController.cs",
    "ClassicUO.Client\Client.cs",
    "ClassicUO.Client\PluginHost.cs",
    "ClassicUO.Client\Input\Keyboard.cs",
    "ClassicUO.Client\Input\KeysTranslator.cs",
    "ClassicUO.Client\Input\Mouse.cs",
    "ClassicUO.Client\Input\InputEventArgs.cs",
    "ClassicUO.Client\Game\GameCursor.cs",
    "ClassicUO.Client\Game\UoAssist.cs",
    "ClassicUO.Client\Game\Scenes\Scene.cs",
    "ClassicUO.Client\Game\Scenes\GameScene.cs",
    "ClassicUO.Client\Game\Scenes\GameSceneInputHandler.cs",
    "ClassicUO.Client\Game\Managers\HotkeysManager.cs",
    "ClassicUO.Client\Game\Managers\MacroManager.cs",
    "ClassicUO.Client\Game\UI\Controls\Control.cs",
    "ClassicUO.Client\Game\UI\Controls\HotkeyBox.cs",
    "ClassicUO.Client\Game\UI\Controls\MacroControl.cs",
    "ClassicUO.Client\Game\UI\Controls\StbTextBox.cs",
    "ClassicUO.Client\Game\UI\Gumps\HealthBarGump.cs",
    "ClassicUO.Client\Game\UI\Gumps\InspectorGump.cs",
    "ClassicUO.Client\Game\UI\Gumps\MessageBoxGump.cs",
    "ClassicUO.Client\Game\UI\Gumps\StandardSkillsGump.cs",
    "ClassicUO.Client\Game\UI\Gumps\SystemChatControl.cs",
    "ClassicUO.Client\Game\UI\Gumps\WorldMapGump.cs",
    "ClassicUO.Client\Game\UI\Gumps\Login\CharacterSelectionGump.cs",
    "ClassicUO.Client\Game\UI\Gumps\Login\LoadingGump.cs",
    "ClassicUO.Client\Game\UI\Gumps\Login\LoginGump.cs",
    "ClassicUO.Client\Game\UI\Gumps\Login\ServerSelectionGump.cs",
    "ClassicUO.Renderer\Arts\Art.cs",
    "ClassicUO.Utility\StringHelper.cs"
)

Write-Step "Overlaying SDL3-migrated sources from Dust765-Light"
foreach ($rel in $overlayFiles) {
    $from = Join-Path $refSrc $rel
    $to = Join-Path $srcRoot $rel
    if (-not (Test-Path $from)) {
        Write-Host "  skip (missing in reference): $rel" -ForegroundColor DarkYellow
        continue
    }
    $dir = Split-Path $to -Parent
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    Copy-Item -Force $from $to
    Write-Host "  $rel" -ForegroundColor Green
}

Write-Step "Preparing SDL3 native runtime (external/x64-sdl3)"
$sdl3Dir = Join-Path $RepoRoot "external\x64-sdl3"
New-Item -ItemType Directory -Path $sdl3Dir -Force | Out-Null

$nativeSources = @(
    (Join-Path $RefRoot "external\x64"),
    $OfficialCuo
)

$nativeNames = @('zlib.dll','SDL3.dll','FAudio.dll','FNA3D.dll','libtheorafile.dll')
foreach ($name in $nativeNames) {
    $copied = $false
    foreach ($dir in $nativeSources) {
        $src = Join-Path $dir $name
        if (Test-Path $src) {
            Copy-Item -Force $src (Join-Path $sdl3Dir $name)
            $copied = $true
            break
        }
    }
    if (-not $copied -and $name -ne 'SDL2.dll') {
        throw "Could not find $name for SDL3 runtime"
    }
}

Write-Host "SDL3 runtime DLLs in $sdl3Dir" -ForegroundColor Green
Get-ChildItem $sdl3Dir | Format-Table Name, Length -AutoSize
