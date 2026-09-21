using VnEditor.Infrastructure.Assets;

namespace VnEditor.Tests.Infrastructure;

public sealed class AssetImporterTests : IDisposable
{
    private readonly string _tempFolder;
    private readonly string _gameFolder;
    private readonly string _sourceFile;

    public AssetImporterTests()
    {
        this._tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        this._gameFolder = Path.Combine(this._tempFolder, ".renpy_game");
        this._sourceFile = Path.Combine(this._tempFolder, "sfondo.png");

        Directory.CreateDirectory(_tempFolder);
        File.WriteAllText(_sourceFile, "contenuto finto");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempFolder))
            Directory.Delete(_tempFolder, recursive: true);
    }

    [Fact]
    public void ImportImageCopiaIlFileInImages()
    {
        AssetImporter importer = new(_gameFolder);
        importer.ImportImage(_sourceFile, "bg_1.png");

        string destination = Path.Combine(_gameFolder, "game", "images", "bg_1.png");
        Assert.True(File.Exists(destination));
    }

    [Fact]
    public void ImportAudioCopiaIlFileInAudio()
    {
        string audioFile = Path.Combine(_tempFolder, "tema.ogg");
        File.WriteAllText(audioFile, "audio finto");

        AssetImporter importer = new(_gameFolder);
        importer.ImportAudio(audioFile, "mus_1.ogg");

        string destination = Path.Combine(_gameFolder, "game", "audio", "mus_1.ogg");
        Assert.True(File.Exists(destination));
    }

    [Fact]
    public void ImportImageNonCancellaIlFileOriginale()
    {
        AssetImporter importer = new(_gameFolder);
        importer.ImportImage(_sourceFile, "bg_1.png");

        Assert.True(File.Exists(_sourceFile));
    }

    [Fact]
    public void FileInesistenteGeneraEccezione()
    {
        AssetImporter importer = new(_gameFolder);

        Assert.Throws<FileNotFoundException>(
            () => importer.ImportImage("file/inesistente.png", "bg_1.png"));
    }
}