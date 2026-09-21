using VnEditor.Domain;
using VnEditor.Domain.Generation;
using VnEditor.Domain.Scenes.Blocks;
/// <summary>Cambia lo sfondo della scena.</summary>
public sealed class BackgroundBlock : Block
{
    public required string BackgroundId {  get; set; }

    public override string Label =>  "Sfondo";

    public override void Accept(IBlockVisitor visitor) => visitor.Visit(this);
    

    public override string Summary(Project project)
    {
        return $"Sfondo → {project.FindBackground(BackgroundId)?.Name ?? "?"}";
    }
}