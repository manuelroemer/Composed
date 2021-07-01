namespace Composed.Query.Tests.Utilities
{
    using System;
    using Shouldly;

    public static class QueryAssertions
    {
        public static void ShouldBeInDisabledState<T>(this Query<T> query)
        {
            var state = query.State.Value;
            state.Key.ShouldBeNull();
            state.Status.ShouldBe(QueryStatus.Disabled);
            state.Data.ShouldBe(default);
            state.Error.ShouldBeNull();
        }

        public static void ShouldBeInLoadingState<T>(this Query<T> query, QueryKey key)
        {
            var state = query.State.Value;
            state.Key.ShouldBe(key);
            state.Status.ShouldBe(QueryStatus.Fetching);
            state.Data.ShouldBe(default);
            state.Error.ShouldBeNull();
        }

        public static void ShouldBeInSuccessState<T>(this Query<T> query, QueryKey key, object? data)
        {
            var state = query.State.Value;
            state.Key.ShouldBe(key);
            state.Status.ShouldBe(QueryStatus.Success);
            state.Data.ShouldBe(data);
            state.Error.ShouldBeNull();
        }

        public static void ShouldBeInErrorState<T>(this Query<T> query, QueryKey key, Exception? error)
        {
            var state = query.State.Value;
            state.Key.ShouldBe(key);
            state.Status.ShouldBe(QueryStatus.Error);
            state.Data.ShouldBe(default(T));
            state.Error.ShouldBe(error);
        }

        public static void ShouldBeInFetchingSuccessState<T>(this Query<T> query, QueryKey key, object? data)
        {
            var state = query.State.Value;
            state.Key.ShouldBe(key);
            state.Status.ShouldBe(QueryStatus.Fetching | QueryStatus.Success);
            state.Data.ShouldBe(data);
            state.Error.ShouldBeNull();
        }

        public static void ShouldBeInFetchingErrorState<T>(this Query<T> query, QueryKey key, Exception? error)
        {
            var state = query.State.Value;
            state.Key.ShouldBe(key);
            state.Status.ShouldBe(QueryStatus.Fetching | QueryStatus.Error);
            state.Data.ShouldBe(default(T));
            state.Error.ShouldBe(error);
        }
    }
}
