using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using VnEditor.AppService.Persistence;
using VnEditor.AppService.Projects.Actions;
using VnEditor.AppService.Sessions;
using VnEditor.Domain;
using VnEditor.Infrastructure.History;

namespace VnEditor.Tests.AppService.Projects;

/// <summary>
/// Verifica che Save deleghi alla persistenza il progetto e la cartella correnti.
/// Controlla che la sessione venga aggiornata soltanto dopo il successo,
/// conservando le modifiche successive e ignorando risultati di progetti sostituiti.
/// </summary>
public sealed class SaveProjectActionTests : IDisposable
{
    private readonly ProjectSession _session = new();
    private readonly IProjectPersistence _persistence =
        Substitute.For<IProjectPersistence>();

    private readonly string _projectFolder =
        Path.Combine(Path.GetTempPath(), "NovellySaveActionTests", "original");

    private readonly SaveProjectAction _action;

    public SaveProjectActionTests()
    {
        _persistence.SaveAsync(
            Arg.Any<Project>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _action = new SaveProjectAction(_session, _persistence);
    }

    [Fact]
    public async Task EmptySession_DoesNotInvokePersistence()
    {
        bool saved = await _action.ExecuteAsync();

        Assert.False(saved);
        Assert.False(_session.HasProject);

        await _persistence.DidNotReceive().SaveAsync(
            Arg.Any<Project>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SuccessfulSave_MarksProjectSavedOnlyAfterPersistenceCompletes()
    {
        PassThroughProjectHistory history = ActivateProject(_projectFolder);
        Project project = history.CurrentProject;

        TaskCompletionSource<bool> completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        _persistence.SaveAsync(project, _projectFolder, Arg.Any<CancellationToken>())
            .Returns(completion.Task);

        Task<bool> execution = _action.ExecuteAsync();

        bool wasModifiedWhileSaving = _session.IsModified;
        bool completedBeforePersistence = execution.IsCompleted;

        completion.SetResult(true);

        bool saved = await execution;

        Assert.True(wasModifiedWhileSaving);
        Assert.False(completedBeforePersistence);
        Assert.True(saved);
        Assert.False(_session.IsModified);
        Assert.Same(project, _session.Project);
        Assert.Equal(_projectFolder, _session.ProjectFolder);

        await _persistence.Received(1)
            .SaveAsync(project, _projectFolder, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PersistenceFailure_PreservesProjectFolderAndDirtyState()
    {
        PassThroughProjectHistory history = ActivateProject(_projectFolder);
        Project project = history.CurrentProject;
        long? originalRevision = _session.CaptureCurrentRevision();

        IOException expected = new("Write failed.");

        _persistence.SaveAsync(project, _projectFolder, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(expected));

        IOException actual = await Assert.ThrowsAsync<IOException>(
            () => _action.ExecuteAsync());

        Assert.Same(expected, actual);
        Assert.Same(project, _session.Project);
        Assert.Equal(_projectFolder, _session.ProjectFolder);
        Assert.Equal(originalRevision, _session.CaptureCurrentRevision());
        Assert.True(_session.IsModified);
    }

    [Fact]
    public async Task CanceledPersistence_PreservesDirtyState()
    {
        PassThroughProjectHistory history = ActivateProject(_projectFolder);
        Project project = history.CurrentProject;
        CancellationToken canceledToken = new(canceled: true);

        _persistence.SaveAsync(project, _projectFolder, Arg.Any<CancellationToken>())
            .Returns(Task.FromCanceled(canceledToken));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _action.ExecuteAsync());

        Assert.True(_session.IsModified);
        Assert.Same(project, _session.Project);
        Assert.Equal(_projectFolder, _session.ProjectFolder);
    }

    [Fact]
    public async Task EditDuringSave_RemainsUnsavedAfterPersistenceCompletes()
    {
        PassThroughProjectHistory history = ActivateProject(_projectFolder);
        Project project = history.CurrentProject;

        TaskCompletionSource<bool> completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        _persistence.SaveAsync(project, _projectFolder, Arg.Any<CancellationToken>())
            .Returns(completion.Task);

        Task<bool> execution = _action.ExecuteAsync();

        history.Edit("Rename project", currentProject => currentProject.Title = "Updated story");
        completion.SetResult(true);

        bool saved = await execution;

        Assert.False(saved);
        Assert.True(_session.IsModified);
        Assert.Same(project, _session.Project);
        Assert.Equal("Updated story", _session.Project?.Title);
        Assert.Equal(_projectFolder, _session.ProjectFolder);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProjectReplacedDuringSave_DoesNotApplyPreviousResult(bool replacementIsSaved)
    {
        PassThroughProjectHistory originalHistory = ActivateProject(_projectFolder);
        Project originalProject = originalHistory.CurrentProject;

        TaskCompletionSource<bool> completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        _persistence.SaveAsync(originalProject, _projectFolder, Arg.Any<CancellationToken>())
            .Returns(completion.Task);

        Task<bool> execution = _action.ExecuteAsync();

        string replacementFolder =
            Path.Combine(Path.GetTempPath(), "NovellySaveActionTests", "replacement");

        PassThroughProjectHistory replacementHistory =
            ActivateProject(replacementFolder, replacementIsSaved);

        long? replacementRevision = _session.CaptureCurrentRevision();

        completion.SetResult(true);

        bool saved = await execution;

        Assert.False(saved);
        Assert.Same(replacementHistory.CurrentProject, _session.Project);
        Assert.Equal(replacementFolder, _session.ProjectFolder);
        Assert.Equal(replacementRevision, _session.CaptureCurrentRevision());
        Assert.Equal(!replacementIsSaved, _session.IsModified);

        await _persistence.Received(1)
            .SaveAsync(originalProject, _projectFolder, Arg.Any<CancellationToken>());

        await _persistence.DidNotReceive().SaveAsync(
            replacementHistory.CurrentProject,
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
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
}