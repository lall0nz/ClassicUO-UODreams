# UODreams PVP — Launcher & Client v1.1.12

**Ritorno a SDL2 + OpenGL** — questa release corregge crash, OOM e instabilità introdotti dalle versioni SDL3/D3D11 (v1.1.8–v1.1.11).

---

## Novità principali

### Client PVP — stack grafico
- **Revert a SDL2 + OpenGL** (non SDL3, non D3D11 forzato)
- Renderer predefinito: **OpenGL** via settings.json / force_driver
- **Nessun** FNA3D_FORCE_DRIVER=D3D11 dal launcher
- NativeAOT modded cuo.dll (~14 MB) con tutte le mod PVP attive

### Mod e fix inclusi (da v1.1.10+)
- **Energy Field** visual helper (Wall of Stone auto-avoid in Visual Helper)
- **Nuove creature / mount**: Umbrascale, Giant Beetle, mount etereo
- **Altezza cavaliere su mount**, animazioni UOP/MUL, ghost mirror, range indicator
- **StbTextBox**, preset Low Ping, case invisibili, rilevamento versione UO

### Launcher
- Footer donazioni (PayPal / Buy Me a Coffee)
- Fix normalizzazione versione client (v1.1.11)
- Aggiornamento launcher + client preserva profili e dati utente

---

## MIGRAZIONE OBBLIGATORIA da v1.1.8 / v1.1.9 / v1.1.10 / v1.1.11

Le versioni 1.1.8–1.1.11 usavano SDL3 e/o D3D11 forzato. Non è sufficiente cliccare solo Aggiorna: i file nativi vecchi (SDL3.dll, stack D3D11) possono restare nella cartella Client e causare crash.

### Opzione A — Installazione pulita (consigliata)
1. Scarica UODreams-PVP-Launcher-v1.1.12.zip
2. Estrai in una cartella nuova
3. Copia manualmente: Client\Data\Profiles\, Assistant\RazorEnhanced\Profiles/Scripts/Backup, launcher.settings.json (opzionale)
4. Non copiare SDL3.dll, vecchi cuo.dll o settings.json con force_driver D3D11

### Opzione B — Aggiornamento in-place
1. Apri il launcher e clicca Aggiorna
2. Verifica Client\: presente SDL2.dll + cuo.dll (~14 MB), assente SDL3.dll
3. Se SDL3.dll è ancora presente, eliminalo manualmente
4. Rimuovi la variabile d ambiente utente FNA3D_FORCE_DRIVER se impostata
5. Riavvia il launcher

### Cosa viene preservato / sostituito
- Client\Data\Profiles\ — PRESERVATO (macro, gump, profili)
- Client\settings.json — PRESERVATO (preferisci force_driver=1 OpenGL)
- Assistant\RazorEnhanced\Profiles/Scripts — PRESERVATO
- cuo.dll, SDL2.dll, SDL3.dll, runtime nativi — SOSTITUITI
- launcher.settings.json — PRESERVATO

---

## Assets
- UODreams-PVP-Launcher-v1.1.12.zip
- UODreams-PVP-Client-v1.1.12.zip

Pacchetti vergini: nessun profilo, settings.json personale, log o script Razor utente.

---

# UODreams PVP — Launcher & Client v1.1.12 (EN)

Revert to SDL2 + OpenGL — fixes crashes, OOM, and instability from SDL3/D3D11 builds (v1.1.8–v1.1.11).

## Highlights
- Back to SDL2 + OpenGL (not SDL3, not forced D3D11)
- Launcher does not set FNA3D_FORCE_DRIVER=D3D11
- NativeAOT modded cuo.dll (~14 MB) with all PVP mods
- Energy Field helper, creatures/mounts, launcher donation footer

## REQUIRED migration from v1.1.8–v1.1.11
Update alone may leave SDL3.dll / D3D11 stack in Client. Recommended: clean install or delete SDL3.dll after update. Remove user env var FNA3D_FORCE_DRIVER if set.

Preserved: Profiles, settings.json, Razor data, launcher.settings.json. Replaced: cuo.dll and native SDL2 runtime.

## Assets
- UODreams-PVP-Launcher-v1.1.12.zip
- UODreams-PVP-Client-v1.1.12.zip
