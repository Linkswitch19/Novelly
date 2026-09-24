using VnEditor.AppService.Process;
using VnEditor.Infrastructure.Process.Commands;
using VnEditor.Infrastructure.Process.Execution;

namespace VnEditor.Tests.Infrastructure.Process.Execution;

/// <summary>
/// Verifica l'esecuzione di un processo esterno, la raccolta del risultato,
/// la cancellazione preventiva e l'arresto quando nessun processo è attivo.
/// </summary>
public sealed class RenpyProcessExecutorTests : IDisposable
{
    private readonly string _tempFolder;

    public RenpyProcessExecutorTests()
    {
        _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempFolder);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempFolder))
            Directory.Delete(_tempFolder, recursive: true);
    }

    [Fact]
    public async Task ExecuteAsyncRestituisceIlRisultatoDelProcesso()
    {
        (string executablePath, string baseDirectory) = GetSuccessfulCommand();

        RenpyProcessExecutor executor = new(executablePath);
        RenpyCommand command = new(baseDirectory, Array.Empty<string>());

        RunResult result = await executor.ExecuteAsync(command);

        Assert.True(result.Success, result.Error);
        Assert.False(string.IsNullOrWhiteSpace(result.Output));
    }

    [Fact]
    public async Task TokenGiaAnnullatoNonAvviaIlProcesso()
    {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        string executablePath = Path.Combine(_tempFolder, "inesistente.exe");
        RenpyProcessExecutor executor = new(executablePath);
        RenpyCommand command = new(_tempFolder, Array.Empty<string>());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => executor.ExecuteAsync(command, cancellation.Token));
    }

    [Fact]
    public void StopCurrentSenzaProcessoNonGeneraErrori()
    {
        string executablePath = Path.Combine(_tempFolder, "inesistente.exe");
        RenpyProcessExecutor executor = new(executablePath);

        Exception? exception = Record.Exception(executor.StopCurrent);

        Assert.Null(exception);
    }

    [Fact]
    public void EseguibileVuotoNonEAccettato()
    {
        Assert.Throws<ArgumentException>(
            () => new RenpyProcessExecutor(" "));
    }

    private (string ExecutablePath, string BaseDirectory) GetSuccessfulCommand()
    {
        if (OperatingSystem.IsWindows())
        {
            string systemDirectory = Environment.GetFolderPath(
                Environment.SpecialFolder.System);

            string executablePath = Path.Combine(systemDirectory, "more.com");
            string inputPath = Path.Combine(_tempFolder, "input.txt");

            File.WriteAllText(inputPath, "Output di test");

            return (executablePath, inputPath);
        }

        const string unixExecutablePath = "/bin/echo";

        return (unixExecutablePath, _tempFolder);
    }
}