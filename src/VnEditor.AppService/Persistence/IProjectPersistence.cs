using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.Domain;

namespace VnEditor.AppService.Persistence;

/// <summary>
/// Salva e carica un progetto nella cartella scelta dall'utente.
/// Non impone un formato di serializzazione o una tecnologia di accesso ai file.
/// </summary>
public interface IProjectPersistence
{
    /// <summary>Salva il progetto nella cartella indicata.</summary>
    Task SaveAsync(Project project, string projectFolder,CancellationToken cancellationToken = default);

    /// <summary>
    /// Carica e restituisce un nuovo progetto. Non modifica il progetto
    /// eventualmente già aperto dal chiamante.
    /// </summary>
    Task<Project> LoadAsync(
        string projectFolder,
        CancellationToken cancellationToken = default);
}
