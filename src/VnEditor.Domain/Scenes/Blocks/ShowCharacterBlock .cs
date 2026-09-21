using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.Domain.Characters;

namespace VnEditor.Domain.Scenes.Blocks;
/// <summary>Mostra un personaggio con una data espressione.</summary>
public sealed class ShowCharacterBlock : Block
{
    public required string CharacterId { get; set; }
    public required string ExpressionId { get; set; }

    public override string Label => "Mostra personaggio";

    public override void Accept(IBlockVisitor visitor) => visitor.Visit(this);

    public override string Summary(Project project)
    {
        Character? character  = project.FindCharacter(this.CharacterId);
        Expression? expression = character?.FindExpression(this.ExpressionId);
        return $"Mostra → {character?.Name ?? "?"} ({expression?.Name ?? "?"})";
    }
}
