namespace Composed
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Reactive;

    /// <summary>
    ///     <para>
    ///         Represents a read-only reference (ref) to an arbitrary mutable value.
    ///     </para>
    ///     <para>
    ///         Refs are the heart of Composed's API and are intended to be used together with the
    ///         functions in the <see cref="Compose"/> class.
    ///         They publish change notifications whenever their currently held values change.
    ///         These notifications are published in the form of the <see cref="INotifyPropertyChanged"/> event
    ///         and via the <see cref="IObservable{Unit}"/> interface implemented by the ref.
    ///     </para>
    /// </summary>
    /// <typeparam name="T">The type of the value held by the ref.</typeparam>
    /// <remarks>
    ///     <para>
    ///         Be aware that a read-only ref's underlying value can still change at any time.
    ///         In that sense, this interface is comparable to .NET's <see cref="IReadOnlyCollection{T}"/> interface.
    ///     </para>
    ///     <para>
    ///         Since <see cref="IRef{T}"/> extends <see cref="IReadOnlyRef{T}"/> it is also possible
    ///         to assign any mutable <see cref="IRef{T}"/> to an <see cref="IReadOnlyRef{T}"/>.
    ///         This can be used to ensure that a ref's value can be read, but not changed, from the outside.
    ///     </para>
    /// </remarks>
    /// <seealso cref="Compose"/>
    /// <remarks>
    ///     It is generally not recommended to manually implement this interface.
    ///     Whenever possible, utilize the <see cref="Compose.Ref{T}(T)"/> overloads to create refs.
    /// </remarks>
    public interface IReadOnlyRef<out T> : IObservable<Unit>, INotifyPropertyChanged
    {
        /// <summary>
        ///     Gets the value which is currently held by the ref.
        /// </summary>
        T Value { get; }

        /// <summary>
        ///     Notifies observers that the ref's value has changed.
        ///     This enables you to manually re-evaluate dependencies and manually trigger effects
        ///     which depend on this ref.
        /// </summary>
        void Notify();
    }
}
