namespace Composed.Tests
{
    using System;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Threading;
    using System.Threading.Tasks;
    using Shouldly;
    using Xunit;
    using Composed;
    using static Composed.Compose;

    public class ComposeTests
    {
        #region Ref Tests

        [Fact]
        public void Ref_ReturnsRefWithInitialValue()
        {
            var dependency = Ref(123);
            dependency.Value.ShouldBe(123);
        }

        [Fact]
        public void Ref_ReturnsMutableRef()
        {
            var dependency = Ref(123);
            dependency.Value = 456;
            dependency.Value.ShouldBe(456);
        }

        [Fact]
        public void Ref_OnValueChanging_PublishesChangingNotifications()
        {
            var wasChangingEventRaised = false;
            var valueAtChangingEvent = default(int?);
            var dependency = Ref(0);

            dependency.PropertyChanging += (sender, e) =>
            {
                wasChangingEventRaised = true;
                valueAtChangingEvent = dependency.Value;
            };
            dependency.Value = 1;

            wasChangingEventRaised.ShouldBeTrue();
            valueAtChangingEvent.ShouldBe(0);
        }

        [Fact]
        public void Ref_OnValueChanged_PublishesChangedNotifications()
        {
            var wasChangedEventRaised = false;
            var wasRefObservableNotified = false;
            var wasChangedObservableNotified = false;
            var valueAtChangedEvent = default(int?);
            var valueAtRefObservable = default(int?);
            var valueAtChangedObservable = default(int?);
            var dependency = Ref(0);

            dependency.PropertyChanged += (sender, e) => (wasChangedEventRaised, valueAtChangedEvent) = (true, dependency.Value);
            dependency.Subscribe(_ => (wasRefObservableNotified, valueAtRefObservable) = (true, dependency.Value));
            dependency.Changed.Subscribe(_ => (wasChangedObservableNotified, valueAtChangedObservable) = (true, dependency.Value));
            dependency.Value = 1;

            wasChangedEventRaised.ShouldBeTrue();
            wasRefObservableNotified.ShouldBeTrue();
            wasChangedObservableNotified.ShouldBeTrue();
            valueAtChangedEvent.ShouldBe(1);
            valueAtRefObservable.ShouldBe(1);
            valueAtChangedObservable.ShouldBe(1);
        }

        [Fact]
        public void Ref_OnSettingSameValue_DoesNotRaisePropertyChangingEvent()
        {
            var wasChangingEventRaised = false;
            var dependency = Ref(0);

            dependency.PropertyChanging += (sender, e) => wasChangingEventRaised = true;
            dependency.Value = 0;

            wasChangingEventRaised.ShouldBeFalse();
        }

        [Fact]
        public void Ref_OnSettingSameValue_DoesNotRaisePropertyChangedEvent()
        {
            var wasChangedEventRaised = false;
            var wasRefObservableNotified = false;
            var wasChangedObservableNotified = false;
            var dependency = Ref(0);

            dependency.PropertyChanged += (sender, e) => wasChangedEventRaised = true;
            dependency.Subscribe(_ => wasRefObservableNotified = true);
            dependency.Changed.Subscribe(_ => wasChangedObservableNotified = true);
            dependency.Value = 0;

            wasChangedEventRaised.ShouldBeFalse();
            wasRefObservableNotified.ShouldBeFalse();
            wasChangedObservableNotified.ShouldBeFalse();
        }

        [Fact]
        public void Ref_CustomEqualityComparer_UsesEqualityComparerChangeNotifications()
        {
            var wasChangingEventRaised = false;
            var wasChangedEventRaised = false;
            var wasRefObservableNotified = false;
            var wasChangedObservableNotified = false;
            var dependency = Ref(0, new NeverEqualEqualityComparer<int>());

            dependency.PropertyChanging += (sender, e) => wasChangingEventRaised = true;
            dependency.PropertyChanged += (sender, e) => wasChangedEventRaised = true;
            dependency.Subscribe(_ => wasRefObservableNotified = true);
            dependency.Changed.Subscribe(_ => wasChangedObservableNotified = true);
            dependency.Value = dependency.Value;

            wasChangingEventRaised.ShouldBeTrue();
            wasChangedEventRaised.ShouldBeTrue();
            wasRefObservableNotified.ShouldBeTrue();
            wasChangedObservableNotified.ShouldBeTrue();
        }

