using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.Domain;

namespace VnEditor.AppService.History;

/// <summary>
/// Coordina le modifiche al progetto corrente. Le implementazioni future
/// potranno sostituire il progetto durante il ripristino della cronologia.
/// </summary>
public interface IProjectHistory
{
    /// <summary>
    /// Progetto corrente. Chi modifica il progetto deve ottenere questa istanza
    /// dalla history, senza conservarne una precedente.
    /// </summary>
    Project CurrentProject { get; }

    /// <summary>
    /// Esegue una modifica del progetto corrente.
    /// </summary>
    /// <param name="description">Nome dell'azione mostrabile all'utente.</param>
    /// <param name="edit">Modifica da eseguire sul progetto corrente.</param>
    /// <param name="mergeKey">
    /// Chiave facoltativa per raggruppare modifiche consecutive in una futura
    /// implementazione della cronologia.
    /// </param>
    void Edit(string description, Action<Project> edit, string? mergeKey = null);
}
