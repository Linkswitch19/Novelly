using System;
using System.Text.Json.Nodes;
using VnEditor.AppService.Serialization;
using VnEditor.Domain;
using VnEditor.Domain.Assets;
using VnEditor.Domain.Characters;
using VnEditor.Domain.Scenes;
using VnEditor.Domain.Scenes.Blocks;
using VnEditor.Infrastructure.Serialization;
using Xunit;

namespace VnEditor.Tests.Infrastructure.Serialization;

public sealed class ProjectJsonDocumentTests
{
    [Fact]
    public void CompleteProjectRoundTrips()
    {
        Project original = new() { Title = "Story" };

        Character character = original.AddCharacter("Anna");
        character.Color = "#e05a8c";

        Expression expression = character.AddExpression("Happy", "happy.png");
        Background background = original.AddBackground("Room", "room.png");
        MusicTrack music = original.AddMusic("Theme", "theme.ogg");

        Scene intro = original.AddScene("Intro");
        original.AddScene("Ending");

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

        original.AddNumberVariable("Score", 12.5m);
        original.AddBooleanVariable("DoorOpen", true);
        original.AddTextVariable("HeroName", "Anna");

        ProjectJsonSerializer serializer = new();
        Project restored = serializer.Deserialize(serializer.Serialize(original));

        Assert.Equivalent(original, restored, strict: true);
        Assert.Equal(intro.Id, restored.StartSceneId);
        Assert.Equal(character.Id, Assert.IsType<ShowCharacterBlock>(restored.Scenes[0].Blocks[2]).CharacterId);
        Assert.Equal(expression.Id, Assert.IsType<ShowCharacterBlock>(restored.Scenes[0].Blocks[2]).ExpressionId);
        Assert.Equal(background.Id, Assert.IsType<BackgroundBlock>(restored.Scenes[0].Blocks[0]).BackgroundId);
        Assert.Equal(music.Id, Assert.IsType<MusicBlock>(restored.Scenes[0].Blocks[1]).MusicId);
    }

    [Theory]
    [InlineData("characters")]
    [InlineData("backgrounds")]
    [InlineData("music")]
    [InlineData("scenes")]
    [InlineData("variables")]
    public void MissingRootCollectionIsRejected(string propertyName)
    {
        Project project = new() { Title = "Story" };
        ProjectJsonSerializer serializer = new();

        JsonObject document = JsonNode.Parse(serializer.Serialize(project))!.AsObject();
        document.Remove(propertyName);

        ProjectSerializationException error = Assert.Throws<ProjectSerializationException>(
            () => serializer.Deserialize(document.ToJsonString()));

        Assert.Equal(ProjectSerializationException.FailureKind.InvalidContent, error.Kind);
    }

    [Theory]
    [InlineData("characters")]
    [InlineData("backgrounds")]
    [InlineData("music")]
    [InlineData("scenes")]
    [InlineData("variables")]
    public void RootCollectionMustBeAnArray(string propertyName)
    {
        Project project = new() { Title = "Story" };
        ProjectJsonSerializer serializer = new();

        JsonObject document = JsonNode.Parse(serializer.Serialize(project))!.AsObject();
        document[propertyName] = new JsonObject();

        ProjectSerializationException error = Assert.Throws<ProjectSerializationException>(
            () => serializer.Deserialize(document.ToJsonString()));

        Assert.Equal(ProjectSerializationException.FailureKind.InvalidContent, error.Kind);
    }

    [Theory]
    [InlineData("expressions")]
    [InlineData("blocks")]
    public void MissingNestedCollectionIsRejected(string propertyName)
    {
        Project project = new() { Title = "Story" };
        project.AddCharacter("Anna");
        project.AddScene("Intro");

        ProjectJsonSerializer serializer = new();
        JsonObject document = JsonNode.Parse(serializer.Serialize(project))!.AsObject();

        JsonObject parent = propertyName switch
        {
            "expressions" => document["characters"]!.AsArray()[0]!.AsObject(),
            "blocks" => document["scenes"]!.AsArray()[0]!.AsObject(),
            _ => throw new ArgumentOutOfRangeException(nameof(propertyName))
        };

        parent.Remove(propertyName);

        ProjectSerializationException error = Assert.Throws<ProjectSerializationException>(
            () => serializer.Deserialize(document.ToJsonString()));

        Assert.Equal(ProjectSerializationException.FailureKind.InvalidContent, error.Kind);
    }
}