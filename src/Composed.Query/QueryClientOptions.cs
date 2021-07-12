namespace Composed.Query
{
    using System;
    using System.Reactive.Concurrency;

    /// <summary>
    ///     Defines configuration options for the <see cref="QueryClient"/> class.
    /// </summary>
    public sealed class QueryClientOptions
    {
        /// <summary>
        ///     <para>
        ///         Gets or sets a factory function which creates an <see cref="QueryCacheInvalidator"/>
        ///         for queries created by the query client.
        ///         The function receives the <see cref="QueryKey"/> of the query for which an
        ///         <see cref="QueryCacheInvalidator"/> should be created.
        ///     </para>
        ///     <para>
        ///         If <see langword="null"/>, this returns a function which creates
        ///         <see cref="TimedQueryCacheInvalidator"/> instances which keep cached a query's data
        ///         in the cache for <b>5 minutes</b> before invalidating it.
        ///     </para>
        /// </summary>
        public QueryCacheInvalidatorFactory? CacheInvalidatorFactory { get; set; }

        /// <summary>
        ///     <para>
        ///         Gets or sets an <see cref="IScheduler"/> which is used by each <see cref="Query{T}"/>
        ///         created by the query client to schedule changes to the <see cref="Query{T}.State"/> property.
        ///     </para>
        ///     <para>
        ///         If <see langword="null"/>, this is <see cref="ImmediateScheduler.Instance"/>.
        ///     </para>
        /// </summary>
        public IScheduler? DefaultQueryScheduler { get; set; }

        internal QueryClientOptions CloneWithDefaultFallbacks() => new()
        {
            CacheInvalidatorFactory = CacheInvalidatorFactory ?? FallbackCacheInvalidatorFactory,
            DefaultQueryScheduler = DefaultQueryScheduler ?? FallbackDefaultQueryScheduler,
        };

        private static readonly QueryCacheInvalidatorFactory FallbackCacheInvalidatorFactory =
            (static _ => new TimedQueryCacheInvalidator(TimeSpan.FromMinutes(5)));

        private static readonly IScheduler? FallbackDefaultQueryScheduler = ImmediateScheduler.Instance;
    }
}
