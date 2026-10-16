using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.Domain;

namespace VnEditor.AppService.History;

/// <summary>
/// Coordina le modifiche al progetto corrente e ne notifica i cambiamenti.
/// Le implementazioni future potranno sostituire il progetto durante
/// il ripristino della cronologia.
/// </summary>
public interface IProjectHistory
{
    /// <summary>
    /// Progetto corrente. Chi modifica il progetto deve ottenere questa istanza
    /// dalla history, senza conservarne una precedente.
    /// </summary>
    Project CurrentProject { get; }

    /// <summary>
    /// Segnala che il progetto corrente è stato sostituito oppure che
    /// il suo contenuto potrebbe essere cambiato.
    /// Chi riceve la notifica deve rileggere <see cref="CurrentProject"/>.
    /// La notifica può essere conservativa: non garantisce che il contenuto
    /// sia effettivamente diverso da quello precedente.
    /// </summary>
    event EventHandler? Changed;

    /// <summary>
    /// Esegue una modifica del progetto corrente.
    /// Quando il contenuto potrebbe essere cambiato, notifica
    /// gli osservatori tramite <see cref="Changed"/>.
    /// Anche una modifica fallita può richiedere la notifica,
    /// se il delegato ha già modificato parzialmente il progetto.
    /// La validazione degli argomenti deve precedere l'esecuzione
    /// del delegato e non deve produrre notifiche di modifica.
    /// </summary>
    /// <param name="description">Nome dell'azione mostrabile all'utente.</param>
    /// <param name="edit">Modifica da eseguire sul progetto corrente.</param>
    /// <param name="mergeKey">
    /// Chiave facoltativa per raggruppare modifiche consecutive in una futura
    /// implementazione della cronologia.
    /// </param>
    void Edit(string description, Action<Project> edit, string? mergeKey = null);
}
