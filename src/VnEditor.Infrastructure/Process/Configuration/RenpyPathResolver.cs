using System;
using System.Collections.Generic;
using System.Text;

namespace VnEditor.Infrastructure.Process.Configuration;

/// <summary>
/// Fornisce metodi di utilità per localizzare e risolvere i percorsi dei componenti 
/// fondamentali dell'SDK di Ren'Py (eseguibile, cartella launcher e template di progetto)
/// navigando gerarchicamente il file system.
/// </summary>
internal static class RenpyPathResolver
{
    /// <summary>Nome della directory radice dell'SDK di Ren'Py.</summary>
    private const string RenpySdkDirectoryName = "renpy-sdk";

    /// <summary>Nome della directory degli strumenti di sviluppo in cui può risiedere l'SDK.</summary>
    private const string ToolsDirectoryName = "tools";
    /// <summary>Nome della directory principale delle risorse dell'applicazione.</summary>
    private const string ResourcesDirectoryName = "resources";
    /// <summary>Nome della cartella contenente il template di base per i progetti Ren'Py.</summary>
    private const string TemplateDirectoryName = "renpy_template";
    /// <summary>Nome della directory interna dell'SDK contenente il launcher di Ren'Py.</summary>
    private const string LauncherDirectoryName = "launcher";
    /// <summary>Nome del file eseguibile di Ren'Py per ambienti Windows.</summary>
    private const string WindowsExecutableName = "renpy.exe";

    /// <summary>Nome dello script/eseguibile di Ren'Py per ambienti Unix (Linux/macOS).</summary>
    private const string UnixExecutableName = "renpy.sh";



    /// <summary>
    /// Individua i percorsi dell'eseguibile, della directory launcher e del template Ren'Py,
    /// cercando ricorsivamente a partire dalla cartella indicata e risalendo verso le directory genitore.
    /// </summary>
    /// <param name="startDirectory">Il percorso della directory da cui avviare la ricerca verso l'alto.</param>
    /// <returns>
    /// Una tupla contenente:
    /// <list type="bullet">
    ///   <item><description><c>ExecutablePath</c>: Percorso assoluto dell'eseguibile di Ren'Py appropriato per l'OS in uso.</description></item>
    ///   <item><description><c>LauncherPath</c>: Percorso assoluto della cartella launcher all'interno dell'SDK.</description></item>
    ///   <item><description><c>TemplatePath</c>: Percorso assoluto della directory contenente il template di progetto.</description></item>
    /// </list>
    /// </returns>
    /// <exception cref="ArgumentException">Lanciata se <paramref name="startDirectory"/> è null, vuota o composta solo da spazi.</exception>
    /// <exception cref="InvalidOperationException">Lanciata se la struttura dell'SDK o delle risorse Ren'Py non viene individuata lungo la gerarchia.</exception>
    /// <exception cref="FileNotFoundException">Lanciata se la directory SDK esiste ma manca l'eseguibile per la piattaforma corrente.</exception>
    /// <exception cref="DirectoryNotFoundException">Lanciata se la directory SDK esiste ma manca la sottocartella launcher.</exception>

    public static (string ExecutablePath, string LauncherPath, string TemplatePath) Resolve(string startDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(startDirectory);

        DirectoryInfo? directory = new(Path.GetFullPath(startDirectory));

        while (directory is not null)
        {
            (string ExecutablePath, string LauncherPath, string TemplatePath)? paths =
                TryResolve(directory.FullName);
            if (paths.HasValue)
                return paths.Value;

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
          $"Installazione Ren'Py non trovata partendo da: {startDirectory}");
    }

    /// <summary>
    /// Tenta di risolvere i percorsi necessari assumendo la cartella specificata come radice dell'installazione.
    /// </summary>
    /// <param name="root">Il percorso della directory radice candidata.</param>
    /// <returns>
    /// Una tupla con i percorsi risolti se la cartella template e l'SDK esistono in <paramref name="root"/>; 
    /// altrimenti, <see langword="null"/>.
    /// </returns>

    private static (string ExecutablePath, string LauncherPath, string TemplatePath)? TryResolve(string root)
    {
        string templatePath = Path.Combine(root, ResourcesDirectoryName,TemplateDirectoryName);
        string? sdkPath = FindSdkPath(root);

        if (!Directory.Exists(templatePath) || sdkPath is null)
            return null;

        string executablePath = FindExecutablePath(sdkPath);
        string launcherPath = FindLauncherPath(sdkPath);

        return (executablePath, launcherPath, templatePath);

    }
    /// <summary>
    /// Cerca la directory dell'SDK di Ren'Py all'interno della radice fornita, 
    /// verificando sia il percorso pacchettizzato standard (<c>renpy-sdk</c>) 
    /// sia la variante per ambiente di sviluppo (<c>tools/renpy-sdk</c>).
    /// </summary>
    /// <param name="root">Il percorso radice in cui effettuare la ricerca.</param>
    /// <returns>Il percorso assoluto dell'SDK se presente; in caso contrario, <see langword="null"/>.</returns>
    private static string? FindSdkPath(string root)
    {
        string packagedSdkPath = Path.Combine(root, RenpySdkDirectoryName);

        if (Directory.Exists(packagedSdkPath))
            return packagedSdkPath;

        string developmentSdkPath = Path.Combine(root, ToolsDirectoryName,RenpySdkDirectoryName);

        if (Directory.Exists(developmentSdkPath))
            return developmentSdkPath;

        return null;
    }

    /// <summary>
    /// Restituisce il percorso dell'eseguibile di Ren'Py per il sistema operativo corrente all'interno dell'SDK indicato.
    /// </summary>
    /// <param name="sdkPath">Il percorso della directory dell'SDK di Ren'Py.</param>
    /// <returns>Il percorso assoluto dell'eseguibile corrispondente (<c>renpy.exe</c> su Windows, <c>renpy.sh</c> su Unix).</returns>
    /// <exception cref="FileNotFoundException">Lanciata se il file eseguibile per la piattaforma corrente non è presente nella cartella SDK.</exception>

    private static string FindExecutablePath(string sdkPath)
    {
        string executableName = OperatingSystem.IsWindows() ? WindowsExecutableName : UnixExecutableName;
        string executablePath = Path.Combine(sdkPath, executableName);

        if (File.Exists(executablePath))
            return executablePath;

        throw new FileNotFoundException("Eseguibile Ren'Py non trovato", executablePath);
    }
    /// <summary>
    /// Restituisce il percorso della cartella del launcher all'interno dell'SDK indicato.
    /// </summary>
    /// <param name="sdkPath">Il percorso della directory dell'SDK di Ren'Py.</param>
    /// <returns>Il percorso assoluto della cartella del launcher.</returns>
    /// <exception cref="DirectoryNotFoundException">Lanciata se la cartella <c>launcher</c> non esiste all'interno dell'SDK.</exception>
    private static string FindLauncherPath(string sdkPath)
    {
        string launcherPath = Path.Combine(sdkPath, LauncherDirectoryName);

        if (Directory.Exists(launcherPath))
            return launcherPath;

        throw new DirectoryNotFoundException(
            $"Launcher Ren'Py non trovato: {launcherPath}");

    }

    

    
}
