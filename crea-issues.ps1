#Requires -Version 5.1
<#
    crea-issues.ps1
    Crea etichette, milestone e issue sul repository GitHub.

    Prerequisiti:
      1. GitHub CLI installata:   winget install GitHub.cli
      2. Autenticata:             gh auth login
      3. Repository remoto gia' collegato:
             git remote add origin https://github.com/<utente>/<repo>.git
             git push -u origin main

    Uso:
        .\crea-issues.ps1
        .\crea-issues.ps1 -SoloSprint 1      # solo lo sprint 1
        .\crea-issues.ps1 -DalloSprint 2     # dallo sprint 2 in poi
#>

param(
    [int] $SoloSprint  = -1,   # crea solo le issue di questo sprint
    [int] $DalloSprint = -1    # crea le issue da questo sprint in poi
)

$ErrorActionPreference = "Continue"

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "GitHub CLI non trovata. Installa con: winget install GitHub.cli"
}

gh auth status 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Non autenticato. Esegui: gh auth login" }

# ---------------------------------------------------------------
# Etichette
# ---------------------------------------------------------------
Write-Host "=== Etichette ===" -ForegroundColor Cyan

$etichette = @(
    @{ nome = "feature";        colore = "0E8A16"; desc = "Nuova funzionalita'" },
    @{ nome = "bug";            colore = "D73A4A"; desc = "Qualcosa non funziona" },
    @{ nome = "chore";          colore = "FEF2C0"; desc = "Configurazione, struttura, dipendenze" },
    @{ nome = "docs";           colore = "0075CA"; desc = "Documentazione" },
    @{ nome = "test";           colore = "BFD4F2"; desc = "Test automatici" },
    @{ nome = "architettura";   colore = "5319E7"; desc = "Tocca la struttura del codice" },
    @{ nome = "blocco-cliente"; colore = "B60205"; desc = "Requisito esplicito della specifica" },
    @{ nome = "rischio-alto";   colore = "E99695"; desc = "Sbagliarlo costa riscritture" }
)

foreach ($e in $etichette) {
    $out = & gh label create $e.nome --color $e.colore --description $e.desc --force 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  $($e.nome)"
    } else {
        Write-Warning "Etichetta non creata: $($e.nome)"
        $out | ForEach-Object { Write-Host "      $_" -ForegroundColor DarkGray }
    }
}

# ---------------------------------------------------------------
# Milestone
# ---------------------------------------------------------------
Write-Host "`n=== Milestone ===" -ForegroundColor Cyan

$milestone = @(
    "Sprint 0 - Preparazione",
    "Sprint 1 - Prova di fattibilita'",
    "Sprint 2 - Dominio e persistenza",
    "Sprint 3 - Editor di base (MVP)",
    "Sprint 4 - Canvas visuale",
    "Sprint 5 - Rami, variabili e condizioni",
    "Sprint 6 - Audio",
    "Sprint 7 - Robustezza",
    "Sprint 8 - Anteprima, esportazione e consegna"
)

$repo = gh repo view --json nameWithOwner -q .nameWithOwner
Write-Host "Repository: $repo"

foreach ($m in $milestone) {
    $out = & gh api "repos/$repo/milestones" -f title="$m" 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  $m"
    } elseif ($out -match "already_exists") {
        Write-Host "  $m (gia' presente)" -ForegroundColor DarkGray
    } else {
        Write-Warning "Milestone non creata: $m"
        $out | ForEach-Object { Write-Host "      $_" -ForegroundColor DarkGray }
    }
}

# ---------------------------------------------------------------
# Issue
# ---------------------------------------------------------------
Write-Host "`n=== Issue ===" -ForegroundColor Cyan

$script:Falliti = @()

function New-Issue {
    param(
        [int]      $Sprint,
        [string]   $Titolo,
        [int]      $Punti,
        [string[]] $Etichette,
        [string]   $Corpo,
        [string]   $PuntoSpecifica = $null
    )

    if ($SoloSprint  -ge 0 -and $Sprint -ne $SoloSprint)  { return }
    if ($DalloSprint -ge 0 -and $Sprint -lt $DalloSprint) { return }

    $testo = $Corpo.Trim()
    $testo += "`n`n**Story point:** $Punti"
    if ($PuntoSpecifica) {
        $testo += "`n**Specifica cliente:** punto $PuntoSpecifica"
    }

    $ghArgs = @("issue","create","--title",$Titolo,"--body",$testo,
                "--milestone",$milestone[$Sprint])
    foreach ($l in $Etichette) { $ghArgs += @("--label",$l) }

    $output = & gh @ghArgs 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  [S$Sprint] $Titolo" -ForegroundColor Green
    } else {
        Write-Warning "Fallita: $Titolo"
        $output | ForEach-Object { Write-Host "      $_" -ForegroundColor DarkGray }
        $script:Falliti += $Titolo
    }
}

