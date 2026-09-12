using System.Text;

namespace VnEditor.Infrastructure.Generation;
/// <summary>
/// Converte il testo scritto dall'utente in testo sicuro per Ren'Py.
/// L'utente non deve conoscere la sintassi del motore: può scrivere
/// qualsiasi carattere e questa classe lo rende innocuo.
/// </summary>
public static class RenpyEscape
{

    /// <summary>
    /// Prepara il testo di un dialogo, di una narrazione o di una scelta.
    /// Il risultato va messo tra virgolette doppie nello script.
    /// </summary>
    
    public static string Text(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        StringBuilder result= new (text.Length+8);
        foreach (Char c in text)
        {
            switch(c)
            {
                // Il backslash va per primo: è lui stesso un carattere di escape
                case '\\': result.Append("\\\\"); break;
                // Chiuderebbe la stringa
                case '"': result.Append("\\\""); break;
                // Ren'Py le usa per i tag di stile, es. {b}grassetto{/b}
                case '{': result.Append("{{"); break;
                // Ren'Py le usa per interpolare le variabili, es. [nome]
                case '[': result.Append("[["); break;
                // I ritorni a capo romperebbero la riga dello script
                case '\n':
                case '\r': result.Append(' '); break;
                default: result.Append(c); break;
            }
        }
        return result.ToString();
    }

}