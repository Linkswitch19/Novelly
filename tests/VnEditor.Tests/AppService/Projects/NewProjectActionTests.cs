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
/// Verifica che New determini una sottocartella con il nome del progetto
/// all'interno della posizione scelta e ne deleghi la creazione al workspace.
/// Controlla nomi non accettabili, annullamento, modifiche non salvate
/// e protezione della sessione durante la creazione.
/// </summary>
public sealed class NewProjectActionTests : IDisposable
{
    private readonly ProjectSession _session = new();
    private readonly IProjectDialogs _dialogs = Substitute.For<IProjectDialogs>();
    private readonly IProjectWorkspace _workspace = Substitute.For<IProjectWorkspace>();
    private readonly IProjectFileAction _saveAction =
        Substitute.For<IProjectFileAction>();

    private readonly string _currentFolder =
        Path.Combine(Path.GetTempPath(), "NovellyNewActionTests", "current");

    private readonly string _parentFolder =
        Path.Combine(Path.GetTempPath(), "NovellyNewActionTests", "location");

    private readonly string _newFolder;
    private readonly Project _createdProject = new() { Title = "New story" };
    private readonly NewProjectAction _action;

    public NewProjectActionTests()
    {
        _newFolder = Path.Combine(_parentFolder, "New story");

        NewProjectRequest request = new("New story", _parentFolder);

        _dialogs.ShowNewProjectAsync()
            .Returns(Task.FromResult<NewProjectRequest?>(request));

        _dialogs.ConfirmUnsavedChangesAsync(Arg.Any<string>())
            .Returns(Task.FromResult(UnsavedChangesChoice.Cancel));

        _dialogs.ShowErrorAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);

        _workspace.CreateAsync("New story", _newFolder, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(_createdProject));

        _saveAction.ExecuteAsync().Returns(Task.FromResult(false));

        PassThroughProjectHistoryFactory historyFactory = new();
        UnsavedChangesGuard unsavedChanges = new(_session, _dialogs, _saveAction);

