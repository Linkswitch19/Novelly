using System;
using System.Collections.Generic;
using System.Text;

namespace VnEditor.Infrastructure.Projects;

/// <summary>
/// Valida e normalizza le cartelle usate per creare o salvare un progetto.
/// Verifica la disponibilità delle nuove destinazioni e impedisce
/// di creare una copia dentro il progetto di origine.
/// Riconosce Save As sulla cartella corrente senza richiederne lo svuotamento.
/// Non modifica il filesystem.
/// </summary>
internal static class ProjectFolderValidator
{
    /// <summary>
    /// Valida e normalizza il percorso della cartella destinata a ospitare un nuovo progetto o un salvataggio con nome.
    /// </summary>
    /// <param name="destinationFolder">Il percorso relativo o assoluto della cartella di destinazione.</param>
    /// <returns>
    /// Una tupla contenente:
    /// <list type="bullet">
    ///   <item><term>DestinationFolder</term><description>Il percorso assoluto normalizzato della destinazione.</description></item>
    ///   <item><term>ParentFolder</term><description>Il percorso assoluto della cartella genitore validata.</description></item>
    /// </list>
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Lanciata se <paramref name="destinationFolder"/> è nullo, vuoto, composto da soli spazi, 
    /// oppure se coincide con la radice di un'unità disco (es. <c>C:\</c>).
    /// </exception>
    /// <exception cref="DirectoryNotFoundException">
    /// Lanciata se la cartella genitore specificata nel percorso non esiste sul disco.
    /// </exception>
    /// <exception cref="IOException">
    /// Lanciata se la destinazione è un file, un symlink/junction, o una cartella esistente non vuota.
    /// </exception>
    internal static (string DestinationFolder, string ParentFolder) ValidateDestination(string destinationFolder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationFolder);

        string fullDestination = NormalizeFolder(destinationFolder);

        DirectoryInfo destination = new(fullDestination);

        DirectoryInfo parent = destination.Parent
            ?? throw new ArgumentException(
                "The project folder cannot be a drive root.",
                nameof(destinationFolder));

        if (!parent.Exists)
        {
            throw new DirectoryNotFoundException(
                $"Parent folder not found: {parent.FullName}");
        }

        EnsureDestinationAvailable(destination);

        return (fullDestination, parent.FullName);
    }

    internal static (string SourceFolder, string DestinationFolder, bool IsSameFolder) ValidateSaveAsFolders(string sourceFolder, string destinationFolder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFolder);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationFolder);

        string fullSource = NormalizeFolder(sourceFolder);
        string fullDestination = NormalizeFolder(destinationFolder);

        if (!Directory.Exists(fullSource))
            throw new DirectoryNotFoundException($"Folder not found: {fullSource}");

        if (AreSameFolders(fullSource, fullDestination))
            return (fullSource, fullDestination, true);

        if (IsDestinationInsideSource(fullSource, fullDestination))
        {
            throw new IOException(
                "Choose a destination outside the current project folder.");
        }

        ValidateDestination(fullDestination);

        return (fullSource, fullDestination, false);
    }

    /// <summary>
    /// Assicura che la destinazione non entri in conflitto con file esistenti,
    /// collegamenti simbolici o directory contenenti già elementi.
    /// </summary>
    /// <param name="destination">L'istanza <see cref="DirectoryInfo"/> che descrive la cartella target.</param>
    /// <exception cref="IOException">
    /// Lanciata se sul percorso esiste già un file, se la cartella è un reparse point, 
    /// oppure se la cartella contiene almeno un file o sottocartella.
    /// </exception>
    private static void EnsureDestinationAvailable(DirectoryInfo destination)
    {
        if (File.Exists(destination.FullName))
            throw new IOException("The destination is a file.");

        if (!destination.Exists)
            return;

        if (IsLinkedDirectory(destination))
            throw new IOException("The destination cannot be a linked folder.");

        if (destination.EnumerateFileSystemInfos().Any())
            throw new IOException("Choose a new or empty project folder.");
    }

    /// <summary>
    /// Verifica se la directory specificata è un Reparse Point (collegamento simbolico o junction).
    /// </summary>
    /// <param name="directory">La directory da verificare.</param>
    /// <returns><c>true</c> se la directory presenta il flag <see cref="FileAttributes.ReparsePoint"/>; altrimenti <c>false</c>.</returns>
    private static bool IsLinkedDirectory(DirectoryInfo directory)
    {
        return (directory.Attributes & FileAttributes.ReparsePoint) != 0;
    }

    private static string NormalizeFolder(string folder)
    {
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));
    }

    private static bool AreSameFolders(string source, string destination)
    {
        StringComparison comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return string.Equals(source, destination, comparison);
    }

    private static bool IsDestinationInsideSource(string source, string destination)
    {
        string relativePath = Path.GetRelativePath(source, destination);

        return !Path.IsPathRooted(relativePath)
            && relativePath != ".."
            && !relativePath.StartsWith(
                ".." + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }
}
