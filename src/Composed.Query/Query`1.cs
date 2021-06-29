namespace Composed.Query
{
    using System;
    using System.Reactive;
    using Composed;
    using static Composed.Compose;

    public class Query<T> : Query
    {
        public new IReadOnlyRef<T?> Data { get; }

        internal Query(
            QueryClient client,
            QueryKeyProvider getKey,
            QueryFunction<T> queryFunction,
            IObservable<Unit>[] dependencies
        ) : base(client, getKey, async () => await queryFunction().ConfigureAwait(false), dependencies)
        {
            Data = Computed(() => base.Data.Value is null ? default : (T)base.Data.Value, base.Data);
        }
    }
}
