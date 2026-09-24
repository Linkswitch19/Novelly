using System;
using System.Collections.Generic;
using System.Text;

namespace VnEditor.AppService.Process;
/// <summary>
/// Risultato dell'esecuzione di un comando Ren'Py.
/// ExitCode 0 significa successo; qualsiasi altro valore è un errore.
/// </summary>

public sealed record RunResult
(
    int ExitCode,
    string Output,
    string Error
)
{
    public bool Success => ExitCode == 0;
}
