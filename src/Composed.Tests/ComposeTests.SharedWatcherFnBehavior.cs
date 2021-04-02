namespace Composed.Tests
{
    using System;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Threading.Tasks;
    using Microsoft.Reactive.Testing;
    using Moq;
    using Shouldly;
    using Xunit;
    using static Composed.Compose;

    public partial class ComposeTests
    {
        // The following test cases test shared behavior of the three watcher functions, i.e.
        // Watch, WatchEffect and Computed.
        // An example for shared behavior is that all three functions re-invoke the effect when
        // a dependency changes.
        // To not pointlessly copy and paste these test cases for each function (+ the sync/async
        // overloads for Watch/WatchEffect, which would make it a total of 5 test cases for
        // each behavior to be tested), the following test cases are ran for each of the 5 functions.

        // The tests require the same method signature for all functions to be tested.
        // For example, Computed (Func<T> -> IRef<T>) must be unified with Watch (Action -> IDisposable) to
        // be usable in the same test.
        // The following delegates define the shared signature used by the tests.
        // The following functions wrap the non-fitting functions and make them fit the signature.
        //
        // Tests like this may not be the cleanest (testing multiple different functions in a single test
        // case is smelly, at the very least), but the benefits outweigh the disadvantages in my opinion.

        private delegate IDisposable WatcherFn(Action effect, params IObservable<Unit>[] dependencies);

        private delegate IDisposable ScheduledWatcherFn(Action effect, IScheduler? scheduler, params IObservable<Unit>[] dependencies);

        private delegate IDisposable AsyncWatcherFn(Func<Task> effect, params IObservable<Unit>[] dependencies);

        private static IDisposable ComputedFn(Action effect, params IObservable<Unit>[] dependencies)
        {
            var @ref = Computed(() =>
            {
                effect();
                return 0;
            }, dependencies);

            return Disposable.Empty;
        }

        private static IDisposable WatchAsyncFn(Action effect, params IObservable<Unit>[] dependencies) =>
            WatchAsyncFn(effect, scheduler: null, dependencies);

        private static IDisposable WatchAsyncFn(Action effect, IScheduler? scheduler, params IObservable<Unit>[] dependencies)
        {
            return Watch(() =>
            {
                effect();
                return Task.CompletedTask;
            }, scheduler, dependencies);
        }

        private static IDisposable WatchEffectAsyncFn(Action effect, params IObservable<Unit>[] dependencies) =>
            WatchEffectAsyncFn(effect, scheduler: null, dependencies);

        private static IDisposable WatchEffectAsyncFn(Action effect, IScheduler? scheduler, params IObservable<Unit>[] dependencies)
        {
            return WatchEffect(() =>
            {
                effect();
                return Task.CompletedTask;
            }, scheduler, dependencies);
        }

        [Theory, MemberData(nameof(DependenciesWithNotifyActionData))]
        public void WatcherFns_OnInvocationAndDependencyChange_InvokeEffectExpectedTimes(IObservable<Unit>[] dependencies, Action changeOneDependency)
        {
            Impl(ComputedFn, immediateEffect: true);
            Impl(Watch, immediateEffect: false);
            Impl(WatchAsyncFn, immediateEffect: false);
            Impl(WatchEffect, immediateEffect: true);
            Impl(WatchEffectAsyncFn, immediateEffect: true);

            void Impl(WatcherFn watcher, bool immediateEffect)
            {
                var effectMock = new Mock<Action>();
                using var subscription = watcher(effectMock.Object, dependencies);

                changeOneDependency();

                effectMock.Verify(fn => fn(), Times.Exactly(immediateEffect ? 2 : 1));
            }
        }

        [Fact]
        public void WatcherFns_DisposedSubscription_DoNotInvokeEffectAgain()
        {
            // Compute not tested here because it doesn't have a disposer in its original signature.
            Impl(Watch, immediateEffect: false);
            Impl(WatchAsyncFn, immediateEffect: false);
            Impl(WatchEffect, immediateEffect: true);
            Impl(WatchEffectAsyncFn, immediateEffect: true);

            void Impl(WatcherFn watcher, bool immediateEffect)
            {
                var dependency = Ref(0);
                var effectMock = new Mock<Action>();
                using var subscription = watcher(effectMock.Object, dependency);

                dependency.Value = 1;
                subscription.Dispose();
                dependency.Value = 2;

                effectMock.Verify(fn => fn(), Times.Exactly(immediateEffect ? 2 : 1));
            }
        }

        [Fact]
        public void WatcherFns_AllDependencyObservablesCompleted_DoNotInvokeEffectAgain()
        {
            Impl(ComputedFn, immediateEffect: true);
            Impl(Watch, immediateEffect: false);
            Impl(WatchAsyncFn, immediateEffect: false);
            Impl(WatchEffect, immediateEffect: true);
            Impl(WatchEffectAsyncFn, immediateEffect: true);

            void Impl(WatcherFn watcher, bool immediateEffect)
            {
                var dependency1 = new NonConformingSubject<Unit>();
                var dependency2 = new NonConformingSubject<Unit>();
                var effectMock = new Mock<Action>();
                using var subscription = watcher(effectMock.Object, dependency1, dependency2);

                dependency1.OnNext(Unit.Default); // Should trigger.
                dependency1.OnCompleted();
                dependency2.OnNext(Unit.Default); // Should trigger.
                dependency2.OnCompleted();
                dependency1.OnNext(Unit.Default); // Should not trigger.
                dependency2.OnNext(Unit.Default); // Should not trigger.

                effectMock.Verify(fn => fn(), Times.Exactly(immediateEffect ? 3 : 2));
            }
        }

        [Fact]
        public void WatcheFns_AnyDependencyObservableErrored_DoNotInvokeEffectAgain()
        {
            Impl(ComputedFn, immediateEffect: true);
            Impl(Watch, immediateEffect: false);
            Impl(WatchAsyncFn, immediateEffect: false);
            Impl(WatchEffect, immediateEffect: true);
            Impl(WatchEffectAsyncFn, immediateEffect: true);

            void Impl(WatcherFn watch, bool immediateEffect)
            {
                var dependency1 = new NonConformingSubject<Unit>();
                var dependency2 = new NonConformingSubject<Unit>();
                var effectMock = new Mock<Action>();
                using var subscription = watch(effectMock.Object, dependency1, dependency2);

                dependency1.OnNext(Unit.Default); // Should trigger.

                try
                {
                    dependency1.OnError(new Exception());
                }
                catch
                {
                }

                dependency1.OnNext(Unit.Default); // Should not trigger.
                dependency2.OnNext(Unit.Default); // Should not trigger.

                effectMock.Verify(fn => fn(), Times.Exactly(immediateEffect ? 2 : 1));
            }
        }

        [Fact]
        public void WatcherFns_PreviousEffectThrew_DoNotInvokeEffectAgain()
        {
            Impl(ComputedFn, immediateEffect: true);
            Impl(Watch, immediateEffect: false);
            Impl(WatchAsyncFn, immediateEffect: false);
            Impl(WatchEffect, immediateEffect: true);
            Impl(WatchEffectAsyncFn, immediateEffect: true);

            void Impl(WatcherFn watch, bool immediateEffect)
            {
                var shouldThrow = false;
                var dependency = new NonConformingSubject<Unit>();
                var effectMock = new Mock<Action>();
                effectMock.Setup(fn => fn()).Callback(() =>
                {
                    if (shouldThrow)
                    {
                        throw new Exception();
                    }
                });
                using var subscription = watch(effectMock.Object, dependency);

                dependency.OnNext(Unit.Default); // Should trigger.

                shouldThrow = true;
                try
                {
                    dependency.OnNext(Unit.Default); // Should trigger.
                }
                catch
                {
                }

                dependency.OnNext(Unit.Default); // Should not trigger.

                effectMock.Verify(fn => fn(), Times.Exactly(immediateEffect ? 3 : 2));
            }
        }

        [Fact]
        public void WatcherFns_DelayedExceptionInAsyncEffect_DoInvokeEffectAgain()
        {
            Impl(Watch, immediateEffect: false);
            Impl(WatchEffect, immediateEffect: true);

            void Impl(AsyncWatcherFn watcher, bool immediateEffect)
            {
                var dependency = Ref(0);
                var effectMock = new Mock<Func<Task>>();
                effectMock.Setup(fn => fn()).Returns(async () =>
                {
                    // There must be a "truly async" method before the exception.
                    // This is by design as any "synchronous" exception *should* throw (as verified by the test
                    // case whose effect synchronously throws).
                    await Task.Delay(1).ConfigureAwait(false);
                    throw new Exception();
                });
                using var subscription = watcher(effectMock.Object, dependency);

                dependency.Value = 1;
                dependency.Value = 2;
                effectMock.Verify(fn => fn(), Times.Exactly(immediateEffect ? 3 : 2));
            }
        }

        [Fact]
        public void WatcherFns_ReturnedDisposable_FulfillDisposableGuidelines()
        {
            Impl(ComputedFn);
            Impl(Watch);
            Impl(WatchAsyncFn);
            Impl(WatchEffect);
            Impl(WatchEffectAsyncFn);

            void Impl(WatcherFn watcher)
            {
                using var subscription = watcher(() => { }, Array.Empty<IObservable<Unit>>());

                // The following two lines ensure that:
                // 1) Dispose doesn't throw.
                // 2) Dispose can be called multiple times.
                subscription.Dispose();
                subscription.Dispose();
            }
        }

        [Fact]
        public void WatcherFns_ErrorInDependencyObservable_ThrowError()
        {
            Impl(ComputedFn);
            Impl(Watch);
            Impl(WatchAsyncFn);
            Impl(WatchEffect);
            Impl(WatchEffectAsyncFn);

            void Impl(WatcherFn watcher)
            {
                var ex = new Exception();
                var observable = Observable.Throw<Unit>(ex);
                var thrown = Record.Exception(() => watcher(() => { }, observable));
                thrown.ShouldNotBeNull();
                thrown.ShouldBeSameAs(ex);
            }
        }

        [Fact]
        public void WatcherFns_ExceptionInEffect_RethrowException()
        {
            Impl(ComputedFn);
            Impl(Watch);
            Impl(WatchAsyncFn);
            Impl(WatchEffect);
            Impl(WatchEffectAsyncFn);

            void Impl(WatcherFn watcher)
            {
                var shouldThrow = false;
                var ex = new Exception();
                var dependency = Ref(0);
                using var subscription = watcher(() =>
                {
                    if (shouldThrow)
                    {
                        throw ex;
                    }
                }, dependency);

                shouldThrow = true;
                var thrown = Record.Exception(() => dependency.Value = 1);

                thrown.ShouldNotBeNull();
                thrown.ShouldBeSameAs(ex);
            }
        }

        [Fact]
        public void WatcherFns_WithoutScheduler_LetDependenciesChooseScheduling()
        {
            Impl(ComputedFn, immediateEffect: true);
            Impl(Watch, immediateEffect: false);
            Impl(WatchAsyncFn, immediateEffect: false);
            Impl(WatchEffect, immediateEffect: true);
            Impl(WatchEffectAsyncFn, immediateEffect: true);

            void Impl(WatcherFn watcher, bool immediateEffect)
            {
                var scheduler = new TestScheduler();
                var dependency = new Subject<Unit>();
                var dependencyObservable = dependency.ObserveOn(scheduler);
                var effectMock = new Mock<Action>();
                var @ref = watcher(effectMock.Object, dependencyObservable);

                // When/Where the initial effect is scheduled is, for the moment, undefined behavior.
                // It's only defined for Computed (which has a separate test case).

                // When the dependency changes, the scheduling is decided by Rx. Since there's only
                // one dependency (with a custom scheduler) the effect should be scheduled on that one.
                dependency.OnNext(Unit.Default);
                effectMock.Verify(fn => fn(), Times.Exactly(immediateEffect ? 1 : 0));
                scheduler.AdvanceBy(1);
                effectMock.Verify(fn => fn(), Times.Exactly(immediateEffect ? 2 : 1));
            }
        }

        [Fact]
        public void WatcherFns_WithScheduler_InvokeEffectOnScheduler()
        {
            Impl(Watch, immediateEffect: false);
            Impl(WatchAsyncFn, immediateEffect: false);
            Impl(WatchEffect, immediateEffect: true);
            Impl(WatchEffectAsyncFn, immediateEffect: true);

            void Impl(ScheduledWatcherFn watcher, bool immediateEffect)
            {
                var dependency = Ref(0);
                var scheduler = new TestScheduler();
                var effectMock = new Mock<Action>();
                var @ref = watcher(effectMock.Object, scheduler, dependency);

                // Initial effect (if given) should already be run on scheduler.
                effectMock.Verify(fn => fn(), Times.Never());
                scheduler.AdvanceBy(1);
                effectMock.Verify(fn => fn(), Times.Exactly(immediateEffect ? 1 : 0));

                // When a dependency changes, the effect should also be run on the scheduler.
                dependency.Value = 1;
                effectMock.Verify(fn => fn(), Times.Exactly(immediateEffect ? 1 : 0));
                scheduler.AdvanceBy(1);
                effectMock.Verify(fn => fn(), Times.Exactly(immediateEffect ? 2 : 1));
            }
        }
    }
}
