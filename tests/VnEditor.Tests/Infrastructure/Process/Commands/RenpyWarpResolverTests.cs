using VnEditor.Infrastructure.Process.Commands;

namespace VnEditor.Tests.Infrastructure.Process.Commands;

/// <summary>
/// Verifica la conversione delle label Ren'Py nel formato file:riga richiesto
/// dall'opzione --warp, inclusi i casi di input non valido e script assente.
/// </summary>
public sealed class RenpyWarpResolverTests : IDisposable
{
    private readonly string _tempFolder;
    private readonly string _projectPath;
    private readonly string _scriptPath;

    public RenpyWarpResolverTests()
    {
        _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        _projectPath = Path.Combine(_tempFolder, "progetto");
        _scriptPath = Path.Combine(_projectPath, "game", "script.rpy");

        Directory.CreateDirectory(Path.GetDirectoryName(_scriptPath)!);

        File.WriteAllText(
            _scriptPath,
            """
            label start:
                jump scena_due

            label scena_due:
                return
            """);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempFolder))
            Directory.Delete(_tempFolder, recursive: true);
    }

    [Fact]
    public async Task LabelEsistenteRestituisceFileERiga()
    {
        string result = await RenpyWarpResolver.ResolveAsync(_projectPath, "scena_due");

        Assert.Equal("script.rpy:4", result);
    }

    [Fact]
    public async Task LabelInesistenteNonEAccettata()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => RenpyWarpResolver.ResolveAsync(_projectPath, "inesistente"));
    }

    [Fact]
    public async Task ScriptInesistenteNonEAccettato()
    {
        File.Delete(_scriptPath);

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => RenpyWarpResolver.ResolveAsync(_projectPath, "start"));
    }

    [Fact]
    public async Task LabelVuotaNonEAccettata()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => RenpyWarpResolver.ResolveAsync(_projectPath, " "));
    }

    [Fact]
    public async Task CancellazioneVieneRispettata()
    {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => RenpyWarpResolver.ResolveAsync(
                _projectPath,
                "start",
                cancellation.Token));
    }
}