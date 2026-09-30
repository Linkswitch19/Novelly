using VnEditor.UI.Selection;

namespace VnEditor.Tests.UI;

/// <summary>
/// Verifica che la selezione condivisa notifichi i cambiamenti e mantenga coerenti sezione, scena e blocco.
/// </summary>
public sealed class WorkspaceSelectionTests
{
    [Fact]
    public void SelectBlock_UpdatesStateAndNotifiesObservers()
    {
        WorkspaceSelection selection = new();
        string? changedProperty = null;
        selection.PropertyChanged += (_, eventArgs) => changedProperty = eventArgs.PropertyName;

        selection.SelectBlock("scene-1", "block-1");

        Assert.Equal(ProjectSection.Scenes, selection.Current.Section);
        Assert.Equal("scene-1", selection.Current.SceneId);
        Assert.Equal("block-1", selection.Current.BlockId);
        Assert.Equal(nameof(WorkspaceSelection.Current), changedProperty);
    }

    [Fact]
    public void SelectSection_ClearsPreviouslySelectedSceneAndBlock()
    {
        WorkspaceSelection selection = new();
        selection.SelectBlock("scene-1", "block-1");

        selection.SelectSection(ProjectSection.Characters);

        Assert.Equal(ProjectSection.Characters, selection.Current.Section);
        Assert.Null(selection.Current.SceneId);
        Assert.Null(selection.Current.BlockId);
    }

    [Fact]
    public void Clear_RemovesTheCurrentSelection()
    {
        WorkspaceSelection selection = new();
        selection.SelectScene("scene-1");

        selection.Clear();

        Assert.Same(SelectionState.Empty, selection.Current);
    }
}