using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using VnEditor.Domain;
using VnEditor.Infrastructure.Projects;

namespace VnEditor.Tests.Infrastructure.Projects;

/// <summary>
/// Verifica la creazione fisica di un progetto da template.
/// Controlla struttura, copia ricorsiva, destinazioni nuove o vuote,
/// conservazione dei contenuti esistenti e pulizia dopo errori.
/// </summary>
public sealed class ProjectCreationServiceTests : IDisposable
{
    private const string ScriptContent = "label start:\n    return\n";
    private const string NestedContent = "Template content.";

    private readonly string _rootFolder;
    private readonly string _templateFolder;
    private readonly string _projectsFolder;
    private readonly ProjectCreationService _service;

    public ProjectCreationServiceTests()
    {
        _rootFolder = Path.Combine(
            Path.GetTempPath(), "NovellyProjectCreationTests", Guid.NewGuid().ToString("N"));

        _templateFolder = Path.Combine(_rootFolder, "template");
        _projectsFolder = Path.Combine(_rootFolder, "projects");

        Directory.CreateDirectory(Path.Combine(_templateFolder, "game", "nested"));
        Directory.CreateDirectory(_projectsFolder);

        File.WriteAllText(
            Path.Combine(_templateFolder, "game", "script.rpy"), ScriptContent);

        File.WriteAllText(
            Path.Combine(_templateFolder, "game", "nested", "data.txt"), NestedContent);

        _service = new ProjectCreationService(_templateFolder);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Create_AcceptsNewOrEmptyDestination(bool destinationExists)
    {
        string destination = Path.Combine(_projectsFolder, "story");

        if (destinationExists)
            Directory.CreateDirectory(destination);

        Project project = await _service.CreateAsync("  My story  ", destination);

        string gameFolder = Path.Combine(destination, ".renpy_game", "game");

        Assert.Equal("My story", project.Title);
        Assert.Empty(project.Scenes);
        Assert.Null(project.StartSceneId);

        Assert.True(Directory.Exists(Path.Combine(gameFolder, "images")));
        Assert.True(Directory.Exists(Path.Combine(gameFolder, "audio")));

        Assert.Equal(
            ScriptContent,
            File.ReadAllText(Path.Combine(gameFolder, "script.rpy")));

        Assert.Equal(
            NestedContent,
            File.ReadAllText(Path.Combine(gameFolder, "nested", "data.txt")));

        Assert.Equal(
            ScriptContent,
            File.ReadAllText(Path.Combine(_templateFolder, "game", "script.rpy")));

        Assert.False(File.Exists(Path.Combine(destination, "project.json")));

        Assert.Equal(
            destination,
            Assert.Single(Directory.EnumerateFileSystemEntries(_projectsFolder)));
    }

    [Fact]
    public async Task DestinationContainingFile_IsRejectedWithoutChangingContent()
    {
        string destination = Path.Combine(_projectsFolder, "story");
        Directory.CreateDirectory(destination);

        string existingFile = Path.Combine(destination, "keep.txt");
        File.WriteAllText(existingFile, "Keep this content.");

        await Assert.ThrowsAsync<IOException>(
            () => _service.CreateAsync("My story", destination));

        Assert.Equal("Keep this content.", File.ReadAllText(existingFile));

        Assert.Equal(
            existingFile,
            Assert.Single(Directory.EnumerateFileSystemEntries(destination)));

        Assert.Equal(
            destination,
            Assert.Single(Directory.EnumerateFileSystemEntries(_projectsFolder)));
    }

    [Fact]
    public async Task DestinationContainingOnlySubdirectory_IsRejected()
    {
        string destination = Path.Combine(_projectsFolder, "story");
        string existingDirectory = Path.Combine(destination, "keep");

        Directory.CreateDirectory(existingDirectory);

        await Assert.ThrowsAsync<IOException>(
            () => _service.CreateAsync("My story", destination));

        Assert.True(Directory.Exists(existingDirectory));
        Assert.Empty(Directory.EnumerateFileSystemEntries(existingDirectory));

        Assert.Equal(
            existingDirectory,
            Assert.Single(Directory.EnumerateFileSystemEntries(destination)));

        Assert.Equal(
            destination,
            Assert.Single(Directory.EnumerateFileSystemEntries(_projectsFolder)));
    }

    [Fact]
    public async Task FileAtDestination_IsRejectedWithoutChangingFile()
    {
        string destination = Path.Combine(_projectsFolder, "story");
        File.WriteAllText(destination, "Existing file.");

        await Assert.ThrowsAsync<IOException>(
            () => _service.CreateAsync("My story", destination));

        Assert.Equal("Existing file.", File.ReadAllText(destination));
        Assert.False(Directory.Exists(destination));

        Assert.Equal(
            destination,
            Assert.Single(Directory.EnumerateFileSystemEntries(_projectsFolder)));
    }

    [Fact]
    public async Task MissingParent_IsRejectedWithoutCreatingFolders()
    {
        string missingParent = Path.Combine(_projectsFolder, "missing");
        string destination = Path.Combine(missingParent, "story");

        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => _service.CreateAsync("My story", destination));

        Assert.False(Directory.Exists(missingParent));
        Assert.Empty(Directory.EnumerateFileSystemEntries(_projectsFolder));
    }

    [Fact]
    public async Task MissingTemplate_FailsWhenCreatingWithoutPublishingDestination()
    {
        string missingTemplate = Path.Combine(_rootFolder, "missing-template");
        string destination = Path.Combine(_projectsFolder, "story");

        ProjectCreationService service = new(missingTemplate);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync("My story", destination));

        Assert.False(Directory.Exists(destination));
        Assert.Empty(Directory.EnumerateFileSystemEntries(_projectsFolder));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PreCanceledOperation_DoesNotChangeDestination(bool destinationExists)
    {
        string destination = Path.Combine(_projectsFolder, "story");

        if (destinationExists)
            Directory.CreateDirectory(destination);

        CancellationToken canceledToken = new(canceled: true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _service.CreateAsync("My story", destination, canceledToken));

        Assert.Equal(destinationExists, Directory.Exists(destination));

        string[] remainingEntries = Directory.GetFileSystemEntries(_projectsFolder);
        Assert.Equal(destinationExists ? 1 : 0, remainingEntries.Length);

        if (destinationExists)
            Assert.Empty(Directory.EnumerateFileSystemEntries(destination));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailureDuringInitialization_RemovesStagingAndPreservesDestination(
        bool destinationExists)
    {
        string brokenTemplate = Path.Combine(_rootFolder, "broken-template");
        Directory.CreateDirectory(brokenTemplate);

        File.WriteAllText(
            Path.Combine(brokenTemplate, "game"),
            "This file prevents creation of the game folders.");

        string destination = Path.Combine(_projectsFolder, "story");

        if (destinationExists)
            Directory.CreateDirectory(destination);

        ProjectCreationService service = new(brokenTemplate);

        await Assert.ThrowsAnyAsync<IOException>(
            () => service.CreateAsync("My story", destination));

        Assert.Equal(destinationExists, Directory.Exists(destination));

        string[] remainingEntries = Directory.GetFileSystemEntries(_projectsFolder);
        Assert.Equal(destinationExists ? 1 : 0, remainingEntries.Length);

        if (destinationExists)
            Assert.Empty(Directory.EnumerateFileSystemEntries(destination));

        Assert.Equal(
            "This file prevents creation of the game folders.",
            File.ReadAllText(Path.Combine(brokenTemplate, "game")));
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootFolder))
            Directory.Delete(_rootFolder, recursive: true);
    }
}