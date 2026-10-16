using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using VnEditor.AppService.Interaction;
using VnEditor.AppService.Persistence;
using VnEditor.AppService.Projects;
using VnEditor.AppService.Projects.Actions;
using VnEditor.AppService.Sessions;
using VnEditor.Domain;
using VnEditor.Infrastructure.History;

namespace VnEditor.Tests.AppService.Projects;

/// <summary>
/// Verifica l'apertura di un progetto con dialoghi e persistenza simulati.
/// Controlla la gestione delle modifiche non salvate e garantisce che
/// annullamento, errori o risultati obsoleti non sostituiscano il progetto corrente.
/// </summary>
public sealed class OpenProjectActionTests : IDisposable
{
    private readonly ProjectSession _session = new();
    private readonly IProjectDialogs _dialogs = Substitute.For<IProjectDialogs>();
    private readonly IProjectPersistence _persistence =
        Substitute.For<IProjectPersistence>();
    private readonly IProjectFileAction _saveAction =
        Substitute.For<IProjectFileAction>();

    private readonly string _currentFolder =
        Path.Combine(Path.GetTempPath(), "NovellyOpenActionTests", "current");

    private readonly string _selectedFolder =
        Path.Combine(Path.GetTempPath(), "NovellyOpenActionTests", "selected");

    private readonly Project _loadedProject = new() { Title = "Opened story" };
    private readonly OpenProjectAction _action;

    public OpenProjectActionTests()
    {
        _dialogs.ChooseProjectFolderAsync(Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult<string?>(_selectedFolder));

        _dialogs.ConfirmUnsavedChangesAsync(Arg.Any<string>())
            .Returns(Task.FromResult(UnsavedChangesChoice.Cancel));

        _dialogs.ShowErrorAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);

        _persistence.LoadAsync(_selectedFolder, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(_loadedProject));

        _saveAction.ExecuteAsync().Returns(Task.FromResult(false));

        PassThroughProjectHistoryFactory historyFactory = new();
        UnsavedChangesGuard unsavedChanges = new(_session, _dialogs, _saveAction);

