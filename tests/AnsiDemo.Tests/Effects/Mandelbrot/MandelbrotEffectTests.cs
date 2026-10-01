using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AnsiDemo.Contracts;
using AnsiDemo.Core;
using AnsiDemo.Effects.Mandelbrot;
using AnsiDemo.Tests.Support;

namespace AnsiDemo.Tests.Effects.Mandelbrot;

public class MandelbrotEffectTests
{
    private const string Name = "mandelbrot";
    private const double FrameBudgetMs = 33.0;

    // N.1: no console, unseeded randomness or wall clock in the effect.
    private static readonly Regex Banned = new(@"System\.Random|\bDateTime|\bStopwatch\b|\bConsole\.", RegexOptions.CultureInvariant);

    private static Frame RenderAt(PixelMode mode, int cols, int rows, double seconds) =>
        SceneRunner.RenderAt(SceneSpec.Solo(Name, mode), cols, rows, seconds);

    [Fact]
    public void IsRegisteredWithHalfBlockAsPreferredMode()
    {
        Assert.Contains(Name, EffectCatalog.Names);
        Assert.Equal(PixelMode.HalfBlock, EffectCatalog.PreferredMode(Name));
        Assert.IsType<MandelbrotEffect>(EffectCatalog.Create(Name));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(2.5)]
    public void SameCallTwiceGivesIdenticalAnsi(double seconds)
    {
        string first = RenderAt(PixelMode.HalfBlock, 80, 24, seconds).ToAnsi();
        string second = RenderAt(PixelMode.HalfBlock, 80, 24, seconds).ToAnsi();

        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData(0.0, "t0")]
    [InlineData(1.0, "t1")]
    [InlineData(4.0, "t4")]
    public void MatchesGolden(double seconds, string golden)
    {
        Golden.Assert(RenderAt(PixelMode.HalfBlock, 80, 24, seconds), golden);
    }

    [Fact]
    public void FrameAtOneSecondDiffersFromStartAndIsLit()
    {
        Frame start = RenderAt(PixelMode.HalfBlock, 80, 24, 0.0);
        Frame later = RenderAt(PixelMode.HalfBlock, 80, 24, 1.0);

        Assert.NotEqual(start.ToAnsi(), later.ToAnsi());
        Assert.True(later.LitCount() >= 39, $"Only {later.LitCount()} of 1920 cells are lit.");
    }

    [Theory]
    [InlineData(PixelMode.HalfBlock, 80, 24)]
    [InlineData(PixelMode.HalfBlock, 200, 60)]
    [InlineData(PixelMode.Braille, 80, 24)]
    [InlineData(PixelMode.Braille, 200, 60)]
    public void RendersAtMinimumAndMaximumSizeInBothModes(PixelMode mode, int cols, int rows)
    {
        Frame frame = RenderAt(mode, cols, rows, 1.0);

        Assert.Equal(cols, frame.Cols);
        Assert.Equal(rows, frame.Rows);
        Assert.True(frame.LitCount() > 0, "The frame is completely black.");
    }

    [Fact]
    public void ResizeKeepsRenderingAtTheNewSize()
    {
        var runner = new SceneRunner(SceneSpec.Solo(Name, PixelMode.HalfBlock), 80, 24);
        runner.Step(DemoClock.HeadlessStep);
        runner.Resize(120, 40);
        var frame = new Frame(120, 40);

        runner.Render(frame);

        Assert.True(frame.LitCount() > 0, "The frame is completely black after a resize.");
    }

    [Fact]
    public void ZoomReachesAtLeastTwoToTheTenthByTwentySeconds()
    {
        Assert.Equal(1.0, MandelbrotEffect.ZoomFactor(0.0));
        Assert.True(MandelbrotEffect.ZoomFactor(20.0) >= 1024.0, $"Zoom at 20 s is {MandelbrotEffect.ZoomFactor(20.0)}.");
    }

