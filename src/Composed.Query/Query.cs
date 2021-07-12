namespace Composed.Query
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Threading.Tasks;
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
        private readonly CancelableQueryFunction<T> _queryFunction;
        private readonly IRef<QueryState<T>> _state;
        private IDisposable? _watchDependenciesSubscription;
        private IDisposable? _rentSharedQuerySubscription;
        private IDisposable? _watchSharedQuerySubscription;
        private bool _isDisposed;
        private SharedQuery<T>? _sharedQuery;

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
            CancelableQueryFunction<T> queryFunction,
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
                    // 2) using the correct shared query.
                    if (newKey == _state.Value.Key)
                    {
                        return;
                    }
                    
                    if (newKey is null)
                    {
                        // No valid key means that the query will be disabled.
                        UnsubscribeFromCurrentSharedQuery();
                        notify = _state.SetValue(QueryState<T>.Disabled, suppressNotification: true);
                    }
                    else
                    {
                        // A valid key means that the query is enabled (but the key may have changed).
                        // A new key means that we must find a new shared query and synchronize the
                        // new initial query state to the current state of that shared query.
                        // Since this query subscribed to the shared query it will receive any
                        // data updates from now on.
                        SubscribeToNewSharedQuery(newKey);
                        notify = SyncStateWithSharedQuery(_sharedQuery);
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

        [MemberNotNull(nameof(_sharedQuery))]
        [MemberNotNull(nameof(_rentSharedQuerySubscription))]
        [MemberNotNull(nameof(_watchSharedQuerySubscription))]
        private QueryState<T> SubscribeToNewSharedQuery(QueryKey key)
        {
            // Called from lock.
            UnsubscribeFromCurrentSharedQuery();

            var (sharedQuery, rentSharedQuerySubscription) = _client.SharedQueryCache.Rent(key, _queryFunction);
            var watchSharedQuerySubscription = UseSharedQueryStateChangedHandler(sharedQuery);

            // Trigger the refetch before subscribing to the data so that we don't immediately receive
            // an "IsFetching" state change event and instead begin in the "IsFetching" state.
            sharedQuery.Refetch();

            _sharedQuery = sharedQuery;
            _rentSharedQuerySubscription = rentSharedQuerySubscription;
            _watchSharedQuerySubscription = watchSharedQuerySubscription;

            return sharedQuery.QueryState.Value;
        }

        private void UnsubscribeFromCurrentSharedQuery()
        {
            // Called from lock.
            _sharedQuery = null;
            _watchSharedQuerySubscription?.Dispose();
            _watchSharedQuerySubscription = null;
            _rentSharedQuerySubscription?.Dispose();
            _rentSharedQuerySubscription = null;
        }

        private IDisposable UseSharedQueryStateChangedHandler(SharedQuery<T> sharedQuery) =>
            Watch(() =>
            {
                var notify = false;

                lock (_lock)
                {
                    // Due to threading, this handler can be invoked:
                    // - after disposal
                    // - after a query key change (which would have changed _sharedQuery).
                    // In either case, the state shouldn't change then.
                    if (_sharedQuery != sharedQuery || _isDisposed)
                    {
                        return;
                    }

                    notify = SyncStateWithSharedQuery(sharedQuery);
                }

                if (notify)
                {
                    _state.Notify();
                }
            }, sharedQuery.QueryState);

        /// <summary>
        ///     Sets the <see cref="State"/> to the state of the shared query.
        ///     Disposes the query when the shared query is disposed.
        /// </summary>
        /// <returns>
        ///     Whether a state changed notification should be sent.
        /// </returns>
        private bool SyncStateWithSharedQuery(SharedQuery<T> sharedQuery)
        {
            // Called from lock.

            // When the underlying shared query gets disabled it means that it was manually disposed
            // via the query client (auto disposal is not possible with active subscriptions).
            // In such a case, also transitively dispose this query as the query client is unusable.
            if (sharedQuery.QueryState.Value.IsDisabled)
            {
                return DisposeInternal();
            }

            return _state.SetValue(
                sharedQuery.QueryState.Value,
                suppressNotification: true
            );
        }

        /// <summary>
        ///     Triggers a refetch of this query (and all other queries sharing the same query key
        ///     and query function).
        /// </summary>
        /// <returns>
        ///     <see langword="true"/> if the data could successfully be set;
        ///     <see langword="false"/> if the data could not be set because the query is currently disabled.
        /// </returns>
        public bool Refetch()
        {
            return _sharedQuery?.Refetch() ?? false;
        }

        /// <summary>
        ///     Manually sets the data of this query (and all other queries sharing the same query key
        ///     and query function) to the specified value.
        ///     This does <i>not</i> interrupt an ongoing fetching attempt.
        /// </summary>
        /// <param name="data">The data to be set.</param>
        /// <returns>
        ///     <see langword="true"/> if the data could successfully be set;
        ///     <see langword="false"/> if the data could not be set because the query is currently disabled.
        /// </returns>
        public bool SetData(T data)
        {
            return _sharedQuery?.SetData(data) ?? false;
        }

        /// <summary>
        ///     Manually sets the error of this query (and all other queries sharing the same query key
        ///     and query function) to the specified value.
        ///     This does <i>not</i> interrupt an ongoing fetching attempt.
        /// </summary>
        /// <param name="error">The error to be set.</param>
        /// <returns>
        ///     <see langword="true"/> if the error could successfully be set;
        ///     <see langword="false"/> if the error could not be set because the query is currently disabled.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="error"/> is <see langword="null"/>.
        /// </exception>
        public bool SetError(Exception error)
        {
            _ = error ?? throw new ArgumentNullException(nameof(error));
            return _sharedQuery?.SetError(error) ?? false;
        }

        /// <summary>
        ///     Notifies a <see cref="CancelableQueryFunction{T}"/> that it should cancel fetching data.
        ///     Calling this function has no effect if the query is not fetching any data at the moment.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task"/> which completes when the query leaves the <see cref="QueryStatus.Fetching"/> status.
        ///     If the query is not fetching any data at the moment, the task immediately completes.
        /// </returns>
        public Task CancelAsync()
        {
            return _sharedQuery?.CancelAsync() ?? Task.CompletedTask;
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
                DisposeInternal();
            }
        }

        /// <summary>
        ///     Performs the disposing logic for the query.
        /// </summary>
        /// <returns>
        ///     Whether a state changed notification should be sent.
        /// </returns>
        private bool DisposeInternal()
        {
            // Called from lock.
            if (_isDisposed)
            {
                return false;
            }

            _watchDependenciesSubscription?.Dispose();
            _watchDependenciesSubscription = null;
            UnsubscribeFromCurrentSharedQuery();

            // Important: Set the state to disabled *after* the disposal flag is set.
            // This prevents subsequent asynchronous state updates from currently executing subscription handlers.
            _isDisposed = true;
            return _state.SetValue(QueryState<T>.Disabled, suppressNotification: true);
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
