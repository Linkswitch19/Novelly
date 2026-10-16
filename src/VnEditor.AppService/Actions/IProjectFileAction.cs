using System;
using System.Collections.Generic;
using System.Text;

namespace VnEditor.AppService.Projects.Actions;
/// <summary>
/// Definisce l'esecuzione di un caso d'uso File.
/// Restituisce true quando l'operazione è completata e false
/// quando viene annullata o non può proseguire.
/// Le eccezioni vengono propagate al chiamante.
/// </summary>
public interface IProjectFileAction
{
    /// <summary>
    /// Esegue in modo asincrono il comando sul file di progetto.
    /// </summary>
    /// <returns>
    /// Un task che rappresenta l'operazione asincrona. Il risultato è <see langword="true"/> se il comando
    /// è stato completato con successo; <see langword="false"/> se l'operazione è stata annullata dall'utente
    /// o se le precondizioni per proseguire non sono soddisfatte.
    /// </returns>
    Task<bool> ExecuteAsync();
}
