using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using VnEditor.AppService.Interaction;
using VnEditor.AppService.Projects;
using VnEditor.AppService.Projects.Actions;
using VnEditor.AppService.Sessions;
using VnEditor.Domain;
using VnEditor.Infrastructure.History;

namespace VnEditor.Tests.AppService.Projects;

/// <summary>
/// Verifica la scelta della destinazione e la delega di Save As al workspace.
/// Controlla annullamento, errori e aggiornamento della cartella dopo il successo,
/// conservando modifiche successive e proteggendo i progetti sostituiti.
/// </summary>
public sealed class SaveProjectAsActionTests : IDisposable
{
    private readonly ProjectSession _session = new();
    private readonly IProjectDialogs _dialogs = Substitute.For<IProjectDialogs>();
    private readonly IProjectWorkspace _workspace = Substitute.For<IProjectWorkspace>();

    private readonly string _sourceFolder =
        Path.Combine(Path.GetTempPath(), "NovellySaveAsActionTests", "source");

    private readonly string _destinationFolder =
        Path.Combine(Path.GetTempPath(), "NovellySaveAsActionTests", "destination");

    private readonly SaveProjectAsAction _action;

    public SaveProjectAsActionTests()
    {
        _dialogs.ChooseProjectFolderAsync(Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult<string?>(_destinationFolder));

        _dialogs.ShowErrorAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);

        _workspace.SaveAsAsync(
            Arg.Any<Project>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _action = new SaveProjectAsAction(_session, _dialogs, _workspace);
    }

