using VnEditor.AppService.Projects;
using VnEditor.Domain;
using VnEditor.Infrastructure.Projects;

/// <summary>
/// Implementa il contratto <see cref="IProjectWorkspace"/> esponendo le operazioni di gestione del workspace su disco.
/// Agisce come facciata delegando la creazione del nuovo progetto a <see cref="ProjectCreationService"/>
/// e la duplicazione/salvataggio con nome a <see cref="ProjectSaveAsService"/>.
/// Non contiene logica di I/O diretta né altera lo stato della sessione attiva (<c>ProjectSession</c>).
/// </summary>
public sealed class FileProjectWorkspace : IProjectWorkspace
{
    private readonly ProjectCreationService _creationService;
    private readonly ProjectSaveAsService _saveAsService;

    /// <summary>
    /// Inizializza una nuova istanza di <see cref="FileProjectWorkspace"/> ricevendo i servizi di creazione e salvataggio.
    /// </summary>
    /// <param name="creationService">Servizio delegato per l'inizializzazione da template di nuovi progetti su disco.</param>
    /// <param name="saveAsService">Servizio delegato per la copia transazionale e persistenza in salvataggi con nome.</param>
    /// <exception cref="ArgumentNullException">Lanciata se <paramref name="creationService"/> o <paramref name="saveAsService"/> sono nulli.</exception>
    public FileProjectWorkspace(ProjectCreationService creationService, ProjectSaveAsService saveAsService)
    {
        ArgumentNullException.ThrowIfNull(creationService);
        ArgumentNullException.ThrowIfNull(saveAsService);

        this._creationService = creationService;
        this._saveAsService = saveAsService;

    }

    /// <summary>
    /// Avvia la creazione transazionale di una nuova cartella di progetto e restituisce il modello in memoria configurato.
    /// </summary>
    /// <param name="projectName">Il nome da assegnare al progetto.</param>
    /// <param name="projectFolder">La directory target in cui creare l'alberatura del progetto.</param>
    /// <param name="cancellationToken">Token facoltativo per monitorare la richiesta di annullamento.</param>
    /// <returns>Il modello di dominio <see cref="Project"/> creato.</returns>
    public Task<Project> CreateAsync(string projectName, string projectFolder,
      CancellationToken cancellationToken = default)
    {
        return this._creationService.CreateAsync(projectName, projectFolder, cancellationToken);
    }

    /// <summary>
    /// Coordina il salvataggio con nome di un progetto esistente verso una cartella di destinazione.
    /// </summary>
    /// <param name="project">Il modello di dominio del progetto da salvare.</param>
    /// <param name="sourceFolder">Il percorso della directory contenente il progetto corrente.</param>
    /// <param name="destinationFolder">La nuova cartella di destinazione in cui posizionare la copia salvata.</param>
    /// <param name="cancellationToken">Token facoltativo per monitorare la richiesta di annullamento.</param>
    /// <returns>Un'attività (<see cref="Task"/>) che rappresenta il salvataggio asincrono.</returns>
    public Task SaveAsAsync(Project project, string sourceFolder, string destinationFolder,
        CancellationToken cancellationToken = default)
    {
        return this._saveAsService.SaveAsAsync(project, sourceFolder, destinationFolder, cancellationToken);

    }
}