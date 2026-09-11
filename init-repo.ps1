#Requires -Version 5.1
<#
    init-repo.ps1
    Crea l'intera struttura del repository VN Editor.
    Da eseguire UNA VOLTA SOLA, dentro la cartella vuota del repository.

    Uso:
        cd C:\dev\vn-editor
        .\init-repo.ps1
#>

# I comandi esterni (dotnet, git) scrivono spesso su stderr anche quando
# funzionano. Con ErrorActionPreference = "Stop" PowerShell 5.1 li tratta
# come errori fatali, quindi si usa "Continue" e si controlla $LASTEXITCODE.
$ErrorActionPreference = "Continue"

function Invoke-Checked {
    <# Esegue un comando esterno e si ferma solo se il codice di uscita e' diverso da 0 #>
    param(
        [Parameter(Mandatory)][scriptblock] $Comando,
        [Parameter(Mandatory)][string]      $Descrizione
    )
    & $Comando 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "$Descrizione non riuscito (codice $LASTEXITCODE)"
    }
}

Write-Host "=== Creazione struttura VN Editor ===" -ForegroundColor Cyan

# ---------------------------------------------------------------
# 0. Controlli preliminari
# ---------------------------------------------------------------
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet non trovato. Esegui prima setup.ps1 o installa .NET 10 SDK."
}

$sdkVersion = (dotnet --version)
Write-Host "SDK .NET rilevato: $sdkVersion"
if (-not $sdkVersion.StartsWith("10.")) {
    Write-Warning "Atteso .NET 10. Controlla con 'dotnet --list-sdks'."
}

# I template Avalonia servono solo qui, per generare il progetto UI.
# Installarli e' idempotente: se ci sono gia', non succede nulla.
Write-Host "-> Template Avalonia" -ForegroundColor Yellow
dotnet new install Avalonia.Templates 2>&1 | Out-Null

# ---------------------------------------------------------------
# 1. Cartelle radice
# ---------------------------------------------------------------
Write-Host "`n-> Cartelle radice" -ForegroundColor Yellow

$cartelleRadice = @(
    "src",
    "tests",
    "docs",
    "resources/renpy_template",
    "tools",
    ".github/workflows"
)
foreach ($c in $cartelleRadice) {
    New-Item -ItemType Directory -Path $c -Force | Out-Null
}

# ---------------------------------------------------------------
# 2. Soluzione e progetti
# ---------------------------------------------------------------
Write-Host "-> Soluzione e progetti" -ForegroundColor Yellow

Invoke-Checked { dotnet new sln -n VnEditor } "Creazione soluzione"

Invoke-Checked { dotnet new classlib     -o src/VnEditor.Domain         -n VnEditor.Domain         } "Progetto Domain"
Invoke-Checked { dotnet new classlib     -o src/VnEditor.Application    -n VnEditor.Application    } "Progetto Application"
Invoke-Checked { dotnet new classlib     -o src/VnEditor.Infrastructure -n VnEditor.Infrastructure } "Progetto Infrastructure"
Invoke-Checked { dotnet new avalonia.mvvm -o src/VnEditor.UI            -n VnEditor.UI             } "Progetto UI"
Invoke-Checked { dotnet new xunit        -o tests/VnEditor.Tests        -n VnEditor.Tests          } "Progetto Tests"

# I file Class1.cs generati automaticamente non servono
Get-ChildItem -Path src, tests -Filter "Class1.cs" -Recurse | Remove-Item -Force

foreach ($proj in Get-ChildItem -Recurse -Filter *.csproj) {
    Invoke-Checked { dotnet sln add $proj.FullName } "Aggiunta di $($proj.Name) alla soluzione"
}

# ---------------------------------------------------------------
# 3. Riferimenti fra progetti (la direzione delle dipendenze)
# ---------------------------------------------------------------
Write-Host "-> Riferimenti fra progetti" -ForegroundColor Yellow

Invoke-Checked { dotnet add src/VnEditor.Application    reference src/VnEditor.Domain         } "Riferimento Application -> Domain"
Invoke-Checked { dotnet add src/VnEditor.Infrastructure reference src/VnEditor.Application    } "Riferimento Infrastructure -> Application"
Invoke-Checked { dotnet add src/VnEditor.UI             reference src/VnEditor.Infrastructure } "Riferimento UI -> Infrastructure"
Invoke-Checked { dotnet add tests/VnEditor.Tests        reference src/VnEditor.Infrastructure } "Riferimento Tests -> Infrastructure"

