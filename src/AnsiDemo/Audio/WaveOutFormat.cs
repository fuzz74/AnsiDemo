using System.Runtime.InteropServices;

namespace AnsiDemo.Audio;

/// <summary>WAVEFORMATEX (18 bytes, packed to 2).</summary>
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct WaveOutFormat
{
    public ushort FormatTag;
    public ushort Channels;
    public uint SamplesPerSec;
    public uint AvgBytesPerSec;
    public ushort BlockAlign;
    public ushort BitsPerSample;
    public ushort Size;

    /// <summary>16-bit mono PCM at <paramref name="sampleRate"/>.</summary>
    public static WaveOutFormat Pcm16Mono(int sampleRate)
    {
        const ushort blockAlign = sizeof(short);
        return new WaveOutFormat
        {
            FormatTag = WaveOutNative.WaveFormatPcm,
            Channels = 1,
            SamplesPerSec = (uint)sampleRate,
            AvgBytesPerSec = (uint)sampleRate * blockAlign,
            BlockAlign = blockAlign,
            BitsPerSample = 16,
            Size = 0,
        };
    }
}
