namespace Composed.Query
{
    using System;
    using System.Reactive;
    using Composed.Query.Internal;

    public sealed class QueryClient : IDisposable
    {
        /// <summary>
        ///     Gets the internal cache where all queries are managed.
        /// </summary>
        internal UnifiedQueryCache UnifiedQueryCache { get; } = new();

        public Query<T> CreateQuery<T>(QueryKey key, QueryFunction<T> queryFunction)
        {
            _ = key ?? throw new ArgumentNullException(nameof(key));
            return CreateQuery(() => key, queryFunction);
        }

        public Query<T> CreateQuery<T>(
            QueryKeyProvider getKey,
            QueryFunction<T> queryFunction,
            params IObservable<Unit>[] dependencies
        )
        {
            _ = getKey ?? throw new ArgumentNullException(nameof(getKey));
            _ = queryFunction ?? throw new ArgumentNullException(nameof(queryFunction));
            _ = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
            return new Query<T>(this, getKey, queryFunction, dependencies);
        }

        /// <summary>
        ///     Disposes any resources used by this <see cref="QueryClient"/> instance.
        ///     Disposing disables any active query created by the client and clears the entire
        ///     query cache.
        /// </summary>
        public void Dispose() =>
            UnifiedQueryCache.Dispose();
    }
}
