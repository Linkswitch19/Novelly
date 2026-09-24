using VnEditor.AppService.Process;
using VnEditor.Infrastructure.Process;
using VnEditor.Tests.Fakes;


namespace VnEditor.Tests.Infrastructure.Process;

/// <summary>
/// Verifica la facade RenpyRunner: validazione del progetto, avvio delegato,
/// gestione delle label inesistenti e arresto senza un processo attivo.
/// </summary>
public sealed class RenpyRunnerTests : IDisposable
{
    private readonly string _tempFolder;
    private readonly string _projectPath;
    private readonly RenpyRunner _runner;

    public RenpyRunnerTests()
    {
        _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        _projectPath = Path.Combine(_tempFolder, "progetto");

        string gamePath = Path.Combine(_projectPath, "game");
        string launcherPath = Path.Combine(_tempFolder, "launcher");
        string templatePath = Path.Combine(_tempFolder, "template");
        string executablePath = GetExecutablePath();

        Directory.CreateDirectory(gamePath);
        Directory.CreateDirectory(launcherPath);
        Directory.CreateDirectory(templatePath);

        File.WriteAllText(
            Path.Combine(gamePath, "script.rpy"),
            """
            label start:
                return
            """);

        FakeRenpyConfiguration configuration = new(
            executablePath,
            launcherPath,
            templatePath);

        _runner = new RenpyRunner(configuration);
    }

    public void Dispose()
    {
        _runner.StopCurrent();

        if (Directory.Exists(_tempFolder))
            Directory.Delete(_tempFolder, recursive: true);
    }

    [Fact]
    public async Task PlayAsyncEsegueIlComando()
    {
        RunResult result = await _runner.PlayAsync(_projectPath);

        Assert.True(result.Success, result.Error);
        Assert.False(string.IsNullOrWhiteSpace(result.Output));
    }

    [Fact]
    public async Task ProgettoInesistenteNonEAccettato()
    {
        string missingProjectPath = Path.Combine(_tempFolder, "inesistente");

        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => _runner.PlayAsync(missingProjectPath));
    }

    [Fact]
    public async Task LabelInesistenteNonEAccettata()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _runner.PlayFromAsync(_projectPath, "inesistente"));
    }

    [Fact]
    public void StopCurrentSenzaProcessoNonGeneraErrori()
    {
        Exception? exception = Record.Exception(_runner.StopCurrent);

        Assert.Null(exception);
    }

    [Fact]
    public void ConfigurazioneNullNonEAccettata()
    {
        Assert.Throws<ArgumentNullException>(
            () => new RenpyRunner(null!));
    }

    private static string GetExecutablePath()
    {
        if (OperatingSystem.IsWindows())
        {
            string systemDirectory = Environment.GetFolderPath(
                Environment.SpecialFolder.System);

            return Path.Combine(systemDirectory, "tree.com");
        }

        return "/bin/echo";
    }
}