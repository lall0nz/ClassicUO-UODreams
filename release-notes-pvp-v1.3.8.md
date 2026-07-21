# 0nE UO Launcher v1.3.8 by lall0ne

## Novita' v1.3.8

### Client ClassicUO
- **Bandage timer Options**: rimossi campi X / Y / Reset Position (sotto al player). Restano **Show Timer Countdown**, drag e **Lock**
- **HP % overhead**: ripristinati i floating `[92%]` sopra i mobile (come prima; insieme alle barre HP quando impostato)
- **Friend/Guild color (Mods)**: il player locale non viene più colorato — solo amici / guild / party (mai "me")
- **Old HP bars (nemici)**: mortal strike → **dark orange** (non più bright yellow). Bianco default e verde poison invariati; player locale invariato

### OTA selettivo
- Manifest **`update.json`** con componenti indipendenti: launcher / client / Razor Enhanced (SHA256)
- Zip legacy launcher (+ Assistant) ancora disponibile per upgraders
- Profilo **`Default PVP`**: copy-only-if-missing — mai sovrascritto
- Profilo stock **`default`**: mai toccato da OTA

### Packaging
- Asset: **`update.json`**, Client zip, Launcher zip (dual exe + Assistant), Assistant-Razor zip, Source zip
- Dual exe: **`0nE UO Launcher.exe`** + **`UODreams Launcher.exe`**
- SDL2 only, pacchetti virgin (niente settings utente / log / bak)

## Come testare l'aggiornamento da v1.3.7

1. Parti da un'installazione **v1.3.7** funzionante.
2. Avvia **`0nE UO Launcher.exe`** → **Aggiornamento disponibile** → **Aggiorna**.
3. Verifica dopo il riavvio:
   - `Client/uodreams-client.version` → **`1.3.8`**
   - `Assistant/RazorEnhanced/uodreams-razor.version` → **`1.3.8`**
   - Titolo launcher → **`0nE UO Launcher v1.3.8`**
4. In Options: timer senza X/Y/Reset; floating `[HP%]` presenti; nemici mortalled con barra arancio scuro; colore amici/guild non tinteggia te stesso.

## Download

| File | Descrizione |
|------|-------------|
| `update.json` | Manifest OTA selettivo (SHA256 + versioni componenti) |
| `UODreams-PVP-by-lall0ne-Launcher-v1.3.8.zip` | **0nE UO Launcher** (+ Assistant per legacy) |
| `UODreams-PVP-by-lall0ne-Client-v1.3.8.zip` | Client ClassicUO modded (virgin) |
| `UODreams-PVP-by-lall0ne-Assistant-Razor-v1.3.8.zip` | Razor Enhanced modded (OTA selettivo) |
| `UODreams-PVP-by-lall0ne-Source-v1.3.8.zip` | Codice sorgente |

Server: `login.uodreams.com:2593`
