using System;
using System.Collections.Generic;
using System.Text;

namespace VnEditor.Domain.Scenes.Blocks;

/// <summary>Espone l'ID della variabile richiamata da un blocco.</summary>
public interface IVariableReferencingBlock
{
    string VariableId { get; }
}
