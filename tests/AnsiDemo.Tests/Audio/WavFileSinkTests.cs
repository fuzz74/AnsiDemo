using System.Buffers.Binary;
using System.Text;
using AnsiDemo.Audio;

namespace AnsiDemo.Tests.Audio;

public class WavFileSinkTests : IDisposable
{
    // Not a multiple of the sink's chunk size, so the last chunk is shorter, and past 32767 so samples wrap negative.
    private const long TotalSamples = 40_000;

    private readonly string _path = Path.Combine(Path.GetTempPath(), $"ansidemo-{Guid.NewGuid():N}.wav");

    public void Dispose()
    {
        File.Delete(_path);
    }

    private byte[] WriteCounting(long totalSamples, out SinkTestSource source)
    {
        source = SinkTestSource.Counting();
        using var sink = new WavFileSink(_path, totalSamples);
        sink.Start(source);
        return File.ReadAllBytes(_path);
    }

    private static string Ascii(byte[] bytes, int offset) => Encoding.ASCII.GetString(bytes, offset, 4);

    private static uint UInt32At(byte[] bytes, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset));

    private static ushort UInt16At(byte[] bytes, int offset) => BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset));

    [Fact]
    public void Start_WritesCanonicalHeader()
    {
        var bytes = WriteCounting(TotalSamples, out _);
        uint dataBytes = (uint)(2 * TotalSamples);

        Assert.Equal("RIFF", Ascii(bytes, 0));
        Assert.Equal(36 + dataBytes, UInt32At(bytes, 4));
        Assert.Equal("WAVE", Ascii(bytes, 8));
        Assert.Equal("fmt ", Ascii(bytes, 12));
        Assert.Equal(16u, UInt32At(bytes, 16));
        Assert.Equal((ushort)1, UInt16At(bytes, 20));
        Assert.Equal((ushort)1, UInt16At(bytes, 22));
        Assert.Equal(44100u, UInt32At(bytes, 24));
        Assert.Equal(88200u, UInt32At(bytes, 28));
        Assert.Equal((ushort)2, UInt16At(bytes, 32));
        Assert.Equal((ushort)16, UInt16At(bytes, 34));
        Assert.Equal("data", Ascii(bytes, 36));
        Assert.Equal(dataBytes, UInt32At(bytes, 40));
    }

    [Fact]
    public void Start_FileLengthIsHeaderPlusTwoBytesPerSample()
    {
        var bytes = WriteCounting(TotalSamples, out _);

        Assert.Equal(44 + 2 * TotalSamples, bytes.LongLength);
    }

    [Fact]
    public void Start_WritesRenderedSamplesLittleEndian()
    {
        var bytes = WriteCounting(TotalSamples, out _);

        for (long n = 0; n < TotalSamples; n++)
        {
            short sample = BinaryPrimitives.ReadInt16LittleEndian(bytes.AsSpan((int)(44 + 2 * n)));
            Assert.Equal(SinkTestSource.CountingSample(n), sample);
        }
    }

    [Fact]
    public void Start_RendersExactlyTotalSamples()
    {
        WriteCounting(TotalSamples, out var source);

        Assert.Equal(TotalSamples, source.SamplesRendered);
    }

    [Fact]
    public void Start_WithZeroSamples_WritesHeaderOnlyAndNeverRenders()
    {
        var bytes = WriteCounting(0, out var source);

        Assert.Equal(44, bytes.Length);
        Assert.Equal(36u, UInt32At(bytes, 4));
        Assert.Equal(0u, UInt32At(bytes, 40));
        Assert.Equal(0, source.RenderCalls);
    }

    [Fact]
    public void Start_Twice_Throws()
    {
        using var sink = new WavFileSink(_path, 100);
        sink.Start(SinkTestSource.Counting());

        Assert.Throws<InvalidOperationException>(() => sink.Start(SinkTestSource.Counting()));
    }

    [Fact]
    public void StopAndDispose_AreNoOps()
    {
        var sink = new WavFileSink(_path, 100);
        sink.Stop();
        sink.Start(SinkTestSource.Counting());
        sink.Stop();
        sink.Stop();
        sink.Dispose();
        sink.Dispose();

        Assert.Equal(44 + 200, new FileInfo(_path).Length);
    }

    [Fact]
    public void Constructor_RejectsNegativeSampleCount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WavFileSink(_path, -1));
    }
}
