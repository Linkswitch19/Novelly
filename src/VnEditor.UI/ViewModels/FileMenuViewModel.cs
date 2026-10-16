using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using VnEditor.AppService.Projects.Actions;
using VnEditor.AppService.Sessions;

namespace VnEditor.UI.ViewModels;

/// <summary>
/// Espone i comandi del menu File e ne gestisce l'abilitazione.
/// Delega i casi d'uso alle azioni ricevute e impedisce operazioni
/// contemporanee, includendo la richiesta di chiusura della finestra.
/// </summary>
public sealed partial class FileMenuViewModel : ViewModelBase, IDisposable
{
    private readonly ProjectSession _session;
    private readonly IProjectFileAction _newAction;
    private readonly IProjectFileAction _openAction;
    private readonly IProjectFileAction _saveAction;
    private readonly IProjectFileAction _saveAsAction;
    private readonly IProjectFileAction _closeAction;

    /// <summary>
    /// Campo di supporto per la proprietà <c>IsBusy</c>, che indica se un'operazione sul file di progetto è attualmente in corso.
    /// </summary>
    private bool _isDisposed;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NewCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveAsCommand))]
    private bool _isBusy;

    /// <summary>
    /// Inizializza una nuova istanza della classe <see cref="FileMenuViewModel"/>.
    /// </summary>
    /// <param name="session">La sessione di progetto da osservare per determinare la presenza di un progetto attivo.</param>
    /// <param name="newAction">L'azione da eseguire per la creazione di un nuovo progetto.</param>
    /// <param name="openAction">L'azione da eseguire per l'apertura di un progetto esistente.</param>
    /// <param name="saveAction">L'azione da eseguire per il salvataggio del progetto corrente.</param>
    /// <param name="saveAsAction">L'azione da eseguire per il salvataggio con nome del progetto corrente.</param>
    /// <param name="closeAction">L'azione da eseguire per verificare se è possibile chiudere il progetto o l'applicazione.</param>
    /// <exception cref="ArgumentNullException">
    /// Sollevata se <paramref name="session"/>, <paramref name="newAction"/>, <paramref name="openAction"/>,
    /// <paramref name="saveAction"/>, <paramref name="saveAsAction"/> o <paramref name="closeAction"/> sono <see langword="null"/>.
    /// </exception>
    public FileMenuViewModel(ProjectSession session,
       IProjectFileAction newAction, IProjectFileAction openAction,
       IProjectFileAction saveAction, IProjectFileAction saveAsAction,
       IProjectFileAction closeAction)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(newAction);
        ArgumentNullException.ThrowIfNull(openAction);
        ArgumentNullException.ThrowIfNull(saveAction);
        ArgumentNullException.ThrowIfNull(saveAsAction);
        ArgumentNullException.ThrowIfNull(closeAction);

        this._session = session;
        this._newAction = newAction;
        this._openAction = openAction;
        this._saveAction = saveAction;
        this._saveAsAction = saveAsAction;
        this._closeAction = closeAction;

        this._session.StateChanged += OnSessionStateChanged;
    }
    /// <summary>
    /// Avvia in modo asincrono il flusso di creazione di un nuovo progetto.
    /// </summary>
    /// <returns>Un'attività asincrona che rappresenta l'operazione.</returns>
    [RelayCommand(CanExecute = nameof(CanStart))]
    private Task NewAsync()
    {
        return RunAsync(this._newAction);
    }

    /// <summary>
    /// Avvia in modo asincrono il flusso di apertura di un progetto esistente.
    /// </summary>
    /// <returns>Un'attività asincrona che rappresenta l'operazione.</returns>
    [RelayCommand(CanExecute = nameof(CanStart))]
    private Task OpenAsync()
    {
        return RunAsync(this._openAction);
    }

    /// <summary>
    /// Avvia in modo asincrono il salvataggio del progetto corrente, se consentito dallo stato attuale.
    /// </summary>
    /// <returns>Un'attività asincrona che rappresenta l'operazione.</returns>
    [RelayCommand(CanExecute = nameof(CanSave))]
    private Task SaveAsync()
    {
        return CanSave()
            ? RunAsync(this._saveAction)
            : Task.CompletedTask;
    }

    /// <summary>
    /// Avvia in modo asincrono il salvataggio con nome del progetto corrente, se consentito dallo stato attuale.
    /// </summary>
    /// <returns>Un'attività asincrona che rappresenta l'operazione.</returns>
    [RelayCommand(CanExecute = nameof(CanSave))]
    private Task SaveAsAsync()
    {
        return CanSave()
            ? RunAsync(this._saveAsAction)
            : Task.CompletedTask;
    }

    /// <summary>
    /// Richiede in modo asincrono la conferma per chiudere il progetto corrente o la finestra dell'applicazione.
    /// </summary>
    /// <returns>
    /// Un'attività asincrona il cui risultato è <see langword="true"/> se la chiusura può procedere;
    /// <see langword="false"/> se un'altra operazione è già in corso, se il ViewModel è stato rilasciato
    /// o se l'utente ha annullato la chiusura.
    /// </returns>
    public Task<bool> RequestCloseAsync()
    {
        return RunAsync(this._closeAction);
    }

    /// <summary>
    /// Rilascia le risorse utilizzate dal ViewModel, annulla la sottoscrizione agli eventi di <see cref="ProjectSession"/>
    /// e aggiorna lo stato di eseguibilità dei comandi.
    /// </summary>
    public void Dispose()
    {
        if (this._isDisposed)
            return;

        this._isDisposed = true;
        this._session.StateChanged -= OnSessionStateChanged;

        NotifyCommands();
    }

    /// <summary>
    /// Esegue un'azione sul file di progetto gestendo lo stato <see cref="IsBusy"/> e prevenendo esecuzioni concorrenti.
    /// </summary>
    /// <param name="action">L'azione sul file di progetto da eseguire.</param>
    /// <returns>
    /// <see langword="true"/> se l'azione è stata avviata e completata con successo;
    /// <see langword="false"/> se non è stato possibile avviare l'operazione o se l'azione ha restituito esito negativo.
    /// </returns>
    private async Task<bool> RunAsync(IProjectFileAction action)
    {
        if (!CanStart())
            return false;

        IsBusy = true;

        try
        {
            return await action.ExecuteAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Determina se è possibile avviare una nuova operazione sul file di progetto.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> se l'istanza non è stata rilasciata e nessuna operazione è attualmente in corso;
    /// altrimenti, <see langword="false"/>.
    /// </returns>
    private bool CanStart()
    {
        return !this._isDisposed && !IsBusy;
    }

    /// <summary>
    /// Determina se è possibile eseguire un'operazione di salvataggio sul progetto corrente.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> se è possibile avviare un'operazione (<see cref="CanStart"/>) ed è presente un progetto attivo nella sessione;
    /// altrimenti, <see langword="false"/>.
    /// </returns>
    private bool CanSave()
    {
        return CanStart() && this._session.HasProject;
    }

    /// <summary>
    /// Gestisce la variazione di stato della sessione aggiornando l'abilitazione dei comandi di salvataggio.
    /// </summary>
    /// <param name="sender">L'origine dell'evento.</param>
    /// <param name="eventArgs">I dati associati all'evento.</param>
    private void OnSessionStateChanged(object? sender, EventArgs eventArgs)
    {
        SaveCommand.NotifyCanExecuteChanged();
        SaveAsCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Notifica a tutti i comandi esposti di rivalutare il proprio stato di eseguibilità.
    /// </summary>
    private void NotifyCommands()
    {
        NewCommand.NotifyCanExecuteChanged();
        OpenCommand.NotifyCanExecuteChanged();
        SaveCommand.NotifyCanExecuteChanged();
        SaveAsCommand.NotifyCanExecuteChanged();
    }

}