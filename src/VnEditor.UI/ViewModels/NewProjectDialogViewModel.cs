using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VnEditor.AppService.Interaction;

namespace VnEditor.UI.ViewModels;

/// <summary>
/// Gestisce i campi e i comandi del dialogo New project.
/// Raccoglie il nome del progetto e la cartella genitore scelta dall'utente,
/// supportando la selezione della posizione tramite dialogo di sistema.
/// Restituisce i dati tramite CloseRequested senza creare cartelle.
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
    /// Ottiene o imposta la cartella genitore nella quale creare il progetto.
    /// La sottocartella con il nome del progetto viene determinata dal caso d'uso New.
    /// Ogni modifica aggiorna l'abilitazione di CreateCommand.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateCommand))]
    private string _parentFolder = string.Empty;

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

        NewProjectRequest request = new(ProjectName.Trim(), ParentFolder.Trim());

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
    /// Apre il selettore di sistema per scegliere la cartella genitore
    /// nella quale verrà creata la sottocartella del progetto.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanInteract))]
    private async Task BrowseAsync()
    {
        if (!CanInteract())
            return;

        IsBusy = true;

        try
        {
            string? suggestedFolder = string.IsNullOrWhiteSpace(ParentFolder)
                 ? null
                 : ParentFolder;

            string? selectedFolder = await _dialogs.ChooseProjectFolderAsync(
                "Choose project location", suggestedFolder);

            if (selectedFolder is not null)
                ParentFolder = selectedFolder;
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
            && !string.IsNullOrWhiteSpace(ParentFolder);
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