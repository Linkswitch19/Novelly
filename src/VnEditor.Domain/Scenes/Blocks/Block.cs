
using VnEditor.Domain.Generation;

namespace VnEditor.Domain.Scenes.Blocks;

/// <summary>
/// Un evento nella timeline di una scena: mostrare uno sfondo, far parlare
/// un personaggio, far partire la musica.
/// Ogni tipo di evento è una sottoclasse in un file suo.
/// </summary>

public abstract class Block
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    /// <summary>Nome mostrato nel menu "Aggiungi evento".</summary>
    public abstract string Label { get; }
    /// <summary>
    /// Se true, dopo questo evento l'esecuzione non continua con l'evento
    /// successivo: il blocco decide dove andare (scelta, salto, fine).
    /// Serve al generatore per sapere se aggiungere un salto finale,
    /// senza dover conoscere i tipi concreti.
    /// </summary>
    public virtual bool EndsScene => false;

    /// <summary>Scrive le righe Ren'Py corrispondenti a questo evento.</summary>
    public abstract void Generate(IScriptWriter writer, Project project);
}
