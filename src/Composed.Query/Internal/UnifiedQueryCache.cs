namespace Composed.Query.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Disposables;

    internal sealed class UnifiedQueryCache : IDisposable
    {
        private readonly object _lock = new();
        private readonly Dictionary<UnifiedQueryCacheKey, UnifiedQueryCacheEntry> _cache = new();
        private bool _isDisposed;

        public (UnifiedQuery<T>, IDisposable) Rent<T>(QueryKey queryKey, QueryFunction<T> queryFunction)
        {
            lock (_lock)
            {
                VerifyNotDisposed();

                var entry = GetOrAdd(queryKey, queryFunction);
                var unsubscribe = Disposable.Create(() =>
                {
                    lock (_lock)
                    {
                        entry.ActiveSubscribers--;
                    }
                });

                entry.ActiveSubscribers++;
                return (entry.UnifiedQuery, unsubscribe);
            }
        }

        private UnifiedQueryCacheEntry<T> GetOrAdd<T>(QueryKey queryKey, QueryFunction<T> queryFunction)
        {
            var cacheKey = new UnifiedQueryCacheKey(queryKey, queryFunction);

            if (!_cache.ContainsKey(cacheKey))
            {
                var unifiedQuery = new UnifiedQuery<T>(queryKey, queryFunction);
                _cache.Add(cacheKey, new UnifiedQueryCacheEntry<T>(unifiedQuery));
            }
            
            return (UnifiedQueryCacheEntry<T>)_cache[cacheKey];
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
                    entry.UnifiedQuery.Dispose();
                }

                _cache.Clear();
            }
        }

        private void VerifyNotDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(UnifiedQueryCache));
            }
        }

        private record UnifiedQueryCacheKey(QueryKey QueryKey, Delegate QueryFunction);

        private class UnifiedQueryCacheEntry
        {
            public IDisposable UnifiedQuery { get; }

            public int ActiveSubscribers { get; set; }

            public UnifiedQueryCacheEntry(IDisposable unifiedQuery)
            {
                UnifiedQuery = unifiedQuery;
            }
        }

        private class UnifiedQueryCacheEntry<T> : UnifiedQueryCacheEntry
        {
            public new UnifiedQuery<T> UnifiedQuery => (UnifiedQuery<T>)base.UnifiedQuery;

            public UnifiedQueryCacheEntry(UnifiedQuery<T> unifiedQuery)
                : base(unifiedQuery) { }
        }
    }
}
