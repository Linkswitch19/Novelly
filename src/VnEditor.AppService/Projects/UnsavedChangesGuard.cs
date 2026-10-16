using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.AppService.Sessions;
using VnEditor.AppService.Interaction;
using VnEditor.AppService.Projects.Actions;

namespace VnEditor.AppService.Projects;

/// <summary>
/// Verifica se l'utente può lasciare il progetto corrente.
/// Gestisce la decisione Save/Discard/Cancel e delega il salvataggio
/// all'azione ricevuta, verificando che la decisione riguardi ancora
/// lo stesso stato del progetto.
/// </summary>
public sealed class UnsavedChangesGuard
{
    private readonly ProjectSession _session;
    private readonly IProjectDialogs _dialogs;
    private readonly IProjectFileAction _saveAction;

    /// <summary>
    /// Inizializza una nuova istanza della classe <see cref="UnsavedChangesGuard"/>.
    /// </summary>
    /// <param name="session">La sessione di progetto corrente da monitorare per modifiche non salvate e controlli di revisione.</param>
    /// <param name="dialogs">Il servizio di dialogo utilizzato per richiedere conferma all'utente e mostrare eventuali errori.</param>
    /// <param name="saveAction">L'azione da eseguire qualora l'utente scelga di salvare le modifiche prima di uscire.</param>
    /// <exception cref="ArgumentNullException">
    /// Sollevata se <paramref name="session"/>, <paramref name="dialogs"/> o <paramref name="saveAction"/> sono <see langword="null"/>.
    /// </exception>
    public UnsavedChangesGuard(ProjectSession session, IProjectDialogs dialogs, IProjectFileAction saveAction)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(saveAction);

        this._session = session;
        this._dialogs = dialogs;
        this._saveAction = saveAction;
    }

    /// <summary>
    /// Determina in modo asincrono se è possibile procedere con la chiusura o la sostituzione del progetto corrente.
    /// </summary>
    /// <remarks>
    /// Se il progetto presenta modifiche non salvate, richiede una decisione all'utente. 
    /// Se il progetto subisce ulteriori modifiche mentre la finestra di dialogo è aperta, l'operazione viene annullata 
    /// e viene notificato un errore all'utente.
    /// </remarks>
    /// <returns>
    /// Un'attività asincrona il cui risultato è <see langword="true"/> se non ci sono modifiche in sospeso, 
    /// se l'utente ha scelto di scartarle o se il salvataggio è andato a buon fine senza conflitti di revisione; 
    /// <see langword="false"/> se l'utente ha annullato l'operazione, se il salvataggio è fallito o se la revisione del progetto è cambiata nel frattempo.
    /// </returns>
    public async Task<bool> CanLeaveAsync()
    {
        if (!this._session.IsModified)
            return true;

        long? expectedRevision = this._session.CaptureCurrentRevision();
        string projectName = this._session.Project?.Title ?? "Untitled project";

        UnsavedChangesChoice choice =
            await this._dialogs.ConfirmUnsavedChangesAsync(projectName);

        if (choice == UnsavedChangesChoice.Cancel)
            return false;

        if (!this._session.IsCurrentRevision(expectedRevision))
        {
            await this._dialogs.ShowErrorAsync(
                "Project changed",
                "The project changed while the dialog was open. Please try again.");

            return false;
        }

        return choice switch
        {
            UnsavedChangesChoice.Save => await SaveBeforeLeavingAsync(expectedRevision),
            UnsavedChangesChoice.Discard => true,
            _ => false
        };
    }

    /// <summary>
    /// Esegue l'azione di salvataggio e verifica che il progetto risulti effettivamente salvato e nella revisione attesa.
    /// </summary>
    /// <param name="expectedRevision">La revisione del progetto acquisita prima dell'apertura del dialogo di conferma.</param>
    /// <returns>
    /// <see langword="true"/> se l'azione di salvataggio ha restituito esito positivo, la revisione corrente coincide ancora 
    /// con <paramref name="expectedRevision"/> e la sessione non risulta più modificata; altrimenti, <see langword="false"/>.
    /// </returns>
    private async Task<bool> SaveBeforeLeavingAsync(long? expectedRevision)
    {
        bool saved = await this._saveAction.ExecuteAsync();

        return saved && this._session.IsCurrentRevision(expectedRevision) && !this._session.IsModified;
    }
}
