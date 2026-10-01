using System.Diagnostics;
using AnsiDemo.Audio;
using AnsiDemo.Contracts;

namespace AnsiDemo.Tests.Audio;

public class WaveOutSinkTests
{
    private const int QueuedSamples = 4 * 2048;

    [Fact]
    public void StopAndDispose_WithoutStart_AreSafeAndRepeatable()
    {
        var sink = new WaveOutSink();

        sink.Stop();
        sink.Stop();
        sink.Dispose();
        sink.Dispose();
        sink.Stop();
    }

    [Fact]
    public void Start_AfterDispose_Throws()
    {
        var sink = new WaveOutSink();
        sink.Dispose();

        Assert.Throws<ObjectDisposedException>(() => sink.Start(SinkTestSource.Silence()));
    }

    [Fact]
    public void Start_WithNullSource_Throws()
    {
        using var sink = new WaveOutSink();

        Assert.Throws<ArgumentNullException>(() => sink.Start(null!));
    }

    [Fact]
    public void Start_WithoutDevice_ThrowsBadDeviceId()
    {
        if (WaveOutSink.DeviceCount > 0)
        {
            return;
        }
        using var sink = new WaveOutSink();

        var ex = Assert.Throws<AudioDeviceException>(() => sink.Start(SinkTestSource.Silence()));
        Assert.Equal(2, ex.Code);
    }

    [Fact]
    [Trait("Category", "Device")]
    public void Start_RendersWithin200Ms()
    {
        if (WaveOutSink.DeviceCount == 0)
        {
            return;
        }
        var source = SinkTestSource.Silence();
        using var sink = new WaveOutSink();

        var stopwatch = Stopwatch.StartNew();
        sink.Start(source);
        var remaining = TimeSpan.FromMilliseconds(200) - stopwatch.Elapsed;
        bool rendered = source.WaitForRender(remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero);

        Assert.True(rendered, $"no Render call within 200 ms (Start took {stopwatch.ElapsedMilliseconds} ms)");
        Assert.True(source.SamplesRendered >= QueuedSamples, $"only {source.SamplesRendered} samples queued");
    }

    [Fact]
    [Trait("Category", "Device")]
    public void Start_KeepsRefillingBuffers()
    {
        if (WaveOutSink.DeviceCount == 0)
        {
            return;
        }
        var source = SinkTestSource.Silence();
        using var sink = new WaveOutSink();

        sink.Start(source);
        bool refilled = SpinWait.SpinUntil(() => source.SamplesRendered >= 2 * QueuedSamples, TimeSpan.FromSeconds(3));

        Assert.True(refilled, $"only {source.SamplesRendered} samples rendered after 3 s");
    }

    [Fact]
    [Trait("Category", "Device")]
    public void Start_Twice_Throws()
    {
        if (WaveOutSink.DeviceCount == 0)
        {
            return;
        }
        using var sink = new WaveOutSink();
        sink.Start(SinkTestSource.Silence());

        Assert.Throws<InvalidOperationException>(() => sink.Start(SinkTestSource.Silence()));
    }

    [Fact]
    [Trait("Category", "Device")]
    public void Stop_ReturnsWithin500MsAndEndsRendering()
    {
        if (WaveOutSink.DeviceCount == 0)
        {
            return;
        }
        var source = SinkTestSource.Silence();
        var sink = new WaveOutSink();
        sink.Start(source);
        Thread.Sleep(100);

        var stopwatch = Stopwatch.StartNew();
        sink.Stop();
        stopwatch.Stop();
        int callsAfterStop = source.RenderCalls;
        Thread.Sleep(100);

        Assert.True(stopwatch.ElapsedMilliseconds <= 500, $"Stop took {stopwatch.ElapsedMilliseconds} ms");
        Assert.Equal(callsAfterStop, source.RenderCalls);
        sink.Stop();
        Assert.Throws<InvalidOperationException>(() => sink.Start(SinkTestSource.Silence()));
        sink.Dispose();
    }
}
