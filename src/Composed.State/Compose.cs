namespace Composed.State
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Concurrency;
    using static Composed.Compose;

    public static class Compose
    {
        public static IReadOnlyRef<T> UseStore<TState, T>(Store<TState> store, Func<TState, T> stateSelector)
        {
            return UseStore(store, stateSelector, equalityComparer: null, scheduler: null);
        }

        public static IReadOnlyRef<T> UseStore<TState, T>(
            Store<TState> store,
            Func<TState, T> stateSelector,
            IScheduler? scheduler
        )
        {
            return UseStore(store, stateSelector, equalityComparer: null, scheduler);
        }

        public static IReadOnlyRef<T> UseStore<TState, T>(
            Store<TState> store,
            Func<TState, T> stateSelector,
            IEqualityComparer<T>? equalityComparer
        )
        {
            return UseStore(store, stateSelector, equalityComparer, scheduler: null);
        }

        public static IReadOnlyRef<T> UseStore<TState, T>(
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
