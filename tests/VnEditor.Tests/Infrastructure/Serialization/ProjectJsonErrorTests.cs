using System.Text.Json.Nodes;
using VnEditor.AppService.Serialization;
using VnEditor.Domain;
using VnEditor.Domain.Scenes;
using VnEditor.Domain.Scenes.Blocks;
using VnEditor.Infrastructure.Serialization;
using Xunit;

namespace VnEditor.Tests.Infrastructure.Serialization;

public sealed class ProjectJsonErrorTests
{
    [Fact]
    public void UnknownBlockTypeIsRejected()
    {
        Project project = new() { Title = "Story" };
        Scene scene = project.AddScene("Start");
        scene.Blocks.Add(new DialogueBlock
        {
            CharacterId = "char_1",
            Text = "Hello"
        });

        ProjectJsonSerializer serializer = new();
        JsonObject document = JsonNode.Parse(serializer.Serialize(project))!.AsObject();

        JsonObject block = document["scenes"]!.AsArray()[0]!["blocks"]!
            .AsArray()[0]!.AsObject();

        block["type"] = "future_block";

        ProjectSerializationException error = Assert.Throws<ProjectSerializationException>(
            () => serializer.Deserialize(document.ToJsonString()));

        Assert.Equal(ProjectSerializationException.FailureKind.InvalidContent, error.Kind);
    }

    [Fact]
    public void UnknownVariableTypeIsRejected()
    {
        Project project = new() { Title = "Story" };
        ProjectJsonSerializer serializer = new();

        JsonObject document = JsonNode.Parse(serializer.Serialize(project))!.AsObject();
        document["variables"]!.AsArray().Add(new JsonObject
        {
            ["type"] = "future_variable"
        });

        ProjectSerializationException error = Assert.Throws<ProjectSerializationException>(
            () => serializer.Deserialize(document.ToJsonString()));

        Assert.Equal(ProjectSerializationException.FailureKind.InvalidContent, error.Kind);
    }

    [Fact]
    public void FutureFormatVersionIsRejected()
    {
        Project project = new() { Title = "Story" };
        ProjectJsonSerializer serializer = new();

        JsonObject document = JsonNode.Parse(serializer.Serialize(project))!.AsObject();
        document["format_version"] = Project.CurrentFormatVersion + 1;

        ProjectSerializationException error = Assert.Throws<ProjectSerializationException>(
            () => serializer.Deserialize(document.ToJsonString()));

        Assert.Equal(ProjectSerializationException.FailureKind.UnsupportedVersion, error.Kind);
    }

    [Fact]
    public void MissingFormatVersionIsRejected()
    {
        Project project = new() { Title = "Story" };
        ProjectJsonSerializer serializer = new();

        JsonObject document = JsonNode.Parse(serializer.Serialize(project))!.AsObject();
        document.Remove("format_version");

        ProjectSerializationException error = Assert.Throws<ProjectSerializationException>(
            () => serializer.Deserialize(document.ToJsonString()));

        Assert.Equal(ProjectSerializationException.FailureKind.InvalidContent, error.Kind);
    }
}