        [Fact]
        public void Ref_UnsubscribingFromObservables_DoesNotLeadToSubsequentChangeNotifications()
        {
            var wasRefObservableNotified = false;
            var wasChangedObservableNotified = false;
            var dependency = Ref(0);

            var sub1 = dependency.Subscribe(_ => wasRefObservableNotified = true);
            var sub2 = dependency.Subscribe(_ => wasChangedObservableNotified = true);
            sub1.Dispose();
            sub2.Dispose();

            wasRefObservableNotified.ShouldBeFalse();
            wasChangedObservableNotified.ShouldBeFalse();
        }

        [Fact]
        public void Ref_ToString_ReturnsExpectedValue()
        {
            Ref(123).ToString().ShouldBe("Ref(123)");
        }

        #endregion

        #region Computed Tests

        [Fact]
        public void Computed_ThrowsArgumentNullException()
        {
            Should.Throw<ArgumentNullException>(() => Computed<int>(compute: null!, Array.Empty<IDependency>()));
            Should.Throw<ArgumentNullException>(() => Computed(() => 0, dependencies: null!));
            Should.Throw<ArgumentNullException>(() => Computed(() => 0, new IDependency[] { null! }));
        }

        [Theory, MemberData(nameof(DifferentDependenciesData))]
        public void Computed_ImmediatelyComputesValue(IDependency[] dependencies)
        {
            var computations = 0;
            var computed = Computed(() => ++computations, dependencies);
            computed.Value.ShouldBe(1);
        }

        [Theory, MemberData(nameof(ChangeableDependenciesData))]
        public void Computed_RecomputesValueWhenDependencyChanges(IDependency[] dependencies, Action changeOneDependency)
        {
            var computations = 0;
            var computed = Computed(() => ++computations, dependencies);
            changeOneDependency();
            computed.Value.ShouldBe(2);
        }

        [Fact]
        public void Computed_CancelsSubscriptionWhenAllDependenciesComplete()
        {
            var invocations = 0;
            var dependency1 = new Subject<Unit>();
            var dependency2 = new Subject<Unit>();
            var computed = Computed(() => ++invocations, dependency1.ToDependency(), dependency2.ToDependency());

            // Completing the first dependency should not cancel the subscription (one dependency is still active).
            dependency1.OnNext(Unit.Default);
            dependency1.OnCompleted();
            dependency1.OnNext(Unit.Default);

            // Completing the second dependency should cancel the subscription (all dependencies are completed).
            dependency2.OnNext(Unit.Default);
            dependency2.OnCompleted();
            dependency2.OnNext(Unit.Default);

            computed.Value.ShouldBe(3);
        }

        [Fact]
        public void Computed_CancelsSubscriptionWhenAnyDependencyErrors()
        {
            var invocations = 0;
            var dependency1 = new Subject<Unit>();
            var dependency2 = new Subject<Unit>();
            var computed = Computed(() => ++invocations, dependency1.ToDependency(), dependency2.ToDependency());

            // When any dependency errors, the entire subscription should error.
            dependency1.OnNext(Unit.Default);
            try
            {
                dependency1.OnError(new Exception());
            }
            catch
            {
            }
            dependency1.OnNext(Unit.Default);

            // Even an active dependency should not invoke the effect.
            dependency2.OnNext(Unit.Default);
            dependency2.OnCompleted();
            dependency2.OnNext(Unit.Default);

            invocations.ShouldBe(2);
        }

        [Fact]
        public void Computed_CancelsSubscriptionsWhenComputationThrowsException()
        {
            var invocations = 0;
            var dependency = Ref(0);
            var shouldComputationThrow = false;
            var computed = Computed(() =>
            {
                invocations++;
                if (shouldComputationThrow)
                {
                    throw new Exception();
                }
                return invocations;
            }, dependency);

            try
            {
                shouldComputationThrow = true;
                dependency.Value++;
            }
            catch
            {
            }
            invocations.ShouldBe(2);

            shouldComputationThrow = false;
            dependency.Value++;
            invocations.ShouldBe(2);
        }

        [Fact]
        public void Computed_ThrowsExceptionOfErroredObservableDependency()
        {
            var ex = new Exception();
            var observable = Observable.Throw<int>(ex);
            var thrown = Record.Exception(() => Computed(() => 0, observable.ToDependency()));
            thrown.ShouldNotBeNull();
            thrown.ShouldBeSameAs(ex);
        }

        [Fact]
        public void Computed_DoesNotCatchExceptionInComputation()
        {
            var dependency = Ref(0);
            var ex = new Exception();
            var thrown = Record.Exception(() => Computed<int>(() => throw ex, dependency));
            thrown.ShouldNotBeNull();
            thrown.ShouldBeSameAs(ex);
        }

        [Fact]
        public void Computed_Ref_DoesNotImplementIRef()
        {
            var computed = Computed(() => 0);
            computed.ShouldNotBeOfType<IRef<int>>();
        }

