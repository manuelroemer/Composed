namespace Composed.Query.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Disposables;

    internal sealed class SharedQueryCache : IDisposable
    {
        private readonly object _lock = new();
        private readonly Dictionary<SharedQueryCacheKey, SharedQueryCacheEntry> _cache = new();
        private readonly QueryCacheInvalidatorFactory _queryCacheInvalidatorFactory;
        private bool _isDisposed;

        public SharedQueryCache(QueryCacheInvalidatorFactory queryCacheInvalidatorFactory)
        {
            _queryCacheInvalidatorFactory = queryCacheInvalidatorFactory;
        }

        public (SharedQuery<T> SharedQuery, IDisposable Unsubscribe) Rent<T>(
            QueryKey queryKey,
            CancelableQueryFunction<T> queryFunction
        )
        {
            lock (_lock)
            {
                VerifyNotDisposed();

                var entry = GetOrAddEntry(queryKey, queryFunction);
                var unsubscribe = Disposable.Create(() => entry.SharedQueryLifetimeManager.OnUnsubscribed());
                entry.SharedQueryLifetimeManager.OnSubscribed();
                return (entry.SharedQuery, unsubscribe);
            }
        }

        private SharedQueryCacheEntry<T> GetOrAddEntry<T>(QueryKey queryKey, CancelableQueryFunction<T> queryFunction)
        {
            var cacheKey = SharedQueryCacheKey.Create(queryKey, queryFunction);

            if (!_cache.ContainsKey(cacheKey))
            {
                var queryKeyInvalidator = _queryCacheInvalidatorFactory(queryKey);
                var newCacheEntry = SharedQueryCacheEntry<T>.Create(
                    cacheKey,
                    queryKey,
                    queryFunction,
                    queryKeyInvalidator,
                    () => RemoveFromCache(cacheKey)
                );

                _cache.Add(cacheKey, newCacheEntry);
            }

            return (SharedQueryCacheEntry<T>)_cache[cacheKey];
        }

        private void RemoveFromCache(SharedQueryCacheKey cacheKey)
        {
            // Invoked from callback.
            lock (_lock)
            {
                _cache.Remove(cacheKey);
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_isDisposed)
                {
                    return;
                }

                _isDisposed = true;

                foreach (var entry in _cache.Values)
                {
                    entry.SharedQuery.Dispose();
                }

                _cache.Clear();
            }
        }

        private void VerifyNotDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(SharedQueryCache));
            }
        }
    }
}
