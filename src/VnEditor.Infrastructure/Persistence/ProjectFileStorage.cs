using System.Text;

namespace VnEditor.Infrastructure.Persistence;

/// <summary>
/// Gestisce la lettura e la scrittura su disco del file di progetto principale (<c>project.json</c>).
/// Oltre al salvataggio atomico tramite file temporaneo, implementa ora un sistema di backup automatico (<c>.bak</c>) 
/// e meccanismi di ripristino o fallback nel caso in cui il file originale vada perduto o corrotto.
/// </summary>
internal static class ProjectFileStorage
{
    private const string ProjectFileName = "project.json";

    /// <summary>
    /// Legge in modo asincrono il contenuto del file di progetto. 
    /// Se il file principale è mancante, tenta automaticamente di caricare il file di backup.
    /// </summary>
    internal static Task<string> ReadAsync(
        string projectFolder,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectFolder);

        string projectFilePath = GetProjectFilePath(projectFolder);
        string fileToRead = GetFileToRead(projectFilePath);

        return File.ReadAllTextAsync(fileToRead, Encoding.UTF8, cancellationToken);
    }

    /// <summary>
    /// Scrive il contenuto su disco in modo atomico, preservando la versione precedente in un file di backup.
    /// In caso di errore durante le operazioni di I/O, tenta di ripristinare automaticamente lo stato originario.
    /// </summary>
    internal static async Task WriteAtomicallyAsync(
        string projectFolder,
        string content,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectFolder);
        ArgumentNullException.ThrowIfNull(content);
        cancellationToken.ThrowIfCancellationRequested();

        string fullFolder = Path.GetFullPath(projectFolder);
        Directory.CreateDirectory(fullFolder);

        string projectFilePath = Path.Combine(fullFolder, ProjectFileName);
        string backupFilePath = GetBackupFilePath(projectFilePath);
        string temporaryFilePath = Path.Combine(
            fullFolder,
            $".{ProjectFileName}.{Guid.NewGuid():N}.tmp");

        try
        {
            await WriteTemporaryFileAsync(temporaryFilePath, content, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            ReplaceOrMoveTemporaryFile(
                temporaryFilePath,
                projectFilePath,
                backupFilePath);
        }
        catch
        {
            RestoreBackupIfProjectMissing(projectFilePath, backupFilePath);
            throw;
        }
        finally
        {
            DeleteTemporaryFileIfPresent(temporaryFilePath);
        }
    }

    /// <summary>
    /// Costruisce il percorso assoluto e definitivo del file di progetto partendo dalla sua cartella.
    /// </summary>
    private static string GetProjectFilePath(string projectFolder)
    {
        string fullFolder = Path.GetFullPath(projectFolder);
        return Path.Combine(fullFolder, ProjectFileName);
    }

    /// <summary>
    /// Restituisce il percorso del file di backup aggiungendo l'estensione ".bak".
    /// </summary>
    private static string GetBackupFilePath(string projectFilePath) =>
        projectFilePath + ".bak";

    /// <summary>
    /// Determina quale file caricare in fase di lettura.
    /// Dà priorità al file di progetto principale; se questo manca, ripiega sul file ".bak" se esistente.
    /// </summary>
    private static string GetFileToRead(string projectFilePath)
    {
        if (File.Exists(projectFilePath))
            return projectFilePath;

        string backupFilePath = GetBackupFilePath(projectFilePath);

        return File.Exists(backupFilePath)
            ? backupFilePath
            : projectFilePath;
    }


    /// <summary>
    /// Scrive fisicamente i byte nel file temporaneo.
    /// Forza esplicitamente lo svuotamento dei buffer del sistema operativo sul disco fisico 
    /// per garantire che i dati siano realmente scritti prima di procedere con la sostituzione.
    /// </summary>
    private static async Task WriteTemporaryFileAsync(
        string temporaryFilePath,
        string content,
        CancellationToken cancellationToken)
    {
        await using FileStream stream = new(
            temporaryFilePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            options: FileOptions.Asynchronous);

        byte[] bytes = Encoding.UTF8.GetBytes(content);
        await stream.WriteAsync(bytes.AsMemory(), cancellationToken);
        stream.Flush(flushToDisk: true);
    }

    /// <summary>
    /// Sostituisce il file di progetto esistente con quello temporaneo.
    /// Utilizzando <see cref="File.Replace"/> con il terzo parametro valorizzato, 
    /// il sistema operativo rinomina automaticamente il vecchio file di progetto trasformandolo nel file di backup.
    /// </summary>
    private static void ReplaceOrMoveTemporaryFile(
        string temporaryFilePath,
        string projectFilePath,
        string backupFilePath)
    {
        if (File.Exists(projectFilePath))
        {
            File.Replace(temporaryFilePath, projectFilePath, backupFilePath);
            return;
        }

        File.Move(temporaryFilePath, projectFilePath);
    }

    /// <summary>
    /// Operazione di salvataggio di emergenza (rollback). Se il file di progetto è andato perduto 
    /// durante il processo di sostituzione, tenta di rinominare il file di backup appena creato 
    /// per ripristinarlo come file principale.
    /// </summary>
    private static void RestoreBackupIfProjectMissing(
        string projectFilePath,
        string backupFilePath)
    {
        if (File.Exists(projectFilePath) || !File.Exists(backupFilePath))
            return;

        try
        {
            File.Move(backupFilePath, projectFilePath);
        }
        catch (IOException)
        {
            // Il backup resta disponibile per un successivo caricamento.
        }
        catch (UnauthorizedAccessException)
        {
            // Il backup resta disponibile per un successivo caricamento.
        }
    }
    /// <summary>
    /// Tenta di eliminare il file temporaneo usato durante il salvataggio.
    /// Ignora intenzionalmente le eccezioni di I/O per non sovrascrivere o mascherare 
    /// l'errore reale nel caso in cui il salvataggio sia fallito in precedenza.
    /// </summary>
    private static void DeleteTemporaryFileIfPresent(string temporaryFilePath)
    {
        try
        {
            File.Delete(temporaryFilePath);
        }
        catch (IOException)
        {
            // La pulizia non deve nascondere l'errore del salvataggio.
        }
        catch (UnauthorizedAccessException)
        {
            // La pulizia non deve nascondere l'errore del salvataggio.
        }
    }
}