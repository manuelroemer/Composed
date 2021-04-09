namespace Composed.Commands.Tests
{
    using System;
    using System.Reactive;
    using System.Windows.Input;
    using Composed.Commands;
    using Moq;
    using Shouldly;
    using Xunit;
    using static Composed.Commands.Compose;

    public class ComposedCommandTests : SharedComposedCommandTests
    {
        protected override ComposedCommandBase CreateCommand(Action execute, Func<bool> canExecute, params IObservable<Unit>[] dependencies) =>
            UseCommand(execute, canExecute, dependencies);

        [Fact]
        public void ICommandExecute_CannotExecute_ThrowsInvalidOperationException()
        {
            var command = UseCommand(() => { }, canExecute: () => false);
            Should.Throw<InvalidOperationException>(() => ((ICommand)command).Execute(parameter: null));
        }

        [Fact]
        public void ICommandExecute_DisposedCommand_ThrowsObjectDisposedException()
        {
            var command = UseCommand(() => { }, canExecute: () => true);
            command.Dispose();
            Should.Throw<ObjectDisposedException>(() => ((ICommand)command).Execute(parameter: null));
        }

        [Fact]
        public void Execute_CanExecute_InvokesExecute()
        {
            var executeMock = new Mock<Action>();
            var command = UseCommand(executeMock.Object, canExecute: () => true);
            command.Execute();
            executeMock.Verify(fn => fn(), Times.Exactly(1));
        }

        [Fact]
        public void Execute_CannotExecute_DoesNotInvokeExecuteAndThrowsInvalidOperationException()
        {
            var executeMock = new Mock<Action>();
            var command = UseCommand(executeMock.Object, canExecute: () => false);
            Should.Throw<InvalidOperationException>(() => command.Execute());
            executeMock.Verify(fn => fn(), Times.Never());
        }

        [Fact]
        public void Execute_DisposedCommand_DoesNotInvokeExecuteAndThrowsObjectDisposedException()
        {
            var executeMock = new Mock<Action>();
            var command = UseCommand(executeMock.Object, canExecute: () => true);
            command.Dispose();
            Should.Throw<ObjectDisposedException>(() => command.Execute());
            executeMock.Verify(fn => fn(), Times.Never());
        }

        [Fact]
        public void TryExecute_CanExecute_InvokesExecuteAndReturnsTrue()
        {
            var executeMock = new Mock<Action>();
            var command = UseCommand(executeMock.Object, () => true);
            var result = command.TryExecute();
            result.ShouldBeTrue();
            executeMock.Verify(fn => fn(), Times.Once());
        }

        [Fact]
        public void TryExecute_CannotExecute_DoesNotInvokeExecuteAndReturnsFalse()
        {
            var executeMock = new Mock<Action>();
            var command = UseCommand(executeMock.Object, () => false);
            var result = command.TryExecute();
            result.ShouldBeFalse();
            executeMock.Verify(fn => fn(), Times.Never());
        }

        [Fact]
        public void TryExecute_DisposedCommand_DoesNotInvokeExecuteAndReturnsFalse()
        {
            var executeMock = new Mock<Action>();
            var command = UseCommand(executeMock.Object, () => true);
            command.Dispose();
            var result = command.TryExecute();
            result.ShouldBeFalse();
            executeMock.Verify(fn => fn(), Times.Never());
        }
    }
}
