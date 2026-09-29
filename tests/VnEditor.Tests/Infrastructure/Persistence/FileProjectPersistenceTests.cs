using System.Text.Json.Nodes;
using VnEditor.AppService.Serialization;
using VnEditor.Domain;
using VnEditor.Domain.Assets;
using VnEditor.Domain.Characters;
using VnEditor.Domain.Scenes;
using VnEditor.Domain.Scenes.Blocks;
using VnEditor.Domain.Variables;
using VnEditor.Infrastructure.Persistence;
using VnEditor.Infrastructure.Serialization;
using Xunit;

namespace VnEditor.Tests.Infrastructure.Persistence;

/// <summary>
/// Verifica il salvataggio e il caricamento del progetto su disco:
/// round-trip completo, conservazione del salvataggio precedente,
/// recupero dal backup e rifiuto di dati non validi.
/// </summary>
public sealed class FileProjectPersistenceTests : IDisposable
{
    private readonly string _folder = Path.Combine(
        Path.GetTempPath(),
        "NovellyPersistenceTests",
        Guid.NewGuid().ToString("N"));

    private readonly FileProjectPersistence _persistence =
        new(new ProjectJsonSerializer());

    public FileProjectPersistenceTests()
    {
        Directory.CreateDirectory(_folder);
    }

    [Fact]
    public async Task CompleteProjectRoundTripsThroughDisk()
    {
        Project original = CreateCompleteProject();

        await _persistence.SaveAsync(original, _folder);
        Project restored = await _persistence.LoadAsync(_folder);

        Assert.True(File.Exists(Path.Combine(_folder, "project.json")));
        Assert.Equivalent(original, restored, strict: true);

        Assert.Equal("room.png", restored.Backgrounds[0].FileName);
        Assert.Equal("theme.ogg", restored.Music[0].FileName);
        Assert.Equal("happy.png", restored.Characters[0].Expressions[0].FileName);

        Assert.IsType<BackgroundBlock>(restored.Scenes[0].Blocks[0]);
        Assert.IsType<MusicBlock>(restored.Scenes[0].Blocks[1]);
        Assert.IsType<ShowCharacterBlock>(restored.Scenes[0].Blocks[2]);
        Assert.IsType<DialogueBlock>(restored.Scenes[0].Blocks[3]);

        Assert.IsType<NumberVariable>(restored.Variables[0]);
        Assert.IsType<BooleanVariable>(restored.Variables[1]);
        Assert.IsType<TextVariable>(restored.Variables[2]);

        Character restoredCharacter = restored.Characters[0];

        Assert.Equal("char_3", restored.AddCharacter("New").Id);
        Assert.Equal("espr_3", restoredCharacter.AddExpression("New", "new.png").Id);
        Assert.Equal("bg_3", restored.AddBackground("New", "new-background.png").Id);
        Assert.Equal("mus_3", restored.AddMusic("New", "new-music.ogg").Id);
        Assert.Equal("scene_4", restored.AddScene("New").Id);
        Assert.Equal("var_5", restored.AddBooleanVariable("New", false).Id);
    }

    [Fact]
    public async Task InvalidAssetPathDoesNotChangePreviousSave()
    {
        await _persistence.SaveAsync(new Project { Title = "Valid" }, _folder);

        Project invalid = new() { Title = "Invalid" };
        invalid.AddBackground("Outside", @"C:\outside.png");

        ProjectSerializationException error =
            await Assert.ThrowsAsync<ProjectSerializationException>(
                () => _persistence.SaveAsync(invalid, _folder));

        Assert.Equal(
            ProjectSerializationException.FailureKind.InvalidContent,
            error.Kind);

        Project restored = await _persistence.LoadAsync(_folder);
        Assert.Equal("Valid", restored.Title);
    }

    [Fact]
    public async Task FailedReplacementKeepsPreviousSave()
    {
        if (!OperatingSystem.IsWindows())
            return;

        await _persistence.SaveAsync(new Project { Title = "Previous" }, _folder);

        string projectFilePath = Path.Combine(_folder, "project.json");

        using (FileStream lockedFile = new(
            projectFilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.None))
        {
            Exception? error = await Record.ExceptionAsync(
                () => _persistence.SaveAsync(
                    new Project { Title = "Next" },
                    _folder));

            Assert.NotNull(error);
        }

        Project restored = await _persistence.LoadAsync(_folder);
        Assert.Equal("Previous", restored.Title);

        Assert.Empty(Directory.EnumerateFiles(
            _folder,
            ".project.json.*.tmp",
            SearchOption.TopDirectoryOnly));
    }

