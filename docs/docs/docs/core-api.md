---
id: core-api
---

# Composed's Core API

(tbd)


## Requirements

Since this guide is using language features added with C# 9, you should have a .NET SDK >= 5 installed.


## Scaffolding the Project

Create a new console application and install the [`Composed`](../packages/Composed/index.md) package:

```console
dotnet new console
dotnet add package Composed
```

Next, change the contents of your `Program.cs` file to the following code:


```csharp
using System;
using Composed;
using static Composed.Compose;

var message = Ref("Hello world!");
Console.WriteLine(message.Value);
```
