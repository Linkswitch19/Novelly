using System;
using System.IO;
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
/// Verifica le decisioni sulle modifiche non salvate prima di lasciare un progetto.
/// Controlla Save, Discard e Cancel, i salvataggi non riusciti e i cambiamenti
/// della sessione durante la conferma o il salvataggio.
/// </summary>
public sealed class UnsavedChangesGuardTests : IDisposable
{
    private readonly ProjectSession _session = new();
    private readonly IProjectDialogs _dialogs = Substitute.For<IProjectDialogs>();
    private readonly IProjectFileAction _saveAction = Substitute.For<IProjectFileAction>();

    private readonly string _projectFolder =
        Path.Combine(Path.GetTempPath(), "NovellyUnsavedChangesTests");

    private readonly UnsavedChangesGuard _guard;

    public UnsavedChangesGuardTests()
    {
        _saveAction.ExecuteAsync().Returns(Task.FromResult(false));

        _dialogs.ShowErrorAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);

        _guard = new UnsavedChangesGuard(_session, _dialogs, _saveAction);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnmodifiedSession_AllowsLeavingWithoutConfirmation(bool hasProject)
    {
        if (hasProject)
            ActivateProject(isSaved: true);

        bool canLeave = await _guard.CanLeaveAsync();

        Assert.True(canLeave);

        await _dialogs.DidNotReceive()
            .ConfirmUnsavedChangesAsync(Arg.Any<string>());

        await _saveAction.DidNotReceive().ExecuteAsync();
    }

    [Theory]
    [InlineData(UnsavedChangesChoice.Cancel, false)]
    [InlineData(UnsavedChangesChoice.Discard, true)]
    public async Task CancelOrDiscard_PreservesProjectAndDirtyState(
        UnsavedChangesChoice choice, bool expectedResult)
    {
        PassThroughProjectHistory history = ActivateProject();

        _dialogs.ConfirmUnsavedChangesAsync("My story")
            .Returns(Task.FromResult(choice));

        long? originalRevision = _session.CaptureCurrentRevision();

        bool canLeave = await _guard.CanLeaveAsync();

        Assert.Equal(expectedResult, canLeave);
        Assert.Same(history.CurrentProject, _session.Project);
        Assert.True(_session.IsModified);
        Assert.Equal(originalRevision, _session.CaptureCurrentRevision());

        await _dialogs.Received(1).ConfirmUnsavedChangesAsync("My story");
        await _saveAction.DidNotReceive().ExecuteAsync();
    }

    [Fact]
    public async Task SaveSucceeded_AllowsLeavingWhenCurrentRevisionIsSaved()
    {
        ActivateProject();

        _dialogs.ConfirmUnsavedChangesAsync("My story")
            .Returns(Task.FromResult(UnsavedChangesChoice.Save));

        _saveAction.ExecuteAsync().Returns(_ =>
        {
            long revision = _session.CaptureSavePoint();
            bool accepted = _session.TryMarkSaved(revision, _projectFolder);

            return Task.FromResult(accepted);
        });

        bool canLeave = await _guard.CanLeaveAsync();

        Assert.True(canLeave);
        Assert.False(_session.IsModified);

        await _saveAction.Received(1).ExecuteAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveWithoutUpdatingSession_DoesNotAllowLeaving(bool reportedSuccess)
    {
        PassThroughProjectHistory history = ActivateProject();

        _dialogs.ConfirmUnsavedChangesAsync("My story")
            .Returns(Task.FromResult(UnsavedChangesChoice.Save));

        _saveAction.ExecuteAsync().Returns(Task.FromResult(reportedSuccess));

        bool canLeave = await _guard.CanLeaveAsync();

        Assert.False(canLeave);
        Assert.True(_session.IsModified);
        Assert.Same(history.CurrentProject, _session.Project);

        await _saveAction.Received(1).ExecuteAsync();
    }

    [Theory]
    [InlineData(UnsavedChangesChoice.Save)]
    [InlineData(UnsavedChangesChoice.Discard)]
    public async Task ProjectEditedDuringConfirmation_RejectsPreviousDecision(
        UnsavedChangesChoice choice)
    {
        PassThroughProjectHistory history = ActivateProject();

        TaskCompletionSource<UnsavedChangesChoice> confirmation =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        _dialogs.ConfirmUnsavedChangesAsync("My story")
            .Returns(confirmation.Task);

        Task<bool> decision = _guard.CanLeaveAsync();

        history.Edit("Rename project", project => project.Title = "Updated story");
        confirmation.SetResult(choice);

        bool canLeave = await decision;

        Assert.False(canLeave);
        Assert.True(_session.IsModified);
        Assert.Same(history.CurrentProject, _session.Project);
        Assert.Equal("Updated story", _session.Project?.Title);

        await _saveAction.DidNotReceive().ExecuteAsync();

        await _dialogs.Received(1)
            .ShowErrorAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ProjectReplacedDuringConfirmation_RejectsPreviousDecision()
    {
        ActivateProject();

        TaskCompletionSource<UnsavedChangesChoice> confirmation =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        _dialogs.ConfirmUnsavedChangesAsync("My story")
            .Returns(confirmation.Task);

        Task<bool> decision = _guard.CanLeaveAsync();

        PassThroughProjectHistory replacement = ActivateProject(isSaved: true);
        confirmation.SetResult(UnsavedChangesChoice.Discard);

        bool canLeave = await decision;

        Assert.False(canLeave);
        Assert.Same(replacement.CurrentProject, _session.Project);
        Assert.False(_session.IsModified);

        await _saveAction.DidNotReceive().ExecuteAsync();

        await _dialogs.Received(1)
            .ShowErrorAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ProjectEditedDuringSave_DoesNotAllowLeaving()
    {
        PassThroughProjectHistory history = ActivateProject();

        _dialogs.ConfirmUnsavedChangesAsync("My story")
            .Returns(Task.FromResult(UnsavedChangesChoice.Save));

        _saveAction.ExecuteAsync().Returns(_ =>
        {
            long revision = _session.CaptureSavePoint();

            history.Edit("Rename project", project => project.Title = "Updated story");
            _session.TryMarkSaved(revision, _projectFolder);

            return Task.FromResult(true);
        });

        bool canLeave = await _guard.CanLeaveAsync();

        Assert.False(canLeave);
        Assert.True(_session.IsModified);
        Assert.Same(history.CurrentProject, _session.Project);

        await _saveAction.Received(1).ExecuteAsync();
    }

    [Fact]
    public async Task ProjectReplacedDuringSave_DoesNotAllowLeaving()
    {
        ActivateProject();

        _dialogs.ConfirmUnsavedChangesAsync("My story")
            .Returns(Task.FromResult(UnsavedChangesChoice.Save));

        Project? replacementProject = null;

        _saveAction.ExecuteAsync().Returns(_ =>
        {
            PassThroughProjectHistory replacement = ActivateProject(isSaved: true);
            replacementProject = replacement.CurrentProject;

            return Task.FromResult(true);
        });

        bool canLeave = await _guard.CanLeaveAsync();

        Assert.False(canLeave);
        Assert.NotNull(replacementProject);
        Assert.Same(replacementProject, _session.Project);
        Assert.False(_session.IsModified);

        await _saveAction.Received(1).ExecuteAsync();
    }

    public void Dispose()
    {
        _session.Dispose();
    }

    private PassThroughProjectHistory ActivateProject(bool isSaved = false)
    {
        Project project = new() { Title = "My story" };
        PassThroughProjectHistory history = new(project);

        _session.SetProject(history, _projectFolder, isSaved);

        return history;
    }
}