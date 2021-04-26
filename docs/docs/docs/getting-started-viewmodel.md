---
id: getting-started-viewmodel
---

# Composed ViewModels

Now that you have an overview over [Composed's Core API](./core-api.md), it is time to tackle
the topic that Composed has actually been built for: UIs.

This guide will show you how you can leverage Composed to easily build reactive view models.
It will also introduce you to the [Composed.Commands](../packages/Composed.Commands/index.md)
package which can be incredibly useful for UI frameworks like WPF or WinUI.

The final result of this guide will look like this:


:::note
This guide presents Composed in the context of a WPF application using an MVVM-like architecture.
It should be noted that the concepts presented here can also be applied to other environments.
[Composed's base package](../packages/Composed/index.md) is entirely independent of any UI framework
or architecture and **does not** force you to use an MVVM approach at all.
:::


## Scaffolding the Project

:::note
This guide assumes that you are at least using .NET 5 with C# 9.
You *can* use any .NET runtime supporting .NET Standard 2.0, but you may have to manually adapt the
code to the lower language version.
:::

If you want to run the code examples of the following sections, you can create a new WPF
application and install the [`Composed`](../packages/Composed/index.md) and 
[`Composed.Commands`](../packages/Composed.Commands/index.md) packages:

```shell
dotnet new wpf -n Counter
dotnet add package Composed
dotnet add package Composed.Commands
```


## Building the View

Open the `MainWindow.xaml` file and change the code to look like this:

```xml
<Window
  x:Class="Counter.MainWindow"
  xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:local="clr-namespace:Counter"
>
  <Window.DataContext>
    <local:MainWindowViewModel />
  </Window.DataContext>
  
  <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
    <TextBlock Text="Count:" />
    <TextBlock Text="{Binding Count.Value}" />

    <Button
      Content="Increment"
      Command="{Binding Increment}"
    />
    <Button
      Content="Decrement"
      Command="{Binding Decrement}"
    />
  </StackPanel>
</Window>
```


## Building the ViewModel

```csharp
using Composed;
using Composed.Commands;
using static Composed.Compose;
using static Composed.Commands.Compose;

public class MainWindowViewModel {
    public IReadOnlyRef<int> Count { get; }

    public ComposedCommand Increment { get; }

    public ComposedCommand Decrement { get; }

    public MainWindowViewModel() {
        var count = Ref(0);
        Count = count;
        Increment = UseCommand(() => count.Value++);
        Decrement = UseCommand(() => count.Value--);
    }
}
```
