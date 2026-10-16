using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.Domain;
using VnEditor.Infrastructure.Assets;

namespace VnEditor.Infrastructure.Projects;

/// <summary>
/// Servizio dell'infrastruttura responsabile della creazione fisica di un nuovo progetto su disco.
/// Copia la struttura iniziale partendo da una cartella template tramite <see cref="ProjectInitializer"/>,
/// sfruttando una transazione di cartella (<see cref="ProjectFolderTransaction"/>) per assicurare che 
/// la directory finale appaia solo al termine e senza corruzioni.
/// Restituisce l'entità di dominio <see cref="Project"/> configurata in memoria, senza scriverla su file
/// e senza interferire con lo stato della sessione attiva.
/// </summary>
public sealed class ProjectCreationService
{
    private readonly string _templatePath;

    /// <summary>
    /// Inizializza il servizio validando il percorso del template predefinito.
    /// </summary>
    /// <param name="templatePath">Percorso relativo o assoluto della directory contenente i file template iniziali.</param>
    /// <exception cref="ArgumentException">Lanciata se <paramref name="templatePath"/> è vuoto o composto da soli spazi.</exception>
    public ProjectCreationService(string templatePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templatePath);

        this._templatePath = Path.GetFullPath(templatePath);
    }
    /// <summary>
    /// Avvia la creazione asincrona del nuovo progetto eseguendo le operazioni di I/O su un thread di background.
    /// </summary>
    /// <param name="projectName">Il nome scelto per il progetto.</param>
    /// <param name="projectFolder">Il percorso della directory target finale dove risiederà il progetto.</param>
    /// <param name="cancellationToken">Token facoltativo per monitorare la richiesta di interruzione anticipata.</param>
    /// <returns>Il modello di dominio <see cref="Project"/> inizializzato in memoria con il titolo impostato.</returns>
    /// <exception cref="ArgumentException">Lanciata se <paramref name="projectName"/> o <paramref name="projectFolder"/> non sono validi.</exception>
    /// <exception cref="OperationCanceledException">Lanciata se l'operazione viene annullata prima o durante l'esecuzione.</exception>
    /// <exception cref="IOException">Lanciata in presenza di conflitti o errori sul file system.</exception>
    public Task<Project> CreateAsync(string projectName, string projectFolder,
                                        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectName);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectFolder);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(
            () => CreateProject(projectName, projectFolder, cancellationToken),
            cancellationToken);
    }
    /// <summary>
    /// Esegue la sequenza transazionale sul file system: staging, copia asset/template, 
    /// creazione entità in memoria e commit atomico nella cartella finale.
    /// </summary>
    private Project CreateProject(string projectName, string projectFolder,
                                    CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ProjectInitializer initializer = new(_templatePath);
        using ProjectFolderTransaction transaction = new(projectFolder);

        initializer.Initialize(transaction.StagingFolder, cancellationToken);

        Project project = new() { Title = projectName.Trim() };

        transaction.Commit(cancellationToken);

        return project;
    }
}
