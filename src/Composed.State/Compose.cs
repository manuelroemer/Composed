namespace Composed.State
{
    using System;
    using System.Collections.Generic;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using static Composed.Compose;

    /// <summary>
    ///     <para>
    ///         Provides a set of static methods which enable the composition of state and state containers.
    ///     </para>
    ///     <para>
    ///         It is recommended that you import this class via a <c>using static</c> directive
    ///         (<c>using static Composed.Commands.Compose;</c>) if you are using C#.
    ///     </para>
    /// </summary>
    public static class Compose
    {
        /// <summary>
        ///     <para>
        ///         Allows selecting and/or computing a value based on a store's most recent state.
        ///         The function returns a computed ref which uses the specified <paramref name="stateSelector"/>
        ///         to (re-)compute its value whenever the store's <see cref="Store{TState}.State"/>
        ///         property changes.
        ///     </para>
        ///     <para>
        ///         When the ref's value is recomputed, it will use <see cref="EqualityComparer{T}.Default"/>
        ///         to determine whether its value effectively changes and whether observers
        ///         should be notified about that change.
        ///     </para>
        ///     <para>
        ///         <paramref name="stateSelector"/> invocations are not scheduled.
        ///     </para>
        /// </summary>
        /// <typeparam name="TState">The type of the state which is managed by the store.</typeparam>
        /// <typeparam name="T">The type of the value which is held by the returned ref.</typeparam>
        /// <param name="store">The store on which the returned ref's value is based.</param>
        /// <param name="stateSelector">
        ///     A function which receives the store's current state and, based on that state,
        ///     returns a new value which will be held by the returned ref.
        /// </param>
        /// <returns>
        ///     A computed <see cref="IReadOnlyRef{T}"/> which depends on the <see cref="Store{TState}.State"/>
        ///     property of the specified <paramref name="store"/> and (re-)computes its value
        ///     using the given <paramref name="stateSelector"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="store"/> or <paramref name="stateSelector"/> is <see langword="null"/>.
        /// </exception>
        /// <seealso cref="Computed{TResult}(Func{TResult}, IObservable{Unit}[])"/>
        public static IReadOnlyRef<T> UseState<TState, T>(Store<TState> store, Func<TState, T> stateSelector)
        {
            return UseState(store, stateSelector, equalityComparer: null, scheduler: null);
        }

        /// <summary>
        ///     <para>
        ///         Allows selecting and/or computing a value based on a store's most recent state.
        ///         The function returns a computed ref which uses the specified <paramref name="stateSelector"/>
        ///         to (re-)compute its value whenever the store's <see cref="Store{TState}.State"/>
        ///         property changes.
        ///     </para>
        ///     <para>
        ///         This overload allows you to specify an <see cref="IScheduler"/>
        ///         which is used for scheduling <paramref name="stateSelector"/> invocations and any
        ///         subsequent notifications.
        ///     </para>
        ///     <para>
        ///         When the ref's value is recomputed, it will use <see cref="EqualityComparer{T}.Default"/>
        ///         to determine whether its value effectively changes and whether observers
        ///         should be notified about that change.
        ///     </para>
        /// </summary>
        /// <typeparam name="TState">The type of the state which is managed by the store.</typeparam>
        /// <typeparam name="T">The type of the value which is held by the returned ref.</typeparam>
        /// <param name="store">The store on which the returned ref's value is based.</param>
        /// <param name="stateSelector">
        ///     A function which receives the store's current state and, based on that state,
        ///     returns a new value which will be held by the returned ref.
        /// </param>
        /// <param name="scheduler">
        ///     <para>
        ///         An <see cref="IScheduler"/> on which the <paramref name="stateSelector"/> invocations and
        ///         any subsequent notifications are scheduled.<br/>
        ///         <b>Important: </b> The initial invocation of <paramref name="stateSelector"/> is always run immediately
        ///         on the calling thread and <i>is not</i> scheduled on this scheduler.
        ///     </para>
        ///     <para>
        ///         If this is <see langword="null"/>, <paramref name="stateSelector"/> invocations are not scheduled.
        ///     </para>
        /// </param>
        /// <returns>
        ///     A computed <see cref="IReadOnlyRef{T}"/> which depends on the <see cref="Store{TState}.State"/>
        ///     property of the specified <paramref name="store"/> and (re-)computes its value
        ///     using the given <paramref name="stateSelector"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="store"/> or <paramref name="stateSelector"/> is <see langword="null"/>.
        /// </exception>
        /// <seealso cref="Computed{TResult}(Func{TResult}, IScheduler?, IObservable{Unit}[])"/>
        public static IReadOnlyRef<T> UseState<TState, T>(
            Store<TState> store,
            Func<TState, T> stateSelector,
            IScheduler? scheduler
        )
        {
            return UseState(store, stateSelector, equalityComparer: null, scheduler);
        }

        /// <summary>
        ///     <para>
        ///         Allows selecting and/or computing a value based on a store's most recent state.
        ///         The function returns a computed ref which uses the specified <paramref name="stateSelector"/>
        ///         to (re-)compute its value whenever the store's <see cref="Store{TState}.State"/>
        ///         property changes.
        ///     </para>
        ///     <para>
        ///         This overload allows you to specify an <see cref="IEqualityComparer{T}"/> which is used
        ///         by the ref to determine whether its value effectively changes and whether
        ///         observers should be notified about such a change.
        ///     </para>
        ///     <para>
        ///         <paramref name="stateSelector"/> invocations are not scheduled.
        ///     </para>
        /// </summary>
        /// <typeparam name="TState">The type of the state which is managed by the store.</typeparam>
        /// <typeparam name="T">The type of the value which is held by the returned ref.</typeparam>
        /// <param name="store">The store on which the returned ref's value is based.</param>
        /// <param name="stateSelector">
        ///     A function which receives the store's current state and, based on that state,
        ///     returns a new value which will be held by the returned ref.
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
        ///     A computed <see cref="IReadOnlyRef{T}"/> which depends on the <see cref="Store{TState}.State"/>
        ///     property of the specified <paramref name="store"/> and (re-)computes its value
        ///     using the given <paramref name="stateSelector"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="store"/> or <paramref name="stateSelector"/> is <see langword="null"/>.
        /// </exception>
        /// <seealso cref="Computed{TResult}(Func{TResult}, IEqualityComparer{TResult}?, IObservable{Unit}[])"/>
        public static IReadOnlyRef<T> UseState<TState, T>(
            Store<TState> store,
            Func<TState, T> stateSelector,
            IEqualityComparer<T>? equalityComparer
        )
        {
            return UseState(store, stateSelector, equalityComparer, scheduler: null);
        }

        /// <summary>
        ///     <para>
        ///         Allows selecting and/or computing a value based on a store's most recent state.
        ///         The function returns a computed ref which uses the specified <paramref name="stateSelector"/>
        ///         to (re-)compute its value whenever the store's <see cref="Store{TState}.State"/>
        ///         property changes.
        ///     </para>
        ///     <para>
        ///         This overload allows you to specify an <see cref="IEqualityComparer{T}"/> which is used
        ///         by the ref to determine whether its value effectively changes and whether
        ///         observers should be notified about such a change and an <see cref="IScheduler"/>
        ///         which is used for scheduling the <paramref name="stateSelector"/> invocations and any
        ///         subsequent notifications.
        ///     </para>
        /// </summary>
        /// <typeparam name="TState">The type of the state which is managed by the store.</typeparam>
        /// <typeparam name="T">The type of the value which is held by the returned ref.</typeparam>
        /// <param name="store">The store on which the returned ref's value is based.</param>
        /// <param name="stateSelector">
        ///     A function which receives the store's current state and, based on that state,
        ///     returns a new value which will be held by the returned ref.
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
        ///         An <see cref="IScheduler"/> on which the <paramref name="stateSelector"/> invocations and
        ///         any subsequent notifications are scheduled.<br/>
        ///         <b>Important: </b> The initial invocation of <paramref name="stateSelector"/> is always run immediately
        ///         on the calling thread and <i>is not</i> scheduled on this scheduler.
        ///     </para>
        ///     <para>
        ///         If this is <see langword="null"/>, <paramref name="stateSelector"/> invocations are not scheduled.
        ///     </para>
        /// </param>
        /// <returns>
        ///     A computed <see cref="IReadOnlyRef{T}"/> which depends on the <see cref="Store{TState}.State"/>
        ///     property of the specified <paramref name="store"/> and (re-)computes its value
        ///     using the given <paramref name="stateSelector"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="store"/> or <paramref name="stateSelector"/> is <see langword="null"/>.
        /// </exception>
        /// <seealso cref="Computed{TResult}(Func{TResult}, IEqualityComparer{TResult}?, IScheduler?, IObservable{Unit}[])"/>
        public static IReadOnlyRef<T> UseState<TState, T>(
            Store<TState> store,
            Func<TState, T> stateSelector,
            IEqualityComparer<T>? equalityComparer,
            IScheduler? scheduler
        )
        {
            _ = store ?? throw new ArgumentNullException(nameof(store));
            _ = stateSelector ?? throw new ArgumentNullException(nameof(stateSelector));
            return Computed(() => stateSelector(store.State.Value), equalityComparer, scheduler, store.State);
        }
    }
}