        _action = new OpenProjectAction(
            _session, _dialogs, _persistence, historyFactory, unsavedChanges);
    }

    [Fact]
    public async Task CanceledSelection_PreservesCurrentProjectWithoutConfirmation()
    {
        PassThroughProjectHistory history = ActivateProject(_currentFolder);
        long? originalRevision = _session.CaptureCurrentRevision();

        _dialogs.ChooseProjectFolderAsync(Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult<string?>(null));

        bool opened = await _action.ExecuteAsync();

        Assert.False(opened);
        Assert.Same(history.CurrentProject, _session.Project);
        Assert.Equal(_currentFolder, _session.ProjectFolder);
        Assert.Equal(originalRevision, _session.CaptureCurrentRevision());
        Assert.True(_session.IsModified);

        await _dialogs.DidNotReceive()
            .ConfirmUnsavedChangesAsync(Arg.Any<string>());

        await _saveAction.DidNotReceive().ExecuteAsync();
        await AssertLoadNotCalledAsync();
    }

    [Fact]
    public async Task CanceledUnsavedChangesConfirmation_DoesNotLoadSelectedProject()
    {
        PassThroughProjectHistory history = ActivateProject(_currentFolder);

        bool opened = await _action.ExecuteAsync();

        Assert.False(opened);
        Assert.Same(history.CurrentProject, _session.Project);
        Assert.Equal(_currentFolder, _session.ProjectFolder);
        Assert.True(_session.IsModified);

        await _dialogs.Received(1).ConfirmUnsavedChangesAsync("My story");
        await _saveAction.DidNotReceive().ExecuteAsync();
        await AssertLoadNotCalledAsync();
    }

    [Fact]
    public async Task FailedSaveBeforeOpening_PreservesCurrentProject()
    {
        PassThroughProjectHistory history = ActivateProject(_currentFolder);

        _dialogs.ConfirmUnsavedChangesAsync("My story")
            .Returns(Task.FromResult(UnsavedChangesChoice.Save));

        bool opened = await _action.ExecuteAsync();

        Assert.False(opened);
        Assert.Same(history.CurrentProject, _session.Project);
        Assert.Equal(_currentFolder, _session.ProjectFolder);
        Assert.True(_session.IsModified);

        await _saveAction.Received(1).ExecuteAsync();
        await AssertLoadNotCalledAsync();
    }

    [Fact]
    public async Task DiscardThenLoadFailure_PreservesCurrentProjectAndDirtyState()
    {
        PassThroughProjectHistory history = ActivateProject(_currentFolder);
        long? originalRevision = _session.CaptureCurrentRevision();

        _dialogs.ConfirmUnsavedChangesAsync("My story")
            .Returns(Task.FromResult(UnsavedChangesChoice.Discard));

        IOException expected = new("Read failed.");

        _persistence.LoadAsync(_selectedFolder, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Project>(expected));

        IOException actual = await Assert.ThrowsAsync<IOException>(
            () => _action.ExecuteAsync());

        Assert.Same(expected, actual);
        Assert.Same(history.CurrentProject, _session.Project);
        Assert.Equal(_currentFolder, _session.ProjectFolder);
        Assert.Equal(originalRevision, _session.CaptureCurrentRevision());
        Assert.True(_session.IsModified);

        await _saveAction.DidNotReceive().ExecuteAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SuccessfulLoad_ActivatesProjectOnlyAfterCompletion(bool hasCurrentProject)
    {
        if (hasCurrentProject)
            ActivateProject(_currentFolder, isSaved: true);

        Project? originalProject = _session.Project;
        string? originalFolder = _session.ProjectFolder;

        TaskCompletionSource<Project> completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        _persistence.LoadAsync(_selectedFolder, Arg.Any<CancellationToken>())
            .Returns(completion.Task);

        Task<bool> execution = _action.ExecuteAsync();

        bool keptProjectDuringLoad = ReferenceEquals(originalProject, _session.Project);
        bool keptFolderDuringLoad = originalFolder == _session.ProjectFolder;
        bool completedBeforePersistence = execution.IsCompleted;

        completion.SetResult(_loadedProject);

        bool opened = await execution;

        Assert.True(keptProjectDuringLoad);
        Assert.True(keptFolderDuringLoad);
        Assert.False(completedBeforePersistence);

        Assert.True(opened);
        Assert.Same(_loadedProject, _session.Project);
        Assert.Equal(_selectedFolder, _session.ProjectFolder);
        Assert.False(_session.IsModified);

        await _persistence.Received(1)
            .LoadAsync(_selectedFolder, Arg.Any<CancellationToken>());

        await _dialogs.DidNotReceive()
            .ConfirmUnsavedChangesAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task SaveBeforeOpening_CompletesBeforeLoadingSelectedProject()
    {
        ActivateProject(_currentFolder);

        _dialogs.ConfirmUnsavedChangesAsync("My story")
            .Returns(Task.FromResult(UnsavedChangesChoice.Save));

        _saveAction.ExecuteAsync().Returns(_ =>
        {
            long revision = _session.CaptureSavePoint();
            bool accepted = _session.TryMarkSaved(revision, _currentFolder);

            return Task.FromResult(accepted);
        });

        bool wasSavedBeforeLoading = false;

        _persistence.LoadAsync(_selectedFolder, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                wasSavedBeforeLoading = !_session.IsModified;

                return Task.FromResult(_loadedProject);
            });

        bool opened = await _action.ExecuteAsync();

        Assert.True(opened);
        Assert.True(wasSavedBeforeLoading);
        Assert.Same(_loadedProject, _session.Project);
        Assert.Equal(_selectedFolder, _session.ProjectFolder);
        Assert.False(_session.IsModified);

        await _saveAction.Received(1).ExecuteAsync();

        await _persistence.Received(1)
            .LoadAsync(_selectedFolder, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EditDuringLoad_RejectsLoadedProject()
    {
        PassThroughProjectHistory history =
            ActivateProject(_currentFolder, isSaved: true);

        TaskCompletionSource<Project> completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        _persistence.LoadAsync(_selectedFolder, Arg.Any<CancellationToken>())
            .Returns(completion.Task);

        Task<bool> execution = _action.ExecuteAsync();

        history.Edit("Rename project", project => project.Title = "Updated story");
        completion.SetResult(_loadedProject);

        bool opened = await execution;

        Assert.False(opened);
        Assert.Same(history.CurrentProject, _session.Project);
        Assert.Equal("Updated story", _session.Project?.Title);
        Assert.Equal(_currentFolder, _session.ProjectFolder);
        Assert.True(_session.IsModified);

        await _dialogs.Received(1)
            .ShowErrorAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProjectReplacedDuringLoad_RejectsPreviousResult(bool replacementIsSaved)
    {
        ActivateProject(_currentFolder, isSaved: true);

        TaskCompletionSource<Project> completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        _persistence.LoadAsync(_selectedFolder, Arg.Any<CancellationToken>())
            .Returns(completion.Task);

        Task<bool> execution = _action.ExecuteAsync();

        string replacementFolder =
            Path.Combine(Path.GetTempPath(), "NovellyOpenActionTests", "replacement");

        PassThroughProjectHistory replacement =
            ActivateProject(replacementFolder, replacementIsSaved);

        long? replacementRevision = _session.CaptureCurrentRevision();

        completion.SetResult(_loadedProject);

        bool opened = await execution;

        Assert.False(opened);
        Assert.Same(replacement.CurrentProject, _session.Project);
        Assert.Equal(replacementFolder, _session.ProjectFolder);
        Assert.Equal(replacementRevision, _session.CaptureCurrentRevision());
        Assert.Equal(!replacementIsSaved, _session.IsModified);

        await _dialogs.Received(1)
            .ShowErrorAsync(Arg.Any<string>(), Arg.Any<string>());
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

    private Task AssertLoadNotCalledAsync()
    {
        return _persistence.DidNotReceive()
            .LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}