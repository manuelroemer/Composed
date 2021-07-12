namespace Composed.Query.Tests
{
    using System;
    using Shouldly;
    using Xunit;

    public class QueryStateTests
    {
        public static TheoryData<QueryStatus, QueryKey?, bool, object?, Exception?> ValidConstructorData => new()
        {
            { QueryStatus.Disabled, null, false, null, null },
            { QueryStatus.Fetching, new QueryKey(), false, null, null },
            { QueryStatus.Success, new QueryKey(), true, new object(), null },
            { QueryStatus.Error, new QueryKey(), false, null, new Exception() },
            { QueryStatus.Error, new QueryKey(), true, new object(), new Exception() },
            { QueryStatus.Fetching | QueryStatus.Success, new QueryKey(), true, new object(), null },
            { QueryStatus.Fetching | QueryStatus.Error, new QueryKey(), false, null, new Exception() },
            { QueryStatus.Fetching | QueryStatus.Error, new QueryKey(), true, new object(), new Exception() },
        };

        public static TheoryData<QueryStatus, QueryKey?, bool, object?, Exception?> InvalidConstructorData => new()
        {
            // Just some invalid status combinations. Its hard to list every single one without too much effort.
            { QueryStatus.Disabled | QueryStatus.Fetching, null, false, null, null },
            { QueryStatus.Disabled | QueryStatus.Success, null, false, null, null },
            { QueryStatus.Disabled | QueryStatus.Error, null, false, null, null },
            { QueryStatus.Success | QueryStatus.Error, new QueryKey(), false, new object(), new Exception() },

            // hasData is false, but data is not default.
            { QueryStatus.Success, new QueryKey(), false, new object(), null },

            { QueryStatus.Disabled, new QueryKey(), false, null, null },                                         // Key not allowed.
            { QueryStatus.Disabled, null, true, new object(), null },                                            // Data not allowed.
            { QueryStatus.Disabled, null, false, null, new Exception() },                                        // Error not allowed.

            { QueryStatus.Fetching, null, false, null, null },                                                   // Missing key.
            { QueryStatus.Fetching, new QueryKey(), true, new object(), null },                                  // Data not allowed.
            { QueryStatus.Fetching, new QueryKey(), false, null, new Exception() },                              // Error not allowed.

            { QueryStatus.Success, null, true, new object(), null },                                             // Missing key.
            { QueryStatus.Success, new QueryKey(), false, null, null },                                          // Missing data.
            { QueryStatus.Success, new QueryKey(), true, new object(), new Exception() },                        // Error not allowed.

            { QueryStatus.Error, null, false, null, new Exception() },                                           // Missing key.
            { QueryStatus.Error, new QueryKey(), false, null, null },                                            // Missing error.

            { QueryStatus.Fetching | QueryStatus.Success, null, true, new object(), null },                      // Missing key.
            { QueryStatus.Fetching | QueryStatus.Success, new QueryKey(), false, null, null },                   // Missing data.
            { QueryStatus.Fetching | QueryStatus.Success, new QueryKey(), true, new object(), new Exception() }, // Error not allowed.

            { QueryStatus.Fetching | QueryStatus.Error, null, false, null, new Exception() },                    // Missing key.
            { QueryStatus.Fetching | QueryStatus.Error, new QueryKey(), false, null, null },                     // Missing error.
        };

        public static TheoryData<QueryState<object>> ValidStatesData => new()
        {
            DisabledState,
            LoadingState,
            SuccessState,
            ErrorStateWithData,
            ErrorStateWithoutData,
            FetchingSuccessState,
            FetchingErrorStateWithData,
            FetchingErrorStateWithoutData,
        };

        private static QueryState<object> DisabledState => new(QueryStatus.Disabled, null, false, null, null);

        private static QueryState<object> LoadingState => new(QueryStatus.Fetching, new QueryKey(), false, null, null);

        private static QueryState<object> SuccessState => new(QueryStatus.Success, new QueryKey(), true, new object(), null);

        private static QueryState<object> ErrorStateWithData => new(QueryStatus.Error, new QueryKey(), true, new object(), new Exception());

        private static QueryState<object> ErrorStateWithoutData => new(QueryStatus.Error, new QueryKey(), false, null, new Exception());

        private static QueryState<object> FetchingSuccessState => new(QueryStatus.Fetching | QueryStatus.Success, new QueryKey(), true, new object(), null);

        private static QueryState<object> FetchingErrorStateWithData => new(QueryStatus.Fetching | QueryStatus.Error, new QueryKey(), true, new object(), new Exception());

        private static QueryState<object> FetchingErrorStateWithoutData => new(QueryStatus.Fetching | QueryStatus.Error, new QueryKey(), false, null, new Exception());

        [Theory]
        [MemberData(nameof(ValidConstructorData))]
        public void Constructor_ValidArguments_DoesNotThrow(QueryStatus status, QueryKey key, bool hasData, object? data, Exception? error)
        {
            _ = new QueryState<object>(status, key, hasData, data, error);
        }

        [Theory]
        [MemberData(nameof(InvalidConstructorData))]
        public void Constructor_InvalidArguments_ThrowsArgumentException(QueryStatus status, QueryKey key, bool hasData, object? data, Exception? error)
        {
            Should.Throw<ArgumentException>(() => new QueryState<object>(status, key, hasData, data, error));
        }

        [Fact]
        public void IsDisabled_ReturnsExpectedValue()
        {
            DisabledState.IsDisabled.ShouldBeTrue();
            LoadingState.IsDisabled.ShouldBeFalse();
            SuccessState.IsDisabled.ShouldBeFalse();
            ErrorStateWithData.IsDisabled.ShouldBeFalse();
            ErrorStateWithoutData.IsDisabled.ShouldBeFalse();
            FetchingSuccessState.IsDisabled.ShouldBeFalse();
            FetchingErrorStateWithData.IsDisabled.ShouldBeFalse();
            FetchingErrorStateWithoutData.IsDisabled.ShouldBeFalse();
        }

        [Fact]
        public void IsLoading_ReturnsExpectedValue()
        {
            DisabledState.IsLoading.ShouldBeFalse();
            LoadingState.IsLoading.ShouldBeTrue();
            SuccessState.IsLoading.ShouldBeFalse();
            ErrorStateWithData.IsLoading.ShouldBeFalse();
            ErrorStateWithoutData.IsLoading.ShouldBeFalse();
            FetchingSuccessState.IsLoading.ShouldBeFalse();
            FetchingErrorStateWithData.IsLoading.ShouldBeFalse();
            FetchingErrorStateWithoutData.IsLoading.ShouldBeFalse();
        }

        [Fact]
        public void IsFetching_ReturnsExpectedValue()
        {
            DisabledState.IsFetching.ShouldBeFalse();
            LoadingState.IsFetching.ShouldBeTrue();
            SuccessState.IsFetching.ShouldBeFalse();
            ErrorStateWithData.IsFetching.ShouldBeFalse();
            ErrorStateWithoutData.IsFetching.ShouldBeFalse();
            FetchingSuccessState.IsFetching.ShouldBeTrue();
            FetchingErrorStateWithData.IsFetching.ShouldBeTrue();
            FetchingErrorStateWithoutData.IsFetching.ShouldBeTrue();
        }

        [Fact]
        public void IsSuccess_ReturnsExpectedValue()
        {
            DisabledState.IsSuccess.ShouldBeFalse();
            LoadingState.IsSuccess.ShouldBeFalse();
            SuccessState.IsSuccess.ShouldBeTrue();
            ErrorStateWithoutData.IsSuccess.ShouldBeFalse();
            FetchingSuccessState.IsSuccess.ShouldBeTrue();
            FetchingErrorStateWithoutData.IsSuccess.ShouldBeFalse();
        }

        [Fact]
        public void IsError_ReturnsExpectedValue()
        {
            DisabledState.IsError.ShouldBeFalse();
            LoadingState.IsError.ShouldBeFalse();
            SuccessState.IsError.ShouldBeFalse();
            ErrorStateWithData.IsError.ShouldBeTrue();
            ErrorStateWithoutData.IsError.ShouldBeTrue();
            FetchingSuccessState.IsError.ShouldBeFalse();
            FetchingErrorStateWithData.IsError.ShouldBeTrue();
            FetchingErrorStateWithoutData.IsError.ShouldBeTrue();
        }

        [Fact]
        public void HasData_ReturnsExpectedValue()
        {
            DisabledState.HasData.ShouldBeFalse();
            LoadingState.HasData.ShouldBeFalse();
            SuccessState.HasData.ShouldBeTrue();
            ErrorStateWithData.HasData.ShouldBeTrue();
            ErrorStateWithoutData.HasData.ShouldBeFalse();
            FetchingSuccessState.HasData.ShouldBeTrue();
            FetchingErrorStateWithData.HasData.ShouldBeTrue();
            FetchingErrorStateWithoutData.HasData.ShouldBeFalse();
        }
        
        [Fact]
        public void HasErrorWithoutData_ReturnsExpectedValue()
        {
            DisabledState.HasErrorWithoutData.ShouldBeFalse();
            LoadingState.HasErrorWithoutData.ShouldBeFalse();
            SuccessState.HasErrorWithoutData.ShouldBeFalse();
            ErrorStateWithData.HasErrorWithoutData.ShouldBeFalse();
            ErrorStateWithoutData.HasErrorWithoutData.ShouldBeTrue();
            FetchingSuccessState.HasErrorWithoutData.ShouldBeFalse();
            FetchingErrorStateWithData.HasErrorWithoutData.ShouldBeFalse();
            FetchingErrorStateWithoutData.HasErrorWithoutData.ShouldBeTrue();
        }

        [Theory]
        [MemberData(nameof(ValidStatesData))]
        public void GetHashCode_ReturnsHashOfKeyStatusDataError(QueryState<object> state)
        {
            var expected = HashCode.Combine(state.Status, state.Key, state.HasData, state.Data, state.Error);
            state.GetHashCode().ShouldBe(expected);
        }

        [Theory]
        [MemberData(nameof(ValidStatesData))]
        public void ToString_ReturnsExpectedFormat(QueryState<object> state)
        {
            var expected = $"QueryState {{ Status = {state.Status}, Key = {state.Key}, HasData = {state.HasData}, Data = {state.Data}, Error = {state.Error} }}";
            state.ToString().ShouldBe(expected);
        }
    }
}
