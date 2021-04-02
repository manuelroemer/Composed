namespace Composed.State
{
    using System;
    using System.Collections.Generic;
    using static Composed.Compose;

    /// <summary>
    ///     <para>
    ///         The base class for a state container based on Composed's API.
    ///     </para>
    ///     <para>
    ///         A <see cref="Store{TState}"/> implementation is based on the following key concepts:
    ///
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     Stores provide the <see cref="State"/> property, a ref which holds the
    ///                     stores current <i>state</i> and provides notifications when the value changes.
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Because <see cref="State"/> is a ref, you can fully utilize Composed's API to
    ///                     interact with it.
    ///                     <see cref="Compose.UseState{TState, T}(Store{TState}, Func{TState, T})"/>
    ///                     is the recommended way to <i>select</i> a property from the store's state.
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Deriving store classes can define public methods (so called <i>actions</i>) which change
    ///                     the store's state using <see cref="SetState(Func{TState, TState})"/>.
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </summary>
    /// <typeparam name="TState">The type of the state which is managed by the store.</typeparam>
    /// <remarks>
    ///     <para>
    ///         It is strongly recommended that the managed state type <typeparamref name="TState"/> is immutable.
    ///         Immutability brings several benefits, such as preventing any undersired state modification from the
    ///         outside or the foundation for undo/redo functionality.
    ///         You can easily achieve make <typeparamref name="TState"/> immutable by using records.
    ///     </para>
    ///     <para>
    ///         <b>Thread Safety:</b><br/>
    ///         The <see cref="Store{TState}"/> class has the following threading characteristics:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     Setting the store's state is synchronized and can therefore be done concurrently.
    ///                     See <see cref="SetState(Func{TState, TState})"/> for details and implications.
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     The store's <see cref="State"/> ref is a standard ref returned by
    ///                     <see cref="Ref{T}(T, IEqualityComparer{T})"/>.
    ///                     Its change notifications are <b>not</b> synchronized.
    ///                     If multiple threads interact with the store, watchers should use
    ///                     schedulers to avoid race conditions and/or stale data.
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    public abstract class Store<TState>
    {
        private readonly object _lock = new();
        private readonly IRef<TState> _state;

        /// <summary>
        ///     Gets a ref holding the store's current state.
        /// </summary>
        public IReadOnlyRef<TState> State { get; }

        /// <summary>
        ///     <para>
        ///         Initializes a new store with the specified <paramref name="initialState"/>.
        ///     </para>
        ///     <para>
        ///         When its value changes, the store's <see cref="State"/> ref will use
        ///         <see cref="EqualityComparer{T}.Default"/> for determining whether the value has
        ///         effectively changed and whether dependents should be notified about that change.
        ///     </para>
        /// </summary>
        /// <param name="initialState">The initial state of the store.</param>
        protected Store(TState initialState)
            : this(initialState, equalityComparer: null) { }

        /// <summary>
        ///     <para>
        ///         Initializes a new store with the specified <paramref name="initialState"/>.
        ///     </para>
        ///     <para>
        ///         This overload allows you to specify the <see cref="IEqualityComparer{T}"/> to be used
        ///         by the store's <see cref="State"/> ref for determining whether the value has effectively
        ///         changed and whether dependents should be notified about that change.
        ///     </para>
        /// </summary>
        /// <param name="initialState">The initial state of the store.</param>
        /// <param name="equalityComparer">
        ///     <para>
        ///         An <see cref="IEqualityComparer{T}"/> instance which will be used to compare the
        ///         <see cref="State"/> ref's new and old value when it changes.
        ///         The ref will only notify its dependents of the change when the equality comparer
        ///         considers the two values unequal.
        ///     </para>
        ///     <para>
        ///         If this is <see langword="null"/>, <see cref="EqualityComparer{T}.Default"/> is used instead.
        ///     </para>
        /// </param>
        protected Store(TState initialState, IEqualityComparer<TState>? equalityComparer)
        {
            _state = Ref(initialState, equalityComparer);
            State = _state;
        }

        /// <summary>
        ///     Sets the store's state to a new value which does not depend on the store's current state.
        /// </summary>
        /// <param name="newState">
        ///     The store's new state.
        /// </param>
        protected void SetState(TState newState)
        {
            bool shouldNotify;

            lock (_lock)
            {
                shouldNotify = _state.SetValue(newState, suppressNotification: true);
            }

            if (shouldNotify)
            {
                _state.Notify();
            }
        }

        /// <summary>
        ///     <para>
        ///         Sets the store's state to a new value which is returned by the given
        ///         <paramref name="provider"/> function based on the store's current state.
        ///     </para>
        ///     <para>
        ///         The <paramref name="provider"/> function is guaranteed to always receive the latest state value.
        ///         While the function is executing, the store's state is guaranteed to not change in between.
        ///     </para>
        ///     <para>
        ///         <b>Important: </b> The <paramref name="provider"/> function is called from a <c>lock</c> block.
        ///         To avoid deadlocks, ensure that the function is not blocking or joining any thread.
        ///         Generally, <paramref name="provider"/> is recommended to be a pure function and to complete
        ///         as quickly as possible.
        ///     </para>
        /// </summary>
        /// <param name="provider">
        ///     A function which receives the store's current state and, based on that, returns the store's new state.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="provider"/> is <see langword="null"/>.
        /// </exception>
        protected void SetState(Func<TState, TState> provider)
        {
            _ = provider ?? throw new ArgumentNullException(nameof(provider));

            bool shouldNotify;

            lock (_lock)
            {
                var previousState = State.Value;
                var newState = provider(previousState);
                shouldNotify = _state.SetValue(newState, suppressNotification: true);
            }

            if (shouldNotify)
            {
                _state.Notify();
            }
        }
    }
}
