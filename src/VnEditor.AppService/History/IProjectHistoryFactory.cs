using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.Domain;

namespace VnEditor.AppService.History;

/// <summary>
/// Crea una history dedicata al progetto fornito.
/// Permette alla sessione di sostituire il progetto senza conoscere
/// l'implementazione concreta della cronologia.
/// </summary>
public interface IProjectHistoryFactory
{
    /// <summary>
    /// Crea una nuova history il cui progetto corrente è
    /// l'istanza fornita.
    /// </summary>
    /// <param name="project">
    /// Progetto realmente creato o caricato da associare alla history.
    /// </param>
    /// <returns>Una nuova history associata al progetto.</returns>
    /// <exception cref="ArgumentNullException">
    /// Il progetto fornito è nullo.
    /// </exception>
    IProjectHistory Create(Project project);
}
