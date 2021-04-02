namespace Composed.Commands.Tests
{
    using System;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using Shouldly;
    using Xunit;
    using static Composed.Commands.Compose;

    public class ComposeTests
    {
        [Fact]
        public void UseCommand_Sync_ThrowsArgumentNullException()
        {
            Should.Throw<ArgumentNullException>(() => UseCommand(execute: null!, canExecute: () => true, scheduler: DefaultScheduler.Instance, dependencies: Array.Empty<IObservable<Unit>>()));
            Should.Throw<ArgumentNullException>(() => UseCommand(execute: () => { }, null!, scheduler: DefaultScheduler.Instance, dependencies: Array.Empty<IObservable<Unit>>()));
            Should.Throw<ArgumentNullException>(() => UseCommand(execute: () => { }, canExecute: () => true, scheduler: DefaultScheduler.Instance, dependencies: null!));
            Should.Throw<ArgumentNullException>(() => UseCommand(execute: () => { }, canExecute: () => true, scheduler: DefaultScheduler.Instance, dependencies: new IObservable<Unit>[] { null! }));
        }
    }
}