        [Fact]
        public void Computed_Ref_OnValueChanging_PublishesChangingNotifications()
        {
            var wasChangingEventRaised = false;
            var valueAtChangingEvent = default(int?);
            var dependency = Ref(0);
            var computed = Computed(() => dependency.Value, dependency);

            computed.PropertyChanging += (sender, e) =>
            {
                wasChangingEventRaised = true;
                valueAtChangingEvent = computed.Value;
            };
            dependency.Value = 1;

            wasChangingEventRaised.ShouldBeTrue();
            valueAtChangingEvent.ShouldBe(0);
        }

        [Fact]
        public void Computed_Ref_OnValueChanged_PublishesChangedNotifications()
        {
            var wasChangedEventRaised = false;
            var wasRefObservableNotified = false;
            var wasChangedObservableNotified = false;
            var valueAtChangedEvent = default(int?);
            var valueAtRefObservable = default(int?);
            var valueAtChangedObservable = default(int?);
            var dependency = Ref(0);
            var computed = Computed(() => dependency.Value, dependency);

            computed.PropertyChanged += (sender, e) => (wasChangedEventRaised, valueAtChangedEvent) = (true, computed.Value);
            computed.Subscribe(_ => (wasRefObservableNotified, valueAtRefObservable) = (true, computed.Value));
            computed.Changed.Subscribe(_ => (wasChangedObservableNotified, valueAtChangedObservable) = (true, computed.Value));
            dependency.Value = 1;

            wasChangedEventRaised.ShouldBeTrue();
            wasRefObservableNotified.ShouldBeTrue();
            wasChangedObservableNotified.ShouldBeTrue();
            valueAtChangedEvent.ShouldBe(1);
            valueAtRefObservable.ShouldBe(1);
            valueAtChangedObservable.ShouldBe(1);
        }

        [Fact]
        public void Computed_Ref_OnSettingSameValue_DoesNotRaisePropertyChangingEvent()
        {
            var wasChangingEventRaised = false;
            var dependency = Ref(0);
            var computed = Computed(() => 0, dependency);

            computed.PropertyChanging += (sender, e) => wasChangingEventRaised = true;
            dependency.Value++;

            wasChangingEventRaised.ShouldBeFalse();
        }

        [Fact]
        public void Computed_Ref_OnSettingSameValue_DoesNotRaisePropertyChangedEvent()
        {
            var wasChangedEventRaised = false;
            var wasRefObservableNotified = false;
            var wasChangedObservableNotified = false;
            var dependency = Ref(0);
            var computed = Computed(() => 0, dependency);

            computed.PropertyChanged += (sender, e) => wasChangedEventRaised = true;
            computed.Subscribe(_ => wasRefObservableNotified = true);
            computed.Changed.Subscribe(_ => wasChangedObservableNotified = true);
            dependency.Value++;

            wasChangedEventRaised.ShouldBeFalse();
            wasRefObservableNotified.ShouldBeFalse();
            wasChangedObservableNotified.ShouldBeFalse();
        }

        [Fact]
        public void Computed_Ref_CustomEqualityComparer_UsesEqualityComparerChangeNotifications()
        {
            var wasChangingEventRaised = false;
            var wasChangedEventRaised = false;
            var wasRefObservableNotified = false;
            var wasChangedObservableNotified = false;
            var dependency = Ref(0);
            var computed = Computed(() => 0, new NeverEqualEqualityComparer<int>(), dependency);

            computed.PropertyChanging += (sender, e) => wasChangingEventRaised = true;
            computed.PropertyChanged += (sender, e) => wasChangedEventRaised = true;
            computed.Subscribe(_ => wasRefObservableNotified = true);
            computed.Changed.Subscribe(_ => wasChangedObservableNotified = true);
            dependency.Value++;

            wasChangingEventRaised.ShouldBeTrue();
            wasChangedEventRaised.ShouldBeTrue();
            wasRefObservableNotified.ShouldBeTrue();
            wasChangedObservableNotified.ShouldBeTrue();
        }

        [Fact]
        public void Computed_Ref_UnsubscribingFromObservables_DoesNotLeadToSubsequentChangeNotifications()
        {
            var wasRefObservableNotified = false;
            var wasChangedObservableNotified = false;
            var computed = Computed(() => 0);

            var sub1 = computed.Subscribe(_ => wasRefObservableNotified = true);
            var sub2 = computed.Subscribe(_ => wasChangedObservableNotified = true);
            sub1.Dispose();
            sub2.Dispose();

            wasRefObservableNotified.ShouldBeFalse();
            wasChangedObservableNotified.ShouldBeFalse();
        }

