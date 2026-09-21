

using VnEditor.Domain.Characters;

namespace VnEditor.Domain.Scenes.Blocks;

/// <summary>Una battuta pronunciata da un personaggio.</summary>
public sealed class DialogueBlock : Block
{
    public required string CharacterId { get; set; }
    public required string Text { get; set; }

    public override string Label => "Dialogo";
    public override string Summary(Project project) =>
        $"{project.FindCharacter(this.CharacterId)?.Name ?? "?"}: {this.Text}";
    public override void Accept(IBlockVisitor visitor) => visitor.Visit(this);
    

}
