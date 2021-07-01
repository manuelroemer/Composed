namespace Composed.Query.Tests
{
    using System;
    using Shouldly;
    using Xunit;

    public class QueryStateTests
    {
        public static TheoryData<QueryStatus, QueryKey?, object?, Exception?> ValidConstructorData => new()
        {
            { QueryStatus.Disabled, null, null, null },
            { QueryStatus.Fetching, new QueryKey(), null, null },
            { QueryStatus.Success, new QueryKey(), new object(), null },
            { QueryStatus.Error, new QueryKey(), null, new Exception() },
            { QueryStatus.Fetching | QueryStatus.Success, new QueryKey(), new object(), null },
            { QueryStatus.Fetching | QueryStatus.Error, new QueryKey(), null, new Exception() },
        };

        public static TheoryData<QueryStatus, QueryKey?, object?, Exception?> InvalidConstructorData => new()
        {
            // Just some invalid status combinations. Its hard to list every single one without too much effort.
            { QueryStatus.Disabled | QueryStatus.Fetching, null, null, null },
            { QueryStatus.Disabled | QueryStatus.Success, null, null, null },
            { QueryStatus.Disabled | QueryStatus.Error, null, null, null },
            { QueryStatus.Success | QueryStatus.Error, new QueryKey(), new object(), new Exception() },

            { QueryStatus.Disabled, new QueryKey(), null, null },                                          // Key not allowed.
            { QueryStatus.Disabled, null, new object(), null },                                            // Data not allowed.
            { QueryStatus.Disabled, null, null, new Exception() },                                         // Error not allowed.

            { QueryStatus.Fetching, null, null, null },                                                    // Missing key.
            { QueryStatus.Fetching, new QueryKey(), new object(), null },                                  // Data not allowed.
            { QueryStatus.Fetching, new QueryKey(), null, new Exception() },                               // Error not allowed.

            { QueryStatus.Success, null, new object(), null },                                             // Missing key.
            { QueryStatus.Success, new QueryKey(), new object(), new Exception() },                        // Error not allowed.

            { QueryStatus.Error, null, null, new Exception() },                                            // Missing key.
            { QueryStatus.Error, new QueryKey(), null, null },                                             // Missing error.
            { QueryStatus.Error, new QueryKey(), new object(), new Exception() },                          // Data not allowed.

            { QueryStatus.Fetching | QueryStatus.Success, null, new object(), null },                      // Missing key.
            { QueryStatus.Fetching | QueryStatus.Success, new QueryKey(), new object(), new Exception() }, // Error not allowed.

            { QueryStatus.Fetching | QueryStatus.Error, null, null, new Exception() },                     // Missing key.
            { QueryStatus.Fetching | QueryStatus.Error, new QueryKey(), null, null },                      // Missing error.
            { QueryStatus.Fetching | QueryStatus.Error, new QueryKey(), new object(), new Exception() },   // Data not allowed.
        };

        public static TheoryData<QueryState<object>> ValidStatesData => new()
        {
            DisabledState,
            LoadingState,
            SuccessState,
            ErrorState,
            FetchingSuccessState,
            FetchingErrorState,
        };

        private static QueryState<object> DisabledState => new(QueryStatus.Disabled, null, null, null);

        private static QueryState<object> LoadingState => new(QueryStatus.Fetching, new QueryKey(), null, null);

        private static QueryState<object> SuccessState => new(QueryStatus.Success, new QueryKey(), new object(), null);

        private static QueryState<object> ErrorState => new(QueryStatus.Error, new QueryKey(), null, new Exception());

        private static QueryState<object> FetchingSuccessState => new(QueryStatus.Fetching | QueryStatus.Success, new QueryKey(), new object(), null);

        private static QueryState<object> FetchingErrorState => new(QueryStatus.Fetching | QueryStatus.Error, new QueryKey(), null, new Exception());

        [Theory]
        [MemberData(nameof(ValidConstructorData))]
        public void Constructor_ValidArguments_DoesNotThrow(QueryStatus status, QueryKey key, object? data, Exception? error)
        {
            _ = new QueryState<object>(status, key, data, error);
        }

        [Theory]
        [MemberData(nameof(InvalidConstructorData))]
        public void Constructor_InvalidArguments_ThrowsArgumentException(QueryStatus status, QueryKey key, object? data, Exception? error)
        {
            Should.Throw<ArgumentException>(() => new QueryState<object>(status, key, data, error));
        }

        [Fact]
        public void IsDisabled_ReturnsExpectedValue()
        {
            DisabledState.IsDisabled.ShouldBeTrue();
            LoadingState.IsDisabled.ShouldBeFalse();
            SuccessState.IsDisabled.ShouldBeFalse();
            ErrorState.IsDisabled.ShouldBeFalse();
            FetchingSuccessState.IsDisabled.ShouldBeFalse();
            FetchingErrorState.IsDisabled.ShouldBeFalse();
        }

        [Fact]
        public void IsLoading_ReturnsExpectedValue()
        {
            DisabledState.IsLoading.ShouldBeFalse();
            LoadingState.IsLoading.ShouldBeTrue();
            SuccessState.IsLoading.ShouldBeFalse();
            ErrorState.IsLoading.ShouldBeFalse();
            FetchingSuccessState.IsLoading.ShouldBeFalse();
            FetchingErrorState.IsLoading.ShouldBeFalse();
        }

        [Fact]
        public void IsFetching_ReturnsExpectedValue()
        {
            DisabledState.IsFetching.ShouldBeFalse();
            LoadingState.IsFetching.ShouldBeTrue();
            SuccessState.IsFetching.ShouldBeFalse();
            ErrorState.IsFetching.ShouldBeFalse();
            FetchingSuccessState.IsFetching.ShouldBeTrue();
            FetchingErrorState.IsFetching.ShouldBeTrue();
        }

        [Fact]
        public void HasData_ReturnsExpectedValue()
        {
            DisabledState.HasData.ShouldBeFalse();
            LoadingState.HasData.ShouldBeFalse();
            SuccessState.HasData.ShouldBeTrue();
            ErrorState.HasData.ShouldBeFalse();
            FetchingSuccessState.HasData.ShouldBeTrue();
            FetchingErrorState.HasData.ShouldBeFalse();
        }

        [Fact]
        public void HasError_ReturnsExpectedValue()
        {
            DisabledState.HasError.ShouldBeFalse();
            LoadingState.HasError.ShouldBeFalse();
            SuccessState.HasError.ShouldBeFalse();
            ErrorState.HasError.ShouldBeTrue();
            FetchingSuccessState.HasError.ShouldBeFalse();
            FetchingErrorState.HasError.ShouldBeTrue();
        }

        [Theory]
        [MemberData(nameof(ValidStatesData))]
        public void GetHashCode_ReturnsHashOfKeyStatusDataError(QueryState<object> state)
        {
            var expected = HashCode.Combine(state.Status, state.Key, state.Data, state.Error);
            state.GetHashCode().ShouldBe(expected);
        }

        [Theory]
        [MemberData(nameof(ValidStatesData))]
        public void ToString_ReturnsExpectedFormat(QueryState<object> state)
        {
            var expected = $"QueryState {{ Status = {state.Status}, Key = {state.Key}, Data = {state.Data}, Error = {state.Error} }}";
            state.ToString().ShouldBe(expected);
        }
    }
}
