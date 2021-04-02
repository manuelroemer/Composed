namespace Composed.Internal
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Reactive;
    using System.Reactive.Subjects;

    /// <summary>
    ///     The shared implementation for <see cref="IRef{T}"/> and <see cref="IReadOnlyRef{T}"/>.
    ///     <see cref="IReadOnlyRef{T}"/> is currently not implemented as a separate class since
    ///     its restrictions are only imposed by the interface (in the same way that an IList is also
    ///     an IReadOnlyList).
    ///     This may be changed at some point, similarly to how .NET also provides a ReadOnlyCollection
    ///     class, but for the moment not having that additional class/complexity is my prefered approach.
    /// </summary>
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
    [DebuggerDisplay("Ref({Value})")]
    internal sealed class Ref<T> : IRef<T>
#pragma warning restore CA1001
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly object _lock;
        private readonly Subject<Unit> _valueChangedSubject;
        private readonly IEqualityComparer<T> _equalityComparer;
        private T _value;

        public T Value
        {
            get => _value;
            set => SetValue(value, suppressNotification: false);
        }

        public Ref(T initialValue, IEqualityComparer<T>? equalityComparer)
        {
            _lock = new object();
            _valueChangedSubject = new Subject<Unit>();
            _equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
            _value = initialValue;
        }

        public bool SetValue(T newValue, bool suppressNotification)
        {
            lock (_lock)
            {
                if (_equalityComparer.Equals(_value, newValue))
                {
                    return false;
                }

                _value = newValue;
            }

            if (!suppressNotification)
            {
                Notify();
            }

            return true;
        }

        public void Notify()
        {
            _valueChangedSubject.OnNext(Unit.Default);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }

        public IDisposable Subscribe(IObserver<Unit> observer) =>
            _valueChangedSubject.Subscribe(observer);

        public override string ToString() =>
            Value?.ToString() ?? "";
    }
}
