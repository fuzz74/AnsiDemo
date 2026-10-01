using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AnsiDemo.Contracts;
using AnsiDemo.Core;
using AnsiDemo.Tests.Support;

namespace AnsiDemo.Tests.Effects.Metaballs;

public class MetaballsEffectTests
{
    private const string Name = "metaballs";

    // N.1: no unseeded randomness, wall clock or console in the effect.
    private static readonly Regex Banned = new(@"System\.Random|DateTime|Stopwatch|Console\.", RegexOptions.CultureInvariant);

    private static Frame RenderAt(PixelMode mode, int cols, int rows, double seconds) =>
        SceneRunner.RenderAt(SceneSpec.Solo(Name, mode), cols, rows, seconds);

    [Fact]
    public void PrefersBraille() => Assert.Equal(PixelMode.Braille, EffectCatalog.PreferredMode(Name));

    [Theory]
    [InlineData(0.0)]
    [InlineData(2.5)]
    public void SameTimeRendersIdentically(double seconds)
    {
        string first = RenderAt(PixelMode.Braille, 80, 24, seconds).ToAnsi();
        string second = RenderAt(PixelMode.Braille, 80, 24, seconds).ToAnsi();
        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData("t0", 0.0)]
    [InlineData("t1", 1.0)]
    [InlineData("t4", 4.0)]
    public void MatchesGolden(string name, double seconds) =>
        Golden.Assert(RenderAt(PixelMode.Braille, 80, 24, seconds), name);

    [Fact]
    public void AnimatesAndLightsEnoughCells()
    {
        Frame start = RenderAt(PixelMode.Braille, 80, 24, 0);
        Frame later = RenderAt(PixelMode.Braille, 80, 24, 1);
        Assert.NotEqual(start.ToAnsi(), later.ToAnsi());
        Assert.True(later.LitCount() >= 39, $"LitCount {later.LitCount()} < 39");
    }

    [Theory]
    [InlineData(PixelMode.Braille, 80, 24)]
    [InlineData(PixelMode.Braille, 200, 60)]
    [InlineData(PixelMode.HalfBlock, 80, 24)]
    [InlineData(PixelMode.HalfBlock, 200, 60)]
    [InlineData(PixelMode.Braille, 81, 25)]
    public void RendersAtAnySize(PixelMode mode, int cols, int rows)
    {
        Frame frame = RenderAt(mode, cols, rows, 1);
        Assert.True(frame.LitCount() > 0);
    }

    [Fact]
    public void RendersAfterResize()
    {
        var runner = new SceneRunner(SceneSpec.Solo(Name, PixelMode.Braille), 80, 24);
        runner.Step(DemoClock.HeadlessStep);
        runner.Resize(200, 60);
        var frame = new Frame(200, 60);
        runner.Render(frame);
        Assert.True(frame.LitCount() > 0);
    }

    [Fact]
    [Trait("Category", "Perf")]
    public void MeetsFrameBudget()
    {
        double ms = Perf.MeanFrameMs(Name, PixelMode.Braille, 200, 60);
        Assert.True(ms <= 33, $"Mean frame {ms:F1} ms > 33 ms");
    }

    [Fact]
    public void AvoidsRandomClockAndConsole()
    {
        string folder = Path.Combine(FindRepoRoot(), "src", "AnsiDemo", "Effects", "Metaballs");
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
