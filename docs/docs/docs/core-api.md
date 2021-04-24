---
id: core-api
---

# Composed's Core API

(tbd)


## Requirements

Since this guide is using language features added with C# 9, you should have a .NET SDK >= 5 installed.


## Scaffolding the Project

Create a new console application and install the [`Composed`](../packages/Composed/index.md) package:

```shell
dotnet new console
dotnet add package Composed
```

Next, change the contents of your `Program.cs` file to the following code:


```csharp
using System;
using Composed;
using static Composed.Compose;

IRef<string> message = Ref("Hello World!");
Console.WriteLine(message.Value);

// Output:
// Hello World!
```

:::note
The following code examples assume that you have the above's `using` directives in place.
:::


## Refs

Refs (short for *reference(s)*) are the heart of Composed's API.
A ref is essentially a wrapper around a value `T` which allows you to read and write that value
and additionally emits notifications whenever that value changes.
Refs have the following (simplified) interface and can be created via the `Ref(T initialValue)` function:

```csharp
// The basic interface of a ref. (Note: This is simplified, some advanced members are missing).
interface IRef<T> : INotifyPropertyChanged, IObservable<Unit> {
  T Value { get; set; }
}

// Refs can be created using the `Compose.Ref<T>(T initialValue)` function.
// Otherwise they can be used like any other variable.
var countRef = Ref(0);
Console.WriteLine($"Initial value: {countRef.Value}");

countRef.Value = 123;
countRef.Value -= 23;
Console.WriteLine($"New value: {countRef.Value}");

// Output:
// Initial value: 0
// New value: 100
```

As you can see in the interface declaration above, `IRef<T>` implements `INotifyPropertyChanged` and
`IObservable<Unit>`. Whenever a ref's `Value` property changes, its `PropertyChanged` event is raised
and a new change notification is dispatched to observers.
This makes refs **reactive** and consequently enables you and others to **react** to value changes.
As a practical example, UI frameworks like WPF can directly create UI bindings to refs and thus
automatically update the UI when a ref's value changes.

You yourself can also react to value changes of a ref. Composed provides three different functions
for you to do just that:

* `Watch`
* `WatchEffect`
* `Computed`


## The `Watch` Function

`Watch` is the simplest of the three functions. It *watches* one or more refs (called the
*dependencies*) and runs a callback function (called the *effect*) whenever one of the ref's
values changes:

```csharp
var count = Ref(0);
Watch(() => Console.WriteLine($"count: {count.Value}"), count);
//    ^ This is the effect.                                        ^ This is a dependency.

count.Value = 1;
count.Value = 2;

// Output:
// count: 1
// count: 2
```

`Watch` accepts an arbitrary number of dependencies and will run the effect whenever one of them changes:

```csharp
var firstName = Ref("John");
var lastName = Ref("Doe");
Watch(
  () => Console.WriteLine($"Full name: {firstName.Value} {lastName.Value}"),
  firstName, lastName
);

firstName.Value = "Jane";
lastName.Value = "Roe";

// Output:
// Full name: Jane Doe
// Full name: Jane Roe
```

`Watch` returns an `IDisposable`. When disposed, the `Watch` *subscription* is torn down.
`Watch` stops watching any of the dependencies and thus won't run the effect anymore:

```csharp
var count = Ref(0);
var subscription = Watch(() => Console.WriteLine($"count: {count.Value}"), count);

count.Value = 1;
subscription.Dispose();
count.Value = 2;

// Output:
// count: 1
```

:::important
It is recommended to always dispose subscriptions when they are no longer required.
Not doing so can create memory leaks (similar to stale .NET event handlers or Rx subscriptions).
:::

Finally, `Watch` also supports asynchronous effects. These can optionally accept a
`CancellationToken` which is canceled when the `Watch` subscription is disposed:

```csharp
var count = Ref(0);
Watch(async (cancellationToken) => {
  // count.Value is immediately stored in a variable, because the value can
  // (and does) change while waiting for `Task.Delay` to finish.
  var currentCount = count.Value;
  await Task.Delay(1000, cancellationToken);
  Console.WriteLine("count {currentCount} one second ago.");
}, count);

count.Value = 1;
count.Value = 2;

// Output:
// count 1 one second ago.
// count 2 one second ago.
```


## The `WatchEffect` Function

`WatchEffect` has the **same** behavior as `Watch` with a single difference:
In addition to running the effect whenever one of the dependencies changes, `WatchEffect` also
runs the effect immediately when called:

```csharp
var count = Ref(0);
WatchEffect(() => Console.WriteLine($"count: {count.Value}"), count);

count.Value = 1;
count.Value = 2;

// Output:
// count: 0
// count: 1
// count: 2
```

Apart from this difference, the two functions do exactly the same. `WatchEffect` also supports an
arbitrary number of dependencies, allows asynchronous effects and returns a disposer for the
subscription.
