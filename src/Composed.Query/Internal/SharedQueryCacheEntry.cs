namespace Composed.Query.Internal
{
    using System;

    internal class SharedQueryCacheEntry : IDisposable
    {
        public SharedQueryCacheKey Key { get; }

        public IDisposable SharedQuery { get; }

        public SharedQueryLifetimeManager SharedQueryLifetimeManager { get; }

        private protected SharedQueryCacheEntry(
            SharedQueryCacheKey key,
            IDisposable sharedQuery,
            SharedQueryLifetimeManager sharedQueryLifetimeManager
        )
        {
            Key = key;
            SharedQuery = sharedQuery;
            SharedQueryLifetimeManager = sharedQueryLifetimeManager;
        }

        public void Dispose()
        {
            SharedQuery.Dispose();
            SharedQueryLifetimeManager.Dispose();
        }
    }

    internal sealed class SharedQueryCacheEntry<T> : SharedQueryCacheEntry
    {
        public new SharedQuery<T> SharedQuery => (SharedQuery<T>)base.SharedQuery;

        private SharedQueryCacheEntry(
            SharedQueryCacheKey key,
            SharedQuery<T> sharedQuery,
            SharedQueryLifetimeManager sharedQueryLifetimeManager
        ) : base(key, sharedQuery, sharedQueryLifetimeManager)
        {
        }

        public static SharedQueryCacheEntry<T> Create(
            SharedQueryCacheKey key,
            QueryKey queryKey,
            CancelableQueryFunction<T> queryFunction,
            QueryCacheInvalidator queryCacheInvalidator,
            Action removeFromCache
        )
        {
            var sharedQuery = new SharedQuery<T>(queryKey, queryFunction);
            var sharedQueryLifetimeManager = new SharedQueryLifetimeManager(queryCacheInvalidator, removeFromCache);
            return new SharedQueryCacheEntry<T>(key, sharedQuery, sharedQueryLifetimeManager);
        }
    }
}
