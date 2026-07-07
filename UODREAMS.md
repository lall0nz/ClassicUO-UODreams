# UODreams — ClassicUO Custom

Client ClassicUO 1.1.0.0 personalizzato per **UODreams**, con launcher dedicato.

## Funzionalità

- **UODreams Launcher** — avvio client, download client UO, scelta assistente (Nessuno / ClassicAssist / Razor Enhanced)
- **Grid container** — zaino a griglia con salvataggio layout
- **Barre vita classiche** — HP/Mana/Stamina stile vecchio UO
- **Auto-evita ostacoli** — pathfinding migliorato
- Server predefinito: `login.uodreams.com:2593`

## Requisiti

- Windows 10/11 x64
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (per compilare)
- File client Ultima Online (cartella con `tiledata.mul`)
- Per **Razor Enhanced**: stack bootstrap in `Client/Bootstrap/` (vedi script di packaging)

## Compilare

```powershell
# Client modded (ClassicAssist / Nessuno)
dotnet publish src\ClassicUO.Client -c Release -r win-x64 --self-contained true -p:PublishAot=false -o bin\client-out

# Launcher
dotnet publish src\ClassicUO.Launcher.Custom -c Release -r win-x64 --self-contained true -o bin\launcher-out

# Pacchetto completo su Desktop (richiede cuo.dll ufficiale per Razor)
powershell -File scripts\package-uodreams.ps1
```

Output giocabile: `%USERPROFILE%\Desktop\UODreams Launcher`

## Struttura principale

| Percorso | Descrizione |
|----------|-------------|
| `src/ClassicUO.Launcher.Custom/` | Launcher WinForms UODreams |
| `src/ClassicUO.Client/` | Client ClassicUO con mod |
| `scripts/package-uodreams.ps1` | Build + deploy + backup zip |
| `scripts/build-uodreams-icon.py` | Rigenera `uodreams.ico` dal logo |
| `docs/UODreams-GitHub.md` | Guida pubblicazione su GitHub |

## Licenza

Basato su [ClassicUO](https://github.com/ClassicUO/ClassicUO) (BSD-2-Clause). Le modifiche UODreams seguono la stessa licenza del progetto upstream dove applicabile.
