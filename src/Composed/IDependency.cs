namespace Composed
{
    using System;
    using System.ComponentModel;
    using System.Reactive;

    /// <summary>
    ///     Represents an element which is able to notify dependents about an arbitrary change.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Implementing this interface enables Composed to track changes in the implementer and
    ///         to react to these changes - for example by invoking effects or recomputing values.
    ///     </para>
    ///     <para>
    ///         This interface is directly implemented by <see cref="IReadOnlyRef{T}"/> and <see cref="IRef{T}"/> instances.
    ///         These can directly be passed as dependencies.
    ///         <br/>
    ///         It is also possible to convert any <see cref="IObservable{T}"/> to an <see cref="IDependency"/>
    ///         using the <see cref="ObservableExtensions.ToDependency{T}(IObservable{T})"/> function.
    ///     </para>
    /// </remarks>
    /// <seealso cref="Compose"/>
    /// <seealso cref="IReadOnlyRef{T}"/>
    /// <seealso cref="IRef{T}"/>
    public interface IDependency
    {
        /// <summary>
        ///     Gets an observable which produces change notifications whenever this object changes
        ///     in a way that should lead to dependents reevaluating their current state.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Composed uses this observable to react to changes of dependencies.
        ///         The <see cref="Compose.Watch(Action, IDependency[])"/> function, for example,
        ///         invokes the specified effect whenever a dependency's <see cref="Changed"/> observable
        ///         provides a new value.
        ///     </para>
        ///     <para>
        ///         In most cases, users of the library should not have a need to interact with this property.
        ///         If you do think that you have to access this property, consider using functions from
        ///         the <see cref="Compose"/> class instead or, if you are dealing with refs, directly
        ///         subscribing to them instead (refs implement <see cref="IObservable{T}"/>).
        ///     </para>
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        IObservable<Unit> Changed { get; }
    }
}
