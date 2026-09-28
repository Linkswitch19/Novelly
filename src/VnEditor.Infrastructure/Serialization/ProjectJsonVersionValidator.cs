using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using VnEditor.AppService.Serialization;
using VnEditor.Domain;

namespace VnEditor.Infrastructure.Serialization;

/// <summary>
/// Il "Buttafuori" del sistema di caricamento.
/// Controlla che il file JSON abbia un numero di versione valido e supportato 
/// prima di permettere al programma di leggerne l'intero contenuto.
/// </summary>
internal static class ProjectJsonVersionValidator
{
    /// <summary>
    /// Ispeziona superficialmente il file JSON appena aperto, cercando solo l'etichetta "format_version".
    /// Se manca, o se è scritta male (es. invece di un numero c'è una parola), blocca tutto.
    /// </summary>
    /// <param name="root">La "radice" del file JSON (la scatola principale che contiene tutto).</param>
    /// <exception cref="ProjectSerializationException">Lanciata se il file è corrotto o senza versione.</exception>
    internal static void Validate(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("format_version", out JsonElement value) ||
            value.ValueKind != JsonValueKind.Number ||
            !value.TryGetInt32(out int version))
        {
            throw new ProjectSerializationException(
                ProjectSerializationException.FailureKind.InvalidContent,
                "La versione del formato è mancante o non valida.");
        }
        Validate(version);


    }

    /// <summary>
    /// Controlla se il numero di versione letto dal file coincide con quello che 
    /// la nostra versione attuale del programma sa leggere.
    /// </summary>
    /// <param name="version">Il numero di versione appena estratto dal file JSON.</param>
    /// <exception cref="ProjectSerializationException">Lanciata se la versione è troppo vecchia o troppo nuova.</exception>
    internal static void Validate (int version)
    {
        if (version != Project.CurrentFormatVersion)
        {
            throw new ProjectSerializationException(
               ProjectSerializationException.FailureKind.UnsupportedVersion,
               $"Versione del formato non supportata: {version}.");

        }
    }
}