# --- Sprint 1 ---------------------------------------------------
New-Issue 1 "Scrivere lo script con indentazione corretta" 3 @("feature") @'
Come sviluppatore
voglio una classe che gestisca l'indentazione dello script Ren'Py
cosi' da non contare gli spazi a mano in ogni blocco

### Criteri di accettazione
- [ ] ``IScriptWriter`` con ``Line(text)`` e ``Block(header)`` che restituisce ``IDisposable``
- [ ] Indentazione a 4 spazi, mai tabulazioni
- [ ] Fine riga ``\n`` anche su Windows
- [ ] Scrittura del file in UTF-8 senza BOM
- [ ] Test: blocchi annidati producono l'indentazione attesa
'@

New-Issue 1 "Applicare l'escaping ai testi dell'utente" 3 @("feature","rischio-alto") @'
Come autore di visual novel
voglio poter scrivere qualsiasi carattere nei dialoghi
cosi' da non dover conoscere la sintassi di Ren'Py

### Criteri di accettazione
- [ ] Le parentesi graffe, le quadre, le virgolette e il backslash vengono convertiti correttamente
- [ ] Test con "Ciao {amico}" e con "[nome]"
- [ ] Vale anche per i testi delle scelte
'@ "14"

New-Issue 1 "Generare uno script Ren'Py da un modello minimo" 5 @("feature") @'
### Criteri di accettazione
- [ ] ``define Character`` per ogni personaggio
- [ ] ``label start`` e una label per scena
- [ ] Sfondo, mostra personaggio, dialogo, musica
- [ ] Ogni scena termina con ``jump`` o ``return``
'@

New-Issue 1 "Copiare template e asset nel progetto Ren'Py" 3 @("feature") @'
### Criteri di accettazione
- [ ] Template copiato in ``.renpy_game/``
- [ ] Asset in ``game/images`` e ``game/audio``
- [ ] Nomi dei file interni sicuri
- [ ] I ``.rpyc`` vecchi vengono cancellati prima di rigenerare
'@

New-Issue 1 "Avviare Ren'Py come processo esterno" 5 @("feature") @'
### Criteri di accettazione
- [ ] ``IRenpyRunner`` con ``PlayAsync``, ``LintAsync``, ``DistributeAsync``
- [ ] Usa ``ArgumentList`` (i percorsi contengono spazi)
- [ ] ``CreateNoWindow``: nessuna finestra nera
- [ ] Percorsi assoluti con ``Path.GetFullPath``
- [ ] ``StopCurrent`` chiude l'istanza precedente
- [ ] Non blocca il thread chiamante
'@

# --- Sprint 2 ---------------------------------------------------
New-Issue 2 "Classi di dominio: progetto, personaggi, asset" 5 @("feature","architettura") @'
### Criteri di accettazione
- [ ] Un file per tipo, nomi in inglese
- [ ] ``VnEditor.Domain`` resta senza riferimenti
'@

New-Issue 2 "Classi di dominio: scene e gerarchia dei blocchi" 5 @("feature","architettura") @'
### Criteri di accettazione
- [ ] ``Block`` astratta con ``Label``, ``Summary``, ``Validate``, ``Generate``
- [ ] ``EndsScene`` come proprieta' virtuale
- [ ] Mai ``if (block is XBlock)`` nel generatore
- [ ] La classe base non conosce le sottoclassi
'@

New-Issue 2 "Classi di dominio: variabili" 3 @("feature") @'
### Criteri di accettazione
- [ ] Tipi numero, booleano, testo
- [ ] Operazioni imposta, somma, sottrai, inverti
- [ ] Operatori di confronto come enum
'@ "17"

