using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using VnEditor.Domain.Scenes.Blocks;

namespace VnEditor.Infrastructure.Serialization;



/// <summary>
/// Gestisce l'elenco dei blocchi (Background, Dialogue, ecc.) e insegna 
/// al programma come salvarli e caricarli correttamente in formato JSON.
/// </summary>
internal static class BlockRegistry
{
    /// <summary>
    /// Associa ogni classe (es. DialogueBlock) alla sua etichetta testuale nel file JSON (es. "dialogue").
    /// </summary>
    private static readonly (Type Type, string Discriminator)[] Entries =
    [
        (typeof(BackgroundBlock), "background"),
        (typeof(DialogueBlock), "dialogue"),
        (typeof(MusicBlock), "music"),
        (typeof(ShowCharacterBlock), "show_character")
    ];
    /// <summary>
    /// Controlla se un'etichetta (es. "dialogue" o "music") fa parte dei blocchi supportati.
    /// </summary>
    /// <param name="discriminator">Il nome dell'etichetta da cercare.</param>
    /// <returns>Restituisce true se il blocco esiste, altrimenti false.</returns>
    internal static bool Contains(string discriminator) =>
        Entries.Any(entry =>
            string.Equals(entry.Discriminator, discriminator, StringComparison.Ordinal));


    /// <summary>
    /// Configura il sistema JSON in modo che sappia distinguere e creare i vari tipi di blocco.
    /// </summary>
    /// <param name="typeInfo">L'oggetto del sistema JSON da configurare.</param>
    /// <remarks>
    /// Il sistema leggerà il campo "type" nel file JSON per capire quale blocco specifico creare. 
    /// Se trova un tipo sconosciuto, blocca il caricamento e genera un errore.
    /// </remarks>
    internal static void Apply(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type != typeof(Block))
            return;
        JsonPolymorphismOptions polymorphism = new()
        {
            TypeDiscriminatorPropertyName = "type",
            IgnoreUnrecognizedTypeDiscriminators = false,
            UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization
        };

        foreach ((Type type, string discriminator) in Entries)
            polymorphism.DerivedTypes.Add(new JsonDerivedType(type, discriminator));

        typeInfo.PolymorphismOptions = polymorphism;

    }


}
