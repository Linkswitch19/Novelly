using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.AppService.History;
using VnEditor.AppService.Projects;
using VnEditor.AppService.Sessions;
using VnEditor.Domain;
using VnEditor.AppService.Interaction;
using System.IO;

namespace VnEditor.AppService.Projects.Actions;

/// <summary>
/// Raccoglie nome e posizione del nuovo progetto e determina
/// una sottocartella con il nome indicato.
/// Verifica le modifiche non salvate, delega la creazione al workspace
/// e attiva il risultato soltanto se la sessione non è cambiata.
/// </summary>
public sealed class NewProjectAction : IProjectFileAction
{
    private readonly ProjectSession _session;
    private readonly IProjectDialogs _dialogs;
    private readonly IProjectWorkspace _workspace;
    private readonly IProjectHistoryFactory _historyFactory;
    private readonly UnsavedChangesGuard _unsavedChanges;

    /// <summary>
    /// Inizializza una nuova istanza della classe <see cref="NewProjectAction"/>.
    /// </summary>
    /// <param name="session">La sessione di progetto in cui attivare il nuovo progetto creato.</param>
    /// <param name="dialogs">Il servizio di dialogo utilizzato per richiedere i dati del nuovo progetto e mostrare errori.</param>
    /// <param name="workspace">Il servizio incaricato di creare e inizializzare il progetto sul file system.</param>
    /// <param name="historyFactory">La factory utilizzata per istanziare la cronologia (<see cref="IProjectHistory"/>) del nuovo progetto.</param>
    /// <param name="unsavedChanges">La guardia incaricata di gestire eventuali modifiche non salvate prima di sostituire il progetto attivo.</param>
    /// <exception cref="ArgumentNullException">
    /// Sollevata se <paramref name="session"/>, <paramref name="dialogs"/>, <paramref name="workspace"/>, 
    /// <paramref name="historyFactory"/> o <paramref name="unsavedChanges"/> sono <see langword="null"/>.
    /// </exception>
    public NewProjectAction(ProjectSession session, IProjectDialogs dialogs, IProjectWorkspace workspace, IProjectHistoryFactory historyFactory, UnsavedChangesGuard unsavedChanges)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(historyFactory);
        ArgumentNullException.ThrowIfNull(unsavedChanges);

        this._session = session;
        this._dialogs = dialogs;
        this._workspace = workspace;
        this._historyFactory = historyFactory;
        this._unsavedChanges = unsavedChanges;
    }

    /// <summary>
    /// Richiede nome e posizione del nuovo progetto, determina la sottocartella
    /// di destinazione e ne delega la creazione al workspace.
    /// </summary>
    /// <remarks>
    /// La posizione scelta dall'utente è la cartella genitore:
    /// il nome del progetto identifica la sottocartella da creare.
    /// Prima della creazione verifica il nome e gestisce le eventuali
    /// modifiche non salvate del progetto corrente.
    /// Il nuovo progetto viene attivato come non salvato soltanto se
    /// la revisione della sessione coincide ancora con quella approvata.
    /// Se la sessione cambia durante la creazione, la cartella può essere
    /// stata creata sul disco, ma il progetto corrente non viene sostituito.
    /// Gli errori operativi vengono propagati al decoratore incaricato
    /// di presentarli all'utente.
    /// </remarks>
    /// <returns>
    /// Un'attività che restituisce <see langword="true"/> quando il progetto
    /// è stato creato e attivato; <see langword="false"/> se l'utente annulla,
    /// il nome non è accettabile, non è possibile lasciare il progetto corrente
    /// oppure la sessione cambia durante la creazione.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// La cartella genitore è nulla, vuota o composta soltanto da spazi,
    /// oppure il percorso di destinazione non è valido.
    /// </exception>
    /// <exception cref="IOException">
    /// La destinazione non è disponibile oppure si verifica un errore
    /// durante la creazione o la copia dei contenuti.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// L'accesso richiesto al filesystem non è consentito.
    /// </exception>
    public async Task<bool> ExecuteAsync()
    {
        NewProjectRequest? request = await _dialogs.ShowNewProjectAsync();

        if (request is null)
            return false;

        string projectName = request.ProjectName.Trim();

        if (!IsSingleFolderName(projectName))
        {
            await _dialogs.ShowErrorAsync(
                "Invalid project name",
                "Choose a project name that can be used as a folder name. Do not enter a path.");

            return false;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(request.ParentFolder);

        string projectFolder = Path.GetFullPath(
            Path.Combine(request.ParentFolder, projectName));

        if (!await _unsavedChanges.CanLeaveAsync())
            return false;

        long? expectedRevision = _session.CaptureCurrentRevision();

        Project project = await _workspace.CreateAsync(projectName, projectFolder);

        IProjectHistory history = _historyFactory.Create(project);

        bool activated = _session.TrySetProject(
            history, projectFolder, false, expectedRevision);

        if (!activated)
        {
            await _dialogs.ShowErrorAsync(
                "Project changed",
                "The current project changed while the new project was being prepared. Please try again.");

            return false;
        }

        return true;
    }

    /// <summary>
    /// Verifica che il nome possa rappresentare una singola cartella
    /// e non un percorso verso un'altra posizione.
    /// </summary>
    /// <param name="name">
    /// Nome del progetto, già privato degli spazi iniziali e finali.
    /// </param>
    /// <returns>
    /// <see langword="true"/> se il nome contiene testo, non coincide con
    /// "." o "..", non termina con un punto e non contiene separatori
    /// o caratteri vietati nei nomi di file dalla piattaforma corrente;
    /// altrimenti <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Non accede al filesystem e non verifica la disponibilità della destinazione.
    /// Gli ulteriori vincoli imposti dal filesystem vengono gestiti
    /// dal servizio Infrastructure durante la creazione.
    /// </remarks>
    private static bool IsSingleFolderName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        if (name is "." or "..")
            return false;

        if (name.EndsWith('.'))
            return false;

        return name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
            && name.IndexOfAny(['/', '\\']) < 0;
    }
}
