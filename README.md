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