        _action = new NewProjectAction(
            _session, _dialogs, _workspace, historyFactory, unsavedChanges);
    }

    [Fact]
    public async Task CanceledDialog_PreservesSessionWithoutUnsavedChangesConfirmation()
    {
        PassThroughProjectHistory history = ActivateProject(_currentFolder);
        long? originalRevision = _session.CaptureCurrentRevision();

        _dialogs.ShowNewProjectAsync()
            .Returns(Task.FromResult<NewProjectRequest?>(null));

        bool created = await _action.ExecuteAsync();

        Assert.False(created);
        Assert.Same(history.CurrentProject, _session.Project);
        Assert.Equal(_currentFolder, _session.ProjectFolder);
        Assert.Equal(originalRevision, _session.CaptureCurrentRevision());
        Assert.True(_session.IsModified);

        await _dialogs.DidNotReceive()
            .ConfirmUnsavedChangesAsync(Arg.Any<string>());

        await _saveAction.DidNotReceive().ExecuteAsync();
        await AssertCreationNotCalledAsync();
    }

    [Theory]
    [InlineData("k", "k")]
    [InlineData("  New story  ", "New story")]
    [InlineData("My.story", "My.story")]
    public async Task ValidName_CreatesNamedSubfolderInsideSelectedLocation(
        string enteredName, string expectedName)
    {
        NewProjectRequest request = new(enteredName, _parentFolder);

        _dialogs.ShowNewProjectAsync()
            .Returns(Task.FromResult<NewProjectRequest?>(request));

        string expectedFolder = Path.GetFullPath(Path.Combine(_parentFolder, expectedName));
        Project expectedProject = new() { Title = expectedName };

        _workspace.CreateAsync(expectedName, expectedFolder, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedProject));

        bool created = await _action.ExecuteAsync();

        Assert.True(created);
        Assert.Same(expectedProject, _session.Project);
        Assert.Equal(expectedFolder, _session.ProjectFolder);
        Assert.True(_session.IsModified);

        await _workspace.Received(1)
            .CreateAsync(expectedName, expectedFolder, Arg.Any<CancellationToken>());

        await _workspace.DidNotReceive()
            .CreateAsync(Arg.Any<string>(), _parentFolder, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("../outside")]
    [InlineData(@"..\outside")]
    [InlineData("nested/story")]
    [InlineData(@"nested\story")]
    [InlineData("/outside")]
    [InlineData(@"C:\outside")]
    [InlineData("story.")]
    [InlineData("bad\0name")]
    public async Task InvalidName_IsRejectedBeforeUnsavedChangesConfirmation(string projectName)
    {
        PassThroughProjectHistory history = ActivateProject(_currentFolder);
        long? originalRevision = _session.CaptureCurrentRevision();

        NewProjectRequest request = new(projectName, _parentFolder);

        _dialogs.ShowNewProjectAsync()
            .Returns(Task.FromResult<NewProjectRequest?>(request));

        bool created = await _action.ExecuteAsync();

        Assert.False(created);
        Assert.Same(history.CurrentProject, _session.Project);
        Assert.Equal(_currentFolder, _session.ProjectFolder);
        Assert.Equal(originalRevision, _session.CaptureCurrentRevision());
        Assert.True(_session.IsModified);

        await _dialogs.Received(1).ShowErrorAsync(
            "Invalid project name",
            "Choose a project name that can be used as a folder name. Do not enter a path.");

        await _dialogs.DidNotReceive()
            .ConfirmUnsavedChangesAsync(Arg.Any<string>());

        await _saveAction.DidNotReceive().ExecuteAsync();
        await AssertCreationNotCalledAsync();
    }

    [Theory]
    [InlineData(UnsavedChangesChoice.Cancel, 0)]
    [InlineData(UnsavedChangesChoice.Save, 1)]
    public async Task LeavingDenied_DoesNotCreateProject(
        UnsavedChangesChoice choice, int expectedSaveCalls)
    {
        PassThroughProjectHistory history = ActivateProject(_currentFolder);

        _dialogs.ConfirmUnsavedChangesAsync("My story")
            .Returns(Task.FromResult(choice));

        bool created = await _action.ExecuteAsync();

        Assert.False(created);
        Assert.Same(history.CurrentProject, _session.Project);
        Assert.Equal(_currentFolder, _session.ProjectFolder);
        Assert.True(_session.IsModified);

        await _saveAction.Received(expectedSaveCalls).ExecuteAsync();
        await AssertCreationNotCalledAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SuccessfulCreation_ActivatesUnsavedProjectAfterCompletion(bool hasCurrentProject)
    {
        if (hasCurrentProject)
            ActivateProject(_currentFolder, isSaved: true);

        Project? originalProject = _session.Project;
        string? originalFolder = _session.ProjectFolder;

        TaskCompletionSource<Project> completion = DelayCreation();
        Task<bool> execution = _action.ExecuteAsync();

        bool keptProjectDuringCreation = ReferenceEquals(originalProject, _session.Project);
        bool keptFolderDuringCreation = originalFolder == _session.ProjectFolder;
        bool completedBeforeWorkspace = execution.IsCompleted;

        completion.SetResult(_createdProject);

        bool created = await execution;

        Assert.True(keptProjectDuringCreation);
        Assert.True(keptFolderDuringCreation);
        Assert.False(completedBeforeWorkspace);

        Assert.True(created);
        Assert.Same(_createdProject, _session.Project);
        Assert.Equal(_newFolder, _session.ProjectFolder);
        Assert.True(_session.IsModified);

        await _workspace.Received(1)
            .CreateAsync("New story", _newFolder, Arg.Any<CancellationToken>());

        await _dialogs.DidNotReceive()
            .ConfirmUnsavedChangesAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task DiscardThenCreationFailure_PreservesCurrentProjectAndDirtyState()
    {
        PassThroughProjectHistory history = ActivateProject(_currentFolder);
        long? originalRevision = _session.CaptureCurrentRevision();

        _dialogs.ConfirmUnsavedChangesAsync("My story")
            .Returns(Task.FromResult(UnsavedChangesChoice.Discard));

        IOException expected = new("Project creation failed.");

        _workspace.CreateAsync("New story", _newFolder, Arg.Any<CancellationToken>())
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

    [Fact]
    public async Task SaveBeforeCreating_CompletesBeforeWorkspaceIsInvoked()
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

        bool wasSavedBeforeCreation = false;

        _workspace.CreateAsync("New story", _newFolder, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                wasSavedBeforeCreation = !_session.IsModified;

                return Task.FromResult(_createdProject);
            });

        bool created = await _action.ExecuteAsync();

        Assert.True(created);
        Assert.True(wasSavedBeforeCreation);
        Assert.Same(_createdProject, _session.Project);
        Assert.Equal(_newFolder, _session.ProjectFolder);
        Assert.True(_session.IsModified);

        await _saveAction.Received(1).ExecuteAsync();
    }

    [Fact]
    public async Task EditDuringCreation_RejectsCreatedProject()
    {
        PassThroughProjectHistory history =
            ActivateProject(_currentFolder, isSaved: true);

        TaskCompletionSource<Project> completion = DelayCreation();
        Task<bool> execution = _action.ExecuteAsync();

        history.Edit("Rename project", project => project.Title = "Updated story");
        completion.SetResult(_createdProject);

        bool created = await execution;

        Assert.False(created);
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
    public async Task SessionChangedDuringCreation_RejectsPreviousResult(bool hasCurrentProject)
    {
        if (hasCurrentProject)
            ActivateProject(_currentFolder, isSaved: true);

        TaskCompletionSource<Project> completion = DelayCreation();
        Task<bool> execution = _action.ExecuteAsync();

        string replacementFolder =
            Path.Combine(Path.GetTempPath(), "NovellyNewActionTests", "replacement");

        PassThroughProjectHistory replacement = ActivateProject(replacementFolder);
        long? replacementRevision = _session.CaptureCurrentRevision();

        completion.SetResult(_createdProject);

        bool created = await execution;

        Assert.False(created);
        Assert.Same(replacement.CurrentProject, _session.Project);
        Assert.Equal(replacementFolder, _session.ProjectFolder);
        Assert.Equal(replacementRevision, _session.CaptureCurrentRevision());
        Assert.True(_session.IsModified);

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

    private TaskCompletionSource<Project> DelayCreation()
    {
        TaskCompletionSource<Project> completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        _workspace.CreateAsync("New story", _newFolder, Arg.Any<CancellationToken>())
            .Returns(completion.Task);

        return completion;
    }

    private Task AssertCreationNotCalledAsync()
    {
        return _workspace.DidNotReceive().CreateAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}