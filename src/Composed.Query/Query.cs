namespace Composed.Query
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Reactive;
    using Composed;
    using Composed.Query.Internal;
    using static Composed.Compose;

    internal sealed record QueryState(
        QueryKey? Key,
        UnifiedQuery? UnifiedQuery,
        bool IsLoading,
        bool IsFetching,
        object? Data,
        Exception? Error
    )
    {
        public static readonly QueryState Disabled = new(null, null, false, false, null, null);

        [MemberNotNullWhen(false, nameof(Key))]
        [MemberNotNullWhen(false, nameof(UnifiedQuery))]
        public bool IsDisabled => Key is null && UnifiedQuery is null;
    }

    /// <summary>
    ///     Represents a query which fetches arbitrary data of type <typeparamref name="T"/>
    ///     and provides details about its lifecycle.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the data returned by the query.
    /// </typeparam>
    public class Query : IDisposable
    {
        private readonly object _lock = new();
        private readonly QueryClient _client;
        private readonly QueryFunction _queryFunction;
        private readonly IRef<QueryState> _state;
        private IDisposable? _watchDependenciesSubscription;
        private IDisposable? _watchUnifiedQuerySubscription;

        /// <summary>
        ///     Gets the <see cref="QueryClient"/> with which the query is associated.
        /// </summary>
        public QueryClient Client => _client;

        /// <summary>
        ///     Gets the <see cref="QueryKey"/> which uniquely identifies this query.
        /// </summary>
        public IReadOnlyRef<QueryKey?> Key { get; }

        /// <summary>
        ///     Gets a value indicating whether the query is currently disabled.
        ///     Disabled queries do not have a key and generally don't perform any kind of actions.
        /// </summary>
        /// <remarks>
        ///     There are two ways for a query to end up disabled:
        ///
        ///     <list type="number">
        ///         <item>
        ///             <description>
        ///                 The query gets disposed.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 The query was created using a <see cref="QueryKeyProvider"/> function
        ///                 which either returned <see langword="null"/> as the <see cref="QueryKey"/>
        ///                 or threw an exception.
        ///                 Either way, this resulted in the query not having a key which effectively
        ///                 disables it.
        ///                 Queries disabled this way are automatically enabled when the
        ///                 <see cref="QueryKeyProvider"/> returns a valid key.
        ///             </description>
        ///         </item>
        ///     </list>
        /// </remarks>
        public IReadOnlyRef<bool> IsDisabled { get; }

        /// <summary>
        ///     Gets a value indicating whether the query is loading the <i>initial</i> data at the moment.
        ///     If this is <see langword="true"/>, the query cannot provide any data at the moment.
        /// </summary>
        public IReadOnlyRef<bool> IsLoading { get; }

        /// <summary>
        ///     Gets a value indicating whether the query is fetching data at the moment.
        /// </summary>
        public IReadOnlyRef<bool> IsFetching { get; }

        /// <summary>
        ///     If the query sucessfully fetched data, gets that fetched data.
        ///     If the query hasn't fetched any data yet, this is the default value of <typeparamref name="T"/>
        ///     (typically <see langword="null"/>).
        /// </summary>
        public IReadOnlyRef<object?> Data { get; }

        /// <summary>
        ///     If the query threw an exception instead of completing successfully, gets that thrown exception.
        ///     Otherwise, this is <see langword="null"/>.
        /// </summary>
        public IReadOnlyRef<Exception?> Error { get; }

        internal Query(
            QueryClient client,
            QueryKeyProvider getKey,
            QueryFunction queryFunction,
            IObservable<Unit>[] dependencies
        )
        {
            _client = client;
            _queryFunction = queryFunction;
            _state = Ref(QueryState.Disabled);

            Key = Computed(() => _state.Value.Key, _state);
            IsDisabled = Computed(() => _state.Value.IsDisabled, _state);
            IsLoading = Computed(() => _state.Value.IsLoading, _state);
            IsFetching = Computed(() => _state.Value.IsFetching, _state);
            Data = Computed(() => _state.Value.Data, _state);
            Error = Computed(() => _state.Value.Error, _state);
            
            _watchDependenciesSubscription = WatchEffect(() =>
            {
                var key = GetQueryKeyUsingProviderFn(getKey);

                lock (_lock)
                {
                    if (key == _state.Value.Key)
                    {
                        return;
                    }
                }

                if (key is not null)
                {
                    Reset(key);
                }
                else
                {
                    Disable();
                }
            }, dependencies);
        }

        private static QueryKey? GetQueryKeyUsingProviderFn(QueryKeyProvider provider)
        {
            // We generally allow the query key provider functions to throw;
            // this is inspired by SWR and actually leads to a much better user experience
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

        private void Reset(QueryKey key)
        {
            UnsubscribeFromCurrentUnifiedQuery();

            var unifiedQueryCache = _client.UnifiedQueryCache;
            var unifiedQuery = unifiedQueryCache.GetOrAdd(key, _queryFunction);

            _watchUnifiedQuerySubscription = WatchEffect(() =>
            {
                var uqState = unifiedQuery.State.Value;

                SetState(state => state with
                {
                    Key = key,
                    UnifiedQuery = unifiedQuery,
                    IsLoading = uqState.IsLoading,
                    IsFetching = uqState.IsFetching,
                    Data = uqState.LastData,
                    Error = uqState.LastError,
                });
            }, unifiedQuery.State);

            Refetch();
        }

        private void Disable()
        {
            UnsubscribeFromCurrentUnifiedQuery();
            SetState(QueryState.Disabled);
        }

        private void UnsubscribeFromCurrentUnifiedQuery()
        {
            _watchUnifiedQuerySubscription?.Dispose();
            _watchUnifiedQuerySubscription = null;
        }

        public void Refetch()
        {
            lock (_lock)
            {
                if (!_state.Value.IsDisabled)
                {
                    _state.Value.UnifiedQuery.Refetch();
                }
            }
        }

#pragma warning disable CA1063 // IDisposable is only implemented in this base class. Query can furthermore only be
#pragma warning disable CA1816 // extended in this package. There's no need for the full ceremony.
        /// <summary>
        ///     Disposes any resources used by <i>this specific</i> <see cref="Query"/> instance
        ///     and disposes any subscriptions made by this query.
        ///     After disposal, the query will forever remain in a disabled state.
        /// </summary>
        public void Dispose()
#pragma warning restore CA1816
#pragma warning restore CA1063
        {
            _watchDependenciesSubscription?.Dispose();
            _watchDependenciesSubscription = null;
            Disable();
        }

        private void SetState(QueryState state) =>
            SetState(_ => state);

        private void SetState(Func<QueryState, QueryState> set)
        {
            lock (_lock)
            {
                _state.Value = set(_state.Value);
            }
        }
    }
}
