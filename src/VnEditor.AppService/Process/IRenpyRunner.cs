using System;
using System.Collections.Generic;
using System.Text;

namespace VnEditor.AppService.Process;
/// <summary>
/// Avvia Ren'Py come processo esterno. L'interfaccia sta in AppService
/// perché definisce il contratto; l'implementazione sta in Infrastructure
/// dove vive System.Diagnostics.Process.
/// </summary>
public interface IRenpyRunner
{
    /// <summary>Avvia il gioco dalla scena iniziale.</summary>
    Task<RunResult> PlayAsync(string projectPath,
                              CancellationToken ct = default);

    /// <summary>
    /// Avvia il gioco da una scena specifica, utile per l'anteprima.
    /// </summary>
    Task<RunResult> PlayFromAsync(string projectPath, string labelId,
                                 CancellationToken ct = default);

    /// <summary>
    /// Esegue il controllo degli errori senza aprire il gioco.
    /// Legge errors.txt e traceback.txt e li restituisce nel risultato.
    /// </summary>

    Task<RunResult> LintAsync(string projectPath,
                                CancellationToken ct = default);

    /// <summary>Crea il pacchetto distribuibile del gioco.</summary>
    Task<RunResult> DistributeAsync(string projectPath,
                                    CancellationToken ct = default);

    /// <summary>Ferma il processo in esecuzione, se presente.</summary>
    void StopCurrent();

}

