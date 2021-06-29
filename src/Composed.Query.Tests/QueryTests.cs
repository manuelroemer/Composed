namespace Composed.Query.Tests
{
    using System;
    using System.Threading.Tasks;
    using Composed.Query.Tests.Utilities;
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

        [Theory]
        [MemberData(nameof(EnabledQueryKeyProviderData))]
        public void Constructor_EnabledQueryKeyProvider_InitializesQueryInLoadingState(QueryKeyProvider queryKeyProvider)
        {
            var controller = new QueryFunctionController<int>();
            var client = new QueryClient();
            using var query = client.CreateQuery(queryKeyProvider, controller.Function);

            query.ShouldBeLoading(key: queryKeyProvider()!);
            controller.Verify(1);
        }

        [Theory]
        [MemberData(nameof(DisabledQueryKeyProviderData))]
        public void Constructor_DisabledQueryKeyProvider_InitializesQueryInDisabledState(QueryKeyProvider queryKeyProvider)
        {
            var controller = new QueryFunctionController<int>();
            var client = new QueryClient();
            using var query = client.CreateQuery(queryKeyProvider, controller.Function);

            query.ShouldBeDisabled();
            controller.Verify(0);
        }

        [Fact]
        public async Task SingleEnabledQuery_OnQueryFunctionCompletion_HasData()
        {
            var controller = new QueryFunctionController<int>();
            var client = new QueryClient();
            using var query = client.CreateQuery(GetKey(), controller.Function);

            query.ShouldBeLoading(key: GetKey());
            controller.Verify(1);

            await controller.ReturnAndWaitForStateChange(query, 123);

            query.ShouldBeIdle(key: GetKey(), data: 123, error: null);
            controller.Verify(1);

            controller.Reset();
            query.Refetch();

            query.ShouldBeFetching(key: GetKey(), data: 123, error: null);
            controller.Verify(2);

            await controller.ReturnAndWaitForStateChange(query, 456);

            query.ShouldBeIdle(key: GetKey(), data: 456, error: null);
            controller.Verify(2);
        }
    }
}