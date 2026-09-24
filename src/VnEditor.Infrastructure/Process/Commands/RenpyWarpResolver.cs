using System;
using System.Collections.Generic;
using System.Text;

namespace VnEditor.Infrastructure.Process.Commands;

/// <summary>
/// Fornisce metodi di utilità per risolvere la posizione di una specifica label all'interno degli script Ren'Py,
/// generando la stringa di destinazione nel formato richiesto dal comando di warp (<c>filename:linenumber</c>).
/// </summary>
internal static class RenpyWarpResolver
{
    /// <summary>Nome della directory interna del progetto contenente gli asset e gli script di gioco.</summary>
    private const string GameDirectoryName = "game";
    /// <summary>Nome del file di script principale di Ren'Py da esaminare.</summary>
    private const string ScriptFileName = "script.rpy";

    /// <summary>
    /// Cerca in modo asincrono la definizione di una label nel file <c>script.rpy</c> del progetto specificato
    /// e restituisce il riferimento al file e al numero di riga (1-based) per l'operazione di warp.
    /// </summary>
    /// <param name="projectPath">Il percorso radice del progetto Ren'Py.</param>
    /// <param name="labelId">L'identificatore della label Ren'Py da individuare (es. <c>"start"</c>).</param>
    /// <param name="ct">Token per monitorare eventuali richieste di annullamento dell'operazione asincrona.</param>
    /// <returns>
    /// Una stringa formattata come <c>{ScriptFileName}:{lineNumber}</c> (ad esempio <c>script.rpy:42</c>)
    /// indicante il punto esatto in cui la label è dichiarata.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Lanciata se <paramref name="projectPath"/> o <paramref name="labelId"/> sono <see langword="null"/>,
    /// vuoti o composti unicamente da spazi bianchi.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Lanciata se il file di script principale (<c>game/script.rpy</c>) non esiste nel percorso specificato.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Lanciata se la label specificata non è stata individuata all'interno del file di script.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Lanciata se l'operazione viene interrotta tramite il <paramref name="ct"/>.
    /// </exception>


    public static async Task<string> ResolveAsync(string projectPath, string labelId, CancellationToken ct= default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(labelId);

        string scriptPath = Path.Combine(
            projectPath,
            GameDirectoryName,
            ScriptFileName);

        if (!File.Exists(scriptPath))
            throw new FileNotFoundException("Script Ren'Py non trovato.", scriptPath);

        string[] lines = await File.ReadAllLinesAsync(scriptPath, ct);
        string expectedLabel = $"label {labelId}:";


        for (int index = 0; index < lines.Length; index++)
        {
            if (string.Equals(lines[index].Trim(), expectedLabel, StringComparison.Ordinal))
                return $"{ScriptFileName}:{index + 1}";
        }
        throw new InvalidOperationException($"Label Ren'Py non trovata: {labelId}");

    }

}
