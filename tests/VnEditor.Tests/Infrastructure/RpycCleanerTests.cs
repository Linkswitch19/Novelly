using VnEditor.Infrastructure.Generation;
using Xunit;

namespace VnEditor.Tests.Infrastructure;

public sealed class RpycCleanerTests : IDisposable
{
    private readonly string _gameFolder;

    public RpycCleanerTests()
    {
        _gameFolder = Path.Combine(
            Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_gameFolder);
    }

    public void Dispose()
    {
        if (Directory.Exists(_gameFolder))
            Directory.Delete(_gameFolder, recursive: true);
    }

    [Fact]
    public void CleanCancellaIFileRpyc()
    {
        File.WriteAllText(Path.Combine(_gameFolder, "script.rpyc"), "");

        RpycCleaner.Clean(_gameFolder);

        Assert.Empty(Directory.GetFiles(_gameFolder, "*.rpyc",
            SearchOption.AllDirectories));
    }

    [Fact]
    public void CleanCancellaIFileRpymc()
    {
        File.WriteAllText(Path.Combine(_gameFolder, "script.rpymc"), "");

        RpycCleaner.Clean(_gameFolder);

        Assert.Empty(Directory.GetFiles(_gameFolder, "*.rpymc",
            SearchOption.AllDirectories));
    }

    [Fact]
    public void CleanNonCancellaIFileRpy()
    {
        string scriptPath = Path.Combine(_gameFolder, "script.rpy");
        File.WriteAllText(scriptPath, "label start:\n    return\n");

        RpycCleaner.Clean(_gameFolder);

        Assert.True(File.Exists(scriptPath));
    }

    [Fact]
    public void CleanFunzionaAncheInSottocartelle()
    {
        string subFolder = Path.Combine(_gameFolder, "game");
        Directory.CreateDirectory(subFolder);
        File.WriteAllText(Path.Combine(subFolder, "script.rpyc"), "");

        RpycCleaner.Clean(_gameFolder);

        Assert.Empty(Directory.GetFiles(_gameFolder, "*.rpyc",
            SearchOption.AllDirectories));
    }
}