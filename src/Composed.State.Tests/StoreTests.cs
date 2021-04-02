namespace Composed.State.Tests
{
    using System;
    using Moq;
    using Shouldly;
    using Xunit;
    using static Composed.Compose;

    public class StoreTests
    {
        [Fact]
        public void SetValue_StateObject_SetsStateAndNotifies()
        {
            var effectMock = new Mock<Action>();
            var store = new TestableStore<object>(new object());
            var newState = new object();
            using var subscription = Watch(effectMock.Object, store.State);

            store.SetState(newState);

            store.State.Value.ShouldBeSameAs(newState);
            effectMock.Verify(fn => fn(), Times.Once());
        }

        [Fact]
        public void SetValue_EqualStateObject_DoesNotSetAndNotify()
        {
            var effectMock = new Mock<Action>();
            var value = new object();
            var store = new TestableStore<object>(value);
            using var subscription = Watch(effectMock.Object, store.State);

            store.SetState(value);

            store.State.Value.ShouldBeSameAs(value);
            effectMock.Verify(fn => fn(), Times.Never());
        }

        [Fact]
        public void SetValue_StateProvider_SetsStateBasedOnOldStateAndNotifies()
        {
            var effectMock = new Mock<Action>();
            var initialState = new object();
            var newState = new object();
            object? oldState = null;
            var store = new TestableStore<object>(initialState);
            using var subscription = Watch(effectMock.Object, store.State);

            store.SetState(state =>
            {
                oldState = state;
                return newState;
            });

            oldState.ShouldBeSameAs(initialState);
            store.State.Value.ShouldBeSameAs(newState);
            effectMock.Verify(fn => fn(), Times.Once());
        }

        [Fact]
        public void SetValue_StateProvider_DoesNotSetAndNotify()
        {
            var effectMock = new Mock<Action>();
            var value = new object();
            var store = new TestableStore<object>(value);
            using var subscription = Watch(effectMock.Object, store.State);

            store.SetState(_ => value);

            store.State.Value.ShouldBeSameAs(value);
            effectMock.Verify(fn => fn(), Times.Never());
        }
    }
}
