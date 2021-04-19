---
id: installation
---

# Installation

Composed is a set of [multiple independent packages](../packages/index.md) hosted on NuGet which can
be added to your project via your prefered IDE's NuGet package manager or by running the following
command (where `PACKAGE_NAME` is replaced by the name of the package that you want to install):

```ps
dotnet add package PACKAGE_NAME
```

Generally, the only *required* package is [`Composed`](../packages/Composed/index.md) which provides 
Composed's Core API and is the foundation on which every other Composed package is built.
You can install `Composed` with the command outlined above:

```ps
dotnet add package Composed
```


## Installing Other Packages

While [`Composed`](../packages/Composed/index.md) provides the foundational API, there are other
optional packages which utilize Composed's API to solve specific problems.
For example, [`Composed.Commands`](../packages/Composed.Commands/index.md) provides
`ICommand` implementations utilizing Composed's API that can, amongst others, be used for WPF
applications.

You can have a look at the [list of packages](../packages/index.md) which are offered to get
an overview about what functionality Composed provides.

Additionally, it is recommended to follow the *Getting Started* guides to get an overview
about Composed's Core API and some of the related packages.
