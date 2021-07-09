namespace Composed.Query.Internal
{
    using System;
    using System.Threading.Tasks;
    using Composed;
    using static Composed.Compose;

    /// <summary>
    ///     A unified query.
    ///     Unified queries essentially do the data fetching and error management of queries.
    ///     Multiple queries with the same query key and query function share a single unified query.
    /// </summary>
    internal sealed class UnifiedQuery<T> : IDisposable
    {
        private readonly object _lock = new();
        private readonly QueryKey _key;
        private readonly QueryFunction<T> _queryFunction;
        private readonly IRef<UnifiedQueryState> _state;
        private bool _isDisposed;

        public IReadOnlyRef<QueryState<T>> QueryState { get; }

        /// <summary>
        ///     Creates a new unified query which immediately starts fetching data.
        /// </summary>
        public UnifiedQuery(QueryKey key, QueryFunction<T> queryFunction)
        {
            _key = key;
            _queryFunction = queryFunction;
            _state = Ref(
                new UnifiedQueryState(
                    new QueryState<T>(QueryStatus.Fetching, key, default, null),
                    FetchAndSetStateAsync()
                )
            );

            QueryState = Computed(() => _state.Value.QueryState, _state);
        }

        /// <summary>
        ///     If the unified query isn't fetching data at the moment, triggers a refetch of that data.
        /// </summary>
        public void Refetch()
        {
            SetState(state => state with
            {
                QueryState = new QueryState<T>(
                    state.QueryState.Status | QueryStatus.Fetching,
                    _key,
                    state.QueryState.Data,
                    state.QueryState.Error
                ),
                FetchingTask = state.FetchingTask ?? FetchAndSetStateAsync(),
            });
        }

        private Task FetchAndSetStateAsync() => Task.Run(async () =>
        {
            try
            {
                var data = await _queryFunction().ConfigureAwait(false);
                SetState(_ => new UnifiedQueryState(
                    new QueryState<T>(QueryStatus.Success, _key, data, null),
                    null
                ));
            }
            catch (Exception ex)
            {
                SetState(_ => new UnifiedQueryState(
                    new QueryState<T>(QueryStatus.Error, _key, default, ex),
                    null
                ));
            }
        });

        public void Dispose()
        {
            var notify = false;

            lock (_lock)
            {
                if (_isDisposed)
                {
                    return;
                }

                var disposedState = new UnifiedQueryState(QueryState<T>.Disabled, null);
                notify = _state.SetValue(disposedState, suppressNotification: true);
                _isDisposed = true;
            }

            if (notify)
            {
                _state.Notify();
            }
        }

        private void SetState(Func<UnifiedQueryState, UnifiedQueryState> set)
        {
            var notify = false;

            lock (_lock)
            {
                if (_isDisposed)
                {
                    return;
                }

                notify = _state.SetValue(set(_state.Value), suppressNotification: true);
            }

            if (notify)
            {
                _state.Notify();
            }
        }

        private sealed record UnifiedQueryState(QueryState<T> QueryState, Task? FetchingTask);
    }
}