# NOTA: VnEditor.Domain non deve MAI avere riferimenti. E' la verifica
# pratica dell'inversione delle dipendenze (SOLID, lettera D).

# ---------------------------------------------------------------
# 4. Cartelle interne dei progetti
# ---------------------------------------------------------------
Write-Host "-> Cartelle interne" -ForegroundColor Yellow

$cartelleInterne = @(
    "src/VnEditor.Domain/Characters",
    "src/VnEditor.Domain/Assets",
    "src/VnEditor.Domain/Scenes",
    "src/VnEditor.Domain/Scenes/Blocks",
    "src/VnEditor.Domain/Variables",
    "src/VnEditor.Domain/Validation",

    "src/VnEditor.Application/Persistence",
    "src/VnEditor.Application/Assets",
    "src/VnEditor.Application/Validation",
    "src/VnEditor.Application/Generation",
    "src/VnEditor.Application/Process",
    "src/VnEditor.Application/Rendering",
    "src/VnEditor.Application/Editing",
    "src/VnEditor.Application/UseCases",

    "src/VnEditor.Infrastructure/Persistence",
    "src/VnEditor.Infrastructure/Assets",
    "src/VnEditor.Infrastructure/Generation",
    "src/VnEditor.Infrastructure/Process",

    "src/VnEditor.UI/ViewModels",
    "src/VnEditor.UI/Views",
    "src/VnEditor.UI/Canvas",

    "tests/VnEditor.Tests/Generation",
    "tests/VnEditor.Tests/Persistence",
    "tests/VnEditor.Tests/Validation"
)
foreach ($c in $cartelleInterne) {
    New-Item -ItemType Directory -Path $c -Force | Out-Null
    # Git non versiona le cartelle vuote: serve un segnaposto
    New-Item -ItemType File -Path (Join-Path $c ".gitkeep") -Force | Out-Null
}

# ---------------------------------------------------------------
# 5. Directory.Build.props — impostazioni comuni a TUTTI i progetti
# ---------------------------------------------------------------
Write-Host "-> Directory.Build.props" -ForegroundColor Yellow

@'
<Project>
  <!--
    Impostazioni applicate automaticamente a ogni progetto della soluzione.
    Evita di ripetere le stesse righe in ogni .csproj.
  -->
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>14</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
</Project>
'@ | Set-Content -Path "Directory.Build.props" -Encoding UTF8

# ---------------------------------------------------------------
# 6. .gitignore
# ---------------------------------------------------------------
Write-Host "-> .gitignore" -ForegroundColor Yellow

Invoke-Checked { dotnet new gitignore --force } "Creazione .gitignore"

@'

# --- Specifico di questo progetto ---
tools/renpy-sdk/
publish/
*.rpyc
progetti-di-prova/
'@ | Add-Content -Path ".gitignore" -Encoding UTF8

# ---------------------------------------------------------------
# 7. README e CONTRIBUTING
# ---------------------------------------------------------------
Write-Host "-> README e CONTRIBUTING" -ForegroundColor Yellow

@'
# VN Editor

Editor di visual novel senza codice. Genera progetti Ren'Py dietro le quinte.

## Requisiti

- Windows 10/11
- .NET 10 SDK
- Visual Studio 2026 Community

## Primo avvio

```powershell
.\setup.ps1      # installa .NET, Git, Visual Studio e l'SDK Ren'Py
dotnet build
dotnet run --project src/VnEditor.UI
```

## Struttura

| Cartella | Contenuto |
|---|---|
| `src/VnEditor.Domain` | Modello: progetto, scene, blocchi, variabili. Nessuna dipendenza. |
| `src/VnEditor.Application` | Interfacce dei servizi e casi d'uso |
| `src/VnEditor.Infrastructure` | Generatore Ren'Py, persistenza JSON, avvio processo |
| `src/VnEditor.UI` | Interfaccia Avalonia |
| `tests/VnEditor.Tests` | Test xunit |
| `resources/renpy_template` | Progetto Ren'Py base, copiato per ogni progetto utente |
| `tools/renpy-sdk` | SDK Ren'Py (non versionato, lo scarica setup.ps1) |

## Versione Ren'Py

Fissata a **8.3.4**. Non aggiornare senza aver rieseguito i test.

