using System.Runtime.InteropServices;

namespace AnsiDemo.Shell;

/// <summary>The part of the kernel32.dll console API used by <see cref="ConsoleSession"/>.</summary>
internal static class ConsoleNative
{
    private const string Library = "kernel32.dll";

    public const int StdOutputHandle = -11;
    public const uint EnableVirtualTerminalProcessing = 0x0004;
    public static readonly IntPtr InvalidHandleValue = new(-1);

    [DllImport(Library, SetLastError = true)]
    public static extern IntPtr GetStdHandle(int stdHandle);

    [DllImport(Library, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetConsoleMode(IntPtr handle, out uint mode);

    [DllImport(Library, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetConsoleMode(IntPtr handle, uint mode);
}
