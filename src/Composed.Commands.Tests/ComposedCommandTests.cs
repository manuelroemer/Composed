namespace Composed.Commands.Tests
{
    using System;
    using System.Windows.Input;
    using Composed.Commands;
    using Moq;
    using Shouldly;
    using Xunit;
    using static Composed.Commands.Compose;
    using static Composed.Compose;

    public class ComposedCommandTests
    {
        [Fact]
        public void CanExecute_OnChange_RaisesCanExecuteChanged()
        {
            var canExecuteDependency = Ref(false);
            ICommand command = UseCommand(() => { }, () => canExecuteDependency.Value, canExecuteDependency);
            Assert.Raises<EventArgs>(
                e => command.CanExecuteChanged += new EventHandler(e),
                e => { },
                () => canExecuteDependency.Value = true
            );
        }

        [Fact]
        public void Execute_CanExecute_InvokesExecute()
        {
            var executeMock = new Mock<ExecuteAction>();
            var command = UseCommand(executeMock.Object, canExecute: () => true);
            command.Execute();
            ((ICommand)command).Execute(parameter: null);
            executeMock.Verify(fn => fn(), Times.Exactly(2));
        }

        [Fact]
        public void Execute_CannotExecute_DoesNotInvokeExecuteAndThrows()
        {
            var executeMock = new Mock<ExecuteAction>();
            var command = UseCommand(executeMock.Object, canExecute: () => false);
            Should.Throw<InvalidOperationException>(() => command.Execute());
            Should.Throw<InvalidOperationException>(() => ((ICommand)command).Execute(parameter: null));
            executeMock.Verify(fn => fn(), Times.Never());
        }

        [Fact]
        public void TryExecute_CanExecute_InvokesExecuteAndReturnsTrue()
        {
            var executeMock = new Mock<ExecuteAction>();
            var command = UseCommand(executeMock.Object, () => true);
            var result = command.TryExecute();
            result.ShouldBeTrue();
            executeMock.Verify(fn => fn(), Times.Once());
        }

        [Fact]
        public void TryExecute_CannotExecute_DoesNotInvokeExecuteAndReturnsFalse()
        {
            var executeMock = new Mock<ExecuteAction>();
            var command = UseCommand(executeMock.Object, () => false);
            var result = command.TryExecute();
            result.ShouldBeFalse();
            executeMock.Verify(fn => fn(), Times.Never());
        }
    }
}
