namespace Composed.Query.Internal
{
    using System.Collections.Concurrent;

    internal sealed record UnifiedQueryCacheKey(QueryKey QueryKey, QueryFunction QueryFunction);

    internal sealed class UnifiedQueryCache
    {
        private readonly ConcurrentDictionary<UnifiedQueryCacheKey, UnifiedQuery> _cache = new();

        public UnifiedQuery GetOrAdd(QueryKey queryKey, QueryFunction queryFunction)
        {
            var cacheKey = new UnifiedQueryCacheKey(queryKey, queryFunction);
            return _cache.GetOrAdd(cacheKey, (_) => new UnifiedQuery(queryFunction));
        }
    }
}
