namespace VnEditor.Domain.Validation;

/// <summary>
/// Descrive un problema trovato nel progetto.
/// BlockId è null quando il problema riguarda l'intera scena.
/// </summary>
public sealed record ValidationIssue(
    ValidationCode Code,
    ValidationSeverity Severity,
    string Message,
    string SceneId,
    string? BlockId);