        [Fact]
        public void Computed_Ref_ToString_ReturnsExpectedValue()
        {
            Computed(() => 123).ToString().ShouldBe("Ref(123)");
        }

        #endregion

        #region Watch Tests

        [Fact]
        public void Watch_ThrowsArgumentNullException()
        {
            Should.Throw<ArgumentNullException>(() => Watch(effect: (Action)null!));
            Should.Throw<ArgumentNullException>(() => Watch(effect: (Func<Task>)null!));
            Should.Throw<ArgumentNullException>(() => Watch(effect: (Func<CancellationToken, Task>)null!));
            Should.Throw<ArgumentNullException>(() => Watch(() => { }, dependencies: null!));
            Should.Throw<ArgumentNullException>(() => Watch(() => Task.CompletedTask, dependencies: null!));
            Should.Throw<ArgumentNullException>(() => Watch(ct => Task.CompletedTask, dependencies: null!));
            Should.Throw<ArgumentNullException>(() => Watch(ct => Task.CompletedTask, new IDependency[] { null! }));
        }

        [Theory, MemberData(nameof(DifferentDependenciesData))]
        public void Watch_Sync_DoesNotImmediatelyInvokeEffect(IDependency[] dependencies)
        {
            var invocations = 0;
            var dependency = Ref(0);
            Watch(() => invocations++, dependencies);
            invocations.ShouldBe(0);
        }

        [Theory, MemberData(nameof(ChangeableDependenciesData))]
        public void Watch_Sync_InvokesEffectWhenDependencyChanges(IDependency[] dependencies, Action changeOneDependency)
        {
            var invocations = 0;
            Watch(() => invocations++, dependencies);
            changeOneDependency();
            invocations.ShouldBe(1);
        }

        [Theory, MemberData(nameof(DifferentDependenciesData))]
        public void Watch_Sync_UnsubscribingFulfillsDisposableContract(IDependency[] dependencies)
        {
            var subscription = Watch(() => { }, dependencies);

            // These should never throw.
            // This also verifies that Watch returns a valid IDisposable when dependencies is an empty array.
            // (This is a different code path in the current implementation).
            subscription.Dispose();
            subscription.Dispose();
        }

        [Theory, MemberData(nameof(ChangeableDependenciesData))]
        public void Watch_Sync_CancelsSubscriptionWhenDisposed(IDependency[] dependencies, Action changeOneDependency)
        {
            var invocations = 0;
            var subscription = Watch(() => invocations++, dependencies);
            subscription.Dispose();
            changeOneDependency();
            invocations.ShouldBe(0);
        }

        [Fact]
        public void Watch_Sync_CancelsSubscriptionWhenAllDependenciesComplete()
        {
            var invocations = 0;
            var dependency1 = new Subject<Unit>();
            var dependency2 = new Subject<Unit>();

            Watch(() => invocations++, dependency1.ToDependency(), dependency2.ToDependency());

            // Completing the first dependency should not cancel the subscription (one dependency is still active).
            dependency1.OnNext(Unit.Default);
            dependency1.OnCompleted();
            dependency1.OnNext(Unit.Default);

            // Completing the second dependency should cancel the subscription (all dependencies are completed).
            dependency2.OnNext(Unit.Default);
            dependency2.OnCompleted();
            dependency2.OnNext(Unit.Default);

            invocations.ShouldBe(2);
        }

        [Fact]
        public void Watch_Sync_CancelsSubscriptionWhenAnyDependencyErrors()
        {
            var invocations = 0;
            var dependency1 = new Subject<Unit>();
            var dependency2 = new Subject<Unit>();

            Watch(() => invocations++, dependency1.ToDependency(), dependency2.ToDependency());

            // When any dependency errors, the entire subscription should error.
            dependency1.OnNext(Unit.Default);
            try
            {
                dependency1.OnError(new Exception());
            }
            catch
            {
            }
            dependency1.OnNext(Unit.Default);

            // Even an active dependency should not invoke the effect.
            dependency2.OnNext(Unit.Default);
            dependency2.OnCompleted();
            dependency2.OnNext(Unit.Default);

            invocations.ShouldBe(1);
        }

        [Fact]
        public void Watch_Sync_CancelsSubscriptionsWhenEffectThrowsException()
        {
            var invocations = 0;
            var dependency = Ref(0);
            var shouldEffectThrow = false;

            Watch(() =>
            {
                invocations++;
                if (shouldEffectThrow)
                {
                    throw new Exception();
                }
            }, dependency);

            try
            {
                shouldEffectThrow = true;
                dependency.Value++;
            }
            catch
            {
            }
            invocations.ShouldBe(1);

            shouldEffectThrow = false;
            dependency.Value++;
            invocations.ShouldBe(1);
        }

