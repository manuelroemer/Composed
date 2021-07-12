namespace Composed.Query
{
    using System;
    using System.Reactive;
    using Composed.Query.Internal;

    /// <summary>
    ///     The <see cref="QueryClient"/> class creates and manages <see cref="Query{T}"/> objects.
    ///     Queries created via the same <see cref="QueryClient"/> instance benefit from
    ///     automatic data caching and query de-duplication.
    /// </summary>
    public sealed class QueryClient : IDisposable
    {
        private readonly QueryClientOptions _options;

        /// <summary>
        ///     Gets the internal cache where all queries are managed.
        /// </summary>
        internal SharedQueryCache SharedQueryCache { get; }

        /// <summary>
        ///     Initializes a new <see cref="QueryClient"/> instance with default options.
        /// </summary>
        public QueryClient()
            : this(null) { }

        /// <summary>
        ///     Initializes a new <see cref="QueryClient"/> instance with the specified <paramref name="options"/>.
        /// </summary>
        /// <param name="options">
        ///     Options which further configure the query client.
        ///     If <see langword="null"/>, default options are used.
        /// </param>
        public QueryClient(QueryClientOptions? options)
        {
            _options = (options ?? new()).CloneWithDefaultFallbacks();
            SharedQueryCache = new SharedQueryCache(_options.CacheInvalidatorFactory!);
        }

        /// <inheritdoc cref="CreateQuery{T}(QueryKey, CancelableQueryFunction{T})"/>
        public Query<T> CreateQuery<T>(QueryKey key, QueryFunction<T> queryFunction)
        {
            _ = key ?? throw new ArgumentNullException(nameof(key));
            _ = queryFunction ?? throw new ArgumentNullException(nameof(queryFunction));
            return CreateQuery(() => key, _ => queryFunction());
        }

        /// <summary>
        ///     Creates and returns a new <see cref="Query{T}"/> with a query key that does not
        ///     change over the lifetime of the query.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of the data returned by the query.
        /// </typeparam>
        /// <param name="key">
        ///     The query key which uniquely identifies the query.
        /// </param>
        /// <param name="queryFunction">
        ///     The query function which, when invoked, fetches the query's data.
        /// </param>
        /// <returns>
        ///     A new <see cref="Query{T}"/> instance which (re-)fetches its data.
        /// </returns>
        public Query<T> CreateQuery<T>(QueryKey key, CancelableQueryFunction<T> queryFunction)
        {
            _ = key ?? throw new ArgumentNullException(nameof(key));
            return CreateQuery(() => key, queryFunction);
        }

        /// <inheritdoc cref="CreateQuery{T}(QueryKeyProvider, CancelableQueryFunction{T}, IObservable{Unit}[])"/>
        public Query<T> CreateQuery<T>(
            QueryKeyProvider getKey,
            QueryFunction<T> queryFunction,
            params IObservable<Unit>[] dependencies
        )
        {
            _ = queryFunction ?? throw new ArgumentNullException(nameof(queryFunction));
            return CreateQuery(getKey, _ => queryFunction(), dependencies);
        }

        /// <summary>
        ///     Creates and returns a new <see cref="Query{T}"/> whose query key is automatically
        ///     reevaluated over its lifetime whenever one of the given <paramref name="dependencies"/>
        ///     changes.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of the data returned by the query.
        /// </typeparam>
        /// <param name="getKey">
        ///     A <see cref="QueryKeyProvider"/> function which, when invoked, returns the query key
        ///     to be used by the query.
        /// </param>
        /// <param name="queryFunction">
        ///     The query function which, when invoked, fetches the query's data.
        /// </param>
        /// <param name="dependencies">
        ///     <para>
        ///         A set of dependencies which will be watched for changes and, when changed, trigger
        ///         a reevaluation of the <paramref name="getKey"/> function.
        ///         This can be any kind of observable. <see cref="IRef{T}"/> and <see cref="IReadOnlyRef{T}"/> instances
        ///         implement <see cref="IObservable{T}"/> and can directly be passed as dependencies.
        ///     </para>
        ///     <para>
        ///         If this is empty, the query's key will never change over the query's lifetime.
        ///     </para>
        /// </param>
        /// <returns>
        ///     A new <see cref="Query{T}"/> instance which (re-)fetches its data.
        /// </returns>
        public Query<T> CreateQuery<T>(
            QueryKeyProvider getKey,
            CancelableQueryFunction<T> queryFunction,
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
            SharedQueryCache.Dispose();
    }
}
