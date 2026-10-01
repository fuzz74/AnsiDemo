using System.Runtime.InteropServices;

namespace AnsiDemo.Audio;

/// <summary>The part of the winmm.dll wave-out API used by <see cref="WaveOutSink"/>. MMRESULT values are returned as int.</summary>
internal static class WaveOutNative
{
    private const string Library = "winmm.dll";

    public const uint WaveMapper = 0xFFFFFFFF;
    public const uint CallbackNull = 0;
    public const ushort WaveFormatPcm = 1;
    public const int MmsyserrNoError = 0;
    public const int MmsyserrBadDeviceId = 2;
    public const int WhdrDone = 0x1;

    [DllImport(Library)]
    public static extern uint waveOutGetNumDevs();

    [DllImport(Library)]
    public static extern int waveOutOpen(out IntPtr handle, uint deviceId, ref WaveOutFormat format,
                                         IntPtr callback, IntPtr instance, uint flags);

    [DllImport(Library)]
    public static extern int waveOutPrepareHeader(IntPtr handle, IntPtr header, uint headerSize);

    [DllImport(Library)]
    public static extern int waveOutWrite(IntPtr handle, IntPtr header, uint headerSize);

    [DllImport(Library)]
    public static extern int waveOutUnprepareHeader(IntPtr handle, IntPtr header, uint headerSize);

    [DllImport(Library)]
    public static extern int waveOutReset(IntPtr handle);

    [DllImport(Library)]
    public static extern int waveOutClose(IntPtr handle);
}
