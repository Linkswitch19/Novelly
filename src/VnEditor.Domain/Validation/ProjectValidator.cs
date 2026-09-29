using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.Domain.Scenes;
using VnEditor.Domain.Scenes.Blocks;

namespace VnEditor.Domain.Validation;

/// <summary>
/// Analizza le scene e i riferimenti dei blocchi, comprese le variabili,
/// senza modificare il progetto.
/// </summary>
public static class ProjectValidator
{
    /// <summary>
    /// Esegue un'analisi completa del progetto, ispezionando ogni scena e i relativi blocchi.
    /// Segnala un avviso (Warning) per le scene prive di contenuto e delega al <see cref="BlockValidationVisitor"/> 
    /// il controllo di integrità referenziale per le istruzioni all'interno delle scene.
    /// </summary>
    /// <param name="project">Il progetto da analizzare.</param>
    /// <returns>
    /// Un oggetto <see cref="ValidationResult"/> contenente l'elenco di tutte le anomalie 
    /// (errori bloccanti o semplici avvisi) riscontrate durante la scansione.
    /// </returns>
    /// <exception cref="ArgumentNullException">Lanciata se il progetto fornito è nullo.</exception>
    public static ValidationResult Validate(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);

        List<ValidationIssue> issues = [];
        foreach (Scene scene in project.Scenes)
        {
            // Segnala un Warning se la scena esiste ma non contiene alcun blocco narrativo.
            if (scene.Blocks.Count == 0)
            {
                string message = $"Scena '{scene.Name}' ({scene.Id}): non contiene blocchi.";
                issues.Add(new ValidationIssue(
                    ValidationCode.EmptyScene,
                    ValidationSeverity.Warning,
                    message,
                    scene.Id,
                    null));

                continue;
            }
            // Se la scena ha dei blocchi, li ispeziona uno per uno tramite il Visitor.
            for (int index = 0; index < scene.Blocks.Count; index++)
            {
                Block block = scene.Blocks[index];
                BlockValidationVisitor visitor = new(project, scene, index + 1, issues);
                visitor.Validate(block);
            }

        }
        return new ValidationResult(issues);
    }
}
