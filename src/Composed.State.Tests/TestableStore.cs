namespace Composed.State.Tests
{
    using System;
    using System.Collections.Generic;

    public class TestableStore<T> : Store<T>
    {
        public TestableStore(T initialValue)
            : base(initialValue, equalityComparer: null) { }

        public TestableStore(T initialValue, IEqualityComparer<T>? equalityComparer)
            : base(initialValue, equalityComparer) { }

        public new void SetState(T newState) =>
            base.SetState(newState);

        public new void SetState(Func<T, T> provider) =>
            base.SetState(provider);
    }
}
