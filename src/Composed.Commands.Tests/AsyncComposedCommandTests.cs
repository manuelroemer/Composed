namespace Composed.Commands.Tests
{
    using System;
    using System.Reactive;
    using System.Threading.Tasks;
    using Composed.Commands;
    using Moq;
    using Shouldly;
    using Xunit;
    using static Composed.Commands.Compose;

    public class AsyncComposedCommandTests : SharedComposedCommandTests
    {
        protected override ComposedCommandBase CreateCommand(Action execute, Func<bool> canExecute, params IObservable<Unit>[] dependencies) =>
            UseCommand(() => { execute(); return Task.CompletedTask; }, canExecute, dependencies);

        [Fact]
        public async Task ExecuteAsync_CanExecute_InvokesExecute()
        {
            var executeAsyncMock = MockExecuteAsync();
            var command = UseCommand(executeAsyncMock.Object, canExecute: () => true);
            await command.ExecuteAsync();
            executeAsyncMock.Verify(fn => fn(), Times.Exactly(1));
        }

        [Fact]
        public async Task ExecuteAsync_CannotExecute_DoesNotInvokeExecuteAndThrowsInvalidOperationException()
        {
            var executeAsyncMock = MockExecuteAsync();
            var command = UseCommand(executeAsyncMock.Object, canExecute: () => false);
            await Should.ThrowAsync<InvalidOperationException>(async () => await command.ExecuteAsync());
            executeAsyncMock.Verify(fn => fn(), Times.Never());
        }

        [Fact]
        public async Task ExecuteAsync_DisposedCommand_DoesNotInvokeExecuteAndThrowsObjectDisposedException()
        {
            var executeAsyncMock = MockExecuteAsync();
            var command = UseCommand(executeAsyncMock.Object, canExecute: () => true);
            command.Dispose();
            await Should.ThrowAsync<ObjectDisposedException>(async () => await command.ExecuteAsync());
            executeAsyncMock.Verify(fn => fn(), Times.Never());
        }

        [Fact]
        public async Task TryExecuteAsync_CanExecute_InvokesExecuteAndReturnsTrue()
        {
            var executeAsyncMock = MockExecuteAsync();
            var command = UseCommand(executeAsyncMock.Object, () => true);
            var result = await command.TryExecuteAsync();
            result.ShouldBeTrue();
            executeAsyncMock.Verify(fn => fn(), Times.Once());
        }

        [Fact]
        public async Task TryExecuteAsync_CannotExecute_DoesNotInvokeExecuteAndReturnsFalse()
        {
            var executeAsyncMock = MockExecuteAsync();
            var command = UseCommand(executeAsyncMock.Object, () => false);
            var result = await command.TryExecuteAsync();
            result.ShouldBeFalse();
            executeAsyncMock.Verify(fn => fn(), Times.Never());
        }

        [Fact]
        public async Task TryExecuteAsync_DisposedCommand_DoesNotInvokeExecuteAndReturnsFalse()
        {
            var executeAsyncMock = MockExecuteAsync();
            var command = UseCommand(executeAsyncMock.Object, () => true);
            command.Dispose();
            var result = await command.TryExecuteAsync();
            result.ShouldBeFalse();
            executeAsyncMock.Verify(fn => fn(), Times.Never());
        }

        private static Mock<Func<Task>> MockExecuteAsync()
        {
            var mock = new Mock<Func<Task>>();
            mock.Setup(fn => fn()).Returns(Task.CompletedTask);
            return mock;
        }
    }
}
