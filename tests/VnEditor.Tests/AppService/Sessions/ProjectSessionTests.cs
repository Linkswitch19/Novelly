using System;
using System.IO;
using VnEditor.AppService.Sessions;
using VnEditor.Domain;
using VnEditor.Infrastructure.History;

namespace VnEditor.Tests.AppService.Sessions;

/// <summary>
/// Verifica il collegamento della sessione alla history del progetto attivo.
/// Controlla revisioni, notifiche, stato modificato e protezione dai risultati
/// di operazioni avviate prima di una modifica o sostituzione del progetto.
/// </summary>
public sealed class ProjectSessionTests : IDisposable
{
    private readonly ProjectSession _session = new();

    private readonly string _originalFolder =
        Path.Combine(Path.GetTempPath(), "NovellySessionTests", "original");

    private readonly string _replacementFolder =
        Path.Combine(Path.GetTempPath(), "NovellySessionTests", "replacement");

    [Fact]
    public void EmptySession_HasNoProjectOrRevision()
    {
        Assert.False(_session.HasProject);
        Assert.Null(_session.Project);
        Assert.Null(_session.ProjectFolder);
        Assert.Null(_session.CaptureCurrentRevision());
        Assert.False(_session.IsModified);
        Assert.True(_session.IsCurrentRevision(null));

        Assert.Throws<InvalidOperationException>(() => _session.CaptureSavePoint());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SetProject_UpdatesStateNormalizesFolderAndNotifies(bool isSaved)
    {
        PassThroughProjectHistory history = CreateHistory("My story");

        string requestedFolder =
            Path.Combine(_originalFolder, "unused", "..", "project");

        int stateNotifications = 0;
        int projectNotifications = 0;

        _session.StateChanged += (_, _) => stateNotifications++;
        _session.ProjectChanged += (_, _) => projectNotifications++;

        _session.SetProject(history, requestedFolder, isSaved);

        Assert.True(_session.HasProject);
        Assert.Same(history.CurrentProject, _session.Project);
        Assert.Equal(Path.GetFullPath(requestedFolder), _session.ProjectFolder);
        Assert.NotNull(_session.CaptureCurrentRevision());
        Assert.Equal(!isSaved, _session.IsModified);
        Assert.Equal(1, stateNotifications);
        Assert.Equal(1, projectNotifications);
    }

    [Fact]
    public void HistoryEdit_AdvancesRevisionMarksDirtyAndNotifies()
    {
        PassThroughProjectHistory history = CreateHistory("My story");
        _session.SetProject(history, _originalFolder, isSaved: true);

        long originalRevision = _session.CaptureSavePoint();
        int stateNotifications = 0;
        int projectNotifications = 0;

        _session.StateChanged += (_, _) => stateNotifications++;
        _session.ProjectChanged += (_, _) => projectNotifications++;

        history.Edit("Rename project", project => project.Title = "Updated story");

        Assert.True(_session.IsModified);
        Assert.Equal("Updated story", _session.Project?.Title);
        Assert.NotEqual(originalRevision, _session.CaptureSavePoint());
        Assert.False(_session.IsCurrentRevision(originalRevision));
        Assert.Equal(1, stateNotifications);
        Assert.Equal(1, projectNotifications);
    }

    [Fact]
    public void PartiallyFailedEdit_StillMarksProjectDirty()
    {
        PassThroughProjectHistory history = CreateHistory("My story");
        _session.SetProject(history, _originalFolder, isSaved: true);

        long originalRevision = _session.CaptureSavePoint();
        InvalidOperationException expected = new("Edit failed after changing the project.");

        InvalidOperationException actual = Assert.Throws<InvalidOperationException>(() =>
            history.Edit("Rename project", project =>
            {
                project.Title = "Partially updated story";
                throw expected;
            }));

        Assert.Same(expected, actual);
        Assert.Equal("Partially updated story", _session.Project?.Title);
        Assert.True(_session.IsModified);
        Assert.NotEqual(originalRevision, _session.CaptureSavePoint());
    }

    [Fact]
    public void ReplacedHistory_NoLongerChangesSessionState()
    {
        PassThroughProjectHistory original = CreateHistory("Original story");
        PassThroughProjectHistory replacement = CreateHistory("Replacement story");

        _session.SetProject(original, _originalFolder, isSaved: true);
        _session.SetProject(replacement, _replacementFolder, isSaved: true);

        long replacementRevision = _session.CaptureSavePoint();
        int stateNotifications = 0;
        int projectNotifications = 0;

        _session.StateChanged += (_, _) => stateNotifications++;
        _session.ProjectChanged += (_, _) => projectNotifications++;

        original.Edit("Rename old project", project => project.Title = "Old project changed");

        Assert.Same(replacement.CurrentProject, _session.Project);
        Assert.Equal("Replacement story", _session.Project?.Title);
        Assert.Equal(replacementRevision, _session.CaptureSavePoint());
        Assert.False(_session.IsModified);
        Assert.Equal(0, stateNotifications);
        Assert.Equal(0, projectNotifications);

        replacement.Edit("Rename current project", project => project.Title = "Current project changed");

        Assert.Equal("Current project changed", _session.Project?.Title);
        Assert.True(_session.IsModified);
        Assert.Equal(1, stateNotifications);
        Assert.Equal(1, projectNotifications);
    }

    [Fact]
    public void CurrentSave_UpdatesFolderAndNotifiesOnlyState()
    {
        PassThroughProjectHistory history = CreateHistory("My story");
        _session.SetProject(history, _originalFolder, isSaved: false);

        long revision = _session.CaptureSavePoint();
        int stateNotifications = 0;
        int projectNotifications = 0;

        _session.StateChanged += (_, _) => stateNotifications++;
        _session.ProjectChanged += (_, _) => projectNotifications++;

        bool accepted = _session.TryMarkSaved(revision, _replacementFolder);

        Assert.True(accepted);
        Assert.Same(history.CurrentProject, _session.Project);
        Assert.Equal(_replacementFolder, _session.ProjectFolder);
        Assert.Equal(revision, _session.CaptureSavePoint());
        Assert.False(_session.IsModified);
        Assert.Equal(1, stateNotifications);
        Assert.Equal(0, projectNotifications);
    }

    [Fact]
    public void SaveBeforeLatestEdit_PreservesDirtyState()
    {
        PassThroughProjectHistory history = CreateHistory("My story");
        _session.SetProject(history, _originalFolder, isSaved: false);

        long savedRevision = _session.CaptureSavePoint();

        history.Edit("Rename project", project => project.Title = "Updated story");

        long currentRevision = _session.CaptureSavePoint();

        bool accepted = _session.TryMarkSaved(savedRevision, _replacementFolder);

        Assert.True(accepted);
        Assert.True(_session.IsModified);
        Assert.Equal(currentRevision, _session.CaptureSavePoint());
        Assert.Equal(_replacementFolder, _session.ProjectFolder);
        Assert.Equal("Updated story", _session.Project?.Title);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SaveFromPreviousProject_IsRejectedWithoutNotifications(bool replacementIsSaved)
    {
        PassThroughProjectHistory original = CreateHistory("My story");
        _session.SetProject(original, _originalFolder, isSaved: false);

        long obsoleteRevision = _session.CaptureSavePoint();

        PassThroughProjectHistory replacement = CreateHistory("My story");
        _session.SetProject(replacement, _replacementFolder, replacementIsSaved);

        long replacementRevision = _session.CaptureSavePoint();
        int stateNotifications = 0;
        int projectNotifications = 0;

        _session.StateChanged += (_, _) => stateNotifications++;
        _session.ProjectChanged += (_, _) => projectNotifications++;

        bool accepted = _session.TryMarkSaved(obsoleteRevision, _originalFolder);

        Assert.False(accepted);
        Assert.Same(replacement.CurrentProject, _session.Project);
        Assert.Equal(_replacementFolder, _session.ProjectFolder);
        Assert.Equal(replacementRevision, _session.CaptureSavePoint());
        Assert.Equal(!replacementIsSaved, _session.IsModified);
        Assert.Equal(0, stateNotifications);
        Assert.Equal(0, projectNotifications);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void MatchingRevision_AllowsProjectActivation(bool hasCurrentProject, bool isSaved)
    {
        if (hasCurrentProject)
        {
            PassThroughProjectHistory original = CreateHistory("Original story");
            _session.SetProject(original, _originalFolder, isSaved: true);
        }

        long? expectedRevision = _session.CaptureCurrentRevision();
        PassThroughProjectHistory candidate = CreateHistory("New story");

        bool accepted =
            _session.TrySetProject(candidate, _replacementFolder, isSaved, expectedRevision);

        Assert.True(accepted);
        Assert.Same(candidate.CurrentProject, _session.Project);
        Assert.Equal(_replacementFolder, _session.ProjectFolder);
        Assert.Equal(!isSaved, _session.IsModified);
        Assert.False(_session.IsCurrentRevision(expectedRevision));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ChangedSession_RejectsActivationWithoutNotifications(bool replaceCurrentProject)
    {
        PassThroughProjectHistory current = CreateHistory("My story");
        _session.SetProject(current, _originalFolder, isSaved: true);

        long? expectedRevision = _session.CaptureCurrentRevision();

        if (replaceCurrentProject)
        {
            current = CreateHistory("Replacement story");
            _session.SetProject(current, _replacementFolder, isSaved: true);
        }
        else
        {
            current.Edit("Rename project", project => project.Title = "Updated story");
        }

        long? currentRevision = _session.CaptureCurrentRevision();
        string? currentFolder = _session.ProjectFolder;
        bool wasModified = _session.IsModified;

        int stateNotifications = 0;
        int projectNotifications = 0;

        _session.StateChanged += (_, _) => stateNotifications++;
        _session.ProjectChanged += (_, _) => projectNotifications++;

        PassThroughProjectHistory candidate = CreateHistory("Candidate story");

        bool accepted =
            _session.TrySetProject(candidate, _replacementFolder, false, expectedRevision);

        Assert.False(accepted);
        Assert.Same(current.CurrentProject, _session.Project);
        Assert.Equal(currentFolder, _session.ProjectFolder);
        Assert.Equal(currentRevision, _session.CaptureCurrentRevision());
        Assert.Equal(wasModified, _session.IsModified);
        Assert.Equal(0, stateNotifications);
        Assert.Equal(0, projectNotifications);
    }

    [Fact]
    public void NullExpectedRevision_CannotReplaceAnActiveProject()
    {
        PassThroughProjectHistory current = CreateHistory("Current story");
        _session.SetProject(current, _originalFolder, isSaved: false);

        PassThroughProjectHistory candidate = CreateHistory("Candidate story");

        bool accepted =
            _session.TrySetProject(candidate, _replacementFolder, true, expectedRevision: null);

        Assert.False(accepted);
        Assert.Same(current.CurrentProject, _session.Project);
        Assert.Equal(_originalFolder, _session.ProjectFolder);
        Assert.True(_session.IsModified);
    }

    [Fact]
    public void DisposedSession_StopsProcessingHistoryChanges()
    {
        PassThroughProjectHistory history = CreateHistory("My story");
        _session.SetProject(history, _originalFolder, isSaved: true);

        _session.Dispose();

        int stateNotifications = 0;
        int projectNotifications = 0;

        _session.StateChanged += (_, _) => stateNotifications++;
        _session.ProjectChanged += (_, _) => projectNotifications++;

        history.Edit("Rename project", project => project.Title = "Updated story");

        Assert.False(_session.IsModified);
        Assert.Equal(0, stateNotifications);
        Assert.Equal(0, projectNotifications);

        Assert.Throws<ObjectDisposedException>(() => _session.CaptureCurrentRevision());
        Assert.Throws<ObjectDisposedException>(() =>
            _session.SetProject(history, _originalFolder, isSaved: true));
    }

    public void Dispose()
    {
        _session.Dispose();
    }

    private static PassThroughProjectHistory CreateHistory(string title)
    {
        Project project = new() { Title = title };

        return new PassThroughProjectHistory(project);
    }
}