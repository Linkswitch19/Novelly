using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.AppService.History;
using VnEditor.AppService.Projects;
using VnEditor.AppService.Sessions;
using VnEditor.Domain;
using VnEditor.AppService.Interaction;

namespace VnEditor.AppService.Projects.Actions;

/// <summary>
/// Esegue il caso d'uso New raccogliendo i dati del progetto,
/// verificando le modifiche non salvate e delegando la creazione
/// al servizio workspace.
/// Attiva il nuovo progetto soltanto se la sessione corrisponde
/// ancora alla revisione approvata.
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
    /// Esegue in modo asincrono il flusso completo di creazione e attivazione di un nuovo progetto.
    /// </summary>
    /// <returns>
    /// Un'attività asincrona il cui risultato è <see langword="true"/> se il nuovo progetto è stato creato e impostato 
    /// come progetto attivo nella sessione; <see langword="false"/> se l'utente ha annullato la finestra di creazione, 
    /// se non è stato possibile lasciare il progetto corrente o se si è verificato un conflitto di revisione durante la preparazione.
    /// </returns>
    public async Task<bool> ExecuteAsync()
    {
        NewProjectRequest? request = await this._dialogs.ShowNewProjectAsync();

        if (request is null)
            return false;

        if (!await this._unsavedChanges.CanLeaveAsync())
            return false;

        long? expectedRevision = this._session.CaptureCurrentRevision();

        Project project = await this._workspace.CreateAsync(
            request.ProjectName, request.ProjectFolder);

        IProjectHistory history = this._historyFactory.Create(project);

        bool activated = this._session.TrySetProject(
            history, request.ProjectFolder, false, expectedRevision);

        if (!activated)
        {
            await this._dialogs.ShowErrorAsync(
                "Project changed",
                "The current project changed while the new project was being prepared. Please try again.");

            return false;
        }

        return true;
    }
}
