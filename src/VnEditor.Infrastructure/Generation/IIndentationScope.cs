namespace VnEditor.Infrastructure.Generation;

/// <summary>
/// Ciò che serve per chiudere un blocco indentato: nient'altro.
/// Implementata da <see cref="ScriptWriter"/>, consumata da <see cref="BlockScope"/>.
/// </summary>
internal interface IIndentationScope
{
    void CloseBlock();
}