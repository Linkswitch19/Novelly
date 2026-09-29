using VnEditor.Domain;
using VnEditor.Domain.Assets;
using VnEditor.Domain.Characters;
using VnEditor.Domain.Scenes;
using VnEditor.Domain.Scenes.Blocks;
using VnEditor.Infrastructure.Generation;

namespace VnEditor.Tests.Generation;

/// <summary>
/// Verifica lo script completo generato da ciascun tipo concreto di Block attuale.
/// </summary>
public class RenpyBlockGenerationTests
{
    [Fact]
    public void BackgroundBlock_GeneraLaScenaConFade()
    {
        Project project = new() { Title = "Test" };
        Background background = project.AddBackground("Aula", "aula.jpg");
        Scene scene = project.AddScene("Inizio");
        scene.Blocks.Add(new BackgroundBlock { BackgroundId = background.Id });

        AssertScript(project,
            "label start:",
            "    jump scene_1",
            "",
            "label scene_1:",
            "    scene bg_1 with fade",
            "    return",
            "",
            "");
    }

    [Fact]
    public void ShowCharacterBlock_GeneraIlPersonaggioConLaSuaEspressione()
    {
        Project project = new() { Title = "Test" };
        Character anna = project.AddCharacter("Anna");
        Expression expression = anna.AddExpression("Felice", "felice.png");
        Scene scene = project.AddScene("Inizio");

        scene.Blocks.Add(new ShowCharacterBlock
        {
            CharacterId = anna.Id,
            ExpressionId = expression.Id
        });

        AssertScript(project,
            "define char_1 = Character(\"Anna\", color=\"#ffffff\")",
            "",
            "label start:",
            "    jump scene_1",
            "",
            "label scene_1:",
            "    show char_1 espr_1",
            "    return",
            "",
            "");
    }

    [Fact]
    public void DialogueBlock_GeneraIlDialogoConEscaping()
    {
        Project project = new() { Title = "Test" };
        Character anna = project.AddCharacter("Anna");
        Scene scene = project.AddScene("Inizio");

        scene.Blocks.Add(new DialogueBlock
        {
            CharacterId = anna.Id,
            Text = "Ciao {amico}!"
        });

        AssertScript(project,
            "define char_1 = Character(\"Anna\", color=\"#ffffff\")",
            "",
            "label start:",
            "    jump scene_1",
            "",
            "label scene_1:",
            "    char_1 \"Ciao {{amico}!\"",
            "    return",
            "",
            "");
    }

    [Fact]
    public void MusicBlock_GeneraIlPercorsoAudio()
    {
        Project project = new() { Title = "Test" };
        MusicTrack track = project.AddMusic("Tema", "tema.ogg");
        Scene scene = project.AddScene("Inizio");
        scene.Blocks.Add(new MusicBlock { MusicId = track.Id });

        AssertScript(project,
            "label start:",
            "    jump scene_1",
            "",
            "label scene_1:",
            "    play music \"audio/tema.ogg\"",
            "    return",
            "",
            "");
    }

    private static void AssertScript(Project project, params string[] expectedLines)
    {
        ScriptWriter writer = new();
        RenpyScriptGenerator generator = new(writer);
        generator.Generate(project);

        string[] actualLines = writer.Build().Split('\n');

        Assert.Equal(expectedLines.Length, actualLines.Length);
        for (int line = 0; line < expectedLines.Length; line++)
            Assert.Equal(expectedLines[line], actualLines[line]);
    }
}