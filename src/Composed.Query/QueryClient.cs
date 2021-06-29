namespace Composed.Query
{
    using System;
    using System.Linq;
    using System.Reactive;
    using Composed.Query.Internal;

    public sealed class QueryClient
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

        public Query<T> CreateQuery<T>(QueryKeyProvider getKey, QueryFunction<T> queryFunction)
        {
            return CreateQuery(getKey, queryFunction, dependencies: Array.Empty<IObservable<Unit>>());
        }

        public Query<T> CreateQuery<T>(
            QueryKeyProvider getKey,
            QueryFunction<T> queryFunction,
            params Query[] dependencies
        )
        {
            _ = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
            var refDependencies = dependencies.Select(q => q.State);
            return CreateQuery(getKey, queryFunction, refDependencies.ToArray());
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
    }
}
