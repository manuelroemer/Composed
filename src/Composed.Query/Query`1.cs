namespace Composed.Query
{
    using System;
    using System.Reactive;
    using Composed;
    using static Composed.Compose;

    /// <inheritdoc/>
    /// <typeparam name="T">
    ///     The type of the data returned by the query.
    /// </typeparam>
    public class Query<T> : Query
    {
        public new IReadOnlyRef<QueryState<T>> State { get; }

        internal Query(
            QueryClient client,
            QueryKeyProvider getKey,
            QueryFunction<T> queryFunction,
            IObservable<Unit>[] dependencies
        ) : base(client, getKey, WrapQueryFunction(queryFunction), dependencies, new QueryState<T>())
        {
            State = Computed(() => (QueryState<T>)base.State.Value, base.State);
        }

        private static QueryFunction WrapQueryFunction(QueryFunction<T> queryFunction)
        {
            _ = queryFunction ?? throw new ArgumentNullException(nameof(queryFunction));
            return async () => await queryFunction().ConfigureAwait(false);
        }
    }
}
