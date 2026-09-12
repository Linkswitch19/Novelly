namespace VnEditor.Domain;

/// <summary>
/// Base di tutto ciò che l'utente crea e può richiamare per nome:
/// personaggi, sfondi, musiche, espressioni, scene.
/// Ogni entità ha un identificatore interno stabile e un nome modificabile.
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// Identificatore interno sicuro, es. "char1". Lo genera il contenitore
    /// tramite <see cref="IdGenerator"/> e non cambia mai, nemmeno quando
    /// l'utente rinomina l'entità: i riferimenti nei blocchi puntano a questo.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Nome scelto dall'utente. Può contenere qualsiasi carattere e può
    /// cambiare in qualsiasi momento.
    /// </summary>
    public required string Name { get; set; }
}