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
    ///         These notifications are published in the form of the <see cref="INotifyPropertyChanged"/> event
    ///         and via the <see cref="IObservable{T}"/> interface implemented by the ref.
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
        ///         When this property is set to a new value, the ref will notify its subscribers about the change.
        ///         Setting it is therefore equivalent to calling <see cref="SetValue(T, bool)"/>
        ///         with the <c>suppressNotification</c> parameter set to <see langword="false"/>.
        ///     </para>
        /// </summary>
        new T Value { get; set; }

        /// <summary>
        ///     Sets the value which is currently held by the ref to the specified <paramref name="value"/>
        ///     and optionally allows suppressing change notifications.
        /// </summary>
        /// <param name="value">The new value to be held by the ref.</param>
        /// <param name="suppressNotification">
        ///     If <see langword="true"/>, the ref will not notify observers about a value change.
        /// </param>
        /// <returns>
        ///     Returns whether the ref's value changed and, as a result, whether a change notification should be/would
        ///     normally have been published.
        ///     You can use this return value to evaluate whether you should manually trigger a change notification
        ///     via <see cref="IReadOnlyRef{T}.Notify"/> when <paramref name="suppressNotification"/> is set
        ///     to <see langword="true"/>.
        /// </returns>
        /// <remarks>
        ///     It is generally recommended to set a ref's value via the <see cref="Value"/> property.
        ///     You should, however, use this method when you require explicit control about when
        ///     change notifications are published.
        ///     An example for such a scenario is when you set the ref's value inside a <c>lock</c> block.
        ///     Not dispatching the notifications inside the <c>lock</c> block is generally a good idea,
        ///     because it prevents potential deadlocks.
        /// </remarks>
        bool SetValue(T value, bool suppressNotification);
    }
}
