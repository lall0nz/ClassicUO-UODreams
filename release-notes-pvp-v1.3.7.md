# 0nE UO Launcher v1.3.7 by lall0ne

## Novita' v1.3.7

### OTA selettivo (update.json)
- Nuovo manifest **`update.json`** su ogni release GitHub con tre componenti indipendenti: **launcher**, **client**, **Razor Enhanced**
- Il launcher legge il manifest e scarica **solo** i pacchetti necessari (con verifica **SHA256**)
- Dialogo aggiornamento mostra quali componenti cambiano (`v1.3.6 -> v1.3.7`) e la dimensione stimata
- Marker versione Razor: **`Assistant/RazorEnhanced/uodreams-razor.version`**
- **Compatibilita' 1.3.6**: zip legacy (launcher con Assistant incluso + client) restano disponibili; il primo salto da 1.3.6 usa ancora il flusso legacy, poi OTA selettivo attivo

### Razor Enhanced — anti-fizzle (Classic Assist)
- Importata logica **anti-fizzle** da Classic Assist nel bundle modded (brand-test Desktop 0nE)
- Riduce freeze/micro-lag su pathing e azioni rapide in PVP
- Profilo **`Default PVP`**: copiato **solo se mancante** — mai sovrascritto se gia' presente
- Profilo stock **`default`**: **mai** toccato da OTA

### Client ClassicUO (da v1.3.5–1.3.6, incluso)
- **Swing Assistant** (Mods → Swing Assistant), micro-freeze configurabile, skip con Moving Shot
- **Damage numbers**: hue personalizzabili, preset font Originale / Old School
- **Name Overheads**: Ctrl+Shift menu, always-show solo mobile/giocatori, larghezza ~50 caratteri
- NativeAOT **`cuo.dll`**, SDL2/OpenGL only, buffer NetClient **4096**

### Packaging
- Asset release: **`update.json`**, Client zip, Launcher zip (Assistant incluso per upgraders 1.3.6), **Assistant-Razor zip** separato, Source zip
- Dual exe: **`0nE UO Launcher.exe`** + **`UODreams Launcher.exe`**
- Pacchetti **virgin** (niente settings utente, log, bak)

## Come testare l'aggiornamento da v1.3.6

1. Parti da un'installazione **v1.3.6** funzionante (launcher + client + Razor).
2. Avvia **`0nE UO Launcher.exe`** → clic **Aggiornamento disponibile** → **Aggiorna**.
3. **Primo salto (1.3.6 → 1.3.7)**: il launcher 1.3.6 usa ancora il flusso **legacy** (zip launcher+client accoppiati) — e' normale, un update piu' grande una sola volta.
4. Dopo il riavvio verifica in cartella installazione:
   - `Client/uodreams-client.version` → **`1.3.7`**
   - `Assistant/RazorEnhanced/uodreams-razor.version` → **`1.3.7`**
   - Titolo launcher → **`0nE UO Launcher v1.3.7`**
5. **OTA selettivo (dalla 1.3.7 in poi)**: alla prossima release con solo client o solo Razor cambiato, il dialogo mostrera' **solo** quel componente e scarichera' solo quello zip.
6. Controlla che **`Profiles/Default PVP`** e **`Profiles/default`** non siano stati sovrascritti se gia' presenti prima dell'update.

## Download

| File | Descrizione |
|------|-------------|
| `update.json` | Manifest OTA selettivo (SHA256 + versioni componenti) |
| `UODreams-PVP-by-lall0ne-Launcher-v1.3.7.zip` | **0nE UO Launcher** (+ Assistant per legacy 1.3.6) |
| `UODreams-PVP-by-lall0ne-Client-v1.3.7.zip` | Client ClassicUO modded (virgin) |
| `UODreams-PVP-by-lall0ne-Assistant-Razor-v1.3.7.zip` | Razor Enhanced modded (OTA selettivo) |
| `UODreams-PVP-by-lall0ne-Source-v1.3.7.zip` | Codice sorgente |

Server: `login.uodreams.com:2593`
