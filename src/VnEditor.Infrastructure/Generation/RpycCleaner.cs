using System;
using System.Collections.Generic;
using System.Text;

namespace VnEditor.Infrastructure.Generation;
/// <summary>
/// Cancella i file compilati di Ren'Py prima di rigenerare lo script.
/// Se restano .rpyc o .rpymc vecchi, Ren'Py li carica al posto del nuovo
/// script e il gioco non riflette le modifiche.
/// </summary>
public static class RpycCleaner
{
    public static void Clean(string gameFolder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameFolder);
        foreach (string file in Directory.GetFiles(
            gameFolder, "*.rpyc", SearchOption.AllDirectories))
            File.Delete(file);

        foreach (string file in Directory.GetFiles(
            gameFolder, "*.rpymc", SearchOption.AllDirectories))
            File.Delete(file);

    }
}