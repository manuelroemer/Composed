namespace Composed.Query.Internal
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Reactive.Disposables;
    using System.Threading;
    using System.Threading.Tasks;
    using Composed;
    using static Composed.Compose;

    internal sealed record UnifiedQueryState(
        bool IsLoading,
        object? LastData,
        Exception? LastError,
        Task? FetchingTask
    )
    {
        public QueryStatus Status =>
            IsLoading
                ? QueryStatus.Loading
                : FetchingTask is not null
                    ? QueryStatus.Fetching
                    : QueryStatus.Idle;
    }

    internal sealed class UnifiedQuery
    {
        private readonly object _lock = new();
        private readonly QueryFunction _queryFunction;
        private readonly IRef<UnifiedQueryState> _state;
        private int _activeSubscriptions;

        public UnifiedQueryState CurrentState => _state.Value;

        public UnifiedQuery(QueryFunction queryFunction)
        {
            _queryFunction = queryFunction;
            _state = Ref(new UnifiedQueryState(true, null, null, FetchAndSetStateAsync()));
        }

        public void Refetch()
        {
            SetState(state => state with
            {
                FetchingTask = state.FetchingTask ?? FetchAndSetStateAsync(),
            });
        }

        private async Task FetchAndSetStateAsync()
        {
            object? data = null;
            Exception? error = null;

            try
            {
                data = await _queryFunction().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                error = ex;
            }

            SetState(state => state with
            {
                IsLoading = false,
                FetchingTask = null,
                LastData = data,
                LastError = error,
            });
        }

        private void SetState(Func<UnifiedQueryState, UnifiedQueryState> set)
        {
            var notify = false;

            lock (_lock)
            {
                notify = _state.SetValue(set(_state.Value), suppressNotification: true);
            }

            if (notify)
            {
                _state.Notify();
            }
        }

        public IDisposable Subscribe(Action<UnifiedQueryState> onStateChanged)
        {
            var subscription = Watch(() => onStateChanged(_state.Value), _state);
            var unsubscribe = Disposable.Create(() =>
            {
                subscription.Dispose();
                Interlocked.Decrement(ref _activeSubscriptions);
                Debug.Assert(_activeSubscriptions >= 0, "The active subscriptions should never fall below 0.");
            });

            Interlocked.Increment(ref _activeSubscriptions);
            return unsubscribe;
        }
    }
}
