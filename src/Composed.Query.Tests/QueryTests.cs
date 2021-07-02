namespace Composed.Query.Tests
{
    using System;
    using System.Threading.Tasks;
    using Composed.Query.Tests.Utilities;
    using Shouldly;
    using Xunit;

    public class QueryTests
    {
        private static QueryKey GetKey(int query = 1) =>
            new QueryKey($"Query{query}");

        public static TheoryData<QueryKeyProvider> EnabledQueryKeyProviderData => new()
        {
            () => GetKey(),
        };

        public static TheoryData<QueryKeyProvider> DisabledQueryKeyProviderData => new()
        {
            () => null,
            () => throw new InvalidOperationException(),
        };

        #region Deconstruct Tests

        [Fact]
        public void Deconstruct_QueryAndState_Deconstructs()
        {
            var client = new QueryClient();
            using var originalQuery = client.CreateQuery(GetKey(), () => Task.FromResult(0));
            var (query, state) = originalQuery;

            query.ShouldBeSameAs(originalQuery);
            state.ShouldBeSameAs(originalQuery.State);
        }

        #endregion

        #region ToString Tests

        [Fact]
        public void ToString_ReturnsQueryStateStringRepresentation()
        {
            var client = new QueryClient();
            using var query = client.CreateQuery(GetKey(), () => Task.FromResult(0));
            query.ToString().ShouldBe(query.State.Value.ToString());
        }

        #endregion

        #region Initialization Tests

        [Theory]
        [MemberData(nameof(EnabledQueryKeyProviderData))]
        public void Initialization_EnabledQueryKeyProvider_InitializesQueryInLoadingState(QueryKeyProvider queryKeyProvider)
        {
            var controller = new QueryFunctionController<int>();
            var client = new QueryClient();
            using var query = client.CreateQuery(queryKeyProvider, controller.Function);

            query.ShouldBeInLoadingState(key: queryKeyProvider()!);
            controller.Verify(1);
        }

        [Theory]
        [MemberData(nameof(DisabledQueryKeyProviderData))]
        public void Initialization_DisabledQueryKeyProvider_InitializesQueryInDisabledState(QueryKeyProvider queryKeyProvider)
        {
            var controller = new QueryFunctionController<int>();
            var client = new QueryClient();
            using var query = client.CreateQuery(queryKeyProvider, controller.Function);

            query.ShouldBeInDisabledState();
            controller.Verify(0);
        }

        #endregion

        #region Query Flows

        [Fact]
        public async Task SingleQuery_WithAllStateTransitions_HasExpectedLifecycle()
        {
            var controller = new QueryFunctionController<int>();
            var client = new QueryClient();
            using var query = client.CreateQuery(GetKey(), controller.Function);
            var error = new Exception();

            // -> Loading
            query.ShouldBeInLoadingState(key: GetKey());
            controller.Verify(1);

            // Loading -> Success
            await controller.ReturnAndWaitForStateChange(query, 123);

            query.ShouldBeInSuccessState(key: GetKey(), data: 123);
            controller.Verify(1);

            // Success -> FetchingSuccess
            controller.Reset();
            query.Refetch();

            query.ShouldBeInFetchingSuccessState(key: GetKey(), data: 123);
            controller.Verify(2);

            // FetchingSuccess -> Success
            await controller.ReturnAndWaitForStateChange(query, 456);

            query.ShouldBeInSuccessState(key: GetKey(), data: 456);
            controller.Verify(2);

            // Success -> FetchingSuccess
            controller.Reset();
            query.Refetch();

            query.ShouldBeInFetchingSuccessState(key: GetKey(), data: 456);
            controller.Verify(3);

            // FetchingSuccess -> Error
            await controller.ThrowAndWaitForStateChange(query, error);

            query.ShouldBeInErrorState(key: GetKey(), error);
            controller.Verify(3);

            // Error -> FetchingError
            controller.Reset();
            query.Refetch();

            query.ShouldBeInFetchingErrorState(key: GetKey(), error);
            controller.Verify(4);

            // FetchingError -> Success
            await controller.ReturnAndWaitForStateChange(query, 789);

            query.ShouldBeInSuccessState(key: GetKey(), 789);
            controller.Verify(4);

            // Success -> Disabled
            query.Dispose();
            query.Refetch(); // Should not change the state after disposal.

            query.ShouldBeInDisabledState();
            controller.Verify(4);
        }

        [Fact]
        public async Task TwoQueries_OneQueryMakingStateTransitions_HaveSharedState()
        {
            var controller = new QueryFunctionController<int>();
            var client = new QueryClient();
            using var controlledQuery = client.CreateQuery(GetKey(), controller.Function);
            using var influencedQuery = client.CreateQuery(GetKey(), controller.Function);
            var queries = new[] { controlledQuery, influencedQuery };
            var error = new Exception();

            // -> Loading
            controlledQuery.ShouldBeInLoadingState(key: GetKey());
            influencedQuery.ShouldBeInLoadingState(key: GetKey());
            controller.Verify(1);

            // Loading -> Success
            await controller.ReturnAndWaitForStateChange(queries, 123);

            controlledQuery.ShouldBeInSuccessState(key: GetKey(), data: 123);
            influencedQuery.ShouldBeInSuccessState(key: GetKey(), data: 123);
            controller.Verify(1);

            // Success -> FetchingSuccess
            controller.Reset();
            controlledQuery.Refetch();

            controlledQuery.ShouldBeInFetchingSuccessState(key: GetKey(), data: 123);
            influencedQuery.ShouldBeInFetchingSuccessState(key: GetKey(), data: 123);
            controller.Verify(2);

            // FetchingSuccess -> Success
            await controller.ReturnAndWaitForStateChange(queries, 456);

            controlledQuery.ShouldBeInSuccessState(key: GetKey(), data: 456);
            influencedQuery.ShouldBeInSuccessState(key: GetKey(), data: 456);
            controller.Verify(2);

            // Success -> FetchingSuccess
            controller.Reset();
            controlledQuery.Refetch();

            controlledQuery.ShouldBeInFetchingSuccessState(key: GetKey(), data: 456);
            influencedQuery.ShouldBeInFetchingSuccessState(key: GetKey(), data: 456);
            controller.Verify(3);

            // FetchingSuccess -> Error
            await controller.ThrowAndWaitForStateChange(queries, error);

            controlledQuery.ShouldBeInErrorState(key: GetKey(), error);
            influencedQuery.ShouldBeInErrorState(key: GetKey(), error);
            controller.Verify(3);

            // Error -> FetchingError
            controller.Reset();
            controlledQuery.Refetch();

            controlledQuery.ShouldBeInFetchingErrorState(key: GetKey(), error);
            influencedQuery.ShouldBeInFetchingErrorState(key: GetKey(), error);
            controller.Verify(4);

            // FetchingError -> Success
            await controller.ReturnAndWaitForStateChange(queries, 789);

            controlledQuery.ShouldBeInSuccessState(key: GetKey(), 789);
            influencedQuery.ShouldBeInSuccessState(key: GetKey(), 789);
            controller.Verify(4);

            // Success -> Disabled (Influenced)
            influencedQuery.Dispose();

            controlledQuery.ShouldBeInSuccessState(key: GetKey(), 789);
            influencedQuery.ShouldBeInDisabledState();

            // Success -> FetchingSuccess (Controlled)
            controller.Reset();
            controlledQuery.Refetch();

            controlledQuery.ShouldBeInFetchingSuccessState(key: GetKey(), 789);
            influencedQuery.ShouldBeInDisabledState();
            controller.Verify(5);

            // FetchingSuccess -> Disabled (Controlled)
            controlledQuery.Dispose();
            controlledQuery.Refetch();

            controlledQuery.ShouldBeInDisabledState();
            influencedQuery.ShouldBeInDisabledState();
            controller.Verify(5);
        }

        [Fact]
        public async Task SingleQuery_DisposalWhileFetching_DoesNotLeaveDisposedState()
        {
            var controller = new QueryFunctionController<int>();
            var client = new QueryClient();
            using var query = client.CreateQuery(GetKey(), controller.Function);

            query.ShouldBeInLoadingState(key: GetKey());
            query.Dispose();
            query.ShouldBeInDisabledState();

            await Should.ThrowAsync<OperationCanceledException>(async () => await controller.ReturnAndWaitForStateChange(query, 123, timeoutMs: 1000));
            query.ShouldBeInDisabledState();
        }

        #endregion
    }
}
