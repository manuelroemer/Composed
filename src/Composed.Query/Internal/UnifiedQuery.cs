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

    internal sealed record UnifiedQueryState<T>(
        QueryStatus Status,
        T? LastData,
        Exception? LastError,
        Task? FetchingTask
    );

    internal sealed class UnifiedQuery<T>
    {
        private readonly object _lock = new();
        private readonly QueryFunction<T> _queryFunction;
        private readonly IRef<UnifiedQueryState<T>> _state;
        private int _activeSubscriptions;

        public UnifiedQueryState<T> CurrentState => _state.Value;

        public UnifiedQuery(QueryFunction<T> queryFunction)
        {
            _queryFunction = queryFunction;
            _state = Ref(new UnifiedQueryState<T>(QueryStatus.Fetching, default, null, FetchAndSetStateAsync()));
        }

        public void Refetch()
        {
            SetState(state => state with
            {
                Status = state.Status | QueryStatus.Fetching,
                FetchingTask = state.FetchingTask ?? FetchAndSetStateAsync(),
            });
        }

        private async Task FetchAndSetStateAsync()
        {
            T? data = default;
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
                Status = error is null ? QueryStatus.Success : QueryStatus.Error,
                FetchingTask = null,
                LastData = data,
                LastError = error,
            });
        }

        private void SetState(Func<UnifiedQueryState<T>, UnifiedQueryState<T>> set)
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

        public IDisposable Subscribe(Action<UnifiedQueryState<T>> onStateChanged)
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
