namespace Composed
{
    using System;
    using Composed.Internal;

    /// <summary>
    ///     Defines extension methods for the <see cref="IObservable{T}"/> interface.
    /// </summary>
    public static class ObservableExtensions
    {
        /// <summary>
        ///     Converts the <paramref name="observable"/> to an <see cref="IDependency"/> whose
        ///     <see cref="IDependency.Changed"/> observable directly mirrors this observable
        ///     (i.e. it provides observers with a new value when the <paramref name="observable"/>
        ///     provides a new value, it completes when the <paramref name="observable"/> completes
        ///     and it forwards an error when the <paramref name="observable"/> errors).
        /// </summary>
        /// <typeparam name="T">The object that provides notification information.</typeparam>
        /// <param name="observable">
        ///     The <see cref="IObservable{T}"/> to be converted to an <see cref="IDependency"/>.
        /// </param>
        /// <returns>
        ///     A new <see cref="IDependency"/> instance mirroring the <paramref name="observable"/>.
        /// </returns>
        /// <remarks>
        ///     This function enables you to use any <see cref="IObservable{T}"/> as a dependency
        ///     in functions like <see cref="Compose.Watch(Action, IDependency[])"/> or
        ///     <see cref="Compose.WatchEffect(Action, IDependency[])"/>.
        ///     Be aware though that this function does not perform any error handling on the
        ///     converted <paramref name="observable"/>. If the <paramref name="observable"/> can
        ///     error, consider handling these errors before calling this method.
        ///     See remarks of <see cref="Compose.Watch(Action, IDependency[])"/> (or similar functions)
        ///     for details.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="observable"/> is <see langword="null"/>.
        /// </exception>
        public static IDependency ToDependency<T>(this IObservable<T> observable)
        {
            _ = observable ?? throw new ArgumentNullException(nameof(observable));
            return observable is IDependency dependency
                ? dependency
                : new ObservableDependency<T>(observable);
        }
    }
}
