---
id: viewmodel
---

# Using Composed for ViewModels

(tbd)


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
