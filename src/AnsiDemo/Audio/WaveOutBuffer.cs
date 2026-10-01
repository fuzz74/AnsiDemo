using System.Runtime.InteropServices;

namespace AnsiDemo.Audio;

/// <summary>
/// One wave-out buffer: a managed sample array the source renders into, and the unmanaged WAVEHDR and sample memory
/// the driver keeps pointers to while the buffer is queued.
/// </summary>
internal sealed class WaveOutBuffer
{
    private static readonly uint HeaderSize = (uint)Marshal.SizeOf<WaveOutHeader>();
    private static readonly int FlagsOffset = (int)Marshal.OffsetOf<WaveOutHeader>(nameof(WaveOutHeader.Flags));

    private IntPtr _header;
    private IntPtr _data;
    private bool _prepared;

    public WaveOutBuffer(int sampleCount)
    {
        Samples = new short[sampleCount];
        int dataBytes = sampleCount * sizeof(short);
        _data = Marshal.AllocHGlobal(dataBytes);
        _header = Marshal.AllocHGlobal((int)HeaderSize);
        Marshal.StructureToPtr(new WaveOutHeader(_data, (uint)dataBytes), _header, fDeleteOld: false);
    }

    public short[] Samples { get; }

    /// <summary>True once the driver has finished playing the buffer (WHDR_DONE).</summary>
    public bool IsDone => (Marshal.ReadInt32(_header, FlagsOffset) & WaveOutNative.WhdrDone) != 0;

    public int Prepare(IntPtr device)
    {
        int result = WaveOutNative.waveOutPrepareHeader(device, _header, HeaderSize);
        _prepared = result == WaveOutNative.MmsyserrNoError;
        return result;
    }

    /// <summary>Copies <see cref="Samples"/> to unmanaged memory and queues the buffer.</summary>
    public int Write(IntPtr device)
    {
        Marshal.Copy(Samples, 0, _data, Samples.Length);
        int flags = Marshal.ReadInt32(_header, FlagsOffset);
        Marshal.WriteInt32(_header, FlagsOffset, flags & ~WaveOutNative.WhdrDone);
        return WaveOutNative.waveOutWrite(device, _header, HeaderSize);
    }

    /// <summary>Unprepares the header and frees the unmanaged memory; call after waveOutReset.</summary>
    public void Release(IntPtr device)
    {
        if (_prepared)
        {
            if (WaveOutNative.waveOutUnprepareHeader(device, _header, HeaderSize) != WaveOutNative.MmsyserrNoError)
            {
                // The driver may still own the memory: leaking it is safer than freeing it underneath the driver.
                return;
            }
            _prepared = false;
        }
        Marshal.FreeHGlobal(_data);
        Marshal.FreeHGlobal(_header);
        _data = IntPtr.Zero;
        _header = IntPtr.Zero;
    }
}