        [Fact]
        public void Watch_Sync_ThrowsExceptionOfErroredObservableDependency()
        {
            var ex = new Exception();
            var observable = Observable.Throw<int>(ex);
            var thrown = Record.Exception(() => Watch(() => { }, observable.ToDependency()));
            thrown.ShouldNotBeNull();
            thrown.ShouldBeSameAs(ex);
        }

        [Fact]
        public void Watch_Sync_DoesNotCatchExceptionInEffect()
        {
            var dependency = Ref(0);
            var ex = new Exception();
            Watch((Action)(() => throw ex), dependency);

            var thrown = Record.Exception(() => dependency.Value++);
            thrown.ShouldNotBeNull();
            thrown.ShouldBeSameAs(ex);
        }

        [Theory, MemberData(nameof(DifferentDependenciesData))]
        public void Watch_Async_DoesNotImmediatelyInvokeEffect(IDependency[] dependencies)
        {
            var invocations = 0;
            var dependency = Ref(0);
            Watch(async () => invocations++, dependencies);
            invocations.ShouldBe(0);
        }

        [Theory, MemberData(nameof(ChangeableDependenciesData))]
        public void Watch_Async_InvokesEffectWhenDependencyChanges(IDependency[] dependencies, Action changeOneDependency)
        {
            var invocations = 0;
            Watch(async () => invocations++, dependencies);
            changeOneDependency();
            invocations.ShouldBe(1);
        }

        [Theory, MemberData(nameof(DifferentDependenciesData))]
        public void Watch_Async_UnsubscribingFulfillsDisposableContract(IDependency[] dependencies)
        {
            var subscription = Watch(async () => { }, dependencies);

            // These should never throw.
            // This also verifies that Watch returns a valid IDisposable when dependencies is an empty array.
            // (This is a different code path in the current implementation).
            subscription.Dispose();
            subscription.Dispose();
        }

        [Theory, MemberData(nameof(ChangeableDependenciesData))]
        public void Watch_Async_CancelsSubscriptionWhenDisposed(IDependency[] dependencies, Action changeOneDependency)
        {
            var wasCanceled = false;
            var invocations = 0;
            var dependency = Ref(0);
            var subscription = Watch(async ct =>
            {
                ct.Register(() => wasCanceled = true);
                invocations++;
            }, dependencies);

            changeOneDependency();
            subscription.Dispose();
            changeOneDependency();

            wasCanceled.ShouldBeTrue();
            invocations.ShouldBe(1);
        }

        [Fact]
        public void Watch_Async_CancelsSubscriptionWhenAllDependenciesComplete()
        {
            var wasCanceled = false;
            var invocations = 0;
            var dependency1 = new Subject<Unit>();
            var dependency2 = new Subject<Unit>();

            Watch(async ct =>
            {
                ct.Register(() => wasCanceled = true);
                invocations++;
            }, dependency1.ToDependency(), dependency2.ToDependency());

            // Completing the first dependency should not cancel the subscription (one dependency is still active).
            dependency1.OnNext(Unit.Default);
            dependency1.OnCompleted();
            dependency1.OnNext(Unit.Default);

            // Completing the second dependency should cancel the subscription (all dependencies are completed).
            dependency2.OnNext(Unit.Default);
            dependency2.OnCompleted();
            dependency2.OnNext(Unit.Default);

            invocations.ShouldBe(2);
            wasCanceled.ShouldBeTrue();
        }

        [Fact]
        public void Watch_Async_CancelsSubscriptionWhenAnyDependencyErrors()
        {
            var wasCanceled = false;
            var invocations = 0;
            var dependency1 = new Subject<Unit>();
            var dependency2 = new Subject<Unit>();

            Watch(async ct =>
            {
                ct.Register(() => wasCanceled = true);
                invocations++;
            }, dependency1.ToDependency(), dependency2.ToDependency());

            // When any dependency errors, the entire subscription should error.
            dependency1.OnNext(Unit.Default);
            try
            {
                dependency1.OnError(new Exception());
            }
            catch
            {
            }
            dependency1.OnNext(Unit.Default);

            // Even an active dependency should not invoke the effect.
            dependency2.OnNext(Unit.Default);
            dependency2.OnCompleted();
            dependency2.OnNext(Unit.Default);

            invocations.ShouldBe(1);
            wasCanceled.ShouldBeTrue();
        }

