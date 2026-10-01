using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AnsiDemo.Contracts;
using AnsiDemo.Core;
using AnsiDemo.Tests.Support;

namespace AnsiDemo.Tests.Effects.Tunnel;

public class TunnelEffectTests
{
    private const string Name = "tunnel";

    // N.1: no console, unseeded randomness or wall clock in the effect.
    private static readonly Regex Banned = new(@"System\.Random|\bDateTime\b|\bStopwatch\b|\bConsole\.", RegexOptions.CultureInvariant);

    private static Frame RenderAt(PixelMode mode, int cols, int rows, double seconds) =>
        SceneRunner.RenderAt(SceneSpec.Solo(Name, mode), cols, rows, seconds);

    [Fact]
    public void PrefersHalfBlock()
    {
        Assert.Equal(PixelMode.HalfBlock, EffectCatalog.PreferredMode(Name));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(2.5)]
    public void SameInputsGiveIdenticalFrames(double seconds)
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
    public void AnimatesAndKeepsMostCellsLit()
    {
        Frame start = RenderAt(PixelMode.HalfBlock, 80, 24, 0.0);
        Frame later = RenderAt(PixelMode.HalfBlock, 80, 24, 1.0);

        Assert.NotEqual(start.ToAnsi(), later.ToAnsi());
        Assert.True(later.LitCount() >= 1152, $"Only {later.LitCount()} of 1920 cells are lit at t = 1.");
    }

    [Theory]
    [InlineData(PixelMode.HalfBlock, 80, 24)]
    [InlineData(PixelMode.HalfBlock, 200, 60)]
    [InlineData(PixelMode.Braille, 80, 24)]
    [InlineData(PixelMode.Braille, 200, 60)]
    public void RendersAtSupportedSizesInBothModes(PixelMode mode, int cols, int rows)
    {
        Frame frame = RenderAt(mode, cols, rows, 1.0);

        Assert.Equal(cols, frame.Cols);
        Assert.Equal(rows, frame.Rows);
        Assert.True(frame.LitCount() > 0, $"Nothing lit in {mode} at {cols}x{rows}.");
    }

    [Fact]
    public void ResizeKeepsRendering()
    {
        var runner = new SceneRunner(SceneSpec.Solo(Name, PixelMode.HalfBlock), 80, 24);
        for (int i = 0; i < 15; i++)
        {
            runner.Step(DemoClock.HeadlessStep);
        }

        runner.Resize(200, 60);
        var frame = new Frame(200, 60);
        runner.Render(frame);

        Assert.True(frame.LitCount() >= 200 * 60 * 6 / 10, $"Only {frame.LitCount()} cells lit after resize.");
    }

    [Fact]
    [Trait("Category", "Perf")]
    public void MeetsFrameBudget()
    {
        double ms = Perf.MeanFrameMs(Name, PixelMode.HalfBlock, 200, 60);

        Assert.True(ms <= 33, $"Mean frame time {ms:F2} ms exceeds 33 ms.");
    }

    [Fact]
    public void SourceAvoidsConsoleRandomAndClock()
    {
        string folder = Path.Combine(FindRepoRoot(), "src", "AnsiDemo", "Effects", "Tunnel");
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
