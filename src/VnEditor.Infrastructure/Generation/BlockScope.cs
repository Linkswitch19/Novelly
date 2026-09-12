using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using VnEditor.Application.Generation;

namespace VnEditor.Infrastructure.Generation;
/// <summary>
/// Rappresenta un blocco indentato aperto. Chiudendolo, l'indentazione torna
/// al livello precedente. Non va istanziato direttamente: lo restituisce
/// <see cref="ScriptWriter.Block"/>.
/// </summary>
public sealed class BlockScope : IDisposable
{
    private readonly IIndentationScope _scope;
    private bool _closed;
    internal BlockScope(IIndentationScope scope) => this._scope = scope;

    public void Dispose()
    {
        if (this._closed) return;
        this._closed = true;
        this._scope.CloseBlock();
    }

}