namespace VnEditor.Domain.Validation;

/// <summary>Raccoglie tutti i problemi trovati durante una validazione.</summary>
public sealed class ValidationResult
{
    public ValidationResult(IEnumerable<ValidationIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);

        Issues = Array.AsReadOnly(issues.ToArray());
        HasErrors = Issues.Any(issue => issue.Severity == ValidationSeverity.Error);
    }

    public IReadOnlyList<ValidationIssue> Issues { get; }

    public bool HasErrors { get; }
}