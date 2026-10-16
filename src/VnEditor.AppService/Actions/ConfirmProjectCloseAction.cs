using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.AppService.Projects;

namespace VnEditor.AppService.Projects.Actions;

public sealed class ConfirmProjectCloseAction : IProjectFileAction
{
    private readonly UnsavedChangesGuard _unsavedChanges;

    /// <summary>
    /// Inizializza una nuova istanza della classe <see cref="ConfirmProjectCloseAction"/>.
    /// </summary>
    /// <param name="unsavedChanges">La guardia incaricata di verificare la presenza di modifiche non salvate e gestire l'eventuale salvataggio.</param>
    /// <exception cref="ArgumentNullException">
    /// Sollevata se <paramref name="unsavedChanges"/> è <see langword="null"/>.
    /// </exception>
    public ConfirmProjectCloseAction(UnsavedChangesGuard unsavedChanges)
    {
        ArgumentNullException.ThrowIfNull(unsavedChanges);

        this._unsavedChanges = unsavedChanges;
    }
    /// <summary>
    /// Verifica in modo asincrono se è possibile procedere con la chiusura del progetto corrente.
    /// </summary>
    /// <returns>
    /// Un'attività asincrona il cui risultato è <see langword="true"/> se il progetto non ha modifiche in sospeso, 
    /// se l'utente ha scelto di scartarle o se il salvataggio è stato completato con successo; 
    /// <see langword="false"/> se l'operazione di chiusura deve essere annullata.
    /// </returns>
    public Task<bool> ExecuteAsync()
    {
        return this._unsavedChanges.CanLeaveAsync();
    }
}
