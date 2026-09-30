using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.AppService.History;
using VnEditor.Domain;

namespace VnEditor.Infrastructure.History;

/// <summary>
/// Implementazione di <see cref="IProjectHistory"/> che esegue le modifiche 
/// sul progetto corrente in modo diretto, senza conservarne la cronologia (nessun supporto per Undo/Redo).
/// Risulta utile in contesti di test, in fase di caricamento iniziale o quando il tracciamento non è richiesto.
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

    /// <summary>
    /// Esegue l'azione di modifica specificata direttamente sul progetto corrente.
    /// La descrizione viene validata ma non conservata.
    /// La chiave di raggruppamento viene accettata e ignorata.
    /// </summary>
    /// <param name="description">Breve descrizione dell'operazione (es. "Cambio nome scena").</param>
    /// <param name="edit">L'azione che applica concretamente la modifica allo stato del progetto.</param>
    /// <param name="mergeKey">Chiave opzionale per raggruppare modifiche simili (ignorata in questa implementazione).</param>
    /// <exception cref="ArgumentException">Lanciata se la descrizione è nulla o composta da soli spazi.</exception>
    /// <exception cref="ArgumentNullException">Lanciata se l'azione di modifica (<paramref name="edit"/>) è nulla.</exception>
    public void Edit(string description, Action<Project> edit, string? mergeKey = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(edit);

        edit(CurrentProject);
    }
}
