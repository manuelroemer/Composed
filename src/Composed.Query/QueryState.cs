namespace Composed.Query
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    ///     Represents the current state of a <see cref="Query{T}"/> instance.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the data returned by the query.
    /// </typeparam>
    public sealed class QueryState<T> : IEquatable<QueryState<T>>
    {
        private static readonly List<QueryStatus> ValidStatuses = new()
        {
            QueryStatus.Disabled,
            QueryStatus.Fetching,
            QueryStatus.Success,
            QueryStatus.Error,
            QueryStatus.Fetching | QueryStatus.Success,
            QueryStatus.Fetching | QueryStatus.Error,
        };

        private readonly int _hashCode;

        /// <summary>
        ///     Gets the <see cref="QueryKey"/> which uniquely identifies the query or
        ///     <see langword="null"/> if the query is disabled and therefore does not have
        ///     an associated query key.
        /// </summary>
        /// <remarks>
        ///     See <see cref="QueryStatus.Disabled"/> for details on disabled queries.
        /// </remarks>
        public QueryKey? Key { get; }

        /// <summary>
        ///     Gets the current status of the query.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         See the different <see cref="QueryStatus"/> values for details on the possible
        ///         statuses of a query.
        ///     </para>
        ///     <para>
        ///         <b>Note:</b> For simple status checks, it is recommended to use the properties
        ///         of this class (like <see cref="IsFetching"/>) instead of this property.
        ///         Doing so allows you to avoid dealing with the <see cref="QueryStatus"/> enum flags.
        ///     </para>
        /// </remarks>
        public QueryStatus Status { get; }

        /// <summary>
        ///     Gets a value indicating whether the query has any data which can be processed (independent of whether
        ///     its <see cref="Status"/> is <see cref="QueryStatus.Success"/> or <see cref="QueryStatus.Error"/>).
        /// </summary>
        /// <returns>
        ///     <see langword="true"/> if <see cref="Data"/> is not <see langword="null"/>;
        ///     <see langword="false"/> otherwise.
        /// </returns>
        [MemberNotNullWhen(true, nameof(Key))]
        [MemberNotNullWhen(true, nameof(Data))]
        // ^ This is incorrect if T is nullable, but this is expected to be rare.
        // For all other cases, this is helpful (and true), hence the annotation is added despite potentially being wrong.
        public bool HasData { get; }

        /// <summary>
        ///     Gets the data fetched by the query.
        ///     If the query hasn't loaded yet or if it encountered an error while fetching
        ///     the data, this is an empty default value (typically <see langword="null"/>).
        /// </summary>
        public T? Data { get; }

        /// <summary>
        ///     Gets an error which was encountered while fetching data.
        ///     If the query hasn't loaded yet or if no error was encountered, this is <see langword="null"/>.
        /// </summary>
        public Exception? Error { get; }

        /// <summary>
        ///     Gets a value indicating whether the query is currently disabled.
        /// </summary>
        /// <remarks>
        ///     See <see cref="QueryStatus.Disabled"/> for details on disabled queries.
        /// </remarks>
        /// <returns>
        ///     <see langword="true"/> if <see cref="Status"/> is <see cref="QueryStatus.Disabled"/>;
        ///     <see langword="false"/> if not.
        /// </returns>
        [MemberNotNullWhen(false, nameof(Key))]
        public bool IsDisabled => Status == QueryStatus.Disabled;

        /// <summary>
        ///     Gets a value indicating whether the query is currently fetching the <b>initial</b> data.
        ///     In comparison to <see cref="IsFetching"/>, this only returns <see langword="true"/>
        ///     while the query is fetching the initial data; it returns <see langword="false"/> as
        ///     soon as the query has either stale data or a stale error.
        /// </summary>
        /// <remarks>
        ///     See <see cref="QueryStatus.Fetching"/> for details on the loading status.
        /// </remarks>
        /// <returns>
        ///     <see langword="true"/> if <see cref="Status"/> is <see cref="QueryStatus.Fetching"/>;
        ///     <see langword="false"/> if not.
        /// </returns>
        [MemberNotNullWhen(true, nameof(Key))]
        public bool IsLoading => Status == QueryStatus.Fetching;

        /// <summary>
        ///     Gets a value indicating whether the query is currently (re-)fetching data using the query function.
        ///     This is <see langword="true"/> when the query is fetching the initial data <i>and</i> when the query is
        ///     refetching data while already having stale data or a stale error.
        /// </summary>
        /// <remarks>
        ///     See <see cref="QueryStatus.Fetching"/> for details on the fetching status.
        /// </remarks>
        /// <returns>
        ///     <see langword="true"/> if <see cref="Status"/> has the <see cref="QueryStatus.Fetching"/> flag;
        ///     <see langword="false"/> if not.
        /// </returns>
        [MemberNotNullWhen(true, nameof(Key))]
        public bool IsFetching => Status.HasFlag(QueryStatus.Fetching);

        /// <summary>
        ///     Gets a value indicating whether the query has successfully fetched data.
        /// </summary>
        /// <returns>
        ///     <see langword="true"/> if <see cref="Status"/> has the <see cref="QueryStatus.Success"/> flag;
        ///     <see langword="false"/> if not.
        /// </returns>
        [MemberNotNullWhen(true, nameof(Key))]
        [MemberNotNullWhen(true, nameof(Data))]
        public bool IsSuccess => Status.HasFlag(QueryStatus.Success);

        /// <summary>
        ///     Gets a value indicating whether the query encountered an error while fetching data.
        /// </summary>
        /// <returns>
        ///     <see langword="true"/> if <see cref="Status"/> has the <see cref="QueryStatus.Error"/> flag;
        ///     <see langword="false"/> if not.
        /// </returns>
        [MemberNotNullWhen(true, nameof(Key))]
        [MemberNotNullWhen(true, nameof(Error))]
        public bool IsError => Status.HasFlag(QueryStatus.Error);

        /// <summary>
        ///     Gets a value indicating whether the query has an error while also not having any
        ///     stale data from previous fetching attempts.
        /// </summary>
        /// <returns>
        ///     <see langword="true"/> if <see cref="Data"/> is <see langword="null"/>
        ///     and <see cref="IsError"/> is <see langword="true"/>;
        ///     <see langword="false"/> otherwise.
        /// </returns>
        [MemberNotNullWhen(true, nameof(Key))]
        [MemberNotNullWhen(true, nameof(Error))]
        public bool HasErrorWithoutData => IsError && !HasData;

        /// <summary>
        ///     Initializes a new <see cref="QueryState{T}"/> instance with the given values.
        /// </summary>
        /// <param name="status">
        ///     The current status of the query.
        /// </param>
        /// <param name="key">
        ///     The <see cref="QueryKey"/> which uniquely identifies the query or
        ///     <see langword="null"/> if the query is disabled and therefore does not have
        ///     an associated query key.
        /// </param>
        /// <param name="hasData">
        ///     <para>
        ///         Whether the query has any data which can be processed (independent of whether
        ///         its <see cref="Status"/> is <see cref="QueryStatus.Success"/> or <see cref="QueryStatus.Error"/>).
        ///     </para>
        ///     <para>
        ///         If <see langword="true"/>, <paramref name="data"/> must be <see langword="default"/>.
        ///     </para>
        /// </param>
        /// <param name="data">
        ///     The data fetched by the query.
        /// </param>
        /// <param name="error">
        ///     An error which was encountered while fetching data.
        /// </param>
        /// <exception cref="ArgumentException">
        ///     Thrown for any invalid combination of the constructor arguments.
        /// </exception>
        public QueryState(QueryStatus status, QueryKey? key, bool hasData, T? data, Exception? error)
        {
            if (!ValidStatuses.Contains(status))
            {
                throw new InvalidEnumArgumentException(
                    $"The specified status is not a valid {nameof(QueryStatus)} combination."
                );
            }

            if (status == QueryStatus.Disabled)
            {
                if (key is not null)
                {
                    throw new ArgumentException("A query in the Disabled state must not have a key.", nameof(key));
                }

                if (hasData)
                {
                    throw new ArgumentException("A query in the Disabled state must not have any data.", nameof(hasData));
                }

                if (!Equals(data, default(T)))
                {
                    throw new ArgumentException("A query in the Disabled state must not have any data.", nameof(data));
                }

                if (error is not null)
                {
                    throw new ArgumentException("A query in the Disabled state must not have an error.", nameof(error));
                }
            }

            if (status != QueryStatus.Disabled)
            {
                if (key is null)
                {
                    throw new ArgumentNullException(nameof(key), "A query not in the Disabled state must have a key.");
                }
            }

            if (status == QueryStatus.Fetching)
            {
                if (hasData)
                {
                    throw new ArgumentException("A query in the initial Fetching state must not have any data.", nameof(hasData));
                }

                if (!Equals(data, default(T)))
                {
                    throw new ArgumentException("A query in the initial Fetching state must not have any data.", nameof(data));
                }

                if (error is not null)
                {
                    throw new ArgumentException("A query in the initial Fetching state must not have an error.", nameof(error));
                }
            }

            if (status.HasFlag(QueryStatus.Success))
            {
                if (!hasData)
                {
                    throw new ArgumentException("A query in the Success state must have data.", nameof(hasData));
                }

                if (error is not null)
                {
                    throw new ArgumentException("A query in the Success state must not have an error.", nameof(error));
                }
            }

            if (status.HasFlag(QueryStatus.Error))
            {
                if (error is null)
                {
                    throw new ArgumentNullException(nameof(error), "A query in the Error state must have an error.");
                }
            }

            if (!hasData && !Equals(data, default(T)))
            {
                throw new ArgumentException("A query's data must be the default data value if it doesn't have any data.", nameof(data));
            }

            Key = key;
            Status = status;
            HasData = hasData;
            Data = data;
            Error = error;
            _hashCode = HashCode.Combine(Status, Key, HasData, Data, Error);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) =>
            Equals(obj as QueryState<T>);

        /// <inheritdoc/>
        public bool Equals(QueryState<T>? other) =>
            other is not null &&
            Equals(other.Status, Status) &&
            Equals(other.Key, Key) &&
            Equals(other.HasData, HasData) &&
            Equals(other.Data, Data) &&
            Equals(other.Error, Error);

        /// <inheritdoc/>
        public override int GetHashCode() =>
            _hashCode;

        /// <inheritdoc/>
        public override string ToString() =>
            $"{nameof(QueryState<T>)} {{ {nameof(Status)} = {Status}, {nameof(Key)} = {Key}, {nameof(HasData)} = {HasData}, {nameof(Data)} = {Data}, {nameof(Error)} = {Error} }}";

        public static bool operator ==(QueryState<T>? left, QueryState<T>? right) =>
            left is null ? right is null : left.Equals(right);

        public static bool operator !=(QueryState<T>? left, QueryState<T>? right) =>
            !(left == right);


        // The following are state transitions and values which are used internally (mainly by the SharedQuery).
        // They are placed here so that the respective files are slimmer.

        internal static readonly QueryState<T> Disabled = new(QueryStatus.Disabled, null, false, default, null);

        internal static QueryState<T> Loading(QueryKey key) =>
            new QueryState<T>(QueryStatus.Fetching, key, false, default, null);

        internal QueryState<T> WithRefetching() =>
            new QueryState<T>(Status | QueryStatus.Fetching, Key, HasData, Data, Error);

        internal QueryState<T> WithSuccess(T data) =>
            new QueryState<T>(QueryStatus.Success, Key, true, data, null);

        internal QueryState<T> WithError(Exception error) =>
            new QueryState<T>(QueryStatus.Error, Key, HasData, Data, error);

        internal QueryState<T> WithManuallySetError(Exception error) =>
            new QueryState<T>(
                IsFetching
                    ? QueryStatus.Fetching | QueryStatus.Error
                    : QueryStatus.Error,
                Key,
                HasData,
                Data,
                error
            );

        internal QueryState<T> WithManuallySetData(T data) =>
            new QueryState<T>(
                IsFetching
                    ? QueryStatus.Fetching | QueryStatus.Success
                    : QueryStatus.Success,
                Key,
                true,
                data,
                null
            );
    }
}
