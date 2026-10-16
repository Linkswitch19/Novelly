using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.AppService.History;
using VnEditor.Domain;

namespace VnEditor.Infrastructure.History;

/// <summary>
/// Esegue direttamente le modifiche sul progetto corrente e notifica
/// gli osservatori, senza conservare la cronologia.
/// Non implementa Undo/Redo né il ripristino delle modifiche fallite.
/// </summary>

public sealed class PassThroughProjectHistory : IProjectHistory
{
    /// <summary>
    /// Inizializza una nuova istanza associandola al progetto da manipolare.
    /// </summary>
    /// <param name="project">Il progetto su cui verranno applicate le modifiche.</param>
    /// <exception cref="ArgumentNullException">Lanciata se il progetto fornito è nullo.</exception>
    public PassThroughProjectHistory(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        CurrentProject = project;
    }

    public Project CurrentProject { get; }

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <summary>
    /// Valida gli argomenti ed esegue direttamente la modifica.
    /// Dopo l'esecuzione notifica gli osservatori anche se il delegato
    /// fallisce, perché il progetto potrebbe essere stato modificato
    /// parzialmente. La descrizione non viene conservata e la chiave
    /// di raggruppamento viene ignorata.
    /// </summary>
    /// <param name="description">Nome dell'azione mostrabile all'utente.</param>
    /// <param name="edit">Modifica da eseguire sul progetto corrente.</param>
    /// <param name="mergeKey">Chiave di raggruppamento, ignorata.</param>
    /// <exception cref="ArgumentException">
    /// La descrizione è nulla, vuota o composta soltanto da spazi.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Il delegato di modifica è nullo.
    /// </exception>
    public void Edit(string description, Action<Project> edit, string? mergeKey = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(edit);
        try
        {
            edit(CurrentProject);
        }
        finally
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
        
    }
}
