namespace Composed.Tests
{
    using System;
    using System.Collections.Generic;
    using Composed;
    using Moq;
    using Shouldly;
    using Xunit;
    using static Composed.Compose;

    public partial class ComposeTests
    {
        [Fact]
        public void Ref_Value_HasExpectedInitialValue()
        {
            var @ref = Ref(123);
            @ref.Value.ShouldBe(123);
        }

        [Fact]
        public void Ref_Value_AllowsSettingValue() =>
            AllowsSettingValueImpl((@ref, value) => @ref.Value = value);
        
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ref_SetValue_AllowsSettingValue(bool suppressNotification) =>
            AllowsSettingValueImpl((@ref, value) => @ref.SetValue(value, suppressNotification));

        private static void AllowsSettingValueImpl(Action<IRef<int>, int> setValue)
        {
            var @ref = Ref(0);
            setValue(@ref, 123);
            @ref.Value.ShouldBe(123);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ref_Value_CorrectlyNotifiesUsingEqualityComparer(bool areValuesEqual) =>
            CorrectlyNotifiesUsingEqualityComparerImpl(@ref => @ref.Value = 123, areValuesEqual);
        
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ref_SetValue_CorrectlyNotifiesUsingEqualityComparer(bool areValuesEqual) =>
            CorrectlyNotifiesUsingEqualityComparerImpl(@ref => @ref.SetValue(123, suppressNotification: false), areValuesEqual);

        private static void CorrectlyNotifiesUsingEqualityComparerImpl(Action<IRef<int>> setValue, bool areValuesEqual)
        {
            var equalityComparerMock = new Mock<IEqualityComparer<int>>();
            var wasChangedEventRaised = false;
            var wasRefObservableNotified = false;
            var @ref = Ref(0, equalityComparerMock.Object);

            equalityComparerMock
                .Setup(x => x.Equals(It.IsAny<int>(), It.IsAny<int>()))
                .Returns(areValuesEqual);

            @ref.PropertyChanged += (sender, e) => wasChangedEventRaised = true;
            @ref.Subscribe(_ => wasRefObservableNotified = true);

            setValue(@ref);

            equalityComparerMock.Verify(x => x.Equals(0, 123), Times.Once());
            wasChangedEventRaised.ShouldBe(!areValuesEqual);
            wasRefObservableNotified.ShouldBe(!areValuesEqual);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ref_SetValue_UsesSuppressNotificationParameter(bool suppressNotification)
        {
            var wasChangedEventRaised = false;
            var wasRefObservableNotified = false;
            var @ref = Ref(0);

            @ref.PropertyChanged += (sender, e) => wasChangedEventRaised = true;
            @ref.Subscribe(_ => wasRefObservableNotified = true);

            @ref.SetValue(123, suppressNotification);

            wasChangedEventRaised.ShouldBe(!suppressNotification);
            wasRefObservableNotified.ShouldBe(!suppressNotification);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ref_SetValue_ReturnsWhetherValueEffectivelyChanged(bool areValuesEqual)
        {
            var equalityComparerMock = new Mock<IEqualityComparer<int>>();
            var @ref = Ref(0, equalityComparerMock.Object);

            equalityComparerMock
                .Setup(x => x.Equals(It.IsAny<int>(), It.IsAny<int>()))
                .Returns(areValuesEqual);

            var hasValueEffectivelyChanged = @ref.SetValue(123, suppressNotification: false);
            hasValueEffectivelyChanged.ShouldBe(!areValuesEqual);
        }

        [Fact]
        public void Ref_Notify_Notifies()
        {
            var wasChangedEventRaised = false;
            var wasRefObservableNotified = false;
            var @ref = Ref(0);

            @ref.PropertyChanged += (sender, e) => wasChangedEventRaised = true;
            @ref.Subscribe(_ => wasRefObservableNotified = true);

            @ref.Notify();

            wasChangedEventRaised.ShouldBeTrue();
            wasRefObservableNotified.ShouldBeTrue();
        }

        [Theory]
        [InlineData("Test")]
        [InlineData(123)]
        [InlineData(null)]
        public void Ref_ToString_ReturnsStringifiedValue(object? value)
        {
            Ref(value).ToString().ShouldBe(value?.ToString() ?? "");
        }
    }
}
