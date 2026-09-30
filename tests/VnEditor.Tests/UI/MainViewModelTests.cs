using VnEditor.UI.Selection;
using VnEditor.UI.ViewModels;

namespace VnEditor.Tests.UI;

/// <summary>
/// Verifica che il ViewModel principale rifletta e notifichi i cambiamenti della selezione condivisa.
/// </summary>
public sealed class MainViewModelTests
{
    [Fact]
    public void NavigationAndClear_UpdateSelectedSectionTitle()
    {
        WorkspaceSelection selection = new();
        selection.SelectSection(ProjectSection.Scenes);

        ProjectPanelViewModel projectPanel = new(selection);
        MainViewModel viewModel = new(selection, projectPanel);
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
}