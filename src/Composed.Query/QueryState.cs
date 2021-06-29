namespace Composed.Query
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    ///     Represents the current state of a <see cref="Query{T}"/> instance.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the data returned by the query.
    /// </typeparam>
    public sealed class QueryState<T> : IEquatable<QueryState<T>>
    {
        internal static readonly QueryState<T> Disabled = new(null, QueryStatus.Disabled, default, null);

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
        public T? Data { get; private init; }

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

        internal QueryState(QueryKey? key, QueryStatus status, T? data, Exception? error)
        {
            Key = key;
            Status = status;
            Data = data;
            Error = error;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) =>
            Equals(obj as QueryState<T>);

        /// <inheritdoc/>
        public bool Equals(QueryState<T>? other) =>
            other is not null &&
            Equals(other.Key, Key) &&
            Equals(other.Status, Status) &&
            Equals(other.Data, Data) &&
            Equals(other.Error, Error);

        /// <inheritdoc/>
        public override int GetHashCode() =>
            HashCode.Combine(Key, Status, Data, Error);

        /// <inheritdoc/>
        public override string ToString() =>
            $"{nameof(QueryState<T>)} {{ {nameof(Key)} = {Key}, {nameof(Status)} = {Status}, {nameof(Data)} = {Data}, {nameof(Error)} = {Error} }}";

        public static bool operator ==(QueryState<T>? left, QueryState<T>? right) =>
            left is null ? right is null : left.Equals(right);

        public static bool operator !=(QueryState<T>? left, QueryState<T>? right) =>
            !(left == right);
    }
}
