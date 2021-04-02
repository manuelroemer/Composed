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
        public void WatchEffect_NullArguments_ThrowsArgumentNullException()
        {
            Should.Throw<ArgumentNullException>(() => WatchEffect(effect: (Action)null!, scheduler: null, dependencies: Array.Empty<IObservable<Unit>>()));
            Should.Throw<ArgumentNullException>(() => WatchEffect(effect: (Func<Task>)null!, scheduler: null, dependencies: Array.Empty<IObservable<Unit>>()));
            Should.Throw<ArgumentNullException>(() => WatchEffect(effect: (Func<CancellationToken, Task>)null!, scheduler: null, dependencies: Array.Empty<IObservable<Unit>>()));

            Should.Throw<ArgumentNullException>(() => WatchEffect(() => { }, scheduler: null, dependencies: null!));
            Should.Throw<ArgumentNullException>(() => WatchEffect(() => Task.CompletedTask, scheduler: null, dependencies: null!));
            Should.Throw<ArgumentNullException>(() => WatchEffect(ct => Task.CompletedTask, scheduler: null, dependencies: null!));

            Should.Throw<ArgumentNullException>(() => WatchEffect(() => { }, scheduler: null, dependencies: new IObservable<Unit>[] { null! }));
            Should.Throw<ArgumentNullException>(() => WatchEffect(() => Task.CompletedTask, scheduler: null, dependencies: new IObservable<Unit>[] { null! }));
            Should.Throw<ArgumentNullException>(() => WatchEffect(ct => Task.CompletedTask, scheduler: null, dependencies: new IObservable<Unit>[] { null! }));
        }
    }
}
