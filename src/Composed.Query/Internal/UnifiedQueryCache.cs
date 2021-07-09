namespace Composed.Query.Internal
{
    using System;
    using System.Collections.Generic;

    internal sealed class UnifiedQueryCache : IDisposable
    {
        private readonly object _lock = new();
        private readonly Dictionary<UnifiedQueryCacheKey, object> _cache = new();
        private bool _isDisposed;

        public UnifiedQuery<T> Get<T>(QueryKey queryKey, QueryFunction<T> queryFunction)
        {
            lock (_lock)
            {
                VerifyNotDisposed();

                var cacheKey = new UnifiedQueryCacheKey(queryKey, queryFunction);

                if (!_cache.ContainsKey(cacheKey))
                {
                    _cache.Add(cacheKey, new UnifiedQuery<T>(queryFunction));
                }

                return (UnifiedQuery<T>)_cache[cacheKey];
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

                foreach (IDisposable unifiedQuery in _cache.Values)
                {
                    unifiedQuery.Dispose();
                }

                _cache.Clear();
                _isDisposed = true;
            }
        }

        private void VerifyNotDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(
                    nameof(UnifiedQueryCache),
                    "The query client's internal query cache has been disposed. " +
                    "This happens when the query client itself is disposed. " +
                    "Using the associated query client is not possible anymore."
                );
            }
        }

        private sealed record UnifiedQueryCacheKey(QueryKey QueryKey, Delegate QueryFunction);
    }
}
