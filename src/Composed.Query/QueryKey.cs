namespace Composed.Query
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class QueryKey : IEquatable<QueryKey>
    {
        private readonly object?[] _components;
        private readonly int _hashCode;

        public QueryKey(params object?[] components)
            : this((IEnumerable<object?>)components) { }

        public QueryKey(IEnumerable<object?> components)
        {
            _components = components?.ToArray() ?? throw new ArgumentNullException(nameof(components));
            _hashCode = HashCode.Combine(_components);
        }

        public override bool Equals(object? obj) =>
            Equals(obj as QueryKey);

        public bool Equals(QueryKey? other)
        {
            if (other is null)
            {
                return false;
            }

            if (_components.Length != other._components.Length)
            {
                return false;
            }

            for (var i = 0; i < _components.Length; i++)
            {
                var thisComponent = _components[i];
                var otherComponent = other._components[i];

                if (!Equals(thisComponent, otherComponent))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode() =>
            _hashCode;

        public override string ToString() =>
            $"[{string.Join(", ", _components)}]";

        public static bool operator ==(QueryKey? left, QueryKey? right) =>
            left is null ? right is null : left.Equals(right);

        public static bool operator !=(QueryKey? left, QueryKey? right) =>
            !(left == right);

        public static implicit operator QueryKey(string key) =>
            new QueryKey(key);

        public static implicit operator QueryKey(object[] components) =>
            new QueryKey(components);
    }
}
