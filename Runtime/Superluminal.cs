using System.IO;
using System;
using System.Runtime.InteropServices;
using System.Buffers;
using System.Text;

// Documentation, values and functions are from the PerformanceAPI.h, PerformanceAPI_capi.h and PerformanceAPI_loader.h files.

/// <summary>
/// Wraps a Superluminal BeginEvent/EndEvent pair that can be used in a disposable pattern.
/// </summary>
public struct SuperluminalEvent : IDisposable
{
    public SuperluminalEvent(string id)
        :this(id, null, Superluminal.DefaultColor)
    {}

    public SuperluminalEvent(string id, string data)
        : this(id, data, Superluminal.DefaultColor)
    {}

    public SuperluminalEvent(string id, string data, UnityEngine.Color color)
        :this(id, data, FromColor(color))
    {

    }

    private static uint FromColor(UnityEngine.Color color)
    {
        return (uint)((byte)(color.r * 256) << 24 | (byte)(color.g * 256) << 16 | (byte)(color.b * 256) << 8 | (byte)(color.a * 256));
    }

    public SuperluminalEvent(string id, string data = null, uint color = Superluminal.DefaultColor)
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        Superluminal.BeginEvent(id, data, color);
#endif
    }

    public void Dispose()
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        Superluminal.EndEvent();
#endif
    }
}

public unsafe static class Superluminal
{
    public const uint DefaultColor = 0xFFFFFFFF;

    /// <summary>
    /// When set to false, no Performance API related calls are going to be made.
    /// </summary>
    public static bool IsEnabled = true;

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
    private const uint MajorVersion = 2;
    private const uint MinorVersion = 0;

    private static uint Version => (MajorVersion << 16) | MinorVersion;

    private static volatile bool _isInitialized = false;
    private static IntPtr _lib = IntPtr.Zero;
    private static delegate* unmanaged[Cdecl]<uint, PerformanceAPI_Functions*, uint> PerformanceAPI_GetAPI;
    private static delegate* unmanaged[Cdecl]<byte*, ushort, void> PerformanceAPI_SetCurrentThreadNameN;
    private static delegate* unmanaged[Cdecl]<char*, ushort, char*, ushort, uint, void> PerformanceAPI_BeginEventWideN;
    private static delegate* unmanaged[Cdecl]<PerformanceAPI_SuppressTailCallOptimization> PerformanceAPI_EndEvent;

    private static void Init()
    {
        if (_isInitialized)
            return;
        _isInitialized = true;

        if (!IsEnabled)
            return;

        var dllPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Superluminal", "Performance", "API", "dll", IntPtr.Size == 8 ? "x64" : "x86", "PerformanceAPI.dll");
        if (!File.Exists(dllPath))
        {
            UnityEngine.Debug.LogWarning($"Couldn't find Superluminal API dll: \"{dllPath}\"");
            IsEnabled = false;
            return;
        }

        _lib = LoadLibrary(dllPath);
        if (_lib == IntPtr.Zero)
        {
            UnityEngine.Debug.LogWarning("LoadLibrary couldn't load the dll!");
            IsEnabled = false;
            return;
        }

        PerformanceAPI_GetAPI = (delegate* unmanaged[Cdecl]<uint, PerformanceAPI_Functions*, uint>)GetProcAddress(_lib, "PerformanceAPI_GetAPI");

        if (PerformanceAPI_GetAPI == null)
        {
            UnityEngine.Debug.LogWarning("Couldn't load \"PerformanceAPI_GetAPI\"!");
            FreeLibrary(_lib);
            IsEnabled = false;
            return;
        }

        PerformanceAPI_Functions functionPointers;
        uint retCode = PerformanceAPI_GetAPI(Version, &functionPointers);
        if (retCode != 1)
        {
            UnityEngine.Debug.LogWarning($"\"PerformanceAPI_GetAPI\" returned with code {retCode}");
            FreeLibrary(_lib);
            IsEnabled = false;
            return;
        }

        if (!functionPointers.IsSet())
        {
            UnityEngine.Debug.LogWarning("Could get function pointers!");
            FreeLibrary(_lib);
            IsEnabled = false;
            return;
        }

        PerformanceAPI_SetCurrentThreadNameN = (delegate* unmanaged[Cdecl]<byte*, ushort, void>)functionPointers.SetCurrentThreadNameN;
        PerformanceAPI_BeginEventWideN = (delegate* unmanaged[Cdecl]<char*, ushort, char*, ushort, uint, void>)functionPointers.BeginEventWideN;
        PerformanceAPI_EndEvent = (delegate* unmanaged[Cdecl]<PerformanceAPI_SuppressTailCallOptimization>)functionPointers.EndEvent;
    }
#endif

