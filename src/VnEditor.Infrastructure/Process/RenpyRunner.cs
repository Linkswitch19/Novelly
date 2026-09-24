using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.AppService.Process;
using VnEditor.Infrastructure.Process.Commands;
using VnEditor.Infrastructure.Process.Execution;

namespace VnEditor.Infrastructure.Process;
/// <summary>
/// Avvia Ren'Py come processo esterno tramite la sua interfaccia CLI.
/// Usa sempre ArgumentList per gestire correttamente i percorsi con spazi.
/// Usa sempre Path.GetFullPath perché Ren'Py cambia la working directory.
/// </summary>
public sealed class RenpyRunner : IRenpyRunner
{
    private readonly RenpyCommandFactory _commandFactory;
    private readonly RenpyProcessExecutor _processExecutor;

    /// <summary>
    /// Inizializza un'istanza del motore di esecuzione di Ren'Py, 
    /// predisponendo la command factory e l'esecutore dei processi in base alla configurazione.
    /// </summary>
    /// <param name="configuration">La configurazione contenente i percorsi dell'SDK di Ren'Py.</param>
    /// <exception cref="ArgumentNullException">Lanciata se <paramref name="configuration"/> è null.</exception>
    public RenpyRunner(IRenpyConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        this._commandFactory = new RenpyCommandFactory(configuration.LauncherPath);
        this._processExecutor = new RenpyProcessExecutor(configuration.ExecutablePath);
    }

    /// <summary>
    /// Avvia il progetto visivo di Ren'Py dall'inizio in modalità di gioco normale.
    /// </summary>
    /// <param name="projectPath">Il percorso radice del progetto Ren'Py da eseguire.</param>
    /// <param name="ct">Token per annullare l'esecuzione in corso.</param>
    /// <returns>Il risultato finale dell'esecuzione (codice di uscita, standard output e standard error).</returns>

    public Task<RunResult> PlayAsync(string projectPath, CancellationToken ct = default)
    {
        string fullProjectPath = RenpyProjectValidator.ValidateAndNormalize(projectPath);
        RenpyCommand command = this._commandFactory.CreatePlay(fullProjectPath);
        return this._processExecutor.ExecuteAsync(command, ct);

    }

    /// <summary>
    /// Avvia il progetto di Ren'Py ma forza il salto (warp) direttamente alla label specificata.
    /// </summary>
    /// <param name="projectPath">Il percorso radice del progetto Ren'Py.</param>
    /// <param name="labelId">Il nome della label a cui saltare (es. "start").</param>
    /// <param name="ct">Token per annullare l'esecuzione in corso.</param>
    /// <returns>Il risultato dell'esecuzione asincrona.</returns>

    public async Task<RunResult> PlayFromAsync(
       string projectPath,
       string labelId,
       CancellationToken ct = default)
    {
        string fullProjectPath = RenpyProjectValidator.ValidateAndNormalize(projectPath);
        string warpSpec = await RenpyWarpResolver.ResolveAsync(fullProjectPath, labelId, ct);
        RenpyCommand command = this._commandFactory.CreatePlayFrom(fullProjectPath, warpSpec);

        return await this._processExecutor.ExecuteAsync(command, ct);

    }

    /// <summary>
    /// Esegue il Linting (controllo errori e sintassi) sul progetto, unendo lo Standard Error del processo
    /// con gli eventuali file di diagnostica (errors.txt/traceback.txt) generati fisicamente nella cartella.
    /// </summary>
    /// <param name="projectPath">Il percorso radice del progetto da analizzare.</param>
    /// <param name="ct">Token per annullare l'operazione di linting.</param>
    /// <returns>Il risultato dell'analisi statica con la proprietà Error consolidata e arricchita.</returns>

    public async Task<RunResult> LintAsync(string projectPath, CancellationToken ct = default)
    {
        string fullProjectPath = RenpyProjectValidator.ValidateAndNormalize(projectPath);

        RenpyDiagnosticFiles.DeleteExisting(fullProjectPath);

        RenpyCommand command = this._commandFactory.CreateLint(fullProjectPath);
        RunResult result = await this._processExecutor.ExecuteAsync(command, ct);
        string diagnostics = await RenpyDiagnosticFiles.ReadAsync(fullProjectPath, ct);
        string error = CombineErrors(result.Error, diagnostics);

        return result with { Error = error };
    }


    /// <summary>
    /// Esegue l'utility di esportazione/distribuzione per generare i pacchetti compilati del gioco.
    /// </summary>
    /// <param name="projectPath">Il percorso radice del progetto da distribuire.</param>
    /// <param name="ct">Token per annullare la distribuzione.</param>
    /// <returns>Il risultato del processo di building.</returns>
    public Task<RunResult> DistributeAsync(string projectPath, CancellationToken ct = default)
    {
        //Si ripete per avere tutto piu indipendente
        string fullProjectPath = RenpyProjectValidator.ValidateAndNormalize(projectPath);
        RenpyCommand command = this._commandFactory.CreateDistribute(fullProjectPath);

        return this._processExecutor.ExecuteAsync(command, ct);
    }

    /// <summary>
    /// Interrompe immediatamente qualsiasi processo Ren'Py attualmente in esecuzione.
    /// </summary>
    public void StopCurrent()
    {
        this._processExecutor.StopCurrent();
    }


    /// <summary>
    /// Combina il testo letto dallo standard error del processo con i testi ricavati 
    /// dai file di diagnostica, inserendo la corretta spaziatura.
    /// </summary>
    /// <param name="standardError">L'output del flusso Standard Error generato dal processo.</param>
    /// <param name="diagnostics">L'output letto dai file errors.txt e traceback.txt.</param>
    /// <returns>Una singola stringa concatenata con eventuali separatori, o la stringa disponibile se l'altra è assente.</returns>
    private static string CombineErrors(string standardError, string diagnostics)
    {
        if (string.IsNullOrWhiteSpace(standardError))
            return diagnostics;

        if (string.IsNullOrWhiteSpace(diagnostics))
            return standardError;

        return standardError.TrimEnd()
            + Environment.NewLine
            + Environment.NewLine
            + diagnostics;
    }
}
