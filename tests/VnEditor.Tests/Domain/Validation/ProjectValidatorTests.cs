using VnEditor.Domain;
using VnEditor.Domain.Characters;
using VnEditor.Domain.Scenes;
using VnEditor.Domain.Scenes.Blocks;
using VnEditor.Domain.Validation;
using Xunit;

namespace VnEditor.Tests.Domain.Validation;

/// <summary>
/// Verifica i riferimenti a personaggi, espressioni, sfondi, musiche e variabili,
/// gli avvisi per le scene vuote e l'assenza di modifiche al progetto.
/// </summary>
public sealed class ProjectValidatorTests
{
    [Fact]
    public void ProgettoConRiferimentiValidiNonHaProblemi()
    {
        (Project project, Scene scene) = CreateProjectWithScene();
        Character character = project.AddCharacter("Anna");
        string expressionId = character.AddExpression("Felice", "felice.png").Id;
        string backgroundId = project.AddBackground("Aula", "aula.png").Id;
        string musicId = project.AddMusic("Tema", "tema.ogg").Id;

        scene.Blocks.Add(new BackgroundBlock { BackgroundId = backgroundId });
        scene.Blocks.Add(new MusicBlock { MusicId = musicId });
        scene.Blocks.Add(new ShowCharacterBlock { CharacterId = character.Id, ExpressionId = expressionId });
        scene.Blocks.Add(new DialogueBlock { CharacterId = character.Id, Text = "Ciao" });

        ValidationResult result = ProjectValidator.Validate(project);

        Assert.Empty(result.Issues);
        Assert.False(result.HasErrors);
    }

    [Fact]
    public void DialogoConPersonaggioMancanteProduceUnErroreConScenaEBlocco()
    {
        (Project project, Scene scene) = CreateProjectWithScene();
        DialogueBlock block = new() { CharacterId = "char_missing", Text = "Ciao" };
        scene.Blocks.Add(block);

        ValidationIssue issue = Assert.Single(ProjectValidator.Validate(project).Issues);

        Assert.Equal(ValidationCode.MissingCharacter, issue.Code);
        Assert.Equal(ValidationSeverity.Error, issue.Severity);
        Assert.Equal(scene.Id, issue.SceneId);
        Assert.Equal(block.Id, issue.BlockId);
        Assert.Contains($"Scena '{scene.Name}' ({scene.Id})", issue.Message);
        Assert.Contains($"blocco 1 'Dialogo' ({block.Id})", issue.Message);
        Assert.Contains("char_missing", issue.Message);
    }

    [Fact]
    public void PersonaggioMancanteNelBloccoMostraProduceUnSoloErrore()
    {
        (Project project, Scene scene) = CreateProjectWithScene();
        scene.Blocks.Add(new ShowCharacterBlock
        {
            CharacterId = "char_missing",
            ExpressionId = "espr_missing"
        });

        ValidationIssue issue = Assert.Single(ProjectValidator.Validate(project).Issues);

        Assert.Equal(ValidationCode.MissingCharacter, issue.Code);
        Assert.Equal(ValidationSeverity.Error, issue.Severity);
    }

    [Fact]
    public void EspressioneDiUnAltroPersonaggioRisultaMancante()
    {
        (Project project, Scene scene) = CreateProjectWithScene();
        Character anna = project.AddCharacter("Anna");
        Character bruno = project.AddCharacter("Bruno");
        string expressionId = bruno.AddExpression("Felice", "bruno-felice.png").Id;
        ShowCharacterBlock block = new() { CharacterId = anna.Id, ExpressionId = expressionId };
        scene.Blocks.Add(block);

        ValidationIssue issue = Assert.Single(ProjectValidator.Validate(project).Issues);

        Assert.Equal(ValidationCode.MissingExpression, issue.Code);
        Assert.Equal(ValidationSeverity.Error, issue.Severity);
        Assert.Equal(block.Id, issue.BlockId);
        Assert.Contains($"personaggio '{anna.Name}' ({anna.Id})", issue.Message);
        Assert.Contains(expressionId, issue.Message);
    }

    [Fact]
    public void SfondoMancanteProduceUnErrore()
    {
        (Project project, Scene scene) = CreateProjectWithScene();
        BackgroundBlock block = new() { BackgroundId = "bg_missing" };
        scene.Blocks.Add(block);

        ValidationIssue issue = Assert.Single(ProjectValidator.Validate(project).Issues);

        Assert.Equal(ValidationCode.MissingBackground, issue.Code);
        Assert.Equal(ValidationSeverity.Error, issue.Severity);
        Assert.Equal(block.Id, issue.BlockId);
        Assert.Contains("bg_missing", issue.Message);
    }

