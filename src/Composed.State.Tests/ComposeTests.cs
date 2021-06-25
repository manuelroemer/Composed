namespace Composed.State.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Concurrency;
    using Shouldly;
    using Xunit;
    using static Composed.State.Compose;

    public class ComposeTests
    {
        [Fact]
        public void UseStore_NullArguments_ThrowsArgumentNullException()
        {
            Should.Throw<ArgumentNullException>(() => UseStore<object, object>(store: null!, stateSelector: state => state, equalityComparer: EqualityComparer<object>.Default, DefaultScheduler.Instance));
            Should.Throw<ArgumentNullException>(() => UseStore(store: new TestableStore<object>(new object()), stateSelector: null!, equalityComparer: EqualityComparer<object>.Default, DefaultScheduler.Instance));
        }
    }
}
