using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AnsiDemo.Contracts;
using AnsiDemo.Core;
using AnsiDemo.Tests.Support;

namespace AnsiDemo.Tests.Effects.Matrix;

public class MatrixEffectTests
{
    private const string Name = "matrix";

    // N.1: no unseeded randomness, wall clock or console in the effect.
    private static readonly Regex Banned = new(@"System\.Random|DateTime|Stopwatch|Console\.", RegexOptions.CultureInvariant);

    [Fact]
    public void SameTimeRendersIdenticalFrames()
    {
        Frame first = Render(80, 24, 2.5);
        Frame second = Render(80, 24, 2.5);

        Assert.Equal(first.ToAnsi(), second.ToAnsi());
    }

    [Theory]
    [InlineData(0.0, "t0")]
    [InlineData(1.0, "t1")]
    [InlineData(4.0, "t4")]
    public void FrameMatchesGolden(double seconds, string golden)
    {
        Golden.Assert(Render(80, 24, seconds), golden);
    }

    [Fact]
    public void FrameAtOneSecondIsAnimatedAndLit()
    {
        Frame t0 = Render(80, 24, 0);
        Frame t1 = Render(80, 24, 1);

        Assert.NotEqual(t0.ToAnsi(), t1.ToAnsi());
        Assert.True(t1.LitCount() >= 39, $"LitCount {t1.LitCount()} is below 2% of 1920 cells.");
    }

    [Fact]
    public void FirstFrameAlreadyShowsRain()
    {
        Assert.True(Render(80, 24, 0).LitCount() >= 39);
    }

    [Theory]
    [InlineData(PixelMode.HalfBlock, 80, 24)]
    [InlineData(PixelMode.HalfBlock, 200, 60)]
    [InlineData(PixelMode.Braille, 80, 24)]
    [InlineData(PixelMode.Braille, 200, 60)]
    public void RendersInBothModesAtMinimumAndLargeSizes(PixelMode mode, int cols, int rows)
    {
        Frame frame = SceneRunner.RenderAt(SceneSpec.Solo(Name, mode), cols, rows, 1.5);

        Assert.True(frame.LitCount() > 0);
    }

    [Fact]
    [Trait("Category", "Perf")]
    public void MeanFrameTimeIsWithinBudget()
    {
        double ms = Perf.MeanFrameMs(Name, PixelMode.Braille, 200, 60);

        Assert.True(ms <= 33, $"Mean frame time {ms:F2} ms exceeds 33 ms.");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(4.0)]
    public void GlyphsArePrintableAsciiOnBlackWithGreenTails(double seconds)
    {
        Frame frame = Render(80, 24, seconds);
        int greenTails = 0;
        for (int y = 0; y < frame.Rows; y++)
        {
            for (int x = 0; x < frame.Cols; x++)
            {
                Cell cell = frame[x, y];
                if (cell.Ch == ' ')
                {
                    continue;
                }

                Assert.InRange(cell.Ch, '!', '~');
                Assert.Equal(Rgb.Black, cell.Bg);
                Assert.True(cell.Fg.G >= cell.Fg.R && cell.Fg.G >= cell.Fg.B, $"Glyph at ({x}, {y}) is not green: {cell.Fg}.");
                if (cell.Fg.G >= 2 * Math.Max(cell.Fg.R, cell.Fg.B) + 40)
                {
                    greenTails++;
                }
            }
        }

        Assert.True(greenTails >= 39, $"Only {greenTails} clearly green tail cells.");
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(3.0)]
    public void EachColumnIsOneDropFadingUpFromItsHead(double seconds)
    {
        Frame frame = Render(80, 24, seconds);
        int nearWhiteHeads = 0;
        for (int x = 0; x < frame.Cols; x++)
        {
            int runs = 0;
            Cell? below = null;
            for (int y = frame.Rows - 1; y >= 0; y--)
            {
                Cell cell = frame[x, y];
                if (cell.Ch == ' ')
                {
                    below = null;
                    continue;
                }

                if (below is null)
                {
                    runs++;
                    if (cell.Fg.R > 150 && cell.Fg.B > 150)
                    {
                        nearWhiteHeads++;
                    }
                }
                else
                {
                    Assert.True(cell.Fg.Luma <= below.Value.Fg.Luma, $"Column {x} gets brighter away from its head at row {y}.");
                }
                below = cell;
            }

            Assert.InRange(runs, 0, 1);
        }

        Assert.True(nearWhiteHeads >= 10, $"Only {nearWhiteHeads} near-white heads.");
    }

