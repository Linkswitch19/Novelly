using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.Application.Generation;

namespace VnEditor.Infrastructure.Generation;

public sealed class ScriptWriter : IScriptWriter,IIndentationScope
{
    private const string IndentUnit = "    "; // 4 spazi, mai tabulazioni
    private const char LineEnding = '\n'; // mai \r\n, anche su Windows

    private readonly StringBuilder _builder = new();
    private int _level;


    public void BlankLIne() => this._builder.Append(LineEnding);

    /// <summary>Restituisce lo script completo.</summary>
    public string Build() => this._builder.ToString();



    public IDisposable Block(string header)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            throw new ArgumentException("L'intestazione non puo essere vuota");

        }
        Line(header);
        _level++;
        return new BlockScope(this);

    }

    void IIndentationScope.CloseBlock()
    {
        if (this._level == 0)
            throw new InvalidOperationException("Chiusura blocco mai aperto");
        this._level--;
    }

    // appende identazione e poi il testo
    public void Line(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        for (int i = 0; i < _level; i++)
            this._builder.Append(IndentUnit);
        this._builder.Append(text).Append(LineEnding);
    }
}
