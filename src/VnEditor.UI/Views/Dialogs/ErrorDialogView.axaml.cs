using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace VnEditor.UI.Views.Dialogs;

/// <summary>
/// Mostra il titolo e il messaggio di un errore operativo
/// e permette all'utente di chiudere la notifica.
/// </summary>
public partial class ErrorDialogView : Window
{
    /// <summary>
    /// Inizializza una nuova istanza della classe <see cref="ErrorDialogView"/>.
    /// </summary>
    /// <param name="title">Il titolo dell'errore, mostrato sia nella barra del titolo della finestra sia nell'intestazione interna.</param>
    /// <param name="message">Il messaggio descrittivo dell'errore da mostrare all'utente.</param>
    /// <exception cref="ArgumentException">
    /// Sollevata se <paramref name="title"/> o <paramref name="message"/> sono <see langword="null"/>, vuoti o composti solo da spazi bianchi.
    /// </exception>
    public ErrorDialogView(string title, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        InitializeComponent();

        Title = title;
        ErrorTitleTextBlock.Text = title;
        ErrorMessageTextBlock.Text = message;
    }

    /// <summary>
    /// Gestisce il clic sul pulsante di chiusura terminando la finestra di dialogo.
    /// </summary>
    /// <param name="sender">L'origine dell'evento.</param>
    /// <param name="eventArgs">I dati associati all'evento di clic.</param>
    private void OnCloseClick(object? sender, RoutedEventArgs eventArgs)
    {
        Close();
    }
}