    [Fact]
    public async Task EmptySession_DoesNotOpenDialogOrInvokeWorkspace()
    {
        bool saved = await _action.ExecuteAsync();

        Assert.False(saved);

        await _dialogs.DidNotReceive()
            .ChooseProjectFolderAsync(Arg.Any<string>(), Arg.Any<string?>());

        await AssertWorkspaceNotCalledAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CanceledSelection_PreservesSession(bool isSaved)
    {
        PassThroughProjectHistory history = ActivateProject(_sourceFolder, isSaved);
        long? originalRevision = _session.CaptureCurrentRevision();

        _dialogs.ChooseProjectFolderAsync(Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult<string?>(null));

        bool saved = await _action.ExecuteAsync();

        Assert.False(saved);
        Assert.Same(history.CurrentProject, _session.Project);
        Assert.Equal(_sourceFolder, _session.ProjectFolder);
        Assert.Equal(originalRevision, _session.CaptureCurrentRevision());
        Assert.Equal(!isSaved, _session.IsModified);

        await AssertWorkspaceNotCalledAsync();
    }

    [Fact]
    public async Task SuccessfulSave_UpdatesFolderOnlyAfterWorkspaceCompletes()
    {
        PassThroughProjectHistory history = ActivateProject(_sourceFolder);
        Project project = history.CurrentProject;
        TaskCompletionSource<bool> completion = DelaySave(project);

        Task<bool> execution = _action.ExecuteAsync();

        string? folderWhileSaving = _session.ProjectFolder;
        bool modifiedWhileSaving = _session.IsModified;
        bool completedBeforeWorkspace = execution.IsCompleted;

        completion.SetResult(true);

        bool saved = await execution;

        Assert.Equal(_sourceFolder, folderWhileSaving);
        Assert.True(modifiedWhileSaving);
        Assert.False(completedBeforeWorkspace);

        Assert.True(saved);
        Assert.Same(project, _session.Project);
        Assert.Equal(_destinationFolder, _session.ProjectFolder);
        Assert.False(_session.IsModified);

        await _dialogs.Received(1)
            .ChooseProjectFolderAsync("Save project as", _sourceFolder);

        await _workspace.Received(1).SaveAsAsync(
            project, _sourceFolder, _destinationFolder, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WorkspaceFailure_PreservesProjectFolderAndDirtyState()
    {
        PassThroughProjectHistory history = ActivateProject(_sourceFolder);
        Project project = history.CurrentProject;
        long? originalRevision = _session.CaptureCurrentRevision();

        IOException expected = new("Copy failed.");

        _workspace.SaveAsAsync(
            project, _sourceFolder, _destinationFolder, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(expected));

        IOException actual = await Assert.ThrowsAsync<IOException>(
            () => _action.ExecuteAsync());

        Assert.Same(expected, actual);
        Assert.Same(project, _session.Project);
        Assert.Equal(_sourceFolder, _session.ProjectFolder);
        Assert.Equal(originalRevision, _session.CaptureCurrentRevision());
        Assert.True(_session.IsModified);
    }

    [Fact]
    public async Task EditDuringSelection_StopsBeforeInvokingWorkspace()
    {
        PassThroughProjectHistory history = ActivateProject(_sourceFolder);

        TaskCompletionSource<string?> selection =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        _dialogs.ChooseProjectFolderAsync(Arg.Any<string>(), Arg.Any<string?>())
            .Returns(selection.Task);

        Task<bool> execution = _action.ExecuteAsync();

        history.Edit("Rename project", project => project.Title = "Updated story");
        selection.SetResult(_destinationFolder);

        bool saved = await execution;

        Assert.False(saved);
        Assert.Same(history.CurrentProject, _session.Project);
        Assert.Equal(_sourceFolder, _session.ProjectFolder);
        Assert.True(_session.IsModified);

        await AssertWorkspaceNotCalledAsync();

        await _dialogs.Received(1)
            .ShowErrorAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ProjectReplacedDuringSelection_StopsBeforeInvokingWorkspace()
    {
        ActivateProject(_sourceFolder);

        TaskCompletionSource<string?> selection =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        _dialogs.ChooseProjectFolderAsync(Arg.Any<string>(), Arg.Any<string?>())
            .Returns(selection.Task);

        Task<bool> execution = _action.ExecuteAsync();

        string replacementFolder =
            Path.Combine(Path.GetTempPath(), "NovellySaveAsActionTests", "replacement");

        PassThroughProjectHistory replacement =
            ActivateProject(replacementFolder, isSaved: true);

        selection.SetResult(_destinationFolder);

        bool saved = await execution;

        Assert.False(saved);
        Assert.Same(replacement.CurrentProject, _session.Project);
        Assert.Equal(replacementFolder, _session.ProjectFolder);
        Assert.False(_session.IsModified);

        await AssertWorkspaceNotCalledAsync();

        await _dialogs.Received(1)
            .ShowErrorAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task EditDuringSave_UpdatesDestinationButPreservesDirtyState()
    {
        PassThroughProjectHistory history = ActivateProject(_sourceFolder);
        Project project = history.CurrentProject;
        TaskCompletionSource<bool> completion = DelaySave(project);

        Task<bool> execution = _action.ExecuteAsync();

        history.Edit("Rename project", currentProject => currentProject.Title = "Updated story");
        completion.SetResult(true);

        bool saved = await execution;

        Assert.True(saved);
        Assert.Same(project, _session.Project);
        Assert.Equal(_destinationFolder, _session.ProjectFolder);
        Assert.Equal("Updated story", _session.Project?.Title);
        Assert.True(_session.IsModified);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProjectReplacedDuringSave_DoesNotApplyPreviousResult(bool replacementIsSaved)
    {
        PassThroughProjectHistory originalHistory = ActivateProject(_sourceFolder);
        TaskCompletionSource<bool> completion = DelaySave(originalHistory.CurrentProject);

        Task<bool> execution = _action.ExecuteAsync();

        string replacementFolder =
            Path.Combine(Path.GetTempPath(), "NovellySaveAsActionTests", "replacement");

        PassThroughProjectHistory replacement =
            ActivateProject(replacementFolder, replacementIsSaved);

        long? replacementRevision = _session.CaptureCurrentRevision();

        completion.SetResult(true);

        bool saved = await execution;

        Assert.False(saved);
        Assert.Same(replacement.CurrentProject, _session.Project);
        Assert.Equal(replacementFolder, _session.ProjectFolder);
        Assert.Equal(replacementRevision, _session.CaptureCurrentRevision());
        Assert.Equal(!replacementIsSaved, _session.IsModified);
    }

    public void Dispose()
    {
        _session.Dispose();
    }

    private PassThroughProjectHistory ActivateProject(string projectFolder, bool isSaved = false)
    {
        Project project = new() { Title = "My story" };
        PassThroughProjectHistory history = new(project);

        _session.SetProject(history, projectFolder, isSaved);

        return history;
    }

    private TaskCompletionSource<bool> DelaySave(Project project)
    {
        TaskCompletionSource<bool> completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        _workspace.SaveAsAsync(
            project, _sourceFolder, _destinationFolder, Arg.Any<CancellationToken>())
            .Returns(completion.Task);

        return completion;
    }

    private Task AssertWorkspaceNotCalledAsync()
    {
        return _workspace.DidNotReceive().SaveAsAsync(
            Arg.Any<Project>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}