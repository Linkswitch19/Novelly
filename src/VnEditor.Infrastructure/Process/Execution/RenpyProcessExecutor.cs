using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using VnEditor.AppService.Process;
using VnEditor.Infrastructure.Process.Commands;
using SystemProcess = System.Diagnostics.Process;

namespace VnEditor.Infrastructure.Process.Execution;

internal sealed class RenpyProcessExecutor
{

    private readonly string _executablePath;
    /// <summary>Oggetto utilizzato per sincronizzare l'accesso alle risorse condivise (lock).</summary>
    private readonly object _processLock = new();
    /// <summary>Riferimento al processo di Ren'Py attualmente in esecuzione (se presente).</summary>
    private SystemProcess? _currentProcess;

    /// <summary>
    /// Inizializza un nuovo esecutore per i processi di Ren'Py.
    /// </summary>
    /// <param name="executablePath">Il percorso assoluto dell'eseguibile di Ren'Py.</param>
    /// <exception cref="ArgumentException">Lanciata se <paramref name="executablePath"/> è nullo o vuoto.</exception>

    public RenpyProcessExecutor(string executablePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        this._executablePath = Path.GetFullPath(executablePath);
    }
    /// <summary>
    /// Avvia l'esecuzione di un comando Ren'Py e attende il suo completamento in modo asincrono.
    /// Se è già in esecuzione un altro processo, questo viene terminato prima del nuovo avvio.
    /// </summary>
    /// <param name="command">Il comando contenente la directory di base e gli argomenti da passare all'eseguibile.</param>
    /// <param name="ct">Token per richiedere l'annullamento dell'esecuzione.</param>
    /// <returns>Un oggetto <see cref="RunResult"/> contenente il codice di uscita, lo standard output e lo standard error.</returns>
    /// <exception cref="ArgumentNullException">Lanciata se <paramref name="command"/> è null.</exception>
    /// <exception cref="OperationCanceledException">Lanciata se l'operazione viene annullata tramite il <paramref name="ct"/>.</exception>
    public async Task<RunResult> ExecuteAsync (RenpyCommand command,CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ct.ThrowIfCancellationRequested();

        using SystemProcess process = new()
        {
            StartInfo = CreateStartInfo(command)
        };
        StartProcess(process);
        try
        {
            return await WaitForResultAsync(process, ct);
        }
        finally
        {
            ClearCurrentProcess(process);
        }


    }

    /// <summary>
    /// Attende la terminazione del processo, raccogliendo in modo asincrono l'output e gli errori.
    /// Registra inoltre il token di annullamento per forzare la chiusura se necessario.
    /// </summary>
    private static async Task<RunResult> WaitForResultAsync(SystemProcess process, CancellationToken ct)
    {
        Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> errorTask = process.StandardError.ReadToEndAsync();

        using CancellationTokenRegistration registration = ct.Register(() => TryKill(process));

        try
        {
            await process.WaitForExitAsync(ct);
        }
        catch(OperationCanceledException)
        {
            TryKill(process);

            try
            {
                Task exitTask = process.WaitForExitAsync(CancellationToken.None);
                await exitTask.WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch (TimeoutException)
            {

            }
            
            throw;
        }
        string output = await outputTask;
        string error = await errorTask;

        return new RunResult(process.ExitCode, output, error);
    }
    /// <summary>
    /// Richiede esplicitamente e in modo sicuro (thread-safe) la chiusura del processo attualmente in esecuzione.
    /// </summary>
    public void StopCurrent()
    {
        lock (this._processLock)
        {
            StopCurrentLocked();

        }
    }

    /// <summary>
    /// Costruisce i parametri di avvio per il processo di sistema, incanalando l'output per la lettura
    /// e impostando correttamente la working directory e gli argomenti.
    /// </summary>
    private ProcessStartInfo CreateStartInfo(RenpyCommand command)
    {
        string executableDirectory = Path.GetDirectoryName(this._executablePath)
            ?? throw new InvalidOperationException($"Directory non valida: {_executablePath}");

        ProcessStartInfo startInfo = new()
        {
            FileName = this._executablePath,
            WorkingDirectory = executableDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        startInfo.ArgumentList.Add(command.BaseDirectory);

        foreach (string argument in command.Arguments)
            startInfo.ArgumentList.Add(argument);

        return startInfo;
            
        
    }

    /// <summary>
    /// Avvia il processo in modo thread-safe. Prima di avviarlo, si assicura di abbattere
    /// qualsiasi altro processo Ren'Py gestito da questa istanza.
    /// </summary>
    private void StartProcess(SystemProcess process)
    {
        lock (this._processLock)
        {
            StopCurrentLocked();

            if (!process.Start())
                throw new InvalidOperationException("Impossibile avviare Ren'Py");

            this._currentProcess = process;
            
        }
    }

    /// <summary>
    /// Interrompe il processo corrente se esistente e resetta il riferimento.
    /// DEVE essere chiamato solo all'interno di un blocco di lock su <c>_processLock</c>.
    /// </summary>
    private void StopCurrentLocked()
    {
        SystemProcess? process = this._currentProcess;
        this._currentProcess= null;

        if (process is not null)
            TryKill(process);
    }
    /// <summary>
    /// Rimuove il riferimento al processo corrente in modo thread-safe, ma solo se coincide 
    /// con quello passato come parametro. Evita di azzerare la variabile se nel frattempo 
    /// è stato avviato un processo più recente.
    /// </summary>

    private void ClearCurrentProcess(SystemProcess process)
    {
        lock (this._processLock)
        {
            if(ReferenceEquals(this._currentProcess,process))
                this._currentProcess = null;
        }
    }
    /// <summary>
    /// Tenta di terminare forzatamente l'intero albero di processi, inghiottendo le eccezioni
    /// comuni in cui si incorre se il processo è già terminato o bloccato a livello di sistema operativo.
    /// </summary>

    private static void TryKill(SystemProcess process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // Il processo non è più disponibile.
        }
        catch (Win32Exception)
        {
            // Il sistema operativo non ha potuto terminare il processo.
        }
        catch (AggregateException)
        {
            // Non tutti i processi discendenti sono stati terminati.
        }
    }








}
