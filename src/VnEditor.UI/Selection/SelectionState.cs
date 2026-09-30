using System;
using System.Collections.Generic;
using System.Text;

namespace VnEditor.UI.Selection;

/// <summary>
/// Rappresenta lo stato della selezione corrente all'interno dell'interfaccia utente.
/// Utilizza categorie generiche (sezioni) e identificatori univoci (ID) stabili anziché conservare 
/// riferimenti diretti agli oggetti in memoria del progetto. Questo approccio previene memory leak 
/// e l'uso di dati obsoleti qualora gli oggetti reali vengano eliminati o ricreati nel motore sottostante.
/// </summary>
public sealed record SelectionState
{

    /// <summary>
    /// Costruttore privato. Le istanze pubbliche devono essere create in modo controllato 
    /// utilizzando le factory statiche (<see cref="Empty"/>, <see cref="ForSection"/>, <see cref="ForScene"/>, <see cref="ForBlock"/>).
    /// </summary>
    private SelectionState (ProjectSection? section, string? sceneId, string? blockId)
    {
        Section = section;
        SceneId = sceneId;
        BlockId = blockId;
    }

    /// <summary>
    /// Ottiene uno stato che rappresenta l'assenza totale di selezione (nessun elemento attivo).
    /// </summary>
    public static SelectionState Empty { get; } = new(null, null, null);

    /// <summary>
    /// Ottiene la macro-sezione del progetto attualmente selezionata nell'interfaccia (es. Personaggi, Scene).
    /// </summary>
    public ProjectSection? Section { get; }

    /// <summary>
    /// Ottiene l'ID della scena attualmente selezionata, se applicabile.
    /// </summary>
    public string? SceneId { get; }

    /// <summary>
    /// Ottiene l'ID del blocco logico/narrativo attualmente selezionato all'interno della scena, se applicabile.
    /// </summary>
    public string? BlockId { get; }

    /// <summary>
    /// Crea uno stato di selezione in cui è attiva una macro-sezione principale, 
    /// ma nessun elemento specifico al suo interno è stato ancora selezionato.
    /// </summary>
    /// <param name="section">La sezione logica da attivare.</param>
    /// <returns>Una nuova istanza che riflette la selezione della sezione.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Lanciata se il valore passato non appartiene all'enumerazione <see cref="ProjectSection"/>.</exception>
    public static SelectionState ForSection(ProjectSection section)
    {
        if(!Enum.IsDefined(section))
            throw new ArgumentOutOfRangeException(nameof(section));

        return new SelectionState(section, null, null);

    }

    /// <summary>
    /// Crea uno stato di selezione focalizzato su una singola scena. 
    /// Imposta automaticamente la sezione di riferimento corretta (<see cref="ProjectSection.Scenes"/>).
    /// </summary>
    /// <param name="sceneId">L'identificatore univoco della scena selezionata.</param>
    /// <returns>Una nuova istanza che riflette la selezione della scena.</returns>
    /// <exception cref="ArgumentException">Lanciata se l'ID fornito è nullo o vuoto.</exception>
    public static SelectionState ForScene(string sceneId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sceneId);

        return new SelectionState(ProjectSection.Scenes, sceneId, null);
    }

    /// <summary>
    /// Crea uno stato di selezione dettagliato per un singolo blocco all'interno di una scena.
    /// Imposta automaticamente la sezione di riferimento su <see cref="ProjectSection.Scenes"/>.
    /// </summary>
    /// <param name="sceneId">L'identificatore univoco della scena che ospita il blocco.</param>
    /// <param name="blockId">L'identificatore univoco del blocco selezionato.</param>
    /// <returns>Una nuova istanza che riflette la selezione del blocco.</returns>
    /// <exception cref="ArgumentException">Lanciata se l'ID della scena o del blocco è nullo o vuoto.</exception>
    public static SelectionState ForBlock(string sceneId, string blockId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sceneId);
        ArgumentException.ThrowIfNullOrWhiteSpace(blockId);

        return new SelectionState(ProjectSection.Scenes, sceneId, blockId);
    }
}
