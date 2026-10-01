using System.Diagnostics;
using AnsiDemo.Contracts;
using AnsiDemo.Core;

namespace AnsiDemo.Tests.Support;

public static class Perf
{
    private const int WarmUpFrames = 10;
    private const int MeasuredFrames = 60;

    /// SceneRunner for SceneSpec.Solo(effect, mode) at cols×rows; 10 warm-up frames; mean ms of Step(1/30) + Render over 60 frames.
    public static double MeanFrameMs(string effect, PixelMode mode, int cols, int rows)
    {
        var runner = new SceneRunner(SceneSpec.Solo(effect, mode), cols, rows);
        var frame = new Frame(cols, rows);
        for (int i = 0; i < WarmUpFrames; i++)
        {
            runner.Step(DemoClock.HeadlessStep);
            runner.Render(frame);
        }

        long start = Stopwatch.GetTimestamp();
        for (int i = 0; i < MeasuredFrames; i++)
        {
            runner.Step(DemoClock.HeadlessStep);
            runner.Render(frame);
        }
        return Stopwatch.GetElapsedTime(start).TotalMilliseconds / MeasuredFrames;
    }
}
