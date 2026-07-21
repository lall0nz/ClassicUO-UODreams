# 0nE UO Launcher v1.3.6 by lall0ne

## Novita' v1.3.6

### Client ClassicUO — Swing Assistant (Mods)
- **Swing bar / Swing Assistant**: nuova sezione **Mods → Swing Assistant** con barra swing timer on-screen (show/lock posizione)
- **Micro-freeze quando swing pronta**: toggle + slider durata **0.2–0.8 s** (step **0.05**, default **0.5 s**) con display decimale
- **Skip freeze con Moving Shot**: micro-freeze disattivato automaticamente mentre l'ability **Moving Shot** e' attiva
- Macro **ToggleSwingReadyMicroFreeze** per attivare/disattivare il micro-freeze in gioco

### Client ClassicUO — Damage numbers
- **Hue personalizzabili** per danni **ricevuti** e **inflitti** (color picker in opzioni)
- **Font dropdown** preset **Originale** / **Old School** (senza slider dimensione font)
- Migrazione automatica profili con preset Unicode "Old School" legacy

### Client ClassicUO — Name Overheads (da v1.3.5, incluso)
- **Ctrl+Shift**: menu filtro NameOverhead visibile **solo mentre i tasti sono premuti**
- **Always show name overheads**: mostra nomi **solo mobile/giocatori** — non tutti gli oggetti a terra
- **Larghezza overhead**: ~50 caratteri (``OBJECT_HANDLES_GUMP_WIDTH`` **450**)

### Baseline v1.3.4 / v1.3.5 (invariato)
- **Hide Carpets / Tappeti**: opzione rinominata; filtro attivo di default con IDs ``0x28A4``-``0x28A6`` in ``carpets.txt``
- **Profilo predefinito PVP**: font Unicode override, journal Unicode, hide carpets, auto-avoid energy field, fast rotation, turn delay **100/45/150/150**
- HP **Both + Smart**, always run, porte/cadaveri automatici, terreno PVP, barre HP classiche, highlight spell range **10**, ghost clone mirror, bandage timer countdown
- **NetClient**: buffer lettura/invio **4096**; NativeAOT ``cuo.dll`` ricompilato; SDL2/OpenGL only

### Razor Enhanced / OTA (invariato da v1.3.4+)
- **OTA aggiorna sempre** gli eseguibili/DLL/plugin Razor in ``Assistant`` ad ogni Update
- Profilo **Default PVP**: copiato **solo se mancante** — mai sovrascritto se gia' presente
- Profilo stock ``default`` **mai** toccato da OTA
- Bundle Razor allineato al brand-test Desktop ``0nE-UO-Launcher-v1.2.8-brand-test``

### Packaging
- Zip launcher con ``0nE UO Launcher.exe`` e ``UODreams Launcher.exe`` (dual exe)
- Client zip: ``UODreams-PVP-by-lall0ne-Client-v1.3.6.zip`` (naming corretto)
- Client e launcher **virgin** (niente settings utente, log, bak)
- Shortcut Desktop 0nE refresh automatico dopo OTA

## Download

| File | Descrizione |
|------|-------------|
| ``UODreams-PVP-by-lall0ne-Launcher-v1.3.6.zip`` | **0nE UO Launcher** + Razor modded |
| ``UODreams-PVP-by-lall0ne-Client-v1.3.6.zip`` | Client ClassicUO modded (virgin) |
| ``UODreams-PVP-by-lall0ne-Source-v1.3.6.zip`` | Codice sorgente |

Server: ``login.uodreams.com:2593``
