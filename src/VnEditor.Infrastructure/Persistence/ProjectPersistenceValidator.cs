using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.Domain;

namespace VnEditor.Infrastructure.Persistence;

/// <summary>
/// Funge da coordinatore principale per i controlli di integrità del progetto 
/// legati alla persistenza (salvataggio e caricamento).
/// Ha il compito di aggregare e delegare le verifiche strutturali ai validatori specializzati.
/// </summary>
internal static class ProjectPersistenceValidator
{
    /// <summary>
    /// Esegue l'intera suite di validazioni necessarie per garantire che i dati del progetto 
    /// siano formalmente corretti, avviando in sequenza i controlli sui contatori e sui file multimediali.
    /// </summary>
    /// <param name="project">Il progetto da sottoporre al processo di controllo globale.</param>
    /// <exception cref="ArgumentNullException">Lanciata nel caso in cui venga passato un riferimento di progetto nullo.</exception>
    internal static void Validate(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);

        ProjectIdCountersValidator.Validate(project);
        ProjectAssetPathsValidator.Validate(project);
    }
}
