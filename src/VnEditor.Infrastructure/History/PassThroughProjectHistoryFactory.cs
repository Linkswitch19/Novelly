using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.AppService.History;
using VnEditor.Domain;

namespace VnEditor.Infrastructure.History;

/// <summary>
/// Crea una history pass-through dedicata a ciascun progetto,
/// senza conservare la cronologia delle modifiche.
/// </summary>
public sealed class PassThroughProjectHistoryFactory : IProjectHistoryFactory
{
    /// <summary>
    /// Crea una nuova history associata all'istanza di progetto fornita.
    /// </summary>
    /// <param name="project">
    /// Progetto realmente creato o caricato da associare alla history.
    /// </param>
    /// <returns>Una nuova history pass-through associata al progetto.</returns>
    /// <exception cref="ArgumentNullException">
    /// Il progetto fornito è nullo.
    /// </exception>
    public IProjectHistory Create(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);

        return new PassThroughProjectHistory(project);
    }
}
