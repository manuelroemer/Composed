namespace Composed.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Disposables;
    using System.Reactive.Subjects;

    /// <summary>
    ///     A simple <see cref="ISubject{T}"/> implementation which does not conform to the Rx grammar.
    ///     Introduced for tests which require non-conforming OnNext/OnCompleted/OnError invocations.
    /// </summary>
    public sealed class NonConformingSubject<T> : SubjectBase<T>
    {
        // Since this is only used in simple tests, we can fall back to a very simple implementation.
        private readonly List<IObserver<T>> _observers = new();
        private bool _isDisposed;

        public override bool HasObservers => _observers.Count > 0;
        public override bool IsDisposed => _isDisposed;

        public override void OnNext(T value) =>
            _observers.ToList().ForEach(x => x.OnNext(value));

        public override void OnError(Exception error) =>
            _observers.ToList().ForEach(x => x.OnError(error));

        public override void OnCompleted() =>
            _observers.ToList().ForEach(x => x.OnCompleted());

        public override IDisposable Subscribe(IObserver<T> observer)
        {
            _observers.Add(observer);
            return Disposable.Create(() => _observers.Remove(observer));
        }

        public override void Dispose()
        {
            _isDisposed = true;
            _observers.Clear();
        }
    }
}
