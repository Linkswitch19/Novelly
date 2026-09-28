using System.Text.Json;
using VnEditor.AppService.Serialization;

namespace VnEditor.Infrastructure.Serialization;

/// <summary>
/// Verifica l'integrità strutturale del file JSON prima della deserializzazione.
/// Assicura che tutte le collezioni (array) obbligatorie per il progetto
/// siano presenti e formattate correttamente, prevenendo errori durante il caricamento.
/// </summary>
internal static class ProjectJsonCollectionsValidator
{
    /// <summary>
    /// Esegue la validazione completa dei nodi JSON. Verifica sia gli array principali
    /// (alla radice del documento) sia quelli annidati all'interno degli elementi.
    /// </summary>
    /// <param name="root">L'elemento JSON principale (radice) che rappresenta l'intero progetto.</param>
    internal static void Validate(JsonElement root)
    {
        // Verifica la presenza dell'array dei personaggi e lo memorizza per l'ispezione
        JsonElement characters = RequireArray(root, "characters");

        // Itera su ogni personaggio per assicurarsi che contenga la propria lista di espressioni
        foreach (JsonElement character in characters.EnumerateArray())
            RequireArray(character, "expressions");

        // Verifica la presenza degli array per sfondi e musiche
        RequireArray(root, "backgrounds");
        RequireArray(root, "music");

        // Verifica la presenza dell'array delle scene e lo memorizza per l'ispezione
        JsonElement scenes = RequireArray(root, "scenes");

        // Itera su ogni scena per assicurarsi che contenga la propria lista di blocchi
        foreach (JsonElement scene in scenes.EnumerateArray())
            RequireArray(scene, "blocks");

        // Verifica la presenza dell'array delle variabili
        RequireArray(root, "variables");
    }

    /// <summary>
    /// Verifica che una specifica proprietà esista all'interno di un nodo genitore
    /// e che sia rigorosamente di tipo Array.
    /// </summary>
    /// <param name="parent">Il nodo JSON in cui cercare la proprietà (es. il root o una specifica scena).</param>
    /// <param name="propertyName">Il nome esatto della proprietà JSON da validare (es. "blocks").</param>
    /// <returns>Restituisce l'elemento JSON validato per permettere eventuali ispezioni annidate.</returns>
    /// <exception cref="ProjectSerializationException">Generata se la proprietà è assente o non è un array valido.</exception>
    private static JsonElement RequireArray(JsonElement parent, string propertyName)
    {
        // Valida tre condizioni fondamentali:
        // 1. Il genitore deve essere un oggetto JSON valido.
        // 2. La proprietà specificata deve esistere all'interno del genitore.
        // 3. Il valore della proprietà deve essere un JSON Array.
        if (parent.ValueKind != JsonValueKind.Object ||
            !parent.TryGetProperty(propertyName, out JsonElement value) ||
            value.ValueKind != JsonValueKind.Array)
        {
            throw new ProjectSerializationException(
                ProjectSerializationException.FailureKind.InvalidContent,
                $"Lista JSON mancante o non valida: '{propertyName}'.");
        }

        // Restituisce l'array validato
        return value;
    }
}