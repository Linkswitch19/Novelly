using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using NSubstitute;
using VnEditor.AppService.Projects.Actions;
using VnEditor.AppService.Sessions;
using VnEditor.Domain;
using VnEditor.Infrastructure.History;
using VnEditor.UI.ViewModels;

namespace VnEditor.Tests.UI;

/// <summary>
/// Verifica l'abilitazione dei comandi File e la delega alle rispettive azioni.
/// Controlla il blocco delle operazioni contemporanee, l'esito della richiesta
/// di chiusura e il ripristino dello stato dopo un errore.
/// </summary>
public sealed class FileMenuViewModelTests : IDisposable
{
    private readonly ProjectSession _session = new();

    private readonly IProjectFileAction _newAction = CreateAction();
    private readonly IProjectFileAction _openAction = CreateAction();
    private readonly IProjectFileAction _saveAction = CreateAction();
    private readonly IProjectFileAction _saveAsAction = CreateAction();
    private readonly IProjectFileAction _closeAction = CreateAction();

    private readonly FileMenuViewModel _viewModel;

    public FileMenuViewModelTests()
    {
        _viewModel = new FileMenuViewModel(
            _session, _newAction, _openAction, _saveAction, _saveAsAction, _closeAction);
    }

    [Fact]
    public void EmptySession_EnablesOnlyNewAndOpen()
    {
        Assert.False(_viewModel.IsBusy);
        Assert.True(_viewModel.NewCommand.CanExecute(null));
        Assert.True(_viewModel.OpenCommand.CanExecute(null));
        Assert.False(_viewModel.SaveCommand.CanExecute(null));
        Assert.False(_viewModel.SaveAsCommand.CanExecute(null));
    }

    [Fact]
    public async Task EmptySession_SaveCommandsDoNotInvokeActions()
    {
        await _viewModel.SaveCommand.ExecuteAsync(null);
        await _viewModel.SaveAsCommand.ExecuteAsync(null);

        await _saveAction.DidNotReceive().ExecuteAsync();
        await _saveAsAction.DidNotReceive().ExecuteAsync();

        Assert.False(_viewModel.IsBusy);
    }

