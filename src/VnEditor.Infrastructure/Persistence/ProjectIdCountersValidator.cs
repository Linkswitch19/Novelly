using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Text;
using VnEditor.AppService.Serialization;
using VnEditor.Domain;
using VnEditor.Domain.Characters;
using VnEditor.Domain.Scenes;

namespace VnEditor.Infrastructure.Persistence;

/// <summary>
/// Fornisce metodi per la validazione dei contatori e degli identificatori univoci (ID)
/// delle entità all'interno di un progetto. Assicura che non vi siano ID duplicati, 
/// malformati o superiori al valore massimo registrato nei contatori.
/// </summary>
internal static class ProjectIdCountersValidator
{

    /// <summary>
    /// Esegue la validazione dei contatori di ID e di tutte le entità e sotto-entità presenti nel progetto.
    /// </summary>
    /// <param name="project">Il progetto di cui validare gli identificatori.</param>
    /// <exception cref="ArgumentNullException">Lanciata se il progetto è nullo.</exception>
    internal static void Validate(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);

        IReadOnlyDictionary<string, int> projectCounters = project.IdCounters;
        ValidateCounters(projectCounters);

        HashSet<string> projectIds = new(StringComparer.Ordinal);
        ValidateEntities(project.Characters, projectCounters, projectIds);
        ValidateEntities(project.Backgrounds, projectCounters, projectIds);
        ValidateEntities(project.Music, projectCounters, projectIds);
        ValidateEntities(project.Scenes, projectCounters, projectIds);
        ValidateEntities(project.Variables, projectCounters, projectIds);

        foreach (Character character in project.Characters)
        {
            IReadOnlyDictionary<string, int> expressionCounters = character.ExpressionIdCounters;
            ValidateCounters(expressionCounters);

            HashSet<string> expressionIds = new(StringComparer.Ordinal);
            ValidateEntities(character.Expressions, expressionCounters, expressionIds);

        }



    }
    /// <summary>
    /// Valida un insieme di entità di un tipo specifico, assicurando che i loro ID siano univoci,
    /// formattati col prefisso corretto e compatibili coi contatori attuali.
    /// </summary>
    /// <typeparam name="T">Il tipo di entità derivato da <see cref="Entity"/>.</typeparam>
    /// <param name="entities">La collezione di entità da validare.</param>
    /// <param name="counters">Il dizionario contenente i valori massimi attesi per ogni prefisso di ID.</param>
    /// <param name="seenIds">L'insieme utilizzato per tenere traccia degli ID già letti ed evitare i duplicati.</param>
    /// <exception cref="ProjectSerializationException">Lanciata per entità nulle, tipi sprovvisti di prefisso o ID invalidi.</exception>
    private static void ValidateEntities<T>(IEnumerable<T> entities, IReadOnlyDictionary<string, int> counters,
                                            HashSet<string> seenIds) where T : Entity
    {
        foreach (T entity in entities)
        {
            if (entity is null)
                throw Invalid("Il progetto contiene un'entità nulla.");

            string prefix;

            try
            {
                prefix = IdGenerator.PrefixOf(entity.GetType());
            }
            catch (InvalidOperationException exception)
            {
                throw new ProjectSerializationException(
                    ProjectSerializationException.FailureKind.InvalidContent,
                    $"Tipo di entità senza prefisso ID: {entity.GetType().Name}.",
                    exception);

            }

            ValidateId(entity.Id, prefix, counters, seenIds);

        }
    }

    /// <summary>
    /// Tenta di estrarre il valore numerico da una stringa ID, verificando che inizi con il prefisso specificato
    /// e che non contenga formattazioni anomale (es. zeri iniziali non previsti).
    /// </summary>
    /// <param name="id">L'ID testuale completo (es. "char_12").</param>
    /// <param name="prefix">Il prefisso previsto per l'ID (es. "char_").</param>
    /// <param name="number">Il numero estratto in caso di esito positivo; altrimenti, 0.</param>
    /// <returns><c>true</c> se l'ID è valido ed è stato analizzato con successo; altrimenti <c>false</c>.</returns>
    private static bool TryParseIdNumber(string id, string prefix, out int number)
    {
        number = 0;

        if (string.IsNullOrWhiteSpace(id) || !id.StartsWith(prefix, StringComparison.Ordinal))
            return false;

        if (!int.TryParse(id.AsSpan(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out number))
            return false;

        return number > 0 &&
            string.Equals(id, prefix + number.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    /// <summary>
    /// Controlla la validità di un singolo ID, assicurandosi che sia analizzabile, che non sia un duplicato
    /// e che il suo valore numerico non ecceda quanto riportato nei contatori del progetto.
    /// </summary>
    /// <param name="id">L'ID da validare.</param>
    /// <param name="prefix">Il prefisso atteso per l'ID.</param>
    /// <param name="counters">Il dizionario dei contatori per il recupero del limite superiore accettabile.</param>
    /// <param name="seenIds">L'insieme degli ID già analizzati.</param>
    /// <exception cref="ProjectSerializationException">Lanciata in caso di ID malformato, duplicato o fuori limite.</exception>
    private static void ValidateId(string id, string prefix, IReadOnlyDictionary<string, int> counters, HashSet<string> seenIds)
    {
        if (!TryParseIdNumber(id,prefix,out int number))
            throw Invalid($"ID non valido: '{id}'.");
        if(!seenIds.Add(id))
            throw Invalid($"ID duplicato: '{id}'.");

        if (!counters.TryGetValue(prefix, out int lastNumber) || lastNumber < number)
            throw Invalid($"Il contatore '{prefix}' non copre l'ID '{id}'.");
    }
    /// <summary>
    /// Ispeziona la dizionario dei contatori di ID, verificando che non ci siano chiavi non valide 
    /// o valori fuori dall'intervallo consentito.
    /// </summary>
    /// <param name="counters">Il dizionario di contatori da validare.</param>
    /// <exception cref="ProjectSerializationException">Lanciata qualora un contatore presenti una chiave vuota o un valore illegale.</exception>
    private static void ValidateCounters(IReadOnlyDictionary<string, int> counters)
    {
        foreach (KeyValuePair<string, int> counter in counters)
        {
            if (string.IsNullOrWhiteSpace(counter.Key) ||
               counter.Value < 0 ||
               counter.Value == int.MaxValue)
            {
                throw Invalid($"Contatore ID non valido: '{counter.Key}'.");
            }
        }
    }

    /// <summary>
    /// Metodo di supporto per generare rapidamente una <see cref="ProjectSerializationException"/> con tipo InvalidContent.
    /// </summary>
    /// <param name="message">Il messaggio di errore descrittivo.</param>
    /// <returns>L'eccezione pronta per essere lanciata.</returns>
    private static ProjectSerializationException Invalid(string message) =>
        new(ProjectSerializationException.FailureKind.InvalidContent, message);
}
