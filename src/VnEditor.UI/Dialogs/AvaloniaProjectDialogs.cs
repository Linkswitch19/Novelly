using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using VnEditor.AppService.Interaction;
using VnEditor.UI.ViewModels;
using VnEditor.UI.Views.Dialogs;

namespace VnEditor.UI.Dialogs;

/// <summary>
/// Implementa le interazioni dei progetti attraverso i dialoghi Avalonia
/// e il selettore di cartelle di sistema.
/// Mantiene il proprietario corretto durante l'apertura di dialoghi annidati
/// e converte le selezioni in percorsi locali.
/// </summary>
public sealed class AvaloniaProjectDialogs : IProjectDialogs
{
    /// <summary>
    /// Finestra attualmente usata come proprietaria dei nuovi dialoghi.
    /// Viene sostituita temporaneamente dal dialogo aperto, così che un
    /// eventuale dialogo annidato abbia come proprietario quello corretto.
    /// </summary>
    private Window _dialogOwner;

    /// <summary>
    /// Inizializza una nuova istanza di <see cref="AvaloniaProjectDialogs"/>.
    /// </summary>
    /// <param name="owner">
    /// Finestra principale che farà da proprietaria ai dialoghi di primo livello
    /// e fornirà il <see cref="IStorageProvider"/> per la selezione delle cartelle.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="owner"/> è <c>null</c>.
    /// </exception>
    public AvaloniaProjectDialogs(Window owner)
    {
        ArgumentNullException.ThrowIfNull(owner);

        this._dialogOwner = owner;
    }

    /// <summary>
    /// Mostra il dialogo modale per la creazione di un nuovo progetto.
    /// </summary>
    /// <returns>
    /// Un task che restituisce i dati inseriti dall'utente come
    /// <see cref="NewProjectRequest"/>, oppure <c>null</c> se il dialogo
    /// viene annullato o chiuso senza conferma.
    /// </returns>
    public Task<NewProjectRequest?> ShowNewProjectAsync()
    {
        NewProjectDialogViewModel viewModel = new(this);
        NewProjectDialogView dialog = new(viewModel);

        return ShowDialogAsync<NewProjectRequest?>(dialog);
    }

    /// <summary>
    /// Apre il selettore di cartelle di sistema per scegliere la cartella di un progetto.
    /// </summary>
    /// <param name="title">Titolo mostrato nel selettore di cartelle.</param>
    /// <param name="suggestedFolder">
    /// Cartella da cui far partire la selezione. Viene ignorata se è
    /// <c>null</c>, vuota o non esiste sul disco.
    /// </param>
    /// <returns>
    /// Un task che restituisce il percorso locale assoluto della cartella scelta,
    /// oppure <c>null</c> se l'utente annulla la selezione.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="title"/> è <c>null</c>, vuoto o composto solo da spazi.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// La piattaforma non supporta la selezione di cartelle, oppure la cartella
    /// scelta non è raggiungibile tramite un percorso locale.
    /// </exception>
    public async Task<string?> ChooseProjectFolderAsync(string title,
        string? suggestedFolder = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        IStorageProvider storageProvider = this._dialogOwner.StorageProvider;

        if (!storageProvider.CanPickFolder)
        {
            throw new NotSupportedException(
                "Folder selection is not available on this platform.");
        }

        using IStorageFolder? startFolder =
            await GetSuggestedFolderAsync(storageProvider, suggestedFolder);

        FolderPickerOpenOptions options = new()
        {
            Title = title,
            AllowMultiple = false,
            SuggestedStartLocation = startFolder
        };

        IReadOnlyList<IStorageFolder> selectedFolders =
            await storageProvider.OpenFolderPickerAsync(options);

        try
        {
            if (selectedFolders.Count == 0)
                return null;

            string? selectedPath = selectedFolders[0].TryGetLocalPath();

            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                throw new NotSupportedException(
                    "Only local project folders are supported.");
            }

            return selectedPath;
        }
        finally
        {
            foreach (IStorageFolder folder in selectedFolders)
                folder.Dispose();
        }
    }

    /// <summary>
    /// Chiede all'utente come gestire le modifiche non salvate di un progetto.
    /// </summary>
    /// <param name="projectName">Nome del progetto mostrato nel dialogo.</param>
    /// <returns>
    /// Un task che restituisce la scelta dell'utente come
    /// <see cref="UnsavedChangesChoice"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="projectName"/> è <c>null</c>.
    /// </exception>
    public Task<UnsavedChangesChoice> ConfirmUnsavedChangesAsync(string projectName)
    {
        ArgumentNullException.ThrowIfNull(projectName);

        UnsavedChangesDialogView dialog = new(projectName);

        return ShowDialogAsync<UnsavedChangesChoice>(dialog);
    }

    /// <summary>
    /// Mostra un dialogo modale di errore e attende che l'utente lo chiuda.
    /// </summary>
    /// <param name="title">Titolo del dialogo.</param>
    /// <param name="message">Messaggio di errore da mostrare.</param>
    /// <returns>Un task che si completa alla chiusura del dialogo.</returns>
    public async Task ShowErrorAsync(string title, string message)
    {
        ErrorDialogView dialog = new(title, message);

        await ShowDialogAsync<object?>(dialog);
    }

    /// <summary>
    /// Mostra un dialogo modale impostandolo temporaneamente come proprietario
    /// corrente, in modo che eventuali dialoghi aperti da esso risultino annidati
    /// correttamente. Il proprietario precedente viene sempre ripristinato.
    /// </summary>
    /// <typeparam name="TResult">Tipo del risultato restituito dal dialogo.</typeparam>
    /// <param name="dialog">Finestra di dialogo da mostrare.</param>
    /// <returns>
    /// Un task che restituisce il risultato con cui il dialogo è stato chiuso.
    /// </returns>
    private async Task<TResult> ShowDialogAsync<TResult>(Window dialog)
    {
        Window previousOwner = this._dialogOwner;
        this._dialogOwner = dialog;

        try
        {
            return await dialog.ShowDialog<TResult>(previousOwner);
        }
        finally
        {
            this._dialogOwner = previousOwner;
        }
    }

    /// <summary>
    /// Risolve la cartella suggerita in un <see cref="IStorageFolder"/> da usare
    /// come posizione iniziale del selettore.
    /// </summary>
    /// <param name="storageProvider">Provider di storage della piattaforma.</param>
    /// <param name="suggestedFolder">Percorso della cartella suggerita, anche relativo.</param>
    /// <returns>
    /// Un task che restituisce la cartella risolta, oppure <c>null</c> se il
    /// percorso è vuoto, la cartella non esiste o il provider non riesce a risolverla.
    /// </returns>
    private static async Task<IStorageFolder?> GetSuggestedFolderAsync(
        IStorageProvider storageProvider, string? suggestedFolder)
    {
        if (string.IsNullOrWhiteSpace(suggestedFolder))
            return null;

        if (!Directory.Exists(suggestedFolder))
            return null;

        string fullFolder = Path.GetFullPath(suggestedFolder);

        return await storageProvider.TryGetFolderFromPathAsync(fullFolder);
    }
}