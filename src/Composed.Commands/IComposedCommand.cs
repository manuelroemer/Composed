namespace Composed.Commands
{
    using System.Diagnostics.CodeAnalysis;
    using System.Windows.Input;

    /// <summary>
    ///     Represents a command which may receive a parameter of type <see cref="object"/>.
    /// </summary>
    /// <remarks>
    ///     <see cref="IComposedCommand"/> implements and extends .NET's <see cref="ICommand"/> interface.
    /// </remarks>
    public interface IComposedCommand : ICommand
    {
        /// <summary>
        ///     Returns a value indicating whether the command can be executed in its current state.
        ///     This method does not pass any parameter to the command.
        /// </summary>
        /// <returns>
        ///     <see langword="true"/> if the command can be executed; otherwise <see langword="false"/>.
        /// </returns>
        bool CanExecute();

        /// <summary>
        ///     Executes the command.
        ///     This method does not pass any parameter to the command.
        /// </summary>
        void Execute();
    }

    /// <summary>
    ///     Represents a command which may receive a parameter of type <typeparamref name="TParameter"/>.
    /// </summary>
    /// <typeparam name="TParameter">
    ///     The type of an optional parameter which may be passed to the command.
    /// </typeparam>
    /// <remarks>
    ///     <see cref="IComposedCommand{TParameter}"/> implements and extends .NET's <see cref="ICommand"/> interface.
    /// </remarks>
    public interface IComposedCommand<in TParameter> : IComposedCommand
    {
        /// <summary>
        ///     Returns a value indicating whether the command can be executed in its current state.
        ///     This method does not pass any parameter to the command.
        /// </summary>
        /// <param name="parameter">
        ///     An optional parameter which may be used by the command to determine whether it can execute.
        ///     This can be <see langword="null"/> (if <typeparamref name="TParameter"/> is a reference type).
        /// </param>
        /// <returns>
        ///     <see langword="true"/> if the command can be executed; otherwise <see langword="false"/>.
        /// </returns>
        bool CanExecute([AllowNull] TParameter parameter);

        /// <summary>
        ///     Executes the command.
        /// </summary>
        /// <param name="parameter">
        ///     An optional parameter which may be used by the command during the execution.
        ///     This can be <see langword="null"/> (if <typeparamref name="TParameter"/> is a reference type).
        /// </param>
        void Execute([AllowNull] TParameter parameter);
    }
}
