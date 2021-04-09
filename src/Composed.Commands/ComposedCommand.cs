namespace Composed.Commands
{
    using System;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Windows.Input;

    /// <summary>
    ///     A command implementation utilizing Composed's API.
    ///     You can create <see cref="ComposedCommand"/> instances via the
    ///     <see cref="Compose.UseCommand(Action)"/> overloads.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class implements the <see cref="ICommand"/> interface.
    ///         Be aware that <see cref="ICommand.Execute(object)"/> invocations will call the
    ///         <see cref="Execute"/> method and can therefore throw.
    ///     </para>
    ///     <para>
    ///         This class implements <see cref="IDisposable"/> and can thus be disposed.
    ///         Once disposed, the command no longer watches the dependencies of
    ///         <see cref="ComposedCommandBase.CanExecute"/> and cannot be executed again.
    ///     </para>
    /// </remarks>
    public sealed class ComposedCommand : ComposedCommandBase
    {
        private readonly Action _execute;

        internal ComposedCommand(
            Action execute,
            Func<bool> canExecute,
            IScheduler? scheduler,
            IObservable<Unit>[] dependencies
        ) : base(canExecute, scheduler, dependencies)
        {
            _execute = execute;
        }

        private protected override void ICommandExecute() =>
            Execute();

        /// <summary>
        ///     Executes the command if it can be executed; throws an exception otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///     The command cannot be executed because its last <see cref="ComposedCommandBase.CanExecute"/> value
        ///     is <see langword="false"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     The command cannot be executed because it has been disposed.
        /// </exception>
        public void Execute()
        {
            if (!TryExecute())
            {
                throw CreateExceptionForInvalidExecuteCall(nameof(TryExecute));
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
