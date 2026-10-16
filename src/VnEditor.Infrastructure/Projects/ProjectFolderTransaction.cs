using System;
using System.Collections.Generic;
using System.Text;

namespace VnEditor.Infrastructure.Projects;

/// <summary>
/// Gestisce una transazione sul file system per la creazione o duplicazione di una cartella di progetto.
/// Alloca una cartella di appoggio temporanea (staging) nella stessa cartella genitore della destinazione
/// per consentire scritture o copie protette. Al <see cref="Commit"/>, sposta atomicamente la cartella temporanea
/// al percorso definitivo; in caso di fallimento o annullamento, <see cref="Dispose"/> ripulisce l'area temporanea.
/// </summary>
internal sealed class ProjectFolderTransaction : IDisposable
{
    private readonly string _destinationFolder;
    private bool _isCommitted;
    private bool _isDisposed;

    /// <summary>
    /// Inizializza una nuova transazione di cartella validando la destinazione
    /// e creando una cartella temporanea di staging con un identificatore univoco.
    /// </summary>
    /// <param name="destinationFolder">Il percorso della directory target finale.</param>
    /// <exception cref="ArgumentException">Lanciata se il percorso non è valido o coincide con la root di un disco.</exception>
    /// <exception cref="DirectoryNotFoundException">Lanciata se la cartella genitore non esiste.</exception>
    /// <exception cref="IOException">Lanciata se la destinazione è occupata o se la cartella temporanea generata esiste già.</exception>
    internal ProjectFolderTransaction(string destinationFolder)
    {
        (string destinationPath, string parentPath) =
            ProjectFolderValidator.ValidateDestination(destinationFolder);

        this._destinationFolder = destinationPath;

        StagingFolder = Path.Combine(
            parentPath, $".novelly-{Guid.NewGuid():N}.tmp");

        if (Directory.Exists(StagingFolder) || File.Exists(StagingFolder))
            throw new IOException("The temporary folder already exists.");

        Directory.CreateDirectory(StagingFolder);
    }

    /// <summary>
    /// Ottiene il percorso assoluto della cartella temporanea in cui preparare i contenuti prima della pubblicazione finale.
    /// </summary>
    internal string StagingFolder { get; }

    /// <summary>
    /// Finalizza l'operazione promuovendo la cartella temporanea a cartella finale di destinazione.
    /// </summary>
    /// <param name="cancellationToken">Token facoltativo per verificare la cancellazione prima di applicare le modifiche su disco.</param>
    /// <exception cref="ObjectDisposedException">Lanciata se la transazione è già stata dismessa.</exception>
    /// <exception cref="InvalidOperationException">Lanciata se il commit è già stato eseguito in precedenza.</exception>
    /// <exception cref="OperationCanceledException">Lanciata se l'operazione è stata annullata prima del commit.</exception>
    /// <exception cref="IOException">Lanciata in caso di errori di spostamento o se la destinazione è stata occupata nel frattempo.</exception>
    internal void Commit(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(this._isDisposed, this);


        if (this._isCommitted)
            throw new InvalidOperationException("The destination is already published.");

        cancellationToken.ThrowIfCancellationRequested();
        ProjectFolderValidator.ValidateDestination(this._destinationFolder);
        cancellationToken.ThrowIfCancellationRequested();

        bool destinationRemoved = false;

        try
        {
            if (Directory.Exists(this._destinationFolder))
            {
                Directory.Delete(this._destinationFolder, recursive: false);
                destinationRemoved = true;
            }

            Directory.Move(StagingFolder, this._destinationFolder);
            this._isCommitted = true;
        }
        catch
        {
            if (destinationRemoved)
                RestoreEmptyDestination();

            throw;
        }
    }

    /// <summary>
    /// Rilascia le risorse della transazione. Se il commit non è andato a buon fine,
    /// rimuove ricorsivamente la cartella temporanea di staging.
    /// </summary>
    public void Dispose()
    {
        if (this._isDisposed)
            return;

        _isDisposed = true;

        if (!this._isCommitted)
            DeleteStagingFolder();
    }
    /// <summary>
    /// Tenta il ripristino della cartella vuota originale qualora la fase di move fallisca a metà.
    /// </summary>
    private void RestoreEmptyDestination()
    {
        try
        {
            if (Directory.Exists(this._destinationFolder) || File.Exists(this._destinationFolder))
                return;

            Directory.CreateDirectory(this._destinationFolder);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
    /// <summary>
    /// Elimina ricorsivamente l'area di staging temporanea ignorando errori di I/O secondari
    /// per non mascherare l'eccezione principale che ha scatenato il rollback.
    /// </summary>
    private void DeleteStagingFolder()
    {
        try
        {
            if (Directory.Exists(StagingFolder))
                Directory.Delete(StagingFolder, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
