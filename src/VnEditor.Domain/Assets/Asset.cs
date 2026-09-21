namespace VnEditor.Domain.Assets;

/// <summary>
/// Entità che corrisponde a un file importato dall'utente e copiato
/// nel progetto Ren'Py.
/// </summary>
public abstract class Asset:Entity
{
   
    /// <summary>
    /// Nome del file dentro game/images o game/audio. È già il nome interno
    /// sicuro, non quello del file originale importato.
    /// </summary>
    public required string FileName { get; set; }
}