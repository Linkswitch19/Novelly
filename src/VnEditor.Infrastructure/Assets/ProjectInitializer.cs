using System;
using System.Collections.Generic;
using System.Text;

namespace VnEditor.Infrastructure.Assets;
/// <summary>
/// Crea la struttura del progetto Ren'Py nascosto copiando il template
/// e preparando le cartelle per gli asset.
/// L'utente non vede mai questa cartella: la gestisce l'app.
/// </summary>
/// 
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
    
    public string Initialize(string projectFolder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectFolder);
        string gameFolder = Path.Combine(projectFolder, ".renpy_game");
        if (!Directory.Exists(gameFolder))
            CopyDirectory(_templatePath, gameFolder);
        return gameFolder;
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (string file in Directory.GetFiles(source))
        {
            string destFiles = Path.Combine(destination, Path.GetFileName(file));
            File.Copy(file, destFiles, overwrite: true);

        }

        foreach (string subDir in Directory.GetDirectories(source))
        {
            string destSubDir = Path.Combine(
                destination, Path.GetFileName(subDir));
            CopyDirectory(subDir, destSubDir);
        }
    }


}
