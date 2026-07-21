# UODreams release packaging

GitHub release client zips must be **virgin**: they must not contain personal account data or other user-specific runtime files from a developer machine.

## Excluded from release packages (PVP and Classic)

Packaging scripts (`scripts/package-release.ps1`, `scripts/package-uodreams.ps1`) call `Clear-UserClientData` before creating zips. The following paths under `Client/` are removed even if present in the build output:

| Path | Reason |
|------|--------|
| `Data/Profiles/` | Per-account/character profiles, macros, saved gumps, `lastcharacter.json` |
| `Data/Client/JournalLogs/` | Saved journal text files |
| `Data/Client/*.usr` | User world-map markers |
| `settings.json` | Client login/settings (username, password, window state) |
| `Logs/` | Runtime log files |
| `Bootstrap/Data/Profiles/` | Same as above (legacy dual-client layout) |
| `Bootstrap/Data/Client/JournalLogs/` | Journal logs in bootstrap copy |
| `Bootstrap/settings.json` | Bootstrap client settings |
| `Bootstrap/Logs/` | Bootstrap runtime logs |
| `Bootstrap/Data/Client/*.usr` | User markers in bootstrap copy |

Razor Enhanced user data is stripped separately:

- `Assistant/RazorEnhanced/Profiles`, `Scripts`, `Backup`, `_deploy_pending`
- Legacy `Data/Plugins/Profiles`, `Scripts`, `Backup`, `_deploy_pending` (and under `RazorEnhanced*` subfolders)

## Kept in releases

- Default/bundled assets only: `Data/XmlGumps`, `ExternalImages`, stock `Data/Client` tables, empty `Data/Plugins` (PVP)
- PVP launcher zip ships `UODreams Launcher.exe` plus virgin `Assistant/RazorEnhanced/` (modded Razor P.E.)
- Classic launcher zip ships only `UODreams Launcher.exe` (no `launcher.settings.json`)

## Preserved on client update

`ClientRuntimeDownloader` backs up the paths above before extracting a new client zip, skips matching zip entries during extract, then restores the backup. User data survives launcher-driven client updates the same way Razor profiles already did.

`Assistant/RazorEnhanced/Profiles`, `Scripts`, and `Backup` are preserved across client updates. Launcher self-updates merge new `Assistant/` files without overwriting existing Razor user data.

Launcher settings (`launcher.settings.json` next to the exe) are never part of the client zip and are not overwritten by client updates. Client extract also backs up / skips that file. Install-root `Logs\` (including `launcher.log`, `client-crash-*.txt`, and `razor-crash-*.txt`) is preserved across client OTA the same way — never wiped on update. Launcher self-update only replaces the exe (+ Assistant merge) and never resets user paths. Client bootstrap/repair after a missing Client folder must not call `ResetUserPaths` — virgin installs simply have no settings file (empty UO path); configured installs keep `UoDirectory`, assistant selection, theme, language, etc.

## Crash reports (support)

On crash, ClassicUO and Razor Enhanced write full exception reports under:

- `<launcher_root>\Logs\client-crash-YYYY-MM-DD_HH-mm-ss.txt`
- `<launcher_root>\Logs\razor-crash-YYYY-MM-DD_HH-mm-ss.txt`

ClassicUO also keeps writing under `Client\Logs\` (dual write). For support, zip the entire `Logs\` folder next to the launcher exe.
