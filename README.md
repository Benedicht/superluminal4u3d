# Superluminal for Unity3D

This is a dynamic wrapper around the [Superluminal Performance API](https://www.superluminal.eu/docs/documentation.html#dynamic_library) to add [instrumentation data](https://www.superluminal.eu/docs/documentation.html#instrumentation_view) while profiling. This wrapper dynamically loads the PerformanceAPI.dll and APIs from it and provides an easy to use API to add instrumentation to your code. 

## Prerequisites

A working installation of [Superluminal](https://www.superluminal.eu) in a default Program Files forlder.

## Installation

Following the steps in the [Installing from a Git URL](https://docs.unity3d.com/Manual/upm-ui-giturl.html) you can use the `git@github.com:Benedicht/superluminal4u3d.git` URL.

## Usage

Name you threads:
```c#
Superluminal.SetCurrentThreadName("My Thread");
```

Mark your code with `Superluminal.BeginEvent()`/`EndEvent()`:

```c#
Superluminal.BeginEvent("event name", "optional additional data", UnityEngine.Color.red);
try
{
	// Add your code here
}
finally
{
    Superluminal.EndEvent();
}
```

Or with a more compact syntax using `SuperluminalEvent`:
```c#
using (new SuperluminalEvent("event name", "optional additional data", UnityEngine.Color.red))
{
	// Add your code here
}
```

## Why not Superluminal Perf from Alexandre Mutel?

[Superluminal Perf from Alexandre Mutel](https://github.com/xoofx/SuperluminalPerf) uses .NET 5 APIs that are not available under Unity3D (like NativeLibrary).