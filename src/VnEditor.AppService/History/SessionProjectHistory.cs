using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.AppService.Sessions;
using VnEditor.Domain;

namespace VnEditor.AppService.History;

/// <summary>
/// Fornisce un riferimento durevole e stabile a <see cref="IProjectHistory"/> che delega
/// dinamicamente ogni operazione alla cronologia del progetto correntemente aperto nella <see cref="ProjectSession"/>.
/// Evita che i ViewModel o i servizi applicativi debbano ricablare i riferimenti alla cronologia 
/// a ogni cambio o caricamento di un nuovo file di progetto.
/// </summary>
public sealed class SessionProjectHistory : IProjectHistory, IDisposable
{
    private readonly ProjectSession _session;
    private bool _isDisposed;

    /// <summary>
    /// Inizializza una nuova istanza associata alla sessione attiva e si registra 
    /// per ricevere le notifiche di variazione del progetto.
    /// </summary>
    /// <param name="session">La sessione applicativa che controlla il ciclo di vita del progetto corrente.</param>
    /// <exception cref="ArgumentNullException">Lanciata se <paramref name="session"/> è nulla.</exception>
    public SessionProjectHistory(ProjectSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        this._session = session;
        this._session.ProjectChanged += OnProjectChanged;
    }

    /// <summary>
    /// Evento scatenato ogni volta che il progetto attivo viene modificato oppure sostituito con un altro file.
    /// </summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Ottiene l'istanza del progetto attualmente caricato e modificabile.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Lanciata se l'istanza è stata dismessa tramite <see cref="Dispose"/>.</exception>
    /// <exception cref="InvalidOperationException">Lanciata tramite la sessione se al momento non è aperto alcun progetto.</exception>
    public Project CurrentProject
    {
        get
        {
            // se dispoded errore
            ObjectDisposedException.ThrowIf(this._isDisposed, this);

            return _session.CurrentHistory.CurrentProject;
        }
    }

    /// <summary>
    /// Inoltra la richiesta di modifica alla cronologia del progetto attivo.
    /// </summary>
    /// <param name="description">Descrizione dell'operazione per scopi diagnostici o di visualizzazione Undo.</param>
    /// <param name="edit">L'azione di mutazione da eseguire sullo stato del modello di dominio.</param>
    /// <param name="mergeKey">Chiave opzionale per raggruppare modifiche adiacenti (es. digitazione testo).</param>
    /// <exception cref="ObjectDisposedException">Lanciata se l'istanza è già stata dismessa.</exception>
    /// <exception cref="InvalidOperationException">Lanciata se nessun progetto è attualmente aperto.</exception>
    public void Edit(string description, Action<Project> edit, string? mergeKey = null)
    {
        ObjectDisposedException.ThrowIf(this._isDisposed, this);

        this._session.CurrentHistory.Edit(description, edit, mergeKey);
    }

    /// <summary>
    /// Rilascia le risorse disiscrivendosi dagli eventi della sessione e azzerando i gestori di eventi registrati.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;
        this._isDisposed = true;
        this._session.ProjectChanged -= OnProjectChanged;

        Changed = null;
    }

    /// <summary>
    /// Gestore che intercetta il cambio del progetto nella sessione e rimbalza l'evento <see cref="Changed"/> all'esterno.
    /// </summary>
    private void OnProjectChanged(object? sender, EventArgs eventArgs)
    {
        if (this._isDisposed)
            return;

        Changed?.Invoke(this, EventArgs.Empty);
    }
}
