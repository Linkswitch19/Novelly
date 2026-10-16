using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.AppService.Interaction;
using VnEditor.UI.ViewModels;

namespace VnEditor.UI.Views.Dialogs;

/// <summary>
/// Collega il risultato del ViewModel alla chiusura del dialogo.
/// Imposta il focus iniziale e impedisce la chiusura mentre
/// è in corso la selezione della cartella.
/// </summary>
public partial class NewProjectDialogView : Window
{
    private readonly NewProjectDialogViewModel _viewModel;

    /// <summary>
    /// Inizializza una nuova istanza della classe <see cref="NewProjectDialogView"/>.
    /// </summary>
    /// <param name="viewModel">Il ViewModel associato al dialogo che gestisce lo stato e la logica di validazione.</param>
    /// <exception cref="ArgumentNullException">
    /// Sollevata se <paramref name="viewModel"/> è <see langword="null"/>.
    /// </exception>
    public NewProjectDialogView(NewProjectDialogViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        this._viewModel = viewModel;

        InitializeComponent();
        DataContext = viewModel;

        this._viewModel.CloseRequested += OnCloseRequested;
    }

    /// <summary>
    /// Gestisce l'evento di apertura della finestra impostando il focus della tastiera sulla casella di testo del nome progetto.
    /// </summary>
    /// <param name="eventArgs">I dati associati all'evento di apertura.</param>
    protected override void OnOpened(EventArgs eventArgs)
    {
        base.OnOpened(eventArgs);

        ProjectNameTextBox.Focus();
    }

    /// <summary>
    /// Intercetta la richiesta di chiusura della finestra, annullandola se il ViewModel è impegnato in un'operazione asincrona.
    /// </summary>
    /// <param name="eventArgs">I dati dell'evento di chiusura che consentono di annullare l'operazione tramite <see cref="WindowClosingEventArgs.Cancel"/>.</param>
    protected override void OnClosing(WindowClosingEventArgs eventArgs)
    {
        base.OnClosing(eventArgs);

        if (_viewModel.IsBusy)
            eventArgs.Cancel = true;
    }

    /// <summary>
    /// Gestisce la chiusura definitiva della finestra rimuovendo la sottoscrizione agli eventi del ViewModel.
    /// </summary>
    /// <param name="eventArgs">I dati associati all'evento di chiusura.</param>
    protected override void OnClosed(EventArgs eventArgs)
    {
        _viewModel.CloseRequested -= OnCloseRequested;

        base.OnClosed(eventArgs);
    }

    /// <summary>
    /// Riceve la richiesta di chiusura dal ViewModel e chiude la finestra restituendo il risultato al chiamante.
    /// </summary>
    /// <param name="request">
    /// I dati del nuovo progetto (<see cref="NewProjectRequest"/>) se l'utente ha confermato; 
    /// <see langword="null"/> se l'operazione è stata annullata.
    /// </param>
    private void OnCloseRequested(NewProjectRequest? request)
    {
        Close(request);
    }
}
