using AnsiDemo.Contracts;
using AnsiDemo.Tests.Support;

namespace AnsiDemo.Tests.Core;

public class PerfTests
{
    [Fact]
    [Trait("Category", "Perf")]
    public void MeanFrameMsIsPositiveAndFinite()
    {
        double ms = Perf.MeanFrameMs("test-pixel", PixelMode.Braille, 80, 24);

        Assert.True(double.IsFinite(ms), $"Mean frame time {ms} ms is not finite.");
        Assert.True(ms > 0, $"Mean frame time {ms} ms is not positive.");
    }
}
