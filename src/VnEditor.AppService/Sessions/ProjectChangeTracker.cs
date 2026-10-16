using System;
using System.Collections.Generic;
using System.Text;

namespace VnEditor.AppService.Sessions;

/// <summary>
/// Tiene traccia delle revisioni del progetto attivo e del suo stato modificato.
/// Registra i salvataggi riusciti senza rendere pulite modifiche successive
/// e rifiuta i risultati appartenenti a un progetto precedentemente aperto.
/// Non conserva il contenuto del progetto e non implementa Undo/Redo.
/// </summary>
public sealed class ProjectChangeTracker
{
    private long _revision;
    private long _activationRevision;
    private long? _savedRevision;

    /// <summary>
    /// Indica se il progetto ha delle modifiche non ancora salvate.
    /// È vero se stiamo tracciando un progetto E la revisione attuale è diversa dall'ultima revisione salvata.
    /// </summary>
    public bool IsModified => IsTracking && _savedRevision != _revision;

    /// <summary>
    /// Indica se il tracker è attualmente attivo su un progetto.
    /// </summary>
    private bool IsTracking => _activationRevision > 0;

    /// <summary>
    /// Inizializza il tracciamento per un nuovo progetto o per un progetto appena caricato.
    /// </summary>
    /// <param name="isSaved">True se il progetto appena caricato è già sincronizzato con il disco (es. appena aperto da file).</param>
    public void BeginProject(bool isSaved)
    {
        long nextRevision = checked(this._revision + 1);

        this._revision = nextRevision;
        this._activationRevision = nextRevision;
        this._savedRevision = isSaved ? nextRevision : null;
    }

    /// <summary>
    /// Registra che è avvenuta una modifica. Incrementa il contatore, 
    /// rendendo di fatto IsModified = true (poiché _revision non combacerà più con _savedRevision).
    /// </summary>
    public void RecordChange()
    {
        EnsureProjectIsTracked();

        this._revision = checked(this._revision + 1);
    }
    /// <summary>
    /// "Scatta una foto" del numero di revisione attuale PRIMA di iniziare a salvare su disco.
    /// Questo numero verrà poi passato a <see cref="TryMarkSaved"/> quando il salvataggio sarà completato.
    /// </summary>
    /// <returns>Il numero di revisione attuale.</returns>
    public long CaptureSavePoint()
    {
        EnsureProjectIsTracked();

        return this._revision;
    }

    /// <summary>
    /// Chiamato quando un'operazione di salvataggio su disco finisce con successo.
    /// </summary>
    /// <param name="revision">Il numero di revisione che era stato catturato da <see cref="CaptureSavePoint"/> all'inizio del salvataggio.</param>
    /// <returns>True se la revisione è stata accettata, False se apparteneva a un progetto vecchio o non valido.</returns>
    public bool TryMarkSaved(long revision)
    {
        if (!IsSavePointForCurrentProject(revision))
            return false;

        this._savedRevision = revision;

        return true;
    }
    /// <summary>
    /// Verifica che la revisione passata appartenga effettivamente al progetto attualmente aperto.
    /// </summary>
    private bool IsSavePointForCurrentProject(long revision)
    {
        return IsTracking && revision >= this._activationRevision && revision <= this._revision;
    }

    /// <summary>
    /// Assicura che un progetto sia attualmente attivo prima di permettere operazioni di modifica.
    /// </summary>
    private void EnsureProjectIsTracked()
    {
        if (!IsTracking)
            throw new InvalidOperationException("No project is being tracked.");
    }

}