New-Issue 2 "Serializzazione JSON polimorfica dei blocchi" 5 @("feature","architettura") @'
### Criteri di accettazione
- [ ] ``BlockRegistry`` in Infrastructure, non nel dominio
- [ ] ``BlockTypeResolver`` applica il registro a runtime
- [ ] Test di andata e ritorno per ogni tipo di blocco
- [ ] Test via reflection: ogni sottoclasse e' registrata
- [ ] Il file contiene ``versione_formato``
'@ "4"

New-Issue 2 "Salvataggio e caricamento del progetto" 3 @("feature") @'
### Criteri di accettazione
- [ ] Scrittura su file temporaneo e rinomina
- [ ] Un'interruzione non corrompe il progetto
- [ ] Percorsi relativi dentro il progetto
'@ "25"

New-Issue 2 "Validatore del progetto" 5 @("feature") @'
### Criteri di accettazione
- [ ] Sprite mancanti, scene vuote, salti rotti
- [ ] Messaggi in italiano comprensibili
- [ ] Distinzione fra errori e avvisi
'@

New-Issue 2 "Suite di test del generatore" 5 @("test") @'
### Criteri di accettazione
- [ ] Dato un progetto, lo script generato e' confrontato riga per riga
- [ ] Un test per ogni tipo di blocco
'@

# --- Sprint 3 ---------------------------------------------------
New-Issue 3 "Finestra principale con i quattro pannelli" 5 @("feature") @'
### Criteri di accettazione
- [ ] Pannelli progetto, canvas, ispettore, timeline
- [ ] Ridimensionabili
'@ "6"

New-Issue 3 "Menu File: nuovo, apri, salva, salva con nome" 3 @("feature") @'
### Criteri di accettazione
- [ ] Alla creazione chiede nome e posizione
- [ ] Crea la struttura di cartelle del progetto
- [ ] Ctrl+S e Ctrl+O funzionanti
- [ ] Avvisa se si chiude con modifiche non salvate
'@ "3"

New-Issue 3 "Introdurre IProjectHistory (implementazione vuota)" 3 @("architettura","rischio-alto") @'
Come sviluppatore
voglio che ogni modifica passi da ``history.Edit`` fin da subito
cosi' da non dover riscrivere i ViewModel quando aggiungero' l'undo

### Criteri di accettazione
- [ ] Interfaccia definita in Application
- [ ] Implementazione temporanea che non fa nulla
- [ ] Tutti i ViewModel la usano gia'
'@

New-Issue 3 "Gestione scene: crea, rinomina, elimina" 3 @("feature") @'
### Criteri di accettazione
- [ ] Avvisa se la scena e' destinazione di un salto
'@ "5"

New-Issue 3 "Duplicare una scena" 2 @("feature") @'
Come autore di visual novel
voglio duplicare una scena esistente
cosi' da riusare un'impostazione senza rifarla da zero

### Criteri di accettazione
- [ ] La copia ha tutti i blocchi dell'originale
- [ ] Il nome diventa ``Scena 01 (copia)``
- [ ] Gli ID dei blocchi sono nuovi, non duplicati
- [ ] L'operazione si annulla con Ctrl+Z
'@ "5"

New-Issue 3 "Riordinare le scene e scegliere quella iniziale" 2 @("feature") "" "5"

New-Issue 3 "Browser degli asset" 5 @("feature") @'
### Criteri di accettazione
- [ ] Mostra personaggi, sfondi, audio
- [ ] Importa, rinomina, elimina
- [ ] Eliminare da una scena NON cancella il file sorgente
'@ "7"

New-Issue 3 "Importazione asset con rinomina sicura" 5 @("feature","rischio-alto") @'
### Criteri di accettazione
- [ ] ``Anna-Maria!.png`` diventa ``char1 espr2.png``
- [ ] La corrispondenza nome visibile / interno e' nel JSON
- [ ] Formati non supportati rifiutati con messaggio chiaro
'@

New-Issue 3 "Creatore di personaggi ed espressioni" 5 @("feature") @'
### Criteri di accettazione
- [ ] Nome, nome visualizzato, colore
- [ ] Espressioni importate e assegnate
- [ ] Sprite predefinito
'@ "9, 10"

New-Issue 3 "Selezione dello sfondo di scena" 2 @("feature") @'
### Criteri di accettazione
- [ ] Lo sfondo appare subito sul canvas
'@ "8"

