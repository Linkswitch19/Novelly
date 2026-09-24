using VnEditor.Infrastructure.Process.Configuration;

namespace VnEditor.Tests.Infrastructure.Process.Configuration;

//Questo verifica entrambi i layout supportati e il caso di installazione assente.
public sealed class RenpyConfigurationTests : IDisposable
{
    private readonly string _tempFolder;
    private readonly string _executableName;

    public RenpyConfigurationTests()
    {
        _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        _executableName = OperatingSystem.IsWindows() ? "renpy.exe" : "renpy.sh";

        Directory.CreateDirectory(_tempFolder);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempFolder))
            Directory.Delete(_tempFolder, recursive: true);
    }

    [Fact]
    public void RisolveLayoutDiSviluppo()
    {
        string sdkPath = CreateInstallation(packaged: false);
        string startDirectory = CreateNestedStartDirectory();

        RenpyConfiguration configuration = new(startDirectory);

        Assert.Equal(Path.Combine(sdkPath, _executableName), configuration.ExecutablePath);
        Assert.Equal(Path.Combine(sdkPath, "launcher"), configuration.LauncherPath);
        Assert.Equal(
            Path.Combine(_tempFolder, "resources", "renpy_template"),
            configuration.TemplatePath);
    }

    [Fact]
    public void RisolveLayoutDistribuito()
    {
        string sdkPath = CreateInstallation(packaged: true);
        string startDirectory = CreateNestedStartDirectory();

        RenpyConfiguration configuration = new(startDirectory);

        Assert.Equal(Path.Combine(sdkPath, _executableName), configuration.ExecutablePath);
        Assert.Equal(Path.Combine(sdkPath, "launcher"), configuration.LauncherPath);
        Assert.Equal(
            Path.Combine(_tempFolder, "resources", "renpy_template"),
            configuration.TemplatePath);
    }

    [Fact]
    public void InstallazioneInesistenteNonEAccettata()
    {
        string startDirectory = CreateNestedStartDirectory();

        Assert.Throws<InvalidOperationException>(
            () => new RenpyConfiguration(startDirectory));
    }

    private string CreateInstallation(bool packaged)
    {
        string sdkPath = packaged
            ? Path.Combine(_tempFolder, "renpy-sdk")
            : Path.Combine(_tempFolder, "tools", "renpy-sdk");

        Directory.CreateDirectory(sdkPath);
        Directory.CreateDirectory(Path.Combine(sdkPath, "launcher"));
        Directory.CreateDirectory(
            Path.Combine(_tempFolder, "resources", "renpy_template"));

        File.WriteAllText(Path.Combine(sdkPath, _executableName), string.Empty);

        return sdkPath;
    }

    private string CreateNestedStartDirectory()
    {
        string startDirectory = Path.Combine(
            _tempFolder,
            "src",
            "VnEditor.UI",
            "bin",
            "Debug",
            "net10.0");

        Directory.CreateDirectory(startDirectory);

        return startDirectory;
    }
}