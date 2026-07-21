# 0nE UO Launcher v1.4.1 by lall0ne

## Novita' v1.4.1 / What's new

### Razor Enhanced — Antifizzle
- Hotkey / SpellGrid: cast + antifizzle wait su **ThreadPool** (non sul thread packet/hotkey CUO)
- Evita starvation di `OnRecv`: `WaitForTargetOrFizzle` non scade a vuoto durante il cast
- `Data\Spells.json` + `Data\Masteries.json` (timeout CA) inclusi nel bundle OTA
- `CastTargetedGeneric` senza wait 0x6C (target gia' embedded)

### Client ClassicUO — Friend Mods color
- Colore amici/gilda Mods: **solo body / aura / mount**
- **Name overhead** mantiene hue originale di **notoriety / guild** (non viene ritinto)

### Client — Login session logging (sempre attivo)
- Scrive `Logs\client-session-YYYY-MM-DD_HH-mm-ss.txt` nella root install del launcher
- Trace degli stage fino a in-world ready (diagnostica login / hang)

### Crash reporter (da v1.4.0, invariato)
- `Logs\client-crash-*` / `Logs\razor-crash-*` preservati da OTA

### Packaging / Desktop
- Brand-test deploy: aggiornamento **incrementale** — **non** cancella Profiles / settings / Logs
- Pacchetti GitHub **virgin** (niente profili/settings/log utente)

## Come testare l'aggiornamento da v1.4.0

1. Parti da un'installazione **v1.4.0** funzionante.
2. Avvia **`0nE UO Launcher.exe`** → **Aggiornamento disponibile** → **Aggiorna**.
3. Verifica dopo il riavvio:
   - `Client/uodreams-client.version` → **`1.4.1`**
   - `Assistant/RazorEnhanced/uodreams-razor.version` → **`1.4.1`**
   - Titolo launcher → **`0nE UO Launcher v1.4.1`**
   - `Assistant/RazorEnhanced/Data/Spells.json` presente
4. Amici Mods: body colorato, nome overhead ancora notoriety/guild.
5. Antifizzle hotkey: cast Chivalry Heal senza blocco client.
6. Dopo un login: `Logs\client-session-*.txt` aggiornato.

## Download

| File | Descrizione |
|------|-------------|
| `update.json` | Manifest OTA selettivo (SHA256 + versioni componenti) |
| `UODreams-PVP-by-lall0ne-Launcher-v1.4.1.zip` | **0nE UO Launcher** (+ Assistant per legacy) |
| `UODreams-PVP-by-lall0ne-Client-v1.4.1.zip` | Client ClassicUO modded (virgin) |
| `UODreams-PVP-by-lall0ne-Assistant-Razor-v1.4.1.zip` | Razor Enhanced modded (OTA selettivo) |
| `UODreams-PVP-by-lall0ne-Source-v1.4.1.zip` | Codice sorgente |

Server: `login.uodreams.com:2593`

---

### English summary
Antifizzle hotkeys wait on ThreadPool (not packet handler); Spells/Masteries JSON shipped. Friend Mods tint body/aura only — name overhead keeps notoriety/guild hue. Always-on `Logs\client-session-*.txt`. Crash logs unchanged from 1.4.0. Selective OTA virgin packages; Desktop brand-test redeploy preserves Profiles/settings/Logs.
