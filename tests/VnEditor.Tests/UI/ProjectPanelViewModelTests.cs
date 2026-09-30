using VnEditor.UI.Selection;
using VnEditor.UI.ViewModels;

namespace VnEditor.Tests.UI;

/// <summary>
/// Verifica che i comandi del pannello progetto aggiornino la selezione condivisa e lo stato visibile della navigazione.
/// </summary>
public sealed class ProjectPanelViewModelTests
{
    [Fact]
    public void SelectCharactersCommand_UpdatesSharedSelection()
    {
        WorkspaceSelection selection = new();
        selection.SelectScene("scene-1");
        ProjectPanelViewModel viewModel = new(selection);

        viewModel.SelectCharactersCommand.Execute(null);

        Assert.Equal(ProjectSection.Characters, selection.Current.Section);
        Assert.Null(selection.Current.SceneId);
        Assert.True(viewModel.IsCharactersSelected);
        Assert.False(viewModel.IsScenesSelected);
    }

    [Fact]
    public void ExternalSelectionChange_UpdatesSelectedCategory()
    {
        WorkspaceSelection selection = new();
        selection.SelectSection(ProjectSection.Scenes);
        ProjectPanelViewModel viewModel = new(selection);
        List<string?> changedProperties = new();
        viewModel.PropertyChanged += (_, eventArgs) => changedProperties.Add(eventArgs.PropertyName);

        selection.SelectSection(ProjectSection.Music);

        Assert.True(viewModel.IsMusicSelected);
        Assert.False(viewModel.IsScenesSelected);
        Assert.Contains(nameof(ProjectPanelViewModel.IsMusicSelected), changedProperties);
    }

   
}