namespace Composed.Commands.Internal
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows.Input;

    internal sealed class ComposedCommand<TParameter> : IComposedCommand<TParameter>
    {
        public event EventHandler? CanExecuteChanged;

        private readonly ExecuteAction<TParameter> _execute;
        private readonly CanExecuteFunc<TParameter> _canExecute;

        public ComposedCommand(ExecuteAction execute, CanExecuteFunc? canExecute = null)
        {
            _execute = _ => execute();
            _canExecute = canExecute is null ? (_ => true) : _ => canExecute();
        }

        public ComposedCommand(ExecuteAction<TParameter> execute, CanExecuteFunc<TParameter>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute ?? (_ => true);
        }

        bool ICommand.CanExecute(object? parameter) =>
            CanExecute((TParameter)parameter);

        public bool CanExecute() =>
            CanExecute(default);

        public bool CanExecute([AllowNull] TParameter parameter) =>
            _canExecute(parameter);

        void ICommand.Execute(object? parameter) =>
            Execute((TParameter)parameter!);

        public void Execute() =>
            Execute(default);

        public void Execute([AllowNull] TParameter parameter)
        {
            if (!CanExecute(parameter))
            {
                throw new InvalidOperationException(
                    $"The command execution for the parameter \"{parameter}\" of type " +
                    $"\"{typeof(TParameter).FullName}\" is blocked by the command's associated " +
                    $"{nameof(CanExecute)} function."
                );
            }
            _execute(parameter);
        }

        public void RaiseCanExecuteChanged() =>
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