    [Fact]
    public void DropsFallAtVariedSpeeds()
    {
        Frame before = Render(80, 24, 0.5);
        Frame after = Render(80, 24, 0.7);
        var moves = new HashSet<int>();
        for (int x = 0; x < before.Cols; x++)
        {
            int from = HeadRow(before, x);
            int to = HeadRow(after, x);
            if (from >= 0 && to >= 0)
            {
                // 0.2 s at 6-25 rows/s moves a head 1.2-5 rows down, too little to leave and respawn.
                Assert.InRange(to - from, 1, 6);
                moves.Add(to - from);
            }
        }

        Assert.True(moves.Count >= 4, $"Only {moves.Count} distinct speeds.");
    }

    [Fact]
    public void GlyphsChangeWhileFalling()
    {
        Frame before = Render(80, 24, 2.0);
        Frame after = Render(80, 24, 2.0 + DemoClock.HeadlessStep * 15);
        int changed = 0;
        for (int y = 0; y < before.Rows; y++)
        {
            for (int x = 0; x < before.Cols; x++)
            {
                char a = before[x, y].Ch;
                char b = after[x, y].Ch;
                if (a != ' ' && b != ' ' && a != b)
                {
                    changed++;
                }
            }
        }

        Assert.True(changed >= 5, $"Only {changed} glyphs changed in half a second.");
    }

    [Fact]
    public void StepSizeDoesNotChangeTheRain()
    {
        var coarse = new SceneRunner(SceneSpec.Solo(Name, PixelMode.Braille), 80, 24);
        var fine = new SceneRunner(SceneSpec.Solo(Name, PixelMode.Braille), 80, 24);
        for (int i = 0; i < 60; i++)
        {
            coarse.Step(DemoClock.HeadlessStep);
            fine.Step(DemoClock.HeadlessStep / 2);
            fine.Step(DemoClock.HeadlessStep / 2);
        }

        var a = new Frame(80, 24);
        var b = new Frame(80, 24);
        coarse.Render(a);
        fine.Render(b);

        Assert.Equal(a.ToAnsi(), b.ToAnsi());
    }

    [Fact]
    public void CellsOutsideTheDropsAreLeftUntouched()
    {
        var effect = (ICellEffect)EffectCatalog.Create(Name);
        var ctx = new FrameContext(0, 0, 0, PixelMode.Braille, 80, 24, 160, 96);
        effect.Reset(ctx, new DemoRandom(7));
        effect.Update(ctx with { Time = 1.0, DeltaTime = 1.0, Beats = 1.0 * DemoClock.Bpm / 60.0 });

        var marker = new Cell('#', new Rgb(1, 2, 3), new Rgb(4, 5, 6));
        var marked = new Frame(80, 24);
        var unmarked = new Frame(80, 24);
        for (int y = 0; y < 24; y++)
        {
            for (int x = 0; x < 80; x++)
            {
                marked[x, y] = marker;
            }
        }
        effect.Render(marked);
        effect.Render(unmarked);

        Assert.True(unmarked.LitCount() > 0);
        for (int y = 0; y < 24; y++)
        {
            for (int x = 0; x < 80; x++)
            {
                Assert.Equal(unmarked[x, y].Ch == ' ' ? marker : unmarked[x, y], marked[x, y]);
            }
        }
    }

    [Fact]
    public void EffectAvoidsBannedApis()
    {
        string folder = Path.Combine(FindRepoRoot(), "src", "AnsiDemo", "Effects", "Matrix");
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

    private static Frame Render(int cols, int rows, double seconds) =>
        SceneRunner.RenderAt(SceneSpec.Solo(Name, PixelMode.Braille), cols, rows, seconds);

    /// <summary>The row of the near-white head in column x, or -1 when the head is not on screen.</summary>
    private static int HeadRow(Frame frame, int x)
    {
        for (int y = frame.Rows - 1; y >= 0; y--)
        {
            Cell cell = frame[x, y];
            if (cell.Ch != ' ')
            {
                return cell.Fg.R > 150 && cell.Fg.B > 150 ? y : -1;
            }
        }
        return -1;
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
