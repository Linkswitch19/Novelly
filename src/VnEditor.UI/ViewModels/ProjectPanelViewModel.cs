using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using VnEditor.UI.Selection;

namespace VnEditor.UI.ViewModels;

/// <summary>
/// Modello di vista (ViewModel) per il pannello laterale del progetto.
/// Gestisce i bottoni di navigazione (Scene, Personaggi, ecc.) e ascolta i cambiamenti 
/// della selezione globale per evidenziare correttamente quale categoria è attiva in questo momento.
/// </summary>
public partial class ProjectPanelViewModel : ViewModelBase
{
    private readonly WorkspaceSelection _selection;
    

    /// <summary>
    /// Inizializza il pannello e si iscrive (sottoscrive) agli eventi della selezione globale.
    /// </summary>
    public ProjectPanelViewModel(WorkspaceSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);

        this._selection = selection;
        this._selection.PropertyChanged += OnSelectionChanged;
    }

    

    public bool IsScenesSelected =>
       this._selection.Current.Section == ProjectSection.Scenes;

    public bool IsStoryValuesSelected =>
        this._selection.Current.Section == ProjectSection.Variables;

    public bool IsCharactersSelected =>
        this._selection.Current.Section == ProjectSection.Characters;

    public bool IsBackgroundsSelected =>
        this._selection.Current.Section == ProjectSection.Backgrounds;

    public bool IsMusicSelected =>
        this._selection.Current.Section == ProjectSection.Music;

    // --- COMANDI COLLEGATI AI BOTTONI DELL'INTERFACCIA ---

    [RelayCommand]
    private void SelectScenes()
    {
        this._selection.SelectSection(ProjectSection.Scenes);
    }

    [RelayCommand]
    private void SelectStoryValues()
    {
        this._selection.SelectSection(ProjectSection.Variables);
    }

    [RelayCommand]
    private void SelectCharacters()
    {
        this._selection.SelectSection(ProjectSection.Characters);
    }

    [RelayCommand]
    private void SelectBackgrounds()
    {
        this._selection.SelectSection(ProjectSection.Backgrounds);
    }

    [RelayCommand]
    private void SelectMusic()
    {
        this._selection.SelectSection(ProjectSection.Music);
    }

   

    /// <summary>
    /// Reagisce ai cambiamenti provenienti dalla classe WorkspaceSelection.
    /// </summary>
    /// <param name="sender">Chi ha lanciato l'evento (in questo caso WorkspaceSelection).</param>
    /// <param name="e">I dettagli dell'evento, contiene il nome della proprietà che è cambiata.</param>
    private void OnSelectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(WorkspaceSelection.Current))
            return;
        OnPropertyChanged(nameof(IsScenesSelected));
        OnPropertyChanged(nameof(IsStoryValuesSelected));
        OnPropertyChanged(nameof(IsCharactersSelected));
        OnPropertyChanged(nameof(IsBackgroundsSelected));
        OnPropertyChanged(nameof(IsMusicSelected));
    }

}
