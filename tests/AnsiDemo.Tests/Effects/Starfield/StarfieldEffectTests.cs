using System.Runtime.CompilerServices;
using AnsiDemo.Contracts;
using AnsiDemo.Core;
using AnsiDemo.Effects.Starfield;
using AnsiDemo.Tests.Support;

namespace AnsiDemo.Tests.Effects.Starfield;

public class StarfieldEffectTests
{
    private const string Name = "starfield";
    private const PixelMode Mode = PixelMode.Braille;

    [Fact]
    public void IsRegisteredWithBrailleAsPreferredMode()
    {
        Assert.Contains(Name, EffectCatalog.Names);
        Assert.Equal(Mode, EffectCatalog.PreferredMode(Name));
        Assert.IsType<StarfieldEffect>(EffectCatalog.Create(Name));
    }

    [Fact]
    public void SameTimeRendersIdenticalFrames()
    {
        string first = SceneRunner.RenderAt(SceneSpec.Solo(Name, Mode), 80, 24, 2.5).ToAnsi();
        string second = SceneRunner.RenderAt(SceneSpec.Solo(Name, Mode), 80, 24, 2.5).ToAnsi();
        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData(0, "t0")]
    [InlineData(1, "t1")]
    [InlineData(4, "t4")]
    public void MatchesGolden(double seconds, string golden)
    {
        Golden.Assert(SceneRunner.RenderAt(SceneSpec.Solo(Name, Mode), 80, 24, seconds), golden);
    }

    [Fact]
    public void AnimatesAndLightsEnoughCells()
    {
        Frame start = SceneRunner.RenderAt(SceneSpec.Solo(Name, Mode), 80, 24, 0);
        Frame later = SceneRunner.RenderAt(SceneSpec.Solo(Name, Mode), 80, 24, 1);
        Assert.NotEqual(start.ToAnsi(), later.ToAnsi());
        Assert.True(later.LitCount() >= 39, $"LitCount {later.LitCount()} < 39");
    }

    [Theory]
    [InlineData(PixelMode.Braille, 80, 24)]
    [InlineData(PixelMode.Braille, 200, 60)]
    [InlineData(PixelMode.HalfBlock, 80, 24)]
    [InlineData(PixelMode.HalfBlock, 200, 60)]
    public void RendersAtMinimumAndLargeSizesInBothModes(PixelMode mode, int cols, int rows)
    {
        Frame frame = SceneRunner.RenderAt(SceneSpec.Solo(Name, mode), cols, rows, 3);
        Assert.True(frame.LitCount() > 0);
    }

    [Fact]
    public void KeepsStarsAfterResize()
    {
        var runner = new SceneRunner(SceneSpec.Solo(Name, Mode), 80, 24);
        runner.Step(DemoClock.HeadlessStep);
        runner.Resize(200, 60);
        runner.Step(DemoClock.HeadlessStep);
        var frame = new Frame(200, 60);
        runner.Render(frame);
        Assert.True(frame.LitCount() >= 200 * 60 / 50, $"LitCount {frame.LitCount()} after resize");
    }

    [Theory]
    [InlineData(PixelMode.Braille)]
    [InlineData(PixelMode.HalfBlock)]
    public void WritesOnlyGreyStarPixelsAndLeavesTheRest(PixelMode mode)
    {
        (int width, int height) = PixelBuffer.SizeFor(mode, 80, 24);
        var effect = new StarfieldEffect();
        effect.Reset(new FrameContext(0, 0, 0, mode, 80, 24, width, height), new DemoRandom(7));
        for (int step = 1; step <= 30; step++)
        {
            double time = step * DemoClock.HeadlessStep;
            effect.Update(new FrameContext(time, DemoClock.HeadlessStep, time * DemoClock.Bpm / 60.0, mode, 80, 24, width, height));
        }

        var background = new Rgb(10, 40, 120);
        var pixels = new PixelBuffer(width, height);
        pixels.Clear(background);
        effect.Render(pixels);

        int kept = pixels.Pixels.Count(p => p == background);
        int changed = pixels.Pixels.Length - kept;
        Assert.True(kept >= pixels.Pixels.Length * 0.8, $"only {kept} of {pixels.Pixels.Length} pixels kept");
        Assert.True(changed >= 200, $"only {changed} star pixels");
        Assert.All(pixels.Pixels.Where(p => p != background), p => Assert.True(p.R == p.G && p.G == p.B, $"{p} is not grey"));
    }

    [Fact]
    [Trait("Category", "Perf")]
    public void MeetsFrameBudget()
    {
        double ms = Perf.MeanFrameMs(Name, Mode, 200, 60);
        Assert.True(ms <= 33, $"mean frame {ms:F2} ms > 33 ms");
    }

    [Fact]
    public void SourceAvoidsConsoleRandomAndClock()
    {
        string folder = Path.Combine(FindRepoRoot(), "src", "AnsiDemo", "Effects", "Starfield");
        string[] files = Directory.GetFiles(folder, "*.cs");
        Assert.Contains(files, file => Path.GetFileName(file) == nameof(StarfieldEffect) + ".cs");

        string[] banned = ["System.Random", "DateTime", "Stopwatch", "Console."];
        var hits = new List<string>();
        foreach (string file in files)
        {
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                foreach (string word in banned)
                {
                    if (lines[i].Contains(word, StringComparison.Ordinal))
                    {
                        hits.Add($"{Path.GetFileName(file)}:{i + 1}: {word}");
                    }
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
