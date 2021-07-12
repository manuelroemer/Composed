namespace Composed.Query.Internal
{
    using System;

    /// <summary>
    ///     The key which uniquely identifies a <see cref="SharedQuery{T}"/> in a <see cref="SharedQueryCache"/>.
    ///     Such a key generally consists of a query key and query function.
    /// </summary>
    internal sealed record SharedQueryCacheKey
    {
        public QueryKey QueryKey { get; init; }

        public Delegate QueryFunction { get; init; }

        private SharedQueryCacheKey(QueryKey queryKey, Delegate queryFunction)
        {
            QueryKey = queryKey;
            QueryFunction = queryFunction;
        }

        public static SharedQueryCacheKey Create<T>(QueryKey queryKey, CancelableQueryFunction<T> queryFunction) =>
            new SharedQueryCacheKey(queryKey, queryFunction);
    }
}
