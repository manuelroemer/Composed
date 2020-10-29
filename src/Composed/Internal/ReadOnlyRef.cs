namespace Composed.Internal
{
    using System.Collections.Generic;

    internal sealed class ReadOnlyRef<T> : RefBase<T>, IReadOnlyRef<T>
    {
        public ReadOnlyRef(T initialValue, IEqualityComparer<T>? equalityComparer)
            : base(initialValue, equalityComparer) { }
    }
}
