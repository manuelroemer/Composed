namespace Composed.Query.Tests
{
    using System;
    using System.Threading.Tasks;
    using Composed.Query.Tests.Utilities;
    using Xunit;
    using static Utilities.QueryKeyHelper;

    public class QueryClientTests
    {
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
    }
}
