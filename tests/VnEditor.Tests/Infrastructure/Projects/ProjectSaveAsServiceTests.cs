using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using VnEditor.AppService.Persistence;
using VnEditor.Domain;
using VnEditor.Infrastructure.Persistence;
using VnEditor.Infrastructure.Projects;
using VnEditor.Infrastructure.Serialization;

namespace VnEditor.Tests.Infrastructure.Projects;

/// <summary>
/// Verifica Save As sul filesystem con persistenza reale.
/// Controlla copia dei contenuti, inizializzazione da template,
/// destinazioni non valide e pulizia dopo errori o annullamento.
/// </summary>
public sealed class ProjectSaveAsServiceTests : IDisposable
{
    private const string TemplateScript = "label start:\n    return\n";
    private const string SourceScript = "label start:\n    \"Original content\"\n    return\n";

    private static readonly byte[] AssetContent = [1, 2, 3, 4];

    private readonly string _rootFolder;
    private readonly string _templateFolder;
    private readonly string _sourceFolder;
    private readonly string _destinationParent;
    private readonly string _destinationFolder;
    private readonly string _originalContent;

    private readonly Project _project = new() { Title = "Updated story" };
    private readonly FileProjectPersistence _persistence;
    private readonly ProjectSaveAsService _service;

    public ProjectSaveAsServiceTests()
    {
        _rootFolder = Path.Combine(
            Path.GetTempPath(), "NovellyProjectSaveAsTests", Guid.NewGuid().ToString("N"));

        _templateFolder = Path.Combine(_rootFolder, "template");
        _sourceFolder = Path.Combine(_rootFolder, "source");
        _destinationParent = Path.Combine(_rootFolder, "destinations");
        _destinationFolder = Path.Combine(_destinationParent, "story");

        Directory.CreateDirectory(Path.Combine(_templateFolder, "game"));
        Directory.CreateDirectory(_sourceFolder);
        Directory.CreateDirectory(_destinationParent);

        File.WriteAllText(
            Path.Combine(_templateFolder, "game", "script.rpy"), TemplateScript);

        ProjectJsonSerializer serializer = new();
        _persistence = new FileProjectPersistence(serializer);

        _originalContent = serializer.Serialize(new Project { Title = "Original story" });

        File.WriteAllText(
            Path.Combine(_sourceFolder, "project.json"), _originalContent);

        _service = new ProjectSaveAsService(_templateFolder, _persistence);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task SaveAs_CopiesOrInitializesGameAndPersistsCurrentModel(
        bool sourceHasGame, bool destinationExists)
    {
        if (sourceHasGame)
            CreateSourceGame();

        if (destinationExists)
            Directory.CreateDirectory(_destinationFolder);

        await _service.SaveAsAsync(_project, _sourceFolder, _destinationFolder);

        Project restored = await _persistence.LoadAsync(_destinationFolder);

        Assert.Equivalent(_project, restored, strict: true);

        string gameFolder = Path.Combine(_destinationFolder, ".renpy_game", "game");
        string expectedScript = sourceHasGame ? SourceScript : TemplateScript;

        Assert.Equal(
            expectedScript,
            File.ReadAllText(Path.Combine(gameFolder, "script.rpy")));

        Assert.True(Directory.Exists(Path.Combine(gameFolder, "images")));
        Assert.True(Directory.Exists(Path.Combine(gameFolder, "audio")));

        if (sourceHasGame)
        {
            Assert.Equal(
                AssetContent,
                File.ReadAllBytes(Path.Combine(gameFolder, "images", "characters", "portrait.png")));

            string sourceAsset = Path.Combine(
                _sourceFolder, ".renpy_game", "game", "images", "characters", "portrait.png");

            Assert.Equal(AssetContent, File.ReadAllBytes(sourceAsset));
        }
        else
        {
            Assert.False(Directory.Exists(Path.Combine(_sourceFolder, ".renpy_game")));
        }

        AssertSourceUnchanged();

        Assert.Equal(
            _destinationFolder,
            Assert.Single(Directory.EnumerateFileSystemEntries(_destinationParent)));
    }

    [Fact]
    public async Task SameFolder_UsesAtomicPersistenceAndPreservesPreviousSaveAsBackup()
    {
        string equivalentDestination = _sourceFolder + Path.DirectorySeparatorChar;

        await _service.SaveAsAsync(_project, _sourceFolder, equivalentDestination);

        Project restored = await _persistence.LoadAsync(_sourceFolder);

        Assert.Equivalent(_project, restored, strict: true);

        Assert.Equal(
            _originalContent,
            File.ReadAllText(Path.Combine(_sourceFolder, "project.json.bak")));

        Assert.False(Directory.Exists(Path.Combine(_sourceFolder, ".renpy_game")));
        Assert.Empty(Directory.EnumerateFileSystemEntries(_destinationParent));
    }

    [Fact]
    public async Task NonEmptyDestination_IsRejectedWithoutChangingExistingContent()
    {
        Directory.CreateDirectory(_destinationFolder);

        string existingFile = Path.Combine(_destinationFolder, "keep.txt");
        File.WriteAllText(existingFile, "Keep this content.");

        await Assert.ThrowsAsync<IOException>(
            () => _service.SaveAsAsync(_project, _sourceFolder, _destinationFolder));

        Assert.Equal("Keep this content.", File.ReadAllText(existingFile));

        Assert.Equal(
            existingFile,
            Assert.Single(Directory.EnumerateFileSystemEntries(_destinationFolder)));

        Assert.Equal(
            _destinationFolder,
            Assert.Single(Directory.EnumerateFileSystemEntries(_destinationParent)));

        AssertSourceUnchanged();
    }

    [Fact]
    public async Task FileAtDestination_IsRejectedWithoutChangingFile()
    {
        File.WriteAllText(_destinationFolder, "Existing file.");

        await Assert.ThrowsAsync<IOException>(
            () => _service.SaveAsAsync(_project, _sourceFolder, _destinationFolder));

        Assert.Equal("Existing file.", File.ReadAllText(_destinationFolder));

        Assert.Equal(
            _destinationFolder,
            Assert.Single(Directory.EnumerateFileSystemEntries(_destinationParent)));

        AssertSourceUnchanged();
    }

    [Fact]
    public async Task DestinationInsideSource_IsRejectedWithoutCreatingFolders()
    {
        string nestedDestination = Path.Combine(_sourceFolder, "nested-copy");

        await Assert.ThrowsAsync<IOException>(
            () => _service.SaveAsAsync(_project, _sourceFolder, nestedDestination));

        Assert.False(Directory.Exists(nestedDestination));

        Assert.Equal(
            Path.Combine(_sourceFolder, "project.json"),
            Assert.Single(Directory.EnumerateFileSystemEntries(_sourceFolder)));

        Assert.Empty(Directory.EnumerateFileSystemEntries(_destinationParent));
        AssertSourceUnchanged();
    }

    [Fact]
    public async Task MissingSource_IsRejectedWithoutPublishingDestination()
    {
        string missingSource = Path.Combine(_rootFolder, "missing-source");

        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => _service.SaveAsAsync(_project, missingSource, _destinationFolder));

        Assert.False(Directory.Exists(missingSource));
        AssertDestinationUnpublished(destinationExisted: false);
        AssertSourceUnchanged();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PersistenceFailure_RemovesPartialCopyAndPreservesDestination(
        bool destinationExists)
    {
        CreateSourceGame();

        if (destinationExists)
            Directory.CreateDirectory(_destinationFolder);

        IOException expected = new("Persistence failed after copying.");

        IProjectPersistence failingPersistence = Substitute.For<IProjectPersistence>();
        bool gameWasCopied = false;

        failingPersistence.SaveAsync(
            Arg.Any<Project>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                string stagingFolder = call.ArgAt<string>(1);

                Assert.Equal(_destinationParent, Path.GetDirectoryName(stagingFolder));

                gameWasCopied = File.Exists(
                    Path.Combine(stagingFolder, ".renpy_game", "game", "script.rpy"));

                File.WriteAllText(
                    Path.Combine(stagingFolder, "partial.json"), "Incomplete content.");

                return Task.FromException(expected);
            });

        ProjectSaveAsService service = new(_templateFolder, failingPersistence);

        IOException actual = await Assert.ThrowsAsync<IOException>(
            () => service.SaveAsAsync(_project, _sourceFolder, _destinationFolder));

        Assert.Same(expected, actual);
        Assert.True(gameWasCopied);

        AssertDestinationUnpublished(destinationExists);
        AssertSourceUnchanged();

        Assert.Equal(
            SourceScript,
            File.ReadAllText(Path.Combine(_sourceFolder, ".renpy_game", "game", "script.rpy")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationBeforePublication_RemovesSavedStagingAndPreservesDestination(
        bool destinationExists)
    {
        if (destinationExists)
            Directory.CreateDirectory(_destinationFolder);

        using CancellationTokenSource cancellation = new();

        IProjectPersistence cancelingPersistence = Substitute.For<IProjectPersistence>();
        bool persistedBeforeCancellation = false;

        cancelingPersistence.SaveAsync(
            Arg.Any<Project>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => PersistThenCancelAsync(
                call.ArgAt<Project>(0), call.ArgAt<string>(1), call.ArgAt<CancellationToken>(2)));

        ProjectSaveAsService service = new(_templateFolder, cancelingPersistence);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.SaveAsAsync(
                _project, _sourceFolder, _destinationFolder, cancellation.Token));

        Assert.True(persistedBeforeCancellation);

        AssertDestinationUnpublished(destinationExists);
        AssertSourceUnchanged();

        async Task PersistThenCancelAsync(Project project, string projectFolder, CancellationToken token)
        {
            Assert.Equal(_destinationParent, Path.GetDirectoryName(projectFolder));

            await _persistence.SaveAsync(project, projectFolder, token);

            persistedBeforeCancellation =
                File.Exists(Path.Combine(projectFolder, "project.json"));

            cancellation.Cancel();
        }
    }

    [Fact]
    public async Task PreCanceledOperation_DoesNotCreateDestination()
    {
        CancellationToken canceledToken = new(canceled: true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _service.SaveAsAsync(
                _project, _sourceFolder, _destinationFolder, canceledToken));

        AssertDestinationUnpublished(destinationExisted: false);
        AssertSourceUnchanged();
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootFolder))
            Directory.Delete(_rootFolder, recursive: true);
    }

    private void CreateSourceGame()
    {
        string gameFolder = Path.Combine(_sourceFolder, ".renpy_game", "game");
        string charactersFolder = Path.Combine(gameFolder, "images", "characters");

        Directory.CreateDirectory(charactersFolder);

        File.WriteAllText(Path.Combine(gameFolder, "script.rpy"), SourceScript);
        File.WriteAllBytes(Path.Combine(charactersFolder, "portrait.png"), AssetContent);
    }

    private void AssertSourceUnchanged()
    {
        Assert.Equal(
            _originalContent,
            File.ReadAllText(Path.Combine(_sourceFolder, "project.json")));
    }

    private void AssertDestinationUnpublished(bool destinationExisted)
    {
        Assert.Equal(destinationExisted, Directory.Exists(_destinationFolder));

        string[] remainingEntries = Directory.GetFileSystemEntries(_destinationParent);
        Assert.Equal(destinationExisted ? 1 : 0, remainingEntries.Length);

        if (destinationExisted)
            Assert.Empty(Directory.EnumerateFileSystemEntries(_destinationFolder));
    }
}