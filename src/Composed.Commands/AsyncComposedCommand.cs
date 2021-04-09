namespace Composed.Commands
{
    using System;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Input;

    /// <summary>
    ///     An asynchronous command implementation utilizing Composed's API.
    ///     You can create <see cref="AsyncComposedCommand"/> instances via the
    ///     <see cref="Compose.UseCommand(Func{CancellationToken, Task})"/> overloads.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class implements the <see cref="ICommand"/> interface.
    ///         Be aware that <see cref="ICommand.Execute(object)"/> invocations will call the
    ///         <see cref="ExecuteAsync()"/> method and can therefore throw.<br/>
    ///         Due to <see cref="ICommand.Execute(object)"/> being synchronous, it calls the asynchronous
    ///         <see cref="ExecuteAsync()"/> method using <c>async void</c>.
    ///         Any exception, including the ones being thrown due to invalid command executions,
    ///         will be dispatched to your application based on the rules of <c>async void</c> methods.
    ///     </para>
    ///     <para>
    ///         This class implements <see cref="IDisposable"/> and can thus be disposed.
    ///         Once disposed, the command no longer watches the dependencies of
    ///         <see cref="ComposedCommandBase.CanExecute"/> and cannot be executed again.
    ///     </para>
    /// </remarks>
    public sealed class AsyncComposedCommand : ComposedCommandBase
    {
        private readonly Func<CancellationToken, Task> _executeAsync;

        internal AsyncComposedCommand(
            Func<CancellationToken, Task> executeAsync,
            Func<bool> canExecute,
            IScheduler? scheduler,
            IObservable<Unit>[] dependencies
        ) : base(canExecute, scheduler, dependencies)
        {
            _executeAsync = executeAsync;
        }

        private protected async override void ICommandExecute() =>
            await TryExecuteAsync().ConfigureAwait(false);

        /// <inheritdoc cref="ComposedCommand.Execute"/>
        public Task ExecuteAsync() =>
            ExecuteAsync(CancellationToken.None);

        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken"/> which can be used to cancel the asynchronous operation.
        /// </param>
        /// <exception cref="OperationCanceledException">
        ///     The operation was canceled through the specified <paramref name="cancellationToken"/>.
        /// </exception>
        /// <inheritdoc cref="ExecuteAsync()"/>
        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (!await TryExecuteAsync(cancellationToken).ConfigureAwait(false))
            {
                throw CreateExceptionForInvalidExecuteCall(nameof(TryExecuteAsync));
            }
        }

        /// <inheritdoc cref="ComposedCommand.TryExecute"/>
        public Task<bool> TryExecuteAsync() =>
            TryExecuteAsync(CancellationToken.None);

        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken"/> which can be used to cancel the asynchronous operation.
        /// </param>
        /// <exception cref="OperationCanceledException">
        ///     The operation was canceled through the specified <paramref name="cancellationToken"/>.
        /// </exception>
        /// <inheritdoc cref="TryExecuteAsync()"/>
        public async Task<bool> TryExecuteAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested ||!CanExecute.Value)
            {
                return false;
            }

            await _executeAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
    }
}
