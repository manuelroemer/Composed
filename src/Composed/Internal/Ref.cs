namespace Composed.Internal
{
    using System.Collections.Generic;

    internal sealed class Ref<T> : RefBase<T>
    {
        public Ref(T initialValue, IEqualityComparer<T>? equalityComparer)
            : base(initialValue, equalityComparer) { }
    }
}
