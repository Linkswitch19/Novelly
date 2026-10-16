using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VnEditor.AppService.Interaction;

namespace VnEditor.UI.ViewModels;

/// <summary>
/// Gestisce lo stato e la logica di presentazione del dialogo modale di creazione di un nuovo progetto.
/// Permette l'inserimento del nome del progetto e della directory di destinazione, supportando
/// la selezione guidata della cartella tramite finestra di dialogo di sistema.
/// Notifica l'esito della finestra tramite l'evento <see cref="CloseRequested"/>.
/// </summary>
public partial class NewProjectDialogViewModel : ViewModelBase
{
    private readonly IProjectDialogs _dialogs;

    /// <summary>
    /// Ottiene o imposta il nome del nuovo progetto da creare.
    /// Ogni modifica notifica a <see cref="CreateCommand"/> di verificare se può essere eseguito.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateCommand))] // dal metodo sotto
    private string _projectName = string.Empty;

    /// <summary>
    /// Ottiene o imposta il percorso sul disco in cui allocare la cartella del progetto.
    /// Ogni modifica notifica a <see cref="CreateCommand"/> di verificare se può essere eseguito.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateCommand))]
    private string _projectFolder = string.Empty;

    /// <summary>
    /// Ottiene o imposta un valore che indica se è in corso un'operazione asincrona (ad esempio l'apertura del selettore cartella).
    /// Ogni modifica notifica a tutti i comandi (<see cref="CreateCommand"/>, <see cref="BrowseCommand"/>, <see cref="CancelCommand"/>)
    /// di ricalcolare il proprio stato di abilitazione.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateCommand))]
    [NotifyCanExecuteChangedFor(nameof(BrowseCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _isBusy;

    /// <summary>
    /// Inizializza una nuova istanza di <see cref="NewProjectDialogViewModel"/>.
    /// </summary>
    /// <param name="dialogs">Il servizio per l'apertura dei dialoghi di sistema e modali dell'applicazione.</param>
    /// <exception cref="ArgumentNullException">Lanciata se <paramref name="dialogs"/> è nullo.</exception>
    public NewProjectDialogViewModel(IProjectDialogs dialogs)
    {
        ArgumentNullException.ThrowIfNull(dialogs);

        this._dialogs = dialogs;
    }

    /// <summary>
    /// Evento scatenato per richiedere la chiusura della finestra di dialogo.
    /// Trasmette l'istanza di <see cref="NewProjectRequest"/> se la creazione è stata confermata,
    /// oppure <c>null</c> se l'utente ha annullato o chiuso il dialogo.
    /// </summary>
    public event Action<NewProjectRequest?>? CloseRequested;

    /// <summary>
    /// Comando associato al pulsante di conferma.
    /// Valida i campi, crea l'oggetto richiesta e richiede la chiusura restituendo i dati.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCreate))]
    private void Create()
    {
        if (!CanCreate())
            return;

        NewProjectRequest request = new(ProjectName.Trim(), ProjectFolder.Trim());

        CloseRequested?.Invoke(request);
    }

    /// <summary>
    /// Comando associato al pulsante di annullamento.
    /// Richiede la chiusura del dialogo senza produrre una richiesta di creazione.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanInteract))]
    private void Cancel()
    {
        if (!CanInteract())
            return;

        CloseRequested?.Invoke(null);
    }

    /// <summary>
    /// Comando asincrono associato al pulsante di navigazione ("Sfoglia").
    /// Apre la finestra di dialogo del file system per selezionare la cartella di destinazione.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanInteract))]
    private async Task BrowseAsync()
    {
        if (!CanInteract())
            return;

        IsBusy = true;

        try
        {
            string? suggestedFolder = string.IsNullOrWhiteSpace(ProjectFolder)
                ? null
                : ProjectFolder;

            string? selectedFolder = await _dialogs.ChooseProjectFolderAsync(
                "Choose project folder", suggestedFolder);

            if (selectedFolder is not null)
                ProjectFolder = selectedFolder;
        }
        catch (Exception exception) when (IsFolderSelectionError(exception))
        {
            await _dialogs.ShowErrorAsync(
                "Unable to choose a folder",
                "The folder could not be selected. Please try again.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Determina se il comando <see cref="CreateCommand"/> può essere eseguito.
    /// </summary>
    /// <returns>
    /// <c>true</c> se l'interfaccia non è occupata e sia il nome che la cartella contengono testo valido;
    /// altrimenti <c>false</c>.
    /// </returns>
    private bool CanCreate()
    {
        return CanInteract()
            && !string.IsNullOrWhiteSpace(ProjectName)
            && !string.IsNullOrWhiteSpace(ProjectFolder);
    }

    /// <summary>
    /// Determina se l'utente può interagire con i comandi della schermata.
    /// </summary>
    /// <returns><c>true</c> se non vi sono operazioni asincrone in corso (<c>!IsBusy</c>); altrimenti <c>false</c>.</returns>
    private bool CanInteract()
    {
        return !IsBusy;
    }

    /// <summary>
    /// Verifica se un'eccezione riscontrata durante la selezione della cartella sia dovuta a errori noti di accesso o I/O.
    /// </summary>
    /// <param name="exception">L'eccezione intercettata.</param>
    /// <returns><c>true</c> se l'errore rientra nelle casistiche tipiche di I/O gestibili da UI; altrimenti <c>false</c>.</returns>
    private static bool IsFolderSelectionError(Exception exception)
    {
        return exception is IOException or UnauthorizedAccessException or NotSupportedException;
    }
}