using System;
using System.Collections.Generic;
using System.Text;
using VnEditor.AppService.History;
using VnEditor.AppService.Interaction;
using VnEditor.AppService.Persistence;
using VnEditor.AppService.Projects;
using VnEditor.AppService.Projects.Actions;
using VnEditor.AppService.Sessions;
using VnEditor.Infrastructure.History;
using VnEditor.Infrastructure.Persistence;
using VnEditor.Infrastructure.Projects;
using VnEditor.Infrastructure.Serialization;
using VnEditor.UI.Dialogs;
using VnEditor.UI.Projects;
using VnEditor.UI.Selection;
using VnEditor.UI.ViewModels;
using VnEditor.UI.Views;

namespace VnEditor.UI;

/// <summary>
/// Compone la finestra principale e le sue dipendenze.
/// Condivide sessione e persistenza tra le azioni File,
/// configura la gestione degli errori e rilascia i componenti
/// alla chiusura della finestra.
/// </summary>
public static class CompositionRoot
{
    /// <summary>
    /// Costruisce il "motore logico" principale dell'applicazione, assemblando i vari sotto-componenti.
    /// </summary>
    /// <param name="session">Sessione del progetto condivisa con le azioni del menu File.</param>
    /// <param name="fileMenu">View model del menu File già configurato.</param>
    /// <returns>Il MainViewModel completamente configurato e pronto all'uso.</returns>
    private static MainViewModel CreateMainViewModel(ProjectSession session,
    FileMenuViewModel fileMenu)
    {
        WorkspaceSelection selection = new();
        selection.SelectSection(ProjectSection.Scenes);

        ProjectPanelViewModel projectPanel = new(selection);

        return new MainViewModel(selection, projectPanel, session, fileMenu);
    }
    /// <summary>
    /// Crea la finestra fisica vera e propria (la UI scritta in XAML) e la collega al suo motore logico.
    /// Alla chiusura della finestra rilascia view model, menu File e sessione.
    /// </summary>
    /// <returns>L'istanza della finestra principale pronta per essere mostrata a schermo.</returns>
    public static MainWindow CreateMainWindow()
    {
        MainWindow window = new();
        ProjectSession session = new();

        IProjectDialogs dialogs = new AvaloniaProjectDialogs(window);
        IProjectPersistence persistence =
            new FileProjectPersistence(new ProjectJsonSerializer());

        string templatePath = Path.Combine(
            AppContext.BaseDirectory, "resources", "renpy_template");

        ProjectCreationService creationService = new(templatePath);
        ProjectSaveAsService saveAsService = new(templatePath, persistence);
        IProjectWorkspace workspace =
            new FileProjectWorkspace(creationService, saveAsService);

        FileMenuViewModel fileMenu =
            CreateFileMenuViewModel(session, dialogs, persistence, workspace);

        MainViewModel viewModel = CreateMainViewModel(session, fileMenu);
        window.DataContext = viewModel;

        window.Closed += (_, _) =>
        {
            viewModel.Dispose();
            fileMenu.Dispose();
            session.Dispose();
        };

        return window;
    }

    /// <summary>
    /// Costruisce il view model del menu File, creando le azioni Nuovo, Apri,
    /// Salva, Salva con nome e Chiudi. Ogni azione è avvolta in un
    /// <see cref="ErrorHandlingProjectFileAction"/> che mostra all'utente un
    /// dialogo di errore con un messaggio dedicato. Le azioni condividono la
    /// stessa sessione e lo stesso controllo delle modifiche non salvate.
    /// </summary>
    /// <param name="session">Sessione del progetto condivisa da tutte le azioni.</param>
    /// <param name="dialogs">Servizio per mostrare dialoghi di conferma, selezione ed errore.</param>
    /// <param name="persistence">Servizio per leggere e salvare i file di progetto.</param>
    /// <param name="workspace">Servizio per creare un nuovo progetto e salvarlo in una nuova cartella.</param>
    /// <returns>Il <see cref="FileMenuViewModel"/> configurato con tutte le azioni File.</returns>
    private static FileMenuViewModel CreateFileMenuViewModel(ProjectSession session,
    IProjectDialogs dialogs, IProjectPersistence persistence, IProjectWorkspace workspace)
    {
        IProjectHistoryFactory historyFactory = new PassThroughProjectHistoryFactory();
        ProjectFileErrorPresenter errors = new(dialogs);

        IProjectFileAction saveAction = new ErrorHandlingProjectFileAction(
            new SaveProjectAction(session, persistence),
            errors,
            "Unable to save project",
            "The project could not be saved. Check the folder and try again.");

        UnsavedChangesGuard unsavedChanges = new(session, dialogs, saveAction);

        IProjectFileAction newAction = new ErrorHandlingProjectFileAction(
            new NewProjectAction(session, dialogs, workspace, historyFactory, unsavedChanges),
            errors,
            "Unable to create project",
            "The project could not be created. Check the project name and selected location, then try again.");

        IProjectFileAction openAction = new ErrorHandlingProjectFileAction(
            new OpenProjectAction(session, dialogs, persistence, historyFactory, unsavedChanges),
            errors,
            "Unable to open project",
            "The project could not be opened. Check the selected folder and try again.");

        IProjectFileAction saveAsAction = new ErrorHandlingProjectFileAction(
            new SaveProjectAsAction(session, dialogs, workspace),
            errors,
            "Unable to save project",
            "The project could not be saved. Choose a new or empty destination folder and try again.");

        IProjectFileAction closeAction = new ErrorHandlingProjectFileAction(
            new ConfirmProjectCloseAction(unsavedChanges),
            errors,
            "Unable to close project",
            "The project could not be closed. Please try again.");

        return new FileMenuViewModel(
            session, newAction, openAction, saveAction, saveAsAction, closeAction);
    }
}
