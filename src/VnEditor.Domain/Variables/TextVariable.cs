namespace VnEditor.Domain.Variables;

public sealed class TextVariable : Variable
{
    public string InitialValue { get; set; } = string.Empty;
}