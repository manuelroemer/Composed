namespace Composed.Query
{
    /// <summary>
    ///     Defines the possible statuses of a query.
    /// </summary>
    public enum QueryStatus
    {
        /// <summary>
        ///     The query is disabled and won't trigger the query function at the moment.
        /// </summary>
        /// <remarks>
        ///     There are two ways for a query to end up disabled:
        ///
        ///     <list type="number">
        ///         <item>
        ///             <description>
        ///                 The query gets disposed.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 The query was created using a <see cref="QueryKeyProvider"/> function
        ///                 which either returned <see langword="null"/> as the <see cref="QueryKey"/>
        ///                 or threw an exception.
        ///                 Either way, this resulted in the query not having a key which effectively
        ///                 disables it.
        ///                 Queries disabled this way are automatically enabled when the
        ///                 <see cref="QueryKeyProvider"/> returns a valid key.
        ///             </description>
        ///         </item>
        ///     </list>
        /// </remarks>
        Disabled,

        /// <summary>
        ///     The query is currently fetching data using the query function.
        ///     In comparison to <see cref="Fetching"/>, the query <i>does not</i> have stale
        ///     data or a stale error, i.e. the query is currently fetching data for the first time.
        /// </summary>
        Loading,

        /// <summary>
        ///     The query is currently fetching data using the query function.
        ///     In comparison to <see cref="Loading"/>, the query <i>does</i> have stale
        ///     data or a stale error.
        /// </summary>
        Fetching,

        /// <summary>
        ///     The query has stale data or a stale error, but it is not actively fetching any
        ///     new data at the moment.
        /// </summary>
        Idle,
    }
}