        [Fact]
        public void Watch_Async_CancelsSubscriptionsWhenEffectThrowsException()
        {
            var invocations = 0;
            var dependency = Ref(0);
            var shouldEffectThrow = false;

            Watch(() =>
            {
                invocations++;
                if (shouldEffectThrow)
                {
                    throw new Exception();
                }
                return Task.CompletedTask;
            }, dependency);

            try
            {
                shouldEffectThrow = true;
                dependency.Value++;
            }
            catch
            {
            }
            invocations.ShouldBe(1);

            shouldEffectThrow = false;
            dependency.Value++;
            invocations.ShouldBe(1);
        }

        [Fact]
        public void Watch_Async_ThrowsExceptionOfErroredObservableDependency()
        {
            var ex = new Exception();
            var observable = Observable.Throw<int>(ex);
            var thrown = Record.Exception(() => Watch(async () => { }, observable.ToDependency()));
            thrown.ShouldNotBeNull();
            thrown.ShouldBeSameAs(ex);
        }

        [Fact]
        public void Watch_Async_DoesNotCancelSubscriptionWhenTaskIsFaulted()
        {
            var invocations = 0;
            var dependency = Ref(0);
            Watch(async () =>
            {
                invocations++;
                throw new Exception();
            }, dependency);

            dependency.Value++;
            dependency.Value++;
            invocations.ShouldBe(2);
        }

        #endregion

        #region WatchEffect Tests

        [Fact]
        public void WatchEffect_ThrowsArgumentNullException()
        {
            Should.Throw<ArgumentNullException>(() => WatchEffect(effect: (Action)null!));
            Should.Throw<ArgumentNullException>(() => WatchEffect(effect: (Func<Task>)null!));
            Should.Throw<ArgumentNullException>(() => WatchEffect(effect: (Func<CancellationToken, Task>)null!));
            Should.Throw<ArgumentNullException>(() => WatchEffect(() => { }, dependencies: null!));
            Should.Throw<ArgumentNullException>(() => WatchEffect(() => Task.CompletedTask, dependencies: null!));
            Should.Throw<ArgumentNullException>(() => WatchEffect(ct => Task.CompletedTask, dependencies: null!));
            Should.Throw<ArgumentNullException>(() => WatchEffect(ct => Task.CompletedTask, new IDependency[] { null! }));
        }

        [Theory, MemberData(nameof(DifferentDependenciesData))]
        public void WatchEffect_Sync_ImmediatelyInvokesEffect(IDependency[] dependencies)
        {
            var invocations = 0;
            var dependency = Ref(0);
            WatchEffect(() => invocations++, dependencies);
            invocations.ShouldBe(1);
        }

        [Theory, MemberData(nameof(ChangeableDependenciesData))]
        public void WatchEffect_Sync_InvokesEffectWhenDependencyChanges(IDependency[] dependencies, Action changeOneDependency)
        {
            var invocations = 0;
            WatchEffect(() => invocations++, dependencies);
            changeOneDependency();
            invocations.ShouldBe(2);
        }

        [Theory, MemberData(nameof(DifferentDependenciesData))]
        public void WatchEffect_Sync_UnsubscribingFulfillsDisposableContract(IDependency[] dependencies)
        {
            var subscription = WatchEffect(() => { }, dependencies);

            // These should never throw.
            // This also verifies that WatchEffect returns a valid IDisposable when dependencies is an empty array.
            // (This is a different code path in the current implementation).
            subscription.Dispose();
            subscription.Dispose();
        }

        [Theory, MemberData(nameof(ChangeableDependenciesData))]
        public void WatchEffect_Sync_CancelsSubscriptionWhenDisposed(IDependency[] dependencies, Action changeOneDependency)
        {
            var invocations = 0;
            var subscription = WatchEffect(() => invocations++, dependencies);
            subscription.Dispose();
            changeOneDependency();
            invocations.ShouldBe(1);
        }

        [Fact]
        public void WatchEffect_Sync_CancelsSubscriptionWhenAllDependenciesComplete()
        {
            var invocations = 0;
            var dependency1 = new Subject<Unit>();
            var dependency2 = new Subject<Unit>();

            WatchEffect(() => invocations++, dependency1.ToDependency(), dependency2.ToDependency());

            // Completing the first dependency should not cancel the subscription (one dependency is still active).
            dependency1.OnNext(Unit.Default);
            dependency1.OnCompleted();
            dependency1.OnNext(Unit.Default);

            // Completing the second dependency should cancel the subscription (all dependencies are completed).
            dependency2.OnNext(Unit.Default);
            dependency2.OnCompleted();
            dependency2.OnNext(Unit.Default);

            invocations.ShouldBe(3);
        }