    /// <summary>
    /// Set the name of the current thread to the specified thread name. 
    /// </summary>
    /// <param name="name">The thread name as an UTF8 encoded string.</param>
    public static void SetCurrentThreadName(string name)
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        if (!IsEnabled)
            return;

        Init();

        if (PerformanceAPI_SetCurrentThreadNameN == null)
            return;

        var byteCount = Encoding.UTF8.GetByteCount(name);

        var buffer = ArrayPool<byte>.Shared.Rent(byteCount);
        Encoding.UTF8.GetBytes(name, buffer);
        fixed (byte* pName = buffer)
        {
            PerformanceAPI_SetCurrentThreadNameN(pName, (ushort)byteCount);
        }
        ArrayPool<byte>.Shared.Return(buffer);
#endif
    }

    /// <summary>
    /// Begin an instrumentation event with the specified ID and runtime data
    /// </summary>
    /// <param name="id">The ID of this scope as an UTF8 encoded string. The ID for a specific scope must be the same over the lifetime of the program (see docs at the top of this file)</param>
    /// <param name="data">[optional] The data for this scope as an UTF8 encoded string. The data can vary for each invocation of this scope and is intended to hold information that is only available at runtime. See docs at the top of this file. Set to null if not available.</param>
    /// <param name="color">[optional] The color for this scope. The color for a specific scope is coupled to the ID and must be the same over the lifetime of the program. Set to DefaultColor to use default coloring.</param>
    public static void BeginEvent(string id, string data = null, uint color = DefaultColor)
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        if (!IsEnabled)
            return;

        Init();

        if (PerformanceAPI_BeginEventWideN == null)
            return;

        fixed (char* pId = id)
        fixed (char* pData = data)
            PerformanceAPI_BeginEventWideN(pId, (ushort)id.Length, pData, data != null ? (ushort)data.Length : (ushort)0, color);
#endif
    }

    /// <summary>
    /// End an instrumentation event. Must be matched with a call to BeginEvent within the same function
    /// </summary>
    public static void EndEvent()
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        if (!IsEnabled)
            return;

        Init();

        if (PerformanceAPI_EndEvent == null)
            return;

        // Note: the return value can be ignored. It is only there to prevent calls to the function from being optimized to jmp instructions as part of tail call optimization.
        PerformanceAPI_SuppressTailCallOptimization ret = PerformanceAPI_EndEvent();
#endif
    }

    /// <summary>
    /// Free the PerformanceAPI module that was previously loaded. After this function is called, you can no longer use the functions.
    /// </summary>
    public static void Free()
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        if (_lib != IntPtr.Zero)
        {
            FreeLibrary(_lib);
            _lib = IntPtr.Zero;
        }
#endif
    }

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
    [DllImport("kernel32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FreeLibrary(IntPtr hModule);

    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32")]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

#pragma warning disable 0649
    struct PerformanceAPI_SuppressTailCallOptimization
    {
        public long SuppressTailCall_0;
        public long SuppressTailCall_1;
        public long SuppressTailCall_2;
    }
#pragma warning restore

    unsafe struct PerformanceAPI_Functions
    {
        public void* SetCurrentThreadName;
        public void* SetCurrentThreadNameN;
        public void* BeginEvent;
        public void* BeginEventN;
        public void* BeginEventWide;
        public void* BeginEventWideN;
        public void* EndEvent;

        public bool IsSet() =>
            this.SetCurrentThreadNameN != null &&
            this.SetCurrentThreadNameN != null &&
            this.BeginEvent != null &&
            this.BeginEventN != null &&
            this.BeginEventWideN != null &&
            this.BeginEventWideN != null &&
            this.EndEvent != null;
    }
#endif
}