New-Issue 3 "Timeline degli eventi riordinabile" 8 @("feature") @'
### Criteri di accettazione
- [ ] Menu Aggiungi costruito dal registro dei blocchi
- [ ] Riordino con drag and drop
- [ ] Ogni riga mostra il riepilogo del blocco
'@ "13"

New-Issue 3 "Pannello di modifica per tipo di blocco" 5 @("feature","architettura") @'
### Criteri di accettazione
- [ ] Usa ``DataTemplates``, nessuno switch sul tipo
- [ ] Un blocco nuovo richiede solo un template nuovo
'@

New-Issue 3 "Evento dialogo" 3 @("feature") @'
### Criteri di accettazione
- [ ] Personaggio, espressione, testo
- [ ] Compare nella timeline con il riepilogo
'@ "14"

New-Issue 3 "Pulsante Gioca" 3 @("feature") @'
### Criteri di accettazione
- [ ] Genera, valida e avvia
- [ ] Non blocca l'interfaccia
- [ ] Premuto due volte, chiude l'istanza precedente
'@ "23"

# --- Sprint 4 ---------------------------------------------------
New-Issue 4 "SceneStateCalculator" 5 @("feature","architettura") @'
Come sviluppatore
voglio una funzione pura che calcoli cosa si vede al blocco N
cosi' da alimentare canvas, anteprima e validazione con lo stesso codice

### Criteri di accettazione
- [ ] Nessuna dipendenza dall'interfaccia
- [ ] Test completi senza avviare l'app
'@

New-Issue 4 "Controllo canvas: disegno" 5 @("feature") @'
### Criteri di accettazione
- [ ] Disegna sfondo e sprite da uno ``SceneState``
- [ ] Rispetta l'ordine dei livelli
'@ "11"

New-Issue 4 "Canvas: selezione, trascinamento, ridimensionamento" 8 @("feature","rischio-alto") @'
### Criteri di accettazione
- [ ] Emette eventi con coordinate gia' relative (0..1)
- [ ] Non conosce blocchi, progetto ne' Ren'Py
- [ ] Maniglie di ridimensionamento
'@ "11"

New-Issue 4 "Ordinamento dei livelli" 3 @("feature") @'
### Criteri di accettazione
- [ ] Porta avanti, indietro, in primo piano, in fondo
'@ "12"

New-Issue 4 "CanvasEditAdapter" 3 @("feature","architettura") @'
### Criteri di accettazione
- [ ] Traduce gli eventi del canvas in modifiche dentro ``history.Edit``
- [ ] Usa ``mergeKey`` per il trascinamento
'@

New-Issue 4 "Generare Transform e zorder nello script" 3 @("feature") @'
### Criteri di accettazione
- [ ] ``show c1 espr1 at Transform(xpos=..., ypos=..., zoom=...) zorder N``
- [ ] Ancoraggio centro-basso
- [ ] Test visivo: posizione nell'editor uguale a quella nel gioco
'@

# --- Sprint 5 ---------------------------------------------------
New-Issue 5 "Evento scelta" 5 @("feature") @'
### Criteri di accettazione
- [ ] Opzioni multiple, ognuna con scena di destinazione
- [ ] Destinazione scelta da elenco, mai scritta a mano
- [ ] Il testo delle opzioni passa dall'escaping
'@ "16"

New-Issue 5 "Evento transizione di scena" 2 @("feature") "" "20"

New-Issue 5 "Editor delle variabili" 5 @("feature") @'
### Criteri di accettazione
- [ ] Tipi numero, booleano, testo
- [ ] Crea, elimina, valore iniziale
- [ ] Nome interno sicuro (``v1``) distinto dal nome visibile
'@ "17"

New-Issue 5 "Evento variabile" 3 @("feature") @'
### Criteri di accettazione
- [ ] Operazioni imposta, somma, sottrai, inverti
- [ ] Genera l'assegnazione Ren'Py corrispondente
'@ "18"

New-Issue 5 "Evento condizione" 5 @("feature","rischio-alto") @'
### Criteri di accettazione
- [ ] Sei operatori: = != > < >= <=
- [ ] Rami vero e falso, ognuno verso una scena
- [ ] ``LiteralFor``: ``5`` / ``True`` / "Alex" secondo il tipo
- [ ] Test per ciascun tipo di variabile
'@ "19"

New-Issue 5 "Controllo dei salti rotti e scene irraggiungibili" 3 @("feature") "" "24"

