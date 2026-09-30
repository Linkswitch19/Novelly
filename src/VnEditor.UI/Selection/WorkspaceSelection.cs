using CommunityToolkit.Mvvm.ComponentModel;

namespace VnEditor.UI.Selection;

/// <summary>
/// Mantiene e gestisce lo stato della selezione condivisa tra tutti i pannelli della finestra principale.
/// Eredita da <see cref="ObservableObject"/> per poter avvisare automaticamente l'interfaccia grafica (UI) 
/// ogni volta che l'utente clicca su un elemento diverso, permettendo l'aggiornamento dei pannelli.
/// </summary>

public sealed class WorkspaceSelection : ObservableObject
{
    private SelectionState _current = SelectionState.Empty;

    /// <summary>
    /// Ottiene lo stato di selezione corrente (il nostro "segnalibro").
    /// L'impostazione di un nuovo valore passa attraverso <see cref="ObservableObject.SetProperty"/> 
    /// per notificare i cambiamenti alla grafica.
    /// </summary>
    public SelectionState Current
    {
        get => this._current;
        private set => SetProperty(ref this._current, value);

    }

    /// <summary>
    /// Cambia la selezione corrente attivando una macro-sezione del progetto (es. Personaggi, Musiche).
    /// </summary>
    /// <param name="section">La sezione logica da selezionare.</param>
    public void SelectSection(ProjectSection section)
    {
        Current = SelectionState.ForSection(section);
    }

    /// <summary>
    /// Cambia la selezione corrente focalizzandosi su una singola scena.
    /// </summary>
    /// <param name="sceneId">L'identificatore univoco della scena da selezionare.</param>
    public void SelectScene(string sceneId)
    {
        Current = SelectionState.ForScene(sceneId);
    }

    /// <summary>
    /// Cambia la selezione corrente evidenziando un blocco specifico all'interno di una scena.
    /// </summary>
    /// <param name="sceneId">L'identificatore della scena contenitore.</param>
    /// <param name="blockId">L'identificatore del blocco narrativo/logico da selezionare.</param>
    public void SelectBlock(string sceneId, string blockId)
    {
        Current = SelectionState.ForBlock(sceneId, blockId);
    }

    /// <summary>
    /// Svuota completamente la selezione, disattivando qualsiasi elemento precedentemente selezionato.
    /// Utile quando si chiude un file o si clicca nel vuoto.
    /// </summary>
    public void Clear()
    {
        Current = SelectionState.Empty;
    }


}