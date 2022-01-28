# Superluminal for Unity3D

This is a dynamic wrapper around the [Superluminal Performance API](https://www.superluminal.eu/docs/documentation.html#dynamic_library) to add [instrumentation data](https://www.superluminal.eu/docs/documentation.html#instrumentation_view) while profiling. This wrapper dynamically loads the PerformanceAPI.dll and APIs from it and provides an easy to use API to add instrumentation to your code. 

## Prerequisites

A working installation of [Superluminal](https://www.superluminal.eu) in a default Program Files forlder.

## Installation

Following the steps in the [Installing from a Git URL](https://docs.unity3d.com/Manual/upm-ui-giturl.html) you can use the `https://github.com/Benedicht/superluminal4u3d.git` URL choosing the `Add package from git URL...` menuitem.

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

## Example

The following code:

```c#
public static void OnUpdate()
{
    using (new SuperluminalEvent("OnUpdate", UnityEngine.Time.frameCount.ToString("N0"), UnityEngine.Color.green))
    {
        RequestEventHelper.ProcessQueue();
        ConnectionEventHelper.ProcessQueue();
        ProtocolEventHelper.ProcessQueue();
        PluginEventHelper.ProcessQueue();

        BestHTTP.Extensions.Timer.Process();

        if (heartbeats != null)
            heartbeats.Update();

        BufferPool.Maintain();
    }
}
```

Produces the following output in Superluminal:
![OnUpdate Example](Documentation~/images/OnUpdateSample.png)

## Why not Superluminal Perf from Alexandre Mutel?

[Superluminal Perf from Alexandre Mutel](https://github.com/xoofx/SuperluminalPerf) uses .NET 5 APIs that are not available under Unity3D (like NativeLibrary).