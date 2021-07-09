namespace Composed.Query
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using Composed;
    using Composed.Query.Internal;
    using static Composed.Compose;

    /// <summary>
    ///     Represents a query which fetches arbitrary data of type <typeparamref name="T"/>
    ///     (leveraging automatic data caching and query de-duplication) and
    ///     provides details about the data fetching lifecycle.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the data returned by the query.
    /// </typeparam>
#pragma warning disable CA1724 // Types should not match namespaces
    public sealed class Query<T> : IDisposable
#pragma warning restore CA1724
    {
        private readonly object _lock = new();
        private readonly QueryClient _client;
        private readonly QueryFunction<T> _queryFunction;
        private readonly IRef<QueryState<T>> _state;
        private UnifiedQuery<T>? _unifiedQuery;
        private IDisposable? _watchDependenciesSubscription;
        private IDisposable? _watchUnifiedQuerySubscription;
        private bool _isDisposed;

        /// <summary>
        ///     Gets the <see cref="QueryClient"/> with which the query is associated.
        /// </summary>
        public QueryClient Client => _client;

        /// <summary>
        ///     <para>
        ///         Gets the current state of the query.
        ///     </para>
        ///     <para>
        ///         <b>Warning:</b> This ref's value can frequently be changed from any thread due
        ///         to the way queries are internally cached and deduplicated.
        ///         If this could cause any problems to your code, use an <see cref="IScheduler"/>
        ///         with this query/ref.
        ///     </para>
        /// </summary>
        public IReadOnlyRef<QueryState<T>> State => _state;

        internal Query(
            QueryClient client,
            QueryKeyProvider getKey,
            QueryFunction<T> queryFunction,
            IObservable<Unit>[] dependencies
        )
        {
            _client = client;
            _queryFunction = queryFunction;
            _state = Ref(QueryState<T>.Disabled);

            _watchDependenciesSubscription = UseQueryKeyChangedHandler(getKey, dependencies);
        }

        private IDisposable UseQueryKeyChangedHandler(QueryKeyProvider getKey, IObservable<Unit>[] dependencies) =>
            WatchEffect(() =>
            {
                var newKey = GetQueryKeyUsingProviderFn(getKey);
                var notify = false;

                lock (_lock)
                {
                    // Disposal race condition:
                    // Since this is a callback we can end up here post disposal.
                    if (_isDisposed)
                    {
                        return;
                    }

                    // If the key didn't change there is no need to do any work as the query is already either:
                    // 1) disabled or
                    // 2) using the correct unified query.
                    if (newKey == _state.Value.Key)
                    {
                        return;
                    }
                    
                    if (newKey is null)
                    {
                        // No valid key means that the query will be disabled.
                        UnsubscribeFromCurrentUnifiedQuery();
                        notify = _state.SetValue(QueryState<T>.Disabled, suppressNotification: true);
                    }
                    else
                    {
                        // A valid key means that the query is enabled (but the key may have changed).
                        // A new key means that we must find a new unified query and synchronize the
                        // new initial query state to the current state of that unified query.
                        // Since this query subscribed to the unified query it will receive any
                        // data updates from now on.
                        var uqState = SubscribeToNewUnifiedQuery(newKey);
                        var initialState = new QueryState<T>(uqState.Status, newKey, uqState.LastData, uqState.LastError);
                        notify = _state.SetValue(initialState, suppressNotification: true);
                    }
                }

                if (notify)
                {
                    _state.Notify();
                }
            }, dependencies);

        private static QueryKey? GetQueryKeyUsingProviderFn(QueryKeyProvider provider)
        {
            // Called from lock.

            // We generally allow the query key provider functions to throw.
            // This is inspired by SWR and actually leads to a much better user experience
            // because you can entirely disregard things like null checks for missing data.
            try
            {
                return provider();
            }
            catch
            {
                return null;
            }
        }

        [MemberNotNull(nameof(_unifiedQuery))]
        [MemberNotNull(nameof(_watchUnifiedQuerySubscription))]
        private UnifiedQueryState<T> SubscribeToNewUnifiedQuery(QueryKey key)
        {
            // Called from lock.
            UnsubscribeFromCurrentUnifiedQuery();

            var cache = _client.UnifiedQueryCache;
            var unifiedQuery = cache.Get(key, _queryFunction);

            // Trigger the refetch before subscribing to the data so that we don't immediately receive
            // an "IsFetching" state change event and instead begin in the "IsFetching" state.
            unifiedQuery.Refetch();

            _unifiedQuery = unifiedQuery;
            _watchUnifiedQuerySubscription = unifiedQuery.Subscribe(s => OnUnifiedQueryStateChanged(unifiedQuery, s));

            return _unifiedQuery.CurrentState;
        }

        private void UnsubscribeFromCurrentUnifiedQuery()
        {
            // Called from lock.
            _unifiedQuery = null;
            _watchUnifiedQuerySubscription?.Dispose();
            _watchUnifiedQuerySubscription = null;
        }

        private void OnUnifiedQueryStateChanged(UnifiedQuery<T> unifiedQuery, UnifiedQueryState<T> uqState)
        {
            var notify = false;

            lock (_lock)
            {
                // Due to threading, this handler can be invoked:
                // - after disposal
                // - after a query key change (which would have changed _unifiedQuery).
                // In either case, the state shouldn't change then.
                if (_unifiedQuery != unifiedQuery || _isDisposed)
                {
                    return;
                }

                // When the underlying unified query is disabled it means that it was manually disposed
                // via the query client (auto disposal is not possible with active subscriptions).
                // In such a case, also transitively dispose this query as the query client is unusable.
                if (uqState.Status == QueryStatus.Disabled)
                {
                    Dispose();
                    return;
                }

                notify = _state.SetValue(
                    new QueryState<T>(uqState.Status, _state.Value.Key, uqState.LastData, uqState.LastError),
                    suppressNotification: true
                );
            }

            if (notify)
            {
                _state.Notify();
            }
        }

        /// <summary>
        ///     Triggers a refetch of this query (and all other queries sharing the same query key).
        /// </summary>
        public void Refetch()
        {
            _unifiedQuery?.Refetch();
        }

        /// <summary>
        ///     Disposes any resources used by <i>this specific</i> <see cref="Query"/> instance
        ///     and disposes any subscriptions made by this query.
        ///     After disposal, the query will forever remain in a disabled state.
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            {
                if (_isDisposed)
                {
                    return;
                }

                _watchDependenciesSubscription?.Dispose();
                _watchDependenciesSubscription = null;
                UnsubscribeFromCurrentUnifiedQuery();

                // Important: Set the state to disabled *after* the disposal flag is set.
                // This prevents subsequent asynchronous state updates from currently executing subscription handlers.
                _isDisposed = true;
                _state.SetValue(QueryState<T>.Disabled, suppressNotification: true);
            }
        }

        /// <summary>
        ///     Deconstructs the query into itself and its <see cref="State"/>.
        /// </summary>
        /// <param name="query">Will be assigned the query itself.</param>
        /// <param name="state">Will be assigned the query's <see cref="State"/>.</param>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public void Deconstruct(out Query<T> query, out IReadOnlyRef<QueryState<T>> state)
        {
            query = this;
            state = State;
        }

        /// <inheritdoc/>
        public override string ToString() =>
            State.Value.ToString();
    }
}
