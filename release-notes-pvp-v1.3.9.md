# 0nE UO Launcher v1.3.9 by lall0ne

## Novita' v1.3.9

### Client ClassicUO
- **Friend highlight (Bootstrap↔Razor)**: bridge `IsFriend` — gli amici Razor (lista rossa) usano il colore **Mods** impostato
- **Player locale escluso**: il personaggio locale non viene più colorato come friend/guild
- **Black / White (Nero / Bianco)**: pulsanti rapidi accanto a tutti i color picker di **Mods** e **Containers**
- **Grid backpack background**: color picker `AltGridContainerBackgroundHue` + controllo opacità
- **HP % overhead**: floating `[92%]` sopra i mobile ripristinati
- **Old HP bars (nemici)**: mortal strike → **dark orange** (non più bright yellow)
- **Bandage timer Options**: rimossi campi X / Y / Reset Position; restano Show Timer Countdown, drag e **Lock**

### OTA selettivo
- Manifest **`update.json`** con componenti indipendenti: launcher / client / Razor Enhanced (SHA256)
- Zip legacy launcher (+ Assistant) ancora disponibile per upgraders
- Profilo **`Default PVP`**: copy-only-if-missing — mai sovrascritto
- Profilo stock **`default`**: mai toccato da OTA

### Packaging
- Asset: **`update.json`**, Client zip, Launcher zip (dual exe + Assistant), Assistant-Razor zip, Source zip
- Dual exe: **`0nE UO Launcher.exe`** + **`UODreams Launcher.exe`**
- SDL2 only, pacchetti virgin (niente settings utente / log / bak)

## Come testare l'aggiornamento da v1.3.8

1. Parti da un'installazione **v1.3.8** funzionante.
2. Avvia **`0nE UO Launcher.exe`** → **Aggiornamento disponibile** → **Aggiorna**.
3. Verifica dopo il riavvio:
   - `Client/uodreams-client.version` → **`1.3.9`**
   - `Assistant/RazorEnhanced/uodreams-razor.version` → **`1.3.9`**
   - Titolo launcher → **`0nE UO Launcher v1.3.9`**
4. In Options: Nero/Bianco sui color picker Mods/Containers; grid backpack hue+opacity; amici Razor con colore Mods (non te stesso); floating HP%; mortal enemy bar arancio scuro; timer senza X/Y/Reset.

## Download

| File | Descrizione |
|------|-------------|
| `update.json` | Manifest OTA selettivo (SHA256 + versioni componenti) |
| `UODreams-PVP-by-lall0ne-Launcher-v1.3.9.zip` | **0nE UO Launcher** (+ Assistant per legacy) |
| `UODreams-PVP-by-lall0ne-Client-v1.3.9.zip` | Client ClassicUO modded (virgin) |
| `UODreams-PVP-by-lall0ne-Assistant-Razor-v1.3.9.zip` | Razor Enhanced modded (OTA selettivo) |
| `UODreams-PVP-by-lall0ne-Source-v1.3.9.zip` | Codice sorgente |

Server: `login.uodreams.com:2593`
