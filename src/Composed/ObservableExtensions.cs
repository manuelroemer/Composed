namespace Composed
{
    using System;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;

    /// <summary>
    ///     Provide extension methods targeting the <see cref="IObservable{T}"/> type which simplify
    ///     interop between Composed's API and Rx.NET.
    /// </summary>
    public static class ObservableExtensions
    {
        /// <summary>
        ///     Converts the observable to an observable which can be passed as a dependency
        ///     to functions such as <c>Computed</c>, <c>Watch</c> or <c>WatchEffect</c>.
        /// </summary>
        /// <typeparam name="T">The type of the observable.</typeparam>
        /// <param name="observable">The observable to be converted.</param>
        /// <returns>
        ///     An <see cref="IObservable{Unit}"/> which can be passed as a dependency.
        ///     The observable is created by projecting each element of the source observable
        ///     to <see cref="Unit.Default"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="observable"/> is <see langword="null"/>.
        /// </exception>
        public static IObservable<Unit> AsDependency<T>(this IObservable<T> observable)
        {
            _ = observable ?? throw new ArgumentNullException(nameof(observable));
            return observable.Select(_ => Unit.Default);
        }
    }
}
