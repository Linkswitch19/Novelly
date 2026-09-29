using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.AppService.Persistence;
using VnEditor.AppService.Serialization;
using VnEditor.Domain;

namespace VnEditor.Infrastructure.Persistence;

/// <summary>
/// Implementazione concreta basata su file system dell'interfaccia <see cref="IProjectPersistence"/>.
/// Agisce da orchestratore principale per le operazioni di I/O del progetto, coordinando 
/// la serializzazione, la validazione dei dati e il salvataggio/caricamento sicuro su disco.
/// </summary>
public sealed class FileProjectPersistence : IProjectPersistence
{
    private const string ProjectFileName = "project.json";

    private readonly IProjectSerializer _serializer;

    /// <summary>
    /// Inizializza una nuova istanza di <see cref="FileProjectPersistence"/>.
    /// </summary>
    /// <param name="serializer">Il componente responsabile della conversione del progetto in formato testuale (e viceversa).</param>
    /// <exception cref="ArgumentNullException">Lanciata se il serializzatore fornito è nullo.</exception>
    public FileProjectPersistence (IProjectSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        this._serializer = serializer;
    }
    /// <summary>
    /// Salva il progetto nella cartella specificata in modo asincrono.
    /// Il processo garantisce che il progetto venga prima validato formalmente e poi serializzato.
    /// Il salvataggio fisico su disco è delegato a un meccanismo atomico per prevenire la corruzione dei dati.
    /// </summary>
    /// <param name="project">L'istanza del progetto da salvare.</param>
    /// <param name="projectFolder">Il percorso della cartella di destinazione.</param>
    /// <param name="cancellationToken">Token per annullare l'operazione in corso.</param>
    /// <exception cref="ArgumentNullException">Lanciata se il progetto è nullo.</exception>
    /// <exception cref="ArgumentException">Lanciata se la cartella di destinazione non è valida.</exception>
    public async Task SaveAsync( Project project, string projectFolder, 
                                CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectFolder);
        cancellationToken.ThrowIfCancellationRequested();

        ProjectPersistenceValidator.Validate(project);
        string content = this._serializer.Serialize(project);

        await ProjectFileStorage.WriteAtomicallyAsync(
            projectFolder,
            content,
            cancellationToken);
    }
    /// <summary>
    /// Carica un progetto dalla cartella specificata in modo asincrono.
    /// Si occupa di leggere i file (incluso l'eventuale ripristino di backup), deserializzare il contenuto 
    /// e validarne l'integrità prima di restituire l'oggetto al chiamante.
    /// </summary>
    /// <param name="projectFolder">Il percorso della cartella da cui caricare il progetto.</param>
    /// <param name="cancellationToken">Token per annullare l'operazione in corso.</param>
    /// <returns>L'istanza del progetto caricata e validata.</returns>
    /// <exception cref="ArgumentException">Lanciata se il percorso della cartella non è valido.</exception>
    public async Task<Project> LoadAsync(
        string projectFolder,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectFolder);
        cancellationToken.ThrowIfCancellationRequested();

        string content = await ProjectFileStorage.ReadAsync(
            projectFolder,
            cancellationToken);

        Project project = this._serializer.Deserialize(content);
        ProjectPersistenceValidator.Validate(project);

        cancellationToken.ThrowIfCancellationRequested();
        return project;
    }

}
