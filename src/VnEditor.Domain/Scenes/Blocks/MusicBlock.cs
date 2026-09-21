using System;
using System.Collections.Generic;
using System.Text;

namespace VnEditor.Domain.Scenes.Blocks;
/// <summary>Fa partire un brano musicale di sottofondo.</summary>
public sealed class MusicBlock : Block
{
    public required string MusicId {  get; set; }

    public override string Label => "Musica";
    public override string Summary(Project project) =>
        $"Musica → {project.FindMusic(this.MusicId)?.Name ?? "?"}";

    public override void Accept(IBlockVisitor visitor) => visitor.Visit(this);
    

}
