using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.AppService.Interaction;

namespace VnEditor.UI.Views.Dialogs;

/// <summary>
/// Mostra il nome del progetto con modifiche non salvate
/// e restituisce la scelta dell'utente senza eseguire
/// salvataggi o modificare la sessione.
/// </summary>
public partial class UnsavedChangesDialogView : Window
{
    /// <summary>
    /// Inizializza una nuova istanza della classe <see cref="UnsavedChangesDialogView"/>.
    /// </summary>
    /// <param name="projectName">
    /// Il nome del progetto da visualizzare nel messaggio di conferma. Se vuoto o composto solo da spazi bianchi,
    /// viene mostrato il valore predefinito <c>"Untitled project"</c>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Sollevata se <paramref name="projectName"/> è <see langword="null"/>.
    /// </exception>
    public UnsavedChangesDialogView(string projectName)
    {
        ArgumentNullException.ThrowIfNull(projectName);

        InitializeComponent();

        ProjectNameTextBlock.Text = string.IsNullOrWhiteSpace(projectName)
            ? "Untitled project"
            : projectName;
    }

    /// <summary>
    /// Gestisce il clic sul pulsante di salvataggio chiudendo il dialogo con il risultato <see cref="UnsavedChangesChoice.Save"/>.
    /// </summary>
    /// <param name="sender">L'origine dell'evento.</param>
    /// <param name="eventArgs">I dati associati all'evento di clic.</param>
    private void OnSaveClick(object? sender, RoutedEventArgs eventArgs)
    {
        Close(UnsavedChangesChoice.Save);
    }

    /// <summary>
    /// Gestisce il clic sul pulsante di scarto delle modifiche chiudendo il dialogo con il risultato <see cref="UnsavedChangesChoice.Discard"/>.
    /// </summary>
    /// <param name="sender">L'origine dell'evento.</param>
    /// <param name="eventArgs">I dati associati all'evento di clic.</param>
    private void OnDiscardClick(object? sender, RoutedEventArgs eventArgs)
    {
        Close(UnsavedChangesChoice.Discard);
    }

    /// <summary>
    /// Gestisce il clic sul pulsante di annullamento chiudendo il dialogo con il risultato <see cref="UnsavedChangesChoice.Cancel"/>.
    /// </summary>
    /// <param name="sender">L'origine dell'evento.</param>
    /// <param name="eventArgs">I dati associati all'evento di clic.</param>
    private void OnCancelClick(object? sender, RoutedEventArgs eventArgs)
    {
        Close(UnsavedChangesChoice.Cancel);
    }
}
