using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.Domain;
using VnEditor.Domain.Generation;
using VnEditor.Domain.Scenes.Blocks;

namespace VnEditor.Infrastructure.Generation;
/// <summary>
/// Traduce i blocchi del dominio in righe di script Ren'Py.
/// Implementa <see cref="IBlockVisitor"/> quindi il compilatore garantisce
/// che ogni tipo di blocco abbia la sua traduzione: dimenticare un blocco
/// nuovo significa un errore di compilazione, non un bug silenzioso.
/// </summary>

public sealed class RenpyScriptVisitor : IBlockVisitor
{
    private readonly IScriptWriter _writer;

    private readonly Project _project;
    
    public RenpyScriptVisitor(IScriptWriter writer, Project project)
    {
        this._writer = writer;
        this._project = project;    
    }

    public void Visit(BackgroundBlock block) =>
        _writer.Line(RenpySyntax.Scene(block.BackgroundId));
    public void Visit(ShowCharacterBlock block) =>
        _writer.Line(RenpySyntax.Show(block.CharacterId,block.ExpressionId));

    public void Visit(DialogueBlock block) =>
        _writer.Line(RenpySyntax.Dialogue(block.CharacterId,block.Text));
    public void Visit(MusicBlock block)
    {
        Domain.Assets.MusicTrack? track = _project.FindMusic(block.MusicId);
        _writer.Line(RenpySyntax.PlayMusic(track?.FileName));
    }


}