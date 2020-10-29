namespace Composed.Internal
{
    using System;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;

    internal sealed class ObservableDependency<T> : IDependency
    {
        public IObservable<Unit> Changed { get; }

        public ObservableDependency(IObservable<T> observable)
        {
            Changed = observable.Select(_ => Unit.Default);
        }
    }
}
