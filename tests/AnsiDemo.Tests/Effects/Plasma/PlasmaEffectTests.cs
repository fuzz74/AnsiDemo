using System.Runtime.CompilerServices;
using AnsiDemo.Contracts;
using AnsiDemo.Core;
using AnsiDemo.Tests.Support;

namespace AnsiDemo.Tests.Effects.Plasma;

public class PlasmaEffectTests
{
    private const string Name = "plasma";
    private const PixelMode Mode = PixelMode.HalfBlock;

    [Fact]
    public void PrefersHalfBlock()
    {
        Assert.Equal(Mode, EffectCatalog.PreferredMode(Name));
    }

    [Fact]
    public void SameTimeRendersIdenticalFrames()
    {
        Frame first = SceneRunner.RenderAt(SceneSpec.Solo(Name, Mode), 80, 24, 2.5);
        Frame second = SceneRunner.RenderAt(SceneSpec.Solo(Name, Mode), 80, 24, 2.5);

        Assert.Equal(first.ToAnsi(), second.ToAnsi());
    }

    [Theory]
    [InlineData(0.0, "t0")]
    [InlineData(1.0, "t1")]
    [InlineData(4.0, "t4")]
    public void MatchesGolden(double seconds, string golden)
    {
        Frame frame = SceneRunner.RenderAt(SceneSpec.Solo(Name, Mode), 80, 24, seconds);

        Golden.Assert(frame, golden);
    }

    [Fact]
    public void AnimatesAndFillsTheScreen()
    {
        Frame start = SceneRunner.RenderAt(SceneSpec.Solo(Name, Mode), 80, 24, 0);
        Frame later = SceneRunner.RenderAt(SceneSpec.Solo(Name, Mode), 80, 24, 1);

        Assert.NotEqual(start.ToAnsi(), later.ToAnsi());
        Assert.True(later.LitCount() >= 1152, $"LitCount {later.LitCount()} < 1152 (60% of 1920 cells)");
    }

    [Fact]
    public void CyclesThroughTheHueRange()
    {
        // The hue offset turns 30° per second, so 6 s later (half a hue cycle) almost every cell has a different colour.
        Frame start = SceneRunner.RenderAt(SceneSpec.Solo(Name, Mode), 80, 24, 0);
        Frame later = SceneRunner.RenderAt(SceneSpec.Solo(Name, Mode), 80, 24, 6);

        int changed = 0;
        for (int y = 0; y < 24; y++)
        {
            for (int x = 0; x < 80; x++)
            {
                if (start[x, y].Fg != later[x, y].Fg)
                {
                    changed++;
                }
            }
        }
        Assert.True(changed > 1920 * 9 / 10, $"only {changed} of 1920 cells changed colour");
    }

    [Theory]
    [InlineData(PixelMode.HalfBlock, 80, 24)]
    [InlineData(PixelMode.HalfBlock, 200, 60)]
    [InlineData(PixelMode.Braille, 80, 24)]
    [InlineData(PixelMode.Braille, 200, 60)]
    public void RendersAtMinimumAndMaximumSizes(PixelMode mode, int cols, int rows)
    {
        Frame frame = SceneRunner.RenderAt(SceneSpec.Solo(Name, mode), cols, rows, 1);

        Assert.Equal(cols, frame.Cols);
        Assert.Equal(rows, frame.Rows);
        Assert.True(frame.LitCount() > 0);
    }

    [Fact]
    public void SurvivesResize()
    {
        var runner = new SceneRunner(SceneSpec.Solo(Name, Mode), 80, 24);
        runner.Step(DemoClock.HeadlessStep);
        runner.Resize(200, 60);
        runner.Step(DemoClock.HeadlessStep);
        var frame = new Frame(200, 60);

        runner.Render(frame);

        Assert.Equal(200 * 60, frame.LitCount());
    }

    [Fact]
    [Trait("Category", "Perf")]
    public void MeetsFrameBudget()
    {
        double ms = Perf.MeanFrameMs(Name, Mode, 200, 60);

        Assert.True(ms <= 33, $"mean frame {ms:F2} ms > 33 ms");
    }

    [Fact]
    public void AvoidsConsoleRandomAndClock()
    {
        string folder = Path.Combine(FindRepoRoot(), "src", "AnsiDemo", "Effects", "Plasma");
        string[] files = Directory.GetFiles(folder, "*.cs");
        Assert.NotEmpty(files);

        string[] banned = ["System.Random", "DateTime", "Stopwatch", "Console."];
        var hits = new List<string>();
        foreach (string file in files)
        {
            string text = File.ReadAllText(file);
            hits.AddRange(banned.Where(api => text.Contains(api, StringComparison.Ordinal))
                .Select(api => $"{Path.GetFileName(file)}: {api}"));
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
