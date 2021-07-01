namespace Composed.Query
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     <para>
    ///         Represents a key which, together with a <see cref="QueryFunction{T}"/>, uniquely
    ///         identifies a query.
    ///     </para>
    ///     <para>
    ///         A query key is similar to an array of objects in the sense that it is composed of
    ///         multiple other objects (typically a combination of strings and simple data objects)
    ///         called "components".
    ///     </para>
    ///     <para>
    ///         Two query keys are considered equal when their components, compared in order, are equal.
    ///         Consider the following examples for details:
    ///
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <c>["simpleKey"]</c> and <c>["simpleKey"]</c> are equal.
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <c>["simpleKey", "123"]</c> and <c>["simpleKey", "123"]</c> are equal.
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <c>["simpleKey"]</c> and <c>["anotherSimpleKey"]</c> are <b>not</b> equal.
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <c>["simpleKey", "123"]</c> and <c>["simpleKey"]</c> are <b>not</b> equal.
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <c>["simpleKey", "123"]</c> and <c>["simpleKey123"]</c> are <b>not</b> equal.
    ///                 </description>
    ///             </item>
    ///         </list>
    ///
    ///         It is important to understand that the <see cref="QueryKey"/> class uses
    ///         <see cref="object.Equals(object)"/> to compare the components of query keys.
    ///         This allows you to use <i>anything</i> as a query key component (even complex objects)
    ///         as long as these objects correctly implemented their <c>Equals</c> method.
    ///         As an example, you can easily use C# 9.0's <c>record</c> classes as complex query key components.
    ///     </para>
    /// </summary>
    public sealed class QueryKey : IEquatable<QueryKey>
    {
        private readonly object?[] _components;
        private readonly int _hashCode;

        /// <summary>
        ///     Gets the components which form the identity of the query key.
        /// </summary>
        public IReadOnlyList<object?> Components => _components.ToArray();

        /// <inheritdoc cref="QueryKey(IEnumerable{object?})"/>
        public QueryKey(params object?[] components)
            : this((IEnumerable<object?>)components) { }

        /// <summary>
        ///     Initializes a new <see cref="QueryKey"/> instance with the given components.
        /// </summary>
        /// <param name="components">The components which form the identity of the query key.</param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="components"/> is <see langword="null"/>.
        /// </exception>
        public QueryKey(IEnumerable<object?> components)
        {
            _components = components?.ToArray() ?? throw new ArgumentNullException(nameof(components));
            _hashCode = CreateHashCode(_components);

            static int CreateHashCode(object?[] components)
            {
                var hashCode = new HashCode();

                foreach (var component in components)
                {
                    hashCode.Add(component);
                }

                return hashCode.ToHashCode();
            }
        }

        /// <summary>
        ///     Returns a value indicating whether this query key equals the given object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>
        ///     <see langword="true"/> if the query key equals the given object;
        ///     <see langword="false"/> if not.
        /// </returns>
        public override bool Equals(object? obj) =>
            Equals(obj as QueryKey);

        /// <summary>
        ///     Returns a value indicating whether this query key equals the given query key.
        /// </summary>
        /// <param name="other">Another query key.</param>
        /// <returns>
        ///     <see langword="true"/> if the query keys are equal;
        ///     <see langword="false"/> if not.
        /// </returns>
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

        /// <inheritdoc/>
        public override int GetHashCode() =>
            _hashCode;

        /// <inheritdoc/>
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
