namespace Composed.Commands
{
    using System;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Windows.Input;
    using Composed;
    using static Composed.Compose;

    /// <summary>
    ///     A command implementation utilizing Composed's API.
    ///     You can create <see cref="ComposedCommand"/> instances via the
    ///     <see cref="Compose.UseCommand(ExecuteAction)"/> overloads.
    /// </summary>
    /// <remarks>
    ///     This class implements the <see cref="ICommand"/> interface.
    ///     Be aware that <see cref="ICommand.Execute(object)"/> invocations will throw an exception
    ///     when the command cannot be executed at that point in time.
    /// </remarks>
    public sealed class ComposedCommand : ICommand
    {
        private readonly ExecuteAction _execute;
        private readonly object _canExecuteChangedLock = new();
        private EventHandler? _canExecuteChanged;

        /// <inheritdoc/>
        event EventHandler ICommand.CanExecuteChanged
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
        ///     Gets a ref holding a value indicating whether the command can execute in its current state.
        /// </summary>
        public IReadOnlyRef<bool> CanExecute { get; }

        internal ComposedCommand(
            ExecuteAction execute,
            CanExecuteFunc canExecute,
            IScheduler? scheduler,
            IObservable<Unit>[] dependencies
        )
        {
            _execute = execute;
            CanExecute = Computed(() => canExecute(), scheduler, dependencies);
            Watch(() => _canExecuteChanged?.Invoke(this, EventArgs.Empty), CanExecute);
        }

        /// <inheritdoc/>
        bool ICommand.CanExecute(object parameter) =>
            CanExecute.Value;

        /// <inheritdoc/>
        void ICommand.Execute(object parameter) =>
            Execute();

        /// <summary>
        ///     Executes the command if it can be executed; otherwise throws an exception.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///     The command cannot be executed because its last <see cref="CanExecute"/> value
        ///     is <see langword="false"/>.
        /// </exception>
        public void Execute()
        {
            if (!TryExecute())
            {
                throw new InvalidOperationException(
                    $"Invalid command execution. The command was executed even though it cannot " +
                    $"be executed in its current state. " +
                    $"You can prevent this exception by preemptively checking whether the command can " +
                    $"be executed or by using the \"{nameof(TryExecute)}\" method instead."
                );
            }
        }

        /// <summary>
        ///     Attempts to execute the command and returns the result.
        /// </summary>
        /// <returns>
        ///     <see langword="true"/> if the command was executed;
        ///     <see langword="false"/> if not.
        /// </returns>
        public bool TryExecute()
        {
            if (!CanExecute.Value)
            {
                return false;
            }

            _execute();
            return true;
        }
    }
}
