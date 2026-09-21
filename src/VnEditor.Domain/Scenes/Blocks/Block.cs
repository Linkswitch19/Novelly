
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

    public abstract string Summary(Project project);

    /// <summary>
    /// Affida il blocco a chi sa cosa farne. Ogni sottoclasse chiama
    /// l'overload giusto, e il visitor riceve il tipo concreto senza
    /// bisogno di controlli.
    /// </summary>
    public abstract void Accept(IBlockVisitor visitor);
}
