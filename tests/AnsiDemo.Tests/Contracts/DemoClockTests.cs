using AnsiDemo.Contracts;

namespace AnsiDemo.Tests.Contracts;

public class DemoClockTests
{
    [Fact]
    public void SecondsPerBarIsOnePointNineTwo()
    {
        Assert.Equal(1.92, DemoClock.SecondsPerBar, 12);
    }

    [Fact]
    public void HeadlessStepIsOneThirtieth()
    {
        Assert.Equal(1.0 / 30, DemoClock.HeadlessStep);
    }
}
