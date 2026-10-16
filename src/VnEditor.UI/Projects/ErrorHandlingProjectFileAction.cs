using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.AppService.Projects.Actions;
namespace VnEditor.UI.Projects;

/// <summary>
/// Avvolge un'azione File delegando la gestione degli errori
/// a ProjectFileErrorPresenter.
/// Mantiene il risultato dell'azione e il contratto IProjectFileAction,
/// senza modificare il progetto o lo stato di occupazione della UI.
/// </summary>
public sealed class ErrorHandlingProjectFileAction : IProjectFileAction
{
    private readonly IProjectFileAction _inner;
    private readonly ProjectFileErrorPresenter _errors;
    private readonly string _errorTitle;
    private readonly string _fallbackMessage;

    /// <summary>
    /// Inizializza una nuova istanza della classe <see cref="ErrorHandlingProjectFileAction"/>.
    /// </summary>
    /// <param name="inner">L'azione sul file di progetto da eseguire e proteggere tramite la gestione degli errori.</param>
    /// <param name="errors">Il presenter incaricato di intercettare e mostrare eventuali errori all'utente.</param>
    /// <param name="errorTitle">Il titolo della finestra o notifica di errore mostrata in caso di fallimento.</param>
    /// <param name="fallbackMessage">Il messaggio di errore predefinito da mostrare se l'eccezione non fornisce dettagli utilizzabili.</param>
    /// <exception cref="ArgumentNullException">
    /// Sollevata se <paramref name="inner"/> o <paramref name="errors"/> sono <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Sollevata se <paramref name="errorTitle"/> o <paramref name="fallbackMessage"/> sono <see langword="null"/>, vuoti o composti solo da spazi bianchi.
    /// </exception>
    public ErrorHandlingProjectFileAction(IProjectFileAction inner, ProjectFileErrorPresenter errors, string errorTitle, string fallbackMessage)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(errors);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorTitle);
        ArgumentException.ThrowIfNullOrWhiteSpace(fallbackMessage);

        this._inner = inner;
        this._errors = errors;
        this._errorTitle = errorTitle;
        this._fallbackMessage = fallbackMessage;
    }

    /// <summary>
    /// Esegue in modo asincrono l'azione sul file incapsulata, intercettando e presentando eventuali errori tramite il presenter configurato.
    /// </summary>
    /// <returns>
    /// Un'attività asincrona che rappresenta l'operazione. Il risultato contiene <see langword="true"/> se l'azione interna 
    /// è stata completata con successo; <see langword="false"/> se l'azione è fallita o è stata interrotta da un errore gestito.
    /// </returns>
    public Task<bool> ExecuteAsync()
    {
        return this._errors.TryRunAsync(this._inner.ExecuteAsync, this._errorTitle, this._fallbackMessage);
    }
}
