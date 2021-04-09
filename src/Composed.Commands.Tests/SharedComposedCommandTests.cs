namespace Composed.Commands.Tests
{
    using System;
    using System.Reactive;
    using System.Windows.Input;
    using Composed.Commands;
    using Moq;
    using Shouldly;
    using Xunit;
    using static Composed.Compose;

    public abstract class SharedComposedCommandTests
    {
        protected abstract ComposedCommandBase CreateCommand(Action execute, Func<bool> canExecute, params IObservable<Unit>[] dependencies);

        [Fact]
        public void CanExecute_OnDependencyChange_ChangesValueAndNotifies()
        {
            var canExecuteDependency = Ref(false);
            using var command = CreateCommand(() => { }, () => canExecuteDependency.Value, canExecuteDependency);

            Assert.Raises<EventArgs>(
                e => ((ICommand)command).CanExecuteChanged += new EventHandler(e),
                e => { },
                () => canExecuteDependency.Value = true
            );

            // Not asserting that the ref notified. That's already covered by Composed's base API tests.
            command.CanExecute.Value.ShouldBeTrue();
        }

        [Fact]
        public void CanExecute_DisposedCommand_ReturnsFalseAndDoesNotNotify()
        {
            var canExecuteDependency = Ref(false);
            var command = CreateCommand(() => { }, () => canExecuteDependency.Value, canExecuteDependency);
            command.Dispose();

            // After disposal, the event should no longer be raised.
            ((ICommand)command).CanExecuteChanged += (_, _) => throw new Exception();

            canExecuteDependency.Value = true;
            command.CanExecute.Value.ShouldBeFalse();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ICommandCanExecute_CanAndCannotExecute_ReturnsCanExecuteValue(bool canExecute)
        {
            var command = CreateCommand(() => { }, () => canExecute);
            ((ICommand)command).CanExecute(parameter: null).ShouldBe(canExecute);
        }

        [Fact]
        public void ICommandExecute_CanExecute_InvokesExecute()
        {
            var executeMock = new Mock<Action>();
            var command = CreateCommand(executeMock.Object, canExecute: () => true);
            ((ICommand)command).Execute(parameter: null);
            executeMock.Verify(fn => fn(), Times.Exactly(1));
        }

        [Fact]
        public void ICommandExecute_CannotExecute_DoesNotInvokeExecute()
        {
            var executeMock = new Mock<Action>();
            var command = CreateCommand(executeMock.Object, canExecute: () => false);

            try
            {
                ((ICommand)command).Execute(parameter: null);
            }
            catch
            {
            }

            executeMock.Verify(fn => fn(), Times.Never());
        }

        [Fact]
        public void ICommandExecute_DisposedCommand_DoesNotInvokeExecute()
        {
            var executeMock = new Mock<Action>();
            var command = CreateCommand(executeMock.Object, canExecute: () => true);
            command.Dispose();

            try
            {
                ((ICommand)command).Execute(parameter: null);
            }
            catch
            {
            }

            executeMock.Verify(fn => fn(), Times.Never());
        }
    }
}
