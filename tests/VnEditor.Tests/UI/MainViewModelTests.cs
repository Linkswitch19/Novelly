using VnEditor.UI.Selection;
using VnEditor.UI.ViewModels;
using NSubstitute;
using VnEditor.AppService.Projects.Actions;
using VnEditor.AppService.Sessions;
using VnEditor.Domain;
using VnEditor.Infrastructure.History;

namespace VnEditor.Tests.UI;

/// <summary>
/// Verifica la navigazione e la sincronizzazione del ViewModel principale
/// con la sessione: progetto attivo, titolo e indicazione delle modifiche.
/// </summary>
public sealed class MainViewModelTests
{
    [Fact]
    public void NavigationAndClear_UpdateSelectedSectionTitle()
    {
        WorkspaceSelection selection = new();
        selection.SelectSection(ProjectSection.Scenes);

        using ProjectSession session = new();
        using FileMenuViewModel fileMenu = CreateFileMenu(session);

        ProjectPanelViewModel projectPanel = new(selection);
        using MainViewModel viewModel = new(selection, projectPanel, session, fileMenu);
        int titleChangedCount = 0;

        viewModel.PropertyChanged += (_, eventArgs) =>
        {
            if (eventArgs.PropertyName == nameof(MainViewModel.SelectedSectionTitle))
                titleChangedCount++;
        };

        projectPanel.SelectCharactersCommand.Execute(null);

        Assert.Equal("Characters", viewModel.SelectedSectionTitle);

        selection.Clear();

        Assert.Equal("No selection", viewModel.SelectedSectionTitle);
        Assert.Equal(2, titleChangedCount);
    }

    [Fact]
    public void EmptySession_ShowsApplicationTitle()
    {
        using ProjectSession session = new();
        using FileMenuViewModel fileMenu = CreateFileMenu(session);

        WorkspaceSelection selection = new();
        ProjectPanelViewModel projectPanel = new(selection);
        using MainViewModel viewModel = new(selection, projectPanel, session, fileMenu);

        Assert.False(viewModel.HasProject);
        Assert.Equal(string.Empty, viewModel.ProjectTitle);
        Assert.Equal("Novelly", viewModel.WindowTitle);
    }

    [Theory]
    [InlineData(true, "My story - Novelly")]
    [InlineData(false, "My story * - Novelly")]
    public void ProjectActivation_UpdatesTitleAndNotifiesBindings(
        bool isSaved, string expectedTitle)
    {
        using ProjectSession session = new();
        using FileMenuViewModel fileMenu = CreateFileMenu(session);

        WorkspaceSelection selection = new();
        ProjectPanelViewModel projectPanel = new(selection);
        using MainViewModel viewModel = new(selection, projectPanel, session, fileMenu);

        List<string?> changedProperties = [];
        viewModel.PropertyChanged += (_, eventArgs) =>
            changedProperties.Add(eventArgs.PropertyName);

        Project project = new() { Title = "My story" };
        PassThroughProjectHistory history = new(project);
        string projectFolder = Path.Combine(Path.GetTempPath(), "NovellyMainViewModelTests");

        session.SetProject(history, projectFolder, isSaved);

        Assert.True(viewModel.HasProject);
        Assert.Equal("My story", viewModel.ProjectTitle);
        Assert.Equal(expectedTitle, viewModel.WindowTitle);

        Assert.Contains(nameof(MainViewModel.HasProject), changedProperties);
        Assert.Contains(nameof(MainViewModel.ProjectTitle), changedProperties);
        Assert.Contains(nameof(MainViewModel.WindowTitle), changedProperties);
    }

    [Fact]
    public void HistoryEditAndSave_UpdateTitleWithoutResettingNavigation()
    {
        using ProjectSession session = new();
        using FileMenuViewModel fileMenu = CreateFileMenu(session);

        WorkspaceSelection selection = new();
        ProjectPanelViewModel projectPanel = new(selection);
        using MainViewModel viewModel = new(selection, projectPanel, session, fileMenu);

        Project project = new() { Title = "My story" };
        PassThroughProjectHistory history = new(project);
        string projectFolder = Path.Combine(Path.GetTempPath(), "NovellyMainViewModelTests");

        session.SetProject(history, projectFolder, isSaved: true);
        projectPanel.SelectCharactersCommand.Execute(null);

        List<string?> changedProperties = [];
        viewModel.PropertyChanged += (_, eventArgs) =>
            changedProperties.Add(eventArgs.PropertyName);

        history.Edit("Rename project", currentProject => currentProject.Title = "Updated story");

        Assert.Equal("Updated story", viewModel.ProjectTitle);
        Assert.Equal("Updated story * - Novelly", viewModel.WindowTitle);
        Assert.Equal("Characters", viewModel.SelectedSectionTitle);
        Assert.Contains(nameof(MainViewModel.ProjectTitle), changedProperties);
        Assert.Contains(nameof(MainViewModel.WindowTitle), changedProperties);

        changedProperties.Clear();

        long savePoint = session.CaptureSavePoint();
        bool accepted = session.TryMarkSaved(savePoint, projectFolder);

        Assert.True(accepted);
        Assert.Equal("Updated story - Novelly", viewModel.WindowTitle);
        Assert.Equal("Characters", viewModel.SelectedSectionTitle);
        Assert.Contains(nameof(MainViewModel.WindowTitle), changedProperties);
    }

    private static FileMenuViewModel CreateFileMenu(ProjectSession session)
    {
        IProjectFileAction action = Substitute.For<IProjectFileAction>();

        return new FileMenuViewModel(session, action, action, action, action, action);
    }
}