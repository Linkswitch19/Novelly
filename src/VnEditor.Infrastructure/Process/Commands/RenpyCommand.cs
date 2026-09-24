using System;
using System.Collections.Generic;
using System.Text;

namespace VnEditor.Infrastructure.Process.Commands;
/// <summary>
/// Rappresenta un comando da eseguire tramite il motore Ren'Py, 
/// incapsulando la directory di base (es. working directory) e gli argomenti a riga di comando.
/// </summary>
internal sealed class RenpyCommand
{
    /// <summary>
    /// Inizializza una nuova istanza di <see cref="RenpyCommand"/> validando gli input
    /// e normalizzando i dati (convertendo il percorso in assoluto e congelando gli argomenti).
    /// </summary>
    /// <param name="baseDirectory">La directory di base (es. la radice del progetto Ren'Py) associata al comando.</param>
    /// <param name="arguments">La sequenza di argomenti da passare all'eseguibile di Ren'Py.</param>
    /// <exception cref="ArgumentException">Lanciata se <paramref name="baseDirectory"/> è null, vuota o composta unicamente da spazi bianchi.</exception>
    /// <exception cref="ArgumentNullException">Lanciata se <paramref name="arguments"/> è null.</exception>
    public RenpyCommand(string baseDirectory ,IEnumerable<string> arguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);
        ArgumentNullException.ThrowIfNull(arguments);

        BaseDirectory = Path.GetFullPath(baseDirectory);
        // Risolve l'IEnumerable in un array (copiando i dati) e lo espone in sola lettura
        Arguments = Array.AsReadOnly(arguments.ToArray());
    }

    /// <summary>
    /// Ottiene il percorso assoluto della directory di base per l'esecuzione del comando.
    /// </summary>
    public string BaseDirectory { get; }
    /// <summary>
    /// Ottiene una lista in sola lettura contenente gli argomenti del comando.
    /// </summary>
    public IReadOnlyList<string> Arguments { get; }


}
