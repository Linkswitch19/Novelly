
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using VnEditor.Domain.Variables;


namespace VnEditor.Infrastructure.Serialization;
/// <summary>
/// Gestisce l'elenco delle Variabili (Numeri, Booleani, Testo) e insegna 
/// al programma come salvarle e caricarle correttamente in formato JSON.
/// </summary>
internal static class VariableRegistry
{
    /// <summary>
    /// Associa ogni classe (es. NumberVariable) alla sua etichetta testuale nel file JSON (es. "number").
    /// </summary>
    private static readonly (Type Type, string Discriminator)[] Entries =
 [
        (typeof(NumberVariable), "number"),
        (typeof(BooleanVariable), "boolean"),
        (typeof(TextVariable), "text")
 ];
    /// <summary>
    /// Controlla se un'etichetta (es. "number" o "text") fa parte delle variabili supportate.
    /// </summary>
    internal static bool Contains(string discriminator) =>
        Entries.Any(entry =>
            string.Equals(entry.Discriminator, discriminator, StringComparison.Ordinal));

    /// <summary>
    /// Crea il manuale di istruzioni (regole JSON) per la classe generica Variable.
    /// </summary>
    internal static void Apply(JsonTypeInfo typeInfo)
    {
        // Se non stiamo guardando la classe generica "Variable", ignora e vai avanti.
        if (typeInfo.Type != typeof(Variable))
            return;

        JsonPolymorphismOptions polymorphism = new()
        {
            TypeDiscriminatorPropertyName = "type", // Cerca l'etichetta chiamata "type"
            IgnoreUnrecognizedTypeDiscriminators = false,
            UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization // Errore se l'etichetta non esiste
        };
        // 2. Scriviamo la lista delle traduzioni nella pagina "DerivedTypes"
        // (Es: "number" -> crea classe NumberVariable)
        foreach ((Type type, string discriminator) in Entries)
            polymorphism.DerivedTypes.Add(new JsonDerivedType(type, discriminator));
        // 3. "Applichiamo" le regole: le attacchiamo ufficialmente alla classe Variable
        typeInfo.PolymorphismOptions = polymorphism;
    }
}
