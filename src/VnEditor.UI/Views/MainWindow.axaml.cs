using Avalonia.Controls;
using System.Threading.Tasks;
using VnEditor.UI.ViewModels;

namespace VnEditor.UI.Views;

/// <summary>
/// Visualizza la finestra principale e collega la sua chiusura
/// alla conferma delle modifiche non salvate.
/// Impedisce richieste di conferma contemporanee.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Indica se è già in corso una richiesta di conferma della chiusura.
    /// Serve a ignorare ulteriori tentativi di chiusura finché la conferma
    /// precedente non è terminata.
    /// </summary>
    private bool _closeCheckInProgress;
    /// <summary>
    /// Indica che l'utente ha già approvato la chiusura, così che la chiusura
    /// effettiva non venga intercettata una seconda volta da <see cref="OnClosing"/>.
    /// </summary>
    private bool _closeApproved;
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Intercetta la chiusura della finestra e la sospende finché il view model
    /// non conferma che si può procedere (ad esempio dopo aver gestito le
    /// modifiche non salvate). Se la conferma viene negata la finestra resta aperta.
    /// </summary>
    /// <param name="eventArgs">
    /// Dati dell'evento di chiusura, usati per annullare la chiusura
    /// mentre è in corso la conferma.
    /// </param>
    /// <remarks>
    /// Il metodo è <c>async void</c> perché sovrascrive un metodo che restituisce
    /// <c>void</c>. La chiusura viene annullata subito in modo sincrono, poi
    /// ripetuta con <see cref="Window.Close()"/> solo a conferma ottenuta.
    /// Le chiamate arrivate durante una conferma già in corso vengono annullate
    /// senza avviare una seconda richiesta.
    /// </remarks>
    protected override async void OnClosing(WindowClosingEventArgs eventArgs)
    {
        base.OnClosing(eventArgs);

        if (eventArgs.Cancel || this._closeApproved)
            return;

        if (DataContext is not MainViewModel viewModel)
            return;

        eventArgs.Cancel = true;

        if (this._closeCheckInProgress)
            return;

        this._closeCheckInProgress = true;

        try
        {
            await Task.Yield();

            bool canClose = await viewModel.FileMenu.RequestCloseAsync();

            if (!canClose)
                return;

            this._closeApproved = true;
            Close();
        }
        finally
        {
            this._closeApproved = false;
            this._closeCheckInProgress = false;
        }
    }
}