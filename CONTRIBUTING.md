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
