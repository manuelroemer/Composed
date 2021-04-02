namespace Composed
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Composed.Internal;

    /// <summary>
    ///     <para>
    ///         Contains a set of static methods which provide the entry point to and the fundamental
    ///         members of Composed's API.
    ///     </para>
    ///     <para>
    ///         It is recommended that you import this class via a <c>using static</c> directive
    ///         (<c>using static Composed.Compose;</c>) if you are using C#.
    ///     </para>
    /// </summary>
    public static partial class Compose
    {
        private static readonly IObservable<Unit> RunEffectImmediatelyObservable = Observable.Return(Unit.Default);

        /// <summary>
        ///     <para>
        ///         Creates and returns a new mutable <see cref="IRef{T}"/> instance which holds the
        ///         specified <paramref name="initialValue"/>.
        ///     </para>
        ///     <para>
        ///         When the ref's value is set, it will use <see cref="EqualityComparer{T}.Default"/>
        ///         for determining whether its value effectively changes and whether observers
        ///         should be notified about that change.
        ///     </para>
        /// </summary>
        /// <typeparam name="T">The type of the value held by the ref.</typeparam>
        /// <param name="initialValue">
        ///     The initial value to be held by the ref.
        /// </param>
        /// <returns>
        ///     A new mutable <see cref="IRef{T}"/> instance which holds the specified <paramref name="initialValue"/>.
        /// </returns>
        public static IRef<T> Ref<T>(T initialValue) =>
            Ref(initialValue, equalityComparer: null);

        /// <summary>
        ///     <para>
        ///         Creates and returns a new mutable <see cref="IRef{T}"/> instance which holds the
        ///         specified <paramref name="initialValue"/>.
        ///     </para>
        ///     <para>
        ///         This overload allows you to specify an <see cref="IEqualityComparer{T}"/> which is used
        ///         by the ref for determining whether its value effectively changes and whether
        ///         observers should be notified about such a change.
        ///     </para>
        /// </summary>
        /// <typeparam name="T">The type of the value held by the ref.</typeparam>
        /// <param name="initialValue">
        ///     The initial value to be held by the ref.
        /// </param>
        /// <param name="equalityComparer">
        ///     <para>
        ///         An <see cref="IEqualityComparer{T}"/> instance which will be used to compare the
        ///         ref's new and old value when it changes.
        ///         The ref will only notify its observers about the change when the equality comparer
        ///         considers the two values unequal.
        ///     </para>
        ///     <para>
        ///         If this is <see langword="null"/>, <see cref="EqualityComparer{T}.Default"/> is used instead.
        ///     </para>
        ///     <para>
        ///         <b>Important: </b> The equality comparer's <see cref="IEqualityComparer{T}.Equals(T, T)"/>
        ///         method is called from a <c>lock</c> block.
        ///         To avoid deadlocks, ensure that the function is not blocking or joining any thread.
        ///     </para>
        /// </param>
        /// <returns>
        ///     A new mutable <see cref="IRef{T}"/> instance which holds the specified <paramref name="initialValue"/>.
        /// </returns>
        public static IRef<T> Ref<T>(T initialValue, IEqualityComparer<T>? equalityComparer) =>
            new Ref<T>(initialValue, equalityComparer);

        /// <summary>
        ///     <para>
        ///         Creates and returns a new <see cref="IReadOnlyRef{T}"/> instance whose value
        ///         is (re-)computed by the specified <paramref name="compute"/> function both initially
        ///         and whenever a dependency in the specified <paramref name="dependencies"/> array changes.
        ///         <br/>
        ///         <c>Computed</c> is therefore ideal to create a ref whose value depends on other refs.
        ///     </para>
        ///     <para>
        ///         When the ref's value is recomputed, it will use <see cref="EqualityComparer{T}.Default"/>
        ///         for determining whether its value effectively changes and whether observers
        ///         should be notified about that change.
        ///     </para>
        ///     <para>
        ///         Scheduling of <paramref name="compute"/> invocations and any subsequent notifications
        ///         depends on the observables passed as <paramref name="dependencies"/>.
        ///     </para>
        /// </summary>
        /// <typeparam name="TResult">
        ///     The type of the value returned by <paramref name="compute"/> and held by the ref.
        /// </typeparam>
        /// <param name="compute">
        ///     <para>
        ///         A function which computes the ref's new value.
        ///         It is immediately invoked to compute the ref's initial value and then subsequently
        ///         invoked whenever one of the <paramref name="dependencies"/> changes.
        ///     </para>
        ///     <para>
        ///         It is strongly recommeneded to keep this function pure and free of side-effects.
        ///         Use one of the respective <see cref="Watch(Action, IObservable{Unit}[])"/> or
        ///         <see cref="WatchEffect(Action, IObservable{Unit}[])"/> overloads if you want
        ///         run side-effects when a dependency changes.
        ///     </para>
        /// </param>
        /// <param name="dependencies">
        ///     <para>
        ///         A set of dependencies which will be watched for changes.
        ///         This can be any observable. <see cref="IRef{T}"/> and <see cref="IReadOnlyRef{T}"/> instances
        ///         implement <see cref="IObservable{T}"/> and can directly be passed as dependencies.
        ///     </para>
        ///     <para>
        ///         If this is empty, ref's value will never be recomputed.
        ///     </para>
        /// </param>
        /// <returns>
        ///     A new <see cref="IReadOnlyRef{T}"/> instance which initially holds the value returned
        ///     by the <paramref name="compute"/> function and recomputes that value whenever a dependency
        ///     in the specified <paramref name="dependencies"/> array changes.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="compute"/>, <paramref name="dependencies"/> or one of the dependencies in
        ///     the <paramref name="dependencies"/> array is <see langword="null"/>.
        /// </exception>
        public static IReadOnlyRef<TResult> Computed<TResult>(Func<TResult> compute, params IObservable<Unit>[] dependencies) =>
            Computed(compute, equalityComparer: null, scheduler: null, dependencies);

        /// <summary>
        ///     <para>
        ///         Creates and returns a new <see cref="IReadOnlyRef{T}"/> instance whose value
        ///         is (re-)computed by the specified <paramref name="compute"/> function both initially
        ///         and whenever a dependency in the specified <paramref name="dependencies"/> array changes.
        ///         <br/>
        ///         <c>Computed</c> is therefore ideal to create a ref whose value depends on other refs.
        ///     </para>
        ///     <para>
        ///         This overload allows you to specify an <see cref="IScheduler"/>
        ///         which is used for scheduling <paramref name="compute"/> invocations and any subsequent
        ///         notifications.
        ///     </para>
        ///     <para>
        ///         When the ref's value is recomputed, it will use <see cref="EqualityComparer{T}.Default"/>
        ///         for determining whether its value effectively changes and whether observers
        ///         should be notified about that change.
        ///     </para>
        /// </summary>
        /// <typeparam name="TResult">
        ///     The type of the value returned by <paramref name="compute"/> and held by the ref.
        /// </typeparam>
        /// <param name="compute">
        ///     <para>
        ///         A function which computes the ref's new value.
        ///         It is immediately invoked to compute the ref's initial value and then subsequently
        ///         invoked whenever one of the <paramref name="dependencies"/> changes.
        ///     </para>
        ///     <para>
        ///         It is strongly recommeneded to keep this function pure and free of side-effects.
        ///         Use one of the respective <see cref="Watch(Action, IObservable{Unit}[])"/> or
        ///         <see cref="WatchEffect(Action, IObservable{Unit}[])"/> overloads if you want
        ///         run side-effects when a dependency changes.
        ///     </para>
        /// </param>
        /// <param name="scheduler">
        ///     <para>
        ///         An <see cref="IScheduler"/> on which the <paramref name="compute"/> invocation and
        ///         any subsequent notifications are scheduled.<br/>
        ///         <b>Important: </b> The initial invocation of <paramref name="compute"/> is always run immediately
        ///         on the calling thread and <i>is not</i> scheduled on this scheduler.
        ///     </para>
        ///     <para>
        ///         If this is <see langword="null"/>, scheduling of <paramref name="compute"/> invocations
        ///         depends on the observables passed as <paramref name="dependencies"/>.
        ///     </para>
        /// </param>
        /// <param name="dependencies">
        ///     <para>
        ///         A set of dependencies which will be watched for changes.
        ///         This can be any observable. <see cref="IRef{T}"/> and <see cref="IReadOnlyRef{T}"/> instances
        ///         implement <see cref="IObservable{T}"/> and can directly be passed as dependencies.
        ///     </para>
        ///     <para>
        ///         If this is empty, ref's value will never be recomputed.
        ///     </para>
        /// </param>
        /// <returns>
        ///     A new <see cref="IReadOnlyRef{T}"/> instance which initially holds the value returned
        ///     by the <paramref name="compute"/> function and recomputes that value whenever a dependency
        ///     in the specified <paramref name="dependencies"/> array changes.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="compute"/>, <paramref name="dependencies"/> or one of the dependencies in
        ///     the <paramref name="dependencies"/> array is <see langword="null"/>.
        /// </exception>
        public static IReadOnlyRef<TResult> Computed<TResult>(
            Func<TResult> compute,
            IScheduler? scheduler,
            params IObservable<Unit>[] dependencies
        )
        {
            return Computed(compute, equalityComparer: null, scheduler, dependencies);
        }

        /// <summary>
        ///     <para>
        ///         Creates and returns a new <see cref="IReadOnlyRef{T}"/> instance whose value
        ///         is (re-)computed by the specified <paramref name="compute"/> function both initially
        ///         and whenever a dependency in the specified <paramref name="dependencies"/> array changes.
        ///         <br/>
        ///         <c>Computed</c> is therefore ideal to create a ref whose value depends on other refs.
        ///     </para>
        ///     <para>
        ///         This overload allows you to specify an <see cref="IEqualityComparer{T}"/> which is used
        ///         by the ref for determining whether its value effectively changes and whether
        ///         observers should be notified about such a change.
        ///     </para>
        ///     <para>
        ///         Scheduling of <paramref name="compute"/> invocations and any subsequent notifications
        ///         depends on the observables passed as <paramref name="dependencies"/>.
        ///     </para>
        /// </summary>
        /// <typeparam name="TResult">
        ///     The type of the value returned by <paramref name="compute"/> and held by the ref.
        /// </typeparam>
        /// <param name="compute">
        ///     <para>
        ///         A function which computes the ref's new value.
        ///         It is immediately invoked to compute the ref's initial value and then subsequently
        ///         invoked whenever one of the <paramref name="dependencies"/> changes.
        ///     </para>
        ///     <para>
        ///         It is strongly recommeneded to keep this function pure and free of side-effects.
        ///         Use one of the respective <see cref="Watch(Action, IObservable{Unit}[])"/> or
        ///         <see cref="WatchEffect(Action, IObservable{Unit}[])"/> overloads if you want
        ///         run side-effects when a dependency changes.
        ///     </para>
        /// </param>
        /// <param name="equalityComparer">
        ///     <para>
        ///         An <see cref="IEqualityComparer{T}"/> instance which will be used to compare the
        ///         ref's new and old value when it changes.
        ///         The ref will only notify its observers about the change when the equality comparer
        ///         considers the two values unequal.
        ///     </para>
        ///     <para>
        ///         If this is <see langword="null"/>, <see cref="EqualityComparer{T}.Default"/> is used instead.
        ///     </para>
        ///     <para>
        ///         <b>Important: </b> The equality comparer's <see cref="IEqualityComparer{T}.Equals(T, T)"/>
        ///         method is called from a <c>lock</c> block.
        ///         To avoid deadlocks, ensure that the function is not blocking or joining any thread.
        ///     </para>
        /// </param>
        /// <param name="dependencies">
        ///     <para>
        ///         A set of dependencies which will be watched for changes.
        ///         This can be any observable. <see cref="IRef{T}"/> and <see cref="IReadOnlyRef{T}"/> instances
        ///         implement <see cref="IObservable{T}"/> and can directly be passed as dependencies.
        ///     </para>
        ///     <para>
        ///         If this is empty, ref's value will never be recomputed.
        ///     </para>
        /// </param>
        /// <returns>
        ///     A new <see cref="IReadOnlyRef{T}"/> instance which initially holds the value returned
        ///     by the <paramref name="compute"/> function and recomputes that value whenever a dependency
        ///     in the specified <paramref name="dependencies"/> array changes.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="compute"/>, <paramref name="dependencies"/> or one of the dependencies in
        ///     the <paramref name="dependencies"/> array is <see langword="null"/>.
        /// </exception>
        public static IReadOnlyRef<TResult> Computed<TResult>(
            Func<TResult> compute,
            IEqualityComparer<TResult>? equalityComparer,
            params IObservable<Unit>[] dependencies
        )
        {
            return Computed(compute, equalityComparer, scheduler: null, dependencies);
        }

        /// <summary>
        ///     <para>
        ///         Creates and returns a new <see cref="IReadOnlyRef{T}"/> instance whose value
        ///         is (re-)computed by the specified <paramref name="compute"/> function both initially
        ///         and whenever a dependency in the specified <paramref name="dependencies"/> array changes.
        ///         <br/>
        ///         <c>Computed</c> is therefore ideal to create a ref whose value depends on other refs.
        ///     </para>
        ///     <para>
        ///         This overload allows you to specify an <see cref="IEqualityComparer{T}"/> which is used
        ///         by the ref for determining whether its value effectively changes and whether
        ///         observers should be notified about such a change and an <see cref="IScheduler"/>
        ///         which is used for scheduling <paramref name="compute"/> invocations and any
        ///         subsequent notifications.
        ///     </para>
        /// </summary>
        /// <typeparam name="TResult">
        ///     The type of the value returned by <paramref name="compute"/> and held by the ref.
        /// </typeparam>
        /// <param name="compute">
        ///     <para>
        ///         A function which computes the ref's new value.
        ///         It is immediately invoked to compute the ref's initial value and then subsequently
        ///         invoked whenever one of the <paramref name="dependencies"/> changes.
        ///     </para>
        ///     <para>
        ///         It is strongly recommeneded to keep this function pure and free of side-effects.
        ///         Use one of the respective <see cref="Watch(Action, IObservable{Unit}[])"/> or
        ///         <see cref="WatchEffect(Action, IObservable{Unit}[])"/> overloads if you want
        ///         run side-effects when a dependency changes.
        ///     </para>
        /// </param>
        /// <param name="equalityComparer">
        ///     <para>
        ///         An <see cref="IEqualityComparer{T}"/> instance which will be used to compare the
        ///         ref's new and old value when it changes.
        ///         The ref will only notify its observers about the change when the equality comparer
        ///         considers the two values unequal.
        ///     </para>
        ///     <para>
        ///         If this is <see langword="null"/>, <see cref="EqualityComparer{T}.Default"/> is used instead.
        ///     </para>
        ///     <para>
        ///         <b>Important: </b> The equality comparer's <see cref="IEqualityComparer{T}.Equals(T, T)"/>
        ///         method is called from a <c>lock</c> block.
        ///         To avoid deadlocks, ensure that the function is not blocking or joining any thread.
        ///     </para>
        /// </param>
        /// <param name="scheduler">
        ///     <para>
        ///         An <see cref="IScheduler"/> on which the <paramref name="compute"/> invocation and
        ///         any subsequent notifications are scheduled.<br/>
        ///         <b>Important: </b> The initial invocation of <paramref name="compute"/> is always run immediately
        ///         on the calling thread and <i>is not</i> scheduled on this scheduler.
        ///     </para>
        ///     <para>
        ///         If this is <see langword="null"/>, scheduling of <paramref name="compute"/> invocations
        ///         depends on the observables passed as <paramref name="dependencies"/>.
        ///     </para>
        /// </param>
        /// <param name="dependencies">
        ///     <para>
        ///         A set of dependencies which will be watched for changes.
        ///         This can be any observable. <see cref="IRef{T}"/> and <see cref="IReadOnlyRef{T}"/> instances
        ///         implement <see cref="IObservable{T}"/> and can directly be passed as dependencies.
        ///     </para>
        ///     <para>
        ///         If this is empty, ref's value will never be recomputed.
        ///     </para>
        /// </param>
        /// <returns>
        ///     A new <see cref="IReadOnlyRef{T}"/> instance which initially holds the value returned
        ///     by the <paramref name="compute"/> function and recomputes that value whenever a dependency
        ///     in the specified <paramref name="dependencies"/> array changes.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="compute"/>, <paramref name="dependencies"/> or one of the dependencies in
        ///     the <paramref name="dependencies"/> array is <see langword="null"/>.
        /// </exception>
        public static IReadOnlyRef<TResult> Computed<TResult>(
            Func<TResult> compute,
            IEqualityComparer<TResult>? equalityComparer,
            IScheduler? scheduler,
            params IObservable<Unit>[] dependencies
        )
        {
            _ = compute ?? throw new ArgumentNullException(nameof(compute));
            _ = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
            EnsureNoNullInDependencies(dependencies);
            return ComputedImpl(compute, equalityComparer, scheduler, dependencies);
        }

        public static IDisposable Watch(Action effect, params IObservable<Unit>[] dependencies) =>
            Watch(effect, scheduler: null, dependencies);

        /// <summary>
        ///     Watches each dependency specified in the <paramref name="dependencies"/> array for
        ///     changes and invokes the specified <paramref name="effect"/> whenever such a
        ///     change occurs.
        ///     In comparison to <see cref="WatchEffect(Action, IDependency[])"/>, the effect does
        ///     not get invoked before one of the dependencies changes for the first time.
        /// </summary>
        /// <param name="effect">
        ///     A function to be invoked whenever one of the specified dependencies changes.
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
        ///         <see cref="ObservableExtensions.ToDependency{T}(IObservable{T})"/> function.
        ///     </para>
        ///     <para>
        ///         If this is empty, the <paramref name="effect"/> will never be invoked.
        ///     </para>
        /// </param>
        /// <returns>
        ///     An <see cref="IDisposable"/> instance which, when disposed, ensures that the dependencies
        ///     are no longer being watched.
        ///     This can be used to cancel watching at any time and to free any references to the dependencies
        ///     created by the <c>Watch</c> function.
        /// </returns>
        /// <remarks>
        ///     This synchronous <c>Watch</c> variant has the following behaviors:
        ///
        ///     <list type="bullet">
        ///         <item>
        ///             <description>
        ///                 <c>Watch</c> does not catch any exception thrown by the <paramref name="effect"/> function.
        ///                 If <paramref name="effect"/> throws an exception, <c>Watch</c> stops watching the
        ///                 dependencies and re-throws the exception.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <c>Watch</c> does not catch any exceptions raised by observables in the
        ///                 <paramref name="dependencies"/> array.
        ///                 When an <see cref="IDependency.Changed"/> observable errors, <c>Watch</c> stops watching
        ///                 the dependencies and re-throws the exception.
        ///                 If you specify an <see cref="IObservable{T}"/> as a dependency (for example via code like
        ///                 <c>myObservable.ToDependency()</c>), ensure that the observable either never errors
        ///                 or that you catch such errors before the conversion to a dependency (for example via code
        ///                 like <c>myObservable.Catch(ex => ...).ToDependency()</c>).
        ///                 Refs created by Composed's API never error and can safely be used as dependencies.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <c>Watch</c> stops watching dependencies whose <see cref="IDependency.Changed"/> observable
        ///                 completes.
        ///                 Refs created by Composed's API never complete and will continuously trigger the
        ///                 <paramref name="effect"/> when changed (unless the <c>Watch</c> function's subscription
        ///                 is disposed).
        ///             </description>
        ///         </item>
        ///     </list>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="effect"/>, <paramref name="dependencies"/> or one of the dependencies in
        ///     the <paramref name="dependencies"/> array is <see langword="null"/>.
        /// </exception>
        /// <example>
        ///     <para>
        ///         The following code demonstrates how the <c>Watch</c> function reacts to dependency changes:
        ///
        ///         <code>
        ///         using System;
        ///         using Composed;
        ///         using static Composed.Compose;
        ///
        ///         IRef&lt;int&gt; dependency1 = Ref(0);
        ///         IRef&lt;int&gt; dependency2 = Ref(0);
        ///
        ///         Watch(() => Console.WriteLine($"Effect 1. dependency1: {dependency1.Value}"), dependency1);
        ///         Watch(() => Console.WriteLine($"Effect 2. dependency2: {dependency2.Value}"), dependency2);
        ///         Watch(
        ///             () => Console.WriteLine($"Effect 3. dependency1: {dependency1.Value}, dependency2: {dependency2.Value}"),
        ///             dependency1,
        ///             dependency2
        ///         );
        ///         
        ///         dependency1.Value = 1;
        ///         dependency2.Value = 1;
        ///
        ///         // Output:
        ///         // Effect 1. dependency1: 1
        ///         // Effect 3. dependency1: 1, dependency2: 0
        ///         // Effect 2. dependency2: 1
        ///         // Effect 3. dependency1: 1, dependency2: 1
        ///         </code>
        ///     </para>
        ///     <para>
        ///         The following code demonstrates how the <c>Watch</c> function can be canceled:
        ///
        ///         <code>
        ///         using System;
        ///         using Composed;
        ///         using static Composed.Compose;
        ///
        ///         IRef&lt;int&gt; dependency = Ref(0);
        ///         IDisposable watchSubscription = Watch(() => Console.WriteLine($"Effect ran. dependency: {dependency.Value}"), dependency);
        ///
        ///         dependency.Value = 1;
        ///         watchSubscription.Dispose();
        ///         dependency.Value = 2;
        ///
        ///         // Output:
        ///         // Effect ran. dependency: 1
        ///         </code>
        ///     </para>
        /// </example>
        public static IDisposable Watch(Action effect, IScheduler? scheduler, params IObservable<Unit>[] dependencies)
        {
            _ = effect ?? throw new ArgumentNullException(nameof(effect));
            _ = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
            EnsureNoNullInDependencies(dependencies);
            return SyncWatchImpl(effect, runEffectNow: false, scheduler, dependencies);
        }

        public static IDisposable Watch(Func<Task> effect, params IObservable<Unit>[] dependencies) =>
            Watch(effect, scheduler: null, dependencies);

        /// <summary>
        ///     Watches each dependency specified in the <paramref name="dependencies"/> array for
        ///     changes and invokes the specified <paramref name="effect"/> whenever such a
        ///     change occurs.
        ///     In comparison to <see cref="WatchEffect(Func{Task}, IObservable{Unit}[])"/>, the effect does
        ///     not get invoked before one of the dependencies changes for the first time.
        /// </summary>
        /// <param name="effect">
        ///     An asynchronous function to be invoked whenever one of the specified dependencies changes.
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
        ///         <see cref="ObservableExtensions.ToDependency{T}(IObservable{T})"/> function.
        ///     </para>
        ///     <para>
        ///         If this is empty, the <paramref name="effect"/> will never be invoked.
        ///     </para>
        /// </param>
        /// <returns>
        ///     An <see cref="IDisposable"/> instance which, when disposed, ensures that the dependencies
        ///     are no longer being watched.
        ///     This can be used to cancel watching at any time and to free any references to the dependencies
        ///     created by the <c>Watch</c> function.
        /// </returns>
        /// <remarks>
        ///     This asynchronous <c>Watch</c> variant has the following behaviors:
        ///
        ///     <list type="bullet">
        ///         <item>
        ///             <description>
        ///                 The asynchronous <paramref name="effect"/> is invoked as a "fire and forget" task.
        ///                 This means that <c>Watch</c> will not handle an exception propagated by the <see cref="Task"/>
        ///                 which is returned by <paramref name="effect"/>.
        ///                 Any unhandled exception thrown by the <paramref name="effect"/> function will therefore
        ///                 follow the .NET runtime's exception escalation policy. In most cases, this means that
        ///                 an unhandled exception will, at some point, raise the <see cref="TaskScheduler.UnobservedTaskException"/>
        ///                 event and then be swallowed.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 If the <paramref name="effect"/> function throws an exception before returning a
        ///                 <see cref="Task"/>, the effect exception behavior of the synchronous
        ///                 <see cref="Watch(Action, IDependency[])"/> overload applies.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <c>Watch</c> does not catch any exceptions raised by observables in the
        ///                 <paramref name="dependencies"/> array.
        ///                 When an <see cref="IDependency.Changed"/> observable errors, <c>Watch</c> stops watching
        ///                 the dependencies and re-throws the exception.
        ///                 If you specify an <see cref="IObservable{T}"/> as a dependency (for example via code like
        ///                 <c>myObservable.ToDependency()</c>), ensure that the observable either never errors
        ///                 or that you catch such errors before the conversion to a dependency (for example via code
        ///                 like <c>myObservable.Catch(ex => ...).ToDependency()</c>).
        ///                 Refs created by Composed's API never error and can safely be used as dependencies.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <c>Watch</c> stops watching dependencies whose <see cref="IDependency.Changed"/> observable
        ///                 completes.
        ///                 Refs created by Composed's API never complete and will continuously trigger the
        ///                 <paramref name="effect"/> when changed (unless the <c>Watch</c> function's subscription
        ///                 is disposed).
        ///             </description>
        ///         </item>
        ///     </list>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="effect"/>, <paramref name="dependencies"/> or one of the dependencies in
        ///     the <paramref name="dependencies"/> array is <see langword="null"/>.
        /// </exception>
        /// <example>
        ///     <para>
        ///         The following code demonstrates how the asynchronous <c>Watch</c> function reacts to dependency changes:
        ///
        ///         <code>
        ///         using System;
        ///         using Composed;
        ///         using static Composed.Compose;
        ///
        ///         IRef&lt;int&gt; dependency1 = Ref(0);
        ///         IRef&lt;int&gt; dependency2 = Ref(0);
        ///
        ///         Watch(
        ///             async () =>
        ///             {
        ///                 await Task.Delay(1000); // You can replace this with any other async invocation.
        ///                 Console.WriteLine($"Effect 1. dependency1: {dependency1.Value}");
        ///             },
        ///             dependency1
        ///         );
        ///
        ///         Watch(
        ///             async () =>
        ///             {
        ///                 await Task.Delay(1000); // You can replace this with any other async invocation.
        ///                 Console.WriteLine($"Effect 2. dependency2: {dependency2.Value}");
        ///             },
        ///             dependency2
        ///         );
        ///
        ///         Watch(
        ///             async () =>
        ///             {
        ///                 await Task.Delay(1000); // You can replace this with any other async invocation.
        ///                 Console.WriteLine($"Effect 3. dependency1: {dependency1.Value}, dependency2: {dependency2.Value}");
        ///             },
        ///             dependency1,
        ///             dependency2
        ///         );
        ///         
        ///         dependency1.Value = 1;
        ///         dependency2.Value = 1;
        ///
        ///         // Output:
        ///         // Effect 1. dependency1: 1
        ///         // Effect 3. dependency1: 1, dependency2: 0
        ///         // Effect 2. dependency2: 1
        ///         // Effect 3. dependency1: 1, dependency2: 1
        ///         </code>
        ///     </para>
        ///     <para>
        ///         The following code demonstrates how the asynchronous <c>Watch</c> function can be canceled:
        ///
        ///         <code>
        ///         using System;
        ///         using Composed;
        ///         using static Composed.Compose;
        ///
        ///         IRef&lt;int&gt; dependency = Ref(0);
        ///         IDisposable watchSubscription = Watch(
        ///             () =>
        ///             {
        ///                 await Task.Delay(1000); // You can replace this with any other async invocation.
        ///                 Console.WriteLine($"Effect ran. dependency: {dependency.Value}");
        ///             },
        ///             dependency
        ///         );
        ///
        ///         dependency.Value = 1;
        ///         watchSubscription.Dispose();
        ///         dependency.Value = 2;
        ///
        ///         // Output:
        ///         // Effect ran. dependency: 1
        ///         </code>
        ///     </para>
        /// </example>
        public static IDisposable Watch(Func<Task> effect, IScheduler? scheduler, params IObservable<Unit>[] dependencies)
        {
            _ = effect ?? throw new ArgumentNullException(nameof(effect));
            _ = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
            EnsureNoNullInDependencies(dependencies);
            return Watch(_ => effect(), scheduler, dependencies);
        }

        public static IDisposable Watch(Func<CancellationToken, Task> effect, params IObservable<Unit>[] dependencies) =>
            Watch(effect, scheduler: null, dependencies);

        /// <summary>
        ///     Watches each dependency specified in the <paramref name="dependencies"/> array for
        ///     changes and invokes the specified <paramref name="effect"/> whenever such a
        ///     change occurs.
        ///     In comparison to <see cref="WatchEffect(Func{CancellationToken, Task}, IObservable{Unit}[])"/>, the effect does
        ///     not get invoked before one of the dependencies changes for the first time.
        ///     This overload provides a <see cref="CancellationToken"/> which notifies the <paramref name="effect"/>
        ///     when the subscription is canceled. 
        /// </summary>
        /// <param name="effect">
        ///     <para>
        ///         An asynchronous function to be invoked whenever one of the specified dependencies changes.
        ///     </para>
        ///     <para>
        ///         This function receives a <see cref="CancellationToken"/> which notifies the effect about a
        ///         <c>Watch</c> cancellation.
        ///         Effects can use this <see cref="CancellationToken"/> to stop or prevent ongoing asynchronous
        ///         operations when <c>Watch</c> stops watching the specified <paramref name="dependencies"/>.
        ///     </para>
        /// </param>
        /// <param name="dependencies">
        ///     <para>
        ///         A set of dependencies which will be watched for changes.
        ///     </para>
        ///     <para>
        ///         This can be any object implementing the <see cref="IObservable{Unit}"/> interface.
        ///         Refs implement this interface and can directly be passed.
        ///         Composed also provides a way to convert any <see cref="IObservable{T}"/> to an
        ///         <see cref="IDependency"/> via the
        ///         <see cref="ObservableExtensions.ToDependency{T}(IObservable{T})"/> function.
        ///     </para>
        ///     <para>
        ///         If this is empty, the <paramref name="effect"/> will never be invoked.
        ///     </para>
        /// </param>
        /// <returns>
        ///     An <see cref="IDisposable"/> instance which, when disposed, ensures that the dependencies
        ///     are no longer being watched.
        ///     This can be used to cancel watching at any time and to free any references to the dependencies
        ///     created by the <c>Watch</c> function.
        /// </returns>
        /// <remarks>
        ///     This asynchronous <c>Watch</c> variant has the following behaviors:
        ///
        ///     <list type="bullet">
        ///         <item>
        ///             <description>
        ///                 The asynchronous <paramref name="effect"/> is invoked as a "fire and forget" task.
        ///                 This means that <c>Watch</c> will not handle an exception propagated by the <see cref="Task"/>
        ///                 which is returned by <paramref name="effect"/>.
        ///                 Any unhandled exception thrown by the <paramref name="effect"/> function will therefore
        ///                 follow the .NET runtime's exception escalation policy. In most cases, this means that
        ///                 an unhandled exception will, at some point, raise the <see cref="TaskScheduler.UnobservedTaskException"/>
        ///                 event and then be swallowed.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 If the <paramref name="effect"/> function throws an exception before returning a
        ///                 <see cref="Task"/>, the effect exception behavior of the synchronous
        ///                 <see cref="Watch(Action, IDependency[])"/> overload applies.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <c>Watch</c> does not catch any exceptions raised by observables in the
        ///                 <paramref name="dependencies"/> array.
        ///                 When an <see cref="IDependency.Changed"/> observable errors, <c>Watch</c> stops watching
        ///                 the dependencies and re-throws the exception.
        ///                 If you specify an <see cref="IObservable{T}"/> as a dependency (for example via code like
        ///                 <c>myObservable.ToDependency()</c>), ensure that the observable either never errors
        ///                 or that you catch such errors before the conversion to a dependency (for example via code
        ///                 like <c>myObservable.Catch(ex => ...).ToDependency()</c>).
        ///                 Refs created by Composed's API never error and can safely be used as dependencies.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <c>Watch</c> stops watching dependencies whose <see cref="IDependency.Changed"/> observable
        ///                 completes.
        ///                 Refs created by Composed's API never complete and will continuously trigger the
        ///                 <paramref name="effect"/> when changed (unless the <c>Watch</c> function's subscription
        ///                 is disposed).
        ///             </description>
        ///         </item>
        ///     </list>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="effect"/>, <paramref name="dependencies"/> or one of the dependencies in
        ///     the <paramref name="dependencies"/> array is <see langword="null"/>.
        /// </exception>
        /// <example>
        ///     <para>
        ///         The following code demonstrates how the asynchronous <c>Watch</c> function reacts to dependency changes:
        ///
        ///         <code>
        ///         using System;
        ///         using Composed;
        ///         using static Composed.Compose;
        ///
        ///         IRef&lt;int&gt; dependency1 = Ref(0);
        ///         IRef&lt;int&gt; dependency2 = Ref(0);
        ///
        ///         Watch(
        ///             async cancellationToken =>
        ///             {
        ///                 await Task.Delay(1000); // You can replace this with any other async invocation.
        ///                 Console.WriteLine($"Effect 1. dependency1: {dependency1.Value}");
        ///             },
        ///             dependency1
        ///         );
        ///
        ///         Watch(
        ///             async cancellationToken =>
        ///             {
        ///                 await Task.Delay(1000); // You can replace this with any other async invocation.
        ///                 Console.WriteLine($"Effect 2. dependency2: {dependency2.Value}");
        ///             },
        ///             dependency2
        ///         );
        ///
        ///         Watch(
        ///             async cancellationToken =>
        ///             {
        ///                 await Task.Delay(1000); // You can replace this with any other async invocation.
        ///                 Console.WriteLine($"Effect 3. dependency1: {dependency1.Value}, dependency2: {dependency2.Value}");
        ///             },
        ///             dependency1,
        ///             dependency2
        ///         );
        ///         
        ///         dependency1.Value = 1;
        ///         dependency2.Value = 1;
        ///
        ///         // Output:
        ///         // Effect 1. dependency1: 1
        ///         // Effect 3. dependency1: 1, dependency2: 0
        ///         // Effect 2. dependency2: 1
        ///         // Effect 3. dependency1: 1, dependency2: 1
        ///         </code>
        ///     </para>
        ///     <para>
        ///         The following code demonstrates how the asynchronous <c>Watch</c> function can be canceled:
        ///
        ///         <code>
        ///         using System;
        ///         using Composed;
        ///         using static Composed.Compose;
        ///
        ///         IRef&lt;int&gt; dependency = Ref(0);
        ///         IDisposable watchSubscription = Watch(
        ///             cancellationToken =>
        ///             {
        ///                 Console.WriteLine($"Effect ran. dependency: {dependency.Value}");
        ///                 
        ///                 await Task.Delay(1000);
        ///
        ///                 // After waiting for the above time, `watchSubscription.Dispose()` should have been
        ///                 // called. The cancellationToken should therefore notify us that the effect should
        ///                 // also cancel ASAP.
        ///                 Console.WriteLine($"Should effect {dependency.Value} be canceled? {cancellationToken.IsCancellationRequested}");
        ///             },
        ///             dependency
        ///         );
        ///
        ///         dependency.Value = 1;
        ///         watchSubscription.Dispose();
        ///         dependency.Value = 2;
        ///
        ///         // Output:
        ///         // Effect ran. dependency: 1
        ///         // Should effect 1 be canceled? True
        ///         </code>
        ///     </para>
        /// </example>
        public static IDisposable Watch(
            Func<CancellationToken, Task> effect,
            IScheduler? scheduler,
            params IObservable<Unit>[] dependencies
        )
        {
            _ = effect ?? throw new ArgumentNullException(nameof(effect));
            _ = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
            EnsureNoNullInDependencies(dependencies);
            return AsyncWatchImpl(effect, runEffectNow: false, scheduler, dependencies);
        }

        public static IDisposable WatchEffect(Action effect, params IObservable<Unit>[] dependencies) =>
            WatchEffect(effect, scheduler: null, dependencies);

        /// <summary>
        ///     Immediately invokes the specified <paramref name="effect"/> once.
        ///     Afterwards, watches each dependency specified in the <paramref name="dependencies"/>
        ///     array for changes and invokes the specified <paramref name="effect"/> whenever such a
        ///     change occurs.
        /// </summary>
        /// <param name="effect">
        ///     A function to be invoked immediately as well as whenever one of the specified
        ///     dependencies changes.
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
        ///         <see cref="ObservableExtensions.ToDependency{T}(IObservable{T})"/> function.
        ///     </para>
        ///     <para>
        ///         If this is empty, the <paramref name="effect"/> will never be invoked.
        ///     </para>
        /// </param>
        /// <returns>
        ///     An <see cref="IDisposable"/> instance which, when disposed, ensures that the dependencies
        ///     are no longer being watched.
        ///     This can be used to cancel watching at any time and to free any references to the dependencies
        ///     created by the <c>WatchEffect</c> function.
        /// </returns>
        /// <remarks>
        ///     This synchronous <c>WatchEffect</c> variant has the following behaviors:
        ///
        ///     <list type="bullet">
        ///         <item>
        ///             <description>
        ///                 <c>WatchEffect</c> does not catch any exception thrown by the <paramref name="effect"/> function.
        ///                 If <paramref name="effect"/> throws an exception, <c>WatchEffect</c> stops watching the
        ///                 dependencies and re-throws the exception.
        ///                 this is also the case for the initial <paramref name="effect"/> invocation.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <c>WatchEffect</c> does not catch any exceptions raised by observables in the
        ///                 <paramref name="dependencies"/> array.
        ///                 When an <see cref="IDependency.Changed"/> observable errors, <c>WatchEffect</c> stops watching
        ///                 the dependencies and re-throws the exception.
        ///                 If you specify an <see cref="IObservable{T}"/> as a dependency (for example via code like
        ///                 <c>myObservable.ToDependency()</c>), ensure that the observable either never errors
        ///                 or that you catch such errors before the conversion to a dependency (for example via code
        ///                 like <c>myObservable.Catch(ex => ...).ToDependency()</c>).
        ///                 Refs created by Composed's API never error and can safely be used as dependencies.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <c>WatchEffect</c> stops watching dependencies whose <see cref="IDependency.Changed"/> observable
        ///                 completes.
        ///                 Refs created by Composed's API never complete and will continuously trigger the
        ///                 <paramref name="effect"/> when changed (unless the <c>WatchEffect</c> function's subscription
        ///                 is disposed).
        ///             </description>
        ///         </item>
        ///     </list>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="effect"/>, <paramref name="dependencies"/> or one of the dependencies in
        ///     the <paramref name="dependencies"/> array is <see langword="null"/>.
        /// </exception>
        /// <example>
        ///     <para>
        ///         The following code demonstrates how the <c>WatchEffect</c> function both immediately
        ///         invokes the effect and reacts to dependency changes:
        ///
        ///         <code>
        ///         using System;
        ///         using Composed;
        ///         using static Composed.Compose;
        ///
        ///         IRef&lt;int&gt; dependency1 = Ref(0);
        ///         IRef&lt;int&gt; dependency2 = Ref(0);
        ///
        ///         WatchEffect(() => Console.WriteLine($"Effect 1. dependency1: {dependency1.Value}"), dependency1);
        ///         WatchEffect(() => Console.WriteLine($"Effect 2. dependency2: {dependency2.Value}"), dependency2);
        ///         WatchEffect(
        ///             () => Console.WriteLine($"Effect 3. dependency1: {dependency1.Value}, dependency2: {dependency2.Value}"),
        ///             dependency1,
        ///             dependency2
        ///         );
        ///
        ///         dependency1.Value = 1;
        ///         dependency2.Value = 1;
        ///
        ///         // Output:
        ///         // Effect 1. dependency1: 0
        ///         // Effect 2. dependency2: 0
        ///         // Effect 3. dependency1: 0, dependency2: 0
        ///         // Effect 1. dependency1: 1
        ///         // Effect 3. dependency1: 1, dependency2: 0
        ///         // Effect 2. dependency2: 1
        ///         // Effect 3. dependency1: 1, dependency2: 1
        ///         </code>
        ///     </para>
        ///     <para>
        ///         The following code demonstrates how the <c>WatchEffect</c> function can be canceled:
        ///
        ///         <code>
        ///         using System;
        ///         using Composed;
        ///         using static Composed.Compose;
        ///
        ///         IRef&lt;int&gt; dependency = Ref(0);
        ///         IDisposable watchSubscription = Watch(() => Console.WriteLine($"Effect ran. dependency: {dependency.Value}"), dependency);
        ///
        ///         dependency.Value = 1;
        ///         watchSubscription.Dispose();
        ///         dependency.Value = 2;
        ///
        ///         // Output:
        ///         // Effect ran. dependency: 0
        ///         // Effect ran. dependency: 1
        ///         </code>
        ///     </para>
        /// </example>
        public static IDisposable WatchEffect(Action effect, IScheduler? scheduler, params IObservable<Unit>[] dependencies)
        {
            _ = effect ?? throw new ArgumentNullException(nameof(effect));
            _ = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
            EnsureNoNullInDependencies(dependencies);
            return SyncWatchImpl(effect, runEffectNow: true, scheduler, dependencies);
        }

        public static IDisposable WatchEffect(Func<Task> effect, params IObservable<Unit>[] dependencies) =>
            WatchEffect(effect, scheduler: null, dependencies);

        /// <summary>
        ///     Immediately invokes the specified <paramref name="effect"/> once.
        ///     Afterwards, watches each dependency specified in the <paramref name="dependencies"/>
        ///     array for changes and invokes the specified <paramref name="effect"/> whenever such a
        ///     change occurs.
        /// </summary>
        /// <param name="effect">
        ///     An asynchronous function to be invoked immediately as well as whenever one of the specified
        ///     dependencies changes.
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
        ///         <see cref="ObservableExtensions.ToDependency{T}(IObservable{T})"/> function.
        ///     </para>
        ///     <para>
        ///         If this is empty, the <paramref name="effect"/> will never be invoked.
        ///     </para>
        /// </param>
        /// <returns>
        ///     An <see cref="IDisposable"/> instance which, when disposed, ensures that the dependencies
        ///     are no longer being watched.
        ///     This can be used to cancel watching at any time and to free any references to the dependencies
        ///     created by the <c>WatchEffect</c> function.
        /// </returns>
        /// <remarks>
        ///     This asynchronous <c>WatchEffect</c> variant has the following behaviors:
        ///
        ///     <list type="bullet">
        ///         <item>
        ///             <description>
        ///                 The asynchronous <paramref name="effect"/> is invoked as a "fire and forget" task.
        ///                 This means that <c>Watch</c> will not handle an exception propagated by the <see cref="Task"/>
        ///                 which is returned by <paramref name="effect"/>.
        ///                 Any unhandled exception thrown by the <paramref name="effect"/> function will therefore
        ///                 follow the .NET runtime's exception escalation policy. In most cases, this means that
        ///                 an unhandled exception will, at some point, raise the <see cref="TaskScheduler.UnobservedTaskException"/>
        ///                 event and then be swallowed.
        ///                 this is also the case for the initial <paramref name="effect"/> invocation.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 If the <paramref name="effect"/> function throws an exception before returning a
        ///                 <see cref="Task"/>, the effect exception behavior of the synchronous
        ///                 <see cref="WatchEffect(Action, IDependency[])"/> overload applies.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <c>WatchEffect</c> does not catch any exceptions raised by observables in the
        ///                 <paramref name="dependencies"/> array.
        ///                 When an <see cref="IDependency.Changed"/> observable errors, <c>WatchEffect</c> stops watching
        ///                 the dependencies and re-throws the exception.
        ///                 If you specify an <see cref="IObservable{T}"/> as a dependency (for example via code like
        ///                 <c>myObservable.ToDependency()</c>), ensure that the observable either never errors
        ///                 or that you catch such errors before the conversion to a dependency (for example via code
        ///                 like <c>myObservable.Catch(ex => ...).ToDependency()</c>).
        ///                 Refs created by Composed's API never error and can safely be used as dependencies.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <c>WatchEffect</c> stops watching dependencies whose <see cref="IDependency.Changed"/> observable
        ///                 completes.
        ///                 Refs created by Composed's API never complete and will continuously trigger the
        ///                 <paramref name="effect"/> when changed (unless the <c>WatchEffect</c> function's subscription
        ///                 is disposed).
        ///             </description>
        ///         </item>
        ///     </list>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="effect"/>, <paramref name="dependencies"/> or one of the dependencies in
        ///     the <paramref name="dependencies"/> array is <see langword="null"/>.
        /// </exception>
        /// <example>
        ///     <para>
        ///         The following code demonstrates how the asynchronous <c>WatchEffect</c> function both immediately
        ///         invokes the effect and reacts to dependency changes:
        ///
        ///         <code>
        ///         using System;
        ///         using Composed;
        ///         using static Composed.Compose;
        ///
        ///         IRef&lt;int&gt; dependency1 = Ref(0);
        ///         IRef&lt;int&gt; dependency2 = Ref(0);
        ///
        ///         WatchEffect(
        ///             async () =>
        ///             {
        ///                 await Task.Delay(1000); // You can replace this with any other async invocation.
        ///                 Console.WriteLine($"Effect 1. dependency1: {dependency1.Value}");
        ///             },
        ///             dependency1
        ///         );
        ///         
        ///         WatchEffect(
        ///             async () =>
        ///             {
        ///                 await Task.Delay(1000); // You can replace this with any other async invocation.
        ///                 Console.WriteLine($"Effect 2. dependency2: {dependency2.Value}");
        ///             },
        ///             dependency2
        ///         );
        ///         
        ///         WatchEffect(
        ///             async () =>
        ///             {
        ///                 await Task.Delay(1000); // You can replace this with any other async invocation.
        ///                 Console.WriteLine($"Effect 3. dependency1: {dependency1.Value}, dependency2: {dependency2.Value}");
        ///             },
        ///             dependency1,
        ///             dependency2
        ///         );
        ///
        ///         dependency1.Value = 1;
        ///         dependency2.Value = 1;
        ///
        ///         // Output:
        ///         // Effect 1. dependency1: 0
        ///         // Effect 2. dependency2: 0
        ///         // Effect 3. dependency1: 0, dependency2: 0
        ///         // Effect 1. dependency1: 1
        ///         // Effect 3. dependency1: 1, dependency2: 0
        ///         // Effect 2. dependency2: 1
        ///         // Effect 3. dependency1: 1, dependency2: 1
        ///         </code>
        ///     </para>
        ///     <para>
        ///         The following code demonstrates how the asynchronous <c>WatchEffect</c> function can be canceled:
        ///
        ///         <code>
        ///         using System;
        ///         using Composed;
        ///         using static Composed.Compose;
        ///
        ///         IRef&lt;int&gt; dependency = Ref(0);
        ///         IDisposable watchSubscription = Watch(
        ///             async () =>
        ///             {
        ///                 await Task.Delay(1000); // You can replace this with any other async invocation.
        ///                 Console.WriteLine($"Effect ran. dependency: {dependency.Value}");
        ///             },
        ///             dependency
        ///         );
        ///
        ///         dependency.Value = 1;
        ///         watchSubscription.Dispose();
        ///         dependency.Value = 2;
        ///
        ///         // Output:
        ///         // Effect ran. dependency: 0
        ///         // Effect ran. dependency: 1
        ///         </code>
        ///     </para>
        /// </example>
        public static IDisposable WatchEffect(Func<Task> effect, IScheduler? scheduler, params IObservable<Unit>[] dependencies)
        {
            _ = effect ?? throw new ArgumentNullException(nameof(effect));
            _ = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
            EnsureNoNullInDependencies(dependencies);
            return WatchEffect(_ => effect(), scheduler, dependencies);
        }

        public static IDisposable WatchEffect(Func<CancellationToken, Task> effect, params IObservable<Unit>[] dependencies) =>
            WatchEffect(effect, scheduler: null, dependencies);

        /// <summary>
        ///     Immediately invokes the specified <paramref name="effect"/> once.
        ///     Afterwards, watches each dependency specified in the <paramref name="dependencies"/>
        ///     array for changes and invokes the specified <paramref name="effect"/> whenever such a
        ///     change occurs.
        ///     This overload provides a <see cref="CancellationToken"/> which notifies the <paramref name="effect"/>
        ///     when the subscription is canceled. 
        /// </summary>
        /// <param name="effect">
        ///     <para>
        ///         An asynchronous function to be invoked immediately as well as whenever one of the specified
        ///         dependencies changes.
        ///     </para>
        ///     <para>
        ///         This function receives a <see cref="CancellationToken"/> which notifies the effect about a
        ///         <c>WatchEffect</c> cancellation.
        ///         Effects can use this <see cref="CancellationToken"/> to stop or prevent ongoing asynchronous
        ///         operations when <c>WatchEffect</c> stops watching the specified <paramref name="dependencies"/>.
        ///     </para>
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
        ///         <see cref="ObservableExtensions.ToDependency{T}(IObservable{T})"/> function.
        ///     </para>
        ///     <para>
        ///         If this is empty, the <paramref name="effect"/> will never be invoked.
        ///     </para>
        /// </param>
        /// <returns>
        ///     An <see cref="IDisposable"/> instance which, when disposed, ensures that the dependencies
        ///     are no longer being watched.
        ///     This can be used to cancel watching at any time and to free any references to the dependencies
        ///     created by the <c>WatchEffect</c> function.
        /// </returns>
        /// <remarks>
        ///     This asynchronous <c>WatchEffect</c> variant has the following behaviors:
        ///
        ///     <list type="bullet">
        ///         <item>
        ///             <description>
        ///                 The asynchronous <paramref name="effect"/> is invoked as a "fire and forget" task.
        ///                 This means that <c>Watch</c> will not handle an exception propagated by the <see cref="Task"/>
        ///                 which is returned by <paramref name="effect"/>.
        ///                 Any unhandled exception thrown by the <paramref name="effect"/> function will therefore
        ///                 follow the .NET runtime's exception escalation policy. In most cases, this means that
        ///                 an unhandled exception will, at some point, raise the <see cref="TaskScheduler.UnobservedTaskException"/>
        ///                 event and then be swallowed.
        ///                 this is also the case for the initial <paramref name="effect"/> invocation.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 If the <paramref name="effect"/> function throws an exception before returning a
        ///                 <see cref="Task"/>, the effect exception behavior of the synchronous
        ///                 <see cref="WatchEffect(Action, IDependency[])"/> overload applies.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <c>WatchEffect</c> does not catch any exceptions raised by observables in the
        ///                 <paramref name="dependencies"/> array.
        ///                 When an <see cref="IDependency.Changed"/> observable errors, <c>WatchEffect</c> stops watching
        ///                 the dependencies and re-throws the exception.
        ///                 If you specify an <see cref="IObservable{T}"/> as a dependency (for example via code like
        ///                 <c>myObservable.ToDependency()</c>), ensure that the observable either never errors
        ///                 or that you catch such errors before the conversion to a dependency (for example via code
        ///                 like <c>myObservable.Catch(ex => ...).ToDependency()</c>).
        ///                 Refs created by Composed's API never error and can safely be used as dependencies.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <c>WatchEffect</c> stops watching dependencies whose <see cref="IDependency.Changed"/> observable
        ///                 completes.
        ///                 Refs created by Composed's API never complete and will continuously trigger the
        ///                 <paramref name="effect"/> when changed (unless the <c>WatchEffect</c> function's subscription
        ///                 is disposed).
        ///             </description>
        ///         </item>
        ///     </list>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="effect"/>, <paramref name="dependencies"/> or one of the dependencies in
        ///     the <paramref name="dependencies"/> array is <see langword="null"/>.
        /// </exception>
        /// <example>
        ///     <para>
        ///         The following code demonstrates how the asynchronous <c>WatchEffect</c> function both immediately
        ///         invokes the effect and reacts to dependency changes:
        ///
        ///         <code>
        ///         using System;
        ///         using Composed;
        ///         using static Composed.Compose;
        ///
        ///         IRef&lt;int&gt; dependency1 = Ref(0);
        ///         IRef&lt;int&gt; dependency2 = Ref(0);
        ///
        ///         WatchEffect(
        ///             async cancellationToken =>
        ///             {
        ///                 await Task.Delay(1000); // You can replace this with any other async invocation.
        ///                 Console.WriteLine($"Effect 1. dependency1: {dependency1.Value}");
        ///             },
        ///             dependency1
        ///         );
        ///         
        ///         WatchEffect(
        ///             async cancellationToken =>
        ///             {
        ///                 await Task.Delay(1000); // You can replace this with any other async invocation.
        ///                 Console.WriteLine($"Effect 2. dependency2: {dependency2.Value}");
        ///             },
        ///             dependency2
        ///         );
        ///         
        ///         WatchEffect(
        ///             async cancellationToken =>
        ///             {
        ///                 await Task.Delay(1000); // You can replace this with any other async invocation.
        ///                 Console.WriteLine($"Effect 3. dependency1: {dependency1.Value}, dependency2: {dependency2.Value}");
        ///             },
        ///             dependency1,
        ///             dependency2
        ///         );
        ///
        ///         dependency1.Value = 1;
        ///         dependency2.Value = 1;
        ///
        ///         // Output:
        ///         // Effect 1. dependency1: 0
        ///         // Effect 2. dependency2: 0
        ///         // Effect 3. dependency1: 0, dependency2: 0
        ///         // Effect 1. dependency1: 1
        ///         // Effect 3. dependency1: 1, dependency2: 0
        ///         // Effect 2. dependency2: 1
        ///         // Effect 3. dependency1: 1, dependency2: 1
        ///         </code>
        ///     </para>
        ///     <para>
        ///         The following code demonstrates how the asynchronous <c>WatchEffect</c> function can be canceled:
        ///
        ///         <code>
        ///         using System;
        ///         using Composed;
        ///         using static Composed.Compose;
        ///
        ///         IRef&lt;int&gt; dependency = Ref(0);
        ///         IDisposable watchSubscription = Watch(
        ///             async cancellationToken =>
        ///             {
        ///                 Console.WriteLine($"Effect ran. dependency: {dependency.Value}");
        ///                 
        ///                 await Task.Delay(1000);
        ///
        ///                 // After waiting for the above time, `watchSubscription.Dispose()` should have been
        ///                 // called. The cancellationToken should therefore notify us that the effect should
        ///                 // also cancel ASAP.
        ///                 Console.WriteLine($"Should effect {dependency.Value} be canceled? {cancellationToken.IsCancellationRequested}");
        ///             },
        ///             dependency
        ///         );
        ///
        ///         dependency.Value = 1;
        ///         watchSubscription.Dispose();
        ///         dependency.Value = 2;
        ///
        ///         // Output:
        ///         // Effect ran. dependency: 0
        ///         // Effect ran. dependency: 1
        ///         // Should effect 0 be canceled? True
        ///         // Should effect 1 be canceled? True
        ///         </code>
        ///     </para>
        /// </example>
        public static IDisposable WatchEffect(
            Func<CancellationToken, Task> effect,
            IScheduler? scheduler,
            params IObservable<Unit>[] dependencies
        )
        {
            _ = effect ?? throw new ArgumentNullException(nameof(effect));
            _ = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
            EnsureNoNullInDependencies(dependencies);
            return AsyncWatchImpl(effect, runEffectNow: true, scheduler, dependencies);
        }

        private static IReadOnlyRef<TResult> ComputedImpl<TResult>(
            Func<TResult> compute,
            IEqualityComparer<TResult>? equalityComparer,
            IScheduler? scheduler,
            params IObservable<Unit>[] dependencies
        )
        {
            var @ref = Ref(compute(), equalityComparer);
            SyncWatchImpl(() => @ref.Value = compute(), runEffectNow: false, scheduler, dependencies);
            return @ref;
        }

        private static IDisposable SyncWatchImpl(
            Action effect,
            bool runEffectNow,
            IScheduler? scheduler,
            params IObservable<Unit>[] dependencies
        )
        {
            return GetDependenciesObservable(runEffectNow, scheduler, dependencies)
                .Subscribe(_ => effect());
        }

        private static IDisposable AsyncWatchImpl(
            Func<CancellationToken, Task> effect,
            bool runEffectNow,
            IScheduler? scheduler,
            params IObservable<Unit>[] dependencies
        )
        {
            return GetDependenciesObservable(runEffectNow, scheduler, dependencies)
                .SelectMany(async (_, cancellationToken) =>
                {
                    await effect(cancellationToken).ConfigureAwait(false);
                    return Unit.Default;
                })
                .Subscribe();
        }

        private static IObservable<Unit> GetDependenciesObservable(
            bool runEffectNow,
            IScheduler? scheduler,
            IEnumerable<IObservable<Unit>> dependencies
        )
        {
            var observable = runEffectNow
                ? dependencies.Prepend(RunEffectImmediatelyObservable).Merge()
                : dependencies.Merge();

            if (scheduler is not null)
            {
                observable = observable.ObserveOn(scheduler);
            }

            return observable;
        }

        private static void EnsureNoNullInDependencies(IObservable<Unit>[] dependencies)
        {
            foreach (var dependency in dependencies)
            {
                if (dependency is null)
                {
                    throw new ArgumentNullException(
                        nameof(dependencies),
                        "At least one of the specified dependencies is null. " +
                        "Ensure that explicitly specified dependency lists do not contain null values."
                    );
                }
            }
        }
    }
}
