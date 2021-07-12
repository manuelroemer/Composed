namespace Composed.Query.Internal
{
    using System;
    using System.Diagnostics;

    /// <summary>
    ///     Manages the lifetime of a shared query.
    ///     A shared query generally lives while it has active subscribers.
    ///     Once no subscribers exist anymore, a <see cref="QueryCacheInvalidator"/> is used to
    ///     determine when the shared query is evicted from the cache.
    ///
    ///     If the <see cref="QueryCacheInvalidator"/> delays eviction, the shared query has the
    ///     chance to be "resurrected" by re-subscribing to it.
    ///
    ///     This class handles the tracking of active subscribers and the interaction with
    ///     the <see cref="QueryCacheInvalidator"/>.
    /// </summary>
    internal sealed class SharedQueryLifetimeManager : IDisposable
    {
        private readonly object _lock = new();
        private readonly QueryCacheInvalidator _queryCacheInvalidator;
        private readonly Action _removeFromCache;
        private uint _subscribers;
        private bool _isInvalidated;

        public SharedQueryLifetimeManager(QueryCacheInvalidator queryCacheInvalidator, Action removeFromCache)
        {
            _queryCacheInvalidator = queryCacheInvalidator;
            _removeFromCache = removeFromCache;
        }

        public void OnSubscribed()
        {
            lock (_lock)
            {
                Debug.Assert(!_isInvalidated);

                _subscribers++;

                if (_subscribers == 1)
                {
                    _queryCacheInvalidator.OnQueryReactivated();
                } 
            }
        }

        public void OnUnsubscribed()
        {
            lock (_lock)
            {
                Debug.Assert(!_isInvalidated);
                Debug.Assert(_subscribers != 0);

                _subscribers--;

                if (_subscribers == 0)
                {
                    _queryCacheInvalidator.OnQueryDeactivated(TryInvalidateQuery);
                }
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                Invalidate();
            }
        }

        private bool TryInvalidateQuery()
        {
            // This function is the reason for having locking in this class.
            // This is a callback which can be invoked from anywhere at any time.

            lock (_lock)
            {
                if (_isInvalidated)
                {
                    // No need to invalidate twice.
                    // Return true because the goal (invalidating the query) is fulfilled.
                    return true;
                }

                if (_subscribers == 0)
                {
                    Invalidate();
                    return true;
                }

                return false;
            }
        }

        private void Invalidate()
        {
            _removeFromCache();
            _isInvalidated = true;
        }
    }
}
