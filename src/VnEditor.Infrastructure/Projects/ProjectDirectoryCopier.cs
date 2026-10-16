using System;
using System.Collections.Generic;
using System.Text;

namespace VnEditor.Infrastructure.Projects;

/// <summary>
/// Fornisce utilità per la copia ricorsiva di cartelle di progetto o template verso una nuova destinazione.
/// Garantisce la sicurezza operativa impedendo la sovrascrittura di percorsi esistenti, 
/// rifiutando collegamenti simbolici o junction points per evitare loop infiniti o riferimenti esterni non sicuri,
/// e verificando regolarmente il token di cancellazione durante il trasferimento.
/// </summary>
internal static class ProjectDirectoryCopier
{
    /// <summary>
    /// Copia ricorsivamente l'intero contenuto della cartella di origine nella cartella di destinazione specificata.
    /// Valida i percorsi verificando che l'origine esista, che la destinazione non sia già presente sul file system 
    /// e che la destinazione non sia posizionata all'interno dell'albero dell'origine.
    /// </summary>
    /// <param name="sourceFolder">Percorso della cartella contenente i file e le cartelle da copiare.</param>
    /// <param name="destinationFolder">Percorso in cui creare la nuova cartella di destinazione con i contenuti copiati.</param>
    /// <param name="cancellationToken">Token facoltativo per richiedere l'interruzione anticipata dell'operazione.</param>
    /// <exception cref="ArgumentException">Lanciata se <paramref name="sourceFolder"/> o <paramref name="destinationFolder"/> sono nulli, vuoti o contengono solo spazi.</exception>
    /// <exception cref="DirectoryNotFoundException">Lanciata se la cartella sorgente non esiste.</exception>
    /// <exception cref="IOException">
    /// Lanciata se la destinazione è contenuta dentro la cartella sorgente, se la destinazione esiste già sul disco, 
    /// o se viene incontrato un file o una cartella di tipo reparse point (link simbolico / junction).
    /// </exception>
    /// <exception cref="OperationCanceledException">Lanciata se l'operazione viene annullata tramite <paramref name="cancellationToken"/>.</exception>
    internal static void Copy(string sourceFolder, string destinationFolder, 
                                CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFolder);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationFolder);
        cancellationToken.ThrowIfCancellationRequested();

        string fullSource = Path.GetFullPath(sourceFolder);
        string fullDestination = Path.GetFullPath(destinationFolder);

        if (!Directory.Exists(fullSource))
            throw new DirectoryNotFoundException($"Folder not found: {fullSource}");

        if (IsDestinationWithinSource(fullSource, fullDestination))
        {
            throw new IOException(
                "The destination must be outside the source folder.");
        }

        if (Directory.Exists(fullDestination) || File.Exists(fullDestination))
            throw new IOException("The destination already exists.");

        CopyDirectory(fullSource, fullDestination, cancellationToken);

    }

    /// <summary>
    /// Metodo ricorsivo che esegue l'attraversamento delle directory e la duplicazione file per file.
    /// </summary>
    /// <param name="source">Percorso assoluto della directory corrente da copiare.</param>
    /// <param name="destination">Percorso assoluto corrispondente in cui ricreare la directory e i suoi file.</param>
    /// <param name="cancellationToken">Token per monitorare l'annullamento della copia.</param>
    private static void CopyDirectory(string source,string destination, 
                                        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureNotLinked(new DirectoryInfo(source));

        Directory.CreateDirectory(destination);

        foreach (string file in Directory.GetFiles(source))
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureNotLinked(new FileInfo(file));

            string destinationFile = Path.Combine(destination, Path.GetFileName(file));
            File.Copy(file, destinationFile, overwrite: false);
        }

        foreach (string subDirectory in Directory.GetDirectories(source))
        {
            cancellationToken.ThrowIfCancellationRequested();

            string destinationDirectory = Path.Combine(
                destination, Path.GetFileName(subDirectory));

            CopyDirectory(subDirectory, destinationDirectory, cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();

    }

    /// <summary>
    /// Determina se il percorso di destinazione è uguale o nidificato all'interno della cartella sorgente.
    /// Applica un confronto case-insensitive su Windows e case-sensitive su sistemi Unix-like.
    /// </summary>
    /// <param name="source">Percorso assoluto normalizzato della sorgente.</param>
    /// <param name="destination">Percorso assoluto normalizzato della destinazione.</param>
    /// <returns><c>true</c> se la destinazione coincide o risiede dentro l'albero della sorgente; altrimenti <c>false</c>.</returns>
    private static bool IsDestinationWithinSource(string source, string destination)
    {
        StringComparison comparison = OperatingSystem.IsWindows()
           ? StringComparison.OrdinalIgnoreCase
           : StringComparison.Ordinal;

        string normalizedSource = Path.TrimEndingDirectorySeparator(source);
        string normalizedDestination = Path.TrimEndingDirectorySeparator(destination);

        string sourcePrefix = Path.EndsInDirectorySeparator(normalizedSource)
            ? normalizedSource
            : normalizedSource + Path.DirectorySeparatorChar;

        return string.Equals(normalizedSource, normalizedDestination, comparison)
            || normalizedDestination.StartsWith(sourcePrefix, comparison);
    }

    /// <summary>
    /// Valida che l'elemento del file system (file o cartella) non sia un Reparse Point (collegamento simbolico, junction point, hard link).
    /// </summary>
    /// <param name="entry">Le informazioni sul file o sulla directory da esaminare.</param>
    /// <exception cref="IOException">Lanciata se l'elemento possiede il flag <see cref="FileAttributes.ReparsePoint"/>.</exception>
    private static void EnsureNotLinked(FileSystemInfo entry)
    {
        if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new IOException(
                $"Linked files or folders cannot be copied: {entry.FullName}");
        }
    }
}
