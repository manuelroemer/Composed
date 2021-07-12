namespace Composed.Query
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    ///     Represents a query function which fetches arbitrary data of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the data returned by the query.
    /// </typeparam>
    /// <returns>
    ///     A task which either resolve's the query's result of type <typeparamref name="T"/> or
    ///     results in an exception when fetching the data failed.
    /// </returns>
    public delegate Task<T> QueryFunction<T>();

    /// <summary>
    ///     Represents a query function which fetches arbitrary data of <typeparamref name="T"/>.
    ///     The query function receives a <see cref="CancellationToken"/> which propagates the
    ///     cancellation of the associated query.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the data returned by the query.
    /// </typeparam>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken"/> which propagates cancellation when the associated
    ///     query is canceled.
    /// </param>
    /// <returns>
    ///     A task which either resolve's the query's result of type <typeparamref name="T"/> or
    ///     results in an exception when fetching the data failed.
    /// </returns>
    public delegate Task<T> CancelableQueryFunction<T>(CancellationToken cancellationToken);

    /// <summary>
    ///     <para>
    ///         A function which returns a <see cref="QueryKey"/> for a query.
    ///         This function is invoked whenever the query's dependencies change.
    ///     </para>
    ///     <para>
    ///         If no query key can be provided at the moment (for example because some required dependency
    ///         is missing at the moment), the function may return <see langword="null"/> or alternatively
    ///         throw <i>any</i> exception.
    ///         Doing so results in the query being effectively disabled until a valid query key is provided.
    ///     </para>
    /// </summary>
    /// <returns>
    ///     A <see cref="QueryKey"/> which uniquely identifies the query or <see langword="null"/>
    ///     if no query key can be provided at the moment.
    /// </returns>
    /// <exception cref="Exception">
    ///     The provider is allowed to throw any kind of exception.
    ///     Doing so is equivalent to returning <see langword="null"/>.
    /// </exception>
    public delegate QueryKey? QueryKeyProvider();

    /// <summary>
    ///     A factory function which creates an <see cref="QueryCacheInvalidator"/> which is used
    ///     to invalidate cached query data once a query is deactivated.
    /// </summary>
    /// <param name="key">
    ///     The query key of the query for which an <see cref="QueryCacheInvalidator"/> should
    ///     be returned.
    ///     This can be used to switch between different invalidation strategies for different queries.
    /// </param>
    /// <returns>
    ///     The <see cref="QueryCacheInvalidator"/> instance to be used.
    /// </returns>
    public delegate QueryCacheInvalidator QueryCacheInvalidatorFactory(QueryKey key);
}