        [Fact]
        public void WatchEffect_Sync_CancelsSubscriptionWhenAnyDependencyErrors()
        {
            var invocations = 0;
            var dependency1 = new Subject<Unit>();
            var dependency2 = new Subject<Unit>();

            WatchEffect(() => invocations++, dependency1.ToDependency(), dependency2.ToDependency());

            // When any dependency errors, the entire subscription should error.
            dependency1.OnNext(Unit.Default);
            try
            {
                dependency1.OnError(new Exception());
            }
            catch
            {
            }
            dependency1.OnNext(Unit.Default);

            // Even an active dependency should not invoke the effect.
            dependency2.OnNext(Unit.Default);
            dependency2.OnCompleted();
            dependency2.OnNext(Unit.Default);

            invocations.ShouldBe(2);
        }

        [Fact]
        public void WatchEffect_Sync_DoesNotEstablishSubscriptionWhenInitialEffectInvocationErrors()
        {
            var invocations = 0;
            var dependency = Ref(0);
            var shouldEffectThrow = false;

            try
            {
                shouldEffectThrow = true;
                WatchEffect(() =>
                {
                    invocations++;
                    if (shouldEffectThrow)
                    {
                        throw new Exception();
                    }
                }, dependency);
            }
            catch
            {
            }

            invocations.ShouldBe(1);
            shouldEffectThrow = false;
            dependency.Value++;
            invocations.ShouldBe(1);
        }

        [Fact]
        public void WatchEffect_Sync_CancelsSubscriptionsWhenEffectThrowsException()
        {
            var invocations = 0;
            var dependency = Ref(0);
            var shouldEffectThrow = false;

            WatchEffect(() =>
            {
                invocations++;
                if (shouldEffectThrow)
                {
                    throw new Exception();
                }
            }, dependency);

            try
            {
                shouldEffectThrow = true;
                dependency.Value++;
            }
            catch
            {
            }
            invocations.ShouldBe(2);

            shouldEffectThrow = false;
            dependency.Value++;
            invocations.ShouldBe(2);
        }

        [Fact]
        public void WatchEffect_Sync_ThrowsExceptionOfErroredObservableDependency()
        {
            var ex = new Exception();
            var observable = Observable.Throw<int>(ex);
            var thrown = Record.Exception(() => WatchEffect(() => { }, observable.ToDependency()));
            thrown.ShouldNotBeNull();
            thrown.ShouldBeSameAs(ex);
        }

        [Fact]
        public void WatchEffect_Sync_DoesNotCatchExceptionInEffect()
        {
            var ex = new Exception();
            var thrown = Record.Exception(() => WatchEffect((Action)(() => throw ex)));
            thrown.ShouldNotBeNull();
            thrown.ShouldBeSameAs(ex);
        }

        [Theory, MemberData(nameof(DifferentDependenciesData))]
        public void WatchEffect_Async_ImmediatelyInvokesEffect(IDependency[] dependencies)
        {
            var invocations = 0;
            var dependency = Ref(0);
            WatchEffect(async () => invocations++, dependencies);
            invocations.ShouldBe(1);
        }

        [Theory, MemberData(nameof(ChangeableDependenciesData))]
        public void WatchEffect_Async_InvokesEffectWhenDependencyChanges(IDependency[] dependencies, Action changeOneDependency)
        {
            var invocations = 0;
            WatchEffect(async () => invocations++, dependencies);
            changeOneDependency();
            invocations.ShouldBe(2);
        }

        [Theory, MemberData(nameof(DifferentDependenciesData))]
        public void WatchEffect_Async_UnsubscribingFulfillsDisposableContract(IDependency[] dependencies)
        {
            var subscription = WatchEffect(async () => { }, dependencies);

            // These should never throw.
            // This also verifies that WatchEffect returns a valid IDisposable when dependencies is an empty array.
            // (This is a different code path in the current implementation).
            subscription.Dispose();
            subscription.Dispose();
        }

        [Theory, MemberData(nameof(ChangeableDependenciesData))]
        public void WatchEffect_Async_CancelsSubscriptionWhenDisposed(IDependency[] dependencies, Action changeOneDependency)
        {
            var wasCanceled = false;
            var invocations = 0;
            var dependency = Ref(0);
            var subscription = WatchEffect(async ct =>
            {
                ct.Register(() => wasCanceled = true);
                invocations++;
            }, dependencies);

            changeOneDependency();
            subscription.Dispose();
            changeOneDependency();

            wasCanceled.ShouldBeTrue();
            invocations.ShouldBe(2);
        }

