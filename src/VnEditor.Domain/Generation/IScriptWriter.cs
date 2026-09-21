


namespace VnEditor.Domain.Generation;


/// <summary>
/// Scrive uno script Ren'Py tenendo conto dell'indentazione.
/// Ren'Py è sensibile all'indentazione come Python: gestirla qui evita
/// che ogni blocco debba contare gli spazi per conto proprio.
/// </summary>
public interface IScriptWriter
{
    /// <summary>Scrive una riga al livello di indentazione corrente.</summary>
    void Line(string text);
    /// <summary>scrive una riga vuota, senza identazione</summary>
    void BlankLine();
    ///<summary>
    /// Scrive l'intestazione e rientra di un livello fino alla chiusura.
    /// Da usare con <c>using</c>.
    ///</summary>
    IDisposable Block(string header);
}