namespace Composed
{
    using System;
    using System.ComponentModel;

    /// <summary>
    ///     <para>
    ///         Represents a reference (ref) to an arbitrary mutable value.
    ///     </para>
    ///     <para>
    ///         Refs are the heart of Composed's API and are intended to be used together with the
    ///         functions in the <see cref="Compose"/> class.
    ///         They publish change notifications whenever their currently held values change.
    ///         These notifications are published in the form of <see cref="INotifyPropertyChanging"/>
    ///         and <see cref="INotifyPropertyChanged"/> events and via the <see cref="IObservable{T}"/>
    ///         interface implemented by the ref.
    ///         In essence, a ref is an observable which provides notifications whenever its value changes.
    ///     </para>
    /// </summary>
    /// <typeparam name="T">The type of the value held by the ref.</typeparam>
    /// <seealso cref="Compose"/>
    public interface IRef<T> : IReadOnlyRef<T>
    {
        /// <summary>
        ///     <para>
        ///         Gets or sets the value which is currently held by the ref.
        ///     </para>
        ///     <para>
        ///         When this is set to a new value, the ref will notify its subscribers about the change.
        ///     </para>
        /// </summary>
        new T Value { get; set; }
    }
}
