using VnEditor.Infrastructure.Process;

namespace VnEditor.Tests.Infrastructure.Process;
//Questo copre il percorso valido e tutti i rami di errore del validatore.
public sealed class RenpyProjectValidatorTests : IDisposable
{
    private readonly string _tempFolder;

    public RenpyProjectValidatorTests()
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
    public void ProgettoValidoRestituiscePercorsoNormalizzato()
    {
        string projectPath = Path.Combine(_tempFolder, "progetto");
        string inputPath = Path.Combine(projectPath, ".");

        Directory.CreateDirectory(Path.Combine(projectPath, "game"));

        string result = RenpyProjectValidator.ValidateAndNormalize(inputPath);

        Assert.Equal(Path.GetFullPath(projectPath), result);
    }

    [Fact]
    public void ProgettoInesistenteNonEAccettato()
    {
        string projectPath = Path.Combine(_tempFolder, "inesistente");

        Assert.Throws<DirectoryNotFoundException>(
            () => RenpyProjectValidator.ValidateAndNormalize(projectPath));
    }

    [Fact]
    public void ProgettoSenzaCartellaGameNonEAccettato()
    {
        string projectPath = Path.Combine(_tempFolder, "progetto");
        Directory.CreateDirectory(projectPath);

        Assert.Throws<DirectoryNotFoundException>(
            () => RenpyProjectValidator.ValidateAndNormalize(projectPath));
    }

    [Fact]
    public void PercorsoVuotoNonEAccettato()
    {
        Assert.Throws<ArgumentException>(
            () => RenpyProjectValidator.ValidateAndNormalize(" "));
    }
}