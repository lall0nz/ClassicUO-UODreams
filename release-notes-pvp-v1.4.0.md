# 0nE UO Launcher v1.4.0 by lall0ne

## Novita' v1.4.0 / What's new

### Crash reporter (NEW)
- **Client + Razor** scrivono crash report completi (versione, OS, stack) in `<install>\Logs\`
  - `Logs\client-crash-YYYY-MM-DD_HH-mm-ss.txt`
  - `Logs\razor-crash-YYYY-MM-DD_HH-mm-ss.txt`
- ClassicUO continua anche in `Client\Logs\` (dual write)
- Razor: file log primario; **nessuna UI DoctorDump** se `Engine.Closing` (teardown)
- Cartella `Logs\` creata se mancante; **preservata** da OTA (come settings)
- Per supporto: zippare l'intera cartella `Logs\` (include anche `launcher.log`)

### Client ClassicUO
- **Friend highlight**: bridge `IsFriend` — amici Razor con colore Mods; **player locale escluso**
- **Mods status colors**: fix para/stun/poison/mortal/LT; UI Mods a una riga; **ModernColorPicker** (stile TazUO)
- **Grid backpack**: hue sfondo + opacità
- **HP % overhead**; mortal enemy → **dark orange**; bandage timer senza X/Y/Reset
- **Login footer**: rimossi Support/Website/Discord; footer **`0nE UO Version {ver}`**
- **Button.Hue** tint sui gump
- Weapon ability **0xAED1** Double Strike / Whirlwind (`PlayerMobile`)
- Persistenza settings/path su OTA (no `ResetUserPaths` su repair client)
- Null-safe walk/profile su teardown; guard `RequestMove`

### Razor Enhanced
- SpecialMoves **0xAED1** (allineato al client)
- PacketHandler snapshot / `Closing` / no ReportCrash su exit viewers
- Critical+High antifizzle: no wait nei packet handler; `CastTargetedGeneric` senza wait 0x6C
- HotKey / SafeAction guard `Closing`; `EnhancedScript` Join(2000)
- Logout: `StopAll` + `CloseAllScriptGumps` + `SendGump` solo se Player esiste
- **Crash file writer** → launcher `Logs\`

### Launcher / OTA
- Persistenza path/settings su update
- Manifest selettivo **`update.json`** (launcher / client / Razor)
- Dual exe nello zip Launcher; pacchetti virgin
- `Logs\` install-root preservati su extract client

## Come testare l'aggiornamento da v1.3.9

1. Parti da un'installazione **v1.3.9** funzionante.
2. Avvia **`0nE UO Launcher.exe`** → **Aggiornamento disponibile** → **Aggiorna**.
3. Verifica dopo il riavvio:
   - `Client/uodreams-client.version` → **`1.4.0`**
   - `Assistant/RazorEnhanced/uodreams-razor.version` → **`1.4.0`**
   - Titolo launcher → **`0nE UO Launcher v1.4.0`**
4. Login: solo **0nE UO Version**; niente link Support/Website/Discord.
5. Crash test (opzionale): dopo un crash, controlla `Logs\client-crash-*.txt` / `Logs\razor-crash-*.txt` e che `Logs\` resti dopo un OTA successivo.

## Download

| File | Descrizione |
|------|-------------|
| `update.json` | Manifest OTA selettivo (SHA256 + versioni componenti) |
| `UODreams-PVP-by-lall0ne-Launcher-v1.4.0.zip` | **0nE UO Launcher** (+ Assistant per legacy) |
| `UODreams-PVP-by-lall0ne-Client-v1.4.0.zip` | Client ClassicUO modded (virgin) |
| `UODreams-PVP-by-lall0ne-Assistant-Razor-v1.4.0.zip` | Razor Enhanced modded (OTA selettivo) |
| `UODreams-PVP-by-lall0ne-Source-v1.4.0.zip` | Codice sorgente |

Server: `login.uodreams.com:2593`

---

### English summary
Crash reporter writes client/razor dumps to install-root `Logs\` (preserved across OTA). Client: friend colors, Mods fixes, ModernColorPicker, grid backpack hue, HP%, login branding, 0xAED1 abilities, settings persistence. Razor: antifizzle/Closing guards, 0xAED1, crash files. Launcher: selective OTA, dual exe, virgin packages.
