namespace Composed.Commands.Tests
{
    using System;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Threading;
    using System.Threading.Tasks;
    using Shouldly;
    using Xunit;
    using static Composed.Commands.Compose;

    public class ComposeTests
    {
        [Fact]
        public void UseCommand_NullArguments_ThrowsArgumentNullException()
        {
            Should.Throw<ArgumentNullException>(() => UseCommand(execute: null!, canExecute: () => true, scheduler: DefaultScheduler.Instance, dependencies: Array.Empty<IObservable<Unit>>()));
            Should.Throw<ArgumentNullException>(() => UseCommand(execute: () => { }, null!, scheduler: DefaultScheduler.Instance, dependencies: Array.Empty<IObservable<Unit>>()));
            Should.Throw<ArgumentNullException>(() => UseCommand(execute: () => { }, canExecute: () => true, scheduler: DefaultScheduler.Instance, dependencies: null!));
            Should.Throw<ArgumentNullException>(() => UseCommand(execute: () => { }, canExecute: () => true, scheduler: DefaultScheduler.Instance, dependencies: new IObservable<Unit>[] { null! }));

            Should.Throw<ArgumentNullException>(() => UseCommand(executeAsync: (Func<CancellationToken, Task>)null!, canExecute: () => true, scheduler: DefaultScheduler.Instance, dependencies: Array.Empty<IObservable<Unit>>()));
            Should.Throw<ArgumentNullException>(() => UseCommand(executeAsync: _ => Task.CompletedTask, null!, scheduler: DefaultScheduler.Instance, dependencies: Array.Empty<IObservable<Unit>>()));
            Should.Throw<ArgumentNullException>(() => UseCommand(executeAsync: _ => Task.CompletedTask, canExecute: () => true, scheduler: DefaultScheduler.Instance, dependencies: null!));
            Should.Throw<ArgumentNullException>(() => UseCommand(executeAsync: _ => Task.CompletedTask, canExecute: () => true, scheduler: DefaultScheduler.Instance, dependencies: new IObservable<Unit>[] { null! }));

            Should.Throw<ArgumentNullException>(() => UseBlockingCommand(execute: null!, canExecute: () => true, scheduler: DefaultScheduler.Instance, dependencies: Array.Empty<IObservable<Unit>>()));
            Should.Throw<ArgumentNullException>(() => UseBlockingCommand(execute: () => { }, null!, scheduler: DefaultScheduler.Instance, dependencies: Array.Empty<IObservable<Unit>>()));
            Should.Throw<ArgumentNullException>(() => UseBlockingCommand(execute: () => { }, canExecute: () => true, scheduler: DefaultScheduler.Instance, dependencies: null!));
            Should.Throw<ArgumentNullException>(() => UseBlockingCommand(execute: () => { }, canExecute: () => true, scheduler: DefaultScheduler.Instance, dependencies: new IObservable<Unit>[] { null! }));

            Should.Throw<ArgumentNullException>(() => UseBlockingCommand(executeAsync: (Func<CancellationToken, Task>)null!, canExecute: () => true, scheduler: DefaultScheduler.Instance, dependencies: Array.Empty<IObservable<Unit>>()));
            Should.Throw<ArgumentNullException>(() => UseBlockingCommand(executeAsync: _ => Task.CompletedTask, null!, scheduler: DefaultScheduler.Instance, dependencies: Array.Empty<IObservable<Unit>>()));
            Should.Throw<ArgumentNullException>(() => UseBlockingCommand(executeAsync: _ => Task.CompletedTask, canExecute: () => true, scheduler: DefaultScheduler.Instance, dependencies: null!));
            Should.Throw<ArgumentNullException>(() => UseBlockingCommand(executeAsync: _ => Task.CompletedTask, canExecute: () => true, scheduler: DefaultScheduler.Instance, dependencies: new IObservable<Unit>[] { null! }));
        }

        [Fact]
        public void UseBlockingCommand_Sync_CannotExecuteWhileAlreadyExecuting()
        {
            bool? canExecuteBeforeRunning = null;
            bool? canExecuteWhileRunning = null;
            bool? canExecuteAfterRunning = null;

            ComposedCommand command = null!;
            command = UseBlockingCommand(() => canExecuteWhileRunning = command.CanExecute.Value);

            canExecuteBeforeRunning = command.CanExecute.Value;
            command.Execute();
            canExecuteAfterRunning = command.CanExecute.Value;

            canExecuteBeforeRunning.ShouldBe(true);
            canExecuteWhileRunning.ShouldBe(false);
            canExecuteAfterRunning.ShouldBe(true);
        }

        [Fact]
        public async Task UseBlockingCommand_Async_CannotExecuteWhileAlreadyExecuting()
        {
            bool? canExecuteBeforeRunning = null;
            bool? canExecuteWhileRunning = null;
            bool? canExecuteAfterRunning = null;

            AsyncComposedCommand command = null!;
            command = UseBlockingCommand(async () => canExecuteWhileRunning = command.CanExecute.Value);

            canExecuteBeforeRunning = command.CanExecute.Value;
            await command.ExecuteAsync();
            canExecuteAfterRunning = command.CanExecute.Value;

            canExecuteBeforeRunning.ShouldBe(true);
            canExecuteWhileRunning.ShouldBe(false);
            canExecuteAfterRunning.ShouldBe(true);
        }
    }
}
