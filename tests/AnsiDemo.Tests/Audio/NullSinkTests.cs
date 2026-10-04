using AnsiDemo.Audio;

namespace AnsiDemo.Tests.Audio;

public class NullSinkTests
{
    [Fact]
    public void StartAndStop_NeverRender()
    {
        var source = SinkTestSource.Counting();
        using var sink = new NullSink();

        sink.Start(source);
        sink.Stop();
        sink.Stop();

        Assert.Equal(0, source.RenderCalls);
    }

    [Fact]
    public void StopAndDispose_WithoutStart_AreAccepted()
    {
        var sink = new NullSink();

        sink.Stop();
        sink.Dispose();
        sink.Dispose();
    }
}
