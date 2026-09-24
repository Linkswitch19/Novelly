using System;
using System.Collections.Generic;
using System.Text;

namespace VnEditor.Infrastructure.Process;

/// <summary>
/// Fornisce metodi di utilità per validare l'esistenza e la struttura di base di un progetto Ren'Py.
/// </summary>
internal static class RenpyProjectValidator
{
    /// <summary>Nome della directory standard in cui Ren'Py memorizza gli script e gli asset del gioco.</summary>
    private const string GameDirectoryName = "game";

    /// <summary>
    /// Valida il percorso specificato per assicurarsi che esista e che rappresenti un progetto Ren'Py valido
    /// (ovvero che contenga la cartella "game" al suo interno).
    /// </summary>
    /// <param name="projectPath">Il percorso (relativo o assoluto) del progetto da validare.</param>
    /// <returns>Il percorso assoluto e normalizzato del progetto.</returns>
    /// <exception cref="ArgumentException">
    /// Lanciata se <paramref name="projectPath"/> è null, vuoto o composto unicamente da spazi.
    /// </exception>
    /// <exception cref="DirectoryNotFoundException">
    /// Lanciata se la directory del progetto non esiste fisicamente, 
    /// oppure se non contiene la sottocartella obbligatoria "game".
    /// </exception>
    public static string ValidateAndNormalize(string projectPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        // Ottiene il percorso assoluto, risolvendo eventuali percorsi relativi (es. "..\" o ".\")
        string fullProjectPath = Path.GetFullPath(projectPath);

        if (!Directory.Exists(fullProjectPath))
            throw new DirectoryNotFoundException($"Progetto Ren'Py non trovato: {fullProjectPath}");

        string gamePath = Path.Combine(fullProjectPath, GameDirectoryName);

        if(!Directory.Exists(gamePath))
            throw new DirectoryNotFoundException($"Cartella game non trovata: {gamePath}");

        return fullProjectPath;
    }



}
