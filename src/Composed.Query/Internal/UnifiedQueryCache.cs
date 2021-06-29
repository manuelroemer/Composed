namespace Composed.Query.Internal
{
    using System;
    using System.Collections.Concurrent;

    internal sealed class UnifiedQueryCache
    {
        private readonly ConcurrentDictionary<UnifiedQueryCacheKey, object> _cache = new();

        public UnifiedQuery<T> GetOrAdd<T>(QueryKey queryKey, QueryFunction<T> queryFunction)
        {
            var cacheKey = new UnifiedQueryCacheKey(queryKey, queryFunction);
            return (UnifiedQuery<T>)_cache.GetOrAdd(cacheKey, (_) => new UnifiedQuery<T>(queryFunction));
        }

        private sealed record UnifiedQueryCacheKey(QueryKey QueryKey, Delegate QueryFunction);
    }
}
