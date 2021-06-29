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

        public static void ShouldBeLoading<T>(this Query<T> query, QueryKey expectedKey)
        {
            var state = query.State.Value;
            state.Key.ShouldBe(expectedKey);
            state.Status.ShouldBe(QueryStatus.Loading);
            state.Data.ShouldBe(default);
            state.Error.ShouldBeNull();
        }

        public static void ShouldBeFetching<T>(this Query<T> query, QueryKey expectedKey, object? expectedData, Exception? expectedError)
        {
            var state = query.State.Value;
            state.Key.ShouldBe(expectedKey);
            state.Status.ShouldBe(QueryStatus.Fetching);
            state.Data.ShouldBe(expectedData);
            state.Error.ShouldBe(expectedError);
        }

        public static void ShouldBeIdle<T>(this Query<T> query, QueryKey expectedKey, object? expectedData, Exception? expectedError)
        {
            var state = query.State.Value;
            state.Key.ShouldBe(expectedKey);
            state.Status.ShouldBe(QueryStatus.Idle);
            state.Data.ShouldBe(expectedData);
            state.Error.ShouldBe(expectedError);
        }
    }
}
