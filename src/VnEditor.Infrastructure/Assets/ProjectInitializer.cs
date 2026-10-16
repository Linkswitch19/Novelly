using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.Infrastructure.Projects;

namespace VnEditor.Infrastructure.Assets;

/// <summary>
/// Prepara la struttura Ren'Py del progetto copiando il template
/// quando .renpy_game non esiste.
/// Garantisce le cartelle game/images e game/audio anche nei progetti
/// già inizializzati, senza sovrascrivere i file esistenti.
/// </summary>
public sealed class ProjectInitializer
{
    private readonly string _templatePath;
    public ProjectInitializer(string templatePath)
    {
        if (!Directory.Exists(templatePath))
            throw new ArgumentException(
                $"Template non trovato: {templatePath}", nameof(templatePath));
        this._templatePath = templatePath;
    }

    /// <summary>
    /// Crea la cartella .renpy_game dentro la cartella del progetto utente,
    /// copiando il template. Se esiste già, non fa nulla.
    /// </summary>

    public string Initialize(
            string projectFolder,
            CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectFolder);
        cancellationToken.ThrowIfCancellationRequested();

        string gameFolder = Path.Combine(projectFolder, ".renpy_game");

        if (!Directory.Exists(gameFolder))
            ProjectDirectoryCopier.Copy(_templatePath, gameFolder, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        Directory.CreateDirectory(Path.Combine(gameFolder, "game", "images"));
        Directory.CreateDirectory(Path.Combine(gameFolder, "game", "audio"));

        return gameFolder;
    }




}
