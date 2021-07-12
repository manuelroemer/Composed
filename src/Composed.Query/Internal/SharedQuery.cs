namespace Composed.Query.Internal
{
    using System;
    using System.Diagnostics;
    using System.Reactive.Concurrency;
    using System.Threading;
    using System.Threading.Tasks;
    using Composed;
    using static Composed.Compose;

    /// <summary>
    ///     The <see cref="SharedQuery{T}"/> class is the central point where all the actual data
    ///     fetching and <see cref="QueryState{T}"/> management happens.
    ///     
    ///     Multiple <see cref="Query{T}"/> instances with the same query key and query function
    ///     *share* a single shared query.
    /// </summary>
    internal sealed class SharedQuery<T> : IDisposable
    {
        private readonly object _lock = new();
        private readonly CancelableQueryFunction<T> _queryFunction;
        private readonly IRef<SharedQueryState> _state;
        private bool _isDisposed;

        /// <summary>
        ///     Gets the current <see cref="QueryState{T}"/> of the shared query.
        /// </summary>
        public IReadOnlyRef<QueryState<T>> QueryState { get; }

        /// <summary>
        ///     Creates a new shared query which immediately starts fetching data.
        /// </summary>
        public SharedQuery(QueryKey key, CancelableQueryFunction<T> queryFunction)
        {
            _queryFunction = queryFunction;
            _state = Ref(new SharedQueryState(QueryState<T>.Loading(key), FetchingTask: null, Cancel: null));
            QueryState = Computed(() => _state.Value.QueryState, ImmediateScheduler.Instance, _state);
            Refetch();
        }

        public bool Refetch()
        {
            return SetState(state =>
            {
                var nextFetchingTask = state.FetchingTask;
                var nextCancel = state.Cancel;

                if (nextFetchingTask is null)
                {
                    Debug.Assert(
                        nextCancel is null,
                        "FetchingTask and Cancel must always be null at the same time. Otherwise, they got out of sync."
                    );

                    (nextFetchingTask, nextCancel) = CreateFetchingTaskAndCancel();
                }

                return state with
                {
                    QueryState = state.QueryState.WithRefetching(),
                    FetchingTask = nextFetchingTask,
                    Cancel = nextCancel,
                };
            });

            (Task FetchingTask, Func<Task> Cancel) CreateFetchingTaskAndCancel()
            {
                var cts = new CancellationTokenSource();
                var fetchingTask = Task.Run(async () =>
                {
                    // General note:
                    // The entire fetching process is run on the task pool so that long running synchronous
                    // query functions do not block.

                    try
                    {
                        var data = await _queryFunction(cts.Token).ConfigureAwait(false);

                        SetState(state => state with
                        {
                            QueryState = state.QueryState.WithSuccess(data),
                            FetchingTask = null,
                            Cancel = null,
                        });
                    }
                    catch (Exception ex)
                    {
                        SetState(state => state with
                        {
                            QueryState = state.QueryState.WithError(ex),
                            FetchingTask = null,
                            Cancel = null,
                        });
                    }
                    finally
                    {
                        cts.Dispose();
                    }
                });

                Func<Task> cancel = async () =>
                {
                    try
                    {
                        cts.Cancel();
                    }
                    catch (ObjectDisposedException)
                    {
                        // This exception can be thrown due to threading race conditions.
                        // In such a case, there's no reason to care. At this point, the task
                        // already completed (either successfully or with an error).
                        // We can return immediately.
                        return;
                    }

                    // If we get here, cancellation is triggered, but we must wait for it to complete.
                    await fetchingTask.ConfigureAwait(false);
                };

                return (fetchingTask, cancel);
            }
        }

        public bool SetData(T data)
        {
            return SetState(state => state with { QueryState = state.QueryState.WithManuallySetData(data) });
        }

        public bool SetError(Exception error)
        {
            return SetState(state => state with { QueryState = state.QueryState.WithManuallySetError(error) });
        }

        public Task CancelAsync()
        {
            return _state.Value.Cancel?.Invoke() ?? Task.CompletedTask;
        }

        /// <summary>
        ///     Synchronized.
        ///     
        ///     Disposes the shared query and sets its state to Disabled.
        ///     Notifies state listeners.
        /// </summary>
        public void Dispose()
        {
            var notify = false;

            lock (_lock)
            {
                if (_isDisposed)
                {
                    return;
                }

                var disposedState = new SharedQueryState(QueryState<T>.Disabled, FetchingTask: null, Cancel: null);
                notify = _state.SetValue(disposedState, suppressNotification: true);
                _isDisposed = true;
            }

            if (notify)
            {
                _state.Notify();
            }
        }

        /// <summary>
        ///     Synchronized.
        ///     
        ///     Sets the shared query's internal state and notifies listeners about the change.
        /// </summary>
        /// <param name="update">An updated function returning the new state.</param>
        /// <returns>
        ///     Whether the state could be set. This is false if the shared query has been disposed.
        /// </returns>
        private bool SetState(Func<SharedQueryState, SharedQueryState> update)
        {
            var notify = false;

            lock (_lock)
            {
                if (_isDisposed)
                {
                    return false;
                }

                notify = _state.SetValue(update(_state.Value), suppressNotification: true);
            }

            if (notify)
            {
                _state.Notify();
            }

            return true;
        }

        private sealed record SharedQueryState(QueryState<T> QueryState, Task? FetchingTask, Func<Task>? Cancel);
    }
}
