using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.Domain.Assets;
using VnEditor.Domain.Characters;
using VnEditor.Domain.Scenes;

namespace VnEditor.Domain;
/// <summary>
/// Genera gli identificatori interni di tutte le entità del progetto,
/// tenendo un contatore separato per ciascun tipo.
/// Il contatore cresce sempre: eliminare un'entità non libera il suo numero,
/// altrimenti si creerebbero Id duplicati.
/// </summary>
/// 
public sealed class IdGenerator
{
    private static readonly Dictionary<Type, String> Prefixes = new()
    {
        [typeof(Character)] = "char_",
        [typeof(Background)] = "bg_",
        [typeof(MusicTrack)] = "mus_",
        [typeof(Expression)] = "espr_",
        [typeof(Scene)] = "scena_",
    };
    private readonly Dictionary<string, int> _counters = [];
    /// <summary>
    /// Stato dei contatori, indicizzato per prefisso. Serve alla
    /// serializzazione: al caricamento va ripristinato, altrimenti i nuovi
    /// Id collidono con quelli già in uso nel progetto.
    /// </summary>
    public IReadOnlyDictionary<string, int> Counters => _counters;
    /// <summary>Restituisce il prossimo identificatore per il tipo dato.</summary>
   
    public string Next<T>() where T: Entity
    {
        var prefix = PrefixOf(typeof(T));
        _counters.TryGetValue(prefix, out var last);
        this._counters[prefix] = last + 1;
        return $"{prefix}{last + 1}";
    }

    /// <summary>Ripristina i contatori dopo il caricamento di un progetto.</summary>
    public void Restore(IReadOnlyDictionary<string, int> counters)
    {
        ArgumentNullException.ThrowIfNull(counters);
        _counters.Clear();
        foreach (var (prefix,value) in counters)
        {
            _counters[prefix] = value;
        }

    }

    /// <summary>
    /// Prefisso associato a un tipo di entità. Pubblico perché i test
    /// verificano che ogni entità ne abbia uno.
    /// </summary>
    public static string PrefixOf(Type type)
    {
        if (!Prefixes.TryGetValue(type, out var prefix))
            throw new InvalidOperationException(
                $"Nessun prefisso definito per {type.Name}. Aggiungilo a IdGenerator.");
        return prefix;
    }
}