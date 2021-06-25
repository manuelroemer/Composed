namespace Composed.Commands
{
    using System;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Windows.Input;
    using Composed;
    using static Composed.Compose;

    // Interface methods should be callable by child types.
    // Not necessary since this class can only be used internally.
#pragma warning disable CA1033

    /// <summary>
    ///     The base class for all command implementations utilizing Composed's API.
    ///     You cannot inherit from this class.
    /// </summary>
    public abstract class ComposedCommandBase : ICommand, IDisposable
    {
        private readonly object _canExecuteChangedLock = new();
        private readonly IRef<bool> _canExecute;
        private readonly IDisposable _canExecuteSubscription;
        private EventHandler? _canExecuteChanged;

        /// <inheritdoc/>
        event EventHandler? ICommand.CanExecuteChanged
        {
            add
            {
                lock (_canExecuteChangedLock)
                {
                    _canExecuteChanged += value;
                }
            }
            remove
            {
                lock (_canExecuteChangedLock)
                {
                    _canExecuteChanged -= value;
                }
            }
        }

        /// <summary>
        ///     <para>
        ///         Gets a ref holding a value indicating whether the command can execute in its current state.
        ///     </para>
        ///     <para>
        ///         Once the command has been disposed, this property will always return <see langword="false"/>.
        ///     </para>
        /// </summary>
        public IReadOnlyRef<bool> CanExecute => _canExecute;

        private protected bool IsDisposed { get; private set; }

        private protected ComposedCommandBase(Func<bool> canExecute, IScheduler? scheduler, IObservable<Unit>[] dependencies)
        {
            _canExecute = Ref(canExecute());

            // Using Watch here instead of Computed because this way we can stop changing the ref's
            // value once the command has been disposed.
            _canExecuteSubscription = Watch(() =>
            {
                _canExecute.Value = canExecute();
                _canExecuteChanged?.Invoke(this, EventArgs.Empty);
            }, scheduler, dependencies);
        }

        /// <inheritdoc/>
        bool ICommand.CanExecute(object? parameter) =>
            CanExecute.Value;

        /// <inheritdoc/>
        void ICommand.Execute(object? parameter) =>
            ICommandExecute();

        private protected abstract void ICommandExecute();

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <param name="disposing">Whether to dispose managed objects.</param>
        /// <inheritdoc cref="Dispose()"/>
        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    _canExecuteSubscription.Dispose();
                    _canExecute.SetValue(false, suppressNotification: true);
                    // Reason for suppressing the notification above:
                    // Having the change notification in place caused issues in apps which disposed
                    // commands during shutdown. The notification caused a binding to update
                    // despite the Dispatcher no longer being available. This caused an exception.
                    //
                    // This may not be the best reason, but it is justified.
                    // Disposing a command essentially leaves it in limbo anyway, i.e. you should
                    // not use it afterwards (and accessing CanExecute *is* a form of using it).
                    // Not providing any functionality post disposal is acceptable.
                    //
                    // With that being said, if anybody who reads this requires this to change,
                    // feel free to start a discussion. :)
                }

                IsDisposed = true;
            }
        }

        private protected InvalidOperationException CreateExceptionForInvalidExecuteCall(string tryExecuteMethodName)
        {
            if (IsDisposed)
            {
                return new ObjectDisposedException(
                    objectName: GetType().Name,
                    $"Invalid command execution.  The command was executed even though it has been" +
                    $"disposed. " +
                    $"You can prevent this exception by preemptively checking whether the command can " +
                    $"be executed or by using the \"{tryExecuteMethodName}\" method instead."
                );
            }

            return new InvalidOperationException(
                $"Invalid command execution. The command was executed even though it cannot " +
                $"be executed in its current state. " +
                $"You can prevent this exception by preemptively checking whether the command can " +
                $"be executed or by using the \"{tryExecuteMethodName}\" method instead."
            );
        }
    }
#pragma warning restore CA1033
}