## Licenze

Vedi `docs/licenses/`.
'@ | Set-Content -Path "README.md" -Encoding UTF8

@'
# Come si lavora su questo progetto

## Flusso Git

GitHub Flow: `main` sempre funzionante, un branch per issue.

```
git switch -c feature/12-duplica-scena
# ... lavoro, commit piccoli ...
git push -u origin feature/12-duplica-scena
# poi pull request su GitHub, con "Closes #12" nella descrizione
```

Nome del branch: `feature/<numero-issue>-<descrizione-breve>` oppure `fix/...`.

## Messaggi di commit

Conventional Commits:

```
feat(canvas): trascinamento degli sprite con il mouse
fix(generator): escape delle parentesi graffe nei dialoghi
refactor(domain): sposta il registro blocchi in Infrastructure
test(generator): copertura del blocco condizione
docs: aggiorna il piano di progetto
chore(deps): aggiorna Avalonia a 11.2
```

Ambiti usati: `domain`, `application`, `generator`, `runner`, `canvas`, `ui`, `persistence`.

## Definition of Done

Una issue e' chiusa quando:

- [ ] I criteri di accettazione sono soddisfatti
- [ ] `dotnet build` non produce warning (sono errori)
- [ ] `dotnet test` passa
- [ ] La funzionalita' e' raggiungibile dall'interfaccia (niente pulsanti finti)
- [ ] Le modifiche al modello passano da `history.Edit`

## Regole di architettura

- `VnEditor.Domain` non ha riferimenti a progetti o pacchetti.
- Un tipo per file, nome del file uguale al nome del tipo.
- Nomi in inglese nel codice, italiano solo nei testi mostrati all'utente.
- Mai `if (block is XBlock)`: la differenza va su una proprieta' della classe base.
- Coordinate relative nel dominio, pixel solo dentro il controllo canvas.
'@ | Set-Content -Path "CONTRIBUTING.md" -Encoding UTF8

# ---------------------------------------------------------------
# 8. GitHub Action per la compilazione automatica
# ---------------------------------------------------------------
Write-Host "-> GitHub Action" -ForegroundColor Yellow

@'
name: build

on:
  push:
    branches: [ main ]
  pull_request:

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"
      - run: dotnet restore
      - run: dotnet build --no-restore
      - run: dotnet test --no-build --verbosity normal
'@ | Set-Content -Path ".github/workflows/ci.yml" -Encoding UTF8

# ---------------------------------------------------------------
# 9. Segnaposto per il template Ren'Py
# ---------------------------------------------------------------
@'
Qui va il progetto Ren'Py base.

Come crearlo (una volta sola):
1. Avvia tools/renpy-sdk/renpy.exe
2. "Create New Project", nome "template"
3. Copia il contenuto della cartella creata dentro questa cartella
4. Cancella questo file e committa

La cartella deve contenere: game/, con dentro gui/, script.rpy, options.rpy.
'@ | Set-Content -Path "resources/renpy_template/LEGGIMI.txt" -Encoding UTF8

# ---------------------------------------------------------------
# 10. Compilazione di prova e primo commit
# ---------------------------------------------------------------
Write-Host "`n-> Compilazione di prova" -ForegroundColor Yellow
dotnet build
if ($LASTEXITCODE -ne 0) {
    Write-Warning "La compilazione ha segnalato problemi. La struttura e' comunque creata."
}

if (Get-Command git -ErrorAction SilentlyContinue) {
    if (-not (Test-Path ".git")) {
        Write-Host "-> Inizializzazione Git" -ForegroundColor Yellow
        git init 2>&1 | Out-Null
        git branch -M main 2>&1 | Out-Null
    }
    git add -A 2>&1 | Out-Null
    git commit -m "chore: struttura iniziale del progetto" 2>&1 | Out-Null
    Write-Host "-> Primo commit creato" -ForegroundColor Yellow
} else {
    Write-Warning "Git non trovato: salto il primo commit."
}

Write-Host "`n=== Fatto ===" -ForegroundColor Green
Write-Host "Prossimi passi:"
Write-Host "  1. git remote add origin <url-del-tuo-repository>"
Write-Host "  2. git push -u origin main"
Write-Host "  3. Crea il template Ren'Py (vedi resources/renpy_template/LEGGIMI.txt)"
Write-Host "  4. Apri VnEditor.sln"