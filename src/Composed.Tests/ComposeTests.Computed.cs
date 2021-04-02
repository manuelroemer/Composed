namespace Composed.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using Microsoft.Reactive.Testing;
    using Moq;
    using Shouldly;
    using Xunit;
    using static Composed.Compose;

    public partial class ComposeTests
    {
        [Fact]
        public void Computed_NullArguments_ThrowsArgumentNullException()
        {
            Should.Throw<ArgumentNullException>(() => Computed(compute: null!, EqualityComparer<int>.Default, Scheduler.Default, Array.Empty<IObservable<Unit>>()));
            Should.Throw<ArgumentNullException>(() => Computed(() => 0, EqualityComparer<int>.Default, Scheduler.Default, dependencies: null!));
            Should.Throw<ArgumentNullException>(() => Computed(() => 0, EqualityComparer<int>.Default, Scheduler.Default, new IObservable<Unit>[] { null! }));
        }

        [Theory, MemberData(nameof(DependenciesData))]
        public void Computed_InitialComputation_ImmediatelyComputesExpectedValueOnce(IObservable<Unit>[] dependencies)
        {
            var computeMock = new Mock<Func<int>>();
            computeMock.Setup(fn => fn()).Returns(123);
            var @ref = Computed(computeMock.Object, dependencies);

            @ref.Value.ShouldBe(123);
            computeMock.Verify(fn => fn(), Times.Once());
        }

        [Fact]
        public void Computed_WithScheduler_ImmediatelyInvokesInitialComputation()
        {
            var dependency = Ref(0);
            var scheduler = new TestScheduler();
            var computeMock = new Mock<Func<int>>();
            var @ref = Computed(computeMock.Object, scheduler, dependency);

            // The initial computation should not be on the scheduler and should therefore not require the
            // scheduler to advance.
            computeMock.Verify(fn => fn(), Times.Once());
        }
    }
}
