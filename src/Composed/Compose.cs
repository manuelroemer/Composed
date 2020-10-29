namespace Composed
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Composed.Internal;

    /// <summary>
    ///     <para>
    ///         Contains a set of static methods which provide the entry point to and the fundamental
    ///         members of Composed's API.
    ///     </para>
    ///     <para>
    ///         It is recommended that you import this class via a <c>using static</c> directive
    ///         (<c>using static Composed.Compose;</c>) if you are using C#.
    ///     </para>
    /// </summary>
    public static partial class Compose
    {
        private static readonly IObservable<Unit> RunEffectImmediatelyObservable = Observable.Return(Unit.Default);

        /// <summary>
        ///     <para>
        ///         Creates and returns a new mutable <see cref="IRef{T}"/> instance which holds the
        ///         specified <paramref name="initialValue"/>.
        ///     </para>
        ///     <para>
        ///         When its value is changed, the ref will use <see cref="EqualityComparer{T}.Default"/>
        ///         for determining whether dependencies will be notified about that change.
        ///     </para>
        /// </summary>
        /// <typeparam name="T">The type of the value held by the ref.</typeparam>
        /// <param name="initialValue">
        ///     The initial value to be held by the ref.
        /// </param>
        /// <returns>
        ///     A new mutable <see cref="IRef{T}"/> instance which holds the specified <paramref name="initialValue"/>.
        /// </returns>
        /// <example>
        ///     The following code demonstrates how you can create a ref for various kind of objects:
        ///
        ///     <code>
        ///     using System;
        ///     using Composed;
        ///     using static Composed.Compose;
        ///
        ///     IRef&lt;string&gt; stringRef = Ref("Hello World");
        ///     IRef&lt;int&gt; intRef = Ref(123);
        ///
        ///     Console.WriteLine(stringRef.Value);
        ///     Console.WriteLine(intRef.Value);
        ///
        ///     // Output:
        ///     // Hello World
        ///     // 123
        ///     </code>
        /// </example>
        public static IRef<T> Ref<T>(T initialValue) =>
            Ref(initialValue, equalityComparer: null);

        /// <summary>
        ///     <para>
        ///         Creates and returns a new mutable <see cref="IRef{T}"/> instance which holds the
        ///         specified <paramref name="initialValue"/>.
        ///     </para>
        ///     <para>
        ///         This overload allows you to specify the <see cref="IEqualityComparer{T}"/> to be used
        ///         by the ref for determining whether dependencies will be notified about a value change.
        ///     </para>
        /// </summary>
        /// <typeparam name="T">The type of the value held by the ref.</typeparam>
        /// <param name="initialValue">
        ///     The initial value to be held by the ref.
        /// </param>
        /// <param name="equalityComparer">
        ///     <para>
        ///         An <see cref="IEqualityComparer{T}"/> instance which will be used to compare the
        ///         ref's new and old value when it is changed.
        ///         The ref will only notify its dependents of the change when the equality comparer
        ///         considers the two values unequal.
        ///     </para>
        ///     <para>
        ///         If this is <see langword="null"/>, <see cref="EqualityComparer{T}.Default"/> is used instead.
        ///     </para>
        /// </param>
        /// <returns>
        ///     A new mutable <see cref="IRef{T}"/> instance which holds the specified <paramref name="initialValue"/>.
        /// </returns>
        /// <example>
        ///     The following code demonstrates the effect of using a custom <see cref="IEqualityComparer{T}"/>:
        ///
        ///     <code>
        ///     using System;
        ///     using Composed;
        ///     using static Composed.Compose;
        ///     
        ///     class NeverEqualEqualityComparer&lt;T&gt; : EqualityComparer&lt;T&gt;
        ///     {
        ///         public override bool Equals(T x, T y) => false;
        ///         
        ///         public override int GetHashCode(T obj) => obj.GetHashCode();
        ///     }
        ///     
        ///     IRef&lt;int&gt; refWithCustomComparer = Ref(123, new NeverEqualEqualityComparer&lt;int&gt;());
        ///     IRef&lt;int&gt; refWithoutCustomComparer = Ref(123, equalityComparer: null); // Equivalent to `Ref(123);`
        ///
        ///     Watch(
        ///         () => Console.WriteLine($"refWithCustomComparer changed. New: {refWithCustomComparer.Value}"),
        ///         refWithCustomComparer
        ///     );
        ///
        ///     Watch(
        ///         () => Console.WriteLine($"refWithoutCustomComparer changed. New: {refWithoutCustomComparer.Value}"),
        ///         refWithoutCustomComparer
        ///     );
        ///
        ///     refWithCustomComparer.Value = 123;
        ///     refWithoutCustomComparer.Value = 123;
        ///
        ///     refWithCustomComparer.Value = 456;
        ///     refWithoutCustomComparer.Value = 456;
        /// 
        ///     // Output:
        ///     // refWithCustomComparer changed. New: 123
        ///     // refWithCustomComparer changed. New: 456
        ///     // refWithoutCustomComparer changed. New: 456
        ///     </code>
        /// </example>
        public static IRef<T> Ref<T>(T initialValue, IEqualityComparer<T>? equalityComparer) =>
            new Ref<T>(initialValue, equalityComparer);

        /// <summary>
        ///     Creates and returns a new <see cref="IReadOnlyRef{T}"/> instance whose value
        ///     is (re-)computed by the specified <paramref name="compute"/> function both initially
        ///     and whenever a dependency in the specified <paramref name="dependencies"/> array changes.
        ///     <br/>
        ///     <c>Computed</c> is therefore ideal to create a ref whose value depends on other refs.
        /// </summary>
        /// <typeparam name="TResult">
        ///     The type of the value produced by <paramref name="compute"/> and held by the ref.
        /// </typeparam>
        /// <param name="compute">
        ///     <para>
        ///         A function which, when invoked, computes the ref's new value.
        ///     </para>
        ///     <para>
        ///         It is a good practice to keep this function free of side effects.
        ///         For side effects, use one of the <see cref="Watch(Action, IDependency[])"/>
        ///         and <see cref="WatchEffect(Action, IDependency[])"/> functions.
        ///     </para>
        /// </param>
        /// <param name="dependencies">
        ///     <para>
        ///         A set of dependencies which will be watched for changes.
        ///     </para>
        ///     <para>
        ///         This can be any object implementing the <see cref="IDependency"/> interface.
        ///         Refs implement this interface and can directly be passed.
        ///         Composed also provides a way to convert any <see cref="IObservable{T}"/> to an
        ///         <see cref="IDependency"/> via the
        ///         <see cref="ObservableExtensions.ToDependency{T}(IObservable{T})"/> function.
        ///     </para>
        ///     <para>
        ///         If this is empty, ref's value will never be recomputed.
        ///     </para>
        /// </param>
        /// <returns>
        ///     A new mutable <see cref="IReadOnlyRef{T}"/> instance which initially holds the value returned
        ///     by the <paramref name="compute"/> function and recomputes that value whenever a dependency
        ///     in the specified <paramref name="dependencies"/> array changes.
        /// </returns>
        /// <example>
        ///     The following code demonstrates how <c>Computed</c> can be used to create a ref whose value depends
        ///     on another ref:
        ///
        ///     <code>
        ///     using System;
        ///     using Composed;
        ///     using static Composed.Compose;
        ///
        ///     IRef&lt;int&gt; number = Ref(0);
        ///     IReadOnlyRef&lt;int&gt; doubleNumber = Computed(() => number.Value * 2, number);
        ///
        ///     Watch(() => Console.WriteLine($"number: {number.Value}, doubleNumber: {doubleNumber.Value}), doubleNumber);
        ///
        ///     number.Value = 1;
        ///     number.Value = 2;
        ///
        ///     // Output:
        ///     // number: 1, doubleNumber: 2
        ///     // number: 2, doubleNumber: 4
        ///     </code>
        /// </example>
        public static IReadOnlyRef<TResult> Computed<TResult>(Func<TResult> compute, params IDependency[] dependencies) =>
            Computed(compute, equalityComparer: null, dependencies);

        /// <summary>
        ///     <para>
        ///         Creates and returns a new <see cref="IReadOnlyRef{T}"/> instance whose value
        ///         is (re-)computed by the specified <paramref name="compute"/> function both initially
        ///         and whenever a dependency in the specified <paramref name="dependencies"/> array changes.
        ///         <br/>
        ///         <c>Computed</c> is therefore ideal to create a ref whose value depends on other refs.
        ///     </para>
        ///     <para>
        ///         This overload allows you to specify the <see cref="IEqualityComparer{T}"/> to be used
        ///         by the ref for determining whether dependencies will be notified about a value change.
        ///     </para>
        /// </summary>
        /// <typeparam name="TResult">
        ///     The type of the value produced by <paramref name="compute"/> and held by the ref.
        /// </typeparam>
        /// <param name="compute">
        ///     <para>
        ///         A function which, when invoked, computes the ref's new value.
        ///     </para>
        ///     <para>
        ///         It is a good practice to keep this function free of side effects.
        ///         For side effects, use one of the <see cref="Watch(Action, IDependency[])"/>
        ///         and <see cref="WatchEffect(Action, IDependency[])"/> functions.
        ///     </para>
        /// </param>
        /// <param name="equalityComparer">
        ///     <para>
        ///         An <see cref="IEqualityComparer{T}"/> instance which will be used to compare the
        ///         ref's new and old value when it is changed.
        ///         The ref will only notify its dependents of the change when the equality comparer
        ///         considers the two values unequal.
        ///     </para>
        ///     <para>
        ///         If this is <see langword="null"/>, <see cref="EqualityComparer{T}.Default"/> is used instead.
        ///     </para>
        /// </param>
        /// <param name="dependencies">
        ///     <para>
        ///         A set of dependencies which will be watched for changes.
        ///     </para>
        ///     <para>
        ///         This can be any object implementing the <see cref="IDependency"/> interface.
        ///         Refs implement this interface and can directly be passed.
        ///         Composed also provides a way to convert any <see cref="IObservable{T}"/> to an
        ///         <see cref="IDependency"/> via the
        ///         <see cref="ObservableExtensions.ToDependency{T}(IObservable{T})"/> function.
        ///     </para>
        ///     <para>
        ///         If this is empty, ref's value will never be recomputed.
        ///     </para>
        /// </param>
        /// <returns>
        ///     A new mutable <see cref="IReadOnlyRef{T}"/> instance which initially holds the value returned
        ///     by the <paramref name="compute"/> function and recomputes that value whenever a dependency
        ///     in the specified <paramref name="dependencies"/> array changes.
        /// </returns>
        /// <example>
        ///     <para>
        ///         The following code demonstrates how <c>Computed</c> can be used to create a ref whose value depends
        ///         on another ref:
        ///
        ///         <code>
        ///         using System;
        ///         using Composed;
        ///         using static Composed.Compose;
        ///
        ///         IRef&lt;int&gt; number = Ref(0);
        ///         IReadOnlyRef&lt;int&gt; doubleNumber = Computed(() => number.Value * 2, number);
        ///
        ///         Watch(() => Console.WriteLine($"number: {number.Value}, doubleNumber: {doubleNumber.Value}), doubleNumber);
        ///
        ///         number.Value = 1;
        ///         number.Value = 2;
        ///
        ///         // Output:
        ///         // number: 1, doubleNumber: 2
        ///         // number: 2, doubleNumber: 4
        ///         </code>
        ///     </para>
        ///     <para>
        ///         The following code demonstrates the effect of using a custom <see cref="IEqualityComparer{T}"/>:
        ///
        ///         <code>
        ///         using System;
        ///         using Composed;
        ///         using static Composed.Compose;
        ///         
        ///         class NeverEqualEqualityComparer&lt;T&gt; : EqualityComparer&lt;T&gt;
        ///         {
        ///             public override bool Equals(T x, T y) => false;
        ///             
        ///             public override int GetHashCode(T obj) => obj.GetHashCode();
        ///         }
        ///         
        ///         IRef&lt;int&gt; dependency = Ref(0);
        ///         IReadOnlyRef&lt;int&gt; computedWithCustomComparer = Computed(() => 0, new NeverEqualEqualityComparer&lt;int&gt;(), dependency);
        ///         IReadOnlyRef&lt;int&gt; computedWithoutCustomComparer = Computed(() => 0, equalityComparer: null, dependency);
        ///
        ///         Watch(
        ///             () => Console.WriteLine($"computedWithCustomComparer changed. New: {computedWithCustomComparer.Value}"),
        ///             computedWithCustomComparer
        ///         );
        ///
        ///         Watch(
        ///             () => Console.WriteLine($"computedWithoutCustomComparer changed. New: {computedWithoutCustomComparer.Value}"),
        ///             computedWithoutCustomComparer
        ///         );
        ///
        ///         // Note:
        ///         // Changing `dependency.Value` only makes the computed refs recompute their value.
        ///         // `dependency.Value` has no effect on the computation. Therefore it doesn't matter which value is assigned.
        ///         dependency.Value++;
        ///         dependency.Value++;
        ///         
        ///         // Output:
        ///         // computedWithCustomComparer changed. New: 0
        ///         // computedWithCustomComparer changed. New: 0
        ///         </code>
        ///     </para>
        /// </example>
        public static IReadOnlyRef<TResult> Computed<TResult>(
            Func<TResult> compute,
            IEqualityComparer<TResult>? equalityComparer,
            params IDependency[] dependencies
        )
        {
            _ = compute ?? throw new ArgumentNullException(nameof(compute));
            _ = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
            EnsureNoNullInDependencies(dependencies);
            return ComputedImpl(compute, equalityComparer, dependencies);
        }

        /// <summary>
        ///     Watches each dependency specified in the <paramref name="dependencies"/> array for
        ///     changes and invokes the specified <paramref name="effect"/> whenever such a
        ///     change occurs.
        ///     In comparison to <see cref="WatchEffect(Action, IDependency[])"/>, the effect does
        ///     not get invoked before one of the dependencies changes for the first time.
        /// </summary>
        /// <param name="effect">
        ///     A function to be invoked whenever one of the specified dependencies changes.
        /// </param>
        /// <param name="dependencies">
        ///     <para>
        ///         A set of dependencies which will be watched for changes.
        ///     </para>
        ///     <para>
        ///         This can be any object implementing the <see cref="IDependency"/> interface.
        ///         Refs implement this interface and can directly be passed.
        ///         Composed also provides a way to convert any <see cref="IObservable{T}"/> to an
        ///         <see cref="IDependency"/> via the
        ///         <see cref="ObservableExtensions.ToDependency{T}(IObservable{T})"/> function.
        ///     </para>
        ///     <para>
        ///         If this is empty, the <paramref name="effect"/> will never be invoked.
        ///     </para>
        /// </param>
        /// <returns>
        ///     An <see cref="IDisposable"/> instance which, when disposed, ensures that the dependencies
        ///     are no longer being watched.
        ///     This can be used to cancel watching at any time and to free any references to the dependencies
        ///     created by the <c>Watch</c> function.
        /// </returns>
        /// <remarks>
        ///     This synchronous <c>Watch</c> variant has the following behaviors:
        ///
        ///     <list type="bullet">
        ///         <item>
        ///             <description>
        ///                 <c>Watch</c> does not catch any exception thrown by the <paramref name="effect"/> function.
        ///                 If <paramref name="effect"/> throws an exception, <c>Watch</c> stops watching the
        ///                 dependencies and re-throws the exception.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <c>Watch</c> does not catch any exceptions raised by observables in the
        ///                 <paramref name="dependencies"/> array.
        ///                 When an <see cref="IDependency.Changed"/> observable errors, <c>Watch</c> stops watching
        ///                 the dependencies and re-throws the exception.
        ///                 If you specify an <see cref="IObservable{T}"/> as a dependency (for example via code like
        ///                 <c>myObservable.ToDependency()</c>), ensure that the observable either never errors
        ///                 or that you catch such errors before the conversion to a dependency (for example via code
        ///                 like <c>myObservable.Catch(ex => ...).ToDependency()</c>).
        ///                 Refs created by Composed's API never error and can safely be used as dependencies.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <c>Watch</c> stops watching dependencies whose <see cref="IDependency.Changed"/> observable
        ///                 completes.
        ///                 Refs created by Composed's API never complete and will continuously trigger the
        ///                 <paramref name="effect"/> when changed (unless the <c>Watch</c> function's subscription
        ///                 is disposed).
        ///             </description>
        ///         </item>
        ///     </list>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="effect"/>, <paramref name="dependencies"/> or one of the dependencies in
        ///     the <paramref name="dependencies"/> array is <see langword="null"/>.
        /// </exception>
        /// <example>
        ///     <para>
        ///         The following code demonstrates how the <c>Watch</c> function reacts to dependency changes:
        ///
        ///         <code>
        ///         using System;
        ///         using Composed;
        ///         using static Composed.Compose;
        ///
        ///         IRef&lt;int&gt; dependency1 = Ref(0);
        ///         IRef&lt;int&gt; dependency2 = Ref(0);
        ///
        ///         Watch(() => Console.WriteLine($"Effect 1. dependency1: {dependency1.Value}"), dependency1);
        ///         Watch(() => Console.WriteLine($"Effect 2. dependency2: {dependency2.Value}"), dependency2);
        ///         Watch(
        ///             () => Console.WriteLine($"Effect 3. dependency1: {dependency1.Value}, dependency2: {dependency2.Value}"),
        ///             dependency1,
        ///             dependency2
        ///         );
        ///         
        ///         dependency1.Value = 1;
        ///         dependency2.Value = 1;
        ///
        ///         // Output:
        ///         // Effect 1. dependency1: 1
        ///         // Effect 3. dependency1: 1, dependency2: 0
        ///         // Effect 2. dependency2: 1
        ///         // Effect 3. dependency1: 1, dependency2: 1
        ///         </code>
        ///     </para>
        ///     <para>
        ///         The following code demonstrates how the <c>Watch</c> function can be canceled:
        ///
        ///         <code>
        ///         using System;
        ///         using Composed;
        ///         using static Composed.Compose;
        ///
        ///         IRef&lt;int&gt; dependency = Ref(0);
        ///         IDisposable watchSubscription = Watch(() => Console.WriteLine($"Effect ran. dependency: {dependency.Value}"), dependency);
        ///
        ///         dependency.Value = 1;
        ///         watchSubscription.Dispose();
        ///         dependency.Value = 2;
        ///
        ///         // Output:
        ///         // Effect ran. dependency: 1
        ///         </code>
        ///     </para>
        /// </example>
        public static IDisposable Watch(Action effect, params IDependency[] dependencies)
        {
            _ = effect ?? throw new ArgumentNullException(nameof(effect));
            _ = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
            EnsureNoNullInDependencies(dependencies);
            return WatchImpl(effect, runEffectNow: false, dependencies);
        }

        /// <summary>
        ///     Watches each dependency specified in the <paramref name="dependencies"/> array for
        ///     changes and invokes the specified <paramref name="effect"/> whenever such a
        ///     change occurs.
        ///     In comparison to <see cref="WatchEffect(Func{Task}, IDependency[])"/>, the effect does
        ///     not get invoked before one of the dependencies changes for the first time.
        /// </summary>
        /// <param name="effect">
        ///     An asynchronous function to be invoked whenever one of the specified dependencies changes.
        /// </param>
        /// <param name="dependencies">
        ///     <para>
        ///         A set of dependencies which will be watched for changes.
        ///     </para>
        ///     <para>
        ///         This can be any object implementing the <see cref="IDependency"/> interface.
        ///         Refs implement this interface and can directly be passed.
        ///         Composed also provides a way to convert any <see cref="IObservable{T}"/> to an
        ///         <see cref="IDependency"/> via the
        ///         <see cref="ObservableExtensions.ToDependency{T}(IObservable{T})"/> function.
        ///     </para>
        ///     <para>
        ///         If this is empty, the <paramref name="effect"/> will never be invoked.
        ///     </para>
        /// </param>
        /// <returns>
        ///     An <see cref="IDisposable"/> instance which, when disposed, ensures that the dependencies
        ///     are no longer being watched.
        ///     This can be used to cancel watching at any time and to free any references to the dependencies
        ///     created by the <c>Watch</c> function.
        /// </returns>
        /// <remarks>
        ///     This asynchronous <c>Watch</c> variant has the following behaviors:
        ///
        ///     <list type="bullet">
        ///         <item>
        ///             <description>
        ///                 The asynchronous <paramref name="effect"/> is invoked as a "fire and forget" task.
        ///                 This means that <c>Watch</c> will not handle an exception propagated by the <see cref="Task"/>
        ///                 which is returned by <paramref name="effect"/>.
        ///                 Any unhandled exception thrown by the <paramref name="effect"/> function will therefore
        ///                 follow the .NET runtime's exception escalation policy. In most cases, this means that
        ///                 an unhandled exception will, at some point, raise the <see cref="TaskScheduler.UnobservedTaskException"/>
        ///                 event and then be swallowed.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 If the <paramref name="effect"/> function throws an exception before returning a
        ///                 <see cref="Task"/>, the effect exception behavior of the synchronous
        ///                 <see cref="Watch(Action, IDependency[])"/> overload applies.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <c>Watch</c> does not catch any exceptions raised by observables in the
        ///                 <paramref name="dependencies"/> array.
        ///                 When an <see cref="IDependency.Changed"/> observable errors, <c>Watch</c> stops watching
        ///                 the dependencies and re-throws the exception.
        ///                 If you specify an <see cref="IObservable{T}"/> as a dependency (for example via code like
        ///                 <c>myObservable.ToDependency()</c>), ensure that the observable either never errors
        ///                 or that you catch such errors before the conversion to a dependency (for example via code
        ///                 like <c>myObservable.Catch(ex => ...).ToDependency()</c>).
        ///                 Refs created by Composed's API never error and can safely be used as dependencies.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <c>Watch</c> stops watching dependencies whose <see cref="IDependency.Changed"/> observable
        ///                 completes.
        ///                 Refs created by Composed's API never complete and will continuously trigger the
        ///                 <paramref name="effect"/> when changed (unless the <c>Watch</c> function's subscription
        ///                 is disposed).
        ///             </description>
        ///         </item>
        ///     </list>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="effect"/>, <paramref name="dependencies"/> or one of the dependencies in
        ///     the <paramref name="dependencies"/> array is <see langword="null"/>.
        /// </exception>
        /// <example>
        ///     <para>
        ///         The following code demonstrates how the asynchronous <c>Watch</c> function reacts to dependency changes:
        ///
        ///         <code>
        ///         using System;
        ///         using Composed;
        ///         using static Composed.Compose;
        ///
        ///         IRef&lt;int&gt; dependency1 = Ref(0);
        ///         IRef&lt;int&gt; dependency2 = Ref(0);
        ///
        ///         Watch(
        ///             async () =>
        ///             {
        ///                 await Task.Delay(1000); // You can replace this with any other async invocation.
        ///                 Console.WriteLine($"Effect 1. dependency1: {dependency1.Value}");
        ///             },
        ///             dependency1
        ///         );
        ///
        ///         Watch(
        ///             async () =>
        ///             {
        ///                 await Task.Delay(1000); // You can replace this with any other async invocation.
        ///                 Console.WriteLine($"Effect 2. dependency2: {dependency2.Value}");
        ///             },
        ///             dependency2
        ///         );
        ///
        ///         Watch(
        ///             async () =>
        ///             {
        ///                 await Task.Delay(1000); // You can replace this with any other async invocation.
        ///                 Console.WriteLine($"Effect 3. dependency1: {dependency1.Value}, dependency2: {dependency2.Value}");
        ///             },
        ///             dependency1,
        ///             dependency2
        ///         );
        ///         
        ///         dependency1.Value = 1;
        ///         dependency2.Value = 1;
        ///
        ///         // Output:
        ///         // Effect 1. dependency1: 1
        ///         // Effect 3. dependency1: 1, dependency2: 0
        ///         // Effect 2. dependency2: 1
        ///         // Effect 3. dependency1: 1, dependency2: 1
        ///         </code>
        ///     </para>
        ///     <para>
        ///         The following code demonstrates how the asynchronous <c>Watch</c> function can be canceled:
        ///
        ///         <code>
        ///         using System;
        ///         using Composed;
        ///         using static Composed.Compose;
        ///
        ///         IRef&lt;int&gt; dependency = Ref(0);
        ///         IDisposable watchSubscription = Watch(
        ///             () =>
        ///             {
        ///                 await Task.Delay(1000); // You can replace this with any other async invocation.
        ///                 Console.WriteLine($"Effect ran. dependency: {dependency.Value}");
        ///             },
        ///             dependency
        ///         );
        ///
        ///         dependency.Value = 1;
        ///         watchSubscription.Dispose();
        ///         dependency.Value = 2;
        ///
        ///         // Output:
        ///         // Effect ran. dependency: 1
        ///         </code>
        ///     </para>
        /// </example>
        public static IDisposable Watch(Func<Task> effect, params IDependency[] dependencies)
        {
            _ = effect ?? throw new ArgumentNullException(nameof(effect));
            _ = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
            EnsureNoNullInDependencies(dependencies);
            return Watch(_ => effect(), dependencies);
        }

        /// <summary>
        ///     Watches each dependency specified in the <paramref name="dependencies"/> array for
        ///     changes and invokes the specified <paramref name="effect"/> whenever such a
        ///     change occurs.
        ///     In comparison to <see cref="WatchEffect(Func{CancellationToken, Task}, IDependency[])"/>, the effect does
        ///     not get invoked before one of the dependencies changes for the first time.
        ///     This overload provides a <see cref="CancellationToken"/> which notifies the <paramref name="effect"/>
        ///     when the subscription is canceled. 
        /// </summary>
        /// <param name="effect">
        ///     <para>
        ///         An asynchronous function to be invoked whenever one of the specified dependencies changes.
        ///     </para>
        ///     <para>
        ///         This function receives a <see cref="CancellationToken"/> which notifies the effect about a
        ///         <c>Watch</c> cancellation.
        ///         Effects can use this <see cref="CancellationToken"/> to stop or prevent ongoing asynchronous
        ///         operations when <c>Watch</c> stops watching the specified <paramref name="dependencies"/>.
        ///     </para>
        /// </param>
        /// <param name="dependencies">
        ///     <para>
        ///         A set of dependencies which will be watched for changes.
        ///     </para>
        ///     <para>
        ///         This can be any object implementing the <see cref="IDependency"/> interface.
        ///         Refs implement this interface and can directly be passed.
        ///         Composed also provides a way to convert any <see cref="IObservable{T}"/> to an
        ///         <see cref="IDependency"/> via the
        ///         <see cref="ObservableExtensions.ToDependency{T}(IObservable{T})"/> function.
        ///     </para>
        ///     <para>
        ///         If this is empty, the <paramref name="effect"/> will never be invoked.
        ///     </para>
        /// </param>
        /// <returns>
        ///     An <see cref="IDisposable"/> instance which, when disposed, ensures that the dependencies
        ///     are no longer being watched.
        ///     This can be used to cancel watching at any time and to free any references to the dependencies
        ///     created by the <c>Watch</c> function.
        /// </returns>
        /// <remarks>
        ///     This asynchronous <c>Watch</c> variant has the following behaviors:
        ///
        ///     <list type="bullet">
        ///         <item>
        ///             <description>
        ///                 The asynchronous <paramref name="effect"/> is invoked as a "fire and forget" task.
        ///                 This means that <c>Watch</c> will not handle an exception propagated by the <see cref="Task"/>
        ///                 which is returned by <paramref name="effect"/>.
        ///                 Any unhandled exception thrown by the <paramref name="effect"/> function will therefore
        ///                 follow the .NET runtime's exception escalation policy. In most cases, this means that
        ///                 an unhandled exception will, at some point, raise the <see cref="TaskScheduler.UnobservedTaskException"/>
        ///                 event and then be swallowed.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 If the <paramref name="effect"/> function throws an exception before returning a
        ///                 <see cref="Task"/>, the effect exception behavior of the synchronous
        ///                 <see cref="Watch(Action, IDependency[])"/> overload applies.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <c>Watch</c> does not catch any exceptions raised by observables in the
        ///                 <paramref name="dependencies"/> array.
        ///                 When an <see cref="IDependency.Changed"/> observable errors, <c>Watch</c> stops watching
        ///                 the dependencies and re-throws the exception.
        ///                 If you specify an <see cref="IObservable{T}"/> as a dependency (for example via code like
        ///                 <c>myObservable.ToDependency()</c>), ensure that the observable either never errors
        ///                 or that you catch such errors before the conversion to a dependency (for example via code
        ///                 like <c>myObservable.Catch(ex => ...).ToDependency()</c>).
        ///                 Refs created by Composed's API never error and can safely be used as dependencies.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <c>Watch</c> stops watching dependencies whose <see cref="IDependency.Changed"/> observable
        ///                 completes.
        ///                 Refs created by Composed's API never complete and will continuously trigger the
        ///                 <paramref name="effect"/> when changed (unless the <c>Watch</c> function's subscription
        ///                 is disposed).
        ///             </description>
        ///         </item>
        ///     </list>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="effect"/>, <paramref name="dependencies"/> or one of the dependencies in
        ///     the <paramref name="dependencies"/> array is <see langword="null"/>.
        /// </exception>
        /// <example>
        ///     <para>
        ///         The following code demonstrates how the asynchronous <c>Watch</c> function reacts to dependency changes:
        ///
        ///         <code>
        ///         using System;
        ///         using Composed;
        ///         using static Composed.Compose;
        ///
        ///         IRef&lt;int&gt; dependency1 = Ref(0);
        ///         IRef&lt;int&gt; dependency2 = Ref(0);
        ///
        ///         Watch(
        ///             async cancellationToken =>
        ///             {
        ///                 await Task.Delay(1000); // You can replace this with any other async invocation.
        ///                 Console.WriteLine($"Effect 1. dependency1: {dependency1.Value}");
        ///             },
        ///             dependency1
        ///         );
        ///
        ///         Watch(
        ///             async cancellationToken =>
        ///             {
        ///                 await Task.Delay(1000); // You can replace this with any other async invocation.
        ///                 Console.WriteLine($"Effect 2. dependency2: {dependency2.Value}");
        ///             },
        ///             dependency2
        ///         );
        ///
        ///         Watch(
        ///             async cancellationToken =>
        ///             {
        ///                 await Task.Delay(1000); // You can replace this with any other async invocation.
        ///                 Console.WriteLine($"Effect 3. dependency1: {dependency1.Value}, dependency2: {dependency2.Value}");
        ///             },
        ///             dependency1,
        ///             dependency2
        ///         );
        ///         
        ///         dependency1.Value = 1;
        ///         dependency2.Value = 1;
        ///
        ///         // Output:
        ///         // Effect 1. dependency1: 1
        ///         // Effect 3. dependency1: 1, dependency2: 0
        ///         // Effect 2. dependency2: 1
        ///         // Effect 3. dependency1: 1, dependency2: 1
        ///         </code>
        ///     </para>
        ///     <para>
        ///         The following code demonstrates how the asynchronous <c>Watch</c> function can be canceled:
        ///
        ///         <code>
        ///         using System;
        ///         using Composed;
        ///         using static Composed.Compose;
        ///
        ///         IRef&lt;int&gt; dependency = Ref(0);
        ///         IDisposable watchSubscription = Watch(
        ///             cancellationToken =>
        ///             {
        ///                 Console.WriteLine($"Effect ran. dependency: {dependency.Value}");
        ///                 
        ///                 await Task.Delay(1000);
        ///
        ///                 // After waiting for the above time, `watchSubscription.Dispose()` should have been
        ///                 // called. The cancellationToken should therefore notify us that the effect should
        ///                 // also cancel ASAP.
        ///                 Console.WriteLine($"Should effect {dependency.Value} be canceled? {cancellationToken.IsCancellationRequested}");
        ///             },
        ///             dependency
        ///         );
        ///
        ///         dependency.Value = 1;
        ///         watchSubscription.Dispose();
        ///         dependency.Value = 2;
        ///
        ///         // Output:
        ///         // Effect ran. dependency: 1
        ///         // Should effect 1 be canceled? True
        ///         </code>
        ///     </para>
        /// </example>
        public static IDisposable Watch(Func<CancellationToken, Task> effect, params IDependency[] dependencies)
        {
            _ = effect ?? throw new ArgumentNullException(nameof(effect));
            _ = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
            EnsureNoNullInDependencies(dependencies);
            return WatchImpl(effect, runEffectNow: false, dependencies);
        }

        /// <summary>
        ///     Immediately invokes the specified <paramref name="effect"/> once.
        ///     Afterwards, watches each dependency specified in the <paramref name="dependencies"/>
        ///     array for changes and invokes the specified <paramref name="effect"/> whenever such a
        ///     change occurs.
        /// </summary>
        /// <param name="effect">
        ///     A function to be invoked immediately as well as whenever one of the specified
        ///     dependencies changes.
        /// </param>
        /// <param name="dependencies">
        ///     <para>
        ///         A set of dependencies which will be watched for changes.
        ///     </para>
        ///     <para>
        ///         This can be any object implementing the <see cref="IDependency"/> interface.
        ///         Refs implement this interface and can directly be passed.
        ///         Composed also provides a way to convert any <see cref="IObservable{T}"/> to an
        ///         <see cref="IDependency"/> via the
        ///         <see cref="ObservableExtensions.ToDependency{T}(IObservable{T})"/> function.
        ///     </para>
        ///     <para>
        ///         If this is empty, the <paramref name="effect"/> will never be invoked.
        ///     </para>
        /// </param>
        /// <returns>
        ///     An <see cref="IDisposable"/> instance which, when disposed, ensures that the dependencies
        ///     are no longer being watched.
        ///     This can be used to cancel watching at any time and to free any references to the dependencies
        ///     created by the <c>WatchEffect</c> function.
        /// </returns>
        /// <remarks>
        ///     This synchronous <c>WatchEffect</c> variant has the following behaviors:
        ///
        ///     <list type="bullet">
        ///         <item>
        ///             <description>
        ///                 <c>WatchEffect</c> does not catch any exception thrown by the <paramref name="effect"/> function.
        ///                 If <paramref name="effect"/> throws an exception, <c>WatchEffect</c> stops watching the
        ///                 dependencies and re-throws the exception.
        ///                 this is also the case for the initial <paramref name="effect"/> invocation.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <c>WatchEffect</c> does not catch any exceptions raised by observables in the
        ///                 <paramref name="dependencies"/> array.
        ///                 When an <see cref="IDependency.Changed"/> observable errors, <c>WatchEffect</c> stops watching
        ///                 the dependencies and re-throws the exception.
        ///                 If you specify an <see cref="IObservable{T}"/> as a dependency (for example via code like
        ///                 <c>myObservable.ToDependency()</c>), ensure that the observable either never errors
        ///                 or that you catch such errors before the conversion to a dependency (for example via code
        ///                 like <c>myObservable.Catch(ex => ...).ToDependency()</c>).
        ///                 Refs created by Composed's API never error and can safely be used as dependencies.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <c>WatchEffect</c> stops watching dependencies whose <see cref="IDependency.Changed"/> observable
        ///                 completes.
        ///                 Refs created by Composed's API never complete and will continuously trigger the
        ///                 <paramref name="effect"/> when changed (unless the <c>WatchEffect</c> function's subscription
        ///                 is disposed).
        ///             </description>
        ///         </item>
        ///     </list>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="effect"/>, <paramref name="dependencies"/> or one of the dependencies in
        ///     the <paramref name="dependencies"/> array is <see langword="null"/>.
        /// </exception>
        /// <example>
        ///     <para>
        ///         The following code demonstrates how the <c>WatchEffect</c> function both immediately
        ///         invokes the effect and reacts to dependency changes:
        ///
        ///         <code>
        ///         using System;
        ///         using Composed;
        ///         using static Composed.Compose;
        ///
        ///         IRef&lt;int&gt; dependency1 = Ref(0);
        ///         IRef&lt;int&gt; dependency2 = Ref(0);
        ///
        ///         WatchEffect(() => Console.WriteLine($"Effect 1. dependency1: {dependency1.Value}"), dependency1);
        ///         WatchEffect(() => Console.WriteLine($"Effect 2. dependency2: {dependency2.Value}"), dependency2);
        ///         WatchEffect(
        ///             () => Console.WriteLine($"Effect 3. dependency1: {dependency1.Value}, dependency2: {dependency2.Value}"),
        ///             dependency1,
        ///             dependency2
        ///         );
        ///
        ///         dependency1.Value = 1;
        ///         dependency2.Value = 1;
        ///
        ///         // Output:
        ///         // Effect 1. dependency1: 0
        ///         // Effect 2. dependency2: 0
        ///         // Effect 3. dependency1: 0, dependency2: 0
        ///         // Effect 1. dependency1: 1
        ///         // Effect 3. dependency1: 1, dependency2: 0
        ///         // Effect 2. dependency2: 1
        ///         // Effect 3. dependency1: 1, dependency2: 1
        ///         </code>
        ///     </para>
        ///     <para>
        ///         The following code demonstrates how the <c>WatchEffect</c> function can be canceled:
        ///
        ///         <code>
        ///         using System;
        ///         using Composed;
        ///         using static Composed.Compose;
        ///
        ///         IRef&lt;int&gt; dependency = Ref(0);
        ///         IDisposable watchSubscription = Watch(() => Console.WriteLine($"Effect ran. dependency: {dependency.Value}"), dependency);
        ///
        ///         dependency.Value = 1;
        ///         watchSubscription.Dispose();
        ///         dependency.Value = 2;
        ///
        ///         // Output:
        ///         // Effect ran. dependency: 0
        ///         // Effect ran. dependency: 1
        ///         </code>
        ///     </para>
        /// </example>
        public static IDisposable WatchEffect(Action effect, params IDependency[] dependencies)
        {
            _ = effect ?? throw new ArgumentNullException(nameof(effect));
            _ = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
            EnsureNoNullInDependencies(dependencies);
            return WatchImpl(effect, runEffectNow: true, dependencies);
        }

        /// <summary>
        ///     Immediately invokes the specified <paramref name="effect"/> once.
        ///     Afterwards, watches each dependency specified in the <paramref name="dependencies"/>
        ///     array for changes and invokes the specified <paramref name="effect"/> whenever such a
        ///     change occurs.
        /// </summary>
        /// <param name="effect">
        ///     An asynchronous function to be invoked immediately as well as whenever one of the specified
        ///     dependencies changes.
        /// </param>
        /// <param name="dependencies">
        ///     <para>
        ///         A set of dependencies which will be watched for changes.
        ///     </para>
        ///     <para>
        ///         This can be any object implementing the <see cref="IDependency"/> interface.
        ///         Refs implement this interface and can directly be passed.
        ///         Composed also provides a way to convert any <see cref="IObservable{T}"/> to an
        ///         <see cref="IDependency"/> via the
        ///         <see cref="ObservableExtensions.ToDependency{T}(IObservable{T})"/> function.
        ///     </para>
        ///     <para>
        ///         If this is empty, the <paramref name="effect"/> will never be invoked.
        ///     </para>
        /// </param>
        /// <returns>
        ///     An <see cref="IDisposable"/> instance which, when disposed, ensures that the dependencies
        ///     are no longer being watched.
        ///     This can be used to cancel watching at any time and to free any references to the dependencies
        ///     created by the <c>WatchEffect</c> function.
        /// </returns>
        /// <remarks>
        ///     This asynchronous <c>WatchEffect</c> variant has the following behaviors:
        ///
        ///     <list type="bullet">
        ///         <item>
        ///             <description>
        ///                 The asynchronous <paramref name="effect"/> is invoked as a "fire and forget" task.
        ///                 This means that <c>Watch</c> will not handle an exception propagated by the <see cref="Task"/>
        ///                 which is returned by <paramref name="effect"/>.
        ///                 Any unhandled exception thrown by the <paramref name="effect"/> function will therefore
        ///                 follow the .NET runtime's exception escalation policy. In most cases, this means that
        ///                 an unhandled exception will, at some point, raise the <see cref="TaskScheduler.UnobservedTaskException"/>
        ///                 event and then be swallowed.
        ///                 this is also the case for the initial <paramref name="effect"/> invocation.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 If the <paramref name="effect"/> function throws an exception before returning a
        ///                 <see cref="Task"/>, the effect exception behavior of the synchronous
        ///                 <see cref="WatchEffect(Action, IDependency[])"/> overload applies.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <c>WatchEffect</c> does not catch any exceptions raised by observables in the
        ///                 <paramref name="dependencies"/> array.
        ///                 When an <see cref="IDependency.Changed"/> observable errors, <c>WatchEffect</c> stops watching
        ///                 the dependencies and re-throws the exception.
        ///                 If you specify an <see cref="IObservable{T}"/> as a dependency (for example via code like
        ///                 <c>myObservable.ToDependency()</c>), ensure that the observable either never errors
        ///                 or that you catch such errors before the conversion to a dependency (for example via code
        ///                 like <c>myObservable.Catch(ex => ...).ToDependency()</c>).
        ///                 Refs created by Composed's API never error and can safely be used as dependencies.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <c>WatchEffect</c> stops watching dependencies whose <see cref="IDependency.Changed"/> observable
        ///                 completes.
        ///                 Refs created by Composed's API never complete and will continuously trigger the
        ///                 <paramref name="effect"/> when changed (unless the <c>WatchEffect</c> function's subscription
        ///                 is disposed).
        ///             </description>
        ///         </item>
        ///     </list>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="effect"/>, <paramref name="dependencies"/> or one of the dependencies in
        ///     the <paramref name="dependencies"/> array is <see langword="null"/>.
        /// </exception>
        /// <example>
        ///     <para>
        ///         The following code demonstrates how the asynchronous <c>WatchEffect</c> function both immediately
        ///         invokes the effect and reacts to dependency changes:
        ///
        ///         <code>
        ///         using System;
        ///         using Composed;
        ///         using static Composed.Compose;
        ///
        ///         IRef&lt;int&gt; dependency1 = Ref(0);
        ///         IRef&lt;int&gt; dependency2 = Ref(0);
        ///
        ///         WatchEffect(
        ///             async () =>
        ///             {
        ///                 await Task.Delay(1000); // You can replace this with any other async invocation.
        ///                 Console.WriteLine($"Effect 1. dependency1: {dependency1.Value}");
        ///             },
        ///             dependency1
        ///         );
        ///         
        ///         WatchEffect(
        ///             async () =>
        ///             {
        ///                 await Task.Delay(1000); // You can replace this with any other async invocation.
        ///                 Console.WriteLine($"Effect 2. dependency2: {dependency2.Value}");
        ///             },
        ///             dependency2
        ///         );
        ///         
        ///         WatchEffect(
        ///             async () =>
        ///             {
        ///                 await Task.Delay(1000); // You can replace this with any other async invocation.
        ///                 Console.WriteLine($"Effect 3. dependency1: {dependency1.Value}, dependency2: {dependency2.Value}");
        ///             },
        ///             dependency1,
        ///             dependency2
        ///         );
        ///
        ///         dependency1.Value = 1;
        ///         dependency2.Value = 1;
        ///
        ///         // Output:
        ///         // Effect 1. dependency1: 0
        ///         // Effect 2. dependency2: 0
        ///         // Effect 3. dependency1: 0, dependency2: 0
        ///         // Effect 1. dependency1: 1
        ///         // Effect 3. dependency1: 1, dependency2: 0
        ///         // Effect 2. dependency2: 1
        ///         // Effect 3. dependency1: 1, dependency2: 1
        ///         </code>
        ///     </para>
        ///     <para>
        ///         The following code demonstrates how the asynchronous <c>WatchEffect</c> function can be canceled:
        ///
        ///         <code>
        ///         using System;
        ///         using Composed;
        ///         using static Composed.Compose;
        ///
        ///         IRef&lt;int&gt; dependency = Ref(0);
        ///         IDisposable watchSubscription = Watch(
        ///             async () =>
        ///             {
        ///                 await Task.Delay(1000); // You can replace this with any other async invocation.
        ///                 Console.WriteLine($"Effect ran. dependency: {dependency.Value}");
        ///             },
        ///             dependency
        ///         );
        ///
        ///         dependency.Value = 1;
        ///         watchSubscription.Dispose();
        ///         dependency.Value = 2;
        ///
        ///         // Output:
        ///         // Effect ran. dependency: 0
        ///         // Effect ran. dependency: 1
        ///         </code>
        ///     </para>
        /// </example>
        public static IDisposable WatchEffect(Func<Task> effect, params IDependency[] dependencies)
        {
            _ = effect ?? throw new ArgumentNullException(nameof(effect));
            _ = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
            EnsureNoNullInDependencies(dependencies);
            return WatchEffect(_ => effect(), dependencies);
        }

        /// <summary>
        ///     Immediately invokes the specified <paramref name="effect"/> once.
        ///     Afterwards, watches each dependency specified in the <paramref name="dependencies"/>
        ///     array for changes and invokes the specified <paramref name="effect"/> whenever such a
        ///     change occurs.
        ///     This overload provides a <see cref="CancellationToken"/> which notifies the <paramref name="effect"/>
        ///     when the subscription is canceled. 
        /// </summary>
        /// <param name="effect">
        ///     <para>
        ///         An asynchronous function to be invoked immediately as well as whenever one of the specified
        ///         dependencies changes.
        ///     </para>
        ///     <para>
        ///         This function receives a <see cref="CancellationToken"/> which notifies the effect about a
        ///         <c>WatchEffect</c> cancellation.
        ///         Effects can use this <see cref="CancellationToken"/> to stop or prevent ongoing asynchronous
        ///         operations when <c>WatchEffect</c> stops watching the specified <paramref name="dependencies"/>.
        ///     </para>
        /// </param>
        /// <param name="dependencies">
        ///     <para>
        ///         A set of dependencies which will be watched for changes.
        ///     </para>
        ///     <para>
        ///         This can be any object implementing the <see cref="IDependency"/> interface.
        ///         Refs implement this interface and can directly be passed.
        ///         Composed also provides a way to convert any <see cref="IObservable{T}"/> to an
        ///         <see cref="IDependency"/> via the
        ///         <see cref="ObservableExtensions.ToDependency{T}(IObservable{T})"/> function.
        ///     </para>
        ///     <para>
        ///         If this is empty, the <paramref name="effect"/> will never be invoked.
        ///     </para>
        /// </param>
        /// <returns>
        ///     An <see cref="IDisposable"/> instance which, when disposed, ensures that the dependencies
        ///     are no longer being watched.
        ///     This can be used to cancel watching at any time and to free any references to the dependencies
        ///     created by the <c>WatchEffect</c> function.
        /// </returns>
        /// <remarks>
        ///     This asynchronous <c>WatchEffect</c> variant has the following behaviors:
        ///
        ///     <list type="bullet">
        ///         <item>
        ///             <description>
        ///                 The asynchronous <paramref name="effect"/> is invoked as a "fire and forget" task.
        ///                 This means that <c>Watch</c> will not handle an exception propagated by the <see cref="Task"/>
        ///                 which is returned by <paramref name="effect"/>.
        ///                 Any unhandled exception thrown by the <paramref name="effect"/> function will therefore
        ///                 follow the .NET runtime's exception escalation policy. In most cases, this means that
        ///                 an unhandled exception will, at some point, raise the <see cref="TaskScheduler.UnobservedTaskException"/>
        ///                 event and then be swallowed.
        ///                 this is also the case for the initial <paramref name="effect"/> invocation.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 If the <paramref name="effect"/> function throws an exception before returning a
        ///                 <see cref="Task"/>, the effect exception behavior of the synchronous
        ///                 <see cref="WatchEffect(Action, IDependency[])"/> overload applies.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <c>WatchEffect</c> does not catch any exceptions raised by observables in the
        ///                 <paramref name="dependencies"/> array.
        ///                 When an <see cref="IDependency.Changed"/> observable errors, <c>WatchEffect</c> stops watching
        ///                 the dependencies and re-throws the exception.
        ///                 If you specify an <see cref="IObservable{T}"/> as a dependency (for example via code like
        ///                 <c>myObservable.ToDependency()</c>), ensure that the observable either never errors
        ///                 or that you catch such errors before the conversion to a dependency (for example via code
        ///                 like <c>myObservable.Catch(ex => ...).ToDependency()</c>).
        ///                 Refs created by Composed's API never error and can safely be used as dependencies.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <c>WatchEffect</c> stops watching dependencies whose <see cref="IDependency.Changed"/> observable
        ///                 completes.
        ///                 Refs created by Composed's API never complete and will continuously trigger the
        ///                 <paramref name="effect"/> when changed (unless the <c>WatchEffect</c> function's subscription
        ///                 is disposed).
        ///             </description>
        ///         </item>
        ///     </list>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="effect"/>, <paramref name="dependencies"/> or one of the dependencies in
        ///     the <paramref name="dependencies"/> array is <see langword="null"/>.
        /// </exception>
        /// <example>
        ///     <para>
        ///         The following code demonstrates how the asynchronous <c>WatchEffect</c> function both immediately
        ///         invokes the effect and reacts to dependency changes:
        ///
        ///         <code>
        ///         using System;
        ///         using Composed;
        ///         using static Composed.Compose;
        ///
        ///         IRef&lt;int&gt; dependency1 = Ref(0);
        ///         IRef&lt;int&gt; dependency2 = Ref(0);
        ///
        ///         WatchEffect(
        ///             async cancellationToken =>
        ///             {
        ///                 await Task.Delay(1000); // You can replace this with any other async invocation.
        ///                 Console.WriteLine($"Effect 1. dependency1: {dependency1.Value}");
        ///             },
        ///             dependency1
        ///         );
        ///         
        ///         WatchEffect(
        ///             async cancellationToken =>
        ///             {
        ///                 await Task.Delay(1000); // You can replace this with any other async invocation.
        ///                 Console.WriteLine($"Effect 2. dependency2: {dependency2.Value}");
        ///             },
        ///             dependency2
        ///         );
        ///         
        ///         WatchEffect(
        ///             async cancellationToken =>
        ///             {
        ///                 await Task.Delay(1000); // You can replace this with any other async invocation.
        ///                 Console.WriteLine($"Effect 3. dependency1: {dependency1.Value}, dependency2: {dependency2.Value}");
        ///             },
        ///             dependency1,
        ///             dependency2
        ///         );
        ///
        ///         dependency1.Value = 1;
        ///         dependency2.Value = 1;
        ///
        ///         // Output:
        ///         // Effect 1. dependency1: 0
        ///         // Effect 2. dependency2: 0
        ///         // Effect 3. dependency1: 0, dependency2: 0
        ///         // Effect 1. dependency1: 1
        ///         // Effect 3. dependency1: 1, dependency2: 0
        ///         // Effect 2. dependency2: 1
        ///         // Effect 3. dependency1: 1, dependency2: 1
        ///         </code>
        ///     </para>
        ///     <para>
        ///         The following code demonstrates how the asynchronous <c>WatchEffect</c> function can be canceled:
        ///
        ///         <code>
        ///         using System;
        ///         using Composed;
        ///         using static Composed.Compose;
        ///
        ///         IRef&lt;int&gt; dependency = Ref(0);
        ///         IDisposable watchSubscription = Watch(
        ///             async cancellationToken =>
        ///             {
        ///                 Console.WriteLine($"Effect ran. dependency: {dependency.Value}");
        ///                 
        ///                 await Task.Delay(1000);
        ///
        ///                 // After waiting for the above time, `watchSubscription.Dispose()` should have been
        ///                 // called. The cancellationToken should therefore notify us that the effect should
        ///                 // also cancel ASAP.
        ///                 Console.WriteLine($"Should effect {dependency.Value} be canceled? {cancellationToken.IsCancellationRequested}");
        ///             },
        ///             dependency
        ///         );
        ///
        ///         dependency.Value = 1;
        ///         watchSubscription.Dispose();
        ///         dependency.Value = 2;
        ///
        ///         // Output:
        ///         // Effect ran. dependency: 0
        ///         // Effect ran. dependency: 1
        ///         // Should effect 0 be canceled? True
        ///         // Should effect 1 be canceled? True
        ///         </code>
        ///     </para>
        /// </example>
        public static IDisposable WatchEffect(Func<CancellationToken, Task> effect, params IDependency[] dependencies)
        {
            _ = effect ?? throw new ArgumentNullException(nameof(effect));
            _ = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
            EnsureNoNullInDependencies(dependencies);
            return WatchImpl(effect, runEffectNow: true, dependencies);
        }

        private static IReadOnlyRef<TResult> ComputedImpl<TResult>(
            Func<TResult> compute,
            IEqualityComparer<TResult>? equalityComparer,
            params IDependency[] dependencies
        )
        {
            var @ref = new ReadOnlyRef<TResult>(default!, equalityComparer);
            WatchImpl(() => @ref.Value = compute(), runEffectNow: true, dependencies);
            return @ref;
        }

        private static IDisposable WatchImpl(Action effect, bool runEffectNow, params IDependency[] dependencies)
        {
            if (runEffectNow)
            {
                effect();
            }

            if (dependencies.Length == 0)
            {
                return Disposable.Empty;
            }

            return dependencies
                .Select(x => x.Changed)
                .Merge()
                .Subscribe(_ => effect());
        }

        private static IDisposable WatchImpl(
            Func<CancellationToken, Task> effect,
            bool runEffectNow,
            params IDependency[] dependencies
        )
        {
            var observable = runEffectNow
                ? dependencies.Select(x => x.Changed).Prepend(RunEffectImmediatelyObservable).Merge()
                : dependencies.Select(x => x.Changed).Merge();

#pragma warning disable CA2000 // CancellationTokenSource is disposed by the returned disposable.
            var cts = new CancellationTokenSource();
#pragma warning restore CA2000
            var watchSubscription = Disposable.Empty;
            var dependencySubscription = observable
                .Finally(() => watchSubscription.Dispose())
                .Subscribe(x => _ = effect(cts.Token));

            return watchSubscription = Disposable.Create(() =>
            {
                dependencySubscription.Dispose();

                try
                {
                    cts.Cancel();
                }
                finally
                {
                    cts.Dispose();
                }
            });
        }

        private static void EnsureNoNullInDependencies(IDependency[] dependencies)
        {
            foreach (var dependency in dependencies)
            {
                if (dependency is null)
                {
                    throw new ArgumentNullException(
                        nameof(dependencies),
                        "At least one of the specified dependencies is null. " +
                        "Ensure that explicitly specified dependency lists do not contain null values."
                    );
                }
            }
        }
    }
}
