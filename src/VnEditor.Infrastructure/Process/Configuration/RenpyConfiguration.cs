using VnEditor.AppService.Process;


namespace VnEditor.Infrastructure.Process.Configuration;
/// <summary>
/// Rappresenta l'implementazione concreta della configurazione dell'ambiente Ren'Py,
/// risolvendo e memorizzando i percorsi assoluti dei componenti chiave del motore.
/// </summary>
public sealed class RenpyConfiguration : IRenpyConfiguration
{




    /// <summary>
    /// Inizializza una nuova istanza di <see cref="RenpyConfiguration"/> risolvendo i percorsi
    /// necessari a partire dalla directory indicata o da quella base dell'applicazione.
    /// </summary>
    /// <param name="baseDirectory">
    /// Cartella di partenza per la ricerca dei componenti. Se <see langword="null"/>,
    /// viene utilizzata la directory corrente di esecuzione dell'applicazione (<see cref="AppContext.BaseDirectory"/>).
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Lanciata se l'installazione o le risorse di Ren'Py non vengono individuate risalendo l'albero delle cartelle.
    /// </exception>
    /// <exception cref="System.IO.FileNotFoundException">
    /// Lanciata se l'eseguibile di Ren'Py non è presente nella cartella dell'SDK.
    /// </exception>
    /// <exception cref="System.IO.DirectoryNotFoundException">
    /// Lanciata se la directory del launcher non esiste all'interno dell'SDK.
    /// </exception>
    public RenpyConfiguration(string? baseDirectory = null)
    {

        (string executablePath, string launcherPath, string templatePath) =
            RenpyPathResolver.Resolve(baseDirectory ?? AppContext.BaseDirectory);

        ExecutablePath = executablePath;
        LauncherPath = launcherPath;
        TemplatePath = templatePath;

    }
    /// <summary>
    /// Ottiene il percorso assoluto del file eseguibile di Ren'Py (<c>renpy.exe</c> su Windows, <c>renpy.sh</c> su Unix).
    /// </summary>
    public string ExecutablePath { get; }
    /// <summary>
    /// Ottiene il percorso assoluto della cartella interna <c>launcher</c> dell'SDK di Ren'Py.
    /// </summary>
    public string LauncherPath { get; }
    /// <summary>
    /// Ottiene il percorso assoluto della cartella contenente i file template di base per i progetti Ren'Py.
    /// </summary>
    public string TemplatePath { get; }
}
