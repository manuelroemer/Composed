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
        private static readonly IObservable<Unit> RunEffectImmediatelyObservable =
            Observable.Return(Unit.Default, ImmediateScheduler.Instance);

        /// <summary>
        ///     <para>
        ///         Creates and returns a new mutable <see cref="IRef{T}"/> instance which holds the
        ///         specified <paramref name="initialValue"/>.
        ///     </para>
        ///     <para>
        ///         When the ref's value is set, it will use <see cref="EqualityComparer{T}.Default"/>
        ///         to determine whether its value effectively changes and whether observers
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
        /// <remarks>
        ///     <b>Thread Safety:</b><br/>
        ///     Refs returned by this function synchronize while comparing and setting new values.
        ///     They <i>do not</i> synchronize during any subsequent change notifications.
        /// </remarks>
        public static IRef<T> Ref<T>(T initialValue) =>
            Ref(initialValue, equalityComparer: null);

        /// <summary>
        ///     <para>
        ///         Creates and returns a new mutable <see cref="IRef{T}"/> instance which holds the
        ///         specified <paramref name="initialValue"/>.
        ///     </para>
        ///     <para>
        ///         This overload allows you to specify an <see cref="IEqualityComparer{T}"/> which is used
        ///         by the ref to determine whether its value effectively changes and whether
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
        /// <remarks>
        ///     <b>Thread Safety:</b><br/>
        ///     Refs returned by this function synchronize while comparing and setting new values.
        ///     They <i>do not</i> synchronize during any subsequent change notifications.
        /// </remarks>
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
        ///         to determine whether its value effectively changes and whether observers
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
        ///         This can be any kind of observable. <see cref="IRef{T}"/> and <see cref="IReadOnlyRef{T}"/> instances
        ///         implement <see cref="IObservable{T}"/> and can directly be passed as dependencies.
        ///     </para>
        ///     <para>
        ///         If this is empty, the ref's value will never be recomputed.
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
        ///         to determine whether its value effectively changes and whether observers
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
        ///         An <see cref="IScheduler"/> on which the <paramref name="compute"/> invocations and
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
        ///         This can be any kind of observable. <see cref="IRef{T}"/> and <see cref="IReadOnlyRef{T}"/> instances
        ///         implement <see cref="IObservable{T}"/> and can directly be passed as dependencies.
        ///     </para>
        ///     <para>
        ///         If this is empty, the ref's value will never be recomputed.
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
        ///         by the ref to determine whether its value effectively changes and whether
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
        ///         This can be any kind of observable. <see cref="IRef{T}"/> and <see cref="IReadOnlyRef{T}"/> instances
        ///         implement <see cref="IObservable{T}"/> and can directly be passed as dependencies.
        ///     </para>
        ///     <para>
        ///         If this is empty, the ref's value will never be recomputed.
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
        ///         by the ref to determine whether its value effectively changes and whether
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
        ///         An <see cref="IScheduler"/> on which the <paramref name="compute"/> invocations and
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
        ///         This can be any kind of observable. <see cref="IRef{T}"/> and <see cref="IReadOnlyRef{T}"/> instances
        ///         implement <see cref="IObservable{T}"/> and can directly be passed as dependencies.
        ///     </para>
        ///     <para>
        ///         If this is empty, the ref's value will never be recomputed.
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

        /// <summary>
        ///     <para>
        ///         Watches each dependency specified in the <paramref name="dependencies"/> array
        ///         and invokes the specified <paramref name="effect"/> whenever one of the dependencies changes.
        ///     </para>
        ///     <para>
        ///         Scheduling of <paramref name="effect"/> invocations depends on the observables
        ///         passed as <paramref name="dependencies"/>.
        ///     </para>
        /// </summary>
        /// <param name="effect">
        ///     The function to be invoked whenever one of the <paramref name="dependencies"/> changes.
        /// </param>
        /// <param name="dependencies">
        ///     <para>
        ///         A set of dependencies which will be watched for changes.
        ///         This can be any kind of observable. <see cref="IRef{T}"/> and <see cref="IReadOnlyRef{T}"/> instances
        ///         implement <see cref="IObservable{T}"/> and can directly be passed as dependencies.
        ///     </para>
        ///     <para>
        ///         If this is empty, the <paramref name="effect"/> will never be invoked.
        ///     </para>
        /// </param>
        /// <returns>
        ///     A disposable which, when disposed, cancels the watch subscription.
        ///     Once canceled, the <paramref name="effect"/> function will not be invoked again
        ///     when one of the <paramref name="dependencies"/> changes.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="effect"/>, <paramref name="dependencies"/> or one of the dependencies in
        ///     the <paramref name="dependencies"/> array is <see langword="null"/>.
        /// </exception>
        public static IDisposable Watch(Action effect, params IObservable<Unit>[] dependencies) =>
            Watch(effect, scheduler: null, dependencies);

        /// <summary>
        ///     <para>
        ///         Watches each dependency specified in the <paramref name="dependencies"/> array
        ///         and invokes the specified <paramref name="effect"/> whenever one of the dependencies changes.
        ///     </para>
        ///     <para>
        ///         This overload allows you to specify an  <see cref="IScheduler"/>
        ///         which is used for scheduling <paramref name="effect"/> invocations.
        ///     </para>
        /// </summary>
        /// <param name="effect">
        ///     The function to be invoked whenever one of the <paramref name="dependencies"/> changes.
        /// </param>
        /// <param name="scheduler">
        ///     <para>
        ///         An <see cref="IScheduler"/> on which the <paramref name="effect"/> invocations are scheduled.
        ///     </para>
        ///     <para>
        ///         If this is <see langword="null"/>, scheduling of <paramref name="effect"/> invocations
        ///         depends on the observables passed as <paramref name="dependencies"/>.
        ///     </para>
        /// </param>
        /// <param name="dependencies">
        ///     <para>
        ///         A set of dependencies which will be watched for changes.
        ///         This can be any kind of observable. <see cref="IRef{T}"/> and <see cref="IReadOnlyRef{T}"/> instances
        ///         implement <see cref="IObservable{T}"/> and can directly be passed as dependencies.
        ///     </para>
        ///     <para>
        ///         If this is empty, the <paramref name="effect"/> will never be invoked.
        ///     </para>
        /// </param>
        /// <returns>
        ///     A disposable which, when disposed, cancels the watch subscription.
        ///     Once canceled, the <paramref name="effect"/> function will not be invoked again
        ///     when one of the <paramref name="dependencies"/> changes.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="effect"/>, <paramref name="dependencies"/> or one of the dependencies in
        ///     the <paramref name="dependencies"/> array is <see langword="null"/>.
        /// </exception>
        public static IDisposable Watch(Action effect, IScheduler? scheduler, params IObservable<Unit>[] dependencies)
        {
            _ = effect ?? throw new ArgumentNullException(nameof(effect));
            _ = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
            EnsureNoNullInDependencies(dependencies);
            return SyncWatchImpl(effect, runEffectNow: false, scheduler, dependencies);
        }

        /// <inheritdoc cref="Watch(Func{CancellationToken, Task}, IObservable{Unit}[])"/>
        public static IDisposable Watch(Func<Task> effect, params IObservable<Unit>[] dependencies) =>
            Watch(effect, scheduler: null, dependencies);

        /// <inheritdoc cref="Watch(Func{CancellationToken, Task}, IScheduler?, IObservable{Unit}[])"/>
        public static IDisposable Watch(Func<Task> effect, IScheduler? scheduler, params IObservable<Unit>[] dependencies)
        {
            _ = effect ?? throw new ArgumentNullException(nameof(effect));
            _ = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
            EnsureNoNullInDependencies(dependencies);
            return Watch(_ => effect(), scheduler, dependencies);
        }

        /// <inheritdoc cref="Watch(Action, IObservable{Unit}[])"/>
        public static IDisposable Watch(Func<CancellationToken, Task> effect, params IObservable<Unit>[] dependencies) =>
            Watch(effect, scheduler: null, dependencies);

        /// <inheritdoc cref="Watch(Action, IScheduler?, IObservable{Unit}[])"/>
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

        /// <summary>
        ///     <para>
        ///         Immediately invokes the specified <paramref name="effect"/> function once.
        ///         Afterwards watches each dependency specified in the <paramref name="dependencies"/> array
        ///         and invokes the specified <paramref name="effect"/> whenever one of the dependencies changes.
        ///     </para>
        ///     <para>
        ///         Scheduling of <paramref name="effect"/> invocations depends on the observables
        ///         passed as <paramref name="dependencies"/>.
        ///     </para>
        /// </summary>
        /// <param name="effect">
        ///     The function to be invoked immediately and whenever one of the <paramref name="dependencies"/> changes.
        /// </param>
        /// <param name="dependencies">
        ///     <para>
        ///         A set of dependencies which will be watched for changes.
        ///         This can be any kind of observable. <see cref="IRef{T}"/> and <see cref="IReadOnlyRef{T}"/> instances
        ///         implement <see cref="IObservable{T}"/> and can directly be passed as dependencies.
        ///     </para>
        ///     <para>
        ///         If this is empty, the <paramref name="effect"/> will never be invoked.
        ///     </para>
        /// </param>
        /// <returns>
        ///     A disposable which, when disposed, cancels the watch subscription.
        ///     Once canceled, the <paramref name="effect"/> function will not be invoked again
        ///     when one of the <paramref name="dependencies"/> changes.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="effect"/>, <paramref name="dependencies"/> or one of the dependencies in
        ///     the <paramref name="dependencies"/> array is <see langword="null"/>.
        /// </exception>
        public static IDisposable WatchEffect(Action effect, params IObservable<Unit>[] dependencies) =>
            WatchEffect(effect, scheduler: null, dependencies);

        /// <summary>
        ///     <para>
        ///         Immediately invokes the specified <paramref name="effect"/> function once.
        ///         Afterwards watches each dependency specified in the <paramref name="dependencies"/> array
        ///         and invokes the specified <paramref name="effect"/> whenever one of the dependencies changes.
        ///     </para>
        ///     <para>
        ///         This overload allows you to specify an <see cref="IScheduler"/>
        ///         which is used for scheduling <paramref name="effect"/> invocations.
        ///     </para>
        /// </summary>
        /// <param name="effect">
        ///     The function to be invoked immediately and whenever one of the <paramref name="dependencies"/> changes.
        /// </param>
        /// <param name="scheduler">
        ///     <para>
        ///         An <see cref="IScheduler"/> on which the <paramref name="effect"/> invocations are scheduled.<br/>
        ///         <b>Important: </b> The initial invocation of <paramref name="effect"/> is always run immediately
        ///         on the calling thread and <i>is not</i> scheduled on this scheduler.
        ///     </para>
        ///     <para>
        ///         If this is <see langword="null"/>, scheduling of <paramref name="effect"/> invocations
        ///         depends on the observables passed as <paramref name="dependencies"/>.
        ///     </para>
        /// </param>
        /// <param name="dependencies">
        ///     <para>
        ///         A set of dependencies which will be watched for changes.
        ///         This can be any kind of observable. <see cref="IRef{T}"/> and <see cref="IReadOnlyRef{T}"/> instances
        ///         implement <see cref="IObservable{T}"/> and can directly be passed as dependencies.
        ///     </para>
        ///     <para>
        ///         If this is empty, the <paramref name="effect"/> will never be invoked.
        ///     </para>
        /// </param>
        /// <returns>
        ///     A disposable which, when disposed, cancels the watch subscription.
        ///     Once canceled, the <paramref name="effect"/> function will not be invoked again
        ///     when one of the <paramref name="dependencies"/> changes.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="effect"/>, <paramref name="dependencies"/> or one of the dependencies in
        ///     the <paramref name="dependencies"/> array is <see langword="null"/>.
        /// </exception>
        public static IDisposable WatchEffect(Action effect, IScheduler? scheduler, params IObservable<Unit>[] dependencies)
        {
            _ = effect ?? throw new ArgumentNullException(nameof(effect));
            _ = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
            EnsureNoNullInDependencies(dependencies);
            return SyncWatchImpl(effect, runEffectNow: true, scheduler, dependencies);
        }

        /// <inheritdoc cref="WatchEffect(Action, IObservable{Unit}[])"/>
        public static IDisposable WatchEffect(Func<Task> effect, params IObservable<Unit>[] dependencies) =>
            WatchEffect(effect, scheduler: null, dependencies);

        /// <inheritdoc cref="WatchEffect(Action, IScheduler?, IObservable{Unit}[])"/>
        public static IDisposable WatchEffect(Func<Task> effect, IScheduler? scheduler, params IObservable<Unit>[] dependencies)
        {
            _ = effect ?? throw new ArgumentNullException(nameof(effect));
            _ = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
            EnsureNoNullInDependencies(dependencies);
            return WatchEffect(_ => effect(), scheduler, dependencies);
        }

        /// <inheritdoc cref="WatchEffect(Action, IObservable{Unit}[])"/>
        public static IDisposable WatchEffect(Func<CancellationToken, Task> effect, params IObservable<Unit>[] dependencies) =>
            WatchEffect(effect, scheduler: null, dependencies);

        /// <inheritdoc cref="WatchEffect(Action, IScheduler?, IObservable{Unit}[])"/>
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
