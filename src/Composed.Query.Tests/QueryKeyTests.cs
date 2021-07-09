namespace Composed.Query.Tests
{
    using System;
    using System.Collections.Generic;
    using Shouldly;
    using Xunit;

    public class QueryKeyTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_NullArguments_ThrowsArgumentNullException()
        {
            Should.Throw<ArgumentNullException>(() => new QueryKey((object?[])null!));
            Should.Throw<ArgumentNullException>(() => new QueryKey((IEnumerable<object?>)null!));
        }

        [Fact]
        public void Constructor_Components_InitializesComponents()
        {
            var components = new[] { 1, "2", new object() };
            var queryKey = new QueryKey(components);
            queryKey.Components.ShouldBeEquivalentTo(components);
        }

        #endregion

        #region Equality Tests

        [Theory]
        [InlineData(new object?[] { }, new object?[] { }, true)]
        [InlineData(new object?[] { null }, new object?[] { null }, true)]
        [InlineData(new object?[] { 1 }, new object?[] { 1 }, true)]
        [InlineData(new object?[] { 1, "abc" }, new object?[] { 1, "abc" }, true)]
        [InlineData(new object?[] {  }, new object?[] { 1 }, false)]
        [InlineData(new object?[] { 1 }, new object?[] { 2 }, false)]
        [InlineData(new object?[] { 1 }, new object?[] { "2" }, false)]
        public void EqualityMembers_ComparableValues_ReturnEqualBasedOnComponents(object?[] first, object?[] second, bool expected)
        {
            var queryKey1 = new QueryKey(first);
            var queryKey2 = new QueryKey(second);

            queryKey1.Equals((object?)queryKey2).ShouldBe(expected);
            queryKey1.Equals(queryKey2).ShouldBe(expected);
            (queryKey1 == queryKey2).ShouldBe(expected);
            (!(queryKey1 != queryKey2)).ShouldBe(expected);

            if (expected)
            {
                queryKey1.GetHashCode().ShouldBe(queryKey2.GetHashCode());
            }
            else
            {
                queryKey1.GetHashCode().ShouldNotBe(queryKey2.GetHashCode());
            }
        }

        #endregion

        #region ToString Tests

        [Theory]
        [InlineData(new object?[] { 1, 2, 3 }, "[1, 2, 3]")]
        [InlineData(new object?[] { "abc", "def" }, "[abc, def]")]
        public void ToString_ReturnsExpectedString(object?[] components, string expected)
        {
            var queryKey = new QueryKey(components);
            queryKey.ToString().ShouldBe(expected);
        }

        #endregion

        #region Implicit Operators

        [Fact]
        public void ImplicitOperatorString_ValidString_CreatesQueryKey()
        {
            QueryKey key = "from_string";
            key.Components[0].ShouldBe("from_string");
        }

        [Fact]
        public void ImplicitOperatorObjectArray_ValidValue_CreatesQueryKey()
        {
            QueryKey key = new object[] { 1 };
            key.Components[0].ShouldBe(1);
        }
        
        [Fact]
        public void ImplicitOperatorObjectArray_NullValue_ThrowsArgumentNullException()
        {
            Should.Throw<ArgumentException>(() => { QueryKey key = (object?[])null!; });
        }

        #endregion
    }
}
