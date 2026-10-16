using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.AppService.History;
using VnEditor.Domain;

namespace VnEditor.AppService.Sessions;

/// <summary>
/// Gestisce il progetto attivo, la sua cartella e il collegamento alla history.
/// Delega il controllo dello stato modificato a ProjectChangeTracker.
/// Consente la sostituzione del progetto soltanto se la revisione attesa
/// coincide ancora con quella corrente.
/// Non crea la history e non esegue operazioni di persistenza.
/// </summary>
public sealed class ProjectSession : IDisposable
{
    private readonly ProjectChangeTracker _changeTracker = new();

    private IProjectHistory? _history;
    private string? _projectFolder;
    private bool _isDisposed;

    /// <summary>
    /// Evento lanciato quando i dati del progetto cambiano (es. aggiunta una scena).
    /// </summary>
    public event EventHandler? ProjectChanged;
    /// <summary>
    /// Evento lanciato quando cambia lo "stato" della sessione 
    /// (es. il progetto viene salvato, oppure viene aperto un progetto diverso).
    /// </summary>

    public event EventHandler? StateChanged;

    /// <summary>
    /// Indica se c'è attualmente un progetto caricato in memoria.
    /// </summary>
    public bool HasProject => _history is not null;

    /// <summary>
    /// Restituisce il progetto attualmente aperto, se presente.
    /// </summary>
    public Project? Project => _history?.CurrentProject;

    /// <summary>
    /// Il percorso assoluto della cartella in cui è salvato il progetto.
    /// </summary>
    public string? ProjectFolder => _projectFolder;

    /// <summary>
    /// Indica se il progetto ha delle modifiche non ancora salvate su disco.
    /// </summary>
    public bool IsModified => _changeTracker.IsModified;

    /// <summary>
    /// Proprietà a uso interno (internal) per ottenere la cronologia. 
    /// Se la sessione è stata "chiusa" (Disposed) o non c'è un progetto, lancia un'eccezione.
    /// </summary>
    internal IProjectHistory CurrentHistory
    {
        get
        {
            ObjectDisposedException.ThrowIf(this._isDisposed, this);

            return _history
                ?? throw new InvalidOperationException("No project is open.");
        }
    }
    /// <summary>
    /// Carica un nuovo progetto in questa sessione.
    /// </summary>
    public void SetProject(IProjectHistory history, string projectFolder, bool isSaved)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        ArgumentNullException.ThrowIfNull(history);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectFolder);

        ArgumentNullException.ThrowIfNull(history.CurrentProject);
        string fullFolder = Path.GetFullPath(projectFolder);

        _changeTracker.BeginProject(isSaved);

        if (_history is not null)
            _history.Changed -= OnHistoryChanged;

        _history = history;
        _projectFolder = fullFolder;

        _history.Changed += OnHistoryChanged;

        NotifyProjectChanged();
    }
    /// <summary>
    /// Restituisce la revisione corrente, oppure null se non è aperto alcun progetto.
    /// Le modifiche e l'attivazione di un progetto fanno avanzare la revisione.
    /// </summary>
    public long? CaptureCurrentRevision()
    {
        ObjectDisposedException.ThrowIf(this._isDisposed, this);

        return HasProject ? this._changeTracker.CaptureSavePoint() : null;
    }
    /// <summary>
    /// Verifica se la sessione si trova ancora nello stato identificato
    /// dalla revisione attesa.
    /// </summary>
    public bool IsCurrentRevision(long? expectedRevision)
    {
        return CaptureCurrentRevision() == expectedRevision;
    }
    /// <summary>
    /// Attiva il progetto associato alla history fornita soltanto se
    /// la sessione corrisponde ancora alla revisione attesa.
    /// Se la revisione non coincide, lascia invariata la sessione.
    /// </summary>
    public bool TrySetProject(IProjectHistory history, string projectFolder, bool isSaved, long? expectedRevision)
    {
        ObjectDisposedException.ThrowIf(this._isDisposed, this);
        ArgumentNullException.ThrowIfNull(history);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectFolder);

        if (!IsCurrentRevision(expectedRevision))
            return false;

        SetProject(history, projectFolder, isSaved);

        return true;
    }

    /// <summary>
    /// Prepara il terreno per il salvataggio chiamando il tracker.
    /// </summary>
    public long CaptureSavePoint()
    {
        ObjectDisposedException.ThrowIf(this._isDisposed, this);

        return this._changeTracker.CaptureSavePoint();
    }

    /// <summary>
    /// Segna il progetto come salvato se il salvataggio è andato a buon fine.
    /// </summary>
    public bool TryMarkSaved(long revision, string projectFolder)
    {
        ObjectDisposedException.ThrowIf(this._isDisposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectFolder);

        string fullFolder = Path.GetFullPath(projectFolder);

        if (!this._changeTracker.TryMarkSaved(revision))
            return false;

        this._projectFolder = fullFolder;

        StateChanged?.Invoke(this, EventArgs.Empty);

        return true;
    }

    /// <summary>
    /// Metodo fondamentale dell'interfaccia IDisposable.
    /// Serve a "spegnere" in modo pulito questa sessione quando il programma viene chiuso.
    /// </summary>
    public void Dispose()
    {
        if (this._isDisposed)
            return;

        this._isDisposed = true;

        if (this._history is not null)
            this._history.Changed -= OnHistoryChanged;

        ProjectChanged = null;
        StateChanged = null;
    }

    /// <summary>
    /// Metodo chiamato in automatico ogni volta che l'utente fa una modifica (Undo/Redo o normale edit).
    /// </summary>
    private void OnHistoryChanged(object? sender, EventArgs eventArgs)
    {
        if (!IsCurrentHistory(sender))
            return;

        this._changeTracker.RecordChange();

        NotifyProjectChanged();
    }

    /// <summary>
    /// "Spara" entrambi gli eventi per avvisare chiunque sia in ascolto (es. la grafica).
    /// </summary>
    private void NotifyProjectChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
        ProjectChanged?.Invoke(this, EventArgs.Empty);
    }
    /// <summary>
    /// Controlla se chi ci ha inviato una notifica è effettivamente il progetto che stiamo gestendo ora.
    /// </summary>
    private bool IsCurrentHistory(object? sender)
    {
        return !_isDisposed && ReferenceEquals(sender, _history);
    }

}
