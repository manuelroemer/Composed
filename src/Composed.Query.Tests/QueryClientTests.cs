namespace Composed.Query.Tests
{
    using System;
    using System.Reactive;
    using System.Threading.Tasks;
    using Composed.Query.Tests.Utilities;
    using Shouldly;
    using Xunit;
    using static Utilities.QueryKeyHelper;

    public class QueryClientTests
    {
        #region CreateQuery Tests

        [Fact]
        public void CreateQuery_NullArguments_ThrowsArgumentNullException()
        {
            using var client = new QueryClient();

            Should.Throw<ArgumentNullException>(() => client.CreateQuery((QueryKey)null!, () => Task.FromResult(123)));
            Should.Throw<ArgumentNullException>(() => client.CreateQuery(new QueryKey(), (CancelableQueryFunction<int>)null!));

            Should.Throw<ArgumentNullException>(() => client.CreateQuery((QueryKeyProvider)null!, () => Task.FromResult(123)));
            Should.Throw<ArgumentNullException>(() => client.CreateQuery(() => new QueryKey(), (CancelableQueryFunction<int>)null!));
            Should.Throw<ArgumentNullException>(() => client.CreateQuery(() => new QueryKey(), () => Task.FromResult(123), (IObservable<Unit>[])null!));
        }

        [Fact]
        public void CreateQuery_DisposedClient_ThrowsObjectDisposedException()
        {
            using var client = new QueryClient();
            client.Dispose();

            Should.Throw<ObjectDisposedException>(() => client.CreateQuery(new QueryKey(), () => Task.FromResult(123)));
            Should.Throw<ObjectDisposedException>(() => client.CreateQuery(() => null, () => Task.FromResult(123)));
        }

        #endregion

        #region Dispose Tests

        [Fact]
        public void Dispose_WithActiveQueries_DisablesActiveQueries()
        {
            using var client = new QueryClient();
            var queries = new[]
            {
                client.CreateQuery(GetKey(1), () => Task.FromResult(123)),
                client.CreateQuery(GetKey(1), () => Task.FromException<int>(new Exception())),
                client.CreateQuery(GetKey(1), async () => { await Task.Delay(10_000); return 123; }),
                client.CreateQuery(GetKey(2), () => Task.FromResult(123)),
                client.CreateQuery(GetKey(2), () => Task.FromException<int>(new Exception())),
                client.CreateQuery(GetKey(2), async () => { await Task.Delay(10_000); return 123; }),
            };

            client.Dispose();

            foreach (var query in queries)
            {
                query.ShouldBeInDisabledState();
            }
        }

        [Fact]
        public void Dispose_DisposedClient_CanBeInvokedMultipleTimes()
        {
            using var client = new QueryClient();
            client.Dispose();
            client.Dispose();
        }

        #endregion
    }
}
