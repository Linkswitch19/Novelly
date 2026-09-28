using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using VnEditor.Domain;
using VnEditor.Domain.Characters;
using VnEditor.Domain.Scenes;
using VnEditor.Domain.Scenes.Blocks;
using VnEditor.Domain.Variables;

namespace VnEditor.Infrastructure.Serialization;

/// <summary>
/// È il "Direttore" che crea le impostazioni finali per salvare e caricare l'intero progetto.
/// Mette insieme le regole di Blocchi, Variabili e del formato del testo JSON.
/// </summary>
internal static class ProjectJsonOptions
{
    /// <summary>
    /// Crea il pacchetto completo delle impostazioni JSON da usare nel programma.
    /// </summary>
    internal static JsonSerializerOptions Create()
    {
        // Prepariamo un "ispettore" che guarderà ogni classe prima di salvarla o caricarla
        DefaultJsonTypeInfoResolver resolver = new();
        resolver.Modifiers.Add(Configure); // Diciamo all'ispettore di usare le nostre regole (metodo Configure)

        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            // Assegniamo il nostro ispettore con tutte le regole
            TypeInfoResolver = resolver,
            // Tolleranza zero: se nel file JSON c'è un dato che non esiste nelle nostre classi, dà errore
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            RespectNullableAnnotations = true,
            WriteIndented = true
        };
    }

    /// <summary>
    /// L'ispettore passa da qui per OGNI classe che deve salvare o caricare, 
    /// e le applica le regole giuste.
    /// </summary>
    private static void Configure(JsonTypeInfo typeInfo)
    {
        BlockRegistry.Apply(typeInfo);
        VariableRegistry.Apply(typeInfo);

        switch (typeInfo.Type)
        {
            case Type project when project == typeof(Project):
                Populate(typeInfo, "characters", "backgrounds", "music", "scenes", "variables");
                Require(typeInfo, "format_version", "start_scene_id", "id_counters");
                break;

            case Type character when character == typeof(Character):
                Populate(typeInfo, "expressions");
                Require(typeInfo, "color", "expression_id_counters");
                break;

            case Type scene when scene == typeof(Scene):
                Populate(typeInfo, "blocks");
                break;

            case Type block when typeof(Block).IsAssignableFrom(block):
                RemoveComputedBlockProperties(typeInfo);
                Require(typeInfo, "id");
                break;

            case Type variable when typeof(Variable).IsAssignableFrom(variable) && !variable.IsAbstract:
                Require(typeInfo, "initial_value");
                break;
        }
    }
    /// <summary>
    /// Rende obbligatorie una o più proprietà specifiche. 
    /// Se durante il caricamento il file JSON non contiene queste informazioni, 
    /// il sistema blocca tutto e dà errore, evitando che il gioco carichi dati incompleti.
    /// </summary>
    private static void Require(JsonTypeInfo typeInfo, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            JsonPropertyInfo property = typeInfo.Properties.Single(
                candidate => candidate.Name == propertyName);

            property.IsRequired = true;
        }
    }

    /// <summary>
    /// Insegna al JSON a "riempire" liste già esistenti invece di crearne di nuove e sovrascriverle.
    /// </summary>
    private static void Populate(JsonTypeInfo typeInfo, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            // Trova la proprietà (es. la lista "blocks" della Scena)
            JsonPropertyInfo property = typeInfo.Properties.Single(
                candidate => candidate.Name == propertyName);
            // Dice al sistema: "Non creare una lista nuova, usa quella che c'è già e aggiungici dentro la roba"
            property.ObjectCreationHandling = JsonObjectCreationHandling.Populate;
        }
    }
    /// <summary>
    /// Nasconde i dati calcolati in automatico per evitare di scriverli inutilmente nel file di salvataggio.
    /// </summary>
    private static void RemoveComputedBlockProperties(JsonTypeInfo typeInfo)
    {
        // Scorriamo la lista al contrario (perché se cancelliamo qualcosa, gli indici non si sballano)
        for (int index = typeInfo.Properties.Count - 1; index >= 0; index--)
        {
            string name = typeInfo.Properties[index].Name;
            // "label" e "ends_scene" sono calcolati dal programma in tempo reale, 
            // quindi li cancelliamo dalle istruzioni di salvataggio del JSON per risparmiare spazio.
            if (name is "label" or "ends_scene")
                typeInfo.Properties.RemoveAt(index);
        }
    }

    
}