New-Issue 5 "Test di accettazione del cliente" 3 @("test","blocco-cliente") @'
### Criteri di accettazione
- [ ] Il progetto MyFirstVN si crea interamente dall'editor
- [ ] Premendo Gioca la storia e' giocabile
- [ ] La scelta porta al ramo corretto
- [ ] Salvato come test di regressione
'@ "37"

# --- Sprint 6 ---------------------------------------------------
New-Issue 6 "Evento musica con ripetizione" 3 @("feature") "" "21"
New-Issue 6 "Evento effetto sonoro" 2 @("feature") "" "21"
New-Issue 6 "Musica di scena" 2 @("feature") @'
### Criteri di accettazione
- [ ] Rimuovere l'evento non cancella il file sorgente
'@ "21"

# --- Sprint 7 ---------------------------------------------------
New-Issue 7 "Annulla/Ripeti a istantanee" 8 @("feature","rischio-alto") @'
### Criteri di accettazione
- [ ] Ctrl+Z e Ctrl+Y funzionanti
- [ ] ``mergeKey`` unisce trascinamenti e digitazione
- [ ] Pila limitata a 100 voci
- [ ] Il menu mostra ``Annulla: <azione>``
- [ ] Dopo un annulla la selezione non va persa
'@ "26"

New-Issue 7 "Test: nessuna modifica fuori da Edit" 3 @("test","architettura") ""

New-Issue 7 "Salvataggio automatico con backup" 3 @("feature") @'
### Criteri di accettazione
- [ ] Un salvataggio fallito non sovrascrive l'unico stato valido
'@ "27"

New-Issue 7 "Eseguire lint prima di Gioca" 3 @("feature") ""

New-Issue 7 "Traduzione degli errori Ren'Py" 5 @("feature") @'
### Criteri di accettazione
- [ ] Legge ``errors.txt`` e ``traceback.txt``
- [ ] Messaggi comprensibili invece del traceback
- [ ] Azioni: Sostituisci file / Trova file / Annulla
- [ ] Il traceback resta in una finestra di log separata
'@ "28"

New-Issue 7 "Finestra di log tecnico con Serilog" 2 @("chore") ""
New-Issue 7 "Gestione dei blocchi sconosciuti" 3 @("feature") @'
### Criteri di accettazione
- [ ] Aprire un progetto piu' recente mostra un messaggio, non perde i dati
'@

# --- Sprint 8 ---------------------------------------------------
New-Issue 8 "Anteprima interna del blocco selezionato" 5 @("feature") @'
### Criteri di accettazione
- [ ] Usa ``SceneStateCalculator``
- [ ] Mostra sfondo, sprite e box del dialogo
'@ "15"

New-Issue 8 "Anteprima scena con --warp" 3 @("feature") @'
### Criteri di accettazione
- [ ] Distinta dal pulsante Gioca
'@ "23"

New-Issue 8 "Esportazione del gioco" 5 @("feature") @'
### Criteri di accettazione
- [ ] Usa ``distribute``
- [ ] Cartella pronta da condividere
'@

New-Issue 8 "Percorrere la lista di verifica del cliente" 5 @("test","blocco-cliente") "" "34"
New-Issue 8 "Pacchetto zip e release su GitHub" 3 @("chore") ""

New-Issue 8 "Dimostrare l'estensibilita' con un blocco nuovo" 3 @("docs","architettura") @'
Come studente
voglio aggiungere un blocco ``scuoti schermo`` senza toccare altro codice
cosi' da dimostrare il principio aperto/chiuso alla discussione

### Criteri di accettazione
- [ ] Un file nuovo piu' una riga nel registro
- [ ] Generatore, validatore, persistenza e UI invariati
- [ ] Il diff del commit e' la prova
'@

New-Issue 8 "Tabella di tracciabilita' requisiti - issue" 2 @("docs") ""

Write-Host "`n=== Fatto ===" -ForegroundColor Green

if ($script:Falliti.Count -gt 0) {
    Write-Host ""
    Write-Warning "$($script:Falliti.Count) issue non create:"
    $script:Falliti | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
    Write-Host "L'errore di ciascuna e' stampato qui sopra." -ForegroundColor Yellow
} else {
    Write-Host "Tutte le issue create senza errori."
}

Write-Host ""
Write-Host "Apri il repository su GitHub: Issues e Milestones."
Write-Host "Poi crea la bacheca: Projects -> New project -> Board."
