using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.AppService.Generation;
using VnEditor.Domain;
using VnEditor.Infrastructure.Assets;

namespace VnEditor.Infrastructure.Generation;
/// <summary>
/// Implementazione concreta di IScriptGenerator.
/// Esegue in ordine: inizializza, pulisce i compilati, genera lo script.
/// </summary>
public sealed class ScriptGeneratorService : IScriptGenerator
{
    private readonly ProjectInitializer _initializer;
    private readonly string _templatePath;

    public ScriptGeneratorService(string templatePath)
    {
        this._templatePath = templatePath;
        this._initializer = new ProjectInitializer(templatePath);
    }
    public async Task GenerateAsync(Project project, string projectFolder, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectFolder);

        string gameFolder = this._initializer.Initialize(projectFolder);

        RpycCleaner.Clean(gameFolder);

        ScriptWriter writer = new();
        RenpyScriptGenerator generator = new(writer);
        generator.Generate(project);

        string scriptPath = Path.Combine(gameFolder, "game", "script.rpy");
        await ScriptFileWriter.WriteAsync(scriptPath, writer.Build(), ct);



    }
}