    [Fact]
    public void MusicaMancanteProduceUnErrore()
    {
        (Project project, Scene scene) = CreateProjectWithScene();
        MusicBlock block = new() { MusicId = "mus_missing" };
        scene.Blocks.Add(block);

        ValidationIssue issue = Assert.Single(ProjectValidator.Validate(project).Issues);

        Assert.Equal(ValidationCode.MissingMusic, issue.Code);
        Assert.Equal(ValidationSeverity.Error, issue.Severity);
        Assert.Equal(block.Id, issue.BlockId);
        Assert.Contains("mus_missing", issue.Message);
    }

    [Fact]
    public void ScenaVuotaProduceUnAvvisoNonBloccante()
    {
        (Project project, Scene scene) = CreateProjectWithScene();

        ValidationResult result = ProjectValidator.Validate(project);
        ValidationIssue issue = Assert.Single(result.Issues);

        Assert.Equal(ValidationCode.EmptyScene, issue.Code);
        Assert.Equal(ValidationSeverity.Warning, issue.Severity);
        Assert.Equal(scene.Id, issue.SceneId);
        Assert.Null(issue.BlockId);
        Assert.Contains($"Scena '{scene.Name}' ({scene.Id})", issue.Message);
        Assert.False(result.HasErrors);
    }

    [Fact]
    public void ValidazioniRipetuteNonModificanoIlProgetto()
    {
        (Project project, Scene scene) = CreateProjectWithScene();
        BackgroundBlock block = new() { BackgroundId = "bg_missing" };
        scene.Blocks.Add(block);
        string? startSceneId = project.StartSceneId;

        ValidationResult first = ProjectValidator.Validate(project);
        ValidationResult second = ProjectValidator.Validate(project);

        Assert.Equal(first.Issues.ToArray(), second.Issues.ToArray());
        Assert.Equal(startSceneId, project.StartSceneId);
        Assert.Single(project.Scenes);
        Assert.Single(scene.Blocks);
        Assert.Same(block, scene.Blocks[0]);
        Assert.Equal("bg_missing", block.BackgroundId);
        Assert.True(first.HasErrors);
    }

    /// <summary>Simula un blocco che conserva il riferimento a una variabile.</summary>
    private sealed class VariableReferenceTestBlock : Block, IVariableReferencingBlock
    {
        public required string VariableId { get; set; }

        public override string Label => "Riferimento variabile";

        public override string Summary(Project project) => Label;

        public override void Accept(IBlockVisitor visitor)
        {
        }
    }

    private static (Project Project, Scene Scene) CreateProjectWithScene()
    {
        Project project = new() { Title = "Prova" };
        Scene scene = project.AddScene("Inizio");
        return (project, scene);
    }

    [Fact]
    public void VariabileMancanteProduceUnErroreConScenaEBlocco()
    {
        (Project project, Scene scene) = CreateProjectWithScene();
        VariableReferenceTestBlock block = new() { VariableId = "var_missing" };
        scene.Blocks.Add(block);

        ValidationResult result = ProjectValidator.Validate(project);
        ValidationIssue issue = Assert.Single(result.Issues);

        Assert.Equal(ValidationCode.MissingVariable, issue.Code);
        Assert.Equal(ValidationSeverity.Error, issue.Severity);
        Assert.Equal(scene.Id, issue.SceneId);
        Assert.Equal(block.Id, issue.BlockId);
        Assert.Contains($"Scena '{scene.Name}' ({scene.Id})", issue.Message);
        Assert.Contains($"blocco 1 '{block.Label}' ({block.Id})", issue.Message);
        Assert.Contains("var_missing", issue.Message);
        Assert.True(result.HasErrors);
    }

    [Fact]
    public void VariabileEsistenteNonProduceProblemi()
    {
        (Project project, Scene scene) = CreateProjectWithScene();
        string variableId = project.AddNumberVariable("Punti", 0m).Id;
        scene.Blocks.Add(new VariableReferenceTestBlock { VariableId = variableId });

        ValidationResult result = ProjectValidator.Validate(project);

        Assert.Empty(result.Issues);
        Assert.False(result.HasErrors);
    }
}