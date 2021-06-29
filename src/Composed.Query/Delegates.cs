namespace Composed.Query
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    ///     Represents a query function which fetches arbitrary data.
    /// </summary>
    /// <returns>
    ///     A task which either resolve's the query's result or
    ///     results in an exception when fetching the data failed.
    /// </returns>
    public delegate Task<object?> QueryFunction();

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
}