    [Fact]
    public void SessionActivation_EnablesSaveCommandsAndNotifiesBindings()
    {
        int saveNotifications = 0;
        int saveAsNotifications = 0;

        _viewModel.SaveCommand.CanExecuteChanged += (_, _) => saveNotifications++;
        _viewModel.SaveAsCommand.CanExecuteChanged += (_, _) => saveAsNotifications++;

        ActivateProject();

        Assert.True(_viewModel.SaveCommand.CanExecute(null));
        Assert.True(_viewModel.SaveAsCommand.CanExecute(null));
        Assert.True(saveNotifications > 0);
        Assert.True(saveAsNotifications > 0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Command_DelegatesOnlyToMatchingAction(int commandIndex)
    {
        ActivateProject();

        IAsyncRelayCommand[] commands =
        [
            _viewModel.NewCommand,
            _viewModel.OpenCommand,
            _viewModel.SaveCommand,
            _viewModel.SaveAsCommand
        ];

        IProjectFileAction[] actions =
        [
            _newAction,
            _openAction,
            _saveAction,
            _saveAsAction,
            _closeAction
        ];

        await commands[commandIndex].ExecuteAsync(null);

        for (int index = 0; index < actions.Length; index++)
        {
            int expectedCalls = index == commandIndex ? 1 : 0;

            await actions[index].Received(expectedCalls).ExecuteAsync();
        }

        Assert.False(_viewModel.IsBusy);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RequestClose_ReturnsActionResult(bool expectedResult)
    {
        _closeAction.ExecuteAsync().Returns(Task.FromResult(expectedResult));

        bool actualResult = await _viewModel.RequestCloseAsync();

        Assert.Equal(expectedResult, actualResult);
        Assert.False(_viewModel.IsBusy);

        await _closeAction.Received(1).ExecuteAsync();
    }

    [Fact]
    public async Task PendingOperation_BlocksOtherCommandsAndClosing()
    {
        ActivateProject();

        TaskCompletionSource<bool> completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        _newAction.ExecuteAsync().Returns(completion.Task);

        Task execution = _viewModel.NewCommand.ExecuteAsync(null);

        try
        {
            Assert.True(_viewModel.IsBusy);
            AssertCommandsDisabled();

            await _viewModel.OpenCommand.ExecuteAsync(null);
            await _viewModel.SaveCommand.ExecuteAsync(null);
            await _viewModel.SaveAsCommand.ExecuteAsync(null);

            bool canClose = await _viewModel.RequestCloseAsync();

            Assert.False(canClose);

            await _openAction.DidNotReceive().ExecuteAsync();
            await _saveAction.DidNotReceive().ExecuteAsync();
            await _saveAsAction.DidNotReceive().ExecuteAsync();
            await _closeAction.DidNotReceive().ExecuteAsync();
        }
        finally
        {
            completion.TrySetResult(true);
            await execution;
        }

        Assert.False(_viewModel.IsBusy);
        Assert.True(_viewModel.NewCommand.CanExecute(null));
        Assert.True(_viewModel.OpenCommand.CanExecute(null));
        Assert.True(_viewModel.SaveCommand.CanExecute(null));
        Assert.True(_viewModel.SaveAsCommand.CanExecute(null));
    }

    [Fact]
    public async Task ActionFailure_RestoresCommandsAndAllowsAnotherOperation()
    {
        InvalidOperationException expected = new("Unexpected action failure.");

        _newAction.ExecuteAsync().Returns(Task.FromException<bool>(expected));

        InvalidOperationException actual =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _viewModel.NewCommand.ExecuteAsync(null));

        Assert.Same(expected, actual);
        Assert.False(_viewModel.IsBusy);
        Assert.True(_viewModel.NewCommand.CanExecute(null));
        Assert.True(_viewModel.OpenCommand.CanExecute(null));

        await _viewModel.OpenCommand.ExecuteAsync(null);

        await _openAction.Received(1).ExecuteAsync();
    }

    [Fact]
    public async Task DisposedViewModel_DisablesCommandsAndStopsObservingSession()
    {
        _viewModel.Dispose();

        int saveNotifications = 0;
        int saveAsNotifications = 0;

        _viewModel.SaveCommand.CanExecuteChanged += (_, _) => saveNotifications++;
        _viewModel.SaveAsCommand.CanExecuteChanged += (_, _) => saveAsNotifications++;

        ActivateProject();

        AssertCommandsDisabled();
        Assert.Equal(0, saveNotifications);
        Assert.Equal(0, saveAsNotifications);

        await _viewModel.NewCommand.ExecuteAsync(null);
        await _viewModel.OpenCommand.ExecuteAsync(null);
        await _viewModel.SaveCommand.ExecuteAsync(null);
        await _viewModel.SaveAsCommand.ExecuteAsync(null);

        bool canClose = await _viewModel.RequestCloseAsync();

        Assert.False(canClose);

        await _newAction.DidNotReceive().ExecuteAsync();
        await _openAction.DidNotReceive().ExecuteAsync();
        await _saveAction.DidNotReceive().ExecuteAsync();
        await _saveAsAction.DidNotReceive().ExecuteAsync();
        await _closeAction.DidNotReceive().ExecuteAsync();
    }

    public void Dispose()
    {
        _viewModel.Dispose();
        _session.Dispose();
    }

    private void ActivateProject()
    {
        Project project = new() { Title = "My story" };
        PassThroughProjectHistory history = new(project);
        string projectFolder = Path.Combine(Path.GetTempPath(), "NovellyFileMenuTests");

        _session.SetProject(history, projectFolder, isSaved: true);
    }

    private void AssertCommandsDisabled()
    {
        Assert.False(_viewModel.NewCommand.CanExecute(null));
        Assert.False(_viewModel.OpenCommand.CanExecute(null));
        Assert.False(_viewModel.SaveCommand.CanExecute(null));
        Assert.False(_viewModel.SaveAsCommand.CanExecute(null));
    }

    private static IProjectFileAction CreateAction()
    {
        IProjectFileAction action = Substitute.For<IProjectFileAction>();
        action.ExecuteAsync().Returns(Task.FromResult(true));

        return action;
    }
}