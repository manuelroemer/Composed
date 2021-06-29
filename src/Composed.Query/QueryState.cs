namespace Composed.Query
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;
    using Composed.Query.Internal;

    /// <summary>
    ///     Represents the current state of a <see cref="Query"/> instance.
    /// </summary>
    public abstract record QueryState
    {
        /// <summary>
        ///     Gets the <see cref="QueryKey"/> which uniquely identifies the query or
        ///     <see langword="null"/> if the query is disabled and therefore does not have
        ///     an associated query key.
        /// </summary>
        /// <remarks>
        ///     See <see cref="QueryStatus.Disabled"/> for details on disabled queries.
        /// </remarks>
        public QueryKey? Key { get; private init; }

        /// <summary>
        ///     Gets the current status of the query.
        /// </summary>
        /// <remarks>
        ///     See the different <see cref="QueryStatus"/> values for details on the possible
        ///     statuses of a query.
        /// </remarks>
        public QueryStatus Status { get; private init; }

        /// <summary>
        ///     Gets the data fetched by the query.
        ///     If the query hasn't loaded yet or if it encountered an error while fetching
        ///     the data, this is an empty default value (typically <see langword="null"/>).
        /// </summary>
        public object? Data { get; private init; }

        /// <summary>
        ///     Gets an error which was encountered while fetching data.
        ///     If the query hasn't loaded yet or if no error was encountered, this is <see langword="null"/>.
        /// </summary>
        public Exception? Error { get; private init; }

        /// <summary>
        ///     Gets a value indicating whether the query is currently disabled.
        ///     This is the case when <see cref="Status"/> is <see cref="QueryStatus.Disabled"/>.
        /// </summary>
        /// <remarks>
        ///     See <see cref="QueryStatus.Disabled"/> for details on disabled queries.
        /// </remarks>
        [MemberNotNullWhen(false, nameof(Key))]
        public bool IsDisabled => Status == QueryStatus.Disabled;

        /// <summary>
        ///     Gets a value indicating whether the query is currently fetching the initial data.
        ///     This is the case when <see cref="Status"/> is <see cref="QueryStatus.Loading"/>.
        /// </summary>
        /// <remarks>
        ///     See <see cref="QueryStatus.Loading"/> for details on the loading status.
        /// </remarks>
        [MemberNotNullWhen(false, nameof(Key))]
        public bool IsLoading => Status == QueryStatus.Loading;

        /// <summary>
        ///     Gets a value indicating whether the query is currently fetching any data.
        ///     This is the case when <see cref="Status"/> is <see cref="QueryStatus.Fetching"/>.
        /// </summary>
        /// <remarks>
        ///     See <see cref="QueryStatus.Fetching"/> for details on the fetching status.
        /// </remarks>
        [MemberNotNullWhen(false, nameof(Key))]
        public bool IsFetching => Status == QueryStatus.Fetching;

        /// <summary>
        ///     Gets a value indicating whether the query is currently idle.
        ///     This is the case when <see cref="Status"/> is <see cref="QueryStatus.Idle"/>.
        /// </summary>
        /// <remarks>
        ///     See <see cref="QueryStatus.Idle"/> for details on the idle status.
        /// </remarks>
        [MemberNotNullWhen(false, nameof(Key))]
        public bool IsIdle => Status == QueryStatus.Idle;

        /// <summary>
        ///     Gets a value indicating whether the query has fetched data.
        ///     This is the case when <see cref="Status"/> is not <see cref="QueryStatus.Loading"/>
        ///     and when <see cref="Error"/> is <see langword="null"/>.
        /// </summary>
        public bool HasData => !IsLoading && !HasError;

        /// <summary>
        ///     Gets a value indicating whether the query encountered an error while fetching data.
        ///     This is the case when <see cref="Error"/> is not <see langword="null"/>.
        /// </summary>
        [MemberNotNullWhen(true, nameof(Error))]
        public bool HasError => Error is not null;

        internal QueryState WithDisabled() =>
            this with
            {
                Key = null,
                Status = QueryStatus.Disabled,
                Data = null,
                Error = null,
            };

        internal QueryState WithNewlyEnabled(QueryKey key, UnifiedQueryState uqState) =>
            this with
            {
                Key = key,
                Status = uqState.Status,
                Data = uqState.LastData,
                Error = uqState.LastError,
            };

        internal QueryState WithUnifiedQueryStateUpdate(UnifiedQueryState uqState) =>
            this with
            {
                Status = uqState.Status,
                Data = uqState.LastData,
                Error = uqState.LastError,
            };
    }

    /// <inheritdoc/>
    /// <typeparam name="T">
    ///     The type of the data returned by the query.
    /// </typeparam>
    public sealed record QueryState<T> : QueryState
    {
        /// <inheritdoc cref="QueryState.Data"/>
        public new T? Data => base.Data is T t ? t : default;

        internal QueryState()
        {
        }

        protected override bool PrintMembers(StringBuilder builder)
        {
            // We don't want to print the shadowed data twice, hence the override.
            return base.PrintMembers(builder);
        }
    }
}
