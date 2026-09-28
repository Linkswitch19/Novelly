using System.Text.Json;
using VnEditor.Domain;
using VnEditor.Domain.Assets;
using VnEditor.Domain.Characters;
using VnEditor.Domain.Scenes;
using VnEditor.Domain.Scenes.Blocks;
using VnEditor.Domain.Variables;
using VnEditor.Infrastructure.Serialization;
using Xunit;

namespace VnEditor.Tests.Infrastructure.Serialization;

public sealed class ProjectJsonSerializerTests
{
    [Fact]
    public void EmptyProjectRoundTrips()
    {
        Project original = new() { Title = "Story" };
        ProjectJsonSerializer serializer = new();

        string json = serializer.Serialize(original);
        Project restored = serializer.Deserialize(json);

        using JsonDocument document = JsonDocument.Parse(json);

        Assert.Equal(
            Project.CurrentFormatVersion,
            document.RootElement.GetProperty("format_version").GetInt32());

        Assert.Equal(original.Title, restored.Title);
        Assert.Equal(original.FormatVersion, restored.FormatVersion);
        Assert.Null(restored.StartSceneId);
        Assert.Empty(restored.Scenes);
        Assert.Empty(restored.Variables);
    }

    [Theory]
    [InlineData("background")]
    [InlineData("dialogue")]
    [InlineData("music")]
    [InlineData("show_character")]
    public void EachBlockRoundTrips(string discriminator)
    {
        Project original = new() { Title = "Story" };

        Character character = original.AddCharacter("Anna");
        Expression expression = character.AddExpression("Happy", "happy.png");
        Background background = original.AddBackground("Room", "room.png");
        MusicTrack music = original.AddMusic("Theme", "theme.ogg");
        Scene scene = original.AddScene("Start");

        Block block = discriminator switch
        {
            "background" => new BackgroundBlock { BackgroundId = background.Id },
            "dialogue" => new DialogueBlock { CharacterId = character.Id, Text = "Hello" },
            "music" => new MusicBlock { MusicId = music.Id },
            "show_character" => new ShowCharacterBlock
            {
                CharacterId = character.Id,
                ExpressionId = expression.Id
            },
            _ => throw new ArgumentOutOfRangeException(nameof(discriminator))
        };

        scene.Blocks.Add(block);

        ProjectJsonSerializer serializer = new();
        string json = serializer.Serialize(original);
        Project restored = serializer.Deserialize(json);

        Block restoredBlock = Assert.Single(Assert.Single(restored.Scenes).Blocks);

        Assert.Equal(block.GetType(), restoredBlock.GetType());
        Assert.Equal(block.Id, restoredBlock.Id);
        Assert.Equivalent(block, restoredBlock, strict: true);

        using JsonDocument document = JsonDocument.Parse(json);

        string? savedDiscriminator = document.RootElement
            .GetProperty("scenes")[0]
            .GetProperty("blocks")[0]
            .GetProperty("type")
            .GetString();

        Assert.Equal(discriminator, savedDiscriminator);
    }

    [Theory]
    [InlineData("number")]
    [InlineData("boolean")]
    [InlineData("text")]
    public void EachVariableRoundTrips(string discriminator)
    {
        Project original = new() { Title = "Story" };

        Variable variable = discriminator switch
        {
            "number" => original.AddNumberVariable("Score", 12.5m),
            "boolean" => original.AddBooleanVariable("DoorOpen", true),
            "text" => original.AddTextVariable("HeroName", "Anna"),
            _ => throw new ArgumentOutOfRangeException(nameof(discriminator))
        };

        ProjectJsonSerializer serializer = new();
        string json = serializer.Serialize(original);
        Project restored = serializer.Deserialize(json);

        Variable restoredVariable = Assert.Single(restored.Variables);

        Assert.Equal(variable.GetType(), restoredVariable.GetType());
        Assert.Equal(variable.Id, restoredVariable.Id);
        Assert.Equal(variable.Name, restoredVariable.Name);
        Assert.Equivalent(variable, restoredVariable, strict: true);

        using JsonDocument document = JsonDocument.Parse(json);

        string? savedDiscriminator = document.RootElement
            .GetProperty("variables")[0]
            .GetProperty("type")
            .GetString();

        Assert.Equal(discriminator, savedDiscriminator);
    }

    [Fact]
    public void RoundTripDoesNotReuseDeletedIds()
    {
        Project original = new() { Title = "Story" };

        Character character = original.AddCharacter("Anna");
        Character deletedCharacter = original.AddCharacter("Deleted");
        original.Characters.Remove(deletedCharacter);

        character.AddExpression("Happy", "happy.png");
        Expression deletedExpression = character.AddExpression("Sad", "sad.png");
        character.Expressions.Remove(deletedExpression);

        original.AddNumberVariable("Score", 1m);
        BooleanVariable deletedVariable = original.AddBooleanVariable("Deleted", false);
        original.Variables.Remove(deletedVariable);

        ProjectJsonSerializer serializer = new();
        Project restored = serializer.Deserialize(serializer.Serialize(original));
        Character restoredCharacter = Assert.Single(restored.Characters);

        Assert.Equal("char_3", restored.AddCharacter("New").Id);
        Assert.Equal("espr_3", restoredCharacter.AddExpression("New", "new.png").Id);
        Assert.Equal("var_3", restored.AddTextVariable("New", "text").Id);
    }

}