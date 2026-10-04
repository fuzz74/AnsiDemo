using System.Runtime.CompilerServices;
using AnsiDemo.Contracts;
using AnsiDemo.Core;
using AnsiDemo.Tests.Support;

namespace AnsiDemo.Tests.Effects.Fire;

public class FireEffectTests
{
    private const string Name = "fire";

    private static Frame At(double t, PixelMode mode = PixelMode.HalfBlock, int cols = 80, int rows = 24) =>
        SceneRunner.RenderAt(SceneSpec.Solo(Name, mode), cols, rows, t);

    [Fact]
    public void PrefersHalfBlock()
    {
        Assert.Equal(PixelMode.HalfBlock, EffectCatalog.PreferredMode(Name));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(2.5)]
    public void SameTimeGivesSameFrame(double t)
    {
        Assert.Equal(At(t).ToAnsi(), At(t).ToAnsi());
    }

    [Theory]
    [InlineData(0.0, "t0")]
    [InlineData(1.0, "t1")]
    [InlineData(4.0, "t4")]
    public void MatchesGolden(double t, string golden)
    {
        Golden.Assert(At(t), golden);
    }

    [Fact]
    public void AnimatesAndLightsEnoughCells()
    {
        Frame first = At(0);
        Frame second = At(1);

        Assert.NotEqual(first.ToAnsi(), second.ToAnsi());
        Assert.True(second.LitCount() >= 39, $"LitCount {second.LitCount()} < 39");
    }

    [Theory]
    [InlineData(PixelMode.HalfBlock, 80, 24)]
    [InlineData(PixelMode.HalfBlock, 200, 60)]
    [InlineData(PixelMode.Braille, 80, 24)]
    [InlineData(PixelMode.Braille, 200, 60)]
    public void RendersAtMinimumAndMaximumSize(PixelMode mode, int cols, int rows)
    {
        Frame frame = At(1, mode, cols, rows);

        Assert.Equal(cols, frame.Cols);
        Assert.Equal(rows, frame.Rows);
        Assert.True(frame.LitCount() > 0);
    }

    [Theory]
    [InlineData(PixelMode.HalfBlock, 80, 24)]
    [InlineData(PixelMode.HalfBlock, 200, 60)]
    [InlineData(PixelMode.Braille, 80, 24)]
    [InlineData(PixelMode.Braille, 200, 60)]
    public void FlamesCoolToBlackBeforeTheTop(PixelMode mode, int cols, int rows)
    {
        foreach (double t in new[] { 0.0, 1.0, 4.0 })
        {
            Frame frame = At(t, mode, cols, rows);
            for (int y = 0; y < 2; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    Cell cell = frame[x, y];
                    Assert.True(cell.Ch == ' ' || (cell.Fg == Rgb.Black && cell.Bg == Rgb.Black),
                        $"cell ({x}, {y}) is lit at t = {t}");
                }
            }
        }
    }

    [Fact]
    public void BottomRowBurns()
    {
        Frame frame = At(1);
        int lit = 0;
        for (int x = 0; x < frame.Cols; x++)
        {
            if (frame[x, frame.Rows - 1].Bg != Rgb.Black)
            {
                lit++;
            }
        }
        Assert.True(lit >= frame.Cols / 2, $"only {lit} of {frame.Cols} bottom cells burn");
    }

    [Fact]
    [Trait("Category", "Perf")]
    public void RendersWithinFrameBudget()
    {
        double ms = Perf.MeanFrameMs(Name, PixelMode.HalfBlock, 200, 60);
        Assert.True(ms <= 33, $"{ms:F1} ms per frame");
    }

    [Fact]
    public void SourceAvoidsConsoleRandomAndClock()
    {
        string folder = Path.Combine(FindRepoRoot(), "src", "AnsiDemo", "Effects", "Fire");
        string[] files = Directory.GetFiles(folder, "*.cs");
        Assert.NotEmpty(files);

        string[] banned = ["System.Random", "DateTime", "Stopwatch", "Console."];
        foreach (string file in files)
        {
            string text = File.ReadAllText(file);
            foreach (string word in banned)
            {
                Assert.False(text.Contains(word, StringComparison.Ordinal), $"{Path.GetFileName(file)} uses {word}");
            }
        }
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
