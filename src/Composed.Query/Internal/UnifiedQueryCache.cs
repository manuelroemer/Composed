namespace Composed.Query.Internal
{
    using System;
    using System.Collections.Generic;

    internal sealed class UnifiedQueryCache
    {
        // There's a reason for not using a ConcurrentDictionary here:
        // Adding a new UnifiedQuery atomically is not possible with a ConcurrentDictionary, i.e.
        // it could always happen that multiple new UnifiedQuery instances get created before one
        // is being added to the dictionary.
        // This should be avoided as creating a query immediately starts it.
        //
        // It's a bad reason which could be circumvented by allowing UnifiedQueries in an initial Disabled state,
        // but for the moment it's more than sufficient and simply the easiest way to do this.
        // May/Should be reworked in the future.
        private readonly Dictionary<UnifiedQueryCacheKey, object> _cache = new();

        public UnifiedQuery<T> Get<T>(QueryKey queryKey, QueryFunction<T> queryFunction)
        {
            lock (_cache)
            {
                var cacheKey = new UnifiedQueryCacheKey(queryKey, queryFunction);

                if (!_cache.ContainsKey(cacheKey))
                {
                    _cache.Add(cacheKey, new UnifiedQuery<T>(queryFunction));
                }

                return (UnifiedQuery<T>)_cache[cacheKey];
            }
        }

        private sealed record UnifiedQueryCacheKey(QueryKey QueryKey, Delegate QueryFunction);
    }
}
