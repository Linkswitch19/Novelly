using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.UI.Selection;
using VnEditor.UI.ViewModels;
using VnEditor.UI.Views;

namespace VnEditor.UI;

/// <summary>
/// Funge da punto di ingresso centrale ("cabina di regia") per l'assemblaggio dell'interfaccia utente.
/// Implementa il pattern Composition Root: è il luogo in cui tutte le classi, le dipendenze e i ViewModel 
/// vengono istanziati e collegati tra loro prima che l'applicazione appaia sullo schermo.
/// </summary>
public static class CompositionRoot
{
    /// <summary>
    /// Costruisce il "motore logico" principale dell'applicazione, assemblando i vari sotto-componenti.
    /// </summary>
    /// <returns>Il MainViewModel completamente configurato e pronto all'uso.</returns>
    private static MainViewModel CreateMainViewModel()
    {
        WorkspaceSelection selection = new();
        selection.SelectSection(ProjectSection.Scenes);

        ProjectPanelViewModel projectPanel = new(selection);
        return new MainViewModel(selection, projectPanel);
    }
    /// <summary>
    /// Crea la finestra fisica vera e propria (la UI scritta in XAML) e la collega al suo motore logico.
    /// </summary>
    /// <returns>L'istanza della finestra principale pronta per essere mostrata a schermo.</returns>
    public static MainWindow CreateMainWindow()
    {
        return new MainWindow
        {
            DataContext = CreateMainViewModel()

        };
    }
}
