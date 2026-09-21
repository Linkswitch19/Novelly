using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.Domain;
using VnEditor.Domain.Characters;
using VnEditor.Domain.Generation;
using VnEditor.Domain.Scenes;
using VnEditor.Domain.Scenes.Blocks;

namespace VnEditor.Infrastructure.Generation;
/// <summary>
/// Genera lo script Ren'Py completo a partire dal modello di progetto.
/// Scrive un unico file script.rpy, rigenerato da zero a ogni chiamata.
/// </summary>

public sealed class RenpyScriptGenerator
{
    private readonly IScriptWriter _writer;

    public RenpyScriptGenerator (IScriptWriter writer)
    {
        this._writer = writer;
    }

    public void Generate (Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        GenerateCharacterDefinitions(project);
        GenerateStartLabel(project);
        GenerateScenes(project.Scenes, new RenpyScriptVisitor(_writer, project));


    }

    private void GenerateStartLabel(Project project)
    {

        using (this._writer.Block(RenpySyntax.Label("start")))
            this._writer.Line(RenpySyntax.Jump(project.StartSceneId));
        this._writer.BlankLine();
    }

    private void GenerateCharacterDefinitions(Project project)
    {
        foreach (Character character in project.Characters)
        {
            //define r = Character( "Rin", color = "#c8ffc8")
            _writer.Line(RenpySyntax.DefineCharacter(character.Id,character.Name,character.Color));
            this._writer.BlankLine();
        }
    }

    private void GenerateScenes (List<Scene> scenes , RenpyScriptVisitor visitor)
    {

        foreach (Scene scene in scenes)
        {
            using (this._writer.Block(RenpySyntax.Label(scene.Id)))
            {
                foreach (Block block in scene.Blocks)
                
                    block.Accept(visitor);
                if (scene.Blocks.Count == 0 || !scene.Blocks[^1].EndsScene) this._writer.Line(RenpySyntax.Return);

            }
            this._writer.BlankLine();

        }
    }


}
