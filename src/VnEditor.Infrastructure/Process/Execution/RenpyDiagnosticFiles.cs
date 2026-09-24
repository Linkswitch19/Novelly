using System;
using System.Collections.Generic;
using System.Text;

namespace VnEditor.Infrastructure.Process.Execution;
/// <summary>
/// Fornisce metodi di utilità per gestire, pulire e leggere i file di diagnostica 
/// (errori e traceback) generati dal motore Ren'Py all'interno della directory di un progetto.
/// </summary>
internal static class RenpyDiagnosticFiles
{
    /// <summary>Nome del file di testo in cui Ren'Py registra gli errori di compilazione/parsing.</summary>
    private const string ErrorsFileName = "errors.txt";

    /// <summary>Nome del file di testo in cui Ren'Py registra i log delle eccezioni a runtime (traceback).</summary>
    private const string TracebackFileName = "traceback.txt";

    /// <summary>Elenco dei file di diagnostica di cui effettuare la lettura o l'eliminazione.</summary>
    private static readonly string[] FileNames =
    [
        ErrorsFileName,
        TracebackFileName,
    ];


    /// <summary>
    /// Elimina eventuali file di diagnostica preesistenti (<c>errors.txt</c> e <c>traceback.txt</c>) 
    /// dalla directory del progetto specificata. Utile per pulire i log prima di un nuovo avvio del motore.
    /// </summary>
    /// <param name="projectPath">Il percorso radice del progetto Ren'Py da cui eliminare i log.</param>
    /// <exception cref="ArgumentException">
    /// Lanciata se <paramref name="projectPath"/> è <see langword="null"/>, vuota o composta unicamente da spazi.
    /// </exception>
    public static void DeleteExisting(string projectPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);

        foreach (string fileName in FileNames)
        {
            string filePath = Path.Combine(projectPath, fileName);

            if(File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    /// <summary>
    /// Legge in modo asincrono il contenuto dei file di diagnostica presenti nel progetto e 
    /// li combina in un'unica stringa formattata, utile per l'analisi o la visualizzazione degli errori.
    /// </summary>
    /// <param name="projectPath">Il percorso radice del progetto Ren'Py contenente i file di diagnostica.</param>
    /// <param name="ct">Token per monitorare eventuali richieste di annullamento dell'operazione asincrona.</param>
    /// <returns>
    /// Una stringa contenente il testo dei log individuati, separati da righe vuote e preceduti dall'intestazione 
    /// del file di provenienza. Restituisce una stringa vuota se nessun file è presente o se sono tutti vuoti.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Lanciata se <paramref name="projectPath"/> è <see langword="null"/>, vuota o composta unicamente da spazi.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Lanciata se l'operazione di lettura viene interrotta tramite il <paramref name="ct"/>.
    /// </exception>

    public static async Task<string> ReadAsync(string projectPath, CancellationToken ct= default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);

        List<string> diagnostics = [];

        foreach (string fileName in FileNames)
        {
            string filePath = Path.Combine(projectPath, fileName);

            if (!File.Exists(filePath))
                continue;

            string content = await File.ReadAllTextAsync(filePath,ct);
            if (string.IsNullOrWhiteSpace(content))
                continue;

            string diagnostic = $"=== {fileName} ==={Environment.NewLine}{content.TrimEnd()}";
            diagnostics.Add(diagnostic);

        }

        return string.Join(
            Environment.NewLine + Environment.NewLine,
            diagnostics);
    }



}
