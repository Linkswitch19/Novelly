using VnEditor.Infrastructure.Process.Execution;

namespace VnEditor.Tests.Infrastructure.Process.Execution;

/// <summary>
/// Verifica la pulizia e la lettura dei file diagnostici generati da Ren'Py,
/// inclusi file assenti, contenuti vuoti e cancellazione asincrona.
/// </summary>
public sealed class RenpyDiagnosticFilesTests : IDisposable
{
    private readonly string _tempFolder;
    private readonly string _errorsPath;
    private readonly string _tracebackPath;

    public RenpyDiagnosticFilesTests()
    {
        _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        _errorsPath = Path.Combine(_tempFolder, "errors.txt");
        _tracebackPath = Path.Combine(_tempFolder, "traceback.txt");

        Directory.CreateDirectory(_tempFolder);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempFolder))
            Directory.Delete(_tempFolder, recursive: true);
    }

    [Fact]
    public void DeleteExistingEliminaSoloIFileDiagnostici()
    {
        string unrelatedPath = Path.Combine(_tempFolder, "script.rpy");

        File.WriteAllText(_errorsPath, "Errore");
        File.WriteAllText(_tracebackPath, "Traceback");
        File.WriteAllText(unrelatedPath, "label start:");

        RenpyDiagnosticFiles.DeleteExisting(_tempFolder);

        Assert.False(File.Exists(_errorsPath));
        Assert.False(File.Exists(_tracebackPath));
        Assert.True(File.Exists(unrelatedPath));
    }

    [Fact]
    public async Task ReadAsyncCombinaIFilePresenti()
    {
        File.WriteAllText(_errorsPath, "Errore di sintassi");
        File.WriteAllText(_tracebackPath, "Dettaglio eccezione");

        string result = await RenpyDiagnosticFiles.ReadAsync(_tempFolder);

        string expected =
            $"=== errors.txt ==={Environment.NewLine}Errore di sintassi"
            + Environment.NewLine
            + Environment.NewLine
            + $"=== traceback.txt ==={Environment.NewLine}Dettaglio eccezione";

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ReadAsyncIgnoraFileVuoti()
    {
        File.WriteAllText(_errorsPath, "   ");
        File.WriteAllText(_tracebackPath, "Dettaglio eccezione");

        string result = await RenpyDiagnosticFiles.ReadAsync(_tempFolder);

        string expected =
            $"=== traceback.txt ==={Environment.NewLine}Dettaglio eccezione";

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ReadAsyncSenzaFileRestituisceStringaVuota()
    {
        string result = await RenpyDiagnosticFiles.ReadAsync(_tempFolder);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task ReadAsyncRispettaLaCancellazione()
    {
        File.WriteAllText(_errorsPath, "Errore");

        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => RenpyDiagnosticFiles.ReadAsync(
                _tempFolder,
                cancellation.Token));
    }
}