namespace Composed.Query
{
    using System;

    /// <summary>
    ///     Defines the possible statuses of a query.
    ///     This is a flagged enum which allows several status combinations.
    ///     See the documentation of the respective flags for details on supported combinations.
    /// </summary>
    [Flags]
    public enum QueryStatus
    {
        /// <summary>
        ///     <para>
        ///         The query is disabled and won't trigger the query function at the moment.
        ///     </para>
        ///     <para>
        ///         This flag should not be combined with other flags.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     There are generally two ways for a query to end up disabled:
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
        Disabled = 1,

        /// <summary>
        ///     <para>
        ///         The query is currently fetching data using the query function.
        ///     </para>
        ///     <para>
        ///         This flag may be combined with <i>either</i> <see cref="Success"/> <i>or</i> <see cref="Error"/>.
        ///         If combined, the query is currently refetching data.
        ///         If not combined, the query is fetching the initial data (and is therefore in
        ///         an "Initial Loading" state).
        ///     </para>
        /// </summary>
        Fetching = 2,

        /// <summary>
        ///     <para>
        ///         The query has successfully resolved data using the query function.
        ///     </para>
        ///     <para>
        ///         This flag may be combined with <see cref="Fetching"/>.
        ///         If combined, the query is currently refetching data, but it has stale data which
        ///         can be worked with in the meantime.
        ///     </para>
        /// </summary>
        Success = 4,

        /// <summary>
        ///     <para>
        ///         The query has already attempted to resolve data, but the query function threw an error.
        ///     </para>
        ///     <para>
        ///         This flag may be combined with <see cref="Fetching"/>.
        ///         If combined, the query is currently refetching data, but it has a stale error which
        ///         can be worked with in the meantime.
        ///     </para>
        /// </summary>
        Error = 8,
    }
}
