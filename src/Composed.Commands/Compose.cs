namespace Composed.Commands
{
    using System;
    using System.Windows.Input;
    using Composed;
    using Composed.Commands.Internal;
    using static Composed.Compose;

    /// <summary>
    ///     <para>
    ///         Provides a set of static methods which enable the composition of commands.
    ///     </para>
    ///     <para>
    ///         It is recommended that you import this class via a <c>using static</c> directive
    ///         (<c>using static Composed.Commands.Compose;</c>) if you are using C#.
    ///     </para>
    /// </summary>
    public static class Compose
    {
        /// <inheritdoc cref="UseCommand{TParameter}(ExecuteAction{TParameter})"/>
        public static IComposedCommand<object?> UseCommand(ExecuteAction execute)
        {
            _ = execute ?? throw new ArgumentNullException(nameof(execute));
            return new ComposedCommand<object?>(execute);
        }

        /// <summary>
        ///     Creates and returns a new <see cref="IComposedCommand{TParameter}"/> instance
        ///     which forwards its <c>Execute</c> method calls to the specified <paramref name="execute"/>
        ///     function.
        ///     This command can always be executed without restrictions.
        /// </summary>
        /// <typeparam name="TParameter">
        ///     The type of an optional parameter which may be passed to the command.
        /// </typeparam>
        /// <param name="execute">
        ///     A function which is invoked when the command is executed.
        /// </param>
        /// <returns>
        ///     A new <see cref="IComposedCommand{TParameter}"/> instance which forwards its <c>Execute</c> method
        ///     calls to the specified <paramref name="execute"/> function.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="execute"/> is <see langword="null"/>.
        /// </exception>
        public static IComposedCommand<TParameter> UseCommand<TParameter>(ExecuteAction<TParameter> execute)
        {
            _ = execute ?? throw new ArgumentNullException(nameof(execute));
            return new ComposedCommand<TParameter>(execute);
        }

        /// <inheritdoc cref="UseCommand{TParameter}(ExecuteAction{TParameter}, CanExecuteFunc{TParameter}, IDependency[])"/>
        public static IComposedCommand<object?> UseCommand(
            ExecuteAction execute,
            CanExecuteFunc canExecute,
            params IDependency[] dependencies
        )
        {
            _ = execute ?? throw new ArgumentNullException(nameof(execute));
            _ = canExecute ?? throw new ArgumentNullException(nameof(canExecute));
            var command = new ComposedCommand<object?>(execute, canExecute);
            Watch(() => command.RaiseCanExecuteChanged(), dependencies);
            return command;
        }

        /// <summary>
        ///     Creates and returns a new <see cref="IComposedCommand{TParameter}"/> instance
        ///     which forwards its method calls to the specified <paramref name="execute"/> and
        ///     <paramref name="canExecute"/> functions.
        ///     The command watches each dependency in the <paramref name="dependencies"/>
        ///     array for changes and raises the <see cref="ICommand.CanExecuteChanged"/> event whenever
        ///     such a change occurs.
        /// </summary>
        /// <typeparam name="TParameter">
        ///     The type of an optional parameter which may be passed to the command.
        /// </typeparam>
        /// <param name="execute">
        ///     A function which is invoked when the command is executed.
        /// </param>
        /// <param name="canExecute">
        ///     A function which returns a boolean value indicating whether a command can be executed in
        ///     its current state.
        /// </param>
        /// <param name="dependencies">
        ///     <para>
        ///         A set of dependencies which will be watched for changes.
        ///     </para>
        ///     <para>
        ///         This can be any object implementing the <see cref="IDependency"/> interface.
        ///         Refs implement this interface and can directly be passed.
        ///         Composed also provides a way to convert any <see cref="IObservable{T}"/> to an
        ///         <see cref="IDependency"/> via the
        ///         <see cref="Composed.ObservableExtensions.ToDependency{T}(IObservable{T})"/> function.
        ///     </para>
        ///     <para>
        ///         If this is empty, the command's <see cref="ICommand.CanExecuteChanged"/> event
        ///         will never be raised.
        ///     </para>
        /// </param>
        /// <returns>
        ///     A new <see cref="IComposedCommand{TParameter}"/> instance which forwards its method
        ///     calls to the specified <paramref name="execute"/> and <paramref name="canExecute"/> functions.
        /// </returns>
        /// <remarks>
        ///     The returned command has the following behaviors:
        ///
        ///     <list type="bullet">
        ///         <item>
        ///             <description>
        ///                 The <paramref name="dependencies"/> are watched using the
        ///                 <see cref="Watch(Action, IDependency[])"/> function.
        ///                 All dependency specific behaviors documented in <see cref="Watch(Action, IDependency[])"/>
        ///                 also apply to this function.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 The <c>Execute</c> overloads invoke <paramref name="canExecute"/>
        ///                 before <paramref name="execute"/> and throw an <see cref="InvalidOperationException"/>
        ///                 if <paramref name="canExecute"/> returns <see langword="false"/>.
        ///             </description>
        ///         </item>
        ///     </list>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="execute"/>, <paramref name="canExecute"/>, <paramref name="dependencies"/>
        ///     or one of the dependencies in the <paramref name="dependencies"/> array is <see langword="null"/>.
        /// </exception>
        public static IComposedCommand<TParameter> UseCommand<TParameter>(
            ExecuteAction<TParameter> execute,
            CanExecuteFunc<TParameter> canExecute,
            params IDependency[] dependencies
        )
        {
            _ = execute ?? throw new ArgumentNullException(nameof(execute));
            _ = canExecute ?? throw new ArgumentNullException(nameof(canExecute));
            var command = new ComposedCommand<TParameter>(execute, canExecute);
            Watch(() => command.RaiseCanExecuteChanged(), dependencies);
            return command;
        }
    }
}
