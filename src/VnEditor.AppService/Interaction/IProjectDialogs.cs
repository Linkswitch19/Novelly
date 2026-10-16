using System;
using System.Collections.Generic;
using System.Text;

namespace VnEditor.AppService.Interaction;

/// <summary>
/// Definisce le interazioni richieste dai casi d'uso File
/// attraverso nomi, percorsi e decisioni dell'utente.
/// L'implementazione concreta dei dialoghi appartiene alla UI.
/// L'annullamento restituisce null oppure UnsavedChangesChoice.Cancel,
/// secondo il tipo di risultato richiesto.
/// </summary>
public interface IProjectDialogs
{
    /// <summary>
    /// Mostra la finestra modale per la configurazione di un nuovo progetto (nome e cartella iniziale).
    /// </summary>
    /// <returns>
    /// Un'istanza di <see cref="NewProjectRequest"/> contenente i dati inseriti dall'utente se confermato;
    /// altrimenti <c>null</c> se l'utente annulla o chiude la finestra.
    /// </returns>
    Task<NewProjectRequest?> ShowNewProjectAsync();

    /// <summary>
    /// Apre la finestra di sistema per la selezione di una cartella sul disco (usata ad esempio in "Apri Progetto" o "Salva con nome").
    /// </summary>
    /// <param name="title">Il titolo da visualizzare nella barra della finestra di dialogo.</param>
    /// <param name="suggestedFolder">Percorso facoltativo iniziale da cui avviare la navigazione nel file system.</param>
    /// <returns>
    /// Il percorso assoluto della cartella selezionata se confermata; altrimenti <c>null</c> se l'operazione viene annullata.
    /// </returns>
    Task<string?> ChooseProjectFolderAsync(string title, string? suggestedFolder = null);

    /// <summary>
    /// Mostra un messaggio di conferma quando l'utente tenta di chiudere l'editor o aprire un altro progetto
    /// in presenza di modifiche non salvate.
    /// </summary>
    /// <param name="projectName">Il nome del progetto attualmente aperto e modificato da mostrare all'utente.</param>
    /// <returns>
    /// Uno dei valori di <see cref="UnsavedChangesChoice"/>: 
    /// <c>Save</c> per salvare prima di procedere, 
    /// <c>Discard</c> per scartare le modifiche, 
    /// oppure <c>Cancel</c> per interrompere l'azione e rimanere nel progetto corrente.
    /// </returns>
    Task<UnsavedChangesChoice> ConfirmUnsavedChangesAsync(string projectName);

    /// <summary>
    /// Mostra una finestra modale per notificare all'utente un errore operativo o un'eccezione non gestita.
    /// </summary>
    /// <param name="title">Il titolo o l'intestazione dell'errore (es. "Salvataggio non riuscito").</param>
    /// <param name="message">Il dettaglio testuale della problematica riscontrata.</param>
    /// <returns>Un'attività (<see cref="Task"/>) che si completa quando l'utente chiude la finestra di notifica.</returns>
    Task ShowErrorAsync(string title, string message);

}
