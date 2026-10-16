using System.ComponentModel;
using VnEditor.UI.Selection;
using VnEditor.AppService.Sessions;
using VnEditor.Domain;

namespace VnEditor.UI.ViewModels;
/// <summary>
/// Espone alla finestra il menu File, la navigazione e lo stato del progetto.
/// Aggiorna il titolo dalla sessione e reimposta la selezione quando
/// viene attivato un progetto diverso.
/// </summary>
public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private readonly ProjectSession _session;
    private Project? _displayedProject;
    public MainViewModel(WorkspaceSelection selection, ProjectPanelViewModel projectPanel,
    ProjectSession session, FileMenuViewModel fileMenu)
    {
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(projectPanel);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(fileMenu);

        Selection = selection;
        ProjectPanel = projectPanel;
        FileMenu = fileMenu;

        this._session = session;
        this._displayedProject = session.Project;

        Selection.PropertyChanged += OnSelectionChanged;
        this._session.StateChanged += OnSessionStateChanged;
    }

    public FileMenuViewModel FileMenu { get; }

    public bool HasProject => this._session.HasProject;

    public string ProjectTitle => this._session.Project?.Title ?? string.Empty;

    public string WindowTitle
    {
        get
        {
            if (!HasProject)
                return "Novelly";

            string modifiedMarker = _session.IsModified ? " *" : string.Empty;

            return $"{ProjectTitle}{modifiedMarker} - Novelly";
        }
    }

    public WorkspaceSelection Selection { get; }

    public ProjectPanelViewModel ProjectPanel { get; }

    public void Dispose()
    {
        Selection.PropertyChanged -= OnSelectionChanged;
        this._session.StateChanged -= OnSessionStateChanged;
    }

    private void OnSessionStateChanged(object? sender, EventArgs eventArgs)
    {
        SynchronizeProjectSelection();

        OnPropertyChanged(nameof(HasProject));
        OnPropertyChanged(nameof(ProjectTitle));
        OnPropertyChanged(nameof(WindowTitle));
    }

    private void SynchronizeProjectSelection()
    {
        Project? currentProject = this._session.Project;

        if (ReferenceEquals(this._displayedProject, currentProject))
            return;

        this._displayedProject = currentProject;

        if (currentProject is null)
            Selection.Clear();
        else
            Selection.SelectSection(ProjectSection.Scenes);
    }

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