    [Fact]
    public void ZoomGrowsContinuouslyAndTurnsBackAtItsDeepestPoint()
    {
        double turn = MandelbrotEffect.MaxZoomOctaves / MandelbrotEffect.ZoomOctavesPerSecond;
        double previous = MandelbrotEffect.ZoomFactor(0.0);
        double deepest = previous;
        for (int step = 1; step <= 30 * 300; step++)
        {
            double zoom = MandelbrotEffect.ZoomFactor(step * DemoClock.HeadlessStep);
            Assert.InRange(zoom / previous, 0.9, 1.1);
            deepest = Math.Max(deepest, zoom);
            previous = zoom;
        }

        Assert.InRange(deepest, 0.95 * Math.Pow(2.0, MandelbrotEffect.MaxZoomOctaves), Math.Pow(2.0, MandelbrotEffect.MaxZoomOctaves));
        Assert.True(MandelbrotEffect.ZoomFactor(turn / 2) < MandelbrotEffect.ZoomFactor(turn));
        Assert.True(MandelbrotEffect.ZoomFactor(turn * 1.5) < MandelbrotEffect.ZoomFactor(turn));
    }

    [Fact]
    public void DeepFramesAreLitInsteadOfGarbage()
    {
        double deepest = MandelbrotEffect.MaxZoomOctaves / MandelbrotEffect.ZoomOctavesPerSecond;
        foreach (double seconds in new[] { 19.0, deepest })
        {
            var runner = new SceneRunner(SceneSpec.Solo(Name, PixelMode.HalfBlock), 80, 24);
            for (int i = 0; i < (int)Math.Round(30 * seconds); i++)
            {
                runner.Step(DemoClock.HeadlessStep);
            }
            var frame = new Frame(80, 24);
            runner.Render(frame);

            Assert.True(frame.LitCount() >= 39, $"Only {frame.LitCount()} cells are lit at {seconds} s.");
            Assert.True(frame.ToAnsi().Split('\n').Distinct().Count() > 12, $"The frame at {seconds} s has almost no detail.");
        }
    }

    [Fact]
    [Trait("Category", "Perf")]
    public void MeanFrameTimeAtMaximumSizeIsWithinBudget()
    {
        double ms = Perf.MeanFrameMs(Name, PixelMode.HalfBlock, 200, 60);

        Assert.True(ms <= FrameBudgetMs, $"Mean frame time {ms:F1} ms exceeds {FrameBudgetMs} ms.");
    }

    [Theory]
    [Trait("Category", "Perf")]
    [InlineData(19.0)]
    [InlineData(MandelbrotEffect.MaxZoomOctaves / MandelbrotEffect.ZoomOctavesPerSecond)]
    public void DeepZoomFrameIsWithinBudgetAndLit(double seconds)
    {
        var runner = new SceneRunner(SceneSpec.Solo(Name, PixelMode.HalfBlock), 200, 60);
        for (int i = 0; i < (int)Math.Round(30 * seconds); i++)
        {
            runner.Step(DemoClock.HeadlessStep);
        }
        var frame = new Frame(200, 60);
        runner.Render(frame);

        const int Frames = 20;
        long start = Stopwatch.GetTimestamp();
        for (int i = 0; i < Frames; i++)
        {
            runner.Render(frame);
        }
        double ms = Stopwatch.GetElapsedTime(start).TotalMilliseconds / Frames;

        Assert.True(ms <= FrameBudgetMs, $"Frame time at {seconds} s is {ms:F1} ms, over {FrameBudgetMs} ms.");
        Assert.True(frame.LitCount() >= 2 * 200 * 60 / 100, $"Only {frame.LitCount()} cells are lit at {seconds} s.");
    }

    [Fact]
    public void SourceAvoidsConsoleRandomAndClock()
    {
        string folder = Path.Combine(FindRepoRoot(), "src", "AnsiDemo", "Effects", "Mandelbrot");
        string[] files = Directory.GetFiles(folder, "*.cs");
        Assert.NotEmpty(files);

        var hits = new List<string>();
        foreach (string file in files)
        {
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                if (Banned.IsMatch(lines[i]))
                {
                    hits.Add($"{Path.GetFileName(file)}:{i + 1}: {lines[i].Trim()}");
                }
            }
        }
        Assert.Empty(hits);
    }

    private static string FindRepoRoot([CallerFilePath] string callerFile = "")
    {
        for (string? dir = Path.GetDirectoryName(callerFile); dir is not null; dir = Path.GetDirectoryName(dir))
        {
            if (File.Exists(Path.Combine(dir, "AnsiDemo.slnx")))
            {
                return dir;
            }
        }
        throw new InvalidOperationException($"AnsiDemo.slnx not found above {callerFile}.");
    }
}
