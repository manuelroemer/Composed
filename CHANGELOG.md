# Composed Changelog


## v0.2.2

### Composed

* Added the `ObservableExtensions` class exposing:
  * `AsDependency`: Converts an `IObservable<T>` to an `IObservable<Unit>`, allowing it to be passed
    as a dependency.

### Composed.Commands

* Added the `UseBlockingCommand` hooks which create commands that cannot be executed while already
  executing at the moment.
* Suppress `CanExecute` change notifications during the `Dispose` flow.

### Composed.State

_No changes._



## v0.2.1

### Composed

_No changes._

### Composed.Commands

* Added the `AsyncComposedCommand` class.
* Added the `ComposedCommandBase` class.
* Added new `UseCommand` overloads for creating `AsyncComposedCommand` instances.
* Replaced `ExecuteAction` and `CanExecuteFunc` with `Action` and `Func<T>` equivalents.

### Composed.State

_No changes._



### v0.2.0

### Composed

* Ref interfaces no longer implement `INotifyPropertyChanging`.
* Ref interfaces no longer implement `IObservable<T>`, but they *do* now implement `IObservable<Unit>`.
* Ref interfaces no longer expose the `Changed` property.
* `IReadOnlyRef<T>` now provides the `Notify()` method.
* `IRef<T>` now provides the `SetValue(T value, bool supressNotification)` method.
* The default ref implementation returned by `Ref(T)` locks/synchronizes while comparing the new value with
  the old value and setting it.
* Composition functions (`Computed`, `Watch`, `WatchEffect`) now all provide an `IScheduler` parameter
  on which, if provided, the effect is scheduled.

### Composed.Commands

* Integrated the changes from the "Composed" package (removed `IDependency` references, provide scheduler, etc.).
* Removed any `IComposedCommand` interfaces.
* Removed any command related members with support for a generic `TParameter`.
* Added the non-generic `ComposedCommand` class which replaces the interfaces.
* Updated the `UseCommand` hooks to return `ComposedCommand` instances.

### Composed.State

Initial release.



## v0.1.5

### Composed

* Added a `DebuggerTypeProxy` to any ref created by Composed.

### Composed.Commands

_No changes._



## v0.1.4

### Composed

* Revert to publishing `.snupkg` files.

### Composed.Commands

* Revert to publishing `.snupkg` files.



## v0.1.3

### Composed

* Publish a deterministic NuGet package.
* Include all files relevant for Source Link in the `.pdb`.

### Composed.Commands

* Publish a deterministic NuGet package.
* Include all files relevant for Source Link in the `.pdb`.



## v0.1.2

### Composed

* `.pdb` files are now published within the `nupkg`.

### Composed.Commands

* `.pdb` files are now published within the `nupkg`.



## v0.1.1

### Composed

_No changes._

### Composed.Commands

* In v0.1.0 the symbols package has issues. With the new release, this is hopefully resolved.



## v0.1.0

### Composed

Initial release.

### Composed.Commands

Initial release.
