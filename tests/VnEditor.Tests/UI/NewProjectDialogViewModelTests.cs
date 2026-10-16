using System;
using System.IO;
using System.Threading.Tasks;
using NSubstitute;
using VnEditor.AppService.Interaction;
using VnEditor.UI.ViewModels;

namespace VnEditor.Tests.UI;

/// <summary>
/// Verifica i campi obbligatori del dialogo New project e la restituzione
/// del nome e della cartella genitore scelti dall'utente.
/// Controlla conferma, annullamento, selezione della posizione ed errori.
/// </summary>
public sealed class NewProjectDialogViewModelTests
{
    private readonly IProjectDialogs _dialogs = Substitute.For<IProjectDialogs>();

    private readonly string _initialFolder =
        Path.Combine(Path.GetTempPath(), "NovellyNewDialogTests", "initial");

    private readonly string _selectedFolder =
        Path.Combine(Path.GetTempPath(), "NovellyNewDialogTests", "selected");

    private readonly NewProjectDialogViewModel _viewModel;

    public NewProjectDialogViewModelTests()
    {
        _dialogs.ChooseProjectFolderAsync(Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult<string?>(null));

        _dialogs.ShowErrorAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);

        _viewModel = new NewProjectDialogViewModel(_dialogs);
    }

    [Theory]
    [InlineData("", "story-folder")]
    [InlineData("   ", "story-folder")]
    [InlineData("My story", "")]
    [InlineData("My story", "   ")]
    public void InvalidFields_DisableCreateAndPreventClosing(string projectName, string parentFolder)
    {
        _viewModel.ProjectName = projectName;
        _viewModel.ParentFolder = parentFolder;

        int closeRequests = 0;
        _viewModel.CloseRequested += _ => closeRequests++;

        Assert.False(_viewModel.CreateCommand.CanExecute(null));

        _viewModel.CreateCommand.Execute(null);

        Assert.Equal(0, closeRequests);
    }

    [Fact]
    public void FieldChanges_UpdateCreateAvailabilityAndNotifyBindings()
    {
        int notifications = 0;
        _viewModel.CreateCommand.CanExecuteChanged += (_, _) => notifications++;

        Assert.False(_viewModel.CreateCommand.CanExecute(null));

        _viewModel.ProjectName = "My story";

        Assert.False(_viewModel.CreateCommand.CanExecute(null));

        _viewModel.ParentFolder = _initialFolder;

        Assert.True(_viewModel.CreateCommand.CanExecute(null));

        _viewModel.ParentFolder = string.Empty;

        Assert.False(_viewModel.CreateCommand.CanExecute(null));
        Assert.True(notifications > 0);
    }

    [Fact]
    public void Create_ReturnsTrimmedNameAndParentFolder()
    {
        _viewModel.ProjectName = "  My story  ";
        _viewModel.ParentFolder = $"  {_initialFolder}  ";

        NewProjectRequest? result = null;
        int closeRequests = 0;

        _viewModel.CloseRequested += request =>
        {
            result = request;
            closeRequests++;
        };

        Assert.True(_viewModel.CreateCommand.CanExecute(null));

        _viewModel.CreateCommand.Execute(null);

        Assert.Equal(1, closeRequests);
        Assert.NotNull(result);
        Assert.Equal("My story", result.ProjectName);
        Assert.Equal(_initialFolder, result.ParentFolder);
    }

    [Fact]
    public void Cancel_RequestsClosingWithoutCreatingProject()
    {
        FillValidFields();

        NewProjectRequest? result = null;
        int closeRequests = 0;

        _viewModel.CloseRequested += request =>
        {
            result = request;
            closeRequests++;
        };

        _viewModel.CancelCommand.Execute(null);

        Assert.Equal(1, closeRequests);
        Assert.Null(result);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Browse_UpdatesParentFolderAndUsesCurrentLocationAsSuggestion(bool hasInitialFolder)
    {
        if (hasInitialFolder)
            _viewModel.ParentFolder = _initialFolder;

        _dialogs.ChooseProjectFolderAsync(Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult<string?>(_selectedFolder));

        int closeRequests = 0;
        _viewModel.CloseRequested += _ => closeRequests++;

        await _viewModel.BrowseCommand.ExecuteAsync(null);

        string? expectedSuggestion = hasInitialFolder ? _initialFolder : null;

        Assert.Equal(_selectedFolder, _viewModel.ParentFolder);
        Assert.False(_viewModel.IsBusy);
        Assert.Equal(0, closeRequests);

        await _dialogs.Received(1)
            .ChooseProjectFolderAsync("Choose project location", expectedSuggestion);
    }

    [Fact]
    public async Task CanceledBrowse_PreservesEnteredFolderAndKeepsDialogOpen()
    {
        FillValidFields();

        int closeRequests = 0;
        _viewModel.CloseRequested += _ => closeRequests++;

        await _viewModel.BrowseCommand.ExecuteAsync(null);

        Assert.Equal(_initialFolder, _viewModel.ParentFolder);
        Assert.False(_viewModel.IsBusy);
        Assert.True(_viewModel.CreateCommand.CanExecute(null));
        Assert.Equal(0, closeRequests);

        await _dialogs.DidNotReceive()
            .ShowErrorAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task PendingBrowse_BlocksCommandsUntilSelectionCompletes()
    {
        FillValidFields();

        TaskCompletionSource<string?> selection =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        _dialogs.ChooseProjectFolderAsync(Arg.Any<string>(), Arg.Any<string?>())
            .Returns(selection.Task);

        int closeRequests = 0;
        _viewModel.CloseRequested += _ => closeRequests++;

        Task execution = _viewModel.BrowseCommand.ExecuteAsync(null);

        try
        {
            Assert.True(_viewModel.IsBusy);
            Assert.False(_viewModel.CreateCommand.CanExecute(null));
            Assert.False(_viewModel.CancelCommand.CanExecute(null));
            Assert.False(_viewModel.BrowseCommand.CanExecute(null));

            _viewModel.CreateCommand.Execute(null);
            _viewModel.CancelCommand.Execute(null);

            Assert.Equal(0, closeRequests);

            await _dialogs.Received(1)
                .ChooseProjectFolderAsync("Choose project location", _initialFolder);
        }
        finally
        {
            selection.TrySetResult(_selectedFolder);
            await execution;
        }

        Assert.False(_viewModel.IsBusy);
        Assert.Equal(_selectedFolder, _viewModel.ParentFolder);
        Assert.True(_viewModel.CreateCommand.CanExecute(null));
        Assert.True(_viewModel.CancelCommand.CanExecute(null));
        Assert.True(_viewModel.BrowseCommand.CanExecute(null));
    }

    [Theory]
    [InlineData("io")]
    [InlineData("permission")]
    [InlineData("unsupported")]
    public async Task ExpectedBrowseError_ShowsMessageAndPreservesFolder(string errorKind)
    {
        FillValidFields();

        Exception expected = errorKind switch
        {
            "io" => new IOException("Folder selection failed."),
            "permission" => new UnauthorizedAccessException("Access denied."),
            "unsupported" => new NotSupportedException("Folder selection is unavailable."),
            _ => throw new ArgumentOutOfRangeException(nameof(errorKind))
        };

        _dialogs.ChooseProjectFolderAsync(Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromException<string?>(expected));

        int closeRequests = 0;
        _viewModel.CloseRequested += _ => closeRequests++;

        await _viewModel.BrowseCommand.ExecuteAsync(null);

        Assert.Equal(_initialFolder, _viewModel.ParentFolder);
        Assert.False(_viewModel.IsBusy);
        Assert.True(_viewModel.CreateCommand.CanExecute(null));
        Assert.True(_viewModel.CancelCommand.CanExecute(null));
        Assert.True(_viewModel.BrowseCommand.CanExecute(null));
        Assert.Equal(0, closeRequests);

        await _dialogs.Received(1).ShowErrorAsync(
            "Unable to choose a folder",
            "The folder could not be selected. Please try again.");
    }

    [Fact]
    public async Task UnexpectedBrowseError_PropagatesAndRestoresInteraction()
    {
        FillValidFields();

        InvalidOperationException expected = new("Unexpected failure.");

        _dialogs.ChooseProjectFolderAsync(Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromException<string?>(expected));

        InvalidOperationException actual =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _viewModel.BrowseCommand.ExecuteAsync(null));

        Assert.Same(expected, actual);
        Assert.Equal(_initialFolder, _viewModel.ParentFolder);
        Assert.False(_viewModel.IsBusy);
        Assert.True(_viewModel.CreateCommand.CanExecute(null));
        Assert.True(_viewModel.CancelCommand.CanExecute(null));
        Assert.True(_viewModel.BrowseCommand.CanExecute(null));

        await _dialogs.DidNotReceive()
            .ShowErrorAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    private void FillValidFields()
    {
        _viewModel.ProjectName = "My story";
        _viewModel.ParentFolder = _initialFolder;
    }
}