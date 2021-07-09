namespace Composed.Query.Tests.Utilities
{
    public static class QueryKeyHelper
    {
        public static QueryKey GetKey(int query = 1) =>
            new QueryKey($"Query{query}");
    }
}
