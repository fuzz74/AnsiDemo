using AnsiDemo.Contracts;

namespace AnsiDemo.Tests.Audio;

/// <summary>
/// A sample source for sink tests that records how often and how much it was asked to render. It renders either
/// silence or a counting sequence (sample n = (short)n). Thread-safe, since WaveOutSink renders on its feeder thread.
/// </summary>
public sealed class SinkTestSource : ISampleSource
{
    private readonly bool _counting;
    private readonly ManualResetEventSlim _rendered = new();
    private long _samplesRendered;
    private int _renderCalls;

    private SinkTestSource(bool counting) => _counting = counting;

    public static SinkTestSource Silence() => new(counting: false);

    public static SinkTestSource Counting() => new(counting: true);

    public long SamplesRendered => Interlocked.Read(ref _samplesRendered);

    public int RenderCalls => Volatile.Read(ref _renderCalls);

    public static short CountingSample(long n) => unchecked((short)n);

    /// <summary>Blocks until Render has been called at least once, or the timeout passes.</summary>
    public bool WaitForRender(TimeSpan timeout) => _rendered.Wait(timeout);

    public void Render(Span<short> buffer)
    {
        long first = Interlocked.Add(ref _samplesRendered, buffer.Length) - buffer.Length;
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = _counting ? CountingSample(first + i) : (short)0;
        }
        Interlocked.Increment(ref _renderCalls);
        _rendered.Set();
    }
}
