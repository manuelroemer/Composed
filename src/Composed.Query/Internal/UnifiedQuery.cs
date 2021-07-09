namespace Composed.Query.Internal
{
    using System;
    using System.Diagnostics;
    using System.Reactive.Concurrency;
    using System.Reactive.Disposables;
    using System.Threading;
    using System.Threading.Tasks;
    using Composed;
    using static Composed.Compose;

    internal sealed record UnifiedQueryState<T>(QueryStatus Status, T? LastData, Exception? LastError, Task? FetchingTask);

    /// <summary>
    ///     A unified query.
    ///     Unified queries essentially do the data fetching and error management of queries.
    ///     Multiple queries with the same query key and query function share a single unified query.
    /// </summary>
    internal sealed class UnifiedQuery<T> : IDisposable
    {
        private readonly object _lock = new();
        private readonly QueryFunction<T> _queryFunction;
        private readonly IRef<UnifiedQueryState<T>> _state;
        private int _activeSubscriptions;
        private bool _isDisposed;

        public UnifiedQueryState<T> CurrentState => _state.Value;

        /// <summary>
        ///     Creates a new unified query which immediately starts fetching data.
        /// </summary>
        public UnifiedQuery(QueryFunction<T> queryFunction)
        {
            _queryFunction = queryFunction;
            _state = Ref(new UnifiedQueryState<T>(QueryStatus.Fetching, default, null, FetchAndSetStateAsync()));
        }

        /// <summary>
        ///     If the unified query isn't fetching data at the moment, triggers a refetch of that data.
        /// </summary>
        public void Refetch()
        {
            SetState(state => state with
            {
                Status = state.Status | QueryStatus.Fetching,
                FetchingTask = state.FetchingTask ?? FetchAndSetStateAsync(),
            });
        }

        private Task FetchAndSetStateAsync() => Task.Run(async () =>
        {
            try
            {
                var data = await _queryFunction().ConfigureAwait(false);
                SetState(_ => new UnifiedQueryState<T>(QueryStatus.Success, data, null, null));
            }
            catch (Exception ex)
            {
                SetState(_ => new UnifiedQueryState<T>(QueryStatus.Error, default, ex, null));
            }
        });

        public IDisposable Subscribe(Action<UnifiedQueryState<T>> onStateChanged)
        {
            lock (_lock)
            {
                if (_isDisposed)
                {
                    return Disposable.Empty;
                }
            }

            var subscription = Watch(() => onStateChanged(_state.Value), ImmediateScheduler.Instance, _state);
            var unsubscribe = Disposable.Create(() =>
            {
                subscription.Dispose();
                Interlocked.Decrement(ref _activeSubscriptions);
                Debug.Assert(_activeSubscriptions >= 0, "The active subscriptions should never fall below 0.");
            });

            Interlocked.Increment(ref _activeSubscriptions);
            return unsubscribe;
        }

        public void Dispose()
        {
            var notify = false;

            lock (_lock)
            {
                if (_isDisposed)
                {
                    return;
                }

                var disposedState = new UnifiedQueryState<T>(QueryStatus.Disabled, default, null, null);
                notify = _state.SetValue(disposedState, suppressNotification: true);
                _isDisposed = true;
            }

            if (notify)
            {
                _state.Notify();
            }
        }

        private void SetState(Func<UnifiedQueryState<T>, UnifiedQueryState<T>> set)
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
    }
}
