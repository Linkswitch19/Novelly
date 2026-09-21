using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.Domain;
using VnEditor.Domain.Characters;
using VnEditor.Domain.Scenes;
using VnEditor.Domain.Scenes.Blocks;
using VnEditor.Infrastructure.Generation;


namespace VnEditor.Tests.Generation;
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
    public void UnProgettoVuotoGeneraSoloStartEReturn()
    {

        this._project.AddScene("Inizio");

        this._generator.Generate(this._project);
        string script = _writer.Build();

        Assert.Contains("label start:", script);
        Assert.Contains("jump scene_1", script);
        Assert.Contains("label scene_1:", script);
        Assert.Contains("return", script);

    }

    [Fact]
    public void IPersonaggiVengonoDefiniti()
    {

        this._project.AddCharacter("Anna");
        this._project.AddScene("Inizio");
        this._generator.Generate(this._project);
        string script = _writer.Build();
        Assert.Contains("define char_1 = Character(\"Anna\", color=\"#ffffff\")", script);

    }

    [Fact]
    public void IlDialogoVieneScrittoCon​EscapingCorretto()
    {
    
        Character anna = this._project.AddCharacter("Anna");
        anna.AddExpression("felice", "char_1 espr_1.png");
        Scene scene = this._project.AddScene("Inizio");

        scene.Blocks.Add(new DialogueBlock
        {
            CharacterId = "char_1",
            Text = "Ciao {amico}!"
        });

        this._generator.Generate(this._project);
        string script = _writer.Build();

        Assert.Contains("char_1 \"Ciao {{amico}!\"", script);
    }


    [Fact]
    public void LosfonDoVieneGeneratoConFade()
    {
        
        this._project.AddBackground("Aula", "bg_1.jpg");
        Scene scene = this._project.AddScene("Inizio");

        scene.Blocks.Add(new BackgroundBlock { BackgroundId = "bg_1" });

        this._generator.Generate(this._project);
        string script = this._writer.Build();

        Assert.Contains("scene bg_1 with fade", script);
    }

    [Fact]
    public void LaMusicaVieneGenerataConIlPercorsoCorretto()
    {
       
        this._project.AddMusic("Tema", "mus_1.ogg");
        Scene scene = this._project.AddScene("Inizio");

        scene.Blocks.Add(new MusicBlock { MusicId = "mus_1" });

        this._generator.Generate(this._project);
        string script = this._writer.Build();

        Assert.Contains("play music \"audio/mus_1.ogg\"", script);
    }

    [Fact]
    public void UnaScenaSenzaBlocchiTerminaConReturn()
    {
        
        this._project.AddScene("Inizio");

        this._generator.Generate(this._project);
        string script = this._writer.Build();

        Assert.Contains("return", script);
    }

    [Fact]
    public void PiuScenesGeneranoPiuLabel()
    {
        
        this._project.AddScene("Prima");
        this._project.AddScene("Seconda");

        this._generator.Generate(this._project);
        string script = this._writer.Build();

        Assert.Contains("label scene_1:", script);
        Assert.Contains("label scene_2:", script);
    }


}
   

