using System;
using System.Collections.Generic;
using System.Text;

namespace VnEditor.Domain.Variables;
/// <summary>Una variabile booleana con il proprio valore iniziale.</summary>
public sealed class BooleanVariable : Variable
{
    public bool InitialValue { get; set; }
}
