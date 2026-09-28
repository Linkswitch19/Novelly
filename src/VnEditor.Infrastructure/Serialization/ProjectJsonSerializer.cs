using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using VnEditor.AppService.Serialization;
using VnEditor.Domain;

namespace VnEditor.Infrastructure.Serialization;

public sealed class ProjectJsonSerializer : IProjectSerializer
{
    private static readonly JsonSerializerOptions Options = ProjectJsonOptions.Create();

    public string Serialize(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        ProjectJsonVersionValidator.Validate(project.FormatVersion);

        try
        {
            return JsonSerializer.Serialize(project, Options);
        }
        catch(NotSupportedException exception)
        {
            throw new ProjectSerializationException(
                ProjectSerializationException.FailureKind.UnknownType,
                "Il progetto contiene un tipo non registrato.",
                exception);
        }
        catch (JsonException exception)
        {
            throw new ProjectSerializationException(
                ProjectSerializationException.FailureKind.InvalidContent,
                "Impossibile serializzare il progetto.",
                exception);
        }
    }

    public Project Deserialize(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ProjectSerializationException(
                ProjectSerializationException.FailureKind.InvalidContent,
                "Il contenuto del progetto è vuoto.");
        }
        try
        {
            using JsonDocument document = JsonDocument.Parse(content);
            ProjectJsonVersionValidator.Validate(document.RootElement);

            Project? project = document.RootElement.Deserialize<Project>(Options);

            return project ?? throw new ProjectSerializationException(
                ProjectSerializationException.FailureKind.InvalidContent,
                "Il contenuto non rappresenta un progetto.");
        }
        catch (JsonException exception)
        {
            throw new ProjectSerializationException(
                ProjectSerializationException.FailureKind.InvalidContent,
                "Il documento JSON del progetto non è valido.",
                exception);
        }
        catch (ArgumentException exception)
        {
            throw new ProjectSerializationException(
                ProjectSerializationException.FailureKind.InvalidContent,
                "Il progetto contiene valori non validi.",
                exception);
        }
        catch (NotSupportedException exception)
        {
            throw new ProjectSerializationException(
                ProjectSerializationException.FailureKind.UnknownType,
                "Il documento contiene un tipo non registrato.",
                exception);
        }
    }
}
