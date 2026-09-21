
using VnEditor.Domain.Assets;
using VnEditor.Infrastructure.Assets;

namespace VnEditor.Tests.Infrastructure;

public sealed class ProjectInitializerTests : IDisposable
{
    private readonly string _tempFolder;
    private readonly string _templateFolder;
    public ProjectInitializerTests()
    {
        this._tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        this._templateFolder = Path.Combine(this._tempFolder, "template");

        // Crea un template minimo
        Directory.CreateDirectory(Path.Combine(this._templateFolder, "game"));
        File.WriteAllText(
            Path.Combine(this._templateFolder, "game", "script.rpy"),
            "label start:\n    return\n");
    }

    public void Dispose()
    {
        if (Directory.Exists(this._tempFolder))
            Directory.Delete(this._tempFolder, recursive: true);
    }

    [Fact]
    public void InitializeCopiaTuttoIlTemplate()
    {
        string projectFolder = Path.Combine(this._tempFolder, "progetto");
        Directory.CreateDirectory(projectFolder);

        ProjectInitializer initializer = new(this._templateFolder);
        string gameFolder = initializer.Initialize(projectFolder);

        Assert.True(Directory.Exists(gameFolder));
        Assert.True(File.Exists(Path.Combine(gameFolder, "game", "script.rpy")));
    }


    [Fact]
    public void InitializeNonSovrascriveSeFolderEsiste()
    {
        string projectFolder = Path.Combine(_tempFolder, "progetto");
        Directory.CreateDirectory(projectFolder);

        ProjectInitializer initializer = new(_templateFolder);
        string gameFolder = initializer.Initialize(projectFolder);

        // Scrivi un file aggiuntivo
        File.WriteAllText(Path.Combine(gameFolder, "game", "extra.txt"), "test");

        // Reinizializza: il file extra deve restare
        initializer.Initialize(projectFolder);

        Assert.True(File.Exists(Path.Combine(gameFolder, "game", "extra.txt")));

    }

    [Fact]
    public void TemplateInesistenteNonEAccettato()
    {
        Assert.Throws<ArgumentException>(
            () => new ProjectInitializer("percorso/inesistente"));
    }
}
