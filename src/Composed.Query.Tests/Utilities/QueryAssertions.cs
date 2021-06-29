namespace Composed.Query.Tests.Utilities
{
    using System;
    using Shouldly;

    public static class QueryAssertions
    {
        public static void ShouldBeDisabled<T>(this Query<T> query)
        {
            var state = query.State.Value;
            state.Key.ShouldBeNull();
            state.Status.ShouldBe(QueryStatus.Disabled);
            state.Data.ShouldBe(default);
            state.Error.ShouldBeNull();
        }

        public static void ShouldBeLoading<T>(this Query<T> query, QueryKey key)
        {
            var state = query.State.Value;
            state.Key.ShouldBe(key);
            state.Status.ShouldBe(QueryStatus.Loading);
            state.Data.ShouldBe(default);
            state.Error.ShouldBeNull();
        }

        public static void ShouldBeFetching<T>(this Query<T> query, QueryKey key, object? data, Exception? error)
        {
            var state = query.State.Value;
            state.Key.ShouldBe(key);
            state.Status.ShouldBe(QueryStatus.Fetching);
            state.Data.ShouldBe(data);
            state.Error.ShouldBe(error);
        }

        public static void ShouldBeIdle<T>(this Query<T> query, QueryKey key, object? data, Exception? error)
        {
            var state = query.State.Value;
            state.Key.ShouldBe(key);
            state.Status.ShouldBe(QueryStatus.Idle);
            state.Data.ShouldBe(data);
            state.Error.ShouldBe(error);
        }
    }
}
