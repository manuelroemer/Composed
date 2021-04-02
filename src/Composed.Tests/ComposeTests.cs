namespace Composed.Tests
{
    using System;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using Xunit;
    using static Composed.Compose;

    public partial class ComposeTests
    {
        /// <summary>
        ///     Theory data with a set of dependency observables.
        /// </summary>
        public static TheoryData<IObservable<Unit>[]> DependenciesData => new()
        {
            Array.Empty<IObservable<Unit>>(),
            new[] { Ref(0) },
            new[] { Ref(0), Ref(0) },
            new[] { Observable.Never<Unit>() },
            new[] { Observable.Never<Unit>(), Observable.Never<Unit>() },
            new[] { Ref(0), (IObservable<Unit>)new Subject<Unit>() },
        };

        /// <summary>
        ///     Theory data with a set of dependency observables and an action that, when invoked,
        ///     causes one of the dependencies to push a new value.
        /// </summary>
        public static TheoryData<IObservable<Unit>[], Action> DependenciesWithNotifyActionData
        {
            get
            {
                var data = new TheoryData<IObservable<Unit>[], Action>();
                var ref1 = Ref(0);
                var ref2 = Ref(0);

                // The goal of this data is mainly that there can be one or more dependencies and
                // that either of them can be triggered.
                // Functions like Watch/WatchEffect should run the effect in any case.
                return new TheoryData<IObservable<Unit>[], Action>()
                {
                    { new IObservable<Unit>[] { ref1 }, () => ref1.Value++ },
                    { new IObservable<Unit>[] { ref1, ref2 }, () => ref1.Value++ },
                    { new IObservable<Unit>[] { ref1, ref2 }, () => ref2.Value++ },
                };
            }
        }

        //        #region Watch Tests

        //        #endregion

        //        #region WatchEffect Tests

        //        [Fact]
        //        public void WatchEffect_ThrowsArgumentNullException()
        //        {
        //            Should.Throw<ArgumentNullException>(() => WatchEffect(effect: (Action)null!));
        //            Should.Throw<ArgumentNullException>(() => WatchEffect(effect: (Func<Task>)null!));
        //            Should.Throw<ArgumentNullException>(() => WatchEffect(effect: (Func<CancellationToken, Task>)null!));
        //            Should.Throw<ArgumentNullException>(() => WatchEffect(() => { }, dependencies: null!));
        //            Should.Throw<ArgumentNullException>(() => WatchEffect(() => Task.CompletedTask, dependencies: null!));
        //            Should.Throw<ArgumentNullException>(() => WatchEffect(ct => Task.CompletedTask, dependencies: null!));
        //            Should.Throw<ArgumentNullException>(() => WatchEffect(ct => Task.CompletedTask, new IDependency[] { null! }));
        //        }

        //        [Theory, MemberData(nameof(DifferentDependenciesData))]
        //        public void WatchEffect_Sync_ImmediatelyInvokesEffect(IDependency[] dependencies)
        //        {
        //            var invocations = 0;
        //            var dependency = Ref(0);
        //            WatchEffect(() => invocations++, dependencies);
        //            invocations.ShouldBe(1);
        //        }

        //        [Theory, MemberData(nameof(ChangeableDependenciesData))]
        //        public void WatchEffect_Sync_InvokesEffectWhenDependencyChanges(IDependency[] dependencies, Action changeOneDependency)
        //        {
        //            var invocations = 0;
        //            WatchEffect(() => invocations++, dependencies);
        //            changeOneDependency();
        //            invocations.ShouldBe(2);
        //        }

        //        [Theory, MemberData(nameof(DifferentDependenciesData))]
        //        public void WatchEffect_Sync_UnsubscribingFulfillsDisposableContract(IDependency[] dependencies)
        //        {
        //            var subscription = WatchEffect(() => { }, dependencies);

        //            // These should never throw.
        //            // This also verifies that WatchEffect returns a valid IDisposable when dependencies is an empty array.
        //            // (This is a different code path in the current implementation).
        //            subscription.Dispose();
        //            subscription.Dispose();
        //        }

        //        [Theory, MemberData(nameof(ChangeableDependenciesData))]
        //        public void WatchEffect_Sync_CancelsSubscriptionWhenDisposed(IDependency[] dependencies, Action changeOneDependency)
        //        {
        //            var invocations = 0;
        //            var subscription = WatchEffect(() => invocations++, dependencies);
        //            subscription.Dispose();
        //            changeOneDependency();
        //            invocations.ShouldBe(1);
        //        }

        //        [Fact]
        //        public void WatchEffect_Sync_CancelsSubscriptionWhenAllDependenciesComplete()
        //        {
        //            var invocations = 0;
        //            var dependency1 = new Subject<Unit>();
        //            var dependency2 = new Subject<Unit>();

        //            WatchEffect(() => invocations++, dependency1.ToDependency(), dependency2.ToDependency());

        //            // Completing the first dependency should not cancel the subscription (one dependency is still active).
        //            dependency1.OnNext(Unit.Default);
        //            dependency1.OnCompleted();
        //            dependency1.OnNext(Unit.Default);

        //            // Completing the second dependency should cancel the subscription (all dependencies are completed).
        //            dependency2.OnNext(Unit.Default);
        //            dependency2.OnCompleted();
        //            dependency2.OnNext(Unit.Default);

        //            invocations.ShouldBe(3);
        //        }

        //        [Fact]
        //        public void WatchEffect_Sync_CancelsSubscriptionWhenAnyDependencyErrors()
        //        {
        //            var invocations = 0;
        //            var dependency1 = new Subject<Unit>();
        //            var dependency2 = new Subject<Unit>();

        //            WatchEffect(() => invocations++, dependency1.ToDependency(), dependency2.ToDependency());

        //            // When any dependency errors, the entire subscription should error.
        //            dependency1.OnNext(Unit.Default);
        //            try
        //            {
        //                dependency1.OnError(new Exception());
        //            }
        //            catch
        //            {
        //            }
        //            dependency1.OnNext(Unit.Default);

        //            // Even an active dependency should not invoke the effect.
        //            dependency2.OnNext(Unit.Default);
        //            dependency2.OnCompleted();
        //            dependency2.OnNext(Unit.Default);

        //            invocations.ShouldBe(2);
        //        }

        //        [Fact]
        //        public void WatchEffect_Sync_DoesNotEstablishSubscriptionWhenInitialEffectInvocationErrors()
        //        {
        //            var invocations = 0;
        //            var dependency = Ref(0);
        //            var shouldEffectThrow = false;

        //            try
        //            {
        //                shouldEffectThrow = true;
        //                WatchEffect(() =>
        //                {
        //                    invocations++;
        //                    if (shouldEffectThrow)
        //                    {
        //                        throw new Exception();
        //                    }
        //                }, dependency);
        //            }
        //            catch
        //            {
        //            }

        //            invocations.ShouldBe(1);
        //            shouldEffectThrow = false;
        //            dependency.Value++;
        //            invocations.ShouldBe(1);
        //        }

        //        [Fact]
        //        public void WatchEffect_Sync_CancelsSubscriptionsWhenEffectThrowsException()
        //        {
        //            var invocations = 0;
        //            var dependency = Ref(0);
        //            var shouldEffectThrow = false;

        //            WatchEffect(() =>
        //            {
        //                invocations++;
        //                if (shouldEffectThrow)
        //                {
        //                    throw new Exception();
        //                }
        //            }, dependency);

        //            try
        //            {
        //                shouldEffectThrow = true;
        //                dependency.Value++;
        //            }
        //            catch
        //            {
        //            }
        //            invocations.ShouldBe(2);

        //            shouldEffectThrow = false;
        //            dependency.Value++;
        //            invocations.ShouldBe(2);
        //        }

        //        [Fact]
        //        public void WatchEffect_Sync_ThrowsExceptionOfErroredObservableDependency()
        //        {
        //            var ex = new Exception();
        //            var observable = Observable.Throw<int>(ex);
        //            var thrown = Record.Exception(() => WatchEffect(() => { }, observable.ToDependency()));
        //            thrown.ShouldNotBeNull();
        //            thrown.ShouldBeSameAs(ex);
        //        }

        //        [Fact]
        //        public void WatchEffect_Sync_DoesNotCatchExceptionInEffect()
        //        {
        //            var ex = new Exception();
        //            var thrown = Record.Exception(() => WatchEffect((Action)(() => throw ex)));
        //            thrown.ShouldNotBeNull();
        //            thrown.ShouldBeSameAs(ex);
        //        }

        //        [Theory, MemberData(nameof(DifferentDependenciesData))]
        //        public void WatchEffect_Async_ImmediatelyInvokesEffect(IDependency[] dependencies)
        //        {
        //            var invocations = 0;
        //            var dependency = Ref(0);
        //            WatchEffect(async () => invocations++, dependencies);
        //            invocations.ShouldBe(1);
        //        }

        //        [Theory, MemberData(nameof(ChangeableDependenciesData))]
        //        public void WatchEffect_Async_InvokesEffectWhenDependencyChanges(IDependency[] dependencies, Action changeOneDependency)
        //        {
        //            var invocations = 0;
        //            WatchEffect(async () => invocations++, dependencies);
        //            changeOneDependency();
        //            invocations.ShouldBe(2);
        //        }

        //        [Theory, MemberData(nameof(DifferentDependenciesData))]
        //        public void WatchEffect_Async_UnsubscribingFulfillsDisposableContract(IDependency[] dependencies)
        //        {
        //            var subscription = WatchEffect(async () => { }, dependencies);

        //            // These should never throw.
        //            // This also verifies that WatchEffect returns a valid IDisposable when dependencies is an empty array.
        //            // (This is a different code path in the current implementation).
        //            subscription.Dispose();
        //            subscription.Dispose();
        //        }

        //        [Theory, MemberData(nameof(ChangeableDependenciesData))]
        //        public void WatchEffect_Async_CancelsSubscriptionWhenDisposed(IDependency[] dependencies, Action changeOneDependency)
        //        {
        //            var wasCanceled = false;
        //            var invocations = 0;
        //            var dependency = Ref(0);
        //            var subscription = WatchEffect(async ct =>
        //            {
        //                ct.Register(() => wasCanceled = true);
        //                invocations++;
        //            }, dependencies);

        //            changeOneDependency();
        //            subscription.Dispose();
        //            changeOneDependency();

        //            wasCanceled.ShouldBeTrue();
        //            invocations.ShouldBe(2);
        //        }

        //        [Fact]
        //        public void WatchEffect_Async_CancelsSubscriptionWhenAllDependenciesComplete()
        //        {
        //            var wasCanceled = false;
        //            var invocations = 0;
        //            var dependency1 = new Subject<Unit>();
        //            var dependency2 = new Subject<Unit>();

        //            WatchEffect(async ct =>
        //            {
        //                ct.Register(() => wasCanceled = true);
        //                invocations++;
        //            }, dependency1.ToDependency(), dependency2.ToDependency());

        //            // Completing the first dependency should not cancel the subscription (one dependency is still active).
        //            dependency1.OnNext(Unit.Default);
        //            dependency1.OnCompleted();
        //            dependency1.OnNext(Unit.Default);

        //            // Completing the second dependency should cancel the subscription (all dependencies are completed).
        //            dependency2.OnNext(Unit.Default);
        //            dependency2.OnCompleted();
        //            dependency2.OnNext(Unit.Default);

        //            invocations.ShouldBe(3);
        //            wasCanceled.ShouldBeTrue();
        //        }

        //        [Fact]
        //        public void WatchEffect_Async_CancelsSubscriptionWhenAnyDependencyErrors()
        //        {
        //            var wasCanceled = false;
        //            var invocations = 0;
        //            var dependency1 = new Subject<Unit>();
        //            var dependency2 = new Subject<Unit>();

        //            WatchEffect(async ct =>
        //            {
        //                ct.Register(() => wasCanceled = true);
        //                invocations++;
        //            }, dependency1.ToDependency(), dependency2.ToDependency());

        //            // When any dependency errors, the entire subscription should error.
        //            dependency1.OnNext(Unit.Default);
        //            try
        //            {
        //                dependency1.OnError(new Exception());
        //            }
        //            catch
        //            {
        //            }
        //            dependency1.OnNext(Unit.Default);

        //            // Even an active dependency should not invoke the effect.
        //            dependency2.OnNext(Unit.Default);
        //            dependency2.OnCompleted();
        //            dependency2.OnNext(Unit.Default);

        //            invocations.ShouldBe(2);
        //            wasCanceled.ShouldBeTrue();
        //        }

        //        [Fact]
        //        public void WatchEffect_Async_CancelsSubscriptionsWhenEffectThrowsExceptionBeforeReturningTask()
        //        {
        //            var invocations = 0;
        //            var dependency = Ref(0);
        //            var shouldEffectThrow = false;

        //            WatchEffect(() =>
        //            {
        //                invocations++;
        //                if (shouldEffectThrow)
        //                {
        //                    throw new Exception();
        //                }
        //                return Task.CompletedTask;
        //            }, dependency);

        //            try
        //            {
        //                shouldEffectThrow = true;
        //                dependency.Value++;
        //            }
        //            catch
        //            {
        //            }
        //            invocations.ShouldBe(2);

        //            shouldEffectThrow = false;
        //            dependency.Value++;
        //            invocations.ShouldBe(2);
        //        }

        //        [Fact]
        //        public void WatchEffect_Async_ThrowsExceptionOfErroredObservableDependency()
        //        {
        //            var ex = new Exception();
        //            var observable = Observable.Throw<int>(ex);
        //            var thrown = Record.Exception(() => WatchEffect(async () => { }, observable.ToDependency()));
        //            thrown.ShouldNotBeNull();
        //            thrown.ShouldBeSameAs(ex);
        //        }

        //        [Fact]
        //        public void WatchEffect_Async_DoesNotCancelSubscriptionWhenTaskIsFaulted()
        //        {
        //            var invocations = 0;
        //            var dependency = Ref(0);
        //            WatchEffect(async () =>
        //            {
        //                invocations++;
        //                throw new Exception();
        //            }, dependency);

        //            dependency.Value++;
        //            dependency.Value++;
        //            invocations.ShouldBe(3);
        //        }

        //        #endregion

    }
}
