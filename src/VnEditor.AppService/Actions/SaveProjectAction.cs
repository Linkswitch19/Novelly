using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.AppService.Persistence;
using VnEditor.AppService.Sessions;
using VnEditor.Domain;

namespace VnEditor.AppService.Projects.Actions;

/// <summary>
/// Salva il progetto corrente tramite il servizio di persistenza atomica
/// e registra la revisione salvata nella sessione.
/// Consente di proseguire soltanto se il risultato appartiene ancora
/// al progetto attivo e non rimangono modifiche non salvate.
/// </summary>
public sealed class SaveProjectAction : IProjectFileAction
{
    private readonly ProjectSession _session;
    private readonly IProjectPersistence _persistence;

    /// <summary>
    /// Inizializza una nuova istanza della classe <see cref="SaveProjectAction"/>.
    /// </summary>
    /// <param name="session">La sessione di progetto corrente contenente il progetto da salvare e il suo stato di revisione.</param>
    /// <param name="persistence">Il servizio di persistenza utilizzato per salvare il progetto sul file system.</param>
    /// <exception cref="ArgumentNullException">Sollevata se <paramref name="session"/> o <paramref name="persistence"/> sono <see langword="null"/>.</exception>
    public SaveProjectAction(ProjectSession session, IProjectPersistence persistence)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(persistence);

        this._session = session;
        this._persistence = persistence;
    }

    /// <inheritdoc/>
    /// <summary>
    /// Esegue in modo asincrono il salvataggio del progetto corrente nella cartella associata alla sessione
    /// e aggiorna lo stato di revisione della sessione.
    /// </summary>
    /// <returns>
    /// Un task che rappresenta l'operazione di salvataggio asincrona. Il risultato è <see langword="true"/> se il progetto è stato salvato,
    /// la revisione appartiene ancora alla sessione attiva e non sono subentrate ulteriori modifiche;
    /// <see langword="false"/> se nessun progetto è aperto o se lo stato della sessione è cambiato durante il salvataggio.
    /// </returns>
    /// <exception cref="InvalidOperationException">Sollevata se c'è un progetto attivo ma <see cref="ProjectSession.ProjectFolder"/> è <see langword="null"/>.</exception>
    public async Task<bool> ExecuteAsync()
    {
        if (this._session.Project is not Project project)
            return false;

        string projectFolder = this._session.ProjectFolder
            ?? throw new InvalidOperationException("The project folder is missing.");

        long revision = this._session.CaptureSavePoint();

        await this._persistence.SaveAsync(project, projectFolder);

        return this._session.TryMarkSaved(revision, projectFolder) && !this._session.IsModified;
    }

}
