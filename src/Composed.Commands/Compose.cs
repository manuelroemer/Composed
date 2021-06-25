namespace Composed.Commands
{
    using System;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Threading;
    using System.Threading.Tasks;
    using Composed;
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
        private static readonly Func<bool> CanAlwaysExecute = () => true;

        #region UseCommand Sync

        /// <summary>
        ///     Creates and returns a new <see cref="ComposedCommand"/> instance which can always
        ///     be executed and, when executed, invokes the specified <paramref name="execute"/> function.
        /// </summary>
        /// <param name="execute">
        ///     The function to be invoked when the command is executed.
        /// </param>
        /// <returns>
        ///     A new <see cref="ComposedCommand"/> instance which can always
        ///     be executed and, when executed, invokes the specified <paramref name="execute"/> function.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="execute"/> is <see langword="null"/>.
        /// </exception>
        public static ComposedCommand UseCommand(Action execute)
        {
            return UseCommand(execute, CanAlwaysExecute, scheduler: null, Array.Empty<IObservable<Unit>>());
        }

        /// <summary>
        ///     <para>
        ///         Creates and returns a new <see cref="ComposedCommand"/> instance which invokes the
        ///         specified <paramref name="execute"/> function when it's executed and which
        ///         uses the specified <paramref name="canExecute"/> function and <paramref name="dependencies"/>
        ///         for determining when it can be executed.
        ///     </para>
        ///     <para>
        ///         Scheduling of <paramref name="canExecute"/> invocations and any subsequent notifications
        ///         depends on the observables passed as <paramref name="dependencies"/>.
        ///     </para>
        /// </summary>
        /// <param name="execute">
        ///     The function to be invoked when the command is executed.
        /// </param>
        /// <param name="canExecute">
        ///     A function which returns a boolean value indicating whether a command can be
        ///     executed in its current state.
        /// </param>
        /// <param name="dependencies">
        ///     <para>
        ///         A set of <paramref name="canExecute"/> dependencies which will be watched for changes.
        ///         This can be any kind of observable. <see cref="IRef{T}"/> and <see cref="IReadOnlyRef{T}"/> instances
        ///         implement <see cref="IObservable{T}"/> and can directly be passed as dependencies.
        ///     </para>
        ///     <para>
        ///         If this is empty, <paramref name="canExecute"/> will never change.
        ///     </para>
        /// </param>
        /// <returns>
        ///     A new <see cref="ComposedCommand"/> instance which invokes the specified <paramref name="execute"/>
        ///     function when it's executed and which uses the specified <paramref name="canExecute"/> function and
        ///     <paramref name="dependencies"/> for determining when it can be executed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="execute"/>, <paramref name="canExecute"/>, <paramref name="dependencies"/>
        ///     or one of the dependencies in the <paramref name="dependencies"/> array is <see langword="null"/>.
        /// </exception>
        public static ComposedCommand UseCommand(
            Action execute,
            Func<bool> canExecute,
            params IObservable<Unit>[] dependencies
        )
        {
            return UseCommand(execute, canExecute, scheduler: null, dependencies);
        }

        /// <summary>
        ///     <para>
        ///         Creates and returns a new <see cref="ComposedCommand"/> instance which invokes the
        ///         specified <paramref name="execute"/> function when it's executed and which
        ///         uses the specified <paramref name="canExecute"/> function and <paramref name="dependencies"/>
        ///         for determining when it can be executed.
        ///     </para>
        ///     <para>
        ///         This overload allows you to specify an <see cref="IScheduler"/>
        ///         which is used for scheduling <paramref name="canExecute"/> invocations and any subsequent
        ///         notifications.
        ///     </para>
        /// </summary>
        /// <param name="execute">
        ///     The function to be invoked when the command is executed.
        /// </param>
        /// <param name="canExecute">
        ///     A function which returns a boolean value indicating whether a command can be
        ///     executed in its current state.
        /// </param>
        /// <param name="scheduler">
        ///     <para>
        ///         An <see cref="IScheduler"/> on which the <paramref name="canExecute"/> invocations and
        ///         any subsequent notifications are scheduled.<br/>
        ///         <b>Important: </b> The initial invocation of <paramref name="canExecute"/> is always run immediately
        ///         on the calling thread and <i>is not</i> scheduled on this scheduler.
        ///     </para>
        ///     <para>
        ///         If this is <see langword="null"/>, scheduling of <paramref name="canExecute"/> invocations
        ///         depends on the observables passed as <paramref name="dependencies"/>.
        ///     </para>
        /// </param>
        /// <param name="dependencies">
        ///     <para>
        ///         A set of <paramref name="canExecute"/> dependencies which will be watched for changes.
        ///         This can be any kind of observable. <see cref="IRef{T}"/> and <see cref="IReadOnlyRef{T}"/> instances
        ///         implement <see cref="IObservable{T}"/> and can directly be passed as dependencies.
        ///     </para>
        ///     <para>
        ///         If this is empty, <paramref name="canExecute"/> will never change.
        ///     </para>
        /// </param>
        /// <returns>
        ///     A new <see cref="ComposedCommand"/> instance which invokes the specified <paramref name="execute"/>
        ///     function when it's executed and which uses the specified <paramref name="canExecute"/> function and
        ///     <paramref name="dependencies"/> for determining when it can be executed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="execute"/>, <paramref name="canExecute"/>, <paramref name="dependencies"/>
        ///     or one of the dependencies in the <paramref name="dependencies"/> array is <see langword="null"/>.
        /// </exception>
        public static ComposedCommand UseCommand(
            Action execute,
            Func<bool> canExecute,
            IScheduler? scheduler,
            params IObservable<Unit>[] dependencies
        )
        {
            _ = execute ?? throw new ArgumentNullException(nameof(execute));
            _ = canExecute ?? throw new ArgumentNullException(nameof(canExecute));
            // dependencies is ANE validated in ComposedCommand's constructor via the Watch call.
            return new ComposedCommand(execute, canExecute, scheduler, dependencies);
        }

        #endregion

        #region UseCommand Async

        /// <inheritdoc cref="UseCommand(Func{CancellationToken, Task})"/>
        public static AsyncComposedCommand UseCommand(Func<Task> executeAsync)
        {
            _ = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            return UseCommand(_ => executeAsync());
        }

        /// <summary>
        ///     Creates and returns a new <see cref="AsyncComposedCommand"/> instance which can always
        ///     be executed and, when executed, invokes the specified <paramref name="executeAsync"/> function.
        /// </summary>
        /// <param name="executeAsync">
        ///     The function to be invoked when the command is executed.
        /// </param>
        /// <returns>
        ///     A new <see cref="AsyncComposedCommand"/> instance which can always
        ///     be executed and, when executed, invokes the specified <paramref name="executeAsync"/> function.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="executeAsync"/> is <see langword="null"/>.
        /// </exception>
        public static AsyncComposedCommand UseCommand(Func<CancellationToken, Task> executeAsync)
        {
            return UseCommand(executeAsync, CanAlwaysExecute, scheduler: null, Array.Empty<IObservable<Unit>>());
        }

        /// <inheritdoc cref="UseCommand(Func{CancellationToken, Task}, Func{bool}, IObservable{Unit}[])"/>
        public static AsyncComposedCommand UseCommand(
           Func<Task> executeAsync,
           Func<bool> canExecute,
           params IObservable<Unit>[] dependencies
        )
        {
            _ = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            return UseCommand(_ => executeAsync(), canExecute, dependencies);
        }

        /// <summary>
        ///     <para>
        ///         Creates and returns a new <see cref="AsyncComposedCommand"/> instance which invokes the
        ///         specified <paramref name="executeAsync"/> function when it's executed and which
        ///         uses the specified <paramref name="canExecute"/> function and <paramref name="dependencies"/>
        ///         for determining when it can be executed.
        ///     </para>
        ///     <para>
        ///         Scheduling of <paramref name="canExecute"/> invocations and any subsequent notifications
        ///         depends on the observables passed as <paramref name="dependencies"/>.
        ///     </para>
        /// </summary>
        /// <param name="executeAsync">
        ///     The function to be invoked when the command is executed.
        /// </param>
        /// <param name="canExecute">
        ///     A function which returns a boolean value indicating whether a command can be
        ///     executed in its current state.
        /// </param>
        /// <param name="dependencies">
        ///     <para>
        ///         A set of <paramref name="canExecute"/> dependencies which will be watched for changes.
        ///         This can be any kind of observable. <see cref="IRef{T}"/> and <see cref="IReadOnlyRef{T}"/> instances
        ///         implement <see cref="IObservable{T}"/> and can directly be passed as dependencies.
        ///     </para>
        ///     <para>
        ///         If this is empty, <paramref name="canExecute"/> will never change.
        ///     </para>
        /// </param>
        /// <returns>
        ///     A new <see cref="AsyncComposedCommand"/> instance which invokes the specified <paramref name="executeAsync"/>
        ///     function when it's executed and which uses the specified <paramref name="canExecute"/> function and
        ///     <paramref name="dependencies"/> for determining when it can be executed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="executeAsync"/>, <paramref name="canExecute"/>, <paramref name="dependencies"/>
        ///     or one of the dependencies in the <paramref name="dependencies"/> array is <see langword="null"/>.
        /// </exception>
        public static AsyncComposedCommand UseCommand(
            Func<CancellationToken, Task> executeAsync,
            Func<bool> canExecute,
            params IObservable<Unit>[] dependencies
        )
        {
            return UseCommand(executeAsync, canExecute, scheduler: null, dependencies);
        }

        /// <inheritdoc cref="UseCommand(Func{CancellationToken, Task}, Func{bool}, IScheduler?, IObservable{Unit}[])"/>
        public static AsyncComposedCommand UseCommand(
            Func<Task> executeAsync,
            Func<bool> canExecute,
            IScheduler? scheduler,
            params IObservable<Unit>[] dependencies
        )
        {
            _ = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            return UseCommand(_ => executeAsync(), canExecute, scheduler, dependencies);
        }

        /// <summary>
        ///     <para>
        ///         Creates and returns a new <see cref="AsyncComposedCommand"/> instance which invokes the
        ///         specified <paramref name="executeAsync"/> function when it's executed and which
        ///         uses the specified <paramref name="canExecute"/> function and <paramref name="dependencies"/>
        ///         for determining when it can be executed.
        ///     </para>
        ///     <para>
        ///         This overload allows you to specify an <see cref="IScheduler"/>
        ///         which is used for scheduling <paramref name="canExecute"/> invocations and any subsequent
        ///         notifications.
        ///     </para>
        /// </summary>
        /// <param name="executeAsync">
        ///     The function to be invoked when the command is executed.
        /// </param>
        /// <param name="canExecute">
        ///     A function which returns a boolean value indicating whether a command can be
        ///     executed in its current state.
        /// </param>
        /// <param name="scheduler">
        ///     <para>
        ///         An <see cref="IScheduler"/> on which the <paramref name="canExecute"/> invocations and
        ///         any subsequent notifications are scheduled.<br/>
        ///         <b>Important: </b> The initial invocation of <paramref name="canExecute"/> is always run immediately
        ///         on the calling thread and <i>is not</i> scheduled on this scheduler.
        ///     </para>
        ///     <para>
        ///         If this is <see langword="null"/>, scheduling of <paramref name="canExecute"/> invocations
        ///         depends on the observables passed as <paramref name="dependencies"/>.
        ///     </para>
        /// </param>
        /// <param name="dependencies">
        ///     <para>
        ///         A set of <paramref name="canExecute"/> dependencies which will be watched for changes.
        ///         This can be any kind of observable. <see cref="IRef{T}"/> and <see cref="IReadOnlyRef{T}"/> instances
        ///         implement <see cref="IObservable{T}"/> and can directly be passed as dependencies.
        ///     </para>
        ///     <para>
        ///         If this is empty, <paramref name="canExecute"/> will never change.
        ///     </para>
        /// </param>
        /// <returns>
        ///     A new <see cref="AsyncComposedCommand"/> instance which invokes the specified <paramref name="executeAsync"/>
        ///     function when it's executed and which uses the specified <paramref name="canExecute"/> function and
        ///     <paramref name="dependencies"/> for determining when it can be executed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="executeAsync"/>, <paramref name="canExecute"/>, <paramref name="dependencies"/>
        ///     or one of the dependencies in the <paramref name="dependencies"/> array is <see langword="null"/>.
        /// </exception>
        public static AsyncComposedCommand UseCommand(
            Func<CancellationToken, Task> executeAsync,
            Func<bool> canExecute,
            IScheduler? scheduler,
            params IObservable<Unit>[] dependencies
        )
        {
            _ = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _ = canExecute ?? throw new ArgumentNullException(nameof(canExecute));
            // dependencies is ANE validated in AsyncComposedCommand's constructor via the Watch call.
            return new AsyncComposedCommand(executeAsync, canExecute, scheduler, dependencies);
        }

        #endregion

        #region UseBlockingCommand Sync

        /// <summary>
        ///     <para>
        ///         Creates and returns a new <see cref="ComposedCommand"/> instance which can always
        ///         be executed (except <i>while</i> it is executing) and, when executed,
        ///         invokes the specified <paramref name="execute"/> function.
        ///     </para>
        ///     <para>
        ///         Blocking commands generally cannot be executed <i>while</i> they are executing.
        ///     </para>
        /// </summary>
        /// <param name="execute">
        ///     The function to be invoked when the command is executed.
        /// </param>
        /// <returns>
        ///     A new <see cref="ComposedCommand"/> instance which can always
        ///     be executed and, when executed, invokes the specified <paramref name="execute"/> function.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="execute"/> is <see langword="null"/>.
        /// </exception>
        public static ComposedCommand UseBlockingCommand(Action execute)
        {
            return UseBlockingCommand(execute, CanAlwaysExecute, scheduler: null, Array.Empty<IObservable<Unit>>());
        }

        /// <summary>
        ///     <para>
        ///         Creates and returns a new <see cref="ComposedCommand"/> instance which invokes the
        ///         specified <paramref name="execute"/> function when it's executed and which
        ///         uses the specified <paramref name="canExecute"/> function and <paramref name="dependencies"/>
        ///         for determining when it can be executed.
        ///     </para>
        ///     <para>
        ///         Blocking commands generally cannot be executed <i>while</i> they are executing.
        ///     </para>
        ///     <para>
        ///         Scheduling of <paramref name="canExecute"/> invocations and any subsequent notifications
        ///         depends on the observables passed as <paramref name="dependencies"/>.
        ///     </para>
        /// </summary>
        /// <param name="execute">
        ///     The function to be invoked when the command is executed.
        /// </param>
        /// <param name="canExecute">
        ///     A function which returns a boolean value indicating whether a command can be
        ///     executed in its current state.
        /// </param>
        /// <param name="dependencies">
        ///     <para>
        ///         A set of <paramref name="canExecute"/> dependencies which will be watched for changes.
        ///         This can be any kind of observable. <see cref="IRef{T}"/> and <see cref="IReadOnlyRef{T}"/> instances
        ///         implement <see cref="IObservable{T}"/> and can directly be passed as dependencies.
        ///     </para>
        ///     <para>
        ///         If this is empty, <paramref name="canExecute"/> will never change.
        ///     </para>
        /// </param>
        /// <returns>
        ///     A new <see cref="ComposedCommand"/> instance which invokes the specified <paramref name="execute"/>
        ///     function when it's executed and which uses the specified <paramref name="canExecute"/> function and
        ///     <paramref name="dependencies"/> for determining when it can be executed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="execute"/>, <paramref name="canExecute"/>, <paramref name="dependencies"/>
        ///     or one of the dependencies in the <paramref name="dependencies"/> array is <see langword="null"/>.
        /// </exception>
        public static ComposedCommand UseBlockingCommand(
            Action execute,
            Func<bool> canExecute,
            params IObservable<Unit>[] dependencies
        )
        {
            return UseBlockingCommand(execute, canExecute, scheduler: null, dependencies);
        }

        /// <summary>
        ///     <para>
        ///         Creates and returns a new <see cref="ComposedCommand"/> instance which invokes the
        ///         specified <paramref name="execute"/> function when it's executed and which
        ///         uses the specified <paramref name="canExecute"/> function and <paramref name="dependencies"/>
        ///         for determining when it can be executed.
        ///     </para>
        ///     <para>
        ///         Blocking commands generally cannot be executed <i>while</i> they are executing.
        ///     </para>
        ///     <para>
        ///         This overload allows you to specify an <see cref="IScheduler"/>
        ///         which is used for scheduling <paramref name="canExecute"/> invocations and any subsequent
        ///         notifications.
        ///     </para>
        /// </summary>
        /// <param name="execute">
        ///     The function to be invoked when the command is executed.
        /// </param>
        /// <param name="canExecute">
        ///     A function which returns a boolean value indicating whether a command can be
        ///     executed in its current state.
        /// </param>
        /// <param name="scheduler">
        ///     <para>
        ///         An <see cref="IScheduler"/> on which the <paramref name="canExecute"/> invocations and
        ///         any subsequent notifications are scheduled.<br/>
        ///         <b>Important: </b> The initial invocation of <paramref name="canExecute"/> is always run immediately
        ///         on the calling thread and <i>is not</i> scheduled on this scheduler.
        ///     </para>
        ///     <para>
        ///         If this is <see langword="null"/>, scheduling of <paramref name="canExecute"/> invocations
        ///         depends on the observables passed as <paramref name="dependencies"/>.
        ///     </para>
        /// </param>
        /// <param name="dependencies">
        ///     <para>
        ///         A set of <paramref name="canExecute"/> dependencies which will be watched for changes.
        ///         This can be any kind of observable. <see cref="IRef{T}"/> and <see cref="IReadOnlyRef{T}"/> instances
        ///         implement <see cref="IObservable{T}"/> and can directly be passed as dependencies.
        ///     </para>
        ///     <para>
        ///         If this is empty, <paramref name="canExecute"/> will never change.
        ///     </para>
        /// </param>
        /// <returns>
        ///     A new <see cref="ComposedCommand"/> instance which invokes the specified <paramref name="execute"/>
        ///     function when it's executed and which uses the specified <paramref name="canExecute"/> function and
        ///     <paramref name="dependencies"/> for determining when it can be executed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="execute"/>, <paramref name="canExecute"/>, <paramref name="dependencies"/>
        ///     or one of the dependencies in the <paramref name="dependencies"/> array is <see langword="null"/>.
        /// </exception>
        public static ComposedCommand UseBlockingCommand(
            Action execute,
            Func<bool> canExecute,
            IScheduler? scheduler,
            params IObservable<Unit>[] dependencies
        )
        {
            _ = canExecute ?? throw new ArgumentNullException(nameof(canExecute));
            _ = execute ?? throw new ArgumentNullException(nameof(execute));
            _ = dependencies ?? throw new ArgumentNullException(nameof(dependencies));

            var isExecuting = Ref(false);
            return UseCommand(BlockingExecute, BlockingCanExecute, scheduler, PushDependency(dependencies, isExecuting));

            bool BlockingCanExecute() =>
                !isExecuting.Value && canExecute();

            void BlockingExecute()
            {
                try
                {
                    isExecuting.Value = true;
                    execute();
                }
                finally
                {
                    isExecuting.Value = false;
                }
            }
        }

        #endregion

        #region UseBlockingCommand Async

        /// <inheritdoc cref="UseBlockingCommand(Func{CancellationToken, Task})"/>
        public static AsyncComposedCommand UseBlockingCommand(Func<Task> executeAsync)
        {
            _ = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            return UseBlockingCommand(_ => executeAsync());
        }

        /// <summary>
        ///     <para>
        ///         Creates and returns a new <see cref="AsyncComposedCommand"/> instance which can
        ///         always be executed (except <i>while</i> it is executing) and, when executed,
        ///         invokes the specified <paramref name="executeAsync"/> function.
        ///     </para>
        ///     <para>
        ///         Blocking commands generally cannot be executed <i>while</i> they are executing.
        ///     </para>
        /// </summary>
        /// <param name="executeAsync">
        ///     The function to be invoked when the command is executed.
        /// </param>
        /// <returns>
        ///     A new <see cref="AsyncComposedCommand"/> instance which can always
        ///     be executed and, when executed, invokes the specified <paramref name="executeAsync"/> function.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="executeAsync"/> is <see langword="null"/>.
        /// </exception>
        public static AsyncComposedCommand UseBlockingCommand(Func<CancellationToken, Task> executeAsync)
        {
            return UseBlockingCommand(executeAsync, CanAlwaysExecute, scheduler: null, Array.Empty<IObservable<Unit>>());
        }

        /// <inheritdoc cref="UseBlockingCommand(Func{CancellationToken, Task}, Func{bool}, IObservable{Unit}[])"/>
        public static AsyncComposedCommand UseBlockingCommand(
           Func<Task> executeAsync,
           Func<bool> canExecute,
           params IObservable<Unit>[] dependencies
        )
        {
            _ = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            return UseBlockingCommand(_ => executeAsync(), canExecute, dependencies);
        }

        /// <summary>
        ///     <para>
        ///         Creates and returns a new <see cref="AsyncComposedCommand"/> instance which invokes the
        ///         specified <paramref name="executeAsync"/> function when it's executed and which
        ///         uses the specified <paramref name="canExecute"/> function and <paramref name="dependencies"/>
        ///         for determining when it can be executed.
        ///     </para>
        ///     <para>
        ///         Blocking commands generally cannot be executed <i>while</i> they are executing.
        ///     </para>
        ///     <para>
        ///         Scheduling of <paramref name="canExecute"/> invocations and any subsequent notifications
        ///         depends on the observables passed as <paramref name="dependencies"/>.
        ///     </para>
        /// </summary>
        /// <param name="executeAsync">
        ///     The function to be invoked when the command is executed.
        /// </param>
        /// <param name="canExecute">
        ///     A function which returns a boolean value indicating whether a command can be
        ///     executed in its current state.
        /// </param>
        /// <param name="dependencies">
        ///     <para>
        ///         A set of <paramref name="canExecute"/> dependencies which will be watched for changes.
        ///         This can be any kind of observable. <see cref="IRef{T}"/> and <see cref="IReadOnlyRef{T}"/> instances
        ///         implement <see cref="IObservable{T}"/> and can directly be passed as dependencies.
        ///     </para>
        ///     <para>
        ///         If this is empty, <paramref name="canExecute"/> will never change.
        ///     </para>
        /// </param>
        /// <returns>
        ///     A new <see cref="AsyncComposedCommand"/> instance which invokes the specified <paramref name="executeAsync"/>
        ///     function when it's executed and which uses the specified <paramref name="canExecute"/> function and
        ///     <paramref name="dependencies"/> for determining when it can be executed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="executeAsync"/>, <paramref name="canExecute"/>, <paramref name="dependencies"/>
        ///     or one of the dependencies in the <paramref name="dependencies"/> array is <see langword="null"/>.
        /// </exception>
        public static AsyncComposedCommand UseBlockingCommand(
            Func<CancellationToken, Task> executeAsync,
            Func<bool> canExecute,
            params IObservable<Unit>[] dependencies
        )
        {
            return UseBlockingCommand(executeAsync, canExecute, scheduler: null, dependencies);
        }

        /// <inheritdoc cref="UseBlockingCommand(Func{CancellationToken, Task}, Func{bool}, IScheduler?, IObservable{Unit}[])"/>
        public static AsyncComposedCommand UseBlockingCommand(
            Func<Task> executeAsync,
            Func<bool> canExecute,
            IScheduler? scheduler,
            params IObservable<Unit>[] dependencies
        )
        {
            _ = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            return UseBlockingCommand(_ => executeAsync(), canExecute, scheduler, dependencies);
        }

        /// <summary>
        ///     <para>
        ///         Creates and returns a new <see cref="AsyncComposedCommand"/> instance which invokes the
        ///         specified <paramref name="executeAsync"/> function when it's executed and which
        ///         uses the specified <paramref name="canExecute"/> function and <paramref name="dependencies"/>
        ///         for determining when it can be executed.
        ///     </para>
        ///     <para>
        ///         Blocking commands generally cannot be executed <i>while</i> they are executing.
        ///     </para>
        ///     <para>
        ///         This overload allows you to specify an <see cref="IScheduler"/>
        ///         which is used for scheduling <paramref name="canExecute"/> invocations and any subsequent
        ///         notifications.
        ///     </para>
        /// </summary>
        /// <param name="executeAsync">
        ///     The function to be invoked when the command is executed.
        /// </param>
        /// <param name="canExecute">
        ///     A function which returns a boolean value indicating whether a command can be
        ///     executed in its current state.
        /// </param>
        /// <param name="scheduler">
        ///     <para>
        ///         An <see cref="IScheduler"/> on which the <paramref name="canExecute"/> invocations and
        ///         any subsequent notifications are scheduled.<br/>
        ///         <b>Important: </b> The initial invocation of <paramref name="canExecute"/> is always run immediately
        ///         on the calling thread and <i>is not</i> scheduled on this scheduler.
        ///     </para>
        ///     <para>
        ///         If this is <see langword="null"/>, scheduling of <paramref name="canExecute"/> invocations
        ///         depends on the observables passed as <paramref name="dependencies"/>.
        ///     </para>
        /// </param>
        /// <param name="dependencies">
        ///     <para>
        ///         A set of <paramref name="canExecute"/> dependencies which will be watched for changes.
        ///         This can be any kind of observable. <see cref="IRef{T}"/> and <see cref="IReadOnlyRef{T}"/> instances
        ///         implement <see cref="IObservable{T}"/> and can directly be passed as dependencies.
        ///     </para>
        ///     <para>
        ///         If this is empty, <paramref name="canExecute"/> will never change.
        ///     </para>
        /// </param>
        /// <returns>
        ///     A new <see cref="AsyncComposedCommand"/> instance which invokes the specified <paramref name="executeAsync"/>
        ///     function when it's executed and which uses the specified <paramref name="canExecute"/> function and
        ///     <paramref name="dependencies"/> for determining when it can be executed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="executeAsync"/>, <paramref name="canExecute"/>, <paramref name="dependencies"/>
        ///     or one of the dependencies in the <paramref name="dependencies"/> array is <see langword="null"/>.
        /// </exception>
        public static AsyncComposedCommand UseBlockingCommand(
            Func<CancellationToken, Task> executeAsync,
            Func<bool> canExecute,
            IScheduler? scheduler,
            params IObservable<Unit>[] dependencies
        )
        {
            _ = canExecute ?? throw new ArgumentNullException(nameof(canExecute));
            _ = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _ = dependencies ?? throw new ArgumentNullException(nameof(dependencies));

            var isExecuting = Ref(false);
            return UseCommand(BlockingExecute, BlockingCanExecute, scheduler, PushDependency(dependencies, isExecuting));

            bool BlockingCanExecute() =>
                !isExecuting.Value && canExecute();

            async Task BlockingExecute(CancellationToken cancellationToken)
            {
                try
                {
                    isExecuting.Value = true;
                    await executeAsync(cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    isExecuting.Value = false;
                }
            }
        }

        #endregion

        private static IObservable<Unit>[] PushDependency(IObservable<Unit>[] dependencies, IObservable<Unit> next)
        {
            var result = new IObservable<Unit>[dependencies.Length + 1];
            dependencies.CopyTo(result, 0);
            result[result.Length - 1] = next;
            return result;
        }
    }
}
