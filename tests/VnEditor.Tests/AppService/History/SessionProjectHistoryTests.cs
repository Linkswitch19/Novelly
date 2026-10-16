using System;
using System.IO;
using VnEditor.AppService.History;
using VnEditor.AppService.Sessions;
using VnEditor.Domain;
using VnEditor.Infrastructure.History;

namespace VnEditor.Tests.AppService.History;

/// <summary>
/// Verifica che il riferimento stabile alla history segua il progetto attivo.
/// Controlla destinazione delle modifiche, notifiche e rilascio del proxy
/// senza interrompere la sessione condivisa.
/// </summary>
public sealed class SessionProjectHistoryTests : IDisposable
{
    private readonly ProjectSession _session = new();

    private readonly string _originalFolder =
        Path.Combine(Path.GetTempPath(), "NovellySessionHistoryTests", "original");

    private readonly string _replacementFolder =
        Path.Combine(Path.GetTempPath(), "NovellySessionHistoryTests", "replacement");

    private readonly SessionProjectHistory _history;

    public SessionProjectHistoryTests()
    {
        _history = new SessionProjectHistory(_session);
    }

    [Fact]
    public void EmptySession_RejectsReadingAndEditingProject()
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = _history.CurrentProject;
        });

        Assert.Throws<InvalidOperationException>(() =>
            _history.Edit("Rename project", project => project.Title = "Updated story"));
    }

    [Fact]
    public void StableReference_ReadsAndEditsTheCurrentlyActiveProject()
    {
        IProjectHistory stableReference = _history;

        PassThroughProjectHistory original = CreateHistory("Original story");
        _session.SetProject(original, _originalFolder, isSaved: true);

        Assert.Same(original.CurrentProject, stableReference.CurrentProject);

        PassThroughProjectHistory replacement = CreateHistory("Replacement story");
        _session.SetProject(replacement, _replacementFolder, isSaved: true);

        Assert.Same(replacement.CurrentProject, stableReference.CurrentProject);

        stableReference.Edit("Rename project", project => project.Title = "Updated story");

        Assert.Equal("Original story", original.CurrentProject.Title);
        Assert.Equal("Updated story", replacement.CurrentProject.Title);
        Assert.Same(replacement.CurrentProject, _session.Project);
        Assert.True(_session.IsModified);
    }

    [Fact]
    public void ActivationAndEdit_NotifyObserversThroughTheStableReference()
    {
        int notifications = 0;
        object? lastSender = null;
        Project? lastNotifiedProject = null;

        _history.Changed += (sender, _) =>
        {
            notifications++;
            lastSender = sender;
            lastNotifiedProject = _history.CurrentProject;
        };

        PassThroughProjectHistory original = CreateHistory("Original story");
        _session.SetProject(original, _originalFolder, isSaved: true);

        Assert.Equal(1, notifications);
        Assert.Same(_history, lastSender);
        Assert.Same(original.CurrentProject, lastNotifiedProject);

        _history.Edit("Rename project", project => project.Title = "Updated story");

        Assert.Equal(2, notifications);
        Assert.Same(_history, lastSender);
        Assert.Same(original.CurrentProject, lastNotifiedProject);
        Assert.Equal("Updated story", lastNotifiedProject?.Title);

        PassThroughProjectHistory replacement = CreateHistory("Replacement story");
        _session.SetProject(replacement, _replacementFolder, isSaved: true);

        Assert.Equal(3, notifications);
        Assert.Same(_history, lastSender);
        Assert.Same(replacement.CurrentProject, lastNotifiedProject);
    }

    [Fact]
    public void PreviousHistoryChanges_AreNotForwarded()
    {
        PassThroughProjectHistory original = CreateHistory("Original story");
        PassThroughProjectHistory replacement = CreateHistory("Replacement story");

        _session.SetProject(original, _originalFolder, isSaved: true);
        _session.SetProject(replacement, _replacementFolder, isSaved: true);

        int notifications = 0;
        _history.Changed += (_, _) => notifications++;

        original.Edit("Rename old project", project => project.Title = "Old project changed");

        Assert.Equal(0, notifications);
        Assert.Same(replacement.CurrentProject, _history.CurrentProject);
        Assert.Equal("Replacement story", _history.CurrentProject.Title);
        Assert.False(_session.IsModified);
    }

    [Fact]
    public void MarkingProjectSaved_DoesNotNotifyContentChanges()
    {
        PassThroughProjectHistory current = CreateHistory("My story");
        _session.SetProject(current, _originalFolder, isSaved: false);

        int notifications = 0;
        _history.Changed += (_, _) => notifications++;

        long revision = _session.CaptureSavePoint();
        bool accepted = _session.TryMarkSaved(revision, _originalFolder);

        Assert.True(accepted);
        Assert.False(_session.IsModified);
        Assert.Equal(0, notifications);
        Assert.Same(current.CurrentProject, _history.CurrentProject);
    }

    [Fact]
    public void DisposedProxy_StopsForwardingWithoutDisposingSession()
    {
        _history.Dispose();

        int notifications = 0;
        _history.Changed += (_, _) => notifications++;

        PassThroughProjectHistory current = CreateHistory("My story");
        _session.SetProject(current, _originalFolder, isSaved: true);

        current.Edit("Rename project", project => project.Title = "Updated story");

        Assert.Equal(0, notifications);
        Assert.Same(current.CurrentProject, _session.Project);
        Assert.True(_session.IsModified);

        Assert.Throws<ObjectDisposedException>(() =>
        {
            _ = _history.CurrentProject;
        });

        Assert.Throws<ObjectDisposedException>(() =>
            _history.Edit("Rename project", project => project.Title = "Another title"));
    }

    public void Dispose()
    {
        _history.Dispose();
        _session.Dispose();
    }

    private static PassThroughProjectHistory CreateHistory(string title)
    {
        Project project = new() { Title = title };

        return new PassThroughProjectHistory(project);
    }
}