namespace Composed.Commands
{
    using System.Diagnostics.CodeAnalysis;

    // The following delegates are used in favor of Action<...> and Func<...> because they allow
    // the placement of [AllowNull] attributes which are required for correct nullability typing.

    /// <summary>
    ///     A function which returns a boolean value indicating whether a command can be executed in
    ///     its current state.
    /// </summary>
    /// <returns>
    ///     <see langword="true"/> if the command can be executed; <see langword="false"/> if not.
    /// </returns>
    public delegate bool CanExecuteFunc();

    /// <summary>
    ///     A function which returns a boolean value indicating whether a command can be executed in
    ///     its current state.
    ///     This function may receive an optional parameter which may be used to determine whether
    ///     the command can execute.
    /// </summary>
    /// <typeparam name="TParameter">
    ///     The type of the parameter which may be passed to the function.
    /// </typeparam>
    /// <param name="parameter">
    ///     An optional parameter which allows the function to determine whether a command can be executed.
    ///     This can be <see langword="null"/>.
    /// </param>
    /// <returns>
    ///     <see langword="true"/> if the command can be executed; <see langword="false"/> if not.
    /// </returns>
    public delegate bool CanExecuteFunc<TParameter>([AllowNull] TParameter parameter);

    /// <summary>
    ///     A function which is executed when a command is executed.
    /// </summary>
    public delegate void ExecuteAction();

    /// <summary>
    ///     A function which is invoked when a command is executed.
    ///     This function may receive an optional parameter which may be used during the execution.
    /// </summary>
    /// <typeparam name="TParameter">
    ///     The type of the parameter which may be passed to the function.
    /// </typeparam>
    /// <param name="parameter">
    ///     An optional parameter to be used by the function.
    ///     This can be <see langword="null"/>.
    /// </param>
    public delegate void ExecuteAction<TParameter>([AllowNull] TParameter parameter);
}
