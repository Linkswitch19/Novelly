namespace VnEditor.AppService.Process;

/// <summary>
/// Configurazione dell'SDK Ren'Py. In sviluppo punta a tools/renpy-sdk,
/// in produzione all'SDK accanto all'eseguibile.
/// </summary>
public interface IRenpyConfiguration
{
    /// <summary>Percorso assoluto dell'eseguibile Ren'Py.</summary>
    string ExecutablePath { get; }
    /// <summary>
    /// Percorso assoluto della cartella del progetto launcher di Ren'Py.
    /// Necessario per i comandi CLI avanzati come 'distribute' (build).
    /// </summary>
    
    string LauncherPath { get; }

    /// <summary>Percorso assoluto del template Ren'Py.</summary>
    string TemplatePath { get; }
}