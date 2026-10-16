using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.Domain;

namespace VnEditor.AppService.Projects;

/// <summary>
/// Definisce i contratti per la creazione della struttura su disco di un nuovo progetto
/// e per la duplicazione/salvataggio in una nuova posizione (<c>Save As</c>).
/// Non altera la sessione attiva (<c>ProjectSession</c>). 
/// La creazione predispone cartelle e modello in memoria senza scrivere il file json;
/// <see cref="SaveAsAsync"/> sincronizza asset e persistenza atomica nella nuova destinazione.
/// </summary>
public interface IProjectWorkspace
{
    /// <summary>
    /// Inizializza un nuovo progetto e crea l'alberatura di cartelle necessaria sul file system 
    /// (es. cartelle per sfondi, personaggi, audio).
    /// </summary>
    /// <param name="projectName">Il nome del nuovo progetto.</param>
    /// <param name="projectFolder">Il percorso della directory principale in cui ospitare il progetto.</param>
    /// <param name="cancellationToken">Token per supportare la cancellazione cooperativa dell'operazione asincrona.</param>
    /// <returns>L'istanza di <see cref="Project"/> appena creata e pronta per essere modificata.</returns>
    /// <exception cref="ArgumentException">Lanciata se <paramref name="projectName"/> o <paramref name="projectFolder"/> sono vuoti o composti da soli spazi.</exception>
    /// <exception cref="IOException">Lanciata in caso di errori durante la creazione delle directory su disco.</exception>
    /// <exception cref="OperationCanceledException">Lanciata se l'operazione viene interrotta tramite <paramref name="cancellationToken"/>.</exception>
    Task<Project> CreateAsync(
    string projectName,
    string projectFolder,
    CancellationToken cancellationToken = default);

    /// <summary>
    /// Salva una copia del progetto e dei relativi file di asset in una nuova cartella di destinazione,
    /// eseguendo una persistenza atomica del file del progetto.
    /// </summary>
    /// <param name="project">Il modello del progetto da salvare nella nuova posizione.</param>
    /// <param name="sourceFolder">Il percorso della cartella d'origine contenente gli asset correnti.</param>
    /// <param name="destinationFolder">Il percorso della directory di destinazione in cui creare la copia.</param>
    /// <param name="cancellationToken">Token per supportare la cancellazione cooperativa dell'operazione asincrona.</param>
    /// <returns>Un'attività (<see cref="Task"/>) che rappresenta l'operazione di salvataggio asincrona.</returns>
    /// <exception cref="ArgumentNullException">Lanciata se <paramref name="project"/> è nullo.</exception>
    /// <exception cref="ArgumentException">Lanciata se <paramref name="sourceFolder"/> o <paramref name="destinationFolder"/> sono vuoti o non validi.</exception>
    /// <exception cref="IOException">Lanciata in caso di errori durante la copia dei file o la scrittura della copia.</exception>
    /// <exception cref="OperationCanceledException">Lanciata se l'operazione viene interrotta tramite <paramref name="cancellationToken"/>.</exception>
    Task SaveAsAsync(
        Project project,
        string sourceFolder,
        string destinationFolder,
        CancellationToken cancellationToken = default);
}
