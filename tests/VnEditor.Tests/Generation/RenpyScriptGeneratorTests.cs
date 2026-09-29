using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.Domain;
using VnEditor.Domain.Characters;
using VnEditor.Domain.Scenes;
using VnEditor.Domain.Scenes.Blocks;
using VnEditor.Infrastructure.Generation;


namespace VnEditor.Tests.Generation;

/// <summary>
/// Verifica struttura, ordine delle definizioni, escaping e formattazione
/// dello script generato da un progetto completo.
/// </summary>
public class RenpyScriptGeneratorTests
{
    private readonly Project _project;
    private readonly ScriptWriter _writer;
    private readonly RenpyScriptGenerator _generator;
    public RenpyScriptGeneratorTests()
    {
        _project = new Project { Title = "Test" };
        _writer = new ScriptWriter();
        _generator = new RenpyScriptGenerator(_writer);
    }


    [Fact]
    public void UnaScenaVuotaGeneraLoScriptCompleto()
    {
        _project.AddScene("Inizio");

        AssertScript(
            "label start:",
            "    jump scene_1",
            "",
            "label scene_1:",
            "    return",
            "",
            "");
    }

    [Fact]
    public void UnProgettoConTuttiIBlocchiAttualiGeneraLoScriptRigaPerRiga()
    {
        Character anna = _project.AddCharacter("Anna");
        Character marco = _project.AddCharacter("Marco");
        marco.Color = "#c8ffc8";

        string expressionId = anna.AddExpression("Felice", "felice.png").Id;
        string backgroundId = _project.AddBackground("Aula", "aula.jpg").Id;
        string musicId = _project.AddMusic("Tema", "tema.ogg").Id;

        Scene firstScene = _project.AddScene("Prima");
        Scene secondScene = _project.AddScene("Seconda");

        firstScene.Blocks.Add(new BackgroundBlock { BackgroundId = backgroundId });
        firstScene.Blocks.Add(new MusicBlock { MusicId = musicId });
        firstScene.Blocks.Add(new ShowCharacterBlock
        {
            CharacterId = anna.Id,
            ExpressionId = expressionId
        });
        firstScene.Blocks.Add(new DialogueBlock
        {
            CharacterId = anna.Id,
            Text = "Ciao [nome]!"
        });

        secondScene.Blocks.Add(new DialogueBlock
        {
            CharacterId = marco.Id,
            Text = "Fine."
        });

        AssertScript(
            "define char_1 = Character(\"Anna\", color=\"#ffffff\")",
            "",
            "define char_2 = Character(\"Marco\", color=\"#c8ffc8\")",
            "",
            "label start:",
            "    jump scene_1",
            "",
            "label scene_1:",
            "    scene bg_1 with fade",
            "    play music \"audio/tema.ogg\"",
            "    show char_1 espr_1",
            "    char_1 \"Ciao [[nome]!\"",
            "    return",
            "",
            "label scene_2:",
            "    char_2 \"Fine.\"",
            "    return",
            "",
            "");
    }

    [Fact]
    public void IlDialogoRispettaEscapingIndentazioneEFineRiga()
    {
        Character anna = _project.AddCharacter("Anna");
        Scene scene = _project.AddScene("Inizio");

        scene.Blocks.Add(new DialogueBlock
        {
            CharacterId = anna.Id,
            Text = "A \"B\" {C} [D]\nE\\F"
        });

        AssertScript(
            "define char_1 = Character(\"Anna\", color=\"#ffffff\")",
            "",
            "label start:",
            "    jump scene_1",
            "",
            "label scene_1:",
            "    char_1 \"A \\\"B\\\" {{C} [[D] E\\\\F\"",
            "    return",
            "",
            "");
    }

    [Fact]
    public void IlNomeDelPersonaggioConVirgoletteVieneProtetto()
    {
        _project.AddCharacter("An\"na");
        _project.AddScene("Inizio");

        AssertScript(
            "define char_1 = Character(\"An\\\"na\", color=\"#ffffff\")",
            "",
            "label start:",
            "    jump scene_1",
            "",
            "label scene_1:",
            "    return",
            "",
            "");
    }

    private void AssertScript(params string[] expectedLines)
    {
        _generator.Generate(_project);
        string[] actualLines = _writer.Build().Split('\n');

        Assert.Equal(expectedLines.Length, actualLines.Length);
        for (int line = 0; line < expectedLines.Length; line++)
            Assert.Equal(expectedLines[line], actualLines[line]);
    }


}
   

