namespace Composed.Internal
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;

#pragma warning disable CA1001 // Types that own disposable fields should be disposable
    internal abstract class RefBase<T> : IRef<T>
#pragma warning restore CA1001
    {
        public event PropertyChangingEventHandler? PropertyChanging;
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly Subject<T> _subject;
        private readonly IEqualityComparer<T> _equalityComparer;
        private T _value;

        public IObservable<Unit> Changed { get; }

        public T Value
        {
            get
            {
                return _value;
            }
            set 
            {
                if (_equalityComparer.Equals(_value, value))
                {
                    return;
                }

                PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(nameof(Value)));
                _value = value;
                _subject.OnNext(value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }

        public RefBase(T initialValue, IEqualityComparer<T>? equalityComparer)
        {
            _subject = new Subject<T>();
            _equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
            _value = initialValue;
            Changed = _subject.Select(_ => Unit.Default);
        }

        public IDisposable Subscribe(IObserver<T> observer) =>
            _subject.Subscribe(observer);

        public override string ToString() =>
            $"Ref({Value})";
    }
}
