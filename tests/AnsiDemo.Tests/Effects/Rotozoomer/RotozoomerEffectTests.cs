using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AnsiDemo.Contracts;
using AnsiDemo.Core;
using AnsiDemo.Tests.Support;

namespace AnsiDemo.Tests.Effects.Rotozoomer;

public class RotozoomerEffectTests
{
    private const string Name = "rotozoomer";
    private const int Cols = 80, Rows = 24;

    // N.1: no console, unseeded randomness or wall clock in the effect.
    private static readonly Regex Banned = new(@"System\.Random|\bDateTime|\bStopwatch\b|\bConsole\.", RegexOptions.CultureInvariant);

    private static Frame RenderAt(double seconds, PixelMode mode = PixelMode.HalfBlock, int cols = Cols, int rows = Rows) =>
        SceneRunner.RenderAt(SceneSpec.Solo(Name, mode), cols, rows, seconds);

    [Fact]
    public void PrefersHalfBlock()
    {
        Assert.Equal(PixelMode.HalfBlock, EffectCatalog.PreferredMode(Name));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(2.5)]
    public void SameTimeRendersIdenticalFrames(double seconds)
    {
        Assert.Equal(RenderAt(seconds).ToAnsi(), RenderAt(seconds).ToAnsi());
    }

    [Theory]
    [InlineData(0.0, "t0")]
    [InlineData(1.0, "t1")]
    [InlineData(4.0, "t4")]
    public void MatchesGolden(double seconds, string golden)
    {
        Golden.Assert(RenderAt(seconds), golden);
    }

    [Fact]
    public void AnimatesAndFillsTheScreen()
    {
        Frame first = RenderAt(0.0);
        Frame second = RenderAt(1.0);

        Assert.NotEqual(first.ToAnsi(), second.ToAnsi());
        Assert.True(second.LitCount() >= Cols * Rows * 6 / 10, $"LitCount {second.LitCount()} < 60% of {Cols * Rows}");
    }

    [Theory]
    [InlineData(PixelMode.HalfBlock, 80, 24)]
    [InlineData(PixelMode.HalfBlock, 200, 60)]
    [InlineData(PixelMode.Braille, 80, 24)]
    [InlineData(PixelMode.Braille, 200, 60)]
    public void RendersAtSupportedSizesInBothModes(PixelMode mode, int cols, int rows)
    {
        Frame frame = RenderAt(1.0, mode, cols, rows);

        Assert.Equal(cols, frame.Cols);
        Assert.Equal(rows, frame.Rows);
        Assert.True(frame.LitCount() > 0);
    }

    [Fact]
    public void ResizeKeepsRendering()
    {
        var runner = new SceneRunner(SceneSpec.Solo(Name, PixelMode.HalfBlock), Cols, Rows);
        runner.Step(DemoClock.HeadlessStep);
        runner.Resize(200, 60);
        var frame = new Frame(200, 60);

        runner.Render(frame);

        Assert.Equal(200 * 60, frame.LitCount());
    }

    [Fact]
    [Trait("Category", "Perf")]
    public void MeetsFrameBudget()
    {
        double ms = Perf.MeanFrameMs(Name, PixelMode.HalfBlock, 200, 60);
        Assert.True(ms <= 33, $"{ms:F2} ms per frame");
    }

    [Fact]
    public void SourceAvoidsConsoleRandomAndClock()
    {
        string folder = Path.Combine(FindRepoRoot(), "src", "AnsiDemo", "Effects", "Rotozoomer");
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
