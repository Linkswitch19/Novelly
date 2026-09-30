using System.ComponentModel;
using VnEditor.UI.Selection;

namespace VnEditor.UI.ViewModels;
/// <summary>
/// Espone alla finestra la selezione condivisa e la navigazione del progetto.
/// </summary>
public sealed class MainViewModel : ViewModelBase
{
    public MainViewModel(WorkspaceSelection selection, ProjectPanelViewModel projectPanel)
    {
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(projectPanel);

        Selection = selection;
        ProjectPanel = projectPanel;
        Selection.PropertyChanged += OnSelectionChanged;
    }


    public WorkspaceSelection Selection { get; }

    public ProjectPanelViewModel ProjectPanel { get; }

    public string SelectedSectionTitle => Selection.Current.Section switch
    {
        ProjectSection.Scenes => "Scenes",
        ProjectSection.Variables => "Story values",
        ProjectSection.Characters => "Characters",
        ProjectSection.Backgrounds => "Backgrounds",
        ProjectSection.Music => "Music",
        _ => "No selection"
    };

    private void OnSelectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(WorkspaceSelection.Current))
            return;

        OnPropertyChanged(nameof(SelectedSectionTitle));
    }
}
