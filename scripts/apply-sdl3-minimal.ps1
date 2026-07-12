# Minimal SDL2 -> SDL3 source migration (keeps UODreams renderer/scene code intact).
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
)

$ErrorActionPreference = "Stop"
$srcDirs = @(
    (Join-Path $RepoRoot "src\ClassicUO.Client"),
    (Join-Path $RepoRoot "src\ClassicUO.Renderer"),
    (Join-Path $RepoRoot "src\ClassicUO.Utility")
)

$files = foreach ($dir in $srcDirs) {
    Get-ChildItem $dir -Recurse -Filter *.cs | ForEach-Object { $_.FullName }
}

foreach ($file in $files) {
    $text = [IO.File]::ReadAllText($file)
    $orig = $text

    $text = $text.Replace('using SDL2;', 'using SDL3;')
    $text = $text.Replace('using static SDL2.SDL;', 'using static SDL3.SDL;')
    $text = $text.Replace('SDL2.SDL', 'SDL3.SDL')

    # SDL3 keymod enum prefix
    $text = $text -replace '\.KMOD_', '.SDL_KMOD_'
    $text = $text -replace 'SDL\.SDL_Keymod\.KMOD_', 'SDL.SDL_Keymod.SDL_KMOD_'

    # Keyboard event layout (SDL3 flattens keysym)
    $text = $text.Replace('e.keysym.sym', '(SDL.SDL_Keycode)e.key')
    $text = $text.Replace('e.keysym.mod', 'e.mod')
    $text = $text.Replace('sdlEvent->key.keysym.sym', '(SDL_Keycode)sdlEvent->key.key')
    $text = $text.Replace('sdlEvent->key.keysym.mod', 'sdlEvent->key.mod')

    # SDL_bool -> bool for common calls
    $text = $text.Replace('SDL_bool.SDL_TRUE', 'true')
    $text = $text.Replace('SDL_bool.SDL_FALSE', 'false')
    $text = $text.Replace('!= SDL.SDL_bool.SDL_FALSE', '!= false')
    $text = $text.Replace('== SDL.SDL_bool.SDL_FALSE', '== false')

    # SDL2 event type names -> SDL3
    $eventMap = @{
        'SDL_EventType.SDL_MOUSEMOTION' = 'SDL_EventType.SDL_EVENT_MOUSE_MOTION'
        'SDL_EventType.SDL_AUDIODEVICEADDED' = 'SDL_EventType.SDL_EVENT_AUDIO_DEVICE_ADDED'
        'SDL_EventType.SDL_AUDIODEVICEREMOVED' = 'SDL_EventType.SDL_EVENT_AUDIO_DEVICE_REMOVED'
        'SDL_EventType.SDL_KEYDOWN' = 'SDL_EventType.SDL_EVENT_KEY_DOWN'
        'SDL_EventType.SDL_KEYUP' = 'SDL_EventType.SDL_EVENT_KEY_UP'
        'SDL_EventType.SDL_TEXTINPUT' = 'SDL_EventType.SDL_EVENT_TEXT_INPUT'
        'SDL_EventType.SDL_MOUSEWHEEL' = 'SDL_EventType.SDL_EVENT_MOUSE_WHEEL'
        'SDL_EventType.SDL_MOUSEBUTTONDOWN' = 'SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN'
        'SDL_EventType.SDL_MOUSEBUTTONUP' = 'SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP'
    }
    foreach ($kv in $eventMap.GetEnumerator()) {
        $text = $text.Replace($kv.Key, $kv.Value)
    }

    if ($text -ne $orig) {
        [IO.File]::WriteAllText($file, $text)
        Write-Host "patched: $file"
    }
}

Write-Host "Minimal SDL3 source migration complete." -ForegroundColor Green
