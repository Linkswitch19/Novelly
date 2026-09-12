using System;
using System.Collections.Generic;
using System.Text;

namespace VnEditor.Infrastructure.Generation;
/// <summary>
/// Scrive lo script su disco. Separato da <see cref="ScriptWriter"/>:
/// costruire il testo e salvarlo sono due responsabilità diverse.
/// </summary>
public static class ScriptFileWriter
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier:false);
    public static Task WriteAsync(string path ,string content, CancellationToken ct = default)
        => File.WriteAllTextAsync(path, content, Utf8NoBom, ct);
}