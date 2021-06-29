namespace Composed.Query.Tests
{
    using System;
    using System.Threading.Tasks;
    using Composed.Query.Tests.Utilities;
    using Moq;
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

        [Theory]
        [MemberData(nameof(EnabledQueryKeyProviderData))]
        public void Constructor_EnabledQueryKeyProvider_InitializesQueryInLoadingState(QueryKeyProvider queryKeyProvider)
        {
            var controller = new QueryFunctionController<int>();
            var client = new QueryClient();
            using var query = client.CreateQuery(queryKeyProvider, controller.Function);

            query.ShouldBeLoading(expectedKey: queryKeyProvider()!);
            controller.FunctionMock.Verify(fn => fn(), Times.Once());
        }

        [Theory]
        [MemberData(nameof(DisabledQueryKeyProviderData))]
        public void Constructor_DisabledQueryKeyProvider_InitializesQueryInDisabledState(QueryKeyProvider queryKeyProvider)
        {
            var controller = new QueryFunctionController<int>();
            var client = new QueryClient();
            using var query = client.CreateQuery(queryKeyProvider, controller.Function);

            query.ShouldBeDisabled();
            controller.FunctionMock.Verify(fn => fn(), Times.Never());
        }

        [Fact]
        public async Task SingleEnabledQuery_OnQueryFunctionCompletion_HasData()
        {
            var controller = new QueryFunctionController<int>();
            var client = new QueryClient();
            using var query = client.CreateQuery(GetKey(), controller.Function);

            query.ShouldBeLoading(expectedKey: GetKey());
            controller.FunctionMock.Verify(fn => fn(), Times.Once());

            controller.Return(123);
            await query.State.WaitNext();

            query.ShouldBeIdle(expectedKey: GetKey(), expectedData: 123, expectedError: null);
            controller.FunctionMock.Verify(fn => fn(), Times.Once());
        }
    }
}
