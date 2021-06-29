namespace Composed.Query.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
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

            query.Key.Value.ShouldBe(queryKeyProvider());
            query.IsDisabled.Value.ShouldBeFalse();
            query.IsLoading.Value.ShouldBeTrue();
            query.IsFetching.Value.ShouldBeTrue();
            query.Data.Value.ShouldBe(default);
            query.Error.Value.ShouldBeNull();
            controller.FunctionMock.Verify(fn => fn(), Times.Once());
        }

        [Theory]
        [MemberData(nameof(DisabledQueryKeyProviderData))]
        public void Constructor_DisabledQueryKeyProvider_InitializesQueryInDisabledState(QueryKeyProvider queryKeyProvider)
        {
            var controller = new QueryFunctionController<int>();
            var client = new QueryClient();
            using var query = client.CreateQuery(queryKeyProvider, controller.Function);

            query.Key.Value.ShouldBeNull();
            query.IsDisabled.Value.ShouldBeTrue();
            query.IsLoading.Value.ShouldBeFalse();
            query.IsFetching.Value.ShouldBeFalse();
            query.Data.Value.ShouldBe(default);
            query.Error.Value.ShouldBeNull();
            controller.FunctionMock.Verify(fn => fn(), Times.Never());
        }

        [Fact]
        public void SingleEnabledQuery_OnQueryFunctionCompletion_HasData()
        {
            var controller = new QueryFunctionController<int>();
            var client = new QueryClient();
            using var query = client.CreateQuery(GetKey(), controller.Function);

            query.Key.Value.ShouldBe(GetKey());
            query.IsDisabled.Value.ShouldBeFalse();
            query.IsLoading.Value.ShouldBeTrue();
            query.IsFetching.Value.ShouldBeTrue();
            query.Data.Value.ShouldBe(default);
            query.Error.Value.ShouldBeNull();
            controller.FunctionMock.Verify(fn => fn(), Times.Once());

            controller.Return(123);

            query.Key.Value.ShouldBe(GetKey());
            query.IsDisabled.Value.ShouldBeFalse();
            query.IsLoading.Value.ShouldBeTrue();
            query.IsFetching.Value.ShouldBeTrue();
            query.Data.Value.ShouldBe(123);
            query.Error.Value.ShouldBeNull();
            controller.FunctionMock.Verify(fn => fn(), Times.Once());
        }
    }
}
