using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.Domain.Scenes.Blocks;

namespace VnEditor.Domain.Scenes;
/// <summary>
/// Una scena della storia. Diventa una label Ren'Py, e i suoi blocchi
/// diventano le righe dentro quella label.
/// </summary>
public sealed class Scene : Entity
{

    /// <summary>Gli eventi della scena, nell'ordine in cui accadono.</summary>
    public List<Block> Blocks { get; } = [];
}
