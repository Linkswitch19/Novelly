using VnEditor.AppService.Process;

namespace VnEditor.Tests.Fakes;

/// <summary>
/// Fornisce percorsi controllati ai test che dipendono da IRenpyConfiguration,
/// evitando di cercare o avviare l'installazione reale di Ren'Py.
/// </summary>
internal sealed class FakeRenpyConfiguration : IRenpyConfiguration
{
    public FakeRenpyConfiguration(
        string executablePath,
        string launcherPath,
        string templatePath)
    {
        ExecutablePath = executablePath;
        LauncherPath = launcherPath;
        TemplatePath = templatePath;
    }

    public string ExecutablePath { get; }

    public string LauncherPath { get; }

    public string TemplatePath { get; }
}