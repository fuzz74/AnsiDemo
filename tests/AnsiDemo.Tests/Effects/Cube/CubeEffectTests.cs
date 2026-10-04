using System.Runtime.CompilerServices;
using AnsiDemo.Contracts;
using AnsiDemo.Core;
using AnsiDemo.Effects.Cube;
using AnsiDemo.Tests.Support;

namespace AnsiDemo.Tests.Effects.Cube;

public class CubeEffectTests
{
    private const string Name = "cube";

    private static Frame RenderAt(PixelMode mode, int cols, int rows, double t) =>
        SceneRunner.RenderAt(SceneSpec.Solo(Name, mode), cols, rows, t);

    [Fact]
    public void IsRegisteredWithBrailleAsPreferredMode()
    {
        Assert.Contains(Name, EffectCatalog.Names);
        Assert.Equal(PixelMode.Braille, EffectCatalog.PreferredMode(Name));
        Assert.IsType<CubeEffect>(EffectCatalog.Create(Name));
    }

    // 4.1
    [Theory]
    [InlineData(0.0)]
    [InlineData(2.5)]
    public void SameTimeRendersIdenticalFrames(double t)
    {
        string first = RenderAt(PixelMode.Braille, 80, 24, t).ToAnsi();
        string second = RenderAt(PixelMode.Braille, 80, 24, t).ToAnsi();

        Assert.Equal(first, second);
    }

    // 4.2
    [Theory]
    [InlineData(0, "t0")]
    [InlineData(1, "t1")]
    [InlineData(4, "t4")]
    public void MatchesGolden(double t, string golden)
    {
        Golden.Assert(RenderAt(PixelMode.Braille, 80, 24, t), golden);
    }

    // 4.3
    [Fact]
    public void AnimatesAndLightsEnoughCells()
    {
        Frame t0 = RenderAt(PixelMode.Braille, 80, 24, 0);
        Frame t1 = RenderAt(PixelMode.Braille, 80, 24, 1);

        Assert.NotEqual(t0.ToAnsi(), t1.ToAnsi());
        Assert.True(t1.LitCount() >= 39, $"LitCount {t1.LitCount()} < 39 (2% of 1920 cells).");
    }

    // 4.4
    [Theory]
    [InlineData(PixelMode.Braille, 80, 24)]
    [InlineData(PixelMode.Braille, 200, 60)]
    [InlineData(PixelMode.HalfBlock, 80, 24)]
    [InlineData(PixelMode.HalfBlock, 200, 60)]
    public void RendersAtMinimumAndMaximumSizes(PixelMode mode, int cols, int rows)
    {
        Frame frame = RenderAt(mode, cols, rows, 1.5);

        Assert.Equal(cols, frame.Cols);
        Assert.True(frame.LitCount() > 0, $"Nothing lit in {mode} at {cols}x{rows}.");
    }

    // 4.5
    [Fact]
    [Trait("Category", "Perf")]
    public void MeetsFrameBudget()
    {
        double ms = Perf.MeanFrameMs(Name, PixelMode.Braille, 200, 60);

        Assert.True(ms <= 33, $"Mean frame time {ms:F2} ms exceeds 33 ms.");
    }

    // Must look like: spanning at least 1/3 of the height, without leaving the frame.
    [Theory]
    [InlineData(PixelMode.Braille, 80, 24)]
    [InlineData(PixelMode.Braille, 200, 60)]
    [InlineData(PixelMode.HalfBlock, 80, 24)]
    [InlineData(PixelMode.HalfBlock, 200, 60)]
    public void SpansAtLeastAThirdOfTheHeight(PixelMode mode, int cols, int rows)
    {
        var runner = new SceneRunner(SceneSpec.Solo(Name, mode), cols, rows);
        var frame = new Frame(cols, rows);
        for (int step = 0; step <= 30 * 12; step += 7)
        {
            runner.Render(frame);
            (int top, int bottom) = LitRowRange(frame);

            Assert.True(bottom - top + 1 >= rows / 3.0,
                $"At t={runner.Time:F2} lit rows {top}..{bottom} span less than a third of {rows}.");
            Assert.True(top > 0 && bottom < rows - 1, $"At t={runner.Time:F2} the cube touches the frame edge.");

            for (int i = 0; i < 7; i++)
            {
                runner.Step(DemoClock.HeadlessStep);
            }
        }
    }

    [Fact]
    public void EdgeColourCyclesWithTime()
    {
        Assert.NotEqual(EdgeColour(0), EdgeColour(2));
        Assert.NotEqual(EdgeColour(2), EdgeColour(4));
    }

    // Fills: sparse.
    [Theory]
    [InlineData(PixelMode.Braille)]
    [InlineData(PixelMode.HalfBlock)]
    public void DrawsOnlyEdgesAndKeepsTheRest(PixelMode mode)
    {
        (int width, int height) = PixelBuffer.SizeFor(mode, 80, 24);
        var effect = new CubeEffect();
        effect.Reset(new FrameContext(0.7, 0, 0.7 * DemoClock.Bpm / 60.0, mode, 80, 24, width, height), new DemoRandom(1));
        var background = new Rgb(10, 20, 30);
        var pixels = new PixelBuffer(width, height);
        pixels.Clear(background);

        effect.Render(pixels);

        int kept = pixels.Pixels.Count(p => p == background);
        Assert.True(kept >= pixels.Pixels.Length * 8 / 10, $"Only {kept} of {pixels.Pixels.Length} pixels kept the background.");
        Assert.True(kept < pixels.Pixels.Length, "The cube drew nothing.");
    }

    // N.1
    [Fact]
    public void SourceAvoidsConsoleRandomAndClock()
    {
        string folder = Path.Combine(FindRepoRoot(), "src", "AnsiDemo", "Effects", "Cube");
        string[] files = Directory.GetFiles(folder, "*.cs");
        Assert.NotEmpty(files);

        string[] banned = ["System.Random", "DateTime", "Stopwatch", "Console."];
        var hits = new List<string>();
        foreach (string file in files)
        {
            string text = File.ReadAllText(file);
            hits.AddRange(banned.Where(word => text.Contains(word, StringComparison.Ordinal))
                .Select(word => $"{Path.GetFileName(file)}: {word}"));
        }
        Assert.Empty(hits);
    }

    private static (int Top, int Bottom) LitRowRange(Frame frame)
    {
        int top = -1, bottom = -1;
        for (int y = 0; y < frame.Rows; y++)
        {
            for (int x = 0; x < frame.Cols; x++)
            {
                Cell cell = frame[x, y];
                if (cell.Ch != ' ' && (cell.Fg != Rgb.Black || cell.Bg != Rgb.Black))
                {
                    top = top < 0 ? y : top;
                    bottom = y;
                    break;
                }
            }
        }
        Assert.True(top >= 0, "Nothing lit.");
        return (top, bottom);
    }

    private static Rgb EdgeColour(double t)
    {
        Frame frame = RenderAt(PixelMode.HalfBlock, 80, 24, t);
        for (int y = 0; y < frame.Rows; y++)
        {
            for (int x = 0; x < frame.Cols; x++)
            {
                if (frame[x, y].Fg != Rgb.Black)
                {
                    return frame[x, y].Fg;
                }
            }
        }
        throw new InvalidOperationException($"Nothing drawn at t={t}.");
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