    [Fact]
    public async Task MissingProjectFileLoadsPreviousBackup()
    {
        await _persistence.SaveAsync(new Project { Title = "First" }, _folder);
        await _persistence.SaveAsync(new Project { Title = "Second" }, _folder);

        string projectFilePath = Path.Combine(_folder, "project.json");
        string backupFilePath = projectFilePath + ".bak";

        Assert.True(File.Exists(backupFilePath));

        File.Delete(projectFilePath);

        Project restored = await _persistence.LoadAsync(_folder);
        Assert.Equal("First", restored.Title);
    }

    [Fact]
    public async Task UnsupportedVersionIsRejectedEvenWhenBackupExists()
    {
        await _persistence.SaveAsync(new Project { Title = "First" }, _folder);
        await _persistence.SaveAsync(new Project { Title = "Second" }, _folder);

        string projectFilePath = Path.Combine(_folder, "project.json");
        string content = await File.ReadAllTextAsync(projectFilePath);
        JsonObject document = JsonNode.Parse(content)!.AsObject();

        document["format_version"] = Project.CurrentFormatVersion + 1;
        await File.WriteAllTextAsync(projectFilePath, document.ToJsonString());

        ProjectSerializationException error =
            await Assert.ThrowsAsync<ProjectSerializationException>(
                () => _persistence.LoadAsync(_folder));

        Assert.Equal(
            ProjectSerializationException.FailureKind.UnsupportedVersion,
            error.Kind);
    }

    [Fact]
    public async Task CounterLowerThanExistingIdIsRejected()
    {
        Project original = new() { Title = "Story" };
        original.AddCharacter("Anna");

        await _persistence.SaveAsync(original, _folder);

        string projectFilePath = Path.Combine(_folder, "project.json");
        string content = await File.ReadAllTextAsync(projectFilePath);
        JsonObject document = JsonNode.Parse(content)!.AsObject();

        document["id_counters"]!["char_"] = 0;
        await File.WriteAllTextAsync(projectFilePath, document.ToJsonString());

        ProjectSerializationException error =
            await Assert.ThrowsAsync<ProjectSerializationException>(
                () => _persistence.LoadAsync(_folder));

        Assert.Equal(
            ProjectSerializationException.FailureKind.InvalidContent,
            error.Kind);
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    private static Project CreateCompleteProject()
    {
        Project project = new() { Title = "Story" };

        Character character = project.AddCharacter("Anna");
        character.Color = "#e05a8c";

        Expression expression = character.AddExpression("Happy", "happy.png");
        Expression removedExpression = character.AddExpression("Removed", "removed.png");
        character.Expressions.Remove(removedExpression);

        Character removedCharacter = project.AddCharacter("Removed");
        project.Characters.Remove(removedCharacter);

        Background background = project.AddBackground("Room", "room.png");
        Background removedBackground = project.AddBackground("Removed", "removed-background.png");
        project.Backgrounds.Remove(removedBackground);

        MusicTrack music = project.AddMusic("Theme", "theme.ogg");
        MusicTrack removedMusic = project.AddMusic("Removed", "removed-music.ogg");
        project.Music.Remove(removedMusic);

        Scene intro = project.AddScene("Intro");
        project.AddScene("Ending");

        Scene removedScene = project.AddScene("Removed");
        project.Scenes.Remove(removedScene);

        intro.Blocks.Add(new BackgroundBlock { BackgroundId = background.Id });
        intro.Blocks.Add(new MusicBlock { MusicId = music.Id });
        intro.Blocks.Add(new ShowCharacterBlock
        {
            CharacterId = character.Id,
            ExpressionId = expression.Id
        });
        intro.Blocks.Add(new DialogueBlock
        {
            CharacterId = character.Id,
            Text = "Hello"
        });

        project.AddNumberVariable("Score", 12.5m);
        project.AddBooleanVariable("DoorOpen", true);
        project.AddTextVariable("HeroName", "Anna");

        NumberVariable removedVariable = project.AddNumberVariable("Removed", 0m);
        project.Variables.Remove(removedVariable);

        return project;
    }
}