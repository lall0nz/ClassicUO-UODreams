# Guida GitHub — UODreams / ClassicUO-Custom

Repository **pubblico** con **solo sorgenti** (niente zip del pacchetto giocabile né binari pesanti).

---

## 1. Prerequisiti

Installa questi strumenti una volta sola:

### Git
Probabilmente già presente. Verifica:
```powershell
git --version
```

### GitHub CLI
```powershell
winget install --id GitHub.cli -e
```
Chiudi e riapri il terminale, poi:
```powershell
gh auth login
```
Scegli: **GitHub.com** → **HTTPS** → **Login with a web browser** e segui il codice a schermo.

### .NET 8 SDK
```powershell
winget install Microsoft.DotNet.SDK.8
```

---

## 2. Preparare il repository locale

Apri PowerShell nella cartella del progetto:

```powershell
cd C:\Users\simon\Projects\ClassicUO-Custom
```

### Submodule (FNA, MP3Sharp)
Obbligatorio per compilare:
```powershell
git submodule update --init --recursive
```

### Cosa viene ignorato da Git
Già configurato in `.gitignore`:
- `_vanilla_ref/`, `_launcher_ref/` — copie di riferimento locali
- `bin/`, `intermediate/` — output di build
- `launcher.settings.json` — impostazioni personali del launcher
- zip di backup sul Desktop

---

## 3. Primo commit

```powershell
git status
git add .gitignore UODREAMS.md docs/ scripts/ launcher.settings.example.json
git add src/ClassicUO.Launcher.Custom/
git add src/ClassicUO.Client/
git add src/ClassicUO.Bootstrap/
# aggiungi gli altri file modificati del client
git add -u
git status
```

Controlla che **non** compaiano cartelle enormi (`_vanilla_ref`, `bin`, zip). Poi:

```powershell
git commit -m "UODreams: launcher, grid container, barre vita classiche e mod client"
```

---

## 4. Creare il repository su GitHub

Sostituisci `TUO-UTENTE` con il tuo username GitHub (es. `simon`):

```powershell
gh repo create TUO-UTENTE/ClassicUO-UODreams --public --source=. --remote=origin --description "ClassicUO custom client e launcher per UODreams"
```

Oppure crea il repo manualmente su [github.com/new](https://github.com/new) e collega il remote:

```powershell
git remote add origin https://github.com/TUO-UTENTE/ClassicUO-UODreams.git
```

### Push (branch `master` → GitHub)

```powershell
git push -u origin master
```

Se preferisci il branch `main`:
```powershell
git branch -M main
git push -u origin main
```

---

## 5. Verifica

```powershell
gh repo view --web
```

Apri il repo nel browser: dovresti vedere sorgenti, `UODREAMS.md`, `scripts/package-uodreams.ps1`, ecc.

---

## 6. Workflow consigliato (dopo il primo push)

| Azione | Comando |
|--------|---------|
| Salvare modifiche | `git add .` → `git commit -m "descrizione"` → `git push` |
| Aggiornare submodule | `git submodule update --remote` |
| Clonare su altro PC | `git clone --recurse-submodules https://github.com/TUO-UTENTE/ClassicUO-UODreams.git` |

---

## 7. Note importanti

- **Non committare** il pacchetto `UODreams Launcher` dal Desktop (centinaia di MB, include .NET self-contained).
- **Non committare** file UO (`tiledata.mul`, ecc.) — sono protetti da copyright.
- Il repo pubblico contiene **codice sorgente** sotto licenza ClassicUO (BSD-2-Clause); le tue mod UODreams restano nel repo.
- Per distribuire il gioco ai giocatori usa zip locale, Google Drive, o in futuro **GitHub Releases** (opzionale).

---

## 8. Risoluzione problemi

| Problema | Soluzione |
|----------|-----------|
| `gh` non riconosciuto | Riavvia terminale dopo `winget install GitHub.cli` |
| Push rifiutato (auth) | `gh auth login` di nuovo |
| File troppo grande | Verifica `.gitignore`; non aggiungere `bin/` o zip |
| Build fallisce dopo clone | `git submodule update --init --recursive` |
| Submodule vuoti | Stesso comando sopra |

---

## Comandi rapidi (copia-incolla)

Dopo `gh auth login`, con **username già impostato**:

```powershell
cd C:\Users\simon\Projects\ClassicUO-Custom
git submodule update --init --recursive
git add -A
git status
# controlla l'elenco, poi:
git commit -m "UODreams: launcher e mod client ClassicUO"
gh repo create NOME-UTENTE/ClassicUO-UODreams --public --source=. --remote=origin --push
```

Sostituisci `NOME-UTENTE` con il tuo account GitHub.
