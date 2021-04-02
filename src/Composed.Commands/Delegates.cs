namespace Composed.Commands
{
    /// <summary>
    ///     A function which returns a boolean value indicating whether a command can be executed in
    ///     its current state.
    /// </summary>
    /// <returns>
    ///     <see langword="true"/> if the command can be executed; <see langword="false"/> if not.
    /// </returns>
    public delegate bool CanExecuteFunc();

    /// <summary>
    ///     A function which is executed when a command is executed.
    /// </summary>
    public delegate void ExecuteAction();
}