        [Fact]
        public void WatchEffect_Async_CancelsSubscriptionWhenAllDependenciesComplete()
        {
            var wasCanceled = false;
            var invocations = 0;
            var dependency1 = new Subject<Unit>();
            var dependency2 = new Subject<Unit>();

            WatchEffect(async ct =>
            {
                ct.Register(() => wasCanceled = true);
                invocations++;
            }, dependency1.ToDependency(), dependency2.ToDependency());

            // Completing the first dependency should not cancel the subscription (one dependency is still active).
            dependency1.OnNext(Unit.Default);
            dependency1.OnCompleted();
            dependency1.OnNext(Unit.Default);

            // Completing the second dependency should cancel the subscription (all dependencies are completed).
            dependency2.OnNext(Unit.Default);
            dependency2.OnCompleted();
            dependency2.OnNext(Unit.Default);

            invocations.ShouldBe(3);
            wasCanceled.ShouldBeTrue();
        }

        [Fact]
        public void WatchEffect_Async_CancelsSubscriptionWhenAnyDependencyErrors()
        {
            var wasCanceled = false;
            var invocations = 0;
            var dependency1 = new Subject<Unit>();
            var dependency2 = new Subject<Unit>();

            WatchEffect(async ct =>
            {
                ct.Register(() => wasCanceled = true);
                invocations++;
            }, dependency1.ToDependency(), dependency2.ToDependency());

            // When any dependency errors, the entire subscription should error.
            dependency1.OnNext(Unit.Default);
            try
            {
                dependency1.OnError(new Exception());
            }
            catch
            {
            }
            dependency1.OnNext(Unit.Default);

            // Even an active dependency should not invoke the effect.
            dependency2.OnNext(Unit.Default);
            dependency2.OnCompleted();
            dependency2.OnNext(Unit.Default);

            invocations.ShouldBe(2);
            wasCanceled.ShouldBeTrue();
        }

        [Fact]
        public void WatchEffect_Async_CancelsSubscriptionsWhenEffectThrowsExceptionBeforeReturningTask()
        {
            var invocations = 0;
            var dependency = Ref(0);
            var shouldEffectThrow = false;

            WatchEffect(() =>
            {
                invocations++;
                if (shouldEffectThrow)
                {
                    throw new Exception();
                }
                return Task.CompletedTask;
            }, dependency);

            try
            {
                shouldEffectThrow = true;
                dependency.Value++;
            }
            catch
            {
            }
            invocations.ShouldBe(2);

            shouldEffectThrow = false;
            dependency.Value++;
            invocations.ShouldBe(2);
        }

        [Fact]
        public void WatchEffect_Async_ThrowsExceptionOfErroredObservableDependency()
        {
            var ex = new Exception();
            var observable = Observable.Throw<int>(ex);
            var thrown = Record.Exception(() => WatchEffect(async () => { }, observable.ToDependency()));
            thrown.ShouldNotBeNull();
            thrown.ShouldBeSameAs(ex);
        }

        [Fact]
        public void WatchEffect_Async_DoesNotCancelSubscriptionWhenTaskIsFaulted()
        {
            var invocations = 0;
            var dependency = Ref(0);
            WatchEffect(async () =>
            {
                invocations++;
                throw new Exception();
            }, dependency);

            dependency.Value++;
            dependency.Value++;
            invocations.ShouldBe(3);
        }

        #endregion

        #region Test Data

        public static TheoryData<IDependency[]> DifferentDependenciesData => new()
        {
            Array.Empty<IDependency>(),
            new[] { Ref(0) },
            new[] { Ref(0), Ref(0) },
            new[] { Observable.Never<int>().ToDependency() },
            new[] { Observable.Never<int>().ToDependency(), Observable.Never<int>().ToDependency() },
            new[] { Ref(0), new Subject<int>().ToDependency() },
        };

        public static TheoryData<IDependency[], Action> ChangeableDependenciesData
        {
            get
            {
                var data = new TheoryData<IDependency[], Action>();

                var ref1 = Ref(0);
                var ref2 = Ref(0);
                var subject = new Subject<int>();
                var completedObservable = Observable.Empty<int>();

                // The goal here is to have a set of different dependency combinations (refs, observables)
                // which all publish change notifications.
                data.Add(new[] { ref1 }, () => ref1.Value++);
                data.Add(new[] { subject.ToDependency() }, () => subject.OnNext(1));
                data.Add(new[] { ref1, ref2 }, () => ref1.Value++);
                data.Add(new[] { ref1, ref2 }, () => ref2.Value++);
                data.Add(new[] { ref1, subject.ToDependency() }, () => ref1.Value++);
                data.Add(new[] { ref1, subject.ToDependency() }, () => subject.OnNext(1));
                data.Add(new[] { ref1, completedObservable.ToDependency() }, () => ref1.Value++);

                return data;
            }
        }

        #endregion
    }
}
