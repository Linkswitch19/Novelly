using VnEditor.Infrastructure.Process.Commands;

namespace VnEditor.Tests.Infrastructure.Process.Commands;

/// <summary>
/// Verifica la costruzione dei comandi CLI per avvio, anteprima, lint e distribuzione,
/// controllando directory di base, ordine degli argomenti e validazione degli input.
/// </summary>
public sealed class RenpyCommandFactoryTests : IDisposable
{
    private readonly string _tempFolder;
    private readonly string _projectPath;
    private readonly string _launcherPath;
    private readonly RenpyCommandFactory _factory;

    public RenpyCommandFactoryTests()
    {
        _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        _projectPath = Path.Combine(_tempFolder, "progetto");
        _launcherPath = Path.Combine(_tempFolder, "renpy-sdk", "launcher");

        Directory.CreateDirectory(_projectPath);
        Directory.CreateDirectory(_launcherPath);

        _factory = new RenpyCommandFactory(_launcherPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempFolder))
            Directory.Delete(_tempFolder, recursive: true);
    }

    [Fact]
    public void CreatePlayNonAggiungeArgomenti()
    {
        RenpyCommand command = _factory.CreatePlay(_projectPath);

        Assert.Equal(Path.GetFullPath(_projectPath), command.BaseDirectory);
        Assert.Empty(command.Arguments);
    }

    [Fact]
    public void CreatePlayFromAggiungeWarp()
    {
        RenpyCommand command = _factory.CreatePlayFrom(_projectPath, "script.rpy:42");

        string[] expectedArguments =
        [
            "--warp",
            "script.rpy:42"
        ];

        Assert.Equal(Path.GetFullPath(_projectPath), command.BaseDirectory);
        Assert.Equal(expectedArguments, command.Arguments);
    }

    [Fact]
    public void CreateLintAggiungeComandoLint()
    {
        RenpyCommand command = _factory.CreateLint(_projectPath);

        string[] expectedArguments =
        [
            "lint"
        ];

        Assert.Equal(Path.GetFullPath(_projectPath), command.BaseDirectory);
        Assert.Equal(expectedArguments, command.Arguments);
    }

    [Fact]
    public void CreateDistributeUsaLauncherEProgettoComeArgomento()
    {
        RenpyCommand command = _factory.CreateDistribute(_projectPath);

        string[] expectedArguments =
        [
            "distribute",
            "--package",
            "pc",
            Path.GetFullPath(_projectPath)
        ];

        Assert.Equal(Path.GetFullPath(_launcherPath), command.BaseDirectory);
        Assert.Equal(expectedArguments, command.Arguments);
    }

    [Fact]
    public void WarpVuotoNonEAccettato()
    {
        Assert.Throws<ArgumentException>(
            () => _factory.CreatePlayFrom(_projectPath, " "));
    }

    [Fact]
    public void LauncherVuotoNonEAccettato()
    {
        Assert.Throws<ArgumentException>(
            () => new RenpyCommandFactory(" "));
    }
}