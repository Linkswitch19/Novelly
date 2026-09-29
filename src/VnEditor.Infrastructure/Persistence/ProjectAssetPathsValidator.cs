using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.AppService.Serialization;
using VnEditor.Domain;
using VnEditor.Domain.Assets;
using VnEditor.Domain.Characters;

namespace VnEditor.Infrastructure.Persistence;

/// <summary>
/// Si occupa di verificare che i file associati agli asset del progetto 
/// (come sfondi, tracce musicali ed espressioni dei personaggi) siano formati unicamente dal nome del file.
/// Previene l'uso di percorsi assoluti o vulnerabilità di "directory traversal" (es. navigazione tra cartelle genitore).
/// </summary>
internal static class ProjectAssetPathsValidator
{
    /// <summary>
    /// Elenco dei caratteri non consentiti all'interno del nome di un file per evitare 
    /// la creazione di percorsi complessi o non validi su diversi sistemi operativi.
    /// </summary>
    private static readonly char[] ForbiddenPathCharacters = ['/', '\\', ':'];

    /// <summary>
    /// Esamina ciclicamente tutte le risorse multimediali contenute nel progetto 
    /// per assicurarsi che nessuna di esse punti a percorsi illegali.
    /// </summary>
    /// <param name="project">Il progetto contenente gli asset da scansionare.</param>
    /// <exception cref="ArgumentNullException">Lanciata se l'istanza del progetto è nulla.</exception>
    internal static void Validate(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);

        foreach (Asset background in project.Backgrounds)
            ValidateAsset(background);

        foreach (Asset track in project.Music)
            ValidateAsset(track);

        foreach (Character character in project.Characters)
        {
            foreach (Asset expression in character.Expressions)
                ValidateAsset(expression);
        }
    }
    /// <summary>
    /// Verifica che la stringa rappresenti solo il nome di un file, senza alcun percorso associato.
    /// Rifiuta stringhe vuote, riferimenti a cartelle correnti/superiori ("." o ".."), 
    /// e la presenza di separatori di directory o percorsi radice/assoluti.
    /// </summary>
    /// <param name="fileName">Il nome del file da valutare.</param>
    /// <returns><c>true</c> se la stringa contiene un nome file pulito e privo di percorsi; altrimenti, <c>false</c>.</returns>
    private static bool IsValidFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        if (fileName is "." or "..")
            return false;

        if (fileName.IndexOfAny(ForbiddenPathCharacters) >= 0)
            return false;

        return !Path.IsPathRooted(fileName);
    }

    /// <summary>
    /// Controlla la correttezza di un singolo asset, verificando sia che l'oggetto esista,
    /// sia che la proprietà <see cref="Asset.FileName"/> contenga un nome file conforme alle regole.
    /// </summary>
    /// <param name="asset">La risorsa da validare.</param>
    /// <exception cref="ProjectSerializationException">
    /// Lanciata se l'asset è nullo o se il suo nome file contiene caratteri vietati/percorsi assoluti.
    /// </exception>
    private static void ValidateAsset(Asset asset)
    {
        if (asset is null)
        {
            throw new ProjectSerializationException(
                ProjectSerializationException.FailureKind.InvalidContent,
                "Il progetto contiene un asset nullo.");
        }

        if (!IsValidFileName(asset.FileName))
        {
            throw new ProjectSerializationException(
                ProjectSerializationException.FailureKind.InvalidContent,
                $"Nome di file non valido per l'asset '{asset.Id}': '{asset.FileName}'.");
        }
    }

}
