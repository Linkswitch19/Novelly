using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using VnEditor.AppService.Interaction;
using VnEditor.AppService.Projects.Actions;
using VnEditor.AppService.Serialization;
using VnEditor.UI.Projects;

namespace VnEditor.Tests.UI.Projects;

/// <summary>
/// Verifica la presentazione degli errori operativi dei comandi File.
/// Controlla messaggi comprensibili, annullamento senza notifiche di errore,
/// propagazione delle eccezioni inattese e collegamento con il decoratore.
/// </summary>
public sealed class ProjectFileErrorPresenterTests
{
    private const string ErrorTitle = "Unable to open project";
    private const string FallbackMessage =
        "The project could not be opened. Please try again.";

    private readonly IProjectDialogs _dialogs = Substitute.For<IProjectDialogs>();
    private readonly ProjectFileErrorPresenter _presenter;

    public ProjectFileErrorPresenterTests()
    {
        _dialogs.ShowErrorAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);

        _presenter = new ProjectFileErrorPresenter(_dialogs);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompletedOperation_PreservesResultWithoutShowingError(bool expectedResult)
    {
        int executions = 0;

        bool result = await _presenter.TryRunAsync(
            () =>
            {
                executions++;

                return Task.FromResult(expectedResult);
            },
            ErrorTitle,
            FallbackMessage);

        Assert.Equal(expectedResult, result);
        Assert.Equal(1, executions);

        await _dialogs.DidNotReceive()
            .ShowErrorAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task CanceledOperation_ReturnsFalseWithoutShowingError()
    {
        CancellationToken canceledToken = new(canceled: true);

        bool result = await _presenter.TryRunAsync(
            () => Task.FromCanceled<bool>(canceledToken),
            ErrorTitle,
            FallbackMessage);

        Assert.False(result);

        await _dialogs.DidNotReceive()
            .ShowErrorAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Theory]
    [InlineData(
        nameof(UnauthorizedAccessException),
        "You do not have permission to use this folder. Choose a folder you can access.")]
    [InlineData(
        nameof(DirectoryNotFoundException),
        "A required folder could not be found. Check the project location and try again.")]
    [InlineData(
        nameof(FileNotFoundException),
        "A required file could not be found. Check that the project files are available.")]
    [InlineData(nameof(IOException), FallbackMessage)]
    [InlineData(nameof(ArgumentException), FallbackMessage)]
    [InlineData(nameof(NotSupportedException), FallbackMessage)]
    public async Task OperationalError_ShowsExpectedMessage(string errorKind, string expectedMessage)
    {
        Exception exception = CreateOperationalException(errorKind);

        bool result = await _presenter.TryRunAsync(
            () => Task.FromException<bool>(exception),
            ErrorTitle,
            FallbackMessage);

        Assert.False(result);

        await _dialogs.Received(1).ShowErrorAsync(ErrorTitle, expectedMessage);
    }

    [Theory]
    [InlineData(
        ProjectSerializationException.FailureKind.InvalidContent,
        "The project data is invalid or incomplete.")]
    [InlineData(
        ProjectSerializationException.FailureKind.UnsupportedVersion,
        "This project's file format is not supported by this version of Novelly.")]
    [InlineData(
        ProjectSerializationException.FailureKind.UnknownType,
        "This project contains content that this version of Novelly does not support.")]
    public async Task SerializationError_ShowsSpecificMessage(
        ProjectSerializationException.FailureKind failureKind, string expectedMessage)
    {
        ProjectSerializationException exception =
            new(failureKind, "Technical serialization details.");

        bool result = await _presenter.TryRunAsync(
            () => Task.FromException<bool>(exception),
            ErrorTitle,
            FallbackMessage);

        Assert.False(result);

        await _dialogs.Received(1).ShowErrorAsync(ErrorTitle, expectedMessage);
    }

    [Fact]
    public async Task SynchronousOperationalError_IsHandled()
    {
        bool result = await _presenter.TryRunAsync(
            () => throw new IOException("Technical file system details."),
            ErrorTitle,
            FallbackMessage);

        Assert.False(result);

        await _dialogs.Received(1).ShowErrorAsync(ErrorTitle, FallbackMessage);
    }

    [Fact]
    public async Task UnexpectedError_IsPropagatedWithoutShowingOperationalMessage()
    {
        InvalidOperationException expected = new("Unexpected failure.");

        InvalidOperationException actual =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _presenter.TryRunAsync(
                    () => Task.FromException<bool>(expected),
                    ErrorTitle,
                    FallbackMessage));

        Assert.Same(expected, actual);

        await _dialogs.DidNotReceive()
            .ShowErrorAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task DecoratedAction_HandlesOperationalErrorThroughPresenter()
    {
        IProjectFileAction inner = Substitute.For<IProjectFileAction>();

        inner.ExecuteAsync()
            .Returns(Task.FromException<bool>(new IOException("Read failed.")));

        ErrorHandlingProjectFileAction decorated = new(
            inner, _presenter, ErrorTitle, FallbackMessage);

        bool result = await decorated.ExecuteAsync();

        Assert.False(result);

        _ = inner.Received(1).ExecuteAsync();

        await _dialogs.Received(1).ShowErrorAsync(ErrorTitle, FallbackMessage);
    }

    private static Exception CreateOperationalException(string errorKind)
    {
        const string technicalMessage = "Technical details not intended for the user.";

        return errorKind switch
        {
            nameof(UnauthorizedAccessException) =>
                new UnauthorizedAccessException(technicalMessage),

            nameof(DirectoryNotFoundException) =>
                new DirectoryNotFoundException(technicalMessage),

            nameof(FileNotFoundException) =>
                new FileNotFoundException(technicalMessage),

            nameof(IOException) =>
                new IOException(technicalMessage),

            nameof(ArgumentException) =>
                new ArgumentException(technicalMessage),

            nameof(NotSupportedException) =>
                new NotSupportedException(technicalMessage),

            _ => throw new ArgumentOutOfRangeException(nameof(errorKind))
        };
    }
}