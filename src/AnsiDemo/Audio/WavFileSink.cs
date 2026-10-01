using System.Buffers.Binary;
using AnsiDemo.Contracts;

namespace AnsiDemo.Audio;

/// <summary>
/// Renders exactly <c>totalSamples</c> samples synchronously in <see cref="Start"/> and writes them to a RIFF/WAVE
/// file (16-bit mono PCM at <see cref="AudioFormat.SampleRate"/>) with the canonical 44-byte header.
/// </summary>
public sealed class WavFileSink : IAudioSink
{
    private const int HeaderBytes = 44;
    private const int ChunkSamples = 4096;
    private const ushort BlockAlign = sizeof(short);

    private readonly string _path;
    private readonly long _totalSamples;
    private bool _started;

    public WavFileSink(string path, long totalSamples)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentOutOfRangeException.ThrowIfNegative(totalSamples);
        // RIFF sizes are 32-bit: 36 + dataBytes must fit in a uint.
        ArgumentOutOfRangeException.ThrowIfGreaterThan(totalSamples, (uint.MaxValue - (HeaderBytes - 8)) / BlockAlign);
        _path = path;
        _totalSamples = totalSamples;
    }

    public void Start(ISampleSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (_started)
        {
            throw new InvalidOperationException("The WAV file sink has already been started.");
        }
        _started = true;

        uint dataBytes = (uint)(_totalSamples * BlockAlign);
        using var stream = new FileStream(_path, FileMode.Create, FileAccess.Write, FileShare.None);
        stream.Write(Header(dataBytes));

        var samples = new short[ChunkSamples];
        var bytes = new byte[ChunkSamples * BlockAlign];
        for (long written = 0; written < _totalSamples; written += samples.Length)
        {
            int count = (int)Math.Min(ChunkSamples, _totalSamples - written);
            var chunk = samples.AsSpan(0, count);
            source.Render(chunk);
            for (int i = 0; i < count; i++)
            {
                BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(i * BlockAlign), chunk[i]);
            }
            stream.Write(bytes, 0, count * BlockAlign);
        }
    }

    public void Stop()
    {
    }

    public void Dispose()
    {
    }

    private static byte[] Header(uint dataBytes)
    {
        var header = new byte[HeaderBytes];
        var span = header.AsSpan();
        "RIFF"u8.CopyTo(span);
        BinaryPrimitives.WriteUInt32LittleEndian(span[4..], HeaderBytes - 8 + dataBytes);
        "WAVE"u8.CopyTo(span[8..]);
        "fmt "u8.CopyTo(span[12..]);
        BinaryPrimitives.WriteUInt32LittleEndian(span[16..], 16);
        BinaryPrimitives.WriteUInt16LittleEndian(span[20..], WaveOutNative.WaveFormatPcm);
        BinaryPrimitives.WriteUInt16LittleEndian(span[22..], 1);
        BinaryPrimitives.WriteUInt32LittleEndian(span[24..], AudioFormat.SampleRate);
        BinaryPrimitives.WriteUInt32LittleEndian(span[28..], AudioFormat.SampleRate * BlockAlign);
        BinaryPrimitives.WriteUInt16LittleEndian(span[32..], BlockAlign);
        BinaryPrimitives.WriteUInt16LittleEndian(span[34..], 16);
        "data"u8.CopyTo(span[36..]);
        BinaryPrimitives.WriteUInt32LittleEndian(span[40..], dataBytes);
        return header;
    }
}
