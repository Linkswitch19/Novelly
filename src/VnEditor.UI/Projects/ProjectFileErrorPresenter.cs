using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.AppService.Serialization;
using VnEditor.AppService.Interaction;

namespace VnEditor.UI.Projects;

/// <summary>
/// Gestisce gli errori operativi dei comandi File e mostra messaggi
/// comprensibili in inglese tramite il servizio di dialoghi.
/// Restituisce false in caso di annullamento o errore gestito.
/// Non modifica il progetto o lo stato della sessione.
/// </summary>
public sealed class ProjectFileErrorPresenter
{
    private readonly IProjectDialogs _dialogs;
    /// <summary>
    /// Inizializza una nuova istanza della classe <see cref="ProjectFileErrorPresenter"/>.
    /// </summary>
    /// <param name="dialogs">Il servizio di dialoghi utilizzato per mostrare i messaggi di errore all'utente.</param>
    /// <exception cref="ArgumentNullException">Sollevata se <paramref name="dialogs"/> è <see langword="null"/>.</exception>
    public ProjectFileErrorPresenter(IProjectDialogs dialogs)
    {
        ArgumentNullException.ThrowIfNull(dialogs);
        this._dialogs = dialogs;
    }
    /// <summary>
    /// Esegue in modo asincrono un'operazione su file intercettando le eccezioni previste e mostrando un dialogo di errore in caso di fallimento.
    /// </summary>
    /// <param name="operation">La funzione asincrona che rappresenta l'operazione da eseguire.</param>
    /// <param name="errorTitle">Il titolo della finestra di dialogo mostrata in caso di errore.</param>
    /// <param name="fallbackMessage">Il messaggio di errore predefinito da mostrare se l'eccezione non ha una mappatura specifica.</param>
    /// <returns>
    /// Un task che rappresenta l'operazione asincrona. Il risultato è <see langword="true"/> se l'operazione ha avuto successo;
    /// <see langword="false"/> se l'operazione è stata annullata, ha restituito <see langword="false"/> o ha generato un errore gestito.
    /// </returns>
    /// <exception cref="ArgumentNullException">Sollevata se <paramref name="operation"/>, <paramref name="errorTitle"/> o <paramref name="fallbackMessage"/> sono <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Sollevata se <paramref name="errorTitle"/> o <paramref name="fallbackMessage"/> sono stringhe vuote o composte solo da spazi bianchi.</exception>
    public async Task<bool> TryRunAsync(Func<Task<bool>> operation, string errorTitle, string fallbackMessage)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorTitle);
        ArgumentException.ThrowIfNullOrWhiteSpace(fallbackMessage);

        try
        {
            return await operation();
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch(Exception exception) when (IsFileOperationError(exception))
        {
            string message = GetErrorMessage(exception, fallbackMessage);

            await this._dialogs.ShowErrorAsync(errorTitle,message);

            return false;

        }

    }
    /// <summary>
    /// Determina se l'eccezione specificata rappresenta un errore previsto e gestibile relativo a un'operazione su file o di serializzazione.
    /// </summary>
    /// <param name="exception">L'eccezione da valutare.</param>
    /// <returns>
    /// <see langword="true"/> se l'eccezione è di tipo <see cref="IOException"/>, <see cref="UnauthorizedAccessException"/>,
    /// <see cref="ArgumentException"/>, <see cref="NotSupportedException"/> o <see cref="ProjectSerializationException"/>;
    /// in caso contrario, <see langword="false"/>.
    /// </returns>
    private static bool IsFileOperationError(Exception exception)
    {
        return exception is IOException
            or UnauthorizedAccessException
            or ArgumentException
            or NotSupportedException
            or ProjectSerializationException;
    }
    /// <summary>
    /// Restituisce un messaggio di errore leggibile dall'utente corrispondente al tipo di eccezione specificata.
    /// </summary>
    /// <param name="exception">L'eccezione intercettata durante l'operazione su file.</param>
    /// <param name="fallbackMessage">Il messaggio predefinito da restituire se per l'eccezione non è definito un messaggio specifico.</param>
    /// <returns>Una stringa in lingua inglese contenente il messaggio di errore da mostrare all'utente.</returns>
    private static string GetErrorMessage(Exception exception, string fallbackMessage)
    {
        return exception switch
        {
            ProjectSerializationException serializationException =>
                GetSerializationErrorMessage(serializationException),

            UnauthorizedAccessException =>
                "You do not have permission to use this folder. Choose a folder you can access.",

            DirectoryNotFoundException =>
                "A required folder could not be found. Check the project location and try again.",

            FileNotFoundException =>
                "A required file could not be found. Check that the project files are available.",

            _ => fallbackMessage
        };
    }

    /// <summary>
    /// Restituisce un messaggio di errore specifico in base alla causa del fallimento indicata da <see cref="ProjectSerializationException.Kind"/>.
    /// </summary>
    /// <param name="exception">L'eccezione di serializzazione del progetto da analizzare.</param>
    /// <returns>Una stringa in lingua inglese che descrive il problema di compatibilità o integrità dei dati del progetto.</returns>
    private static string GetSerializationErrorMessage(ProjectSerializationException exception)
    {
        return exception.Kind switch
        {
            ProjectSerializationException.FailureKind.UnsupportedVersion =>
                "This project's file format is not supported by this version of Novelly.",

            ProjectSerializationException.FailureKind.UnknownType =>
                "This project contains content that this version of Novelly does not support.",

            _ => "The project data is invalid or incomplete."
        };
    }
}
