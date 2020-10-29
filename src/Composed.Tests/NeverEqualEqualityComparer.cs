namespace Composed.Tests
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    public sealed class NeverEqualEqualityComparer<T> : EqualityComparer<T>
    {
        public override bool Equals([AllowNull] T x, [AllowNull] T y) =>
            false;

        public override int GetHashCode([DisallowNull] T obj) =>
            obj.GetHashCode();
    }
}
