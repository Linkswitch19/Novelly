using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.AppService.History;
using VnEditor.AppService.Persistence;
using VnEditor.AppService.Sessions;
using VnEditor.Domain;
using VnEditor.AppService.Interaction;
using VnEditor.AppService.Projects;

namespace VnEditor.AppService.Projects.Actions;

public sealed class OpenProjectAction : IProjectFileAction
{
    private readonly ProjectSession _session;
    private readonly IProjectDialogs _dialogs;
    private readonly IProjectPersistence _persistence;
    private readonly IProjectHistoryFactory _historyFactory;
    private readonly UnsavedChangesGuard _unsavedChanges;

    /// <summary>
    /// Inizializza una nuova istanza della classe <see cref="OpenProjectAction"/>.
    /// </summary>
    /// <param name="session">La sessione di progetto in cui attivare il progetto caricato.</param>
    /// <param name="dialogs">Il servizio di dialogo utilizzato per selezionare la cartella del progetto e mostrare eventuali errori.</param>
    /// <param name="persistence">Il servizio di persistenza incaricato di leggere e deserializzare il progetto dal file system.</param>
    /// <param name="historyFactory">La factory utilizzata per istanziare la cronologia (<see cref="IProjectHistory"/>) del progetto caricato.</param>
    /// <param name="unsavedChanges">La guardia incaricata di gestire eventuali modifiche non salvate prima di sostituire il progetto attivo.</param>
    /// <exception cref="ArgumentNullException">
    /// Sollevata se <paramref name="session"/>, <paramref name="dialogs"/>, <paramref name="persistence"/>, 
    /// <paramref name="historyFactory"/> o <paramref name="unsavedChanges"/> sono <see langword="null"/>.
    /// </exception>
    public OpenProjectAction(ProjectSession session, IProjectDialogs dialogs, IProjectPersistence persistence, IProjectHistoryFactory historyFactory, UnsavedChangesGuard unsavedChanges)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(persistence);
        ArgumentNullException.ThrowIfNull(historyFactory);
        ArgumentNullException.ThrowIfNull(unsavedChanges);

        this._session = session;
        this._dialogs = dialogs;
        this._persistence = persistence;
        this._historyFactory = historyFactory;
        this._unsavedChanges = unsavedChanges;
    }

    /// <summary>
    /// Esegue in modo asincrono il flusso completo di selezione, caricamento e attivazione di un progetto esistente.
    /// </summary>
    /// <returns>
    /// Un'attività asincrona il cui risultato è <see langword="true"/> se il progetto è stato caricato e impostato 
    /// come progetto attivo nella sessione; <see langword="false"/> se l'utente ha annullato la selezione della cartella, 
    /// se non è stato possibile lasciare il progetto corrente o se si è verificato un conflitto di revisione durante il caricamento.
    /// </returns>
    public async Task<bool> ExecuteAsync()
    {
        string? projectFolder = await this._dialogs.ChooseProjectFolderAsync(
            "Open project", this._session.ProjectFolder);

        if (projectFolder is null)
            return false;

        if (!await this._unsavedChanges.CanLeaveAsync())
            return false;

        long? expectedRevision = this._session.CaptureCurrentRevision();

        Project project = await this._persistence.LoadAsync(projectFolder);

        IProjectHistory history = this._historyFactory.Create(project);

        bool activated = this._session.TrySetProject(
            history, projectFolder, true, expectedRevision);

        if (!activated)
        {
            await this._dialogs.ShowErrorAsync(
                "Project changed",
                "The current project changed while the selected project was loading. Please try again.");

            return false;
        }

        return true;
    }
}
