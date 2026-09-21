using System;
using System.Collections.Generic;
using System.Text;

namespace VnEditor.Infrastructure.Assets;
/// <summary>
/// Copia gli asset importati dall'utente nelle cartelle corrette del
/// progetto Ren'Py, rinominandoli con il nome interno sicuro.
/// Il file originale non viene mai modificato o cancellato.
/// </summary>

public sealed class AssetImporter
{
    private readonly string _gameFolder;
    public AssetImporter(string gameFolder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameFolder);
        this._gameFolder = gameFolder;
    }
    /// <summary>
    /// Copia un'immagine (sfondo o sprite) in game/images.
    /// </summary>
    public void ImportImage(string sourcePath, string internalFileName)
    {
        string destination = Path.Combine(
            this._gameFolder, "game", "images", internalFileName);
        CopyFile(sourcePath, destination);

    }

    private void CopyFile(string source, string destination)
    {
        if (!File.Exists(source))
            throw new FileNotFoundException(
                $"File non trovato: {source}", source);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.Copy(source, destination, overwrite: true);

    }


    /// <summary>
    /// Copia un file audio in game/audio.
    /// </summary>
    public void ImportAudio(string sourcePath, string internalFileName)
    {
        string destination = Path.Combine(
            _gameFolder, "game", "audio", internalFileName);

        CopyFile(sourcePath, destination);
    }
}
