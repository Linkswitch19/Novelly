using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.AppService.Persistence;
using VnEditor.Domain;
using VnEditor.Infrastructure.Assets;

namespace VnEditor.Infrastructure.Projects;

/// <summary>
/// Coordina il salvataggio con nome (<c>Save As</c>) di un progetto su una destinazione su disco.
/// Ricostruisce la struttura Ren'Py, copia gli asset di gioco esistenti (<c>.renpy_game</c>)
/// e serializza il file di modello nella cartella di staging temporanea tramite <see cref="IProjectPersistence"/>.
/// Al completamento di tutti i passaggi, promuove atomicamente la cartella temporanea a cartella finale.
/// Se la destinazione coincide con la sorgente, delega direttamente a un salvataggio sul posto.
/// Non muta lo stato della sessione attiva (<c>ProjectSession</c>).
/// </summary>
public sealed class ProjectSaveAsService
{
    private readonly string _templatePath;
    private readonly IProjectPersistence _persistence;

    /// <summary>
    /// Inizializza il servizio verificando la validità del percorso template e del servizio di persistenza.
    /// </summary>
    /// <param name="templatePath">Percorso relativo o assoluto della directory contenente i file template iniziali.</param>
    /// <param name="persistence">Il servizio responsabile della persistenza atomica dei dati di progetto.</param>
    /// <exception cref="ArgumentException">Lanciata se <paramref name="templatePath"/> è nullo, vuoto o formato da soli spazi.</exception>
    /// <exception cref="ArgumentNullException">Lanciata se <paramref name="persistence"/> è nullo.</exception>
    public ProjectSaveAsService(string templatePath, IProjectPersistence persistence)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templatePath);
        ArgumentNullException.ThrowIfNull(persistence);

        this._templatePath = Path.GetFullPath(templatePath);
        this._persistence = persistence;
    }
    /// <summary>
    /// Esegue il salvataggio con nome del progetto nella destinazione specificata.
    /// </summary>
    /// <param name="project">Il modello di dominio da salvare.</param>
    /// <param name="sourceFolder">Il percorso della directory sorgente contenente il progetto attualmente aperto.</param>
    /// <param name="destinationFolder">Il percorso della directory di arrivo in cui creare la nuova copia del progetto.</param>
    /// <param name="cancellationToken">Token facoltativo per monitorare la richiesta di annullamento.</param>
    /// <returns>Un'attività (<see cref="Task"/>) che rappresenta l'operazione asincrona.</returns>
    /// <exception cref="ArgumentNullException">Lanciata se <paramref name="project"/> è nullo.</exception>
    /// <exception cref="ArgumentException">Lanciata se i percorsi non sono validi o non rispettano i vincoli di cartella.</exception>
    /// <exception cref="OperationCanceledException">Lanciata se l'operazione viene interrotta tramite <paramref name="cancellationToken"/>.</exception>
    /// <exception cref="IOException">Lanciata in caso di conflitti o problemi di scrittura sul file system.</exception>
    public async Task SaveAsAsync(Project project, string sourceFolder, string destinationFolder,
                                    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        cancellationToken.ThrowIfCancellationRequested();

        (string fullSource, string fullDestination, bool isSameFolder) =
            ProjectFolderValidator.ValidateSaveAsFolders(
                sourceFolder, destinationFolder);

        if (isSameFolder)
        {
            await this._persistence.SaveAsync(
                project, fullSource, cancellationToken).ConfigureAwait(false);

            return;
        }

        using ProjectFolderTransaction transaction = new(fullDestination);

        await Task.Run(
            () => PrepareCopy(fullSource, transaction.StagingFolder, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        await this._persistence.SaveAsync(
            project, transaction.StagingFolder, cancellationToken).ConfigureAwait(false);

        transaction.Commit(cancellationToken);
    }

    /// <summary>
    /// Prepara la struttura fisica della cartella di destinazione temporanea copiando la directory di gioco
    /// e assicurando la presenza dei file template richiesti dall'engine Ren'Py.
    /// </summary>
    /// <param name="sourceFolder">Cartella sorgente validata da cui prelevare gli asset correnti.</param>
    /// <param name="stagingFolder">Percorso dell'area di lavoro temporanea creata dalla transazione.</param>
    /// <param name="cancellationToken">Token per monitorare l'annullamento della procedura.</param>
    private void PrepareCopy(string sourceFolder, string stagingFolder,
                             CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ProjectInitializer initializer = new(this._templatePath);

        string sourceGameFolder = Path.Combine(sourceFolder, ".renpy_game");
        string destinationGameFolder = Path.Combine(stagingFolder, ".renpy_game");

        if (Directory.Exists(sourceGameFolder))
        {
            ProjectDirectoryCopier.Copy(
                sourceGameFolder, destinationGameFolder, cancellationToken);
        }

        initializer.Initialize(stagingFolder, cancellationToken);
    }
}
