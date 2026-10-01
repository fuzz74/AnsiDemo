using System.Runtime.InteropServices;

namespace AnsiDemo.Audio;

/// <summary>WAVEHDR with pointer-sized fields, so the layout is right on x86 and x64.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct WaveOutHeader(IntPtr data, uint bufferLength)
{
    public IntPtr Data = data;
    public uint BufferLength = bufferLength;
    public uint BytesRecorded = 0;
    public IntPtr User = IntPtr.Zero;
    public uint Flags = 0;
    public uint Loops = 0;
    public IntPtr Next = IntPtr.Zero;
    public IntPtr Reserved = IntPtr.Zero;
}
