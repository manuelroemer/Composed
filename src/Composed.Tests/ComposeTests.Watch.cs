namespace Composed.Tests
{
    using System;
    using System.Reactive;
    using System.Threading;
    using System.Threading.Tasks;
    using Shouldly;
    using Xunit;
    using static Composed.Compose;

    public partial class ComposeTests
    {
        [Fact]
        public void Watch_NullArguments_ThrowsArgumentNullException()
        {
            Should.Throw<ArgumentNullException>(() => Watch(effect: (Action)null!, scheduler: null, dependencies: Array.Empty<IObservable<Unit>>()));
            Should.Throw<ArgumentNullException>(() => Watch(effect: (Func<Task>)null!, scheduler: null, dependencies: Array.Empty<IObservable<Unit>>()));
            Should.Throw<ArgumentNullException>(() => Watch(effect: (Func<CancellationToken, Task>)null!, scheduler: null, dependencies: Array.Empty<IObservable<Unit>>()));

            Should.Throw<ArgumentNullException>(() => Watch(() => { }, scheduler: null, dependencies: null!));
            Should.Throw<ArgumentNullException>(() => Watch(() => Task.CompletedTask, scheduler: null, dependencies: null!));
            Should.Throw<ArgumentNullException>(() => Watch(ct => Task.CompletedTask, scheduler: null, dependencies: null!));

            Should.Throw<ArgumentNullException>(() => Watch(() => { }, scheduler: null, dependencies: new IObservable<Unit>[] { null! }));
            Should.Throw<ArgumentNullException>(() => Watch(() => Task.CompletedTask, scheduler: null, dependencies: new IObservable<Unit>[] { null! }));
            Should.Throw<ArgumentNullException>(() => Watch(ct => Task.CompletedTask, scheduler: null, dependencies: new IObservable<Unit>[] { null! }));
        }
    }
}
