namespace Composed.Query.Tests.Utilities
{
    using System;
    using Shouldly;

    public static class QueryAssertions
    {
        public static void ShouldBeDisabled(this Query query)
        {
            var state = query.State.Value;
            state.Key.ShouldBeNull();
            state.Status.ShouldBe(QueryStatus.Disabled);
            state.Data.ShouldBe(default);
            state.Error.ShouldBeNull();
        }

        public static void ShouldBeLoading(this Query query, QueryKey expectedKey)
        {
            var state = query.State.Value;
            state.Key.ShouldBe(expectedKey);
            state.Status.ShouldBe(QueryStatus.Loading);
            state.Data.ShouldBe(default);
            state.Error.ShouldBeNull();
        }

        public static void ShouldBeFetching(this Query query, QueryKey expectedKey, object? expectedData, Exception? expectedError)
        {
            var state = query.State.Value;
            state.Key.ShouldBe(expectedKey);
            state.Status.ShouldBe(QueryStatus.Fetching);
            state.Data.ShouldBe(expectedData);
            state.Error.ShouldBe(expectedError);
        }

        public static void ShouldBeIdle(this Query query, QueryKey expectedKey, object? expectedData, Exception? expectedError)
        {
            var state = query.State.Value;
            state.Key.ShouldBe(expectedKey);
            state.Status.ShouldBe(QueryStatus.Idle);
            state.Data.ShouldBe(expectedData);
            state.Error.ShouldBe(expectedError);
        }
    }
}
