using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.Domain;

namespace VnEditor.AppService.Generation;
/// <summary>
/// Coordina la generazione completa del progetto Ren'Py:
/// inizializzazione, pulizia, generazione script, copia asset.
/// </summary>
public interface IScriptGenerator
{
    Task GenerateAsync(Project project, String projectFolder,
                       CancellationToken ct= default);
}
