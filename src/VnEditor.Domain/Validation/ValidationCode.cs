namespace VnEditor.Domain.Validation;

/// <summary>Identifica la regola che ha prodotto un problema di validazione.</summary>
public enum ValidationCode
{
    MissingCharacter,
    MissingExpression,
    MissingBackground,
    MissingMusic,
    MissingVariable,
    EmptyScene
}