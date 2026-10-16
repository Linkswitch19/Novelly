using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.AppService.Projects;
using VnEditor.AppService.Sessions;
using VnEditor.Domain;
using VnEditor.AppService.Interaction;

namespace VnEditor.AppService.Projects.Actions;

/// <summary>
/// Esegue Save As scegliendo la destinazione e delegando la copia
/// e il salvataggio al servizio workspace.
/// Aggiorna cartella e revisione salvata dopo il successo,
/// conservando eventuali modifiche successive.
/// </summary>
public sealed class SaveProjectAsAction : IProjectFileAction
{
    private readonly ProjectSession _session;
    private readonly IProjectDialogs _dialogs;
    private readonly IProjectWorkspace _workspace;

    /// <summary>
    /// Inizializza una nuova istanza della classe <see cref="SaveProjectAsAction"/>.
    /// </summary>
    /// <param name="session">La sessione di progetto corrente da cui leggere lo stato e su cui aggiornare il punto di salvataggio e la cartella.</param>
    /// <param name="dialogs">Il servizio di dialogo utilizzato per selezionare la cartella di destinazione e mostrare eventuali errori.</param>
    /// <param name="workspace">Il servizio incaricato di copiare e salvare il progetto nella nuova cartella di destinazione.</param>
    /// <exception cref="ArgumentNullException">
    /// Sollevata se <paramref name="session"/>, <paramref name="dialogs"/> o <paramref name="workspace"/> sono <see langword="null"/>.
    /// </exception>
    public SaveProjectAsAction(ProjectSession session, IProjectDialogs dialogs, IProjectWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(workspace);

        this._session = session;
        this._dialogs = dialogs;
        this._workspace = workspace;
    }

    /// <summary>
    /// Esegue in modo asincrono il salvataggio del progetto corrente in una nuova cartella selezionata dall'utente.
    /// </summary>
    /// <returns>
    /// Un'attività asincrona il cui risultato è <see langword="true"/> se il progetto è stato salvato nella nuova destinazione 
    /// e lo stato della sessione è stato aggiornato con successo; <see langword="false"/> se nessun progetto è attivo, 
    /// se l'utente ha annullato la selezione della cartella o se si è verificato un conflitto di revisione.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Sollevata se un progetto è attivo nella sessione ma <see cref="ProjectSession.ProjectFolder"/> è <see langword="null"/>.
    /// </exception>
    public async Task<bool> ExecuteAsync()
    {
        if (this._session.Project is not Project project)
            return false;

        string sourceFolder = this._session.ProjectFolder
            ?? throw new InvalidOperationException("The project folder is missing.");

        long revision = this._session.CaptureSavePoint();

        string? destinationFolder = await _dialogs.ChooseProjectFolderAsync(
            "Save project as", sourceFolder);

        if (destinationFolder is null)
            return false;

        if (!this._session.IsCurrentRevision(revision))
        {
            await this._dialogs.ShowErrorAsync(
                "Project changed",
                "The project changed while the dialog was open. Please try again.");

            return false;
        }

        await this._workspace.SaveAsAsync(project, sourceFolder, destinationFolder);

        return this._session.TryMarkSaved(revision, destinationFolder);
    }

}
