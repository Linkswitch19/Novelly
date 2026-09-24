using System;
using System.Collections.Generic;
using System.Text;

namespace VnEditor.Infrastructure.Process.Commands;

/// <summary>
/// Fornisce metodi factory per costruire rapidamente istanze preconfigurate di <see cref="RenpyCommand"/>,
/// necessarie per avviare diverse operazioni sul motore Ren'Py (come il gioco normale, il warp, il linting o l'esportazione).
/// </summary>

internal sealed class RenpyCommandFactory
{

    /// <summary>Comando Ren'Py per l'analisi statica degli script (linting).</summary>
    private const string LintCommandName = "lint";

    /// <summary>Comando Ren'Py per generare le build di distribuzione del gioco.</summary>
    private const string DistributeCommandName = "distribute";

    /// <summary>Opzione a riga di comando per saltare direttamente a una riga specifica del gioco.</summary>
    private const string WarpOptionName = "--warp";

    /// <summary>Opzione a riga di comando per specificare il tipo di pacchetto di destinazione durante la distribuzione.</summary>
    private const string PackageOptionName = "--package";

    /// <summary>Identificatore del formato di pacchetto destinato a piattaforme PC (Windows/Linux).</summary>
    private const string PcPackageName = "pc";

    /// <summary>Il percorso assoluto della cartella del launcher all'interno dell'SDK di Ren'Py.</summary>
    private readonly string _launcherPath;


    /// <summary>
    /// Inizializza una nuova istanza di <see cref="RenpyCommandFactory"/> memorizzando il percorso del launcher di Ren'Py.
    /// </summary>
    /// <param name="launcherPath">Il percorso della directory del launcher di Ren'Py.</param>
    /// <exception cref="ArgumentException">Lanciata se <paramref name="launcherPath"/> è null, vuota o composta da soli spazi.</exception>
    public RenpyCommandFactory(string launcherPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(launcherPath);
        this._launcherPath = Path.GetFullPath(launcherPath);
    }

    /// <summary>
    /// Crea un comando per avviare normalmente il progetto visivo dall'inizio.
    /// </summary>
    /// <param name="projectPath">Il percorso radice del progetto Ren'Py da eseguire.</param>
    /// <returns>Un <see cref="RenpyCommand"/> configurato senza argomenti aggiuntivi.</returns>

    public RenpyCommand CreatePlay(string projectPath)
    {
        return new RenpyCommand(
            projectPath,
            Array.Empty<string>());
    }

    /// <summary>
    /// Crea un comando per avviare il progetto visivo saltando direttamente a uno specifico punto dello script.
    /// </summary>
    /// <param name="projectPath">Il percorso radice del progetto Ren'Py da eseguire.</param>
    /// <param name="warpSpec">La stringa di destinazione nel formato <c>file:riga</c> (es. <c>script.rpy:42</c>).</param>
    /// <returns>Un <see cref="RenpyCommand"/> configurato con l'opzione <c>--warp</c>.</returns>
    /// <exception cref="ArgumentException">Lanciata se <paramref name="warpSpec"/> è null, vuota o composta da soli spazi.</exception>
    public RenpyCommand CreatePlayFrom(string projectPath, string warpSpec)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(warpSpec);

        string[] arguments =
        [
            WarpOptionName,
            warpSpec
        ];

        return new RenpyCommand(projectPath, arguments);
    }

    /// <summary>
    /// Crea un comando per eseguire il controllo sintattico e logico (Lint) degli script del progetto.
    /// </summary>
    /// <param name="projectPath">Il percorso radice del progetto Ren'Py da analizzare.</param>
    /// <returns>Un <see cref="RenpyCommand"/> configurato per invocare il comando interno <c>lint</c>.</returns>

    public RenpyCommand CreateLint(string projectPath)
    {
        string[] arguments =
        [
            LintCommandName
        ];

        return new RenpyCommand(projectPath, arguments); 
    }

    /// <summary>
    /// Crea un comando per generare una build di distribuzione del progetto (esportazione per PC).
    /// </summary>
    /// <param name="projectPath">Il percorso radice del progetto Ren'Py da distribuire.</param>
    /// <returns>Un <see cref="RenpyCommand"/> configurato per eseguire l'utility di distribuzione del launcher.</returns>
    /// <remarks>
    /// A differenza degli altri comandi, questo imposta la <c>BaseDirectory</c> sulla cartella del launcher 
    /// invece che sul progetto, passando il percorso del progetto come argomento.
    /// </remarks>

    public RenpyCommand CreateDistribute(string projectPath)
    {
        string[] arguments =
        [
            DistributeCommandName,
            PackageOptionName,
            PcPackageName,
            Path.GetFullPath(projectPath)
        ];

        return new RenpyCommand(this._launcherPath, arguments);
    }




}
